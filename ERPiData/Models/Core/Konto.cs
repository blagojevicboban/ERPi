using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Core;

/// <summary>
/// Jedinstveni kontni plan preduzeća (sintetika i analitika) — preuzet iz ERPiFinansije bez
/// izmena, jer je tu već zreo i u aktivnom razvoju. Isti zapis koriste i knjiženja iz Finansija
/// i automatsko knjiženje iz Zarada/Sredstava (Faza 6) — po pravom stranom ključu, ne po
/// broju konta kao stringu (kako je moralo dok su baze bile odvojene).
/// </summary>
public class Konto
{
    [Key]
    public int KontoId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKonta { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NazivKonta { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VrstaKonta { get; set; } = "Aktivna";

    public bool IsSintetika { get; set; }
    public int Klasa { get; set; }

    [MaxLength(20)]
    public string? StariKonto { get; set; }

    [MaxLength(50)]
    public string? Ulica { get; set; }

    [MaxLength(50)]
    public string? Mesto { get; set; }

    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }

    /// <summary>"broj - naziv" za padajuće liste i pretragu konta.</summary>
    [NotMapped]
    public string Prikaz => $"{BrojKonta} - {NazivKonta}";
}
