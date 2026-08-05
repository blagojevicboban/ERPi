using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Izvestaji;

public partial class BrutoBilansView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly BrutoBilansService _service;

    public BrutoBilansView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BrutoBilansService(_db);

        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await Ucitaj();
    }

    private async void BtnUcitaj_Click(object sender, RoutedEventArgs e) => await Ucitaj();

    private async Task Ucitaj()
    {
        try
        {
            DateTime? odD = DpOd.SelectedDate;
            DateTime? doD = DpDo.SelectedDate;
            int? klasa = CmbKlasa.SelectedIndex > 0 ? CmbKlasa.SelectedIndex - 1 : null;

            bool saTotalima = ChkSaTotalima.IsChecked == true;

            List<BrutoBilansRed> redovi = saTotalima
                ? await _service.GetBrutoBilansSaTotalimaAsync(odD, doD, klasa)
                : await _service.GetBrutoBilansAsync(odD, doD, klasa);

            DgBrutoBilans.ItemsSource = redovi;

            var detaljni = redovi.Where(r => r.Tip == BrutoBilansRedTip.Detalj).ToList();
            TxtUkupnoDuguje.Text = detaljni.Sum(r => r.Duguje).ToString("N2");
            TxtUkupnoPotrazuje.Text = detaljni.Sum(r => r.Potrazuje).ToString("N2");
            TxtUkupnoSaldoDuguje.Text = detaljni.Sum(r => r.SaldoDuguje).ToString("N2");
            TxtUkupnoSaldoPotrazuje.Text = detaljni.Sum(r => r.SaldoPotrazuje).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju bruto bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
