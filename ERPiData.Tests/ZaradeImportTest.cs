using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERPiData;
using ERPiMigration.Importers;
using ERPiZaradeData;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class ZaradeImportTest
{
    private const string ProductionDbPath = @"C:\Users\Admin\AppData\Local\ERPiZaradeApp\Baze\firma_100188310_PSSS_PIROT_DOO_PIROT.db";

    [Fact]
    public async Task TestImportErpiZaradePsssPirotDatabase()
    {
        if (!File.Exists(ProductionDbPath))
        {
            // Skip if user's file doesn't exist on build agent, but here we run locally
            return;
        }

        // Target SQLite database options
        var targetOptions = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseSqlite($"Data Source=Test_PsssPirot_Import.db")
            .Options;

        // Ensure database created & migrated
        using (var destDb = new ErpiDbContext(targetOptions))
        {
            await destDb.Database.EnsureDeletedAsync();
            await destDb.Database.EnsureCreatedAsync();
        }

        // Source SQLite database options
        var srcOptions = new DbContextOptionsBuilder<PlataDbContext>()
            .UseSqlite($"Data Source={ProductionDbPath}")
            .Options;

        using var srcDb = new PlataDbContext(srcOptions);
        using var destDbFinal = new ErpiDbContext(targetOptions);

        var importer = new ErpiZaradeProdukcijaImporter(destDbFinal);
        var result = await importer.ImportFromDatabaseAsync(srcDb);

        Assert.True(result.Uspesno, $"Import failed with error: {result.Greska}");
        Assert.True(result.UvezenoRadnika > 0, "No workers imported");
        Assert.True(result.UvezenoObracuna > 0, "No calculations imported");

        // Verify company details imported
        var firma = await destDbFinal.Firme.FirstOrDefaultAsync();
        Assert.NotNull(firma);
        Assert.Contains("PSSS", firma.Naziv, StringComparison.OrdinalIgnoreCase);

        // Verify workers mapped to core partners
        var radnici = await destDbFinal.Radnici.Include(r => r.Partner).ToListAsync();
        Assert.NotEmpty(radnici);
        Assert.All(radnici, r => Assert.NotNull(r.Partner));

        // Clean up test DB file
        destDbFinal.Database.EnsureDeleted();
    }
}
