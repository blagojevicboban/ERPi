using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum TipObustave
{
    ZakonskoIzdrzavanje = 0,
    SudskaZabrana = 1,
    Kredit = 2,
    Administrativna = 3,
    Sindikat = 4,
    Ostalo = 5
}

[Table("Krediti")]
public class Kredit
{
    [Key] public int Id { get; set; }
    [ForeignKey(nameof(Radnik))] public int RadnikId { get; set; }

    [MaxLength(60)] public string Opis { get; set; } = "";

    [Column(TypeName = "decimal(14,2)")] public decimal UkupanIznos { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal MesecnaRata { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal OstatakDuga { get; set; }

    public int BrojRata { get; set; }
    public int PlateneRate { get; set; }
    public DateTime DatumPocetka { get; set; }
    public DateTime? DatumZavrsetka { get; set; }
    public bool Aktivan { get; set; } = true;

    [MaxLength(60)] public string PrimalacNaziv { get; set; } = "";
    [MaxLength(25)] public string PrimalacRacun { get; set; } = "";
    [MaxLength(2)] public string ModelPozivaNaBroj { get; set; } = "";
    [MaxLength(25)] public string PozivNaBroj { get; set; } = "";

    public TipObustave Tip { get; set; } = TipObustave.Kredit;
    public int RedosledNaplate { get; set; }

    public Radnik Radnik { get; set; } = null!;
}

[Table("RadniSati")]
public class RadniSat : IPripadaIsplati
{
    [Key] public int Id { get; set; }
    [ForeignKey(nameof(Radnik))] public int RadnikId { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }

    public int? IsplataId { get; set; }
    public Isplata? Isplata { get; set; }

    public int RedovniSati { get; set; }
    public int BolovanjeSati { get; set; }
    public int PrekovremeneSati { get; set; }
    public int GodisnjiOdmorSati { get; set; }
    public int DrzavniPraznikSati { get; set; }
    public int NocniSati { get; set; }
    public int SmenskiSati { get; set; }
    public int RadPraznikomSati { get; set; }
    public int NocniRadPraznikomSati { get; set; }
    public int PlacenoOdsustvoSati { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal Stimulacija { get; set; }

    public int RadNedeljomSati { get; set; }
    public int PlacenoZakonskiSati { get; set; }
    public int BolovanjePreko60Sati { get; set; }
    public int PorodiljskoOdsustvoSati { get; set; }
    public int Bolovanje100Sati { get; set; }
    public int TopliObrokDani { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal RegresIznos { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal Prosek { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Varijabila { get; set; }

    public Radnik Radnik { get; set; } = null!;
}

[Table("PoreskeStope")]
public class PoreznaStopa
{
    [Key] public int Id { get; set; }
    public int RedniBroj { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal GranjaOd { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal GranicaDo { get; set; }
    [Column(TypeName = "decimal(6,4)")] public decimal Stopa { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal FiksniIznos { get; set; }
    public int GodisnjuVazenja { get; set; }
    public int MesecVazenja { get; set; }
}

[Table("Kategorije")]
public class Kategorija
{
    [Key] public int Id { get; set; }
    [MaxLength(10)] public string Sifra { get; set; } = "";
    [MaxLength(60)] public string Naziv { get; set; } = "";
    [Column(TypeName = "decimal(8,4)")] public decimal Koeficijent { get; set; }
    [Column(TypeName = "decimal(6,4)")] public decimal StopaPio { get; set; }
    [Column(TypeName = "decimal(6,4)")] public decimal StopaZdravstvo { get; set; }
}

[Table("Samodoprinosi")]
public class Samodoprinosi
{
    [Key] public int Id { get; set; }
    [ForeignKey(nameof(Radnik))] public int RadnikId { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Iznos { get; set; }
    [MaxLength(60)] public string Opis { get; set; } = "";
    public Radnik Radnik { get; set; } = null!;
}

[Table("Normativi")]
public class Normativ
{
    [Key] public int Id { get; set; }
    [MaxLength(20)] public string Sifra { get; set; } = "";
    [MaxLength(60)] public string Naziv { get; set; } = "";
    [Column(TypeName = "decimal(10,4)")] public decimal VrednostBoda { get; set; }
    public char Tip { get; set; } = 'P';
}

[Table("PlatniRazredi")]
public class PlatniRazred
{
    [Key] public int Id { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R1 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R2 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R3 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R4 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R5 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R6 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R7 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R8 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal R9 { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal P1 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P2 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P3 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P4 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P5 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P6 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P7 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P8 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal P9 { get; set; }
}

[Table("Banke")]
public class Banka
{
    [Key] public int Id { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    [MaxLength(10)] public string Sifra { get; set; } = "";
    [MaxLength(60)] public string Naziv { get; set; } = "";
    [MaxLength(30)] public string ZiroRacun { get; set; } = "";
}

public enum AkcijaObracuna
{
    Kreiran = 0,
    Prekalkulisan = 1,
    Zakljucan = 2,
    Otkljucan = 3,
    Obrisan = 4,
    Storniran = 5,
    PppPdGenerisan = 6,
    IsplataDodata = 7,
    IsplataObrisana = 8,
    NalogZaKnjizenje = 9,
    BolovanjeEvidentirano = 10,
    ObrazacRfzo = 11
}

public class ObracunAudit
{
    [Key] public int ObracunAuditId { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int? BrojRadnika { get; set; }
    [MaxLength(60)] public string? ImeRadnika { get; set; }
    public AkcijaObracuna Akcija { get; set; }
    public int? KorisnikId { get; set; }
    [MaxLength(100)] public string? KorisnickoIme { get; set; }
    [MaxLength(300)] public string? Detalji { get; set; }
    public DateTime Vreme { get; set; } = DateTime.Now;
}

[Table("ObracunVerzije")]
public class ObracunVerzija : IPripadaIsplati
{
    [Key] public int ObracunVerzijaId { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int RadnikId { get; set; }
    public int? IsplataId { get; set; }
    public int BrojRadnika { get; set; }
    [MaxLength(60)] public string ImeRadnika { get; set; } = "";
    public int Verzija { get; set; } = 1;
    [MaxLength(300)] public string Razlog { get; set; } = "";
    [MaxLength(100)] public string? KorisnickoIme { get; set; }
    public DateTime Vreme { get; set; } = DateTime.Now;
    public bool BioZakljucan { get; set; }
    public bool BioStorniran { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal Bruto { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PorezNaDohodak { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DoprinosiRadnik { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DoprinosiPoslodavac { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal NetoIsplata { get; set; }
    public string Snimak { get; set; } = "";
}

public enum IshodSlanja
{
    Poslato = 0,
    Neuspesno = 1,
    Preskoceno = 2
}

public class SlanjeListica
{
    [Key] public int SlanjeListicaId { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int BrojRadnika { get; set; }
    [MaxLength(60)] public string ImeRadnika { get; set; } = "";
    [MaxLength(120)] public string Email { get; set; } = "";
    public IshodSlanja Ishod { get; set; }
    public bool ZasticenLozinkom { get; set; }
    [MaxLength(300)] public string? Napomena { get; set; }
    public int? KorisnikId { get; set; }
    [MaxLength(100)] public string? KorisnickoIme { get; set; }
    public DateTime Vreme { get; set; } = DateTime.Now;
}

