using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Parovanje (zatvaranje) dve stavke naloga — npr. faktura (Duguje) zatvorena uplatom
/// (Potražuje). M:N po stavci je moguć (delimična zatvaranja), zato je ovo zasebna relaciona
/// tabela, a ne polje na StavkaNaloga — "koliko je stavka X zatvorena" se uvek IZNOVA računa
/// zbirom ovih redova, ne čuva kao poseban brojač na stavci.
/// </summary>
public class ZatvaranjeStavke
{
    [Key]
    public int ZatvaranjeStavkeId { get; set; }

    public int StavkaDugujeId { get; set; }
    [ForeignKey(nameof(StavkaDugujeId))]
    public StavkaNaloga? StavkaDuguje { get; set; }

    public int StavkaPotrazujeId { get; set; }
    [ForeignKey(nameof(StavkaPotrazujeId))]
    public StavkaNaloga? StavkaPotrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    public DateTime DatumZatvaranja { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string VrstaZatvaranja { get; set; } = "Rucno";

    [MaxLength(250)]
    public string? Napomena { get; set; }

    /// <summary>Ko je zatvaranje izvršio — pravi FK ka Core.Korisnik, ne string kopija imena.</summary>
    public int? KorisnikId { get; set; }
    [ForeignKey(nameof(KorisnikId))]
    public Korisnik? Korisnik { get; set; }
}
