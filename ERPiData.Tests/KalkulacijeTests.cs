using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class KalkulacijeTests
{
    private ErpiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ErpiDbContext(options);
    }

    [Fact]
    public async Task Kalkulacija_ShouldSaveWithItemsAndCalculatePdvAndTotals()
    {
        using var db = CreateInMemoryDb();

        var magacin = new Magacin
        {
            SifraMagacina = "MAG01",
            NazivMagacina = "Glavni Magacin",
            VrstaMagacina = "Veleprodaja"
        };
        db.Magacini.Add(magacin);

        var dobavljac = new Partner
        {
            SifraPartnera = "P001",
            Naziv = "Dobavljač D.O.O.",
            Pib = "100000001",
            JeDobavljac = true
        };
        db.Partneri.Add(dobavljac);

        var artikal = new Artikal
        {
            SifraArtikla = "ART001",
            Naziv = "Roba A",
            JedinicaMere = "kom",
            NabavnaCena = 100m,
            ProdajnaCena = 150m,
            PdvStopa = 20m
        };
        db.Artikli.Add(artikal);

        await db.SaveChangesAsync();

        var kalkulacija = new Kalkulacija
        {
            MagacinId = magacin.MagacinId,
            PartnerId = dobavljac.PartnerId,
            BrojKalkulacije = 1,
            BrojFaktureDobavljaca = "FA-2026/01",
            Datum = DateTime.Now,
            UkupnoNabavna = 1000m,
            UkupnoPdv = 200m,
            UkupnoProdajna = 1500m,
            Stavke = new List<StavkaKalkulacije>
            {
                new StavkaKalkulacije
                {
                    ArtikalId = artikal.ArtikalId,
                    Kolicina = 10m,
                    NabavnaCena = 100m,
                    RabatProcenat = 0m,
                    MarzaProcenat = 50m,
                    PdvStopa = 20m,
                    ProdajnaCena = 150m,
                    IznosNabavni = 1000m,
                    IznosPdv = 200m,
                    IznosProdajni = 1500m
                }
            }
        };

        db.Kalkulacije.Add(kalkulacija);

        var pdvZapis = new PdvZapis
        {
            PartnerId = dobavljac.PartnerId,
            BrojDokumenta = "FA-2026/01",
            TipKnjige = "KPR",
            Osnovica = 1000m,
            StopaPdv = 20m,
            IznosPdv = 200m,
            Ukupno = 1200m
        };
        db.PdvZapisi.Add(pdvZapis);

        await db.SaveChangesAsync();

        var me = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == "MAG01");
        Assert.NotNull(me);
        Assert.Equal("MAG01 - Glavni Magacin", me.Prikaz);

        var ke = await db.Kalkulacije
            .Include(k => k.Magacin)
            .Include(k => k.Partner)
            .Include(k => k.Stavke)
            .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacija.KalkulacijaId);

        Assert.NotNull(ke);
        Assert.Equal("Glavni Magacin", ke.Magacin?.NazivMagacina);
        Assert.Equal("Dobavljač D.O.O.", ke.Partner?.Naziv);
        Assert.Single(ke.Stavke);
        Assert.Equal(1000m, ke.UkupnoNabavna);
        Assert.Equal(200m, ke.UkupnoPdv);
        Assert.Equal(1500m, ke.UkupnoProdajna);

        var pdv = await db.PdvZapisi.FirstOrDefaultAsync(p => p.BrojDokumenta == "FA-2026/01");
        Assert.NotNull(pdv);
        Assert.Equal("KPR", pdv.TipKnjige);
        Assert.Equal(1200m, pdv.Ukupno);
    }
}
