using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Sredstva;

/// <summary>
/// Osnovno sredstvo (registar) — port iz ERPiSredstvaData.Models.Sredstvo. Odgovara SREDSTVA.DBF.
/// Razlika od izvora: <c>Konto</c> (string) je postao <see cref="KontoId"/>, pravi strani ključ
/// ka jedinstvenom kontnom planu (<see cref="Core.Konto"/>) — isti obrazac kao svuda drugde u
/// ERPi (vidi doc komentar na <c>StavkaNaloga.KontoId</c>).
/// </summary>
public class Sredstvo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string InventarskiBroj { get; set; } = string.Empty;

    [NotMapped]
    public bool IsSelected { get; set; }

    [NotMapped]
    public string InventarskiBrojSort => System.Text.RegularExpressions.Regex.Replace(InventarskiBroj ?? "", @"\d+", m => m.Value.PadLeft(20, '0'));

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    public DateTime DatumNabavke { get; set; }

    public DateTime DatumAktiviranja { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IspravkaVrednosti { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SadasnjaVrednost { get; set; }

    /// <summary>Računovodstvena amortizaciona grupa (I–V po MRS 16) — katalog kod, ne FK (vidi <c>PoreskaGrupaCatalog</c>).</summary>
    [MaxLength(10)]
    public string AmortizacionaGrupa { get; set; } = string.Empty;

    /// <summary>Konto osnovnog sredstva (grupa 02x) u jedinstvenom kontnom planu.</summary>
    public int? KontoId { get; set; }
    public Konto? Konto { get; set; }

    /// <summary>Legacy DOS obračunska organizaciona jedinica — nema svoj šifarnik ni u izvornom ERPiSredstva, zadržano kao numerički kod.</summary>
    public int ObracunskaJedinica { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal StopaAmortizacije { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RezidualnaVrednost { get; set; } = 0m;

    /// <summary>Poreska amortizaciona grupa (Obrazac OA, Pravilnik o poreskoj amortizaciji) — katalog kod I–V.</summary>
    [MaxLength(10)]
    public string PoreskaGrupa { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal PoreskaStopa { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PoreskaNabavnaVrednost { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PoreskaIspravkaVrednosti { get; set; } = 0m;

    public bool JeAktivno { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Kolicina { get; set; } = 1;

    /// <summary>Originalna SIFRA iz SREDSTVA.DBF — za veze sa Karticom i Prijavom pri DOS uvozu.</summary>
    public int LegacySifra { get; set; }

    // Navigation
    public ICollection<Kartica> Kartice { get; set; } = new List<Kartica>();
    public ICollection<Prijava> Prijave { get; set; } = new List<Prijava>();
    public ICollection<Rashod> Rashodi { get; set; } = new List<Rashod>();
}
