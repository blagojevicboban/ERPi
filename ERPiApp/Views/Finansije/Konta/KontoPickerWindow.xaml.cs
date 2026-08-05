using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp.Views.Finansije.Konta;

/// <summary>
/// F2 pretraga kontnog plana za brzi izbor konta u gridovima (npr. <see cref="ERPiApp.Views.Finansije.Nalozi.NalogEditWindow"/>).
/// Poziva se sa već otvorenim <see cref="ErpiDbContext"/> firme (ne otvara sopstvenu konekciju —
/// vidi napomenu o deljenom _db kontekstu u PLAN_NASTAVKA.md §2).
/// </summary>
public partial class KontoPickerWindow : Window
{
    private readonly List<Konto> _allKonta;
    public Konto? IzabraniKonto { get; private set; }

    public KontoPickerWindow(ErpiDbContext db, string initialSearch = "")
    {
        InitializeComponent();
        _allKonta = db.Konta.OrderBy(k => k.BrojKonta).ToList();
        TxtPretraga.Text = initialSearch;
        Loaded += (_, _) =>
        {
            TxtPretraga.Focus();
            TxtPretraga.SelectAll();
            ApplyFilter();
        };
    }

    private void ApplyFilter()
    {
        if (DgKonta == null) return;

        string query = TxtPretraga.Text.Trim().ToLower();
        var filtered = _allKonta.Where(k =>
            string.IsNullOrEmpty(query) ||
            k.BrojKonta.ToLower().Contains(query) ||
            k.NazivKonta.ToLower().Contains(query)
        ).ToList();

        DgKonta.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgKonta.SelectedIndex = 0;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void TxtPretraga_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && DgKonta.Items.Count > 0)
        {
            DgKonta.Focus();
            if (DgKonta.SelectedIndex < 0) DgKonta.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PotvrdiIzbor();
            e.Handled = true;
        }
    }

    private void DgKonta_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PotvrdiIzbor();
            e.Handled = true;
        }
    }

    private void DgKonta_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PotvrdiIzbor();
    }

    private void PotvrdiIzbor()
    {
        if (DgKonta.SelectedItem is Konto izabran)
        {
            IzabraniKonto = izabran;
            DialogResult = true;
            Close();
        }
    }

    private void BtnIzaberi_Click(object sender, RoutedEventArgs e)
    {
        PotvrdiIzbor();
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
