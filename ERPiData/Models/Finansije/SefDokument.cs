using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Status i podaci e-Fakture na Sistem e-Faktura (SEF API / UBL 2.1).
/// </summary>
public class SefDokument
{
    [Key]
    public int SefDokumentId { get; set; }

    public int? PartnerId { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [Required]
    [MaxLength(50)]
    public string BrojDokumenta { get; set; } = string.Empty;

    public DateTime DatumDokumenta { get; set; } = DateTime.Now;

    public DateTime? DatumSlanja { get; set; }

    [MaxLength(50)]
    public string? CirId { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Nacrt"; // Nacrt, Poslato, Odobreno, Odbijeno

    [MaxLength(20)]
    public string TipDokumenta { get; set; } = "Faktura"; // Faktura, AvansnaFaktura, KnjižnoOdobrenje

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Osnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Ukupno { get; set; }

    public string? UblXmlContent { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }
}
