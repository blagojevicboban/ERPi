using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Šifarnik robe i materijala.
/// </summary>
public class Artikal
{
    [Key]
    public int ArtikalId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(20)]
    public string JedinicaMere { get; set; } = "kom";

    [MaxLength(50)]
    public string? Pakovanje { get; set; }

    [MaxLength(20)]
    public string? TarifniBroj { get; set; }

    [MaxLength(50)]
    public string? Barkod { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PdvStopa { get; set; } = 20.00m;

    [MaxLength(20)]
    public string? KlasifikacionaSifra { get; set; }

    /// <summary>
    /// Format "šifra - naziv (JM)" za padajuće liste u editoru kalkulacije.
    /// </summary>
    [NotMapped]
    public string Prikaz => $"{SifraArtikla} - {Naziv} ({JedinicaMere})";
}
