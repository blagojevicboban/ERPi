using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.Shell;

public partial class DashboardView : UserControl
{
    public DashboardView(ErpiDbContext db)
    {
        InitializeComponent();

        ItemsStatistika.ItemsSource = new[]
        {
            new StatKartica("Firme", db.Firme.Count()),
            new StatKartica("Korisnici", db.Korisnici.Count()),
            new StatKartica("Partneri", db.Partneri.Count()),
            new StatKartica("Konta", db.Konta.Count()),
            new StatKartica("Mesta troška", db.MestaTroska.Count())
        };
    }

    private record StatKartica(string Naslov, int Vrednost);
}
