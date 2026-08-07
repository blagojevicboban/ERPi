using System.Text;
using ERPiSredstvaData;
using ERPiSredstvaData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiMigration.Importers;

/// <summary>
/// Rezultat DOS/DBF migracije Osnovnih sredstava (KOR28 stil DBF fajlova) u privremenu
/// ERPiSredstvaData (SredstvaDbContext) SQLite bazu.
/// </summary>
public class SredstvaDbfMigrationResult
{
    public bool Uspesno { get; set; }
    public string Poruka { get; set; } = string.Empty;

    public int UvezenoDobavljaca { get; set; }
    public int UvezenoSredstava { get; set; }
    public int UvezenoKartica { get; set; }
    public int UvezenoRashoda { get; set; }
    public int UvezenoPrijava { get; set; }
}

/// <summary>
/// Direktan uvoz iz DOS/Clipper DBF fajlova (KOR28 stil — SREDSTVA.DBF, KARTICA.DBF, RASHOD.DBF,
/// PRIJAVA.DBF, KONTPLAN.DBF, KORISNIC.DBF) u ERPiSredstvaData šemu (SredstvaDbContext), kako bi se
/// dalje mogao proslediti kroz <see cref="ErpiSredstvaProdukcijaImporter"/> u objedinjenu ERPi bazu.
/// <para/>
/// Ovo je 1:1 port logike iz <c>ERPiSredstvaMigration/Program.cs</c> (konzolni alat), sveden na
/// pozivnu metodu po istom obrascu kao <see cref="ZaradeDbfMigrator"/> — mapiranje DBF kolona je
/// namerno neizmenjeno, samo je Console.Write(Line) zamenjen sa <paramref name="log"/> callback-om
/// i hardkodovane putanje (<c>C:\SREDSTVA\SREDS\KOR28\</c>) su zamenjene parametrom
/// <paramref name="dbfDir"/>. Za razliku od Zarade (koja ima poseban MESEC.DBF za aktivni
/// period), Sredstva DBF format nema period-osetljive tabele — sve se čita u jednom prolazu.
/// <paramref name="onProgress"/> javlja grubu procentualnu poziciju (0-100) posle svakog od 6
/// koraka (Firma/Dobavljači/Sredstva/Kartice/Rashodi/Prijave) — pozivalac je skalira u svoj
/// segment progres bara (vidi <see cref="ERPiApp.Views.Sredstva.Podesavanja.SredstvaDosImportWindow"/>).
/// </summary>
public static class SredstvaDbfMigrator
{
    public static async Task<SredstvaDbfMigrationResult> MigrateAsync(string dbfDir, string sqliteDb, Action<string>? log = null, Action<int>? onProgress = null)
    {
        void Log(string s) => log?.Invoke(s);
        void Progress(int percent) => onProgress?.Invoke(percent);

        var result = new SredstvaDbfMigrationResult();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var cp852 = Encoding.GetEncoding(852);
        var opts = new DbfDataReader.DbfDataReaderOptions { Encoding = cp852, SkipDeletedRecords = true };

        if (!Directory.Exists(dbfDir))
        {
            result.Uspesno = false;
            result.Poruka = $"Direktorijum sa DBF fajlovima ne postoji: {dbfDir}";
            return result;
        }

        string? RequireDbf(string fileName)
        {
            var direktno = Path.Combine(dbfDir, fileName);
            if (File.Exists(direktno)) return direktno;

            // KORISNIC.DBF u originalnom DOS rasporedu živi jedan nivo iznad KOR** foldera
            // (deljen registar firmi), pa ako korisnik izabere baš KOR28-stil folder, tražimo
            // i u roditeljskom direktorijumu pre nego što odustanemo.
            var roditelj = Directory.GetParent(dbfDir)?.FullName;
            if (roditelj != null)
            {
                var uRoditelju = Path.Combine(roditelj, fileName);
                if (File.Exists(uRoditelju)) return uRoditelju;
            }
            return null;
        }

        // ── Firma iz KORISNIC.DBF (ako postoji — nije uvek prisutna u svakom KOR** folderu) ──
        var firma = new Firma { Naziv = $"Uvoz iz DOS ({Path.GetFileName(dbfDir.TrimEnd('\\', '/'))})" };
        var korisnicDbf = RequireDbf("KORISNIC.DBF");
        if (korisnicDbf != null)
        {
            try
            {
                using var rKor = new DbfDataReader.DbfDataReader(korisnicDbf, opts);
                var colsKor = GetCols(rKor);
                if (rKor.Read())
                {
                    var ime = Str(GetSafe(rKor, colsKor, "IME"));
                    if (!string.IsNullOrWhiteSpace(ime))
                        firma.Naziv = ime;

                    firma.Mesto = Str(GetSafe(rKor, colsKor, "GRAD"));
                    if (string.IsNullOrWhiteSpace(firma.Mesto))
                        firma.Mesto = Str(GetSafe(rKor, colsKor, "MESTO"));

                    firma.PIB = Str(GetSafe(rKor, colsKor, "PIB"));
                    firma.MaticniBroj = Str(GetSafe(rKor, colsKor, "MB"));
                }
                Log($"[OK] Firma pročitana iz KORISNIC.DBF: {firma.Naziv}");
            }
            catch (Exception ex)
            {
                Log($"[!] Upozorenje: greška pri čitanju KORISNIC.DBF ({ex.Message}). Koristim podrazumevani naziv firme.");
            }
        }
        else
        {
            Log("[!] Nema KORISNIC.DBF (ni u izabranom folderu ni u roditeljskom) — firma se popunjava kasnije, ručno.");
        }

        var sredstvaDbf = RequireDbf("SREDSTVA.DBF");
        if (sredstvaDbf == null)
        {
            result.Uspesno = false;
            result.Poruka = $"SREDSTVA.DBF nije pronađen u {dbfDir}. Proverite da li ste izabrali ispravan folder (KOR** stil sa SREDSTVA.DBF/KARTICA.DBF/...).";
            return result;
        }

        // ── Kreiramo svežu privremenu SQLite bazu (pozivalac prosleđuje privremenu putanju) ──
        if (File.Exists(sqliteDb))
        {
            try { File.Delete(sqliteDb); }
            catch (Exception ex)
            {
                result.Uspesno = false;
                result.Poruka = $"Ne mogu da pripremim privremenu bazu na {sqliteDb}: {ex.Message}";
                return result;
            }
        }

        using var db = SredstvaDbContext.Create(sqliteDb);
        Log("[OK] Privremena SQLite baza otvorena.");

        db.Firme.Add(firma);
        await db.SaveChangesAsync();
        Log($"[1/6] Firma kreirana (ID={firma.Id}).");
        Progress(10);

        // ── 2. DOBAVLJAČI (KONTPLAN.DBF) ──
        var dobavljaciMap = new Dictionary<int, int>(); // konto -> db.Id
        var kontplanDbf = RequireDbf("KONTPLAN.DBF");
        if (kontplanDbf != null)
        {
            using var r = new DbfDataReader.DbfDataReader(kontplanDbf, opts);
            var cols = GetCols(r);
            while (r.Read())
            {
                var konto = ToInt(GetSafe(r, cols, "KONTO"));
                var opisKonta = Str(GetSafe(r, cols, "OPIS_KONTA"));
                if (konto <= 0 && string.IsNullOrWhiteSpace(opisKonta)) continue;

                var d = new Dobavljac
                {
                    Konto = konto,
                    OpisKonta = opisKonta,
                    UlicaIBroj = Str(GetSafe(r, cols, "ULICA_I_BR")),
                    MestoIBroj = Str(GetSafe(r, cols, "MESTO_I_BR"))
                };
                db.Dobavljaci.Add(d);
                await db.SaveChangesAsync();
                dobavljaciMap[konto] = d.Id;
                result.UvezenoDobavljaca++;
            }
        }
        else
        {
            Log("[!] Nema KONTPLAN.DBF — Prijave neće imati dobavljača.");
        }
        Log($"[2/6] Dobavljači uvezeni: {dobavljaciMap.Count}");
        Progress(25);

        // ── 3. SREDSTVA (SREDSTVA.DBF) ──
        var sredstvaMap = new Dictionary<int, int>(); // legacySifra -> db.Id
        var sredstvaBatch = new List<Sredstvo>();
        using (var r = new DbfDataReader.DbfDataReader(sredstvaDbf, opts))
        {
            var cols = GetCols(r);
            var batch = sredstvaBatch;
            while (r.Read())
            {
                var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
                var invenBr = Str(GetSafe(r, cols, "INVEN_BR"));
                var naziv = Str(GetSafe(r, cols, "NAZIV"));

                // Preskačemo prazna sredstva (bez šifre i naziva ili sa šifrom 0)
                if (sifra <= 0 || string.IsNullOrWhiteSpace(naziv))
                {
                    continue;
                }

                var nabavna = ToDec(GetSafe(r, cols, "NABAVNA"));
                var otpisana = ToDec(GetSafe(r, cols, "OTPISANA"));
                var s = new Sredstvo
                {
                    LegacySifra = sifra,
                    InventarskiBroj = string.IsNullOrWhiteSpace(invenBr) ? sifra.ToString() : invenBr,
                    Naziv = naziv,
                    Konto = Str(GetSafe(r, cols, "KONTO")),
                    NabavnaVrednost = nabavna,
                    IspravkaVrednosti = otpisana,
                    SadasnjaVrednost = nabavna - otpisana,
                    StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
                    AmortizacionaGrupa = ToInt(GetSafe(r, cols, "AMORT_GR1")).ToString(),
                    DatumAktiviranja = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
                    DatumNabavke = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
                    JeAktivno = true
                };
                batch.Add(s);
            }
            db.Sredstva.AddRange(batch);
            await db.SaveChangesAsync();
            foreach (var s in batch)
                sredstvaMap[s.LegacySifra] = s.Id;
            result.UvezenoSredstava = batch.Count;
        }
        Log($"[3/6] Sredstva uvezena: {sredstvaMap.Count}");
        Progress(40);

        // ── 4. KARTICE (KARTICA.DBF) ──
        var karticaDbf = RequireDbf("KARTICA.DBF");
        if (karticaDbf != null)
        {
            int karticeSkip = 0;
            var karticeBatch = new List<Kartica>();
            using (var r = new DbfDataReader.DbfDataReader(karticaDbf, opts))
            {
                var cols = GetCols(r);
                while (r.Read())
                {
                    var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
                    if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { karticeSkip++; continue; }
                    karticeBatch.Add(new Kartica
                    {
                        SredstvoId = sredstvoId,
                        RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
                        Datum = ToDate(GetSafe(r, cols, "DATUM")) ?? DateTime.MinValue,
                        OpisPromene = Str(GetSafe(r, cols, "OPIS_PROM")),
                        ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
                        Konto = Str(GetSafe(r, cols, "KONTO")),
                        AmortizacionaGrupa1 = ToInt(GetSafe(r, cols, "AMORT_GR1")),
                        AmortizacionaGrupa2 = ToInt(GetSafe(r, cols, "AMORT_GR2")),
                        StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
                        KoeficijentRevalorizacije = ToDec(GetSafe(r, cols, "KOEFIC_REV")),
                        Kolicina = ToDec(GetSafe(r, cols, "KOLICINA")),
                        NabavnaVrednost = ToDec(GetSafe(r, cols, "NABAVNA")),
                        IspravkaVrednosti = ToDec(GetSafe(r, cols, "OTPISANA"))
                    });
                }
            }
            db.Kartice.AddRange(karticeBatch);
            await db.SaveChangesAsync();
            result.UvezenoKartica = karticeBatch.Count;
            Log($"[4/6] Kartice uvezene: {karticeBatch.Count} (preskočeno: {karticeSkip})");

            // Dopunjujemo Konto za Sredstvo ako je bilo prazno u SREDSTVA.DBF
            var kontoIzKartica = karticeBatch
                .Where(k => !string.IsNullOrWhiteSpace(k.Konto))
                .GroupBy(k => k.SredstvoId)
                .ToDictionary(g => g.Key, g => g.First().Konto);

            bool izmenaKonta = false;
            foreach (var s in sredstvaBatch)
            {
                if (string.IsNullOrWhiteSpace(s.Konto) && kontoIzKartica.TryGetValue(s.Id, out var kStr))
                {
                    s.Konto = kStr;
                    izmenaKonta = true;
                }
            }
            if (izmenaKonta) await db.SaveChangesAsync();

            // SREDSTVA.DBF-ovo NABAVNA/OTPISANA je snimak početnog stanja (obično 2001),
            // ne tekuće stanje — tekuće se dobija akumulacijom cele istorije iz KARTICA.DBF
            // (isti model kao AmortizacijaCalculator: svaka kartica je delta na prethodno
            // stanje). Bez ovog rekalkulisanja, Sredstvo ostaje zaglavljeno na početnoj
            // vrednosti iz uvoza dok stvarna istorija (Redovan otpis/Revalorizacija) živi
            // samo u Karticama — to je uzrok razilaženja ERPi vs. ERPiSredstva registra.
            var sumeZaSredstvo = karticeBatch
                .GroupBy(k => k.SredstvoId)
                .ToDictionary(
                    g => g.Key,
                    g => (Nabavna: g.Sum(k => k.NabavnaVrednost), Ispravka: g.Sum(k => k.IspravkaVrednosti)));

            foreach (var s in sredstvaBatch)
            {
                if (!sumeZaSredstvo.TryGetValue(s.Id, out var sume)) continue;
                s.NabavnaVrednost = sume.Nabavna;
                s.IspravkaVrednosti = sume.Ispravka;
                s.SadasnjaVrednost = sume.Nabavna - sume.Ispravka;
            }
            await db.SaveChangesAsync();
        }
        else
        {
            Log("[!] Nema KARTICA.DBF — sredstva neće imati istoriju promena.");
        }
        Progress(70);

        // ── 5. RASHODI (RASHOD.DBF) ──
        var rashodDbf = RequireDbf("RASHOD.DBF");
        if (rashodDbf != null)
        {
            int rashodiSkip = 0;
            var rashodiBatch = new List<Rashod>();
            using (var r = new DbfDataReader.DbfDataReader(rashodDbf, opts))
            {
                var cols = GetCols(r);
                while (r.Read())
                {
                    var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
                    if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { rashodiSkip++; continue; }
                    var kodInt = ToInt(GetSafe(r, cols, "KOD"));
                    rashodiBatch.Add(new Rashod
                    {
                        SredstvoId = sredstvoId,
                        BrojNaloga = ToInt(GetSafe(r, cols, "BR_NALOGA")),
                        RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
                        Kod = Enum.IsDefined(typeof(TipoviPromena), kodInt) ? (TipoviPromena)kodInt : TipoviPromena.Rashodovanje,
                        KodTekst = Str(GetSafe(r, cols, "KOD_TEXT")),
                        Datum = ToDate(GetSafe(r, cols, "DATUM")) ?? DateTime.MinValue,
                        DokumentBroj = Str(GetSafe(r, cols, "DOKUM_BROJ")),
                        Podaci = ToDec(GetSafe(r, cols, "PODACI")),
                        ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
                        Knjizen = ToInt(GetSafe(r, cols, "KNJIZEN")) == 1
                    });
                }
            }
            db.Rashodi.AddRange(rashodiBatch);
            await db.SaveChangesAsync();
            result.UvezenoRashoda = rashodiBatch.Count;
            Log($"[5/6] Rashodi uvezeni: {rashodiBatch.Count} (preskočeno: {rashodiSkip})");

            // Označavamo JeAktivno = false za rashodovana sredstva
            var rashodovanaSredstvaIds = rashodiBatch
                .Where(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.Prodaja || r.Kod == TipoviPromena.Otudjenje || r.Kod == TipoviPromena.Brisanje)
                .Select(r => r.SredstvoId)
                .Union(sredstvaBatch.Where(s => s.SadasnjaVrednost <= 0).Select(s => s.Id))
                .ToHashSet();

            foreach (var s in sredstvaBatch)
            {
                if (rashodovanaSredstvaIds.Contains(s.Id))
                {
                    s.JeAktivno = false;
                }
            }
            await db.SaveChangesAsync();
        }
        else
        {
            Log("[!] Nema RASHOD.DBF.");
        }
        Progress(85);

        // ── 6. PRIJAVE (PRIJAVA.DBF) ──
        var prijavaDbf = RequireDbf("PRIJAVA.DBF");
        if (prijavaDbf != null)
        {
            int prijaveSkip = 0;
            var prijaveBatch = new List<Prijava>();
            using (var r = new DbfDataReader.DbfDataReader(prijavaDbf, opts))
            {
                var cols = GetCols(r);
                while (r.Read())
                {
                    var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
                    if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { prijaveSkip++; continue; }

                    var kontoBr = ToInt(GetSafe(r, cols, "KONTO"));
                    int? dobavljacId = dobavljaciMap.TryGetValue(kontoBr, out var dId) ? dId : null;

                    prijaveBatch.Add(new Prijava
                    {
                        SredstvoId = sredstvoId,
                        BrojNaloga = ToInt(GetSafe(r, cols, "BR_NALOGA")),
                        RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
                        ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
                        Konto = Str(GetSafe(r, cols, "KONTO")),
                        AmortizacionaGrupa1 = ToInt(GetSafe(r, cols, "AMORT_GR1")),
                        AmortizacionaGrupa2 = ToInt(GetSafe(r, cols, "AMORT_GR2")),
                        StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
                        DatumAktiviranja = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
                        RevalorizacionaGrupa = ToInt(GetSafe(r, cols, "REVAL_GR")),
                        NabavnaVrednost = ToDec(GetSafe(r, cols, "NABAVNA")),
                        OtpisanaVrednost = ToDec(GetSafe(r, cols, "OTPISANA")),
                        JedinicaMere = Str(GetSafe(r, cols, "J_MERA")),
                        Kolicina = ToDec(GetSafe(r, cols, "KOLICINA")),
                        InventarskiBroj = Str(GetSafe(r, cols, "INVEN_BR")),
                        BrojFakture = Str(GetSafe(r, cols, "BR_FAKTURE")),
                        DatumFakture = ToDate(GetSafe(r, cols, "DAT_FAKTUR")),
                        BrojNalaznice = ToInt(GetSafe(r, cols, "BR_NALAZ")),
                        BrNal = Str(GetSafe(r, cols, "BR_NAL")),
                        GodNal = ToInt(GetSafe(r, cols, "GOD_NAL")),
                        Knjizen = ToInt(GetSafe(r, cols, "KNJIZEN")) == 1,
                        DobavljacId = dobavljacId
                    });
                }
            }
            db.Prijave.AddRange(prijaveBatch);
            await db.SaveChangesAsync();
            result.UvezenoPrijava = prijaveBatch.Count;
            Log($"[6/6] Prijave uvezene: {prijaveBatch.Count} (preskočeno: {prijaveSkip})");
        }
        else
        {
            Log("[!] Nema PRIJAVA.DBF.");
        }
        Progress(100);

        Log("[OK] Kompletna DOS/DBF migracija Sredstava završena.");
        result.Uspesno = true;
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Dictionary<string, int> GetCols(DbfDataReader.DbfDataReader r)
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = i;
        return d;
    }

    private static object? GetSafe(DbfDataReader.DbfDataReader r, Dictionary<string, int> cols, string key)
    {
        if (cols.TryGetValue(key, out int idx)) return r.GetValue(idx);
        return null;
    }

    private static string Str(object? v) => v?.ToString()?.Trim() ?? string.Empty;
    private static int ToInt(object? v) { try { return Convert.ToInt32(v); } catch { return 0; } }
    private static decimal ToDec(object? v) { try { return Convert.ToDecimal(v); } catch { return 0m; } }
    private static DateTime? ToDate(object? v) { if (v is DateTime dt && dt != DateTime.MinValue) return dt; return null; }
}
