using System;
using System.Threading.Tasks;
using ERPiData;
using ERPiFinansijeData;
using ERPiMigration.Importers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class FinansijeImportTests
{
    private AccountingDbContext CreateSrcInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;

        return new AccountingDbContext(options);
    }

    private ErpiDbContext CreateDestInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;

        return new ErpiDbContext(options);
    }

    [Fact]
    public async Task ImportFromDatabase_ShouldTransferEntitiesAndMapForeignKeysCorrectly()
    {
        var dbNameSrc = Guid.NewGuid().ToString();
        var dbNameDest = Guid.NewGuid().ToString();

        using var srcDb = CreateSrcInMemoryDb(dbNameSrc);
        using var destDb = CreateDestInMemoryDb(dbNameDest);

        // Populate source DB
        srcDb.Konta.Add(new ERPiFinansijeData.Models.Konto { BrojKonta = "2040", NazivKonta = "Kupci u zemlji", IsSintetika = false });
        srcDb.Partneri.Add(new ERPiFinansijeData.Models.Partner { SifraPartnera = "P001", Naziv = "Partner Test", Pib = "123456789" });
        srcDb.Magacini.Add(new ERPiFinansijeData.Models.Magacin { SifraMagacina = "M1", NazivMagacina = "Magacin 1", VrstaMagacina = "Veleprodaja" });
        srcDb.Artikli.Add(new ERPiFinansijeData.Models.Artikal { SifraArtikla = "A1", Naziv = "Artikal 1", JedinicaMere = "kom", NabavnaCena = 10m, ProdajnaCena = 15m });

        await srcDb.SaveChangesAsync();

        var nalog = new ERPiFinansijeData.Models.Nalog
        {
            BrojNaloga = 501,
            DatumNaloga = DateTime.Now,
            VrstaNaloga = "Finansijski",
            Opis = "Test uvoza",
            UkupnoDuguje = 100m,
            UkupnoPotrazuje = 100m,
            IsKnjizen = true
        };
        nalog.Stavke.Add(new ERPiFinansijeData.Models.StavkaNaloga
        {
            RedniBroj = 1,
            BrojKonta = "2040",
            Duguje = 100m,
            Potrazuje = 0m,
            Opis = "Duguje stavka"
        });
        srcDb.Nalozi.Add(nalog);
        await srcDb.SaveChangesAsync();

        // Perform Import
        var importer = new ErpiFinansijeImporter(destDb);
        var result = await importer.ImportFromDatabaseAsync(srcDb);

        Assert.True(result.Success);
        Assert.Equal(1, result.UvezenoKonta);
        Assert.Equal(1, result.UvezenoPartnera);
        Assert.Equal(1, result.UvezenoMagacina);
        Assert.Equal(1, result.UvezenoArtikala);
        Assert.Equal(1, result.UvezenoNaloga);

        // Verify foreign key resolution
        var importedNalog = await destDb.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.BrojNaloga == 501);
        Assert.NotNull(importedNalog);
        Assert.Single(importedNalog.Stavke);

        var importedStavka = importedNalog.Stavke[0];
        var importedKonto = await destDb.Konta.FindAsync(importedStavka.KontoId);
        Assert.NotNull(importedKonto);
        Assert.Equal("2040", importedKonto.BrojKonta);
    }
}
