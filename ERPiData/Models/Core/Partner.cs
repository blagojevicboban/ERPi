using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Core;

/// <summary>
/// Objedinjena tabela partnera — dobavljači, kupci, radnici, banke, Poreska uprava. Ovde stoje
/// SAMO zajednički identitetski podaci (naziv, PIB/MB/JMBG, kontakt, računi); operativni podaci
/// specifični za modul (npr. koeficijenti i doprinosi radnika) ostaju u modulskoj tabeli
/// (<c>Radnik</c> u Zaradama) koja se na ovaj zapis vezuje preko <c>PartnerId</c> stranog ključa
/// — namerno NIJE sve prepisano ovde, da Partner ne postane "god table" sa gomilom praznih
/// kolona za tipove kojima ne pripadaju.
///
/// <see cref="JeDobavljac"/>/<see cref="JeKupac"/>/... su nezavisni bool-ovi, a ne jedan enum
/// tipa, jer je kupac-dobavljač uobičajen slučaj u knjigovodstvu — partner sme da bude oboje
/// istovremeno.
/// </summary>
public class Partner
{
    [Key]
    public int PartnerId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraPartnera { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Adresa { get; set; }

    [MaxLength(100)]
    public string? PttIMesto { get; set; }

    [MaxLength(30)]
    public string? Pib { get; set; }

    [MaxLength(30)]
    public string? MaticniBroj { get; set; }

    /// <summary>JMBG — postoji samo kod partnera koji su fizička lica (radnici, primaoci po ugovoru).</summary>
    [MaxLength(13)]
    public string? Jmbg { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Žiro račun — pravna lica (dobavljač/kupac/banka).</summary>
    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    /// <summary>Tekući račun — fizička lica (radnik, primalac po ugovoru).</summary>
    [MaxLength(50)]
    public string? BankovniRacun { get; set; }

    [MaxLength(100)]
    public string? NazivBanke { get; set; }

    [MaxLength(20)]
    public string? KontoPartnera { get; set; }

    public bool JeDobavljac { get; set; }
    public bool JeKupac { get; set; }
    public bool JeRadnik { get; set; }
    public bool JeBanka { get; set; }
    public bool JePoreskaUprava { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>"šifra - naziv" za padajuće liste, isti obrazac kao Artikal/Magacin/Konto.Prikaz.</summary>
    [NotMapped]
    public string Prikaz => string.IsNullOrWhiteSpace(SifraPartnera) ? Naziv : $"{SifraPartnera} - {Naziv}";
}
