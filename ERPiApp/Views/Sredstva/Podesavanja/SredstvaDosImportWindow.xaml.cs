using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiApp.Models;
using ERPiApp.Services;
using ERPiApp.Services.Finansije;
using ERPiData;
using ERPiMigration.Importers;
using ERPiSredstvaData;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Sredstva.Podesavanja;

/// <summary>
/// Uvoz i migracija DOS/DBF podataka za Osnovna sredstva — isti obrazac ekrana kao
/// <see cref="ERPiApp.Views.Finansije.DosImportWindow"/> (skeniranje radnog direktorijuma preko
/// KORISNIC.DBF, izbor jedne firme iz liste, log toka uvoza), ali bez checkbox-ova za module jer
/// Sredstva ima samo jedan fiksni skup DBF tabela (SREDSTVA/KARTICA/RASHOD/PRIJAVA/KONTPLAN).
/// Dodatno nudi odredište: uvoz u već-aktivnu firmu (kao ranije, iz
/// <see cref="PodesavanjaSredstvaView"/>), ili kreiranje potpuno nove ERPi firme (nova baza,
/// registrovana u <see cref="CompanyRegistryService"/>) popunjene podacima iz KORISNIC.DBF.
/// </summary>
public partial class SredstvaDosImportWindow : Window
{
    private readonly ErpiDbContext _aktivnaDb;
    private readonly CompanyRegistryService _registry = new();
    private List<DbfFirmaDto> _pronadjeneFirme = new();

    /// <summary>Popunjen samo ako je uvoz urađen u NOVU firmu — pozivalac (PodesavanjaSredstvaView)
    /// ga koristi da obavesti korisnika da je nova firma kreirana i registrovana.</summary>
    public CompanyEntry? NovaFirmaKreirana { get; private set; }

    public SredstvaDosImportWindow(ErpiDbContext aktivnaDb)
    {
        InitializeComponent();
        _aktivnaDb = aktivnaDb;

        var aktivnaFirma = _aktivnaDb.Firme.FirstOrDefault();
        RbAktivnaFirma.Content = $"🏢 Uvezi u aktivnu firmu: {aktivnaFirma?.Naziv ?? "(nepoznato)"}";

        string defaultPath = @"C:\SREDSTVA\SREDS";
        if (!Directory.Exists(defaultPath))
        {
            defaultPath = AppDomain.CurrentDomain.BaseDirectory;
        }

        TxtFolderPath.Text = defaultPath;
        SkenirajFolder(defaultPath);
    }

