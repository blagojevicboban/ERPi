using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ERPiData;

namespace ERPiApp.Views.Sredstva.Prijave;

public class PrijavaRedViewModel
{
    public int BrojNaloga { get; init; }
    public DateTime DatumAktiviranja { get; init; }
    public int BrojStavki { get; init; }
    public decimal UkupnaNabavnaVrednost { get; init; }
    public bool Knjizen { get; init; }
    public string KnjizenTekst => Knjizen ? "✔️ Da" : "❌ Ne";
    public string PartnerNaziv { get; init; } = string.Empty;
}

public partial class PrijavaPage : Page
{
    private readonly ErpiDbContext _db;
    private List<PrijavaRedViewModel> _all = new();

    public PrijavaPage(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += PrijavaPage_Loaded;
    }

    private void PrijavaPage_Loaded(object sender, RoutedEventArgs e)
    {
        var prijave = _db.SredstvaPrijave
            .Include(p => p.Partner)
            .OrderBy(p => p.BrojNaloga)
            .ThenBy(p => p.RedBroj)
            .ToList();

        _all = prijave
            .GroupBy(p => p.BrojNaloga)
            .Select(g =>
            {
                var first = g.First();
                return new PrijavaRedViewModel
                {
                    BrojNaloga = g.Key,
                    DatumAktiviranja = first.DatumAktiviranja,
                    BrojStavki = g.Count(),
                    UkupnaNabavnaVrednost = g.Sum(x => x.NabavnaVrednost),
                    Knjizen = first.Knjizen,
                    PartnerNaziv = first.Partner?.Naziv ?? "Nepoznat dobavljač"
                };
            })
            .ToList();

        PrijavaGrid.ItemsSource = _all;

        var proknjizeno = _all.Count(p => p.Knjizen);
        StatUkupno.Text = _all.Count.ToString();
        StatStavki.Text = prijave.Count.ToString();
        StatUkupnaVrednost.Text = prijave.Sum(p => p.NabavnaVrednost).ToString("N2");
        StatKnjizeno.Text = proknjizeno.ToString();
        StatCekanje.Text = (_all.Count - proknjizeno).ToString();

        SubtitleText.Text = $"Ukupno {_all.Count} naloga  •  {prijave.Count} stavki  •  Proknjiženo: {proknjizeno}";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            PrijavaGrid.ItemsSource = _all;
        }
        else
        {
            PrijavaGrid.ItemsSource = _all.Where(p =>
                p.BrojNaloga.ToString().Contains(q) ||
                p.PartnerNaziv.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    private void PrijavaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
        {
            StatusText.Text = $"Nalog #{p.BrojNaloga}  •  Dupli klik za pregled naloga";
            UcitajStavke(p.BrojNaloga);
        }
        else
        {
            StavkeGrid.ItemsSource = null;
            TxtDetaljiNaslov.Text = "Izaberite nalog";
        }
    }

    private void UcitajStavke(int brojNaloga)
    {
        TxtDetaljiNaslov.Text = $"Stavke (Nalog #{brojNaloga})";
        var stavke = _db.SredstvaPrijave
            .Include(p => p.Sredstvo)
            .Where(p => p.BrojNaloga == brojNaloga)
            .OrderBy(p => p.RedBroj)
            .Select(p => new
            {
                p.RedBroj,
                SredstvoNaziv = p.Sredstvo != null ? p.Sredstvo.Naziv : "Nema naziva",
                p.Kolicina,
                p.NabavnaVrednost
            })
            .ToList();

        StavkeGrid.ItemsSource = stavke;
    }

    private void BtnEditNalog_Click(object sender, RoutedEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
        {
            var w = new PrijavaWindow(_db, p.BrojNaloga) { Owner = Window.GetWindow(this) };
            if (w.ShowDialog() == true)
            {
                PrijavaPage_Loaded(null!, null!);
            }
        }
        else
        {
            MessageBox.Show("Izaberite nalog iz tabele levo.", "Uredi Nalog", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void PrijavaGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
        {
            var w = new PrijavaWindow(_db, p.BrojNaloga) { Owner = Window.GetWindow(this) };
            if (w.ShowDialog() == true)
            {
                PrijavaPage_Loaded(null!, null!);
            }
        }
    }

    private void BtnNova_Click(object sender, RoutedEventArgs e)
    {
        var w = new PrijavaWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (w.ShowDialog() == true)
        {
            PrijavaPage_Loaded(null!, null!);
        }
    }
}
