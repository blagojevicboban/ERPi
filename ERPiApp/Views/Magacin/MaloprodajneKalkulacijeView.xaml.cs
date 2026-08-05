using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

public partial class MaloprodajneKalkulacijeView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<MaloprodajnaKalkulacija> _sveKalkulacije = new();

    public MaloprodajneKalkulacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            _sveKalkulacije = await service.GetKalkulacijeAsync(TxtPretraga.Text.Trim());
            DgKalkulacije.ItemsSource = _sveKalkulacije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju maloprodajnih kalkulacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajPodatke();
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju koju želite proknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana kalkulacija je već proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Knjiži maloprodajnu kalkulaciju br. {selektovana.BrojKalkulacije}?\nKnjiženje upisuje robu u prodavnicu i kreira nalog u Glavnoj knjizi.", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            await service.KnjiziKalkulacijuAsync(selektovana.MaloprodajnaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju koju želite rasknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana kalkulacija nije proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Rasknjiži maloprodajnu kalkulaciju br. {selektovana.BrojKalkulacije}?\nRasknjižavanje poništava zalihe na kartici prodavnice i briše nalog u Glavnoj knjizi.", "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            await service.RasknjiziKalkulacijuAsync(selektovana.MaloprodajnaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
