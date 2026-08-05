using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Core;

public enum TipMestaTroska
{
    MestoTroska = 0,   // Npr. Poslovna jedinica Beograd, Uprava, Odeljenje prodaje
    Projekat = 1,      // Npr. Projekat Izgradnja Objekta A, IT Razvoj ERP
    Objekat = 2,       // Npr. Maloprodajni objekat Niš, Magacin Zemun
    PoslovnaJedinica = 3
}

/// <summary>
/// Mesto troška za analitiku rashoda. Zarade danas vezuju radnika za mesto troška preko
/// <c>SifraMestaTroska</c> stringa jer rade nad odvojenom bazom — kad Radnik pređe u ovu bazu
/// (Faza 5), ta veza postaje pravi strani ključ ka ovoj tabeli.
/// </summary>
public class MestoTroska
{
    [Key]
    public int MestoTroskaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Sifra { get; set; } = string.Empty; // Npr. MT-01, PRJ-2026-A

    [Required]
    [MaxLength(100)]
    public string Naziv { get; set; } = string.Empty; // Npr. Gradilište Novi Sad

    public TipMestaTroska Tip { get; set; } = TipMestaTroska.MestoTroska;

    public bool IsAktivno { get; set; } = true;
    public string Napomena { get; set; } = string.Empty;
}
