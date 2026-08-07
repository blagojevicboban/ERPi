using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using ERPiApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class MaloprodajnaKalkulacijaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly ObservableCollection<MaloprodajnaKalkulacijaStavka> _stavke = new();
    private readonly MaloprodajnaKalkulacija? _existingKalkulacija;
    private bool _updating;

    public MaloprodajnaKalkulacijaEditWindow(ErpiDbContext db, MaloprodajnaKalkulacija? existingKalkulacija = null)
    {
        InitializeComponent();
        _db = db;
        _existingKalkulacija = existingKalkulacija;
        DgStavke.ItemsSource = _stavke;
        TxtPoreskaStopaProcenat.Text = "20";
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var magacini = await _db.Magacini.AsNoTracking().OrderBy(m => m.NazivMagacina).ToListAsync();
            var mpMagacini = magacini.Where(m => m.VrstaMagacina == "Maloprodaja").ToList();
            var magaciniZaPrikaz = mpMagacini.Count > 0 ? mpMagacini : magacini;

            CmbMagacinPrima.ItemsSource = magaciniZaPrikaz;
            CmbMagacinDaje.ItemsSource = magacini;
            var partneri = await _db.Partneri.AsNoTracking().Where(p => p.JeDobavljac).OrderBy(p => p.SifraPartnera).ThenBy(p => p.Naziv).ToListAsync();
            PartnerPicker.Poveži(CmbPartner, partneri);

            var konta = await _db.Konta.AsNoTracking().ToListAsync();
            KontoPicker.PoveziDobavljace(CmbKontoDobavljaca, konta);

            ColArtikal.ItemsSource = await _db.Artikli.AsNoTracking().OrderBy(a => a.SifraArtikla).ToListAsync();

            if (_existingKalkulacija != null)
            {
                Title = $"Izmena MP kalkulacije #{_existingKalkulacija.BrojKalkulacije}";
                TxtBrojKalkulacije.Text = _existingKalkulacija.BrojKalkulacije.ToString();
                DpDatum.SelectedDate = _existingKalkulacija.Datum;
                CmbMagacinPrima.SelectedValue = _existingKalkulacija.MagacinIdPrima;
                CmbMagacinDaje.SelectedValue = _existingKalkulacija.MagacinIdDaje;
                PartnerPicker.PostaviPartnera(CmbPartner, _existingKalkulacija.DobavljacId);
                if (_existingKalkulacija.KontoDobavljacaId.HasValue && konta.FirstOrDefault(k => k.KontoId == _existingKalkulacija.KontoDobavljacaId.Value) is { } kDob)
                {
                    KontoPicker.PostaviKonto(CmbKontoDobavljaca, kDob.BrojKonta);
                }
                TxtBrojOtpremnice.Text = _existingKalkulacija.BrojOtpremnice;
                DpDatumOtpremnice.SelectedDate = _existingKalkulacija.DatumOtpremnice;
                TxtBrojRacuna.Text = _existingKalkulacija.BrojRacuna;

                TxtTransportniTroskovi.Text = _existingKalkulacija.TransportniTroskovi.ToString("N2");
                TxtTroskoviUskladistenja.Text = _existingKalkulacija.TroskoviUskladistenja.ToString("N2");
                TxtUtovarIstovar.Text = _existingKalkulacija.UtovarIstovar.ToString("N2");
                TxtTransportnoOsiguranje.Text = _existingKalkulacija.TransportnoOsiguranje.ToString("N2");
                TxtOstaliTroskovi.Text = _existingKalkulacija.OstaliTroskovi.ToString("N2");
                TxtMarzaProcenat.Text = _existingKalkulacija.MarzaProcenat.ToString("N2");
                TxtPoreskaStopaProcenat.Text = _existingKalkulacija.PoreskaStopaProcenat.ToString("N2");
                TxtRabatPri.Text = _existingKalkulacija.RabatPri.ToString("N2");

                if (_existingKalkulacija.Stavke.Count == 0)
                {
                    TxtNabavnaVrednost.Text = _existingKalkulacija.NabavnaVrednost.ToString("N2");
                }

                foreach (var s in _existingKalkulacija.Stavke.OrderBy(s => s.RedniBroj))
                {
                    _stavke.Add(new MaloprodajnaKalkulacijaStavka
                    {
                        RedniBroj = s.RedniBroj,
                        ArtikalId = s.ArtikalId,
                        Kolicina = s.Kolicina,
                        NabavnaCena = s.NabavnaCena,
                        ProdajnaCena = s.ProdajnaCena
                    });
                }
            }
            else
            {
                DpDatum.SelectedDate = DateTime.Now;
                if (magaciniZaPrikaz.Count > 0) CmbMagacinPrima.SelectedIndex = 0;

                int max = await _db.MaloprodajneKalkulacije.Select(k => (int?)k.BrojKalkulacije).MaxAsync() ?? 0;
                TxtBrojKalkulacije.Text = (max + 1).ToString();
                TxtPoreskaStopaProcenat.Text = "20";
            }
        }
        catch (Exception ex)
        {
            if (_existingKalkulacija != null)
            {
                MessageBox.Show($"Greška pri učitavanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Prikazi();
    }

    private static decimal ParseUneto(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return v;
        return 0m;
    }

    private MaloprodajnaKalkulacija SkupiUnos()
    {
        return new MaloprodajnaKalkulacija
        {
            MaloprodajnaKalkulacijaId = _existingKalkulacija?.MaloprodajnaKalkulacijaId ?? 0,
            BrojKalkulacije = int.TryParse(TxtBrojKalkulacije.Text.Trim(), out int brojKalk) ? brojKalk : 0,
            Datum = DpDatum.SelectedDate ?? DateTime.Now,
            MagacinIdPrima = (CmbMagacinPrima.SelectedValue as int?) ?? 0,
            MagacinIdDaje = CmbMagacinDaje.SelectedValue as int?,
            DobavljacId = PartnerPicker.IzabraniPartner(CmbPartner)?.PartnerId ?? (CmbPartner.SelectedValue as int?),
            KontoDobavljacaId = (CmbKontoDobavljaca.SelectedItem as Konto)?.KontoId ?? (CmbKontoDobavljaca.SelectedValue as int?),
            BrojOtpremnice = TxtBrojOtpremnice.Text.Trim(),
            DatumOtpremnice = DpDatumOtpremnice.SelectedDate,
            BrojRacuna = TxtBrojRacuna.Text.Trim(),
            NabavnaVrednost = ParseUneto(TxtNabavnaVrednost.Text),
            TransportniTroskovi = ParseUneto(TxtTransportniTroskovi.Text),
            TroskoviUskladistenja = ParseUneto(TxtTroskoviUskladistenja.Text),
            UtovarIstovar = ParseUneto(TxtUtovarIstovar.Text),
            TransportnoOsiguranje = ParseUneto(TxtTransportnoOsiguranje.Text),
            OstaliTroskovi = ParseUneto(TxtOstaliTroskovi.Text),
            MarzaProcenat = ParseUneto(TxtMarzaProcenat.Text),
            PoreskaStopaProcenat = ParseUneto(TxtPoreskaStopaProcenat.Text),
            RabatPri = ParseUneto(TxtRabatPri.Text),
            IsKnjizen = _existingKalkulacija?.IsKnjizen ?? false,
            IsTrgovinskiKnjizen = _existingKalkulacija?.IsTrgovinskiKnjizen ?? false,
            Stavke = _stavke.ToList()
        };
    }

    private void Input_Changed(object sender, TextChangedEventArgs e) => Prikazi();

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new MaloprodajnaKalkulacijaStavka { RedniBroj = _stavke.Count + 1 });
        Prikazi();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is MaloprodajnaKalkulacijaStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            Prikazi();
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(Prikazi), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void Prikazi()
    {
        if (_updating) return;
        _updating = true;
        try
        {
            var k = SkupiUnos();
            if (k.Stavke.Count > 0)
            {
                MaloprodajnaKalkulacijaService.IzracunajSaStavkama(k);
                TxtNabavnaVrednost.Text = k.NabavnaVrednost.ToString("N2");
                TxtNabavnaVrednost.IsReadOnly = true;
                DgStavke.CommitEdit(DataGridEditingUnit.Cell, true);
                DgStavke.CommitEdit(DataGridEditingUnit.Row, true);
                DgStavke.Items.Refresh();
            }
            else
            {
                TxtNabavnaVrednost.IsReadOnly = false;
                MaloprodajnaKalkulacijaService.Izracunaj(k);
            }

            TxtSvegaTroskovi.Text = k.SvegaTroskovi.ToString("N2");
            TxtSvegaNabavno.Text = k.SvegaNabavno.ToString("N2");
            TxtRazlika.Text = k.Razlika.ToString("N2");
            TxtPorez.Text = k.Porez.ToString("N2");
            TxtRabatIznos.Text = k.RabatIznos.ToString("N2");
            TxtProdajnaVrednost.Text = k.ProdajnaVrednost.ToString("N2");
        }
        finally
        {
            _updating = false;
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojKalkulacije.Text.Trim(), out _))
        {
            MessageBox.Show("Unesite ispravan broj kalkulacije.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacinPrima.SelectedValue == null)
        {
            MessageBox.Show("Izaberite magacin prima (prodavnicu).", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (s.ArtikalId == 0)
            {
                MessageBox.Show("Svaka stavka mora imati izabran artikal.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            await service.SaveKalkulacijuAsync(SkupiUnos());
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
