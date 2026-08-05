using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Zarade;
using ERPiZaradeData;
using Microsoft.EntityFrameworkCore;

namespace ERPiMigration.Importers;

public class ZaradeImportResult
{
    public int UvezenoRadnika { get; set; }
    public int UvezenoObracuna { get; set; }
    public int UvezenoIsplata { get; set; }
    public int UvezenoUgovora { get; set; }
    public int UvezenoRadnihSati { get; set; }
    public int UvezenoSamodoprinosa { get; set; }
    public int UvezenoKredita { get; set; }
    public int UvezenoPppPdPrijava { get; set; }
    public int UvezenoBolovanja { get; set; }
    public int UvezenoDoprinosaPoslodavca { get; set; }
    public int UvezenoPoreza { get; set; }
    public int UvezenoDoprinosa { get; set; }
    public int UvezenoBanaka { get; set; }
    public int UvezenoPlatnihRazreda { get; set; }
    public int UvezenoPoreskihOlaksica { get; set; }
    public int UvezenoSablonaUgovora { get; set; }
    public bool Uspesno { get; set; } = true;
    public string Greska { get; set; } = "";
}

public class ErpiZaradeProdukcijaImporter
{
    private readonly ErpiDbContext _destDb;

    public ErpiZaradeProdukcijaImporter(ErpiDbContext destDb)
    {
        _destDb = destDb;
    }

