using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Magacin;

public class TrebovanjeNalog
{
    [Key]
    public int TrebovanjeNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    public int MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    public bool IsKnjizen { get; set; }

    public List<TrebovanjeStavka> Stavke { get; set; } = new();
}

public class TrebovanjeStavka
{
    [Key]
    public int TrebovanjeStavkaId { get; set; }

    public int TrebovanjeNalogId { get; set; }
    [ForeignKey(nameof(TrebovanjeNalogId))]
    public TrebovanjeNalog? TrebovanjeNalog { get; set; }

    public int RedniBroj { get; set; }

    /// <summary>Materijalno (ne Robno) knjigovodstvo — FK na <see cref="Materijal"/>, ne na <see cref="Artikal"/>.</summary>
    public int MaterijalId { get; set; }
    [ForeignKey(nameof(MaterijalId))]
    public Materijal? Materijal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    [MaxLength(20)]
    public string? KontoTroska { get; set; }
}
