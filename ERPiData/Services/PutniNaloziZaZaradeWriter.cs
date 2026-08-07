using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

// ── Oblik fajla koji Zarade modul čita ────────────────────────────────
// Nazivi svojstava su nazivi polja u fajlu i moraju odgovarati uvozniku na drugoj strani
// (PutniNaloziImportService u ERPiApp/Services/Zarade). Menjaju se samo uz podizanje
// VerzijaFormata. Isti kontrakt kao stari ERPiFinansije → ERPiZarade izvoz (nasleđen 1:1),
// ovde samo obe strane žive u istoj bazi/rešenju — vidi PLAN_NASTAVKA.md §D.13.

internal sealed class PutniNaloziZaZaradeFajl
{
    public string Format { get; set; } = PutniNaloziZaZaradeWriter.OznakaFormata;
    public int Verzija { get; set; } = PutniNaloziZaZaradeWriter.VerzijaFormata;
    public string Izvor { get; set; } = "";
    public PnzFirma? Firma { get; set; }

    /// <summary>Mesec kome prekoračenje pripada za PPP-PD — mesec isplate, ne mesec putovanja.</summary>
    public int Godina { get; set; }
    public int Mesec { get; set; }

    public List<PnzStavka> Stavke { get; set; } = [];
}

internal sealed class PnzFirma
{
    public string Naziv { get; set; } = "";
    public string? Pib { get; set; }
    public string? MaticniBroj { get; set; }
}

internal sealed class PnzStavka
{
    public string Jmbg { get; set; } = "";

    /// <summary>Samo za čitljivost pri proveri pre uvoza — uparivanje ide isključivo po JMBG-u.</summary>
    public string ZaposleniIme { get; set; } = "";

    public string BrojNaloga { get; set; } = "";
    public string DatumPovratka { get; set; } = "";
    public decimal UkupnoDnevnice { get; set; }
    public decimal NeoporeziviDeo { get; set; }

    /// <summary>Deo koji ulazi u zaradu — ono što se uvozi kao Iznos na strani Zarade.</summary>
    public decimal PrekoracenjeDnevnice { get; set; }
}

/// <summary>Jedna stavka izvoza, spremna za prikaz u tabeli pre potvrde snimanja.</summary>
public sealed class StavkaIzvozaZaZarade
{
    public required string Jmbg { get; init; }
    public required string ZaposleniIme { get; init; }
    public required string BrojNaloga { get; init; }
    public required DateTime DatumPovratka { get; init; }
    public required decimal UkupnoDnevnice { get; init; }
    public required decimal PrekoracenjeDnevnice { get; init; }
}

/// <summary>Šta je pripremljeno za izvoz i šta bi izvoz izostavio (već formatirane poruke za prikaz).</summary>
public sealed class RezultatIzvozaZaZarade
{
    public string? Json { get; init; }
    public IReadOnlyList<string> Nalazi { get; init; } = [];
    public IReadOnlyList<StavkaIzvozaZaZarade> Stavke { get; init; } = [];

    public int BrojStavki => Stavke.Count;
}

/// <summary>
/// Izvoz oporezivog dela dnevnice (prekoračenje neoporezivog limita) u fajl koji Zarade modul
/// uvozi u obračun zarade konkretnog radnika (<c>PutniNaloziImportService</c>).
///
/// Prekoračenje se ovde <b>računa</b>, ne prepisuje: ovaj servis je jedini koji zna i stvarno
/// isplaćenu dnevnicu (<see cref="PutniNalog.UkupnoDnevnice"/>) i zakonski limit koji je na dan
/// putovanja važio (<see cref="NeoporeziviIznosDnevnice"/>). Zarade modul taj broj samo
/// prepisuje u obračun.
///
/// Samo dnevnice <b>u zemlji</b> i samo nalozi koji su već proknjiženi
/// (<see cref="PutniNalog.IsKnjizeno"/>) — dok nalog nije proknjižen, iznosi i JMBG se još
/// mogu menjati.
/// </summary>
public static class PutniNaloziZaZaradeWriter
{
    /// <summary>Oznaka po kojoj uvoz prepoznaje fajl.</summary>
    public const string OznakaFormata = "ERPi-putni-nalozi-za-zarade";

    /// <summary>Broj verzije formata; menja se kad se promeni značenje nekog polja.</summary>
    public const int VerzijaFormata = 1;