    public async Task<ZaradeImportResult> ImportFromDatabaseAsync(PlataDbContext srcDb)
    {
        var result = new ZaradeImportResult();

        try
        {
            // 1. Sinhronizacija šifarnika
            await ImportSifarniciAsync(srcDb, result);

            // 2. Partneri i Mesta Troška iz Radnika
            var srcRadnici = await srcDb.Radnici.AsNoTracking().ToListAsync();
            var partneriByJmbg = await _destDb.Partneri
                .Where(p => p.Jmbg != null && p.Jmbg != "")
                .ToDictionaryAsync(p => p.Jmbg!.Trim(), StringComparer.OrdinalIgnoreCase);

            var mestaTroskaBySifra = await _destDb.MestaTroska
                .Where(m => m.Sifra != "")
                .ToDictionaryAsync(m => m.Sifra.Trim(), StringComparer.OrdinalIgnoreCase);

            // Partner po BrojRadnika mapiranje za ovu sesiju uvoza
            var partnerIdByBrojRadnika = new Dictionary<int, int>();

            foreach (var r in srcRadnici.GroupBy(r => r.BrojRadnika).Select(g => g.First()))
            {
                Partner? partner = null;
                string cleanJmbg = (r.Jmbg ?? "").Trim();
                if (!string.IsNullOrEmpty(cleanJmbg) && partneriByJmbg.TryGetValue(cleanJmbg, out var pExisting))
                {
                    partner = pExisting;
                }

                if (partner == null)
                {
                    partner = new Partner
                    {
                        SifraPartnera = $"R-{r.BrojRadnika:D4}",
                        Naziv = r.ImeIPrezime,
                        Jmbg = cleanJmbg,
                        Adresa = r.AdresaStanovanja,
                        PttIMesto = string.IsNullOrWhiteSpace(r.SifraOpstine) ? r.Mesto : $"{r.SifraOpstine} {r.Mesto}",
                        Telefon = "",
                        ZiroRacun = r.BankovniRacun,
                        JeRadnik = true,
                        IsActive = true
                    };
                    _destDb.Partneri.Add(partner);
                    await _destDb.SaveChangesAsync();
                    if (!string.IsNullOrEmpty(cleanJmbg)) partneriByJmbg[cleanJmbg] = partner;
                }
                else if (!partner.JeRadnik)
                {
                    partner.JeRadnik = true;
                    await _destDb.SaveChangesAsync();
                }

                partnerIdByBrojRadnika[r.BrojRadnika] = partner.PartnerId;

                // Mesto troška
                if (!string.IsNullOrWhiteSpace(r.SifraMestaTroska))
                {
                    string cleanMt = r.SifraMestaTroska.Trim();
                    if (!mestaTroskaBySifra.ContainsKey(cleanMt))
                    {
                        var mt = new MestoTroska
                        {
                            Sifra = cleanMt,
                            Naziv = $"Mesto troška {cleanMt}"
                        };
                        _destDb.MestaTroska.Add(mt);
                        await _destDb.SaveChangesAsync();
                        mestaTroskaBySifra[cleanMt] = mt;
                    }
                }
            }

            // 3. Uvoz Radnika
            var existingRadniciKeys = (await _destDb.Radnici
                .Select(r => new { r.BrojRadnika, r.Godina, r.Mesec })
                .ToListAsync())
                .ToHashSet();

            var radniciSrcToDestMap = new Dictionary<int, Radnik>();

            foreach (var sr in srcRadnici)
            {
                var key = new { sr.BrojRadnika, sr.Godina, sr.Mesec };
                if (existingRadniciKeys.Contains(key)) continue;

                int? partnerId = partnerIdByBrojRadnika.TryGetValue(sr.BrojRadnika, out int pId) ? pId : null;
                int? mtId = (!string.IsNullOrWhiteSpace(sr.SifraMestaTroska) && mestaTroskaBySifra.TryGetValue(sr.SifraMestaTroska.Trim(), out var mtObj)) ? mtObj.MestoTroskaId : null;

                var dr = new Radnik
                {
                    PartnerId = partnerId,
                    MestoTroskaId = mtId,
                    Godina = sr.Godina,
                    Mesec = sr.Mesec,
                    BrojRadnika = sr.BrojRadnika,
                    ImeIPrezime = sr.ImeIPrezime,
                    Jmbg = sr.Jmbg,
                    MaticniBroj = sr.MaticniBroj,
                    DatumRodjenja = sr.DatumRodjenja,
                    MestoRodjenja = sr.MestoRodjenja,
                    AdresaStanovanja = sr.AdresaStanovanja,
                    Mesto = sr.Mesto,
                    SifraOpstine = sr.SifraOpstine,
                    Email = sr.Email,
                    Lbo = sr.Lbo,
                    DatumZaposlenja = sr.DatumZaposlenja,
                    DatumPrestanka = sr.DatumPrestanka,
                    Kategorija = sr.Kategorija,
                    Radno_Mesto = sr.Radno_Mesto,
                    BrojRadneJedinice = sr.BrojRadneJedinice,
                    SifraMestaTroska = sr.SifraMestaTroska,
                    MinuliRadGodine = sr.MinuliRadGodine,
                    Koeficijent = sr.Koeficijent,
                    Koeficijent1 = sr.Koeficijent1,
                    OsnovnaPlata = sr.OsnovnaPlata,
                    StopaPio = sr.StopaPio,
                    StopaZdravstvo = sr.StopaZdravstvo,
                    StopaNezaposlenost = sr.StopaNezaposlenost,
                    BankovniRacun = sr.BankovniRacun,
                    NazivBanke = sr.NazivBanke,
                    Aktivan = sr.Aktivan,
                    VanRadnogOdnosa = sr.VanRadnogOdnosa,
                    LicniOslobodjenje = sr.LicnoOslobodjenje,
                    ProcenatPovracajaPoreza = sr.ProcenatPovracajaPoreza,
                    ProcenatPovracajaDoprinosa = sr.ProcenatPovracajaDoprinosa,
                    OlaksicaVaziDo = sr.OlaksicaVaziDo,
                    Operativni = sr.Operativni,
                    DatumUnosa = sr.DatumUnosa,
                    DatumIzmene = sr.DatumIzmene
                };

                _destDb.Radnici.Add(dr);
                radniciSrcToDestMap[sr.Id] = dr;
                result.UvezenoRadnika++;
            }
            await _destDb.SaveChangesAsync();

            // Sastavi kompletnu mapu RadnikId iz izvora u dest bazu po (BrojRadnika, Godina, Mesec)
            var allDestRadnici = await _destDb.Radnici.AsNoTracking().ToListAsync();
            var destRadniciByPeriodAndBroj = allDestRadnici
                .ToDictionary(r => (r.BrojRadnika, r.Godina, r.Mesec), r => r.Id);

            // 4. Uvoz Isplata
            var srcIsplate = await srcDb.Isplate.AsNoTracking().ToListAsync();
            var isplataSrcToDestMap = new Dictionary<int, int>();
            var existingIsplateKeys = (await _destDb.Isplate.Select(i => new { i.Godina, i.Mesec, i.RedniBroj }).ToListAsync()).ToHashSet();

            foreach (var si in srcIsplate)
            {
                var key = new { si.Godina, si.Mesec, si.RedniBroj };
                if (existingIsplateKeys.Contains(key))
                {
                    var existing = await _destDb.Isplate.FirstAsync(i => i.Godina == si.Godina && i.Mesec == si.Mesec && i.RedniBroj == si.RedniBroj);
                    isplataSrcToDestMap[si.IsplataId] = existing.IsplataId;
                    continue;
                }

                var di = new Isplata
                {
                    Godina = si.Godina,
                    Mesec = si.Mesec,
                    RedniBroj = si.RedniBroj,
                    Rod = (RodIsplate)(int)si.Rod,
                    Vrsta = (VrstaIsplate)(int)si.Vrsta,
                    Opis = si.Opis,
                    DatumIsplate = si.DatumIsplate,
                    DatumKreiranja = si.DatumKreiranja
                };
                _destDb.Isplate.Add(di);
                await _destDb.SaveChangesAsync();
                isplataSrcToDestMap[si.IsplataId] = di.IsplataId;
                result.UvezenoIsplata++;
            }

            // 5. Uvoz Ugovora
            // Dedup: Ugovor nema jedinstven indeks u bazi (samo obican BrojRadnika), pa se
            // ponovni uvoz mora ručno zaustaviti po (BrojRadnika, Broj, DatumZakljucenja) —
            // najbliža prirodna oznaka ugovora koju izvor nosi.
            var srcUgovori = await srcDb.Ugovori.Include(u => u.VrstaUgovora).AsNoTracking().ToListAsync();
            var vrstaUgovoraBySifra = await _destDb.VrsteUgovora.ToDictionaryAsync(v => v.Sifra);
            var ugovorSrcToDestMap = new Dictionary<int, int>();

            // GroupBy+First umesto ToDictionary: ako u bazi već postoje stariji duplikati (npr.
            // iz perioda pre nego što je ovaj dedup dodat), grupisanje ih toleriše umesto da puca
            // na "duplicate key" — poslednja odbrana je ionako da se ništa novo ne duplira.
            var existingUgovoriByKey = (await _destDb.Ugovori
                .Select(u => new { u.UgovorId, u.BrojRadnika, u.Broj, u.DatumZakljucenja })
                .ToListAsync())
                .GroupBy(u => new { u.BrojRadnika, u.Broj, u.DatumZakljucenja })
                .ToDictionary(g => g.Key, g => g.First().UgovorId);

            foreach (var su in srcUgovori)
            {
                if (!vrstaUgovoraBySifra.TryGetValue(su.VrstaUgovora.Sifra, out var vuDest)) continue;

                var kljucUgovora = new { su.BrojRadnika, su.Broj, su.DatumZakljucenja };
                if (existingUgovoriByKey.TryGetValue(kljucUgovora, out var postojeciUgovorId))
                {
                    ugovorSrcToDestMap[su.UgovorId] = postojeciUgovorId;
                    continue;
                }

                var du = new Ugovor
                {
                    VrstaUgovoraId = vuDest.VrstaUgovoraId,
                    BrojRadnika = su.BrojRadnika,
                    TipPrimaoca = (TipPrimaocaPrihoda)(int)su.TipPrimaoca,
                    Broj = su.Broj,
                    Predmet = su.Predmet,
                    DatumZakljucenja = su.DatumZakljucenja,
                    DatumOd = su.DatumOd,
                    DatumDo = su.DatumDo,
                    UgovorenIznos = su.UgovorenIznos,
                    IznosJeNeto = su.IznosJeNeto,
                    Aktivan = su.Aktivan,
                    Tekst = su.Tekst,
                    DatumTeksta = su.DatumTeksta,
                    Napomena = su.Napomena,
                    DatumUnosa = su.DatumUnosa
                };
                _destDb.Ugovori.Add(du);
                await _destDb.SaveChangesAsync();
                ugovorSrcToDestMap[su.UgovorId] = du.UgovorId;
                existingUgovoriByKey[kljucUgovora] = du.UgovorId;
                result.UvezenoUgovora++;
            }

            // 6. Uvoz Obračuna Plate
            var srcObracuni = await srcDb.ObracuniPlata
                .Include(o => o.Stavke)
                .ThenInclude(s => s.VrstaPrimanja)
                .AsNoTracking().ToListAsync();

            var vrstPrimanjaBySifra = await _destDb.VrstePrimanja.ToDictionaryAsync(v => v.Sifra);
            var srcRadniciDict = srcRadnici.ToDictionary(r => r.Id);

            // Dedup: ObracunPlate nema jedinstven indeks u bazi (samo obican), pa ponovno
            // pokretanje uvoza nad već uvezenom firmom mora samo da preskoči postojeće —
            // inače tiho duplira svaki obračun (uhvaćeno pri testiranju re-importa).
            var existingObracuniKeys = (await _destDb.ObracuniPlata
                .Select(o => new { o.RadnikId, o.Godina, o.Mesec })
                .ToListAsync())
                .ToHashSet();

            foreach (var so in srcObracuni)
            {
                if (!srcRadniciDict.TryGetValue(so.RadnikId, out var srcRadnik)) continue;

                if (!destRadniciByPeriodAndBroj.TryGetValue((srcRadnik.BrojRadnika, so.Godina, so.Mesec), out int destRadnikId)) continue;

                if (existingObracuniKeys.Contains(new { RadnikId = destRadnikId, so.Godina, so.Mesec })) continue;

                int? destIsplataId = so.IsplataId.HasValue && isplataSrcToDestMap.TryGetValue(so.IsplataId.Value, out int iId) ? iId : null;
                int? destUgovorId = so.UgovorId.HasValue && ugovorSrcToDestMap.TryGetValue(so.UgovorId.Value, out int uId) ? uId : null;

                var dobr = new ObracunPlate
                {
                    RadnikId = destRadnikId,
                    Godina = so.Godina,
                    Mesec = so.Mesec,
                    IsplataId = destIsplataId,
                    Zakljucan = so.Zakljucan,
                    UgovorId = destUgovorId,
                    OsnovicaDoprinosa = so.OsnovicaDoprinosa,
                    Storniran = so.Storniran,
                    DatumStorniranja = so.DatumStorniranja,
                    RazlogStorniranja = so.RazlogStorniranja,
                    Verzija = so.Verzija,
                    OlaksicaOznaka = so.OlaksicaOznaka,
                    OlaksicaPorez = so.OlaksicaPorez,
                    OlaksicaDoprinosi = so.OlaksicaDoprinosi,
                    OlaksicaUmanjujeUplatu = so.OlaksicaUmanjujeUplatu,

                    BrutoZarada = so.BrutoZarada,
                    BrutoBolovanje = so.BrutoBolovanje,
                    BrutoNaknade = so.BrutoNaknade,
                    BrutoStimulacija = so.BrutoStimulacija,
                    BrutoMinuliRad = so.BrutoMinuliRad,

                    NetoZar = so.NetoZar, NetoNerd = so.NetoNerd, NetoGOd = so.NetoGOd, NetoTo = so.NetoTo, NetoReg = so.NetoReg,
                    Neto = so.Neto, NetoBol = so.NetoBol, NetoB100 = so.NetoB100, NetoPlac = so.NetoPlac, NetoPlZ = so.NetoPlZ,
                    NetoDrza = so.NetoDrza, NetoNocni = so.NetoNocni, NetoVezba = so.NetoVezba, NetoPrek = so.NetoPrek, NetoTer = so.NetoTer,
                    KorDod = so.KorDod, KorDod1 = so.KorDod1, Kumul = so.Kumul, NetoNede = so.NetoNede,

                    DoprinosPioRadnik = so.DoprinosPioRadnik,
                    DoprinosZdravstvoRadnik = so.DoprinosZdravstvoRadnik,
                    DoprinosNezaposlenostRadnik = so.DoprinosNezaposlenostRadnik,

                    DoprinosPioPoslodavac = so.DoprinosPioPoslodavac,
                    DoprinosZdravstvoPoslodavac = so.DoprinosZdravstvoPoslodavac,
                    DoprinosNezaposlenostPoslodavac = so.DoprinosNezaposlenostPoslodavac,

                    PorezNaDohodak = so.PorezNaDohodak,
                    PoreskaOsnovica = so.PoreskaOsnovica,
                    LicniOdbitak = so.LicniOdbitak,

                    KreditObustava = so.KreditObustava,
                    Samodoprinosi = so.Samodoprinosi,
                    OstaliOdbici = so.OstaliOdbici,

                    NetoIsplata = so.NetoIsplata,

                    RedovniSati = so.RedovniSati,
                    BolovanjeSati = so.BolovanjeSati,
                    PrekovremeneSati = so.PrekovremeneSati,
                    GodisnjioOdmorSati = so.GodisnjioOdmorSati,
                    DrzavniPraznikSati = so.DrzavniPraznikSati,
                    NocniSati = so.NocniSati,
                    SmenskiSati = so.SmenskiSati,
                    RadPraznikomSati = so.RadPraznikomSati,
                    NocniRadPraznikomSati = so.NocniRadPraznikomSati,
                    PlacenoOdsustvoSati = so.PlacenoOdsustvoSati,

                    DatumObracuna = so.DatumObracuna,
                    Prosek = so.Prosek,
                    Napomena = so.Napomena,

                    Koeficijent = so.Koeficijent,
                    MinuliRadGodine = so.MinuliRadGodine,
                    Kategorija = so.Kategorija,
                    BrojRadneJedinice = so.BrojRadneJedinice,
                    UkupnoRadnihSatiLegacy = so.UkupnoRadnihSatiLegacy,
                    FondSatiMesecni = so.FondSatiMesecni,
                    CenaSataRedovan = so.CenaSataRedovan,
                    CenaSataMinuliRad = so.CenaSataMinuliRad,
                    DodaciLegacy = so.DodaciLegacy,
                    DodatakNaM1 = so.DodatakNaM1,
                    DodatakNaM2 = so.DodatakNaM2,
                    DodatakNaM3 = so.DodatakNaM3,
                    BrutoOsnovica = so.BrutoOsnovica,
                    TopliObrokIznos = so.TopliObrokIznos,
                    BrutoPioOsnovica = so.BrutoPioOsnovica,
                    NetoNaknadeLegacy = so.NetoNaknadeLegacy,
                    Operativni = so.Operativni,
                    Oznaka = so.Oznaka,
                    NedeljaSati = so.NedeljaSati,
                    BolovanjePreko60SatiLegacy = so.BolovanjePreko60SatiLegacy,
                    PorodiljskoOdsustvoSatiLegacy = so.PorodiljskoOdsustvoSatiLegacy,
                    PlacenoOdsustvoSatiLegacy = so.PlacenoOdsustvoSatiLegacy,
                    PlacenoZakonskiSatiLegacy = so.PlacenoZakonskiSatiLegacy,
                    Bolovanje100SatiLegacy = so.Bolovanje100SatiLegacy,
                    MinimalnaPlataOsnovica = so.MinimalnaPlataOsnovica,
                    SifraSamodoprinosa1 = so.SifraSamodoprinosa1,
                    SifraSamodoprinosa2 = so.SifraSamodoprinosa2,
                    PosebanPorez = so.PosebanPorez,
                    NetoPorez = so.NetoPorez,
                    NetoBezPoreza = so.NetoBezPoreza,
                    Varijabila = so.Varijabila
                };

                foreach (var ss in so.Stavke)
                {
                    if (vrstPrimanjaBySifra.TryGetValue(ss.VrstaPrimanja.Sifra, out var vpDest))
                    {
                        dobr.Stavke.Add(new ObracunStavka
                        {
                            VrstaPrimanjaId = vpDest.VrstaPrimanjaId,
                            Sati = ss.Sati,
                            Iznos = ss.Iznos,
                            OporeziviDeo = ss.OporeziviDeo
                        });
                    }
                }

                _destDb.ObracuniPlata.Add(dobr);
                existingObracuniKeys.Add(new { RadnikId = destRadnikId, so.Godina, so.Mesec });
                result.UvezenoObracuna++;
            }
            await _destDb.SaveChangesAsync();

            // 7. Uvoz Kredita i Radnih Sati
            // Dedup: Kredit nema jedinstven indeks u bazi, pa se prati po (RadnikId, Opis,
            // UkupanIznos, DatumPocetka) da ponovno pokretanje ne duplira zapise.
            var existingKreditiKeys = (await _destDb.Krediti
                .Select(k => new { k.RadnikId, k.Opis, k.UkupanIznos, k.DatumPocetka })
                .ToListAsync())
                .ToHashSet();

            var srcKrediti = await srcDb.Krediti.AsNoTracking().ToListAsync();
            foreach (var sk in srcKrediti)
            {
                if (!srcRadniciDict.TryGetValue(sk.RadnikId, out var rSrc)) continue;
                if (!destRadniciByPeriodAndBroj.TryGetValue((rSrc.BrojRadnika, rSrc.Godina, rSrc.Mesec), out int rDestId)) continue;

                if (existingKreditiKeys.Contains(new { RadnikId = rDestId, sk.Opis, sk.UkupanIznos, sk.DatumPocetka })) continue;

                _destDb.Krediti.Add(new Kredit
                {
                    RadnikId = rDestId,
                    Opis = sk.Opis,
                    UkupanIznos = sk.UkupanIznos,
                    MesecnaRata = sk.MesecnaRata,
                    OstatakDuga = sk.OstatakDuga,
                    BrojRata = sk.BrojRata,
                    PlateneRate = sk.PlateneRate,
                    DatumPocetka = sk.DatumPocetka,
                    DatumZavrsetka = sk.DatumZavrsetka,
                    Aktivan = sk.Aktivan,
                    PrimalacNaziv = sk.PrimalacNaziv,
                    PrimalacRacun = sk.PrimalacRacun,
                    ModelPozivaNaBroj = sk.ModelPozivaNaBroj,
                    PozivNaBroj = sk.PozivNaBroj,
                    Tip = (TipObustave)(int)sk.Tip,
                    RedosledNaplate = sk.RedosledNaplate
                });
                existingKreditiKeys.Add(new { RadnikId = rDestId, sk.Opis, sk.UkupanIznos, sk.DatumPocetka });
                result.UvezenoKredita++;
            }

            // Dedup: RadniSat ima jedinstven indeks (RadnikId, Godina, Mesec, IsplataId) u bazi —
            // bez ovog preskakanja, ponovni uvoz baca UNIQUE constraint grešku i obara CEO
            // preostali SaveChanges (uključujući nepovezane PppPdPrijave/Bolovanja niže).
            var existingRadniSatiKeys = (await _destDb.RadniSati
                .Select(rs => new { rs.RadnikId, rs.Godina, rs.Mesec, rs.IsplataId })
                .ToListAsync())
                .ToHashSet();

            var srcRadniSati = await srcDb.RadniSati.AsNoTracking().ToListAsync();
            foreach (var srs in srcRadniSati)
            {
                if (!srcRadniciDict.TryGetValue(srs.RadnikId, out var rSrc)) continue;
                if (!destRadniciByPeriodAndBroj.TryGetValue((rSrc.BrojRadnika, srs.Godina, srs.Mesec), out int rDestId)) continue;

                int? destIsplataId = srs.IsplataId.HasValue && isplataSrcToDestMap.TryGetValue(srs.IsplataId.Value, out int iId) ? iId : null;

                if (existingRadniSatiKeys.Contains(new { RadnikId = rDestId, srs.Godina, srs.Mesec, IsplataId = destIsplataId })) continue;

                _destDb.RadniSati.Add(new RadniSat
                {
                    RadnikId = rDestId,
                    Godina = srs.Godina,
                    Mesec = srs.Mesec,
                    IsplataId = destIsplataId,
                    RedovniSati = srs.RedovniSati,
                    BolovanjeSati = srs.BolovanjeSati,
                    PrekovremeneSati = srs.PrekovremeneSati,
                    GodisnjiOdmorSati = srs.GodisnjiOdmorSati,
                    DrzavniPraznikSati = srs.DrzavniPraznikSati,
                    NocniSati = srs.NocniSati,
                    SmenskiSati = srs.SmenskiSati,
                    RadPraznikomSati = srs.RadPraznikomSati,
                    NocniRadPraznikomSati = srs.NocniRadPraznikomSati,
                    PlacenoOdsustvoSati = srs.PlacenoOdsustvoSati,
                    Stimulacija = srs.Stimulacija,
                    RadNedeljomSati = srs.RadNedeljomSati,
                    PlacenoZakonskiSati = srs.PlacenoZakonskiSati,
                    BolovanjePreko60Sati = srs.BolovanjePreko60Sati,
                    PorodiljskoOdsustvoSati = srs.PorodiljskoOdsustvoSati,
                    Bolovanje100Sati = srs.Bolovanje100Sati,
                    TopliObrokDani = srs.TopliObrokDani,
                    RegresIznos = srs.RegresIznos,
                    Prosek = srs.Prosek,
                    Varijabila = srs.Varijabila
                });
                existingRadniSatiKeys.Add(new { RadnikId = rDestId, srs.Godina, srs.Mesec, IsplataId = destIsplataId });
                result.UvezenoRadnihSati++;
            }

            // Samodoprinosi (obustave/krediti detalj po radniku i periodu) — nema jedinstven
            // indeks u bazi, dedup po (RadnikId, Godina, Mesec, Opis, Iznos).
            var existingSamodoprinosiKeys = (await _destDb.Samodoprinosi
                .Select(s => new { s.RadnikId, s.Godina, s.Mesec, s.Opis, s.Iznos })
                .ToListAsync())
                .ToHashSet();

            var srcSamodoprinosi = await srcDb.Samodoprinosi.AsNoTracking().ToListAsync();
            foreach (var ssd in srcSamodoprinosi)
            {
                if (!srcRadniciDict.TryGetValue(ssd.RadnikId, out var rSrc)) continue;
                if (!destRadniciByPeriodAndBroj.TryGetValue((rSrc.BrojRadnika, ssd.Godina, ssd.Mesec), out int rDestId)) continue;

                var kljucSamodoprinosa = new { RadnikId = rDestId, ssd.Godina, ssd.Mesec, ssd.Opis, ssd.Iznos };
                if (existingSamodoprinosiKeys.Contains(kljucSamodoprinosa)) continue;

                _destDb.Samodoprinosi.Add(new Samodoprinosi
                {
                    RadnikId = rDestId,
                    Godina = ssd.Godina,
                    Mesec = ssd.Mesec,
                    Iznos = ssd.Iznos,
                    Opis = ssd.Opis
                });
                existingSamodoprinosiKeys.Add(kljucSamodoprinosa);
                result.UvezenoSamodoprinosa++;
            }

            // DoprinosiPoslodavca (detaljni doprinosi poslodavca po radniku i periodu) — nema
            // jedinstven indeks u bazi, dedup po (RadnikId, Godina, Mesec).
            var existingDpKeys = (await _destDb.DoprinosiPoslodavca
                .Select(d => new { d.RadnikId, d.Godina, d.Mesec })
                .ToListAsync())
                .ToHashSet();

            var srcDoprinosiPoslodavca = await srcDb.DoprinosiPoslodavca.AsNoTracking().ToListAsync();
            foreach (var sdp in srcDoprinosiPoslodavca)
            {
                if (!srcRadniciDict.TryGetValue(sdp.RadnikId, out var rSrc)) continue;
                if (!destRadniciByPeriodAndBroj.TryGetValue((rSrc.BrojRadnika, sdp.Godina, sdp.Mesec), out int rDestId)) continue;

                var kljucDp = new { RadnikId = rDestId, sdp.Godina, sdp.Mesec };
                if (existingDpKeys.Contains(kljucDp)) continue;

                _destDb.DoprinosiPoslodavca.Add(new DoprinosiPoslodavca
                {
                    RadnikId = rDestId,
                    Godina = sdp.Godina,
                    Mesec = sdp.Mesec,
                    Zar1 = sdp.Zar1, Zar2 = sdp.Zar2, Zar3 = sdp.Zar3, Zar4 = sdp.Zar4, Zar5 = sdp.Zar5,
                    Zar6 = sdp.Zar6, Zar7 = sdp.Zar7, Zar8 = sdp.Zar8, Zar9 = sdp.Zar9,
                    Bol1 = sdp.Bol1, Bol2 = sdp.Bol2, Bol3 = sdp.Bol3, Bol4 = sdp.Bol4, Bol5 = sdp.Bol5,
                    Bol6 = sdp.Bol6, Bol7 = sdp.Bol7, Bol8 = sdp.Bol8, Bol9 = sdp.Bol9,
                    Nak1 = sdp.Nak1, Nak2 = sdp.Nak2, Nak3 = sdp.Nak3, Nak4 = sdp.Nak4, Nak5 = sdp.Nak5,
                    Nak6 = sdp.Nak6, Nak7 = sdp.Nak7, Nak8 = sdp.Nak8, Nak9 = sdp.Nak9,
                    Nep1 = sdp.Nep1, Nep2 = sdp.Nep2, Nep3 = sdp.Nep3, Nep4 = sdp.Nep4, Nep5 = sdp.Nep5,
                    Nep6 = sdp.Nep6, Nep7 = sdp.Nep7, Nep8 = sdp.Nep8, Nep9 = sdp.Nep9,
                    B60F1 = sdp.B60F1, B60F2 = sdp.B60F2, B60F3 = sdp.B60F3, B60F4 = sdp.B60F4, B60F5 = sdp.B60F5,
                    B60F6 = sdp.B60F6, B60F7 = sdp.B60F7, B60F8 = sdp.B60F8, B60F9 = sdp.B60F9,
                    B601 = sdp.B601, B602 = sdp.B602, B603 = sdp.B603, B604 = sdp.B604, B605 = sdp.B605,
                    B606 = sdp.B606, B607 = sdp.B607, B608 = sdp.B608, B609 = sdp.B609,
                    Inv1 = sdp.Inv1, Inv2 = sdp.Inv2, Inv3 = sdp.Inv3, Inv4 = sdp.Inv4, Inv5 = sdp.Inv5,
                    Inv6 = sdp.Inv6, Inv7 = sdp.Inv7, Inv8 = sdp.Inv8, Inv9 = sdp.Inv9,
                    Por1 = sdp.Por1, Por2 = sdp.Por2, Por3 = sdp.Por3, Por4 = sdp.Por4, Por5 = sdp.Por5,
                    Por6 = sdp.Por6, Por7 = sdp.Por7, Por8 = sdp.Por8, Por9 = sdp.Por9
                });
                existingDpKeys.Add(kljucDp);
                result.UvezenoDoprinosaPoslodavca++;
            }

            // 8. PppPdPrijave i Bolovanja
            // Dedup: oba imaju jedinstvene indekse u bazi (PppPdPrijava: Godina+Mesec+RedniBroj;
            // Bolovanje: BrojRadnika+Godina+Mesec+DatumOd) — bez preskakanja, ponovni uvoz baca
            // UNIQUE constraint grešku.
            var existingPrijaveKeys = (await _destDb.PppPdPrijave
                .Select(p => new { p.Godina, p.Mesec, p.RedniBroj })
                .ToListAsync())
                .ToHashSet();

            var srcPrijave = await srcDb.PppPdPrijave.AsNoTracking().ToListAsync();
            foreach (var sp in srcPrijave)
            {
                if (existingPrijaveKeys.Contains(new { sp.Godina, sp.Mesec, sp.RedniBroj })) continue;

                _destDb.PppPdPrijave.Add(new PppPdPrijava
                {
                    Godina = sp.Godina,
                    Mesec = sp.Mesec,
                    RedniBroj = sp.RedniBroj,
                    VrstaPrijave = sp.VrstaPrijave,
                    KlijentskaOznaka = sp.KlijentskaOznaka,
                    DatumPlacanja = sp.DatumPlacanja,
                    VrstaIzmene = (VrstaIzmenePrijave)(int)sp.VrstaIzmene,
                    JipdKojiSeMenja = sp.JipdKojiSeMenja,
                    BrojResenja = sp.BrojResenja,
                    OsnovIzmene = (OsnovIzmenePrijave)(int)sp.OsnovIzmene,
                    BrojZaposlenih = sp.BrojZaposlenih,
                    ZbirPoreza = sp.ZbirPoreza,
                    ZbirDoprinosa = sp.ZbirDoprinosa,
                    Jipd = sp.Jipd,
                    Bop = sp.Bop,
                    IznosZaUplatu = sp.IznosZaUplatu,
                    RacunZaUplatu = sp.RacunZaUplatu,
                    ModelPozivaNaBroj = sp.ModelPozivaNaBroj,
                    SvrhaUplate = sp.SvrhaUplate,
                    Status = (StatusPrijave)(int)sp.Status,
                    DatumPodnosenja = sp.DatumPodnosenja,
                    DatumStatusa = sp.DatumStatusa,
                    Napomena = sp.Napomena,
                    PutanjaFajla = sp.PutanjaFajla,
                    DatumKreiranja = sp.DatumKreiranja
                });
                existingPrijaveKeys.Add(new { sp.Godina, sp.Mesec, sp.RedniBroj });
                result.UvezenoPppPdPrijava++;
            }

            var existingBolovanjaKeys = (await _destDb.Bolovanja
                .Select(b => new { b.BrojRadnika, b.Godina, b.Mesec, b.DatumOd })
                .ToListAsync())
                .ToHashSet();

            var srcBolovanja = await srcDb.Bolovanja.AsNoTracking().ToListAsync();
            foreach (var sb in srcBolovanja)
            {
                if (existingBolovanjaKeys.Contains(new { sb.BrojRadnika, sb.Godina, sb.Mesec, sb.DatumOd })) continue;

                _destDb.Bolovanja.Add(new Bolovanje
                {
                    BrojRadnika = sb.BrojRadnika,
                    Godina = sb.Godina,
                    Mesec = sb.Mesec,
                    DatumPocetkaSprecenosti = sb.DatumPocetkaSprecenosti,
                    DatumOd = sb.DatumOd,
                    DatumDo = sb.DatumDo,
                    Osnov = (OsnovSprecenosti)(int)sb.Osnov,
                    PrvaIsplata = sb.PrvaIsplata,
                    BrojDoznake = sb.BrojDoznake,
                    Napomena = sb.Napomena,
                    DatumUnosa = sb.DatumUnosa
                });
                existingBolovanjaKeys.Add(new { sb.BrojRadnika, sb.Godina, sb.Mesec, sb.DatumOd });
                result.UvezenoBolovanja++;
            }

            await _destDb.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.Uspesno = false;
            result.Greska = ex.InnerException?.Message ?? ex.Message;
        }

        return result;
    }

