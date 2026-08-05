using ERPiData.Models.Core;

namespace ERPiData.Models.Sredstva;

/// <summary>
/// Odgovara PRIJAVA.DBF — nalog za aktiviranje/prijem osnovnog sredstva. Port iz
/// ERPiSredstvaData.Models.Prijava. Dve razlike od izvora: <c>Konto</c> (string) je postao
/// <see cref="KontoId"/> FK, i <c>DobavljacId</c> (zaseban Dobavljac model, samo konto+adresa) je
/// postao <see cref="PartnerId"/> FK ka jedinstvenom <see cref="Core.Partner"/> (dobavljač je ovde
/// samo partner sa <c>JeDobavljac = true</c> — vidi "Trim, don't transplant whole" u
/// import-from-source-apps skill fajlu: zaseban Dobavljac model/ekran namerno nije prenet).
/// </summary>
public class Prijava
{
    public int Id { get; set; }
    public int BrojNaloga { get; set; }
    public int RedBroj { get; set; }

    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;

    public int ObracunskaJedinica { get; set; }

    public int? KontoId { get; set; }
    public Konto? Konto { get; set; }

    public int AmortizacionaGrupa1 { get; set; }
    public int AmortizacionaGrupa2 { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public DateTime DatumAktiviranja { get; set; }
    public int RevalorizacionaGrupa { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal OtpisanaVrednost { get; set; }
    public string JedinicaMere { get; set; } = string.Empty;
    public decimal Kolicina { get; set; }
    public string InventarskiBroj { get; set; } = string.Empty;
    public string BrojFakture { get; set; } = string.Empty;
    public DateTime? DatumFakture { get; set; }
    public int BrojNalaznice { get; set; }
    public string BrNal { get; set; } = string.Empty;
    public int GodNal { get; set; }
    public bool Knjizen { get; set; }

    public int? PartnerId { get; set; }
    public Partner? Partner { get; set; }
}
