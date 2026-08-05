using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiMigration.Importers;
using ERPiZaradeData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ERPiApp.Views.Zarade.Podesavanja;

/// <summary>
/// Podešavanja Zarade modula. Za sada samo uvoz podataka (iz ERPiZarade instalacije
/// direktno EF Core → EF Core, ili iz starih DOS/DBF fajlova preko privremene
/// ERPiZaradeData baze) — rezervna kopija i e-mail podešavanja (kao u ERPiZarade
/// PodesavanjaPage) dolaze u sledećoj fazi.
/// </summary>
public partial class PodesavanjaZaradeView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly StringBuilder _log = new();

    public PodesavanjaZaradeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        var defaultZaradeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERPiZaradeApp", "Baze");
        if (Directory.Exists(defaultZaradeFolder))
        {
            var prviDb = Directory.GetFiles(defaultZaradeFolder, "*.db")
                                   .FirstOrDefault(f => !f.EndsWith("_stara_PlataApp.db", StringComparison.OrdinalIgnoreCase));
            if (prviDb != null)
            {
                TxtPutanjaZaradeBaze.Text = prviDb;
            }
        }
    }

    private void Log(string poruka)
    {
        _log.AppendLine(poruka);
        TxtLog.Text = _log.ToString();
        LogScroll.ScrollToEnd();
    }

    private void BtnIzaberiZaradeBazu_Click(object sender, RoutedEventArgs e)
    {
        var defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERPiZaradeApp", "Baze");
        var dlg = new OpenFileDialog
        {
            Filter = "SQLite baza (*.db)|*.db|Svi fajlovi (*.*)|*.*",
            Title = "Izaberite plata.db / firma_*.db datoteku ERPiZarade programa",
            InitialDirectory = Directory.Exists(defaultFolder) ? defaultFolder : ""
        };

        if (dlg.ShowDialog() == true)
        {
            TxtPutanjaZaradeBaze.Text = dlg.FileName;
        }
    }

    private void BtnIzaberiDbfFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Izaberite folder sa DOS/DBF fajlovima (npr. C:\\PLATA\\KOR28)"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtDbfFolder.Text = dlg.FolderName;
        }
    }

    private async void BtnPokreniUvozZarade_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.ExpandEnvironmentVariables(TxtPutanjaZaradeBaze.Text.Trim());
        if (!File.Exists(path))
        {
            MessageBox.Show($"Datoteka baze ne postoji na putanji: {path}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BtnPokreniUvozZarade.IsEnabled = false;
        BtnPokreniDosUvoz.IsEnabled = false;

        try
        {
            Log($"Otvaram ERPiZarade bazu: {path}");
            var options = new DbContextOptionsBuilder<PlataDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;

            using var srcDb = new PlataDbContext(options);

            var importer = new ErpiZaradeProdukcijaImporter(_db);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            if (res.Uspesno)
            {
                Log("[OK] Uvoz iz ERPiZarade uspešan.");
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
                Log($"[GREŠKA] {res.Greska}");
                MessageBox.Show($"Greška pri uvozu: {res.Greska}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"[GREŠKA] {ex.Message}");
            MessageBox.Show($"Neočekivana greška pri uvozu iz ERPiZarade: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnPokreniUvozZarade.IsEnabled = true;
            BtnPokreniDosUvoz.IsEnabled = true;
        }
    }

    private async void BtnPokreniDosUvoz_Click(object sender, RoutedEventArgs e)
    {
        var dbfDir = TxtDbfFolder.Text.Trim();
        if (!Directory.Exists(dbfDir))
        {
            MessageBox.Show($"Folder sa DBF fajlovima ne postoji: {dbfDir}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BtnPokreniUvozZarade.IsEnabled = false;
        BtnPokreniDosUvoz.IsEnabled = false;

        var tempDb = Path.Combine(Path.GetTempPath(), $"erpi_zarade_dos_{Guid.NewGuid():N}.db");
        try
        {
            Log($"Pokrećem DOS uvoz iz: {dbfDir}");
            var migracija = await ZaradeDbfMigrator.MigrateAsync(dbfDir, tempDb, Log);

            if (!migracija.Uspesno)
            {
                Log($"[GREŠKA] {migracija.Poruka}");
                MessageBox.Show($"Greška pri čitanju DBF fajlova: {migracija.Poruka}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log("Prosleđujem privremenu bazu u uvoz prema ERPi...");
            var options = new DbContextOptionsBuilder<PlataDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            using var srcDb = new PlataDbContext(options);
            var importer = new ErpiZaradeProdukcijaImporter(_db);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            if (res.Uspesno)
            {
                Log("[OK] DOS uvoz uspešan.");
                MessageBox.Show($"DOS uvoz je uspešan!\n\n" +
                                $"• Uvezeno radnika: {res.UvezenoRadnika}\n" +
                                $"• Uvezeno obračuna: {res.UvezenoObracuna}\n" +
                                $"• Uvezeno radnih sati: {res.UvezenoRadnihSati}",
                                "DOS uvoz uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Log($"[GREŠKA] {res.Greska}");
                MessageBox.Show($"Greška pri uvozu u ERPi: {res.Greska}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"[GREŠKA] {ex.Message}");
            MessageBox.Show($"Neočekivana greška pri DOS uvozu: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { /* ignoriši - privremeni fajl */ }
            BtnPokreniUvozZarade.IsEnabled = true;
            BtnPokreniDosUvoz.IsEnabled = true;
        }
    }
}
