using System;
using System.Windows;
using ERPiData;
using ERPiData.Services;

namespace ERPiApp.Views.SefPfr;

public partial class SefUlazneFaktureWindow : Window
{
    private readonly ErpiDbContext _db;

    public SefUlazneFaktureWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        DpOdDatuma.SelectedDate = DateTime.Today.AddDays(-30);
        Loaded += (s, e) => UcitajUlazneFakture();
    }

    private async void UcitajUlazneFakture()
    {
        try
        {
            TxtStatus.Text = "Preuzimanje faktura sa SEF-a...";
            DateTime odDatuma = DpOdDatuma.SelectedDate ?? DateTime.Today.AddDays(-30);

            var service = new SefService(_db);
            var res = await service.PreuzmiUlazneFaktureAsync(odDatuma);
            if (res.Success && res.Data != null)
            {
                DgUlazneFakture.ItemsSource = res.Data;
                TxtStatus.Text = $"Preuzeto {res.Data.Count} ulaznih e-faktura od datuma {odDatuma:dd.MM.yyyy}.";
            }
            else
            {
                TxtStatus.Text = $"Greška: {res.Message}";
                MessageBox.Show(res.Message, "SEF Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Greška pri preuzimanju.";
            MessageBox.Show($"Greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajUlazneFakture();
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
