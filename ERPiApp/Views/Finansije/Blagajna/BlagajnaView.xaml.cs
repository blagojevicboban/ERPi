using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiApp.Services;
using ERPiData;
using ERPiData.Models.Core;
using FirmaModel = ERPiData.Models.Core.Firma;
using ERPiData.Models.Finansije;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.Blagajna;

public partial class BlagajnaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BlagajnaService _service;
    private List<BlagajnickiNalog> _sviNalozi = new();

    public BlagajnaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BlagajnaService(_db);
        Loaded += BlagajnaView_Loaded;
    }

    private void BlagajnaView_Loaded(object sender, RoutedEventArgs e)
    {
        DpDnevnikOd.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DpDnevnikDo.SelectedDate = DateTime.Today;

        LoadNalozi();
        LoadDnevnik();
    }

    private async void LoadNalozi()
    {
        try
        {
            _sviNalozi = await _service.GetBlagajnickiNaloziAsync();
            FiltrirajNaloge();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju naloga blagajne: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadDnevnik()
    {
        try
        {
            VrstaBlagajne vrsta = CmbDnevnikBlagajna?.SelectedIndex == 1 ? VrstaBlagajne.Devizna : VrstaBlagajne.Dinarska;
            DateTime odD = DpDnevnikOd?.SelectedDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime doD = DpDnevnikDo?.SelectedDate ?? DateTime.Today;

            var (redovi, summary) = await _service.GetBlagajnickiDnevnikAsync(vrsta, odD, doD);

            DgDnevnik.ItemsSource = redovi;
            TxtPocetnoStanje.Text = $"{summary.PocetnoStanje:N2} RSD";
            TxtUkupnoUplata.Text = $"{summary.UkupnoUplata:N2} RSD";
            TxtUkupnoIsplata.Text = $"{summary.UkupnoIsplata:N2} RSD";
            TxtKrajnjeStanje.Text = $"{summary.KrajnjeStanje:N2} RSD";
        }
        catch { }
    }

    private void FiltrirajNaloge()
    {
        if (DgBlagajnickiNalozi == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";
        int filterIndex = CmbFilterBlagajna?.SelectedIndex ?? 0;

        var filtered = _sviNalozi.Where(b =>
            (filterIndex == 0 || (filterIndex == 1 && b.VrstaBlagajne == VrstaBlagajne.Dinarska) || (filterIndex == 2 && b.VrstaBlagajne == VrstaBlagajne.Devizna)) &&
            (string.IsNullOrEmpty(search) ||
             b.BrojNaloga.ToLower().Contains(search) ||
             b.UplatilacIsplatilac.ToLower().Contains(search) ||
             b.Svrha.ToLower().Contains(search))
        ).ToList();

        DgBlagajnickiNalozi.ItemsSource = filtered;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajNaloge();
    private void CmbFilterBlagajna_SelectionChanged(object sender, SelectionChangedEventArgs e) => FiltrirajNaloge();

    private void CmbDnevnikFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadDnevnik();
    private void DpDnevnikFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadDnevnik();
    private void BtnOsveziDnevnik_Click(object sender, RoutedEventArgs e) => LoadDnevnik();

    private void BtnNovaUplata_Click(object sender, RoutedEventArgs e)
    {
        var nov = new BlagajnickiNalog { VrstaNaloga = VrstaBlagajnickogNaloga.Uplata };
        var win = new BlagajnickiNalogEditWindow(nov, _db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadNalozi();
            LoadDnevnik();
        }
    }

    private void BtnNovaIsplata_Click(object sender, RoutedEventArgs e)
    {
        var nov = new BlagajnickiNalog { VrstaNaloga = VrstaBlagajnickogNaloga.Isplata };
        var win = new BlagajnickiNalogEditWindow(nov, _db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadNalozi();
            LoadDnevnik();
        }
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgBlagajnickiNalozi?.SelectedItem is BlagajnickiNalog bn)
        {
            if (bn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi nalozi blagajne se ne mogu menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var full = await _service.GetBlagajnickiNalogByIdAsync(bn.BlagajnickiNalogId);
            if (full != null)
            {
                var win = new BlagajnickiNalogEditWindow(full, _db) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true)
                {
                    LoadNalozi();
                    LoadDnevnik();
                }
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgBlagajnickiNalozi?.SelectedItem is BlagajnickiNalog bn)
        {
            if (bn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi nalozi blagajne se ne mogu brisati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Da li ste sigurni da želite obrisati nalog blagajne br. {bn.BrojNaloga}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.ObrisiBlagajnickiNalogAsync(bn.BlagajnickiNalogId);
                LoadNalozi();
                LoadDnevnik();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgBlagajnickiNalozi?.SelectedItem is BlagajnickiNalog bn)
        {
            if (bn.IsKnjizeno)
            {
                MessageBox.Show("Nalog blagajne je već proknjižen.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string kontoBlagajne = bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "2440" : "2430";
            var res = MessageBox.Show($"Da li želite da proknjižite nalog blagajne br. {bn.BrojNaloga} na Konto {kontoBlagajne} u Glavnoj knjizi?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (success, msg, nalogId) = await _service.KnjiziBlagajnickiNalogAsync(bn.BlagajnickiNalogId);
                if (success)
                {
                    MessageBox.Show(msg, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadNalozi();
                    LoadDnevnik();
                }
                else
                {
                    MessageBox.Show(msg, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgBlagajnickiNalozi, "Evidencija naloga blagajne", "NaloziBlagajne");

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (DgBlagajnickiNalozi?.SelectedItem is not BlagajnickiNalog bn)
        {
            MessageBox.Show("Molimo izaberite nalog blagajne za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };
            var pdfBytes = PdfReportService.GenerisiBlagajnickiNalogPdf(firma, bn);

            string siguranBroj = string.Join("_", (bn.BrojNaloga ?? "Nalog").Split(Path.GetInvalidFileNameChars()));
            string tempPath = Path.Combine(Path.GetTempPath(), $"BlagajnickiNalog_{siguranBroj}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju štampanog naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaDnevnik_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };

            VrstaBlagajne vrsta = CmbDnevnikBlagajna?.SelectedIndex == 1 ? VrstaBlagajne.Devizna : VrstaBlagajne.Dinarska;
            DateTime odD = DpDnevnikOd?.SelectedDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime doD = DpDnevnikDo?.SelectedDate ?? DateTime.Today;

            var (redovi, summary) = await _service.GetBlagajnickiDnevnikAsync(vrsta, odD, doD);
            var pdfBytes = PdfReportService.GenerisiBlagajnickiDnevnikPdf(firma, vrsta, odD, doD, redovi, summary);

            string tempPath = Path.Combine(Path.GetTempPath(), $"BlagajnickiDnevnik_{vrsta}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju Dnevnika blagajne: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
