using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.Sredstva.Dashboard;

public partial class SredstvaDashboardPage : Page
{
    private readonly SredstvaDashboardViewModel _viewModel;

    public SredstvaDashboardPage(ErpiDbContext db)
    {
        InitializeComponent();
        _viewModel = new SredstvaDashboardViewModel(db);
        DataContext = _viewModel;
    }
}
