using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Ulazna kalkulacija cene robe/materijala u magacinu (Veleprodaja/Ulaz).
/// </summary>
public class Kalkulacija
{
    [Key]
    public int KalkulacijaId { get; set; }

    public int MagacinId { get; set; }

    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    public int? PartnerId { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? KontoDobavljacaId { get; set; }

    [ForeignKey(nameof(KontoDobavljacaId))]
    public Konto? KontoDobavljaca { get; set; }

    public int BrojKalkulacije { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? BrojFaktureDobavljaca { get; set; }

    public DateTime? DatumFakture { get; set; }

    [MaxLength(50)]
    public string? BrojOtpremnice { get; set; }

    public DateTime? DatumOtpremnice { get; set; }

    [MaxLength(50)]
    public string? BrojRacuna { get; set; }

    public DateTime? DatumRacuna { get; set; }

    [MaxLength(30)]
    public string VrstaKalkulacije { get; set; } = "Veleprodaja";

    // Nabavka i zavisni troškovi
    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TransportniTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TroskoviUskladistenja { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UtovarIstovar { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TransportnoOsiguranje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OstaliTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaNabavno { get; set; }

    // Marža, porez i prodajna vrednost
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Razlika { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal MarzaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Porez { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal PoreskaStopaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoNabavna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoProdajna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    public bool IsKnjizen { get; set; }

    public int? NalogId { get; set; }

    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<StavkaKalkulacije> Stavke { get; set; } = new();
}

