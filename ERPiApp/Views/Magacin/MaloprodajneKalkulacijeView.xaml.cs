using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

public partial class MaloprodajneKalkulacijeView : UserControl
{
    private readonly ErpiDbContext _db;
    private List<MaloprodajnaKalkulacija> _sveKalkulacije = new();

    public MaloprodajneKalkulacijeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += (_, _) => UcitajPodatke();
    }

    public async void UcitajPodatke()
    {
        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            _sveKalkulacije = await service.GetKalkulacijeAsync(TxtPretraga.Text.Trim());
            DgKalkulacije.ItemsSource = _sveKalkulacije;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju maloprodajnih kalkulacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        UcitajPodatke();
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju koju želite proknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana kalkulacija je već proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Knjiži maloprodajnu kalkulaciju br. {selektovana.BrojKalkulacije}?\nKnjiženje upisuje robu u prodavnicu i kreira nalog u Glavnoj knjizi.", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            await service.KnjiziKalkulacijuAsync(selektovana.MaloprodajnaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju koju želite rasknjižiti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana kalkulacija nije proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Rasknjiži maloprodajnu kalkulaciju br. {selektovana.BrojKalkulacije}?\nRasknjižavanje poništava zalihe na kartici prodavnice i briše nalog u Glavnoj knjizi.", "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var service = new MaloprodajnaKalkulacijaService(_db);
            await service.RasknjiziKalkulacijuAsync(selektovana.MaloprodajnaKalkulacijaId);
            UcitajPodatke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void BtnNova_Click(object sender, RoutedEventArgs e) => OtveriZaIzmenu(null);

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija selektovana)
        {
            OtveriZaIzmenu(selektovana);
        }
        else
        {
            MessageBox.Show("Izaberite MP kalkulaciju za izmenu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DgKalkulacije_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija selektovana)
        {
            OtveriZaIzmenu(selektovana);
        }
    }

    private async void OtveriZaIzmenu(MaloprodajnaKalkulacija? kalk)
    {
        MaloprodajnaKalkulacija? puna = kalk;
        if (kalk != null)
        {
            puna = await _db.MaloprodajneKalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalk.MaloprodajnaKalkulacijaId);
        }

        var win = new MaloprodajnaKalkulacijaEditWindow(_db, puna) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            UcitajPodatke();
        }
    }

    private void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija kalkulacija)
        {
            if (kalkulacija.IsKnjizen)
            {
                MessageBox.Show("Proknjižena kalkulacija se ne može brisati. Prvo je rasknjižite.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete MP kalkulaciju br. {kalkulacija.BrojKalkulacije}?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var k = _db.MaloprodajneKalkulacije.Find(kalkulacija.MaloprodajnaKalkulacijaId);
                    if (k != null)
                    {
                        _db.MaloprodajneKalkulacije.Remove(k);
                        _db.SaveChanges();
                        UcitajPodatke();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Izaberite MP kalkulaciju za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgKalkulacije, "MP_Kalkulacije", "Maloprodajne Kalkulacije");

    private void BtnStampajPdf_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
        {
            MessageBox.Show("Izaberite MP kalkulaciju za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            MessageBox.Show($"Priprema PDF štampanog dokumenta za MP kalkulaciju br. {selektovana.BrojKalkulacije}...", "PDF Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampanog dokumenta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
