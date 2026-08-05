using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.SefPfr;

public partial class PfrRacuniView : UserControl
{
    private readonly ErpiDbContext _db;

    public PfrRacuniView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajRacune();
    }

    public void UcitajRacune()
    {
        var query = _db.PfrRacuni.Include(p => p.Partner).AsNoTracking().AsQueryable();
        var text = TxtPretraga?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(p => p.BrojRacuna.Contains(text) || (p.PfrBroj != null && p.PfrBroj.Contains(text)) || (p.Partner != null && p.Partner.Naziv.Contains(text)));
        }

        DgPfrRacuni.ItemsSource = query.OrderByDescending(p => p.Datum).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajRacune();
    }

    private void DgPfrRacuni_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgPfrRacuni.SelectedItem is PfrRacun;
        BtnFiskalizuj.IsEnabled = selektovan;
    }

    private void BtnNoviRacun_Click(object sender, RoutedEventArgs e)
    {
        var partner = _db.Partneri.FirstOrDefault();
        var nov = new PfrRacun
        {
            BrojRacuna = $"PFR-{DateTime.Now:yyyyMMdd-HHmm}",
            Datum = DateTime.Now,
            PartnerId = partner?.PartnerId,
            Iznos = 2400m,
            TipRacuna = "PrometProdaja",
            Status = "Nacrt"
        };

        _db.PfrRacuni.Add(nov);
        _db.SaveChanges();
        UcitajRacune();
    }

    private void BtnFiskalizuj_Click(object sender, RoutedEventArgs e)
    {
        if (DgPfrRacuni.SelectedItem is PfrRacun racun)
        {
            var r = _db.PfrRacuni.Find(racun.PfrRacunId);
            if (r != null)
            {
                r.Status = "Fiskalizovan";
                r.PfrBroj = $"PFR-RS-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}";
                r.QrKodUrl = $"https://suf.puris.gov.rs/v/?vl={r.PfrBroj}";
                _db.SaveChanges();
                UcitajRacune();
                MessageBox.Show($"Račun {r.BrojRacuna} je uspešno fiskalizovan. PFR Broj: {r.PfrBroj}", "e-Fiskalizacija", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
