using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Šifarnik poreskih tarifa (tarifni brojevi). Portovano iz ERPiFinansijeData, 1:1 — samostalan
/// šifarnik bez FK zavisnosti, analogno legacy tarife.dbf (tar_broj, porez_pr, pos_p_pr, por_u_cen)
/// iz MAT6.PRG / tarifni().
/// </summary>
public class PoreskaTarifa
{
    [Key]
    public int PoreskaTarifaId { get; set; }

    [Required]
    [MaxLength(2)]
    public string TarifniBroj { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PorezProcenat { get; set; }

    /// <summary>Posebna (dodatna) poreska stopa — legacy POS_P_PR.</summary>
    [Column(TypeName = "decimal(5, 2)")]
    public decimal PosebanPorezProcenat { get; set; }

    public bool PorezUCeni { get; set; }

    [NotMapped]
    public string Prikaz => string.IsNullOrEmpty(TarifniBroj)
        ? "(bez tarife)"
        : PorezUCeni ? $"{TarifniBroj} - {PorezProcenat:N0}% (u ceni)" : $"{TarifniBroj} - {PorezProcenat:N0}%";
}
