using System.Windows;
using System.Windows.Controls;

namespace ERPiApp.Views.Shell;

public partial class PodesavanjaView : UserControl
{
    public PodesavanjaView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Prikaži trenutno stanje toggle-a
        TglInfoTraka.IsChecked = AppConfig.PrikaziInfoTraku;

        // Prikaži info o verziji i putanjama
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVerzija.Text = $"v{version?.ToString(3)} ({System.IO.Path.GetFileName(AppConfig.DbPath)})";
        TxtDbPath.Text = AppConfig.DbPath;
        TxtSettingsPath.Text = System.IO.Path.Combine(AppConfig.AppDataDir, "ui_settings.json");
    }

    private void TglInfoTraka_Checked(object sender, RoutedEventArgs e)
    {
        AppConfig.PrikaziInfoTraku = true;
        RefreshInfoTraka();
    }

    private void TglInfoTraka_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfig.PrikaziInfoTraku = false;
        RefreshInfoTraka();
    }

    private static void RefreshInfoTraka()
    {
        // Pronađi MainWindow i ažuriraj vidljivost info trake odmah
        if (Application.Current.MainWindow is MainWindow mw)
            mw.UpdateInfoTrakaVisibility();
    }
}
