using System.Globalization;
using System.Windows;
using ERPiApp.Views.Shell;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class ZatvoriStavkeWindow : Window
{
    private readonly ZatvaranjeStavkiService _service;

    public ZatvoriStavkeWindow(ZatvaranjeStavkiService service, List<OtvorenaStavkaRed> otvoreneStavke)
    {
        InitializeComponent();
        _service = service;

        DgDuguje.ItemsSource = otvoreneStavke.Where(s => s.Strana == "Duguje" && s.Preostalo > 0.01m).ToList();
        DgPotrazuje.ItemsSource = otvoreneStavke.Where(s => s.Strana == "Potrazuje" && s.Preostalo > 0.01m).ToList();
    }

    private void Selekcija_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DgDuguje.SelectedItem is OtvorenaStavkaRed d && DgPotrazuje.SelectedItem is OtvorenaStavkaRed p)
        {
            var predlog = Math.Min(d.Preostalo, p.Preostalo);
            TxtIznos.Text = predlog.ToString("N2", CultureInfo.CurrentCulture);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (DgDuguje.SelectedItem is not OtvorenaStavkaRed duguje || DgPotrazuje.SelectedItem is not OtvorenaStavkaRed potrazuje)
        {
            ShowError("Izaberite po jednu stavku sa obe strane.");
            return;
        }

        if (!decimal.TryParse(TxtIznos.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var iznos) || iznos <= 0)
        {
            ShowError("Unesite ispravan iznos veći od 0.");
            return;
        }

        try
        {
            await _service.ZatvoriAsync(
                duguje.StavkaNalogaId, potrazuje.StavkaNalogaId, iznos, DateTime.Now,
                korisnikId: AppSession.TrenutniKorisnik?.KorisnikId);

            DialogResult = true;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
