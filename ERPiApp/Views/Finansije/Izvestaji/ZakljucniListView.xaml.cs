using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ERPiApp.Views.Finansije.Izvestaji;

/// <summary>
/// Zaključni list — totali prometa po sintetičkim (3-cifrenim) kontima za period. Port iz
/// ERPiFinansije (IzvestajiView "📑 Zaključni list" kartica), podaci iz već-portovanog
/// <see cref="BrutoBilansService.GetZakljucniListAsync"/> (Faza 3.6), samo je UI ekran nedostajao.
/// </summary>
public partial class ZakljucniListView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BrutoBilansService _service;

    public ZakljucniListView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BrutoBilansService(_db);

        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async System.Threading.Tasks.Task Ucitaj()
    {
        try
        {
            var redovi = await _service.GetZakljucniListAsync(DpOd.SelectedDate, DpDo.SelectedDate);
            DgZakljucniList.ItemsSource = redovi;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju zaključnog lista: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var redovi = (DgZakljucniList.ItemsSource as List<ZakljucniListRed>)
                ?? await _service.GetZakljucniListAsync(DpOd.SelectedDate, DpDo.SelectedDate);
            var firma = await _db.Firme.FirstOrDefaultAsync();

            var doc = new Stampe.ZakljucniListDocument(firma, redovi, DpOd.SelectedDate, DpDo.SelectedDate);
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ZakljucniList_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            doc.GeneratePdf(pdfPath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgZakljucniList, "Zaključni list", "Zakljucni_List");
}
