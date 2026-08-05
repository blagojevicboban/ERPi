using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Finansije;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Robna (Artikal-bazirana) varijanta internog kretanja robe između magacina — pokriva tri
/// tab-a iz izvornog <c>ERPiFinansijeApp/Views/Trgovina/TrgovinaView.xaml</c> (Primopredaje/
/// Zaduženja/Razduženja), koji u izvoru dele istu tabelu i razlikuju se samo preko
/// <see cref="VrstaDokumenta"/> (vidi PLAN_NASTAVKA.md §3i).
///
/// Namerno ODVOJENO od <see cref="PrimopredajaNalog"/> (koji je Materijalno/<c>MaterijalId</c>-
/// bazirano, Faza 3.12/3g) — izvorne tabele su različite (Robno radi nad Artikal šifarnikom,
/// Materijalno nad Materijal šifarnikom), pa i modeli ostaju odvojeni, isti obrazac kao
/// Kalkulacija (Robno) naspram Ulaz/Trebovanje (Materijalno).
/// </summary>
public static class VrstaRobnogKretanja
{
    public const string Primopredaja = "Primopredaja";
    public const string Zaduzenje = "Zaduženje";
    public const string Razduzenje = "Razduženje";
}

public class RobnoKretanjeNalog
{
    [Key]
    public int RobnoKretanjeNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    public int MagacinIdDaje { get; set; }
    [ForeignKey(nameof(MagacinIdDaje))]
    public Magacin? MagacinDaje { get; set; }

    public int MagacinIdPrima { get; set; }
    [ForeignKey(nameof(MagacinIdPrima))]
    public Magacin? MagacinPrima { get; set; }

    /// <summary>"Primopredaja" / "Zaduženje" / "Razduženje" — vidi <see cref="VrstaRobnogKretanja"/>.</summary>
    [Required]
    [MaxLength(30)]
    public string VrstaDokumenta { get; set; } = VrstaRobnogKretanja.Primopredaja;

    public bool IsKnjizen { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal StopaPdv { get; set; } = 20m;

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<RobnoKretanjeStavka> Stavke { get; set; } = new();
}

public class RobnoKretanjeStavka
{
    [Key]
    public int RobnoKretanjeStavkaId { get; set; }

    public int RobnoKretanjeNalogId { get; set; }
    [ForeignKey(nameof(RobnoKretanjeNalogId))]
    public RobnoKretanjeNalog? RobnoKretanjeNalog { get; set; }

    public int RedniBroj { get; set; }

    /// <summary>Robno (ne Materijalno) knjigovodstvo — FK na <see cref="Artikal"/>.</summary>
    public int ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }
}
