using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Core;

/// <summary>
/// Preduzeće/firma nad kojom se vodi ceo ERP sistem. Objedinjuje polja koja su do sada
/// bila razdvojena po modulima: SEF/PFR parametri su iz ERPiFinansije, a
/// <see cref="SifraOpstine"/>/<see cref="SifraDelatnosti"/>/<see cref="Zastupnik"/> i srodna
/// polja iz ERPiZarade (traže ih PPP-PD i obrazac OZ-7/OZ-10). Nijedan modul ne dobija svoju
/// kopiju firme — svi čitaju iz iste tabele.
/// </summary>
public class Firma
{
    [Key]
    public int FirmaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Sifra { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Adresa { get; set; }

    [MaxLength(100)]
    public string? PttIMesto { get; set; }

    /// <summary>Šifra opštine sedišta po šifarniku Poreske uprave — PPP-PD zaglavlje (SedistePrebivaliste).</summary>
    [MaxLength(3)]
    public string? SifraOpstine { get; set; }

    /// <summary>Šifra pretežne delatnosti po Klasifikaciji delatnosti — zaglavlje obrasca OZ-10.</summary>
    [MaxLength(10)]
    public string? SifraDelatnosti { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }

    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    /// <summary>Poseban tekući račun na koji RFZO uplaćuje refundiranu naknadu zarade (nije isto što i <see cref="ZiroRacun"/>).</summary>
    [MaxLength(30)]
    public string? PosebanRacun { get; set; }

    /// <summary>Podračun poslovne jedinice; popunjava se samo kad ga filijala traži (OZ-7).</summary>
    [MaxLength(30)]
    public string? PodracunPoslovneJedinice { get; set; }

    [MaxLength(30)]
    public string? Pib { get; set; }

    [MaxLength(30)]
    public string? MaticniBroj { get; set; }

    [MaxLength(20)]
    public string? JbkjsBroj { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Lice koje firmu zastupa pri potpisivanju (npr. ugovori van radnog odnosa).</summary>
    [MaxLength(60)]
    public string? Zastupnik { get; set; }

    /// <summary>Funkcija zastupnika („direktor", „zakonski zastupnik").</summary>
    [MaxLength(40)]
    public string? FunkcijaZastupnika { get; set; }

    [MaxLength(250)]
    public string? SefApiKey { get; set; }

    [MaxLength(20)]
    public string SefEnvironment { get; set; } = "Demo";

    [MaxLength(250)]
    public string PfrUrl { get; set; } = "http://localhost:8443";

    [MaxLength(100)]
    public string PfrPacKod { get; set; } = "123456";

    [MaxLength(100)]
    public string PfrKasirName { get; set; } = "Glavni Kasir";

    /// <summary>
    /// Dozvoljava rad sa simuliranom fiskalizacijom kada PFR nije dostupan.
    /// Podrazumevano ISKLJUČENO - simulirani računi nemaju pravnu vrednost.
    /// </summary>
    public bool PfrSimulatorMod { get; set; }

    [MaxLength(500)]
    public string? Napomena { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime DatumKreiranja { get; set; } = DateTime.Now;
}
