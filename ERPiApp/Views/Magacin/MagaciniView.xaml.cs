using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class MagaciniView : UserControl
{
    private readonly ErpiDbContext _db;

    public MagaciniView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajMagacine();
    }

    public void UcitajMagacine()
    {
        var query = _db.Magacini.AsNoTracking().AsQueryable();
        var text = TxtPretraga?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(m => m.SifraMagacina.Contains(text) || m.NazivMagacina.Contains(text));
        }

        DgMagacini.ItemsSource = query.OrderBy(m => m.SifraMagacina).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajMagacine();
    }

    private void DgMagacini_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgMagacini.SelectedItem is ERPiData.Models.Magacin.Magacin;
        BtnIzmeniMagacin.IsEnabled = selektovan;
        BtnObrisiMagacin.IsEnabled = selektovan;
    }

    private void DgMagacini_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void BtnNoviMagacin_Click(object sender, RoutedEventArgs e)
    {
        var win = new MagacinEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajMagacine();
        }
    }

    private void BtnIzmeniMagacin_Click(object sender, RoutedEventArgs e)
    {
        OtveriZaIzmenu();
    }

    private void OtveriZaIzmenu()
    {
        if (DgMagacini.SelectedItem is ERPiData.Models.Magacin.Magacin magacin)
        {
            var win = new MagacinEditWindow(_db, magacin.MagacinId) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajMagacine();
            }
        }
    }

    private void BtnObrisiMagacin_Click(object sender, RoutedEventArgs e)
    {
        if (DgMagacini.SelectedItem is ERPiData.Models.Magacin.Magacin magacin)
        {
            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete magacin '{magacin.NazivMagacina}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var m = _db.Magacini.Find(magacin.MagacinId);
                    if (m != null)
                    {
                        _db.Magacini.Remove(m);
                        _db.SaveChanges();
                        UcitajMagacine();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju magacina: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
