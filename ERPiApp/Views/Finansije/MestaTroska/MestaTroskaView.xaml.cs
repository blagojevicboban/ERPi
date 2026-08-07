using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiApp.Services;
using ERPiData;
using ERPiData.Models.Core;
using FirmaModel = ERPiData.Models.Core.Firma;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.MestaTroska;

public partial class MestaTroskaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly MestaTroskaService _service;
    private List<MestoTroska> _svaMesta = new();

    public MestaTroskaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new MestaTroskaService(_db);
        Loaded += MestaTroskaView_Loaded;
    }

    private void MestaTroskaView_Loaded(object sender, RoutedEventArgs e)
    {
        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        LoadMestaTroska();
    }

    private async void LoadMestaTroska()
    {
        try
        {
            _svaMesta = await _service.GetMestaTroskaAsync();

            Filtriraj();

            CmbIzvestajMesto.ItemsSource = _svaMesta;
            if (_svaMesta.Any())
            {
                CmbIzvestajMesto.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju mesta troška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadAnalitiku()
    {
        if (CmbIzvestajMesto?.SelectedValue is int mestoId && mestoId > 0)
        {
            try
            {
                DateTime odD = DpOd?.SelectedDate ?? new DateTime(DateTime.Today.Year, 1, 1);
                DateTime doD = DpDo?.SelectedDate ?? DateTime.Today;

                var (redovi, summary) = await _service.GetAnalitikaPoMestuTroskaAsync(mestoId, odD, doD);

                DgAnalitika.ItemsSource = redovi;
                TxtUkupnoPrihodi.Text = $"{summary.UkupnoPrihodi:N2} RSD";
                TxtUkupnoRashodi.Text = $"{summary.UkupnoRashodi:N2} RSD";
                TxtNetoRezultat.Text = $"{summary.NetoRezultat:N2} RSD";
            }
            catch { }
        }
    }

    private void Filtriraj()
    {
        if (DgMestaTroska == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _svaMesta.Where(m =>
            string.IsNullOrEmpty(search) ||
            m.Sifra.ToLower().Contains(search) ||
            m.Naziv.ToLower().Contains(search)
        ).ToList();

        DgMestaTroska.ItemsSource = filtered;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void CmbIzvestajFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadAnalitiku();
    private void DpFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadAnalitiku();
    private void BtnOsveziAnalitiku_Click(object sender, RoutedEventArgs e) => LoadAnalitiku();

    private void BtnNovoMesto_Click(object sender, RoutedEventArgs e)
    {
        var win = new MestoTroskaEditWindow(new MestoTroska(), _db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadMestaTroska();
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgMestaTroska?.SelectedItem is MestoTroska mt)
        {
            var full = await _service.GetMestoTroskaByIdAsync(mt.MestoTroskaId);
            if (full != null)
            {
                var win = new MestoTroskaEditWindow(full, _db) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true) LoadMestaTroska();
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgMestaTroska?.SelectedItem is MestoTroska mt)
        {
            if (MessageBox.Show($"Da li ste sigurni da želite obrisati mesto troška/projekat '{mt.Naziv}' ({mt.Sifra})?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _service.ObrisiMestoTroskaAsync(mt.MestoTroskaId);
                    LoadMestaTroska();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgMestaTroska, "Šifarnik mesta troška i projekata", "Mesta_Troska");

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };
            var pdfBytes = PdfReportService.GenerisiSifarnikMestaTroskaPdf(firma, _svaMesta);

            string tempPath = Path.Combine(Path.GetTempPath(), $"Mesta_Troska_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju šifarnika: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