    private void SkenirajFolder(string folderPath)
    {
        try
        {
            _pronadjeneFirme = DosImportService.Instance.SkenirajRadniDirektorijum(folderPath);
            DgFirme.ItemsSource = _pronadjeneFirme;
            TxtFirmCount.Text = $"Pronađeno: {_pronadjeneFirme.Count} firmi";

            if (_pronadjeneFirme.Any())
            {
                DgFirme.SelectedItem = _pronadjeneFirme[0];
            }

            AppendLog($"Skeniran folder '{folderPath}'. Pronađeno {_pronadjeneFirme.Count} firmi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri skeniranju radnog foldera:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite radni direktorijum sa DOS/DBF podacima za Sredstva",
            InitialDirectory = TxtFolderPath.Text
        };

        if (dialog.ShowDialog() == true)
        {
            TxtFolderPath.Text = dialog.FolderName;
            SkenirajFolder(dialog.FolderName);
        }
    }

    private void DgFirme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgFirme.SelectedItem is DbfFirmaDto izabrana)
        {
            foreach (var f in _pronadjeneFirme) f.IsSelected = (f == izabrana);
            TxtStatus.Text = $"Izabrana firma: {izabrana.Naziv} ({izabrana.Sifra})";

            // Prepopunjava polja Nove firme sa podacima iz DOS-a — korisnik i dalje može ispraviti.
            TxtNovaFirmaNaziv.Text = izabrana.Naziv;
            TxtNovaFirmaSifra.Text = izabrana.Sifra;
            TxtNovaFirmaPib.Text = izabrana.Pib;
            TxtNovaFirmaMb.Text = izabrana.MaticniBroj;
        }
    }

    private void Odrediste_Changed(object sender, RoutedEventArgs e)
    {
        if (PnlNovaFirma == null) return; // poziva se i tokom InitializeComponent()

        bool novaFirma = RbNovaFirma.IsChecked == true;
        PnlNovaFirma.Visibility = novaFirma ? Visibility.Visible : Visibility.Collapsed;
        ChkBrisiPostojece.IsEnabled = !novaFirma; // nova firma je uvek prazna, brisanje nema smisla
        if (novaFirma) ChkBrisiPostojece.IsChecked = false;
        BtnStartImport.Content = novaFirma ? "🚀 Pokreni Uvoz u Novu Firmu" : "🚀 Pokreni Uvoz u Aktivnu Firmu";
    }

    private async void BtnStartImport_Click(object sender, RoutedEventArgs e)
    {
        var izabranaFirma = DgFirme.SelectedItem as DbfFirmaDto ?? _pronadjeneFirme.FirstOrDefault(f => f.IsSelected);
        if (izabranaFirma == null)
        {
            MessageBox.Show("Molimo izaberite firmu iz tabele za uvoz.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool uNovuFirmu = RbNovaFirma.IsChecked == true;
        ErpiDbContext destDb;
        string? novaFirmaDbPath = null;

        if (uNovuFirmu)
        {
            var naziv = TxtNovaFirmaNaziv.Text.Trim();
            if (string.IsNullOrEmpty(naziv))
            {
                MessageBox.Show("Naziv nove firme je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sifra = TxtNovaFirmaSifra.Text.Trim();
            if (string.IsNullOrEmpty(sifra)) sifra = "F" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var fileSafeNaziv = string.Concat(naziv.Split(Path.GetInvalidFileNameChars()));
            novaFirmaDbPath = Path.Combine(_registry.DefaultDataDirectory, $"{sifra}_{fileSafeNaziv}.db");

            if (File.Exists(novaFirmaDbPath))
            {
                MessageBox.Show("Baza sa ovim imenom već postoji na disku. Promenite šifru ili naziv nove firme.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                destDb = ErpiDbContext.Create(novaFirmaDbPath);
                destDb.Firme.Add(new ERPiData.Models.Core.Firma
                {
                    Sifra = sifra,
                    Naziv = naziv,
                    Pib = string.IsNullOrWhiteSpace(TxtNovaFirmaPib.Text) ? null : TxtNovaFirmaPib.Text.Trim(),
                    MaticniBroj = string.IsNullOrWhiteSpace(TxtNovaFirmaMb.Text) ? null : TxtNovaFirmaMb.Text.Trim()
                });
                await destDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kreiranje nove firme nije uspelo: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            destDb = _aktivnaDb;

            if (ChkBrisiPostojece.IsChecked == true)
            {
                var confirm = MessageBox.Show(
                    "Da li ste sigurni da želite da OBRIŠETE sve postojeće podatke o osnovnim sredstvima (sredstva, kartice, rashode, prijave, popise) iz aktivne firme pre uvoza?\n\nOva akcija je nepovratna!",
                    "Potvrda brisanja postojećih podataka",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;
            }
        }

        BtnStartImport.IsEnabled = false;
        TxtLog.Text = "";
        PbProgress.Value = 0;
        AppendLog($"Pokrećem DOS uvoz iz: {izabranaFirma.FolderPath}");

        var tempDb = Path.Combine(Path.GetTempPath(), $"erpi_sredstva_dos_{Guid.NewGuid():N}.db");
        try
        {
            if (!uNovuFirmu && ChkBrisiPostojece.IsChecked == true)
            {
                AppendLog("Brišem postojeće podatke o sredstvima, karticama, rashodima, prijavama i popisima...");
                destDb.PopisneStavke.RemoveRange(destDb.PopisneStavke);
                destDb.Popisi.RemoveRange(destDb.Popisi);
                destDb.SredstvaRashodi.RemoveRange(destDb.SredstvaRashodi);
                destDb.SredstvaPrijave.RemoveRange(destDb.SredstvaPrijave);
                destDb.SredstvaKartice.RemoveRange(destDb.SredstvaKartice);
                destDb.Sredstva.RemoveRange(destDb.Sredstva);
                await destDb.SaveChangesAsync();
                AppendLog("[OK] Postojeći podaci obrisani iz baze.");
            }

            PbProgress.Value = 15;
            TxtStatus.Text = "Čitanje DOS/DBF fajlova...";
            var migracija = await SredstvaDbfMigrator.MigrateAsync(izabranaFirma.FolderPath, tempDb, AppendLog);

            if (!migracija.Uspesno)
            {
                AppendLog($"[GREŠKA] {migracija.Poruka}");
                MessageBox.Show($"Greška pri čitanju DBF fajlova: {migracija.Poruka}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PbProgress.Value = 60;
            TxtStatus.Text = "Prenos u ERPi bazu...";
            AppendLog("Prosleđujem privremenu bazu u uvoz prema ERPi...");

            var options = new DbContextOptionsBuilder<SredstvaDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            using var srcDb = new SredstvaDbContext(options);
            var importer = new ErpiSredstvaProdukcijaImporter(destDb);
            var res = await importer.ImportFromDatabaseAsync(srcDb);

            PbProgress.Value = 100;

            if (res.Uspesno)
            {
                AppendLog("[OK] DOS uvoz uspešan.");
                TxtStatus.Text = "Uvoz završen";
                TxtPercentage.Text = "100%";

                if (uNovuFirmu && novaFirmaDbPath != null)
                {
                    var entry = new CompanyEntry
                    {
                        Sifra = TxtNovaFirmaSifra.Text.Trim(),
                        Naziv = TxtNovaFirmaNaziv.Text.Trim(),
                        Pib = TxtNovaFirmaPib.Text.Trim(),
                        DbPath = novaFirmaDbPath
                    };
                    var companies = _registry.Load();
                    companies.Add(entry);
                    _registry.Save(companies);
                    NovaFirmaKreirana = entry;
                }

                string poruka = uNovuFirmu
                    ? $"DOS uvoz je uspešan u NOVU firmu „{TxtNovaFirmaNaziv.Text.Trim()}“!\n\n" +
                      $" • Uvezeno sredstava: {res.UvezenoSredstava}\n" +
                      $" • Uvezeno kartica: {res.UvezenoKartica}\n" +
                      $" • Uvezeno prijava: {res.UvezenoPrijava}\n" +
                      $" • Uvezeno rashoda: {res.UvezenoRashoda}\n\n" +
                      "Nova firma je registrovana — pristupite joj preko „Promeni firmu“."
                    : $"DOS uvoz je uspešan!\n\n" +
                      $" • Uvezeno novih sredstava: {res.UvezenoSredstava} (od ukupno {migracija.UvezenoSredstava} u DBF)\n" +
                      $" • Uvezeno novih kartica: {res.UvezenoKartica} (od ukupno {migracija.UvezenoKartica} u DBF)\n" +
                      $" • Uvezeno novih prijava: {res.UvezenoPrijava} (od ukupno {migracija.UvezenoPrijava} u DBF)\n" +
                      $" • Uvezeno novih rashoda: {res.UvezenoRashoda} (od ukupno {migracija.UvezenoRashoda} u DBF)\n" +
                      $" • Uvezeno novih konta: {res.UvezenoKonta}\n" +
                      $" • Uvezeno partnera-dobavljača: {res.UvezenoPartneraDobavljaca}";

                MessageBox.Show(poruka, "DOS uvoz uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                AppendLog($"[GREŠKA] {res.Greska}");
                MessageBox.Show($"Greška pri uvozu u ERPi: {res.Greska}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            string detalji = ex.InnerException?.Message ?? ex.Message;
            AppendLog($"[GREŠKA] {ex.Message}");
            if (ex.InnerException != null) AppendLog($"[GREŠKA - detalji] {ex.InnerException.Message}");
            MessageBox.Show($"Neočekivana greška pri DOS uvozu: {ex.Message}\n\nDetalji: {detalji}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { /* ignoriši - privremeni fajl */ }
            if (uNovuFirmu) destDb.Dispose(); // aktivnaDb ostaje otvorena kod pozivaoca, nova baza se zatvara ovde
            BtnStartImport.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        TxtLog.ScrollToEnd();
    }
}
