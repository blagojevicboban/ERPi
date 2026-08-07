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
using ERPiData.Models.Core;
using FirmaModel = ERPiData.Models.Core.Firma;
using ERPiData.Models.Finansije;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.Kompenzacije;

public partial class KompenzacijeView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly KompenzacijaService _service;
    private List<Kompenzacija> _sveKompenzacije = new();

    public KompenzacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KompenzacijaService(_db);
        Loaded += KompenzacijeView_Loaded;
    }

    private void KompenzacijeView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadKompenzacije();
        LoadSkeniranje();
    }

    private async void LoadKompenzacije()
    {
        try
        {
            _sveKompenzacije = await _service.GetKompenzacijeAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kompenzacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadSkeniranje()
    {
        try
        {
            var kandidati = await _service.GetObostranaDugovanjaAsync();
            DgKandidati.ItemsSource = kandidati;
        }
        catch { }
    }

    private void Filtriraj()
    {
        if (DgKompenzacije == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _sveKompenzacije.Where(k =>
            string.IsNullOrEmpty(search) ||
            k.BrojDokumenta.ToLower().Contains(search) ||
            k.NazivPartnera.ToLower().Contains(search)
        ).ToList();

        DgKompenzacije.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgKompenzacije.SelectedIndex = 0;
        }
        else
        {
            DgKompenzacijaStavke.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void DgKompenzacije_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgKompenzacije.SelectedItem is Kompenzacija k)
        {
            DgKompenzacijaStavke.ItemsSource = k.Stavke;
        }
        else
        {
            DgKompenzacijaStavke.ItemsSource = null;
        }
    }

    private void BtnOsveziSkeniranje_Click(object sender, RoutedEventArgs e) => LoadSkeniranje();

    private void BtnNovaKompenzacija_Click(object sender, RoutedEventArgs e)
    {
        var nova = new Kompenzacija { Datum = DateTime.Today };
        OtvoriEditor(nova);
    }

    private void BtnIzmeniKompenzacija_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is not Kompenzacija k) return;

        if (k.IsKnjizeno)
        {
            MessageBox.Show("Proknjižena kompenzacija se ne može menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        OtvoriEditor(k);
    }

    private void DgKandidati_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgKandidati.SelectedItem is not ObostranoDugovanjeCandidate kandidat) return;

        var nova = new Kompenzacija { Datum = DateTime.Today, PartnerId = kandidat.PartnerId };
        OtvoriEditor(nova);
    }

    private void OtvoriEditor(Kompenzacija kompenzacija)
    {
        var dijalog = new KompenzacijaEditWindow(_db, kompenzacija) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKompenzacije();
            LoadSkeniranje();
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            if (k.IsKnjizeno)
            {
                MessageBox.Show("Proknjižene kompenzacije se ne mogu brisati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Da li ste sigurni da želite obrisati kompenzaciju br. {k.BrojDokumenta}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.ObrisiKompenzacijuAsync(k.KompenzacijaId);
                LoadKompenzacije();
                LoadSkeniranje();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            if (k.IsKnjizeno)
            {
                MessageBox.Show("Kompenzacija je već proknjižena.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Da li želite da proknjižite kompenzaciju br. {k.BrojDokumenta} i automatski zatvorite stavke u IOS-u?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (success, msg, nalogId) = await _service.KnjiziIZatvoriKompenzacijuAsync(k.KompenzacijaId);
                if (success)
                {
                    MessageBox.Show(msg, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadKompenzacije();
                    LoadSkeniranje();
                }
                else
                {
                    MessageBox.Show(msg, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKompenzacije, "Evidencija kompenzacija i poravnanja", "Kompenzacije_Poravnanja");

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is not Kompenzacija k)
        {
            MessageBox.Show("Izaberite kompenzaciju za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };
            var pdfBytes = PdfReportService.GenerisiKompenzacijuPdf(firma, k);

            string siguranBroj = string.Join("_", (k.BrojDokumenta ?? "Kompenzacija").Split(Path.GetInvalidFileNameChars()));
            string tempPath = Path.Combine(Path.GetTempPath(), $"Kompenzacija_{siguranBroj}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju Izjave o kompenzaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
