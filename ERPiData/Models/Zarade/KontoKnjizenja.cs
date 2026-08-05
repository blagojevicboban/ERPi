using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

public enum StranaKnjizenja
{
    Duguje = 0,
    Potrazuje = 1
}

[Table("KontaKnjizenja")]
public class KontoKnjizenja
{
    [Key]
    public int KontoKnjizenjaId { get; set; }

    [Required, MaxLength(40)]
    public string Kljuc { get; set; } = "";

    [Required, MaxLength(120)]
    public string Naziv { get; set; } = "";

    [MaxLength(20)]
    public string Konto { get; set; } = "";

    public StranaKnjizenja Strana { get; set; }
    public int Redosled { get; set; }

    [MaxLength(250)]
    public string Napomena { get; set; } = "";

    [NotMapped]
    public string StranaTekst => Strana == StranaKnjizenja.Duguje ? "Duguje" : "Potražuje";
}
