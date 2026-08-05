using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Core;

/// <summary>
/// Uloga korisnika u sistemu. Enum umesto slobodnog teksta (kako je stajalo u ERPiFinansije)
/// da vrednost ne zavisi od tačnog pisanja stringa u kodu koji je proverava.
/// </summary>
public enum UlogaKorisnika
{
    Administrator = 0,
    Operater = 1,
    Gledalac = 2
}

/// <summary>
/// Jedan korisnik sistema, zajednički za sve module — prijava je jednom, ne po modulu
/// (Faza 2, Single Sign-On).
/// </summary>
public class Korisnik
{
    [Key]
    public int KorisnikId { get; set; }

    [Required]
    [MaxLength(50)]
    public string KorisnickoIme { get; set; } = string.Empty;

    [Required]
    public string LozinkaHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ImeIPrezime { get; set; } = string.Empty;

    public UlogaKorisnika Uloga { get; set; } = UlogaKorisnika.Operater;

    public bool IsActive { get; set; } = true;
    public DateTime? PoslednjaPrijava { get; set; }
}
