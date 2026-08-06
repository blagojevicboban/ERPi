using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ERPiApp.Views.Pomoc;

public partial class PomocPage : Page
{
    private readonly List<PomocTema> _sveTeme;

    public PomocPage(string? initijalnaTema = null)
    {
        InitializeComponent();

        _sveTeme = InicijalizujSveTeme();

        Loaded += (_, _) =>
        {
            PrimeniFiltere(initijalnaTema);
        };
    }

    private List<PomocTema> InicijalizujSveTeme()
    {
        var teme = new List<PomocTema>();

        // 🌐 OPŠTE & ERPi HUB TEME
        teme.AddRange(new[]
        {
            new PomocTema
            {
                Naslov = "👋 Dobrodošli u ERPi",
                Modul = "Opšte",
                Sadrzaj = "ERPi je integrisani savremeni desktop ERP sistem za Finansije, Osnovna Sredstva i Obračun Zarada, razvijen po uzoru na legacy DOS/Clipper sisteme sa savremenom grafikom i unified baze podataka.\n\n" +
                          "KLJUČNE FUNKCIJE:\n" +
                          "• Rad sa neograničenim brojem firmi u istoj instalaciji.\n" +
                          "• Brz uvoz starih podataka iz DOS programa.\n" +
                          "• Integrisani F1 sistem pomoći i izvoz zvanične PDF i XML dokumentacije (SEF, ePorezi, PPP-PD)."
            },
            new PomocTema
            {
                Naslov = "🔐 Prijava, korisnici i RBAC uloge",
                Modul = "Opšte",
                Kljuc = "korisnici",
                Sadrzaj = "Pristup aplikaciji je zaštićen korisničkim nalozima.\n\n" +
                          "1. ULOGE:\n" +
                          "• Administrator: Puni pristup svim funkcijama, upravljanje korisnicima, rasknjižavanje naloga i restauracija rezervnih kopija.\n" +
                          "• Operater / Knjigovođa: Unos i knjiženje dokumenata, rad sa nalozima, sredstvima i radnim satima.\n" +
                          "• Auditor / Gledalac: Pregled podataka i generisanje izveštaja bez prava unosa.\n\n" +
                          "2. BEZBEDNOST:\n" +
                          "Lozinke se čuvaju osoljene i kriptovane PBKDF2 HMAC-SHA256 algoritmom sa 100.000 iteracija."
            },
            new PomocTema
            {
                Naslov = "🏢 Upravljanje firmama",
                Modul = "Opšte",
                Kljuc = "firme",
                Sadrzaj = "Meni '🏢 Upravljanje firmama' omogućava dodavanje preduzeća i brzi rad u bazi izabrane firme.\n\n" +
                          "• Dodajte novu firmu i popunite matične podatke (PIB, MBR, tekući račun, adresa).\n" +
                          "• Klikom na '⭐ Postavi kao aktivnu' prebacujete se na bazu podataka izabrane firme."
            },
            new PomocTema
            {
                Naslov = "💾 Backup i restauracija baze",
                Modul = "Opšte",
                Sadrzaj = "Podešavanje automatskog pravljenja rezervnih kopija (auto-backup) i ručni uvoz/restauracija baze podataka u sekciji Podešavanja."
            }
        });

        // 💰 FINANSIJE & ROBNO TEME
        teme.AddRange(new[]
        {
            new PomocTema
            {
                Naslov = "📖 Glavna knjiga i Nalozi za knjiženje",
                Modul = "Finansije",
                Kljuc = "Nalozi",
                Sadrzaj = "Meni '📖 Glavna knjiga i Nalozi' služi za dvostruko knjigovodstveno knjiženje.\n\n" +
                          "1. UNOS NOVOG NALOGA:\n" +
                          "• Kliknite na dugme '➕ Novi nalog', unesite broj, datum i opis.\n" +
                          "• Dodajte stavke. Na dnu se u realnom vremenu prikazuje ŽIVA PROVERA RAVNOTEŽE (Duguje = Potražuje). Knjiženje je dozvoljeno samo kad je saldo 0.00 RSD.\n" +
                          "• Taster F2 u polju opisa stavke otvara brzi šifarnik opisa promena.\n\n" +
                          "2. KNJIŽENJE I RASKNJIŽAVANJE:\n" +
                          "• Dugme 'Proknjiži' zaključava nalog.\n" +
                          "• Dugme 'Rasknjiži' (administratori) vraća nalog u nacrt radi ispravki uz zapis u audit logu."
            },
            new PomocTema
            {
                Naslov = "📋 Kontni plan",
                Modul = "Finansije",
                Kljuc = "Konta",
                Sadrzaj = "Meni '📋 Kontni plan' sadrži šifarnik svih konta po hijerarhiji (klasa, grupa, sintetika, analitika).\n\n" +
                          "Konta sa proknjiženim prometom se ne mogu brisati radi zaštite integriteta Glavne knjige."
            },
            new PomocTema
            {
                Naslov = "📋 Dnevnik i Kartice konta",
                Modul = "Finansije",
                Kljuc = "Kartice",
                Sadrzaj = "Hronološki uvid u promet konta sa filtriranjem po datumu i masovnom štampom u PDF/Excel."
            },
            new PomocTema
            {
                Naslov = "👥 Partneri i Otvorene stavke (IOS)",
                Modul = "Finansije",
                Kljuc = "Partneri",
                Sadrzaj = "Analitičke kartice kupaca i dobavljača, ručno i automatsko zatvaranje otvorenih stavki i štampa IOS obrazaca."
            },
            new PomocTema
            {
                Naslov = "🤝 Kompenzacije, Asignacije i Cesije",
                Modul = "Finansije",
                Kljuc = "Kompenzacije",
                Sadrzaj = "Meni za prebijanje obostranih dugovanja i potraživanja bez novčanog prometa. Podržane dvojne kompenzacije i trojne asignacije/cesije sa pametnim skeniranjem obostranih dugovanja."
            },
            new PomocTema
            {
                Naslov = "📦 Robno knjigovodstvo (VP/MP, Fakture, Nivelacije)",
                Modul = "Finansije",
                Kljuc = "Robno",
                Sadrzaj = "Kalkulacije nabavke robe (VP/MP), izlazni računi-otpremnice sa slanjem na e-Fakture (SEF) i nivelacije cena po magacinima."
            },
            new PomocTema
            {
                Naslov = "🏭 Materijalno knjigovodstvo i Zalihe",
                Modul = "Finansije",
                Kljuc = "Magacin",
                Sadrzaj = "Prijemnice, trebovanja i izdatnice materijala po ponderisanoj prosečnoj ceni, uz popisne liste i proveru integriteta."
            },
            new PomocTema
            {
                Naslov = "🧾 PDV Evidencija (KPR, KIR i PP-PDV XML)",
                Modul = "Finansije",
                Kljuc = "Pdv",
                Sadrzaj = "Knjige ulaznih i izlaznih računa, POPDV analiza i izvoz zvaničnog PP-PDV XML fajla za direktno učitavanje na portal ePorezi."
            },
            new PomocTema
            {
                Naslov = "🏛️ Zvanični APR Bilansi i Poreski Bilans (PB-1)",
                Modul = "Finansije",
                Kljuc = "Bilansi",
                Sadrzaj = "Bilans stanja, Bilans uspeha, Statistički izveštaj, Tokovi gotovine, Promene na kapitalu i Poreski Bilans PB-1 (sa Obrazcem OA i PDP prijavom)."
            },
            new PomocTema
            {
                Naslov = "🔍 AI / OCR Čitač skeniranih računa",
                Modul = "Finansije",
                Sadrzaj = "Automatsko OCR parsiranje teksta sa skeniranih PDF računa u DMS-u (PIB, datum, iznos, PDV) i kreiranje uravnoteženog naloga knjiženja u 1-klik."
            },
            new PomocTema
            {
                Naslov = "🏦 Uvoz elektronskih bankarskih izvoda",
                Modul = "Finansije",
                Sadrzaj = "Uvoz izvoda (Halcom, Asseco, CAMT.053, MT940) sa automatskim uparivanjem nalogodavca, računa i zatvaranjem otvorenih stavki."
            }
        });

        // 🏗️ OSNOVNA SREDSTVA TEME
        teme.AddRange(new[]
        {
            new PomocTema
            {
                Naslov = "🏗️ Osnovna sredstva (Kartice)",
                Modul = "Sredstva",
                Kljuc = "sredstva",
                Sadrzaj = "Meni '🏗️ Osnovna sredstva' omogućava upravljanje katalogom svih osnovnih sredstava preduzeća sa praćenjem nabavne, otpisane i sadašnje vrednosti po kontima i RJ."
            },
            new PomocTema
            {
                Naslov = "📥 Prijava sredstava",
                Modul = "Sredstva",
                Kljuc = "prijava",
                Sadrzaj = "Evidencija nabavke i aktiviranja novih osnovnih sredstava kroz naloge za prijavu sa dobavljačima i stavkama."
            },
            new PomocTema
            {
                Naslov = "📤 Rashod i promene",
                Modul = "Sredstva",
                Kljuc = "rashod",
                Sadrzaj = "Evidencija rashodovanja, prodaje, otuđenja, prenosa i povećanja vrednosti osnovnih sredstava."
            },
            new PomocTema
            {
                Naslov = "⚙️ Računovodstvena i Poreska Amortizacija",
                Modul = "Sredstva",
                Kljuc = "amortizacija",
                Sadrzaj = "Obračun računovodstvene amortizacije (MRS 16) sa rezidualnom vrednošću i pojedinačna poreska amortizacija po zakonskim grupama I–V (Obrazac OA i PB-1)."
            },
            new PomocTema
            {
                Naslov = "📋 Popis i Nalepnice (Bar-kodovi)",
                Modul = "Sredstva",
                Kljuc = "popis",
                Sadrzaj = "Formiranje popisnih komisija, štampa praznih listi za terenski popis, masovni unos stvarnog stanja i štampa CODE-128 bar-kod nalepnica."
            }
        });

        // 👥 OBRAČUN ZARADA TEME
        teme.AddRange(new[]
        {
            new PomocTema
            {
                Naslov = "🚀 Brzi start — tok obračuna zarada",
                Modul = "Zarade",
                Sadrzaj = "Standardni mesečni tok rada obrade zarada:\n\n" +
                          "1. Kreirajte novi obračunski period.\n" +
                          "2. Proverite parametre perioda (vrednost boda i fond časova).\n" +
                          "3. Unesite radne sate zaposlenih.\n" +
                          "4. Sačuvajte i preračunajte (bruto, porez, doprinosi, neto).\n" +
                          "5. Odštampajte platne listiće, spiskove i generišite PPP-PD XML."
            },
            new PomocTema
            {
                Naslov = "👤 Zaposleni i kadrovska evidencija",
                Modul = "Zarade",
                Kljuc = "Radnici",
                Sadrzaj = "Matični podaci zaposlenih: JMBG, koeficijent, ugovorena plata, minuli rad, poreske olakšice i tekući račun."
            },
            new PomocTema
            {
                Naslov = "⏱️ Radni sati i Obračun",
                Modul = "Zarade",
                Kljuc = "RadniSati",
                Sadrzaj = "Unos redovnih sati, bolovanja do/preko 30 dana, prekovremenog rada, godišnjeg odmora i stimulacija sa brzim popunjavanjem."
            },
            new PomocTema
            {
                Naslov = "📊 Obračun plate i Platni listići",
                Modul = "Zarade",
                Kljuc = "Obracun",
                Sadrzaj = "Pregled obračunatih plata po radnicima, masovni izvoz platnih listića u PDF i izrada virmana za banku."
            },
            new PomocTema
            {
                Naslov = "🧾 PPP-PD prijava i Poreska uprava",
                Modul = "Zarade",
                Kljuc = "PppPd",
                Sadrzaj = "Automatsko generisanje zvaničnog PPP-PD XML fajla za portal ePorezi sa provere JMBG i opština rezistentnosti."
            }
        });

        return teme;
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        PrimeniFiltere();
    }

