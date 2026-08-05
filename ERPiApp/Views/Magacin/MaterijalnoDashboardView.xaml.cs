using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ERPiData;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public class UlazRedDto
{
    public int UlazNalogId { get; set; }
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string SifraMagacina { get; set; } = "";
    public string StatusText { get; set; } = "";
}

public class TrebovanjeRedDto
{
    public int TrebovanjeNalogId { get; set; }
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string SifraMagacina { get; set; } = "";
    public string StatusText { get; set; } = "";
}

public class TopMaterijalRedDto
{
    public string SifraArtikla { get; set; } = "";
    public string NazivArtikla { get; set; } = "";
    public decimal VrednostZaliha { get; set; }
    public decimal Promet { get; set; }
}

/// <summary>"U pripremi" (nije proknjiženo) → Visible; sve ostalo → Collapsed. Skriva dugme "Knjiži" za već proknjižene naloge.</summary>
public class StatusUKnjizenjuVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) == "U pripremi" ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Port iz ERPiFinansijeApp — radna tabla Materijalnog knjigovodstva (Ulazi/Trebovanja). Razlika:
/// deli već otvoren <see cref="ErpiDbContext"/> (konstruktor, ne AppConfig.DbPath), i "Knjiži"
/// akcija je ovde direktno na redu u dashboard-u (ERPiFinansije je ima na posebnom tabu u
/// MagacinView, koji ovde nije portovan — vidi PLAN_NASTAVKA.md §3g).
/// </summary>
public partial class MaterijalnoDashboardView : UserControl
{
    private readonly ErpiDbContext _db;

    public MaterijalnoDashboardView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        // Loaded, ne direktan poziv iz konstruktora — isti razlog kao KarticaKontaView (vidi
        // njenu opsežnu napomenu): ako se await unutra LoadData ikad zavrsi sinhrono, DataGrid
        // operacije bi se odigrale pre nego sto je control dodat u vizuelno stablo.
        Loaded += (_, _) => LoadData();
    }

    public async void LoadData()
    {
        try
        {
            // ===== VREDNOST ZALIHA MATERIJALA =====
            var bilansRedovi = await RobniBrutoBilansService.GetMaterijalniBrutoBilansAsync(_db);

            decimal vrednostUkupno = bilansRedovi.Sum(r => r.SaldoVrednosni);
            int brojMaterijala = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Sum(r => r.SaldoKolicinski) != 0);
            int negativnaStanja = bilansRedovi.Count(r => r.SaldoKolicinski < 0 || r.Cena < 0);

            TxtVrednostZaliha.Text = $"{vrednostUkupno:N2} RSD";
            TxtBrojMaterijala.Text = $"{brojMaterijala}";
            TxtNegativnaStanja.Text = $"{negativnaStanja}";

            // ===== TOP MATERIJALI PO PROMETU / VREDNOSTI =====
            var topMaterijali = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TopMaterijalRedDto
                {
                    SifraArtikla = g.Key,
                    NazivArtikla = g.First().NazivArtikla,
                    VrednostZaliha = g.Sum(r => r.SaldoVrednosni),
                    Promet = g.Sum(r => r.UlazVrednost + r.IzlazVrednost)
                })
                .OrderByDescending(x => x.VrednostZaliha)
                .Take(10)
                .ToList();
            DgTopMaterijali.ItemsSource = topMaterijali;

            // ===== POSLEDNJI ULAZI =====
            var ulazi = await new UlazService(_db).GetUlaziAsync();
            DgPoslednjiUlazi.ItemsSource = ulazi
                .OrderByDescending(u => u.Datum)
                .Take(8)
                .Select(u => new UlazRedDto
                {
                    UlazNalogId = u.UlazNalogId,
                    BrojNaloga = u.BrojNaloga,
                    Datum = u.Datum,
                    SifraMagacina = u.Magacin?.SifraMagacina ?? "",
                    StatusText = u.IsKnjizen ? "Proknjižen" : "U pripremi"
                })
                .ToList();

            // ===== POSLEDNJA TREBOVANJA =====
            var trebovanja = await new TrebovanjeService(_db).GetTrebovanjaAsync();
            DgPoslednjaTrebovanja.ItemsSource = trebovanja
                .OrderByDescending(t => t.Datum)
                .Take(8)
                .Select(t => new TrebovanjeRedDto
                {
                    TrebovanjeNalogId = t.TrebovanjeNalogId,
                    BrojNaloga = t.BrojNaloga,
                    Datum = t.Datum,
                    SifraMagacina = t.Magacin?.SifraMagacina ?? "",
                    StatusText = t.IsKnjizen ? "Proknjiženo" : "U pripremi"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju radne table Materijalno: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== BRZE AKCIJE =====
    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovoTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new TrebovanjeEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private async void BtnOtvoriUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UlazRedDto red) return;

        try
        {
            var puna = await _db.UlazNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.UlazNalogId == red.UlazNalogId);
            if (puna == null) return;

            var dijalog = new UlazEditWindow(_db, puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOtvoriTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TrebovanjeRedDto red) return;

        try
        {
            var puna = await _db.TrebovanjeNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.TrebovanjeNalogId == red.TrebovanjeNalogId);
            if (puna == null) return;

            var dijalog = new TrebovanjeEditWindow(_db, puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UlazRedDto red) return;

        if (MessageBox.Show($"Knjiži ulaz br. {red.BrojNaloga}? Posle knjiženja nisu dozvoljene izmene.",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            await new UlazService(_db).KnjiziUlazAsync(red.UlazNalogId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TrebovanjeRedDto red) return;

        if (MessageBox.Show($"Knjiži trebovanje br. {red.BrojNaloga}? Posle knjiženja nisu dozvoljene izmene.",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            await new TrebovanjeService(_db).KnjiziTrebovanjeAsync(red.TrebovanjeNalogId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
