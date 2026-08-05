namespace ERPiData.Services;

/// <summary>
/// Konta na koja se knjiže robna dokumenta.
/// Portovano iz ERPiFinansijeData.
/// </summary>
public static class RobnaKonta
{
    public const string RobaVeleprodaja = "1320";
    public const string RazlikaUCeniVeleprodaja = "1329";
    public const string RobaMaloprodaja = "1340";
    public const string UkalkulisaniPdvMaloprodaja = "1344";
    public const string UkalkulisaniPdvMaloprodajaPosebnaStopa = "13441";
    public const string RazlikaUCeniMaloprodaja = "1348";

    public static string RobaZaVrstuMagacina(string? vrstaMagacina)
        => vrstaMagacina == "Maloprodaja" ? RobaMaloprodaja : RobaVeleprodaja;

    public static string RazlikaZaVrstuMagacina(string? vrstaMagacina)
        => vrstaMagacina == "Maloprodaja" ? RazlikaUCeniMaloprodaja : RazlikaUCeniVeleprodaja;

    public static string UkalkulisaniPdvZaStopu(decimal poreskaStopaProcenat)
        => poreskaStopaProcenat >= 18m ? UkalkulisaniPdvMaloprodaja : UkalkulisaniPdvMaloprodajaPosebnaStopa;
}
