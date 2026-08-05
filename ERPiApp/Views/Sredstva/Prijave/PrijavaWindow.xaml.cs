using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Sredstva;
using ERPiData.Services.Sredstva;
using ERPiApp.Views.Sredstva.Prijave.Stampe;

namespace ERPiApp.Views.Sredstva.Prijave;

public class PrijavaStavkaViewModel
{
    public int RedBroj { get; set; }
    public string InventarskiBroj { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public decimal NabavnaVrednost { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public string AmortizacionaGrupa { get; set; } = string.Empty;
    public int? KontoId { get; set; }
    public string KontoPrikaz { get; set; } = string.Empty;
    public int ObracunskaJedinica { get; set; }
    public decimal Kolicina { get; set; } = 1m;
    public decimal OtpisanaVrednost { get; set; }
    public int Sifra { get; set; }
    public string BrojFakture { get; set; } = string.Empty;
    public string PoreskaGrupa { get; set; } = string.Empty;
    public decimal PoreskaStopa { get; set; }
}

/// <summary>
/// Nalog za prijem/aktiviranje osnovnih sredstava. Port iz ERPiSredstvaApp.Views.Rashod.PrijavaWindow
/// (izvorni namespace je legacy — fizički folder je "Prijave"). Razlike od izvora: konto je
/// ComboBox nad Core.Konto (KontoId FK) umesto slobodnog teksta, dobavljač je Core.Partner
/// (JeDobavljac=true) umesto zasebnog Dobavljac modela — "+ Novi dobavljač" brzi unos namerno
/// nije prenet, novi dobavljač (partner) se unosi kroz postojeći ekran Partneri.
/// </summary>
public partial class PrijavaWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _brojNaloga;
    public ObservableCollection<PrijavaStavkaViewModel> Stavke { get; set; } = new();

    public PrijavaWindow(ErpiDbContext db, int? brojNaloga = null)
    {
        InitializeComponent();
        _db = db;
        _brojNaloga = brojNaloga;
        DataContext = this;
        Loaded += PrijavaWindow_Loaded;
    }

    private void PrijavaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CmbPartner.ItemsSource = _db.Partneri.Where(p => p.JeDobavljac).OrderBy(p => p.Naziv).ToList();
        CmbKonto.ItemsSource = _db.Konta.OrderBy(k => k.BrojKonta).ToList();
        DpDatum.SelectedDate = DateTime.Today;
        CmbPoreskaGrupa.ItemsSource = PoreskaGrupaCatalog.Grupe;
        CmbPoreskaGrupa.SelectedIndex = 2; // Default: Grupa III (15%)

        TxtNaziv.LostFocus += TxtNaziv_LostFocus;

        if (_brojNaloga.HasValue)
        {
            Title = $"Pregled Prijave #{_brojNaloga.Value}";
            UcitajPostojeceNaloge(_brojNaloga.Value);
        }
        else
        {
            Title = "Nova Prijava (Nalog)";
            var maxNalog = _db.SredstvaPrijave.Any() ? _db.SredstvaPrijave.Max(p => p.BrojNaloga) : 0;
            TxtBrojNaloga.Text = (maxNalog + 1).ToString();
        }
    }

    private void TxtNaziv_LostFocus(object sender, RoutedEventArgs e)
    {
        var kontoTekst = (CmbKonto.SelectedItem as Konto)?.BrojKonta ?? string.Empty;
        var predlog = PoreskaGrupaCatalog.PredloziGrupu(kontoTekst, TxtNaziv.Text);
        if (predlog != null)
        {
            CmbPoreskaGrupa.SelectedValue = predlog.Kod;
        }
    }

