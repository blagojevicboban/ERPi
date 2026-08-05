using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class KontaService
{
    private readonly ErpiDbContext _db;

    public KontaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<Konto>> GetKontaAsync(string? search = null)
    {
        var query = _db.Konta.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKonta.Contains(search) || k.NazivKonta.Contains(search));
        }
        return await query.OrderBy(k => k.BrojKonta).ToListAsync();
    }

    public async Task<Konto?> GetKontoByIdAsync(int id)
    {
        return await _db.Konta.FirstOrDefaultAsync(k => k.KontoId == id);
    }

    public async Task<Konto?> GetKontoByBrojAsync(string brojKonta)
    {
        return await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.ToLower() == brojKonta.ToLower());
    }

    public async Task<Konto> SaveKontoAsync(Konto konto)
    {
        if (konto.BrojKonta.Length > 0 && char.IsDigit(konto.BrojKonta[0]))
        {
            konto.Klasa = konto.BrojKonta[0] - '0';
        }
        konto.IsSintetika = konto.BrojKonta.Length <= 3;

        if (konto.KontoId == 0)
        {
            var postojeci = await GetKontoByBrojAsync(konto.BrojKonta);
            if (postojeci != null)
            {
                throw new InvalidOperationException($"Konto sa brojem {konto.BrojKonta} već postoji!");
            }
            _db.Konta.Add(konto);
        }
        else
        {
            _db.Konta.Update(konto);
        }

        await _db.SaveChangesAsync();
        return konto;
    }

    public async Task<bool> CanDeleteKontoAsync(int kontoId)
    {
        bool imaStavki = await _db.StavkeNaloga.AnyAsync(s => s.KontoId == kontoId);
        return !imaStavki;
    }

    public async Task<bool> DeleteKontoAsync(int id)
    {
        var konto = await GetKontoByIdAsync(id);
        if (konto == null) return false;

        bool canDelete = await CanDeleteKontoAsync(konto.KontoId);
        if (!canDelete)
        {
            throw new InvalidOperationException($"Konto {konto.BrojKonta} se ne može obrisati jer postoje knjiženja koja ga koriste.");
        }

        _db.Konta.Remove(konto);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteKontaAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return 0;

        var konta = await _db.Konta.Where(k => idList.Contains(k.KontoId)).ToListAsync();
        var zauzetiIds = await _db.StavkeNaloga
            .Where(s => idList.Contains(s.KontoId))
            .Select(s => s.KontoId)
            .Distinct()
            .ToListAsync();

        if (zauzetiIds.Any())
        {
            var zauzetiKonta = konta.Where(k => zauzetiIds.Contains(k.KontoId)).Select(k => k.BrojKonta);
            var spisak = string.Join(", ", zauzetiKonta);
            throw new InvalidOperationException($"Sledeća konta se ne mogu obrisati jer postoje knjiženja sa njima: {spisak}");
        }

        _db.Konta.RemoveRange(konta);
        await _db.SaveChangesAsync();
        return konta.Count;
    }
}
