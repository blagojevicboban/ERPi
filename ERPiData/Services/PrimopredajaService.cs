using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za upravljanje dokumentima primopredaje (internog prenosa iz magacina u magacin).
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class PrimopredajaService
{
    private readonly ErpiDbContext _db;

    public PrimopredajaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<PrimopredajaNalog>> GetPrimopredajeAsync(string? search = null)
    {
        var query = _db.PrimopredajaNalozi
            .Include(p => p.Stavke).ThenInclude(s => s.Materijal)
            .Include(p => p.MagacinDaje)
            .Include(p => p.MagacinPrima)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.BrojNaloga.ToString().Contains(search)
                || (p.MagacinDaje != null && p.MagacinDaje.NazivMagacina.Contains(search))
                || (p.MagacinPrima != null && p.MagacinPrima.NazivMagacina.Contains(search)));
        }

        return await query.OrderByDescending(p => p.Datum)
            .ThenByDescending(p => p.PrimopredajaNalogId)
            .ToListAsync();
    }

    public async Task<PrimopredajaNalog> SavePrimopredajuAsync(PrimopredajaNalog nalog)
    {
        if (nalog.PrimopredajaNalogId == 0)
        {
            if (nalog.BrojNaloga == 0)
            {
                nalog.BrojNaloga = (await _db.PrimopredajaNalozi.Select(p => (int?)p.BrojNaloga).MaxAsync() ?? 0) + 1;
            }
            _db.PrimopredajaNalozi.Add(nalog);
        }
        else
        {
            var existing = await _db.PrimopredajaNalozi
                .Include(p => p.Stavke)
                .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);

            if (existing != null)
            {
                if (existing.IsKnjizen)
                    throw new InvalidOperationException("Proknjižena primopredaja se ne može menjati.");

                existing.BrojNaloga = nalog.BrojNaloga;
                existing.Datum = nalog.Datum;
                existing.MagacinIdDaje = nalog.MagacinIdDaje;
                existing.MagacinIdPrima = nalog.MagacinIdPrima;
                existing.StopaPdv = nalog.StopaPdv;
                existing.VrstaDokumenta = nalog.VrstaDokumenta;

                _db.PrimopredajaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = nalog.Stavke;
            }
        }

        await _db.SaveChangesAsync();
        return nalog;
    }

    public async Task KnjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke).ThenInclude(s => s.Materijal)
            .Include(p => p.MagacinDaje)
            .Include(p => p.MagacinPrima)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja je već proknjižena.");
        if (nalog.MagacinDaje == null || nalog.MagacinPrima == null)
            throw new InvalidOperationException("Magacini za primopredaju moraju biti izabrani.");

        bool prelaziVpMp = (nalog.MagacinDaje.VrstaMagacina ?? "Veleprodaja") != (nalog.MagacinPrima.VrstaMagacina ?? "Veleprodaja");

        var kartice = new MaterijalnaKarticaService(_db);
        decimal ukupnoVrednostDaje = 0m;
        decimal ukupnoVrednostPrima = 0m;

        string sifraDaje = nalog.MagacinDaje.SifraMagacina;
        string sifraPrima = nalog.MagacinPrima.SifraMagacina;

        foreach (var s in nalog.Stavke)
        {
            string sifraMaterijala = s.Materijal?.SifraArtikla ?? s.MaterijalId.ToString();

            // 1. Izlaz iz magacina koji daje
            decimal vrednost = await kartice.DodajIzlazRedAsync(
                sifraDaje,
                sifraMaterijala,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {sifraPrima}",
                s.Kolicina);

            // 2. Ulaz u magacin koji prima (preračun kod prelaska VP↔MP)
            decimal vrednostPrima = prelaziVpMp ? PreracunajVrednost(vrednost, nalog.MagacinDaje, nalog.MagacinPrima, nalog.StopaPdv) : vrednost;
            decimal jedinicaCena = s.Kolicina != 0 ? vrednostPrima / s.Kolicina : 0m;

            await kartice.DodajUlazRedAsync(
                sifraPrima,
                sifraMaterijala,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {sifraDaje}",
                s.Kolicina,
                jedinicaCena);

            ukupnoVrednostDaje += vrednost;
            ukupnoVrednostPrima += vrednostPrima;
        }

        if (prelaziVpMp && ukupnoVrednostDaje != 0)
        {
            nalog.NalogId = await KreirajNalogPrelazaVpMpAsync(nalog, nalog.MagacinDaje, nalog.MagacinPrima, ukupnoVrednostDaje, ukupnoVrednostPrima);
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    private static decimal PreracunajVrednost(decimal vrednost, Magacin magDaje, Magacin magPrima, decimal stopaPdv)
    {
        bool primaJeMaloprodaja = magPrima.VrstaMagacina == "Maloprodaja";
        bool dajeJeMaloprodaja = magDaje.VrstaMagacina == "Maloprodaja";

        if (primaJeMaloprodaja && !dajeJeMaloprodaja)
            return Math.Round(vrednost * (1 + stopaPdv / 100m), 2);

        if (!primaJeMaloprodaja && dajeJeMaloprodaja)
            return Math.Round(vrednost / (1 + stopaPdv / 100m), 2);

        return vrednost;
    }

    private async Task<int> KreirajNalogPrelazaVpMpAsync(PrimopredajaNalog nalog, Magacin magDaje, Magacin magPrima, decimal vrednostDaje, decimal vrednostPrima)
    {
        string kontoDajeStr = RobnaKonta.RobaZaVrstuMagacina(magDaje.VrstaMagacina);
        string kontoPrimaStr = RobnaKonta.RobaZaVrstuMagacina(magPrima.VrstaMagacina);
        string kontoPdvStr = RobnaKonta.UkalkulisaniPdvZaStopu(nalog.StopaPdv);

        var kontoDaje = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoDajeStr);
        var kontoPrima = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoPrimaStr);
        var kontoPdv = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoPdvStr);

        decimal pdvIznos = Math.Abs(vrednostPrima - vrednostDaje);
        bool prelazUMaloprodaju = magPrima.VrstaMagacina == "Maloprodaja";

        string opis = $"{nalog.VrstaDokumenta} br. {nalog.BrojNaloga} ({magDaje.SifraMagacina} → {magPrima.SifraMagacina})";
        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

        var glavniNalog = new Nalog
        {
            BrojNaloga = sledeciBroj,
            DatumNaloga = nalog.Datum,
            Opis = opis,
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = DateTime.Now,
            VrstaNaloga = "PRIMOPREDAJA"
        };

        int rb = 1;
        if (kontoPrima != null)
        {
            glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoPrima.KontoId, Opis = opis, Duguje = vrednostPrima, Potrazuje = 0m });
        }
        if (kontoDaje != null)
        {
            glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, KontoId = kontoDaje.KontoId, Opis = opis, Duguje = 0m, Potrazuje = vrednostDaje });
        }

        if (pdvIznos != 0 && kontoPdv != null)
        {
            if (prelazUMaloprodaju)
                glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb, KontoId = kontoPdv.KontoId, Opis = opis, Duguje = 0m, Potrazuje = pdvIznos });
            else
                glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb, KontoId = kontoPdv.KontoId, Opis = opis, Duguje = pdvIznos, Potrazuje = 0m });
        }

        glavniNalog.UkupnoDuguje = glavniNalog.Stavke.Sum(s => s.Duguje);
        glavniNalog.UkupnoPotrazuje = glavniNalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(glavniNalog);
        await _db.SaveChangesAsync();
        return glavniNalog.NalogId;
    }

    public async Task RasknjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke).ThenInclude(s => s.Materijal)
            .Include(p => p.MagacinDaje)
            .Include(p => p.MagacinPrima)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (!nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja nije proknjižena.");
        if (nalog.MagacinDaje == null || nalog.MagacinPrima == null)
            throw new InvalidOperationException("Magacini za primopredaju nisu pronađeni.");

        var kartice = new MaterijalnaKarticaService(_db);
        string sifraDaje = nalog.MagacinDaje.SifraMagacina;
        string sifraPrima = nalog.MagacinPrima.SifraMagacina;

        foreach (var s in nalog.Stavke.AsEnumerable().Reverse())
        {
            string sifraMaterijala = s.Materijal?.SifraArtikla ?? s.MaterijalId.ToString();

            await kartice.UkloniPoslednjiRedAsync(
                sifraPrima,
                sifraMaterijala,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {sifraDaje}");

            await kartice.UkloniPoslednjiRedAsync(
                sifraDaje,
                sifraMaterijala,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {sifraPrima}");
        }

        if (nalog.NalogId.HasValue)
        {
            var glavniNalog = await _db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == nalog.NalogId.Value);
            if (glavniNalog != null)
            {
                _db.StavkeNaloga.RemoveRange(glavniNalog.Stavke);
                _db.Nalozi.Remove(glavniNalog);
            }
            nalog.NalogId = null;
        }

        nalog.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}
