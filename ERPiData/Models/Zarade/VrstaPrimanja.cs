using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

[Table("VrstePrimanja")]
public class VrstaPrimanja
{
    [Key]
    public int VrstaPrimanjaId { get; set; }

    [Required, MaxLength(10)]
    public string Sifra { get; set; } = "";

    [Required, MaxLength(80)]
    public string Naziv { get; set; } = "";

    [MaxLength(9)]
    public string Svp { get; set; } = "";

    public bool Oporezivo { get; set; } = true;
    public bool UlaziUOsnovicuDoprinosa { get; set; } = true;

    [Column(TypeName = "decimal(14,2)")]
    public decimal NeoporeziviLimit { get; set; }

    [MaxLength(10)]
    public string Konto { get; set; } = "";

    public bool NaTeretFonda { get; set; }
    public bool VecIsplacenoVanObracuna { get; set; }

    public int Redosled { get; set; }
    public bool Aktivna { get; set; } = true;
    public bool JeSistemska { get; set; }

    public ICollection<ObracunStavka> Stavke { get; set; } = [];
}

[Table("ObracunStavke")]
public class ObracunStavka
{
    [Key]
    public int ObracunStavkaId { get; set; }

    [ForeignKey(nameof(Obracun))]
    public int ObracunPlateId { get; set; }

    [ForeignKey(nameof(VrstaPrimanja))]
    public int VrstaPrimanjaId { get; set; }

    public int Sati { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Iznos { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal OporeziviDeo { get; set; }

    public decimal NeoporeziviDeo => Iznos - OporeziviDeo;

    public ObracunPlate Obracun { get; set; } = null!;
    public VrstaPrimanja VrstaPrimanja { get; set; } = null!;
}

[Table("UnetaPrimanja")]
public class UnetoPrimanje : IPripadaIsplati
{
    [Key]
    public int UnetoPrimanjeId { get; set; }

    [ForeignKey(nameof(Radnik))]
    public int RadnikId { get; set; }

    public int Godina { get; set; }
    public int Mesec { get; set; }

    public int? IsplataId { get; set; }
    public Isplata? Isplata { get; set; }

    [ForeignKey(nameof(VrstaPrimanja))]
    public int VrstaPrimanjaId { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Iznos { get; set; }

    [MaxLength(200)]
    public string Napomena { get; set; } = "";

    public Radnik Radnik { get; set; } = null!;
    public VrstaPrimanja VrstaPrimanja { get; set; } = null!;
}
