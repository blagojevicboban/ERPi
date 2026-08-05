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
/// Ponude/Predračuni kupcima — lista + 1-klik konverzija u Račun-otpremnicu. Port iz
/// ERPiFinansijeApp/Views/Trgovina/TrgovinaView.xaml ("Ponude &amp; Predračuni" tab), vidi
/// PLAN_NASTAVKA.md §3i.
/// </summary>
public partial class PonudeView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<PonudaPredracun> _svePonude = new();

    public PonudeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        // Loaded, ne direktan poziv iz konstruktora — deli već otvoren _db, await bi mogao
        // da se završi sinhrono pre nego što je control u vizuelnom stablu (isti razlog kao
        // KarticaKontaView/MaterijalnoDashboardView — vidi PLAN_NASTAVKA.md §2).
        Loaded += (_, _) => UcitajPonude();
    }

    public async void UcitajPonude()
    {
        try
        {
            _svePonude = await new KomercijalaService(_db).GetPonudeAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju ponuda i predračuna: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filtriraj()
    {
        string search = TxtPretraga?.Text?.Trim().ToLower() ?? "";
        var filtrirano = _svePonude.Where(p =>
            string.IsNullOrEmpty(search) ||
            p.BrojDokumenta.ToLower().Contains(search) ||
            (p.Partner?.Naziv.ToLower().Contains(search) ?? false) ||
            p.VrstaDokumenta.ToLower().Contains(search)
        ).ToList();

        DgPonude.ItemsSource = filtrirano;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void DgPonude_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool izabrano = DgPonude.SelectedItem is PonudaPredracun;
        BtnIzmeniPonudu.IsEnabled = izabrano;
        BtnObrisiPonudu.IsEnabled = izabrano;
        BtnPretvoriURacun.IsEnabled = izabrano;

        DgPonudaStavke.ItemsSource = (DgPonude.SelectedItem as PonudaPredracun)?.Stavke;
    }

    private void DgPonude_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OtvoriZaIzmenu();

    private void BtnNovaPonuda_Click(object sender, RoutedEventArgs e)
    {
        var win = new PonudaEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) UcitajPonude();
    }

    private void BtnIzmeniPonudu_Click(object sender, RoutedEventArgs e) => OtvoriZaIzmenu();

    private void OtvoriZaIzmenu()
    {
        if (DgPonude.SelectedItem is PonudaPredracun p)
        {
            var win = new PonudaEditWindow(_db, p.PonudaPredracunId) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true) UcitajPonude();
        }
    }

    private async void BtnObrisiPonudu_Click(object sender, RoutedEventArgs e)
    {
        if (DgPonude.SelectedItem is not PonudaPredracun p) return;

        var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete {p.VrstaDokumenta} br. {p.BrojDokumenta}?",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            await new KomercijalaService(_db).ObrisiPonuduAsync(p.PonudaPredracunId);
            UcitajPonude();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnPretvoriURacun_Click(object sender, RoutedEventArgs e)
    {
        if (DgPonude.SelectedItem is not PonudaPredracun p) return;

        try
        {
            var (success, msg, _) = await new KomercijalaService(_db).PretvoriPonuduURacunAsync(p.PonudaPredracunId);
            MessageBox.Show(msg, success ? "Uspeh" : "Obaveštenje", MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (success) UcitajPonude();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri konverziji u račun: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgPonude, "Ponude_i_Predracuni", "Ponude");
}
