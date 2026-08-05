using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Evidencija PFR (Process Fiscal Receipts) e-Fiskalizovanog računa.
/// </summary>
public class PfrRacun
{
    [Key]
    public int PfrRacunId { get; set; }

    public int? PartnerId { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [Required]
    [MaxLength(50)]
    public string BrojRacuna { get; set; } = string.Empty;

    public DateTime Datum { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string TipRacuna { get; set; } = "PrometProdaja"; // PrometProdaja, PrometRefunkcija, AvansProdaja

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    [MaxLength(100)]
    public string? PfrBroj { get; set; }

    [MaxLength(500)]
    public string? QrKodUrl { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Fiskalizovan"; // Fiskalizovan, Greška, Storniran

    [MaxLength(250)]
    public string? Napomena { get; set; }
}
