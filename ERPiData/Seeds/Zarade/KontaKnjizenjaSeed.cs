using System.Collections.Generic;
using ERPiData.Models.Zarade;

namespace ERPiData.Seeds.Zarade;

public static class KontaKnjizenjaSeed
{
    public const string TrosakZarade = "TROSAK_ZARADE";
    public const string TrosakDoprinosaPoslodavca = "TROSAK_DOPRINOSA_POSLODAVCA";
    public const string ObavezaNetoZarada = "OBAVEZA_NETO_ZARADA";
    public const string ObavezaPorezZaposleni = "OBAVEZA_POREZ_ZAPOSLENI";
    public const string ObavezaDoprinosiZaposleni = "OBAVEZA_DOPRINOSI_ZAPOSLENI";
    public const string ObavezaPoreziDoprinosiPoslodavac = "OBAVEZA_POREZI_DOPRINOSI_POSLODAVAC";
    public const string ObavezaObustave = "OBAVEZA_OBUSTAVE";
    public const string ObavezaSamodoprinos = "OBAVEZA_SAMODOPRINOS";

    public const string PotrazivanjeRefundacije = "POTRAZIVANJE_REFUNDACIJE";
    public const string ObavezaNetoRefundacija = "OBAVEZA_NETO_REFUNDACIJA";
    public const string ObavezaPoreziZaposleniRefundacija = "OBAVEZA_POREZI_ZAPOSLENI_REFUNDACIJA";
    public const string ObavezaPoreziPoslodavacRefundacija = "OBAVEZA_POREZI_POSLODAVAC_REFUNDACIJA";

    public const string TrosakNaknade = "TROSAK_NAKNADE";
    public const string TrosakDoprinosaIsplatioca = "TROSAK_DOPRINOSA_ISPLATIOCA";
    public const string ObavezaNetoNaknada = "OBAVEZA_NETO_NAKNADA";
    public const string ObavezaPorezNaknada = "OBAVEZA_POREZ_NAKNADA";
    public const string ObavezaDoprinosiNaknada = "OBAVEZA_DOPRINOSI_NAKNADA";
    public const string ObavezaDoprinosiIsplatioca = "OBAVEZA_DOPRINOSI_ISPLATIOCA";

    public static List<KontoKnjizenja> Podrazumevana() =>
    [
        new()
        {
            Kljuc = TrosakZarade,
            Naziv = "Troškovi zarada i naknada zarada (bruto)",
            Konto = "520",
            Strana = StranaKnjizenja.Duguje,
            Redosled = 10,
            Napomena = "Koristi se za obračun koji nije razložen na stavke; kad stavke postoje, trošak ide na konto upisan uz vrstu primanja."
        },
        new()
        {
            Kljuc = TrosakDoprinosaPoslodavca,
            Naziv = "Troškovi doprinosa na zarade na teret poslodavca",
            Konto = "521",
            Strana = StranaKnjizenja.Duguje,
            Redosled = 20
        },
        new()
        {
            Kljuc = ObavezaNetoZarada,
            Naziv = "Obaveze za neto zarade i naknade zarada",
            Konto = "450",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 30,
            Napomena = "Iznos je jednak zbiru naloga za prenos neto zarada."
        },
        new()
        {
            Kljuc = ObavezaPorezZaposleni,
            Naziv = "Obaveze za porez na zarade na teret zaposlenog",
            Konto = "451",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 40
        },
        new()
        {
            Kljuc = ObavezaDoprinosiZaposleni,
            Naziv = "Obaveze za doprinose na zarade na teret zaposlenog",
            Konto = "452",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 50
        },
        new()
        {
            Kljuc = ObavezaPoreziDoprinosiPoslodavac,
            Naziv = "Obaveze za poreze i doprinose na teret poslodavca",
            Konto = "453",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 60
        },
        new()
        {
            Kljuc = ObavezaObustave,
            Naziv = "Ostale obaveze — obustave iz zarade",
            Konto = "469",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 70,
            Napomena = "Rate kredita, sudske zabrane i ostali odbici. Skidaju se samo na konačnoj zaradi."
        },
        new()
        {
            Kljuc = ObavezaSamodoprinos,
            Naziv = "Obaveze za samodoprinos",
            Konto = "489",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 80
        },
        new()
        {
            Kljuc = PotrazivanjeRefundacije,
            Naziv = "Potraživanja za naknade zarada koje se refundiraju",
            Konto = "225",
            Strana = StranaKnjizenja.Duguje,
            Redosled = 90,
            Napomena = "Iznos je jednak koloni „за исплату“ obrasca OZ-10 — bruto naknada uvećana za doprinose na teret poslodavca."
        },
        new()
        {
            Kljuc = ObavezaNetoRefundacija,
            Naziv = "Obaveze za neto naknade zarada koje se refundiraju",
            Konto = "454",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 91
        },
        new()
        {
            Kljuc = ObavezaPoreziZaposleniRefundacija,
            Naziv = "Obaveze za poreze i doprinose na naknade koje se refundiraju — na teret zaposlenog",
            Konto = "455",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 92
        },
        new()
        {
            Kljuc = ObavezaPoreziPoslodavacRefundacija,
            Naziv = "Obaveze za poreze i doprinose na naknade koje se refundiraju — na teret poslodavca",
            Konto = "456",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 93
        },
        new()
        {
            Kljuc = TrosakNaknade,
            Naziv = "Troškovi naknada po ugovorima van radnog odnosa",
            Konto = "522",
            Strana = StranaKnjizenja.Duguje,
            Redosled = 110
        },
        new()
        {
            Kljuc = TrosakDoprinosaIsplatioca,
            Naziv = "Troškovi doprinosa na naknade na teret isplatioca",
            Konto = "521",
            Strana = StranaKnjizenja.Duguje,
            Redosled = 120
        },
        new()
        {
            Kljuc = ObavezaNetoNaknada,
            Naziv = "Obaveze prema fizičkim licima za naknade po ugovorima",
            Konto = "465",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 130
        },
        new()
        {
            Kljuc = ObavezaPorezNaknada,
            Naziv = "Obaveze za porez na naknade po ugovorima",
            Konto = "489",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 140
        },
        new()
        {
            Kljuc = ObavezaDoprinosiNaknada,
            Naziv = "Obaveze za doprinose na naknade na teret primaoca",
            Konto = "489",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 150
        },
        new()
        {
            Kljuc = ObavezaDoprinosiIsplatioca,
            Naziv = "Obaveze za doprinose na naknade na teret isplatioca",
            Konto = "489",
            Strana = StranaKnjizenja.Potrazuje,
            Redosled = 160
        }
    ];
}