    private async Task ImportSifarniciAsync(PlataDbContext srcDb, ZaradeImportResult result)
    {
        // Firma
        var srcFirma = await srcDb.Firme.AsNoTracking().FirstOrDefaultAsync();
        if (srcFirma != null)
        {
            var destFirma = await _destDb.Firme.FirstOrDefaultAsync();
            if (destFirma == null)
            {
                destFirma = new Firma
                {
                    Sifra = !string.IsNullOrWhiteSpace(srcFirma.Pib) ? srcFirma.Pib : "PSSS-PIROT",
                    Naziv = srcFirma.Naziv,
                    Adresa = srcFirma.Adresa,
                    PttIMesto = srcFirma.Grad,
                    SifraOpstine = srcFirma.SifraOpstine,
                    Pib = srcFirma.Pib,
                    MaticniBroj = srcFirma.Mb,
                    ZiroRacun = srcFirma.BankovniRacun,
                    SifraDelatnosti = srcFirma.SifraDelatnosti,
                    PosebanRacun = srcFirma.PosebanRacun,
                    PodracunPoslovneJedinice = srcFirma.PodracunPoslovneJedinice,
                    Telefon = srcFirma.Telefon,
                    Email = srcFirma.Email,
                    Zastupnik = srcFirma.Zastupnik,
                    FunkcijaZastupnika = srcFirma.FunkcijaZastupnika,
                    Napomena = srcFirma.Napomena
                };
                _destDb.Firme.Add(destFirma);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(srcFirma.Naziv)) destFirma.Naziv = srcFirma.Naziv;
                if (!string.IsNullOrWhiteSpace(srcFirma.Adresa)) destFirma.Adresa = srcFirma.Adresa;
                if (!string.IsNullOrWhiteSpace(srcFirma.Grad)) destFirma.PttIMesto = srcFirma.Grad;
                if (!string.IsNullOrWhiteSpace(srcFirma.SifraOpstine)) destFirma.SifraOpstine = srcFirma.SifraOpstine;
                if (!string.IsNullOrWhiteSpace(srcFirma.Pib)) destFirma.Pib = srcFirma.Pib;
                if (!string.IsNullOrWhiteSpace(srcFirma.Mb)) destFirma.MaticniBroj = srcFirma.Mb;
                if (!string.IsNullOrWhiteSpace(srcFirma.BankovniRacun)) destFirma.ZiroRacun = srcFirma.BankovniRacun;
                if (!string.IsNullOrWhiteSpace(srcFirma.SifraDelatnosti)) destFirma.SifraDelatnosti = srcFirma.SifraDelatnosti;
                if (!string.IsNullOrWhiteSpace(srcFirma.Zastupnik)) destFirma.Zastupnik = srcFirma.Zastupnik;
                if (!string.IsNullOrWhiteSpace(srcFirma.FunkcijaZastupnika)) destFirma.FunkcijaZastupnika = srcFirma.FunkcijaZastupnika;
            }
        }

