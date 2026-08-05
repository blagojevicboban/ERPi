using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class PartneriView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<Partner> _sviPartneri = new();

    public PartneriView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Ucitaj();
    }

    private void Ucitaj()
    {
        _sviPartneri = _db.Partneri.OrderBy(p => p.Naziv).ToList();
        Filtriraj();
    }

    private void Filtriraj()
    {
        IEnumerable<Partner> upit = _sviPartneri;

        var pretraga = TxtPretraga.Text?.Trim();
        if (!string.IsNullOrEmpty(pretraga))
        {
            upit = upit.Where(p =>
                p.Naziv.Contains(pretraga, StringComparison.OrdinalIgnoreCase) ||
                p.SifraPartnera.Contains(pretraga, StringComparison.OrdinalIgnoreCase) ||
                (p.Pib?.Contains(pretraga, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        DgPartneri.ItemsSource = upit.ToList();
        DgStavke.ItemsSource = null;
        TxtStavkeNaslov.Text = "📋 Otvorene stavke";
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void DgPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgPartneri.SelectedItem is not Partner partner)
        {
            DgStavke.ItemsSource = null;
            TxtStavkeNaslov.Text = "📋 Otvorene stavke";
            return;
        }

        TxtStavkeNaslov.Text = $"📋 Otvorene stavke — {partner.Naziv}";
        DgStavke.ItemsSource = UcitajOtvoreneStavke(partner.PartnerId);
    }

    /// <summary>
    /// Hronološke proknjižene stavke partnera, sa kumulativnim saldom koji se restartuje na
    /// svaki novi konto — partner koji je i kupac (204x) i dobavljač (435x) ima DVA nezavisna
    /// salda, ne jedan pomešan (to pomešano ne bi odgovaralo nijednom stvarnom kontu).
    /// </summary>
    private List<StavkaRed> UcitajOtvoreneStavke(int partnerId)
    {
        var stavke = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == partnerId && s.Nalog!.Status == StatusNaloga.Proknjizen)
            .OrderBy(s => s.KontoId)
            .ThenBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToList();

        var rezultat = new List<StavkaRed>();
        int? prethodniKonto = null;
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            if (prethodniKonto != s.KontoId) saldo = 0m;
            saldo += s.Duguje - s.Potrazuje;
            prethodniKonto = s.KontoId;

            rezultat.Add(new StavkaRed(
                s.Konto?.Prikaz ?? "?",
                s.Nalog!.DatumNaloga,
                s.Nalog.BrojNaloga,
                string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis ?? "") : s.Opis,
                s.Duguje,
                s.Potrazuje,
                saldo));
        }

        return rezultat;
    }

    private record StavkaRed(string Konto, DateTime Datum, int BrojNaloga, string Opis, decimal Duguje, decimal Potrazuje, decimal Saldo);

    private void DgPartneri_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => IzmeniIzabranog();

    private void BtnNovPartner_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PartnerEditWindow(_db, new Partner()) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) Ucitaj();
    }

    private void BtnIzmeniPartnera_Click(object sender, RoutedEventArgs e) => IzmeniIzabranog();

    private void IzmeniIzabranog()
    {
        if (DgPartneri.SelectedItem is not Partner partner) return;
        var dlg = new PartnerEditWindow(_db, partner) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) Ucitaj();
    }

    private void BtnObrisiPartnera_Click(object sender, RoutedEventArgs e)
    {
        if (DgPartneri.SelectedItem is not Partner partner) return;

        var koristiSeUNalozima = _db.StavkeNaloga.Any(s => s.PartnerId == partner.PartnerId);
        if (koristiSeUNalozima)
        {
            MessageBox.Show("Partner je korišćen u nalozima za knjiženje i ne može se obrisati.",
                "Partner je u upotrebi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show($"Obrisati partnera „{partner.Naziv}"+"\"?", "Potvrda brisanja",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        _db.Partneri.Remove(partner);
        _db.SaveChanges();
        Ucitaj();
    }
}
