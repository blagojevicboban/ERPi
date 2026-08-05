namespace ERPiApp.Models;

/// <summary>Jedan red u lokalnom registru poznatih firmi (companies.json) — ne baza podataka.</summary>
public class CompanyEntry
{
    public string Sifra { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public string DbPath { get; set; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Pib) ? Naziv : $"{Naziv}  (PIB: {Pib})";
}
