using System;
using System.Windows;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Partneri;

public partial class IstorijaZatvaranjaWindow : Window
{
    private readonly ZatvaranjeStavkiService _service;
    private readonly int _partnerId;

    public bool NestoOtkazano { get; private set; }

    public IstorijaZatvaranjaWindow(ZatvaranjeStavkiService service, int partnerId)
    {
        InitializeComponent();
        _service = service;
        _partnerId = partnerId;
        TxtNaslov.Text = "🕘 Istorija zatvaranja stavki";
        LoadIstoriju();
    }

    private async void LoadIstoriju()
    {
        try
        {
            DgIstorija.ItemsSource = await _service.GetIstorijaZatvaranjaAsync(_partnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju istorije zatvaranja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOtkaziZatvaranje_Click(object sender, RoutedEventArgs e)
    {
        if (DgIstorija.SelectedItem is not ZatvaranjeStavke zatvaranje)
        {
            MessageBox.Show("Izaberite zatvaranje koje želite da otkažete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Otkazati zatvaranje u iznosu {zatvaranje.Iznos:N2} RSD od {zatvaranje.DatumZatvaranja:dd.MM.yyyy}?",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            await _service.OtkaziZatvaranjeAsync(zatvaranje.ZatvaranjeStavkeId, AppSession.TrenutniKorisnik?.KorisnikId, AppSession.TrenutniKorisnik?.KorisnickoIme);
            NestoOtkazano = true;
            LoadIstoriju();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otkazivanju zatvaranja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
