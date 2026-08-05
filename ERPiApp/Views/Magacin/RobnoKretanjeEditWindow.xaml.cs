using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Editor Robnog (Artikal-baziranog) internog kretanja — Primopredaja/Zaduženje/Razduženje
/// (vidi <see cref="RobnoKretanjeNalog"/>). NIJE za Materijalno knjigovodstvo — to ostaje
/// <see cref="PrimopredajaEditWindow"/> nad <see cref="Materijal"/> šifarnikom (vidi
/// PLAN_NASTAVKA.md §3g/§3i, "Robno i Materijalno se ne mešaju").
/// </summary>
public partial class RobnoKretanjeEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly string _vrsta;
    private readonly ObservableCollection<RobnoKretanjeStavka> _stavke = new();
    private readonly int _postojeciId;

    public RobnoKretanjeEditWindow(ErpiDbContext db, string vrsta)
    {
        InitializeComponent();
        _db = db;
        _vrsta = vrsta;
        DgStavke.ItemsSource = _stavke;
        DpDatum.SelectedDate = DateTime.Now;

        Title = $"Novi(a) {vrsta.ToLower()}";
        TxtNaslovStavke.Text = $"📦 Stavke {vrsta.ToLower()}";
        ColArtikal.ItemsSource = _db.Artikli.OrderBy(a => a.Naziv).ToList();
        UcitajMagacine();
    }

    public RobnoKretanjeEditWindow(ErpiDbContext db, string vrsta, RobnoKretanjeNalog postojeci)
    {
        InitializeComponent();
        _db = db;
        _vrsta = vrsta;
        DgStavke.ItemsSource = _stavke;
        _postojeciId = postojeci.RobnoKretanjeNalogId;

        if (postojeci.IsKnjizen)
        {
            MessageBox.Show($"{vrsta} br. {postojeci.BrojNaloga} je proknjižen(a) i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            IsEnabled = false;
        }

        Title = $"Izmena — {vrsta.ToLower()} br. {postojeci.BrojNaloga}";
        TxtNaslovStavke.Text = $"📦 Stavke {vrsta.ToLower()}";
        TxtBrojNaloga.Text = postojeci.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = postojeci.Datum;
        TxtStopaPdv.Text = postojeci.StopaPdv.ToString("G");

        ColArtikal.ItemsSource = _db.Artikli.OrderBy(a => a.Naziv).ToList();
        foreach (var s in postojeci.Stavke.OrderBy(s => s.RedniBroj))
        {
            _stavke.Add(new RobnoKretanjeStavka { RedniBroj = s.RedniBroj, ArtikalId = s.ArtikalId, Kolicina = s.Kolicina });
        }

        UcitajMagacine(postojeci.MagacinIdDaje, postojeci.MagacinIdPrima);
    }

    private void UcitajMagacine(int? selektujDajeId = null, int? selektujPrimaId = null)
    {
        var magacini = _db.Magacini.OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacinDaje.ItemsSource = magacini;
        CmbMagacinPrima.ItemsSource = magacini;

        if (selektujDajeId.HasValue)
        {
            CmbMagacinDaje.SelectedItem = magacini.FirstOrDefault(m => m.MagacinId == selektujDajeId.Value);
        }
        else if (magacini.Count > 0)
        {
            CmbMagacinDaje.SelectedIndex = 0;
        }

        if (selektujPrimaId.HasValue)
        {
            CmbMagacinPrima.SelectedItem = magacini.FirstOrDefault(m => m.MagacinId == selektujPrimaId.Value);
        }
        else if (magacini.Count > 1)
        {
            CmbMagacinPrima.SelectedIndex = 1;
        }

        if (_postojeciId == 0)
        {
            int max = _db.RobnaKretanja.Where(n => n.VrstaDokumenta == _vrsta).Select(n => (int?)n.BrojNaloga).Max() ?? 0;
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new RobnoKretanjeStavka { RedniBroj = _stavke.Count + 1 });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is RobnoKretanjeStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            DgStavke.Items.Refresh();
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Unesite ispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacinDaje.SelectedItem is not ERPiData.Models.Magacin.Magacin magDaje)
        {
            MessageBox.Show("Izaberite magacin koji daje.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacinPrima.SelectedItem is not ERPiData.Models.Magacin.Magacin magPrima)
        {
            MessageBox.Show("Izaberite magacin koji prima.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (magDaje.MagacinId == magPrima.MagacinId)
        {
            MessageBox.Show("Magacin koji daje i magacin koji prima moraju biti različiti.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(TxtStopaPdv.Text.Trim(), out decimal stopaPdv))
        {
            stopaPdv = 20m;
        }
        if (_stavke.Count == 0)
        {
            MessageBox.Show($"Dodajte bar jednu stavku {_vrsta.ToLower()}.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (s.ArtikalId == 0)
            {
                MessageBox.Show("Svaka stavka mora imati izabran artikal.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (s.Kolicina <= 0)
            {
                MessageBox.Show("Količina na svakoj stavci mora biti veća od 0.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var service = new RobnoKretanjeService(_db);

            var noveStavke = new List<RobnoKretanjeStavka>();
            int red = 1;
            foreach (var s in _stavke)
            {
                noveStavke.Add(new RobnoKretanjeStavka
                {
                    RedniBroj = red++,
                    ArtikalId = s.ArtikalId,
                    Kolicina = s.Kolicina
                });
            }

            var nalog = new RobnoKretanjeNalog
            {
                RobnoKretanjeNalogId = _postojeciId,
                BrojNaloga = brojNaloga,
                Datum = DpDatum.SelectedDate ?? DateTime.Now,
                MagacinIdDaje = magDaje.MagacinId,
                MagacinIdPrima = magPrima.MagacinId,
                VrstaDokumenta = _vrsta,
                StopaPdv = stopaPdv,
                Stavke = noveStavke
            };

            await service.SaveKretanjeAsync(nalog);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
