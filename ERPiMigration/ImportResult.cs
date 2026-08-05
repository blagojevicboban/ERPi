namespace ERPiMigration;

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int UvezenoPartnera { get; set; }
    public int UvezenoKonta { get; set; }
    public int UvezenoMestaTroska { get; set; }
    public int UvezenoNaloga { get; set; }
    public int UvezenoStavkiNaloga { get; set; }
    public int UvezenoMagacina { get; set; }
    public int UvezenoArtikala { get; set; }
    public int UvezenoKalkulacija { get; set; }
    public int UvezenoStavkiKalkulacije { get; set; }
    public int UvezenoPdvZapisa { get; set; }
    public int UvezenoSefDokumenata { get; set; }
    public int UvezenoPfrRacuna { get; set; }
}
