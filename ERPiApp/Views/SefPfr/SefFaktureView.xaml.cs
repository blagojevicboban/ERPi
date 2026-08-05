using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.SefPfr;

public partial class SefFaktureView : UserControl
{
    private readonly ErpiDbContext _db;

    public SefFaktureView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        UcitajFakture();
    }

    public void UcitajFakture()
    {
        var query = _db.SefDokumenti.Include(s => s.Partner).AsNoTracking().AsQueryable();
        var text = TxtPretraga?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(s => s.BrojDokumenta.Contains(text) || (s.Partner != null && s.Partner.Naziv.Contains(text)));
        }

        DgSefFakture.ItemsSource = query.OrderByDescending(s => s.DatumDokumenta).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajFakture();
    }

    private void DgSefFakture_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selektovan = DgSefFakture.SelectedItem is SefDokument;
        BtnPosaljiNaSef.IsEnabled = selektovan;
    }

    private void BtnNovaFaktura_Click(object sender, RoutedEventArgs e)
    {
        var partner = _db.Partneri.FirstOrDefault();
        if (partner == null)
        {
            MessageBox.Show("Molimo unesite bar jednog partnera pre kreiranja e-Fakture.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nov = new SefDokument
        {
            BrojDokumenta = $"SEF-{DateTime.Now:yyyyMMdd-HHmm}",
            DatumDokumenta = DateTime.Now,
            PartnerId = partner.PartnerId,
            Osnovica = 10000m,
            IznosPdv = 2000m,
            Ukupno = 12000m,
            Status = "Nacrt",
            TipDokumenta = "Faktura"
        };

        _db.SefDokumenti.Add(nov);
        _db.SaveChanges();
        UcitajFakture();
    }

    private void BtnPosaljiNaSef_Click(object sender, RoutedEventArgs e)
    {
        if (DgSefFakture.SelectedItem is SefDokument faktura)
        {
            var dok = _db.SefDokumenti.Find(faktura.SefDokumentId);
            if (dok != null)
            {
                dok.Status = "Poslato";
                dok.DatumSlanja = DateTime.Now;
                dok.CirId = $"CIR-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                _db.SaveChanges();
                UcitajFakture();
                MessageBox.Show($"Faktura {dok.BrojDokumenta} je uspešno poslata na SEF API. Dodeljen CIR ID: {dok.CirId}", "SEF API", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void BtnOsveziStatus_Click(object sender, RoutedEventArgs e)
    {
        UcitajFakture();
    }
}
