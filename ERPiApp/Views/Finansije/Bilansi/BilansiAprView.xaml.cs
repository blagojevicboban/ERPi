using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ERPiApp.Views.Finansije.Bilansi;

/// <summary>
/// Zvanični Finansijski Izveštaji za APR — jedinstven tabovani ekran koji objedinjuje Bilans
/// Stanja, Bilans Uspeha, Statistički izveštaj (SI), Tokove gotovine (Cash Flow), Promene na
/// kapitalu i Poreski Bilans (PB-1/PDP/OA). Port iz ERPiFinansije (Views/Bilansi/BilansiView),
/// zamenjuje ranije odvojene <c>BilansStanjaView</c>/<c>BilansUspehaView</c> nav stavke —
/// korisnik je tražio da bude jedan ekran kao u izvoru, ne dva odvojena menija.
/// Bilans Stanja/Uspeha koriste već-portovani <see cref="BilansService"/> (Faza 3.6); SI/Cash
/// Flow/Promene na kapitalu koriste već-portovani <see cref="AprProsireniIzvestajiService"/>
/// (servisni sloj je postojao, samo ovaj objedinjeni ekran nije bio izgrađen).
/// </summary>
public partial class BilansiAprView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BilansService _bilansService;
    private readonly AprProsireniIzvestajiService _aprService;

    public BilansiAprView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _bilansService = new BilansService(_db);
        _aprService = new AprProsireniIzvestajiService(_db);

        DpDatumStanja.SelectedDate = DateTime.Today;
        DpOdDatuma.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDoDatuma.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await UcitajSve();
    }

    private int Godina => (DpDoDatuma.SelectedDate ?? DateTime.Today).Year;

    private async void BtnOsvezi_Click(object sender, RoutedEventArgs e) => await UcitajSve();

    private async Task UcitajSve()
    {
        try
        {
            DgBilansStanja.ItemsSource = await _bilansService.GetBilansStanjaAsync(DpDatumStanja.SelectedDate);
            DgBilansUspeha.ItemsSource = await _bilansService.GetBilansUspehaAsync(DpOdDatuma.SelectedDate, DpDoDatuma.SelectedDate);
            DgStatistickiIzvestaj.ItemsSource = await _aprService.GenerisiStatistickiIzvestajAsync(Godina);
            DgCashFlow.ItemsSource = await _aprService.GenerisiCashFlowAsync(Godina);
            DgPromeneNaKapitalu.ItemsSource = await _aprService.GenerisiPromeneNaKapitaluAsync(Godina);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri obračunu finansijskih izveštaja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPoreskiBilans_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = new PoreskiBilansWindow(_db, Godina) { Owner = Window.GetWindow(this) };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju Poreskog Bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajStanjePdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pozicije = (DgBilansStanja.ItemsSource as List<BilansPozicija>) ?? await _bilansService.GetBilansStanjaAsync(DpDatumStanja.SelectedDate);
            var firma = await _db.Firme.FirstOrDefaultAsync();
            DateTime datum = DpDatumStanja.SelectedDate ?? DateTime.Today;

            var doc = new Stampe.BilansPozicijeDocument(firma, pozicije, $"BILANS STANJA na datum {datum:dd.MM.yyyy.}", "(Iznosi su iskazani u RSD po AOP pozicijama APR-a)", "Pozicija Bilansa Stanja");
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"BilansStanja_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            doc.GeneratePdf(pdfPath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajUspehaPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pozicije = (DgBilansUspeha.ItemsSource as List<BilansPozicija>) ?? await _bilansService.GetBilansUspehaAsync(DpOdDatuma.SelectedDate, DpDoDatuma.SelectedDate);
            var firma = await _db.Firme.FirstOrDefaultAsync();
            string period = (DpOdDatuma.SelectedDate.HasValue && DpDoDatuma.SelectedDate.HasValue)
                ? $"za period od {DpOdDatuma.SelectedDate:dd.MM.yyyy.} do {DpDoDatuma.SelectedDate:dd.MM.yyyy.}"
                : "za tekuću poslovnu godinu";

            var doc = new Stampe.BilansPozicijeDocument(firma, pozicije, $"BILANS USPEHA {period}", "(Iznosi su iskazani u RSD po AOP pozicijama APR-a)", "Pozicija Bilansa Uspeha");
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"BilansUspeha_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            doc.GeneratePdf(pdfPath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelStanja_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgBilansStanja, "Bilans Stanja", "Bilans_Stanja");

    private void BtnExportExcelUspeha_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgBilansUspeha, "Bilans Uspeha", "Bilans_Uspeha");

    private void BtnExportExcelSI_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgStatistickiIzvestaj, "Statisticki Izvestaj", $"Statisticki_Izvestaj_{Godina}");

    private void BtnExportExcelCashFlow_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgCashFlow, "Cash Flow", $"Cash_Flow_{Godina}");

    private void BtnExportExcelKapital_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgPromeneNaKapitalu, "Promene na Kapitalu", $"Promene_Na_Kapitalu_{Godina}");
}
