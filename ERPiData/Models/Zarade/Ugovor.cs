using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum TipPrimaocaPrihoda
{
    Zaposleni = 1,
    OsnivacZaposlenUSvomDrustvu = 2,
    SamostalnaDelatnost = 3,
    Poljoprivrednik = 4,
    NijeOsiguranPoDrugomOsnovu = 5,
    Nerezident = 6,
    InvalidnoLice = 7,
    VojniOsiguranik = 8,
    PenzionerPoOsnovuZaposlenosti = 9,
    PenzionerPoOsnovuSamostalneDelatnosti = 10,
    NemaDoprinosaVanRadnogOdnosa = 11,
    VojniPenzioner = 12,
    PoljoprivredniPenzioner = 13
}

[Table("VrsteUgovora")]
public class VrstaUgovora
{
    [Key]
    public int VrstaUgovoraId { get; set; }

    [Required, MaxLength(10)]
    public string Sifra { get; set; } = "";

    [Required, MaxLength(80)]
    public string Naziv { get; set; } = "";

    [MaxLength(3)]
    public string Ovp { get; set; } = "";

    [Column(TypeName = "decimal(6,2)")]
    public decimal NormiraniTroskoviProcenat { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaPoreza { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaPioPrimalac { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaZdravstvoPrimalac { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaNezaposlenostPrimalac { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaPioIsplatilac { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaZdravstvoIsplatilac { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal StopaNezaposlenostIsplatilac { get; set; }

    [MaxLength(10)]
    public string Konto { get; set; } = "";

    [MaxLength(3)]
    public string SifraPlacanja { get; set; } = "";

    public int Redosled { get; set; }
    public bool Aktivna { get; set; } = true;

    [MaxLength(200)]
    public string Napomena { get; set; } = "";

    public ICollection<Ugovor> Ugovori { get; set; } = [];

    [NotMapped]
    public decimal ZbirStopaPrimaoca => StopaPioPrimalac + StopaZdravstvoPrimalac + StopaNezaposlenostPrimalac;

    [NotMapped]
    public decimal ZbirStopaIsplatioca => StopaPioIsplatilac + StopaZdravstvoIsplatilac + StopaNezaposlenostIsplatilac;

    [NotMapped]
    public string NazivSaSifrom => $"{Sifra} — {Naziv}";
}

[Table("Ugovori")]
public class Ugovor
{
    [Key]
    public int UgovorId { get; set; }

    [ForeignKey(nameof(VrstaUgovora))]
    public int VrstaUgovoraId { get; set; }

    public int BrojRadnika { get; set; }

    public TipPrimaocaPrihoda TipPrimaoca { get; set; } = TipPrimaocaPrihoda.NijeOsiguranPoDrugomOsnovu;

    [MaxLength(20)]
    public string Broj { get; set; } = "";

    [MaxLength(200)]
    public string Predmet { get; set; } = "";

    public DateTime DatumZakljucenja { get; set; } = DateTime.Today;
    public DateTime? DatumOd { get; set; }
    public DateTime? DatumDo { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal UgovorenIznos { get; set; }

    public bool IznosJeNeto { get; set; }
    public bool Aktivan { get; set; } = true;

    public string Tekst { get; set; } = "";
    public DateTime? DatumTeksta { get; set; }

    [MaxLength(200)]
    public string Napomena { get; set; } = "";

    public DateTime DatumUnosa { get; set; } = DateTime.Now;

    public VrstaUgovora VrstaUgovora { get; set; } = null!;
    public ICollection<ObracunPlate> Obracuni { get; set; } = [];

    [NotMapped]
    public string PeriodStr => DatumOd.HasValue
        ? $"{DatumOd:dd.MM.yyyy}–{(DatumDo.HasValue ? DatumDo.Value.ToString("dd.MM.yyyy") : "…")}"
        : "";

    [NotMapped]
    public string OznakaPrimaoca => ((int)TipPrimaoca).ToString("D2");
}
