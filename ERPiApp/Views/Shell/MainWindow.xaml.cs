using System.Windows;
using System.Windows.Input;
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
        PnlUpozorenjeLozinka.Visibility = Visibility.Visible;
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new DashboardView(_db);
    }

    private void NavNalozi_Click(object sender, RoutedEventArgs e)
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

    private void NavBrutoBilans_Click(object sender, RoutedEventArgs e)
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

    private void NavMagacin_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📦 Magacin i PDV evidencija";
        MainContentHost.Content = new MagacinMainView(_db);
    }

    private void NavSefPfr_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 SEF e-Fakture i e-Fiskalizacija (PFR)";
        MainContentHost.Content = new SefPfrMainView(_db);
    }

    private void NavUvoz_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Uvoz podataka iz ERPiFinansije";
        MainContentHost.Content = new ERPiApp.Views.Podesavanja.UvozWizardView(_db);
    }

    private void FirmaBorder_MouseDown(object sender, MouseButtonEventArgs e) => PromeniFirmu();

    private void BtnOdjava_Click(object sender, RoutedEventArgs e) => PromeniFirmu();

    private void PromeniFirmu()
    {
        AppSession.Ocisti();
        new CompanySelectWindow().Show();
        Close();
    }
}
