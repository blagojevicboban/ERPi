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
    public int UvezenoMaterijala { get; set; }
    public int UvezenoPoreskihTarifa { get; set; }
    public int UvezenoMaterijalnihKartica { get; set; }
    public int UvezenoUlaza { get; set; }
    public int UvezenoStavkiUlaza { get; set; }
    public int UvezenoTrebovanja { get; set; }
    public int UvezenoStavkiTrebovanja { get; set; }
    public int UvezenoPrimopredaja { get; set; }
    public int UvezenoStavkiPrimopredaja { get; set; }
    public int UvezenoMaloprodajnihKalkulacija { get; set; }
    public int UvezenoStavkiMaloprodajnihKalkulacija { get; set; }
    public int UvezenoRacunaOtpremnica { get; set; }
    public int UvezenoStavkiRacunaOtpremnica { get; set; }
    public int UvezenoNivelacija { get; set; }
    public int UvezenoStavkiNivelacija { get; set; }
}
