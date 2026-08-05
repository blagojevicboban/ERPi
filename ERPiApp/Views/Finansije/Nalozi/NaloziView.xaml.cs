using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiApp.Services;
using ERPiData;
using ERPiData.Models.Finansije;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using FirmaModel = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Nalozi;

public partial class NaloziView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<Nalog> _sviNalozi = new();

    public NaloziView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        RbSviNalozi.IsChecked = true;
        Ucitaj();
    }

    public void Ucitaj()
    {
        try
        {
            _sviNalozi = _db.Nalozi
                .Include(n => n.Stavke)
                .ThenInclude(s => s.Konto)
                .Include(n => n.Stavke)
                .ThenInclude(s => s.Partner)
                .OrderByDescending(n => n.BrojNaloga)
                .ToList();

            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();
    private void Filter_Changed(object sender, RoutedEventArgs e) => Filtriraj();

    private void Filtriraj()
    {
        string pretraga = TxtPretraga?.Text?.Trim().ToLower() ?? "";
        var filtrirani = _sviNalozi.AsEnumerable();

        if (!string.IsNullOrEmpty(pretraga))
        {
            filtrirani = filtrirani.Where(n =>
                n.BrojNaloga.ToString().Contains(pretraga) ||
                (n.Opis != null && n.Opis.ToLower().Contains(pretraga)) ||
                (n.VrstaNaloga != null && n.VrstaNaloga.ToLower().Contains(pretraga))
            );
        }

        if (RbProknjizeni?.IsChecked == true)
            filtrirani = filtrirani.Where(n => n.Status == StatusNaloga.Proknjizen);
        else if (RbNeproknjizeni?.IsChecked == true)
            filtrirani = filtrirani.Where(n => n.Status == StatusNaloga.Nacrt);

        var lista = filtrirani.ToList();
        DgNalozi.ItemsSource = lista;

        if (lista.Any())
            DgNalozi.SelectedIndex = 0;
        else
            DgStavke.ItemsSource = null;
    }

    private void DgNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog nalog)
        {
            DgStavke.ItemsSource = nalog.Stavke.OrderBy(s => s.RedniBroj).ToList();
        }
        else
        {
            DgStavke.ItemsSource = null;
        }
    }

    private void DgNalozi_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BtnIzmeniNalog_Click(sender, e);
    }

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        var novNalog = new Nalog
        {
            BrojNaloga = (_sviNalozi.Max(n => (int?)n.BrojNaloga) ?? 0) + 1,
            DatumNaloga = DateTime.Today,
            Status = StatusNaloga.Nacrt,
            VrstaNaloga = "Finansijski"
        };
        var win = new NalogEditWindow(_db, novNalog) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            Ucitaj();
        }
    }

    private void BtnIzmeniNalog_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selektovan)
        {
            var win = new NalogEditWindow(_db, selektovan) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                Ucitaj();
            }
        }
    }

    private void BtnObrisiNalog_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog nalog)
        {
            if (nalog.Status == StatusNaloga.Proknjizen)
            {
                MessageBox.Show("Proknjižen nalog se ne može obrisati. Prvo ga rasknjižite.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete nalog br. {nalog.BrojNaloga}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _db.Nalozi.Remove(nalog);
                _db.SaveChanges();
                Ucitaj();
            }
        }
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        if (nalog.Status == StatusNaloga.Proknjizen)
        {
            MessageBox.Show("Nalog je već proknjižen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Math.Abs(nalog.UkupnoDuguje - nalog.UkupnoPotrazuje) > 0.01m)
        {
            MessageBox.Show("Nalog nije uravnotežen (Duguje ≠ Potražuje) — ne može se proknjižiti.", "Nalog nije uravnotežen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        nalog.Status = StatusNaloga.Proknjizen;
        nalog.DatumKnjizenja = DateTime.Now;
        _db.SaveChanges();
        Ucitaj();
    }

    private void BtnProknjiziSve_Click(object sender, RoutedEventArgs e)
    {
        var neproknjizeni = _sviNalozi.Where(n => n.Status == StatusNaloga.Nacrt).ToList();
        if (!neproknjizeni.Any())
        {
            MessageBox.Show("Nema neproknjiženih naloga.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var res = MessageBox.Show($"Da li želite da proknjižite svih {neproknjizeni.Count} neproknjiženih naloga?", "Masovno knjiženje", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            foreach (var n in neproknjizeni)
            {
                if (Math.Abs(n.UkupnoDuguje - n.UkupnoPotrazuje) <= 0.01m)
                {
                    n.Status = StatusNaloga.Proknjizen;
                    n.DatumKnjizenja = DateTime.Now;
                }
            }
            _db.SaveChanges();
            Ucitaj();
        }
    }

    private void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        if (nalog.Status != StatusNaloga.Proknjizen)
        {
            MessageBox.Show("Nalog nije proknjižen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        nalog.Status = StatusNaloga.Nacrt;
        nalog.DatumKnjizenja = null;
        _db.SaveChanges();
        Ucitaj();
    }

    private void BtnPreknjizavanje_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funkcija automatskog preknjižavanja stavki sa konta na konto je u pripremi.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNapredniFilter_Click(object sender, RoutedEventArgs e)
    {
        TxtPretraga.Focus();
    }

    private void BtnUvozIzvoda_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Uvoz bankarskih izvoda u naloge (HALCOM, Asseco, XML) otvara se iz menija 🏦 Uvoz izvoda banke.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnUvozZarada_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Nalog zarada automatski se knjiži iz modula ERPiZarade.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        var selektovaniNalozi = DgNalozi.SelectedItems.Cast<Nalog>().ToList();
        if (!selektovaniNalozi.Any() && DgNalozi.SelectedItem is Nalog singleNalog)
        {
            selektovaniNalozi.Add(singleNalog);
        }

        if (!selektovaniNalozi.Any())
        {
            MessageBox.Show("Molimo izaberite jedan ili više naloga za štampu.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };

            var nalogIds = selektovaniNalozi.Select(n => n.NalogId).ToList();
            var naloziSaStavkama = await _db.Nalozi
                .Include(n => n.Stavke)
                .ThenInclude(s => s.Konto)
                .Where(n => nalogIds.Contains(n.NalogId))
                .ToListAsync();

            var nalogeForPdf = selektovaniNalozi
                .Select(s => naloziSaStavkama.FirstOrDefault(n => n.NalogId == s.NalogId) ?? s)
                .ToList();

            byte[] pdfBytes = PdfReportService.GenerisiNalogePdf(firma, nalogeForPdf);

            string fileName = nalogeForPdf.Count == 1 
                ? $"Nalog_{nalogeForPdf[0].BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                : $"Nalozi_Vise_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            string pdfPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampe: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelNalozi_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgNalozi, "Nalozi_Za_Knjizenje", "Nalozi");

    private void BtnNovaGodina_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Automatski prenos početnog stanja u novu poslovnu godinu (otvaranje naloga početnog stanja 01.01.) pokreće se na kraju godine.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
