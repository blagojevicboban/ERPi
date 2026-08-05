using ERPiData.Models.Core;

namespace ERPiApp;

public static class AppSession
{
    public static Korisnik? TrenutniKorisnik { get; set; }
    public static Firma? TrenutnaFirma { get; set; }

    public static bool IsAdministrator => TrenutniKorisnik?.Uloga == UlogaKorisnika.Administrator;

    /// <summary>Poziva se pri odjavi/promeni firme, da naredna sesija ne nasledi tuđe podatke.</summary>
    public static void Ocisti()
    {
        TrenutniKorisnik = null;
        TrenutnaFirma = null;
    }
}
