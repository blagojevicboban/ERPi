using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiData.Models.Zarade;

[Table("Porezi")]
public class Porezi
{
    [Key]
    public int Id { get; set; }

    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int RedniBroj { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal Zarada { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal AkPorez { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal AkPorez2 { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal AkPorez3 { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal AkPorez4 { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal Prvast { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Drugast { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Trecast { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LinPorez3 { get; set; }

    [MaxLength(10)] public string SifPlac1 { get; set; } = "";
    [MaxLength(40)] public string ZiroR1 { get; set; } = "";
    [MaxLength(20)] public string PozivNa1 { get; set; } = "";
    [MaxLength(20)] public string PozivNa3 { get; set; } = "";
    [MaxLength(60)] public string Svrha1 { get; set; } = "";
    [MaxLength(60)] public string Svrha2 { get; set; } = "";
    [MaxLength(60)] public string Primalac1 { get; set; } = "";
    [MaxLength(60)] public string Primalac2 { get; set; } = "";

    [MaxLength(10)] public string SifPlac2 { get; set; } = "";
    [MaxLength(40)] public string ZiroR2 { get; set; } = "";
    [MaxLength(20)] public string PozivNa2 { get; set; } = "";
    [MaxLength(20)] public string PozivNa4 { get; set; } = "";
    [Column(TypeName = "decimal(6,2)")] public decimal PosPorez { get; set; }
    [MaxLength(60)] public string Svrha3 { get; set; } = "";
    [MaxLength(60)] public string Svrha4 { get; set; } = "";
    [MaxLength(60)] public string Primalac3 { get; set; } = "";
    [MaxLength(60)] public string Primalac4 { get; set; } = "";

    [Column(TypeName = "decimal(6,2)")] public decimal ProcDrzav { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcNocni { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcPreko { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcMinul { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcNedel { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcBolov { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcPlac { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcPlZa { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcInval { get; set; }

    public int FondCasova { get; set; }
    public int CasZaOb { get; set; }

    [Column(TypeName = "decimal(10,4)")] public decimal VrBoda { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal ProcIzdrz { get; set; }

    [MaxLength(10)] public string Akont { get; set; } = "DA";
    [Column(TypeName = "decimal(14,2)")] public decimal ProsBrut { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TopliObrokCena { get; set; }
}
