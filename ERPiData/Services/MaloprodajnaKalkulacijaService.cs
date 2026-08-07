using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za kalkulacije maloprodaje (prijem robe u prodavnicu po prodajnim cenama sa PDV-om).
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class MaloprodajnaKalkulacijaService
{
    private readonly ErpiDbContext _db;

    public MaloprodajnaKalkulacijaService(ErpiDbContext db)
    {
        _db = db;
    }

    public static void Izracunaj(MaloprodajnaKalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;
        k.SvegaNabavno = k.NabavnaVrednost + k.SvegaTroskovi;
        k.Razlika = Math.Round(k.SvegaNabavno * k.MarzaProcenat / 100m, 2);
        decimal prodajnaBezPoreza = k.SvegaNabavno + k.Razlika;
        k.Porez = Math.Round(prodajnaBezPoreza * k.PoreskaStopaProcenat / 100m, 2);
        k.ProdajnaVrednost = prodajnaBezPoreza + k.Porez;
        k.RabatIznos = Math.Round(k.NabavnaVrednost * k.RabatPri / 100m, 2);
    }

    public static void IzracunajSaStavkama(MaloprodajnaKalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;

        foreach (var s in k.Stavke)
        {
            s.Iznos = s.Kolicina * s.NabavnaCena;
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
            s.RazlikaIznos = Math.Round(s.NabavnaVrednost * k.MarzaProcenat / 100m, 2);
            decimal prodajnaBezPoreza = s.NabavnaVrednost + s.RazlikaIznos;
            s.PorezIznos = Math.Round(prodajnaBezPoreza * k.PoreskaStopaProcenat / 100m, 2);
            s.ProdajnaVrednost = prodajnaBezPoreza + s.PorezIznos;
            s.ProdajnaCena = s.Kolicina != 0 ? s.ProdajnaVrednost / s.Kolicina : 0m;
        }

        k.NabavnaVrednost = svegaIznos;
        k.SvegaNabavno = k.Stavke.Sum(s => s.NabavnaVrednost);
        k.Razlika = k.Stavke.Sum(s => s.RazlikaIznos);
        k.Porez = k.Stavke.Sum(s => s.PorezIznos);
        k.ProdajnaVrednost = k.Stavke.Sum(s => s.ProdajnaVrednost);
        k.RabatIznos = Math.Round(k.NabavnaVrednost * k.RabatPri / 100m, 2);
    }

    public async Task<List<MaloprodajnaKalkulacija>> GetKalkulacijeAsync(string? search = null)
    {
        var query = _db.MaloprodajneKalkulacije
            .Include(k => k.MagacinPrima)
            .Include(k => k.MagacinDaje)
            .Include(k => k.Dobavljac)
            .Include(k => k.KontoDobavljaca)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKalkulacije.ToString().Contains(search) || (k.Dobavljac != null && k.Dobavljac.Naziv.Contains(search)));
        }
        return await query.OrderByDescending(k => k.Datum).ToListAsync();
    }

    public async Task<MaloprodajnaKalkulacija> SaveKalkulacijuAsync(MaloprodajnaKalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count > 0)
        {
            IzracunajSaStavkama(kalkulacija);
        }
        else
        {
            Izracunaj(kalkulacija);
        }

        if (kalkulacija.MaloprodajnaKalkulacijaId == 0)
        {
            if (kalkulacija.BrojKalkulacije == 0)
            {
                kalkulacija.BrojKalkulacije = (await _db.MaloprodajneKalkulacije.Select(k => (int?)k.BrojKalkulacije).MaxAsync() ?? 0) + 1;
            }
            _db.MaloprodajneKalkulacije.Add(kalkulacija);
        }
        else
        {
            var existing = await _db.MaloprodajneKalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacija.MaloprodajnaKalkulacijaId);

            if (existing == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
            if (existing.IsKnjizen) throw new InvalidOperationException("Proknjižena kalkulacija se ne može menjati.");

            existing.BrojKalkulacije = kalkulacija.BrojKalkulacije;
            existing.Datum = kalkulacija.Datum;
            existing.MagacinIdPrima = kalkulacija.MagacinIdPrima;
            existing.MagacinIdDaje = kalkulacija.MagacinIdDaje;
            existing.DobavljacId = kalkulacija.DobavljacId;
            existing.KontoDobavljacaId = kalkulacija.KontoDobavljacaId;
            existing.BrojOtpremnice = kalkulacija.BrojOtpremnice;
            existing.DatumOtpremnice = kalkulacija.DatumOtpremnice;
            existing.BrojRacuna = kalkulacija.BrojRacuna;
            existing.DatumRacuna = kalkulacija.DatumRacuna;
            existing.TransportniTroskovi = kalkulacija.TransportniTroskovi;
            existing.TroskoviUskladistenja = kalkulacija.TroskoviUskladistenja;
            existing.UtovarIstovar = kalkulacija.UtovarIstovar;
            existing.TransportnoOsiguranje = kalkulacija.TransportnoOsiguranje;
            existing.OstaliTroskovi = kalkulacija.OstaliTroskovi;
            existing.SvegaTroskovi = kalkulacija.SvegaTroskovi;
            existing.RabatPri = kalkulacija.RabatPri;
            existing.NabavnaVrednost = kalkulacija.NabavnaVrednost;
            existing.SvegaNabavno = kalkulacija.SvegaNabavno;
            existing.Razlika = kalkulacija.Razlika;
            existing.MarzaProcenat = kalkulacija.MarzaProcenat;
            existing.Porez = kalkulacija.Porez;
            existing.PoreskaStopaProcenat = kalkulacija.PoreskaStopaProcenat;
            existing.ProdajnaVrednost = kalkulacija.ProdajnaVrednost;
            existing.RabatIznos = kalkulacija.RabatIznos;

            _db.MaloprodajneKalkulacijeStavke.RemoveRange(existing.Stavke);
            existing.Stavke = kalkulacija.Stavke;
            kalkulacija = existing;
        }

        await _db.SaveChangesAsync();
        return kalkulacija;
    }

    public async Task KnjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.MaloprodajneKalkulacije
            .Include(k => k.MagacinPrima)
            .Include(k => k.MagacinDaje)
            .Include(k => k.Dobavljac)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacijaId);

        if (kalkulacija == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
        if (kalkulacija.IsKnjizen) throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} je već proknjižena.");

        if (kalkulacija.Stavke.Count > 0)
        {
            var kartice = new MaterijalnaKarticaService(_db);
            string opisKartice = $"MP kalkulacija {kalkulacija.BrojKalkulacije}";

            if (kalkulacija.MagacinDaje != null)
            {
                foreach (var s in kalkulacija.Stavke)
                {
                    string sifra = s.Artikal?.SifraArtikla ?? s.ArtikalId.ToString();
                    await kartice.DodajIzlazRedAsync(kalkulacija.MagacinDaje.SifraMagacina, sifra, kalkulacija.Datum, opisKartice, s.Kolicina);
                }
            }
            else if (kalkulacija.MagacinPrima != null)
            {
                foreach (var s in kalkulacija.Stavke)
                {
                    string sifra = s.Artikal?.SifraArtikla ?? s.ArtikalId.ToString();
                    await kartice.DodajUlazRedAsync(kalkulacija.MagacinPrima.SifraMagacina, sifra, kalkulacija.Datum, opisKartice, s.Kolicina, s.ProdajnaCena);
                }
            }
            else
            {
                throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} nema izabran magacin za knjiženje.");
            }
        }

        await KnjiziUGlavnuKnjiguAsync(kalkulacija);

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    private async Task KnjiziUGlavnuKnjiguAsync(MaloprodajnaKalkulacija kalkulacija)
    {
        if (kalkulacija.ProdajnaVrednost == 0) return;

        string opis = $"Kalkulacija maloprodaje {kalkulacija.BrojKalkulacije}";
        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

        var kontoRoba = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == RobnaKonta.RobaMaloprodaja);
        var kontoPdv = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == RobnaKonta.UkalkulisaniPdvZaStopu(kalkulacija.PoreskaStopaProcenat));
        var kontoRazlika = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == RobnaKonta.RazlikaUCeniMaloprodaja);
        int? dobavljacKontoId = kalkulacija.KontoDobavljacaId;
        if (!dobavljacKontoId.HasValue)
        {
            var kontoDobavljac = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "4350")
                               ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("435"));
            dobavljacKontoId = kontoDobavljac?.KontoId;
        }

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
        if (kontoRoba != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoRoba.KontoId,
                Opis = opis,
                BrojDokumenta = kalkulacija.BrojRacuna,
                Duguje = kalkulacija.ProdajnaVrednost,
                Potrazuje = 0m,
                PartnerId = kalkulacija.DobavljacId
            });
        }

        if (kalkulacija.Porez != 0 && kontoPdv != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoPdv.KontoId,
                Opis = opis,
                Duguje = 0m,
                Potrazuje = kalkulacija.Porez,
                PartnerId = kalkulacija.DobavljacId
            });
        }

        if (kalkulacija.Razlika != 0 && kontoRazlika != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoRazlika.KontoId,
                Opis = opis,
                Duguje = 0m,
                Potrazuje = kalkulacija.Razlika,
                PartnerId = kalkulacija.DobavljacId
            });
        }

        if (dobavljacKontoId.HasValue)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = dobavljacKontoId.Value,
                Opis = opis,
                BrojDokumenta = kalkulacija.BrojRacuna,
                Duguje = 0m,
                Potrazuje = kalkulacija.SvegaNabavno,
                PartnerId = kalkulacija.DobavljacId
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
        var kalkulacija = await _db.MaloprodajneKalkulacije
            .Include(k => k.MagacinPrima)
            .Include(k => k.MagacinDaje)
            .Include(k => k.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacijaId);

        if (kalkulacija == null) throw new InvalidOperationException("Kalkulacija nije pronađena.");
        if (!kalkulacija.IsKnjizen) throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} nije proknjižena.");

        if (kalkulacija.Stavke.Count > 0)
        {
            string? sifraMagacina = kalkulacija.MagacinDaje?.SifraMagacina ?? kalkulacija.MagacinPrima?.SifraMagacina;

            if (!string.IsNullOrWhiteSpace(sifraMagacina))
            {
                var kartice = new MaterijalnaKarticaService(_db);
                foreach (var s in kalkulacija.Stavke.AsEnumerable().Reverse())
                {
                    string sifra = s.Artikal?.SifraArtikla ?? s.ArtikalId.ToString();
                    await kartice.UkloniPoslednjiRedAsync(sifraMagacina, sifra, $"MP kalkulacija {kalkulacija.BrojKalkulacije}");
                }
            }
        }

        if (kalkulacija.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.FirstOrDefaultAsync(n => n.NalogId == kalkulacija.NalogId.Value);
            if (nalog != null) _db.Nalozi.Remove(nalog);
        }

        kalkulacija.IsKnjizen = false;
        kalkulacija.NalogId = null;
        await _db.SaveChangesAsync();
    }
}
