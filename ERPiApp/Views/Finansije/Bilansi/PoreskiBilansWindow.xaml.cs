using System;
using System.Collections.Generic;
using System.Windows;
using ERPiData;
using ERPiData.Models.Finansije;
using ERPiData.Services;

namespace ERPiApp.Views.Finansije.Bilansi;

/// <summary>
/// Poreski bilans i prijava poreza na dobit (PB-1 / OA / PDP). Port iz ERPiFinansije
/// (Views/Bilansi/PoreskiBilansWindow), otvara se iz <see cref="BilansiAprView"/>-ovog
/// "📜 Poreski Bilans" dugmeta, sa deljenim <see cref="ErpiDbContext"/> firme umesto
/// sopstvene SQLite konekcije (izvor je otvarao AccountingDbContext direktno na AppConfig.DbPath).
/// </summary>
public partial class PoreskiBilansWindow : Window
{
    private readonly ErpiDbContext _db;
    private readonly int _godina;
    private List<Pb1Stavka> _pb1Stavke = new();
    private List<PoreskaAmortizacijaStavka> _oaStavke = new();
    private ObrazacPdpResult? _pdpResult;

    public PoreskiBilansWindow(ErpiDbContext db, int godina)
    {
        InitializeComponent();
        _db = db;
        _godina = godina;
        Loaded += PoreskiBilansWindow_Loaded;
    }

    private async void PoreskiBilansWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = new PoreskiBilansService(_db);

            // 1. Obrazac PB-1
            var (pb1, oporezivaDobit, obracunatiPorez) = await service.GenerisiPoreskiBilansPb1Async(_godina);
            _pb1Stavke = pb1;
            DgPb1.ItemsSource = _pb1Stavke;

            // 2. Obrazac OA
            _oaStavke = await service.GenerisiPoreskuAmortizacijuOaAsync(_godina);
            DgOa.ItemsSource = _oaStavke;

            // 3. Obrazac PDP
            _pdpResult = await service.GenerisiObrazacPdpAsync(_godina);
            TxtPdpFirma.Text = _pdpResult.NazivObveznika;
            TxtPdpPib.Text = _pdpResult.Pib;
            TxtPdpOporezivaDobit.Text = $"{_pdpResult.OporezivaDobit:N2} RSD";
            TxtPdpObracunatiPorez.Text = $"{_pdpResult.ObracunatiPorez:N2} RSD";
            TxtPdpAkontacija.Text = $"{_pdpResult.MesecnaAkontacija:N2} RSD / mesečno";

            TxtStatus.Text = $"Poreski bilans PB-1 i prijava PDP za {_godina}. godinu su uspešno obračunati.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Greška pri obračunu.";
            MessageBox.Show($"Greška pri obračunu Poreskog Bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ERPiApp.Services.ExcelExportService.ExportDataGridToExcel(DgPb1, $"Obrazac_PB1_{_godina}", $"Poreski_Bilans_PB1_{_godina}");

    private void BtnZatvori_Click(object sender, RoutedEventArgs e) => Close();
}
