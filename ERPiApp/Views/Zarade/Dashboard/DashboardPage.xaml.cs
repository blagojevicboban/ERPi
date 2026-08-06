using System.Windows.Controls;

namespace ERPiApp.Views.Zarade.Dashboard;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
