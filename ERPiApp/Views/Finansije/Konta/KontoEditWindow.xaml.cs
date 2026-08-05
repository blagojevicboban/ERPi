using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Konta;

public partial class KontoEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly Konto _konto;
    private readonly bool _isEdit;

    public KontoEditWindow(ErpiDbContext db, Konto? konto = null)
    {
        InitializeComponent();
        _db = db;
        _isEdit = konto != null;
        _konto = konto ?? new Konto();

        if (_isEdit)
        {
            TxtHeader.Text = $"✏️ Izmena konta {_konto.BrojKonta}";
            TxtBrojKonta.Text = _konto.BrojKonta;
            TxtBrojKonta.IsReadOnly = true;
            TxtNazivKonta.Text = _konto.NazivKonta;
            TxtStariKonto.Text = _konto.StariKonto;
            TxtUlica.Text = _konto.Ulica;
            TxtMesto.Text = _konto.Mesto;
            TxtZiroRacun.Text = _konto.ZiroRacun;
            TxtTelefon.Text = _konto.Telefon;

            for (int i = 0; i < CmbVrstaKonta.Items.Count; i++)
            {
                if (((ComboBoxItem)CmbVrstaKonta.Items[i]).Content.ToString() == _konto.VrstaKonta)
                {
                    CmbVrstaKonta.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string broj = TxtBrojKonta.Text.Trim();
        string naziv = TxtNazivKonta.Text.Trim();

        if (string.IsNullOrWhiteSpace(broj))
        {
            MessageBox.Show("Broj konta je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Naziv konta je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _konto.BrojKonta = broj;
        _konto.NazivKonta = naziv;
        _konto.StariKonto = string.IsNullOrWhiteSpace(TxtStariKonto.Text) ? null : TxtStariKonto.Text.Trim();
        _konto.Ulica = string.IsNullOrWhiteSpace(TxtUlica.Text) ? null : TxtUlica.Text.Trim();
        _konto.Mesto = string.IsNullOrWhiteSpace(TxtMesto.Text) ? null : TxtMesto.Text.Trim();
        _konto.ZiroRacun = string.IsNullOrWhiteSpace(TxtZiroRacun.Text) ? null : TxtZiroRacun.Text.Trim();
        _konto.Telefon = string.IsNullOrWhiteSpace(TxtTelefon.Text) ? null : TxtTelefon.Text.Trim();
        _konto.VrstaKonta = ((ComboBoxItem)CmbVrstaKonta.SelectedItem)?.Content?.ToString() ?? "Aktivna";

        try
        {
            var service = new KontaService(_db);
            await service.SaveKontoAsync(_konto);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
