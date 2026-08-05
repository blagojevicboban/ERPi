using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class BlagajnaService
{
    private readonly ErpiDbContext _db;

    public BlagajnaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<BlagajnickiNalog>> GetBlagajnickiNaloziAsync(VrstaBlagajne? vrsta = null)
    {
        var query = _db.BlagajnickiNalozi.AsQueryable();
        if (vrsta.HasValue)
        {
            query = query.Where(b => b.VrstaBlagajne == vrsta.Value);
        }

        return await query
            .OrderByDescending(b => b.Datum)
            .ThenByDescending(b => b.BlagajnickiNalogId)
            .ToListAsync();
    }

    public async Task<BlagajnickiNalog?> GetBlagajnickiNalogByIdAsync(int id)
    {
        return await _db.BlagajnickiNalozi.FirstOrDefaultAsync(b => b.BlagajnickiNalogId == id);
    }

    public async Task<BlagajnickiNalog> SacuvajBlagajnickiNalogAsync(BlagajnickiNalog bn)
    {
        if (bn.BlagajnickiNalogId == 0)
        {
            if (string.IsNullOrWhiteSpace(bn.BrojNaloga))
            {
                int sledeciBroj = await _db.BlagajnickiNalozi.CountAsync(b => b.VrstaBlagajne == bn.VrstaBlagajne && b.VrstaNaloga == bn.VrstaNaloga) + 1;
                string oznakaBlagajne = bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "DEV" : "DIN";
                string ozNaloga = bn.VrstaNaloga == VrstaBlagajnickogNaloga.Uplata ? "U" : "I";

                bn.BrojNaloga = $"BL{ozNaloga}-{oznakaBlagajne}-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.BlagajnickiNalozi.Add(bn);
        }
        else
        {
            _db.BlagajnickiNalozi.Update(bn);
        }

        await _db.SaveChangesAsync();
        return bn;
    }

    public async Task<bool> ObrisiBlagajnickiNalogAsync(int id)
    {
        var bn = await _db.BlagajnickiNalozi.FindAsync(id);
        if (bn == null || bn.IsKnjizeno) return false;

        _db.BlagajnickiNalozi.Remove(bn);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(List<BlagajnickiDnevnikRed> Redovi, BlagajnickiDnevnikSummary Summary)> GetBlagajnickiDnevnikAsync(
        VrstaBlagajne vrsta, DateTime odDatuma, DateTime doDatuma)
    {
        var sviPre = await _db.BlagajnickiNalozi
            .Where(b => b.VrstaBlagajne == vrsta && b.Datum.Date < odDatuma.Date)
            .ToListAsync();

        decimal pocetniSaldo = sviPre
            .Sum(b => b.VrstaNaloga == VrstaBlagajnickogNaloga.Uplata ? b.Iznos : -b.Iznos);

        var naloziPeriod = await _db.BlagajnickiNalozi
            .Where(b => b.VrstaBlagajne == vrsta && b.Datum.Date >= odDatuma.Date && b.Datum.Date <= doDatuma.Date)
            .OrderBy(b => b.Datum)
            .ThenBy(b => b.BlagajnickiNalogId)
            .ToListAsync();

        var redovi = new List<BlagajnickiDnevnikRed>();
        decimal tekuciSaldo = pocetniSaldo;
        decimal ukupnoUplata = 0m;
        decimal ukupnoIsplata = 0m;

        foreach (var n in naloziPeriod)
        {
            decimal uplata = n.VrstaNaloga == VrstaBlagajnickogNaloga.Uplata ? n.Iznos : 0m;
            decimal isplata = n.VrstaNaloga == VrstaBlagajnickogNaloga.Isplata ? n.Iznos : 0m;

            tekuciSaldo += (uplata - isplata);
            ukupnoUplata += uplata;
            ukupnoIsplata += isplata;

            redovi.Add(new BlagajnickiDnevnikRed
            {
                BlagajnickiNalogId = n.BlagajnickiNalogId,
                BrojNaloga = n.BrojNaloga,
                Datum = n.Datum,
                Vrsta = n.VrstaNaloga.ToString(),
                UplatilacIsplatilac = n.UplatilacIsplatilac,
                Svrha = n.Svrha,
                BrojKontaProtu = n.BrojKontaProtu,
                Uplata = uplata,
                Isplata = isplata,
                Saldo = tekuciSaldo
            });
        }

        var summary = new BlagajnickiDnevnikSummary
        {
            PocetnoStanje = pocetniSaldo,
            UkupnoUplata = ukupnoUplata,
            UkupnoIsplata = ukupnoIsplata
        };

        return (redovi, summary);
    }

    public async Task<(bool Success, string Message, int? NalogId)> KnjiziBlagajnickiNalogAsync(int blagajnickiNalogId)
    {
        var bn = await GetBlagajnickiNalogByIdAsync(blagajnickiNalogId);
        if (bn == null) return (false, "Blagajnički nalog ne postoji.", null);

        if (bn.IsKnjizeno)
        {
            return (false, "Nalog blagajne je već proknjižen.", bn.NalogId);
        }

        if (bn.Iznos <= 0)
        {
            return (false, "Iznos naloga blagajne mora biti veći od 0.", null);
        }

        string brojKontaBlagajne = bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "2440" : "2430";
        string nazivBlagajne = bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "Devizna blagajna" : "Dinarska blagajna";
        string brojKontaProtu = string.IsNullOrWhiteSpace(bn.BrojKontaProtu) ? "2410" : bn.BrojKontaProtu;

        var kontoBlagajne = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == brojKontaBlagajne)
                            ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith(brojKontaBlagajne.Substring(0, 3)))
                            ?? await _db.Konta.FirstAsync();

        var kontoProtu = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == brojKontaProtu)
                         ?? await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta.StartsWith(brojKontaProtu.Substring(0, Math.Min(3, brojKontaProtu.Length))))
                         ?? kontoBlagajne;

        int sledeciBrojNaloga = await _db.Nalozi.CountAsync() + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            VrstaNaloga = "BL",
            DatumNaloga = bn.Datum,
            Opis = $"Blagajnički nalog {bn.BrojNaloga} ({bn.VrstaNaloga}): {bn.Svrha} ({bn.UplatilacIsplatilac})",
            Status = StatusNaloga.Proknjizen,
            UkupnoDuguje = bn.Iznos,
            UkupnoPotrazuje = bn.Iznos
        };

        if (bn.VrstaNaloga == VrstaBlagajnickogNaloga.Uplata)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 1,
                KontoId = kontoBlagajne.KontoId,
                Opis = $"Uplata u {nazivBlagajne} — {bn.Svrha}",
                Duguje = bn.Iznos,
                Potrazuje = 0m,
                BrojDokumenta = bn.BrojNaloga,
                DatumDokumenta = bn.Datum
            });

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 2,
                KontoId = kontoProtu.KontoId,
                Opis = $"Protivkonto uplate u blagajnu ({bn.UplatilacIsplatilac})",
                Duguje = 0m,
                Potrazuje = bn.Iznos,
                BrojDokumenta = bn.BrojNaloga,
                DatumDokumenta = bn.Datum
            });
        }
        else
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 1,
                KontoId = kontoProtu.KontoId,
                Opis = $"Protivkonto isplate iz blagajne ({bn.UplatilacIsplatilac})",
                Duguje = bn.Iznos,
                Potrazuje = 0m,
                BrojDokumenta = bn.BrojNaloga,
                DatumDokumenta = bn.Datum
            });

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 2,
                KontoId = kontoBlagajne.KontoId,
                Opis = $"Isplata iz {nazivBlagajne} — {bn.Svrha}",
                Duguje = 0m,
                Potrazuje = bn.Iznos,
                BrojDokumenta = bn.BrojNaloga,
                DatumDokumenta = bn.Datum
            });
        }

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        bn.IsKnjizeno = true;
        bn.Status = "Proknjiženo";
        bn.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno proknjižen nalog blagajne br. {bn.BrojNaloga} u Glavnu knjigu (Nalog BL br. {sledeciBrojNaloga})!", nalog.NalogId);
    }
}
