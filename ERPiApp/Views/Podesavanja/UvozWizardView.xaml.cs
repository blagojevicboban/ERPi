using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiFinansijeData;
using ERPiZaradeData;
using ERPiMigration.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ERPiApp.Views.Podesavanja;

public partial class UvozWizardView : UserControl
{
    private readonly ErpiDbContext _db;

    public UvozWizardView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        // Ako u %LocalAppData%\ERPiFinansije\Baze postoji baza.db, postavi je kao podrazumevanu
        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERPiFinansije", "Baze", "baza.db");
        if (File.Exists(defaultPath))
        {
            TxtPutanjaBaze.Text = defaultPath;
        }
    }

    private void BtnIzaberiBazu_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "SQLite baza (*.db)|*.db|Svi fajlovi (*.*)|*.*",
            Title = "Izaberite staru baza.db datoteku"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtPutanjaBaze.Text = dlg.FileName;
        }
    }

    private void BtnAnaliziraj_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.ExpandEnvironmentVariables(TxtPutanjaBaze.Text.Trim());
        if (!File.Exists(path))
        {
            MessageBox.Show($"Datoteka baze ne postoji na putanji: {path}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;

            using var srcDb = new AccountingDbContext(options);

            var brPartnera = srcDb.Partneri.Count();
            var brKonta = srcDb.Konta.Count();
            var brNaloga = srcDb.Nalozi.Count();
            var brMagacina = srcDb.Magacini.Count();
            var brArtikala = srcDb.Artikli.Count();
            var brKalkulacija = srcDb.Kalkulacije.Count();

            TxtStatistika.Text = $"• Partneri: {brPartnera}\n• Kontni plan: {brKonta} konta\n• Glavna knjiga: {brNaloga} naloga\n• Magacini: {brMagacina}\n• Artikli: {brArtikala}\n• Kalkulacije: {brKalkulacija}";
            PnlStatistika.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čitanju baze: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnPokreniUvoz_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.ExpandEnvironmentVariables(TxtPutanjaBaze.Text.Trim());
        if (!File.Exists(path))
        {
            MessageBox.Show($"Datoteka baze ne postoji na putanji: {path}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BtnPokreniUvoz.IsEnabled = false;
        BtnAnaliziraj.IsEnabled = false;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;

            using var srcDb = new AccountingDbContext(options);

            var importer = new ErpiFinansijeImporter(_db);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            if (res.Success)
            {
                MessageBox.Show(res.Message, "Uvoz uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Greška pri uvozu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Neočekivana greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnPokreniUvoz.IsEnabled = true;
            BtnAnaliziraj.IsEnabled = true;
        }
    }

    private async void BtnPokreniUvozZarade_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.ExpandEnvironmentVariables(TxtPutanjaBaze.Text.Trim());
        if (!File.Exists(path))
        {
            MessageBox.Show($"Datoteka baze ne postoji na putanji: {path}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BtnPokreniUvozZarade.IsEnabled = false;
        BtnPokreniUvoz.IsEnabled = false;
        BtnAnaliziraj.IsEnabled = false;

        try
        {
            var options = new DbContextOptionsBuilder<PlataDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;

            using var srcDb = new PlataDbContext(options);

            var importer = new ErpiZaradeProdukcijaImporter(_db);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            if (res.Uspesno)
            {
                MessageBox.Show($"Uvoz iz ERPiZarade je uspešan!\n\n" +
                                $"• Uvezeno radnika: {res.UvezenoRadnika}\n" +
                                $"• Uvezeno obračuna: {res.UvezenoObracuna}\n" +
                                $"• Uvezeno isplata: {res.UvezenoIsplata}\n" +
                                $"• Uvezeno ugovora: {res.UvezenoUgovora}\n" +
                                $"• Uvezeno radnih sati: {res.UvezenoRadnihSati}\n" +
                                $"• Uvezeno kredita: {res.UvezenoKredita}\n" +
                                $"• Uvezeno PPP-PD prijava: {res.UvezenoPppPdPrijava}\n" +
                                $"• Uvezeno bolovanja: {res.UvezenoBolovanja}",
                                "Uvoz iz ERPiZarade uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Greška pri uvozu: {res.Greska}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Neočekivana greška pri uvozu iz ERPiZarade: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnPokreniUvozZarade.IsEnabled = true;
            BtnPokreniUvoz.IsEnabled = true;
            BtnAnaliziraj.IsEnabled = true;
        }
    }

    private void BtnPokreniUvozDOS_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new ERPiApp.Views.Finansije.DosImportWindow(_db)
            {
                Owner = Window.GetWindow(this)
            };
            if (window.ShowDialog() == true && window.NovaFirmaKreirana != null)
            {
                MessageBox.Show(
                    $"Nova firma „{window.NovaFirmaKreirana.Naziv}“ je kreirana i registrovana.\n\nPristupite joj preko „Promeni firmu“ u zaglavlju aplikacije.",
                    "Nova firma kreirana", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju prozora za DOS uvoz:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
