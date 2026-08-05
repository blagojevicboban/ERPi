using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Narudžbenica dobavljaču — evidencija naručene robe pre nego što se pretvori u ulaznu
/// kalkulaciju. Portovano iz ERPiFinansijeData.Models.NarudzbenicaDobavljacu (§3i u
/// PLAN_NASTAVKA.md), sa SifraArtikla→ArtikalId i NazivDobavljaca→PartnerId navigacijom
/// pretvorenim u prave FK-ove. Za razliku od izvora nosi i <see cref="MagacinId"/>: ERPi-jev
/// <see cref="Kalkulacija"/> zahteva magacin (nenullable), izvorni model ga nije imao jer
/// ERPiFinansije-in Kalkulacija nije bio magacinski vezan — vidi konverziju u
/// KomercijalaService.PretvoriNarudzbenicuUKalkulacijuAsync.
/// </summary>
public class NarudzbenicaDobavljacu
{
    [Key]
    public int NarudzbenicaId { get; set; }

    [MaxLength(30)]
    public string BrojNarudzbenice { get; set; } = string.Empty; // Npr. NAR-2026/001

    public DateTime Datum { get; set; } = DateTime.Today;
    public DateTime? RokIsporuke { get; set; } = DateTime.Today.AddDays(7);

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Naručeno"; // Naručeno, Delimično, Završeno, Otkazano

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoNeto { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoBruto { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    public int? KalkulacijaId { get; set; } // Link ka kreiranoj ulaznoj kalkulaciji
    [ForeignKey(nameof(KalkulacijaId))]
    public Kalkulacija? Kalkulacija { get; set; }

    public List<NarudzbenicaStavka> Stavke { get; set; } = new();
}

public class NarudzbenicaStavka
{
    [Key]
    public int NarudzbenicaStavkaId { get; set; }

    public int NarudzbenicaId { get; set; }
    [ForeignKey(nameof(NarudzbenicaId))]
    public NarudzbenicaDobavljacu? NarudzbenicaDobavljacu { get; set; }

    public int RedniBroj { get; set; }

    public int? ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal KolicinaNarucena { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18, 3)")]
    public decimal KolicinaPristigla { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PdvStopa { get; set; } = 20.0m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosNeto { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosBruto { get; set; }
}
