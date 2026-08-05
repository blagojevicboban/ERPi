using System.Globalization;
using System.Windows;
using ERPiData.Models.Core;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class KamataWindow : Window
{
    private readonly Partner _partner;
    private readonly KamataService _service;
    private List<KamataStavka> _poslednjiObracun = new();

    public KamataWindow(KamataService service, Partner partner)
    {
        InitializeComponent();
        _service = service;
        _partner = partner;
        TxtNaslovPartnera.Text = $"💰 Obračun kamate — {partner.Naziv}";
        DpDatumObracuna.SelectedDate = DateTime.Now;
        DpNovaStopaOd.SelectedDate = DateTime.Now;

        LoadStope();
    }

    private async void LoadStope()
    {
        try
        {
            DgStope.ItemsSource = await _service.GetStopeAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Greška pri učitavanju kamatnih stopa: {ex.Message}");
        }
    }

    private async void BtnDodajStopu_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtNovaStopaProcenat.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var stopa))
        {
            ShowError("Unesite ispravnu vrednost stope (npr. 8,5).");
            return;
        }

        if (DpNovaStopaOd.SelectedDate is not DateTime datumOd)
        {
            ShowError("Izaberite datum od kada stopa važi.");
            return;
        }

        try
        {
            await _service.DodajStopuAsync(datumOd, stopa, "Ručno uneta stopa");
            TxtNovaStopaProcenat.Text = string.Empty;
            LoadStope();
        }
        catch (Exception ex)
        {
            ShowError($"Greška pri dodavanju stope: {ex.Message}");
        }
    }

    private async void BtnObracunaj_Click(object sender, RoutedEventArgs e)
    {
        if (DpDatumObracuna.SelectedDate is not DateTime datumObracuna)
        {
            ShowError("Izaberite datum obračuna.");
            return;
        }

        try
        {
            _poslednjiObracun = await _service.ObracunajKamatuAsync(_partner.PartnerId, datumObracuna);
            DgKamata.ItemsSource = _poslednjiObracun;
            TxtUkupnaKamata.Text = _poslednjiObracun.Sum(k => k.ObracunataKamata).ToString("N2", CultureInfo.CurrentCulture);
            TxtError.Visibility = Visibility.Collapsed;

            if (_poslednjiObracun.Count == 0)
            {
                MessageBox.Show("Nema dugovnih otvorenih stavki sa kašnjenjem na zadati datum obračuna.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Greška pri obračunu kamate: {ex.Message}");
        }
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (_poslednjiObracun.Count == 0)
        {
            ShowError("Prvo izvršite obračun kamate (dugme \"Obračunaj\").");
            return;
        }

        var ukupnaKamata = _poslednjiObracun.Sum(k => k.ObracunataKamata);
        var potvrda = MessageBox.Show(
            $"Proknjižiti obračunatu kamatu od {ukupnaKamata:N2} RSD u glavnu knjigu?",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var datumObracuna = DpDatumObracuna.SelectedDate ?? DateTime.Now;
            await _service.ProknjiziKamatuNalogAsync(_partner.PartnerId, ukupnaKamata, datumObracuna,
                opis: null);

            MessageBox.Show("Kamata je proknjižena u glavnu knjigu.", "Gotovo", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void BtnZatvoriProzor_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
