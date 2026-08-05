using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum VrstaIzmenePrijave
{
    Nema = 0,
    Izmena = 1,
    PoNalazuKontrole = 2,
    PoNaloguSuda = 3
}

public enum OsnovIzmenePrijave
{
    Nema = 0,
    ZalbaPrviStepen = 1,
    ZalbaDrugiStepen = 2,
    PoNaloguSuda = 3
}

public enum StatusPrijave
{
    Pripremljena = 0,
    Podneta = 1,
    Prihvacena = 2,
    Odbijena = 3,
    Stornirana = 4
}

[Table("PppPdPrijave")]
public class PppPdPrijava
{
    [Key]
    public int Id { get; set; }

    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int RedniBroj { get; set; } = 1;

    [MaxLength(2)]
    public string VrstaPrijave { get; set; } = "1";

    [MaxLength(50)]
    public string KlijentskaOznaka { get; set; } = "";

    public DateTime DatumPlacanja { get; set; }

    public VrstaIzmenePrijave VrstaIzmene { get; set; } = VrstaIzmenePrijave.Nema;

    [MaxLength(20)]
    public string JipdKojiSeMenja { get; set; } = "";

    [MaxLength(200)]
    public string BrojResenja { get; set; } = "";

    public OsnovIzmenePrijave OsnovIzmene { get; set; } = OsnovIzmenePrijave.Nema;

    public int BrojZaposlenih { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ZbirPoreza { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ZbirDoprinosa { get; set; }

    [MaxLength(20)]
    public string Jipd { get; set; } = "";

    [MaxLength(30)]
    public string Bop { get; set; } = "";

    [Column(TypeName = "decimal(14,2)")]
    public decimal IznosZaUplatu { get; set; }

    [MaxLength(25)]
    public string RacunZaUplatu { get; set; } = "";

    [MaxLength(2)]
    public string ModelPozivaNaBroj { get; set; } = "";

    [MaxLength(140)]
    public string SvrhaUplate { get; set; } = "";

    public StatusPrijave Status { get; set; } = StatusPrijave.Pripremljena;

    public DateTime? DatumPodnosenja { get; set; }
    public DateTime? DatumStatusa { get; set; }

    [MaxLength(500)]
    public string Napomena { get; set; } = "";

    [MaxLength(260)]
    public string PutanjaFajla { get; set; } = "";

    public DateTime DatumKreiranja { get; set; } = DateTime.Now;
}
