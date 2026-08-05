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

    public int ArtikalId { get; set; }

    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RabatProcenat { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal MarzaProcenat { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PdvStopa { get; set; } = 20.00m;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosNabavni { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosProdajni { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }
}
