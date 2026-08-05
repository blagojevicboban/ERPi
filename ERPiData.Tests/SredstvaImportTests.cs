using ERPiData.Models.Sredstva;
using ERPiMigration.Importers;
using ERPiSredstvaData;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class SredstvaImportTests
{
    [Fact]
    public async Task ImportFromDatabaseAsync_SkipsEmptyAndZeroSifraSredstva()
    {
        // Arrange: Kreiramo InMemory izvornu (SredstvaDbContext) i ciljnu (ErpiDbContext) bazu
        var srcOptions = new DbContextOptionsBuilder<SredstvaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var destOptions = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var srcDb = new SredstvaDbContext(srcOptions);
        using var destDb = new ErpiDbContext(destOptions);

        // Dodajemo testna sredstva: jedno ispravno, jedno sa LegacySifra 0, jedno bez Naziva
        srcDb.Sredstva.AddRange(
            new ERPiSredstvaData.Models.Sredstvo
            {
                Id = 1,
                LegacySifra = 101,
                InventarskiBroj = "101",
                Naziv = "Frižider 240L",
                NabavnaVrednost = 30000m,
                IspravkaVrednosti = 5000m,
                SadasnjaVrednost = 25000m
            },
            new ERPiSredstvaData.Models.Sredstvo
            {
                Id = 2,
                LegacySifra = 0,
                InventarskiBroj = "0",
                Naziv = "", // Prazno sredstvo sa sifrom 0
                NabavnaVrednost = 0m,
                IspravkaVrednosti = 0m,
                SadasnjaVrednost = 0m
            },
            new ERPiSredstvaData.Models.Sredstvo
            {
                Id = 3,
                LegacySifra = 102,
                InventarskiBroj = "102",
                Naziv = "   ", // Prazan naziv
                NabavnaVrednost = 0m
            }
        );
        await srcDb.SaveChangesAsync();

        // Act
        var importer = new ErpiSredstvaProdukcijaImporter(destDb);
        var result = await importer.ImportFromDatabaseAsync(srcDb);

        // Assert
        Assert.True(result.Uspesno);
        Assert.Equal(1, result.UvezenoSredstava);

        var uvezenaSredstva = await destDb.Sredstva.ToListAsync();
        Assert.Single(uvezenaSredstva);
        Assert.Equal(101, uvezenaSredstva[0].LegacySifra);
        Assert.Equal("Frižider 240L", uvezenaSredstva[0].Naziv);
    }
}
