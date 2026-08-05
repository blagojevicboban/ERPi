using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class KompenzacijaService
{
    private readonly ErpiDbContext _db;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;

    public KompenzacijaService(ErpiDbContext db)
    {
        _db = db;
        _zatvaranjeService = new ZatvaranjeStavkiService(_db);
    }

    public async Task<List<ObostranoDugovanjeCandidate>> GetObostranaDugovanjaAsync()
    {
        var partneri = await _db.Partneri.ToListAsync();
        var rezultat = new List<ObostranoDugovanjeCandidate>();

        foreach (var p in partneri)
        {
            var otvoreneStavke = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(p.PartnerId, DateTime.Today, samoOtvorene: true);

            decimal potrazivanjeKupac = otvoreneStavke
                .Where(s => s.Konto.StartsWith("2040") || s.Konto.StartsWith("204"))
                .Sum(s => s.Preostalo);

            decimal obavezaDobavljac = otvoreneStavke
                .Where(s => s.Konto.StartsWith("4350") || s.Konto.StartsWith("435"))
                .Sum(s => s.Preostalo);

            if (potrazivanjeKupac > 0 && obavezaDobavljac > 0)
            {
                rezultat.Add(new ObostranoDugovanjeCandidate
                {
                    PartnerId = p.PartnerId,
                    NazivPartnera = p.Naziv,
                    Pib = p.Pib ?? "",
                    PotrazivanjeKupac = potrazivanjeKupac,
                    ObavezaDobavljac = obavezaDobavljac
                });
            }
        }

        return rezultat.OrderByDescending(r => r.MaksimalnaKompenzacija).ToList();
    }

    public async Task<List<Kompenzacija>> GetKompenzacijeAsync()
    {
        return await _db.Kompenzacije
            .Include(k => k.Stavke)
            .OrderByDescending(k => k.Datum)
            .ThenByDescending(k => k.KompenzacijaId)
            .ToListAsync();
    }

    public async Task<Kompenzacija?> GetKompenzacijaByIdAsync(int id)
    {
        return await _db.Kompenzacije
            .Include(k => k.Stavke)
            .FirstOrDefaultAsync(k => k.KompenzacijaId == id);
    }

    public async Task<Kompenzacija> SacuvajKompenzacijuAsync(Kompenzacija kompenzacija)
    {
        decimal zbirPotrazivanja = kompenzacija.Stavke.Where(s => s.Strana == "Duguje").Sum(s => s.IznosZaKompenzaciju);
        decimal zbirObaveza = kompenzacija.Stavke.Where(s => s.Strana == "Potražuje").Sum(s => s.IznosZaKompenzaciju);
        kompenzacija.UkupanIznosKompenzacije = Math.Min(zbirPotrazivanja, zbirObaveza);

        if (kompenzacija.KompenzacijaId == 0)
        {
            if (string.IsNullOrWhiteSpace(kompenzacija.BrojDokumenta))
            {
                int sledeciBroj = await _db.Kompenzacije.CountAsync() + 1;
                kompenzacija.BrojDokumenta = $"KOM-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.Kompenzacije.Add(kompenzacija);
        }
        else
        {
            var postojeceStavke = _db.KompenzacijeStavke.Where(s => s.KompenzacijaId == kompenzacija.KompenzacijaId);
            _db.KompenzacijeStavke.RemoveRange(postojeceStavke);

            _db.Kompenzacije.Update(kompenzacija);
        }

        await _db.SaveChangesAsync();
        return kompenzacija;
    }

    public async Task<bool> ObrisiKompenzacijuAsync(int id)
    {
        var kompenzacija = await _db.Kompenzacije.FindAsync(id);
        if (kompenzacija == null || kompenzacija.IsKnjizeno) return false;

        _db.Kompenzacije.Remove(kompenzacija);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message, int? NalogId)> KnjiziIZatvoriKompenzacijuAsync(
        int kompenzacijaId, int korisnikId = 1, string korisnickoIme = "Administrator")
    {
        var kompenzacija = await GetKompenzacijaByIdAsync(kompenzacijaId);
        if (kompenzacija == null) return (false, "Kompenzacija ne postoji.", null);

        if (kompenzacija.IsKnjizeno)
        {
            return (false, "Kompenzacija je već proknjižena.", kompenzacija.NalogId);
        }

        if (kompenzacija.UkupanIznosKompenzacije <= 0)
        {
            return (false, "Iznos kompenzacije mora biti veći od 0.", null);
        }

        decimal zbirPotrazivanja = kompenzacija.Stavke.Where(s => s.Strana == "Duguje").Sum(s => s.IznosZaKompenzaciju);
        decimal zbirObaveza = kompenzacija.Stavke.Where(s => s.Strana == "Potražuje").Sum(s => s.IznosZaKompenzaciju);
        if (Math.Abs(zbirPotrazivanja - zbirObaveza) > 0.01m)
        {
            return (false, $"Zbir potraživanja ({zbirPotrazivanja:N2}) mora biti jednak zbiru obaveza ({zbirObaveza:N2}) uključenih u kompenzaciju.", null);
        }

        int sledeciBrojNaloga = await _db.Nalozi.CountAsync() + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            VrstaNaloga = "KOM",
            DatumNaloga = kompenzacija.Datum,
            Opis = $"Kompenzacija br. {kompenzacija.BrojDokumenta} ({kompenzacija.Vrsta})",
            Status = StatusNaloga.Proknjizen
        };

        var noveLinije = new Dictionary<(string Kljuc, string Strana), StavkaNaloga>();
        int rbr = 1;

        foreach (var grupa in kompenzacija.Stavke.GroupBy(s => (Kljuc: s.PartnerId > 0 ? $"P{s.PartnerId}" : $"K{s.BrojKonta}", s.Strana)))
        {
            decimal iznos = grupa.Sum(s => s.IznosZaKompenzaciju);
            if (iznos <= 0) continue;

            bool jeSinteticki = grupa.Key.Kljuc.StartsWith("K");
            int? partnerIdZaLiniju = jeSinteticki ? null : grupa.First().PartnerId;
            string brojKontaZaLiniju = jeSinteticki ? grupa.First().BrojKonta : (grupa.Key.Strana == "Duguje" ? "2040" : "4350");

            var kontoObj = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == brojKontaZaLiniju)
                           ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith(brojKontaZaLiniju.Substring(0, Math.Min(3, brojKontaZaLiniju.Length))))
                           ?? await _db.Konta.FirstAsync();

            StavkaNaloga linija = grupa.Key.Strana == "Duguje"
                ? new StavkaNaloga
                {
                    RedniBroj = rbr++,
                    KontoId = kontoObj.KontoId,
                    Opis = $"Kompenzacija potraživanja br. {kompenzacija.BrojDokumenta}",
                    Duguje = 0m,
                    Potrazuje = iznos,
                    BrojDokumenta = kompenzacija.BrojDokumenta,
                    DatumDokumenta = kompenzacija.Datum,
                    PartnerId = partnerIdZaLiniju
                }
                : new StavkaNaloga
                {
                    RedniBroj = rbr++,
                    KontoId = kontoObj.KontoId,
                    Opis = $"Kompenzacija obaveze br. {kompenzacija.BrojDokumenta}",
                    Duguje = iznos,
                    Potrazuje = 0m,
                    BrojDokumenta = kompenzacija.BrojDokumenta,
                    DatumDokumenta = kompenzacija.Datum,
                    PartnerId = partnerIdZaLiniju
                };

            nalog.Stavke.Add(linija);
            noveLinije[grupa.Key] = linija;
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        foreach (var st in kompenzacija.Stavke)
        {
            string kljuc = st.PartnerId > 0 ? $"P{st.PartnerId}" : $"K{st.BrojKonta}";
            if (st.StavkaNalogaId > 0 && st.IznosZaKompenzaciju > 0 &&
                noveLinije.TryGetValue((kljuc, st.Strana), out var novaLinija))
            {
                await _zatvaranjeService.ZatvoriAsync(
                    stavkaDugujeId: st.Strana == "Duguje" ? st.StavkaNalogaId : novaLinija.StavkaNalogaId,
                    stavkaPotrazujeId: st.Strana == "Duguje" ? novaLinija.StavkaNalogaId : st.StavkaNalogaId,
                    iznos: st.IznosZaKompenzaciju,
                    datum: kompenzacija.Datum,
                    vrstaZatvaranja: "Kompenzacija",
                    napomena: $"Automatsko zatvaranje po kompenzaciji br. {kompenzacija.BrojDokumenta}",
                    korisnikId: korisnikId
                );
            }
        }

        kompenzacija.IsKnjizeno = true;
        kompenzacija.Status = "Proknjiženo";
        kompenzacija.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno proknjižena kompenzacija br. {kompenzacija.BrojDokumenta} (Nalog KOM br. {sledeciBrojNaloga}) i zatvorene stavke u IOS-u!", nalog.NalogId);
    }
}
