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
/// Servis za uvozne kalkulacije sa zavisnim troškovima (carina, špedicija, prevoz) i deviznim fakturama.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class UvoznaKalkulacijaService
{
    private readonly ErpiDbContext _db;

    public UvoznaKalkulacijaService(ErpiDbContext db)
    {
        _db = db;
    }

    public void ProracunajUvoznuKalkulaciju(UvoznaKalkulacija kalkulacija)
    {
        if (kalkulacija == null || !kalkulacija.Stavke.Any()) return;

        foreach (var s in kalkulacija.Stavke)
        {
            s.InoIznosDevize = Math.Round(s.Kolicina * s.InoCenaDevize, 2);
            s.InoIznosRsd = Math.Round(s.InoIznosDevize * kalkulacija.KursValute, 2);
        }

        kalkulacija.UkupnoDevize = kalkulacija.Stavke.Sum(s => s.InoIznosDevize);
        kalkulacija.UkupnoFakturaRsd = Math.Round(kalkulacija.UkupnoDevize * kalkulacija.KursValute, 2);

        decimal ukupniZavisniTroskoviRsd = kalkulacija.SpedicijaRsd + kalkulacija.PrevozRsd + kalkulacija.OstaliZavisniTroskoviRsd;
        decimal ukupnoInoRsd = kalkulacija.UkupnoFakturaRsd > 0 ? kalkulacija.UkupnoFakturaRsd : 1m;
        decimal ukupnaCarinaRsd = 0m;

        foreach (var s in kalkulacija.Stavke)
        {
            s.CarinaIznosRsd = Math.Round(s.InoIznosRsd * (s.CarinaProcenat / 100m), 2);
            ukupnaCarinaRsd += s.CarinaIznosRsd;

            decimal udeo = s.InoIznosRsd / ukupnoInoRsd;
            s.RasporedjeniZavisniTroskoviRsd = Math.Round(ukupniZavisniTroskoviRsd * udeo, 2);

            s.UkupnaNabavnaVrednostRsd = s.InoIznosRsd + s.CarinaIznosRsd + s.RasporedjeniZavisniTroskoviRsd;
            s.NabavnaCenaPoJediniciRsd = s.Kolicina > 0 ? Math.Round(s.UkupnaNabavnaVrednostRsd / s.Kolicina, 4) : 0m;
        }

        kalkulacija.CarinaRsd = ukupnaCarinaRsd;
        kalkulacija.UkupnaNabavnaVrednostRsd = kalkulacija.UkupnoFakturaRsd + kalkulacija.CarinaRsd + ukupniZavisniTroskoviRsd;
    }

    public async Task<List<UvoznaKalkulacija>> GetKalkulacijeAsync(string? search = null)
    {
        var query = _db.UvozneKalkulacije
            .Include(u => u.InoPartner)
            .Include(u => u.Magacin)
            .Include(u => u.Stavke).ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.BrojKalkulacije.Contains(search) || u.InoBrojFakture.Contains(search));
        }

        return await query.OrderByDescending(u => u.DatumKalkulacije).ToListAsync();
    }

    public async Task SaveUvozAsync(UvoznaKalkulacija kalkulacija)
    {
        ProracunajUvoznuKalkulaciju(kalkulacija);

        if (kalkulacija.UvoznaKalkulacijaId == 0)
        {
            _db.UvozneKalkulacije.Add(kalkulacija);
        }
        else
        {
            var existing = await _db.UvozneKalkulacije
                .Include(u => u.Stavke)
                .FirstOrDefaultAsync(u => u.UvoznaKalkulacijaId == kalkulacija.UvoznaKalkulacijaId);

            if (existing != null)
            {
                if (existing.IsKnjizen)
                    throw new InvalidOperationException("Proknjižena uvozna kalkulacija se ne može menjati.");

                existing.BrojKalkulacije = kalkulacija.BrojKalkulacije;
                existing.DatumKalkulacije = kalkulacija.DatumKalkulacije;
                existing.InoPartnerId = kalkulacija.InoPartnerId;
                existing.InoBrojFakture = kalkulacija.InoBrojFakture;
                existing.DatumInoFakture = kalkulacija.DatumInoFakture;
                existing.Valuta = kalkulacija.Valuta;
                existing.KursValute = kalkulacija.KursValute;
                existing.SpedicijaRsd = kalkulacija.SpedicijaRsd;
                existing.PrevozRsd = kalkulacija.PrevozRsd;
                existing.OstaliZavisniTroskoviRsd = kalkulacija.OstaliZavisniTroskoviRsd;
                existing.MagacinId = kalkulacija.MagacinId;

                _db.UvozneStavke.RemoveRange(existing.Stavke);
                existing.Stavke = kalkulacija.Stavke;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task KnjiziUvozAsync(int uvoznaKalkulacijaId)
    {
        var kalkulacija = await _db.UvozneKalkulacije
            .Include(u => u.InoPartner)
            .Include(u => u.Magacin)
            .Include(u => u.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(u => u.UvoznaKalkulacijaId == uvoznaKalkulacijaId);

        if (kalkulacija == null) throw new InvalidOperationException("Uvozna kalkulacija nije pronađena.");
        if (kalkulacija.IsKnjizen) throw new InvalidOperationException($"Uvozna kalkulacija #{kalkulacija.BrojKalkulacije} je već proknjižena.");

        ProracunajUvoznuKalkulaciju(kalkulacija);

        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;
        var nalog = new Nalog
        {
            BrojNaloga = sledeciBroj,
            DatumNaloga = kalkulacija.DatumKalkulacije,
            Opis = $"Knjiženje uvozne kalkulacije #{kalkulacija.BrojKalkulacije} (Ino faktura {kalkulacija.InoBrojFakture})",
            Status = StatusNaloga.Proknjizen,
            DatumKnjizenja = DateTime.Now,
            VrstaNaloga = "UVOZ"
        };

        var kontoNabavka = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "1300")
                          ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("130"));

        var kontoInoDobavljac = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "4350")
                               ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("435"));

        var kontoZavisni = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == "4330")
                          ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith("433"));

        int rb = 1;
        if (kontoNabavka != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoNabavka.KontoId,
                Opis = $"Uvoz robe po kalkulaciji #{kalkulacija.BrojKalkulacije}",
                Duguje = kalkulacija.UkupnaNabavnaVrednostRsd,
                Potrazuje = 0m
            });
        }

        if (kontoInoDobavljac != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoInoDobavljac.KontoId,
                Opis = $"Ino faktura #{kalkulacija.InoBrojFakture}",
                Duguje = 0m,
                Potrazuje = kalkulacija.UkupnoFakturaRsd,
                Valuta = kalkulacija.Valuta,
                KursValute = kalkulacija.KursValute,
                DevizniPotrazuje = kalkulacija.UkupnoDevize,
                PartnerId = kalkulacija.InoPartnerId
            });
        }

        decimal zavisniTroskoviUkupno = kalkulacija.CarinaRsd + kalkulacija.SpedicijaRsd + kalkulacija.PrevozRsd + kalkulacija.OstaliZavisniTroskoviRsd;
        if (zavisniTroskoviUkupno > 0 && kontoZavisni != null)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                KontoId = kontoZavisni.KontoId,
                Opis = $"Zavisni troškovi uvoza (Carina, Prevoz, Špedicija) - Kalkulacija #{kalkulacija.BrojKalkulacije}",
                Duguje = 0m,
                Potrazuje = zavisniTroskoviUkupno
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        kalkulacija.IsKnjizen = true;
        kalkulacija.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();
    }

    public async Task RasknjiziUvozAsync(int uvoznaKalkulacijaId)
    {
        var kalkulacija = await _db.UvozneKalkulacije.FirstOrDefaultAsync(k => k.UvoznaKalkulacijaId == uvoznaKalkulacijaId);
        if (kalkulacija == null) throw new InvalidOperationException("Uvozna kalkulacija nije pronađena.");
        if (!kalkulacija.IsKnjizen) throw new InvalidOperationException($"Uvozna kalkulacija #{kalkulacija.BrojKalkulacije} nije proknjižena.");

        if (kalkulacija.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == kalkulacija.NalogId.Value);
            if (nalog != null)
            {
                _db.StavkeNaloga.RemoveRange(nalog.Stavke);
                _db.Nalozi.Remove(nalog);
            }
        }

        kalkulacija.IsKnjizen = false;
        kalkulacija.NalogId = null;
        await _db.SaveChangesAsync();
    }
}
