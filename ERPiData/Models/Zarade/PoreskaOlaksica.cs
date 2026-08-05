using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum MehanizamOlaksice
{
    Povracaj = 0,
    Oslobodjenje = 1
}

public enum IzvorMfp
{
    UmanjenjePoreza = 0,
    UmanjenjeDoprinosa = 1,
    OsnovicaPoreza = 2,
    OsnovicaDoprinosa = 3,
    ProcenatOlaksice = 4,
    FiksnaVrednost = 5
}

[Table("PoreskeOlaksice")]
public class PoreskaOlaksica
{
    [Key]
    public int PoreskaOlaksicaId { get; set; }

    [Required, MaxLength(2)]
    public string Sifra { get; set; } = "";

    [Required, MaxLength(100)]
    public string Naziv { get; set; } = "";

    [MaxLength(100)]
    public string PravniOsnov { get; set; } = "";

    public MehanizamOlaksice Mehanizam { get; set; } = MehanizamOlaksice.Povracaj;

    [Column(TypeName = "decimal(6,2)")]
    public decimal ProcenatPoreza { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal ProcenatDoprinosa { get; set; }

    public DateTime? VaziOd { get; set; }
    public DateTime? VaziDo { get; set; }

    public bool Aktivna { get; set; } = true;

    [MaxLength(300)]
    public string Napomena { get; set; } = "";

    public ICollection<OlaksicaMfp> MfpDeklaracije { get; set; } = [];
}

[Table("OlaksicaMfp")]
public class OlaksicaMfp
{
    [Key]
    public int OlaksicaMfpId { get; set; }

    [ForeignKey(nameof(Olaksica))]
    public int PoreskaOlaksicaId { get; set; }

    [Required, MaxLength(10)]
    public string Oznaka { get; set; } = "";

    public IzvorMfp Izvor { get; set; } = IzvorMfp.UmanjenjePoreza;

    [Column(TypeName = "decimal(14,2)")]
    public decimal FiksnaVrednost { get; set; }

    public PoreskaOlaksica Olaksica { get; set; } = null!;
}
