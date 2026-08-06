using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Devizno;

public partial class DeviznoValviranjeView : UserControl
{
    private readonly ErpiDbContext _db;
    private readonly DeviznoKnjigovodstvoService _service;
    private List<DeviznoKnjigovodstvoResult> _rezultati = new();

    /// <summary>Raised when the user clicks "Zatvori" (only visible when hosted in a dialog) or after a successful knjiženje in dialog mode.</summary>
    public event EventHandler? CloseRequested;

    public DeviznoValviranjeView(ErpiDbContext db, bool prikaziDugmeZatvori = false)
    {
        InitializeComponent();
        _db = db;
        _service = new DeviznoKnjigovodstvoService(_db);

        BtnZatvori.Visibility = prikaziDugmeZatvori ? Visibility.Visible : Visibility.Collapsed;

        DpNaDan.SelectedDate = new DateTime(DateTime.Today.Year, 12, 31);
        Loaded += DeviznoValviranjeView_Loaded;
    }

    private void DeviznoValviranjeView_Loaded(object sender, RoutedEventArgs e)
    {
        Izracunaj();
    }

    private async void Izracunaj()
    {
        try
        {
            DateTime naDan = DpNaDan.SelectedDate ?? DateTime.Today;
            decimal.TryParse(TxtKursEur.Text.Trim(), out decimal kursEur);
            decimal.TryParse(TxtKursUsd.Text.Trim(), out decimal kursUsd);

            if (kursEur <= 0) kursEur = 117.20m;
            if (kursUsd <= 0) kursUsd = 108.50m;

            _rezultati = await _service.ObracunajValviranjeAsync(naDan, kursEur, kursUsd);
            DgValviranje.ItemsSource = _rezultati;

            decimal ukupneRazlike = _rezultati.Sum(r => r.KursnaRazlikaRsd);
            TxtStatus.Text = $"Proračunato {_rezultati.Count} deviznih konta. Ukupna netovana kursna razlika: {ukupneRazlike:N2} RSD.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri proračunu kursnih razlika: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnIzracunaj_Click(object sender, RoutedEventArgs e)
    {
        Izracunaj();
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (!_rezultati.Any(r => r.KursnaRazlikaRsd != 0))
        {
            MessageBox.Show("Nema kursnih razlika za knjiženje.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            DateTime naDan = DpNaDan.SelectedDate ?? DateTime.Today;
            var (success, message, nalog) = await _service.ProknjiziValviranjeAsync(naDan, _rezultati);

            if (success)
            {
                MessageBox.Show($"✅ {message}", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                MessageBox.Show($"❌ {message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
