using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Kompenzacije;

/// <summary>
/// Jedan red otvorene stavke (kupac ili dobavljač) u editoru kompenzacije, sa checkbox-selekcijom
/// i "slotom" (P{PartnerId}) preko kog se stavke vraćaju svom vlasniku kad se Partner2/3 zameni ili
/// ukloni. Za razliku od ERPiFinansije, ERPi nema "sintetičke partnere" izvedene iz legacy konta —
/// StavkaNaloga.PartnerId je pravi FK, pa je SlotKljuc uvek "P{PartnerId}".
/// </summary>
public class OtvorenaStavkaZaKompenzaciju : INotifyPropertyChanged
{
    public OtvorenaStavkaRed Stavka { get; }
    public int PartnerId { get; }
    public string NazivPartnera { get; }
    public string SlotKljuc { get; }

    public OtvorenaStavkaZaKompenzaciju(OtvorenaStavkaRed stavka, int partnerId, string nazivPartnera, string slotKljuc)
    {
        Stavka = stavka;
        PartnerId = partnerId;
        NazivPartnera = nazivPartnera;
        SlotKljuc = slotKljuc;
    }

    public int StavkaNalogaId => Stavka.StavkaNalogaId;
    public string BrojDokumenta => Stavka.BrojDokumenta ?? "";
    public DateTime Datum => Stavka.Datum;
    public decimal Preostalo => Stavka.Preostalo;
    public string BrojKonta => Stavka.Konto;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class KompenzacijaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly KompenzacijaService _service;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;
    private readonly Kompenzacija _kompenzacija;

    private readonly ObservableCollection<OtvorenaStavkaZaKompenzaciju> _kupciStavke = new();
    private readonly ObservableCollection<OtvorenaStavkaZaKompenzaciju> _dobavljaciStavke = new();
    private List<Partner> _partneri = new();

    private Partner? _partner1;
    private Partner? _partner2;
    private Partner? _partner3;

    public KompenzacijaEditWindow(ErpiDbContext db, Kompenzacija kompenzacija)
    {
        InitializeComponent();
        _db = db;
        _service = new KompenzacijaService(_db);
        _zatvaranjeService = new ZatvaranjeStavkiService(_db);
        _kompenzacija = kompenzacija;

        DgKupciStavke.ItemsSource = _kupciStavke;
        DgDobavljaciStavke.ItemsSource = _dobavljaciStavke;

        Loaded += KompenzacijaEditWindow_Loaded;
    }

    private static string SlotKljuc(Partner p) => $"P{p.PartnerId}";

    private async void KompenzacijaEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _partneri = _db.Partneri.Where(p => p.JeKupac || p.JeDobavljac).OrderBy(p => p.Naziv).ToList();
            CmbPartneri.ItemsSource = _partneri;
            CmbPartner2.ItemsSource = _partneri;
            CmbPartner3.ItemsSource = _partneri;

            DpDatum.SelectedDate = _kompenzacija.Datum;
            TxtNapomena.Text = _kompenzacija.Napomena;

            CmbVrsta.SelectedIndex = (int)_kompenzacija.Vrsta;
            AzurirajVidljivostPartnera();

            if (_kompenzacija.PartnerId is int p1) await SelectirajPartnera(CmbPartneri, p1);
            if (_kompenzacija.Partner2Id is int p2) await SelectirajPartnera(CmbPartner2, p2);
            if (_kompenzacija.Partner3Id is int p3) await SelectirajPartnera(CmbPartner3, p3);

