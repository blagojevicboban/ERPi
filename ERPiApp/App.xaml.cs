using System.Windows;
using ERPiApp.Views.Firma;

namespace ERPiApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prvi ekran je uvek izbor firme, ne login — jedna baza po firmi (Faza 1), pa se
        // korisničko ime/lozinka proveravaju tek pošto se zna koja baza se otvara.
        new CompanySelectWindow().Show();
    }
}
