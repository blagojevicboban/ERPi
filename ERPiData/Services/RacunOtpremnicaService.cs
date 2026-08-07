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
/// Servis za prodaju i izdavanje računa-otpremnica.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class RacunOtpremnicaService
{
    private readonly ErpiDbContext _db;

    public RacunOtpremnicaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<RacunOtpremnica>> GetRacuneAsync(int? magacinId = null)
    {
        var query = _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .Include(r => r.Magacin)
            .Include(r => r.KontoKupca)
            .Include(r => r.Stavke).ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (magacinId.HasValue && magacinId.Value > 0)
        {
            query = query.Where(r => r.MagacinId == magacinId.Value);
        }

        return await query.OrderByDescending(r => r.DatumRacuna)
            .ThenByDescending(r => r.RacunOtpremnicaId)
            .ToListAsync();
    }

    public async Task<RacunOtpremnica?> GetRacunByIdAsync(int id)
    {
        return await _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .Include(r => r.Magacin)
            .Include(r => r.KontoKupca)
            .Include(r => r.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == id);
    }

    public async Task SaveRacunAsync(RacunOtpremnica racun)
    {
        // Preračunaj zbirove
        decimal osn = 0m, rab = 0m, pdv = 0m, tot = 0m;
        int rb = 1;
        foreach (var s in racun.Stavke)
        {
            s.RedniBroj = rb++;
            decimal brutovrednost = s.Kolicina * s.ProdajnaCena;
            decimal iznosRabata = brutovrednost * (s.RabatProcenat / 100m);
            s.Osnovica = brutovrednost - iznosRabata;
            s.IznosPdv = s.Osnovica * (s.StopaPdv / 100m);
            s.Ukupno = s.Osnovica + s.IznosPdv;

            osn += s.Osnovica;
            rab += iznosRabata;
            pdv += s.IznosPdv;
            tot += s.Ukupno;
        }

        racun.UkupnoOsnovica = osn;
        racun.UkupnoRabat = rab;
        racun.UkupnoPdv = pdv;
        racun.UkupnoZaUplatu = tot;

        if (racun.RacunOtpremnicaId == 0)
        {
            if (racun.BrojRacuna == 0)
            {
                racun.BrojRacuna = (await _db.RacuniOtpremnice.Select(r => (int?)r.BrojRacuna).MaxAsync() ?? 0) + 1;
            }
            _db.RacuniOtpremnice.Add(racun);
        }
        else
        {
            var existing = await _db.RacuniOtpremnice
                .Include(r => r.Stavke)
                .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racun.RacunOtpremnicaId);

            if (existing != null)
            {
                if (existing.IsKnjizen)
                    throw new InvalidOperationException("Proknjiženi račun-otpremnica se ne može menjati.");

                existing.TipDokumenta = racun.TipDokumenta;
                existing.RokVazenjaPredracuna = racun.RokVazenjaPredracuna;
                existing.BrojRacuna = racun.BrojRacuna;
                existing.DatumRacuna = racun.DatumRacuna;
                existing.RokPlacanja = racun.RokPlacanja;
                existing.PartnerId = racun.PartnerId;
                existing.MagacinId = racun.MagacinId;
                existing.KontoKupcaId = racun.KontoKupcaId;
                existing.Napomena = racun.Napomena;
                existing.UkupnoOsnovica = racun.UkupnoOsnovica;
                existing.UkupnoRabat = racun.UkupnoRabat;
                existing.UkupnoPdv = racun.UkupnoPdv;
                existing.UkupnoZaUplatu = racun.UkupnoZaUplatu;

                _db.RacunOtpremnicaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = racun.Stavke;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task PretvoriUFakturuAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice.FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Predračun nije pronađen.");
        if (racun.TipDokumenta != TipRacunOtpremnice.Predracun)
            throw new InvalidOperationException("Dokument nije predračun.");

        racun.TipDokumenta = TipRacunOtpremnice.Racun;
        racun.DatumRacuna = DateTime.Now;
        racun.RokVazenjaPredracuna = null;
        await _db.SaveChangesAsync();
    }

    public async Task KnjiziRacunAsync(int racunOtpremnicaId)
    {
        var racun = await GetRacunByIdAsync(racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Račun nije pronađen.");
        if (racun.TipDokumenta == TipRacunOtpremnice.Predracun)
            throw new InvalidOperationException("Predračun se ne može knjižiti — prvo ga pretvorite u račun.");
        if (racun.IsKnjizen) throw new InvalidOperationException("Račun je već proknjižen.");

        // Uslužne stavke (ArtikalId == null) ne razdužuju magacin — samo robne stavke idu na
        // materijalnu karticu. Magacin je obavezan SAMO ako račun ima bar jednu robnu stavku.
        decimal nabavnaVrednostProdate = 0m;
        var robneStavke = racun.Stavke.Where(s => s.ArtikalId.HasValue).ToList();
        if (robneStavke.Count > 0)
        {
            if (racun.Magacin == null)
            {
                throw new InvalidOperationException($"Račun {racun.BrojRacuna} nema izabran magacin — izaberite magacin pre knjiženja.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in robneStavke)
            {
                string sifraArtikla = s.Artikal?.SifraArtikla ?? s.ArtikalId.ToString()!;
                nabavnaVrednostProdate += await kartice.DodajIzlazRedAsync(
                    racun.Magacin.SifraMagacina, sifraArtikla, racun.DatumRacuna,
                    $"Račun {racun.BrojRacuna}", s.Kolicina);
            }
        }

        // Konta za knjiženje
        Konto? kontoKupca = null;
        if (racun.KontoKupcaId.HasValue)
        {
            kontoKupca = await _db.Konta.FirstOrDefaultAsync(k => k.KontoId == racun.KontoKupcaId.Value);
        }
        kontoKupca ??= await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "2040")
                      ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("204"));

        var kontoPrihod = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "6120")
                          ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("612"));

        var kontoPdv = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "4700")
                       ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("470"));

        var kontoNabavna = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "5010")
                           ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("501"));

        string kontoRobeStr = RobnaKonta.RobaZaVrstuMagacina(racun.Magacin?.VrstaMagacina);
        var kontoRobe = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == kontoRobeStr);

        int sledeciBrojNaloga = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;
        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            DatumNaloga = racun.DatumRacuna,
            VrstaNaloga = "Prodaja",
            Opis = $"Račun-otpremnica br. {racun.BrojRacuna}",
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = DateTime.Now
        };

        int rb = 1;
        if (kontoKupca != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoKupca.KontoId,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Faktura br. {racun.BrojRacuna}",
                Duguje = racun.UkupnoZaUplatu,
                Potrazuje = 0m,
                PartnerId = racun.PartnerId
            });
        }

        if (kontoPrihod != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoPrihod.KontoId,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Prihod po fakturi {racun.BrojRacuna}",
                Duguje = 0m,
                Potrazuje = racun.UkupnoOsnovica,
                PartnerId = racun.PartnerId
            });
        }

        if (racun.UkupnoPdv > 0 && kontoPdv != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoPdv.KontoId,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Obračunati PDV po fakturi {racun.BrojRacuna}",
                Duguje = 0m,
                Potrazuje = racun.UkupnoPdv,
                PartnerId = racun.PartnerId
            });
        }

        if (nabavnaVrednostProdate != 0 && kontoNabavna != null && kontoRobe != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoNabavna.KontoId,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Nabavna vrednost prodate robe po fakturi {racun.BrojRacuna}",
                Duguje = nabavnaVrednostProdate,
                Potrazuje = 0m
            });

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoRobe.KontoId,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Razduženje robe po fakturi {racun.BrojRacuna}",
                Duguje = 0m,
                Potrazuje = nabavnaVrednostProdate
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        racun.IsKnjizen = true;
        racun.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();
    }

    public async Task RasknjiziRacunAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Magacin)
            .Include(r => r.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);

        if (racun == null) throw new InvalidOperationException("Račun nije pronađen.");
        if (!racun.IsKnjizen) throw new InvalidOperationException("Račun nije proknjižen.");

        if (racun.Magacin != null)
        {
            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in racun.Stavke.Where(s => s.ArtikalId.HasValue).AsEnumerable().Reverse())
            {
                string sifraArtikla = s.Artikal?.SifraArtikla ?? s.ArtikalId.ToString()!;
                await kartice.UkloniPoslednjiRedAsync(racun.Magacin.SifraMagacina, sifraArtikla, $"Račun {racun.BrojRacuna}");
            }
        }

        if (racun.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.FirstOrDefaultAsync(n => n.NalogId == racun.NalogId.Value);
            if (nalog != null)
            {
                _db.Nalozi.Remove(nalog);
            }
        }

        racun.IsKnjizen = false;
        racun.NalogId = null;
        await _db.SaveChangesAsync();
    }
}
