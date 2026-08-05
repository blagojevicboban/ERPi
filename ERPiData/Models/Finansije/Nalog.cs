using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Finansije;

/// <summary>Stanje naloga — nacrt se sme menjati slobodno, proknjižen ulazi u zvanične kartice/bilanse.</summary>
public enum StatusNaloga
{
    Nacrt = 0,
    Proknjizen = 1
}

/// <summary>
/// Dnevnik glavne knjige. Isti nalog nastaje i ručnim unosom ovde, i automatski iz drugih
/// modula (obračun zarada, amortizacija — Faza 6) — po tome IzvorModula/IzvorId znaju odakle
/// je nalog potekao, da se automatski nalog ne duplira niti ručno menja mimo izvora.
/// </summary>
public class Nalog
{
    [Key]
    public int NalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime DatumNaloga { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string VrstaNaloga { get; set; } = "Finansijski";

    [MaxLength(250)]
    public string? Opis { get; set; }

    public StatusNaloga Status { get; set; } = StatusNaloga.Nacrt;
    public DateTime? DatumKnjizenja { get; set; }

    /// <summary>Modul koji je nalog automatski kreirao (npr. "Zarade", "Sredstva"); null za ručni unos ovde.</summary>
    [MaxLength(30)]
    public string? IzvorModula { get; set; }

    /// <summary>Id zapisa u izvornom modulu (npr. ObracunPlate.Id) — sprečava dupli automatski nalog za isti obračun.</summary>
    public int? IzvorId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoDuguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPotrazuje { get; set; }

    [NotMapped]
    public decimal Saldo => UkupnoDuguje - UkupnoPotrazuje;

    [NotMapped]
    public bool IsUravnotezen => Math.Abs(Saldo) < 0.01m;

    public List<StavkaNaloga> Stavke { get; set; } = new();
}
