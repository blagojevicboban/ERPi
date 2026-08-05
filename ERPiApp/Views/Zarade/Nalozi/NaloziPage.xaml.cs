using ERPiApp.Services.Zarade;
using ERPiData.Seeds.Zarade;
using System.Windows.Controls;
using ERPiData.Models.Zarade;

namespace ERPiApp.Views.Zarade.Nalozi;

public partial class NaloziPage : Page
{
    /// <param name="rod">
    /// Rod isplata za koje se pripremaju nalozi. Zarada i naknada van radnog odnosa idu
    /// zasebnim prijavama, pa i zasebnim paketima naloga — svaki sa svojim BOP-om.
    /// </param>
    public NaloziPage(RodIsplate rod = RodIsplate.Zarada)
    {
        InitializeComponent();
        DataContext = new NaloziViewModel(rod);
    }
}
