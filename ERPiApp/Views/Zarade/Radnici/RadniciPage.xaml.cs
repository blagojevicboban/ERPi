using ERPiApp.Services.Zarade;
using ERPiData.Seeds.Zarade;
using System.Windows;
using System.Windows.Controls;

namespace ERPiApp.Views.Zarade.Radnici;

public partial class RadniciPage : Page
{
    public RadniciPage()
    {
        InitializeComponent();
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RadniciViewModel vm)
        {
            vm.IsEditing = true;
            vm.StatusPoruka = "Izmena podataka radnika...";
        }
    }
}
