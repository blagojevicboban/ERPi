using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Ponuda ili predračun (proforma faktura) kupcu — pre stvarnog izdavanja fakture.
/// Portovano iz ERPiFinansijeData.Models.PonudaPredracun (§3i u PLAN_NASTAVKA.md), sa
/// SifraArtikla→ArtikalId i cache-ovanim NazivPartnera→PartnerId navigacijom pretvorenim u
/// prave FK-ove (isti obrazac kao <see cref="RacunOtpremnica"/>).
/// </summary>
public class PonudaPredracun
{
    [Key]
    public int PonudaPredracunId { get; set; }

    [MaxLength(30)]
    public string BrojDokumenta { get; set; } = string.Empty; // Npr. PON-2026/001 ili PRD-2026/001

    [MaxLength(20)]
    public string VrstaDokumenta { get; set; } = "Ponuda"; // "Ponuda" ili "Predračun"

    public DateTime Datum { get; set; } = DateTime.Today;
    public DateTime RokVazenja { get; set; } = DateTime.Today.AddDays(15);

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Nacrt"; // Nacrt, Poslato, Prihvaćeno, Fakturisano, Odbijeno

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoNeto { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoBruto { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    public int? RacunOtpremnicaId { get; set; } // Link ka kreiranom izlaznom računu
    [ForeignKey(nameof(RacunOtpremnicaId))]
    public RacunOtpremnica? RacunOtpremnica { get; set; }

    public List<PonudaStavka> Stavke { get; set; } = new();
}

public class PonudaStavka
{
    [Key]
    public int PonudaStavkaId { get; set; }

    public int PonudaPredracunId { get; set; }
    [ForeignKey(nameof(PonudaPredracunId))]
    public PonudaPredracun? PonudaPredracun { get; set; }

    public int RedniBroj { get; set; }

    public int? ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Kolicina { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RabatProcenat { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PdvStopa { get; set; } = 20.0m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosNeto { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosBruto { get; set; }
}
