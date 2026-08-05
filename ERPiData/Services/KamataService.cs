using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class KamataStavka
{
    public DateTime Datum { get; set; }
    public int BrojNaloga { get; set; }
    public string? Opis { get; set; }
    public decimal Iznos { get; set; }
    public int BrojDanaKasnjenja { get; set; }
    public decimal ObracunataKamata { get; set; }
}

/// <summary>
/// Obračun zatezne kamate na dugovne otvorene stavke partnera (konto kupaca 204/120), konformni
/// metod — formula 1:1 preneta iz ERPiFinansijeData.KamataService. Skraćeno u odnosu na tu
/// verziju: bez "sintetički konto" grane (ObracunajKamatuZaKontoAsync) jer ERPi šema nema
/// legacy DBF razlog da StavkaNaloga.PartnerId izostane — vidi ZatvaranjeStavkiService.
/// </summary>
public class KamataService
{
    private readonly ErpiDbContext _db;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;

    public KamataService(ErpiDbContext db)
    {
        _db = db;
        _zatvaranjeService = new ZatvaranjeStavkiService(db);
    }

    public async Task EnsureSeedRatesAsync()
    {
        if (!await _db.KamatneStope.AnyAsync())
        {
            var defaultStope = new List<KamatnaStopa>
            {
                new KamatnaStopa { DatumOd = new DateTime(2021, 1, 1), GodisnjaStopaProcenat = 8.00m, Napomena = "Zakon o zateznoj kamati NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2022, 1, 1), GodisnjaStopaProcenat = 8.50m, Napomena = "Referentna stopa NBS + 8%" },
                new KamatnaStopa { DatumOd = new DateTime(2022, 7, 1), GodisnjaStopaProcenat = 10.00m, Napomena = "Korekcija stope NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2023, 1, 1), GodisnjaStopaProcenat = 13.00m, Napomena = "Referentna kamatna stopa NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2023, 7, 1), GodisnjaStopaProcenat = 14.00m, Napomena = "Stopa zatezne kamate NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2024, 1, 1), GodisnjaStopaProcenat = 14.50m, Napomena = "Zatezna kamatna stopa 2024" },
                new KamatnaStopa { DatumOd = new DateTime(2024, 7, 1), GodisnjaStopaProcenat = 14.00m, Napomena = "Korekcija kamatne stope NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2025, 1, 1), GodisnjaStopaProcenat = 13.75m, Napomena = "Stopa zatezne kamate 2025" },
                new KamatnaStopa { DatumOd = new DateTime(2026, 1, 1), GodisnjaStopaProcenat = 13.50m, Napomena = "Važeća stopa zatezne kamate 2026" }
            };

            _db.KamatneStope.AddRange(defaultStope);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<KamatnaStopa>> GetStopeAsync()
    {
        await EnsureSeedRatesAsync();
        return await _db.KamatneStope.OrderBy(k => k.DatumOd).ToListAsync();
    }

    public async Task<KamatnaStopa> DodajStopuAsync(DateTime datumOd, decimal godisnjaStopaProcenat, string? napomena)
    {
        var stopa = new KamatnaStopa { DatumOd = datumOd, GodisnjaStopaProcenat = godisnjaStopaProcenat, Napomena = napomena };
        _db.KamatneStope.Add(stopa);
        await _db.SaveChangesAsync();
        return stopa;
    }

    public async Task BrisiStopuAsync(int kamatnaStopaId)
    {
        var item = await _db.KamatneStope.FindAsync(kamatnaStopaId);
        if (item != null)
        {
            _db.KamatneStope.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obračun zatezne kamate na dugovne (Duguje) otvorene stavke partnera po konformnom metodu.
    /// Osnovica je preostali (nezatvoreni) iznos svake stavke — ako je faktura delimično/potpuno
    /// zatvorena uplatom, kamata se računa samo na ono što stvarno stoji otvoreno na datumObracuna.
    /// </summary>
    public async Task<List<KamataStavka>> ObracunajKamatuAsync(int partnerId, DateTime datumObracuna)
    {
        var otvoreneStavke = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partnerId, datumObracuna, samoOtvorene: true);

        var stope = await GetStopeAsync();
        if (stope.Count == 0)
            throw new InvalidOperationException("Nema unetih kamatnih stopa — unesite bar jednu stopu pre obračuna.");

        var rezultat = new List<KamataStavka>();
        // Kamata se obračunava samo na dug kupca prema nama (konto kupaca 204/120, strana Duguje) —
        // otvorene stavke na kontu dobavljača su naše obaveze prema partneru, ne njegov dug prema nama.
        foreach (var s in otvoreneStavke.Where(s => s.Strana == "Duguje" && JeKontoKupca(s.Konto)))
        {
            var datumDuga = s.Datum.Date;
            if (datumDuga >= datumObracuna.Date) continue;

            int dana = (datumObracuna.Date - datumDuga).Days;
            decimal kamata = ObracunajKamatuZaPeriod(s.Preostalo, datumDuga, datumObracuna.Date, stope);

            if (kamata > 0)
            {
                rezultat.Add(new KamataStavka
                {
                    Datum = datumDuga,
                    BrojNaloga = s.BrojNaloga,
                    Opis = s.Opis,
                    Iznos = s.Preostalo,
                    BrojDanaKasnjenja = dana,
                    ObracunataKamata = kamata
                });
            }
        }

        return rezultat;
    }

    /// <summary>Konto kupaca po novom (204) ili starom (120) zakonu — poredi se prefiks Konto.Prikaz stringa ("204... - Naziv").</summary>
    private static bool JeKontoKupca(string? konto)
        => !string.IsNullOrWhiteSpace(konto) && (konto.StartsWith("204") || konto.StartsWith("120"));

    private static decimal ObracunajKamatuZaPeriod(decimal glavnica, DateTime od, DateTime doDatuma, List<KamatnaStopa> stopeSortirane)
    {
        var granice = new List<DateTime> { od };
        granice.AddRange(stopeSortirane.Select(s => s.DatumOd.Date).Where(d => d > od && d < doDatuma));
        granice.Add(doDatuma);
        granice = granice.Distinct().OrderBy(d => d).ToList();

        decimal ukupno = 0m;
        for (int i = 0; i < granice.Count - 1; i++)
        {
            DateTime periodOd = granice[i];
            DateTime periodDo = granice[i + 1];
            int dana = (periodDo - periodOd).Days;
            if (dana <= 0) continue;

            var stopa = stopeSortirane
                .Where(s => s.DatumOd.Date <= periodOd)
                .OrderByDescending(s => s.DatumOd)
                .FirstOrDefault();
            if (stopa == null) continue;

            // Konformni metod: glavnica * ((1 + r/100)^(dana/365) - 1)
            double r = (double)(stopa.GodisnjaStopaProcenat / 100m);
            double koeficijent = Math.Pow(1.0 + r, (double)dana / 365.0) - 1.0;
            decimal parcijalnaKamata = glavnica * (decimal)koeficijent;

            ukupno += parcijalnaKamata;
        }

        return Math.Round(ukupno, 2);
    }

    /// <summary>
    /// Knjiži obračunatu zateznu kamatu u Glavnu knjigu (konto kupca partnera Duguje / konto
    /// 662000 "Prihodi od zateznih kamata" Potražuje). Za razliku od ERPiFinansijeData verzije
    /// (koja bez provere upisuje string "204000" ako partner nema stavki), ovde konto MORA biti
    /// stvaran FK — bez postojeće dugovne stavke na kontu kupca nema odakle da se preuzme KontoId.
    /// </summary>
    public async Task<Nalog> ProknjiziKamatuNalogAsync(int partnerId, decimal ukupnaKamata, DateTime datumObracuna, string? opis)
    {
        if (ukupnaKamata <= 0)
            throw new InvalidOperationException("Iznos kamate za knjiženje mora biti veći od 0.");

        var partner = await _db.Partneri.FindAsync(partnerId);
        if (partner == null)
            throw new ArgumentException("Partner nije pronađen.");

        var stavkaKupca = await _db.StavkeNaloga
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == partnerId && s.Konto != null &&
                (s.Konto.BrojKonta.StartsWith("204") || s.Konto.BrojKonta.StartsWith("120")))
            .OrderByDescending(s => s.StavkaNalogaId)
            .FirstOrDefaultAsync();

        if (stavkaKupca?.Konto == null)
            throw new InvalidOperationException("Partner nema proknjiženih stavki na kontu kupca (204/120) — kamata se ne može proknjižiti bez postojećeg duga.");

        var kontoPrihod = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "662000");
        if (kontoPrihod == null)
            throw new InvalidOperationException("Konto 662000 (Prihodi od zateznih kamata) ne postoji u kontnom planu — dodajte ga pre knjiženja kamate.");

        int maxBrojNaloga = await _db.Nalozi.MaxAsync(n => (int?)n.BrojNaloga) ?? 0;

        var nalog = new Nalog
        {
            BrojNaloga = maxBrojNaloga + 1,
            DatumNaloga = datumObracuna,
            Opis = string.IsNullOrWhiteSpace(opis) ? $"Obračun zatezne kamate za partnera {partner.Naziv}" : opis,
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = datumObracuna,
            UkupnoDuguje = ukupnaKamata,
            UkupnoPotrazuje = ukupnaKamata,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga
                {
                    RedniBroj = 1,
                    KontoId = stavkaKupca.Konto.KontoId,
                    PartnerId = partnerId,
                    Opis = $"Obračunata zatezna kamata do {datumObracuna:dd.MM.yyyy}",
                    Duguje = ukupnaKamata,
                    Potrazuje = 0m
                },
                new StavkaNaloga
                {
                    RedniBroj = 2,
                    KontoId = kontoPrihod.KontoId,
                    Opis = $"Prihod od zatezne kamate — {partner.Naziv}",
                    Duguje = 0m,
                    Potrazuje = ukupnaKamata
                }
            }
        };

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        return nalog;
    }
}
