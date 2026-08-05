using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using ERPiData;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

public partial class ArtikalEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _artikalId;

    public ArtikalEditWindow(ErpiDbContext db, int? artikalId)
    {
        InitializeComponent();
        _db = db;
        _artikalId = artikalId;

        if (_artikalId.HasValue)
        {
            Title = "Izmena artikla";
            UcitajData();
        }
        else
        {
            Title = "Nov artikal";
        }
    }

    private void UcitajData()
    {
        var a = _db.Artikli.Find(_artikalId!.Value);
        if (a != null)
        {
            TxtSifra.Text = a.SifraArtikla;
            TxtNaziv.Text = a.Naziv;
            TxtJM.Text = a.JedinicaMere;
            TxtBarkod.Text = a.Barkod;
            TxtNabavnaCena.Text = a.NabavnaCena.ToString("N2", CultureInfo.InvariantCulture);
            TxtProdajnaCena.Text = a.ProdajnaCena.ToString("N2", CultureInfo.InvariantCulture);
            TxtPdvStopa.Text = a.PdvStopa.ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        var sifra = TxtSifra.Text.Trim();
        var naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Šifra artikla je obavezna.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Naziv artikla je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtNabavnaCena.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var nabavna))
        {
            MessageBox.Show("Nevažeća nabavna cena.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtProdajnaCena.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var prodajna))
        {
            MessageBox.Show("Nevažeća prodajna cena.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPdvStopa.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var pdv))
        {
            MessageBox.Show("Nevažeća PDV stopa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dup = _db.Artikli.FirstOrDefault(a => a.SifraArtikla == sifra && a.ArtikalId != (_artikalId ?? 0));
        if (dup != null)
        {
            MessageBox.Show($"Artikal sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Artikal artikal;
            if (_artikalId.HasValue)
            {
                artikal = _db.Artikli.Find(_artikalId.Value)!;
            }
            else
            {
                artikal = new Artikal();
                _db.Artikli.Add(artikal);
            }

            artikal.SifraArtikla = sifra;
            artikal.Naziv = naziv;
            artikal.JedinicaMere = TxtJM.Text.Trim();
            artikal.Barkod = TxtBarkod.Text.Trim();
            artikal.NabavnaCena = nabavna;
            artikal.ProdajnaCena = prodajna;
            artikal.PdvStopa = pdv;

            _db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju artikla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
