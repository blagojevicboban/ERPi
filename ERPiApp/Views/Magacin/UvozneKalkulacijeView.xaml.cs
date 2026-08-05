using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

public partial class UvozneKalkulacijeView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<UvoznaKalkulacija> _sviUvozi = new();

    public UvozneKalkulacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            var service = new UvoznaKalkulacijaService(_db);
            _sviUvozi = await service.GetKalkulacijeAsync(TxtPretraga.Text.Trim());
            DgUvoz.ItemsSource = _sviUvozi;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju uvoznih kalkulacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajPodatke();
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgUvoz.SelectedItem is not UvoznaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite uvoznu kalkulaciju koju želite proknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana uvozna kalkulacija je već proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Knjiži uvoznu kalkulaciju #{selektovana.BrojKalkulacije}?\nKnjiženje kreira nalog uvoza u Glavnoj knjizi.", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new UvoznaKalkulacijaService(_db);
            await service.KnjiziUvozAsync(selektovana.UvoznaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju uvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgUvoz.SelectedItem is not UvoznaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite uvoznu kalkulaciju koju želite rasknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana uvozna kalkulacija nije proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Rasknjiži uvoznu kalkulaciju #{selektovana.BrojKalkulacije}?\nRasknjižavanje briše nalog uvoza u Glavnoj knjizi.", "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new UvoznaKalkulacijaService(_db);
            await service.RasknjiziUvozAsync(selektovana.UvoznaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju uvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
