using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za nivelacije cena robe na zalihama i automatsko knjiženje razlike u ceni.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class NivelacijaService
{
    private readonly ErpiDbContext _db;

    public NivelacijaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<NivelacijaCena>> GetNivelacijeAsync(string? pretraga = null)
    {
        var query = _db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(pretraga))
        {
            pretraga = pretraga.ToLower();
            query = query.Where(n =>
                n.BrojNivelacije.ToString().Contains(pretraga) ||
                (n.Opis != null && n.Opis.ToLower().Contains(pretraga)) ||
                (n.Magacin != null && n.Magacin.NazivMagacina.ToLower().Contains(pretraga)));
        }

        return await query.OrderByDescending(n => n.DatumNivelacije)
            .ThenByDescending(n => n.NivelacijaCenaId)
            .ToListAsync();
    }

    public async Task<NivelacijaCena?> GetNivelacijaByIdAsync(int id)
    {
        return await _db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == id);
    }

    public async Task<NivelacijaCena> SaveNivelacijaAsync(NivelacijaCena niv)
    {
        niv.UkupnoRazlika = niv.Stavke.Sum(s => s.UkupnaRazlika);

        if (niv.NivelacijaCenaId == 0)
        {
            if (niv.BrojNivelacije == 0)
            {
                niv.BrojNivelacije = (await _db.NivelacijeCena.Select(n => (int?)n.BrojNivelacije).MaxAsync() ?? 0) + 1;
            }
            _db.NivelacijeCena.Add(niv);
        }
        else
        {
            var existing = await _db.NivelacijeCena
                .Include(n => n.Stavke)
                .FirstOrDefaultAsync(n => n.NivelacijaCenaId == niv.NivelacijaCenaId);

            if (existing != null)
            {
                if (existing.IsKnjizen)
                    throw new InvalidOperationException("Proknjižena nivelacija cena se ne može menjati.");

                existing.BrojNivelacije = niv.BrojNivelacije;
                existing.DatumNivelacije = niv.DatumNivelacije;
                existing.MagacinId = niv.MagacinId;
                existing.Opis = niv.Opis;
                existing.UkupnoRazlika = niv.UkupnoRazlika;

                _db.NivelacijeStavke.RemoveRange(existing.Stavke);
                existing.Stavke = niv.Stavke;
            }
        }

        await _db.SaveChangesAsync();
        return niv;
    }

    public async Task<bool> KnjiziNivelacijuAsync(int id)
    {
        var niv = await _db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == id);

        if (niv == null || niv.IsKnjizen) return false;

        // Ažuriranje cena u artiklima
        foreach (var st in niv.Stavke)
        {
            if (st.Artikal != null && st.NovaCena > 0)
            {
                st.Artikal.ProdajnaCena = st.NovaCena;
            }
        }

        // Kreiranje naloga knjiženja za razliku u ceni
        if (niv.UkupnoRazlika != 0)
        {
            string kontoMagacinaStr = RobnaKonta.RobaZaVrstuMagacina(niv.Magacin?.VrstaMagacina);
            string kontoRazlikeStr = RobnaKonta.RazlikaZaVrstuMagacina(niv.Magacin?.VrstaMagacina);

            var kontoMagacina = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoMagacinaStr);
            var kontoRazlike = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoRazlikeStr);

            int sledeciBrojNaloga = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;
            var nalog = new Nalog
            {
                BrojNaloga = sledeciBrojNaloga,
                DatumNaloga = niv.DatumNivelacije,
                Opis = $"Nivelacija cena br. {niv.BrojNivelacije}",
                Status = StatusNaloga.Proknjizen,
                DatumKnjizenja = DateTime.Now,
                VrstaNaloga = "Nivelacija"
            };

            int rb = 1;
            if (niv.UkupnoRazlika > 0)
            {
                if (kontoMagacina != null)
                    nalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoMagacina.KontoId, Opis = nalog.Opis, Duguje = niv.UkupnoRazlika, Potrazuje = 0 });
                if (kontoRazlike != null)
                    nalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoRazlike.KontoId, Opis = nalog.Opis, Duguje = 0, Potrazuje = niv.UkupnoRazlika });
            }
            else
            {
                decimal absRazlika = Math.Abs(niv.UkupnoRazlika);
                if (kontoRazlike != null)
                    nalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoRazlike.KontoId, Opis = nalog.Opis, Duguje = absRazlika, Potrazuje = 0 });
                if (kontoMagacina != null)
                    nalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoMagacina.KontoId, Opis = nalog.Opis, Duguje = 0, Potrazuje = absRazlika });
            }

            nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
            nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

            _db.Nalozi.Add(nalog);
            await _db.SaveChangesAsync();
            niv.NalogId = nalog.NalogId;
        }

        niv.IsKnjizen = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RasknjiziNivelacijuAsync(int id)
    {
        var niv = await _db.NivelacijeCena
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == id);

        if (niv == null || !niv.IsKnjizen) return false;

        foreach (var st in niv.Stavke)
        {
            if (st.Artikal != null && st.NovaCena > 0 && st.Artikal.ProdajnaCena != st.NovaCena)
            {
                throw new InvalidOperationException(
                    $"Rasknjiženje nije moguće: cena artikla {st.Artikal.SifraArtikla} je naknadno menjana " +
                    "(nakon ove nivelacije), pa se ne može bezbedno vratiti na staru vrednost.");
            }
        }

        foreach (var st in niv.Stavke)
        {
            if (st.Artikal != null && st.NovaCena > 0)
            {
                st.Artikal.ProdajnaCena = st.StaraCena;
            }
        }

        if (niv.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.FirstOrDefaultAsync(n => n.NalogId == niv.NalogId.Value);
            if (nalog != null) _db.Nalozi.Remove(nalog);
        }

        niv.IsKnjizen = false;
        niv.NalogId = null;
        await _db.SaveChangesAsync();
        return true;
    }
}
