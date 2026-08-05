using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Finansije.Nalozi;

/// <summary>
/// Masovna zamena konta na svim stavkama naloga glavne knjige (preneto iz ERPiFinansije,
/// analogno DOS proceduri 'prekm'). Za razliku od izvora, konto se ovde bira iz šifarnika
/// (KontoId FK), ne unosi kao slobodan string broja konta — u ERPi šemi je Konto pravi
/// strani ključ, ne string, pa slobodan unos ne bi mogao da se pouzdano poveže sa Kontom.
/// </summary>
public partial class PreknjizavanjeWindow : Window
{
    private readonly ErpiDbContext _db;

    public PreknjizavanjeWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var konta = _db.Konta.OrderBy(k => k.BrojKonta).ToList();
        CmbStaroKonto.ItemsSource = konta;
        CmbNovoKonto.ItemsSource = konta;
    }

    private async void BtnIzvrsi_Click(object sender, RoutedEventArgs e)
    {
        var staro = CmbStaroKonto.SelectedItem as Konto;
        var novo = CmbNovoKonto.SelectedItem as Konto;

        if (staro == null)
        {
            MessageBox.Show("Izaberite staro konto.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (novo == null)
        {
            MessageBox.Show("Izaberite novo konto.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (staro.KontoId == novo.KontoId)
        {
            MessageBox.Show("Staro i novo konto ne mogu biti jednaki.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var potvrda = MessageBox.Show(
            $"PAŽNJA!\n\nDa li zaista želite da preknjižite sve stavke sa konta '{staro.Prikaz}' na novo konto '{novo.Prikaz}'?",
            "Potvrda preknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            int zamenjeno = await PreknjiziKontoAsync(staro.KontoId, novo.KontoId);
            MessageBox.Show($"Preknjižavanje uspešno završeno!\n\nZamenjeno je ukupno {zamenjeno} stavki sa konta {staro.Prikaz} na konto {novo.Prikaz}.",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri preknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Masovno preknjižavanje konta na stavkama naloga (analogno preknjizi() iz DOS FIN1.PRG).
    /// Zamenjuje staro konto novim kontom na svim stavkama naloga i ažurira bazu.
    /// </summary>
    private async System.Threading.Tasks.Task<int> PreknjiziKontoAsync(int staroKontoId, int novoKontoId)
    {
        var stavke = await _db.StavkeNaloga
            .Where(s => s.KontoId == staroKontoId)
            .ToListAsync();

        foreach (var s in stavke)
        {
            s.KontoId = novoKontoId;
        }

        if (stavke.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return stavke.Count;
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BtnOdustani_Click(sender, e);
        }
    }
}
