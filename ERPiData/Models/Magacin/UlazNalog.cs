using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Finansije;

namespace ERPiData.Models.Magacin;

public class UlazNalog
{
    [Key]
    public int UlazNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    public int MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    [MaxLength(30)]
    public string? BrojRacuna { get; set; }

    public DateTime? DatumRacuna { get; set; }

    public bool IsKnjizen { get; set; }

    public List<UlazStavka> Stavke { get; set; } = new();
}

public class UlazStavka
{
    [Key]
    public int UlazStavkaId { get; set; }

    public int UlazNalogId { get; set; }
    [ForeignKey(nameof(UlazNalogId))]
    public UlazNalog? UlazNalog { get; set; }

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
}

public class PrimopredajaNalog
{
    [Key]
    public int PrimopredajaNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    public int MagacinIdDaje { get; set; }
    [ForeignKey(nameof(MagacinIdDaje))]
    public Magacin? MagacinDaje { get; set; }

    public int MagacinIdPrima { get; set; }
    [ForeignKey(nameof(MagacinIdPrima))]
    public Magacin? MagacinPrima { get; set; }

    [Required]
    [MaxLength(30)]
    public string VrstaDokumenta { get; set; } = "Primopredaja";

    public bool IsKnjizen { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal StopaPdv { get; set; } = 20m;

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<PrimopredajaStavka> Stavke { get; set; } = new();
}

public class PrimopredajaStavka
{
    [Key]
    public int PrimopredajaStavkaId { get; set; }

    public int PrimopredajaNalogId { get; set; }
    [ForeignKey(nameof(PrimopredajaNalogId))]
    public PrimopredajaNalog? PrimopredajaNalog { get; set; }

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
}
