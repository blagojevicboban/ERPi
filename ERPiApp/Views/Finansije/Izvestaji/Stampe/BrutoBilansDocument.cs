using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPiData.Services;
using CoreFirma = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Izvestaji.Stampe;

/// <summary>PDF izveštaj — bruto bilans (sintetički promet i saldo po kontima za period). Port iz ERPiFinansije (PdfReportService.GenerisiBrutoBilansPdf) — <see cref="BrutoBilansView"/>-ov "🖨️ PDF" dugme dotad nije stvarno generisalo PDF (samo je prikazivalo poruku o uspehu).</summary>
public class BrutoBilansDocument : IDocument
{
    private readonly CoreFirma? _firma;
    private readonly List<BrutoBilansRed> _redovi;
    private readonly DateTime? _odDatuma;
    private readonly DateTime? _doDatuma;
    private const string PrimaryColor = "#059669";

    public BrutoBilansDocument(CoreFirma? firma, List<BrutoBilansRed> redovi, DateTime? odDatuma, DateTime? doDatuma)
    {
        _firma = firma;
        _redovi = redovi;
        _odDatuma = odDatuma;
        _doDatuma = doDatuma;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Portrait());
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("BRUTO BILANS").SemiBold().FontSize(14).FontColor(PrimaryColor);
                string period = _odDatuma.HasValue || _doDatuma.HasValue
                    ? $"Period: {_odDatuma?.ToString("dd.MM.yyyy.") ?? "—"} - {_doDatuma?.ToString("dd.MM.yyyy.") ?? "—"}"
                    : "Ceo period";
                c.Item().Text(period).FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(220).AlignRight().Column(c =>
            {
                if (_firma != null)
                {
                    c.Item().AlignRight().Text(_firma.Naziv).FontSize(11).SemiBold();
                    if (!string.IsNullOrEmpty(_firma.Pib))
                        c.Item().AlignRight().Text($"PIB: {_firma.Pib}").FontSize(9).FontColor(Colors.Grey.Darken2);
                }
                c.Item().PaddingTop(4).AlignRight().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(2).AlignRight().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2));
                    x.Span("Strana ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(55);   // Konto
                columns.RelativeColumn(3);    // Naziv
                columns.ConstantColumn(85);   // Duguje
                columns.ConstantColumn(85);   // Potrazuje
                columns.ConstantColumn(85);   // Saldo duguje
                columns.ConstantColumn(85);   // Saldo potrazuje
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Konto").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Naziv konta").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Potražuje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Saldo duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Saldo potr.").Bold();

                static IContainer HeaderCellStyle(IContainer c) => c.Background(Colors.Green.Darken3)
                    .PaddingVertical(4).PaddingHorizontal(3)
                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8));
            });

            decimal zbirDuguje = 0, zbirPotrazuje = 0, zbirSaldoDuguje = 0, zbirSaldoPotrazuje = 0;

            foreach (var r in _redovi)
            {
                bool jeTotal = r.Tip != BrutoBilansRedTip.Detalj;

                if (jeTotal)
                {
                    var pozadina = r.Tip == BrutoBilansRedTip.KlasaTotal ? Colors.Grey.Lighten2 : Colors.Grey.Lighten3;
                    table.Cell().ColumnSpan(2).Element(c => TotalCellStyle(c, pozadina)).Text(r.NazivKonta).Bold();
                    table.Cell().Element(c => TotalCellStyle(c, pozadina)).AlignRight().Text(r.Duguje.ToString("N2")).Bold();
                    table.Cell().Element(c => TotalCellStyle(c, pozadina)).AlignRight().Text(r.Potrazuje.ToString("N2")).Bold();
                    table.Cell().Element(c => TotalCellStyle(c, pozadina)).AlignRight().Text(r.SaldoDuguje.ToString("N2")).Bold();
                    table.Cell().Element(c => TotalCellStyle(c, pozadina)).AlignRight().Text(r.SaldoPotrazuje.ToString("N2")).Bold();
                    continue;
                }

                zbirDuguje += r.Duguje;
                zbirPotrazuje += r.Potrazuje;
                zbirSaldoDuguje += r.SaldoDuguje;
                zbirSaldoPotrazuje += r.SaldoPotrazuje;

                table.Cell().Element(CellStyle).Text(r.BrojKonta);
                table.Cell().Element(CellStyle).Text(r.NazivKonta);
                table.Cell().Element(CellStyle).AlignRight().Text(r.Duguje.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.Potrazuje.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.SaldoDuguje.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.SaldoPotrazuje.ToString("N2"));
            }

            table.Cell().ColumnSpan(2).Element(c => TotalCellStyle(c, Colors.Blue.Lighten4)).Text("UKUPNO:").Bold();
            table.Cell().Element(c => TotalCellStyle(c, Colors.Blue.Lighten4)).AlignRight().Text(zbirDuguje.ToString("N2")).Bold();
            table.Cell().Element(c => TotalCellStyle(c, Colors.Blue.Lighten4)).AlignRight().Text(zbirPotrazuje.ToString("N2")).Bold();
            table.Cell().Element(c => TotalCellStyle(c, Colors.Blue.Lighten4)).AlignRight().Text(zbirSaldoDuguje.ToString("N2")).Bold();
            table.Cell().Element(c => TotalCellStyle(c, Colors.Blue.Lighten4)).AlignRight().Text(zbirSaldoPotrazuje.ToString("N2")).Bold();

            static IContainer CellStyle(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(7.5f));

            static IContainer TotalCellStyle(IContainer c, string pozadina) => c.Background(pozadina)
                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(7.5f));
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.DefaultTextStyle(s => s.FontSize(9).SemiBold());
            x.Span("Strana ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }
}
