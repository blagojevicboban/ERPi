using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class PartneriView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;
    private List<Partner> _sviPartneri = new();

    public PartneriView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _zatvaranjeService = new ZatvaranjeStavkiService(db);
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

    private async void DgPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e) => await OsveziStavke();

    private async void ChkSveStavke_Changed(object sender, RoutedEventArgs e) => await OsveziStavke();

    private async Task OsveziStavke()
    {
        if (DgPartneri.SelectedItem is not Partner partner)
        {
            DgStavke.ItemsSource = null;
            TxtStavkeNaslov.Text = "📋 Otvorene stavke";
            return;
        }

        TxtStavkeNaslov.Text = $"📋 Otvorene stavke — {partner.Naziv}";
        var samoOtvorene = ChkSveStavke.IsChecked != true;
        DgStavke.ItemsSource = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, samoOtvorene: samoOtvorene);
    }

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

    private async void BtnZatvoriStavke_Click(object sender, RoutedEventArgs e)
    {
        if (DgPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera.", "Nije izabran partner", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var otvorene = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId);
        var dlg = new ZatvoriStavkeWindow(_zatvaranjeService, otvorene) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await OsveziStavke();
    }

    private async void BtnKamata_Click(object sender, RoutedEventArgs e)
    {
        if (DgPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera.", "Nije izabran partner", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new KamataWindow(new KamataService(_db), partner) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await OsveziStavke();
    }

    private void BtnIosIzvestaj_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new IosIzvestajWindow(_zatvaranjeService) { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
    }
}
