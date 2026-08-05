using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class NivelacijaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    public NivelacijaCena Nivelacija { get; private set; }
    public ObservableCollection<NivelacijaStavka> StavkeCollection { get; set; }

    public NivelacijaEditWindow(ErpiDbContext db, NivelacijaCena? nivelacija = null)
    {
        InitializeComponent();
        _db = db;

        Nivelacija = nivelacija ?? new NivelacijaCena
        {
            BrojNivelacije = (_db.NivelacijeCena.Select(n => (int?)n.BrojNivelacije).Max() ?? 0) + 1,
            DatumNivelacije = DateTime.Now
        };

        StavkeCollection = new ObservableCollection<NivelacijaStavka>(Nivelacija.Stavke);
        DgStavke.ItemsSource = StavkeCollection;

        UcitajMagacine();
        PopuniPolja();
        PracunajUkupno();
    }

    private void UcitajMagacine()
    {
        var magacini = _db.Magacini.OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacin.ItemsSource = magacini;
        ColArtikal.ItemsSource = _db.Artikli.OrderBy(a => a.Naziv).ToList();

        if (Nivelacija.MagacinId.HasValue)
        {
            CmbMagacin.SelectedItem = magacini.FirstOrDefault(m => m.MagacinId == Nivelacija.MagacinId.Value);
        }
        else if (magacini.Count > 0)
        {
            CmbMagacin.SelectedIndex = 0;
        }
    }

    private void PopuniPolja()
    {
        TxtBrojNivelacije.Text = Nivelacija.BrojNivelacije.ToString();
        DpDatum.SelectedDate = Nivelacija.DatumNivelacije;
        TxtOpis.Text = Nivelacija.Opis;
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        var nova = new NivelacijaStavka
        {
            RedniBroj = StavkeCollection.Count + 1,
            KolicinaZaliha = 1,
            StaraCena = 0,
            NovaCena = 0
        };
        StavkeCollection.Add(nova);
        PracunajUkupno();
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is NivelacijaStavka selected)
        {
            StavkeCollection.Remove(selected);
            RenumerisiStavke();
            PracunajUkupno();
        }
    }

    private void RenumerisiStavke()
    {
        int rbr = 1;
        foreach (var st in StavkeCollection)
        {
            st.RedniBroj = rbr++;
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is NivelacijaStavka st)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (st.ArtikalId > 0)
                {
                    var art = _db.Artikli.FirstOrDefault(a => a.ArtikalId == st.ArtikalId);
                    if (art != null)
                    {
                        if (st.StaraCena == 0) st.StaraCena = art.ProdajnaCena;
                    }
                }

                st.RazlikaPoJedinici = st.NovaCena - st.StaraCena;
                st.UkupnaRazlika = st.KolicinaZaliha * st.RazlikaPoJedinici;
                PracunajUkupno();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void PracunajUkupno()
    {
        decimal ukupno = StavkeCollection.Sum(s => s.UkupnaRazlika);
        TxtUkupno.Text = $"Ukupna razlika: {ukupno:N2} RSD";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNivelacije.Text.Trim(), out int brojNivelacije))
        {
            MessageBox.Show("Molimo unesite ispravan broj nivelacije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin za nivelaciju.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Nivelacija.BrojNivelacije = brojNivelacije;
        Nivelacija.DatumNivelacije = DpDatum.SelectedDate ?? DateTime.Now;
        Nivelacija.Opis = TxtOpis.Text.Trim();
        Nivelacija.MagacinId = magacin.MagacinId;

        Nivelacija.Stavke = StavkeCollection.ToList();
        Nivelacija.UkupnoRazlika = StavkeCollection.Sum(s => s.UkupnaRazlika);

        try
        {
            var service = new NivelacijaService(_db);
            await service.SaveNivelacijaAsync(Nivelacija);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
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
