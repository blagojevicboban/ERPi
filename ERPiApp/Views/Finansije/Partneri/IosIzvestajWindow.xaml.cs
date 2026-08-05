using System.Windows;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class IosIzvestajWindow : Window
{
    private readonly ZatvaranjeStavkiService _service;

    public IosIzvestajWindow(ZatvaranjeStavkiService service)
    {
        InitializeComponent();
        _service = service;
        DpNaDan.SelectedDate = DateTime.Now;
        _ = Osvezi();
    }

    private async void BtnOsvezi_Click(object sender, RoutedEventArgs e) => await Osvezi();

    private async Task Osvezi()
    {
        var naDan = DpNaDan.SelectedDate ?? DateTime.Now;
        var kontoPrefix = string.IsNullOrWhiteSpace(TxtKontoPrefix.Text) ? null : TxtKontoPrefix.Text.Trim();
        var samoOtvorene = ChkSamoOtvorene.IsChecked == true;

        DgPartneri.ItemsSource = await _service.GetIosIzvestajAsync(naDan, kontoPrefix, samoOtvorene);
        DgStavke.ItemsSource = null;
        TxtStavkeNaslov.Text = "📋 Stavke";
    }

    private void DgPartneri_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DgPartneri.SelectedItem is not IosPartnerGrupa grupa)
        {
            DgStavke.ItemsSource = null;
            TxtStavkeNaslov.Text = "📋 Stavke";
            return;
        }

        TxtStavkeNaslov.Text = $"📋 Stavke — {grupa.NazivPartnera}";
        DgStavke.ItemsSource = grupa.Stavke;
    }
}
