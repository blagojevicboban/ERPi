using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.PutniNalozi;

public partial class PutniNaloziView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly PutniNalogService _service;
    private List<PutniNalog> _sviNalozi = new();

    public PutniNaloziView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new PutniNalogService(_db);
        Loaded += PutniNaloziView_Loaded;
    }

    private void PutniNaloziView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadNalozi();
    }

    private async void LoadNalozi()
    {
        try
        {
            _sviNalozi = await _service.GetPutniNaloziAsync();
            FiltrirajNaloge();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju putnih naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FiltrirajNaloge()
    {
        if (DgPutniNalozi == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _sviNalozi.Where(p =>
            string.IsNullOrEmpty(search) ||
            p.BrojNaloga.ToLower().Contains(search) ||
            p.ZaposleniIme.ToLower().Contains(search) ||
            p.Relacija.ToLower().Contains(search) ||
            p.SvrhaPutovanja.ToLower().Contains(search)
        ).ToList();

        DgPutniNalozi.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgPutniNalozi.SelectedIndex = 0;
        }
        else
        {
            DgStavkeTroskova.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajNaloge();

    private void DgPutniNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgPutniNalozi.SelectedItem is PutniNalog pn)
        {
            DgStavkeTroskova.ItemsSource = pn.StavkeTroskova;
        }
        else
        {
            DgStavkeTroskova.ItemsSource = null;
        }
    }

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Unos novog putnog naloga biće dostupan u narednom prikazu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi putni nalozi se ne mogu menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi putni nalozi se ne mogu brisati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Da li ste sigurni da želite obrisati putni nalog br. {pn.BrojNaloga}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.ObrisiPutniNalogAsync(pn.PutniNalogId);
                LoadNalozi();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Putni nalog je već proknjižen.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string kontoStr = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "5340" : "5330";
            var res = MessageBox.Show($"Da li želite da proknjižite troškove putnog naloga br. {pn.BrojNaloga} na Konto {kontoStr} u Glavnoj knjizi?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (success, msg, nalogId) = await _service.KnjiziPutniNalogAsync(pn.PutniNalogId);
                if (success)
                {
                    MessageBox.Show(msg, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadNalozi();
                }
                else
                {
                    MessageBox.Show(msg, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
