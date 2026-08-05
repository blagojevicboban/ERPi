using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiMigration.Importers;
using ERPiSredstvaData;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Sredstva.Podesavanja;

/// <summary>
/// Podešavanja Sredstva modula — za sada samo DOS/DBF uvoz (SREDSTVA.DBF/KARTICA.DBF/RASHOD.DBF/
/// PRIJAVA.DBF/KONTPLAN.DBF/KORISNIC.DBF, Faza 7.2b), po uzoru na
/// <see cref="ERPiApp.Views.Zarade.Podesavanja.PodesavanjaZaradeView"/>-ov DOS uvoz tab. Za razliku
/// od Zarade nema karticu "Uvoz iz postojeće instalacije" (EF-to-EF iz žive ERPiSredstvaApp baze) —
/// namerno izostavljeno, DOS uvoz je jedini traženi put za ovaj modul.
/// </summary>
public partial class PodesavanjaSredstvaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly StringBuilder _log = new();

    public PodesavanjaSredstvaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private void Log(string poruka)
    {
        _log.AppendLine(poruka);
        TxtLog.Text = _log.ToString();
        LogScroll.ScrollToEnd();
    }

    private void BtnIzaberiDbfFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite folder sa DOS/DBF fajlovima (npr. C:\\SREDSTVA\\SREDS\\KOR28)"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtDbfFolder.Text = dlg.FolderName;
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

        BtnPokreniDosUvoz.IsEnabled = false;

        var tempDb = Path.Combine(Path.GetTempPath(), $"erpi_sredstva_dos_{Guid.NewGuid():N}.db");
        try
        {
            Log($"Pokrećem DOS uvoz iz: {dbfDir}");
            var migracija = await SredstvaDbfMigrator.MigrateAsync(dbfDir, tempDb, Log);

            if (!migracija.Uspesno)
            {
                Log($"[GREŠKA] {migracija.Poruka}");
                MessageBox.Show($"Greška pri čitanju DBF fajlova: {migracija.Poruka}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ChkBrisiPostojece.IsChecked == true)
            {
                var confirm = MessageBox.Show(
                    "Da li ste sigurni da želite da OBRIŠETE sve postojeće podatke o osnovnim sredstvima (sredstva, kartice, rashode, prijave, popise) iz ove firme pre uvoza?\n\nOva akcija je nepovratna!",
                    "Potvrda brisanja postojećih podataka",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    Log("[OTKAZANO] Uvoz je otkazan od strane korisnika.");
                    BtnPokreniDosUvoz.IsEnabled = true;
                    return;
                }

                Log("Brišem postojeće podatke o sredstvima, karticama, rashodima, prijavama i popisima...");
                _db.PopisneStavke.RemoveRange(_db.PopisneStavke);
                _db.Popisi.RemoveRange(_db.Popisi);
                _db.SredstvaRashodi.RemoveRange(_db.SredstvaRashodi);
                _db.SredstvaPrijave.RemoveRange(_db.SredstvaPrijave);
                _db.SredstvaKartice.RemoveRange(_db.SredstvaKartice);
                _db.Sredstva.RemoveRange(_db.Sredstva);
                await _db.SaveChangesAsync();
                Log("[OK] Postojeći podaci obrisani iz baze.");
            }

            Log("Prosleđujem privremenu bazu u uvoz prema ERPi...");
            var options = new DbContextOptionsBuilder<SredstvaDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            using var srcDb = new SredstvaDbContext(options);
            var importer = new ErpiSredstvaProdukcijaImporter(_db);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            if (res.Uspesno)
            {
                Log("[OK] DOS uvoz uspešan.");
                string poruka;
                if (res.UvezenoSredstava == 0 && res.UvezenoKartica == 0 && res.UvezenoPrijava == 0 && res.UvezenoRashoda == 0 && migracija.UvezenoSredstava > 0)
                {
                    poruka = $"DOS uvoz je uspešno završen!\n\n" +
                             $"Svi podaci iz DOS DBF fajlova već postoje u bazi i sprečeno je dupliranje:\n" +
                             $" • Pronađeno sredstava u DBF: {migracija.UvezenoSredstava} (već postoje u bazi)\n" +
                             $" • Pronađeno kartica u DBF: {migracija.UvezenoKartica} (već postoje u bazi)\n" +
                             $" • Pronađeno rashoda u DBF: {migracija.UvezenoRashoda} (već postoje u bazi)\n" +
                             $" • Pronađeno prijava u DBF: {migracija.UvezenoPrijava} (već postoje u bazi)";
                }
                else
                {
                    poruka = $"DOS uvoz je uspešan!\n\n" +
                             $" • Uvezeno novih sredstava: {res.UvezenoSredstava} (od ukupno {migracija.UvezenoSredstava} u DBF)\n" +
                             $" • Uvezeno novih kartica: {res.UvezenoKartica} (od ukupno {migracija.UvezenoKartica} u DBF)\n" +
                             $" • Uvezeno novih prijava: {res.UvezenoPrijava} (od ukupno {migracija.UvezenoPrijava} u DBF)\n" +
                             $" • Uvezeno novih rashoda: {res.UvezenoRashoda} (od ukupno {migracija.UvezenoRashoda} u DBF)\n" +
                             $" • Uvezeno novih konta: {res.UvezenoKonta}\n" +
                             $" • Uvezeno partnera-dobavljača: {res.UvezenoPartneraDobavljaca}";
                }
                MessageBox.Show(poruka, "DOS uvoz uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
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
            BtnPokreniDosUvoz.IsEnabled = true;
        }
    }
}
