using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Port iz ERPiFinansijeApp — razlika: prima deljeni <see cref="ErpiDbContext"/> kroz konstruktor
/// (ne otvara sopstvenu konekciju, vidi napomenu o deljenom _db u PLAN_NASTAVKA.md §2) i grid kolona
/// za materijal je pravi FK combo (<c>MaterijalId</c>), ne slobodan tekst šifre.
/// </summary>
public partial class UlazEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly ObservableCollection<UlazStavka> _stavke = new();
    private readonly int _postojeciId;

    public UlazEditWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        DgStavke.ItemsSource = _stavke;
        DpDatum.SelectedDate = DateTime.Now;

        ColMaterijal.ItemsSource = _db.Materijali.OrderBy(m => m.Naziv).ToList();
        UcitajMagacine();
    }

    public UlazEditWindow(ErpiDbContext db, UlazNalog postojeci)
    {
        InitializeComponent();
        _db = db;
        DgStavke.ItemsSource = _stavke;
        _postojeciId = postojeci.UlazNalogId;

        if (postojeci.IsKnjizen)
        {
            MessageBox.Show($"Ulaz br. {postojeci.BrojNaloga} je proknjižen i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            IsEnabled = false;
        }

        Title = $"Izmena ulaza br. {postojeci.BrojNaloga}";
        TxtBrojNaloga.Text = postojeci.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = postojeci.Datum;
        TxtBrojRacuna.Text = postojeci.BrojRacuna ?? "";

        ColMaterijal.ItemsSource = _db.Materijali.OrderBy(m => m.Naziv).ToList();
        foreach (var s in postojeci.Stavke.OrderBy(s => s.RedniBroj))
        {
            _stavke.Add(new UlazStavka { RedniBroj = s.RedniBroj, MaterijalId = s.MaterijalId, Kolicina = s.Kolicina, Cena = s.Cena, Iznos = s.Iznos });
        }

        UcitajMagacine(postojeci.MagacinId);
    }

    private void UcitajMagacine(int? selektujId = null)
    {
        var magacini = _db.Magacini.OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacin.ItemsSource = magacini;
        if (selektujId.HasValue)
        {
            CmbMagacin.SelectedItem = magacini.FirstOrDefault(m => m.MagacinId == selektujId.Value) ?? (magacini.Count > 0 ? magacini[0] : null);
        }
        else if (magacini.Count > 0)
        {
            CmbMagacin.SelectedIndex = 0;
        }

        if (_postojeciId == 0)
        {
            int max = _db.UlazNalozi.Select(n => (int?)n.BrojNaloga).Max() ?? 0;
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new UlazStavka { RedniBroj = _stavke.Count + 1 });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is UlazStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            DgStavke.Items.Refresh();
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Unesite ispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_stavke.Count == 0)
        {
            MessageBox.Show("Dodajte bar jednu stavku ulaza.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (s.MaterijalId == 0)
            {
                MessageBox.Show("Svaka stavka mora imati izabran materijal.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var service = new UlazService(_db);

            var noveStavke = new List<UlazStavka>();
            int red = 1;
            foreach (var s in _stavke)
            {
                noveStavke.Add(new UlazStavka
                {
                    RedniBroj = red++,
                    MaterijalId = s.MaterijalId,
                    Kolicina = s.Kolicina,
                    Cena = s.Cena,
                    Iznos = s.Kolicina * s.Cena
                });
            }

            if (_postojeciId == 0)
            {
                var nalog = new UlazNalog
                {
                    BrojNaloga = brojNaloga,
                    Datum = DpDatum.SelectedDate ?? DateTime.Now,
                    MagacinId = magacin.MagacinId,
                    BrojRacuna = TxtBrojRacuna.Text.Trim()
                };
                nalog.Stavke.AddRange(noveStavke);
                await service.SaveUlazAsync(nalog);
            }
            else
            {
                await service.UpdateUlazAsync(_postojeciId, DpDatum.SelectedDate ?? DateTime.Now, magacin.MagacinId, TxtBrojRacuna.Text.Trim(), noveStavke);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
