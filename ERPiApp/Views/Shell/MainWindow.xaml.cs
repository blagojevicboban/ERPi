using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERPiApp.Views.Finansije.Nalozi;
using ERPiApp.Views.Finansije.Partneri;
using ERPiApp.Views.Firma;
using ERPiApp.Views.Magacin;
using ERPiApp.Views.SefPfr;
using ERPiData;

namespace ERPiApp.Views.Shell;

public partial class MainWindow : Window
{
    private readonly ErpiDbContext _db;

    public MainWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        ErpiDbContext.EnsureDbSchemaUpdated(_db);

        WindowState = AppConfig.StartMaximized ? WindowState.Maximized : WindowState.Normal;

        TxtFirmaNaziv.Text = AppSession.TrenutnaFirma?.Naziv ?? "—";
        TxtFirmaSifra.Text = AppSession.TrenutnaFirma?.Sifra ?? "—";
        TxtImeKorisnika.Text = AppSession.TrenutniKorisnik?.ImeIPrezime ?? "—";
        TxtUlogaKorisnika.Text = AppSession.TrenutniKorisnik?.Uloga.ToString() ?? "—";

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.ToString(3)}  •  2026";

        MainContentHost.Content = new DashboardView(_db);

        Closed += (_, _) => _db.Dispose();
    }

    /// <summary>Poziva LoginWindow pre Show() ako je prijavljeni korisnik i dalje na podrazumevanoj lozinci.</summary>
    public void PrikaziUpozorenjeODefaultLozinci()
    {
        // Info traka se prikazuje samo ako je korisnik to uključio u podešavanjima
        if (AppConfig.PrikaziInfoTraku)
            PnlUpozorenjeLozinka.Visibility = Visibility.Visible;
    }

    /// <summary>Osvežava vidljivost info trake prema trenutnom podešavanju — poziva je PodesavanjaView.</summary>
    public void UpdateInfoTrakaVisibility()
    {
        PnlUpozorenjeLozinka.Visibility = AppConfig.PrikaziInfoTraku
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            NavPomoc_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.M && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            BtnToggleSidebar_Click(sender, e);
            e.Handled = true;
        }
    }

    public void NavPomoc_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "❓ Pomoć i Korisnička Uputstva";
        MainContentHost.Content = new ERPiApp.Views.Pomoc.PomocPage();
    }

    private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        if (SidebarColumn.Width.Value > 100)
        {
            SidebarColumn.Width = new GridLength(64);
            TxtBrandTitle.Visibility = Visibility.Collapsed;
            TxtBrandSubtitle.Visibility = Visibility.Collapsed;
            PnlFirmaDetails.Visibility = Visibility.Collapsed;
            PnlModulSwitcher.Visibility = Visibility.Collapsed;
            SetNavHeadersVisibility(Visibility.Collapsed);
        }
        else
        {
            SidebarColumn.Width = new GridLength(240);
            TxtBrandTitle.Visibility = Visibility.Visible;
            TxtBrandSubtitle.Visibility = Visibility.Visible;
            PnlFirmaDetails.Visibility = Visibility.Visible;
            PnlModulSwitcher.Visibility = Visibility.Visible;
            SetNavHeadersVisibility(Visibility.Visible);
        }
    }

    /// <summary>Sklanja/vraća sekcijske naslove i separatore u sve tri modulske nav-liste (Finansije/Zarade/Sredstva)
    /// kad se bočni meni sklopi na uzanu traku — nema smisla da širok tekst naslova (npr. "FINANSIJSKO
    /// KNJIGOVODSTVO") stoji u koloni od 64px.</summary>
    private void SetNavHeadersVisibility(Visibility vidljivost)
    {
        foreach (var panel in new[] { PnlNavFinansije, PnlNavZarade, PnlNavSredstva })
        {
            foreach (var child in panel.Children.OfType<UIElement>())
            {
                if (child is TextBlock or Separator)
                    child.Visibility = vidljivost;
            }
        }
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new DashboardView(_db);
    }

    public void NavNalozi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📖 Glavna knjiga i Nalozi";
        MainContentHost.Content = new NaloziView(_db);
    }

    private void NavPartneri_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👥 Partneri";
        MainContentHost.Content = new PartneriView(_db);
    }

    private void NavKonta_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 Kontni plan";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Konta.KontaView(_db);
    }

    private void NavMestaTroska_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🎯 Mesta troška i projekti";
        MainContentHost.Content = new ERPiApp.Views.Finansije.MestaTroska.MestaTroskaView(_db);
    }

    private void NavKarticaKonta_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💳 Kartice konta";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Izvestaji.KarticaKontaView(_db);
    }

    public void NavBrutoBilans_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Bruto bilans";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Izvestaji.BrutoBilansView(_db);
    }

    private void NavBilansStanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏛️ Bilans Stanja";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Bilansi.BilansStanjaView(_db);
    }

    private void NavBilansUspeha_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📈 Bilans Uspeha";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Bilansi.BilansUspehaView(_db);
    }

    private void NavIzvodi_Click(object sender, RoutedEventArgs e)
    {
        var win = new ERPiApp.Views.Finansije.Izvodi.UvozIzvodaWindow(_db) { Owner = this };
        win.ShowDialog();
    }

    private void NavBlagajna_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💰 Dinarska i Devizna Blagajna";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Blagajna.BlagajnaView(_db);
    }

    private void NavPutniNalozi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🚗 Službena putovanja i Putni nalozi";
        MainContentHost.Content = new ERPiApp.Views.Finansije.PutniNalozi.PutniNaloziView(_db);
    }

    private void NavKompenzacije_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🤝 Dvojne i Trojne Kompenzacije";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Kompenzacije.KompenzacijeView(_db);
    }

    private void NavDevizno_Click(object sender, RoutedEventArgs e)
    {
        var win = new ERPiApp.Views.Finansije.Devizno.DeviznoValviranjeWindow(_db) { Owner = this };
        win.ShowDialog();
    }

    private void NavRobnoDashboard_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Radna tabla — Robno knjigovodstvo";
        MainContentHost.Content = new ERPiApp.Views.Magacin.RobnoDashboardView(_db);
    }

    private void NavMagacin_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📦 Robno — Kalkulacije, Magacini, Artikli";
        MainContentHost.Content = new MagacinMainView(_db);
    }

    private void NavRacuniOtpremnice_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🧾 Računi - Otpremnice";
        MainContentHost.Content = new ERPiApp.Views.Magacin.RacuniOtpremniceView(_db);
    }

    private void NavPdvEvidencija_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🧾 PDV Evidencija (KIR / KPR / PP-PDV)";
        MainContentHost.Content = new ERPiApp.Views.Finansije.PdvEvidencijaView(_db);
    }

    private void NavMaterijalno_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Radna tabla — Materijalno knjigovodstvo";
        MainContentHost.Content = new ERPiApp.Views.Magacin.MaterijalnoDashboardView(_db);
    }

    private void NavMaterijalnoSkladiste_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏭 Skladište i Zalihe — Materijalno knjigovodstvo";
        MainContentHost.Content = new ERPiApp.Views.Magacin.MaterijalnoSkladisteView(_db);
    }

    private void NavSefPfr_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 SEF e-Fakture i e-Fiskalizacija (PFR)";
        MainContentHost.Content = new SefPfrMainView(_db);
    }

    public void NavUvoz_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Uvoz podataka iz ERPiFinansije";
        MainContentHost.Content = new ERPiApp.Views.Podesavanja.UvozWizardView(_db);
    }

    public void NavIzvestaji_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Izveštaji i Bruto Bilans";
        MainContentHost.Content = new ERPiApp.Views.Finansije.Izvestaji.BrutoBilansView(_db);
    }

    public void NavPodesavanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🔧 Podešavanja aplikacije";
        MainContentHost.Content = new PodesavanjaView(_db);
    }

    private void FirmaBorder_MouseDown(object sender, MouseButtonEventArgs e) => PromeniFirmu();

    private void BtnOdjava_Click(object sender, RoutedEventArgs e) => PromeniFirmu();

    private void PromeniFirmu()
    {
        AppSession.Ocisti();
        new CompanySelectWindow().Show();
        Close();
    }

    // ── Preklopnik modula ─────────────────────────────────────────────

    private void TabModulFinansije_Click(object sender, RoutedEventArgs e)
    {
        PnlNavFinansije.Visibility = Visibility.Visible;
        PnlNavZarade.Visibility = Visibility.Collapsed;
        PnlNavSredstva.Visibility = Visibility.Collapsed;
        PostaviBojuSidebara((Color)FindResource("SidebarStartColor"), (Color)FindResource("SidebarEndColor"), new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)));
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new DashboardView(_db);
    }

    private void TabModulZarade_Click(object sender, RoutedEventArgs e)
    {
        PnlNavFinansije.Visibility = Visibility.Collapsed;
        PnlNavZarade.Visibility = Visibility.Visible;
        PnlNavSredstva.Visibility = Visibility.Collapsed;
        PostaviBojuSidebara((Color)FindResource("ZaradeSidebarStartColor"), (Color)FindResource("ZaradeSidebarEndColor"), new SolidColorBrush(Color.FromRgb(0x90, 0xCA, 0xF9)));
        TxtHeaderTitle.Text = "📁 Obračunski periodi zarada";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Obracuni.ObracuniPage();
    }

    private void TabModulSredstva_Click(object sender, RoutedEventArgs e)
    {
        PnlNavFinansije.Visibility = Visibility.Collapsed;
        PnlNavZarade.Visibility = Visibility.Collapsed;
        PnlNavSredstva.Visibility = Visibility.Visible;
        PostaviBojuSidebara((Color)FindResource("SredstvaSidebarStartColor"), (Color)FindResource("SredstvaSidebarEndColor"), new SolidColorBrush(Color.FromRgb(0x95, 0xD5, 0xB2)));
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Dashboard.SredstvaDashboardPage(_db);
    }

    /// <summary>
    /// Menja gradijent pozadine sidebar-a i akcentne boje kartica prema aktivnom modulu — svaki modul zadržava
    /// prepoznatljivu boju svog izvornog samostalnog app-a (Finansije = plavo/teget,
    /// Zarade = ljubičasto, Sredstva = zeleno).
    /// </summary>
    private void PostaviBojuSidebara(Color start, Color end, SolidColorBrush accentBrush)
    {
        GradStopSidebar1.Color = start;
        GradStopSidebar2.Color = end;
        TxtBrandSubtitle.Foreground = accentBrush;
        TxtFirmaSifra.Foreground = accentBrush;
        TxtUlogaKorisnika.Foreground = accentBrush;
        VersionText.Foreground = accentBrush;
        BtnOdjava.Foreground = accentBrush;
        BtnOdjava.BorderBrush = accentBrush;
    }

    // ── Navigacija Zarade ─────────────────────────────────────────────

    private void NavZaradeObracuni_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📁 Obračunski periodi zarada";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Obracuni.ObracuniPage();
    }

    private void NavZaradeRadnici_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👤 Radnici i ugovori o radu";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Radnici.RadniciPage();
    }

    private void NavZaradeRadniSati_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⏱️ Radni sati";
        MainContentHost.Content = new ERPiApp.Views.Zarade.RadniSati.RadniSatiPage();
    }

    private void NavZaradePrimanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🎁 Ostala primanja";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Primanja.PrimanjaPage();
    }

    private void NavZaradePorezi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚖️ Poreske stope i parametri";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Porezi.PoreziPage();
    }

    private void NavZaradeDoprinosi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📈 Stope doprinosa";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Doprinosi.DoprinosiPage();
    }

    private void NavZaradePlatniRazredi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Platni razredi";
        MainContentHost.Content = new ERPiApp.Views.Zarade.PlatniRazredi.PlatniRazrediPage();
    }

    private void NavZaradeIsplate_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💸 Isplate zarada u mesecu";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Isplate.IsplatePage();
    }

    private void NavZaradeObracunPlate_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Mesečni obračun plate";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Obracun.ObracunPage();
    }

    private void NavZaradeListici_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🧾 Platni listići (slanje e-mailom)";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Listici.ListiciPage();
    }

    private void NavZaradeBolovanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏥 Bolovanja i RFZO refundacije";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Bolovanja.BolovanjaPage();
    }

    private void NavZaradePppPd_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 PPP-PD prijava za zarade";
        MainContentHost.Content = new ERPiApp.Views.Zarade.PppPd.PppPdPage();
    }

    private void NavZaradeNalozi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏦 Nalozi za prenos (Halcom / ePP)";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Nalozi.NaloziPage();
    }

    private void NavZaradeKnjizenje_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📒 Nalog za knjiženje zarada";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Knjizenje.KnjizenjePage();
    }

    private void NavZaradePrimaoci_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👤 Primaoci po ugovoru";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Ugovori.PrimaociPage();
    }

    private void NavZaradeIsplateNaknada_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💸 Isplate naknada van radnog odnosa";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Isplate.IsplatePage();
    }

    private void NavZaradeUgovori_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📝 Ugovori i honorari";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Ugovori.UgovoriPage();
    }

    private void NavZaradeVrsteUgovora_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 Vrste ugovora van radnog odnosa";
        MainContentHost.Content = new ERPiApp.Views.Zarade.VrsteUgovora.VrsteUgovoraPage();
    }

    private void NavZaradeSabloniUgovora_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🖋️ Šabloni ugovora";
        MainContentHost.Content = new ERPiApp.Views.Zarade.SabloniUgovora.SabloniUgovoraPage();
    }

    private void NavZaradePppPdNaknade_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 PPP-PD prijava za naknade";
        MainContentHost.Content = new ERPiApp.Views.Zarade.PppPd.PppPdPage();
    }

    private void NavZaradeStampe_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📑 Izveštaji i rekapitulacije";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Stampe.StampePage();
    }

    private void NavZaradePppPo_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🧾 PPP-PO godišnje potvrde";
        MainContentHost.Content = new ERPiApp.Views.Zarade.PppPo.PppPoPage();
    }

    private void NavZaradeKrediti_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💳 Krediti i obustave";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Krediti.KreditiPage();
    }

    private void NavZaradeBanke_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏦 Šifarnik banaka";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Banke.BankePage();
    }

    private void NavZaradePraznici_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📅 Kalendar državnih praznika";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Praznici.PrazniciPage();
    }

    private void NavZaradeVrstePrimanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💰 Šifarnik vrsta primanja";
        MainContentHost.Content = new ERPiApp.Views.Zarade.VrstePrimanja.VrstePrimanjaPage();
    }

    private void NavZaradeOlaksice_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏷️ Poreske olakšice";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Olaksice.OlaksicePage();
    }

    private void NavZaradeKontaKnjizenja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📗 Konta za knjiženje zarada";
        MainContentHost.Content = new ERPiApp.Views.Zarade.KontaKnjizenja.KontaKnjizenjaPage();
    }

    private void NavZaradePodesavanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Podešavanja — Zarade";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Podesavanja.PodesavanjaZaradeView(_db);
    }

    /// <summary>Osvežava prikaz aktivnog perioda (godina/mesec) u zaglavlju.</summary>
    public void OsveziAktivniPeriodPrikaz() { /* placeholder for Zarade views */ }

    /// <summary>Otvara stranicu Porezi — poziva je NoviObracunWindow kad nema podataka.</summary>
    public void OtvoriPorezi()
    {
        TxtHeaderTitle.Text = "🏦 Porezi";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Porezi.PoreziPage();
    }

    /// <summary>Navigates to the Obracun page — called by ObracuniPage.</summary>
    public void NavigateToObracun(int godina, int mesec)
    {
        AppConfig.ActiveGodina = godina;
        AppConfig.ActiveMesec = mesec;
        TxtHeaderTitle.Text = $"📋 Obračun zarada {mesec}/{godina}";
        MainContentHost.Content = new ERPiApp.Views.Zarade.Obracun.ObracunPage();
    }

    // ── Navigacija Sredstva ───────────────────────────────────────────

    private void NavSredstvaDashboard_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Dashboard.SredstvaDashboardPage(_db);
    }

    private void NavSredstvaRegistar_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏛️ Registar osnovnih sredstava";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Sredstva.SredstvaPage(_db);
    }

    private void NavSredstvaKartice_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 Analitičke kartice sredstava";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Kartice.KarticePage(_db);
    }

    private void NavSredstvaPrijave_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📥 Prijava sredstava";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Prijave.PrijavaPage(_db);
    }

    private void NavSredstvaRashod_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📤 Rashod i promene";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Rashod.RashodPage(_db);
    }

    private void NavSredstvaAmortizacija_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📈 Obračun amortizacije";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Amortizacija.AmortizacijaPage(_db);
    }

    private void NavSredstvaPopis_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🗂️ Popis sredstava";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Popis.PopisPage(_db);
    }

    private void NavSredstvaRevalorizacija_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💹 Revalorizacija";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Revalorizacija.RevalorizacijaPage(_db);
    }

    private void NavSredstvaIzvestaji_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Izveštaji — Osnovna sredstva";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Izvestaji.IzvestajiPage(_db);
    }

    private void NavSredstvaPodesavanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Podešavanja — Osnovna sredstva";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Podesavanja.PodesavanjaSredstvaView(_db);
    }

    /// <summary>Otvara analitičku karticu izabranog sredstva — poziva je SredstvaPage (dupli klik / dugme "Kartica").</summary>
    public void NavigateToSredstvaKartica(int sredstvoId)
    {
        TxtHeaderTitle.Text = "📋 Analitičke kartice sredstava";
        MainContentHost.Content = new ERPiApp.Views.Sredstva.Kartice.KarticePage(_db, sredstvoId);
    }
}
