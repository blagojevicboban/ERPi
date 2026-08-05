using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ERPiData;
using ERPiApp.Services.Finansije;

namespace ERPiApp.Views.Finansije;

public partial class DosImportWindow : Window
{
    private readonly ErpiDbContext _db;
    private List<DbfFirmaDto> _pronadjeneFirme = new();

    public DosImportWindow(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;

        string defaultPath = @"C:\KNJIGE\Radni";
        if (!Directory.Exists(defaultPath))
        {
            defaultPath = AppDomain.CurrentDomain.BaseDirectory;
        }

        TxtFolderPath.Text = defaultPath;
        SkenirajFolder(defaultPath);
    }

    private void SkenirajFolder(string folderPath)
    {
        try
        {
            _pronadjeneFirme = DosImportService.Instance.SkenirajRadniDirektorijum(folderPath);
            DgFirme.ItemsSource = _pronadjeneFirme;
            TxtFirmCount.Text = $"Pronađeno: {_pronadjeneFirme.Count} firmi";
            
            if (_pronadjeneFirme.Any())
            {
                DgFirme.SelectedItem = _pronadjeneFirme[0];
            }

            AppendLog($"Skeniran folder '{folderPath}'. Pronađeno {_pronadjeneFirme.Count} firmi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri skeniranju radnog foldera:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite radni direktorijum sa DOS/DBF podacima",
            InitialDirectory = TxtFolderPath.Text
        };

        if (dialog.ShowDialog() == true)
        {
            TxtFolderPath.Text = dialog.FolderName;
            SkenirajFolder(dialog.FolderName);
        }
    }

    private void DgFirme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgFirme.SelectedItem is DbfFirmaDto izabrana)
        {
            foreach (var f in _pronadjeneFirme)
            {
                f.IsSelected = (f == izabrana);
            }
            TxtStatus.Text = $"Izabrana firma: {izabrana.Naziv} ({izabrana.Sifra})";
        }
    }

    private async void BtnStartImport_Click(object sender, RoutedEventArgs e)
    {
        var izabranaFirma = DgFirme.SelectedItem as DbfFirmaDto ?? _pronadjeneFirme.FirstOrDefault(f => f.IsSelected);
        if (izabranaFirma == null)
        {
            MessageBox.Show("Molimo izaberite firmu iz tabele za uvoz.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool importFinansijsko = ChkFinansijsko.IsChecked == true;
        bool importRobno = ChkRobno.IsChecked == true;
        bool importMaterijalno = ChkMaterijalno.IsChecked == true;

        if (!importFinansijsko && !importRobno && !importMaterijalno)
        {
            MessageBox.Show("Molimo štiklirajte bar jedan modul za uvoz (Finansijsko, Robno ili Materijalno).", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool brisiPostojece = ChkBrisiPostojece.IsChecked == true;
        if (brisiPostojece)
        {
            var res = MessageBox.Show(
                $"UPOZORENJE: Izabrali ste opciju za BRISANJE postojećih podataka u izabranim modulima pre uvoza.\n\nDa li ste sigurni da želite obrisati postojeće podatke u aktivnoj bazi za izabrane module i izvršiti čisti uvoz iz firme '{izabranaFirma.Naziv}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;
        }

        BtnStartImport.IsEnabled = false;
        TxtLog.Text = "";
        AppendLog($"Započet uvoz firme '{izabranaFirma.Naziv}' ({izabranaFirma.Sifra}) u aktivnu bazu...");

        var progressHandler = new Progress<DosImportProgress>(p =>
        {
            PbProgress.Value = p.Percentage;
            TxtPercentage.Text = $"{p.Percentage}%";
            TxtStatus.Text = $"{p.FirmName} - {p.StepDescription}";
            if (!string.IsNullOrEmpty(p.LogMessage))
            {
                AppendLog(p.LogMessage);
            }
        });

        try
        {
            await DosImportService.Instance.UveziJednuFirmuAsync(
                _db,
                izabranaFirma,
                importFinansijsko,
                importRobno,
                importMaterijalno,
                brisiPostojece,
                progressHandler);

            MessageBox.Show($"Uvoz je uspešno završen za firmu '{izabranaFirma.Naziv}'!\n\nIzabrani moduli su uspešno zavedeni u aktivnu ERPi bazu.",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            string errDetails = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show($"Greška pri uvozu podataka:\n{ex.Message}\n\nDetalji: {errDetails}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnStartImport.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        TxtLog.ScrollToEnd();
    }
}
