using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using SkiaSharp;
using ERPiData;
using ERPiData.Models.Sredstva;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Sredstva.Dashboard;

/// <summary>
/// Port iz ERPiSredstvaApp.Views.Dashboard.DashboardViewModel — ista "Radna tabla" (KPI kartice +
/// grafikoni), adaptirana na deljeni <see cref="ErpiDbContext"/> umesto samostalnog SredstvaDbContext.
/// Razlika od izvora: "Vrednost po kontima" grupiše po <c>Sredstvo.Konto.BrojKonta</c> (prava FK
/// navigacija), ne po string koloni <c>Sredstvo.Konto</c> kao u izvoru — isti obrazac string→FK kao
/// svuda drugde u ERPi (vidi PLAN_NASTAVKA.md §2).
/// </summary>
public partial class SredstvaDashboardViewModel : ObservableObject
{
    private readonly ErpiDbContext _db;

    [ObservableProperty]
    private int _ukupanBrojSredstava;

    [ObservableProperty]
    private decimal _ukupnaNabavnaVrednost;

    [ObservableProperty]
    private decimal _ukupnaSadasnjaVrednost;

    [ObservableProperty]
    private ISeries[] _statusSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _kontoSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _topSredstvaSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _topSredstvaXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _topSredstvaYAxes = Array.Empty<Axis>();

    public SredstvaDashboardViewModel(ErpiDbContext db)
    {
        _db = db;
        UcitajPodatke();
    }

