using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

public partial class MagacinMainView : UserControl
{
    private readonly ErpiDbContext _db;
    private PonudeView? _vPonude;
    private NarudzbeniceView? _vNarudzbenice;
    private KalkulacijeView? _vKalkulacije;
    private NivelacijeView? _vNivelacije;
    private MaloprodajneKalkulacijeView? _vMaloprodaja;
    private UvozneKalkulacijeView? _vUvoz;
    private ArtikliView? _vArtikli;
    private MagaciniView? _vMagacini;
    private PoreskeTarifeView? _vPoreskeTarife;
    private RobnoKretanjaView? _vPrimopredaje;
    private RobnoKretanjaView? _vZaduzenja;
    private RobnoKretanjaView? _vRazduzenja;
    private RobneKarticeView? _vRobneKartice;
    private RobniBrutoBilansView? _vRobniBrutoBilans;

    public MagacinMainView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        _vPonude = new PonudeView(_db);
        _vNarudzbenice = new NarudzbeniceView(_db);
        _vKalkulacije = new KalkulacijeView(_db);
        _vNivelacije = new NivelacijeView(_db);
        _vMaloprodaja = new MaloprodajneKalkulacijeView(_db);
        _vUvoz = new UvozneKalkulacijeView(_db);
        _vArtikli = new ArtikliView(_db);
        _vMagacini = new MagaciniView(_db);
        _vPoreskeTarife = new PoreskeTarifeView(_db);
        _vPrimopredaje = new RobnoKretanjaView(_db, VrstaRobnogKretanja.Primopredaja);
        _vZaduzenja = new RobnoKretanjaView(_db, VrstaRobnogKretanja.Zaduzenje);
        _vRazduzenja = new RobnoKretanjaView(_db, VrstaRobnogKretanja.Razduzenje);
        _vRobneKartice = new RobneKarticeView(_db);
        _vRobniBrutoBilans = new RobniBrutoBilansView(_db);

        HostPonude.Content = _vPonude;
        HostNarudzbenice.Content = _vNarudzbenice;
        HostKalkulacije.Content = _vKalkulacije;
        HostNivelacije.Content = _vNivelacije;
        HostMaloprodaja.Content = _vMaloprodaja;
        HostUvoz.Content = _vUvoz;
        HostArtikli.Content = _vArtikli;
        HostMagacini.Content = _vMagacini;
        HostPoreskeTarife.Content = _vPoreskeTarife;
        HostPrimopredaje.Content = _vPrimopredaje;
        HostZaduzenja.Content = _vZaduzenja;
        HostRazduzenja.Content = _vRazduzenja;
        HostRobneKartice.Content = _vRobneKartice;
        HostRobniBrutoBilans.Content = _vRobniBrutoBilans;
    }

    private void TabMainMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            _vPonude?.UcitajPonude();
            _vNarudzbenice?.UcitajNarudzbenice();
            _vKalkulacije?.UcitajKalkulacije();
            _vNivelacije?.UcitajPodatke();
            _vMaloprodaja?.UcitajPodatke();
            _vUvoz?.UcitajPodatke();
            _vArtikli?.UcitajArtikle();
            _vMagacini?.UcitajMagacine();
            _vPoreskeTarife?.UcitajTarife();
            _vPrimopredaje?.UcitajPodatke();
            _vZaduzenja?.UcitajPodatke();
            _vRazduzenja?.UcitajPodatke();
            _vRobniBrutoBilans?.UcitajPodatke();
        }
    }
}
