using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Finansije;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

public class NaloziTests
{
    private ErpiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ErpiDbContext(options);
    }

    [Fact]
    public async Task Nalog_ShouldCalculateTotalsAndBalanceCorrectly()
    {
        using var db = CreateInMemoryDb();

        var nalog = new Nalog
        {
            BrojNaloga = 101,
            DatumNaloga = DateTime.Now,
            VrstaNaloga = "Finansijski",
            Opis = "Test nalog glavne knjige",
            UkupnoDuguje = 15000m,
            UkupnoPotrazuje = 15000m,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { RedniBroj = 1, Duguje = 15000m, Potrazuje = 0m, Opis = "Duguje stavka" },
                new StavkaNaloga { RedniBroj = 2, Duguje = 0m, Potrazuje = 15000m, Opis = "Potražuje stavka" }
            }
        };

        db.Nalozi.Add(nalog);
        await db.SaveChangesAsync();

        var sačuvani = await db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == nalog.NalogId);

        Assert.NotNull(sačuvani);
        Assert.Equal(15000m, sačuvani.UkupnoDuguje);
        Assert.Equal(15000m, sačuvani.UkupnoPotrazuje);
        Assert.Equal(0m, sačuvani.Saldo);
        Assert.True(sačuvani.IsUravnotezen);
        Assert.Equal(2, sačuvani.Stavke.Count);
    }

    [Fact]
    public void Nalog_IsNotBalanced_WhenDugujeAndPotrazujeDoNotMatch()
    {
        var nalog = new Nalog
        {
            UkupnoDuguje = 1000m,
            UkupnoPotrazuje = 500m
        };

        Assert.Equal(500m, nalog.Saldo);
        Assert.False(nalog.IsUravnotezen);
    }
}