    public void UcitajPodatke()
    {
        var svaSredstva = _db.Sredstva.ToList();
        // Konto navigacija se učitava posebno da izbegnemo lazy-loading N+1 (Include nije podešen).
        var kontoBrojevi = _db.Konta.ToDictionary(k => k.KontoId, k => k.BrojKonta);

        // Mapa KontoId iz Kartica za sredstva koja nemaju direktan KontoId
        var karticaKontoMap = _db.SredstvaKartice.AsNoTracking()
            .Where(k => k.KontoId.HasValue)
            .GroupBy(k => k.SredstvoId)
            .ToDictionary(g => g.Key, g => g.First().KontoId!.Value);

        // Mapa KontoId iz Prijava za dodatni fallback
        var prijavaKontoMap = _db.SredstvaPrijave.AsNoTracking()
            .Where(p => p.KontoId.HasValue)
            .GroupBy(p => p.SredstvoId)
            .ToDictionary(g => g.Key, g => g.First().KontoId!.Value);

        // Auto-popravka KontoId za sredstva u bazi ako je bio null
        bool izmenaKonta = false;
        foreach (var s in svaSredstva)
        {
            if (!s.KontoId.HasValue)
            {
                if (karticaKontoMap.TryGetValue(s.Id, out var kIdFromKartica))
                {
                    s.KontoId = kIdFromKartica;
                    izmenaKonta = true;
                }
                else if (prijavaKontoMap.TryGetValue(s.Id, out var kIdFromPrijava))
                {
                    s.KontoId = kIdFromPrijava;
                    izmenaKonta = true;
                }
            }
        }
        if (izmenaKonta)
        {
            try { _db.SaveChanges(); } catch { }
        }

        // Auto-ispravka statusa JeAktivno za rashodovana/prodata sredstva
        var rashodovanaSredstvaIds = _db.SredstvaRashodi.AsNoTracking()
            .Where(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.Prodaja || r.Kod == TipoviPromena.Otudjenje || r.Kod == TipoviPromena.Brisanje)
            .Select(r => r.SredstvoId)
            .Union(_db.SredstvaKartice.AsNoTracking()
                .Where(k => k.OpisPromene != null && (k.OpisPromene.StartsWith("Rashod") || k.OpisPromene.StartsWith("Prodaja") || k.OpisPromene.StartsWith("Otudjenje")))
                .Select(k => k.SredstvoId))
            .ToHashSet();

        bool izmenaStatusa = false;
        foreach (var s in svaSredstva)
        {
            if (rashodovanaSredstvaIds.Contains(s.Id) && s.JeAktivno)
            {
                s.JeAktivno = false;
                izmenaStatusa = true;
            }
        }
        if (izmenaStatusa)
        {
            try { _db.SaveChanges(); } catch { }
        }

        UkupanBrojSredstava = svaSredstva.Count;
        UkupnaNabavnaVrednost = svaSredstva.Sum(s => s.NabavnaVrednost);
        UkupnaSadasnjaVrednost = svaSredstva.Sum(s => s.SadasnjaVrednost);

        // Status Sredstava (Donut)
        var aktivna = svaSredstva.Count(s => s.JeAktivno);
        var neaktivna = svaSredstva.Count(s => !s.JeAktivno);

        StatusSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { aktivna }, Name = "Aktivna", InnerRadius = 30 },
            new PieSeries<int> { Values = new[] { neaktivna }, Name = "Rashodovana", InnerRadius = 30 }
        };

        // Vrednost po Kontima (Pie)
        var poKontima = svaSredstva
            .Where(s => s.JeAktivno && s.SadasnjaVrednost > 0)
            .GroupBy(s => {
                var kId = s.KontoId
                          ?? (karticaKontoMap.TryGetValue(s.Id, out var k1) ? k1 : (int?)null)
                          ?? (prijavaKontoMap.TryGetValue(s.Id, out var k2) ? k2 : (int?)null);
                if (kId.HasValue && kontoBrojevi.TryGetValue(kId.Value, out var broj) && !string.IsNullOrWhiteSpace(broj))
                    return broj;
                return "Nepoznato";
            })
            .Select(g => new { Konto = g.Key, Vrednost = (double)g.Sum(s => s.SadasnjaVrednost) })
            .OrderByDescending(x => x.Vrednost)
            .Take(10) // Top 10 konta
            .ToList();

        var kontoPieSeries = new List<ISeries>();
        foreach (var k in poKontima)
        {
            kontoPieSeries.Add(new PieSeries<double>
            {
                Values = new[] { k.Vrednost },
                Name = $"Konto: {k.Konto}",
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Model:N0}",
                ToolTipLabelFormatter = point => $"{point.Model:N2}"
            });
        }
        KontoSeries = kontoPieSeries.ToArray();

        // Top 5 Najvrednijih Sredstava (horizontalni bar chart)
        var top5 = svaSredstva
            .Where(s => s.JeAktivno)
            .OrderByDescending(s => s.SadasnjaVrednost)
            .Take(5)
            .ToList();

        // Redosled je obrnut jer RowSeries crta prvu stavku pri dnu - zelimo najveci iznos na vrhu
        var top5ZaGrafikon = Enumerable.Reverse(top5).ToList();

        TopSredstvaSeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = top5ZaGrafikon.Select(s => (double)s.SadasnjaVrednost).ToArray(),
                Name = "Sadašnja vrednost",
                Fill = new SolidColorPaint(SKColor.Parse("#1B4332")), // Sredstva zelena
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#333333")),
                DataLabelsPosition = DataLabelsPosition.Right,
                DataLabelsFormatter = point =>
                {
                    var s = top5ZaGrafikon[point.Index];
                    var naziv = string.IsNullOrWhiteSpace(s.Naziv) ? s.InventarskiBroj : $"({s.InventarskiBroj}) {s.Naziv}";
                    return $"{point.Model:N0}   {naziv}";
                },
                YToolTipLabelFormatter = point => $"{point.Model:N2}"
            }
        };

        // MaxLimit veci od najveceg iznosa ostavlja prostor sa desne strane najduzeg stubica za labelu
        var maxVrednost = top5.Count > 0 ? top5.Max(s => (double)s.SadasnjaVrednost) : 0;

        TopSredstvaXAxes = new Axis[]
        {
            new Axis { IsVisible = false, MinLimit = 0, MaxLimit = maxVrednost * 1.9 }
        };

        TopSredstvaYAxes = new Axis[]
        {
            new Axis { IsVisible = false }
        };
    }
}
