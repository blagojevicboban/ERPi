using System;
using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Finansije;

public enum VrstaBlagajne
{
    Dinarska = 0,
    Devizna = 1
}

public enum VrstaBlagajnickogNaloga
{
    Uplata = 0,
    Isplata = 1
}

public class BlagajnickiNalog
{
    [Key]
    public int BlagajnickiNalogId { get; set; }

    public string BrojNaloga { get; set; } = string.Empty;
    public VrstaBlagajne VrstaBlagajne { get; set; } = VrstaBlagajne.Dinarska;
    public VrstaBlagajnickogNaloga VrstaNaloga { get; set; } = VrstaBlagajnickogNaloga.Uplata;

    public DateTime Datum { get; set; } = DateTime.Today;

    public string UplatilacIsplatilac { get; set; } = string.Empty;
    public string Svrha { get; set; } = string.Empty;

    public string BrojKontaProtu { get; set; } = "2410";

    public decimal Iznos { get; set; }

    public string Status { get; set; } = "Nacrt";
    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; }
    public bool IsKnjizeno { get; set; }
}

public class BlagajnickiDnevnikRed
{
    public int BlagajnickiNalogId { get; set; }
    public string BrojNaloga { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string Vrsta { get; set; } = "Uplata";
    public string UplatilacIsplatilac { get; set; } = string.Empty;
    public string Svrha { get; set; } = string.Empty;
    public string BrojKontaProtu { get; set; } = string.Empty;

    public decimal Uplata { get; set; }
    public decimal Isplata { get; set; }
    public decimal Saldo { get; set; }
}

public class BlagajnickiDnevnikSummary
{
    public decimal PocetnoStanje { get; set; }
    public decimal UkupnoUplata { get; set; }
    public decimal UkupnoIsplata { get; set; }
    public decimal KrajnjeStanje => PocetnoStanje + UkupnoUplata - UkupnoIsplata;
}
