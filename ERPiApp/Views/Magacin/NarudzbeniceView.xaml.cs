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

/// <summary>
/// Narudžbenice dobavljačima — lista + 1-klik konverzija u ulaznu Kalkulaciju. Port iz
/// ERPiFinansijeApp/Views/Trgovina/TrgovinaView.xaml ("Narudžbenice Dobavljačima" tab), vidi
/// PLAN_NASTAVKA.md §3i.
/// </summary>
public partial class NarudzbeniceView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<NarudzbenicaDobavljacu> _sveNarudzbenice = new();

    public NarudzbeniceView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        // Loaded, ne direktan poziv iz konstruktora — vidi napomenu u PonudeView/§2 PLAN_NASTAVKA.md.
        Loaded += (_, _) => UcitajNarudzbenice();
    }

    public async void UcitajNarudzbenice()
    {
        try
        {
            _sveNarudzbenice = await new KomercijalaService(_db).GetNarudzbeniceAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju narudžbenica: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filtriraj()
    {
        string search = TxtPretraga?.Text?.Trim().ToLower() ?? "";
        var filtrirano = _sveNarudzbenice.Where(n =>
            string.IsNullOrEmpty(search) ||
            n.BrojNarudzbenice.ToLower().Contains(search) ||
            (n.Partner?.Naziv.ToLower().Contains(search) ?? false)
        ).ToList();

        DgNarudzbenice.ItemsSource = filtrirano;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void DgNarudzbenice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool izabrano = DgNarudzbenice.SelectedItem is NarudzbenicaDobavljacu;
        BtnIzmeniNarudzbenicu.IsEnabled = izabrano;
        BtnObrisiNarudzbenicu.IsEnabled = izabrano;
        BtnPretvoriUKalkulaciju.IsEnabled = izabrano;

        DgNarudzbenicaStavke.ItemsSource = (DgNarudzbenice.SelectedItem as NarudzbenicaDobavljacu)?.Stavke;
    }

    private void DgNarudzbenice_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OtvoriZaIzmenu();

    private void BtnNovaNarudzbenica_Click(object sender, RoutedEventArgs e)
    {
        var win = new NarudzbenicaEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) UcitajNarudzbenice();
    }

    private void BtnIzmeniNarudzbenicu_Click(object sender, RoutedEventArgs e) => OtvoriZaIzmenu();

    private void OtvoriZaIzmenu()
    {
        if (DgNarudzbenice.SelectedItem is NarudzbenicaDobavljacu n)
        {
            var win = new NarudzbenicaEditWindow(_db, n.NarudzbenicaId) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true) UcitajNarudzbenice();
        }
    }

    private async void BtnObrisiNarudzbenicu_Click(object sender, RoutedEventArgs e)
    {
        if (DgNarudzbenice.SelectedItem is not NarudzbenicaDobavljacu n) return;

        var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete narudžbenicu br. {n.BrojNarudzbenice}?",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            await new KomercijalaService(_db).ObrisiNarudzbenicuAsync(n.NarudzbenicaId);
            UcitajNarudzbenice();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnPretvoriUKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNarudzbenice.SelectedItem is not NarudzbenicaDobavljacu n) return;

        try
        {
            var (success, msg, _) = await new KomercijalaService(_db).PretvoriNarudzbenicuUKalkulacijuAsync(n.NarudzbenicaId);
            MessageBox.Show(msg, success ? "Uspeh" : "Obaveštenje", MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (success) UcitajNarudzbenice();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri konverziji u kalkulaciju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgNarudzbenice, "Narudzbenice_Dobavljacima", "Narudzbenice");
}
