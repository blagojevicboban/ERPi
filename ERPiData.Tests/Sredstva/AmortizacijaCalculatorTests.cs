using ERPiData.Models.Sredstva;
using ERPiData.Services.Sredstva;

namespace ERPiData.Tests.Sredstva;

/// <summary>Port iz ERPiSredstvaData.Tests, bez izmena — AmortizacijaCalculator je preneta bez izmena logike.</summary>
public class AmortizacijaCalculatorTests
{
    private static Kartica K(DateTime datum, decimal nabavna = 0, decimal ispravka = 0, int id = 0) => new()
    {
        Id = id,
        Datum = datum,
        NabavnaVrednost = nabavna,
        IspravkaVrednosti = ispravka
    };

    [Fact]
    public void PunaGodina_BezPromena_ObracunavaCelogodisnjuStopu()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(100_000m, rezultat.NabavnaVrednost);
        Assert.Equal(0m, rezultat.PrethodnaIspravka);
        Assert.Equal(20_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void PrestupnaGodina_Koristi366Dana()
    {
        var kartice = new List<Kartica> { K(new DateTime(2019, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(36.6m, kartice, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        Assert.Equal(36_600m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void NabavkaUsredGodine_ObracunavaSamoOdDatumaNabavke()
    {
        var kartice = new List<Kartica> { K(new DateTime(2026, 7, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        int ocekivaniDani = (new DateTime(2026, 12, 31) - new DateTime(2026, 7, 1)).Days + 1;
        decimal ocekivano = Math.Round(100_000m * 0.20m * ocekivaniDani / 365m, 2);

        Assert.Equal(ocekivano, rezultat.NovaAmortizacija);
        Assert.Equal(100_000m, rezultat.NabavnaVrednost);
    }

    [Fact]
    public void NovaAmortizacija_OgranicenaNaNeotpisanuVrednost()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m, ispravka: 99_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(50m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(1_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void RashodUsredPerioda_SmanjujeOsnovicuZaNarednoObracunavanje()
    {
        var kartice = new List<Kartica>
        {
            K(new DateTime(2020, 1, 1), nabavna: 100_000m),
            K(new DateTime(2026, 7, 2), nabavna: -40_000m)
        };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(60_000m, rezultat.NabavnaVrednost);
    }

    [Fact]
    public void AmortizacijaDoRashoda_ObracunavaSrazmeranIznosDoDatumaRashodovanja()
    {
        var kartice = new List<Kartica>
        {
            K(new DateTime(2020, 1, 1), nabavna: 100_000m)
        };

        var rezultat = AmortizacijaCalculator.Izracunaj(
            20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 5, 15));

        Assert.True(rezultat.NovaAmortizacija > 0);
        Assert.Equal(7397.26m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void NultaStopaAmortizacije_DajeNuluBezObziraNaOsnovicu()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(0m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(0m, rezultat.NovaAmortizacija);
    }

    [Theory]
    [InlineData("Amortizacija (2026)", true, 2026)]
    [InlineData("Amortizacija (03/2026)", true, 2026)]
    [InlineData("Amortizacija (Q1/2026)", true, 2026)]
    [InlineData("Redovan otpis (2025)", true, 2025)]
    [InlineData("Redovan otpis 2024", true, 2024)]
    [InlineData("Revalorizacija (2026)", false, 0)]
    [InlineData("Nabavka", false, 0)]
    public void TryParseGodina_PrepoznajeGodinuIzOpisaPromene(string opis, bool ocekivanUspeh, int ocekivanaGodina)
    {
        bool uspeh = AmortizacijaCalculator.TryParseGodina(opis, out int godina);

        Assert.Equal(ocekivanUspeh, uspeh);
        if (ocekivanUspeh)
            Assert.Equal(ocekivanaGodina, godina);
    }

    [Theory]
    [InlineData("2026-01-01", "2026-12-31", "Amortizacija (2026)")]
    [InlineData("2026-03-01", "2026-03-31", "Amortizacija (03/2026)")]
    [InlineData("2026-01-01", "2026-03-31", "Amortizacija (Q1/2026)")]
    [InlineData("2026-04-01", "2026-06-30", "Amortizacija (Q2/2026)")]
    public void GenerisiOpisPromene_FormiraStandardniOpisZaPeriod(string startStr, string endStr, string ocekivano)
    {
        DateTime start = DateTime.Parse(startStr);
        DateTime end = DateTime.Parse(endStr);

        string rezultat = AmortizacijaCalculator.GenerisiOpisPromene(start, end);

        Assert.Equal(ocekivano, rezultat);
    }

    [Fact]
    public void IzracunajPrethodnuIspravku_SabiraSamoKarticePreDatogTrenutka()
    {
        var pre1 = K(new DateTime(2025, 1, 1), ispravka: 100m, id: 1);
        var pre2 = K(new DateTime(2025, 6, 1), ispravka: 50m, id: 2);
        var trenutna = K(new DateTime(2026, 1, 1), ispravka: 200m, id: 3);
        var posle = K(new DateTime(2026, 6, 1), ispravka: 999m, id: 4);

        var sve = new List<Kartica> { pre1, pre2, trenutna, posle };

        decimal prethodna = AmortizacijaCalculator.IzracunajPrethodnuIspravku(sve, trenutna);

        Assert.Equal(150m, prethodna);
    }

    [Fact]
    public void IzracunajPrethodnuIspravku_IstiDatum_KoristiIdKaoTieBreaker()
    {
        var datum = new DateTime(2026, 1, 1);
        var ranija = K(datum, ispravka: 10m, id: 1);
        var trenutna = K(datum, ispravka: 20m, id: 2);
        var kasnija = K(datum, ispravka: 30m, id: 3);

        var sve = new List<Kartica> { ranija, trenutna, kasnija };

        decimal prethodna = AmortizacijaCalculator.IzracunajPrethodnuIspravku(sve, trenutna);

        Assert.Equal(10m, prethodna);
    }

    [Fact]
    public void RezidualnaVrednost_UmanjujeOsnovicuAmortizacije()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(
            stopaAmortizacije: 20m,
            kartice: kartice,
            start: new DateTime(2026, 1, 1),
            end: new DateTime(2026, 12, 31),
            rezidualnaVrednost: 10_000m);

        Assert.Equal(18_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void RezidualnaVrednost_OgranicavaMaksimalniOtpis()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m, ispravka: 89_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(
            stopaAmortizacije: 20m,
            kartice: kartice,
            start: new DateTime(2026, 1, 1),
            end: new DateTime(2026, 12, 31),
            rezidualnaVrednost: 10_000m);

        Assert.Equal(1_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void PraviloOdNarednogMeseca_AktivacijaUsredMeseca_PocinjePrvogUSledecomMesecu()
    {
        var kartice = new List<Kartica> { K(new DateTime(2026, 5, 15), nabavna: 120_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(
            stopaAmortizacije: 10m,
            kartice: kartice,
            start: new DateTime(2026, 1, 1),
            end: new DateTime(2026, 12, 31),
            rezidualnaVrednost: 0m,
            pocetakRule: PocetakAmortizacijeRule.OdNarednogMeseca,
            datumAktiviranja: new DateTime(2026, 5, 15));

        int ocekivaniDani = (new DateTime(2026, 12, 31) - new DateTime(2026, 6, 1)).Days + 1;
        decimal ocekivano = Math.Round(120_000m * 0.10m * ocekivaniDani / 365m, 2);

        Assert.Equal(ocekivano, rezultat.NovaAmortizacija);
    }
}
