using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class DeviznoKnjigovodstvoResult
{
    public string BrojKonta { get; set; } = string.Empty;
    public string Valuta { get; set; } = "EUR";
    public decimal DevizniSaldo { get; set; }
    public decimal KnjigovodstveniSaldoRsd { get; set; }
    public decimal TekuciKurs { get; set; }
    public decimal ValviraniSaldoRsd { get; set; }
    public decimal KursnaRazlikaRsd { get; set; }
}

public class DeviznoKnjigovodstvoService
{
    private readonly ErpiDbContext _db;

    public DeviznoKnjigovodstvoService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<DeviznoKnjigovodstvoResult>> ObracunajValviranjeAsync(DateTime naDan, decimal tekuciKursEur = 117.20m, decimal tekuciKursUsd = 108.50m)
    {
        var rezultati = new List<DeviznoKnjigovodstvoResult>();

        var stavke = await _db.StavkeNaloga
            .Include(s => s.Konto)
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen && s.Nalog.DatumNaloga <= naDan)
            .Where(s => (s.Konto != null && (s.Konto.BrojKonta.StartsWith("204") || s.Konto.BrojKonta.StartsWith("435") || s.Konto.BrojKonta.StartsWith("244"))) || s.Valuta != "RSD")
            .ToListAsync();

        var grupisano = stavke.GroupBy(s => new { BrojKonta = s.Konto?.BrojKonta ?? "2040", Valuta = string.IsNullOrEmpty(s.Valuta) ? "EUR" : s.Valuta });

        foreach (var g in grupisano)
        {
            decimal devDuguje = g.Sum(s => s.DevizniDuguje);
            decimal devPotrazuje = g.Sum(s => s.DevizniPotrazuje);
            decimal devSaldo = devDuguje - devPotrazuje;

            decimal rsdDuguje = g.Sum(s => s.Duguje);
            decimal rsdPotrazuje = g.Sum(s => s.Potrazuje);
            decimal rsdSaldo = rsdDuguje - rsdPotrazuje;

            if (devSaldo == 0 && rsdSaldo == 0) continue;

            decimal kurs = g.Key.Valuta.ToUpper() == "USD" ? tekuciKursUsd : tekuciKursEur;
            decimal valviraniRsd = Math.Round(devSaldo * kurs, 2);
            decimal razlika = valviraniRsd - rsdSaldo;

            rezultati.Add(new DeviznoKnjigovodstvoResult
            {
                BrojKonta = g.Key.BrojKonta,
                Valuta = g.Key.Valuta,
                DevizniSaldo = devSaldo,
                KnjigovodstveniSaldoRsd = rsdSaldo,
                TekuciKurs = kurs,
                ValviraniSaldoRsd = valviraniRsd,
                KursnaRazlikaRsd = razlika
            });
        }

        return rezultati;
    }

    public async Task<(bool Success, string Message, Nalog? Nalog)> ProknjiziValviranjeAsync(DateTime naDan, List<DeviznoKnjigovodstvoResult> stavkeValviranja)
    {
        if (stavkeValviranja == null || !stavkeValviranja.Any(s => s.KursnaRazlikaRsd != 0))
            return (false, "Nema kursnih razlika za knjiženje.", null);

        try
        {
            int sledeciBroj = (await _db.Nalozi.MaxAsync(n => (int?)n.BrojNaloga) ?? 0) + 1;

            var nalog = new Nalog
            {
                BrojNaloga = sledeciBroj,
                DatumNaloga = naDan,
                Opis = $"Automatsko valviranje deviznih konta na dan {naDan:dd.MM.yyyy}",
                Status = StatusNaloga.Proknjizen,
                VrstaNaloga = "VAL"
            };

            _db.Nalozi.Add(nalog);
            await _db.SaveChangesAsync();

            var konto6630 = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "6630" || k.BrojKonta == "663") ?? await _db.Konta.FirstAsync();
            var konto5630 = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "5630" || k.BrojKonta == "563") ?? await _db.Konta.FirstAsync();

            int rbr = 1;
            foreach (var st in stavkeValviranja.Where(s => s.KursnaRazlikaRsd != 0))
            {
                var targetKonto = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == st.BrojKonta) ?? await _db.Konta.FirstAsync();

                if (st.KursnaRazlikaRsd > 0)
                {
                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        KontoId = targetKonto.KontoId,
                        Opis = $"Pozitivna kursna razlika ({st.Valuta}) na dan {naDan:dd.MM.yyyy}",
                        Duguje = st.KursnaRazlikaRsd,
                        Potrazuje = 0m,
                        Valuta = st.Valuta,
                        KursValute = st.TekuciKurs
                    });

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        KontoId = konto6630.KontoId,
                        Opis = $"Pozitivna kursna razlika ({st.Valuta}) konto {st.BrojKonta}",
                        Duguje = 0m,
                        Potrazuje = st.KursnaRazlikaRsd
                    });
                }
                else
                {
                    decimal absRazlika = Math.Abs(st.KursnaRazlikaRsd);

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        KontoId = konto5630.KontoId,
                        Opis = $"Negativna kursna razlika ({st.Valuta}) konto {st.BrojKonta}",
                        Duguje = absRazlika,
                        Potrazuje = 0m
                    });

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        KontoId = targetKonto.KontoId,
                        Opis = $"Negativna kursna razlika ({st.Valuta}) na dan {naDan:dd.MM.yyyy}",
                        Duguje = 0m,
                        Potrazuje = absRazlika,
                        Valuta = st.Valuta,
                        KursValute = st.TekuciKurs
                    });
                }
            }

            await _db.SaveChangesAsync();
            return (true, $"Nalog valviranja #{nalog.BrojNaloga} je uspešno sačuvan i proknjižen.", nalog);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri knjiženju valviranja: {ex.Message}", null);
        }
    }
}
