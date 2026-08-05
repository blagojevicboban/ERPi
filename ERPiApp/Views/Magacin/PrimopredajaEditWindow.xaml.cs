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

public partial class PrimopredajaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly ObservableCollection<PrimopredajaStavka> _stavke = new();
    private readonly int _postojeciId;

    public PrimopredajaEditWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        DgStavke.ItemsSource = _stavke;
        DpDatum.SelectedDate = DateTime.Now;

        ColMaterijal.ItemsSource = _db.Materijali.OrderBy(m => m.Naziv).ToList();
        UcitajMagacine();
    }

    public PrimopredajaEditWindow(ErpiDbContext db, PrimopredajaNalog postojeci)
    {
        InitializeComponent();
        _db = db;
        DgStavke.ItemsSource = _stavke;
        _postojeciId = postojeci.PrimopredajaNalogId;

        if (postojeci.IsKnjizen)
        {
            MessageBox.Show($"Primopredaja br. {postojeci.BrojNaloga} je proknjižena i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            IsEnabled = false;
        }

        Title = $"Izmena primopredaje br. {postojeci.BrojNaloga}";
        TxtBrojNaloga.Text = postojeci.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = postojeci.Datum;
        TxtStopaPdv.Text = postojeci.StopaPdv.ToString("G");

        ColMaterijal.ItemsSource = _db.Materijali.OrderBy(m => m.Naziv).ToList();
        foreach (var s in postojeci.Stavke.OrderBy(s => s.RedniBroj))
        {
            _stavke.Add(new PrimopredajaStavka { RedniBroj = s.RedniBroj, MaterijalId = s.MaterijalId, Kolicina = s.Kolicina });
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
            int max = _db.PrimopredajaNalozi.Select(n => (int?)n.BrojNaloga).Max() ?? 0;
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new PrimopredajaStavka { RedniBroj = _stavke.Count + 1 });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is PrimopredajaStavka selektovana)
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
            MessageBox.Show("Dodajte bar jednu stavku primopredaje.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (s.MaterijalId == 0)
            {
                MessageBox.Show("Svaka stavka mora imati izabran materijal.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var service = new PrimopredajaService(_db);

            var noveStavke = new List<PrimopredajaStavka>();
            int red = 1;
            foreach (var s in _stavke)
            {
                noveStavke.Add(new PrimopredajaStavka
                {
                    RedniBroj = red++,
                    MaterijalId = s.MaterijalId,
                    Kolicina = s.Kolicina
                });
            }

            var nalog = new PrimopredajaNalog
            {
                PrimopredajaNalogId = _postojeciId,
                BrojNaloga = brojNaloga,
                Datum = DpDatum.SelectedDate ?? DateTime.Now,
                MagacinIdDaje = magDaje.MagacinId,
                MagacinIdPrima = magPrima.MagacinId,
                StopaPdv = stopaPdv,
                Stavke = noveStavke
            };

            await service.SavePrimopredajuAsync(nalog);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
