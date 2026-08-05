using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Bilansi;

public partial class BilansUspehaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BilansService _service;

    public BilansUspehaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BilansService(_db);

        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async Task Ucitaj()
    {
        try
        {
            var pozicije = await _service.GetBilansUspehaAsync(DpOd.SelectedDate, DpDo.SelectedDate);
            DgBilansUspeha.ItemsSource = pozicije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju Bilansa Uspeha: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
