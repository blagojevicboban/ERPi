using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ERPiApp.Views.Finansije.Izvestaji;

/// <summary>
/// Dnevnik glavne knjige — hronološki pregled svih proknjiženih naloga (stavka po stavka) sa
/// ukupnim iznosima. Port iz ERPiFinansije (IzvestajiView "📖 Dnevnik glavne knjige" kartica +
/// DnevnikPreviewWindow), prilagođen na Konto FK (StavkaNaloga.KontoId) umesto string BrojKonta,
/// i na ERPi-jev obrazac pune stranice (DataGrid + toolbar) umesto zasebnog preview prozora.
/// </summary>
public partial class DnevnikGlavneKnjigeView : UserControl
{
    private readonly ErpiDbContext _db;

    public DnevnikGlavneKnjigeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async Task<List<Stampe.DnevnikRed>> UcitajRedoveAsync()
    {
        DateTime? odD = DpOd.SelectedDate;
        DateTime? doD = DpDo.SelectedDate;

        var query = _db.Nalozi
            .Include(n => n.Stavke).ThenInclude(s => s.Konto)
            .Where(n => n.Status == StatusNaloga.Proknjizen);

        if (odD.HasValue) query = query.Where(n => n.DatumNaloga >= odD.Value.Date);
        if (doD.HasValue) query = query.Where(n => n.DatumNaloga <= doD.Value.Date.AddDays(1).AddTicks(-1));

        var nalozi = await query.OrderBy(n => n.DatumNaloga).ThenBy(n => n.BrojNaloga).ToListAsync();

        return nalozi
            .SelectMany(n => n.Stavke.OrderBy(s => s.RedniBroj).Select(s => new Stampe.DnevnikRed
            {
                BrojNaloga = n.BrojNaloga,
                Datum = n.DatumNaloga,
                DokumentOpis = !string.IsNullOrWhiteSpace(s.BrojDokumenta) ? s.BrojDokumenta! : (s.Opis ?? n.Opis ?? ""),
                BrojKonta = s.Konto?.BrojKonta ?? "",
                NazivKonta = s.Konto?.NazivKonta ?? "",
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje
            }))
            .ToList();
    }

    private async Task Ucitaj()
    {
        try
        {
            var redovi = await UcitajRedoveAsync();
            DgDnevnik.ItemsSource = redovi;
            TxtUkupnoDuguje.Text = redovi.Sum(r => r.Duguje).ToString("N2");
            TxtUkupnoPotrazuje.Text = redovi.Sum(r => r.Potrazuje).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju dnevnika glavne knjige: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var redovi = (DgDnevnik.ItemsSource as List<Stampe.DnevnikRed>) ?? await UcitajRedoveAsync();
            var firma = await _db.Firme.FirstOrDefaultAsync();

            var doc = new Stampe.DnevnikGlavneKnjigeDocument(firma, redovi, DpOd.SelectedDate, DpDo.SelectedDate);
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"DnevnikGlavneKnjige_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
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
        var firma = await _db.Firme.FirstOrDefaultAsync();

        var headerLines = new List<(string Text, double FontSize, bool Bold, string? ColorHex)>();
        if (firma != null)
        {
            headerLines.Add((firma.Naziv, 14, true, "#2563EB"));
            if (!string.IsNullOrEmpty(firma.Pib))
                headerLines.Add(($"PIB: {firma.Pib}", 9, false, "#64748B"));
        }
        headerLines.Add(("DNEVNIK GLAVNE KNJIGE", 16, true, "#1E293B"));
        if (DpOd.SelectedDate.HasValue || DpDo.SelectedDate.HasValue)
            headerLines.Add(($"Period: {DpOd.SelectedDate?.ToString("dd.MM.yyyy.") ?? "—"} - {DpDo.SelectedDate?.ToString("dd.MM.yyyy.") ?? "—"}", 9, false, "#64748B"));

        ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgDnevnik, "Dnevnik glavne knjige", "Dnevnik_Glavne_Knjige", headerLines: headerLines);
    }
}
