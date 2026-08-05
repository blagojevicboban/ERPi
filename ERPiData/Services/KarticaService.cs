using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class KarticaRed
{
    public int StavkaNalogaId { get; set; }
    public int RedniBroj { get; set; }
    public int NalogId { get; set; }
    public DateTime Datum { get; set; }
    public int BrojNaloga { get; set; }
    public string? Opis { get; set; }
    public string? OpisPromene { get; set; }
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }

    public decimal? Preostalo { get; set; }
    public string? StatusZatvaranja { get; set; }
    public DateTime? ValutaDospela { get; set; }
    public int? DanaKasnjenja { get; set; }
}

public class KarticaService
{
    private readonly ErpiDbContext _db;

    public KarticaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<Konto>> GetKontaAsync(bool samoSaPrometom = false, string? search = null)
    {
        if (samoSaPrometom)
        {
            var activeKontoIds = await _db.StavkeNaloga
                .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen)
                .Select(s => s.KontoId)
                .Distinct()
                .ToListAsync();

            var queryable = _db.Konta.Where(k => activeKontoIds.Contains(k.KontoId));
            if (!string.IsNullOrWhiteSpace(search))
            {
                queryable = queryable.Where(k => k.BrojKonta.Contains(search) || k.NazivKonta.Contains(search));
            }
            return await queryable.OrderBy(k => k.BrojKonta).ToListAsync();
        }

        var query = _db.Konta.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKonta.Contains(search) || k.NazivKonta.Contains(search));
        }
        return await query.OrderBy(k => k.BrojKonta).ToListAsync();
    }

    public async Task<List<KarticaRed>> GetKarticaKontaAsync(string brojKonta, DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Konto)
            .Include(s => s.Nalog)
            .Where(s => s.Konto != null && s.Konto.BrojKonta == brojKonta && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        var rezultat = new List<KarticaRed>();
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            saldo += s.Duguje - s.Potrazuje;

            string prikazOpis;
            if (!string.IsNullOrWhiteSpace(s.BrojDokumenta))
            {
                prikazOpis = s.BrojDokumenta;
            }
            else if (!string.IsNullOrWhiteSpace(s.Opis))
            {
                prikazOpis = s.Opis;
            }
            else if (!string.IsNullOrWhiteSpace(s.Nalog?.Opis))
            {
                prikazOpis = s.Nalog.Opis;
            }
            else
            {
                prikazOpis = "";
            }

            rezultat.Add(new KarticaRed
            {
                StavkaNalogaId = s.StavkaNalogaId,
                RedniBroj = s.RedniBroj,
                NalogId = s.Nalog!.NalogId,
                Datum = s.Nalog.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = prikazOpis,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        if (odDatuma.HasValue) rezultat = rezultat.Where(r => r.Datum >= odDatuma.Value).ToList();
        if (doDatuma.HasValue) rezultat = rezultat.Where(r => r.Datum <= doDatuma.Value).ToList();

        return rezultat;
    }
}
