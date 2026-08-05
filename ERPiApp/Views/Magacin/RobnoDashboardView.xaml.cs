using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public class KalkulacijaRedDto
{
    public string Tip { get; set; } = "";
    public int BrojKalkulacije { get; set; }
    public DateTime Datum { get; set; }
    public string StatusText { get; set; } = "";
}

public class NivelacijaRedDto
{
    public int BrojNivelacije { get; set; }
    public DateTime DatumNivelacije { get; set; }
    public string NazivMagacina { get; set; } = "";
    public decimal UkupnoRazlika { get; set; }
}

public class TopArtikalRedDto
{
    public string SifraArtikla { get; set; } = "";
    public string NazivArtikla { get; set; } = "";
    public decimal VrednostZaliha { get; set; }
    public decimal Promet { get; set; }
}

/// <summary>
/// Radna tabla Robnog knjigovodstva (Kalkulacije VP/Nivelacije/Otpremnice/Primopredaje) —
/// port iz ERPiFinansijeApp/Views/Trgovina/RobnoDashboardView, prilagođen ERPi obrascu (deli
/// već otvoren <see cref="ErpiDbContext"/>, isti stil kao <see cref="MaterijalnoDashboardView"/>
/// za Materijalno). NE meša Robno/Materijalno — vrednost zaliha ovde dolazi iz
/// <see cref="RobniBrutoBilansService.GetRobniBrutoBilansAsync"/> (Artikal-bazirano), ne
/// <c>GetMaterijalniBrutoBilansAsync</c>.
/// Razlika od izvora: MP kalkulacije ovde nemaju svoj create-dijalog još (samo knjiži/rasknjiži
/// u <see cref="MaloprodajneKalkulacijeView"/>, vidi PLAN_NASTAVKA.md), pa brza akcija
/// "Nova kalkulacija (MP)" iz izvora namerno nije dodata dok taj ekran ne dobije editor.
/// </summary>
public partial class RobnoDashboardView : UserControl
{
    private readonly ErpiDbContext _db;

    public RobnoDashboardView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        // Loaded, ne direktan poziv iz konstruktora — isti razlog kao MaterijalnoDashboardView
        // (vidi njenu napomenu / PLAN_NASTAVKA.md §2).
        Loaded += (_, _) => LoadData();
    }

    public async void LoadData()
    {
        try
        {
            var magaciniMap = await _db.Magacini.AsNoTracking()
                .ToDictionaryAsync(m => m.SifraMagacina, m => m.VrstaMagacina ?? "Veleprodaja", StringComparer.OrdinalIgnoreCase);

            var bilansService = new RobniBrutoBilansService(_db);
            var bilansRedovi = await bilansService.GetRobniBrutoBilansAsync();

            decimal vrednostVp = bilansRedovi
                .Where(r => (magaciniMap.TryGetValue(r.SifraMagacina, out var v) ? v : "Veleprodaja") != "Maloprodaja")
                .Sum(r => r.SaldoVrednosni);
            decimal vrednostMp = bilansRedovi
                .Where(r => (magaciniMap.TryGetValue(r.SifraMagacina, out var v) ? v : "Veleprodaja") == "Maloprodaja")
                .Sum(r => r.SaldoVrednosni);
            decimal vrednostUkupno = vrednostVp + vrednostMp;

            int brojArtikala = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Sum(r => r.SaldoKolicinski) != 0);

            TxtVrednostZalihaVp.Text = $"{vrednostVp:N2} RSD";
            TxtVrednostZalihaMp.Text = $"{vrednostMp:N2} RSD";
            TxtVrednostZalihaUkupno.Text = $"{vrednostUkupno:N2} RSD";
            TxtBrojArtikala.Text = $"{brojArtikala} artikala na zalihi";

            // ===== TOP ARTIKLI PO PROMETU / VREDNOSTI =====
            var topArtikli = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TopArtikalRedDto
                {
                    SifraArtikla = g.Key,
                    NazivArtikla = g.First().NazivArtikla,
                    VrednostZaliha = g.Sum(r => r.SaldoVrednosni),
                    Promet = g.Sum(r => r.UlazVrednost + r.IzlazVrednost)
                })
                .OrderByDescending(x => x.VrednostZaliha)
                .Take(10)
                .ToList();
            DgTopArtikli.ItemsSource = topArtikli;

            // ===== POSLEDNJE KALKULACIJE (VP) =====
            var kalkulacijeVp = await _db.Kalkulacije.AsNoTracking()
                .OrderByDescending(k => k.Datum)
                .Take(8)
                .Select(k => new KalkulacijaRedDto
                {
                    Tip = "VP",
                    BrojKalkulacije = k.BrojKalkulacije,
                    Datum = k.Datum,
                    StatusText = k.VrstaKalkulacije
                })
                .ToListAsync();
            DgPoslednjeKalkulacije.ItemsSource = kalkulacijeVp;

            // ===== POSLEDNJE NIVELACIJE =====
            var nivelacije = await _db.NivelacijeCena.AsNoTracking()
                .Include(n => n.Magacin)
                .OrderByDescending(n => n.DatumNivelacije)
                .Take(8)
                .Select(n => new NivelacijaRedDto
                {
                    BrojNivelacije = n.BrojNivelacije,
                    DatumNivelacije = n.DatumNivelacije,
                    NazivMagacina = n.Magacin != null ? n.Magacin.NazivMagacina : "",
                    UkupnoRazlika = n.UkupnoRazlika
                })
                .ToListAsync();
            DgPoslednjeNivelacije.ItemsSource = nivelacije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju radne table Robno: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== BRZE AKCIJE =====
    private void BtnNovaKalkulacijaVp_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KalkulacijaEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaNivelacija_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new NivelacijaEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaOtpremnica_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new RacunOtpremnicaEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new RobnoKretanjeEditWindow(_db, VrstaRobnogKretanja.Primopredaja) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }
}