        // KontaKnjizenja
        var srcKonta = await srcDb.KontaKnjizenja.AsNoTracking().ToListAsync();
        var destKontaKeys = (await _destDb.KontaKnjizenja.Select(k => k.Kljuc).ToListAsync()).ToHashSet();
        foreach (var sk in srcKonta)
        {
            if (!destKontaKeys.Contains(sk.Kljuc))
            {
                _destDb.KontaKnjizenja.Add(new KontoKnjizenja
                {
                    Kljuc = sk.Kljuc,
                    Naziv = sk.Naziv,
                    Konto = sk.Konto,
                    Strana = (StranaKnjizenja)(int)sk.Strana,
                    Redosled = sk.Redosled,
                    Napomena = sk.Napomena
                });
            }
        }

        // VrstePrimanja
        var srcVp = await srcDb.VrstePrimanja.AsNoTracking().ToListAsync();
        var destVpSifre = (await _destDb.VrstePrimanja.Select(v => v.Sifra).ToListAsync()).ToHashSet();
        foreach (var sv in srcVp)
        {
            if (!destVpSifre.Contains(sv.Sifra))
            {
                _destDb.VrstePrimanja.Add(new VrstaPrimanja
                {
                    Sifra = sv.Sifra,
                    Naziv = sv.Naziv,
                    Svp = sv.Svp,
                    Oporezivo = sv.Oporezivo,
                    UlaziUOsnovicuDoprinosa = sv.UlaziUOsnovicuDoprinosa,
                    NeoporeziviLimit = sv.NeoporeziviLimit,
                    Konto = sv.Konto,
                    NaTeretFonda = sv.NaTeretFonda,
                    VecIsplacenoVanObracuna = sv.VecIsplacenoVanObracuna,
                    Redosled = sv.Redosled,
                    Aktivna = sv.Aktivna,
                    JeSistemska = sv.JeSistemska
                });
            }
        }

