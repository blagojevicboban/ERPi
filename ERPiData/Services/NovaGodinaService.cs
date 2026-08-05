using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za prenos početnog stanja u novu poslovnu godinu na osnovu zaključnih salda proknjiženih naloga.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class NovaGodinaService
{
    private readonly ErpiDbContext _db;

    public NovaGodinaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<BrutoBilansRed>> GetZakljucniSaldoAsync(int godina)
    {
        var krajGodine = new DateTime(godina, 12, 31, 23, 59, 59);

        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Konto)
            .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen && s.Nalog.DatumNaloga <= krajGodine)
            .ToListAsync();

        return stavke
            .GroupBy(s => s.KontoId)
            .Select(g =>
            {
                var firstKonto = g.First().Konto;
                string brojKonta = firstKonto?.BrojKonta ?? g.Key.ToString();
                string nazivKonta = firstKonto?.NazivKonta ?? "";

                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                decimal saldo = duguje - potrazuje;
                return new BrutoBilansRed
                {
                    BrojKonta = brojKonta,
                    NazivKonta = nazivKonta,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    SaldoDuguje = saldo > 0 ? saldo : 0,
                    SaldoPotrazuje = saldo < 0 ? -saldo : 0
                };
            })
            .Where(r => r.Saldo != 0m)
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }

    private const string VrstaNalogaPrenosPocetnogStanja = "PrenosPocetnogStanja";

    public async Task<bool> PostojiPrenosAsync(int novaGodina)
    {
        var pocetakGodine = new DateTime(novaGodina, 1, 1);
        return await _db.Nalozi.AnyAsync(n => n.VrstaNaloga == VrstaNalogaPrenosPocetnogStanja && n.DatumNaloga == pocetakGodine);
    }

    public async Task<Nalog> PrenesiUNovuGoduAsync(int izvornaGodina)
    {
        int novaGodina = izvornaGodina + 1;

        if (await PostojiPrenosAsync(novaGodina))
        {
            throw new InvalidOperationException($"Prenos početnog stanja za {novaGodina}. godinu je već izvršen.");
        }

        var saldoPoKontu = await GetZakljucniSaldoAsync(izvornaGodina);
        if (saldoPoKontu.Count == 0)
        {
            throw new InvalidOperationException($"Nema proknjiženih naloga sa nenultim saldom zaključno sa {izvornaGodina}. godinom — nema šta da se prenese.");
        }

        decimal ukupanSaldo = saldoPoKontu.Sum(r => r.Saldo);
        if (Math.Abs(ukupanSaldo) >= 0.01m)
        {
            throw new InvalidOperationException(
                $"Knjige nisu u ravnoteži zaključno sa {izvornaGodina}. godinom (razlika {ukupanSaldo:N2}) — " +
                "verovatno postoji neispravan proknjižen nalog. Ispravite ga pre prenosa u novu godinu.");
        }

        int sledeciBrojNaloga = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;
        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            DatumNaloga = new DateTime(novaGodina, 1, 1),
            VrstaNaloga = VrstaNalogaPrenosPocetnogStanja,
            Opis = $"Prenos početnog stanja iz {izvornaGodina}. godine",
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = DateTime.Now
        };

        var kontaMap = await _db.Konta.ToDictionaryAsync(k => k.BrojKonta, k => k, StringComparer.OrdinalIgnoreCase);

        int red = 1;
        foreach (var r in saldoPoKontu)
        {
            kontaMap.TryGetValue(r.BrojKonta, out var konto);
            if (konto == null) continue;

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = red++,
                KontoId = konto.KontoId,
                Opis = "Preneseno početno stanje",
                Duguje = r.Saldo > 0 ? r.Saldo : 0m,
                Potrazuje = r.Saldo < 0 ? -r.Saldo : 0m
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        return nalog;
    }
}
