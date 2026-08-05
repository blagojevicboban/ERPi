using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Bilansi;

public partial class BilansStanjaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BilansService _service;

    public BilansStanjaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BilansService(_db);

        DpNaDatum.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async Task Ucitaj()
    {
        try
        {
            var pozicije = await _service.GetBilansStanjaAsync(DpNaDatum.SelectedDate);
            DgBilansStanja.ItemsSource = pozicije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju Bilansa Stanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
