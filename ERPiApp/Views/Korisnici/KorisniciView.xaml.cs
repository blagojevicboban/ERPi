using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp.Views.Korisnici;

public partial class KorisniciView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<Korisnik> _sviKorisnici = new();

    public KorisniciView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (s, e) => UcitajKorisnike();
    }

    private void UcitajKorisnike()
    {
        try
        {
            _sviKorisnici = _db.Korisnici.OrderBy(k => k.KorisnikId).ToList();
            PrimeniFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju korisnika:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrimeniFilter()
    {
        var upit = TxtPretraga.Text.Trim().ToLower();

        var filtrirani = string.IsNullOrWhiteSpace(upit)
            ? _sviKorisnici
            : _sviKorisnici.Where(k =>
                k.KorisnickoIme.ToLower().Contains(upit) ||
                k.ImeIPrezime.ToLower().Contains(upit) ||
                k.Uloga.ToString().ToLower().Contains(upit)).ToList();

        DgKorisnici.ItemsSource = filtrirani;
        TxtUkupno.Text = $"Ukupno: {filtrirani.Count} korisnika";
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => PrimeniFilter();

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e) => UcitajKorisnike();

    private void BtnNoviKorisnik_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Samo korisnici sa ulogom Administrator mogu dodavati nove korisničke naloge.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dijalog = new KorisnikEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            UcitajKorisnike();
        }
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not Korisnik korisnik) return;

        if (!AppSession.IsAdministrator && AppSession.TrenutniKorisnik?.KorisnikId != korisnik.KorisnikId)
        {
            MessageBox.Show("Nemate pravo izmene tuđih korisničkih naloga.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dijalog = new KorisnikEditWindow(_db, korisnik) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            UcitajKorisnike();
        }
    }

    private void BtnBrisi_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not Korisnik korisnik) return;

        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Samo korisnici sa ulogom Administrator mogu brisati i deaktivirati naloge.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (korisnik.KorisnickoIme == "admin")
        {
            MessageBox.Show("Glavni administratorski nalog (admin) ne može biti izbrisan ili deaktiviran.",
                "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rez = MessageBox.Show(
            $"Da li ste sigurni da želite da izbrišete ili deaktivirate nalog '{korisnik.KorisnickoIme}'?\n\nKliknite YES za brisanje ili NO za deaktivaciju.",
            "Potvrda brisanja / deaktivacije",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (rez == MessageBoxResult.Yes)
        {
            try
            {
                _db.Korisnici.Remove(korisnik);
                _db.SaveChanges();
                UcitajKorisnike();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri brisanju korisničkog naloga:\n{ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else if (rez == MessageBoxResult.No)
        {
            korisnik.IsActive = false;
            _db.SaveChanges();
            UcitajKorisnike();
        }
    }
}
