using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Finansije;

public enum VrstaSlužbenogPutovanja
{
    Zemlja = 0,
    Inostranstvo = 1
}

public class PutniNalog
{
    [Key]
    public int PutniNalogId { get; set; }

    public string BrojNaloga { get; set; } = string.Empty;
    public VrstaSlužbenogPutovanja Vrsta { get; set; } = VrstaSlužbenogPutovanja.Zemlja;

    public string ZaposleniIme { get; set; } = string.Empty;
    public string RadnoMesto { get; set; } = string.Empty;

    [MaxLength(13)]
    public string Jmbg { get; set; } = string.Empty;

    public string Relacija { get; set; } = string.Empty;
    public string SvrhaPutovanja { get; set; } = string.Empty;
    public string PrevoznoSredstvo { get; set; } = "Službeno vozilo";

    public DateTime DatumPolaska { get; set; } = DateTime.Now;
    public DateTime DatumPovratka { get; set; } = DateTime.Now.AddDays(1);

    public double TrajanjeSati { get; set; }
    public decimal BrojDnevnica { get; set; }
    public decimal IznosDnevniceRsd { get; set; } = 3000m;

    public decimal UkupnoDnevnice { get; set; }
    public decimal TroskoviGoriva { get; set; }
    public decimal TroskoviSmestaja { get; set; }
    public decimal TroskoviPrevoza { get; set; }
    public decimal OstaliTroskovi { get; set; }

    public decimal Akontacija { get; set; }
    public decimal UkupnoZaIsplatu { get; set; }

    public string Status { get; set; } = "Nacrt";
    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; }
    public bool IsKnjizeno { get; set; }

    public List<PutniNalogTrosakStavka> StavkeTroskova { get; set; } = new();
}

public class PutniNalogTrosakStavka
{
    [Key]
    public int PutniNalogTrosakStavkaId { get; set; }

    public int PutniNalogId { get; set; }
    public PutniNalog? PutniNalog { get; set; }

    public int RedniBroj { get; set; }
    public string VrstaTroska { get; set; } = "Gorivo";
    public string BrojRacuna { get; set; } = string.Empty;
    public DateTime DatumRacuna { get; set; } = DateTime.Today;
    public decimal Iznos { get; set; }
    public string Opis { get; set; } = string.Empty;
}
