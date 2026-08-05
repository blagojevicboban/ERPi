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

    private bool _isSelected = true;
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
                            IsSelected = true
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
                        IsSelected = true
                    });
                }
            }
        }

        return firme;
    }

    public async Task UveziFirmeAsync(ErpiDbContext destDb, List<DbfFirmaDto> izabraneFirme, bool brisiPostojece, IProgress<DosImportProgress> progress)
    {
        await Task.Run(async () =>
        {
            if (brisiPostojece)
            {
                Report(progress, "Baza", "Čišćenje baze", 0, "🗑️ Brisanje postojećih podataka iz baze pre uvoza...");
                destDb.StavkeNaloga.RemoveRange(destDb.StavkeNaloga);
                destDb.Nalozi.RemoveRange(destDb.Nalozi);
                destDb.StavkeKalkulacije.RemoveRange(destDb.StavkeKalkulacije);
                destDb.Kalkulacije.RemoveRange(destDb.Kalkulacije);
                destDb.Artikli.RemoveRange(destDb.Artikli);
                destDb.Magacini.RemoveRange(destDb.Magacini);
                destDb.Partneri.RemoveRange(destDb.Partneri);
                destDb.Konta.RemoveRange(destDb.Konta);
                await destDb.SaveChangesAsync();
                Report(progress, "Baza", "Čišćenje baze", 0, "   --> Baza uspešno očišćena!");
            }

            int totalFirme = izabraneFirme.Count;
            int currentFirmaIdx = 0;

            string tempDir = Path.Combine(Path.GetTempPath(), "ERPiDosImport");
            Directory.CreateDirectory(tempDir);

            foreach (var firmaDto in izabraneFirme)
            {
                currentFirmaIdx++;
                int basePercent = (int)(((double)(currentFirmaIdx - 1) / totalFirme) * 100);

                Report(progress, firmaDto.Naziv, "Inicijalizacija", basePercent, $"🚀 Uvoz DOS podataka za firmu: {firmaDto.Naziv} ({firmaDto.Sifra})...");

                string tempDbPath = Path.Combine(tempDir, $"temp_{Guid.NewGuid():N}.db");

                try
                {
                    using (var firmDb = AccountingDbContext.Create(tempDbPath))
                    {
                        // 1. Unos Firme
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

                        // 2. Kontni plan
                        var kontplanFile = Path.Combine(firmaDto.FolderPath, "KONTPLAN.DBF");
                        if (File.Exists(kontplanFile))
                        {
                            Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 5, "📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
                            var rows = DbfImportService.ReadRows(kontplanFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var konto = DbfImportService.MapKonto(r);
                                if (konto != null)
                                {
                                    firmDb.Konta.Add(konto);
                                    count++;
                                }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 10, $"   --> Uvezeno {count} konta!");
                        }

                        // 3. Partneri
                        var ankontFile = Path.Combine(firmaDto.FolderPath, "ANKONT.DBF");
                        if (File.Exists(ankontFile))
                        {
                            Report(progress, firmaDto.Naziv, "Partneri", basePercent + 15, "👥 Uvoz Partnera (ANKONT.DBF)...");
                            var rows = DbfImportService.ReadRows(ankontFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var partner = DbfImportService.MapPartner(r, count + 1);
                                if (partner != null)
                                {
                                    firmDb.Partneri.Add(partner);
                                    count++;
                                }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Partneri", basePercent + 25, $"   --> Uvezeno {count} partnera!");
                        }

                        // 4. Magacini i Artikli
                        var magacinFile = Path.Combine(firmaDto.FolderPath, "MAGACIN.DBF");
                        if (File.Exists(magacinFile))
                        {
                            Report(progress, firmaDto.Naziv, "Magacini", basePercent + 30, "📦 Uvoz Magacina (MAGACIN.DBF)...");
                            var rows = DbfImportService.ReadRows(magacinFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var magacin = DbfImportService.MapMagacin(r);
                                if (magacin != null)
                                {
                                    firmDb.Magacini.Add(magacin);
                                    count++;
                                }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Magacini", basePercent + 35, $"   --> Uvezeno {count} magacina!");
                        }

                        var artikliFile = Path.Combine(firmaDto.FolderPath, "ARTIKLI.DBF");
                        if (File.Exists(artikliFile))
                        {
                            Report(progress, firmaDto.Naziv, "Artikli", basePercent + 38, "🛒 Uvoz Artikala (ARTIKLI.DBF)...");
                            var rows = DbfImportService.ReadRows(artikliFile);
                            int count = 0;
                            foreach (var r in rows)
                            {
                                var artikal = DbfImportService.MapArtikal(r);
                                if (artikal != null)
                                {
                                    firmDb.Artikli.Add(artikal);
                                    count++;
                                }
                            }
                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Artikli", basePercent + 47, $"   --> Uvezeno {count} artikala!");
                        }

                        // 5. Nalozi
                        var nalogFile = Path.Combine(firmaDto.FolderPath, "NALOG.DBF");
                        if (File.Exists(nalogFile))
                        {
                            Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 50, "📖 Uvoz Naloga glavne knjige (NALOG.DBF)...");
                            var nalogRows = DbfImportService.ReadRows(nalogFile);
                            var naloziGroups = DbfImportService.GroupNalogRows(nalogRows);

                            int countNaloga = 0;
                            foreach (var (brNaloga, redovi) in naloziGroups)
                            {
                                var nalog = DbfImportService.MapNalogGrupa(brNaloga, redovi);
                                if (nalog != null)
                                {
                                    firmDb.Nalozi.Add(nalog);
                                    countNaloga++;
                                }
                            }

                            await firmDb.SaveChangesAsync();
                            Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 80, $"   --> Uvezeno {countNaloga} naloga!");
                        }

                        // 6. Prebacivanje iz privremene baze u ErpiDbContext preko ErpiFinansijeImporter
                        Report(progress, firmaDto.Naziv, "Spajanje baze", basePercent + 85, "🔄 Konverzija u ujedinjenu ERPi bazu...");
                        var importer = new ErpiFinansijeImporter(destDb);
                        var importRes = await importer.ImportFromDatabaseAsync(firmDb);

                        Report(progress, firmaDto.Naziv, "Završeno", basePercent + 100,
                            $"✅ Firma '{firmaDto.Naziv}' uspešno uvežena! (Konta: {importRes.UvezenoKonta}, Partneri: {importRes.UvezenoPartnera}, Nalozi: {importRes.UvezenoNaloga}, Magacini: {importRes.UvezenoMagacina}, Artikli: {importRes.UvezenoArtikala})");
                    }
                }
                finally
                {
                    try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
                }
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
