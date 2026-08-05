using ERPiData.Models.Core;

namespace ERPiData.Models.Sredstva;

/// <summary>
/// Odgovara KARTICA.DBF — hronološki log svih promena po sredstvu (audit trail: nabavka,
/// amortizacija, revalorizacija, rashod...). Port iz ERPiSredstvaData.Models.Kartica; <c>Konto</c>
/// (string) je postao <see cref="KontoId"/> FK, isti obrazac kao <see cref="Sredstvo"/>.
/// </summary>
public class Kartica
{
    public int Id { get; set; }

    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;

    public int RedBroj { get; set; }
    public DateTime Datum { get; set; }
    public string OpisPromene { get; set; } = string.Empty;
    public int ObracunskaJedinica { get; set; }

    public int? KontoId { get; set; }
    public Konto? Konto { get; set; }

    public int AmortizacionaGrupa1 { get; set; }
    public int AmortizacionaGrupa2 { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public decimal KoeficijentRevalorizacije { get; set; }
    public decimal Kolicina { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal IspravkaVrednosti { get; set; }
}
