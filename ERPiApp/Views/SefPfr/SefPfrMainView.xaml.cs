using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.SefPfr;

public partial class SefPfrMainView : UserControl
{
    private readonly ErpiDbContext _db;
    private SefFaktureView? _vSef;
    private PfrRacuniView? _vPfr;

    public SefPfrMainView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        _vSef = new SefFaktureView(_db);
        _vPfr = new PfrRacuniView(_db);

        HostSefFakture.Content = _vSef;
        HostPfrRacuni.Content = _vPfr;
    }

    private void TabMainSefPfr_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            _vSef?.UcitajFakture();
            _vPfr?.UcitajRacune();
        }
    }
}