    private static readonly JsonSerializerOptions Opcije = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// Sastavlja izvoz za dati mesec <b>isplate</b> dnevnice (po datumu povratka sa puta). Ne
    /// snima fajl — poziva se s ekrana koji prvo pokazuje šta je pronađeno i šta bi izvoz
    /// izostavio, pa tek onda snima na potvrdu.
    /// </summary>
    public static async Task<RezultatIzvozaZaZarade> GenerisiAsync(
        ErpiDbContext db, Firma? firma, int godina, int mesec)
    {
        var nalazi = new List<string>();
        var servis = new PutniNalogService(db);

        var nalozi = await db.PutniNalozi
            .Where(p => p.IsKnjizeno
                     && p.Vrsta == VrstaSlužbenogPutovanja.Zemlja
                     && p.DatumPovratka.Year == godina
                     && p.DatumPovratka.Month == mesec)
            .OrderBy(p => p.DatumPovratka)
            .ToListAsync();

        if (nalozi.Count == 0)
        {
            nalazi.Add($"[Upozorenje] Nema proknjiženih putnih naloga: za {mesec:D2}/{godina} " +
                       "nema nijednog proknjiženog putnog naloga u zemlji.");
            return new RezultatIzvozaZaZarade { Nalazi = nalazi };
        }

        if (string.IsNullOrWhiteSpace(firma?.Pib))
        {
            nalazi.Add("[Upozorenje] Firma nema unet PIB: Zarade modul PIB koristi samo kao " +
                       "dodatnu proveru pri uvozu — izvoz nastavlja i bez njega.");
        }

        var stavke = new List<PnzStavka>();
        var prikaz = new List<StavkaIzvozaZaZarade>();

        foreach (var nalog in nalozi)
        {
            if (string.IsNullOrWhiteSpace(nalog.Jmbg))
            {
                nalazi.Add($"[Greška] Nalog bez JMBG-a: putni nalog {nalog.BrojNaloga} " +
                           $"({nalog.ZaposleniIme}) nema unet JMBG i izostaje iz izvoza. Unesite " +
                           "JMBG na nalogu i izvezite ponovo.");
                continue;
            }

            decimal limit = await servis.VaziciNeoporeziviIznosAsync(nalog.DatumPovratka);
            if (limit <= 0m)
            {
                nalazi.Add("[Greška] Neoporezivi iznos dnevnice nije unet: za " +
                           $"{nalog.DatumPovratka:dd.MM.yyyy} nema unetog zakonskog limita " +
                           $"(šifarnik „Neoporezivi iznos dnevnice“). Nalog {nalog.BrojNaloga} " +
                           "izostaje iz izvoza dok se limit ne unese.");
                continue;
            }

            decimal prekoracenje = PutniNalogService.PrekoracenjeDnevnice(
                nalog.UkupnoDnevnice, nalog.BrojDnevnica, limit);

            if (prekoracenje <= 0m) continue; // Cela dnevnica je neoporeziva — nema šta da uđe u zaradu.

            stavke.Add(new PnzStavka
            {
                Jmbg = nalog.Jmbg.Trim(),
                ZaposleniIme = nalog.ZaposleniIme,
                BrojNaloga = nalog.BrojNaloga,
                DatumPovratka = nalog.DatumPovratka.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                UkupnoDnevnice = nalog.UkupnoDnevnice,
                NeoporeziviDeo = nalog.UkupnoDnevnice - prekoracenje,
                PrekoracenjeDnevnice = prekoracenje
            });

            prikaz.Add(new StavkaIzvozaZaZarade
            {
                Jmbg = nalog.Jmbg.Trim(),
                ZaposleniIme = nalog.ZaposleniIme,
                BrojNaloga = nalog.BrojNaloga,
                DatumPovratka = nalog.DatumPovratka,
                UkupnoDnevnice = nalog.UkupnoDnevnice,
                PrekoracenjeDnevnice = prekoracenje
            });
        }

        if (stavke.Count == 0)
        {
            nalazi.Add("[Upozorenje] Nema prekoračenja za izvoz: nijedan proknjižen nalog za " +
                       $"{mesec:D2}/{godina} ne prelazi neoporezivi iznos (ili su svi izostavljeni " +
                       "zbog nalaza iznad).");
            return new RezultatIzvozaZaZarade { Nalazi = nalazi };
        }

        var fajl = new PutniNaloziZaZaradeFajl
        {
            Izvor = $"ERPi {Verzija()}",
            Firma = firma == null ? null : new PnzFirma
            {
                Naziv = firma.Naziv,
                Pib = Prazno(firma.Pib),
                MaticniBroj = Prazno(firma.MaticniBroj)
            },
            Godina = godina,
            Mesec = mesec,
            Stavke = stavke
        };

        return new RezultatIzvozaZaZarade
        {
            Json = JsonSerializer.Serialize(fajl, Opcije),
            Nalazi = nalazi,
            Stavke = prikaz
        };
    }

    private static string? Prazno(string? vrednost)
        => string.IsNullOrWhiteSpace(vrednost) ? null : vrednost.Trim();

    private static string Verzija()
        => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
}
