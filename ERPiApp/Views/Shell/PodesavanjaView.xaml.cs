using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using ERPiData;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Shell;

public partial class PodesavanjaView : UserControl
{
    private readonly ErpiDbContext _db;

    public PodesavanjaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Prikaži trenutno stanje toggle-a
        TglInfoTraka.IsChecked = AppConfig.PrikaziInfoTraku;
        TglStartMaximized.IsChecked = AppConfig.StartMaximized;

        // Prikaži info o verziji i putanjama
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVerzija.Text = $"v{version?.ToString(3)} ({System.IO.Path.GetFileName(AppConfig.DbPath)})";
        TxtDbPath.Text = AppConfig.DbPath;
        TxtSettingsPath.Text = System.IO.Path.Combine(AppConfig.AppDataDir, "ui_settings.json");

        // Učitaj SEF podešavanja aktivne firme
        try
        {
            var firma = await _db.Firme.AsNoTracking().FirstOrDefaultAsync();
            if (firma != null)
            {
                TxtSefApiKey.Text = firma.SefApiKey ?? "";
                TxtJbkjsBroj.Text = firma.JbkjsBroj ?? "";
                TxtFirmaEmail.Text = firma.Email ?? "";
                CmbSefEnvironment.SelectedIndex = (firma.SefEnvironment ?? "Demo")
                    .Equals("Production", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                TxtPfrUrl.Text = string.IsNullOrWhiteSpace(firma.PfrUrl) ? "http://localhost:8443" : firma.PfrUrl;
                TxtPfrPacKod.Text = string.IsNullOrWhiteSpace(firma.PfrPacKod) ? "123456" : firma.PfrPacKod;
                TxtPfrKasirName.Text = string.IsNullOrWhiteSpace(firma.PfrKasirName) ? "Glavni Kasir" : firma.PfrKasirName;
                ChkPfrSimulatorMod.IsChecked = firma.PfrSimulatorMod;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju SEF podešavanja:\n{ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        OsveziStatusWebServera();
    }

    private void OsveziStatusWebServera()
    {
        TxtWebServerStatus.Inlines.Clear();

        if (ErpiWebServer.IsRunning)
        {
            string url = ErpiWebServer.DashboardUrl;
            TxtWebServerStatus.Inlines.Add(new Run("🟢 Server je aktivan na "));

            var link = new Hyperlink(new Run($"http://localhost:{ErpiWebServer.Port}")) { NavigateUri = new Uri(url) };
            link.RequestNavigate += WebServerLink_RequestNavigate;
            TxtWebServerStatus.Inlines.Add(link);

            TxtWebServerStatus.Foreground = System.Windows.Media.Brushes.Green;
            TxtApiToken.Text = ErpiWebServer.AccessToken;
        }
        else
        {
            TxtWebServerStatus.Inlines.Add(new Run("🔴 Server je zaustavljen"));
            TxtWebServerStatus.Foreground = System.Windows.Media.Brushes.Red;
            TxtApiToken.Text = "— server nije pokrenut —";
        }
    }

    private void WebServerLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju pretraživača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }

    private void BtnPokreniWebServer_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtApiPort.Text.Trim(), out int port) || port <= 0)
            port = 5050;

