using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Finansije;

public enum VrstaKompenzacije
{
    Dvojna = 0,
    Asignacija = 1,
    Cesija = 2
}

public class Kompenzacija
{
    [Key]
    public int KompenzacijaId { get; set; }

    public string BrojDokumenta { get; set; } = string.Empty;
    public VrstaKompenzacije Vrsta { get; set; } = VrstaKompenzacije.Dvojna;

    public DateTime Datum { get; set; } = DateTime.Today;

    public int? PartnerId { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;
    public string? KontoPartnera1 { get; set; }

    public int? Partner2Id { get; set; }
    public string? NazivPartnera2 { get; set; }
    public string? KontoPartnera2 { get; set; }

    public int? Partner3Id { get; set; }
    public string? NazivPartnera3 { get; set; }
    public string? KontoPartnera3 { get; set; }

    public decimal UkupanIznosKompenzacije { get; set; }

    public string Status { get; set; } = "Nacrt";
    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; }
    public bool IsKnjizeno { get; set; }

    public List<KompenzacijaStavka> Stavke { get; set; } = new();
}

public class KompenzacijaStavka
{
    [Key]
    public int KompenzacijaStavkaId { get; set; }

    public int KompenzacijaId { get; set; }
    public Kompenzacija? Kompenzacija { get; set; }

    public int RedniBroj { get; set; }
    public int StavkaNalogaId { get; set; }

    public int PartnerId { get; set; }

    public string BrojDokumenta { get; set; } = string.Empty;
    public DateTime DatumDokumenta { get; set; } = DateTime.Today;

    public string Strana { get; set; } = "Duguje";
    public string BrojKonta { get; set; } = "2040";

    public decimal IznosFakture { get; set; }
    public decimal IznosPreostalo { get; set; }
    public decimal IznosZaKompenzaciju { get; set; }
}

public class ObostranoDugovanjeCandidate
{
    public int PartnerId { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;

    public decimal PotrazivanjeKupac { get; set; }
    public decimal ObavezaDobavljac { get; set; }

    public decimal MaksimalnaKompenzacija => Math.Min(PotrazivanjeKupac, ObavezaDobavljac);
}
