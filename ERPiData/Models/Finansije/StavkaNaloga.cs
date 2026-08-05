using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPiData.Models.Core;

namespace ERPiData.Models.Finansije;

/// <summary>
/// Jedna stavka (red) naloga glavne knjige. Konto/Partner/Mesto troška su ovde pravi strani
/// ključevi ka Core šemi — u ERPiFinansije je Konto bio string (BrojKonta) jer su knjiženja i
/// šifarnik živeli u istoj bazi ali bez FK-a; sad kad postoji jedinstvena Konta tabela, veza
/// je stroga, pa ne postoji stavka koja pokazuje na konto koji ne postoji u kontnom planu.
/// </summary>
public class StavkaNaloga
{
    [Key]
    public int StavkaNalogaId { get; set; }

    public int NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public int RedniBroj { get; set; }

    public int KontoId { get; set; }
    [ForeignKey(nameof(KontoId))]
    public Konto? Konto { get; set; }

    [MaxLength(50)]
    public string? BrojDokumenta { get; set; }

    public DateTime? DatumDokumenta { get; set; }
    public DateTime? ValutaDospela { get; set; }

    [MaxLength(250)]
    public string? Opis { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Duguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Potrazuje { get; set; }

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? MestoTroskaId { get; set; }
    [ForeignKey(nameof(MestoTroskaId))]
    public MestoTroska? MestoTroska { get; set; }

    [MaxLength(10)]
    public string Valuta { get; set; } = "RSD";

    [Column(TypeName = "decimal(18, 4)")]
    public decimal KursValute { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DevizniDuguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DevizniPotrazuje { get; set; }

    // Popunjava se samo za PDV-relevantne linije (konto 4700 izlazni PDV / 2700 ulazni PDV) —
    // PDV evidencija (kasnija podfaza) odavde čita poresku osnovicu i stopu.
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Osnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? StopaPdv { get; set; }
}
