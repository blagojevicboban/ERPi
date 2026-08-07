using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class RacunOtpremnicaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly RacunOtpremnica? _existingRacun;
    private readonly ObservableCollection<RacunOtpremnicaStavka> _stavke = new();

    public RacunOtpremnicaEditWindow(ErpiDbContext db, RacunOtpremnica? existingRacun = null)
    {
        InitializeComponent();
        _db = db;
        _existingRacun = existingRacun;
        DgStavke.ItemsSource = _stavke;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var artikli = _db.Artikli.OrderBy(a => a.Naziv).ToList();
            ColArtikal.ItemsSource = artikli;

            var partneri = _db.Partneri.Where(p => p.JeKupac).OrderBy(p => p.Naziv).ToList();
            CmbPartner.ItemsSource = partneri;

            var magacini = _db.Magacini.OrderBy(m => m.SifraMagacina).ToList();
            CmbMagacin.ItemsSource = magacini;
            if (magacini.Count > 0) CmbMagacin.SelectedIndex = 0;

            if (_existingRacun != null)
            {
                ChkPredracun.IsChecked = _existingRacun.TipDokumenta == TipRacunOtpremnice.Predracun;
                DpRokVazenja.SelectedDate = _existingRacun.RokVazenjaPredracuna;
                AzurirajNaslovITipPolja();
                TxtBrojRacuna.Text = _existingRacun.BrojRacuna.ToString();
                TxtBrojRacuna.IsReadOnly = true;
                TxtBrojOtpremnice.Text = _existingRacun.BrojOtpremnice ?? _existingRacun.BrojRacuna.ToString();
                DpDatum.SelectedDate = _existingRacun.DatumRacuna;
                if (_existingRacun.PartnerId.HasValue)
                {
                    CmbPartner.SelectedItem = partneri.FirstOrDefault(p => p.PartnerId == _existingRacun.PartnerId.Value);
                }
                TxtRokPlacanja.Text = _existingRacun.RokPlacanjaDana.ToString();
                CmbNacinPlacanja.Text = _existingRacun.NacinPlacanja ?? "Virman (račun)";
                if (_existingRacun.MagacinId.HasValue)
                {
                    CmbMagacin.SelectedItem = magacini.FirstOrDefault(m => m.MagacinId == _existingRacun.MagacinId.Value);
                }

                foreach (var st in _existingRacun.Stavke.OrderBy(s => s.RedniBroj))
                {
                    _stavke.Add(new RacunOtpremnicaStavka
                    {
                        RedniBroj = st.RedniBroj,
                        ArtikalId = st.ArtikalId,
                        Kolicina = st.Kolicina,
                        ProdajnaCena = st.ProdajnaCena,
                        RabatProcenat = st.RabatProcenat,
                        StopaPdv = st.StopaPdv,
                        Osnovica = st.Osnovica,
                        IznosPdv = st.IznosPdv,
                        Ukupno = st.Ukupno
                    });
                }
            }
            else
            {
                AzurirajNaslovITipPolja();
                DpDatum.SelectedDate = DateTime.Now;

                int maxBr = (_db.RacuniOtpremnice.Select(r => (int?)r.BrojRacuna).Max() ?? 0) + 1;
                TxtBrojRacuna.Text = maxBr.ToString();
                TxtBrojOtpremnice.Text = maxBr.ToString();

                _stavke.Add(new RacunOtpremnicaStavka { RedniBroj = 1, StopaPdv = 20m });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju podataka: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChkPredracun_CheckedChanged(object sender, RoutedEventArgs e) => AzurirajNaslovITipPolja();

    private void AzurirajNaslovITipPolja()
    {
        bool jePredracun = ChkPredracun.IsChecked == true;
        TxtRokVazenjaLabel.Visibility = jePredracun ? Visibility.Visible : Visibility.Collapsed;
        DpRokVazenja.Visibility = jePredracun ? Visibility.Visible : Visibility.Collapsed;

        string osnova = jePredracun ? "predračuna" : "računa-otpremnice";
        TxtNaslov.Text = _existingRacun != null
            ? $"✏️ Izmena {osnova} #{_existingRacun.BrojRacuna}"
            : $"➕ Novi {(jePredracun ? "predračun" : "račun - otpremnica")}";
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new RacunOtpremnicaStavka { RedniBroj = _stavke.Count + 1, StopaPdv = 20m });
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is RacunOtpremnicaStavka model)
        {
            _stavke.Remove(model);
            int rbr = 1;
            foreach (var s in _stavke) s.RedniBroj = rbr++;
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojRacuna.Text.Trim(), out int brRacuna))
        {
            MessageBox.Show("Molimo unesite ispravan broj računa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Partner? partner = CmbPartner.SelectedItem as Partner;
        ERPiData.Models.Magacin.Magacin? magacin = CmbMagacin.SelectedItem as ERPiData.Models.Magacin.Magacin;

        int.TryParse(TxtRokPlacanja.Text, out int rokDana);

        // Stavka je validna ako je roba iz šifarnika (ArtikalId) ILI slobodna usluga (OpisUsluge) —
        // zakon o fiskalizaciji ne pravi razliku između robe i usluge, faktura mora moći da nosi i jedno i drugo.
        var validneStavke = _stavke.Where(s =>
            ((s.ArtikalId is int aid && aid > 0) || !string.IsNullOrWhiteSpace(s.OpisUsluge)) && s.Kolicina > 0).ToList();
        if (validneStavke.Count == 0)
        {
            MessageBox.Show("Unesite bar jednu validnu stavku (robu iz šifarnika ili opis usluge) sa količinom većom od 0.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool imaRobnihStavki = validneStavke.Any(s => s.ArtikalId is int rid && rid > 0);
        if (imaRobnihStavki && magacin == null)
        {
            MessageBox.Show("Izaberite magacin iz koga se izdaje roba (obavezno kad faktura ima robne stavke).", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var service = new RacunOtpremnicaService(_db);

            var racun = _existingRacun ?? new RacunOtpremnica();
            racun.TipDokumenta = ChkPredracun.IsChecked == true ? TipRacunOtpremnice.Predracun : TipRacunOtpremnice.Racun;
            racun.RokVazenjaPredracuna = racun.TipDokumenta == TipRacunOtpremnice.Predracun ? DpRokVazenja.SelectedDate : null;
            racun.BrojRacuna = brRacuna;
            racun.BrojOtpremnice = string.IsNullOrWhiteSpace(TxtBrojOtpremnice.Text) ? brRacuna.ToString() : TxtBrojOtpremnice.Text.Trim();
            racun.DatumRacuna = DpDatum.SelectedDate ?? DateTime.Now;
            racun.PartnerId = partner?.PartnerId;
            racun.RokPlacanjaDana = rokDana;
            racun.NacinPlacanja = CmbNacinPlacanja.Text.Trim();
            racun.MagacinId = magacin?.MagacinId;

            racun.Stavke = validneStavke.Select((s, idx) =>
            {
                decimal brutovrednost = s.Kolicina * s.ProdajnaCena;
                decimal iznosRabata = brutovrednost * (s.RabatProcenat / 100m);
                decimal osnovica = brutovrednost - iznosRabata;
                decimal pdv = osnovica * (s.StopaPdv / 100m);
                decimal ukupno = osnovica + pdv;

                return new RacunOtpremnicaStavka
                {
                    RedniBroj = idx + 1,
                    ArtikalId = s.ArtikalId,
                    Kolicina = s.Kolicina,
                    ProdajnaCena = s.ProdajnaCena,
                    RabatProcenat = s.RabatProcenat,
                    StopaPdv = s.StopaPdv,
                    Osnovica = osnovica,
                    IznosPdv = pdv,
                    Ukupno = ukupno
                };
            }).ToList();

            await service.SaveRacunAsync(racun);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju računa-otpremnice:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