        // VrsteUgovora
        var srcVu = await srcDb.VrsteUgovora.AsNoTracking().ToListAsync();
        var destVuSifre = (await _destDb.VrsteUgovora.Select(v => v.Sifra).ToListAsync()).ToHashSet();
        foreach (var sv in srcVu)
        {
            if (!destVuSifre.Contains(sv.Sifra))
            {
                _destDb.VrsteUgovora.Add(new VrstaUgovora
                {
                    Sifra = sv.Sifra,
                    Naziv = sv.Naziv,
                    Ovp = sv.Ovp,
                    NormiraniTroskoviProcenat = sv.NormiraniTroskoviProcenat,
                    StopaPoreza = sv.StopaPoreza,
                    StopaPioPrimalac = sv.StopaPioPrimalac,
                    StopaZdravstvoPrimalac = sv.StopaZdravstvoPrimalac,
                    StopaNezaposlenostPrimalac = sv.StopaNezaposlenostPrimalac,
                    StopaPioIsplatilac = sv.StopaPioIsplatilac,
                    StopaZdravstvoIsplatilac = sv.StopaZdravstvoIsplatilac,
                    StopaNezaposlenostIsplatilac = sv.StopaNezaposlenostIsplatilac,
                    Konto = sv.Konto,
                    SifraPlacanja = sv.SifraPlacanja,
                    Redosled = sv.Redosled,
                    Aktivna = sv.Aktivna,
                    Napomena = sv.Napomena
                });
            }
        }

