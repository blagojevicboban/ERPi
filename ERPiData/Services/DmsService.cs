using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za Dokument Menadžment Sistem (DMS) — skladištenje i vezivanje priloženih skeniranih slika/PDF dokumenata.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class DmsService
{
    private readonly ErpiDbContext _db;

    public DmsService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<List<DokumentPrilog>> GetPriloziZaNalogAsync(int nalogId)
    {
        return await _db.DokumentiPrilozi
            .Where(d => d.NalogId == nalogId)
            .OrderByDescending(d => d.DatumPriloga)
            .ToListAsync();
    }

    public async Task<int> GetBrojPrilogaZaNalogAsync(int nalogId)
    {
        return await _db.DokumentiPrilozi.CountAsync(d => d.NalogId == nalogId);
    }

    public async Task<(bool Success, string Message, DokumentPrilog? Prilog)> DodajPrilogAsync(
        int? nalogId,
        int? racunId,
        int? kalkulacijaId,
        string izvornaPutanja,
        string tipDokumenta = "Ulazni Račun",
        string korisnik = "Admin")
    {
        if (string.IsNullOrWhiteSpace(izvornaPutanja) || !File.Exists(izvornaPutanja))
            return (false, "Izvorni fajl ne postoji na navedenoj putanji.", null);

        try
        {
            string dmsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMS", "Dokumenti");
            Directory.CreateDirectory(dmsFolder);

            string ekstenzija = Path.GetExtension(izvornaPutanja);
            string noviNaziv = $"DMS_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}{ekstenzija}";
            string ciljnaPutanja = Path.Combine(dmsFolder, noviNaziv);

            File.Copy(izvornaPutanja, ciljnaPutanja, overwrite: true);

            var fi = new FileInfo(ciljnaPutanja);

            var prilog = new DokumentPrilog
            {
                NalogId = nalogId,
                RacunOtpremnicaId = racunId,
                KalkulacijaId = kalkulacijaId,
                NazivFajla = Path.GetFileName(izvornaPutanja),
                TipDokumenta = tipDokumenta,
                PutanjaFajla = ciljnaPutanja,
                VelicinaBytes = fi.Length,
                DatumPriloga = DateTime.Now,
                Korisnik = korisnik
            };

            _db.DokumentiPrilozi.Add(prilog);
            await _db.SaveChangesAsync();

            return (true, "Dokument je uspešno priložen u DMS.", prilog);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri priključivanju dokumenta: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> ObrisiPrilogAsync(int prilogId)
    {
        var prilog = await _db.DokumentiPrilozi.FindAsync(prilogId);
        if (prilog == null) return (false, "Prilog nije pronađen u bazi.");

        try
        {
            if (File.Exists(prilog.PutanjaFajla))
            {
                File.Delete(prilog.PutanjaFajla);
            }

            _db.DokumentiPrilozi.Remove(prilog);
            await _db.SaveChangesAsync();

            return (true, "Prilog je obrisan.");
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri brisanju: {ex.Message}");
        }
    }
}
