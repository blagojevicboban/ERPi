using System;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiData.Tests;

/// <summary>
/// Pokriva servise portovane iz ERPiFinansijeData u sesiji od 05.08.2026 (vidi
/// PLAN_NASTAVKA.md §3g) — prosečna (ponderisana) cena na materijalnoj kartici i
/// knjiženje/rasknjiženje Ulaza i Trebovanja preko nje.
/// </summary>
public class RobnoMaterijalnoTests
{
    private ErpiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ErpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ErpiDbContext(options);
    }

    private static async Task<(Magacin magacin, Materijal materijal)> SeedAsync(ErpiDbContext db)
    {
        var magacin = new Magacin { SifraMagacina = "MAT01", NazivMagacina = "Magacin materijala", VrstaMagacina = "Materijalni" };
        var materijal = new Materijal { SifraArtikla = "M001", Naziv = "Čelična šipka", JedinicaMere = "kg" };
        db.Magacini.Add(magacin);
        db.Materijali.Add(materijal);
        await db.SaveChangesAsync();
        return (magacin, materijal);
    }

    [Fact]
    public async Task MaterijalnaKarticaService_DodajUlazPaIzlaz_RacunaPonderisanuProsecnuCenu()
    {
        using var db = CreateInMemoryDb();
        var (magacin, materijal) = await SeedAsync(db);
        var kartice = new MaterijalnaKarticaService(db);

        // 100 kom po 10 + 100 kom po 20 => prosečna cena (1000+2000)/200 = 15
        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Test ulaz 1", 100m, 10m);
        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Test ulaz 2", 100m, 20m);

        var (stanje, saldo) = await kartice.GetTrenutnoStanjeAsync(magacin.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(200m, stanje);
        Assert.Equal(3000m, saldo);

        // Izdavanje 50 kom mora ići po trenutnoj prosečnoj ceni (15), ne po poslednjoj unetoj (20)
        decimal iznosIzlaza = await kartice.DodajIzlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Test izlaz", 50m);
        Assert.Equal(750m, iznosIzlaza);

        var (stanjePosle, saldoPosle) = await kartice.GetTrenutnoStanjeAsync(magacin.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(150m, stanjePosle);
        Assert.Equal(2250m, saldoPosle);
    }

    [Fact]
    public async Task MaterijalnaKarticaService_Izlaz_BacaGreskuAkoPreOdeUMinus()
    {
        using var db = CreateInMemoryDb();
        var (magacin, materijal) = await SeedAsync(db);
        var kartice = new MaterijalnaKarticaService(db);

        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Ulaz", 10m, 5m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => kartice.DodajIzlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Prevelik izlaz", 11m));
    }

    [Fact]
    public async Task UlazService_Knjizenje_UpisujeKarticuIZakljucavaIzmene()
    {
        using var db = CreateInMemoryDb();
        var (magacin, materijal) = await SeedAsync(db);

        var ulaz = new UlazNalog { BrojNaloga = 1, Datum = DateTime.Today, MagacinId = magacin.MagacinId };
        ulaz.Stavke.Add(new UlazStavka { RedniBroj = 1, MaterijalId = materijal.MaterijalId, Kolicina = 20m, Cena = 30m, Iznos = 600m });

        var ulazService = new UlazService(db);
        await ulazService.SaveUlazAsync(ulaz);
        await ulazService.KnjiziUlazAsync(ulaz.UlazNalogId);

        var kartice = new MaterijalnaKarticaService(db);
        var (stanje, saldo) = await kartice.GetTrenutnoStanjeAsync(magacin.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(20m, stanje);
        Assert.Equal(600m, saldo);

        // Već proknjižen — nova izmena ili ponovno knjiženje mora pući
        await Assert.ThrowsAsync<InvalidOperationException>(() => ulazService.KnjiziUlazAsync(ulaz.UlazNalogId));
    }

    [Fact]
    public async Task TrebovanjeService_Knjizenje_IzdajePoTrenutnojProsecnojCeni()
    {
        using var db = CreateInMemoryDb();
        var (magacin, materijal) = await SeedAsync(db);

        // Prethodni ulaz da postoji zaliha po ceni 40
        await new MaterijalnaKarticaService(db).DodajUlazRedAsync(magacin.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Početna zaliha", 50m, 40m);

        var trebovanje = new TrebovanjeNalog { BrojNaloga = 1, Datum = DateTime.Today, MagacinId = magacin.MagacinId };
        trebovanje.Stavke.Add(new TrebovanjeStavka { RedniBroj = 1, MaterijalId = materijal.MaterijalId, Kolicina = 10m, KontoTroska = "5130" });

        var trebService = new TrebovanjeService(db);
        await trebService.SaveTrebovanjeAsync(trebovanje);
        await trebService.KnjiziTrebovanjeAsync(trebovanje.TrebovanjeNalogId);

        var kartice = new MaterijalnaKarticaService(db);
        var (stanje, saldo) = await kartice.GetTrenutnoStanjeAsync(magacin.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(40m, stanje); // 50 - 10
        Assert.Equal(1600m, saldo); // 2000 - (10*40)

        // Rasknjiženje mora vratiti tačno na prethodno stanje
        await trebService.RasknjiziTrebovanjeAsync(trebovanje.TrebovanjeNalogId);
        var (stanjeVraceno, saldoVraceno) = await kartice.GetTrenutnoStanjeAsync(magacin.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(50m, stanjeVraceno);
        Assert.Equal(2000m, saldoVraceno);
    }
}
