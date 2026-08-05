using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Vodi materijalnu karticu po prosečnoj (ponderisanoj) ceni — port 1:1 iz
/// ERPiFinansijeData (analogno legacy dodaj_mat_kar / ul_dodaj_m_kar / tr_dodaj_m_kar
/// procedurama iz M2/M3.PRG, validirano protiv stvarnog legacy M_KART.DBF snapshota).
///
/// <see cref="MaterijalnaKartica"/> je čisto append-only knjigovodstvena traka (jedan red po
/// promeni), namerno zadržana sa string šiframa magacina/artikla kao u izvoru — nije master
/// entitet sa FK integritetom kao Ulaz/Trebovanje/Primopredaja nalozi (koji su ovde ispravljeni
/// na prave FK-ove, vidi PLAN_NASTAVKA.md §3g), već istorijski trag prometa.
/// </summary>
public class MaterijalnaKarticaService
{
    private readonly ErpiDbContext _db;

    public MaterijalnaKarticaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<Magacin>> GetMagaciniAsync(string? search = null)
    {
        var query = _db.Magacini.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.SifraMagacina.Contains(search) || m.NazivMagacina.Contains(search));
        }
        return await query.OrderBy(m => m.SifraMagacina).ToListAsync();
    }

    public async Task<List<Materijal>> GetMaterijaliAsync(string? search = null)
    {
        var query = _db.Materijali.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.SifraArtikla.Contains(search) || m.Naziv.Contains(search));
        }
        return await query.OrderBy(m => m.Naziv).ToListAsync();
    }

    /// <summary>Šifre materijala koji imaju bar jedan red na kartici — za "samo sa karticom" filter. sifraMagacina=null znači svi magacini.</summary>
    public async Task<HashSet<string>> GetArtikliSaKarticomAsync(string? sifraMagacina)
    {
        var upit = _db.MaterijalneKartice.AsQueryable();
        if (!string.IsNullOrEmpty(sifraMagacina)) upit = upit.Where(k => k.SifraMagacina == sifraMagacina);

        var sifre = await upit.Select(k => k.SifraArtikla).Distinct().ToListAsync();
        return new HashSet<string>(sifre, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Skuplja (magacin, materijal, kartice) trojke sa prometom. sifraMagacina=null znači svi magacini; materijalFilter=null znači svi materijali.</summary>
    public async Task<List<(Magacin Magacin, Materijal Materijal, List<MaterijalnaKartica> Kartice)>> PrikupiKarticeAsync(
        string? sifraMagacina, IReadOnlyCollection<Materijal>? materijalFilter)
    {
        var magaciniZaObradu = sifraMagacina == null
            ? await _db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync()
            : await _db.Magacini.Where(m => m.SifraMagacina == sifraMagacina).ToListAsync();

        var sifreFiltera = materijalFilter?.Select(a => a.SifraArtikla).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var materijalDict = await _db.Materijali.ToDictionaryAsync(a => a.SifraArtikla, a => a);
        var rezultat = new List<(Magacin, Materijal, List<MaterijalnaKartica>)>();

        foreach (var mag in magaciniZaObradu)
        {
            var upit = _db.MaterijalneKartice.Where(k => k.SifraMagacina == mag.SifraMagacina);
            if (sifreFiltera != null) upit = upit.Where(k => sifreFiltera.Contains(k.SifraArtikla));

            var sifreArtikala = await upit.Select(k => k.SifraArtikla).Distinct().ToListAsync();

            foreach (var sifra in sifreArtikala.OrderBy(s => s))
            {
                var kartice = await _db.MaterijalneKartice
                    .Where(k => k.SifraMagacina == mag.SifraMagacina && k.SifraArtikla == sifra)
                    .OrderBy(k => k.DatumPromene)
                    .ThenBy(k => k.MaterijalnaKarticaId)
                    .ToListAsync();

                if (kartice.Count == 0) continue;

                var materijal = materijalDict.TryGetValue(sifra, out var mat) ? mat : new Materijal { SifraArtikla = sifra, Naziv = sifra };
                rezultat.Add((mag, materijal, kartice));
            }
        }

        return rezultat;
    }

    /// <summary>Redovi kartice sa negativnim stanjem ili negativnom cenom — znak greške u knjiženju (legacy provera_m_kart()).</summary>
    public async Task<List<MaterijalnaKartica>> GetNegativnaStanjaAsync()
    {
        return await _db.MaterijalneKartice
            .Where(k => k.Stanje < 0 || k.Cena < 0)
            .OrderBy(k => k.SifraMagacina)
            .ThenBy(k => k.SifraArtikla)
            .ThenBy(k => k.RedniBroj)
            .ToListAsync();
    }

    public async Task<List<MaterijalnaKartica>> GetKarticaAsync(string sifraMagacina, string sifraArtikla)
    {
        return await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderBy(k => k.DatumPromene)
            .ThenBy(k => k.RedniBroj)
            .ToListAsync();
    }

    /// <summary>Tekuće stanje (količina) i tekući saldo (vrednost) — prosečna cena = saldo/stanje.</summary>
    public async Task<(decimal stanje, decimal saldo)> GetTrenutnoStanjeAsync(string sifraMagacina, string sifraArtikla)
    {
        var poslednja = await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderByDescending(k => k.RedniBroj)
            .FirstOrDefaultAsync();

        return poslednja == null ? (0m, 0m) : (poslednja.Stanje, poslednja.Saldo);
    }

    private async Task<int> SledeciRedniBrojAsync(string sifraMagacina, string sifraArtikla)
    {
        var poslednji = await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderByDescending(k => k.RedniBroj)
            .Select(k => (int?)k.RedniBroj)
            .FirstOrDefaultAsync();
        return (poslednji ?? 0) + 1;
    }

    /// <summary>Prijem robe — knjiži se po unetoj ceni; Saldo se akumulira (staro_saldo + kolicina*cena).</summary>
    public async Task DodajUlazRedAsync(string sifraMagacina, string sifraArtikla, DateTime datum, string opis, decimal kolicina, decimal cena)
    {
        var (staroStanje, staroSaldo) = await GetTrenutnoStanjeAsync(sifraMagacina, sifraArtikla);
        decimal iznos = kolicina * cena;

        _db.MaterijalneKartice.Add(new MaterijalnaKartica
        {
            SifraMagacina = sifraMagacina,
            SifraArtikla = sifraArtikla,
            RedniBroj = await SledeciRedniBrojAsync(sifraMagacina, sifraArtikla),
            DatumPromene = datum,
            OpisPromene = opis,
            Ulaz = kolicina,
            Izlaz = 0,
            Stanje = staroStanje + kolicina,
            Cena = cena,
            CenaIzlaz = 0,
            Duguje = iznos,
            Potrazuje = 0,
            Saldo = staroSaldo + iznos
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Izdavanje (trebovanje) — po TRENUTNOJ prosečnoj ceni (saldo/stanje), ne po
    /// nekoj unetoj ceni, jer se roba na zalihama vrednuje po ponderisanoj nabavnoj
    /// ceni. Baca grešku ako bi stanje otišlo u minus.
    /// </summary>
    public async Task<decimal> DodajIzlazRedAsync(string sifraMagacina, string sifraArtikla, DateTime datum, string opis, decimal kolicina)
    {
        var (staroStanje, staroSaldo) = await GetTrenutnoStanjeAsync(sifraMagacina, sifraArtikla);
        if (kolicina > staroStanje)
        {
            throw new InvalidOperationException(
                $"Nedovoljno stanje na zalihama za artikal {sifraArtikla} u magacinu {sifraMagacina} " +
                $"(na stanju: {staroStanje:N2}, traženo: {kolicina:N2}).");
        }

        decimal prosecnaCena = staroStanje != 0m ? staroSaldo / staroStanje : 0m;
        decimal iznos = kolicina * prosecnaCena;

        _db.MaterijalneKartice.Add(new MaterijalnaKartica
        {
            SifraMagacina = sifraMagacina,
            SifraArtikla = sifraArtikla,
            RedniBroj = await SledeciRedniBrojAsync(sifraMagacina, sifraArtikla),
            DatumPromene = datum,
            OpisPromene = opis,
            Ulaz = 0,
            Izlaz = kolicina,
            Stanje = staroStanje - kolicina,
            Cena = prosecnaCena,
            CenaIzlaz = prosecnaCena,
            Duguje = 0,
            Potrazuje = iznos,
            Saldo = staroSaldo - iznos
        });
        await _db.SaveChangesAsync();
        return iznos;
    }

    /// <summary>
    /// Uklanja poslednji upisani red kartice za dati magacin/artikal — koristi se pri
    /// rasknjiženju dokumenta (Ulaz/Trebovanje/Primopredaja). Dozvoljeno je samo ako je
    /// taj red zaista poslednji upisan (RedniBroj) I ako mu odgovara opis dokumenta koji
    /// se rasknjižava — u suprotnom bi brisanje pokvarilo tekuće stanje/saldo (prosečnu
    /// cenu) za naloge proknjižene posle njega, pa se baca greška.
    /// </summary>
    public async Task UkloniPoslednjiRedAsync(string sifraMagacina, string sifraArtikla, string opis)
    {
        var poslednji = await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderByDescending(k => k.RedniBroj)
            .FirstOrDefaultAsync();

        if (poslednji == null) return;

        if (poslednji.OpisPromene != opis)
        {
            throw new InvalidOperationException(
                $"Rasknjiženje nije moguće: za artikal {sifraArtikla} u magacinu {sifraMagacina} postoji kasnije " +
                "knjiženje, pa se ovaj nalog više ne može bezbedno rasknjižiti a da se ne pokvari stanje zaliha.");
        }

        _db.MaterijalneKartice.Remove(poslednji);
        await _db.SaveChangesAsync();
    }
}
