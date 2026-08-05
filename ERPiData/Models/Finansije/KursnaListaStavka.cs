using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Finansije;

public class KursnaListaStavka
{
    [Key]
    public int KursnaListaStavkaId { get; set; }

    public DateTime Datum { get; set; }

    [Required]
    [MaxLength(10)]
    public string ValutaOznaka { get; set; } = string.Empty;

    public int ValutaSifra { get; set; }

    [MaxLength(100)]
    public string NazivValute { get; set; } = string.Empty;

    public int Jedinica { get; set; } = 1;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal SrednjiKurs { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal KupovniKurs { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdavniKurs { get; set; }
}
