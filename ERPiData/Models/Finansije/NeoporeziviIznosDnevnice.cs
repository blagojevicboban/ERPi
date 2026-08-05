using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Finansije;

public class NeoporeziviIznosDnevnice
{
    [Key]
    public int NeoporeziviIznosDnevniceId { get; set; }

    public DateTime DatumOd { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal IznosZemljaRsd { get; set; }

    [MaxLength(200)]
    public string? Napomena { get; set; }
}
