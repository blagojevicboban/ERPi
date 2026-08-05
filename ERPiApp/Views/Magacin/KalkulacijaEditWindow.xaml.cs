using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class KalkulacijaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _kalkulacijaId;
    public ObservableCollection<StavkaKalkulacijeModel> StavkeModels { get; set; } = new();

    public class StavkaKalkulacijeModel
    {
        public int ArtikalId { get; set; }
        public decimal Kolicina { get; set; } = 1.00m;
        public decimal NabavnaCena { get; set; }
        public decimal RabatProcenat { get; set; }
        public decimal MarzaProcenat { get; set; }
        public decimal PdvStopa { get; set; } = 20.00m;
        public decimal ProdajnaCena { get; set; }
        public decimal IznosNabavni => Math.Round(Kolicina * NabavnaCena * (1.00m - (RabatProcenat / 100.00m)), 2);
        public decimal IznosPdv => Math.Round(IznosNabavni * (PdvStopa / 100.00m), 2);
        public decimal IznosProdajni => Math.Round(Kolicina * ProdajnaCena, 2);
    }

    public KalkulacijaEditWindow(ErpiDbContext db, int? kalkulacijaId)
    {
        InitializeComponent();
        _db = db;
        _kalkulacijaId = kalkulacijaId;

        UcitajSifarnike();

        if (_kalkulacijaId.HasValue)
        {
            Title = "Izmena ulazne kalkulacije";
            UcitajKalkulaciju();
        }
        else
        {
            Title = "Nova ulazna kalkulacija";
            DpDatum.SelectedDate = DateTime.Now;
            GenerisiNoviBroj();
        }

        DgStavke.ItemsSource = StavkeModels;
        OsveziSume();
    }

    private void UcitajSifarnike()
    {
        var magacini = _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacin.ItemsSource = magacini;
        if (magacini.Any()) CmbMagacin.SelectedIndex = 0;

        var partneri = _db.Partneri.AsNoTracking().Where(p => p.JeDobavljac || p.IsActive).OrderBy(p => p.Naziv).ToList();
        CmbPartner.ItemsSource = partneri;

        var artikli = _db.Artikli.AsNoTracking().OrderBy(a => a.SifraArtikla).ToList();
        ColArtikal.ItemsSource = artikli;
    }

    private void GenerisiNoviBroj()
    {
        var max = _db.Kalkulacije.Max(k => (int?)k.BrojKalkulacije) ?? 0;
        TxtBrojKalkulacije.Text = (max + 1).ToString();
    }

    private void UcitajKalkulaciju()
    {
        var k = _db.Kalkulacije
            .Include(x => x.Stavke)
            .FirstOrDefault(x => x.KalkulacijaId == _kalkulacijaId!.Value);

        if (k == null) return;

        CmbMagacin.SelectedValue = k.MagacinId;
        CmbPartner.SelectedValue = k.PartnerId;
        TxtBrojKalkulacije.Text = k.BrojKalkulacije.ToString();
        DpDatum.SelectedDate = k.Datum;
        TxtBrojFakture.Text = k.BrojFaktureDobavljaca;
        TxtNapomena.Text = k.Napomena;

        StavkeModels.Clear();
        foreach (var st in k.Stavke)
        {
            StavkeModels.Add(new StavkaKalkulacijeModel
            {
                ArtikalId = st.ArtikalId,
                Kolicina = st.Kolicina,
                NabavnaCena = st.NabavnaCena,
                RabatProcenat = st.RabatProcenat,
                MarzaProcenat = st.MarzaProcenat,
                PdvStopa = st.PdvStopa,
                ProdajnaCena = st.ProdajnaCena
            });
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        var prviArtikal = _db.Artikli.AsNoTracking().FirstOrDefault();
        if (prviArtikal == null)
        {
            MessageBox.Show("Molimo unesite bar jedan artikal u šifarnik pre pravljenja kalkulacije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StavkeModels.Add(new StavkaKalkulacijeModel
        {
            ArtikalId = prviArtikal.ArtikalId,
            Kolicina = 1,
            NabavnaCena = prviArtikal.NabavnaCena,
            ProdajnaCena = prviArtikal.ProdajnaCena,
            PdvStopa = prviArtikal.PdvStopa
        });

        OsveziSume();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is StavkaKalkulacijeModel item)
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
        decimal sumNabavni = 0;
        decimal sumPdv = 0;
        decimal sumProdajni = 0;

        foreach (var s in StavkeModels)
        {
            if (s.ProdajnaCena == 0 && s.NabavnaCena > 0 && s.MarzaProcenat > 0)
            {
                s.ProdajnaCena = Math.Round(s.NabavnaCena * (1 + (s.MarzaProcenat / 100m)), 2);
            }

            sumNabavni += s.IznosNabavni;
            sumPdv += s.IznosPdv;
            sumProdajni += s.IznosProdajni;
        }

        TxtZbirNabavni.Text = $"{sumNabavni:N2} RSD";
        TxtZbirPdv.Text = $"{sumPdv:N2} RSD";
        TxtZbirProdajni.Text = $"{sumProdajni:N2} RSD";
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMagacin.SelectedValue is not int magacinId || magacinId <= 0)
        {
            MessageBox.Show("Molimo izaberite magacin.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!StavkeModels.Any())
        {
            MessageBox.Show("Kalkulacija mora imati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtBrojKalkulacije.Text, out var brojKalk))
        {
            MessageBox.Show("Nevažeći broj kalkulacije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Kalkulacija k;
            if (_kalkulacijaId.HasValue)
            {
                k = _db.Kalkulacije.Include(x => x.Stavke).FirstOrDefault(x => x.KalkulacijaId == _kalkulacijaId.Value)!;
                _db.StavkeKalkulacije.RemoveRange(k.Stavke);
            }
            else
            {
                k = new Kalkulacija();
                _db.Kalkulacije.Add(k);
            }

            k.MagacinId = magacinId;
            k.PartnerId = CmbPartner.SelectedValue as int?;
            k.BrojKalkulacije = brojKalk;
            k.Datum = DpDatum.SelectedDate ?? DateTime.Now;
            k.BrojFaktureDobavljaca = TxtBrojFakture.Text.Trim();
            k.Napomena = TxtNapomena.Text.Trim();
            k.VrstaKalkulacije = "Ulazna";

            k.UkupnoNabavna = StavkeModels.Sum(s => s.IznosNabavni);
            k.UkupnoPdv = StavkeModels.Sum(s => s.IznosPdv);
            k.UkupnoProdajna = StavkeModels.Sum(s => s.IznosProdajni);

            k.Stavke = StavkeModels.Select(s => new StavkaKalkulacije
            {
                ArtikalId = s.ArtikalId,
                Kolicina = s.Kolicina,
                NabavnaCena = s.NabavnaCena,
                RabatProcenat = s.RabatProcenat,
                MarzaProcenat = s.MarzaProcenat,
                PdvStopa = s.PdvStopa,
                ProdajnaCena = s.ProdajnaCena,
                IznosNabavni = s.IznosNabavni,
                IznosPdv = s.IznosPdv,
                IznosProdajni = s.IznosProdajni
            }).ToList();

            _db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
