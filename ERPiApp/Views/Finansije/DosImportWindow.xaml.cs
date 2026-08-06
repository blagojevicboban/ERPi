using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiApp.Models;
using ERPiApp.Services;
using ERPiData;
using ERPiApp.Services.Finansije;

namespace ERPiApp.Views.Finansije;

public partial class DosImportWindow : Window
{
    private readonly ErpiDbContext _aktivnaDb;
    private readonly CompanyRegistryService _registry = new();
    private List<DbfFirmaDto> _pronadjeneFirme = new();

    /// <summary>Popunjen samo ako je uvoz urađen u NOVU firmu — pozivalac ga koristi da obavesti
    /// korisnika da je nova firma kreirana i registrovana (po uzoru na
    /// <see cref="ERPiApp.Views.Sredstva.Podesavanja.SredstvaDosImportWindow"/>).</summary>
    public CompanyEntry? NovaFirmaKreirana { get; private set; }

    public DosImportWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _aktivnaDb = db;

        var aktivnaFirma = _aktivnaDb.Firme.FirstOrDefault();
        RbAktivnaFirma.Content = $"🏢 Uvezi u aktivnu firmu: {aktivnaFirma?.Naziv ?? "(nepoznato)"}";

        string defaultPath = @"C:\KNJIGE\Radni";
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
            Title = "Izaberite radni direktorijum sa DOS/DBF podacima",
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
            foreach (var f in _pronadjeneFirme)
            {
                f.IsSelected = (f == izabrana);
            }
            TxtStatus.Text = $"Izabrana firma: {izabrana.Naziv} ({izabrana.Sifra})";

            // Preuzima podatke o firmi iz DOS-a i nudi ih za pregled/ispravku pre uvoza u novu firmu —
            // korisnik i dalje može ispraviti bilo koje polje pre nego što pokrene uvoz.
            TxtNovaFirmaNaziv.Text = izabrana.Naziv;
            TxtNovaFirmaSifra.Text = izabrana.Sifra;
            TxtNovaFirmaPib.Text = izabrana.Pib;
            TxtNovaFirmaMb.Text = izabrana.MaticniBroj;
            TxtNovaFirmaAdresa.Text = izabrana.Adresa;
            TxtNovaFirmaMesto.Text = izabrana.PttIMesto;
            TxtNovaFirmaTelefon.Text = izabrana.Telefon;
            TxtNovaFirmaZiroRacun.Text = izabrana.ZiroRacun;
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

        bool importFinansijsko = ChkFinansijsko.IsChecked == true;
        bool importRobno = ChkRobno.IsChecked == true;
        bool importMaterijalno = ChkMaterijalno.IsChecked == true;

        if (!importFinansijsko && !importRobno && !importMaterijalno)
        {
            MessageBox.Show("Molimo štiklirajte bar jedan modul za uvoz (Finansijsko, Robno ili Materijalno).", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MaticniBroj = string.IsNullOrWhiteSpace(TxtNovaFirmaMb.Text) ? null : TxtNovaFirmaMb.Text.Trim(),
                    Adresa = string.IsNullOrWhiteSpace(TxtNovaFirmaAdresa.Text) ? null : TxtNovaFirmaAdresa.Text.Trim(),
                    PttIMesto = string.IsNullOrWhiteSpace(TxtNovaFirmaMesto.Text) ? null : TxtNovaFirmaMesto.Text.Trim(),
                    Telefon = string.IsNullOrWhiteSpace(TxtNovaFirmaTelefon.Text) ? null : TxtNovaFirmaTelefon.Text.Trim(),
                    ZiroRacun = string.IsNullOrWhiteSpace(TxtNovaFirmaZiroRacun.Text) ? null : TxtNovaFirmaZiroRacun.Text.Trim()
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
        }

        bool brisiPostojece = !uNovuFirmu && ChkBrisiPostojece.IsChecked == true;
        if (brisiPostojece)
        {
            var res = MessageBox.Show(
                $"UPOZORENJE: Izabrali ste opciju za BRISANJE postojećih podataka u izabranim modulima pre uvoza.\n\nDa li ste sigurni da želite obrisati postojeće podatke u aktivnoj bazi za izabrane module i izvršiti čisti uvoz iz firme '{izabranaFirma.Naziv}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;
        }

        BtnStartImport.IsEnabled = false;
        TxtLog.Text = "";
        AppendLog(uNovuFirmu
            ? $"Započet uvoz firme '{izabranaFirma.Naziv}' ({izabranaFirma.Sifra}) u NOVU firmu '{TxtNovaFirmaNaziv.Text.Trim()}'..."
            : $"Započet uvoz firme '{izabranaFirma.Naziv}' ({izabranaFirma.Sifra}) u aktivnu bazu...");

        var progressHandler = new Progress<DosImportProgress>(p =>
        {
            PbProgress.Value = p.Percentage;
            TxtPercentage.Text = $"{p.Percentage}%";
            TxtStatus.Text = $"{p.FirmName} - {p.StepDescription}";
            if (!string.IsNullOrEmpty(p.LogMessage))
            {
                AppendLog(p.LogMessage);
            }
        });

        try
        {
            await DosImportService.Instance.UveziJednuFirmuAsync(
                destDb,
                izabranaFirma,
                importFinansijsko,
                importRobno,
                importMaterijalno,
                brisiPostojece,
                progressHandler);

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

                MessageBox.Show(
                    $"Uvoz je uspešno završen za NOVU firmu '{entry.Naziv}'!\n\nIzabrani moduli su zavedeni u novokreiranu ERPi bazu. Nova firma je registrovana — pristupite joj preko „Promeni firmu“ u zaglavlju aplikacije.",
                    "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show($"Uvoz je uspešno završen za firmu '{izabranaFirma.Naziv}'!\n\nIzabrani moduli su uspešno zavedeni u aktivnu ERPi bazu.",
                    "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
        }
        catch (Exception ex)
        {
            string errDetails = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show($"Greška pri uvozu podataka:\n{ex.Message}\n\nDetalji: {errDetails}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (uNovuFirmu) destDb.Dispose(); // aktivna baza ostaje otvorena kod pozivaoca, nova se zatvara ovde
            BtnStartImport.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        TxtLog.ScrollToEnd();
    }
}
