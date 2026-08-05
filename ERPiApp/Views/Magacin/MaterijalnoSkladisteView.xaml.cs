using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

/// <summary>Red u levoj listi materijala na tabu "Kartice materijala" — samo za prikaz (bez čekiranja, vidi napomenu ispod).</summary>
public class MaterijalIzbor : INotifyPropertyChanged
{
    public Materijal Materijal { get; }
    public MaterijalIzbor(Materijal materijal) => Materijal = materijal;

    public string SifraArtikla => Materijal.SifraArtikla;
    public string Naziv => Materijal.Naziv;

#pragma warning disable CS0067 // Zadržan zbog simetrije sa ERPiFinansije obrascem, trenutno se ne koristi (vidi klasnu napomenu).
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
}

/// <summary>
/// Port iz ERPiFinansijeApp/Views/Magacin/MagacinView.xaml — puni tabelarni ekran materijalnog
/// knjigovodstva (6 tabova: Šifrarnik materijala, Ulazi, Trebovanja, Primopredaje, Kartice
/// materijala, Bruto bilans). Razlike od izvora ("Trim, don't transplant whole" —
/// vidi import-from-source-apps skill i PLAN_NASTAVKA.md §3g/§3i):
/// <list type="bullet">
/// <item>Deli već otvoren <see cref="ErpiDbContext"/> (konstruktor), ne otvara sopstvenu konekciju
/// po metodi kao izvor.</item>
/// <item>Ulazi/Trebovanja/Primopredaje master-detail gridovi binduju prave FK navigacione
/// property-je (<c>Magacin.NazivMagacina</c>, <c>Materijal.Naziv</c>) umesto ručnog spajanja
/// preko string šifara u code-behind-u (izvor je to radio jer stavke nisu imale FK).</item>
/// <item>Nema PDF štampu (🖨️) — ERPiApp još nema <c>PdfReportService</c> metode za ove izveštaje
/// (šifarnik materijala, ulaz/trebovanje/primopredaja nalog, materijalna kartica, bruto bilans
/// materijala); svaki tab umesto toga ima Excel izvoz (već postojeći opšti
/// <c>ExcelExportService.ExportDataGridToExcel</c>). Dodavanje PDF izveštaja ostaje za kasnije,
/// isti opštiji nedostatak kao kod ostalih novoportovanih ekrana.</item>
/// <item>Kartice materijala tab nema čekiranje/multi-select + kontekstni meni za grupnu štampu
/// više kartica odjednom — ta funkcija je u izvoru postojala isključivo da bi hranila PDF štampu
/// više kartica u jednom fajlu, koje ovde nema; obično single-select je dovoljno za pregled i
/// Excel izvoz jedne kartice u trenutku.</item>
/// </list>
/// </summary>
public partial class MaterijalnoSkladisteView : UserControl
{
    private readonly ErpiDbContext _db;

    private List<Materijal> _sviMaterijaliSifrarnik = new();
    private List<UlazNalog> _sviUlazi = new();
    private List<TrebovanjeNalog> _svaTrebovanja = new();
    private List<PrimopredajaNalog> _svePrimopredaje = new();
    private List<Materijal> _sviArtikli = new();
    private HashSet<string> _materijaliSaKarticom = new(StringComparer.OrdinalIgnoreCase);
    private List<MaterijalnaKartica> _trenutnaKarticaMaterijala = new();
    private List<RobniBrutoBilansRed> _sviBrutoRedoviMat = new();

    private static readonly ERPiData.Models.Magacin.Magacin SviMagaciniOpcija = new()
    {
        MagacinId = -1,
        SifraMagacina = "*",
        NazivMagacina = "🏢 Svi magacini"
    };

    private static bool JeSviMagacini(ERPiData.Models.Magacin.Magacin? m) => m == null || m.MagacinId == -1;

    public MaterijalnoSkladisteView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        // Postavljeno u kodu POSLE InitializeComponent(), ne kao XAML literal — IsChecked="True"
        // bi Checked event ispalio sinhrono usred InitializeComponent(), pre nego što kasnije
        // deklarisani gridovi u istom XAML stablu uopšte postoje (vidi import-from-source-apps skill).
        RbSviUlazi.IsChecked = true;
        RbSviTrebovanja.IsChecked = true;
        RbSviPrimopredaje.IsChecked = true;
        ChkSamoSaKarticom.IsChecked = true;

