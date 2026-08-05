using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.Magacin;

public partial class MagacinMainView : UserControl
{
    private readonly ErpiDbContext _db;
    private KalkulacijeView? _vKalkulacije;
    private NivelacijeView? _vNivelacije;
    private MaloprodajneKalkulacijeView? _vMaloprodaja;
    private UvozneKalkulacijeView? _vUvoz;
    private ArtikliView? _vArtikli;
    private MagaciniView? _vMagacini;

    public MagacinMainView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        _vKalkulacije = new KalkulacijeView(_db);
        _vNivelacije = new NivelacijeView(_db);
        _vMaloprodaja = new MaloprodajneKalkulacijeView(_db);
        _vUvoz = new UvozneKalkulacijeView(_db);
        _vArtikli = new ArtikliView(_db);
        _vMagacini = new MagaciniView(_db);

        HostKalkulacije.Content = _vKalkulacije;
        HostNivelacije.Content = _vNivelacije;
        HostMaloprodaja.Content = _vMaloprodaja;
        HostUvoz.Content = _vUvoz;
        HostArtikli.Content = _vArtikli;
        HostMagacini.Content = _vMagacini;
    }

    private void TabMainMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            _vKalkulacije?.UcitajKalkulacije();
            _vNivelacije?.UcitajPodatke();
            _vMaloprodaja?.UcitajPodatke();
            _vUvoz?.UcitajPodatke();
            _vArtikli?.UcitajArtikle();
            _vMagacini?.UcitajMagacine();
        }
    }
}
