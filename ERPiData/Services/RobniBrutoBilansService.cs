using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class RobniBrutoBilansRed
{
    public string SifraMagacina { get; set; } = string.Empty;
    public string NazivMagacina { get; set; } = string.Empty;

    public string SifraArtikla { get; set; } = string.Empty;
    public string NazivArtikla { get; set; } = string.Empty;
    public string? Pakovanje { get; set; }
    public string JedinicaMere { get; set; } = "kom";
    public decimal Cena { get; set; }

    public decimal PocetnoStanjeKolicina { get; set; }
    public decimal PocetnoStanjeVrednost { get; set; }

    public decimal UlazKolicina { get; set; }
    public decimal UlazVrednost { get; set; }

    public decimal IzlazKolicina { get; set; }
    public decimal IzlazVrednost { get; set; }

    public decimal SaldoKolicinski { get; set; }
    public decimal SaldoVrednosni { get; set; }
}

public class RobniBrutoBilansService
{
    private readonly ErpiDbContext _db;

    public RobniBrutoBilansService(ErpiDbContext db)
    {
        _db = db;
    }

    public Task<List<RobniBrutoBilansRed>> GetRobniBrutoBilansAsync(int? magacinId = null, DateTime? doDatuma = null, string? pretraga = null)
        => IzracunajAsync(magacinId, doDatuma, pretraga, samoRoba: true);

    public Task<List<RobniBrutoBilansRed>> GetMaterijalniBrutoBilansAsync(int? magacinId = null, DateTime? doDatuma = null, string? pretraga = null)
        => IzracunajAsync(magacinId, doDatuma, pretraga, samoRoba: false);

    private async Task<List<RobniBrutoBilansRed>> IzracunajAsync(int? magacinId, DateTime? doDatuma, string? pretraga, bool samoRoba)
    {
        string? trazeniMagacinSifra = null;
        if (magacinId.HasValue && magacinId.Value > 0)
        {
            var mag = await _db.Magacini.FirstOrDefaultAsync(m => m.MagacinId == magacinId.Value);
            trazeniMagacinSifra = mag?.SifraMagacina;
        }

        var query = _db.MaterijalneKartice.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trazeniMagacinSifra))
        {
            query = query.Where(k => k.SifraMagacina == trazeniMagacinSifra);
        }

        if (doDatuma.HasValue)
        {
            DateTime krajDana = doDatuma.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(k => k.DatumPromene <= krajDana);
        }

        var karticeList = await query.ToListAsync();

        var magaciniMap = await _db.Magacini.ToDictionaryAsync(m => m.SifraMagacina, m => m.NazivMagacina, StringComparer.OrdinalIgnoreCase);
        var robaMap = await _db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
        var materijalMap = samoRoba
            ? new Dictionary<string, Materijal>(StringComparer.OrdinalIgnoreCase)
            : await _db.Materijali.ToDictionaryAsync(m => m.SifraArtikla, m => m, StringComparer.OrdinalIgnoreCase);

        var rezultat = karticeList
            .GroupBy(k => new { k.SifraMagacina, k.SifraArtikla })
            .Where(g => robaMap.ContainsKey(g.Key.SifraArtikla) == samoRoba)
            .Select(g =>
            {
                materijalMap.TryGetValue(g.Key.SifraArtikla, out var mat);
                robaMap.TryGetValue(g.Key.SifraArtikla, out var rob);
                magaciniMap.TryGetValue(g.Key.SifraMagacina, out string? nazivMag);

                var last = g.OrderBy(k => k.DatumPromene).ThenBy(k => k.MaterijalnaKarticaId).LastOrDefault();

                decimal ukUlazKol = g.Sum(k => k.Ulaz);
                decimal ukUlazVred = g.Sum(k => k.Duguje);
                decimal ukIzlazKol = g.Sum(k => k.Izlaz);
                decimal ukIzlazVred = g.Sum(k => k.Potrazuje);

                decimal zadnjeStanjeKol = (last != null && last.Stanje != 0m) ? last.Stanje : (ukUlazKol - ukIzlazKol);
                decimal zadnjiSaldoVred = (last != null && last.Saldo != 0m) ? last.Saldo : (ukUlazVred - ukIzlazVred);
                decimal zadnjaCena = (last != null && last.Cena != 0m) ? last.Cena : (rob?.ProdajnaCena ?? 0m);

                if (zadnjiSaldoVred == 0m && zadnjeStanjeKol != 0m)
                {
                    zadnjiSaldoVred = zadnjeStanjeKol * zadnjaCena;
                }

                return new RobniBrutoBilansRed
                {
                    SifraMagacina = g.Key.SifraMagacina,
                    NazivMagacina = nazivMag ?? g.Key.SifraMagacina,
                    SifraArtikla = g.Key.SifraArtikla,
                    NazivArtikla = mat?.Naziv ?? rob?.Naziv ?? g.Key.SifraArtikla,
                    Pakovanje = rob?.Pakovanje,
                    JedinicaMere = mat?.JedinicaMere ?? rob?.JedinicaMere ?? "kom",
                    Cena = zadnjaCena,

                    UlazKolicina = ukUlazKol,
                    UlazVrednost = ukUlazVred,

                    IzlazKolicina = ukIzlazKol,
                    IzlazVrednost = ukIzlazVred,

                    SaldoKolicinski = zadnjeStanjeKol,
                    SaldoVrednosni = zadnjiSaldoVred
                };
            })
            .OrderBy(r => r.SifraMagacina)
            .ThenBy(r => r.SifraArtikla)
            .ToList();

        if (!string.IsNullOrWhiteSpace(pretraga))
        {
            string s = pretraga.ToLower();
            rezultat = rezultat.Where(r =>
                r.SifraArtikla.ToLower().Contains(s) ||
                r.NazivArtikla.ToLower().Contains(s) ||
                r.SifraMagacina.ToLower().Contains(s) ||
                r.NazivMagacina.ToLower().Contains(s)).ToList();
        }

        return rezultat;
    }
}
