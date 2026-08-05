using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Port iz ERPiFinansijeData — <see cref="TrebovanjeNalog"/>/<see cref="TrebovanjeStavka"/> ovde
/// nose prave FK-ove (<c>MagacinId</c>/<c>MaterijalId</c>) umesto DBF-stil stringova, vidi napomenu
/// u <see cref="UlazService"/>.
/// </summary>
public class TrebovanjeService
{
    private readonly ErpiDbContext _db;

    public TrebovanjeService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<TrebovanjeNalog>> GetTrebovanjaAsync(string? search = null)
    {
        var query = _db.TrebovanjeNalozi.Include(n => n.Stavke).Include(n => n.Magacin).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.BrojNaloga.ToString().Contains(search));
        }
        return await query.OrderByDescending(n => n.Datum).ToListAsync();
    }

    public async Task<TrebovanjeNalog> SaveTrebovanjeAsync(TrebovanjeNalog nalog)
    {
        if (nalog.TrebovanjeNalogId == 0)
        {
            _db.TrebovanjeNalozi.Add(nalog);
        }
        else
        {
            _db.TrebovanjeNalozi.Update(nalog);
        }
        await _db.SaveChangesAsync();
        return nalog;
    }

    /// <summary>
    /// Izmena postojećeg, neproknjiženog trebovanja — briše stare stavke i upisuje
    /// nove (legacy izmena_treb() dozvoljava izmenu samo dok nije proknjiženo).
    /// </summary>
    public async Task UpdateTrebovanjeAsync(int trebovanjeNalogId, DateTime datum, int magacinId, List<TrebovanjeStavka> noveStavke)
    {
        var nalog = await _db.TrebovanjeNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.TrebovanjeNalogId == trebovanjeNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Nalog trebovanja nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Trebovanje {nalog.BrojNaloga} je već proknjiženo i nisu dozvoljene nikakve izmene.");
        }

        nalog.Datum = datum;
        nalog.MagacinId = magacinId;

        _db.TrebovanjeStavke.RemoveRange(nalog.Stavke);
        nalog.Stavke.Clear();
        foreach (var s in noveStavke)
        {
            nalog.Stavke.Add(s);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Knjiži trebovanje — izdavanje materijala iz magacina po trenutnoj prosečnoj
    /// ceni. Baca grešku ako bi neka stavka izazvala negativno stanje.
    /// </summary>
    public async Task KnjiziTrebovanjeAsync(int trebovanjeNalogId)
    {
        var nalog = await _db.TrebovanjeNalozi
            .Include(n => n.Stavke).ThenInclude(s => s.Materijal)
            .Include(n => n.Magacin)
            .FirstOrDefaultAsync(n => n.TrebovanjeNalogId == trebovanjeNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Nalog trebovanja nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Trebovanje {nalog.BrojNaloga} je već proknjiženo.");
        }

        string sifraMagacina = nalog.Magacin!.SifraMagacina;
        var kartice = new MaterijalnaKarticaService(_db);
        foreach (var s in nalog.Stavke)
        {
            await kartice.DodajIzlazRedAsync(sifraMagacina, s.Materijal!.SifraArtikla, nalog.Datum, $"Trebovanje {nalog.BrojNaloga}", s.Kolicina);
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rasknjiži trebovanje — uklanja redove materijalne kartice koje je ovaj nalog upisao
    /// (obrnutim redosledom od knjiženja) i vraća nalog u status nacrta radi izmene. Baca
    /// grešku ako je za neki artikal u međuvremenu knjiženo nešto kasnije.
    /// </summary>
    public async Task RasknjiziTrebovanjeAsync(int trebovanjeNalogId)
    {
        var nalog = await _db.TrebovanjeNalozi
            .Include(n => n.Stavke).ThenInclude(s => s.Materijal)
            .Include(n => n.Magacin)
            .FirstOrDefaultAsync(n => n.TrebovanjeNalogId == trebovanjeNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Nalog trebovanja nije pronađen.");
        }
        if (!nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Trebovanje {nalog.BrojNaloga} nije proknjiženo.");
        }

        string sifraMagacina = nalog.Magacin!.SifraMagacina;
        var kartice = new MaterijalnaKarticaService(_db);
        foreach (var s in nalog.Stavke.AsEnumerable().Reverse())
        {
            await kartice.UkloniPoslednjiRedAsync(sifraMagacina, s.Materijal!.SifraArtikla, $"Trebovanje {nalog.BrojNaloga}");
        }

        nalog.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}
