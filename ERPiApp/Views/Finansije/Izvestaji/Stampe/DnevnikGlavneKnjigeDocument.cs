using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CoreFirma = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Izvestaji.Stampe;

/// <summary>PDF izveštaj — dnevnik glavne knjige (hronološki pregled proknjiženih stavki). Port iz ERPiFinansije (PdfReportService.GenerisiDnevnikPdf), prilagođen na Konto FK umesto string BrojKonta.</summary>
public class DnevnikGlavneKnjigeDocument : IDocument
{
    private readonly CoreFirma? _firma;
    private readonly List<DnevnikRed> _redovi;
    private readonly DateTime? _odDatuma;
    private readonly DateTime? _doDatuma;
    private const string PrimaryColor = "#2563EB";

    public DnevnikGlavneKnjigeDocument(CoreFirma? firma, List<DnevnikRed> redovi, DateTime? odDatuma, DateTime? doDatuma)
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
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("DNEVNIK GLAVNE KNJIGE").SemiBold().FontSize(14).FontColor(PrimaryColor);
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

            col.Item().PaddingTop(4).Text($"Broj proknjiženih stavki: {_redovi.Count}").FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(45);   // Nalog
                columns.ConstantColumn(60);   // Datum
                columns.RelativeColumn(3);    // Dokument/Opis
                columns.ConstantColumn(60);   // Konto
                columns.RelativeColumn(2);    // Naziv konta
                columns.ConstantColumn(70);   // Duguje
                columns.ConstantColumn(70);   // Potrazuje
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Nalog").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Datum").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Dokument / Opis").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Konto").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Naziv konta").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Duguje").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Potražuje").Bold();

                static IContainer HeaderCellStyle(IContainer c) => c.Background(Colors.Blue.Darken3)
                    .PaddingVertical(4).PaddingHorizontal(4)
                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8));
            });

            foreach (var red in _redovi)
            {
                table.Cell().Element(CellStyle).Text(red.BrojNaloga.ToString());
                table.Cell().Element(CellStyle).Text(red.Datum.ToString("dd.MM.yyyy"));
                table.Cell().Element(CellStyle).Text(red.DokumentOpis);
                table.Cell().Element(CellStyle).Text(red.BrojKonta);
                table.Cell().Element(CellStyle).Text(red.NazivKonta);
                table.Cell().Element(CellStyle).AlignRight().Text(red.Duguje.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(red.Potrazuje.ToString("N2"));

                static IContainer CellStyle(IContainer c) => c.BorderBottom(0.5f)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3).PaddingHorizontal(4)
                    .DefaultTextStyle(x => x.FontSize(7.5f));
            }

            table.Cell().ColumnSpan(5).Element(TotalCellStyle).AlignRight().Text("UKUPNO:").Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text(_redovi.Sum(r => r.Duguje).ToString("N2")).Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text(_redovi.Sum(r => r.Potrazuje).ToString("N2")).Bold();

            static IContainer TotalCellStyle(IContainer c) => c.Background(Colors.Grey.Lighten3)
                .PaddingVertical(5).PaddingHorizontal(4);
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

/// <summary>Jedan red dnevnika — jedna stavka naloga, obogaćena podacima naloga radi hronološkog prikaza.</summary>
public class DnevnikRed
{
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string DokumentOpis { get; set; } = "";
    public string BrojKonta { get; set; } = "";
    public string NazivKonta { get; set; } = "";
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
}