        // Praznici
        var srcPraznici = await srcDb.Praznici.AsNoTracking().ToListAsync();
        var destPrazniciDatumi = (await _destDb.Praznici.Select(p => p.Datum).ToListAsync()).ToHashSet();
        foreach (var sp in srcPraznici)
        {
            if (!destPrazniciDatumi.Contains(sp.Datum))
            {
                _destDb.Praznici.Add(new Praznik
                {
                    Datum = sp.Datum,
                    Naziv = sp.Naziv,
                    Neradni = sp.Neradni,
                    RucniUnos = sp.RucniUnos
                });
            }
        }

        // Porezi (istorijski sistemski parametri poreza po periodu — dedup po Godina+Mesec+RedniBroj)
        var srcPorezi = await srcDb.Porezi.AsNoTracking().ToListAsync();
        var destPoreziKeys = (await _destDb.Porezi.Select(p => new { p.Godina, p.Mesec, p.RedniBroj }).ToListAsync()).ToHashSet();
        foreach (var sp in srcPorezi)
        {
            var kljuc = new { sp.Godina, sp.Mesec, sp.RedniBroj };
            if (destPoreziKeys.Contains(kljuc)) continue;

            _destDb.Porezi.Add(new Porezi
            {
                Godina = sp.Godina,
                Mesec = sp.Mesec,
                RedniBroj = sp.RedniBroj,
                Zarada = sp.Zarada,
                AkPorez = sp.AkPorez,
                AkPorez2 = sp.AkPorez2,
                AkPorez3 = sp.AkPorez3,
                AkPorez4 = sp.AkPorez4,
                Prvast = sp.Prvast,
                Drugast = sp.Drugast,
                Trecast = sp.Trecast,
                LinPorez3 = sp.LinPorez3,
                SifPlac1 = sp.SifPlac1,
                ZiroR1 = sp.ZiroR1,
                PozivNa1 = sp.PozivNa1,
                PozivNa3 = sp.PozivNa3,
                Svrha1 = sp.Svrha1,
                Svrha2 = sp.Svrha2,
                Primalac1 = sp.Primalac1,
                Primalac2 = sp.Primalac2,
                SifPlac2 = sp.SifPlac2,
                ZiroR2 = sp.ZiroR2,
                PozivNa2 = sp.PozivNa2,
                PozivNa4 = sp.PozivNa4,
                PosPorez = sp.PosPorez,
                Svrha3 = sp.Svrha3,
                Svrha4 = sp.Svrha4,
                Primalac3 = sp.Primalac3,
                Primalac4 = sp.Primalac4,
                ProcDrzav = sp.ProcDrzav,
                ProcNocni = sp.ProcNocni,
                ProcPreko = sp.ProcPreko,
                ProcMinul = sp.ProcMinul,
                ProcNedel = sp.ProcNedel,
                ProcBolov = sp.ProcBolov,
                ProcPlac = sp.ProcPlac,
                ProcPlZa = sp.ProcPlZa,
                ProcInval = sp.ProcInval,
                FondCasova = sp.FondCasova,
                CasZaOb = sp.CasZaOb,
                VrBoda = sp.VrBoda,
                ProcIzdrz = sp.ProcIzdrz,
                Akont = sp.Akont,
                ProsBrut = sp.ProsBrut,
                TopliObrokCena = sp.TopliObrokCena
            });
            destPoreziKeys.Add(kljuc);
            result.UvezenoPoreza++;
        }

