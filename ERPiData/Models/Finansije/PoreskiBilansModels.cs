namespace ERPiData.Models.Finansije;

public class Pb1Stavka
{
    public int RedniBroj { get; set; }
    public string Opis { get; set; } = string.Empty;
    public decimal RacunovodstveniIznos { get; set; }
    public decimal PoreskiIznos { get; set; }
    public decimal Uskladjivanje { get; set; }
}

public class PoreskaAmortizacijaStavka
{
    public int Grupa { get; set; }
    public string NazivGrupe { get; set; } = string.Empty;
    public decimal PoreskaStopa { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal NeotpisanaPoreskaVrednost { get; set; }
    public decimal RacunovodstvenaAmortizacija { get; set; }
    public decimal PoreskaAmortizacija { get; set; }
    public decimal RazlikaAmortizacije => RacunovodstvenaAmortizacija - PoreskaAmortizacija;
}

public class ObrazacPdpResult
{
    public string NazivObveznika { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public int PoreskiPeriodGodina { get; set; }
    public decimal OporezivaDobit { get; set; }
    public decimal StopaPoreza { get; set; } = 15.0m;
    public decimal ObracunatiPorez { get; set; }
    public decimal PoreskiKredit { get; set; }
    public decimal NetKonacnaPoreskaObaveza { get; set; }
    public decimal MesecnaAkontacija { get; set; }
}
