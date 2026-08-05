using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Lista Robnih (Artikal-baziranih) internih kretanja, parametrizovana po
/// <see cref="VrstaRobnogKretanja"/> — isti ekran pokriva tabove Primopredaje/Zaduženja/
/// Razduženja (izvorni <c>TrgovinaView</c> obrazac: jedan ekran, tri filtrirana taba, vidi
/// PLAN_NASTAVKA.md §3i), umesto tri skoro-identična ekrana.
/// </summary>
public partial class RobnoKretanjaView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly string _vrsta;

    public RobnoKretanjaView(ErpiDbContext db, string vrsta)
    {
        InitializeComponent();
        _db = db;
        _vrsta = vrsta;
        // IsChecked se namerno NE piše kao XAML literal niti direktno iz konstruktora — Checked
        // handler (Filter_Changed) zove UcitajPodatke() koja čita _db/DgKretanja; ide kroz
        // Loaded da se izbegne isti obrazac bug kao NaloziView/KarticaKontaView (vidi PLAN_NASTAVKA.md §2).
        Loaded += (_, _) => RbSvi.IsChecked = true;
    }

    public async void UcitajPodatke()
    {
        var search = TxtPretraga?.Text?.Trim();
        var svi = await new RobnoKretanjeService(_db).GetKretanjaAsync(_vrsta, search);

        if (RbProknjizeni.IsChecked == true) svi = svi.Where(p => p.IsKnjizen).ToList();
        else if (RbNeproknjizeni.IsChecked == true) svi = svi.Where(p => !p.IsKnjizen).ToList();

        DgKretanja.ItemsSource = svi;
        DgStavke.ItemsSource = null;
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => UcitajPodatke();

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => UcitajPodatke();

    private void DgKretanja_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgKretanja.SelectedItem is RobnoKretanjeNalog;
        BtnIzmeni.IsEnabled = selektovan;
        BtnObrisi.IsEnabled = selektovan;
        BtnKnjizi.IsEnabled = selektovan && DgKretanja.SelectedItem is RobnoKretanjeNalog n1 && !n1.IsKnjizen;
        BtnRasknjizi.IsEnabled = selektovan && DgKretanja.SelectedItem is RobnoKretanjeNalog n2 && n2.IsKnjizen;

        DgStavke.ItemsSource = DgKretanja.SelectedItem is RobnoKretanjeNalog nalog
            ? nalog.Stavke.OrderBy(s => s.RedniBroj).ToList()
            : null;
    }

    private void DgKretanja_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OtvoriZaIzmenu();

    private void BtnNovi_Click(object sender, RoutedEventArgs e)
    {
        var win = new RobnoKretanjeEditWindow(_db, _vrsta) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) UcitajPodatke();
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e) => OtvoriZaIzmenu();

    private void OtvoriZaIzmenu()
    {
        if (DgKretanja.SelectedItem is RobnoKretanjeNalog nalog)
        {
            var win = new RobnoKretanjeEditWindow(_db, _vrsta, nalog) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true) UcitajPodatke();
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKretanja.SelectedItem is not RobnoKretanjeNalog nalog) return;

        var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete dokument br. {nalog.BrojNaloga}?",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            await new RobnoKretanjeService(_db).ObrisiKretanjeAsync(nalog.RobnoKretanjeNalogId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKretanja.SelectedItem is not RobnoKretanjeNalog nalog) return;

        if (MessageBox.Show($"Proknjiži dokument br. {nalog.BrojNaloga}? Posle knjiženja nisu dozvoljene izmene.",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            await new RobnoKretanjeService(_db).KnjiziKretanjeAsync(nalog.RobnoKretanjeNalogId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKretanja.SelectedItem is not RobnoKretanjeNalog nalog) return;

        if (MessageBox.Show($"Rasknjiži dokument br. {nalog.BrojNaloga}?",
            "Potvrda rasknjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            await new RobnoKretanjeService(_db).RasknjiziKretanjeAsync(nalog.RobnoKretanjeNalogId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
