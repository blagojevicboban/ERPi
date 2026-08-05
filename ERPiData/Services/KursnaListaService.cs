using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

public class KursnaListaService
{
    private readonly ErpiDbContext _db;
    private readonly NbsApiClient _nbsClient;

    public KursnaListaService(ErpiDbContext db, NbsApiClient? nbsClient = null)
    {
        _db = db;
        _nbsClient = nbsClient ?? new NbsApiClient();
    }

    public async Task<List<KursnaListaStavka>> GetKursnaListaZaDatumAsync(DateTime datum)
    {
        var targetDate = datum.Date;
        var postojeci = await _db.KursneListeStavke
            .Where(k => k.Datum == targetDate)
            .OrderBy(k => k.ValutaOznaka)
            .ToListAsync();

        if (postojeci.Count > 0)
            return postojeci;

        var noviKursevi = await _nbsClient.PreuzmiKursnuListuAsync(targetDate);
        if (noviKursevi.Count > 0)
        {
            _db.KursneListeStavke.AddRange(noviKursevi);
            await _db.SaveChangesAsync();
        }

        return noviKursevi;
    }

    public async Task<List<KursnaListaStavka>> OsveziSaNbsAsync(DateTime datum)
    {
        var targetDate = datum.Date;

        var novi = await _nbsClient.PreuzmiKursnuListuAsync(targetDate);
        if (novi.Count == 0)
            return new List<KursnaListaStavka>();

        var stari = await _db.KursneListeStavke.Where(k => k.Datum == targetDate).ToListAsync();
        if (stari.Count > 0)
            _db.KursneListeStavke.RemoveRange(stari);

        _db.KursneListeStavke.AddRange(novi);
        await _db.SaveChangesAsync();

        return novi;
    }

    public async Task<decimal> PretvoriDevizeURsdAsync(decimal iznos, string valutaOznaka, DateTime datum)
    {
        if (string.IsNullOrWhiteSpace(valutaOznaka) || valutaOznaka.Equals("RSD", StringComparison.OrdinalIgnoreCase))
            return iznos;

        var kursevi = await GetKursnaListaZaDatumAsync(datum);
        var stavka = kursevi.FirstOrDefault(k => k.ValutaOznaka.Equals(valutaOznaka, StringComparison.OrdinalIgnoreCase));

        if (stavka == null || stavka.SrednjiKurs <= 0)
            throw new InvalidOperationException(
                $"Ne postoji kurs za valutu {valutaOznaka} na dan {datum:dd.MM.yyyy}. " +
                "Preuzmite kursnu listu sa NBS-a ili je unesite ručno pre knjiženja.");

        return Math.Round((iznos * stavka.SrednjiKurs) / stavka.Jedinica, 2);
    }
}
