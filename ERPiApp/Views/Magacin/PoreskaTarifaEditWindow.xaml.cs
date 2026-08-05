using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using ERPiData;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

public partial class PoreskaTarifaEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _poreskaTarifaId;

    public PoreskaTarifaEditWindow(ErpiDbContext db, int? poreskaTarifaId)
    {
        InitializeComponent();
        _db = db;
        _poreskaTarifaId = poreskaTarifaId;

        if (_poreskaTarifaId.HasValue)
        {
            Title = "Izmena poreske tarife";
            UcitajData();
        }
        else
        {
            Title = "Nova poreska tarifa";
        }
    }

    private void UcitajData()
    {
        var t = _db.PoreskeTarife.Find(_poreskaTarifaId!.Value);
        if (t != null)
        {
            TxtTarifniBroj.Text = t.TarifniBroj;
            TxtPorezProcenat.Text = t.PorezProcenat.ToString("N2", CultureInfo.InvariantCulture);
            TxtPosebanPorezProcenat.Text = t.PosebanPorezProcenat.ToString("N2", CultureInfo.InvariantCulture);
            ChkPorezUCeni.IsChecked = t.PorezUCeni;
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        var tarifniBroj = TxtTarifniBroj.Text.Trim();

        if (string.IsNullOrWhiteSpace(tarifniBroj))
        {
            MessageBox.Show("Tarifni broj je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPorezProcenat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var porez))
        {
            MessageBox.Show("Nevažeći procenat poreza.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPosebanPorezProcenat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var posebanPorez))
        {
            MessageBox.Show("Nevažeći procenat posebnog poreza.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dup = _db.PoreskeTarife.FirstOrDefault(t => t.TarifniBroj == tarifniBroj && t.PoreskaTarifaId != (_poreskaTarifaId ?? 0));
        if (dup != null)
        {
            MessageBox.Show($"Tarifa sa brojem '{tarifniBroj}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            PoreskaTarifa tarifa;
            if (_poreskaTarifaId.HasValue)
            {
                tarifa = _db.PoreskeTarife.Find(_poreskaTarifaId.Value)!;
            }
            else
            {
                tarifa = new PoreskaTarifa();
                _db.PoreskeTarife.Add(tarifa);
            }

            tarifa.TarifniBroj = tarifniBroj;
            tarifa.PorezProcenat = porez;
            tarifa.PosebanPorezProcenat = posebanPorez;
            tarifa.PorezUCeni = ChkPorezUCeni.IsChecked == true;

            _db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju tarife: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
