using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.Magacin;

public partial class MagacinMainView : UserControl
{
    private readonly ErpiDbContext _db;
    private KalkulacijeView? _vKalkulacije;
    private ArtikliView? _vArtikli;
    private MagaciniView? _vMagacini;

    public MagacinMainView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        _vKalkulacije = new KalkulacijeView(_db);
        _vArtikli = new ArtikliView(_db);
        _vMagacini = new MagaciniView(_db);

        HostKalkulacije.Content = _vKalkulacije;
        HostArtikli.Content = _vArtikli;
        HostMagacini.Content = _vMagacini;
    }

    private void TabMainMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            _vKalkulacije?.UcitajKalkulacije();
            _vArtikli?.UcitajArtikle();
            _vMagacini?.UcitajMagacine();
        }
    }
}
