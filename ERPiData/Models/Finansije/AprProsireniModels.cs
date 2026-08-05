namespace ERPiData.Models.Finansije;

public class StatistickiIzvestajStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public string KontoGrupa { get; set; } = string.Empty;
    public decimal IznosTekuca { get; set; }
    public decimal IznosPrethodna { get; set; }
}

public class CashFlowStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public string TipAktivnosti { get; set; } = "Poslovne"; // Poslovne, Investicione, Finansijske
    public decimal Priliv { get; set; }
    public decimal Odliv { get; set; }
    public decimal NetoPrilivOdliv => Priliv - Odliv;
}

public class PromeneNaKapitaluStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public decimal OsnovniKapital { get; set; }
    public decimal Rezerve { get; set; }
    public decimal NerasporedjenaDobit { get; set; }
    public decimal Gubitak { get; set; }
    public decimal UkupnoKapital => OsnovniKapital + Rezerve + NerasporedjenaDobit - Gubitak;
}
