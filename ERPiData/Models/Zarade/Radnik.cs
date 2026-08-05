using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Zarade;

/// <summary>
/// Evidencija radnika po obračunskim periodima — port RADNICII.DBF + RADNICI.DBF.
/// Jedan red = jedan radnik u jednom obračunskom periodu (Godina + Mesec).
/// Povezan sa zajedničkim Core.Partner identitetom i Core.MestoTroska.
/// </summary>
[Table("Radnici")]
public class Radnik
{
    [Key]
    public int Id { get; set; }

    // ── Veza sa Core entitetima ──────────────────────────────────────
    [ForeignKey(nameof(Partner))]
    public int? PartnerId { get; set; }
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(MestoTroska))]
    public int? MestoTroskaId { get; set; }
    public MestoTroska? MestoTroska { get; set; }

    // ── Obračunski period ────────────────────────────────────────────
    public int Godina { get; set; }
    public int Mesec { get; set; }

    // ── Identifikacija ───────────────────────────────────────────────
    public int BrojRadnika { get; set; }

    [Required, MaxLength(60)]
    public string ImeIPrezime { get; set; } = "";

    [MaxLength(13)]
    public string Jmbg { get; set; } = "";

    [MaxLength(20)]
    public string MaticniBroj { get; set; } = "";

    // ── Lični podaci ─────────────────────────────────────────────────
    public DateTime? DatumRodjenja { get; set; }

    [MaxLength(60)]
    public string MestoRodjenja { get; set; } = "";

    [MaxLength(80)]
    public string AdresaStanovanja { get; set; } = "";

    [MaxLength(40)]
    public string Mesto { get; set; } = "";

    [MaxLength(3)]
    public string SifraOpstine { get; set; } = "";

    [MaxLength(120)]
    public string Email { get; set; } = "";

    [MaxLength(11)]
    public string Lbo { get; set; } = "";

    // ── Podaci o zaposlenju ──────────────────────────────────────────
    public DateTime? DatumZaposlenja { get; set; }
    public DateTime? DatumPrestanka { get; set; }

    [MaxLength(10)]
    public string Kategorija { get; set; } = "";

    [MaxLength(60)]
    public string Radno_Mesto { get; set; } = "";

    public int BrojRadneJedinice { get; set; } = 1;

    [MaxLength(20)]
    public string SifraMestaTroska { get; set; } = "";

    public int MinuliRadGodine { get; set; }

    // ── Koeficijenti i osnova ────────────────────────────────────────
    [Column(TypeName = "decimal(10,4)")]
    public decimal Koeficijent { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal Koeficijent1 { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal OsnovnaPlata { get; set; }

    // ── Doprinosi i porezi ───────────────────────────────────────────
    [Column(TypeName = "decimal(6,4)")]
    public decimal StopaPio { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal StopaZdravstvo { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal StopaNezaposlenost { get; set; }

    // ── Bankarski podaci ─────────────────────────────────────────────
    [MaxLength(25)]
    public string BankovniRacun { get; set; } = "";

    [MaxLength(30)]
    public string NazivBanke { get; set; } = "";

    // ── Status ───────────────────────────────────────────────────────
    public bool Aktivan { get; set; } = true;
    public bool VanRadnogOdnosa { get; set; }

    // ── Poresko oslobođenje ──────────────────────────────────────────
    [Column(TypeName = "decimal(12,2)")]
    public decimal LicniOslobodjenje { get; set; }

    // ── Poreske olakšice ─────────────────────────────────────────────
    [Column(TypeName = "decimal(6,2)")]
    public decimal ProcenatPovracajaPoreza { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal ProcenatPovracajaDoprinosa { get; set; }

    public DateTime? OlaksicaVaziDo { get; set; }

    // ── Legacy / operativni podaci ───────────────────────────────────
    [MaxLength(10)]
    public string Operativni { get; set; } = "";

    public DateTime DatumUnosa { get; set; } = DateTime.Now;
    public DateTime? DatumIzmene { get; set; }

    // ── Navigaciona svojstva ─────────────────────────────────────────
    public ICollection<ObracunPlate> Obracuni { get; set; } = [];
    public ICollection<Kredit> Krediti { get; set; } = [];
    public ICollection<RadniSat> RadniSati { get; set; } = [];
}
