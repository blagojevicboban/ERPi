using System.IO;
using System.Text.Json;
using ERPiApp.Models;

namespace ERPiApp.Services;

/// <summary>
/// Lokalni registar poznatih firmi — kao companies.json u ERPiHub-u, samo svedeno na ono što
/// ovoj aplikaciji treba (bez auto-detekcije legacy modula, ta uloga ostaje ERPiHub-u). Registar
/// je samo prečica za prikaz liste; ako firma u međuvremenu promeni naziv u svojoj bazi, ovde
/// zaostaje stari naziv dok se ponovo ne doda — CompanySelectWindow zato uz Naziv/Otvori ne
/// obećava da je uvek sveže, samo da je dovoljno za izbor prave baze.
/// </summary>
public class CompanyRegistryService
{
    private readonly string _configFilePath;

    public CompanyRegistryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "ERPi");
        Directory.CreateDirectory(dir);
        _configFilePath = Path.Combine(dir, "companies.json");
        DefaultDataDirectory = Path.Combine(dir, "Baze");
        Directory.CreateDirectory(DefaultDataDirectory);
    }

    /// <summary>Folder gde nove firme dobijaju svoju bazu ako korisnik ne izabere drugu putanju.</summary>
    public string DefaultDataDirectory { get; }

    public List<CompanyEntry> Load()
    {
        if (!File.Exists(_configFilePath)) return new List<CompanyEntry>();

        try
        {
            var json = File.ReadAllText(_configFilePath);
            return JsonSerializer.Deserialize<List<CompanyEntry>>(json) ?? new List<CompanyEntry>();
        }
        catch
        {
            return new List<CompanyEntry>();
        }
    }

    public void Save(List<CompanyEntry> companies)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(companies, options);
        File.WriteAllText(_configFilePath, json);
    }
}
