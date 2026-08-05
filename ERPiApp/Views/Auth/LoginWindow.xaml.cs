using System.Windows;
using System.Windows.Input;
using ERPiApp.Views.Firma;
using ERPiApp.Views.Shell;
using ERPiData;

namespace ERPiApp.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly ErpiDbContext _db;

    public LoginWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        var firma = _db.Firme.FirstOrDefault();
        TxtFirma.Text = firma?.Naziv ?? "Nije dostupna firma";

#if DEBUG
        TxtUsername.Text = "admin";
        TxtPassword.Password = "admin123";
#endif
        TxtUsername.Focus();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"ERPi © 2026 Blagojević Boban - v{version?.ToString(3)}";
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DoLogin();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e) => DoLogin();

    private void DoLogin()
    {
        TxtError.Visibility = Visibility.Collapsed;

        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Unesite korisničko ime i lozinku.");
            return;
        }

        var korisnik = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == username);

        if (korisnik == null || !ErpiDbContext.VerifyPassword(password, korisnik.LozinkaHash))
        {
            ShowError("Pogrešno korisničko ime ili lozinka.");
            return;
        }

        if (!korisnik.IsActive)
        {
            ShowError("Vaš nalog je deaktiviran. Obratite se administratoru.");
            return;
        }

        AppSession.TrenutniKorisnik = korisnik;
        AppSession.TrenutnaFirma = _db.Firme.FirstOrDefault();
        korisnik.PoslednjaPrijava = DateTime.Now;
        _db.SaveChanges();

        // Podrazumevana lozinka iz seed-a je javno poznata (nalazi se u izvornom kodu i
        // migracijama). Faza 2 ovde samo upozorava umesto da blokira (kao ERPiFinansije) —
        // hard-blokiranje čeka ekran za upravljanje korisnicima, koji još ne postoji.
        var defaultPasswordActive = ErpiDbContext.VerifyPassword("admin123", korisnik.LozinkaHash);

        var mainWindow = new MainWindow(_db);
        if (defaultPasswordActive) mainWindow.PrikaziUpozorenjeODefaultLozinci();
        mainWindow.Show();

        Close();
    }

    private void BtnPromeniFirmu_Click(object sender, RoutedEventArgs e)
    {
        _db.Dispose();
        new CompanySelectWindow().Show();
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