        // Doprinosi (istorijske sistemske stope doprinosa po periodu — dedup po Godina+Mesec+RedniBroj)
        var srcDoprinosi = await srcDb.Doprinosi.AsNoTracking().ToListAsync();
        var destDoprinosiKeys = (await _destDb.Doprinosi.Select(d => new { d.Godina, d.Mesec, d.RedniBroj }).ToListAsync()).ToHashSet();
        foreach (var sd in srcDoprinosi)
        {
            var kljuc = new { sd.Godina, sd.Mesec, sd.RedniBroj };
            if (destDoprinosiKeys.Contains(kljuc)) continue;

            _destDb.Doprinosi.Add(new Doprinos
            {
                Godina = sd.Godina,
                Mesec = sd.Mesec,
                RedniBroj = sd.RedniBroj,
                Naziv = sd.Naziv,
                ProcRadn = sd.ProcRadn,
                ProcPosl = sd.ProcPosl,
                B60ProcR = sd.B60ProcR,
                B60ProcP = sd.B60ProcP,
                Bp60ProcP = sd.Bp60ProcP,
                Bp60FProcP = sd.Bp60FProcP,
                PorProcP = sd.PorProcP,
                NepProcP = sd.NepProcP,
                InvProcP = sd.InvProcP,
                Svrha1 = sd.Svrha1,
                Svrha2 = sd.Svrha2,
                Primalac1 = sd.Primalac1,
                Primalac2 = sd.Primalac2,
                ZiroRacun = sd.ZiroRacun,
                ZiroRacP = sd.ZiroRacP,
                PozivNaB = sd.PozivNaB,
                PozivNa2 = sd.PozivNa2,
                SifPlac = sd.SifPlac,
                SifPlacP = sd.SifPlacP,
                NajnizaOsnovica = sd.NajnizaOsnovica,
                NajvisaOsnovica = sd.NajvisaOsnovica
            });
            destDoprinosiKeys.Add(kljuc);
            result.UvezenoDoprinosa++;
        }

