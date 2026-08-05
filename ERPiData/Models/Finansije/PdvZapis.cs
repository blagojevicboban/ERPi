using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Zapis u evidenciji PDV-a (KIR - Knjiga izlaznih računa, KPR - Knjiga primljenih računa).
/// </summary>
public class PdvZapis
{
    [Key]
    public int PdvZapisId { get; set; }

    public int? PartnerId { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? NalogId { get; set; }

    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    [Required]
    [MaxLength(10)]
    public string TipKnjige { get; set; } = "KPR"; // KPR ili KIR

    [Required]
    [MaxLength(50)]
    public string BrojDokumenta { get; set; } = string.Empty;

    public DateTime DatumDokumenta { get; set; } = DateTime.Now;

    public DateTime DatumPoreskogDogadjaja { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Osnovica { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal StopaPdv { get; set; } = 20.00m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Ukupno { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }
}
