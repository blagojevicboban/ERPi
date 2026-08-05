using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERPiData;

/// <summary>
/// Koristi se isključivo od strane "dotnet ef" alata pri generisanju migracija.
/// Pokazuje na privremenu bazu jer se šema izvodi iz modela, ne iz podataka.
/// </summary>
public class ErpiDbContextFactory : IDesignTimeDbContextFactory<ErpiDbContext>
{
    public ErpiDbContext CreateDbContext(string[] args)
    {
        var putanja = Path.Combine(Path.GetTempPath(), "erpi_designtime.db");

        var optionsBuilder = new DbContextOptionsBuilder<ErpiDbContext>();
        optionsBuilder.UseSqlite($"Data Source={putanja}");

        return new ErpiDbContext(optionsBuilder.Options);
    }
}
