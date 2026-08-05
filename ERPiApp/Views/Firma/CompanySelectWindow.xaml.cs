using System.IO;
using System.Windows;
using ERPiApp.Models;
using ERPiApp.Services;
using ERPiApp.Views.Auth;
using ERPiData;
using Microsoft.Win32;

namespace ERPiApp.Views.Firma;

public partial class CompanySelectWindow : Window
{
    private readonly CompanyRegistryService _registry = new();
    private List<CompanyEntry> _companies = new();

    public CompanySelectWindow()
    {
        InitializeComponent();
        UcitajListu();
    }

    private void UcitajListu()
    {
        _companies = _registry.Load();
        LstFirme.ItemsSource = _companies;

        var imaFirmi = _companies.Count > 0;
        LstFirme.Visibility = imaFirmi ? Visibility.Visible : Visibility.Collapsed;
        TxtEmptyState.Visibility = imaFirmi ? Visibility.Collapsed : Visibility.Visible;

        if (imaFirmi) LstFirme.SelectedIndex = 0;
    }

    private void BtnNovaFirma_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NovaFirmaWindow(_registry) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Kreirana != null)
        {
            _companies.Add(dlg.Kreirana);
            _registry.Save(_companies);
            UcitajListu();
            LstFirme.SelectedItem = _companies.LastOrDefault(c => c.DbPath == dlg.Kreirana.DbPath);
        }
    }

    private void BtnOtvoriBazu_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Izaberite ERPi bazu (.db)",
            Filter = "ERPi baza (*.db)|*.db|Sve datoteke (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        if (_companies.Any(c => string.Equals(c.DbPath, dlg.FileName, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("Ova baza je već na listi.");
            return;
        }

        try
        {
            using var db = ErpiDbContext.Create(dlg.FileName);
            var firma = db.Firme.FirstOrDefault();
            var entry = new CompanyEntry
            {
                Sifra = firma?.Sifra ?? "—",
                Naziv = firma?.Naziv ?? Path.GetFileNameWithoutExtension(dlg.FileName),
                Pib = firma?.Pib ?? "",
                DbPath = dlg.FileName
            };
            _companies.Add(entry);
            _registry.Save(_companies);
            UcitajListu();
            LstFirme.SelectedItem = _companies.LastOrDefault(c => c.DbPath == entry.DbPath);
        }
        catch (Exception ex)
        {
            ShowError($"Baza se ne može otvoriti: {ex.Message}");
        }
    }

    private void BtnUkloni_Click(object sender, RoutedEventArgs e)
    {
        if (LstFirme.SelectedItem is not CompanyEntry selected) return;

        var potvrda = MessageBox.Show(
            $"Ukloniti „{selected.Naziv}" + "\" sa liste?\n\nBaza podataka na disku se NE briše.",
            "Ukloni sa liste", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        _companies.Remove(selected);
        _registry.Save(_companies);
        UcitajListu();
    }

    private void LstFirme_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Otvori();

    private void BtnOtvori_Click(object sender, RoutedEventArgs e) => Otvori();

    private void Otvori()
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (LstFirme.SelectedItem is not CompanyEntry selected)
        {
            ShowError("Izaberite firmu sa liste.");
            return;
        }

        if (!File.Exists(selected.DbPath))
        {
            ShowError("Baza na disku nije pronađena. Uklonite je sa liste ili je ponovo dodajte preko 📂.");
            return;
        }

        ErpiDbContext db;
        try
        {
            db = ErpiDbContext.Create(selected.DbPath);
        }
        catch (Exception ex)
        {
            ShowError($"Baza se ne može otvoriti: {ex.Message}");
            return;
        }

        // Zarade ekrani (portovani iz samostalnog ERPiZaradeApp-a) svaki otvara SVOJ
        // ErpiDbContext preko AppConfig.DbPath umesto da dele ovaj _db — nasleđe iz sveta
        // gde je ERPiZaradeApp imao tačno jednu bazu. Bez ovoga AppConfig.DbPath ostaje
        // nepostavljen i pada na svoj podrazumevani "prvi .db u %LocalAppData%\ERPiApp\Baze"
        // koji nema nikakve veze sa izabranom firmom — svi Zarade ekrani deluju prazno bez
        // obzira koja je firma zapravo aktivna. Mora se postaviti ovde, pre nego što se
        // otvori ijedan Zarade ekran.
        AppConfig.DbPath = selected.DbPath;

        var loginWindow = new LoginWindow(db);
        loginWindow.Show();
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
