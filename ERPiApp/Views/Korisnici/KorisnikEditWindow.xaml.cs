using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp.Views.Korisnici;

public partial class KorisnikEditWindow : Window
{
    private readonly ErpiDbContext _db;
    public Korisnik Korisnik { get; private set; }
    private readonly bool _isNew;

    public KorisnikEditWindow(ErpiDbContext db, Korisnik? korisnik = null)
    {
        InitializeComponent();
        _db = db;

        CmbUloga.ItemsSource = Enum.GetValues<UlogaKorisnika>();

        if (korisnik == null)
        {
            _isNew = true;
            Korisnik = new Korisnik { IsActive = true, Uloga = UlogaKorisnika.Operater };
            TxtTitle.Text = "👤 Dodavanje korisničkog naloga";
            LblLozinka.Text = "Lozinka *";
            CmbUloga.SelectedItem = UlogaKorisnika.Operater;
        }
        else
        {
            _isNew = false;
            Korisnik = korisnik;
            TxtTitle.Text = "✏️ Izmena korisničkog naloga";
            LblLozinka.Text = "Nova lozinka (opciono)";
            TxtLozinkaHint.Visibility = Visibility.Visible;
            PopuniPolja();
        }
    }

    private void PopuniPolja()
    {
        TxtKorisnickoIme.Text = Korisnik.KorisnickoIme;
        TxtImeIPrezime.Text = Korisnik.ImeIPrezime;
        ChkIsActive.IsChecked = Korisnik.IsActive;
        CmbUloga.SelectedItem = Korisnik.Uloga;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BtnCancel_Click(sender, e);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var username = TxtKorisnickoIme.Text.Trim();
        var name = TxtImeIPrezime.Text.Trim();
        var password = TxtLozinka.Password;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Molimo unesite korisničko ime.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtKorisnickoIme.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Molimo unesite ime i prezime korisnika.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtImeIPrezime.Focus();
            return;
        }

        if (_isNew && string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Molimo unesite lozinku za novi nalog.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtLozinka.Focus();
            return;
        }

        var existing = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == username && k.KorisnikId != Korisnik.KorisnikId);
        if (existing != null)
        {
            MessageBox.Show($"Korisničko ime '{username}' već postoji u bazi. Izaberite drugo.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtKorisnickoIme.Focus();
            return;
        }

        Korisnik.KorisnickoIme = username;
        Korisnik.ImeIPrezime = name;
        Korisnik.IsActive = ChkIsActive.IsChecked ?? true;

        if (CmbUloga.SelectedItem is UlogaKorisnika izabranaUloga)
        {
            Korisnik.Uloga = izabranaUloga;
        }

        if (!string.IsNullOrEmpty(password))
        {
            Korisnik.LozinkaHash = ErpiDbContext.HashPassword(password);
        }

        try
        {
            if (_isNew)
            {
                _db.Korisnici.Add(Korisnik);
            }
            _db.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju korisničkog naloga:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
