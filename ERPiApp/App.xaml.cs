using System.IO;
using System.Windows;
using ERPiApp.Services;
using ERPiApp.Views.Firma;
using ERPiApp.Views.Shell;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Neočekivana greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

#if DEBUG
        // --autologin preskače CompanySelectWindow i LoginWindow, otvara/kreira jednu fiksnu
        // test firmu i ulazi kao prvi aktivan administrator. Postoji isključivo za UI
        // automatizaciju (.claude/skills/run-erpi-app) — SendKeys prema PasswordBox-u je
        // nepouzdan u tom okruženju. Ograđeno sa #if DEBUG — u Release build-u ovog koda
        // nema, pa se prijava ne može zaobići u isporučenoj aplikaciji.
        if (e.Args.Contains("--autologin") && PokusajAutoLogin()) return;
#endif

        // Prvi ekran je inače uvek izbor firme, ne login — jedna baza po firmi (Faza 1), pa se
        // korisničko ime/lozinka proveravaju tek pošto se zna koja baza se otvara.
        new CompanySelectWindow().Show();
    }

#if DEBUG
    private static bool PokusajAutoLogin()
    {
        try
        {
            var registry = new CompanyRegistryService();
            var dbPath = Path.Combine(registry.DefaultDataDirectory, "AUTOTEST.db");
            var db = ErpiDbContext.Create(dbPath);
            AppConfig.DbPath = dbPath; // vidi napomenu u CompanySelectWindow.Otvori()

            if (!db.Firme.Any())
            {
                db.Firme.Add(new Firma { Sifra = "AUTOTEST", Naziv = "ERPi Autotest d.o.o." });
                db.SaveChanges();
            }

            var korisnik = db.Korisnici.FirstOrDefault(k => k.IsActive && k.Uloga == UlogaKorisnika.Administrator)
                           ?? db.Korisnici.FirstOrDefault(k => k.IsActive);
            if (korisnik == null) return false;

            AppSession.TrenutniKorisnik = korisnik;
            AppSession.TrenutnaFirma = db.Firme.FirstOrDefault();
            new MainWindow(db).Show();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Automatska prijava (--autologin) nije uspela: {ex}");
            return false;
        }
    }
#endif
}
