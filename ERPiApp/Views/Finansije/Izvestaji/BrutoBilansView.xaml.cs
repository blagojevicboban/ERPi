using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;
using QuestPDF.Fluent;

namespace ERPiApp.Views.Finansije.Izvestaji;

public partial class BrutoBilansView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BrutoBilansService _service;

    public BrutoBilansView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BrutoBilansService(_db);

        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async Task Ucitaj()
    {
        try
        {
            DateTime? odD = DpOd.SelectedDate;
            DateTime? doD = DpDo.SelectedDate;
            int? klasa = CmbKlasa.SelectedIndex > 0 ? CmbKlasa.SelectedIndex - 1 : null;

            bool saTotalima = ChkSaTotalima.IsChecked == true;

            List<BrutoBilansRed> redovi = saTotalima
                ? await _service.GetBrutoBilansSaTotalimaAsync(odD, doD, klasa)
                : await _service.GetBrutoBilansAsync(odD, doD, klasa);

            DgBrutoBilans.ItemsSource = redovi;

            var detaljni = redovi.Where(r => r.Tip == BrutoBilansRedTip.Detalj).ToList();
            TxtUkupnoDuguje.Text = detaljni.Sum(r => r.Duguje).ToString("N2");
            TxtUkupnoPotrazuje.Text = detaljni.Sum(r => r.Potrazuje).ToString("N2");
            TxtUkupnoSaldoDuguje.Text = detaljni.Sum(r => r.SaldoDuguje).ToString("N2");
            TxtUkupnoSaldoPotrazuje.Text = detaljni.Sum(r => r.SaldoPotrazuje).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju bruto bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var firma = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Firme);
            var redovi = (DgBrutoBilans.ItemsSource as List<BrutoBilansRed>) ?? new List<BrutoBilansRed>();

            var doc = new Stampe.BrutoBilansDocument(firma, redovi, DpOd.SelectedDate, DpDo.SelectedDate);
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"BrutoBilans_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            doc.GeneratePdf(pdfPath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var firma = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Firme);

        var headerLines = new List<(string Text, double FontSize, bool Bold, string? ColorHex)>();
        if (firma != null)
        {
            headerLines.Add((firma.Naziv, 14, true, "#2563EB"));
            if (!string.IsNullOrEmpty(firma.Pib))
                headerLines.Add(($"PIB: {firma.Pib}", 9, false, "#64748B"));
        }
        headerLines.Add(("BRUTO BILANS", 16, true, "#1E293B"));
        if (DpOd.SelectedDate.HasValue || DpDo.SelectedDate.HasValue)
            headerLines.Add(($"Period: {DpOd.SelectedDate?.ToString("dd.MM.yyyy.") ?? "—"} - {DpDo.SelectedDate?.ToString("dd.MM.yyyy.") ?? "—"}", 9, false, "#64748B"));

        ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgBrutoBilans, "Bruto Bilans", "Bruto_Bilans", headerLines: headerLines);
    }

    private async void BtnAnalitike_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var analitikeService = new OtvoreneStavkeService(_db);
            var redovi = await analitikeService.GetBrutoBilansAnalitikeAsync(DpOd.SelectedDate, DpDo.SelectedDate);

            var dijalog = new BrutoBilansAnalitikePreviewWindow(redovi) { Owner = Window.GetWindow(this) };
            dijalog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prikazu bruto bilansa analitike: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
