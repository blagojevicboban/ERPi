using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiData;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiFinansijeData;
using Microsoft.EntityFrameworkCore;

namespace ERPiMigration.Importers;

public class ErpiFinansijeImporter
{
    private readonly ErpiDbContext _destDb;

    public ErpiFinansijeImporter(ErpiDbContext destDb)
    {
        _destDb = destDb;
    }

    public async Task<ImportResult> ImportFromDatabaseAsync(AccountingDbContext srcDb)
    {
        var result = new ImportResult();

        try
        {
            // 1. Konta
            var srcKonta = await srcDb.Konta.AsNoTracking().ToListAsync();
            var existingKonta = await _destDb.Konta.ToDictionaryAsync(k => k.BrojKonta);

            foreach (var sk in srcKonta)
            {
                if (!existingKonta.ContainsKey(sk.BrojKonta))
                {
                    var nk = new ERPiData.Models.Core.Konto
                    {
                        BrojKonta = sk.BrojKonta,
                        NazivKonta = sk.NazivKonta,
                        IsSintetika = sk.IsSintetika,
                        VrstaKonta = sk.VrstaKonta
                    };
                    _destDb.Konta.Add(nk);
                    existingKonta[sk.BrojKonta] = nk;
                    result.UvezenoKonta++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 2. Partneri
            var srcPartneri = await srcDb.Partneri.AsNoTracking().ToListAsync();
            var partneriBySifra = await _destDb.Partneri.ToDictionaryAsync(p => p.SifraPartnera);
            var partneriByIdSrcMap = new Dictionary<int, ERPiData.Models.Core.Partner>();
            var existingPartneriByPib = (await _destDb.Partneri.Where(p => p.Pib != null && p.Pib != "").ToListAsync())
                .GroupBy(p => p.Pib!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var sp in srcPartneri)
            {
                ERPiData.Models.Core.Partner? targetPartner = null;

                if (partneriBySifra.TryGetValue(sp.SifraPartnera, out var p1))
                {
                    targetPartner = p1;
                }
                else if (!string.IsNullOrWhiteSpace(sp.Pib) && existingPartneriByPib.TryGetValue(sp.Pib, out var p2))
                {
                    targetPartner = p2;
                }

                if (targetPartner == null)
                {
                    targetPartner = new ERPiData.Models.Core.Partner
                    {
                        SifraPartnera = sp.SifraPartnera,
                        Naziv = sp.Naziv,
                        Pib = sp.Pib,
                        MaticniBroj = sp.MaticniBroj,
                        Adresa = sp.Adresa,
                        PttIMesto = sp.PttIMesto,
                        Telefon = sp.Telefon,
                        ZiroRacun = sp.ZiroRacun,
                        JeDobavljac = true,
                        JeKupac = true,
                        IsActive = true
                    };
                    _destDb.Partneri.Add(targetPartner);
                    result.UvezenoPartnera++;
                }

                partneriBySifra[sp.SifraPartnera] = targetPartner;
                partneriByIdSrcMap[sp.PartnerId] = targetPartner;
                if (!string.IsNullOrWhiteSpace(sp.Pib))
                {
                    existingPartneriByPib[sp.Pib] = targetPartner;
                }
            }
            await _destDb.SaveChangesAsync();

            // 3. Magacini
            var srcMagacini = await srcDb.Magacini.AsNoTracking().ToListAsync();
            var magaciniDict = await _destDb.Magacini.ToDictionaryAsync(m => m.SifraMagacina);

            foreach (var sm in srcMagacini)
            {
                if (!magaciniDict.ContainsKey(sm.SifraMagacina))
                {
                    var nm = new ERPiData.Models.Magacin.Magacin
                    {
                        SifraMagacina = sm.SifraMagacina,
                        NazivMagacina = sm.NazivMagacina,
                        VrstaMagacina = sm.VrstaMagacina,
                        OdgovornoLice = sm.OdgovornoLice
                    };
                    _destDb.Magacini.Add(nm);
                    magaciniDict[sm.SifraMagacina] = nm;
                    result.UvezenoMagacina++;
                }
            }
            await _destDb.SaveChangesAsync();
            magaciniDict = await _destDb.Magacini.ToDictionaryAsync(m => m.SifraMagacina);

            // 4. Artikli
            var srcArtikli = await srcDb.Artikli.AsNoTracking().ToListAsync();
            var artikliDict = await _destDb.Artikli.ToDictionaryAsync(a => a.SifraArtikla);

            foreach (var sa in srcArtikli)
            {
                if (!artikliDict.ContainsKey(sa.SifraArtikla))
                {
                    var na = new ERPiData.Models.Magacin.Artikal
                    {
                        SifraArtikla = sa.SifraArtikla,
                        Naziv = sa.Naziv,
                        JedinicaMere = sa.JedinicaMere,
                        Barkod = sa.Barkod,
                        NabavnaCena = sa.NabavnaCena,
                        ProdajnaCena = sa.ProdajnaCena,
                        PdvStopa = 20m
                    };
                    _destDb.Artikli.Add(na);
                    artikliDict[sa.SifraArtikla] = na;
                    result.UvezenoArtikala++;
                }
            }
            await _destDb.SaveChangesAsync();
            artikliDict = await _destDb.Artikli.ToDictionaryAsync(a => a.SifraArtikla);

            // 5. Nalozi i Stavke
            var srcNalozi = await srcDb.Nalozi.Include(n => n.Stavke).AsNoTracking().ToListAsync();
            var existingNaloziBrojevi = (await _destDb.Nalozi.Select(n => n.BrojNaloga).ToListAsync()).ToHashSet();

            foreach (var sn in srcNalozi)
            {
                if (existingNaloziBrojevi.Contains(sn.BrojNaloga)) continue;

                var nn = new ERPiData.Models.Finansije.Nalog
                {
                    BrojNaloga = sn.BrojNaloga,
                    DatumNaloga = sn.DatumNaloga,
                    VrstaNaloga = sn.VrstaNaloga,
                    Opis = sn.Opis,
                    UkupnoDuguje = sn.UkupnoDuguje,
                    UkupnoPotrazuje = sn.UkupnoPotrazuje,
                    Status = sn.IsKnjizen ? StatusNaloga.Proknjizen : StatusNaloga.Nacrt,
                    DatumKnjizenja = sn.DatumKnjiženja
                };

                foreach (var st in sn.Stavke)
                {
                    existingKonta.TryGetValue(st.BrojKonta, out var konto);
                    ERPiData.Models.Core.Partner? partner = null;
                    if (st.PartnerId.HasValue)
                    {
                        partneriByIdSrcMap.TryGetValue(st.PartnerId.Value, out partner);
                    }

                    if (konto != null)
                    {
                        nn.Stavke.Add(new ERPiData.Models.Finansije.StavkaNaloga
                        {
                            RedniBroj = st.RedniBroj,
                            KontoId = konto.KontoId,
                            PartnerId = partner?.PartnerId,
                            BrojDokumenta = st.BrojDokumenta,
                            Opis = st.Opis,
                            Duguje = st.Duguje,
                            Potrazuje = st.Potrazuje
                        });
                        result.UvezenoStavkiNaloga++;
                    }
                }

                _destDb.Nalozi.Add(nn);
                result.UvezenoNaloga++;
            }
            await _destDb.SaveChangesAsync();

            // 6. Kalkulacije
            var srcKalkulacije = await srcDb.Kalkulacije.Include(k => k.Stavke).AsNoTracking().ToListAsync();
            var existingKalkBrojevi = (await _destDb.Kalkulacije.Select(k => k.BrojKalkulacije).ToListAsync()).ToHashSet();

            foreach (var sk in srcKalkulacije)
            {
                if (existingKalkBrojevi.Contains(sk.BrojKalkulacije)) continue;

                magaciniDict.TryGetValue(sk.SifraMagacina ?? "", out var mag);
                partneriBySifra.TryGetValue(sk.SifraDobavljaca ?? "", out var dob);

                if (mag != null)
                {
                    var nk = new ERPiData.Models.Magacin.Kalkulacija
                    {
                        MagacinId = mag.MagacinId,
                        PartnerId = dob?.PartnerId,
                        BrojKalkulacije = sk.BrojKalkulacije,
                        BrojFaktureDobavljaca = sk.BrojRacuna ?? sk.BrojOtpremnice,
                        Datum = sk.Datum,
                        DatumFakture = sk.DatumRacuna ?? sk.DatumOtpremnice,
                        VrstaKalkulacije = "Ulazna",
                        UkupnoNabavna = sk.NabavnaVrednost,
                        UkupnoProdajna = sk.ProdajnaVrednost,
                        UkupnoPdv = sk.Porez
                    };

                    foreach (var st in sk.Stavke)
                    {
                        artikliDict.TryGetValue(st.SifraArtikla, out var art);
                        if (art != null)
                        {
                            nk.Stavke.Add(new ERPiData.Models.Magacin.StavkaKalkulacije
                            {
                                ArtikalId = art.ArtikalId,
                                Kolicina = st.Kolicina,
                                NabavnaCena = st.NabavnaCena,
                                MarzaProcenat = st.RazlikaProcenat,
                                PdvStopa = st.PorezProcenat,
                                ProdajnaCena = st.ProdajnaCena,
                                IznosNabavni = st.NabavnaVrednost,
                                IznosPdv = st.PorezIznos,
                                IznosProdajni = st.ProdajnaVrednost
                            });
                            result.UvezenoStavkiKalkulacije++;
                        }
                    }

                    _destDb.Kalkulacije.Add(nk);
                    result.UvezenoKalkulacija++;
                }
            }
            await _destDb.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Uspešno uvezeno iz ERPiFinansije: {result.UvezenoPartnera} partnera, {result.UvezenoKonta} konta, {result.UvezenoNaloga} naloga, {result.UvezenoKalkulacija} kalkulacija.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Greška pri uvozu: {ex.Message}";
        }

        return result;
    }
}
