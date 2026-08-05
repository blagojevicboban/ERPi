using System.Windows;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class PartnerEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly Partner _partner;
    private readonly bool _jeNov;

    public PartnerEditWindow(ErpiDbContext db, Partner partner)
    {
        InitializeComponent();
        _db = db;
        _partner = partner;
        _jeNov = partner.PartnerId == 0;

        TxtNaslov.Text = _jeNov ? "➕ Nov partner" : "✏️ Izmena partnera";

        TxtSifra.Text = partner.SifraPartnera;
        TxtNaziv.Text = partner.Naziv;
        TxtAdresa.Text = partner.Adresa;
        TxtPib.Text = partner.Pib;
        TxtMaticniBroj.Text = partner.MaticniBroj;
        TxtJmbg.Text = partner.Jmbg;
        TxtTelefon.Text = partner.Telefon;
        TxtEmail.Text = partner.Email;
        TxtZiroRacun.Text = partner.ZiroRacun;
        TxtBankovniRacun.Text = partner.BankovniRacun;
        TxtNazivBanke.Text = partner.NazivBanke;

        ChkDobavljac.IsChecked = partner.JeDobavljac;
        ChkKupac.IsChecked = partner.JeKupac;
        ChkRadnik.IsChecked = partner.JeRadnik;
        ChkBanka.IsChecked = partner.JeBanka;
        ChkPoreskaUprava.IsChecked = partner.JePoreskaUprava;
        ChkAktivan.IsChecked = _jeNov || partner.IsActive;
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        var naziv = TxtNaziv.Text.Trim();
        if (string.IsNullOrEmpty(naziv))
        {
            ShowError("Naziv je obavezan.");
            return;
        }

        var sifra = TxtSifra.Text.Trim();
        if (string.IsNullOrEmpty(sifra))
        {
            ShowError("Šifra je obavezna.");
            return;
        }

        var vecPostoji = _db.Partneri.Any(p =>
            p.SifraPartnera == sifra && p.PartnerId != _partner.PartnerId);
        if (vecPostoji)
        {
            ShowError("Partner sa ovom šifrom već postoji.");
            return;
        }

        _partner.SifraPartnera = sifra;
        _partner.Naziv = naziv;
        _partner.Adresa = TxtAdresa.Text.Trim();
        _partner.Pib = TxtPib.Text.Trim();
        _partner.MaticniBroj = TxtMaticniBroj.Text.Trim();
        _partner.Jmbg = TxtJmbg.Text.Trim();
        _partner.Telefon = TxtTelefon.Text.Trim();
        _partner.Email = TxtEmail.Text.Trim();
        _partner.ZiroRacun = TxtZiroRacun.Text.Trim();
        _partner.BankovniRacun = TxtBankovniRacun.Text.Trim();
        _partner.NazivBanke = TxtNazivBanke.Text.Trim();
        _partner.JeDobavljac = ChkDobavljac.IsChecked == true;
        _partner.JeKupac = ChkKupac.IsChecked == true;
        _partner.JeRadnik = ChkRadnik.IsChecked == true;
        _partner.JeBanka = ChkBanka.IsChecked == true;
        _partner.JePoreskaUprava = ChkPoreskaUprava.IsChecked == true;
        _partner.IsActive = ChkAktivan.IsChecked == true;

        if (_jeNov) _db.Partneri.Add(_partner);
        _db.SaveChanges();

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