        // Banke (šifarnik banaka po periodu — dedup po Godina+Mesec+Sifra)
        var srcBanke = await srcDb.Banke.AsNoTracking().ToListAsync();
        var destBankeKeys = (await _destDb.Banke.Select(b => new { b.Godina, b.Mesec, b.Sifra }).ToListAsync()).ToHashSet();
        foreach (var sb in srcBanke)
        {
            var kljuc = new { sb.Godina, sb.Mesec, sb.Sifra };
            if (destBankeKeys.Contains(kljuc)) continue;

            _destDb.Banke.Add(new Banka
            {
                Godina = sb.Godina,
                Mesec = sb.Mesec,
                Sifra = sb.Sifra,
                Naziv = sb.Naziv,
                ZiroRacun = sb.ZiroRacun
            });
            destBankeKeys.Add(kljuc);
            result.UvezenoBanaka++;
        }

        // PlatniRazredi (jedan aktivan set stopa — samo ako dest još nema nijedan zapis)
        if (!await _destDb.PlatniRazredi.AnyAsync())
        {
            var srcRazred = await srcDb.PlatniRazredi.AsNoTracking().FirstOrDefaultAsync();
            if (srcRazred != null)
            {
                _destDb.PlatniRazredi.Add(new PlatniRazred
                {
                    R1 = srcRazred.R1, R2 = srcRazred.R2, R3 = srcRazred.R3, R4 = srcRazred.R4, R5 = srcRazred.R5,
                    R6 = srcRazred.R6, R7 = srcRazred.R7, R8 = srcRazred.R8, R9 = srcRazred.R9,
                    P1 = srcRazred.P1, P2 = srcRazred.P2, P3 = srcRazred.P3, P4 = srcRazred.P4, P5 = srcRazred.P5,
                    P6 = srcRazred.P6, P7 = srcRazred.P7, P8 = srcRazred.P8, P9 = srcRazred.P9
                });
                result.UvezenoPlatnihRazreda++;
            }
        }

        // PoreskeOlaksice (dedup po Sifra)
        var srcOlaksice = await srcDb.PoreskeOlaksice.AsNoTracking().ToListAsync();
        var destOlaksiceSifre = (await _destDb.PoreskeOlaksice.Select(o => o.Sifra).ToListAsync()).ToHashSet();
        foreach (var so in srcOlaksice)
        {
            if (destOlaksiceSifre.Contains(so.Sifra)) continue;

            _destDb.PoreskeOlaksice.Add(new PoreskaOlaksica
            {
                Sifra = so.Sifra,
                Naziv = so.Naziv,
                PravniOsnov = so.PravniOsnov,
                Mehanizam = (MehanizamOlaksice)(int)so.Mehanizam,
                ProcenatPoreza = so.ProcenatPoreza,
                ProcenatDoprinosa = so.ProcenatDoprinosa,
                VaziOd = so.VaziOd,
                VaziDo = so.VaziDo,
                Aktivna = so.Aktivna,
                Napomena = so.Napomena
            });
            destOlaksiceSifre.Add(so.Sifra);
            result.UvezenoPoreskihOlaksica++;
        }

        // SabloniUgovora (dedup po Sifra)
        var srcSabloni = await srcDb.SabloniUgovora.AsNoTracking().ToListAsync();
        var destSabloniSifre = (await _destDb.SabloniUgovora.Select(s => s.Sifra).ToListAsync()).ToHashSet();
        foreach (var ss in srcSabloni)
        {
            if (destSabloniSifre.Contains(ss.Sifra)) continue;

            int? destVrstaUgovoraId = null;
            if (ss.VrstaUgovoraId.HasValue)
            {
                var vuSrc = await srcDb.VrsteUgovora.AsNoTracking().FirstOrDefaultAsync(v => v.VrstaUgovoraId == ss.VrstaUgovoraId.Value);
                if (vuSrc != null && (await _destDb.VrsteUgovora.FirstOrDefaultAsync(v => v.Sifra == vuSrc.Sifra)) is { } vuDest)
                {
                    destVrstaUgovoraId = vuDest.VrstaUgovoraId;
                }
            }

            _destDb.SabloniUgovora.Add(new SablonUgovora
            {
                Sifra = ss.Sifra,
                Naziv = ss.Naziv,
                VrstaUgovoraId = destVrstaUgovoraId,
                Tekst = ss.Tekst,
                Redosled = ss.Redosled,
                Aktivan = ss.Aktivan,
                JeSistemski = ss.JeSistemski,
                Napomena = ss.Napomena
            });
            destSabloniSifre.Add(ss.Sifra);
            result.UvezenoSablonaUgovora++;
        }

        await _destDb.SaveChangesAsync();
    }
}
