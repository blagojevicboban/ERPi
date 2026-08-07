using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Pojedinačna stavka robe/materijala na ulaznoj kalkulaciji.
/// </summary>
public class StavkaKalkulacije
{
    [Key]
    public int StavkaKalkulacijeId { get; set; }

    public int KalkulacijaId { get; set; }

    [ForeignKey(nameof(KalkulacijaId))]
    public Kalkulacija? Kalkulacija { get; set; }

    public int RedniBroj { get; set; }

    public int ArtikalId { get; set; }

    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    /// <summary>Kolicina * NabavnaCena (fakturna vrednost stavke bez zavisnih troškova).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    /// <summary>Srazmerni deo zavisnih troškova (Kalkulacija.SvegaTroskovi) raspodeljen na ovu stavku.</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Troskovi { get; set; }

    /// <summary>Iznos + Troskovi (ukupna nabavna vrednost stavke u skladištu).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 6)")]
    public decimal RazlikaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednostBezPoreza { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal PorezProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    /// <summary>ProdajnaVrednost / Kolicina — prodajna cena po jedinici mere.</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal StaraCena { get; set; }

    // Backward compatibility aliases
    [Column(TypeName = "decimal(5, 2)")]
    public decimal RabatProcenat { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal MarzaProcenat { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PdvStopa { get; set; } = 20.00m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosNabavni { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosProdajni { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }
}

