using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ERPiApp.Views.SefPfr;

/// <summary>
/// SEF e-Fakture i PFR fiskalizacija — radi nad PRAVIM proknjiženim <see cref="RacunOtpremnica"/>
/// zapisima (ne nad izmišljenim demo dokumentima). Ruta se predlaže po tipu partnera, u skladu sa
/// Zakonom o fiskalizaciji: pravno lice (ima PIB) → SEF e-Faktura; fizičko lice / bez partnera →
/// PFR fiskalni račun. Vidi PLAN_NASTAVKA.md.
/// </summary>
public partial class SefFaktureView : UserControl
{
    private readonly ErpiDbContext _db;

    public SefFaktureView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajFakture();
    }

    private static bool JeSefKandidat(Partner? p) => p != null && !string.IsNullOrWhiteSpace(p.Pib);

    public void UcitajFakture()
    {
        var query = _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .AsNoTracking()
            .Where(r => r.TipDokumenta == TipRacunOtpremnice.Racun && r.IsKnjizen)
            .AsQueryable();

        var text = TxtPretraga?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(r => r.BrojRacuna.ToString().Contains(text) || (r.Partner != null && r.Partner.Naziv.Contains(text)));
        }

        DgSefFakture.ItemsSource = query.OrderByDescending(r => r.DatumRacuna).ToList();
        AzurirajDugmad();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajFakture();
    }

    private void DgSefFakture_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AzurirajDugmad();
    }

    private void AzurirajDugmad()
    {
        var selektovan = DgSefFakture.SelectedItem as RacunOtpremnica;
        bool jeSef = selektovan != null && JeSefKandidat(selektovan.Partner);
        bool jePfr = selektovan != null && !JeSefKandidat(selektovan.Partner);

        BtnPosaljiNaSef.IsEnabled = jeSef;
        BtnSacuvajUbl.IsEnabled = jeSef;
        BtnFiskalizuj.IsEnabled = jePfr;
        BtnOsveziStatus.IsEnabled = selektovan != null;
    }

    private async void BtnPosaljiNaSef_Click(object sender, RoutedEventArgs e)
    {
        if (DgSefFakture.SelectedItem is not RacunOtpremnica selektovan) return;

        try
        {
            var service = new SefService(_db);
            var (success, message) = await service.PosaljiNaSefAsync(selektovan.RacunOtpremnicaId);
            MessageBox.Show(message, success ? "SEF" : "Greška", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            UcitajFakture();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri slanju na SEF: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnFiskalizuj_Click(object sender, RoutedEventArgs e)
    {
        if (DgSefFakture.SelectedItem is not RacunOtpremnica selektovan) return;

        try
        {
            var service = new PfrService(_db);
            var (success, message) = await service.FiskalizujRacunOtpremnicuAsync(selektovan.RacunOtpremnicaId);
            MessageBox.Show(message, success ? "Fiskalizacija" : "Greška", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            UcitajFakture();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri fiskalizaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOsveziStatus_Click(object sender, RoutedEventArgs e)
    {
        if (DgSefFakture.SelectedItem is not RacunOtpremnica selektovan) return;

        try
        {
            var service = new SefService(_db);
            var (success, message, _) = await service.OsveziStatusNaSefuAsync(selektovan.RacunOtpremnicaId);
            MessageBox.Show(message, success ? "SEF status" : "Info", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Information);
            UcitajFakture();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri osvežavanju statusa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnSacuvajUbl_Click(object sender, RoutedEventArgs e)
    {
        if (DgSefFakture.SelectedItem is not RacunOtpremnica selektovan) return;

        var dialog = new SaveFileDialog
        {
            Filter = "UBL 2.1 XML (*.xml)|*.xml",
            FileName = $"Racun_{selektovan.BrojRacuna}.xml"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var service = new SefService(_db);
            var (success, message) = await service.SacuvajUblXmlFajlAsync(selektovan.RacunOtpremnicaId, dialog.FileName);
            MessageBox.Show(message, success ? "Uspeh" : "Greška", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju XML fajla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnUlazneFakture_Click(object sender, RoutedEventArgs e)
    {
        var win = new SefUlazneFaktureWindow(_db) { Owner = Window.GetWindow(this) };
        win.ShowDialog();
    }
}
