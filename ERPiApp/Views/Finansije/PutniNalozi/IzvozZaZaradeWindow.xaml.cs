using System;
using System.Linq;
using System.Windows;
using ERPiData;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.PutniNalozi;

/// <summary>
/// Izvozi deo dnevnice iznad zakonskog neoporezivog iznosa u JSON koji Zarade modul uvozi u
/// obračun zarade (<c>PutniNaloziImportService</c> u <c>ERPiApp/Services/Zarade</c>). Prvo
/// pokazuje šta je pronađeno i šta bi izvoz izostavio, pa tek na potvrdu snima fajl.
/// </summary>
public partial class IzvozZaZaradeWindow : Window
{
    private readonly ErpiDbContext _db;
    private string? _spremanJson;
    private int _brojStavki;

    public IzvozZaZaradeWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        for (int m = 1; m <= 12; m++)
            CmbMesec.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = $"{m:D2} — {System.Globalization.CultureInfo.GetCultureInfo("sr-Latn-RS").DateTimeFormat.GetMonthName(m)}",
                Tag = m
            });
        CmbMesec.SelectedIndex = DateTime.Today.Month - 1;

        int godinaSad = DateTime.Today.Year;
        for (int g = godinaSad - 1; g <= godinaSad + 1; g++)
            CmbGodina.Items.Add(g);
        CmbGodina.SelectedItem = godinaSad;
    }

    private async void BtnPripremi_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMesec.SelectedItem is not System.Windows.Controls.ComboBoxItem mesecItem
            || CmbGodina.SelectedItem is not int godina)
        {
            return;
        }

        int mesec = (int)mesecItem.Tag;

        BtnSacuvaj.IsEnabled = false;
        _spremanJson = null;
        TxtStatus.Text = "Pripremam...";

        try
        {
            var firma = await _db.Firme.AsNoTracking().FirstOrDefaultAsync();
            var rezultat = await PutniNaloziZaZaradeWriter.GenerisiAsync(_db, firma, godina, mesec);

            DgStavke.ItemsSource = rezultat.Stavke;

            if (rezultat.Nalazi.Count > 0)
            {
                ListaNalaza.ItemsSource = rezultat.Nalazi;
                PanelNalazi.Visibility = Visibility.Visible;
            }
            else
            {
                PanelNalazi.Visibility = Visibility.Collapsed;
            }

            _spremanJson = rezultat.Json;
            _brojStavki = rezultat.BrojStavki;

            TxtStatus.Text = rezultat.Json != null
                ? $"Spremno za izvoz: {rezultat.BrojStavki} stavki za {mesec:D2}/{godina}."
                : $"Nema ničega za izvoz za {mesec:D2}/{godina}.";

            BtnSacuvaj.IsEnabled = rezultat.Json != null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pripremi izvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "";
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (_spremanJson == null) return;

        var mesecItem = (System.Windows.Controls.ComboBoxItem)CmbMesec.SelectedItem;
        int mesec = (int)mesecItem.Tag;
        int godina = (int)CmbGodina.SelectedItem;

        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Putni nalozi za Zarade (*.json)|*.json",
            FileName = $"PutniNaloziZaZarade_{godina}_{mesec:D2}.json",
            Title = "Sačuvaj izvoz za Zarade"
        };

        if (sfd.ShowDialog() != true) return;

        try
        {
            System.IO.File.WriteAllText(sfd.FileName, _spremanJson);
            MessageBox.Show(
                $"Izvezeno {_brojStavki} stavki u:\n{sfd.FileName}\n\n" +
                "Uvezite fajl u modulu Zarade (Primanja → „📥 Uvoz putnih naloga\").",
                "Izvoz završen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fajl nije sačuvan: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e) => Close();
}
