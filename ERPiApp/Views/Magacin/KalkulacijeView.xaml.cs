using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class KalkulacijeView : UserControl
{
    private readonly ErpiDbContext _db;

    public KalkulacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajMagacine();
        UcitajKalkulacije();
    }

    private void UcitajMagacine()
    {
        var magacini = _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToList();
        magacini.Insert(0, new ERPiData.Models.Magacin.Magacin { MagacinId = 0, SifraMagacina = "", NazivMagacina = "-- Svi magacini --" });
        CmbMagacinFilter.ItemsSource = magacini;
        CmbMagacinFilter.SelectedIndex = 0;
    }

    public void UcitajKalkulacije()
    {
        var query = _db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Partner)
            .AsNoTracking()
            .AsQueryable();

        if (CmbMagacinFilter.SelectedValue is int magacinId && magacinId > 0)
        {
            query = query.Where(k => k.MagacinId == magacinId);
        }

        var text = TxtPretraga?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(k => k.BrojKalkulacije.ToString().Contains(text) || (k.BrojFaktureDobavljaca != null && k.BrojFaktureDobavljaca.Contains(text)) || (k.Partner != null && k.Partner.Naziv.Contains(text)));
        }

        DgKalkulacije.ItemsSource = query.OrderByDescending(k => k.Datum).ThenByDescending(k => k.BrojKalkulacije).ToList();
    }

    private void CmbMagacinFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UcitajKalkulacije();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajKalkulacije();
    }

    private void DgKalkulacije_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgKalkulacije.SelectedItem is Kalkulacija;
        BtnIzmeniKalkulaciju.IsEnabled = selektovan;
        BtnObrisiKalkulaciju.IsEnabled = selektovan;
    }

    private void DgKalkulacije_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void BtnNovaKalkulacija_Click(object sender, RoutedEventArgs e)
    {
        var win = new KalkulacijaEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajKalkulacije();
        }
    }

    private void BtnIzmeniKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void OtveriZaIzmenu()
    {
        if (DgKalkulacije.SelectedItem is Kalkulacija selektovana)
        {
            var puna = _db.Kalkulacije
                .Include(k => k.Magacin)
                .Include(k => k.Partner)
                .Include(k => k.KontoDobavljaca)
                .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
                .FirstOrDefault(k => k.KalkulacijaId == selektovana.KalkulacijaId);

            if (puna != null)
            {
                var win = new KalkulacijaEditWindow(_db, puna) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true)
                {
                    UcitajKalkulacije();
                }
            }
        }
    }

    private void BtnObrisiKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is Kalkulacija kalkulacija)
        {
            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete kalkulaciju br. {kalkulacija.BrojKalkulacije}?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var k = _db.Kalkulacije.Find(kalkulacija.KalkulacijaId);
                    if (k != null)
                    {
                        _db.Kalkulacije.Remove(k);
                        _db.SaveChanges();
                        UcitajKalkulacije();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            MessageBox.Show($"Priprema PDF štampanog dokumenta za kalkulaciju br. {selektovana.BrojKalkulacije}...", "PDF Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampanog dokumenta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgKalkulacije, "Ulazne_Kalkulacije", "Kalkulacije");
}
