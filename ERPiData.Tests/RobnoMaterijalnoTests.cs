using System;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
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

    [Fact]
    public async Task PrimopredajaService_KnjizenjeIRasknjizenje_PrenosiZaliheIzmedjuMagacina()
    {
        using var db = CreateInMemoryDb();
        var magDaje = new Magacin { SifraMagacina = "MAG1", NazivMagacina = "Magacin 1", VrstaMagacina = "Veleprodaja" };
        var magPrima = new Magacin { SifraMagacina = "MAG2", NazivMagacina = "Magacin 2", VrstaMagacina = "Veleprodaja" };
        var materijal = new Materijal { SifraArtikla = "M100", Naziv = "Bakarni kabl", JedinicaMere = "m" };
        db.Magacini.AddRange(magDaje, magPrima);
        db.Materijali.Add(materijal);
        await db.SaveChangesAsync();

        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magDaje.SifraMagacina, materijal.SifraArtikla, DateTime.Today, "Prijem u MAG1", 100m, 50m);

        var primopredaja = new PrimopredajaNalog
        {
            BrojNaloga = 1,
            Datum = DateTime.Today,
            MagacinIdDaje = magDaje.MagacinId,
            MagacinIdPrima = magPrima.MagacinId
        };
        primopredaja.Stavke.Add(new PrimopredajaStavka
        {
            RedniBroj = 1,
            MaterijalId = materijal.MaterijalId,
            Kolicina = 30m
        });

        var service = new PrimopredajaService(db);
        await service.SavePrimopredajuAsync(primopredaja);
        await service.KnjiziPrimopredajuAsync(primopredaja.PrimopredajaNalogId);

        var (stanjeDaje, saldoDaje) = await kartice.GetTrenutnoStanjeAsync(magDaje.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(70m, stanjeDaje);
        Assert.Equal(3500m, saldoDaje);

        var (stanjePrima, saldoPrima) = await kartice.GetTrenutnoStanjeAsync(magPrima.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(30m, stanjePrima);
        Assert.Equal(1500m, saldoPrima);

        // Rasknjižavanje primopredaje
        await service.RasknjiziPrimopredajuAsync(primopredaja.PrimopredajaNalogId);
        var (stanjeDajeVraceno, _) = await kartice.GetTrenutnoStanjeAsync(magDaje.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(100m, stanjeDajeVraceno);

        var (stanjePrimaVraceno, _) = await kartice.GetTrenutnoStanjeAsync(magPrima.SifraMagacina, materijal.SifraArtikla);
        Assert.Equal(0m, stanjePrimaVraceno);
    }

    [Fact]
    public async Task RacunOtpremnicaService_Knjizenje_RazduzujeKarticuIKreiraNalogProdaje()
    {
        using var db = CreateInMemoryDb();
        var magacin = new Magacin { SifraMagacina = "VP1", NazivMagacina = "Veleprodajni magacin", VrstaMagacina = "Veleprodaja" };
        var partner = new Partner { SifraPartnera = "P001", Naziv = "Kupac Test D.O.O." };
        var artikal = new Artikal { SifraArtikla = "A001", Naziv = "Televizor 55\"", JedinicaMere = "kom", ProdajnaCena = 60000m };
        var kontoKupci = new Konto { BrojKonta = "2040", NazivKonta = "Kupci u zemlji" };
        var kontoPrihod = new Konto { BrojKonta = "6120", NazivKonta = "Prihodi od prodaje robe na domaćem tržištu" };
        var kontoPdv = new Konto { BrojKonta = "4700", NazivKonta = "Obračunati PDV" };

        db.Magacini.Add(magacin);
        db.Partneri.Add(partner);
        db.Artikli.Add(artikal);
        db.Konta.AddRange(kontoKupci, kontoPrihod, kontoPdv);
        await db.SaveChangesAsync();

        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, artikal.SifraArtikla, DateTime.Today, "Početna zaliha", 10m, 40000m);

        var racun = new RacunOtpremnica
        {
            BrojRacuna = 1001,
            DatumRacuna = DateTime.Today,
            PartnerId = partner.PartnerId,
            MagacinId = magacin.MagacinId,
            KontoKupcaId = kontoKupci.KontoId
        };
        racun.Stavke.Add(new RacunOtpremnicaStavka
        {
            RedniBroj = 1,
            ArtikalId = artikal.ArtikalId,
            Kolicina = 2m,
            ProdajnaCena = 60000m,
            StopaPdv = 20m
        });

        var service = new RacunOtpremnicaService(db);
        await service.SaveRacunAsync(racun);

        Assert.Equal(120000m, racun.UkupnoOsnovica);
        Assert.Equal(24000m, racun.UkupnoPdv);
        Assert.Equal(144000m, racun.UkupnoZaUplatu);

        await service.KnjiziRacunAsync(racun.RacunOtpremnicaId);

        var osvezenRacun = await service.GetRacunByIdAsync(racun.RacunOtpremnicaId);
        Assert.NotNull(osvezenRacun);
        Assert.True(osvezenRacun!.IsKnjizen);
        Assert.NotNull(osvezenRacun.NalogId);
    }

    [Fact]
    public async Task NivelacijaService_Knjizenje_AzuriraCenuArtiklaIKreiraNalog()
    {
        using var db = CreateInMemoryDb();
        var magacin = new Magacin { SifraMagacina = "MAG1", NazivMagacina = "Glavni magacin", VrstaMagacina = "Veleprodaja" };
        var artikal = new Artikal { SifraArtikla = "A002", Naziv = "Frižider", ProdajnaCena = 50000m };
        var kontoRoba = new Konto { BrojKonta = "1320", NazivKonta = "Roba u magacinu" };
        var kontoRazlika = new Konto { BrojKonta = "1329", NazivKonta = "Razlika u ceni" };

        db.Magacini.Add(magacin);
        db.Artikli.Add(artikal);
        db.Konta.AddRange(kontoRoba, kontoRazlika);
        await db.SaveChangesAsync();

        var nivelacija = new NivelacijaCena
        {
            BrojNivelacije = 1,
            DatumNivelacije = DateTime.Today,
            MagacinId = magacin.MagacinId,
            Opis = "Povećanje cena radijatora"
        };
        nivelacija.Stavke.Add(new NivelacijaStavka
        {
            RedniBroj = 1,
            ArtikalId = artikal.ArtikalId,
            KolicinaZaliha = 5m,
            StaraCena = 50000m,
            NovaCena = 55000m,
            RazlikaPoJedinici = 5000m,
            UkupnaRazlika = 25000m
        });

        var service = new NivelacijaService(db);
        await service.SaveNivelacijaAsync(nivelacija);
        await service.KnjiziNivelacijuAsync(nivelacija.NivelacijaCenaId);

        var osvezena = await service.GetNivelacijaByIdAsync(nivelacija.NivelacijaCenaId);
        Assert.NotNull(osvezena);
        Assert.True(osvezena!.IsKnjizen);
        Assert.Equal(55000m, artikal.ProdajnaCena);
    }

    [Fact]
    public async Task UvoznaKalkulacijaService_ProracunZavisnihTroskova_RacunaPraspedicijuICarinu()
    {
        using var db = CreateInMemoryDb();
        var inoPartner = new Partner { SifraPartnera = "INO1", Naziv = "Ino Supplier GMBH" };
        var magacin = new Magacin { SifraMagacina = "UVOZ1", NazivMagacina = "Carinsko skladište" };
        var artikal = new Artikal { SifraArtikla = "IMP1", Naziv = "Mašina za pakovanje" };

        db.Partneri.Add(inoPartner);
        db.Magacini.Add(magacin);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        var uvoz = new UvoznaKalkulacija
        {
            BrojKalkulacije = "UV-2026-01",
            DatumKalkulacije = DateTime.Today,
            InoPartnerId = inoPartner.PartnerId,
            InoBrojFakture = "INV-99",
            Valuta = "EUR",
            KursValute = 117m,
            SpedicijaRsd = 10000m,
            PrevozRsd = 20000m,
            MagacinId = magacin.MagacinId
        };
        uvoz.Stavke.Add(new UvoznaStavka
        {
            ArtikalId = artikal.ArtikalId,
            Kolicina = 10m,
            InoCenaDevize = 100m,
            CarinaProcenat = 10m
        });

        var service = new UvoznaKalkulacijaService(db);
        await service.SaveUvozAsync(uvoz);

        Assert.Equal(1000m, uvoz.UkupnoDevize);
        Assert.Equal(117000m, uvoz.UkupnoFakturaRsd);
        Assert.Equal(11700m, uvoz.CarinaRsd);
        Assert.Equal(158700m, uvoz.UkupnaNabavnaVrednostRsd); // 117000 + 11700 + 30000
    }

    [Fact]
    public async Task KalkulacijaService_IzracunajSaStavkama_RaspodeljujeZavisneTroskoviIZapisujeUGlavnuKnjigu()
    {
        using var db = CreateInMemoryDb();
        var partner = new Partner { SifraPartnera = "SUP1", Naziv = "Domaći Dobavljač DOO" };
        var magacin = new Magacin { SifraMagacina = "VP01", NazivMagacina = "Veleprodaja" };
        var artikal1 = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        var artikal2 = new Artikal { SifraArtikla = "A2", Naziv = "Artikal 2" };
        var kontoDob = new Konto { BrojKonta = "4350", NazivKonta = "Dobavljači u zemlji" };
        var robniKonto = new Konto { BrojKonta = "1320", NazivKonta = "Roba u veleprodaji" };

        db.Partneri.Add(partner);
        db.Magacini.Add(magacin);
        db.Artikli.AddRange(artikal1, artikal2);
        db.Konta.AddRange(kontoDob, robniKonto);
        await db.SaveChangesAsync();

        var kalk = new Kalkulacija
        {
            BrojKalkulacije = 101,
            Datum = DateTime.Today,
            MagacinId = magacin.MagacinId,
            PartnerId = partner.PartnerId,
            KontoDobavljacaId = kontoDob.KontoId,
            BrojRacuna = "INV-101",
            TransportniTroskovi = 3000m,
            TroskoviUskladistenja = 2000m, // ukupno zavisnih troškova = 5000
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m
        };

        // Stavka 1: 100 * 100 = 10,000 RSD (50% učešća -> 2,500 RSD troškova)
        // Stavka 2: 100 * 100 = 10,000 RSD (50% učešća -> 2,500 RSD troškova)
        kalk.Stavke.Add(new StavkaKalkulacije { ArtikalId = artikal1.ArtikalId, Kolicina = 100m, NabavnaCena = 100m });
        kalk.Stavke.Add(new StavkaKalkulacije { ArtikalId = artikal2.ArtikalId, Kolicina = 100m, NabavnaCena = 100m });

        KalkulacijaService.IzracunajSaStavkama(kalk);

        Assert.Equal(5000m, kalk.SvegaTroskovi);
        Assert.Equal(20000m, kalk.NabavnaVrednost);
        Assert.Equal(25000m, kalk.SvegaNabavno); // 20000 + 5000
        Assert.Equal(2500m, kalk.Razlika); // 10% od 25000
        Assert.Equal(5500m, kalk.Porez); // 20% od (25000 + 2500)
        Assert.Equal(33000m, kalk.ProdajnaVrednost); // 25000 + 2500 + 5500

        Assert.Equal(2500m, kalk.Stavke[0].Troskovi);
        Assert.Equal(2500m, kalk.Stavke[1].Troskovi);
        Assert.Equal(12500m, kalk.Stavke[0].NabavnaVrednost);

        var service = new KalkulacijaService(db);
        await service.SaveKalkulacijuAsync(kalk);
        await service.KnjiziKalkulacijuAsync(kalk.KalkulacijaId);

        var osvezena = await service.GetKalkulacijaByIdAsync(kalk.KalkulacijaId);
        Assert.NotNull(osvezena);
        Assert.True(osvezena!.IsKnjizen);
        Assert.NotNull(osvezena.NalogId);
    }

    [Fact]
    public async Task MaloprodajnaKalkulacijaService_IzracunajSaStavkama_RaspodeljujeZavisneTroskoviIZapisujeUGlavnuKnjigu()
    {
        using var db = CreateInMemoryDb();
        var partner = new Partner { SifraPartnera = "P1", Naziv = "Test Dobavljač", Pib = "100000001" };
        var magacin = new Magacin { SifraMagacina = "MP1", NazivMagacina = "Prodavnica 1", VrstaMagacina = "Maloprodaja" };
        var artikal1 = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1", JedinicaMere = "kom", NabavnaCena = 100m };
        var artikal2 = new Artikal { SifraArtikla = "A2", Naziv = "Artikal 2", JedinicaMere = "kom", NabavnaCena = 100m };
        var kontoDob = new Konto { BrojKonta = "4350", NazivKonta = "Dobavljači u zemlji" };
        var kontoRobaMP = new Konto { BrojKonta = "1340", NazivKonta = "Roba u maloprodaji" };
        var kontoPdvMP = new Konto { BrojKonta = "1344", NazivKonta = "Ukalkulisani PDV" };
        var kontoRazlikaMP = new Konto { BrojKonta = "1348", NazivKonta = "Ukalkulisana razlika u ceni" };

        db.Partneri.Add(partner);
        db.Magacini.Add(magacin);
        db.Artikli.AddRange(artikal1, artikal2);
        db.Konta.AddRange(kontoDob, kontoRobaMP, kontoPdvMP, kontoRazlikaMP);
        await db.SaveChangesAsync();

        var kalk = new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 201,
            Datum = DateTime.Today,
            MagacinIdPrima = magacin.MagacinId,
            DobavljacId = partner.PartnerId,
            KontoDobavljacaId = kontoDob.KontoId,
            BrojRacuna = "MP-INV-201",
            TransportniTroskovi = 1000m,
            MarzaProcenat = 20m,
            PoreskaStopaProcenat = 20m
        };

        kalk.Stavke.Add(new MaloprodajnaKalkulacijaStavka { ArtikalId = artikal1.ArtikalId, Kolicina = 10m, NabavnaCena = 100m }); // Iznos = 1000
        kalk.Stavke.Add(new MaloprodajnaKalkulacijaStavka { ArtikalId = artikal2.ArtikalId, Kolicina = 10m, NabavnaCena = 100m }); // Iznos = 1000

        MaloprodajnaKalkulacijaService.IzracunajSaStavkama(kalk);

        Assert.Equal(1000m, kalk.SvegaTroskovi);
        Assert.Equal(2000m, kalk.NabavnaVrednost);
        Assert.Equal(3000m, kalk.SvegaNabavno); // 2000 + 1000
        Assert.Equal(600m, kalk.Razlika); // 20% od 3000
        Assert.Equal(720m, kalk.Porez); // 20% od (3000 + 600)
        Assert.Equal(4320m, kalk.ProdajnaVrednost); // 3000 + 600 + 720

        var service = new MaloprodajnaKalkulacijaService(db);
        await service.SaveKalkulacijuAsync(kalk);
        await service.KnjiziKalkulacijuAsync(kalk.MaloprodajnaKalkulacijaId);

        var sacuvana = (await service.GetKalkulacijeAsync()).FirstOrDefault(k => k.MaloprodajnaKalkulacijaId == kalk.MaloprodajnaKalkulacijaId);
        Assert.NotNull(sacuvana);
        Assert.True(sacuvana!.IsKnjizen);
        Assert.NotNull(sacuvana.NalogId);
    }
}


