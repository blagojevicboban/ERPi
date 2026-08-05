using System;
using System.Linq;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Robni Bruto Bilans (po Artikal šifarniku) — koristi postojeći
/// <see cref="RobniBrutoBilansService.GetRobniBrutoBilansAsync"/> (servisni sloj je već postojao,
/// samo ekran nije bio portovan — vidi PLAN_NASTAVKA.md §3i). NE meša se sa
/// <see cref="RobniBrutoBilansService.GetMaterijalniBrutoBilansAsync"/>, koji koristi
/// <see cref="MaterijalnoDashboardView"/> za KPI karte Materijalnog knjigovodstva.
/// </summary>
public partial class RobniBrutoBilansView : UserControl
{
    private readonly ErpiDbContext _db;

    public RobniBrutoBilansView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            if (CmbMagacin.ItemsSource == null)
            {
                var magacini = _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToList();
                magacini.Insert(0, new ERPiData.Models.Magacin.Magacin { MagacinId = 0, SifraMagacina = "", NazivMagacina = "-- Svi magacini --" });
                CmbMagacin.ItemsSource = magacini;
                CmbMagacin.SelectedIndex = 0;
                return; // SelectionChanged će sam pozvati UcitajPodatke ponovo
            }

            int? magacinId = CmbMagacin.SelectedValue is int id && id > 0 ? id : null;
            var service = new RobniBrutoBilansService(_db);
            var redovi = await service.GetRobniBrutoBilansAsync(magacinId, DpDoDatuma.SelectedDate, TxtPretraga.Text?.Trim());

            DgBilans.ItemsSource = redovi;
            TxtUkupnoDuguje.Text = $"Ukupno Duguje: {redovi.Sum(r => r.UlazVrednost):N2} RSD";
            TxtUkupnoPotrazuje.Text = $"Ukupno Potražuje: {redovi.Sum(r => r.IzlazVrednost):N2} RSD";
            TxtUkupnoSaldo.Text = $"Saldo Zaliha: {redovi.Sum(r => r.SaldoVrednosni):N2} RSD";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Greška pri učitavanju robnog bruto bilansa: {ex.Message}", "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e) => UcitajPodatke();
    private void DpDoDatuma_SelectedDateChanged(object sender, EventArgs e) => UcitajPodatke();
    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => UcitajPodatke();
    private void BtnOsvezi_Click(object sender, System.Windows.RoutedEventArgs e) => UcitajPodatke();
}
