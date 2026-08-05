using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum OsnovSprecenosti
{
    Bolest = 0,
    PovredaNaRadu = 1,
    ProfesionalnaBolest = 2,
    NegaClanaPorodice = 3,
    NegaClanaPorodiceClan78 = 4,
    IzolacijaIPracenje = 5,
    DavalacTkivaIOrgana = 6,
    OdrzavanjeTrudnoce = 7
}

[Table("Bolovanja")]
public class Bolovanje
{
    [Key]
    public int BolovanjeId { get; set; }

    public int BrojRadnika { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }

    public DateTime DatumPocetkaSprecenosti { get; set; } = DateTime.Today;
    public DateTime DatumOd { get; set; } = DateTime.Today;
    public DateTime DatumDo { get; set; } = DateTime.Today;

    public OsnovSprecenosti Osnov { get; set; } = OsnovSprecenosti.Bolest;
    public bool PrvaIsplata { get; set; }

    [MaxLength(30)]
    public string BrojDoznake { get; set; } = "";

    [MaxLength(200)]
    public string Napomena { get; set; } = "";

    public DateTime DatumUnosa { get; set; } = DateTime.Now;

    [NotMapped]
    public int BrojDana => DatumDo >= DatumOd ? (DatumDo - DatumOd).Days + 1 : 0;

    [NotMapped]
    public int DanSprecenostiNaPocetku => (DatumOd - DatumPocetkaSprecenosti).Days + 1;

    public static int? PrviDanNaTeretFonda(OsnovSprecenosti osnov) => osnov switch
    {
        OsnovSprecenosti.PovredaNaRadu => 1,
        OsnovSprecenosti.ProfesionalnaBolest => 1,
        OsnovSprecenosti.DavalacTkivaIOrgana => 1,
        OsnovSprecenosti.NegaClanaPorodice => null,
        _ => 31
    };

    [NotMapped]
    public string PeriodStr => $"{DatumOd:dd.MM.yyyy}–{DatumDo:dd.MM.yyyy}";

    [NotMapped]
    public string OsnovNaziv => NazivOsnova(Osnov);

    public static string NazivOsnova(OsnovSprecenosti osnov) => osnov switch
    {
        OsnovSprecenosti.Bolest => "Bolest",
        OsnovSprecenosti.PovredaNaRadu => "Povreda na radu",
        OsnovSprecenosti.ProfesionalnaBolest => "Profesionalna bolest",
        OsnovSprecenosti.NegaClanaPorodice => "Nega člana porodice 65%",
        OsnovSprecenosti.NegaClanaPorodiceClan78 => "Nega člana porodice — čl. 78. st. 3",
        OsnovSprecenosti.IzolacijaIPracenje => "Izolacija i praćenje",
        OsnovSprecenosti.DavalacTkivaIOrgana => "Davalac tkiva i organa",
        _ => "Održavanje trudnoće"
    };
}
