using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Konta;

public partial class KontaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly KontaService _service;
    private List<Konto> _allKonta = new();

    public KontaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KontaService(_db);
        LoadKonta();
    }

    private async void LoadKonta()
    {
        try
        {
            _allKonta = await _service.GetKontaAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        if (DgKonta == null) return;

        string search = TxtPretraga.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(search)
            ? _allKonta
            : _allKonta.Where(k => k.BrojKonta.ToLower().Contains(search) || k.NazivKonta.ToLower().Contains(search)).ToList();

        DgKonta.ItemsSource = filtered;
        UpdateActionButtonsState();
    }

    private void DgKonta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtonsState();
    }

    private void UpdateActionButtonsState()
    {
        var count = DgKonta?.SelectedItems?.Count ?? 0;
        if (BtnIzmeniKonto != null) BtnIzmeniKonto.IsEnabled = count > 0;
        if (BtnObrisiKonto != null) BtnObrisiKonto.IsEnabled = count > 0;
        if (CmiIzmeniKonto != null) CmiIzmeniKonto.IsEnabled = count > 0;
        if (CmiObrisiKonto != null) CmiObrisiKonto.IsEnabled = count > 0;
    }

    private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
        {
            row.Focus();
            if (!row.IsSelected)
            {
                row.IsSelected = true;
            }
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnNoviKonto_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KontoEditWindow(_db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKonta();
        }
    }

    private void BtnIzmeniKonto_Click(object sender, RoutedEventArgs e)
    {
        var selectedKonto = DgKonta.SelectedItems.OfType<Konto>().FirstOrDefault();
        if (selectedKonto == null)
        {
            MessageBox.Show("Izaberite konto za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OtvoriIzmenuKonta(selectedKonto);
    }

    private void DgKonta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) == null) return;
        if (DgKonta.SelectedItem is not Konto selectedKonto) return;

        OtvoriIzmenuKonta(selectedKonto);
    }

    private void OtvoriIzmenuKonta(Konto konto)
    {
        var dijalog = new KontoEditWindow(_db, konto) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKonta();
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match) return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void BtnObrisiKonto_Click(object sender, RoutedEventArgs e)
    {
        var selectedKonta = DgKonta.SelectedItems.OfType<Konto>().ToList();
        if (!selectedKonta.Any())
        {
            MessageBox.Show("Izaberite konto za brisanje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string poruka = selectedKonta.Count == 1
            ? $"Da li ste sigurni da želite da obrišete konto {selectedKonta[0].BrojKonta} ({selectedKonta[0].NazivKonta})?"
            : $"Da li ste sigurni da želite da obrišete {selectedKonta.Count} izabranih konta?";

        var potvrda = MessageBox.Show(
            poruka,
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var ids = selectedKonta.Select(k => k.KontoId);
            int obrisanoCount = await _service.DeleteKontaAsync(ids);

            string uspehPoruka = obrisanoCount == 1
                ? $"Konto {selectedKonta[0].BrojKonta} je uspešno obrisan."
                : $"Uspešno je obrisano {obrisanoCount} konta.";

            MessageBox.Show(uspehPoruka, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadKonta();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
