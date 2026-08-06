using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPiData.Services;
using CoreFirma = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Izvestaji.Stampe;

/// <summary>PDF izveštaj — zaključni list (totali prometa po sintetičkim kontima za period). Port iz ERPiFinansije (PdfReportService.GenerisiZakljucniListPdf), podaci iz već-portovanog BrutoBilansService.GetZakljucniListAsync.</summary>
public class ZakljucniListDocument : IDocument
{
    private readonly CoreFirma? _firma;
    private readonly List<ZakljucniListRed> _redovi;
    private readonly DateTime? _odDatuma;
    private readonly DateTime? _doDatuma;
    private const string PrimaryColor = "#0891B2";

    public ZakljucniListDocument(CoreFirma? firma, List<ZakljucniListRed> redovi, DateTime? odDatuma, DateTime? doDatuma)
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
            page.Size(PageSizes.A4.Landscape());
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
                c.Item().Text("ZAKLJUČNI LIST").SemiBold().FontSize(14).FontColor(PrimaryColor);
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
                columns.RelativeColumn(2);    // Naziv
                columns.ConstantColumn(65);   // Pocetno duguje
                columns.ConstantColumn(65);   // Pocetno potrazuje
                columns.ConstantColumn(65);   // Promet duguje
                columns.ConstantColumn(65);   // Promet potrazuje
                columns.ConstantColumn(65);   // Ukupno duguje
                columns.ConstantColumn(65);   // Ukupno potrazuje
                columns.ConstantColumn(65);   // Saldo duguje
                columns.ConstantColumn(65);   // Saldo potrazuje
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Konto").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Naziv").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Poč. duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Poč. potr.").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Promet duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Promet potr.").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Ukupno duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Ukupno potr.").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Saldo duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Saldo potr.").Bold();

                static IContainer HeaderCellStyle(IContainer c) => c.Background(Colors.Cyan.Darken3)
                    .PaddingVertical(4).PaddingHorizontal(3)
                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(7.5f));
            });

            foreach (var red in _redovi)
            {
                bool jeTotal = red.Tip != BrutoBilansRedTip.Detalj;

                table.Cell().Element(c => CellStyle(c, jeTotal)).Text(red.BrojKonta);
                table.Cell().Element(c => CellStyle(c, jeTotal)).Text(red.NazivKonta);
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.PocetnoDuguje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.PocetnoPotrazuje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.PrometDuguje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.PrometPotrazuje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.UkupnoDuguje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.UkupnoPotrazuje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.SaldoDuguje.ToString("N2"));
                table.Cell().Element(c => CellStyle(c, jeTotal)).AlignRight().Text(red.SaldoPotrazuje.ToString("N2"));
            }

            static IContainer CellStyle(IContainer c, bool jeTotal)
            {
                var cell = c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3).PaddingHorizontal(3);
                cell = jeTotal ? cell.Background(Colors.Grey.Lighten3) : cell;
                return cell.DefaultTextStyle(x => jeTotal ? x.FontSize(7.5f).SemiBold() : x.FontSize(7.5f));
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Strana ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }
}
