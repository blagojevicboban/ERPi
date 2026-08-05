using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

[Table("SabloniUgovora")]
public class SablonUgovora
{
    [Key]
    public int SablonUgovoraId { get; set; }

    [Required, MaxLength(10)]
    public string Sifra { get; set; } = "";

    [Required, MaxLength(80)]
    public string Naziv { get; set; } = "";

    [ForeignKey(nameof(VrstaUgovora))]
    public int? VrstaUgovoraId { get; set; }

    public VrstaUgovora? VrstaUgovora { get; set; }

    public string Tekst { get; set; } = "";

    public int Redosled { get; set; }
    public bool Aktivan { get; set; } = true;
    public bool JeSistemski { get; set; }

    [MaxLength(200)]
    public string Napomena { get; set; } = "";

    [NotMapped]
    public string NazivSaSifrom => $"{Sifra} — {Naziv}";
}
