namespace ERPiData.Models.Sredstva;

public enum StatusPopisa
{
    UToku = 0,
    Zavrsen = 1
}

/// <summary>Godišnji popis osnovnih sredstava — port iz ERPiSredstvaData.Models.Popis, bez izmena.</summary>
public class Popis
{
    public int Id { get; set; }

    public DateTime DatumPopisa { get; set; }
    public int Godina { get; set; }

    public int KomisijaId { get; set; }
    public Komisija Komisija { get; set; } = null!;

    public StatusPopisa Status { get; set; } = StatusPopisa.UToku;

    public ICollection<PopisnaStavka> Stavke { get; set; } = new List<PopisnaStavka>();
}