    private void UcitajPostojeceNaloge(int brojNaloga)
    {
        var prijave = _db.SredstvaPrijave
            .Include(p => p.Sredstvo)
            .Include(p => p.Konto)
            .Where(p => p.BrojNaloga == brojNaloga)
            .OrderBy(p => p.RedBroj)
            .ToList();

        if (prijave.Count == 0) return;

        var prva = prijave.First();
        TxtBrojNaloga.Text = prva.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        CmbPartner.SelectedValue = prva.PartnerId;
        DpDatum.SelectedDate = prva.DatumAktiviranja;

        foreach (var p in prijave)
        {
            Stavke.Add(new PrijavaStavkaViewModel
            {
                RedBroj = p.RedBroj,
                InventarskiBroj = p.InventarskiBroj,
                Naziv = p.Sredstvo?.Naziv ?? "Nepoznato",
                NabavnaVrednost = p.NabavnaVrednost,
                StopaAmortizacije = p.StopaAmortizacije,
                AmortizacionaGrupa = p.AmortizacionaGrupa1.ToString(),
                KontoId = p.KontoId,
                KontoPrikaz = p.Konto?.BrojKonta ?? string.Empty,
                ObracunskaJedinica = p.ObracunskaJedinica,
                Kolicina = p.Kolicina,
                OtpisanaVrednost = p.OtpisanaVrednost,
                BrojFakture = p.BrojFakture,
                Sifra = p.Sredstvo?.LegacySifra ?? 0
            });
        }

        if (prva.Knjizen)
        {
            GridNovaStavka.IsEnabled = false;
            CmbPartner.IsEnabled = false;
            DpDatum.IsEnabled = false;
            BtnDodaj.Visibility = Visibility.Collapsed;
            BtnProknjizi.Visibility = Visibility.Collapsed;
            Title += " (PROKNJIŽENO)";
            MessageBox.Show("Ovaj nalog je već proknjižen i ne može se menjati.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInvBroj.Text) || string.IsNullOrWhiteSpace(TxtNaziv.Text))
        {
            MessageBox.Show("Inventarski broj i Naziv su obavezni.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtNabavna.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal nabavna))
        {
            MessageBox.Show("Neispravna nabavna vrednost.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtStopa.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal stopa))
        {
            MessageBox.Show("Neispravna stopa amortizacije.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var invBroj = TxtInvBroj.Text.Trim();
        if (_db.Sredstva.Any(s => s.InventarskiBroj == invBroj) || Stavke.Any(s => s.InventarskiBroj == invBroj))
        {
            MessageBox.Show($"Sredstvo sa inventarskim brojem {invBroj} već postoji!", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int oj = 1;
        string pgKod = CmbPoreskaGrupa.SelectedValue as string ?? "III";
        decimal pgStopa = PoreskaGrupaCatalog.GetStopaZaGrupu(pgKod);
        var konto = CmbKonto.SelectedItem as Konto;

        var novaStavka = new PrijavaStavkaViewModel
        {
            RedBroj = Stavke.Count + 1,
            InventarskiBroj = invBroj,
            Naziv = TxtNaziv.Text.Trim(),
            NabavnaVrednost = nabavna,
            StopaAmortizacije = stopa,
            AmortizacionaGrupa = TxtGrupa.Text.Trim(),
            KontoId = konto?.KontoId,
            KontoPrikaz = konto?.BrojKonta ?? string.Empty,
            ObracunskaJedinica = oj,
            Kolicina = 1m,
            OtpisanaVrednost = 0m,
            BrojFakture = "",
            PoreskaGrupa = pgKod,
            PoreskaStopa = pgStopa
        };

        Stavke.Add(novaStavka);
        ObrisiPoljaZaUnos();
    }

    private void ObrisiPoljaZaUnos()
    {
        TxtInvBroj.Text = "";
        TxtNaziv.Text = "";
        TxtNabavna.Text = "";
        TxtStopa.Text = "";
        TxtGrupa.Text = "";
        CmbKonto.SelectedIndex = -1;
        TxtInvBroj.Focus();
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (!Stavke.Any())
        {
            MessageBox.Show("Nema stavki u nalogu za knjiženje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var resMsg = MessageBox.Show($"Da li ste sigurni da želite da proknjižite nalog sa {Stavke.Count} stavki?", "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (resMsg != MessageBoxResult.Yes) return;

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            DateTime datum = DpDatum.SelectedDate ?? DateTime.Today;
            int? partnerId = CmbPartner.SelectedValue as int?;
            int brojNaloga = _brojNaloga ?? ((_db.SredstvaPrijave.Max(p => (int?)p.BrojNaloga) ?? 0) + 1);

            foreach (var stavka in Stavke)
            {
                int.TryParse(stavka.AmortizacionaGrupa, out int amGrInt);

                var sredstvo = new Sredstvo
                {
                    InventarskiBroj = stavka.InventarskiBroj,
                    Naziv = stavka.Naziv,
                    DatumNabavke = datum,
                    DatumAktiviranja = datum,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    IspravkaVrednosti = 0,
                    SadasnjaVrednost = stavka.NabavnaVrednost,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    AmortizacionaGrupa = stavka.AmortizacionaGrupa,
                    PoreskaGrupa = !string.IsNullOrEmpty(stavka.PoreskaGrupa) ? stavka.PoreskaGrupa : "III",
                    PoreskaStopa = stavka.PoreskaStopa > 0 ? stavka.PoreskaStopa : 15m,
                    PoreskaNabavnaVrednost = stavka.NabavnaVrednost,
                    PoreskaIspravkaVrednosti = stavka.OtpisanaVrednost,
                    JeAktivno = true,
                    LegacySifra = 0
                };
                _db.Sredstva.Add(sredstvo);
                _db.SaveChanges(); // Potrebno da dobijemo ID

                var kartica = new Kartica
                {
                    SredstvoId = sredstvo.Id,
                    RedBroj = 1,
                    Datum = datum,
                    OpisPromene = "Pocetno stanje / Nabavka",
                    KontoId = stavka.KontoId,
                    ObracunskaJedinica = stavka.ObracunskaJedinica,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    IspravkaVrednosti = 0,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    AmortizacionaGrupa1 = amGrInt,
                    KoeficijentRevalorizacije = 1
                };
                _db.SredstvaKartice.Add(kartica);

                var prijava = new Prijava
                {
                    BrojNaloga = brojNaloga,
                    RedBroj = stavka.RedBroj,
                    SredstvoId = sredstvo.Id,
                    DatumAktiviranja = datum,
                    InventarskiBroj = stavka.InventarskiBroj,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    KontoId = stavka.KontoId,
                    ObracunskaJedinica = stavka.ObracunskaJedinica,
                    AmortizacionaGrupa1 = amGrInt,
                    Kolicina = stavka.Kolicina,
                    OtpisanaVrednost = stavka.OtpisanaVrednost,
                    BrojFakture = stavka.BrojFakture,
                    PartnerId = partnerId,
                    Knjizen = true
                };
                _db.SredstvaPrijave.Add(prijava);
            }

            _db.SaveChanges();
            transaction.Commit();
            MessageBox.Show("Nalog je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show($"Greška prilikom knjiženja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (Stavke.Count == 0)
        {
            MessageBox.Show("Nema stavki za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga);
            var datum = DpDatum.SelectedDate ?? DateTime.Today;
            var partnerNaziv = (CmbPartner.SelectedItem as Partner)?.Naziv ?? "";
            var firma = _db.Firme.FirstOrDefault();
            var doc = new PrijavaDocument(brojNaloga, datum, partnerNaziv, Stavke, firma);

            var tempFile = Path.Combine(Path.GetTempPath(), $"Prijava_{brojNaloga}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);

            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška prilikom generisanja PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
