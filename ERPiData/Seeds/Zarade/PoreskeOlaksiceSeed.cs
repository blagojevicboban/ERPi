using System.Collections.Generic;
using ERPiData.Models.Zarade;

namespace ERPiData.Seeds.Zarade;

public static class PoreskeOlaksiceSeed
{
    private const string Proveriti = "Proveriti oznaku i procenat u važećem Katalogu vrste prihoda.";

    public static List<PoreskaOlaksica> Podrazumevane() =>
    [
        Povracaj("08", "Novozaposleno lice — povraćaj 65%", "čl. 21v ZPDG", 65m),
        Povracaj("09", "Novozaposleno lice — povraćaj 70%", "čl. 21v ZPDG", 70m),
        Povracaj("10", "Novozaposleno lice — povraćaj 75%", "čl. 21v ZPDG", 75m),
        Oslobodjenje("24", "Kvalifikovano novozaposleno lice", "čl. 21ž ZPDG", 70m, 100m),
        Oslobodjenje("32", "Osnivač inovativnog preduzeća", "čl. 21đ ZPDG", 100m, 100m)
    ];

    private static PoreskaOlaksica Povracaj(string sifra, string naziv, string osnov, decimal procenat)
        => new()
        {
            Sifra = sifra,
            Naziv = naziv,
            PravniOsnov = osnov,
            Mehanizam = MehanizamOlaksice.Povracaj,
            ProcenatPoreza = procenat,
            ProcenatDoprinosa = procenat,
            Aktivna = true,
            Napomena = Proveriti
        };

    private static PoreskaOlaksica Oslobodjenje(
        string sifra, string naziv, string osnov, decimal procenatPoreza, decimal procenatDoprinosa)
        => new()
        {
            Sifra = sifra,
            Naziv = naziv,
            PravniOsnov = osnov,
            Mehanizam = MehanizamOlaksice.Oslobodjenje,
            ProcenatPoreza = procenatPoreza,
            ProcenatDoprinosa = procenatDoprinosa,
            Aktivna = true,
            Napomena = Proveriti + " Uneti i MFP deklaraciju, inače se umanjenje neće prijaviti."
        };
}
