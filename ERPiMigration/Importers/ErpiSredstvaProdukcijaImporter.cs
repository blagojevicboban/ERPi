using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Sredstva;
using ERPiSredstvaData;
using Microsoft.EntityFrameworkCore;

namespace ERPiMigration.Importers;

public class SredstvaImportResult
{
    public int UvezenoSredstava { get; set; }
    public int UvezenoKartica { get; set; }
    public int UvezenoPrijava { get; set; }
    public int UvezenoRashoda { get; set; }
    public int UvezenoKomisija { get; set; }
    public int UvezenoClanovaKomisije { get; set; }
    public int UvezenoPopisa { get; set; }
    public int UvezenoPopisnihStavki { get; set; }
    public int UvezenoKonta { get; set; }
    public int UvezenoPartneraDobavljaca { get; set; }
    public bool Uspesno { get; set; } = true;
    public string Greska { get; set; } = "";
}

/// <summary>
/// EF-to-EF uvoz iz ERPiSredstvaData šeme (<see cref="SredstvaDbContext"/>) u jedinstveni
/// ErpiDbContext — interna faza koju poziva <see cref="SredstvaDbfMigrator"/> pošto DOS/DBF
/// fajlove prvo pretoči u privremenu SredstvaDbContext bazu (isti dvostepeni obrazac kao
/// ZaradeDbfMigrator → ErpiZaradeProdukcijaImporter). Nije izložen kao zaseban "uvoz iz žive
/// ERPiSredstvaApp instalacije" korisnički put — samo plumbing za DOS uvoz.
/// <para/>
/// Razlike od izvorne šeme (vidi doc komentare na <see cref="Sredstvo"/>/<see cref="Kartica"/>/
/// <see cref="Prijava"/>): string <c>Konto</c> na sve tri tabele postaje <c>KontoId</c> FK
/// (auto-kreira <see cref="Konto"/> ako broj konta ne postoji već u ciljnoj bazi, isti obrazac kao
/// <see cref="ErpiFinansijeImporter"/>); izvorni zaseban <c>Dobavljac</c> model postaje
/// <see cref="Partner"/> (JeDobavljac = true), pošto ERPi namerno nije preneo Dobavljac kao
/// zaseban entitet (§3h u PLAN_NASTAVKA.md).
/// </summary>
public class ErpiSredstvaProdukcijaImporter
{
    private readonly ErpiDbContext _destDb;

    public ErpiSredstvaProdukcijaImporter(ErpiDbContext destDb)
    {
        _destDb = destDb;
    }

