using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

public partial class MagacinEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _magacinId;

    public MagacinEditWindow(ErpiDbContext db, int? magacinId)
    {
        InitializeComponent();
        _db = db;
        _magacinId = magacinId;

        if (_magacinId.HasValue)
        {
            Title = "Izmena magacina";
            UcitajData();
        }
        else
        {
            Title = "Novi magacin";
        }
    }

    private void UcitajData()
    {
        var m = _db.Magacini.Find(_magacinId!.Value);
        if (m != null)
        {
            TxtSifra.Text = m.SifraMagacina;
            TxtNaziv.Text = m.NazivMagacina;
            TxtOdgovornoLice.Text = m.OdgovornoLice;

            foreach (ComboBoxItem item in CmbVrsta.Items)
            {
                if (item.Content.ToString() == m.VrstaMagacina)
                {
                    CmbVrsta.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        var sifra = TxtSifra.Text.Trim();
        var naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Šifra magacina je obavezna.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Naziv magacina je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dup = _db.Magacini.FirstOrDefault(m => m.SifraMagacina == sifra && m.MagacinId != (_magacinId ?? 0));
        if (dup != null)
        {
            MessageBox.Show($"Magacin sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            ERPiData.Models.Magacin.Magacin magacin;
            if (_magacinId.HasValue)
            {
                magacin = _db.Magacini.Find(_magacinId.Value)!;
            }
            else
            {
                magacin = new ERPiData.Models.Magacin.Magacin();
                _db.Magacini.Add(magacin);
            }

            magacin.SifraMagacina = sifra;
            magacin.NazivMagacina = naziv;
            magacin.OdgovornoLice = TxtOdgovornoLice.Text.Trim();
            magacin.VrstaMagacina = (CmbVrsta.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Veleprodaja";

            _db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju magacina: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
