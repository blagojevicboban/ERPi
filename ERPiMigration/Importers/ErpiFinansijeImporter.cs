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
            var existingKonta = (await _destDb.Konta.ToListAsync())
                .GroupBy(k => k.BrojKonta.Trim())
                .ToDictionary(g => g.Key, g => g.First());

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

            // 7. Materijali (M_SIFR.DBF šifarnik) — nezavisna šifarnička serija od Artikli (Robno),
            // koristi se kao FK sa Materijalno knjigovodstvenim dokumentima (Ulaz/Trebovanje/Primopredaja) ispod.
            var srcMaterijali = await srcDb.Materijali.AsNoTracking().ToListAsync();
            var materijaliDict = (await _destDb.Materijali.Where(m => m.SifraArtikla != null && m.SifraArtikla != "").ToListAsync())
                .GroupBy(m => m.SifraArtikla)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var sm in srcMaterijali)
            {
                if (!materijaliDict.ContainsKey(sm.SifraArtikla))
                {
                    var nm = new ERPiData.Models.Magacin.Materijal
                    {
                        SifraArtikla = sm.SifraArtikla,
                        Naziv = sm.Naziv,
                        JedinicaMere = sm.JedinicaMere,
                        Pakovanje = sm.Pakovanje
                    };
                    _destDb.Materijali.Add(nm);
                    materijaliDict[sm.SifraArtikla] = nm;
                    result.UvezenoMaterijala++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 8. Poreske tarife (TARIFE.DBF šifarnik)
            var srcTarife = await srcDb.PoreskeTarife.AsNoTracking().ToListAsync();
            var tarifeDict = (await _destDb.PoreskeTarife.ToListAsync())
                .GroupBy(t => t.TarifniBroj)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var st2 in srcTarife)
            {
                if (!tarifeDict.ContainsKey(st2.TarifniBroj))
                {
                    var nt = new ERPiData.Models.Magacin.PoreskaTarifa
                    {
                        TarifniBroj = st2.TarifniBroj,
                        PorezProcenat = st2.PorezProcenat,
                        PosebanPorezProcenat = st2.PosebanPorezProcenat,
                        PorezUCeni = st2.PorezUCeni
                    };
                    _destDb.PoreskeTarife.Add(nt);
                    tarifeDict[st2.TarifniBroj] = nt;
                    result.UvezenoPoreskihTarifa++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 9. Materijalne/Robne kartice (MAT_KART.DBF, M_KART.DBF) — istorijski, samo dopisni zapisi
            var srcKartice = await srcDb.MaterijalneKartice.AsNoTracking().ToListAsync();
            var existingKarticeKeys = (await _destDb.MaterijalneKartice
                    .Select(k => new { k.SifraMagacina, k.SifraArtikla, k.RedniBroj })
                    .ToListAsync())
                .Select(k => (k.SifraMagacina, k.SifraArtikla, k.RedniBroj))
                .ToHashSet();

            foreach (var sk2 in srcKartice)
            {
                var kljuc = (sk2.SifraMagacina, sk2.SifraArtikla, sk2.RedniBroj);
                if (existingKarticeKeys.Add(kljuc))
                {
                    _destDb.MaterijalneKartice.Add(new ERPiData.Models.Magacin.MaterijalnaKartica
                    {
                        SifraMagacina = sk2.SifraMagacina,
                        SifraArtikla = sk2.SifraArtikla,
                        RedniBroj = sk2.RedniBroj,
                        DatumPromene = sk2.DatumPromene,
                        OpisPromene = sk2.OpisPromene,
                        Ulaz = sk2.Ulaz,
                        Izlaz = sk2.Izlaz,
                        Stanje = sk2.Stanje,
                        Cena = sk2.Cena,
                        CenaIzlaz = sk2.CenaIzlaz,
                        Duguje = sk2.Duguje,
                        Potrazuje = sk2.Potrazuje,
                        Saldo = sk2.Saldo
                    });
                    result.UvezenoMaterijalnihKartica++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 10. Ulazi materijala (ULAZ.DBF) — Materijalno knjigovodstvo, FK na Materijal (ne Artikal)
            var srcUlazi = await srcDb.UlazNalozi.Include(u => u.Stavke).AsNoTracking().ToListAsync();
            var existingUlaziBrojevi = (await _destDb.UlazNalozi.Select(u => u.BrojNaloga).ToListAsync()).ToHashSet();

            foreach (var su in srcUlazi)
            {
                if (existingUlaziBrojevi.Contains(su.BrojNaloga)) continue;
                if (!magaciniDict.TryGetValue(su.SifraMagacina, out var mag)) continue;

                var nu = new ERPiData.Models.Magacin.UlazNalog
                {
                    BrojNaloga = su.BrojNaloga,
                    Datum = su.Datum,
                    MagacinId = mag.MagacinId,
                    BrojRacuna = su.BrojRacuna,
                    DatumRacuna = su.DatumRacuna,
                    IsKnjizen = su.IsKnjizen
                };

                foreach (var st3 in su.Stavke)
                {
                    if (!materijaliDict.TryGetValue(st3.SifraArtikla, out var mat)) continue;
                    nu.Stavke.Add(new ERPiData.Models.Magacin.UlazStavka
                    {
                        RedniBroj = st3.RedniBroj,
                        MaterijalId = mat.MaterijalId,
                        Kolicina = st3.Kolicina,
                        Cena = st3.Cena,
                        Iznos = st3.Iznos
                    });
                    result.UvezenoStavkiUlaza++;
                }

                if (nu.Stavke.Count > 0)
                {
                    _destDb.UlazNalozi.Add(nu);
                    existingUlaziBrojevi.Add(su.BrojNaloga);
                    result.UvezenoUlaza++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 11. Trebovanja materijala (TREBOV.DBF) — Materijalno knjigovodstvo
            var srcTrebovanja = await srcDb.TrebovanjeNalozi.Include(t => t.Stavke).AsNoTracking().ToListAsync();
            var existingTrebovanjaBrojevi = (await _destDb.TrebovanjeNalozi.Select(t => t.BrojNaloga).ToListAsync()).ToHashSet();

            foreach (var st4 in srcTrebovanja)
            {
                if (existingTrebovanjaBrojevi.Contains(st4.BrojNaloga)) continue;
                if (!magaciniDict.TryGetValue(st4.SifraMagacina, out var mag)) continue;

                var nt2 = new ERPiData.Models.Magacin.TrebovanjeNalog
                {
                    BrojNaloga = st4.BrojNaloga,
                    Datum = st4.Datum,
                    MagacinId = mag.MagacinId,
                    IsKnjizen = st4.IsKnjizen
                };

                foreach (var stv in st4.Stavke)
                {
                    if (!materijaliDict.TryGetValue(stv.SifraArtikla, out var mat)) continue;
                    nt2.Stavke.Add(new ERPiData.Models.Magacin.TrebovanjeStavka
                    {
                        RedniBroj = stv.RedniBroj,
                        MaterijalId = mat.MaterijalId,
                        Kolicina = stv.Kolicina,
                        Cena = stv.Cena,
                        Iznos = stv.Iznos,
                        KontoTroska = stv.KontoTroska
                    });
                    result.UvezenoStavkiTrebovanja++;
                }

                if (nt2.Stavke.Count > 0)
                {
                    _destDb.TrebovanjeNalozi.Add(nt2);
                    existingTrebovanjaBrojevi.Add(st4.BrojNaloga);
                    result.UvezenoTrebovanja++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 12. Primopredaje / Zaduženja / Razduženja (MAT_NAL.DBF, ZADUZ.DBF, RAZDUZ.DBF) — Materijalno knjigovodstvo
            var srcPrimopredaje = await srcDb.PrimopredajaNalozi.Include(p => p.Stavke).AsNoTracking().ToListAsync();
            var existingPrimopredajaKeys = (await _destDb.PrimopredajaNalozi
                    .Select(p => new { p.VrstaDokumenta, p.BrojNaloga })
                    .ToListAsync())
                .Select(p => (p.VrstaDokumenta, p.BrojNaloga))
                .ToHashSet();

            foreach (var sp in srcPrimopredaje)
            {
                var kljuc = (sp.VrstaDokumenta, sp.BrojNaloga);
                if (existingPrimopredajaKeys.Contains(kljuc)) continue;
                if (!magaciniDict.TryGetValue(sp.SifraMagacinaDaje, out var magDaje)) continue;
                if (!magaciniDict.TryGetValue(sp.SifraMagacinaPrima, out var magPrima)) continue;

                var np = new ERPiData.Models.Magacin.PrimopredajaNalog
                {
                    BrojNaloga = sp.BrojNaloga,
                    Datum = sp.Datum,
                    MagacinIdDaje = magDaje.MagacinId,
                    MagacinIdPrima = magPrima.MagacinId,
                    VrstaDokumenta = sp.VrstaDokumenta,
                    IsKnjizen = sp.IsKnjizen
                };

                foreach (var stv in sp.Stavke)
                {
                    if (!materijaliDict.TryGetValue(stv.SifraArtikla, out var mat)) continue;
                    np.Stavke.Add(new ERPiData.Models.Magacin.PrimopredajaStavka
                    {
                        RedniBroj = stv.RedniBroj,
                        MaterijalId = mat.MaterijalId,
                        Kolicina = stv.Kolicina,
                        Cena = stv.Cena,
                        Iznos = stv.Iznos
                    });
                    result.UvezenoStavkiPrimopredaja++;
                }

                if (np.Stavke.Count > 0)
                {
                    _destDb.PrimopredajaNalozi.Add(np);
                    existingPrimopredajaKeys.Add(kljuc);
                    result.UvezenoPrimopredaja++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 13. Maloprodajne kalkulacije (MALKULAC.DBF & MAL_NAL.DBF) — Robno, FK na Artikal
            var srcMalKalkulacije = await srcDb.MaloprodajneKalkulacije.Include(m => m.Stavke).AsNoTracking().ToListAsync();
            var existingMalKalkKeys = (await _destDb.MaloprodajneKalkulacije
                    .Select(m => new { m.BrojKalkulacije, m.MagacinIdPrima })
                    .ToListAsync())
                .Select(m => (m.BrojKalkulacije, m.MagacinIdPrima))
                .ToHashSet();

            foreach (var sm2 in srcMalKalkulacije)
            {
                if (!magaciniDict.TryGetValue(sm2.SifraMagacinaPrima ?? "", out var magPrima)) continue;

                var kljuc = (sm2.BrojKalkulacije, magPrima.MagacinId);
                if (existingMalKalkKeys.Contains(kljuc)) continue;

                magaciniDict.TryGetValue(sm2.SifraMagacinaDaje ?? "", out var magDaje);
                partneriBySifra.TryGetValue(sm2.SifraDobavljaca ?? "", out var dob2);

                var nmk = new ERPiData.Models.Magacin.MaloprodajnaKalkulacija
                {
                    BrojKalkulacije = sm2.BrojKalkulacije,
                    Datum = sm2.Datum,
                    MagacinIdPrima = magPrima.MagacinId,
                    MagacinIdDaje = magDaje?.MagacinId,
                    DobavljacId = dob2?.PartnerId,
                    BrojOtpremnice = sm2.BrojOtpremnice,
                    DatumOtpremnice = sm2.DatumOtpremnice,
                    BrojRacuna = sm2.BrojRacuna,
                    DatumRacuna = sm2.DatumRacuna,
                    TransportniTroskovi = sm2.TransportniTroskovi,
                    TroskoviUskladistenja = sm2.TroskoviUskladistenja,
                    UtovarIstovar = sm2.UtovarIstovar,
                    TransportnoOsiguranje = sm2.TransportnoOsiguranje,
                    OstaliTroskovi = sm2.OstaliTroskovi,
                    IsKnjizen = sm2.IsKnjizen,
                    IsTrgovinskiKnjizen = sm2.IsTrgovinskiKnjizen,
                    SvegaTroskovi = sm2.SvegaTroskovi,
                    RabatPri = sm2.RabatPri,
                    NabavnaVrednost = sm2.NabavnaVrednost,
                    SvegaNabavno = sm2.SvegaNabavno,
                    Razlika = sm2.Razlika,
                    MarzaProcenat = sm2.MarzaProcenat,
                    Porez = sm2.Porez,
                    PoreskaStopaProcenat = sm2.PoreskaStopaProcenat,
                    ProdajnaVrednost = sm2.ProdajnaVrednost,
                    RabatIznos = sm2.RabatIznos
                };

                foreach (var stv in sm2.Stavke)
                {
                    if (!artikliDict.TryGetValue(stv.SifraArtikla, out var art)) continue;
                    nmk.Stavke.Add(new ERPiData.Models.Magacin.MaloprodajnaKalkulacijaStavka
                    {
                        RedniBroj = stv.RedniBroj,
                        ArtikalId = art.ArtikalId,
                        Kolicina = stv.Kolicina,
                        NabavnaCena = stv.NabavnaCena,
                        Iznos = stv.Iznos,
                        Troskovi = stv.Troskovi,
                        NabavnaVrednost = stv.NabavnaVrednost,
                        RazlikaProcenat = stv.RazlikaProcenat,
                        RazlikaIznos = stv.RazlikaIznos,
                        ProdajnaVrednostBezPoreza = stv.ProdajnaVrednostBezPoreza,
                        PorezProcenat = stv.PorezProcenat,
                        PorezIznos = stv.PorezIznos,
                        PosebanPorezProcenat = stv.PosebanPorezProcenat,
                        PosebanPorezIznos = stv.PosebanPorezIznos,
                        PrenetiPorez = stv.PrenetiPorez,
                        PrenetiPosebanPorez = stv.PrenetiPosebanPorez,
                        PorezZaUplatu = stv.PorezZaUplatu,
                        Taksa = stv.Taksa,
                        ProdajnaVrednost = stv.ProdajnaVrednost,
                        ProdajnaCena = stv.ProdajnaCena,
                        TarifniBroj = stv.TarifniBroj,
                        BrojRazduzenja = stv.BrojRazduzenja,
                        IsKnjizen = stv.IsKnjizen,
                        IsTrgovinskiKnjizen = stv.IsTrgovinskiKnjizen
                    });
                    result.UvezenoStavkiMaloprodajnihKalkulacija++;
                }

                if (nmk.Stavke.Count > 0)
                {
                    _destDb.MaloprodajneKalkulacije.Add(nmk);
                    existingMalKalkKeys.Add(kljuc);
                    result.UvezenoMaloprodajnihKalkulacija++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 14. Računi-Otpremnice (RAC_OTP.DBF & RAC_POD.DBF) — Robno, FK na Artikal.
            // Izvor (temp firmDb) već čuva prava MagacinId/ArtikalId polja koja pokazuju na SVOJE
            // (temp) tabele Magacina/Artikala, ne na destDb — zato se prevode preko Sifra kolona iz
            // temp Magacini/Artikli šifarnika, isto kao u koraku 15 (Nivelacije cena) ispod.
            var srcMagaciniByIdTemp = await srcDb.Magacini.AsNoTracking().ToDictionaryAsync(m => m.MagacinId, m => m.SifraMagacina);
            var srcArtikliByIdTemp = await srcDb.Artikli.AsNoTracking().ToDictionaryAsync(a => a.ArtikalId, a => a.SifraArtikla);

            var srcRacuni = await srcDb.RacuniOtpremnice.Include(r => r.Stavke).AsNoTracking().ToListAsync();
            var existingRacuniBrojevi = (await _destDb.RacuniOtpremnice.Select(r => r.BrojRacuna).ToListAsync()).ToHashSet();

            foreach (var sr in srcRacuni)
            {
                if (existingRacuniBrojevi.Contains(sr.BrojRacuna)) continue;

                int? magId = null;
                if (sr.MagacinId.HasValue && srcMagaciniByIdTemp.TryGetValue(sr.MagacinId.Value, out var magSifra) && magaciniDict.TryGetValue(magSifra, out var mag2))
                {
                    magId = mag2.MagacinId;
                }

                int? kontoKupcaId = null;
                if (!string.IsNullOrWhiteSpace(sr.KontoKupca) && existingKonta.TryGetValue(sr.KontoKupca.Trim(), out var kontoKupca))
                {
                    kontoKupcaId = kontoKupca.KontoId;
                }

                var nr = new ERPiData.Models.Magacin.RacunOtpremnica
                {
                    TipDokumenta = (ERPiData.Models.Magacin.TipRacunOtpremnice)(int)sr.TipDokumenta,
                    RokVazenjaPredracuna = sr.RokVazenjaPredracuna,
                    BrojRacuna = sr.BrojRacuna,
                    DatumRacuna = sr.DatumRacuna,
                    RokPlacanja = sr.RokPlacanja,
                    MagacinId = magId,
                    Napomena = sr.Napomena,
                    UkupnoOsnovica = sr.UkupnoOsnovica,
                    UkupnoRabat = sr.UkupnoRabat,
                    UkupnoPdv = sr.UkupnoPdv,
                    UkupnoZaUplatu = sr.UkupnoZaUplatu,
                    IsKnjizen = sr.IsKnjizen,
                    BrojOtpremnice = sr.BrojOtpremnice,
                    KontoKupcaId = kontoKupcaId,
                    RokPlacanjaDana = sr.RokPlacanjaDana
                };

                foreach (var stv in sr.Stavke)
                {
                    int? aId = null;
                    if (stv.ArtikalId.HasValue && srcArtikliByIdTemp.TryGetValue(stv.ArtikalId.Value, out var artSifra) && artikliDict.TryGetValue(artSifra, out var art2))
                    {
                        aId = art2.ArtikalId;
                    }
                    if (aId == null) continue;

                    nr.Stavke.Add(new ERPiData.Models.Magacin.RacunOtpremnicaStavka
                    {
                        RedniBroj = stv.RedniBroj,
                        ArtikalId = aId,
                        Kolicina = stv.Kolicina,
                        ProdajnaCena = stv.ProdajnaCena,
                        RabatProcenat = stv.RabatProcenat,
                        StopaPdv = stv.StopaPdv,
                        Osnovica = stv.Osnovica,
                        IznosPdv = stv.IznosPdv,
                        Ukupno = stv.Ukupno
                    });
                    result.UvezenoStavkiRacunaOtpremnica++;
                }

                if (nr.Stavke.Count > 0)
                {
                    _destDb.RacuniOtpremnice.Add(nr);
                    existingRacuniBrojevi.Add(sr.BrojRacuna);
                    result.UvezenoRacunaOtpremnica++;
                }
            }
            await _destDb.SaveChangesAsync();

            // 15. Nivelacije cena (NIV_NAL.DBF & P_M_NIV.DBF) — Robno, FK na Artikal, ista prevodna šema kao korak 14.
            var srcNivelacije = await srcDb.NivelacijeCena.Include(n => n.Stavke).AsNoTracking().ToListAsync();
            var existingNivelacijeBrojevi = (await _destDb.NivelacijeCena.Select(n => n.BrojNivelacije).ToListAsync()).ToHashSet();

            foreach (var sn2 in srcNivelacije)
            {
                if (existingNivelacijeBrojevi.Contains(sn2.BrojNivelacije)) continue;

                int? magId = null;
                if (sn2.MagacinId.HasValue && srcMagaciniByIdTemp.TryGetValue(sn2.MagacinId.Value, out var magSifra2) && magaciniDict.TryGetValue(magSifra2, out var mag3))
                {
                    magId = mag3.MagacinId;
                }

                var nn2 = new ERPiData.Models.Magacin.NivelacijaCena
                {
                    BrojNivelacije = sn2.BrojNivelacije,
                    DatumNivelacije = sn2.DatumNivelacije,
                    MagacinId = magId,
                    Opis = sn2.Opis,
                    IsKnjizen = sn2.IsKnjizen
                };

                decimal ukupnaRazlika = 0m;
                foreach (var stv in sn2.Stavke)
                {
                    int? aId = null;
                    if (stv.ArtikalId.HasValue && srcArtikliByIdTemp.TryGetValue(stv.ArtikalId.Value, out var artSifra2) && artikliDict.TryGetValue(artSifra2, out var art3))
                    {
                        aId = art3.ArtikalId;
                    }
                    if (aId == null) continue;

                    nn2.Stavke.Add(new ERPiData.Models.Magacin.NivelacijaStavka
                    {
                        RedniBroj = stv.RedniBroj,
                        ArtikalId = aId,
                        KolicinaZaliha = stv.KolicinaZaliha,
                        StaraCena = stv.StaraCena,
                        NovaCena = stv.NovaCena,
                        RazlikaPoJedinici = stv.RazlikaPoJedinici,
                        UkupnaRazlika = stv.UkupnaRazlika
                    });
                    ukupnaRazlika += stv.UkupnaRazlika;
                    result.UvezenoStavkiNivelacija++;
                }

                nn2.UkupnoRazlika = ukupnaRazlika;

                if (nn2.Stavke.Count > 0)
                {
                    _destDb.NivelacijeCena.Add(nn2);
                    existingNivelacijeBrojevi.Add(sn2.BrojNivelacije);
                    result.UvezenoNivelacija++;
                }
            }
            await _destDb.SaveChangesAsync();

            result.Success = true;
            bool imaNovihZapisa = result.UvezenoPartnera > 0 || result.UvezenoNaloga > 0 || result.UvezenoArtikala > 0 || result.UvezenoMagacina > 0
                || result.UvezenoMaterijala > 0 || result.UvezenoKalkulacija > 0 || result.UvezenoMaloprodajnihKalkulacija > 0
                || result.UvezenoRacunaOtpremnica > 0 || result.UvezenoNivelacija > 0 || result.UvezenoUlaza > 0
                || result.UvezenoTrebovanja > 0 || result.UvezenoPrimopredaja > 0 || result.UvezenoMaterijalnihKartica > 0
                || result.UvezenoPoreskihTarifa > 0;

            if (!imaNovihZapisa)
            {
                result.Message = $"Podaci iz baze su već ranije uvezeni u vašu bazu (baza sadrži sve naloge, konta, artikle i partnere: {srcPartneri.Count} partnera, {srcNalozi.Count} naloga). Nema novih zapisa za uvoz.";
            }
            else
            {
                result.Message = $"Uspešno uvezeno iz ERPiFinansije:\n• Partnera: {result.UvezenoPartnera}\n• Konta: {result.UvezenoKonta}\n• Magacina: {result.UvezenoMagacina}\n• Artikala: {result.UvezenoArtikala}\n• Materijala: {result.UvezenoMaterijala}\n• Naloga u GK: {result.UvezenoNaloga}\n• Kalkulacija: {result.UvezenoKalkulacija}\n• MP kalkulacija: {result.UvezenoMaloprodajnihKalkulacija}\n• Računa-otpremnica: {result.UvezenoRacunaOtpremnica}\n• Nivelacija cena: {result.UvezenoNivelacija}\n• Ulaza materijala: {result.UvezenoUlaza}\n• Trebovanja: {result.UvezenoTrebovanja}\n• Primopredaja: {result.UvezenoPrimopredaja}\n• Kartica: {result.UvezenoMaterijalnihKartica}\n• Poreskih tarifa: {result.UvezenoPoreskihTarifa}";
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
