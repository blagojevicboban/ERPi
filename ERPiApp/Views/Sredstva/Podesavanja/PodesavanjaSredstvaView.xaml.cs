using System.Windows;
using System.Windows.Controls;
using ERPiData;

namespace ERPiApp.Views.Sredstva.Podesavanja;

/// <summary>
/// Podešavanja Sredstva modula — za sada samo DOS/DBF uvoz (SREDSTVA.DBF/KARTICA.DBF/RASHOD.DBF/
/// PRIJAVA.DBF/KONTPLAN.DBF/KORISNIC.DBF, Faza 7.2b). Sam tok uvoza (skeniranje radnog direktorijuma,
/// izbor firme, log) je u <see cref="SredstvaDosImportWindow"/>, po uzoru na
/// <see cref="ERPiApp.Views.Finansije.DosImportWindow"/> — ovaj ekran samo otvara taj dijalog.
/// </summary>
public partial class PodesavanjaSredstvaView : UserControl
{
    private readonly ErpiDbContext _db;

    public PodesavanjaSredstvaView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private void BtnOtvoriDosUvoz_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SredstvaDosImportWindow(_db) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.NovaFirmaKreirana != null)
        {
            MessageBox.Show(
                $"Nova firma „{dlg.NovaFirmaKreirana.Naziv}“ je kreirana i registrovana.\n\nPristupite joj preko „Promeni firmu“ u zaglavlju aplikacije.",
                "Nova firma kreirana", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
