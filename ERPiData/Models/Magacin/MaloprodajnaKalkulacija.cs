using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Finansije;

namespace ERPiData.Models.Magacin;

public class MaloprodajnaKalkulacija
{
    [Key]
    public int MaloprodajnaKalkulacijaId { get; set; }

    public int SifraProdavnice { get; set; }

    public int BrojKalkulacije { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? SifraMagacinaPrima { get; set; }

    [MaxLength(20)]
    public string? SifraMagacinaDaje { get; set; }

    [MaxLength(20)]
    public string? SifraDobavljaca { get; set; }

    [MaxLength(30)]
    public string? BrojOtpremnice { get; set; }
    public DateTime? DatumOtpremnice { get; set; }

    [MaxLength(30)]
    public string? BrojRacuna { get; set; }
    public DateTime? DatumRacuna { get; set; }

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

    public bool IsKnjizen { get; set; }
    public bool IsTrgovinskiKnjizen { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal RabatPri { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaNabavno { get; set; }

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
    public decimal RabatIznos { get; set; }

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<MaloprodajnaKalkulacijaStavka> Stavke { get; set; } = new();
}

public class MaloprodajnaKalkulacijaStavka
{
    [Key]
    public int MaloprodajnaKalkulacijaStavkaId { get; set; }

    public int MaloprodajnaKalkulacijaId { get; set; }
    [ForeignKey(nameof(MaloprodajnaKalkulacijaId))]
    public MaloprodajnaKalkulacija? MaloprodajnaKalkulacija { get; set; }

    public int RedniBroj { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Troskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 6)")]
    public decimal RazlikaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednostBezPoreza { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal PorezProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal PosebanPorezProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PosebanPorezIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPorez { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPosebanPorez { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezZaUplatu { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Taksa { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [MaxLength(10)]
    public string? TarifniBroj { get; set; }

    public int? BrojRazduzenja { get; set; }

    public bool IsKnjizen { get; set; }
    public bool IsTrgovinskiKnjizen { get; set; }

    [NotMapped]
    public string? NazivArtikla { get; set; }

    [NotMapped]
    public string? JedinicaMere { get; set; }
}
