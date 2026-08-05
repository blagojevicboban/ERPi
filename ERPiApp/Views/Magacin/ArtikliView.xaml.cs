using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class ArtikliView : UserControl
{
    private readonly ErpiDbContext _db;

    public ArtikliView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajArtikle();
    }

    public void UcitajArtikle()
    {
        var query = _db.Artikli.AsNoTracking().AsQueryable();
        var text = TxtPretraga?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(a => a.SifraArtikla.Contains(text) || a.Naziv.Contains(text) || (a.Barkod != null && a.Barkod.Contains(text)));
        }

        DgArtikli.ItemsSource = query.OrderBy(a => a.SifraArtikla).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajArtikle();
    }

    private void DgArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgArtikli.SelectedItem is Artikal;
        BtnIzmeniArtikal.IsEnabled = selektovan;
        BtnObrisiArtikal.IsEnabled = selektovan;
    }

    private void DgArtikli_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void BtnNovArtikal_Click(object sender, RoutedEventArgs e)
    {
        var win = new ArtikalEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajArtikle();
        }
    }

    private void BtnIzmeniArtikal_Click(object sender, RoutedEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void OtveriZaIzmenu()
    {
        if (DgArtikli.SelectedItem is Artikal artikal)
        {
            var win = new ArtikalEditWindow(_db, artikal.ArtikalId) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajArtikle();
            }
        }
    }

    private void BtnObrisiArtikal_Click(object sender, RoutedEventArgs e)
    {
        if (DgArtikli.SelectedItem is Artikal artikal)
        {
            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete artikal '{artikal.Naziv}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var a = _db.Artikli.Find(artikal.ArtikalId);
                    if (a != null)
                    {
                        _db.Artikli.Remove(a);
                        _db.SaveChanges();
                        UcitajArtikle();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju artikla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