            // Selekcija svakog partnera mora da sačeka da prethodna asinhrona zamena stavki
            // (ZameniPartneraUSlotuAsync) završi — inače dve konkurentne izmene _kupciStavke/
            // _dobavljaciStavke kolekcija mogu da se preklope.
            async Task SelectirajPartnera(ComboBox combo, int partnerId)
            {
                var poklapanje = _partneri.FirstOrDefault(p => p.PartnerId == partnerId);
                if (poklapanje == null) return;
                combo.SelectedItem = poklapanje;
                await ZameniPartneraUSlotuAsync(null, poklapanje);
                if (combo == CmbPartneri) _partner1 = poklapanje;
                else if (combo == CmbPartner2) _partner2 = poklapanje;
                else _partner3 = poklapanje;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri inicijalizaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbVrsta_SelectionChanged(object sender, SelectionChangedEventArgs e) => AzurirajVidljivostPartnera();

    private void AzurirajVidljivostPartnera()
    {
        if (CmbVrsta == null || PnlPartner2 == null || PnlPartner3 == null) return;

        int vrsta = CmbVrsta.SelectedIndex < 0 ? 0 : CmbVrsta.SelectedIndex;

        if (vrsta == 0) // Dvojna
        {
            LblPartner1.Text = "Partner / Ugovorna strana:";
            PnlPartner2.Visibility = Visibility.Collapsed;
            PnlPartner3.Visibility = Visibility.Collapsed;
            UkloniPartneraIzSlota(2);
            UkloniPartneraIzSlota(3);
        }
        else if (vrsta == 1) // Asignacija
        {
            LblPartner1.Text = "Asignant:";
            LblPartner2.Text = "Asignat:";
            LblPartner3.Text = "Asignatar:";
            PnlPartner2.Visibility = Visibility.Visible;
            PnlPartner3.Visibility = Visibility.Visible;
        }
        else // Cesija
        {
            LblPartner1.Text = "Cedent:";
            LblPartner2.Text = "Cesionar:";
            LblPartner3.Text = "Cesijat (dužnik):";
            PnlPartner2.Visibility = Visibility.Visible;
            PnlPartner3.Visibility = Visibility.Visible;
        }
    }

    private async void CmbPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPartneri.SelectedItem is not Partner partner) return;
        await ZameniPartneraUSlotuAsync(_partner1, partner);
        _partner1 = partner;
    }

    private async void CmbPartner2_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPartner2.SelectedItem is not Partner partner) return;
        await ZameniPartneraUSlotuAsync(_partner2, partner);
        _partner2 = partner;
    }

    private async void CmbPartner3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPartner3.SelectedItem is not Partner partner) return;
        await ZameniPartneraUSlotuAsync(_partner3, partner);
        _partner3 = partner;
    }

    private void UkloniPartneraIzSlota(int slot)
    {
        Partner? partner = slot switch { 2 => _partner2, 3 => _partner3, _ => null };
        if (partner == null) return;

        UkloniStavkePartnera(SlotKljuc(partner));
        if (slot == 2) { _partner2 = null; CmbPartner2.SelectedIndex = -1; }
        if (slot == 3) { _partner3 = null; CmbPartner3.SelectedIndex = -1; }
        RacunajIznos();
    }

    /// <summary>
    /// Zamenjuje stavke jednog "slota" (Partner1/2/3) u zajedničkim gridovima — uklanja stavke
    /// prethodnog partnera iz tog slota (ako postoji) i učitava otvorene stavke novog partnera,
    /// razdvojene na kupčevu (204) i dobavljačevu (435) stranu.
    /// </summary>
    private async Task ZameniPartneraUSlotuAsync(Partner? stariPartner, Partner noviPartner)
    {
        if (stariPartner != null)
        {
            UkloniStavkePartnera(SlotKljuc(stariPartner));
        }

        var otvorene = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(
            noviPartner.PartnerId, DpDatum.SelectedDate ?? DateTime.Today, samoOtvorene: true);

        string slotKljuc = SlotKljuc(noviPartner);

        var kupci = otvorene.Where(s => s.Konto.StartsWith("204"))
                            .Select(s => new OtvorenaStavkaZaKompenzaciju(s, noviPartner.PartnerId, noviPartner.Naziv, slotKljuc)).ToList();

        var dobavljaci = otvorene.Where(s => s.Konto.StartsWith("435"))
                                 .Select(s => new OtvorenaStavkaZaKompenzaciju(s, noviPartner.PartnerId, noviPartner.Naziv, slotKljuc)).ToList();

        foreach (var item in kupci)
        {
            item.PropertyChanged += Item_PropertyChanged;
            _kupciStavke.Add(item);
        }
        foreach (var item in dobavljaci)
        {
            item.PropertyChanged += Item_PropertyChanged;
            _dobavljaciStavke.Add(item);
        }

        RacunajIznos();
    }

    private void UkloniStavkePartnera(string slotKljuc)
    {
        foreach (var item in _kupciStavke.Where(i => i.SlotKljuc == slotKljuc).ToList())
        {
            item.PropertyChanged -= Item_PropertyChanged;
            _kupciStavke.Remove(item);
        }
        foreach (var item in _dobavljaciStavke.Where(i => i.SlotKljuc == slotKljuc).ToList())
        {
            item.PropertyChanged -= Item_PropertyChanged;
            _dobavljaciStavke.Remove(item);
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RacunajIznos();

    private void RacunajIznos()
    {
        decimal zbirKupci = _kupciStavke.Where(s => s.IsSelected).Sum(s => s.Preostalo);
        decimal zbirDobavljaci = _dobavljaciStavke.Where(s => s.IsSelected).Sum(s => s.Preostalo);

        decimal kompenzacija = Math.Min(zbirKupci, zbirDobavljaci);
        if (kompenzacija <= 0 && (zbirKupci > 0 || zbirDobavljaci > 0))
        {
            kompenzacija = Math.Max(zbirKupci, zbirDobavljaci);
        }

        TxtUkupanIznos.Text = $"{kompenzacija:N2} RSD";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (CmbPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int vrstaIndex = CmbVrsta.SelectedIndex < 0 ? 0 : CmbVrsta.SelectedIndex;
        var vrsta = (VrstaKompenzacije)vrstaIndex;

        if (vrsta != VrstaKompenzacije.Dvojna && CmbPartner2.SelectedItem is not Partner)
        {
            MessageBox.Show("Za asignaciju/cesiju izaberite bar drugo lice u poravnanju.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var izabraneKupci = _kupciStavke.Where(s => s.IsSelected).ToList();
        var izabraneDobavljaci = _dobavljaciStavke.Where(s => s.IsSelected).ToList();

        if (izabraneKupci.Count == 0 && izabraneDobavljaci.Count == 0)
        {
            MessageBox.Show("Izaberite bar jednu stavku za kompenzaciju.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kompenzacija.Vrsta = vrsta;
        _kompenzacija.PartnerId = partner.PartnerId;
        _kompenzacija.NazivPartnera = partner.Naziv;
        _kompenzacija.KontoPartnera1 = null;

        if (vrsta != VrstaKompenzacije.Dvojna && CmbPartner2.SelectedItem is Partner partner2)
        {
            _kompenzacija.Partner2Id = partner2.PartnerId;
            _kompenzacija.NazivPartnera2 = partner2.Naziv;
        }
        else
        {
            _kompenzacija.Partner2Id = null;
            _kompenzacija.NazivPartnera2 = null;
        }

        if (vrsta != VrstaKompenzacije.Dvojna && CmbPartner3.SelectedItem is Partner partner3)
        {
            _kompenzacija.Partner3Id = partner3.PartnerId;
            _kompenzacija.NazivPartnera3 = partner3.Naziv;
        }
        else
        {
            _kompenzacija.Partner3Id = null;
            _kompenzacija.NazivPartnera3 = null;
        }

        _kompenzacija.Datum = DpDatum.SelectedDate ?? DateTime.Today;
        _kompenzacija.Napomena = TxtNapomena.Text.Trim();

        _kompenzacija.Stavke.Clear();
        int rbr = 1;

        foreach (var k in izabraneKupci)
        {
            _kompenzacija.Stavke.Add(new KompenzacijaStavka
            {
                RedniBroj = rbr++,
                StavkaNalogaId = k.StavkaNalogaId,
                PartnerId = k.PartnerId,
                BrojDokumenta = k.BrojDokumenta,
                DatumDokumenta = k.Datum,
                Strana = "Duguje",
                BrojKonta = k.BrojKonta,
                IznosFakture = k.Preostalo,
                IznosPreostalo = k.Preostalo,
                IznosZaKompenzaciju = k.Preostalo
            });
        }

        foreach (var d in izabraneDobavljaci)
        {
            _kompenzacija.Stavke.Add(new KompenzacijaStavka
            {
                RedniBroj = rbr++,
                StavkaNalogaId = d.StavkaNalogaId,
                PartnerId = d.PartnerId,
                BrojDokumenta = d.BrojDokumenta,
                DatumDokumenta = d.Datum,
                Strana = "Potražuje",
                BrojKonta = d.BrojKonta,
                IznosFakture = d.Preostalo,
                IznosPreostalo = d.Preostalo,
                IznosZaKompenzaciju = d.Preostalo
            });
        }

        try
        {
            await _service.SacuvajKompenzacijuAsync(_kompenzacija);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju kompenzacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
