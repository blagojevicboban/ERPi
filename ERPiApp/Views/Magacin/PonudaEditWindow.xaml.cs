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
/// Unos/izmena Ponude ili Predračuna. Port iz
/// ERPiFinansijeApp/Views/Trgovina/PonudaEditWindow.xaml, prilagođeno DataGridComboBoxColumn
/// artikal-pikeru (isti obrazac kao KalkulacijaEditWindow) umesto WrapPanel "dodaj stavku"
/// trake iz izvora — vidi PLAN_NASTAVKA.md §3i.
/// </summary>
public partial class PonudaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _ponudaId;
    public ObservableCollection<PonudaStavkaModel> StavkeModels { get; set; } = new();

    public class PonudaStavkaModel
    {
        public int ArtikalId { get; set; }
        public decimal Kolicina { get; set; } = 1.00m;
        public decimal Cena { get; set; }
        public decimal RabatProcenat { get; set; }
        public decimal PdvStopa { get; set; } = 20.00m;
        public decimal IznosNeto => Math.Round(Kolicina * Cena * (1.00m - (RabatProcenat / 100.00m)), 2);
        public decimal IznosPdv => Math.Round(IznosNeto * (PdvStopa / 100.00m), 2);
        public decimal IznosBruto => Math.Round(IznosNeto + IznosPdv, 2);
    }

    public PonudaEditWindow(ErpiDbContext db, int? ponudaId)
    {
        InitializeComponent();
        _db = db;
        _ponudaId = ponudaId;

        UcitajSifarnike();

        if (_ponudaId.HasValue)
        {
            Title = "Izmena ponude / predračuna";
            UcitajPonudu();
        }
        else
        {
            Title = "Nova ponuda / predračun";
            DpDatum.SelectedDate = DateTime.Today;
            DpRokVazenja.SelectedDate = DateTime.Today.AddDays(15);
        }

        DgStavke.ItemsSource = StavkeModels;
        OsveziSume();
    }

    private void UcitajSifarnike()
    {
        var partneri = _db.Partneri.AsNoTracking().Where(p => p.JeKupac || p.IsActive).OrderBy(p => p.Naziv).ToList();
        CmbPartner.ItemsSource = partneri;

        var artikli = _db.Artikli.AsNoTracking().OrderBy(a => a.SifraArtikla).ToList();
        ColArtikal.ItemsSource = artikli;
    }

    private void UcitajPonudu()
    {
        var p = _db.PonudePredracuni
            .Include(x => x.Stavke)
            .FirstOrDefault(x => x.PonudaPredracunId == _ponudaId!.Value);

        if (p == null) return;

        foreach (ComboBoxItem item in CmbVrsta.Items)
        {
            if ((string)item.Content == p.VrstaDokumenta) { CmbVrsta.SelectedItem = item; break; }
        }

        CmbPartner.SelectedValue = p.PartnerId;
        TxtBrojDokumenta.Text = p.BrojDokumenta;
        DpDatum.SelectedDate = p.Datum;
        DpRokVazenja.SelectedDate = p.RokVazenja;
        TxtNapomena.Text = p.Napomena;

        StavkeModels.Clear();
        foreach (var st in p.Stavke.OrderBy(s => s.RedniBroj))
        {
            StavkeModels.Add(new PonudaStavkaModel
            {
                ArtikalId = st.ArtikalId ?? 0,
                Kolicina = st.Kolicina,
                Cena = st.Cena,
                RabatProcenat = st.RabatProcenat,
                PdvStopa = st.PdvStopa
            });
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        var prviArtikal = _db.Artikli.AsNoTracking().FirstOrDefault();
        if (prviArtikal == null)
        {
            MessageBox.Show("Molimo unesite bar jedan artikal u šifarnik pre pravljenja dokumenta.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StavkeModels.Add(new PonudaStavkaModel
        {
            ArtikalId = prviArtikal.ArtikalId,
            Kolicina = 1,
            Cena = prviArtikal.ProdajnaCena,
            PdvStopa = prviArtikal.PdvStopa
        });

        OsveziSume();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is PonudaStavkaModel item)
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
            MessageBox.Show("Ponuda mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            PonudaPredracun p;
            if (_ponudaId.HasValue)
            {
                p = await new KomercijalaService(_db).GetPonudaByIdAsync(_ponudaId.Value)
                    ?? throw new InvalidOperationException("Ponuda više ne postoji.");
            }
            else
            {
                p = new PonudaPredracun();
            }

            p.VrstaDokumenta = (CmbVrsta.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ponuda";
            p.PartnerId = CmbPartner.SelectedValue as int?;
            p.Datum = DpDatum.SelectedDate ?? DateTime.Today;
            p.RokVazenja = DpRokVazenja.SelectedDate ?? DateTime.Today.AddDays(15);
            p.Napomena = TxtNapomena.Text.Trim();

            p.Stavke = StavkeModels.Select((s, idx) => new PonudaStavka
            {
                RedniBroj = idx + 1,
                ArtikalId = s.ArtikalId,
                Kolicina = s.Kolicina,
                Cena = s.Cena,
                RabatProcenat = s.RabatProcenat,
                PdvStopa = s.PdvStopa
            }).ToList();

            await new KomercijalaService(_db).SacuvajPonuduAsync(p);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju dokumenta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
