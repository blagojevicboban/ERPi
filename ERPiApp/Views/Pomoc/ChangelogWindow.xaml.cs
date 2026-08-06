using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPiApp.Views.Pomoc;

public partial class ChangelogWindow : Window
{
    public ChangelogWindow()
    {
        InitializeComponent();

        DodajVerziju("v3.0.0 — Objedinjeni ERPi Hub (2026)", new[]
        {
            "🌟 Spajanje modula Finansije, Osnovna sredstva i Obračun zarada u jedinstveni ERPi desktop paket.",
            "🗄️ Jedinstvena SQLite baza podataka — sve tabele firmi, partnera, konta i dokumenata na jednom mestu.",
            "❓ Objedinjeno korisničko uputstvo sa pretragom preko 60 tema i izvozom HTML priručnika.",
            "⚡ Novi brzi sistem navigacije i integrisani F1 help za sve module."
        });

        DodajVerziju("v2.5.0 — ERPi Finansije, Sredstva & Zarade", new[]
        {
            "📜 Evidencija komercijalnih ponuda, OCR parsiranje ulaznih računa i bankarski matching engine.",
            "🏗️ Obračun računovodstvene (MRS 16) i poreske amortizacije (Obrazac OA).",
            "👥 Obračun zarada sa izvozom PPP-PD XML i platnih spiskova."
        });
    }

    private void DodajVerziju(string naslovVerzije, string[] stavke)
    {
        PnlChangelog.Children.Add(new TextBlock
        {
            Text = naslovVerzije,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["PrimaryBrush"],
            Margin = new Thickness(0, 8, 0, 8)
        });

        foreach (var st in stavke)
        {
            PnlChangelog.Children.Add(new TextBlock
            {
                Text = $"• {st}",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 0, 0, 4)
            });
        }

        PnlChangelog.Children.Add(new Separator { Background = (Brush)Application.Current.Resources["BorderBrush"], Margin = new Thickness(0, 12, 0, 12) });
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
