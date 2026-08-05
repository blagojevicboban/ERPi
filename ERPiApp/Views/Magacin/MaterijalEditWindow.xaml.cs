using System;
using System.Linq;
using System.Windows;
using ERPiData;
using ERPiData.Models.Magacin;

namespace ERPiApp.Views.Magacin;

/// <summary>Šifarnik materijala CRUD — analogno <see cref="ArtikalEditWindow"/>, samo nad <see cref="Materijal"/>.</summary>
public partial class MaterijalEditWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int? _materijalId;

    public MaterijalEditWindow(ErpiDbContext db, int? materijalId)
    {
        InitializeComponent();
        _db = db;
        _materijalId = materijalId;

        if (_materijalId.HasValue)
        {
            Title = "Izmena materijala";
            UcitajData();
        }
        else
        {
            Title = "Nov materijal";
        }
    }

    private void UcitajData()
    {
        var m = _db.Materijali.Find(_materijalId!.Value);
        if (m != null)
        {
            TxtSifra.Text = m.SifraArtikla;
            TxtNaziv.Text = m.Naziv;
            TxtJM.Text = m.JedinicaMere;
            TxtPakovanje.Text = m.Pakovanje;
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        var sifra = TxtSifra.Text.Trim();
        var naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Šifra materijala je obavezna.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Naziv materijala je obavezan.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dup = _db.Materijali.FirstOrDefault(m => m.SifraArtikla == sifra && m.MaterijalId != (_materijalId ?? 0));
        if (dup != null)
        {
            MessageBox.Show($"Materijal sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Materijal materijal;
            if (_materijalId.HasValue)
            {
                materijal = _db.Materijali.Find(_materijalId.Value)!;
            }
            else
            {
                materijal = new Materijal();
                _db.Materijali.Add(materijal);
            }

            materijal.SifraArtikla = sifra;
            materijal.Naziv = naziv;
            materijal.JedinicaMere = string.IsNullOrWhiteSpace(TxtJM.Text) ? "kom" : TxtJM.Text.Trim();
            materijal.Pakovanje = string.IsNullOrWhiteSpace(TxtPakovanje.Text) ? null : TxtPakovanje.Text.Trim();

            _db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
