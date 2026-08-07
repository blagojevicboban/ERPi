using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public enum BrutoBilansRedTip
{
    Detalj,
    SintetikaTotal,
    KlasaTotal
}

public class BrutoBilansRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal SaldoDuguje { get; set; }
    public decimal SaldoPotrazuje { get; set; }
    public BrutoBilansRedTip Tip { get; set; } = BrutoBilansRedTip.Detalj;

    public decimal Saldo => SaldoDuguje - SaldoPotrazuje;
}

public class ZakljucniListRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;

    public decimal PocetnoDuguje { get; set; }
    public decimal PocetnoPotrazuje { get; set; }

    public decimal PrometDuguje { get; set; }
    public decimal PrometPotrazuje { get; set; }

    public decimal UkupnoDuguje { get; set; }
    public decimal UkupnoPotrazuje { get; set; }

    public decimal SaldoDuguje { get; set; }
    public decimal SaldoPotrazuje { get; set; }

    public BrutoBilansRedTip Tip { get; set; } = BrutoBilansRedTip.Detalj;
}

public class BrutoBilansService
{
    private readonly ErpiDbContext _db;

    public BrutoBilansService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<BrutoBilansRed>> GetBrutoBilansAsync(
        DateTime? odDatuma = null, DateTime? doDatuma = null, int? klasa = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Konto)
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen);

        if (odDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query.ToListAsync();

        if (klasa.HasValue)
            stavke = stavke.Where(s => s.Konto != null && s.Konto.BrojKonta.Length > 0 && s.Konto.BrojKonta[0] - '0' == klasa.Value).ToList();

        return stavke
            .GroupBy(s => s.Konto?.BrojKonta ?? "000")
            .Select(g =>
            {
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                decimal saldo = duguje - potrazuje;
                return new BrutoBilansRed
                {
                    BrojKonta = g.Key,
                    NazivKonta = g.FirstOrDefault()?.Konto?.NazivKonta ?? g.Key,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    SaldoDuguje = saldo > 0 ? saldo : 0,
                    SaldoPotrazuje = saldo < 0 ? -saldo : 0
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }

    public async Task<List<BrutoBilansRed>> GetBrutoBilansSaTotalimaAsync(
        DateTime? odDatuma = null, DateTime? doDatuma = null, int? klasa = null)
    {
        var detalji = await GetBrutoBilansAsync(odDatuma, doDatuma, klasa);

        var rezultat = new List<BrutoBilansRed>();
        string? tekucaSintetika = null;
        string? tekucaKlasa = null;
        decimal sintDuguje = 0, sintPotrazuje = 0, sintSaldoDuguje = 0, sintSaldoPotrazuje = 0;
        decimal klasaDuguje = 0, klasaPotrazuje = 0, klasaSaldoDuguje = 0, klasaSaldoPotrazuje = 0;

        void ZatvoriSintetiku(string sintetika)
        {
            rezultat.Add(new BrutoBilansRed
            {
                NazivKonta = $"TOTAL sintetičkog konta {sintetika}",
                Duguje = sintDuguje,
                Potrazuje = sintPotrazuje,
                SaldoDuguje = sintSaldoDuguje,
                SaldoPotrazuje = sintSaldoPotrazuje,
                Tip = BrutoBilansRedTip.SintetikaTotal
            });
            sintDuguje = 0;
            sintPotrazuje = 0;
            sintSaldoDuguje = 0;
            sintSaldoPotrazuje = 0;
        }

        void ZatvoriKlasu(string klasaOznaka)
        {
            rezultat.Add(new BrutoBilansRed
            {
                NazivKonta = $"KLASA: {klasaOznaka}",
                Duguje = klasaDuguje,
                Potrazuje = klasaPotrazuje,
                SaldoDuguje = klasaSaldoDuguje,
                SaldoPotrazuje = klasaSaldoPotrazuje,
                Tip = BrutoBilansRedTip.KlasaTotal
            });
            klasaDuguje = 0;
            klasaPotrazuje = 0;
            klasaSaldoDuguje = 0;
            klasaSaldoPotrazuje = 0;
        }

        foreach (var red in detalji)
        {
            var sintetika = red.BrojKonta.Length >= 3 ? red.BrojKonta.Substring(0, 3) : red.BrojKonta;
            var klasaOznaka = red.BrojKonta.Length > 0 ? red.BrojKonta[0].ToString() : "";

            if (tekucaKlasa != null && klasaOznaka != tekucaKlasa)
            {
                ZatvoriSintetiku(tekucaSintetika!);
                ZatvoriKlasu(tekucaKlasa);
                tekucaSintetika = null;
            }
            else if (tekucaSintetika != null && sintetika != tekucaSintetika)
            {
                ZatvoriSintetiku(tekucaSintetika);
            }

            rezultat.Add(red);
            sintDuguje += red.Duguje;
            sintPotrazuje += red.Potrazuje;
            sintSaldoDuguje += red.SaldoDuguje;
            sintSaldoPotrazuje += red.SaldoPotrazuje;
            klasaDuguje += red.Duguje;
            klasaPotrazuje += red.Potrazuje;
            klasaSaldoDuguje += red.SaldoDuguje;
            klasaSaldoPotrazuje += red.SaldoPotrazuje;
            tekucaSintetika = sintetika;
            tekucaKlasa = klasaOznaka;
        }

        if (tekucaSintetika != null) ZatvoriSintetiku(tekucaSintetika);
        if (tekucaKlasa != null) ZatvoriKlasu(tekucaKlasa);

        return rezultat;
    }

    public async Task<List<ZakljucniListRed>> GetZakljucniListAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Konto)
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen);

        if (odDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value.Date);
        if (doDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value.Date.AddDays(1).AddTicks(-1));

        var stavke = await query.ToListAsync();

        var sintetikaMap = await _db.Konta
            .Where(k => k.IsSintetika)
            .ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        var analitikaGrupisano = stavke
            .GroupBy(s => s.Konto?.BrojKonta ?? "000")
            .Select(g =>
            {
                var analKonto = g.Key;
                var sintKonto = analKonto.Length >= 3 ? analKonto.Substring(0, 3) : analKonto;

                decimal pocDug = g.Where(s => IsPocetnoStanje(s)).Sum(s => s.Duguje);
                decimal pocPot = g.Where(s => IsPocetnoStanje(s)).Sum(s => s.Potrazuje);

                decimal promDug = g.Where(s => !IsPocetnoStanje(s)).Sum(s => s.Duguje);
                decimal promPot = g.Where(s => !IsPocetnoStanje(s)).Sum(s => s.Potrazuje);

                decimal ukDug = pocDug + promDug;
                decimal ukPot = pocPot + promPot;

                decimal razlika = ukDug - ukPot;
                decimal salDug = razlika > 0 ? razlika : 0m;
                decimal salPot = razlika < 0 ? -razlika : 0m;

                return new
                {
                    AnalitickiKonto = analKonto,
                    SintetickiKonto = sintKonto,
                    PocetnoDuguje = pocDug,
                    PocetnoPotrazuje = pocPot,
                    PrometDuguje = promDug,
                    PrometPotrazuje = promPot,
                    UkupnoDuguje = ukDug,
                    UkupnoPotrazuje = ukPot,
                    SaldoDuguje = salDug,
                    SaldoPotrazuje = salPot
                };
            })
            .ToList();

        var grupisano = analitikaGrupisano
            .GroupBy(a => a.SintetickiKonto)
            .Select(g =>
            {
                var sintKonto = g.Key;
                string naziv = sintetikaMap.TryGetValue(sintKonto, out var n)
                    ? n
                    : (g.Select(x => x.AnalitickiKonto).FirstOrDefault() ?? sintKonto);

                decimal pocDug = g.Sum(x => x.PocetnoDuguje);
                decimal pocPot = g.Sum(x => x.PocetnoPotrazuje);
                decimal promDug = g.Sum(x => x.PrometDuguje);
                decimal promPot = g.Sum(x => x.PrometPotrazuje);
                decimal ukDug = g.Sum(x => x.UkupnoDuguje);
                decimal ukPot = g.Sum(x => x.UkupnoPotrazuje);

                decimal sirovSalDug = g.Sum(x => x.SaldoDuguje);
                decimal sirovSalPot = g.Sum(x => x.SaldoPotrazuje);

                decimal salDug = 0m, salPot = 0m;
                if (sirovSalDug > sirovSalPot)
                {
                    salDug = sirovSalDug - sirovSalPot;
                }
                else
                {
                    salPot = sirovSalPot - sirovSalDug;
                }

                return new ZakljucniListRed
                {
                    BrojKonta = sintKonto,
                    NazivKonta = naziv,
                    PocetnoDuguje = pocDug,
                    PocetnoPotrazuje = pocPot,
                    PrometDuguje = promDug,
                    PrometPotrazuje = promPot,
                    UkupnoDuguje = ukDug,
                    UkupnoPotrazuje = ukPot,
                    SaldoDuguje = salDug,
                    SaldoPotrazuje = salPot,
                    Tip = BrutoBilansRedTip.Detalj
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();

        var rezultat = new List<ZakljucniListRed>();
        string? tekucaKlasa = null;

        decimal klasaPocDug = 0, klasaPocPot = 0;
        decimal klasaPromDug = 0, klasaPromPot = 0;
        decimal klasaUkDug = 0, klasaUkPot = 0;
        decimal klasaSalDug = 0, klasaSalPot = 0;

        void ZatvoriKlasu(string klasaOznaka)
        {
            rezultat.Add(new ZakljucniListRed
            {
                BrojKonta = "",
                NazivKonta = $"KLASA : {klasaOznaka}",
                PocetnoDuguje = klasaPocDug,
                PocetnoPotrazuje = klasaPocPot,
                PrometDuguje = klasaPromDug,
                PrometPotrazuje = klasaPromPot,
                UkupnoDuguje = klasaUkDug,
                UkupnoPotrazuje = klasaUkPot,
                SaldoDuguje = klasaSalDug,
                SaldoPotrazuje = klasaSalPot,
                Tip = BrutoBilansRedTip.KlasaTotal
            });

            klasaPocDug = klasaPocPot = 0;
            klasaPromDug = klasaPromPot = 0;
            klasaUkDug = klasaUkPot = 0;
            klasaSalDug = klasaSalPot = 0;
        }

        foreach (var red in grupisano)
        {
            var klasaOznaka = red.BrojKonta.Length > 0 ? red.BrojKonta[0].ToString() : "";
            if (tekucaKlasa != null && klasaOznaka != tekucaKlasa)
            {
                ZatvoriKlasu(tekucaKlasa);
            }

            rezultat.Add(red);
            klasaPocDug += red.PocetnoDuguje;
            klasaPocPot += red.PocetnoPotrazuje;
            klasaPromDug += red.PrometDuguje;
            klasaPromPot += red.PrometPotrazuje;
            klasaUkDug += red.UkupnoDuguje;
            klasaUkPot += red.UkupnoPotrazuje;
            klasaSalDug += red.SaldoDuguje;
            klasaSalPot += red.SaldoPotrazuje;
            tekucaKlasa = klasaOznaka;
        }

        if (tekucaKlasa != null) ZatvoriKlasu(tekucaKlasa);

        // Rekapitulacija po klasama na dnu (K L A S A : 0..7 i K L A S A : U)
        rezultat.Add(new ZakljucniListRed
        {
            BrojKonta = "",
            NazivKonta = "R E K A P I T U L A C I J A",
            Tip = BrutoBilansRedTip.SintetikaTotal
        });

        var klaseTotali = rezultat
            .Where(r => r.Tip == BrutoBilansRedTip.KlasaTotal)
            .ToList();

        decimal rekapUkPocDug = 0, rekapUkPocPot = 0;
        decimal rekapUkPromDug = 0, rekapUkPromPot = 0;
        decimal rekapUkUkDug = 0, rekapUkUkPot = 0;
        decimal rekapUkSalDug = 0, rekapUkSalPot = 0;

        foreach (var kt in klaseTotali)
        {
            var rKlasa = new ZakljucniListRed
            {
                BrojKonta = "",
                NazivKonta = kt.NazivKonta.Replace("KLASA :", "K L A S A : "),
                PocetnoDuguje = kt.PocetnoDuguje,
                PocetnoPotrazuje = kt.PocetnoPotrazuje,
                PrometDuguje = kt.PrometDuguje,
                PrometPotrazuje = kt.PrometPotrazuje,
                UkupnoDuguje = kt.UkupnoDuguje,
                UkupnoPotrazuje = kt.UkupnoPotrazuje,
                SaldoDuguje = kt.SaldoDuguje,
                SaldoPotrazuje = kt.SaldoPotrazuje,
                Tip = BrutoBilansRedTip.KlasaTotal
            };
            rezultat.Add(rKlasa);

            rekapUkPocDug += kt.PocetnoDuguje;
            rekapUkPocPot += kt.PocetnoPotrazuje;
            rekapUkPromDug += kt.PrometDuguje;
            rekapUkPromPot += kt.PrometPotrazuje;
            rekapUkUkDug += kt.UkupnoDuguje;
            rekapUkUkPot += kt.UkupnoPotrazuje;
            rekapUkSalDug += kt.SaldoDuguje;
            rekapUkSalPot += kt.SaldoPotrazuje;
        }

        rezultat.Add(new ZakljucniListRed
        {
            BrojKonta = "",
            NazivKonta = "K L A S A :  U",
            PocetnoDuguje = rekapUkPocDug,
            PocetnoPotrazuje = rekapUkPocPot,
            PrometDuguje = rekapUkPromDug,
            PrometPotrazuje = rekapUkPromPot,
            UkupnoDuguje = rekapUkUkDug,
            UkupnoPotrazuje = rekapUkUkPot,
            SaldoDuguje = rekapUkSalDug,
            SaldoPotrazuje = rekapUkSalPot,
            Tip = BrutoBilansRedTip.KlasaTotal
        });

        return rezultat;
    }

    private static bool IsPocetnoStanje(StavkaNaloga stavka)
    {
        if (stavka == null) return false;
        var nalog = stavka.Nalog;
        if (nalog == null) return false;
        if (nalog.BrojNaloga == 0) return true;
        if (!string.IsNullOrEmpty(nalog.VrstaNaloga) &&
            (nalog.VrstaNaloga.Equals("PrenosPocetnogStanja", StringComparison.OrdinalIgnoreCase) ||
             nalog.VrstaNaloga.Equals("PocetnoStanje", StringComparison.OrdinalIgnoreCase) ||
             nalog.VrstaNaloga.Equals("Početno stanje", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (!string.IsNullOrEmpty(nalog.Opis) &&
            (nalog.Opis.StartsWith("Pocetn", StringComparison.OrdinalIgnoreCase) ||
             nalog.Opis.StartsWith("Početn", StringComparison.OrdinalIgnoreCase) ||
             nalog.Opis.StartsWith("Prenos poč", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}