    private void TxtPretragaTema_TextChanged(object sender, TextChangedEventArgs e)
    {
        PrimeniFiltere();
    }

    private void PrimeniFiltere(string? selektujKljucIliNaslov = null)
    {
        string pretraga = TxtPretragaTema.Text?.Trim().ToLowerInvariant() ?? "";
        string izabraniModul = "Sve";

        if (RadFilterFinansije.IsChecked == true) izabraniModul = "Finansije";
        else if (RadFilterSredstva.IsChecked == true) izabraniModul = "Sredstva";
        else if (RadFilterZarade.IsChecked == true) izabraniModul = "Zarade";
        else if (RadFilterOpste.IsChecked == true) izabraniModul = "Opšte";

        var filtrirano = _sveTeme.Where(t =>
        {
            bool modulMatch = izabraniModul == "Sve" || t.Modul.Equals(izabraniModul, StringComparison.OrdinalIgnoreCase);
            bool pretragaMatch = string.IsNullOrEmpty(pretraga) ||
                                 t.Naslov.ToLowerInvariant().Contains(pretraga) ||
                                 t.Sadrzaj.ToLowerInvariant().Contains(pretraga);
            return modulMatch && pretragaMatch;
        }).ToList();

        LstTeme.ItemsSource = filtrirano;

        if (filtrirano.Any())
        {
            PomocTema? nadjena = null;
            if (!string.IsNullOrEmpty(selektujKljucIliNaslov))
            {
                nadjena = filtrirano.FirstOrDefault(t =>
                    (t.Kljuc != null && t.Kljuc.Equals(selektujKljucIliNaslov, StringComparison.OrdinalIgnoreCase)) ||
                    t.Naslov.Contains(selektujKljucIliNaslov, StringComparison.OrdinalIgnoreCase));
            }

            LstTeme.SelectedItem = nadjena ?? filtrirano.First();
        }
        else
        {
            TxtNaslovTeme.Text = "Nema pronađenih tema";
            TxtModulTag.Text = izabraniModul;
            TxtSadrzajTeme.Text = "Pokušajte sa drugačijim pojmom pretrage ili izaberite opciju '🌐 Sva uputstva'.";
        }
    }

    private void LstTeme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstTeme.SelectedItem is PomocTema tema)
        {
            TxtNaslovTeme.Text = tema.Naslov;
            TxtModulTag.Text = tema.Modul;
            TxtSadrzajTeme.Text = tema.Sadrzaj;
        }
    }

    private void BtnOtvoriHtml_Click(object sender, RoutedEventArgs e)
    {
        string izabraniModul = "erpi";
        if (RadFilterFinansije.IsChecked == true) izabraniModul = "finansije";
        else if (RadFilterSredstva.IsChecked == true) izabraniModul = "sredstva";
        else if (RadFilterZarade.IsChecked == true) izabraniModul = "zarade";

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string htmlPath = Path.Combine(baseDir, "Resources", "Help", $"uputstvo-{izabraniModul}.html");

        if (!File.Exists(htmlPath))
        {
            // Fallback na radni direktorijum projekta
            htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Help", $"uputstvo-{izabraniModul}.html");
        }

        if (File.Exists(htmlPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju uputstva: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show($"Fajl uputstva nije pronađen: {htmlPath}", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnChangelog_Click(object sender, RoutedEventArgs e)
    {
        new ChangelogWindow { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
