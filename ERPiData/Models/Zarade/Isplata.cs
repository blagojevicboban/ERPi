using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum RodIsplate
{
    Zarada = 0,
    VanRadnogOdnosa = 1
}

public enum VrstaIsplate
{
    KonacnaZarada = 0,
    Akontacija = 1,
    Bonus = 2,
    TrinaestaPlata = 3,
    Ostalo = 9
}

public interface IPripadaIsplati
{
    int Godina { get; }
    int Mesec { get; }
    int? IsplataId { get; }
}

[Table("Isplate")]
public class Isplata
{
    [Key]
    public int IsplataId { get; set; }

    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int RedniBroj { get; set; } = 1;

    public RodIsplate Rod { get; set; } = RodIsplate.Zarada;
    public VrstaIsplate Vrsta { get; set; } = VrstaIsplate.KonacnaZarada;

    [MaxLength(80)]
    public string Opis { get; set; } = "";

    public DateTime DatumIsplate { get; set; }
    public DateTime DatumKreiranja { get; set; } = DateTime.Now;

    public ICollection<ObracunPlate> Obracuni { get; set; } = [];

    [NotMapped]
    public bool JePrva => Rod == RodIsplate.Zarada && RedniBroj <= 1;

    [NotMapped]
    public string OznakaZaKonacnuIsplatu
        => Rod == RodIsplate.VanRadnogOdnosa || Vrsta != VrstaIsplate.Akontacija ? "K" : "A";

    [NotMapped]
    public bool NosiObustave => Rod == RodIsplate.Zarada && Vrsta == VrstaIsplate.KonacnaZarada;

    [NotMapped]
    public bool JeVanRadnogOdnosa => Rod == RodIsplate.VanRadnogOdnosa;

    [NotMapped]
    public string PeriodStr => $"{Mesec:D2}/{Godina}";

    [NotMapped]
    public string NazivKratki => string.IsNullOrWhiteSpace(Opis)
        ? (Rod == RodIsplate.VanRadnogOdnosa ? NazivRoda : NazivVrste(Vrsta))
        : Opis.Trim();

    [NotMapped]
    public string NazivRoda => Rod == RodIsplate.VanRadnogOdnosa ? "Naknade po ugovoru" : "Zarada";

    [NotMapped]
    public string Naziv => $"{RedniBroj}. {NazivKratki}";

    public static string NazivVrste(VrstaIsplate vrsta) => vrsta switch
    {
        VrstaIsplate.KonacnaZarada => "Konačna zarada",
        VrstaIsplate.Akontacija => "Akontacija",
        VrstaIsplate.Bonus => "Bonus",
        VrstaIsplate.TrinaestaPlata => "13. plata",
        _ => "Ostalo"
    };
}
