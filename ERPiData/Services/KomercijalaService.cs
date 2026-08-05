using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Ponude/Predračuni kupcima i Narudžbenice dobavljačima, sa 1-klik konverzijom u
/// <see cref="RacunOtpremnica"/> odn. <see cref="Kalkulacija"/>. Portovano iz
/// ERPiFinansijeData.Services.KomercijalaService (§3i u PLAN_NASTAVKA.md), prilagođeno pravim
/// FK-ovima (ArtikalId/PartnerId/MagacinId) umesto DBF-stil string šifri iz izvora.
/// </summary>
public class KomercijalaService
{
    private readonly ErpiDbContext _db;

    public KomercijalaService(ErpiDbContext db)
    {
        _db = db;
    }

    #region Ponude i Predračuni

    public async Task<List<PonudaPredracun>> GetPonudeAsync()
    {
        return await _db.PonudePredracuni
            .Include(p => p.Partner)
            .Include(p => p.Stavke).ThenInclude(s => s.Artikal)
            .OrderByDescending(p => p.Datum)
            .ThenByDescending(p => p.PonudaPredracunId)
            .ToListAsync();
    }

    public async Task<PonudaPredracun?> GetPonudaByIdAsync(int id)
    {
        return await _db.PonudePredracuni
            .Include(p => p.Partner)
            .Include(p => p.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(p => p.PonudaPredracunId == id);
    }

    public async Task<PonudaPredracun> SacuvajPonuduAsync(PonudaPredracun ponuda)
    {
        foreach (var s in ponuda.Stavke)
        {
            decimal bruto = s.Kolicina * s.Cena;
            decimal rabat = bruto * (s.RabatProcenat / 100m);
            s.IznosNeto = bruto - rabat;
            s.IznosPdv = s.IznosNeto * (s.PdvStopa / 100m);
            s.IznosBruto = s.IznosNeto + s.IznosPdv;
        }

        ponuda.UkupnoNeto = ponuda.Stavke.Sum(s => s.IznosNeto);
        ponuda.UkupnoPdv = ponuda.Stavke.Sum(s => s.IznosPdv);
        ponuda.UkupnoBruto = ponuda.Stavke.Sum(s => s.IznosBruto);

        if (ponuda.PonudaPredracunId == 0)
        {
            if (string.IsNullOrWhiteSpace(ponuda.BrojDokumenta))
            {
                int sledeciBroj = await _db.PonudePredracuni.CountAsync() + 1;
                string prefiks = ponuda.VrstaDokumenta == "Predračun" ? "PRD" : "PON";
                ponuda.BrojDokumenta = $"{prefiks}-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.PonudePredracuni.Add(ponuda);
        }
        else
        {
            var postojeceStavke = _db.PonudeStavke.Where(s => s.PonudaPredracunId == ponuda.PonudaPredracunId);
            _db.PonudeStavke.RemoveRange(postojeceStavke);

            _db.PonudePredracuni.Update(ponuda);
        }

        await _db.SaveChangesAsync();
        return ponuda;
    }

    public async Task<bool> ObrisiPonuduAsync(int id)
    {
        var ponuda = await _db.PonudePredracuni.FindAsync(id);
        if (ponuda == null) return false;

        _db.PonudePredracuni.Remove(ponuda);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 1-klik konverzija Ponude/Predračuna u izlazni Račun-otpremnicu, spreman za dalje
    /// knjiženje i SEF slanje (RacunOtpremnicaService).
    /// </summary>
    public async Task<(bool Success, string Message, int? RacunId)> PretvoriPonuduURacunAsync(int ponudaId)
    {
        var ponuda = await GetPonudaByIdAsync(ponudaId);
        if (ponuda == null) return (false, "Ponuda ili predračun ne postoji.", null);

        if (ponuda.RacunOtpremnicaId.HasValue && ponuda.RacunOtpremnicaId.Value > 0)
        {
            return (false, $"Ponuda je već pretvorena u račun br. {ponuda.RacunOtpremnicaId}.", ponuda.RacunOtpremnicaId);
        }

        int sledeciBroj = (await _db.RacuniOtpremnice.Select(r => (int?)r.BrojRacuna).MaxAsync() ?? 0) + 1;

        var novRacun = new RacunOtpremnica
        {
            BrojRacuna = sledeciBroj,
            TipDokumenta = TipRacunOtpremnice.Racun,
            DatumRacuna = DateTime.Today,
            RokPlacanja = DateTime.Today.AddDays(15),
            PartnerId = ponuda.PartnerId,
            UkupnoOsnovica = ponuda.UkupnoNeto,
            UkupnoPdv = ponuda.UkupnoPdv,
            UkupnoZaUplatu = ponuda.UkupnoBruto,
            Napomena = $"Automatski kreirano iz {ponuda.VrstaDokumenta} br. {ponuda.BrojDokumenta}. {ponuda.Napomena}",
            IsKnjizen = false
        };

        int rbr = 1;
        foreach (var st in ponuda.Stavke)
        {
            novRacun.Stavke.Add(new RacunOtpremnicaStavka
            {
                RedniBroj = rbr++,
                ArtikalId = st.ArtikalId,
                Kolicina = st.Kolicina,
                ProdajnaCena = st.Cena,
                RabatProcenat = st.RabatProcenat,
                StopaPdv = st.PdvStopa,
                Osnovica = st.IznosNeto,
                IznosPdv = st.IznosPdv,
                Ukupno = st.IznosBruto
            });
        }

        _db.RacuniOtpremnice.Add(novRacun);
        await _db.SaveChangesAsync();

        ponuda.Status = "Fakturisano";
        ponuda.RacunOtpremnicaId = novRacun.RacunOtpremnicaId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno kreiran izlazni račun br. {sledeciBroj} iz {ponuda.VrstaDokumenta}!", novRacun.RacunOtpremnicaId);
    }

    #endregion

    #region Narudžbenice Dobavljačima

    public async Task<List<NarudzbenicaDobavljacu>> GetNarudzbeniceAsync()
    {
        return await _db.NarudzbeniceDobavljacima
            .Include(n => n.Partner)
            .Include(n => n.Magacin)
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .OrderByDescending(n => n.Datum)
            .ThenByDescending(n => n.NarudzbenicaId)
            .ToListAsync();
    }

    public async Task<NarudzbenicaDobavljacu?> GetNarudzbenicaByIdAsync(int id)
    {
        return await _db.NarudzbeniceDobavljacima
            .Include(n => n.Partner)
            .Include(n => n.Magacin)
            .Include(n => n.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NarudzbenicaId == id);
    }

    public async Task<NarudzbenicaDobavljacu> SacuvajNarudzbenicuAsync(NarudzbenicaDobavljacu narudzbenica)
    {
        foreach (var s in narudzbenica.Stavke)
        {
            s.IznosNeto = s.KolicinaNarucena * s.Cena;
            s.IznosPdv = s.IznosNeto * (s.PdvStopa / 100m);
            s.IznosBruto = s.IznosNeto + s.IznosPdv;
        }

        narudzbenica.UkupnoNeto = narudzbenica.Stavke.Sum(s => s.IznosNeto);
        narudzbenica.UkupnoPdv = narudzbenica.Stavke.Sum(s => s.IznosPdv);
        narudzbenica.UkupnoBruto = narudzbenica.Stavke.Sum(s => s.IznosBruto);

        if (narudzbenica.NarudzbenicaId == 0)
        {
            if (string.IsNullOrWhiteSpace(narudzbenica.BrojNarudzbenice))
            {
                int sledeciBroj = await _db.NarudzbeniceDobavljacima.CountAsync() + 1;
                narudzbenica.BrojNarudzbenice = $"NAR-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.NarudzbeniceDobavljacima.Add(narudzbenica);
        }
        else
        {
            var postojeceStavke = _db.NarudzbeniceStavke.Where(s => s.NarudzbenicaId == narudzbenica.NarudzbenicaId);
            _db.NarudzbeniceStavke.RemoveRange(postojeceStavke);

            _db.NarudzbeniceDobavljacima.Update(narudzbenica);
        }

        await _db.SaveChangesAsync();
        return narudzbenica;
    }

    public async Task<bool> ObrisiNarudzbenicuAsync(int id)
    {
        var narudzbenica = await _db.NarudzbeniceDobavljacima.FindAsync(id);
        if (narudzbenica == null) return false;

        _db.NarudzbeniceDobavljacima.Remove(narudzbenica);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 1-klik konverzija Narudžbenice dobavljaču u ulaznu Kalkulaciju. Zahteva da narudžbenica
    /// ima izabran <see cref="NarudzbenicaDobavljacu.MagacinId"/> — ERPi-jeva Kalkulacija je
    /// magacinski vezana (nenullable MagacinId), za razliku od izvornog ERPiFinansije modela.
    /// </summary>
    public async Task<(bool Success, string Message, int? KalkulacijaId)> PretvoriNarudzbenicuUKalkulacijuAsync(int narudzbenicaId)
    {
        var narudzbenica = await GetNarudzbenicaByIdAsync(narudzbenicaId);
        if (narudzbenica == null) return (false, "Narudžbenica ne postoji.", null);

        if (narudzbenica.KalkulacijaId.HasValue && narudzbenica.KalkulacijaId.Value > 0)
        {
            return (false, $"Narudžbenica je već pretvorena u kalkulaciju br. {narudzbenica.KalkulacijaId}.", narudzbenica.KalkulacijaId);
        }

        if (!narudzbenica.MagacinId.HasValue)
        {
            return (false, "Narudžbenica nema izabran magacin prijema — izaberite magacin pre konverzije u kalkulaciju.", null);
        }

        var stavkeBezArtikla = narudzbenica.Stavke.Where(s => !s.ArtikalId.HasValue).ToList();
        if (stavkeBezArtikla.Count > 0)
        {
            return (false, "Sve stavke narudžbenice moraju imati izabran artikal pre konverzije u kalkulaciju.", null);
        }

        int sledeciBroj = (await _db.Kalkulacije.Select(k => (int?)k.BrojKalkulacije).MaxAsync() ?? 0) + 1;

        var novaKalkulacija = new Kalkulacija
        {
            MagacinId = narudzbenica.MagacinId.Value,
            PartnerId = narudzbenica.PartnerId,
            BrojKalkulacije = sledeciBroj,
            Datum = DateTime.Today,
            VrstaKalkulacije = "Veleprodaja",
            UkupnoNabavna = narudzbenica.UkupnoNeto,
            UkupnoProdajna = narudzbenica.UkupnoNeto,
            UkupnoPdv = narudzbenica.UkupnoPdv,
            Napomena = $"Automatski kreirano iz narudžbenice br. {narudzbenica.BrojNarudzbenice}. {narudzbenica.Napomena}"
        };

        foreach (var st in narudzbenica.Stavke)
        {
            novaKalkulacija.Stavke.Add(new StavkaKalkulacije
            {
                ArtikalId = st.ArtikalId!.Value,
                Kolicina = st.KolicinaNarucena,
                NabavnaCena = st.Cena,
                ProdajnaCena = st.Cena,
                PdvStopa = st.PdvStopa,
                IznosNabavni = st.IznosNeto,
                IznosProdajni = st.IznosNeto,
                IznosPdv = st.IznosPdv
            });
        }

        _db.Kalkulacije.Add(novaKalkulacija);
        await _db.SaveChangesAsync();

        narudzbenica.Status = "Završeno";
        narudzbenica.KalkulacijaId = novaKalkulacija.KalkulacijaId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno kreirana ulazna kalkulacija br. {sledeciBroj} iz narudžbenice!", novaKalkulacija.KalkulacijaId);
    }

    #endregion
}
