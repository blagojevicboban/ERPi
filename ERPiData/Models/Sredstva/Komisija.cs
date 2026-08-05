namespace ERPiData.Models.Sredstva;

/// <summary>Popisna komisija — port iz ERPiSredstvaData.Models.Komisija, bez izmena.</summary>
public class Komisija
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public DateTime DatumKreiranja { get; set; }
    public bool JeAktivna { get; set; } = true;

    public ICollection<ClanKomisije> Clanovi { get; set; } = new List<ClanKomisije>();
    public ICollection<Popis> Popisi { get; set; } = new List<Popis>();
}