    public async Task<SredstvaImportResult> ImportFromDatabaseAsync(SredstvaDbContext srcDb)
    {
        var result = new SredstvaImportResult();

        try
        {
            await ImportFirmaAsync(srcDb);

            var kontaCache = await _destDb.Konta.ToDictionaryAsync(k => k.BrojKonta.Trim());

            int? ResolveKonto(string? kontoBroj)
            {
                var trimmed = (kontoBroj ?? "").Trim();
                if (trimmed.Length == 0) return null;

                if (kontaCache.TryGetValue(trimmed, out var existing)) return existing.KontoId;

                var novi = new Konto
                {
                    BrojKonta = trimmed,
                    NazivKonta = $"(uvezeno iz ERPiSredstva, konto {trimmed})",
                    VrstaKonta = "Aktivna"
                };
                _destDb.Konta.Add(novi);
                kontaCache[trimmed] = novi;
                result.UvezenoKonta++;
                return null; // KontoId dobija vrednost tek posle SaveChanges — vidi FlushNoveKonteAsync niže
            }

            async Task<int?> FlushKontoIdAsync(string? kontoBroj)
            {
                var trimmed = (kontoBroj ?? "").Trim();
                if (trimmed.Length == 0) return null;
                if (kontaCache.TryGetValue(trimmed, out var k) && k.KontoId > 0) return k.KontoId;
                await _destDb.SaveChangesAsync();
                return kontaCache.TryGetValue(trimmed, out var k2) ? k2.KontoId : null;
            }

            // 1. Dobavljači → Partneri (SifraPartnera "SR-DOB-{Konto}" je stabilan ključ za dedup/re-import)
            var srcDobavljaci = await srcDb.Dobavljaci.AsNoTracking().ToListAsync();
            var partnerBySifra = (await _destDb.Partneri
                    .Where(p => p.SifraPartnera != null && p.SifraPartnera != "")
                    .ToListAsync())
                .GroupBy(p => p.SifraPartnera!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var sd in srcDobavljaci)
            {
                var sifra = $"SR-DOB-{sd.Konto}";
                if (partnerBySifra.ContainsKey(sifra)) continue;

                var partner = new Partner
                {
                    SifraPartnera = sifra,
                    Naziv = string.IsNullOrWhiteSpace(sd.OpisKonta) ? $"Dobavljač {sd.Konto}" : sd.OpisKonta,
                    Adresa = sd.UlicaIBroj,
                    PttIMesto = sd.MestoIBroj,
                    JeDobavljac = true,
                    IsActive = true
                };
                _destDb.Partneri.Add(partner);
                partnerBySifra[sifra] = partner;
                result.UvezenoPartneraDobavljaca++;
            }
            await _destDb.SaveChangesAsync();

            var partnerIdBySrcDobavljacId = srcDobavljaci.ToDictionary(
                sd => sd.Id,
                sd => partnerBySifra[$"SR-DOB-{sd.Konto}"].PartnerId);

            // 2. Sredstva (dedup po InventarskiBroj — jedini prirodni ključ, nema DB unique indeks)
            var srcSredstva = await srcDb.Sredstva.AsNoTracking().ToListAsync();
            var existingSredstvaByInvBroj = (await _destDb.Sredstva
                    .Select(s => new { s.Id, s.InventarskiBroj })
                    .ToListAsync())
                .GroupBy(s => s.InventarskiBroj.Trim())
                .ToDictionary(g => g.Key, g => g.First().Id);

            var sredstvoIdBySrcId = new Dictionary<int, int>();

            var srcKarticeWithKonto = await srcDb.Kartice.AsNoTracking()
                .Where(k => k.Konto != null && k.Konto != "")
                .Select(k => new { k.SredstvoId, k.Konto })
                .ToListAsync();

            var srcKarticeKonta = srcKarticeWithKonto
                .GroupBy(k => k.SredstvoId)
                .ToDictionary(g => g.Key, g => g.First().Konto);

            foreach (var ss in srcSredstva)
            {
                if (ss.LegacySifra <= 0 || string.IsNullOrWhiteSpace(ss.Naziv))
                {
                    continue;
                }

                var invBroj = (ss.InventarskiBroj ?? "").Trim();
                if (existingSredstvaByInvBroj.TryGetValue(invBroj, out var postojeciId))
                {
                    sredstvoIdBySrcId[ss.Id] = postojeciId;
                    continue;
                }

                var kontoStr = !string.IsNullOrWhiteSpace(ss.Konto)
                    ? ss.Konto
                    : (srcKarticeKonta.TryGetValue(ss.Id, out var kFromKartica) ? kFromKartica : string.Empty);

                ResolveKonto(kontoStr);
                var kontoId = await FlushKontoIdAsync(kontoStr);

                var ns = new Sredstvo
                {
                    InventarskiBroj = ss.InventarskiBroj ?? string.Empty,
                    Naziv = ss.Naziv,
                    DatumNabavke = ss.DatumNabavke,
                    DatumAktiviranja = ss.DatumAktiviranja,
                    NabavnaVrednost = ss.NabavnaVrednost,
                    IspravkaVrednosti = ss.IspravkaVrednosti,
                    SadasnjaVrednost = ss.SadasnjaVrednost,
                    AmortizacionaGrupa = ss.AmortizacionaGrupa,
                    KontoId = kontoId,
                    ObracunskaJedinica = ss.ObracunskaJedinica,
                    StopaAmortizacije = ss.StopaAmortizacije,
                    RezidualnaVrednost = ss.RezidualnaVrednost,
                    PoreskaGrupa = ss.PoreskaGrupa,
                    PoreskaStopa = ss.PoreskaStopa,
                    PoreskaNabavnaVrednost = ss.PoreskaNabavnaVrednost,
                    PoreskaIspravkaVrednosti = ss.PoreskaIspravkaVrednosti,
                    JeAktivno = ss.JeAktivno,
                    Kolicina = ss.Kolicina,
                    LegacySifra = ss.LegacySifra
                };
                _destDb.Sredstva.Add(ns);
                await _destDb.SaveChangesAsync(); // treba nam Id za mapiranje pre nastavka
                sredstvoIdBySrcId[ss.Id] = ns.Id;
                existingSredstvaByInvBroj[invBroj] = ns.Id;
                result.UvezenoSredstava++;
            }

            // 3. Kartice (dedup po SredstvoId(dest) + RedBroj — RedBroj je sekvencijalan po sredstvu)
            var srcKartice = await srcDb.Kartice.AsNoTracking().ToListAsync();
            var existingKarticaKeys = (await _destDb.SredstvaKartice
                    .Select(k => new { k.SredstvoId, k.RedBroj })
                    .ToListAsync())
                .ToHashSet();

            foreach (var sk in srcKartice)
            {
                if (!sredstvoIdBySrcId.TryGetValue(sk.SredstvoId, out var destSredstvoId)) continue;

                var kljuc = new { SredstvoId = destSredstvoId, sk.RedBroj };
                if (existingKarticaKeys.Contains(kljuc)) continue;

                ResolveKonto(sk.Konto);
                var kontoId = await FlushKontoIdAsync(sk.Konto);

                _destDb.SredstvaKartice.Add(new Kartica
                {
                    SredstvoId = destSredstvoId,
                    RedBroj = sk.RedBroj,
                    Datum = sk.Datum,
                    OpisPromene = sk.OpisPromene,
                    ObracunskaJedinica = sk.ObracunskaJedinica,
                    KontoId = kontoId,
                    AmortizacionaGrupa1 = sk.AmortizacionaGrupa1,
                    AmortizacionaGrupa2 = sk.AmortizacionaGrupa2,
                    StopaAmortizacije = sk.StopaAmortizacije,
                    KoeficijentRevalorizacije = sk.KoeficijentRevalorizacije,
                    Kolicina = sk.Kolicina,
                    NabavnaVrednost = sk.NabavnaVrednost,
                    IspravkaVrednosti = sk.IspravkaVrednosti
                });
                existingKarticaKeys.Add(kljuc);
                result.UvezenoKartica++;
            }
            await _destDb.SaveChangesAsync();

            // 4. Prijave (dedup po BrojNaloga + RedBroj)
            var srcPrijave = await srcDb.Prijave.AsNoTracking().ToListAsync();
            var existingPrijaveKeys = (await _destDb.SredstvaPrijave
                    .Select(p => new { p.BrojNaloga, p.RedBroj })
                    .ToListAsync())
                .ToHashSet();

            foreach (var sp in srcPrijave)
            {
                if (!sredstvoIdBySrcId.TryGetValue(sp.SredstvoId, out var destSredstvoId)) continue;

                var kljuc = new { sp.BrojNaloga, sp.RedBroj };
                if (existingPrijaveKeys.Contains(kljuc)) continue;

                ResolveKonto(sp.Konto);
                var kontoId = await FlushKontoIdAsync(sp.Konto);
                int? partnerId = sp.DobavljacId.HasValue && partnerIdBySrcDobavljacId.TryGetValue(sp.DobavljacId.Value, out var pId)
                    ? pId
                    : null;

                _destDb.SredstvaPrijave.Add(new Prijava
                {
                    BrojNaloga = sp.BrojNaloga,
                    RedBroj = sp.RedBroj,
                    SredstvoId = destSredstvoId,
                    ObracunskaJedinica = sp.ObracunskaJedinica,
                    KontoId = kontoId,
                    AmortizacionaGrupa1 = sp.AmortizacionaGrupa1,
                    AmortizacionaGrupa2 = sp.AmortizacionaGrupa2,
                    StopaAmortizacije = sp.StopaAmortizacije,
                    DatumAktiviranja = sp.DatumAktiviranja,
                    RevalorizacionaGrupa = sp.RevalorizacionaGrupa,
                    NabavnaVrednost = sp.NabavnaVrednost,
                    OtpisanaVrednost = sp.OtpisanaVrednost,
                    JedinicaMere = sp.JedinicaMere,
                    Kolicina = sp.Kolicina,
                    InventarskiBroj = sp.InventarskiBroj,
                    BrojFakture = sp.BrojFakture,
                    DatumFakture = sp.DatumFakture,
                    BrojNalaznice = sp.BrojNalaznice,
                    BrNal = sp.BrNal,
                    GodNal = sp.GodNal,
                    Knjizen = sp.Knjizen,
                    PartnerId = partnerId
                });
                existingPrijaveKeys.Add(kljuc);
                result.UvezenoPrijava++;
            }
            await _destDb.SaveChangesAsync();

            // 5. Rashodi (dedup po BrojNaloga + RedBroj — nema string reference, prost port)
            var srcRashodi = await srcDb.Rashodi.AsNoTracking().ToListAsync();
            var existingRashodiKeys = (await _destDb.SredstvaRashodi
                    .Select(r => new { r.BrojNaloga, r.RedBroj })
                    .ToListAsync())
                .ToHashSet();

            foreach (var sr in srcRashodi)
            {
                if (!sredstvoIdBySrcId.TryGetValue(sr.SredstvoId, out var destSredstvoId)) continue;

                var kljuc = new { sr.BrojNaloga, sr.RedBroj };
                if (existingRashodiKeys.Contains(kljuc)) continue;

                _destDb.SredstvaRashodi.Add(new Rashod
                {
                    BrojNaloga = sr.BrojNaloga,
                    RedBroj = sr.RedBroj,
                    SredstvoId = destSredstvoId,
                    Kod = (TipoviPromena)(int)sr.Kod,
                    KodTekst = sr.KodTekst,
                    Datum = sr.Datum,
                    DokumentBroj = sr.DokumentBroj,
                    Podaci = sr.Podaci,
                    ObracunskaJedinica = sr.ObracunskaJedinica,
                    Knjizen = sr.Knjizen
                });
                existingRashodiKeys.Add(kljuc);
                result.UvezenoRashoda++;
            }
            await _destDb.SaveChangesAsync();

            // Ažuriranje statusa JeAktivno = false za rashodovana sredstva
            var rashodIds1 = await _destDb.SredstvaRashodi
                .Where(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.Prodaja || r.Kod == TipoviPromena.Otudjenje || r.Kod == TipoviPromena.Brisanje)
                .Select(r => r.SredstvoId)
                .ToListAsync();

            var rashodIds2 = await _destDb.SredstvaKartice
                .Where(k => k.OpisPromene != null && (k.OpisPromene.StartsWith("Rashod") || k.OpisPromene.StartsWith("Prodaja") || k.OpisPromene.StartsWith("Otudjenje")))
                .Select(k => k.SredstvoId)
                .ToListAsync();

            var destRashodovanaIds = rashodIds1.Concat(rashodIds2).ToHashSet();

            var zaUpdateAktivno = await _destDb.Sredstva.Where(s => destRashodovanaIds.Contains(s.Id) && s.JeAktivno).ToListAsync();
            if (zaUpdateAktivno.Count > 0)
            {
                foreach (var s in zaUpdateAktivno) s.JeAktivno = false;
                await _destDb.SaveChangesAsync();
            }

            // 6. Komisije (dedup po Naziv + DatumKreiranja)
            var srcKomisije = await srcDb.Komisije.AsNoTracking().ToListAsync();
            var existingKomisijeKeys = (await _destDb.Komisije
                    .Select(k => new { k.Naziv, k.DatumKreiranja })
                    .ToListAsync())
                .ToHashSet();
            var komisijaIdBySrcId = new Dictionary<int, int>();

            foreach (var sk in srcKomisije)
            {
                var kljuc = new { sk.Naziv, sk.DatumKreiranja };
                if (existingKomisijeKeys.Contains(kljuc))
                {
                    var postojeca = await _destDb.Komisije.FirstAsync(k => k.Naziv == sk.Naziv && k.DatumKreiranja == sk.DatumKreiranja);
                    komisijaIdBySrcId[sk.Id] = postojeca.Id;
                    continue;
                }

                var nk = new Komisija
                {
                    Naziv = sk.Naziv,
                    DatumKreiranja = sk.DatumKreiranja,
                    JeAktivna = sk.JeAktivna
                };
                _destDb.Komisije.Add(nk);
                await _destDb.SaveChangesAsync();
                komisijaIdBySrcId[sk.Id] = nk.Id;
                existingKomisijeKeys.Add(kljuc);
                result.UvezenoKomisija++;
            }

            // 7. Članovi komisije (dedup po KomisijaId(dest) + ImePrezime + Uloga)
            var srcClanovi = await srcDb.ClanoviKomisije.AsNoTracking().ToListAsync();
            var existingClanoviKeys = (await _destDb.ClanoviKomisije
                    .Select(c => new { c.KomisijaId, c.ImePrezime, c.Uloga })
                    .ToListAsync())
                .ToHashSet();

            foreach (var sc in srcClanovi)
            {
                if (!komisijaIdBySrcId.TryGetValue(sc.KomisijaId, out var destKomisijaId)) continue;

                var kljuc = new { KomisijaId = destKomisijaId, sc.ImePrezime, sc.Uloga };
                if (existingClanoviKeys.Contains(kljuc)) continue;

                _destDb.ClanoviKomisije.Add(new ClanKomisije
                {
                    KomisijaId = destKomisijaId,
                    ImePrezime = sc.ImePrezime,
                    Uloga = sc.Uloga
                });
                existingClanoviKeys.Add(kljuc);
                result.UvezenoClanovaKomisije++;
            }
            await _destDb.SaveChangesAsync();

            // 8. Popisi (dedup po Godina + KomisijaId(dest) + DatumPopisa)
            var srcPopisi = await srcDb.Popisi.AsNoTracking().ToListAsync();
            var existingPopisiKeys = (await _destDb.Popisi
                    .Select(p => new { p.Godina, p.KomisijaId, p.DatumPopisa })
                    .ToListAsync())
                .ToHashSet();
            var popisIdBySrcId = new Dictionary<int, int>();

            foreach (var sp in srcPopisi)
            {
                if (!komisijaIdBySrcId.TryGetValue(sp.KomisijaId, out var destKomisijaId)) continue;

                var kljuc = new { sp.Godina, KomisijaId = destKomisijaId, sp.DatumPopisa };
                if (existingPopisiKeys.Contains(kljuc))
                {
                    var postojeci = await _destDb.Popisi.FirstAsync(p => p.Godina == sp.Godina && p.KomisijaId == destKomisijaId && p.DatumPopisa == sp.DatumPopisa);
                    popisIdBySrcId[sp.Id] = postojeci.Id;
                    continue;
                }

                var np = new ERPiData.Models.Sredstva.Popis
                {
                    DatumPopisa = sp.DatumPopisa,
                    Godina = sp.Godina,
                    KomisijaId = destKomisijaId,
                    Status = (StatusPopisa)(int)sp.Status
                };
                _destDb.Popisi.Add(np);
                await _destDb.SaveChangesAsync();
                popisIdBySrcId[sp.Id] = np.Id;
                existingPopisiKeys.Add(kljuc);
                result.UvezenoPopisa++;
            }

            // 9. Popisne stavke (dedup po PopisId(dest) + SredstvoId(dest))
            var srcStavke = await srcDb.PopisneStavke.AsNoTracking().ToListAsync();
            var existingStavkeKeys = (await _destDb.PopisneStavke
                    .Select(s => new { s.PopisId, s.SredstvoId })
                    .ToListAsync())
                .ToHashSet();

            foreach (var ss in srcStavke)
            {
                if (!popisIdBySrcId.TryGetValue(ss.PopisId, out var destPopisId)) continue;
                if (!sredstvoIdBySrcId.TryGetValue(ss.SredstvoId, out var destSredstvoId)) continue;

                var kljuc = new { PopisId = destPopisId, SredstvoId = destSredstvoId };
                if (existingStavkeKeys.Contains(kljuc)) continue;

                _destDb.PopisneStavke.Add(new PopisnaStavka
                {
                    PopisId = destPopisId,
                    SredstvoId = destSredstvoId,
                    KnjiznaKolicina = ss.KnjiznaKolicina,
                    PopisanaKolicina = ss.PopisanaKolicina,
                    KnjiznaVrednost = ss.KnjiznaVrednost,
                    ProcenjenaVrednost = ss.ProcenjenaVrednost,
                    Napomena = ss.Napomena
                });
                existingStavkeKeys.Add(kljuc);
                result.UvezenoPopisnihStavki++;
            }
            await _destDb.SaveChangesAsync();

            result.Uspesno = true;
        }
        catch (Exception ex)
        {
            result.Uspesno = false;
            result.Greska = ex.InnerException?.Message ?? ex.Message;
        }

        return result;
    }

    private async Task ImportFirmaAsync(SredstvaDbContext srcDb)
    {
        var srcFirma = await srcDb.Firme.AsNoTracking().FirstOrDefaultAsync();
        if (srcFirma == null) return;

        var destFirma = await _destDb.Firme.FirstOrDefaultAsync();
        if (destFirma == null) return; // Firma se kreira kroz CompanySelectWindow, ne ovde

        // Samo dopunjuje prazna polja — nikad ne prepisuje već postojeće podatke firme.
        if (string.IsNullOrWhiteSpace(destFirma.PttIMesto) && !string.IsNullOrWhiteSpace(srcFirma.Mesto))
            destFirma.PttIMesto = srcFirma.Mesto;
        if (string.IsNullOrWhiteSpace(destFirma.MaticniBroj) && !string.IsNullOrWhiteSpace(srcFirma.MaticniBroj))
            destFirma.MaticniBroj = srcFirma.MaticniBroj;
        if (string.IsNullOrWhiteSpace(destFirma.Pib) && !string.IsNullOrWhiteSpace(srcFirma.PIB))
            destFirma.Pib = srcFirma.PIB;

        await _destDb.SaveChangesAsync();
    }
}
