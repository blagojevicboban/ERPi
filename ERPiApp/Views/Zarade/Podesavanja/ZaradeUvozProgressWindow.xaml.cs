using System.Windows;

namespace ERPiApp.Views.Zarade.Podesavanja;

/// <summary>
/// Mali "u toku" dijalog sa indeterminate progres trakom i uživo log prikazom, koji se
/// otvara dok traje uvoz podataka u <see cref="PodesavanjaZaradeView"/> (i ERPiZarade EF-to-EF
/// i DOS/DBF put) — operacija može trajati po nekoliko minuta bez ijednog druge vizuelne povratne
/// informacije osim ove, pa korisnik ne treba da posumnja da je aplikacija "zamrznuta".
/// Ne podržava otkazivanje — samo prikazuje napredak; zatvara ga pozivalac kad operacija završi.
/// </summary>
public partial class ZaradeUvozProgressWindow : Window
{
    public ZaradeUvozProgressWindow(string naslov)
    {
        InitializeComponent();
        Title = naslov;
        TxtNaslov.Text = $"⚡ {naslov}";
    }

    /// <summary>Dodaje liniju u log i postavlja je kao trenutni status (poslednja linija = najsvežija).</summary>
    public void AppendLog(string linija)
    {
        TxtLog.Text = TxtLog.Text.Length > 0 ? TxtLog.Text + "\n" + linija : linija;
        LogScroll.ScrollToEnd();
        TxtStatus.Text = linija;
    }
}
