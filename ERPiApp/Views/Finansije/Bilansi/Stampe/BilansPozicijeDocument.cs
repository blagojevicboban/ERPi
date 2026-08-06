using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPiData.Models.Finansije;
using CoreFirma = ERPiData.Models.Core.Firma;

namespace ERPiApp.Views.Finansije.Bilansi.Stampe;

/// <summary>PDF izveštaj — zajednički za Bilans Stanja i Bilans Uspeha (obe zvanične AOP šeme dele isti oblik reda, <see cref="BilansPozicija"/>). Port iz ERPiFinansije (PdfReportService.GenerisiBilansStanjaPdf/GenerisiBilansUspehaPdf, spojeno u jednu klasu jer se razlikuju samo po naslovu/podnaslovu).</summary>
public class BilansPozicijeDocument : IDocument
{
    private readonly CoreFirma? _firma;
    private readonly List<BilansPozicija> _pozicije;
    private readonly string _naslov;
    private readonly string _podnaslov;
    private readonly string _kolonaNaziv;
    private const string PrimaryColor = "#1E3A8A";

    public BilansPozicijeDocument(CoreFirma? firma, List<BilansPozicija> pozicije, string naslov, string podnaslov, string kolonaNaziv)
    {
        _firma = firma;
        _pozicije = pozicije;
        _naslov = naslov;
        _podnaslov = podnaslov;
        _kolonaNaziv = kolonaNaziv;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Portrait());
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            if (_firma != null)
            {
                col.Item().Text(_firma.Naziv).Bold().FontSize(13).FontColor(PrimaryColor);
                col.Item().Text($"PIB: {_firma.Pib ?? "---"} | MB: {_firma.MaticniBroj ?? "---"} | {_firma.PttIMesto}").FontSize(9).FontColor(Colors.Grey.Darken2);
            }
            col.Item().PaddingTop(6).Text(_naslov).Bold().FontSize(15).AlignCenter();
            col.Item().Text(_podnaslov).FontSize(9).Italic().AlignCenter().FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(45);   // AOP
                columns.RelativeColumn();     // Naziv pozicije
                columns.ConstantColumn(65);   // Konta
                columns.ConstantColumn(110);  // Iznos
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("AOP").Bold();
                header.Cell().Element(HeaderCellStyle).Text(_kolonaNaziv).Bold();
                header.Cell().Element(HeaderCellStyle).Text("Konta").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Iznos (RSD)").Bold();

                static IContainer HeaderCellStyle(IContainer c) => c.Background(Colors.Grey.Lighten3)
                    .PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(x => x.SemiBold().FontSize(9));
            });

            foreach (var p in _pozicije)
            {
                bool istaknuto = p.TipPozicije != TipPozicijeBilansa.AopStavka;
                var pozadina = p.TipPozicije switch
                {
                    TipPozicijeBilansa.Naslov => Colors.Grey.Lighten2,
                    TipPozicijeBilansa.Ukupno => Colors.Blue.Lighten4,
                    TipPozicijeBilansa.Grupa => Colors.Grey.Lighten4,
                    _ => Colors.White
                };

                table.Cell().Element(c => CellStyle(c, pozadina)).Text(p.AopCode);
                table.Cell().Element(c => CellStyle(c, pozadina)).Text(t => { if (istaknuto) t.Span(p.Naziv).Bold(); else t.Span(p.Naziv); });
                table.Cell().Element(c => CellStyle(c, pozadina)).Text(p.OpsegKonta);
                table.Cell().Element(c => CellStyle(c, pozadina)).AlignRight().Text(t =>
                {
                    if (istaknuto) t.Span(p.IznosTekucaGodina.ToString("N2")).Bold();
                    else t.Span(p.IznosTekucaGodina.ToString("N2"));
                });

                static IContainer CellStyle(IContainer c, string pozadina) => c.Background(pozadina)
                    .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3).PaddingHorizontal(4).DefaultTextStyle(x => x.FontSize(8.5f));
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(4).Text("(Iznosi su iskazani u RSD po AOP pozicijama APR-a)").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1).AlignCenter();
            col.Item().AlignCenter().Text(x =>
            {
                x.Span("Strana ");
                x.CurrentPageNumber();
                x.Span(" od ");
                x.TotalPages();
            });
        });
    }
}
