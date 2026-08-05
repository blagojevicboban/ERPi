using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Port iz ERPiFinansijeApp — prikaz redova materijalne kartice sa negativnim stanjem/cenom.
/// Razlika: nema PDF štampu (ERPiApp još nema <c>PdfReportService</c> metodu za ovaj izveštaj,
/// vidi PLAN_NASTAVKA.md), zamenjena Excel izvozom po ustaljenom obrascu iz ove sekcije.
/// </summary>
public partial class ProveraKarticaWindow : Window
{
    public ProveraKarticaWindow(List<MaterijalnaKartica> redovi)
    {
        InitializeComponent();
        DgProvera.ItemsSource = redovi;
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgProvera, "Provera materijalnih kartica", "Provera_Materijalnih_Kartica");

    private void BtnZatvori_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
