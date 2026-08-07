using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ERPiApp.Services;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Izvestaji;

/// <summary>
/// Prikaz bruto bilansa grupisanog po partneru umesto po kontu — "drill-down" u analitiku iza
/// sintetičkih totala glavnog Bruto bilansa (<see cref="BrutoBilansView"/>).
/// </summary>
public partial class BrutoBilansAnalitikePreviewWindow : Window
{
    public BrutoBilansAnalitikePreviewWindow(List<BrutoBilansAnalitikeRed> redovi)
    {
        InitializeComponent();

        TxtPodnaslov.Text = $"Broj partnera: {redovi.Count}";

        DgAnalitike.ItemsSource = redovi;
        TxtUkupnoDuguje.Text = redovi.Sum(r => r.Duguje).ToString("N2");
        TxtUkupnoPotrazuje.Text = redovi.Sum(r => r.Potrazuje).ToString("N2");
        TxtUkupnoSaldo.Text = redovi.Sum(r => r.Saldo).ToString("N2");
    }

    private void BtnExportExcelAnalitike_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgAnalitike, TxtNaslov.Text, "Bruto_Bilans_Analitike");
}
