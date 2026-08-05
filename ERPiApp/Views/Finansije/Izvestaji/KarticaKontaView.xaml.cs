using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Izvestaji;

public partial class KarticaKontaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly KarticaService _service;
    private List<Konto> _svaKonta = new();

    public KarticaKontaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KarticaService(_db);
        DpKarticaOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpKarticaDo.SelectedDate = DateTime.Today;

        // Ucitavanje mora cekati Loaded, ne pozvati se direktno iz konstruktora: LoadKonta
        // postavlja LstKonta.SelectedIndex = 0, sto sinhrono okida SelectionChanged. Ako se
        // await unutra ikad zavrsi sinhrono (npr. _db je vec otvoren/topao, za razliku od
        // ERPiFinansije koje uvek otvara svezu konekciju po ekranu), ceo lanac LoadKonta ->
        // ApplyFilter -> SelectedIndex -> SelectionChanged -> UcitajKarticu odradi se JOS
        // UNUTAR konstruktora, pre nego sto je ovaj UserControl uopste dodat u vizuelno
        // stablo (MainContentHost.Content = new KarticaKontaView(...) jos nije zavrsio) —
        // DataGrid tada NullReferenceException-uje interno na SelectedIndex. Isti obrazac kao
        // NaloziView-ov RadioButton.IsChecked slucaj, samo za DataGrid selekciju. Ostali
        // ekrani sa "izaberi prvi red" (KompenzacijeView, PutniNaloziView) vec koriste ovaj
        // Loaded obrazac — ne vracati na direktan poziv.
        Loaded += (_, _) => LoadKonta();
    }

    private async void LoadKonta()
    {
        try
        {
            bool samoSaPrometom = ChkSamoSaPrometom?.IsChecked ?? true;
            _svaKonta = await _service.GetKontaAsync(samoSaPrometom);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        LoadKonta();
    }

    private void ApplyFilter()
    {
        if (LstKonta == null) return;

        string search = TxtPretragaKonta?.Text.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(search)
            ? _svaKonta
            : _svaKonta.Where(k => k.BrojKonta.ToLower().Contains(search) || k.NazivKonta.ToLower().Contains(search)).ToList();

        LstKonta.ItemsSource = filtered;
        if (filtered.Any())
        {
            LstKonta.SelectedIndex = 0;
        }
        else
        {
            DgKartica.ItemsSource = null;
            TxtNaslovKonta.Text = "Nema konta za prikaz";
            TxtPodnaslovKonta.Text = "";
            TxtSumaDuguje.Text = "0,00";
            TxtSumaPotrazuje.Text = "0,00";
            TxtSumaSaldo.Text = "0,00";
        }
    }

    private void TxtPretragaKonta_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private async void LstKonta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstKonta.SelectedItem is not Konto konto) return;

        TxtNaslovKonta.Text = $"{konto.BrojKonta} — {konto.NazivKonta}";
        TxtPodnaslovKonta.Text = konto.IsSintetika ? "Sintetički konto" : "Analitički konto";

        await UcitajKarticu();
    }

    private async void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        await UcitajKarticu();
    }

    private async Task UcitajKarticu()
    {
        if (LstKonta.SelectedItem is not Konto konto) return;

        try
        {
            var kartica = await _service.GetKarticaKontaAsync(konto.BrojKonta, DpKarticaOd.SelectedDate, DpKarticaDo.SelectedDate);
            DgKartica.ItemsSource = kartica;
            TxtSumaDuguje.Text = kartica.Sum(r => r.Duguje).ToString("N2");
            TxtSumaPotrazuje.Text = kartica.Sum(r => r.Potrazuje).ToString("N2");
            TxtSumaSaldo.Text = (kartica.Count > 0 ? kartica[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
