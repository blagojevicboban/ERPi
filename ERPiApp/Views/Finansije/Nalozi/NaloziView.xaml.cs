using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.Nalozi;

public partial class NaloziView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<Nalog> _sviNalozi = new();

    public NaloziView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Ucitaj();

        // Postavljeno OVDE, ne kao IsChecked="True" u XAML-u: taj literal bi Checked event
        // (Filter_Changed → Filtriraj) okinuo sinhrono usred InitializeComponent(), pre nego
        // što DgNalozi/TxtPretraga (deklarisani niže u istom XAML stablu) uopšte postoje —
        // NullReferenceException. Ucitaj() je gore već pozvao Filtriraj() jednom napravo.
        RbSvi.IsChecked = true;
    }

    private void Ucitaj()
    {
        _sviNalozi = _db.Nalozi
            .Include(n => n.Stavke).ThenInclude(s => s.Konto)
            .Include(n => n.Stavke).ThenInclude(s => s.Partner)
            .OrderByDescending(n => n.DatumNaloga)
            .ThenByDescending(n => n.BrojNaloga)
            .ToList();
        Filtriraj();
    }

    private void Filtriraj()
    {
        IEnumerable<Nalog> upit = _sviNalozi;

        if (RbNacrt.IsChecked == true)
            upit = upit.Where(n => n.Status == StatusNaloga.Nacrt);
        else if (RbProknjizeni.IsChecked == true)
            upit = upit.Where(n => n.Status == StatusNaloga.Proknjizen);

        var pretraga = TxtPretraga.Text?.Trim();
        if (!string.IsNullOrEmpty(pretraga))
        {
            upit = upit.Where(n =>
                (n.Opis?.Contains(pretraga, StringComparison.OrdinalIgnoreCase) ?? false) ||
                n.BrojNaloga.ToString().Contains(pretraga));
        }

        DgNalozi.ItemsSource = upit.ToList();
        DgStavke.ItemsSource = null;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();
    private void Filter_Changed(object sender, RoutedEventArgs e) => Filtriraj();

    private void DgNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgStavke.ItemsSource = DgNalozi.SelectedItem is Nalog nalog
            ? nalog.Stavke.OrderBy(s => s.RedniBroj).ToList()
            : null;
    }

    private void DgNalozi_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => IzmeniIzabrani();

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        var sledeciBroj = (_sviNalozi.Count == 0 ? 0 : _sviNalozi.Max(n => n.BrojNaloga)) + 1;
        var dlg = new NalogEditWindow(_db, new Nalog { BrojNaloga = sledeciBroj }) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) Ucitaj();
    }

    private void BtnIzmeniNalog_Click(object sender, RoutedEventArgs e) => IzmeniIzabrani();

    private void IzmeniIzabrani()
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        if (nalog.Status == StatusNaloga.Proknjizen)
        {
            MessageBox.Show("Proknjižen nalog se ne može menjati — prvo ga rasknjižite.",
                "Nalog je proknjižen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new NalogEditWindow(_db, nalog) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) Ucitaj();
    }

    private void BtnObrisiNalog_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        if (nalog.Status == StatusNaloga.Proknjizen)
        {
            MessageBox.Show("Proknjižen nalog se ne može obrisati — prvo ga rasknjižite.",
                "Nalog je proknjižen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show($"Obrisati nalog br. {nalog.BrojNaloga}?", "Potvrda brisanja",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        _db.Nalozi.Remove(nalog);
        _db.SaveChanges();
        Ucitaj();
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        if (!nalog.IsUravnotezen)
        {
            MessageBox.Show("Nalog nije uravnotežen (Duguje ≠ Potražuje) — ne može se proknjižiti.",
                "Nalog nije uravnotežen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        nalog.Status = StatusNaloga.Proknjizen;
        nalog.DatumKnjizenja = DateTime.Now;
        _db.SaveChanges();
        Ucitaj();
    }

    private void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog nalog) return;

        nalog.Status = StatusNaloga.Nacrt;
        nalog.DatumKnjizenja = null;
        _db.SaveChanges();
        Ucitaj();
    }
}
