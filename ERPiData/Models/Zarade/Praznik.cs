using System.ComponentModel.DataAnnotations;

namespace ERPiData.Models.Zarade;

public class Praznik
{
    [Key]
    public int PraznikId { get; set; }

    public DateTime Datum { get; set; }

    [Required, MaxLength(80)]
    public string Naziv { get; set; } = "";

    public bool Neradni { get; set; } = true;
    public bool RucniUnos { get; set; }
}