        try
        {
            ErpiWebServer.Start(AppConfig.DbPath, port);
            OsveziStatusWebServera();
            MessageBox.Show(
                $"🌐 Web Server i Cloud REST API je pokrenut na portu {port}.\n\n" +
                "Pristup je zaštićen tokenom koji važi do zaustavljanja servera. Dashboard otvorite " +
                "dugmetom „Otvori Web Dashboard\" — token se tada automatski prosleđuje.\n\n" +
                "Zaustavite server kada vam više nije potreban.",
                "Web Server Pokrenut", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pokretanju Web Servera:\n{ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZaustaviWebServer_Click(object sender, RoutedEventArgs e)
    {
        ErpiWebServer.Stop();
        OsveziStatusWebServera();
        MessageBox.Show("⏹️ Web Server je zaustavljen.", "Web Server Zaustavljen", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOtvoriWebDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (!ErpiWebServer.IsRunning)
        {
            BtnPokreniWebServer_Click(sender, e);
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ErpiWebServer.DashboardUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju pretraživača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TglInfoTraka_Checked(object sender, RoutedEventArgs e)
    {
        AppConfig.PrikaziInfoTraku = true;
        RefreshInfoTraka();
    }

    private void TglInfoTraka_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfig.PrikaziInfoTraku = false;
        RefreshInfoTraka();
    }

    private static void RefreshInfoTraka()
    {
        // Pronađi MainWindow i ažuriraj vidljivost info trake odmah
        if (Application.Current.MainWindow is MainWindow mw)
            mw.UpdateInfoTrakaVisibility();
    }

    private void TglStartMaximized_Checked(object sender, RoutedEventArgs e) => AppConfig.StartMaximized = true;

    private void TglStartMaximized_Unchecked(object sender, RoutedEventArgs e) => AppConfig.StartMaximized = false;

    private async void BtnSacuvajSef_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var firma = await _db.Firme.FirstOrDefaultAsync();
            if (firma == null)
            {
                MessageBox.Show("Aktivna firma nije pronađena u bazi.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            firma.SefApiKey = string.IsNullOrWhiteSpace(TxtSefApiKey.Text) ? null : TxtSefApiKey.Text.Trim();
            firma.JbkjsBroj = string.IsNullOrWhiteSpace(TxtJbkjsBroj.Text) ? null : TxtJbkjsBroj.Text.Trim();
            firma.Email = string.IsNullOrWhiteSpace(TxtFirmaEmail.Text) ? null : TxtFirmaEmail.Text.Trim();
            firma.SefEnvironment = CmbSefEnvironment.SelectedIndex == 1 ? "Production" : "Demo";

            firma.PfrUrl = string.IsNullOrWhiteSpace(TxtPfrUrl.Text) ? "http://localhost:8443" : TxtPfrUrl.Text.Trim();
            firma.PfrPacKod = string.IsNullOrWhiteSpace(TxtPfrPacKod.Text) ? "123456" : TxtPfrPacKod.Text.Trim();
            firma.PfrKasirName = string.IsNullOrWhiteSpace(TxtPfrKasirName.Text) ? "Glavni Kasir" : TxtPfrKasirName.Text.Trim();
            firma.PfrSimulatorMod = ChkPfrSimulatorMod.IsChecked ?? false;

            await _db.SaveChangesAsync();

            MessageBox.Show("SEF/PFR podešavanja su uspešno sačuvana!", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju SEF podešavanja:\n{ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnTestirajSef_Click(object sender, RoutedEventArgs e)
    {
        string apiKey = TxtSefApiKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            MessageBox.Show("Molimo unesite SEF API ključ pre testiranja konekcije.", "Upozorenje",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string env = CmbSefEnvironment.SelectedIndex == 1 ? "Production" : "Demo";

        BtnTestirajSef.IsEnabled = false;
        try
        {
            var client = new SefApiClient(apiKey, env);
            var res = await client.TestConnectionAsync();

            if (res.Success)
                MessageBox.Show($"✅ {res.Message}", "SEF Konekcija Uspešna", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"❌ {res.Message}", "Greška Konekcije", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnTestirajSef.IsEnabled = true;
        }
    }

    private async void BtnTestirajPfr_Click(object sender, RoutedEventArgs e)
    {
        var pfrClient = new PfrApiClient();
        var postavke = new PfrPostavke
        {
            PfrUrl = TxtPfrUrl.Text.Trim(),
            PacKod = TxtPfrPacKod.Text.Trim(),
            Kasir = TxtPfrKasirName.Text.Trim(),
            SimulatorMod = ChkPfrSimulatorMod.IsChecked ?? false
        };

        BtnTestirajPfr.IsEnabled = false;
        try
        {
            var (success, message) = await pfrClient.TestirajPfrKonekcijuAsync(postavke);

            if (success)
                MessageBox.Show($"✅ {message}", "PFR Konekcija Uspešna", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"❌ {message}", "Greška PFR Konekcije", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnTestirajPfr.IsEnabled = true;
        }
    }

    private void BtnUvozDOS_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new ERPiApp.Views.Finansije.DosImportWindow(_db)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pokretanju uvoza iz DOS sistema:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
