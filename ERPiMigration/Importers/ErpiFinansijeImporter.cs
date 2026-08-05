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
            var existingKonta = await _destDb.Konta.ToDictionaryAsync(k => k.BrojKonta.Trim());

            foreach (var sk in srcKonta)
            {
                string brojKontaClean = sk.BrojKonta.Trim();
                if (!existingKonta.ContainsKey(brojKontaClean))
                {
                    var nk = new ERPiData.Models.Core.Konto
                    {
                        BrojKonta = brojKontaClean,
                        NazivKonta = sk.NazivKonta,
                        IsSintetika = sk.IsSintetika,
                        VrstaKonta = sk.VrstaKonta
                    };
                    _destDb.Konta.Add(nk);
                    existingKonta[brojKontaClean] = nk;
                    result.UvezenoKonta++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 2. Partneri
            var srcPartneri = await srcDb.Partneri.AsNoTracking().ToListAsync();
            var partneriBySifra = (await _destDb.Partneri.Where(p => p.SifraPartnera != null && p.SifraPartnera != "").ToListAsync())
                .GroupBy(p => p.SifraPartnera)
                .ToDictionary(g => g.Key, g => g.First());

            var partneriByIdSrcMap = new Dictionary<int, ERPiData.Models.Core.Partner>();
            var existingPartneriByPib = (await _destDb.Partneri.Where(p => p.Pib != null && p.Pib != "").ToListAsync())
                .GroupBy(p => p.Pib!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var sp in srcPartneri)
            {
                ERPiData.Models.Core.Partner? targetPartner = null;

                if (!string.IsNullOrWhiteSpace(sp.SifraPartnera) && partneriBySifra.TryGetValue(sp.SifraPartnera, out var p1))
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

                if (!string.IsNullOrWhiteSpace(sp.SifraPartnera))
                {
                    partneriBySifra[sp.SifraPartnera] = targetPartner;
                }
                partneriByIdSrcMap[sp.PartnerId] = targetPartner;
                if (!string.IsNullOrWhiteSpace(sp.Pib))
                {
                    existingPartneriByPib[sp.Pib] = targetPartner;
                }
            }
            await _destDb.SaveChangesAsync();

            // 3. Magacini
            var srcMagacini = await srcDb.Magacini.AsNoTracking().ToListAsync();
            var magaciniDict = (await _destDb.Magacini.Where(m => m.SifraMagacina != null && m.SifraMagacina != "").ToListAsync())
                .GroupBy(m => m.SifraMagacina)
                .ToDictionary(g => g.Key, g => g.First());

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
            magaciniDict = (await _destDb.Magacini.Where(m => m.SifraMagacina != null && m.SifraMagacina != "").ToListAsync())
                .GroupBy(m => m.SifraMagacina)
                .ToDictionary(g => g.Key, g => g.First());

            // 4. Artikli
            var srcArtikli = await srcDb.Artikli.AsNoTracking().ToListAsync();
            var artikliDict = (await _destDb.Artikli.Where(a => a.SifraArtikla != null && a.SifraArtikla != "").ToListAsync())
                .GroupBy(a => a.SifraArtikla)
                .ToDictionary(g => g.Key, g => g.First());

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
            artikliDict = (await _destDb.Artikli.Where(a => a.SifraArtikla != null && a.SifraArtikla != "").ToListAsync())
                .GroupBy(a => a.SifraArtikla)
                .ToDictionary(g => g.Key, g => g.First());

            // 5. Nalozi i Stavke (Kompozitni ključ VrstaNaloga + BrojNaloga za sprecavanje preskakanja razlicitih vrsta naloga sa istim brojem)
            var srcNalozi = await srcDb.Nalozi.Include(n => n.Stavke).AsNoTracking().ToListAsync();
            var existingNaloziKeys = (await _destDb.Nalozi.Select(n => $"{n.VrstaNaloga}_{n.BrojNaloga}").ToListAsync()).ToHashSet();

            foreach (var sn in srcNalozi)
            {
                string nalogKey = $"{sn.VrstaNaloga}_{sn.BrojNaloga}";
                if (existingNaloziKeys.Contains(nalogKey)) continue;

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
                    existingKonta.TryGetValue(st.BrojKonta.Trim(), out var konto);
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
                existingNaloziKeys.Add(nalogKey);
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
            if (result.UvezenoPartnera == 0 && result.UvezenoNaloga == 0 && result.UvezenoArtikala == 0 && result.UvezenoMagacina == 0)
            {
                result.Message = $"Podaci iz baze su već ranije uvezeni u vašu bazu (baza sadrži sve naloge, konta, artikle i partnere: {srcPartneri.Count} partnera, {srcNalozi.Count} naloga). Nema novih zapisa za uvoz.";
            }
            else
            {
                result.Message = $"Uspešno uvezeno iz ERPiFinansije:\n• Partnera: {result.UvezenoPartnera}\n• Konta: {result.UvezenoKonta}\n• Magacina: {result.UvezenoMagacina}\n• Artikala: {result.UvezenoArtikala}\n• Naloga u GK: {result.UvezenoNaloga}\n• Kalkulacija: {result.UvezenoKalkulacija}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Greška pri uvozu: {ex.Message}";
        }

        return result;
    }
}
