using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;

namespace ERPiData.Models.Magacin;

public enum TipRacunOtpremnice
{
    Racun = 0,
    Predracun = 1
}

public class RacunOtpremnica
{
    [Key]
    public int RacunOtpremnicaId { get; set; }

    public TipRacunOtpremnice TipDokumenta { get; set; } = TipRacunOtpremnice.Racun;

    public DateTime? RokVazenjaPredracuna { get; set; }

    public int BrojRacuna { get; set; }

    public DateTime DatumRacuna { get; set; } = DateTime.Now;

    public DateTime? RokPlacanja { get; set; }

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoOsnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoRabat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoZaUplatu { get; set; }

    public bool IsKnjizen { get; set; }

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public long? SefId { get; set; }
    public SefStatusFakture SefStatus { get; set; } = SefStatusFakture.NijePoslata;
    public DateTime? SefDatumSlanja { get; set; }
    [MaxLength(500)]
    public string? SefPoruka { get; set; }

    [MaxLength(100)]
    public string? FiskalniBroj { get; set; }
    [MaxLength(1000)]
    public string? FiskalniQrKod { get; set; }
    public DateTime? FiskalniDatum { get; set; }

    public List<RacunOtpremnicaStavka> Stavke { get; set; } = new();

    [MaxLength(20)]
    public string? BrojOtpremnice { get; set; }

    public int? KontoKupcaId { get; set; }
    [ForeignKey(nameof(KontoKupcaId))]
    public Konto? KontoKupca { get; set; }

    public int RokPlacanjaDana { get; set; } = 15;

    [MaxLength(50)]
    public string? NacinPlacanja { get; set; }
}

public class RacunOtpremnicaStavka
{
    [Key]
    public int RacunOtpremnicaStavkaId { get; set; }

    public int RacunOtpremnicaId { get; set; }
    [ForeignKey(nameof(RacunOtpremnicaId))]
    public RacunOtpremnica? RacunOtpremnica { get; set; }

    public int RedniBroj { get; set; }

    public int? ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RabatProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal StopaPdv { get; set; } = 20m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Osnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Ukupno { get; set; }
}
