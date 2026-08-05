using System;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class PdvTests
{
    private ErpiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpiDbContext(options);
    }

    [Fact]
    public async Task PdvService_GetKirZapisi_VracaProknjizeneRacune()
    {
        using var db = CreateInMemoryDb();
        var partner = new Partner { SifraPartnera = "KUP1", Naziv = "Kupac Test D.O.O.", Pib = "109876543" };
        var magacin = new Magacin { SifraMagacina = "M1", NazivMagacina = "Glavni" };
        db.Partneri.Add(partner);
        db.Magacini.Add(magacin);
        await db.SaveChangesAsync();

        var racun = new RacunOtpremnica
        {
            BrojRacuna = 1,
            DatumRacuna = DateTime.Today,
            PartnerId = partner.PartnerId,
            MagacinId = magacin.MagacinId,
            UkupnoZaUplatu = 120000m,
            IsKnjizen = true
        };
        racun.Stavke.Add(new RacunOtpremnicaStavka
        {
            RedniBroj = 1,
            Kolicina = 1,
            ProdajnaCena = 100000m,
            Osnovica = 100000m,
            StopaPdv = 20m,
            IznosPdv = 20000m,
            Ukupno = 120000m
        });

        db.RacuniOtpremnice.Add(racun);
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var kirZapisi = await service.GetKirZapisiAsync();

        Assert.Single(kirZapisi);
        Assert.Equal("1", kirZapisi[0].BrojDokumenta);
        Assert.Equal(100000m, kirZapisi[0].Osnovica20);
        Assert.Equal(20000m, kirZapisi[0].Pdv20);
        Assert.Equal(120000m, kirZapisi[0].UkupnaNaknadaSaPdv);
    }

    [Fact]
    public async Task PdvService_GenerisiPpPdvXml_GeneriseValidanXmlString()
    {
        using var db = CreateInMemoryDb();
        var firma = new Firma { Naziv = "ARHIBEL D.O.O.", Pib = "101234567", Adresa = "Knez Mihailova 1" };
        db.Firme.Add(firma);
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var (success, message, xml) = await service.GenerisiPpPdvXmlAsync(DateTime.Today.AddDays(-30), DateTime.Today);

        Assert.True(success);
        Assert.Contains("101234567", xml);
        Assert.Contains("ARHIBEL D.O.O.", xml);
        Assert.Contains("PoreskaPrijavaPPPDV", xml);
    }
}
