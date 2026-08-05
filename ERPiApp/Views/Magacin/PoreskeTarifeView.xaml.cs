using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class PoreskeTarifeView : UserControl
{
    private readonly ErpiDbContext _db;

    public PoreskeTarifeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajTarife();
    }

    public void UcitajTarife()
    {
        var query = _db.PoreskeTarife.AsNoTracking().AsQueryable();
        var text = TxtPretraga?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(t => t.TarifniBroj.Contains(text));
        }

        DgTarife.ItemsSource = query.OrderBy(t => t.TarifniBroj).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajTarife();
    }

    private void DgTarife_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovana = DgTarife.SelectedItem is PoreskaTarifa;
        BtnIzmeniTarifu.IsEnabled = selektovana;
        BtnObrisiTarifu.IsEnabled = selektovana;
    }

    private void DgTarife_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtvoriZaIzmenu();
    }

    private void BtnNovaTarifa_Click(object sender, RoutedEventArgs e)
    {
        var win = new PoreskaTarifaEditWindow(_db, null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajTarife();
        }
    }

    private void BtnIzmeniTarifu_Click(object sender, RoutedEventArgs e)
    {
        OtvoriZaIzmenu();
    }

    private void OtvoriZaIzmenu()
    {
        if (DgTarife.SelectedItem is PoreskaTarifa tarifa)
        {
            var win = new PoreskaTarifaEditWindow(_db, tarifa.PoreskaTarifaId) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajTarife();
            }
        }
    }

    private void BtnObrisiTarifu_Click(object sender, RoutedEventArgs e)
    {
        if (DgTarife.SelectedItem is PoreskaTarifa tarifa)
        {
            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete tarifu '{tarifa.TarifniBroj}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var t = _db.PoreskeTarife.Find(tarifa.PoreskaTarifaId);
                    if (t != null)
                    {
                        _db.PoreskeTarife.Remove(t);
                        _db.SaveChanges();
                        UcitajTarife();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju tarife: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgTarife, "Poreske_Tarife", "Tarife");
    }
}
