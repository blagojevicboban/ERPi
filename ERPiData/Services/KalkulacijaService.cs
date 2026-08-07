using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za Ulazne / Veleprodajne kalkulacije nabavke robe.
/// Obračunava zavisne troškove nabavke (transport, uskladištenje, utovar/istovar, osiguranje, ostalo),
/// vrši srazmernu alokaciju po stavkama artikala i generiše naloge za knjiženje u Glavnu knjigu.
/// </summary>
public class KalkulacijaService
{
    private readonly ErpiDbContext _db;

    public KalkulacijaService(ErpiDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obračun kalkulacije bez stavki (na nivou dokumenata).
    /// </summary>
    public static void Izracunaj(Kalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;
        k.SvegaNabavno = k.NabavnaVrednost + k.SvegaTroskovi;
        k.Razlika = Math.Round(k.SvegaNabavno * k.MarzaProcenat / 100m, 2);
        k.Porez = Math.Round((k.SvegaNabavno + k.Razlika) * k.PoreskaStopaProcenat / 100m, 2);
        k.ProdajnaVrednost = k.SvegaNabavno + k.Razlika + k.Porez;

        k.UkupnoNabavna = k.SvegaNabavno;
        k.UkupnoProdajna = k.ProdajnaVrednost;
        k.UkupnoPdv = k.Porez;
    }

    /// <summary>
    /// Obračun kalkulacije sa stavkama. Zavisni troškovi se raspoređuju srazmerno po učešću svake stavke
    /// u ukupnoj fakturnoj vrednosti (Iznos), sa korekcijom zaokruživanja na poslednjoj stavci.
    /// </summary>
    public static void IzracunajSaStavkama(Kalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;

        int rb = 1;
        foreach (var s in k.Stavke)
        {
            s.RedniBroj = rb++;
            s.Iznos = Math.Round(s.Kolicina * s.NabavnaCena, 2);
        }
        decimal svegaIznos = k.Stavke.Sum(s => s.Iznos);

        decimal raspodeljenoTroskova = 0;
        for (int i = 0; i < k.Stavke.Count; i++)
        {
            var s = k.Stavke[i];
            bool poslednja = i == k.Stavke.Count - 1;

            s.Troskovi = poslednja
                ? k.SvegaTroskovi - raspodeljenoTroskova
                : (svegaIznos != 0 ? Math.Round(k.SvegaTroskovi * s.Iznos / svegaIznos, 2) : 0m);
            if (!poslednja) raspodeljenoTroskova += s.Troskovi;

            s.NabavnaVrednost = s.Iznos + s.Troskovi;
            s.RazlikaProcenat = k.MarzaProcenat;
            s.RazlikaIznos = Math.Round(s.NabavnaVrednost * k.MarzaProcenat / 100m, 2);
            decimal prodajnaBezPoreza = s.NabavnaVrednost + s.RazlikaIznos;
            s.ProdajnaVrednostBezPoreza = prodajnaBezPoreza;
            s.PorezProcenat = k.PoreskaStopaProcenat;
            s.PorezIznos = Math.Round(prodajnaBezPoreza * k.PoreskaStopaProcenat / 100m, 2);
            s.ProdajnaVrednost = prodajnaBezPoreza + s.PorezIznos;
            s.ProdajnaCena = s.Kolicina != 0 ? Math.Round(s.ProdajnaVrednost / s.Kolicina, 4) : 0m;

            s.IznosNabavni = s.NabavnaVrednost;
            s.IznosProdajni = s.ProdajnaVrednost;
            s.IznosPdv = s.PorezIznos;
        }

        k.NabavnaVrednost = svegaIznos;
        k.SvegaNabavno = k.Stavke.Sum(s => s.NabavnaVrednost);
        k.Razlika = k.Stavke.Sum(s => s.RazlikaIznos);
        k.Porez = k.Stavke.Sum(s => s.PorezIznos);
        k.ProdajnaVrednost = k.Stavke.Sum(s => s.ProdajnaVrednost);

        k.UkupnoNabavna = k.SvegaNabavno;
        k.UkupnoProdajna = k.ProdajnaVrednost;
        k.UkupnoPdv = k.Porez;
    }

    public async Task<List<Kalkulacija>> GetKalkulacijeAsync(int? magacinId = null, string? search = null)
    {
        var query = _db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Partner)
            .Include(k => k.KontoDobavljaca)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (magacinId.HasValue && magacinId.Value > 0)
        {
            query = query.Where(k => k.MagacinId == magacinId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(k => k.BrojKalkulacije.ToString().Contains(search)
                || (k.BrojFaktureDobavljaca != null && k.BrojFaktureDobavljaca.ToLower().Contains(search))
                || (k.BrojRacuna != null && k.BrojRacuna.ToLower().Contains(search))
                || (k.Partner != null && k.Partner.Naziv.ToLower().Contains(search)));
        }

        return await query.OrderByDescending(k => k.Datum)
            .ThenByDescending(k => k.BrojKalkulacije)
            .ToListAsync();
    }

    public async Task<Kalkulacija?> GetKalkulacijaByIdAsync(int kalkulacijaId)
    {
        return await _db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Partner)
            .Include(k => k.KontoDobavljaca)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);
    }

    public async Task<Kalkulacija> SaveKalkulacijuAsync(Kalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count > 0)
        {
            IzracunajSaStavkama(kalkulacija);
        }
        else
        {
            Izracunaj(kalkulacija);
        }

        if (kalkulacija.KalkulacijaId == 0)
        {
            if (kalkulacija.BrojKalkulacije == 0)
            {
                kalkulacija.BrojKalkulacije = (await _db.Kalkulacije.MaxAsync(k => (int?)k.BrojKalkulacije) ?? 0) + 1;
            }
            _db.Kalkulacije.Add(kalkulacija);
        }
        else
        {
            var existing = await _db.Kalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacija.KalkulacijaId);

            if (existing == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
            if (existing.IsKnjizen) throw new InvalidOperationException("Proknjižena kalkulacija se ne može menjati.");

            existing.BrojKalkulacije = kalkulacija.BrojKalkulacije;
            existing.Datum = kalkulacija.Datum;
            existing.MagacinId = kalkulacija.MagacinId;
            existing.PartnerId = kalkulacija.PartnerId;
            existing.KontoDobavljacaId = kalkulacija.KontoDobavljacaId;
            existing.BrojOtpremnice = kalkulacija.BrojOtpremnice;
            existing.DatumOtpremnice = kalkulacija.DatumOtpremnice;
            existing.BrojRacuna = kalkulacija.BrojRacuna;
            existing.DatumRacuna = kalkulacija.DatumRacuna;
            existing.BrojFaktureDobavljaca = kalkulacija.BrojFaktureDobavljaca ?? kalkulacija.BrojRacuna;
            existing.DatumFakture = kalkulacija.DatumFakture ?? kalkulacija.DatumRacuna;
            existing.NabavnaVrednost = kalkulacija.NabavnaVrednost;
            existing.TransportniTroskovi = kalkulacija.TransportniTroskovi;
            existing.TroskoviUskladistenja = kalkulacija.TroskoviUskladistenja;
            existing.UtovarIstovar = kalkulacija.UtovarIstovar;
            existing.TransportnoOsiguranje = kalkulacija.TransportnoOsiguranje;
            existing.OstaliTroskovi = kalkulacija.OstaliTroskovi;
            existing.SvegaTroskovi = kalkulacija.SvegaTroskovi;
            existing.SvegaNabavno = kalkulacija.SvegaNabavno;
            existing.Razlika = kalkulacija.Razlika;
            existing.MarzaProcenat = kalkulacija.MarzaProcenat;
            existing.Porez = kalkulacija.Porez;
            existing.PoreskaStopaProcenat = kalkulacija.PoreskaStopaProcenat;
            existing.ProdajnaVrednost = kalkulacija.ProdajnaVrednost;
            existing.Napomena = kalkulacija.Napomena;

            _db.StavkeKalkulacije.RemoveRange(existing.Stavke);
            existing.Stavke = kalkulacija.Stavke;
            kalkulacija = existing;
        }

        await _db.SaveChangesAsync();
        return kalkulacija;
    }

    public async Task ObrisiKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije
            .Include(k => k.Stavke)
            .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);

        if (kalkulacija == null) return;
        if (kalkulacija.IsKnjizen)
            throw new InvalidOperationException($"Proknjižena kalkulacija #{kalkulacija.BrojKalkulacije} se ne može obrisati.");

        _db.StavkeKalkulacije.RemoveRange(kalkulacija.Stavke);
        _db.Kalkulacije.Remove(kalkulacija);
        await _db.SaveChangesAsync();
    }

    public async Task KnjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Partner)
            .Include(k => k.KontoDobavljaca)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);

        if (kalkulacija == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
        if (kalkulacija.IsKnjizen) throw new InvalidOperationException($"Kalkulacija #{kalkulacija.BrojKalkulacije} je već proknjižena.");

        if (kalkulacija.Stavke.Count > 0)
        {
            if (kalkulacija.Magacin == null)
                throw new InvalidOperationException($"Kalkulacija #{kalkulacija.BrojKalkulacije} nema izabran magacin za knjiženje zaliha.");

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke)
            {
                if (s.Artikal != null)
                {
                    await kartice.DodajUlazRedAsync(kalkulacija.Magacin.SifraMagacina, s.Artikal.SifraArtikla, kalkulacija.Datum,
                        $"Kalkulacija {kalkulacija.BrojKalkulacije}", s.Kolicina, s.ProdajnaCena);
                }
            }
        }

        await KnjiziUGlavnuKnjiguAsync(kalkulacija);

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    private async Task KnjiziUGlavnuKnjiguAsync(Kalkulacija kalkulacija)
    {
        decimal svegaNabavno = kalkulacija.SvegaNabavno;
        decimal razlika = kalkulacija.Razlika;
        decimal prodajnaBezPoreza = svegaNabavno + razlika;

        if (prodajnaBezPoreza == 0) return;

        string opis = $"Kalkulacija veleprodaje #{kalkulacija.BrojKalkulacije}";
        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBroj,
            DatumNaloga = kalkulacija.Datum,
            Opis = opis,
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = DateTime.Now,
            VrstaNaloga = "KALKULACIJA"
        };

        int rb = 1;
        // Roba na zalihama (1320)
        var robniKonto = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == RobnaKonta.RobaVeleprodaja)
            ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("132"));
        if (robniKonto != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = robniKonto.KontoId,
                Opis = opis,
                BrojDokumenta = kalkulacija.BrojRacuna ?? kalkulacija.BrojFaktureDobavljaca,
                Duguje = prodajnaBezPoreza,
                Potrazuje = 0m
            });
        }

        // Razlika u ceni (1329)
        if (razlika != 0)
        {
            var marzaKonto = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == RobnaKonta.RazlikaUCeniVeleprodaja)
                ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("1329"));
            if (marzaKonto != null)
            {
                nalog.Stavke.Add(new StavkaNaloga
                {
                    RedniBroj = rb++,
                    KontoId = marzaKonto.KontoId,
                    Opis = opis,
                    Duguje = 0m,
                    Potrazuje = razlika
                });
            }
        }

        // Dobavljač (4350 / KontoDobavljaca)
        int? dobavljacKontoId = kalkulacija.KontoDobavljacaId;
        if (!dobavljacKontoId.HasValue)
        {
            var dobKonto = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "4350")
                ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("435") || k.BrojKonta.StartsWith("433"));
            dobavljacKontoId = dobKonto?.KontoId;
        }

        if (dobavljacKontoId.HasValue)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb,
                KontoId = dobavljacKontoId.Value,
                PartnerId = kalkulacija.PartnerId,
                Opis = opis,
                BrojDokumenta = kalkulacija.BrojRacuna ?? kalkulacija.BrojFaktureDobavljaca,
                Duguje = 0m,
                Potrazuje = svegaNabavno
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        kalkulacija.NalogId = nalog.NalogId;
    }

    public async Task RasknjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);

        if (kalkulacija == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
        if (!kalkulacija.IsKnjizen) throw new InvalidOperationException($"Kalkulacija #{kalkulacija.BrojKalkulacije} nije proknjižena.");

        if (kalkulacija.Stavke.Count > 0 && kalkulacija.Magacin != null)
        {
            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke.AsEnumerable().Reverse())
            {
                if (s.Artikal != null)
                {
                    await kartice.UkloniPoslednjiRedAsync(kalkulacija.Magacin.SifraMagacina, s.Artikal.SifraArtikla, $"Kalkulacija {kalkulacija.BrojKalkulacije}");
                }
            }
        }

        if (kalkulacija.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == kalkulacija.NalogId.Value);
            if (nalog != null)
            {
                _db.StavkeNaloga.RemoveRange(nalog.Stavke);
                _db.Nalozi.Remove(nalog);
            }
            kalkulacija.NalogId = null;
        }

        kalkulacija.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}
