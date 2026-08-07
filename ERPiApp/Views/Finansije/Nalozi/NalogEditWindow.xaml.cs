using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ERPiData;
using ERPiData.Models.Finansije;

namespace ERPiApp.Views.Finansije.Nalozi;

public partial class NalogEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly Nalog _nalog;
    private readonly bool _jeNov;
    private readonly List<StavkaNaloga> _stavke;
    private bool _isReadOnly;

    public NalogEditWindow(ErpiDbContext db, Nalog nalog, bool isReadOnly = false, int? fokusRedniBroj = null)
    {
        InitializeComponent();
        _db = db;
        _nalog = nalog;
        _jeNov = nalog.NalogId == 0;
        _isReadOnly = isReadOnly;

        // Radna kopija stavki — original se ne dira dok korisnik ne sačuva (Otkaži mora
        // ostaviti tačno ono što je bilo pre otvaranja dijaloga).
        _stavke = nalog.Stavke.Select(s => new StavkaNaloga
        {
            RedniBroj = s.RedniBroj,
            KontoId = s.KontoId,
            PartnerId = s.PartnerId,
            MestoTroskaId = s.MestoTroskaId,
            BrojDokumenta = s.BrojDokumenta,
            Opis = s.Opis,
            Duguje = s.Duguje,
            Potrazuje = s.Potrazuje
        }).ToList();

        TxtBrojNaloga.Text = nalog.BrojNaloga.ToString();
        DpDatum.SelectedDate = _jeNov ? DateTime.Now : nalog.DatumNaloga;
        TxtVrsta.Text = string.IsNullOrEmpty(nalog.VrstaNaloga) ? "Finansijski" : nalog.VrstaNaloga;
        TxtOpisNaloga.Text = nalog.Opis;

        ColKonto.ItemsSource = _db.Konta.OrderBy(k => k.BrojKonta).ToList();
        ColPartner.ItemsSource = _db.Partneri.OrderBy(p => p.Naziv).ToList();
        ColMestoTroska.ItemsSource = _db.MestaTroska.OrderBy(m => m.Naziv).ToList();

        DgStavke.ItemsSource = _stavke;

        Title = _jeNov
            ? "Novi nalog za knjiženje"
            : _isReadOnly
                ? $"📖 Pregled proknjiženog naloga #{nalog.BrojNaloga} (Samo za čitanje)"
                : $"Izmena naloga #{nalog.BrojNaloga}";

        if (_isReadOnly) PrimeniReadOnlyRezim();

        OsveziBalans();
        PozicionirajNaStavku(fokusRedniBroj);
    }

    /// <summary>
    /// Zaključava zaglavlje/grid i prikazuje samo "Rasknjiži i izmeni" — koristi se kad se
    /// nalog otvara na uvid iz Kartice konta/spiska naloga, a već je proknjižen.
    /// </summary>
    private void PrimeniReadOnlyRezim()
    {
        DpDatum.IsEnabled = false;
        TxtVrsta.IsReadOnly = true;
        TxtOpisNaloga.IsReadOnly = true;
        DgStavke.IsReadOnly = true;
        BtnDodajStavku.Visibility = Visibility.Collapsed;
        BtnObrisiStavku.Visibility = Visibility.Collapsed;
        BtnSacuvajNacrt.Visibility = Visibility.Collapsed;
        BtnProknjizi.Visibility = Visibility.Collapsed;
        BtnRaskniziIIzmeni.Visibility = Visibility.Visible;
        BtnOtkazi.Content = "Zatvori (Esc)";
    }

    /// <summary>
    /// Vraća dijalog u editabilan režim posle rasknjižavanja iz "Rasknjiži i izmeni" —
    /// isti prozor nastavlja kao obična izmena, bez ponovnog otvaranja.
    /// </summary>
    private void PrebaciURezimIzmene()
    {
        _isReadOnly = false;
        Title = $"Izmena naloga #{_nalog.BrojNaloga}";
        DpDatum.IsEnabled = true;
        TxtVrsta.IsReadOnly = false;
        TxtOpisNaloga.IsReadOnly = false;
        DgStavke.IsReadOnly = false;
        BtnDodajStavku.Visibility = Visibility.Visible;
        BtnObrisiStavku.Visibility = Visibility.Visible;
        BtnSacuvajNacrt.Visibility = Visibility.Visible;
        BtnProknjizi.Visibility = Visibility.Visible;
        BtnRaskniziIIzmeni.Visibility = Visibility.Collapsed;
        BtnOtkazi.Content = "Otkaži (Esc)";
    }

    private void BtnRaskniziIIzmeni_Click(object sender, RoutedEventArgs e)
    {
        var odgovor = MessageBox.Show(
            $"Da li želite da rasknjižite nalog #{_nalog.BrojNaloga} radi izmene?",
            "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (odgovor != MessageBoxResult.Yes) return;

        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _nalog.Status = StatusNaloga.Nacrt;
        _nalog.DatumKnjizenja = null;
        _db.SaveChanges();

        PrebaciURezimIzmene();
        MessageBox.Show($"Nalog #{_nalog.BrojNaloga} je uspešno rasknjižen i omogućen za izmenu.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Selektuje i skroluje na stavku iz koje je nalog otvoren (klik u kartici konta). Radna
    /// kopija stavki ne nosi StavkaNalogaId (vidi konstruktor), pa se pozicioniranje radi po
    /// RedniBroj — jedinom identifikatoru koji working-copy stavke zadržavaju iz originala.
    /// Grid redovi postoje tek posle prvog layout prolaza, pa se pozicioniranje radi na
    /// Loaded (isti razlog kao "ne zovi loader direktno iz konstruktora" pravilo za ovaj projekat).
    /// </summary>
    private void PozicionirajNaStavku(int? redniBroj)
    {
        if (redniBroj is not int rb) return;

        var trazena = _stavke.FirstOrDefault(s => s.RedniBroj == rb);
        if (trazena == null) return;

        Loaded += (_, _) =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DgStavke.SelectedItem = trazena;
                DgStavke.ScrollIntoView(trazena);
            }), System.Windows.Threading.DispatcherPriority.Background);
        };
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new StavkaNaloga { RedniBroj = _stavke.Count + 1 });
        OsveziGrid();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is not StavkaNaloga stavka) return;
        _stavke.Remove(stavka);
        for (var i = 0; i < _stavke.Count; i++) _stavke[i].RedniBroj = i + 1;
        OsveziGrid();
    }

    private void OsveziGrid()
    {
        DgStavke.ItemsSource = null;
        DgStavke.ItemsSource = _stavke;
        OsveziBalans();
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // Binding se commit-uje tek posle ovog događaja — sačekaj jedan Dispatcher ciklus.
        Dispatcher.BeginInvoke(new Action(OsveziBalans));
    }

    private void DgStavke_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.F2) return;
        if (DgStavke.SelectedItem is not StavkaNaloga stavka) return;

        DgStavke.CommitEdit(DataGridEditingUnit.Row, true);

        var picker = new Konta.KontoPickerWindow(_db) { Owner = this };
        if (picker.ShowDialog() == true && picker.IzabraniKonto != null)
        {
            stavka.KontoId = picker.IzabraniKonto.KontoId;
            OsveziGrid();
        }

        e.Handled = true;
    }

    private void OsveziBalans()
    {
        var duguje = _stavke.Sum(s => s.Duguje);
        var potrazuje = _stavke.Sum(s => s.Potrazuje);
        TxtZbirDuguje.Text = duguje.ToString("N2");
        TxtZbirPotrazuje.Text = potrazuje.ToString("N2");

        if (_stavke.Count == 0)
        {
            TxtBalansStatus.Text = "⚠️ Nema stavki";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
        }
        else if (Math.Abs(duguje - potrazuje) < 0.01m)
        {
            TxtBalansStatus.Text = "✅ Uravnoteženo";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xFD, 0xF5));
        }
        else
        {
            TxtBalansStatus.Text = $"❌ Razlika: {duguje - potrazuje:N2}";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSacuvajNacrt_Click(object sender, RoutedEventArgs e) => Sacuvaj(StatusNaloga.Nacrt);
    private void BtnProknjizi_Click(object sender, RoutedEventArgs e) => Sacuvaj(StatusNaloga.Proknjizen);

    private void Sacuvaj(StatusNaloga zeljeniStatus)
    {
        if (DpDatum.SelectedDate == null)
        {
            MessageBox.Show("Unesite datum naloga.", "Nedostaje datum", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Count == 0)
        {
            MessageBox.Show("Nalog mora imati bar jednu stavku.", "Nema stavki", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Any(s => s.KontoId == 0))
        {
            MessageBox.Show("Sve stavke moraju imati izabran konto.", "Nedostaje konto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var duguje = _stavke.Sum(s => s.Duguje);
        var potrazuje = _stavke.Sum(s => s.Potrazuje);

        if (zeljeniStatus == StatusNaloga.Proknjizen && Math.Abs(duguje - potrazuje) >= 0.01m)
        {
            MessageBox.Show("Nalog nije uravnotežen (Duguje ≠ Potražuje) — ne može se proknjižiti. Sačuvajte kao nacrt.",
                "Nalog nije uravnotežen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _nalog.DatumNaloga = DpDatum.SelectedDate.Value;
        _nalog.VrstaNaloga = string.IsNullOrWhiteSpace(TxtVrsta.Text) ? "Finansijski" : TxtVrsta.Text.Trim();
        _nalog.Opis = TxtOpisNaloga.Text.Trim();
        _nalog.UkupnoDuguje = duguje;
        _nalog.UkupnoPotrazuje = potrazuje;
        _nalog.Status = zeljeniStatus;
        _nalog.DatumKnjizenja = zeljeniStatus == StatusNaloga.Proknjizen ? DateTime.Now : null;

        // Zamena stavki sadržajem radne kopije — brisanje starih i upis novih, umesto diff-a.
        // Dovoljno prosto za obim Faze 3.1; ako se pokaže sporo na velikim nalozima, prelazi se
        // na pravi diff po StavkaNalogaId.
        if (!_jeNov && _nalog.Stavke.Count > 0)
        {
            _db.StavkeNaloga.RemoveRange(_nalog.Stavke);
        }

        _nalog.Stavke = _stavke.Select(s => new StavkaNaloga
        {
            RedniBroj = s.RedniBroj,
            KontoId = s.KontoId,
            PartnerId = s.PartnerId,
            MestoTroskaId = s.MestoTroskaId,
            BrojDokumenta = s.BrojDokumenta,
            Opis = s.Opis,
            Duguje = s.Duguje,
            Potrazuje = s.Potrazuje
        }).ToList();

        if (_jeNov) _db.Nalozi.Add(_nalog);

        _db.SaveChanges();

        DialogResult = true;
        Close();
    }
}
