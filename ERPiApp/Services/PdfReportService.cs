using System;
using System.Collections.Generic;
using System.Linq;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiData.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERPiApp.Services;

/// <summary>
/// Centralni servis za generisanje PDF izveštaja pomoću QuestPDF biblioteke.
/// Uključuje: Dnevnik knjiženja, Karticu konta, IOS obrazac, Bruto bilans, Kalkulacije, Račune-Otpremnice, Blagajnu i Putne naloge.
/// </summary>
public class PdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerisiNalogePdf(Firma firma, List<Nalog> nalozi)
    {
        return Document.Create(container =>
        {
            foreach (var nalog in nalozi)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).Text($"NALOG ZA KNJIŽENJE br. {nalog.BrojNaloga}").Bold().FontSize(16).AlignCenter();
                        
                        string statusText = nalog.Status == StatusNaloga.Proknjizen ? "PROKNJIŽEN" : "NACRT";
                        col.Item().PaddingTop(3).Text($"Datum: {nalog.DatumNaloga:dd.MM.yyyy}   |   Vrsta: {nalog.VrstaNaloga ?? "Finansijski"}   |   Status: {statusText}").FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken2);
                        
                        if (!string.IsNullOrWhiteSpace(nalog.Opis))
                        {
                            col.Item().PaddingTop(3).Text($"Opis: {nalog.Opis}").FontSize(10).AlignCenter().Italic();
                        }
                        
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // R.br
                                columns.ConstantColumn(80);  // Konto
                                columns.RelativeColumn(3);   // Dokument / Opis
                                columns.ConstantColumn(100); // Duguje
                                columns.ConstantColumn(100); // Potražuje
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("R.br.").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Konto").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Dokument / Opis").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje (RSD)").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje (RSD)").Bold().AlignRight();
                            });

                            decimal zbirDuguje = 0;
                            decimal zbirPotrazuje = 0;
                            int rbr = 1;

                            foreach (var st in nalog.Stavke)
                            {
                                zbirDuguje += st.Duguje;
                                zbirPotrazuje += st.Potrazuje;

                                int displayRbr = st.RedniBroj > 0 ? st.RedniBroj : rbr++;
                                string brojKonta = st.Konto?.BrojKonta ?? "";
                                string tekstDokumentOpis = !string.IsNullOrWhiteSpace(st.BrojDokumenta) && !string.IsNullOrWhiteSpace(st.Opis) && !st.BrojDokumenta.Equals(st.Opis, StringComparison.OrdinalIgnoreCase)
                                    ? $"{st.BrojDokumenta} — {st.Opis}"
                                    : (!string.IsNullOrWhiteSpace(st.BrojDokumenta) ? st.BrojDokumenta : (st.Opis ?? nalog.Opis ?? ""));

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(displayRbr.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(brojKonta);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(tekstDokumentOpis);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Duguje:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Potrazuje:N2}").AlignRight();
                            }

                            table.Cell().ColumnSpan(3).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPNO NALOG:").Bold().AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        });

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Nalog izradio:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Nalog proknjižio:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Odobrio / Kontrolisao:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerisiRacunOtpremnicuPdf(Firma firma, RacunOtpremnica racun)
    {
        var partner = racun.Partner;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(firma.Naziv).Bold().FontSize(14);
                            col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                            col.Item().Text($"Adresa: {firma.Adresa}, {firma.PttIMesto}");
                            if (!string.IsNullOrWhiteSpace(firma.ZiroRacun)) col.Item().Text($"Žiro račun: {firma.ZiroRacun}");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            bool jePredracun = racun.TipDokumenta == TipRacunOtpremnice.Predracun;
                            string naslovDokumenta = jePredracun ? "PREDRAČUN" : "RAČUN - OTPREMNICA";
                            col.Item().Text($"{naslovDokumenta} br. {racun.BrojRacuna}").Bold().FontSize(14).FontColor(jePredracun ? Colors.Orange.Darken2 : Colors.Blue.Darken2);
                            col.Item().Text($"Mesto i datum izdavanja: {firma.PttIMesto ?? "Beograd"}, {racun.DatumRacuna:dd.MM.yyyy}.");
                            if (jePredracun)
                            {
                                if (racun.RokVazenjaPredracuna.HasValue) col.Item().Text($"Rok važenja predračuna: {racun.RokVazenjaPredracuna.Value:dd.MM.yyyy}.");
                            }
                            else
                            {
                                col.Item().Text($"Rok plaćanja: {racun.DatumRacuna.AddDays(racun.RokPlacanjaDana):dd.MM.yyyy}. ({racun.RokPlacanjaDana} dana)");
                            }
                            if (!string.IsNullOrWhiteSpace(racun.NacinPlacanja)) col.Item().Text($"Način plaćanja: {racun.NacinPlacanja}");
                        });
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("KUPAC / PRIMALAC:").Bold().FontSize(10);
                            c.Item().Text(partner?.Naziv ?? "(nepoznat kupac)").Bold();
                            if (partner != null)
                            {
                                c.Item().Text($"PIB: {partner.Pib} | MB: {partner.MaticniBroj}");
                                c.Item().Text($"Adresa: {partner.Adresa}, {partner.PttIMesto}");
                            }
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Rbr
                            columns.ConstantColumn(70);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(35);  // J.M.
                            columns.ConstantColumn(50);  // Kol
                            columns.ConstantColumn(60);  // Cena
                            columns.ConstantColumn(40);  // Rabat
                            columns.ConstantColumn(40);  // PDV%
                            columns.ConstantColumn(65);  // Osnovica
                            columns.ConstantColumn(70);  // Ukupno
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla / robe").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Rab%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("PDV%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Osnovica").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Ukupno").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in racun.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.Artikal?.SifraArtikla ?? "USL").Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.Artikal?.Naziv ?? st.OpisUsluge ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.Artikal?.JedinicaMere ?? st.JedinicaMereUsluge ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.ProdajnaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.RabatProcenat:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.StopaPdv:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Osnovica:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Ukupno:N2}").AlignRight();
                            rbr++;
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(260).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                        t.Cell().Text("Ukupno osnovica bez PDV:").Bold();
                        t.Cell().Text($"{racun.UkupnoOsnovica:N2} RSD").AlignRight();
                        t.Cell().Text("Ukupno PDV:").Bold();
                        t.Cell().Text($"{racun.UkupnoPdv:N2} RSD").AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("ZA UPLATU:").Bold().FontSize(11);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{racun.UkupnoZaUplatu:N2} RSD").Bold().FontSize(11).AlignRight();
                    });

                    col.Item().PaddingTop(20).Column(c =>
                    {
                        if (racun.TipDokumenta == TipRacunOtpremnice.Predracun)
                        {
                            c.Item().Text("Ovaj predračun ne predstavlja fakturu niti obavezu plaćanja, već služi za informisanje o uslovima buduće isporuke.").FontSize(8);
                            c.Item().Text($"Plaćanje: {racun.NacinPlacanja ?? "Virman"}.").FontSize(8);
                        }
                        else
                        {
                            c.Item().Text($"Roba otpremljena uz otpremnicu broj {racun.BrojOtpremnice ?? racun.BrojRacuna.ToString()}.").FontSize(8);
                            c.Item().Text($"Plaćanje: {racun.NacinPlacanja ?? "Virman"} u roku od {racun.RokPlacanjaDana} dana od datuma prijema robe.").FontSize(8);
                        }
                        c.Item().Text("U slučaju spora nadležan je stvarno i mesno nadležni sud.").FontSize(8);
                        c.Item().Text("Ovaj dokument je punovažan bez potpisa i pečata.").FontSize(8);
                    });

                    col.Item().PaddingTop(25).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Robu izdao / Fakturisao:").Italic();
                            c.Item().PaddingTop(20).Text("_______________________");
                        });
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Robu primio / Kupac:").Italic();
                            c.Item().PaddingTop(20).Text("_______________________");
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiDnevnikPdf(Firma firma, List<Nalog> nalozi, Dictionary<int, string>? promene = null)
    {
        promene ??= new Dictionary<int, string>();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("DNEVNIK KNJIŽENJA (GLAVNA KNJIGA)").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Nalog").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Dokument / Opis").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Promena").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje (RSD)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje (RSD)").Bold().AlignRight();
                        });

                        decimal zbirDuguje = 0;
                        decimal zbirPotrazuje = 0;

                        foreach (var nalog in nalozi)
                        {
                            foreach (var st in nalog.Stavke)
                            {
                                zbirDuguje += st.Duguje;
                                zbirPotrazuje += st.Potrazuje;

                                string opisPromene = st.Opis ?? "";
                                string brojKonta = st.Konto?.BrojKonta ?? "";
                                string prikazDokumentOpis = !string.IsNullOrWhiteSpace(st.BrojDokumenta) ? st.BrojDokumenta : (nalog.Opis ?? "");

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(nalog.BrojNaloga.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(nalog.DatumNaloga.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(prikazDokumentOpis);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(brojKonta);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(opisPromene);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Duguje:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Potrazuje:N2}").AlignRight();
                            }
                        }

                        table.Cell().ColumnSpan(5).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPAN PROMET DNEVNIKA:").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiKarticuPdf(Firma firma, Konto konto, List<KarticaRed> stavke,
        DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            container.Page(page => ComposeKarticaPage(page, firma, konto, stavke, odDatuma, doDatuma));
        }).GeneratePdf();
    }

    public static byte[] GenerisiViseKarticaPdf(Firma firma, List<(Konto Konto, List<KarticaRed> Stavke)> kartice,
        DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            foreach (var (konto, stavke) in kartice)
            {
                container.Page(page => ComposeKarticaPage(page, firma, konto, stavke, odDatuma, doDatuma));
            }
        }).GeneratePdf();
    }

    private static void ComposeKarticaPage(PageDescriptor page, Firma firma, Konto konto, List<KarticaRed> stavke,
        DateTime? odDatuma, DateTime? doDatuma)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

        page.Header().Column(col =>
        {
            col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
            col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
            col.Item().PaddingTop(10).Text("KARTICA KONTA").Bold().FontSize(16).AlignCenter();
            col.Item().Text($"{konto.BrojKonta} — {konto.NazivKonta}").FontSize(12).AlignCenter();
            if (odDatuma.HasValue || doDatuma.HasValue)
                col.Item().Text($"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}").FontSize(9).AlignCenter().FontColor(Colors.Grey.Medium);
            col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });

        page.Content().PaddingVertical(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(45);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Nalog").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Opis").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Promena").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje").Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje").Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Saldo").Bold().AlignRight();
                });

                decimal zbirDuguje = 0, zbirPotrazuje = 0;

                foreach (var s in stavke)
                {
                    zbirDuguje += s.Duguje;
                    zbirPotrazuje += s.Potrazuje;

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.Datum.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.BrojNaloga.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.Opis ?? "");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.OpisPromene ?? "");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Duguje:N2}").AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Potrazuje:N2}").AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Saldo:N2}").AlignRight();
                }

                table.Cell().ColumnSpan(4).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPNO:").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{(stavke.Count > 0 ? stavke[^1].Saldo : 0m):N2}").Bold().AlignRight();
            });
        });

        page.Footer().AlignRight().Text(x =>
        {
            x.Span("Stranica ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }

    public static byte[] GenerisiIOSPdf(Firma firma, Partner partner, List<KarticaRed> stavke)
    {
        var grupa = new IosPartnerGrupa
        {
            SifraPartnera = partner.SifraPartnera,
            NazivPartnera = partner.Naziv,
            Konto = partner.KontoPartnera ?? partner.SifraPartnera,
            Adresa = partner.Adresa,
            PttIMesto = partner.PttIMesto,
            Pib = partner.Pib,
            Partner = partner,
            Stavke = stavke
        };
        return GenerisiZbirniIOSPdf(firma, new List<IosPartnerGrupa> { grupa });
    }

    public static byte[] GenerisiZbirniIOSPdf(Firma firma, List<IosPartnerGrupa> grupe, string? odKonta = null, string? doKonta = null, DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            if (grupe.Count == 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).Text("IZVOD OTVORENIH STAVKI (IOS)").Bold().FontSize(16).AlignCenter();
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(30).AlignCenter().Text("Nema otvorenih stavki za izabrani opseg i kriterijume.").FontSize(12).FontColor(Colors.Grey.Medium).Italic();
                });
                return;
            }

            foreach (var grupa in grupe)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text(firma.Naziv).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                                left.Item().Text(firma.Adresa ?? "").FontSize(9);
                                left.Item().Text(firma.PttIMesto ?? "").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(firma.Telefon))
                                    left.Item().Text($"Tel: {firma.Telefon}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(firma.ZiroRacun))
                                    left.Item().Text($"Žiro račun: {firma.ZiroRacun}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(firma.Pib))
                                    left.Item().Text($"PIB: {firma.Pib}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text($"DATUM: {DateTime.Now:dd.MM.yyyy}").FontSize(9).Bold();
                                right.Item().PaddingTop(4).Text($"DUŽNIK: {grupa.Konto} / {grupa.SifraPartnera}").FontSize(10).Bold();
                                right.Item().Text(grupa.NazivPartnera).FontSize(11).Bold();
                                if (!string.IsNullOrWhiteSpace(grupa.Adresa))
                                    right.Item().Text($"{grupa.Adresa}, {grupa.PttIMesto}").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(grupa.Pib))
                                    right.Item().Text($"PIB: {grupa.Pib}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        col.Item().PaddingTop(10).Text("I Z V O D   O T V O R E N I H   S T A V K I").Bold().FontSize(14).AlignCenter();
                        col.Item().PaddingTop(2).AlignCenter().Text("___________________________________________").FontSize(10).FontColor(Colors.Grey.Medium);

                        decimal netoSaldo = grupa.Saldo;
                        string uKorist = netoSaldo >= 0 ? "našu korist" : "Vašu korist";

                        col.Item().PaddingTop(8).Text($"Na osnovu naše evidencije utvrdili smo saldo od {Math.Abs(netoSaldo):N2} din. u {uKorist}.").FontSize(9.5f);
                        col.Item().Text("Molimo Vas da uporedite stanje na kartici sa našim stanjem.").FontSize(9.5f);
                        col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(58);
                                columns.ConstantColumn(42);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(75);
                                columns.ConstantColumn(75);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Datum").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nalog").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Opis promene").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Broj dokumenta").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Duguje").Bold().FontSize(8).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Potražuje").Bold().FontSize(8).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Saldo").Bold().FontSize(8).AlignRight();
                            });

                            decimal zbirDuguje = 0, zbirPotrazuje = 0;

                            foreach (var s in grupa.Stavke)
                            {
                                zbirDuguje += s.Duguje;
                                zbirPotrazuje += s.Potrazuje;

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.Datum.ToString("dd.MM.yyyy")).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.BrojNaloga.ToString()).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.Opis ?? "").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.OpisPromene ?? "").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Duguje:N2}").FontSize(8).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Potrazuje:N2}").FontSize(8).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Saldo:N2}").FontSize(8).AlignRight();
                            }

                            table.Cell().ColumnSpan(4).PaddingVertical(3).PaddingHorizontal(2).Text("UKUPNO:").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{zbirDuguje:N2}").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{zbirPotrazuje:N2}").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{grupa.Saldo:N2}").Bold().FontSize(8.5f).AlignRight();
                        });

                        if (grupa.Stavke.Count == 0)
                        {
                            col.Item().PaddingTop(15).AlignCenter().Text("Nema proknjiženih otvorenih stavki.").FontColor(Colors.Grey.Medium).Italic();
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerisiBlagajnickiNalogPdf(Firma firma, BlagajnickiNalog bn)
    {
        bool isUplata = bn.VrstaNaloga == VrstaBlagajnickogNaloga.Uplata;
        string naslov = isUplata ? "NALOG ZA UPLATU U BLAGAJNU (UPLATNICA)" : "NALOG ZA ISPLATU IZ BLAGAJNE (ISPLATNICA)";
        string valuta = bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "DEV" : "RSD";
        string naslovLica = isUplata ? "Uplatilac:" : "Primalac isplate:";
        string naslovSvrhe = isUplata ? "Svrha uplate:" : "Svrha isplate:";
        string bojaAkcent = isUplata ? Colors.Green.Medium : Colors.Orange.Darken1;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(firma.Naziv).Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Broj: {bn.BrojNaloga}").Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Datum: {bn.Datum:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                    col.Item().PaddingTop(6).Text(naslov).Bold().FontSize(14).FontColor(bojaAkcent).AlignCenter();
                    col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(bojaAkcent);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Vrsta blagajne:").Bold();
                            r.RelativeItem().Text(bn.VrstaBlagajne == VrstaBlagajne.Devizna ? "Devizna blagajna (2440)" : "Dinarska blagajna (2430)");
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(120).Text(naslovLica).Bold();
                            r.RelativeItem().Text(bn.UplatilacIsplatilac ?? "---").FontSize(11).Bold();
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(120).Text(naslovSvrhe).Bold();
                            r.RelativeItem().Text(bn.Svrha ?? "---");
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(120).Text("Protivkonto:").Bold();
                            r.RelativeItem().Text(string.IsNullOrWhiteSpace(bn.BrojKontaProtu) ? "2410" : bn.BrojKontaProtu);
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(120).Text("Status:").Bold();
                            r.RelativeItem().Text(bn.Status);
                        });

                        c.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        c.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Text("IZNOS ZA PREUZIMANJE / UPLATU:").Bold().FontSize(12);
                            r.RelativeItem().AlignRight().Text($"{bn.Iznos:N2} {valuta}").Bold().FontSize(16).FontColor(bojaAkcent);
                        });
                    });

                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Uplatio / Primio:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Blagajnik:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Likvidator / Kontrolor:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiBlagajnickiDnevnikPdf(Firma firma, VrstaBlagajne vrsta, DateTime odD, DateTime doD, List<BlagajnickiDnevnikRed> redovi, BlagajnickiDnevnikSummary summary)
    {
        string nazivBlagajne = vrsta == VrstaBlagajne.Devizna ? "DEVIZNA BLAGAJNA (2440)" : "DINARSKA BLAGAJNA (2430)";
        string valuta = vrsta == VrstaBlagajne.Devizna ? "DEV" : "RSD";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(13).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).Text($"BLAGAJNIČKI DNEVNIK — {nazivBlagajne}").Bold().FontSize(15).AlignCenter();
                    col.Item().Text($"Za period: {odD:dd.MM.yyyy} do {doD:dd.MM.yyyy}").FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).PaddingVertical(6).PaddingHorizontal(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Početno stanje: {summary.PocetnoStanje:N2} {valuta}").Bold();
                        row.RelativeItem().Text($"Ukupno uplate: {summary.UkupnoUplata:N2} {valuta}").Bold().FontColor(Colors.Green.Medium);
                        row.RelativeItem().Text($"Ukupno isplate: {summary.UkupnoIsplata:N2} {valuta}").Bold().FontColor(Colors.Orange.Darken2);
                        row.RelativeItem().AlignRight().Text($"Krajnje stanje: {summary.KrajnjeStanje:N2} {valuta}").Bold().FontColor(Colors.Blue.Darken2);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65); // Datum
                            columns.ConstantColumn(85); // Broj naloga
                            columns.RelativeColumn(2f);  // Lice / Svrha
                            columns.ConstantColumn(45); // Konto
                            columns.ConstantColumn(70); // Uplata
                            columns.ConstantColumn(70); // Isplata
                            columns.ConstantColumn(75); // Saldo
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Broj naloga").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Uplatilac / Isplatilac — Svrha").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Uplata").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Isplata").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Saldo").Bold().AlignRight();
                        });

                        foreach (var r in redovi)
                        {
                            string opisPrikaz = $"{r.UplatilacIsplatilac} — {r.Svrha}";
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(r.Datum.ToString("dd.MM.yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(r.BrojNaloga);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(opisPrikaz);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(r.BrojKontaProtu);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(r.Uplata > 0 ? $"{r.Uplata:N2}" : "-").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(r.Isplata > 0 ? $"{r.Isplata:N2}" : "-").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{r.Saldo:N2}").Bold().AlignRight();
                        }

                        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text("SVEGA DNEVNIK:").Bold().AlignRight();
                        table.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text($"{summary.UkupnoUplata:N2}").Bold().AlignRight();
                        table.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text($"{summary.UkupnoIsplata:N2}").Bold().AlignRight();
                        table.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text($"{summary.KrajnjeStanje:N2}").Bold().AlignRight();
                    });

                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Blagajnik:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(20).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Kontrolisao / Likvidator:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(20).Text("_______________________").AlignCenter();
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiPutniNalogPdf(Firma firma, PutniNalog pn)
    {
        string tipSputa = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "u inostranstvu" : "u zemlji";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(firma.Naziv).Bold().FontSize(13).FontColor(Colors.Blue.Medium);
                            c.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Nalog br: {pn.BrojNaloga}").Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Datum izdavanja: {pn.DatumPolaska:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                    col.Item().PaddingTop(10).Text($"PUTNI NALOG ZA SLUŽBENO PUTOVANJE ({tipSputa.ToUpper()})").Bold().FontSize(15).AlignCenter().FontColor(Colors.Indigo.Darken2);
                    col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Indigo.Medium);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                    {
                        c.Item().Text("1. PODACI O ZAPOSLENOM I NALOGU ZA PUTOVANJE").Bold().FontSize(10.5f).FontColor(Colors.Indigo.Darken2);
                        c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Zaposleni: ").Bold(); tx.Span(pn.ZaposleniIme ?? "---"); });
                            r.RelativeItem().Text(tx => { tx.Span("Radno mesto: ").Bold(); tx.Span(pn.RadnoMesto ?? "---"); });
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Relacija putovanja: ").Bold(); tx.Span(pn.Relacija ?? "---"); });
                            r.RelativeItem().Text(tx => { tx.Span("Prevozno sredstvo: ").Bold(); tx.Span(pn.PrevoznoSredstvo ?? "---"); });
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Svrha putovanja: ").Bold(); tx.Span(pn.SvrhaPutovanja ?? "---"); });
                            r.RelativeItem().Text(tx => { tx.Span("Status: ").Bold(); tx.Span(pn.Status ?? "Nacrt"); });
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Datum polaska: ").Bold(); tx.Span($"{pn.DatumPolaska:dd.MM.yyyy HH:mm}"); });
                            r.RelativeItem().Text(tx => { tx.Span("Datum povratka: ").Bold(); tx.Span($"{pn.DatumPovratka:dd.MM.yyyy HH:mm}"); });
                        });

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Trajanje: ").Bold(); tx.Span($"{pn.TrajanjeSati:N1} sati ({pn.BrojDnevnica:N1} dnevnica x {pn.IznosDnevniceRsd:N2} RSD)"); });
                            r.RelativeItem().Text(tx => { tx.Span("Ukupno dnevnice: ").Bold(); tx.Span($"{pn.UkupnoDnevnice:N2} RSD"); });
                        });
                    });

                    col.Item().PaddingTop(12).Text("2. OBRAČUN POJEDINAČNIH PUTNIH TROŠKOVA I RAČUNA").Bold().FontSize(10.5f).FontColor(Colors.Indigo.Darken2);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);  // R.br
                            columns.ConstantColumn(85);  // Vrsta troška
                            columns.ConstantColumn(95);  // Broj računa
                            columns.ConstantColumn(75);  // Datum
                            columns.RelativeColumn(2f);  // Opis
                            columns.ConstantColumn(85);  // Iznos (RSD)
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Vrsta troška").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Broj računa").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Opis").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Iznos (RSD)").Bold().AlignRight();
                        });

                        int rb = 1;
                        decimal ukupnoStavke = 0;
                        foreach (var st in pn.StavkeTroskova)
                        {
                            ukupnoStavke += st.Iznos;
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text((st.RedniBroj > 0 ? st.RedniBroj : rb++).ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.VrstaTroska);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.BrojRacuna);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.DatumRacuna.ToString("dd.MM.yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.Opis);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Iznos:N2}").AlignRight();
                        }

                        if (!pn.StavkeTroskova.Any())
                        {
                            table.Cell().ColumnSpan(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4).Text("Nema unetih pojedinačnih računa troškova.").Italic().AlignCenter();
                        }

                        table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text("UKUPNO PRIZNATI TROŠKOVI (BEZ DNEVNICA):").Bold().AlignRight();
                        table.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text($"{ukupnoStavke:N2}").Bold().AlignRight();
                    });

                    decimal ukupniTroskovi = pn.UkupnoDnevnice + pn.TroskoviGoriva + pn.TroskoviSmestaja + pn.TroskoviPrevoza + pn.OstaliTroskovi;

                    col.Item().PaddingTop(12).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                    {
                        c.Item().Text("3. REKAPITULACIJA I OBRAČUN ZA ISPLATU").Bold().FontSize(10.5f).FontColor(Colors.Indigo.Darken2);
                        c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("• Obračunate dnevnice:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.UkupnoDnevnice:N2} RSD");
                        });
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("• Troškovi goriva:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.TroskoviGoriva:N2} RSD");
                        });
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("• Troškovi smeštaja:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.TroskoviSmestaja:N2} RSD");
                        });
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("• Troškovi prevoza / putarina:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.TroskoviPrevoza:N2} RSD");
                        });
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("• Ostali službeni troškovi:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.OstaliTroskovi:N2} RSD");
                        });

                        c.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("SVEGA TROŠKOVI SLUŽBENOG PUTA:").Bold();
                            r.ConstantItem(120).AlignRight().Text($"{ukupniTroskovi:N2} RSD").Bold();
                        });

                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Isplaćena akontacija:");
                            r.ConstantItem(120).AlignRight().Text($"{pn.Akontacija:N2} RSD");
                        });

                        c.Item().PaddingTop(6).Background(Colors.Indigo.Lighten5).Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("UKUPNO ZA ISPLATU / (POVRAĆAJ):").Bold().FontSize(11).FontColor(Colors.Indigo.Darken2);
                            r.ConstantItem(140).AlignRight().Text($"{pn.UkupnoZaIsplatu:N2} RSD").Bold().FontSize(13).FontColor(Colors.Indigo.Darken2);
                        });
                    });

                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Nalogodavac:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Podnosilac obračuna (Zaposleni):").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Obračunao / Likvidator:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiSifarnikMestaTroskaPdf(Firma firma, List<MestoTroska> lista)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("ŠIFARNIK MESTA TROŠKA I PROJEKATA").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(110);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Naziv").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Tip jedinice").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Status").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Napomena").Bold();
                        });

                        foreach (var m in lista.OrderBy(x => x.Sifra))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(m.Sifra);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(m.Naziv);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(m.Tip.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(m.IsAktivno ? "Aktivno" : "Neaktivno");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(m.Napomena);
                        }

                        if (!lista.Any())
                        {
                            table.Cell().ColumnSpan(5).PaddingVertical(8).Text("Nema unetih mesta troška/projekata.").Italic().AlignCenter();
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiKompenzacijuPdf(Firma firma, Kompenzacija k)
    {
        string nazivVrste = k.Vrsta switch
        {
            VrstaKompenzacije.Asignacija => "IZJAVA O ASIGNACIJI",
            VrstaKompenzacije.Cesija => "IZJAVA O CESIJI",
            _ => "IZJAVA O KOMPENZACIJI (PREBIJANJU POTRAŽIVANJA I OBAVEZA)"
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(firma.Naziv).Bold().FontSize(13).FontColor(Colors.Blue.Medium);
                            c.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Broj: {k.BrojDokumenta}").Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Datum: {k.Datum:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                    col.Item().PaddingTop(10).Text(nazivVrste).Bold().FontSize(15).AlignCenter().FontColor(Colors.Purple.Darken2);
                    col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Purple.Medium);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                    {
                        c.Item().Text("UGOVORNE STRANE").Bold().FontSize(10.5f).FontColor(Colors.Purple.Darken2);
                        c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Strana 1: ").Bold(); tx.Span($"{k.NazivPartnera} (konto {k.KontoPartnera1 ?? "---"})"); });
                        });
                        if (!string.IsNullOrWhiteSpace(k.NazivPartnera2))
                        {
                            c.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(tx => { tx.Span("Strana 2: ").Bold(); tx.Span($"{k.NazivPartnera2} (konto {k.KontoPartnera2 ?? "---"})"); });
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(k.NazivPartnera3))
                        {
                            c.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(tx => { tx.Span("Strana 3: ").Bold(); tx.Span($"{k.NazivPartnera3} (konto {k.KontoPartnera3 ?? "---"})"); });
                            });
                        }

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(tx => { tx.Span("Vrsta: ").Bold(); tx.Span(k.Vrsta.ToString()); });
                            r.RelativeItem().Text(tx => { tx.Span("Status: ").Bold(); tx.Span(k.Status); });
                        });

                        if (!string.IsNullOrWhiteSpace(k.Napomena))
                        {
                            c.Item().PaddingTop(6).Text(tx => { tx.Span("Napomena: ").Bold(); tx.Span(k.Napomena); });
                        }
                    });

                    col.Item().PaddingTop(12).Text("STAVKE OBUHVAĆENE PORAVNANJEM").Bold().FontSize(10.5f).FontColor(Colors.Purple.Darken2);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // R.br
                            columns.ConstantColumn(65);  // Strana
                            columns.ConstantColumn(50);  // Konto
                            columns.RelativeColumn(2f);  // Broj dokumenta
                            columns.ConstantColumn(70);  // Datum
                            columns.ConstantColumn(90);  // Iznos
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Strana").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Broj dokumenta / fakture").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Iznos (RSD)").Bold().AlignRight();
                        });

                        foreach (var st in k.Stavke.OrderBy(s => s.RedniBroj))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.RedniBroj.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.Strana);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.BrojKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.BrojDokumenta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.DatumDokumenta.ToString("dd.MM.yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.IznosZaKompenzaciju:N2}").AlignRight();
                        }

                        if (!k.Stavke.Any())
                        {
                            table.Cell().ColumnSpan(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4).Text("Nema unetih stavki.").Italic().AlignCenter();
                        }

                        table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text("UKUPAN IZNOS KOMPENZACIJE:").Bold().AlignRight();
                        table.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).Text($"{k.UkupanIznosKompenzacije:N2}").Bold().AlignRight();
                    });

                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Strana 1:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Strana 2:").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Strana 3 (ako postoji):").FontSize(9).AlignCenter();
                            c.Item().PaddingTop(24).Text("_______________________").AlignCenter();
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
