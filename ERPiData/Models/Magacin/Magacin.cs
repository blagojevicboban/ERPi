using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Šifarnik magacina preduzeća (veleprodaja, maloprodaja, materijalni magacin).
/// </summary>
public class Magacin
{
    [Key]
    public int MagacinId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraMagacina { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NazivMagacina { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OdgovornoLice { get; set; }

    [MaxLength(30)]
    public string VrstaMagacina { get; set; } = "Veleprodaja";

    /// <summary>
    /// Format "šifra - naziv" za prikaze u padajućim listama.
    /// </summary>
    [NotMapped]
    public string Prikaz => string.IsNullOrWhiteSpace(SifraMagacina)
        ? NazivMagacina
        : $"{SifraMagacina} - {NazivMagacina}";
}
