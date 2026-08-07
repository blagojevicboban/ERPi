using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiApp.Services;
using ERPiApp.Views.Finansije.Izvestaji;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using FirmaModel = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class PartneriView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<Partner> _sviPartneri = new();
    private Partner? _izabraniPartner;
    private bool _ucitavanjeKonta;

    public PartneriView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        LoadPartnere();
    }

    private async void LoadPartnere() => await OsveziPartnereAsync();

    private async Task OsveziPartnereAsync(int? selektujPartnerId = null)
    {
        try
        {
            var service = new OtvoreneStavkeService(_db);
            _sviPartneri = await service.GetPartneriAsync();
            PrimeniFilterPartnera();

            if (selektujPartnerId is int id)
            {
                LstPartneri.SelectedItem = _sviPartneri.FirstOrDefault(p => p.PartnerId == id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LstPartneri_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && ItemsControl.ContainerFromElement(LstPartneri, d) is ListBoxItem item)
        {
            item.IsSelected = true;
        }
    }

    private async void MiIzmeniPartnera_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new PartnerEditWindow(_db, partner) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            await OsveziPartnereAsync(partner.PartnerId);
        }
    }

    private void MiObrisiPartnera_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner) return;

        var koristiSeUNalozima = _db.StavkeNaloga.Any(s => s.PartnerId == partner.PartnerId);
        if (koristiSeUNalozima)
        {
            MessageBox.Show("Partner je korišćen u nalozima za knjiženje i ne može se obrisati.",
                "Partner je u upotrebi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show($"Obrisati partnera „{partner.Naziv}\"?", "Potvrda brisanja",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        _db.Partneri.Remove(partner);
        _db.SaveChanges();
        LoadPartnere();
    }

    private void BtnNovPartner_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PartnerEditWindow(_db, new Partner()) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) LoadPartnere();
    }

    private void TxtPretragaPartnera_TextChanged(object sender, TextChangedEventArgs e) => PrimeniFilterPartnera();

    private void RbFilterPartneri_Checked(object sender, RoutedEventArgs e) => PrimeniFilterPartnera();

    private void PrimeniFilterPartnera()
    {
        if (LstPartneri == null) return;

        IEnumerable<Partner> izvor = _sviPartneri;
        if (RbPartneriKupci?.IsChecked == true)
        {
            izvor = izvor.Where(JeKontoKupca);
        }
        else if (RbPartneriDobavljaci?.IsChecked == true)
        {
            izvor = izvor.Where(JeKontoDobavljaca);
        }

        string search = TxtPretragaPartnera?.Text?.Trim()?.ToLower() ?? "";
        if (!string.IsNullOrEmpty(search))
        {
            izvor = izvor.Where(p => p.SifraPartnera.ToLower().Contains(search) || p.Naziv.ToLower().Contains(search));
        }

        LstPartneri.ItemsSource = izvor.ToList();
    }

    private static bool JeKontoKupca(Partner p)
    {
        string konto = p.KontoPartnera ?? p.SifraPartnera;
        return konto.StartsWith(KontoPicker.Grupe.KupciNoviZakon, StringComparison.Ordinal)
            || konto.StartsWith(KontoPicker.Grupe.KupciStariZakon, StringComparison.Ordinal);
    }

    private static bool JeKontoDobavljaca(Partner p)
    {
        string konto = p.KontoPartnera ?? p.SifraPartnera;
        return konto.StartsWith(KontoPicker.Grupe.DobavljaciNoviZakon, StringComparison.Ordinal)
            || konto.StartsWith(KontoPicker.Grupe.DobavljaciStariZakon, StringComparison.Ordinal);
    }

    private async void LstPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner) return;

        _izabraniPartner = partner;
        TxtNaslovPartnera.Text = partner.Naziv;
        TxtPodnaslovPartnera.Text = $"Šifra: {partner.SifraPartnera}" + (string.IsNullOrWhiteSpace(partner.Pib) ? "" : $" | PIB: {partner.Pib}");

        try
        {
            var service = new OtvoreneStavkeService(_db);
            List<PartnerKontoInfo> konta = partner.PartnerId > 0
                ? await service.GetPartnerKontaAsync(partner.PartnerId)
                : new List<PartnerKontoInfo> { new() { BrojKonta = partner.KontoPartnera ?? partner.SifraPartnera, NazivKonta = partner.Naziv, BrojStavki = 0 } };

            _ucitavanjeKonta = true;
            CmbKontoKartice.ItemsSource = konta;
            CmbKontoKartice.SelectedIndex = konta.Count > 0 ? 0 : -1;
            _ucitavanjeKonta = false;

            bool viseKonta = konta.Count > 1;
            CmbKontoKartice.Visibility = viseKonta ? Visibility.Visible : Visibility.Collapsed;
            TxtKontoJedini.Visibility = viseKonta ? Visibility.Collapsed : Visibility.Visible;
            TxtKontoJedini.Text = konta.Count == 1 ? konta[0].Prikaz : "—";

            if (konta.Count > 0)
            {
                await UcitajKarticuZaKontoAsync(partner, konta[0].BrojKonta);
            }
            else
            {
                DgOtvoreneStavke.ItemsSource = null;
                TxtSaldoPartnera.Text = 0m.ToString("N2");
            }

            AzurirajStanjeObracunaKamate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (TabStavke.SelectedIndex == 1)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private async void CmbKontoKartice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ucitavanjeKonta || _izabraniPartner == null) return;
        if (CmbKontoKartice.SelectedItem is not PartnerKontoInfo konto) return;

        await UcitajKarticuZaKontoAsync(_izabraniPartner, konto.BrojKonta);
        AzurirajStanjeObracunaKamate();
    }

    private void AzurirajStanjeObracunaKamate()
    {
        string? brojKonta = (CmbKontoKartice.SelectedItem as PartnerKontoInfo)?.BrojKonta;
        bool jeKupac = !string.IsNullOrWhiteSpace(brojKonta) &&
            (brojKonta.StartsWith(KontoPicker.Grupe.KupciNoviZakon, StringComparison.Ordinal) ||
             brojKonta.StartsWith(KontoPicker.Grupe.KupciStariZakon, StringComparison.Ordinal));

        BtnObracunKamate.IsEnabled = jeKupac;
        BtnObracunKamate.ToolTip = jeKupac
            ? "Kalkulacija i obračun zatezne kamate za dospela potraživanja"
            : "Obračun kamate je moguć samo za konto kupca (204/120)";
    }

    private async Task UcitajKarticuZaKontoAsync(Partner partner, string brojKonta)
    {
        try
        {
            var service = new OtvoreneStavkeService(_db);
            var stavke = partner.PartnerId > 0
                ? await service.GetOtvoreneStavkeAsync(partner.PartnerId, brojKonta)
                : await service.GetOtvoreneStavkeZaKontoAsync(brojKonta);

            DgOtvoreneStavke.ItemsSource = stavke;
            TxtSaldoPartnera.Text = (stavke.Count > 0 ? stavke[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TabStavke_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabStavke.SelectedIndex == 1 && _izabraniPartner != null)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private async Task LoadPraveOtvoreneStavkeAsync()
    {
        if (_izabraniPartner == null) return;

        try
        {
            var service = new ZatvaranjeStavkiService(_db);
            DgPraveOtvoreneStavke.ItemsSource = _izabraniPartner.PartnerId > 0
                ? await service.GetOtvoreneStavkeZaPartneraAsync(_izabraniPartner.PartnerId)
                : await service.GetOtvoreneStavkeZaKontoAsync(_izabraniPartner.KontoPartnera ?? _izabraniPartner.SifraPartnera);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki (IOS): {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnZatvoriStavke_Click(object sender, RoutedEventArgs e)
    {
        if (_izabraniPartner == null)
        {
            MessageBox.Show("Izaberite partnera.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var service = new ZatvaranjeStavkiService(_db);
        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(_izabraniPartner.PartnerId);
        var dlg = new ZatvoriStavkeWindow(service, otvorene) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private void BtnIstorijaZatvaranja_Click(object sender, RoutedEventArgs e)
    {
        if (_izabraniPartner == null)
        {
            MessageBox.Show("Izaberite partnera.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var service = new ZatvaranjeStavkiService(_db);
        var dijalog = new IstorijaZatvaranjaWindow(service, _izabraniPartner.PartnerId) { Owner = Window.GetWindow(this) };
        dijalog.ShowDialog();
        _ = LoadPraveOtvoreneStavkeAsync();
    }

    private async void BtnStampajIOS_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za izvoz IOS obrasca.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var service = new OtvoreneStavkeService(_db);
            var grupe = await service.GetIosIzvestajAsync(null, null, null, null, true);
            var izabranaGrupa = grupe.FirstOrDefault(g => g.SifraPartnera == partner.SifraPartnera || g.NazivPartnera == partner.Naziv);

            var firma = await _db.Firme.FirstOrDefaultAsync() ?? new FirmaModel { Naziv = "Moja Firma" };
            var list = izabranaGrupa != null ? new List<IosPartnerGrupa> { izabranaGrupa } : new List<IosPartnerGrupa>();

            var win = new IosPreviewWindow(list, firma) { Owner = Window.GetWindow(this) };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju IOS štampanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnObracunKamate_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za obračun kamate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new KamataWindow(new KamataService(_db), partner) { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
    }

    private void BtnKursnaLista_Click(object sender, RoutedEventArgs e)
    {
        var win = new KursnaListaWindow(_db) { Owner = Window.GetWindow(this) };
        win.ShowDialog();
    }

    private async void BtnVerifikujRacun_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Molimo izaberite partnera sa liste za verifikaciju tekućeg računa.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string pibIliMb = !string.IsNullOrWhiteSpace(partner.Pib) ? partner.Pib : partner.MaticniBroj ?? "";
        if (string.IsNullOrWhiteSpace(pibIliMb))
        {
            MessageBox.Show($"Partner '{partner.Naziv}' nema unet PIB ni matični broj.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var client = new NbsApiClient();
        var res = await client.ProveriTekuciRacunPartneraAsync(pibIliMb);

        if (res.Success)
        {
            string poruka = $"🏛️ NBS REGISTAR TEKUĆIH RAČUNA:\n\n" +
                            $"• Partner: {partner.Naziv}\n" +
                            $"• PIB / MB: {pibIliMb}\n" +
                            $"• Tekući račun: {res.TekuciRacun ?? partner.ZiroRacun ?? "Nije registrovan"}\n" +
                            $"• Status naloga: {res.StatusBlokade}\n\n" +
                            $"Aplikacija je verifikovala podatke u zvaničnom registru NBS.";

            MessageBox.Show(poruka, "Verifikacija tekućeg računa NBS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"❌ {res.Message}", "Greška pri verifikaciji", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelPartneri_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgOtvoreneStavke, TxtNaslovPartnera.Text, "Partneri_Otvorene_Stavke");
}
