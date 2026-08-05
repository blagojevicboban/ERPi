using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

public partial class NivelacijeView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<NivelacijaCena> _sveNivelacije = new();

    public NivelacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            var service = new NivelacijaService(_db);
            _sveNivelacije = await service.GetNivelacijeAsync(TxtPretraga.Text.Trim());
            DgNivelacije.ItemsSource = _sveNivelacije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju nivelacija cena: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajPodatke();
    }

    private void BtnNovaNivelacija_Click(object sender, RoutedEventArgs e)
    {
        var win = new NivelacijaEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajPodatke();
        }
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        OtvoriIzabranu();
    }

    private void DgNivelacije_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtvoriIzabranu();
    }

    private void OtvoriIzabranu()
    {
        if (DgNivelacije.SelectedItem is NivelacijaCena selektovana)
        {
            var win = new NivelacijaEditWindow(_db, selektovana) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajPodatke();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena selektovana)
        {
            MessageBox.Show("Izaberite nivelaciju koju želite proknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana nivelacija je već proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Knjiži nivelaciju cena br. {selektovana.BrojNivelacije}?\nKnjiženje ažurira prodajne cene artikala i kreira nalog u Glavnoj knjizi.", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new NivelacijaService(_db);
            await service.KnjiziNivelacijuAsync(selektovana.NivelacijaCenaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena selektovana)
        {
            MessageBox.Show("Izaberite nivelaciju koju želite rasknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana nivelacija nije proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Rasknjiži nivelaciju cena br. {selektovana.BrojNivelacije}?\nRasknjižavanje vraća prodajne cene artikala i briše nalog u Glavnoj knjizi.", "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new NivelacijaService(_db);
            await service.RasknjiziNivelacijuAsync(selektovana.NivelacijaCenaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
