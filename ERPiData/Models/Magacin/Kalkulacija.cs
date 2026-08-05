using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Magacin;

/// <summary>
/// Ulazna kalkulacija cene robe/materijala u magacinu.
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

    public int BrojKalkulacije { get; set; }

    [MaxLength(50)]
    public string? BrojFaktureDobavljaca { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    public DateTime? DatumFakture { get; set; }

    [MaxLength(30)]
    public string VrstaKalkulacije { get; set; } = "Veleprodaja";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoNabavna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoProdajna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    public List<StavkaKalkulacije> Stavke { get; set; } = new();
}
