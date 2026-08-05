using System;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class SefPfrTests
{
    private ErpiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ErpiDbContext(options);
    }

    [Fact]
    public async Task SefDokument_ShouldSaveAndTransitionStatus()
    {
        using var db = CreateInMemoryDb();

        var kupac = new Partner
        {
            SifraPartnera = "K001",
            Naziv = "Kupac D.O.O.",
            Pib = "109876543",
            JeKupac = true
        };
        db.Partneri.Add(kupac);
        await db.SaveChangesAsync();

        var sefFaktura = new SefDokument
        {
            PartnerId = kupac.PartnerId,
            BrojDokumenta = "SEF-2026-001",
            DatumDokumenta = DateTime.Now,
            TipDokumenta = "Faktura",
            Status = "Nacrt",
            Osnovica = 10000m,
            IznosPdv = 2000m,
            Ukupno = 12000m
        };
        db.SefDokumenti.Add(sefFaktura);
        await db.SaveChangesAsync();

        var sačuvana = await db.SefDokumenti.Include(s => s.Partner).FirstOrDefaultAsync(s => s.SefDokumentId == sefFaktura.SefDokumentId);
        Assert.NotNull(sačuvana);
        Assert.Equal("SEF-2026-001", sačuvana.BrojDokumenta);
        Assert.Equal("Kupac D.O.O.", sačuvana.Partner?.Naziv);
        Assert.Equal("Nacrt", sačuvana.Status);

        sačuvana.Status = "Poslato";
        sačuvana.CirId = "CIR-12345678";
        sačuvana.DatumSlanja = DateTime.Now;
        await db.SaveChangesAsync();

        var osvežena = await db.SefDokumenti.FindAsync(sefFaktura.SefDokumentId);
        Assert.Equal("Poslato", osvežena?.Status);
        Assert.Equal("CIR-12345678", osvežena?.CirId);
    }

    [Fact]
    public async Task PfrRacun_ShouldSaveFiscalReceipt()
    {
        using var db = CreateInMemoryDb();

        var racun = new PfrRacun
        {
            BrojRacuna = "PFR-2026-100",
            Datum = DateTime.Now,
            Iznos = 5000m,
            TipRacuna = "PrometProdaja",
            Status = "Fiskalizovan",
            PfrBroj = "PFR-RS-ABC123XYZ",
            QrKodUrl = "https://suf.puris.gov.rs/v/?vl=PFR-RS-ABC123XYZ"
        };
        db.PfrRacuni.Add(racun);
        await db.SaveChangesAsync();

        var sačuvani = await db.PfrRacuni.FirstOrDefaultAsync(p => p.BrojRacuna == "PFR-2026-100");
        Assert.NotNull(sačuvani);
        Assert.Equal("Fiskalizovan", sačuvani.Status);
        Assert.Equal("PFR-RS-ABC123XYZ", sačuvani.PfrBroj);
    }
}
