using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Godišnja stopa zatezne kamate koja važi počev od <see cref="DatumOd"/>, do sledeće
/// definisane stope (ili do danas ako je poslednja). 1:1 preneto iz ERPiFinansijeData —
/// ova tabela nema string/FK problem, isti oblik radi i u objedinjenoj šemi.
/// </summary>
public class KamatnaStopa
{
    [Key]
    public int KamatnaStopaId { get; set; }

    public DateTime DatumOd { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal GodisnjaStopaProcenat { get; set; }

    [MaxLength(200)]
    public string? Napomena { get; set; }
}
