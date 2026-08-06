using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiFinansijeData;
using ERPiFinansijeData.Services;
using ERPiMigration.Importers;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Services.Finansije;

public class DbfFirmaDto : INotifyPropertyChanged
{
    public string Sifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string Pib { get; set; } = "";
    public string MaticniBroj { get; set; } = "";
    public string Adresa { get; set; } = "";
    public string PttIMesto { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string ZiroRacun { get; set; } = "";
    public string FolderPath { get; set; } = "";

    private bool _isSelected = false;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class DosImportProgress
{
    public string FirmName { get; set; } = "";
    public string StepDescription { get; set; } = "";
    public int Percentage { get; set; }
    public string LogMessage { get; set; } = "";
}

public class DosImportService
{
    private static DosImportService? _instance;
    public static DosImportService Instance => _instance ??= new DosImportService();

    private DosImportService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public List<DbfFirmaDto> SkenirajRadniDirektorijum(string radniDir)
    {
        var firme = new List<DbfFirmaDto>();
        if (!Directory.Exists(radniDir)) return firme;

        var korisnicFile = Path.Combine(radniDir, "KORISNIC.DBF");
        if (File.Exists(korisnicFile))
        {
            var rows = DbfImportService.ReadRows(korisnicFile);
            foreach (var r in rows)
            {
                string sifra = GetVal(r, "KOR", "SIFRA", "KOD");
                string naziv = GetVal(r, "IME", "NAZIV", "FIRMA");
                string pib = GetVal(r, "PIB", "PIBK");
                string mb = GetVal(r, "MB", "MATICNI", "MATK");
                string adresa = GetVal(r, "UL", "ADRESA", "ULICA");
                string mesto = GetVal(r, "BR", "GRAD", "MESTO");
                string ziro = GetVal(r, "Z", "ZIRO", "RACUN");
                string tel = GetVal(r, "TEL", "TELEFON");

                if (!string.IsNullOrWhiteSpace(sifra))
                {
                    var folderName = "KOR" + sifra.PadLeft(2, '0');
                    var folderPath = Path.Combine(radniDir, folderName);
                    if (!Directory.Exists(folderPath))
                    {
                        folderPath = Path.Combine(radniDir, "KOR" + sifra);
                    }

                    if (Directory.Exists(folderPath))
                    {
                        firme.Add(new DbfFirmaDto
                        {
                            Sifra = folderName,
                            Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Firma {sifra}" : naziv,
                            Pib = pib,
                            MaticniBroj = mb,
                            Adresa = adresa,
                            PttIMesto = mesto,
                            Telefon = tel,
                            ZiroRacun = ziro,
                            FolderPath = folderPath,
                            IsSelected = false
                        });
                    }
                }
            }
        }

        if (!firme.Any())
        {
            var dirs = Directory.GetDirectories(radniDir);
            foreach (var dir in dirs)
            {
                var dbfFiles = Directory.GetFiles(dir, "*.DBF");
                if (dbfFiles.Any())
                {
                    var folderName = Path.GetFileName(dir);
                    firme.Add(new DbfFirmaDto
                    {
                        Sifra = folderName,
                        Naziv = $"Firma {folderName}",
                        FolderPath = dir,
                        IsSelected = false
                    });
                }
            }
        }

        if (firme.Any())
        {
            firme[0].IsSelected = true;
        }

        return firme;
    }

    public async Task UveziJednuFirmuAsync(
        ErpiDbContext destDb,
        DbfFirmaDto firmaDto,
        bool importFinansijsko,
        bool importRobno,
        bool importMaterijalno,
        bool brisiPostojece,
        IProgress<DosImportProgress> progress)
    {
        await Task.Run(async () =>
        {
            // Brisanje postojećih podataka (ako je brisiPostojece == true) se namerno izvršava TEK
            // nakon što se svi izabrani DBF-ovi uspešno pročitaju i mapiraju u privremenu bazu (vidi
            // niže, neposredno pre poziva ErpiFinansijeImporter-a) — NE ovde na početku. Incident
            // 06.08.2026: brisanje je ranije bilo prvi korak i trajno se commit-ovalo pre uvoza; kad bi
            // mapiranje DBF-a posle toga puklo (npr. dupla šifra artikla), aktivna baza je ostajala
            // trajno prazna jer se do spajanja nikad nije ni stiglo. Odlaganjem brisanja do trenutka
            // kad je privremena baza već spremna za spajanje, pad pri čitanju DBF-a više ne briše ništa.
            Report(progress, firmaDto.Naziv, "Inicijalizacija", 10, $"🚀 Uvoz DOS podataka za izabranu firmu: {firmaDto.Naziv} ({firmaDto.Sifra})...");

            string tempDir = Path.Combine(Path.GetTempPath(), "ERPiDosImport");
            Directory.CreateDirectory(tempDir);
            string tempDbPath = Path.Combine(tempDir, $"temp_{Guid.NewGuid():N}.db");

            try
            {
                using (var firmDb = AccountingDbContext.Create(tempDbPath))
                {
                    // Unos osnovne Firme
                    var dbFirma = new ERPiFinansijeData.Models.Firma
                    {
                        Sifra = firmaDto.Sifra,
                        Naziv = firmaDto.Naziv,
                        Pib = firmaDto.Pib,
                        MaticniBroj = firmaDto.MaticniBroj,
                        Adresa = firmaDto.Adresa,
                        PttIMesto = firmaDto.PttIMesto,
                        Telefon = firmaDto.Telefon,
                        ZiroRacun = firmaDto.ZiroRacun,
                        IsActive = true
                    };
                    firmDb.Firme.Add(dbFirma);
                    await firmDb.SaveChangesAsync();

                    // 1. FINANSIJSKO KNJIGOVODSTVO
                    if (importFinansijsko)
                    {
                        var kontplanFile = Path.Combine(firmaDto.FolderPath, "KONTPLAN.DBF");
                        if (File.Exists(kontplanFile))
                        {
                            Report(progress, firmaDto.Naziv, "Kontni plan", 20, "📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
                            var rows = DbfImportService.ReadRows(kontplanFile);
                            var vidjeniBrojeviKonta = new HashSet<string>();
                            int count = 0;
                            int duplikata = 0;
                            foreach (var r in rows)
                            {
                                var konto = DbfImportService.MapKonto(r);
                                if (konto == null) continue;

                                // KONTPLAN.DBF u starim DOS/Clipper bazama zna da nosi duplirane brojeve
                                // konta (npr. nepobrisani stari zapisi) — Konto.BrojKonta ima UNIQUE indeks
                                // u ovoj (temp) bazi, pa bi drugi upis istog broja oborio SaveChangesAsync
                                // sa "UNIQUE constraint failed: Konta.BrojKonta". Zadržava se prvo pojavljivanje.
                                if (!vidjeniBrojeviKonta.Add(konto.BrojKonta)) { duplikata++; continue; }

                                firmDb.Konta.Add(konto);
                                count++;
                            }
                            await firmDb.SaveChangesAsync();
                            string napomenaDuplikata = duplikata > 0 ? $" ({duplikata} dupliranih brojeva konta preskočeno)" : "";
                            Report(progress, firmaDto.Naziv, "Kontni plan", 30, $"   --> Uvezeno {count} konta!{napomenaDuplikata}");
                        }

                        var ankontFile = Path.Combine(firmaDto.FolderPath, "ANKONT.DBF");
                        if (File.Exists(ankontFile))
                        {
                            Report(progress, firmaDto.Naziv, "Partneri", 40, "👥 Uvoz Partnera (ANKONT.DBF)...");
                            var rows = DbfImportService.ReadRows(ankontFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var partner = DbfImportService.MapPartner(r, count + 1);
                                if (partner != null) { firmDb.Partneri.Add(partner); count++; }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Partneri", 50, $"   --> Uvezeno {count} partnera!");
                        }

                        // Šifarnik opisa promena (PROMENE.DBF) — koristi se samo da popuni Opis stavki naloga,
                        // ne postoji kao zasebna tabela u ujedinjenoj ERPi šemi (razlikuje se po firmi, pa se
                        // ne čuva kao deljeni rečnik — vidi napomenu u ERPiFinansijeData.Models.Promena).
                        var promeneMap = new Dictionary<int, string>();
                        var promeneFile = Path.Combine(firmaDto.FolderPath, "PROMENE.DBF");
                        if (File.Exists(promeneFile))
                        {
                            Report(progress, firmaDto.Naziv, "Šifarnik promena", 33, "🏷️ Uvoz šifarnika opisa promena (PROMENE.DBF)...");
                            var rows = DbfImportService.ReadRows(promeneFile);
                            foreach (var r in rows)
                            {
                                var promena = DbfImportService.MapPromena(r);
                                if (promena != null) promeneMap[promena.Sifra] = promena.Opis;
                            }
                            Report(progress, firmaDto.Naziv, "Šifarnik promena", 35, $"   --> Učitano {promeneMap.Count} opisa promena!");
                        }

                        var nalogFile = Path.Combine(firmaDto.FolderPath, "NALOG.DBF");
                        if (File.Exists(nalogFile))
                        {
                            Report(progress, firmaDto.Naziv, "Nalozi", 40, "📖 Uvoz Naloga glavne knjige (NALOG.DBF)...");
                            var nalogRows = DbfImportService.ReadRows(nalogFile);
                            var naloziGroups = DbfImportService.GroupNalogRows(nalogRows);

                            int countNaloga = 0;
                            foreach (var (brNaloga, redovi) in naloziGroups)
                            {
                                var nalog = DbfImportService.MapNalogGrupa(brNaloga, redovi, promeneMap);
                                if (nalog != null) { firmDb.Nalozi.Add(nalog); countNaloga++; }
                            }

                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Nalozi", 45, $"   --> Uvezeno {countNaloga} naloga!");
                        }
                    }

                    // 2. ROBNO KNJIGOVODSTVO
                    if (importRobno)
                    {
                        var magacinFile = Path.Combine(firmaDto.FolderPath, "MAGACIN.DBF");
                        // Legacy DOS/Clipper šifarnici (MAGACIN.DBF, ARTIKLI.DBF) znaju da nose duplirane šifre
                        // (isti bag kao KONTPLAN.DBF, vidi napomenu kod Kontnog plana) — zadržava se prvo
                        // pojavljivanje, jer bi drugi upis iste šifre kasnije oborio ToDictionary (magaciniMapTemp/
                        // artikliMapTemp niže) sa "An item with the same key has already been added".
                        var vidjeneSifreMagacina = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (File.Exists(magacinFile))
                        {
                            Report(progress, firmaDto.Naziv, "Magacini", 40, "📦 Uvoz Magacina (MAGACIN.DBF)...");
                            var rows = DbfImportService.ReadRows(magacinFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var magacin = DbfImportService.MapMagacin(r);
                                if (magacin == null || !vidjeneSifreMagacina.Add(magacin.SifraMagacina)) continue;

                                firmDb.Magacini.Add(magacin);
                                count++;
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Magacini", 50, $"   --> Uvezeno {count} magacina!");
                        }

                        var artikliFile = Path.Combine(firmaDto.FolderPath, "ARTIKLI.DBF");
                        var vidjeneSifreArtikala = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (File.Exists(artikliFile))
                        {
                            Report(progress, firmaDto.Naziv, "Artikli", 60, "🛒 Uvoz Artikala robe (ARTIKLI.DBF)...");
                            var rows = DbfImportService.ReadRows(artikliFile);
                            int count = 0;
                            int duplikataArtikala = 0;
                            foreach (var r in rows)
                            {
                                var artikal = DbfImportService.MapArtikal(r);
                                if (artikal == null) continue;
                                if (!vidjeneSifreArtikala.Add(artikal.SifraArtikla)) { duplikataArtikala++; continue; }

                                firmDb.Artikli.Add(artikal);
                                count++;
                            }
                            await firmDb.SaveChangesAsync();
                            string napomenaArtikala = duplikataArtikala > 0 ? $" ({duplikataArtikala} dupliranih šifri artikala preskočeno)" : "";
                            Report(progress, firmaDto.Naziv, "Artikli", 70, $"   --> Uvezeno {count} artikala!{napomenaArtikala}");
                        }

                        var tarifeFile = Path.Combine(firmaDto.FolderPath, "TARIFE.DBF");
                        if (File.Exists(tarifeFile))
                        {
                            Report(progress, firmaDto.Naziv, "Poreske tarife", 71, "🧾 Uvoz Poreskih tarifa (TARIFE.DBF)...");
                            var rows = DbfImportService.ReadRows(tarifeFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var tarifa = DbfImportService.MapPoreskaTarifa(r);
                                if (tarifa != null) { firmDb.PoreskeTarife.Add(tarifa); count++; }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Poreske tarife", 72, $"   --> Uvezeno {count} poreskih tarifa!");
                        }

                        // Kalkulacije veleprodaje i maloprodaje trebaju šifru magacina/artikla iz TEMP baze
                        // (RAC_OTP/NIV_NAL koriste prava MagacinId/ArtikalId polja) — ove mape su lokalne za firmDb,
                        // prevode se na destDb FK-jeve tek u ErpiFinansijeImporter preko Sifra kolona.
                        // GroupBy+First (ne direktan ToDictionary) — odbrana u dubinu ako ipak provuku dupli redovi.
                        var magaciniMapTemp = (await firmDb.Magacini.AsNoTracking().ToListAsync())
                            .GroupBy(m => m.SifraMagacina, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First().MagacinId, StringComparer.OrdinalIgnoreCase);
                        var artikliMapTemp = (await firmDb.Artikli.AsNoTracking().ToListAsync())
                            .GroupBy(a => a.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First().ArtikalId, StringComparer.OrdinalIgnoreCase);

                        var kalkulacFile = Path.Combine(firmaDto.FolderPath, "KALKULAC.DBF");
                        var kalNalFile = Path.Combine(firmaDto.FolderPath, "KAL_NAL.DBF");
                        if (File.Exists(kalkulacFile))
                        {
                            Report(progress, firmaDto.Naziv, "Kalkulacije", 73, "🧮 Uvoz Kalkulacija veleprodaje (KALKULAC.DBF & KAL_NAL.DBF)...");
                            var kalkRows = DbfImportService.ReadRows(kalkulacFile);
                            var stavkeRows = File.Exists(kalNalFile) ? DbfImportService.ReadRows(kalNalFile) : new List<Dictionary<string, string>>();
                            var stavkeGrouped = DbfImportService.GroupKalkulacijaStavke(stavkeRows);

                            int kalkCount = 0;
                            int totalStavke = 0;
                            // Legacy KALKULAC.DBF ume da sadrži i zaostalo duplo zaglavlje sa istim brojem;
                            // stavke se smeju vezati samo za prvo, inače bi se udvostručile.
                            var iskorisceneGrupe = new HashSet<int>();
                            foreach (var r in kalkRows)
                            {
                                var kalk = DbfImportService.MapKalkulacija(r);
                                if (kalk == null) continue;

                                if (iskorisceneGrupe.Add(kalk.BrojKalkulacije) && stavkeGrouped.TryGetValue(kalk.BrojKalkulacije, out var redoviStavki))
                                {
                                    int rBr = 1;
                                    foreach (var sRow in redoviStavki)
                                    {
                                        var st = DbfImportService.MapKalkulacijaStavka(sRow, rBr++);
                                        if (st != null) { kalk.Stavke.Add(st); totalStavke++; }
                                    }
                                }
                                DbfImportService.DopuniZbiroveIzStavki(kalk);
                                firmDb.Kalkulacije.Add(kalk);
                                kalkCount++;
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Kalkulacije", 76, $"   --> Uvezeno {kalkCount} kalkulacija sa {totalStavke} stavki!");
                        }

                        var malkulacFile = Path.Combine(firmaDto.FolderPath, "MALKULAC.DBF");
                        var malNalFile = Path.Combine(firmaDto.FolderPath, "MAL_NAL.DBF");
                        if (File.Exists(malkulacFile))
                        {
                            Report(progress, firmaDto.Naziv, "Kalkulacije MP", 77, "🏪 Uvoz Maloprodajnih kalkulacija (MALKULAC.DBF & MAL_NAL.DBF)...");
                            var malkRows = DbfImportService.ReadRows(malkulacFile);
                            var malStavkeRows = File.Exists(malNalFile) ? DbfImportService.ReadRows(malNalFile) : new List<Dictionary<string, string>>();
                            var malStavkeGrouped = DbfImportService.GroupMaloprodajnaKalkulacijaStavke(malStavkeRows);

                            int malkCount = 0;
                            int malTotalStavke = 0;
                            var iskorisceneMalGrupe = new HashSet<(int, int)>();
                            foreach (var r in malkRows)
                            {
                                var malk = DbfImportService.MapMaloprodajnaKalkulacija(r);
                                if (malk == null) continue;

                                var kljuc = (malk.SifraProdavnice, malk.BrojKalkulacije);
                                if (iskorisceneMalGrupe.Add(kljuc) && malStavkeGrouped.TryGetValue(kljuc, out var redoviStavki))
                                {
                                    int rBr = 1;
                                    foreach (var sRow in redoviStavki)
                                    {
                                        var st = DbfImportService.MapMaloprodajnaKalkulacijaStavka(sRow, rBr++);
                                        if (st != null) { malk.Stavke.Add(st); malTotalStavke++; }
                                    }
                                }
                                DbfImportService.DopuniZbiroveIzStavki(malk);
                                firmDb.MaloprodajneKalkulacije.Add(malk);
                                malkCount++;
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Kalkulacije MP", 78, $"   --> Uvezeno {malkCount} maloprodajnih kalkulacija sa {malTotalStavke} stavki!");
                        }

                        var racOtpFile = Path.Combine(firmaDto.FolderPath, "RAC_OTP.DBF");
                        var racPodFile = Path.Combine(firmaDto.FolderPath, "RAC_POD.DBF");
                        if (File.Exists(racOtpFile))
                        {
                            Report(progress, firmaDto.Naziv, "Računi-Otpremnice", 79, "📜 Uvoz Računa-Otpremnica (RAC_OTP.DBF & RAC_POD.DBF)...");
                            var racOtpRows = DbfImportService.ReadRows(racOtpFile);
                            var racPodRows = File.Exists(racPodFile) ? DbfImportService.ReadRows(racPodFile) : new List<Dictionary<string, string>>();
                            var racuni = DbfImportService.MapRacunOtpremnice(racOtpRows, racPodRows, magaciniMapTemp, artikliMapTemp);
                            if (racuni.Count > 0)
                            {
                                firmDb.RacuniOtpremnice.AddRange(racuni);
                                await firmDb.SaveChangesAsync();
                            }
                            int totStavke = racuni.Sum(r => r.Stavke.Count);
                            Report(progress, firmaDto.Naziv, "Računi-Otpremnice", 80, $"   --> Uvezeno {racuni.Count} računa-otpremnica sa {totStavke} stavki!");
                        }

                        var nivNalFile = Path.Combine(firmaDto.FolderPath, "NIV_NAL.DBF");
                        var pmNivFile = Path.Combine(firmaDto.FolderPath, "P_M_NIV.DBF");
                        if (File.Exists(nivNalFile) || File.Exists(pmNivFile))
                        {
                            Report(progress, firmaDto.Naziv, "Nivelacije cena", 81, "🏷️ Uvoz Nivelacija cena (NIV_NAL.DBF & P_M_NIV.DBF)...");
                            var nivNalRows = File.Exists(nivNalFile) ? DbfImportService.ReadRows(nivNalFile) : new List<Dictionary<string, string>>();
                            var pmNivRows = File.Exists(pmNivFile) ? DbfImportService.ReadRows(pmNivFile) : new List<Dictionary<string, string>>();
                            var nivelacije = DbfImportService.MapNivelacijeCena(nivNalRows, pmNivRows, magaciniMapTemp, artikliMapTemp);
                            if (nivelacije.Count > 0)
                            {
                                firmDb.NivelacijeCena.AddRange(nivelacije);
                                await firmDb.SaveChangesAsync();
                            }
                            int totStavke = nivelacije.Sum(n => n.Stavke.Count);
                            Report(progress, firmaDto.Naziv, "Nivelacije cena", 82, $"   --> Uvezeno {nivelacije.Count} nivelacija cena sa {totStavke} stavki!");
                        }
                    }

                    // 3. MATERIJALNO KNJIGOVODSTVO
                    if (importMaterijalno)
                    {
                        var msifrFile = Path.Combine(firmaDto.FolderPath, "M_SIFR.DBF");
                        if (File.Exists(msifrFile))
                        {
                            Report(progress, firmaDto.Naziv, "Materijali", 84, "🧱 Uvoz Šifarnika materijala (M_SIFR.DBF)...");
                            var rows = DbfImportService.ReadRows(msifrFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var materijal = DbfImportService.MapMaterijal(r);
                                if (materijal != null) { firmDb.Materijali.Add(materijal); count++; }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Materijali", 85, $"   --> Uvezeno {count} materijala!");
                        }

                        var matKarticaFile = Path.Combine(firmaDto.FolderPath, "MAT_KART.DBF");
                        var mKarticaFile = Path.Combine(firmaDto.FolderPath, "M_KART.DBF");
                        int karticeCount = 0;
                        if (File.Exists(matKarticaFile))
                        {
                            Report(progress, firmaDto.Naziv, "Materijalne kartice", 86, "📊 Uvoz Robnih kartica (MAT_KART.DBF)...");
                            var rows = DbfImportService.ReadRows(matKarticaFile);
                            int rBr = 1;
                            foreach (var r in rows)
                            {
                                var mk = DbfImportService.MapMaterijalnaKartica(r, rBr++);
                                if (mk != null) { firmDb.MaterijalneKartice.Add(mk); karticeCount++; }
                            }
                            await firmDb.SaveChangesAsync();
                        }
                        if (File.Exists(mKarticaFile))
                        {
                            Report(progress, firmaDto.Naziv, "Materijalne kartice", 87, "📊 Uvoz Materijalnih kartica (M_KART.DBF)...");
                            var rows = DbfImportService.ReadRows(mKarticaFile);
                            int rBr = 1;
                            foreach (var r in rows)
                            {
                                var mk = DbfImportService.MapMaterijalnaKartica(r, rBr++);
                                if (mk != null) { firmDb.MaterijalneKartice.Add(mk); karticeCount++; }
                            }
                            await firmDb.SaveChangesAsync();
                        }
                        if (karticeCount > 0)
                        {
                            Report(progress, firmaDto.Naziv, "Materijalne kartice", 88, $"   --> Uvezeno {karticeCount} stavki robnih/materijalnih kartica!");
                        }

                        var ulazFile = Path.Combine(firmaDto.FolderPath, "ULAZ.DBF");
                        if (File.Exists(ulazFile))
                        {
                            Report(progress, firmaDto.Naziv, "Ulazi materijala", 89, "📥 Uvoz Ulaza materijala (ULAZ.DBF)...");
                            var ulazi = DbfImportService.MapUlazNalozi(DbfImportService.ReadRows(ulazFile));
                            if (ulazi.Count > 0)
                            {
                                firmDb.UlazNalozi.AddRange(ulazi);
                                await firmDb.SaveChangesAsync();
                            }
                            Report(progress, firmaDto.Naziv, "Ulazi materijala", 90, $"   --> Uvezeno {ulazi.Count} ulaza materijala!");
                        }

                        var trebovFile = Path.Combine(firmaDto.FolderPath, "TREBOV.DBF");
                        if (File.Exists(trebovFile))
                        {
                            Report(progress, firmaDto.Naziv, "Trebovanja", 91, "📤 Uvoz Trebovanja materijala (TREBOV.DBF)...");
                            var trebovanja = DbfImportService.MapTrebovanjeNalozi(DbfImportService.ReadRows(trebovFile));
                            if (trebovanja.Count > 0)
                            {
                                firmDb.TrebovanjeNalozi.AddRange(trebovanja);
                                await firmDb.SaveChangesAsync();
                            }
                            Report(progress, firmaDto.Naziv, "Trebovanja", 92, $"   --> Uvezeno {trebovanja.Count} trebovanja materijala!");
                        }

                        string matNalPath = Path.Combine(firmaDto.FolderPath, "MAT_NAL.DBF");
                        string zaduzPath = Path.Combine(firmaDto.FolderPath, "ZADUZ.DBF");
                        string razduzPath = Path.Combine(firmaDto.FolderPath, "RAZDUZ.DBF");
                        var svePrimopredaje = new List<ERPiFinansijeData.Models.PrimopredajaNalog>();
                        if (File.Exists(matNalPath) || File.Exists(zaduzPath) || File.Exists(razduzPath))
                        {
                            Report(progress, firmaDto.Naziv, "Primopredaje", 93, "🔄 Uvoz Naloga za primopredaju/zaduženje/razduženje (MAT_NAL.DBF, ZADUZ.DBF, RAZDUZ.DBF)...");
                        }
                        if (File.Exists(matNalPath))
                        {
                            svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(matNalPath), "Primopredaja"));
                        }
                        if (File.Exists(zaduzPath))
                        {
                            svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(zaduzPath), "Zaduženje"));
                        }
                        if (File.Exists(razduzPath))
                        {
                            svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(razduzPath), "Razduženje"));
                        }
                        if (svePrimopredaje.Count > 0)
                        {
                            firmDb.PrimopredajaNalozi.AddRange(svePrimopredaje);
                            await firmDb.SaveChangesAsync();
                            int totStavke = svePrimopredaje.Sum(n => n.Stavke.Count);
                            Report(progress, firmaDto.Naziv, "Primopredaje", 94, $"   --> Uvezeno {svePrimopredaje.Count} naloga (primopredaje/zaduženja/razduženja) sa {totStavke} stavki!");
                        }
                    }

                    // Brisanje postojećih podataka izabranih modula (ako je brisiPostojece == true) —
                    // namerno tek ovde, pošto je privremena baza već uspešno popunjena (vidi napomenu
                    // na početku metode). PRAGMA foreign_keys = OFF jer redosled brisanja između Robno i
                    // Materijalno tabela nije potpuno FK-bezbedan kad se briše samo jedan od ta dva modula
                    // (npr. Ulaz/Trebovanje/Primopredaja iz Materijalno i dalje referenciraju Magacin i kad
                    // se briše samo Robno) — ionako se sve odmah zatim ponovo puni iz DBF-a.
                    if (brisiPostojece)
                    {
                        Report(progress, firmaDto.Naziv, "Čišćenje baze", 94, "🗑️ Brisanje postojećih podataka iz baze pre uvoza...");
                        await destDb.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

                        if (importFinansijsko)
                        {
                            Report(progress, firmaDto.Naziv, "Čišćenje baze", 94, "🗑️ Brisanje postojećih Finansijskih podataka (Nalozi, Partneri, Konta)...");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM StavkeNaloga;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Nalozi;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Partneri;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Konta;");
                        }

                        if (importRobno)
                        {
                            Report(progress, firmaDto.Naziv, "Čišćenje baze", 94, "🗑️ Brisanje postojećih Robnih podataka (Kalkulacije, Računi-otpremnice, Nivelacije, Artikli, Magacini)...");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM RacunOtpremnicaStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM RacuniOtpremnice;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM NivelacijeStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM NivelacijeCena;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM MaloprodajneKalkulacijeStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM MaloprodajneKalkulacije;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM RobnaKretanja;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM StavkeKalkulacije;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Kalkulacije;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM PoreskeTarife;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Artikli;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Magacini;");
                        }

                        if (importMaterijalno)
                        {
                            Report(progress, firmaDto.Naziv, "Čišćenje baze", 94, "🗑️ Brisanje postojećih Materijalnih podataka (Ulaz, Trebovanje, Primopredaja, Kartice)...");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM MaterijalneKartice;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM UlazStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM UlazNalozi;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM TrebovanjeStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM TrebovanjeNalozi;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM PrimopredajaStavke;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM PrimopredajaNalozi;");
                            await destDb.Database.ExecuteSqlRawAsync("DELETE FROM Materijali;");
                        }

                        await destDb.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                        Report(progress, firmaDto.Naziv, "Čišćenje baze", 95, "   --> Izabrani moduli su uspešno očišćeni u bazi!");
                    }

                    // Prenos u ujedinjenu bazu destDb preko ErpiFinansijeImporter
                    Report(progress, firmaDto.Naziv, "Spajanje baze", 96, "🔄 Konverzija u aktivnu ERPi bazu...");
                    var importer = new ErpiFinansijeImporter(destDb);
                    var importRes = await importer.ImportFromDatabaseAsync(firmDb);

                    Report(progress, firmaDto.Naziv, "Završeno", 100,
                        $"✅ Firma '{firmaDto.Naziv}' uspešno uvežena u aktivnu bazu! (Konta: {importRes.UvezenoKonta}, Partneri: {importRes.UvezenoPartnera}, Nalozi: {importRes.UvezenoNaloga}, Magacini: {importRes.UvezenoMagacina}, Artikli: {importRes.UvezenoArtikala}, Materijali: {importRes.UvezenoMaterijala}, Kalkulacija: {importRes.UvezenoKalkulacija}, MP kalkulacija: {importRes.UvezenoMaloprodajnihKalkulacija}, Računa-otpremnica: {importRes.UvezenoRacunaOtpremnica}, Nivelacija: {importRes.UvezenoNivelacija}, Ulaza: {importRes.UvezenoUlaza}, Trebovanja: {importRes.UvezenoTrebovanja}, Primopredaja: {importRes.UvezenoPrimopredaja})");
                }
            }
            finally
            {
                try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
            }
        });
    }

    private static string GetVal(Dictionary<string, string> row, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }
        return "";
    }

    private static void Report(IProgress<DosImportProgress> progress, string firm, string step, int percent, string logMsg)
    {
        progress.Report(new DosImportProgress
        {
            FirmName = firm,
            StepDescription = step,
            Percentage = Math.Min(100, Math.Max(0, percent)),
            LogMessage = logMsg
        });
    }
}
