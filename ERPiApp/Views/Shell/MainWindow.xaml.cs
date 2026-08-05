using System.Windows;
using System.Windows.Input;
using ERPiApp.Views.Finansije.Nalozi;
using ERPiApp.Views.Firma;
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

    private void FirmaBorder_MouseDown(object sender, MouseButtonEventArgs e) => PromeniFirmu();

    private void BtnOdjava_Click(object sender, RoutedEventArgs e) => PromeniFirmu();

    private void PromeniFirmu()
    {
        AppSession.Ocisti();
        new CompanySelectWindow().Show();
        Close();
    }
}
