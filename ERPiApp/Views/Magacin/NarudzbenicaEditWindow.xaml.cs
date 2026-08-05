using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Unos/izmena Narudžbenice dobavljaču. Port iz
/// ERPiFinansijeApp/Views/Trgovina/NarudzbenicaEditWindow.xaml, prilagođeno
/// DataGridComboBoxColumn artikal-pikeru (isti obrazac kao KalkulacijaEditWindow) i dodatnom
/// polju <see cref="NarudzbenicaDobavljacu.MagacinId"/> (magacin prijema — potreban za
/// konverziju u Kalkulaciju, vidi PLAN_NASTAVKA.md §3i i model modela).
/// </summary>
public partial class NarudzbenicaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _narudzbenicaId;
    public ObservableCollection<NarudzbenicaStavkaModel> StavkeModels { get; set; } = new();

    public class NarudzbenicaStavkaModel
    {
        public int ArtikalId { get; set; }
        public decimal KolicinaNarucena { get; set; } = 1.00m;
        public decimal KolicinaPristigla { get; set; }
        public decimal Cena { get; set; }
        public decimal PdvStopa { get; set; } = 20.00m;
        public decimal IznosNeto => Math.Round(KolicinaNarucena * Cena, 2);
        public decimal IznosPdv => Math.Round(IznosNeto * (PdvStopa / 100.00m), 2);
        public decimal IznosBruto => Math.Round(IznosNeto + IznosPdv, 2);
    }

    public NarudzbenicaEditWindow(ErpiDbContext db, int? narudzbenicaId)
    {
        InitializeComponent();
        _db = db;
        _narudzbenicaId = narudzbenicaId;

        UcitajSifarnike();

        if (_narudzbenicaId.HasValue)
        {
            Title = "Izmena narudžbenice dobavljaču";
            UcitajNarudzbenicu();
        }
        else
        {
            Title = "Nova narudžbenica dobavljaču";
            DpDatum.SelectedDate = DateTime.Today;
            DpRokIsporuke.SelectedDate = DateTime.Today.AddDays(7);
        }

        DgStavke.ItemsSource = StavkeModels;
        OsveziSume();
    }

    private void UcitajSifarnike()
    {
        var partneri = _db.Partneri.AsNoTracking().Where(p => p.JeDobavljac || p.IsActive).OrderBy(p => p.Naziv).ToList();
        CmbPartner.ItemsSource = partneri;

        var magacini = _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacin.ItemsSource = magacini;

        var artikli = _db.Artikli.AsNoTracking().OrderBy(a => a.SifraArtikla).ToList();
        ColArtikal.ItemsSource = artikli;
    }

    private void UcitajNarudzbenicu()
    {
        var n = _db.NarudzbeniceDobavljacima
            .Include(x => x.Stavke)
            .FirstOrDefault(x => x.NarudzbenicaId == _narudzbenicaId!.Value);

        if (n == null) return;

        CmbPartner.SelectedValue = n.PartnerId;
        CmbMagacin.SelectedValue = n.MagacinId;
        TxtBrojNarudzbenice.Text = n.BrojNarudzbenice;
        DpDatum.SelectedDate = n.Datum;
        DpRokIsporuke.SelectedDate = n.RokIsporuke;
        TxtNapomena.Text = n.Napomena;

        StavkeModels.Clear();
        foreach (var st in n.Stavke.OrderBy(s => s.RedniBroj))
        {
            StavkeModels.Add(new NarudzbenicaStavkaModel
            {
                ArtikalId = st.ArtikalId ?? 0,
                KolicinaNarucena = st.KolicinaNarucena,
                KolicinaPristigla = st.KolicinaPristigla,
                Cena = st.Cena,
                PdvStopa = st.PdvStopa
            });
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        var prviArtikal = _db.Artikli.AsNoTracking().FirstOrDefault();
        if (prviArtikal == null)
        {
            MessageBox.Show("Molimo unesite bar jedan artikal u šifarnik pre pravljenja narudžbenice.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StavkeModels.Add(new NarudzbenicaStavkaModel
        {
            ArtikalId = prviArtikal.ArtikalId,
            KolicinaNarucena = 1,
            Cena = prviArtikal.NabavnaCena,
            PdvStopa = prviArtikal.PdvStopa
        });

        OsveziSume();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is NarudzbenicaStavkaModel item)
        {
            StavkeModels.Remove(item);
            OsveziSume();
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(OsveziSume));
    }

    private void OsveziSume()
    {
        TxtZbirNeto.Text = $"{StavkeModels.Sum(s => s.IznosNeto):N2} RSD";
        TxtZbirPdv.Text = $"{StavkeModels.Sum(s => s.IznosPdv):N2} RSD";
        TxtZbirBruto.Text = $"{StavkeModels.Sum(s => s.IznosBruto):N2} RSD";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!StavkeModels.Any())
        {
            MessageBox.Show("Narudžbenica mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            NarudzbenicaDobavljacu n;
            if (_narudzbenicaId.HasValue)
            {
                n = await new KomercijalaService(_db).GetNarudzbenicaByIdAsync(_narudzbenicaId.Value)
                    ?? throw new InvalidOperationException("Narudžbenica više ne postoji.");
            }
            else
            {
                n = new NarudzbenicaDobavljacu();
            }

            n.PartnerId = CmbPartner.SelectedValue as int?;
            n.MagacinId = CmbMagacin.SelectedValue as int?;
            n.Datum = DpDatum.SelectedDate ?? DateTime.Today;
            n.RokIsporuke = DpRokIsporuke.SelectedDate;
            n.Napomena = TxtNapomena.Text.Trim();

            n.Stavke = StavkeModels.Select((s, idx) => new NarudzbenicaStavka
            {
                RedniBroj = idx + 1,
                ArtikalId = s.ArtikalId,
                KolicinaNarucena = s.KolicinaNarucena,
                KolicinaPristigla = s.KolicinaPristigla,
                Cena = s.Cena,
                PdvStopa = s.PdvStopa
            }).ToList();

            await new KomercijalaService(_db).SacuvajNarudzbenicuAsync(n);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju narudžbenice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