        // Loaded, ne direktan poziv iz konstruktora — isti razlog kao KarticaKontaView/
        // MaterijalnoDashboardView (deljen _db može da završi upit sinhrono).
        Loaded += (_, _) => LoadAllData();
    }

    private void LoadAllData()
    {
        LoadSifrarnikMaterijala();
        LoadMagaciniIArtikli();
        LoadUlazi();
        LoadTrebovanja();
        LoadPrimopredaje();
        LoadBrutoBilansMaterijala();
    }

    // ===================== ŠIFRARNIK MATERIJALA =====================

    private async void LoadSifrarnikMaterijala()
    {
        try
        {
            _sviMaterijaliSifrarnik = await _db.Materijali.AsNoTracking().OrderBy(a => a.SifraArtikla).ToListAsync();
            ApplyFilterSifrarnikMaterijala();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju šifarnika materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterSifrarnikMaterijala()
    {
        string search = TxtPretragaSifrarnikMaterijala.Text.Trim().ToLower();
        DgSifrarnikMaterijala.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviMaterijaliSifrarnik
            : _sviMaterijaliSifrarnik.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaSifrarnikMaterijala_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterSifrarnikMaterijala();

    private void BtnNoviMaterijal_Click(object sender, RoutedEventArgs e)
    {
        var win = new MaterijalEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikMaterijala();
            LoadMagaciniIArtikli();
        }
    }

    private void BtnIzmeniMaterijal_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuMaterijala();
    private void DgSifrarnikMaterijala_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuMaterijala();

    private void OtvoriIzmenuMaterijala()
    {
        if (DgSifrarnikMaterijala.SelectedItem is not Materijal selektovan)
        {
            MessageBox.Show("Izaberite materijal sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new MaterijalEditWindow(_db, selektovan.MaterijalId) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikMaterijala();
            LoadMagaciniIArtikli();
        }
    }

    private async void BtnBrisiMaterijal_Click(object sender, RoutedEventArgs e)
    {
        if (DgSifrarnikMaterijala.SelectedItem is not Materijal selektovan)
        {
            MessageBox.Show("Izaberite materijal za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            bool imaKartice = await _db.MaterijalneKartice.AnyAsync(mk => mk.SifraArtikla == selektovan.SifraArtikla);
            if (imaKartice)
            {
                MessageBox.Show($"Materijal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla}) ima otvorene materijalne kartice i promet!\n\nBrisanje nije dozvoljeno jer postoje knjiženja u sistemu.",
                    "Zaštita brisanja", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var potv = MessageBox.Show($"Da li ste sigurni da želite trajno obrisati materijal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla})?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potv == MessageBoxResult.Yes)
            {
                var a = await _db.Materijali.FirstOrDefaultAsync(x => x.MaterijalId == selektovan.MaterijalId);
                if (a != null)
                {
                    _db.Materijali.Remove(a);
                    await _db.SaveChangesAsync();
                }
                LoadSifrarnikMaterijala();
                LoadMagaciniIArtikli();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== KARTICE MATERIJALA =====================

    private async void LoadMagaciniIArtikli()
    {
        try
        {
            var magacini = await _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToListAsync();
            var stavkeZaCombo = new List<ERPiData.Models.Magacin.Magacin> { SviMagaciniOpcija };
            stavkeZaCombo.AddRange(magacini);
            CmbMagacin.ItemsSource = stavkeZaCombo;
            CmbMagacin.SelectedIndex = 0;

            _sviArtikli = await _db.Materijali.AsNoTracking().OrderBy(a => a.Naziv).ToListAsync();
            await OsveziMaterijaleSaKarticomAsync();
            FiltrirajArtikle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju magacina/materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OsveziMaterijaleSaKarticomAsync()
    {
        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin)
        {
            _materijaliSaKarticom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var service = new MaterijalnaKarticaService(_db);
        _materijaliSaKarticom = await service.GetArtikliSaKarticomAsync(JeSviMagacini(magacin) ? null : magacin.SifraMagacina);
    }

    private void FiltrirajArtikle()
    {
        string search = TxtPretragaArtikla.Text.Trim().ToLower();
        IEnumerable<Materijal> izvor = _sviArtikli;

        if (ChkSamoSaKarticom.IsChecked == true)
            izvor = izvor.Where(a => _materijaliSaKarticom.Contains(a.SifraArtikla));

        if (!string.IsNullOrEmpty(search))
            izvor = izvor.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search));

        LstArtikli.ItemsSource = izvor.Select(a => new MaterijalIzbor(a)).ToList();
    }

    private void TxtPretragaArtikla_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajArtikle();
    private void ChkSamoSaKarticom_Changed(object sender, RoutedEventArgs e) => FiltrirajArtikle();

    private async void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await OsveziMaterijaleSaKarticomAsync();
        FiltrirajArtikle();
        LoadKarticaMaterijala();
    }

    private void LstArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadKarticaMaterijala();

    private async void LoadKarticaMaterijala()
    {
        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin || LstArtikli.SelectedItem is not MaterijalIzbor izbor)
        {
            TxtNaslovArtikla.Text = "Izaberite magacin i materijal sa leve strane";
            TxtStanjeArtikla.Text = "";
            DgKarticaMaterijala.ItemsSource = null;
            _trenutnaKarticaMaterijala.Clear();
            PrikaziSumeMaterijala();
            return;
        }

        var artikal = izbor.Materijal;
        try
        {
            var upit = _db.MaterijalneKartice.AsNoTracking().Where(k => k.SifraArtikla == artikal.SifraArtikla);
            if (!JeSviMagacini(magacin)) upit = upit.Where(k => k.SifraMagacina == magacin.SifraMagacina);

            _trenutnaKarticaMaterijala = await upit
                .OrderBy(k => k.DatumPromene)
                .ThenBy(k => k.MaterijalnaKarticaId)
                .ToListAsync();

            DgKarticaMaterijala.ItemsSource = _trenutnaKarticaMaterijala;
            PrikaziSumeMaterijala();

            decimal zadnjeStanje = _trenutnaKarticaMaterijala.LastOrDefault()?.Stanje ?? 0m;
            decimal zadnjiSaldo = _trenutnaKarticaMaterijala.LastOrDefault()?.Saldo ?? 0m;
            decimal prosecnaCena = zadnjeStanje != 0 ? zadnjiSaldo / zadnjeStanje : 0;

            TxtNaslovArtikla.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) — {magacin.NazivMagacina}";
            TxtStanjeArtikla.Text = $"Trenutno stanje: {zadnjeStanje:N2} {artikal.JedinicaMere} | Prosečna cena: {prosecnaCena:N2} RSD | Stavki prometa: {_trenutnaKarticaMaterijala.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrikaziSumeMaterijala()
    {
        TxtSumaUlazMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Ulaz).ToString("N2");
        TxtSumaIzlazMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Izlaz).ToString("N2");
        TxtSumaDugujeMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Duguje).ToString("N2");
        TxtSumaPotrazujeMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Potrazuje).ToString("N2");
        TxtSumaSaldoMaterijal.Text = (_trenutnaKarticaMaterijala.Count > 0 ? _trenutnaKarticaMaterijala[^1].Saldo : 0m).ToString("N2");
    }

    private async void BtnProveraKartica_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = new MaterijalnaKarticaService(_db);
            var negativni = await service.GetNegativnaStanjaAsync();
            if (negativni.Count == 0)
            {
                MessageBox.Show("Nema negativnih stanja ni negativnih cena u materijalnim karticama.", "Provera materijalnih kartica", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new ProveraKarticaWindow(negativni) { Owner = Window.GetWindow(this) };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri proveri materijalnih kartica: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ULAZI =====================

    private async void LoadUlazi()
    {
        try
        {
            var service = new UlazService(_db);
            _sviUlazi = await service.GetUlaziAsync();
            ApplyFilterUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterUlazi()
    {
        if (DgUlazi == null) return;

        string search = TxtPretragaUlaz.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniUlazi?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniUlazi?.IsChecked == true;

        DgUlazi.ItemsSource = _sviUlazi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search)) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaUlaz_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterUlazi();
    private void Filter_Ulazi_Changed(object sender, RoutedEventArgs e) => ApplyFilterUlazi();

    private void DgUlazi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgUlazStavke.ItemsSource = DgUlazi.SelectedItem is UlazNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadUlazi();
    }

    private async void BtnIzmeniUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selektovan.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Ulaz #{selektovan.BrojNaloga} je proknjižen i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjižen ulaz", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje ulaza dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var service = new UlazService(_db);
                await service.RasknjiziUlazAsync(selektovan.UlazNalogId);
                LoadUlazi();

                var osvezen = _sviUlazi.FirstOrDefault(u => u.UlazNalogId == selektovan.UlazNalogId);
                if (osvezen != null)
                {
                    var dijalogR = new UlazEditWindow(_db, osvezen) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadUlazi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var dijalog = new UlazEditWindow(_db, selektovan) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadUlazi();
    }

    private async void BtnKnjiziUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovan.IsKnjizen)
        {
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var service = new UlazService(_db);
            await service.KnjiziUlazAsync(selektovan.UlazNalogId);
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== TREBOVANJA =====================

    private async void LoadTrebovanja()
    {
        try
        {
            var service = new TrebovanjeService(_db);
            _svaTrebovanja = await service.GetTrebovanjaAsync();
            ApplyFilterTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterTrebovanja()
    {
        if (DgTrebovanja == null) return;

        string search = TxtPretragaTrebovanje.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniTrebovanja?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniTrebovanja?.IsChecked == true;

        DgTrebovanja.ItemsSource = _svaTrebovanja.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search)) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaTrebovanje_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterTrebovanja();
    private void Filter_Trebovanja_Changed(object sender, RoutedEventArgs e) => ApplyFilterTrebovanja();

    private void DgTrebovanja_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgTrebovanjeStavke.ItemsSource = DgTrebovanja.SelectedItem is TrebovanjeNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovoTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new TrebovanjeEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadTrebovanja();
    }

    private async void BtnIzmeniTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selektovano.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Trebovanje #{selektovano.BrojNaloga} je proknjiženo i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjiženo trebovanje", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje trebovanja dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var service = new TrebovanjeService(_db);
                await service.RasknjiziTrebovanjeAsync(selektovano.TrebovanjeNalogId);
                LoadTrebovanja();

                var osvezeno = _svaTrebovanja.FirstOrDefault(t => t.TrebovanjeNalogId == selektovano.TrebovanjeNalogId);
                if (osvezeno != null)
                {
                    var dijalogR = new TrebovanjeEditWindow(_db, osvezeno) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadTrebovanja();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var dijalog = new TrebovanjeEditWindow(_db, selektovano) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadTrebovanja();
    }

    private async void BtnKnjiziTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je već proknjiženo!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var service = new TrebovanjeService(_db);
            await service.KnjiziTrebovanjeAsync(selektovano.TrebovanjeNalogId);
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je uspešno proknjiženo!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== PRIMOPREDAJE (M4) =====================

    private async void LoadPrimopredaje()
    {
        try
        {
            var service = new PrimopredajaService(_db);
            _svePrimopredaje = await service.GetPrimopredajeAsync();
            ApplyFilterPrimopredaja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju primopredaja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterPrimopredaja()
    {
        if (DgPrimopredaje == null) return;

        string search = TxtPretragaPrimopredaja.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniPrimopredaje?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniPrimopredaje?.IsChecked == true;

        DgPrimopredaje.ItemsSource = _svePrimopredaje.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search)
                || (n.MagacinDaje != null && n.MagacinDaje.NazivMagacina.ToLower().Contains(search))
                || (n.MagacinPrima != null && n.MagacinPrima.NazivMagacina.ToLower().Contains(search))) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaja();
    private void Filter_Primopredaje_Changed(object sender, RoutedEventArgs e) => ApplyFilterPrimopredaja();

    private void DgPrimopredaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgPrimopredajaStavke.ItemsSource = DgPrimopredaje.SelectedItem is PrimopredajaNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PrimopredajaEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadPrimopredaje();
    }

    private async void BtnIzmeniPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Primopredaja #{selektovano.BrojNaloga} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                "Proknjižena primopredaja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje primopredaje dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var service = new PrimopredajaService(_db);
                await service.RasknjiziPrimopredajuAsync(selektovano.PrimopredajaNalogId);
                LoadPrimopredaje();

                var osvezeno = _svePrimopredaje.FirstOrDefault(p => p.PrimopredajaNalogId == selektovano.PrimopredajaNalogId);
                if (osvezeno != null)
                {
                    var dijalogR = new PrimopredajaEditWindow(_db, osvezeno) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadPrimopredaje();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var dijalog = new PrimopredajaEditWindow(_db, selektovano) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadPrimopredaje();
    }

    private async void BtnKnjiziPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju sa liste.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var service = new PrimopredajaService(_db);
            await service.KnjiziPrimopredajuAsync(selektovano.PrimopredajaNalogId);
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je uspešno proknjižena u materijalnom knjigovodstvu!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPrimopredaje();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== BRUTO BILANS MATERIJALA =====================

    private async void LoadBrutoBilansMaterijala()
    {
        try
        {
            if (CmbMagacinBrutoMat.ItemsSource == null)
            {
                var magacini = await _db.Magacini.AsNoTracking().ToListAsync();
                magacini.Insert(0, new ERPiData.Models.Magacin.Magacin { MagacinId = 0, SifraMagacina = "SVI", NazivMagacina = "--- Svi magacini ---" });
                CmbMagacinBrutoMat.ItemsSource = magacini;
                CmbMagacinBrutoMat.SelectedIndex = 0;
            }

            int? magId = (CmbMagacinBrutoMat.SelectedValue is int idVal && idVal > 0) ? idVal : null;
            DateTime? doDatuma = DpDoDatumaBrutoMat.SelectedDate;
            string? pretraga = TxtPretragaBrutoMat.Text.Trim();

            var bilansService = new RobniBrutoBilansService(_db);
            _sviBrutoRedoviMat = await bilansService.GetMaterijalniBrutoBilansAsync(magId, doDatuma, pretraga);

            DgBrutoBilansMat.ItemsSource = _sviBrutoRedoviMat;

            decimal ukDug = _sviBrutoRedoviMat.Sum(r => r.UlazVrednost);
            decimal ukPot = _sviBrutoRedoviMat.Sum(r => r.IzlazVrednost);
            decimal ukSal = _sviBrutoRedoviMat.Sum(r => r.SaldoVrednosni);

            TxtUkupnoDugujeBrutoMat.Text = $"Ukupno Duguje: {ukDug:N2} RSD";
            TxtUkupnoPotrazujeBrutoMat.Text = $"Ukupno Potražuje: {ukPot:N2} RSD";
            TxtUkupnoSaldoBrutoMat.Text = $"Saldo Zaliha: {ukSal:N2} RSD";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri računanju Bruto bilansa materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbMagacinBrutoMat_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void DpDoDatumaBrutoMat_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void TxtPretragaBrutoMat_TextChanged(object sender, TextChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void BtnOsveziBrutoMat_Click(object sender, RoutedEventArgs e) => LoadBrutoBilansMaterijala();

    // ===================== EXCEL EXPORT DUGMIĆI =====================

    private void BtnExportExcelMaterijali_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgSifrarnikMaterijala, "Šifrarnik materijala", "Sifrarnik_Materijala");

    private void BtnExportExcelUlazi_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgUlazi, "Ulazi materijala", "Ulazi_Materijala");

    private void BtnExportExcelTrebovanja_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgTrebovanja, "Trebovanja materijala", "Trebovanja_Materijala");

    private void BtnExportExcelPrimopredaje_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgPrimopredaje, "Primopredaje materijala", "Primopredaje_Materijala");

    private void BtnExportExcelKartica_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgKarticaMaterijala, TxtNaslovArtikla.Text, "Materijalna_Kartica");

    private void BtnExportExcelBrutoMat_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgBrutoBilansMat, "Bruto bilans materijalnog knjigovodstva", "Bruto_Bilans_Materijalnog_Knjigovodstva");
}
