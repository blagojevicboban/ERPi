using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class RacuniOtpremniceView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<RacunOtpremnica> _sviRacuni = new();

    public RacuniOtpremniceView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            var service = new RacunOtpremnicaService(_db);
            _sviRacuni = await service.GetRacuneAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju računa-otpremnica: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filtriraj()
    {
        string term = TxtPretraga.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(term))
        {
            DgRacuni.ItemsSource = _sviRacuni;
        }
        else
        {
            DgRacuni.ItemsSource = _sviRacuni.Where(r =>
                r.BrojRacuna.ToString().Contains(term) ||
                (r.Partner != null && r.Partner.Naziv.ToLowerInvariant().Contains(term)) ||
                (r.Magacin != null && r.Magacin.NazivMagacina.ToLowerInvariant().Contains(term))).ToList();
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        Filtriraj();
    }

    private void BtnNoviRacun_Click(object sender, RoutedEventArgs e)
    {
        var win = new RacunOtpremnicaEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajPodatke();
        }
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        OtvoriIzabran();
    }

    private void DgRacuni_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtvoriIzabran();
    }

    private void OtvoriIzabran()
    {
        if (DgRacuni.SelectedItem is RacunOtpremnica selektovan)
        {
            var win = new RacunOtpremnicaEditWindow(_db, selektovan) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajPodatke();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovan)
        {
            MessageBox.Show("Izaberite račun koji želite proknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovan.IsKnjizen)
        {
            MessageBox.Show("Izabrani račun je već proknjižen.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Knjiži račun-otpremnicu br. {selektovan.BrojRacuna}?\nKnjiženje razdužuje robnu karticu i kreira nalog prodaje u Glavnoj knjizi.", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new RacunOtpremnicaService(_db);
            await service.KnjiziRacunAsync(selektovan.RacunOtpremnicaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovan)
        {
            MessageBox.Show("Izaberite račun koji želite rasknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selektovan.IsKnjizen)
        {
            MessageBox.Show("Izabrani račun nije proknjižen.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Rasknjiži račun-otpremnicu br. {selektovan.BrojRacuna}?\nRasknjižavanje poništava izlaz sa robne kartice i briše nalog prodaje u Glavnoj knjizi.", "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new RacunOtpremnicaService(_db);
            await service.RasknjiziRacunAsync(selektovan.RacunOtpremnicaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovan)
        {
            MessageBox.Show("Izaberite račun za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            MessageBox.Show($"Priprema PDF štampanog dokumenta za račun-otpremnicu br. {selektovan.BrojRacuna}...", "PDF Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampanog dokumenta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRacuni, "Racuni_Otpremnice", "Racuni");
}
