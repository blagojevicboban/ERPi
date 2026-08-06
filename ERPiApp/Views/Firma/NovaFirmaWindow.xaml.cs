using System.IO;
using System.Windows;
using ERPiApp.Models;
using ERPiApp.Services;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp.Views.Firma;

public partial class NovaFirmaWindow : Window
{
    private readonly CompanyRegistryService _registry;

    /// <summary>Popunjen tek nakon uspešnog kreiranja — čita ga pozivalac (CompanySelectWindow).</summary>
    public CompanyEntry? Kreirana { get; private set; }

    public NovaFirmaWindow(CompanyRegistryService registry)
    {
        InitializeComponent();
        _registry = registry;
        TxtNaziv.Focus();
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e) => Close();

    private void BtnKreiraj_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        var naziv = TxtNaziv.Text.Trim();
        if (string.IsNullOrEmpty(naziv))
        {
            ShowError("Naziv firme je obavezan.");
            return;
        }

        var sifra = TxtSifra.Text.Trim();
        if (string.IsNullOrEmpty(sifra))
        {
            // Bez šifre baza dobija generičko ime i vremenski žig, da se ne obriše/preklopi
            // sledeća firma bez šifre.
            sifra = "F" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        var fileSafeNaziv = string.Concat(naziv.Split(Path.GetInvalidFileNameChars()));
        var dbPath = Path.Combine(_registry.DefaultDataDirectory, $"{sifra}_{fileSafeNaziv}.db");

        if (File.Exists(dbPath))
        {
            ShowError("Baza sa ovim imenom već postoji na disku. Promenite šifru ili naziv.");
            return;
        }

        try
        {
            using var db = ErpiDbContext.Create(dbPath);
            db.Firme.Add(new ERPiData.Models.Core.Firma
            {
                Sifra = sifra,
                Naziv = naziv,
                Pib = string.IsNullOrWhiteSpace(TxtPib.Text) ? null : TxtPib.Text.Trim(),
                MaticniBroj = string.IsNullOrWhiteSpace(TxtMaticniBroj.Text) ? null : TxtMaticniBroj.Text.Trim(),
                Adresa = string.IsNullOrWhiteSpace(TxtAdresa.Text) ? null : TxtAdresa.Text.Trim(),
                PttIMesto = string.IsNullOrWhiteSpace(TxtPttIMesto.Text) ? null : TxtPttIMesto.Text.Trim(),
                Telefon = string.IsNullOrWhiteSpace(TxtTelefon.Text) ? null : TxtTelefon.Text.Trim(),
                ZiroRacun = string.IsNullOrWhiteSpace(TxtZiroRacun.Text) ? null : TxtZiroRacun.Text.Trim()
            });
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            ShowError($"Kreiranje baze nije uspelo: {ex.Message}");
            return;
        }

        Kreirana = new CompanyEntry
        {
            Sifra = sifra,
            Naziv = naziv,
            Pib = TxtPib.Text.Trim(),
            DbPath = dbPath
        };

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
