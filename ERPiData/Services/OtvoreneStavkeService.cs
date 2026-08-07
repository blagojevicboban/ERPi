using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za Otvorene stavke (IOS) i kartice kupaca/dobavljača.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class OtvoreneStavkeService
{
    private readonly ErpiDbContext _db;

    public OtvoreneStavkeService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<Partner>> GetPartneriAsync(string? search = null)
    {
        var query = _db.Partneri.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.SifraPartnera.Contains(search) || p.Naziv.Contains(search));
        }
        var pravi = await query.ToListAsync();

        var konti = await _db.StavkeNaloga
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == null && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen &&
                s.Konto != null &&
                (s.Konto.BrojKonta.StartsWith("204") || s.Konto.BrojKonta.StartsWith("435") ||
                 s.Konto.BrojKonta.StartsWith("120") || s.Konto.BrojKonta.StartsWith("220")))
            .Select(s => s.Konto!.BrojKonta)
            .Distinct()
            .ToListAsync();

        if (konti.Count > 0)
        {
            var kontaMap = await _db.Konta
                .AsNoTracking()
                .ToDictionaryAsync(k => k.BrojKonta.Trim(), k => k.NazivKonta, StringComparer.OrdinalIgnoreCase);

            IEnumerable<Partner> sinteticki = konti.Select(k => new Partner
            {
                PartnerId = 0,
                SifraPartnera = k,
                Naziv = kontaMap.TryGetValue(k.Trim(), out var naziv) && !string.IsNullOrWhiteSpace(naziv) ? naziv : $"Konto {k}",
                KontoPartnera = k
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                sinteticki = sinteticki.Where(p =>
                    p.SifraPartnera.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Naziv.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            pravi.AddRange(sinteticki);
        }

        return pravi.OrderBy(p => p.Naziv).ToList();
    }

    public async Task<List<PartnerKontoInfo>> GetPartnerKontaAsync(int partnerId)
    {
        var grupe = await _db.StavkeNaloga
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen && s.Konto != null)
            .GroupBy(s => s.Konto!.BrojKonta)
            .Select(g => new { BrojKonta = g.Key, BrojStavki = g.Count() })
            .ToListAsync();

        var kontaMap = await _db.Konta
            .AsNoTracking()
            .ToDictionaryAsync(k => k.BrojKonta.Trim(), k => k.NazivKonta, StringComparer.OrdinalIgnoreCase);

        return grupe
            .Select(g => new PartnerKontoInfo
            {
                BrojKonta = g.BrojKonta,
                NazivKonta = kontaMap.TryGetValue(g.BrojKonta.Trim(), out var naziv) ? naziv : null,
                BrojStavki = g.BrojStavki
            })
            .OrderByDescending(k => k.BrojStavki)
            .ThenBy(k => k.BrojKonta)
            .ToList();
    }

    public async Task<List<KarticaRed>> GetOtvoreneStavkeAsync(int partnerId, string? brojKontaPrefix = null)
    {
        var upit = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen);

        if (!string.IsNullOrWhiteSpace(brojKontaPrefix))
        {
            upit = upit.Where(s => s.Konto != null && s.Konto.BrojKonta.StartsWith(brojKontaPrefix));
        }

        var stavke = await upit
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        return IzgradiKarticu(stavke);
    }

    public async Task<List<KarticaRed>> GetOtvoreneStavkeZaKontoAsync(string brojKonta)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Konto)
            .Where(s => s.PartnerId == null && s.Konto != null && s.Konto.BrojKonta == brojKonta && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        return IzgradiKarticu(stavke);
    }

    private static List<KarticaRed> IzgradiKarticu(List<StavkaNaloga> stavke)
    {
        var rezultat = new List<KarticaRed>();
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            saldo += s.Duguje - s.Potrazuje;
            rezultat.Add(new KarticaRed
            {
                StavkaNalogaId = s.StavkaNalogaId,
                RedniBroj = s.RedniBroj,
                NalogId = s.Nalog!.NalogId,
                Datum = s.Nalog.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Bruto bilans analitike — promet i saldo po partneru (umesto po kontu), iz proknjiženih
    /// naloga sa dodeljenim partnerom. "Drill-down" u analitiku iza svakog sintetičkog totala u
    /// <c>BrutoBilansView</c>. <paramref name="odDatuma"/>/<paramref name="doDatuma"/> je
    /// dopuna u odnosu na ERPiFinansije-in original (koji je period ignorisao) — poštuje isti
    /// period koji je primenjen na finansijski bruto bilans.
    /// </summary>
    public async Task<List<BrutoBilansAnalitikeRed>> GetBrutoBilansAnalitikeAsync(
        DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.PartnerId != null && s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen);

        if (odDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query.ToListAsync();

        return stavke
            .GroupBy(s => s.PartnerId!.Value)
            .Select(g =>
            {
                var partner = g.First().Partner;
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansAnalitikeRed
                {
                    SifraPartnera = partner?.SifraPartnera ?? "?",
                    NazivPartnera = partner?.Naziv ?? "?",
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .OrderBy(r => r.NazivPartnera)
            .ToList();
    }

    public async Task<List<IosPartnerGrupa>> GetIosIzvestajAsync(
        string? odKonta = null,
        string? doKonta = null,
        DateTime? odDatuma = null,
        DateTime? doDatuma = null,
        bool samoSaSaldom = true,
        bool koristiZatvaranje = false)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Include(s => s.Konto)
            .Where(s => s.Nalog != null && s.Nalog.Status == StatusNaloga.Proknjizen);

        if (odDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);

        if (doDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        string odK = (odKonta ?? "").Trim();
        string doK = (doKonta ?? "").Trim();

        if (!string.IsNullOrEmpty(odK) || !string.IsNullOrEmpty(doK))
        {
            stavke = stavke.Where(s =>
            {
                string k = s.Konto?.BrojKonta.Trim() ?? "";
                if (string.IsNullOrEmpty(k)) return false;

                if (!string.IsNullOrEmpty(odK) && string.IsNullOrEmpty(doK))
                {
                    return k.StartsWith(odK, StringComparison.OrdinalIgnoreCase) ||
                           string.Compare(k, odK, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (string.IsNullOrEmpty(odK) && !string.IsNullOrEmpty(doK))
                {
                    return k.StartsWith(doK, StringComparison.OrdinalIgnoreCase) ||
                           string.Compare(k, doK, StringComparison.OrdinalIgnoreCase) <= 0;
                }

                if (string.Equals(odK, doK, StringComparison.OrdinalIgnoreCase))
                {
                    return k.StartsWith(odK, StringComparison.OrdinalIgnoreCase);
                }

                bool okOd = k.StartsWith(odK, StringComparison.OrdinalIgnoreCase) || string.Compare(k, odK, StringComparison.OrdinalIgnoreCase) >= 0;
                bool okDo = k.StartsWith(doK, StringComparison.OrdinalIgnoreCase) || string.Compare(k, doK, StringComparison.OrdinalIgnoreCase) <= 0;

                return okOd && okDo;
            }).ToList();
        }

        var kontaMap = await _db.Konta
            .AsNoTracking()
            .ToDictionaryAsync(k => k.BrojKonta.Trim(), k => k.NazivKonta, StringComparer.OrdinalIgnoreCase);

        Dictionary<int, decimal> zatvorenoPoDuguje = new();
        Dictionary<int, decimal> zatvorenoPoPotrazuje = new();
        if (koristiZatvaranje)
        {
            var stavkaIds = stavke.Select(s => s.StavkaNalogaId).ToList();
            var zatvaranja = await _db.ZatvaranjaStavki
                .Where(z => stavkaIds.Contains(z.StavkaDugujeId) || stavkaIds.Contains(z.StavkaPotrazujeId))
                .ToListAsync();
            zatvorenoPoDuguje = zatvaranja.GroupBy(z => z.StavkaDugujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));
            zatvorenoPoPotrazuje = zatvaranja.GroupBy(z => z.StavkaPotrazujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));
        }

        var grupeDict = new Dictionary<string, IosPartnerGrupa>();

        foreach (var s in stavke)
        {
            string brojKonta = s.Konto?.BrojKonta ?? "";
            string key = s.PartnerId.HasValue
                ? $"P_{s.PartnerId.Value}_{brojKonta}"
                : $"K_{brojKonta}";

            if (!grupeDict.TryGetValue(key, out var grupa))
            {
                string nazivVal = s.Partner != null && !string.IsNullOrWhiteSpace(s.Partner.Naziv)
                    ? s.Partner.Naziv
                    : (kontaMap.TryGetValue(brojKonta.Trim(), out var kNaziv) && !string.IsNullOrWhiteSpace(kNaziv)
                        ? kNaziv
                        : $"Konto {brojKonta}");

                var partnerObj = s.Partner ?? new Partner
                {
                    PartnerId = s.PartnerId ?? 0,
                    SifraPartnera = string.IsNullOrWhiteSpace(brojKonta) ? "---" : brojKonta,
                    Naziv = nazivVal,
                    KontoPartnera = brojKonta
                };

                grupa = new IosPartnerGrupa
                {
                    SifraPartnera = partnerObj.SifraPartnera,
                    NazivPartnera = nazivVal,
                    Konto = brojKonta,
                    Adresa = partnerObj.Adresa,
                    PttIMesto = partnerObj.PttIMesto,
                    Pib = partnerObj.Pib,
                    Partner = partnerObj,
                    Stavke = new List<KarticaRed>()
                };

                grupeDict[key] = grupa;
            }

            decimal prethodniSaldo = grupa.Stavke.Count > 0 ? grupa.Stavke[^1].Saldo : 0m;
            decimal noviSaldo = prethodniSaldo + s.Duguje - s.Potrazuje;

            decimal? preostalo = null;
            string? statusZatvaranja = null;
            int? danaKasnjenja = null;

            if (koristiZatvaranje)
            {
                if (s.Duguje > 0)
                {
                    decimal zatvoreno = zatvorenoPoDuguje.TryGetValue(s.StavkaNalogaId, out var z1) ? z1 : 0m;
                    (preostalo, statusZatvaranja) = ZatvaranjeStavkiService.IzracunajPreostaloIStatus(s.Duguje, zatvoreno);
                }
                else if (s.Potrazuje > 0)
                {
                    decimal zatvoreno = zatvorenoPoPotrazuje.TryGetValue(s.StavkaNalogaId, out var z2) ? z2 : 0m;
                    (preostalo, statusZatvaranja) = ZatvaranjeStavkiService.IzracunajPreostaloIStatus(s.Potrazuje, zatvoreno);
                }

                if (s.ValutaDospela.HasValue && preostalo.HasValue && preostalo.Value > 0.01m)
                {
                    danaKasnjenja = Math.Max(0, (DateTime.Now.Date - s.ValutaDospela.Value.Date).Days);
                }
            }

            grupa.Stavke.Add(new KarticaRed
            {
                StavkaNalogaId = s.StavkaNalogaId,
                RedniBroj = s.RedniBroj,
                NalogId = s.Nalog!.NalogId,
                Datum = s.Nalog.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
                OpisPromene = s.BrojDokumenta,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = noviSaldo,
                Preostalo = preostalo,
                StatusZatvaranja = statusZatvaranja,
                ValutaDospela = s.ValutaDospela,
                DanaKasnjenja = danaKasnjenja
            });
        }

        var rezultat = grupeDict.Values.ToList();

        if (samoSaSaldom)
        {
            rezultat = rezultat.Where(g => g.Saldo != 0m || (g.Stavke.Count > 0 && g.Stavke.Any(st => st.Saldo != 0m))).ToList();
        }

        return rezultat
            .OrderBy(g => g.Konto)
            .ThenBy(g => g.NazivPartnera)
            .ToList();
    }
}

public class IosPartnerGrupa : INotifyPropertyChanged
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public string SifraPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public string? Adresa { get; set; }
    public string? PttIMesto { get; set; }
    public string? Pib { get; set; }
    public Partner Partner { get; set; } = null!;
    public List<KarticaRed> Stavke { get; set; } = new();
    public decimal UkupnoDuguje => Stavke.Sum(s => s.Duguje);
    public decimal UkupnoPotrazuje => Stavke.Sum(s => s.Potrazuje);
    public decimal Saldo => Stavke.Count > 0 ? Stavke[^1].Saldo : 0m;
    public int BrojStavki => Stavke.Count;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class BrutoBilansAnalitikeRed
{
    public string SifraPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }
}

public class PartnerKontoInfo
{
    public string BrojKonta { get; set; } = string.Empty;
    public string? NazivKonta { get; set; }
    public int BrojStavki { get; set; }
    public string Prikaz => string.IsNullOrWhiteSpace(NazivKonta) ? BrojKonta : $"{BrojKonta} — {NazivKonta}";
}
