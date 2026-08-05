# 🧭 Plan nastavka razvoja — ERPi

> Radni dokument za nastavak posla u novoj sesiji. Prati fazni roadmap iz
> [`ANALIZA_I_PLAN.md`](ANALIZA_I_PLAN.md) i beleži šta je urađeno, šta je namerno odloženo
> i koje odluke ne treba poništavati bez razloga.
>
> Stanje na dan **05.08.2026**, verzija **2.0.0-alpha**.

---

## 1. Gde smo

| Faza | Stavka | Status |
| :--- | :--- | :--- |
| **1** | Core šema (`ERPiData`: Firma, Korisnik, Partner, Konto, MestoTroska) + početna migracija | ✅ |
| **2** | WPF Shell (`MainWindow`), `LoginWindow`, `CompanySelectWindow` — izbor/kreiranje firme, jedna baza po firmi | ✅ |
| **3.1** | Finansije — Glavna knjiga: `Nalog`/`StavkaNaloga`, `NaloziView`, `NalogEditWindow` (MVP) | ✅ |
| **3.2** | Partneri — CRUD (`PartneriView`/`PartnerEditWindow`) + otvorene stavke po kontu (MVP) | ✅ |
| **3.2b** | `ZatvaranjeStavke` — ručno parovanje Duguje/Potražuje stavki, delimična zatvaranja, `ZatvoriStavkeWindow` | ✅ |
| **3.2c** | IOS izveštaj (svi partneri odjednom), kamate (zatezna) | ✅ |
| **3.3** | Magacin (kalkulacije, šifarnici magacina/artikala, kalkulacija editor) i PDV evidencija (`PdvZapis`) | ✅ |
| **3.4** | SEF e-Fakture (UBL 2.1 API) i e-Fiskalizacija (`PfrRacun`) | ✅ |
| **4** | Osnovna sredstva | ⬜ |
| **5** | Obračun zarada — jedini modul sa realnim produkcionim korisnicima danas | ⬜ |
| **6** | Automatsko knjiženje (Zarade/Sredstva → Nalog) | ⬜ (šema već ima kuku: `Nalog.IzvorModula`/`IzvorId`) |
| **7** | `ERPiMigration` — DOS import (Finansije/Sredstva/Zarade) + direktan `ErpiZaradeProdukcijaImporter` | ⬜ |
| **8** | Velopack pakovanje i CI/CD | ⬜ |

---

## 2. Odluke koje ne treba poništavati

- **`Partner` je namerno "mršav"** — samo zajednički identitet (naziv, PIB/MB/JMBG, kontakt,
  računi) + bool-ovi `JeDobavljac/JeKupac/JeRadnik/JeBanka/JePoreskaUprava`. Operativni podaci
  specifični za modul (npr. koeficijenti radnika) ostaju u modulskoj tabeli, vezanoj `PartnerId`
  stranim ključem — ne prepisivati ih nazad u `Partner`, to je tačno ono što je plan zvao
  "god table" rizikom.
- **`StavkaNaloga.KontoId` je pravi strani ključ**, ne string `BrojKonta` kao u ERPiFinansije —
  to je i poenta objedinjene baze. Isto važi za `PartnerId`/`MestoTroskaId`.
- **`Nalog` nosi `IzvorModula`/`IzvorId`** (nullable) — priprema za Fazu 6 (automatsko
  knjiženje iz Zarada/Sredstava), da se automatski nalog može prepoznati i ne duplira.
- **`NalogEditWindow` menja stavke brisanjem starih + upisom novih**, ne diff-om po
  `StavkaNalogaId`. Dovoljno za MVP obim; ako se pokaže sporo na velikim nalozima, prelazi se
  na pravi diff.
- **ERPiZarade je jedini modul sa produkcionim podacima.** Uvoz iz njega u Fazu 7 ide direktno
  EF Core → EF Core (`ErpiZaradeProdukcijaImporter`), NE preko DOS/DBF puta — vidi
  `ANALIZA_I_PLAN.md` odeljak 4. Finansije/Sredstva nemaju produkcione podatke, njima je DOS
  import dovoljan.
- **`RadioButton.IsChecked="True"` se NIKAD ne piše kao XAML literal** ako njegov `Checked`
  handler dira sibling elemente deklarisane niže u istom XAML stablu — puca
  `NullReferenceException` usred `InitializeComponent()` (uhvaćeno u `NaloziView`, isti obrazac
  bug kao u ERPiFinansije). Postavi `IsChecked = true` u code-behind, posle
  `InitializeComponent()`.

---

## 3. Poznati nedostaci u Fazi 3.1 (MVP, namerno odloženo)

- Nema UI za devizno knjiženje (`Valuta`/`KursValute`/`DevizniDuguje`/`DevizniPotrazuje` postoje
  u modelu, ne u `NalogEditWindow` gridu).
- Nema UI za PDV `Osnovica`/`StopaPdv` (polja postoje u modelu za kasniju PDV podfazu — 3.3).
- `ColPartner`/`ColMestoTroska` u editoru nemaju način da se izbor vrati na prazno posle prvog
  izbora bez zatvaranja dijaloga — sitna UX mana, ne blokira unos.
- Nema F2 pretrage konta ni šifarnika opisa (ERPiFinansije ih ima) — obična padajuća lista.
- Nema Konta/MestaTroska CRUD ekrana još (Partneri sad ima, Faza 3.2) — unose se direktno u
  bazu ili preko budućih šifarničkih ekrana.

## 3a. Poznati nedostaci u Fazi 3.2/3.2b (MVP, namerno odloženo)

- **Nema IOS izveštaja za sve partnere odjednom** (samo po jednom, izabranom u listi) — puni
  `GetIosIzvestajAsync` iz ERPiFinansije namerno nije prenet, nosi mnogo legacy-DBF logike
  ("sintetički partneri" izvedeni iz konta kad `PartnerId` nije popunjen) koja u ERPi šemi
  sa pravim `KontoId`/`PartnerId` FK-ovima od početka nije potrebna. Ide u 3.2c.
- Nema kamata (zatezna kamata na kašnjenje) — `KamatnaStopa` (ERPiFinansije) nije preneta.
  Ide u 3.2c.
- **`ZatvoriGrupnoAsync`** (M:N grupno parovanje — jedna uplata zatvara više faktura odjednom)
  nije prenet, samo 1:1 `ZatvoriAsync`. ERPiFinansijeData ga ima gotovog ako zatreba (FIFO
  alokacija preko liste (StavkaId, Iznos) parova).
- **`ZatvoriStavkeWindow` nije proveren end-to-end sa stvarnim uparivanjem** (zahteva prethodno
  Konta + proknjižene Naloge sa PartnerId na stavkama, dug UI setup za jednu driver sesiju) —
  provereno je samo da se ekran otvara bez pada i da guard ("izaberite partnera") radi. Logika
  `ZatvoriAsync` je 1:1 prenesena iz ERPiFinansije (koja je u produkciji), ali sam čin
  uparivanja kroz `ZatvoriStavkeWindow` UI još nije vizuelno potvrđen. Prvi sledeći rad na
  Partnerima neka to proveri pre nego što se osloni na ovaj ekran.

---

## 3b. Poznati nedostaci u Fazi 3.2c (MVP, namerno odloženo)

- **Nema PDF export** ni za IOS izveštaj ni za obračun kamate (`GenerisiZbirniIOSPdf`/
  `GenerisiKamataPdf` iz ERPiFinansije nisu preneti) — ERPi uopšte još nema `PdfReportService`
  ni bilo koji PDF izveštaj; ovo je opštiji nedostatak celog projekta, ne samo ovog ekrana.
  Ide zajedno sa prvim pravim izveštajem kome PDF stvarno zatreba.
- **IOS filter po kontu je samo jedan prefiks** (`kontoPrefix`), ne pravi opseg
  `odKonta`-`doKonta` sa poređenjem stringova kao u ERPiFinansije
  `OtvoreneStavkeService.GetIosIzvestajAsync` — dovoljno za "pokaži samo konto 204" ili "435",
  nedovoljno za "od 200000 do 209999" opseg. Doći po potrebi.
- **`ObracunajKamatuZaKontoAsync`/`ProknjiziKamatuNalogZaKontoAsync` (kamata za "sintetički"
  konto bez partnera) nije prenet** — namerno, jer ERPi šema nema legacy DBF razlog da
  `StavkaNaloga.PartnerId` izostane (vidi napomenu u `KamataService`/`ZatvaranjeStavkiService`).
  Ako se ikad pojavi proknjižena stavka bez `PartnerId`-ja na kontu kupca, kamata se za nju danas
  ne može obračunati — trebalo bi prvo popraviti unos (dodeliti partnera), ne dodavati sintetičku
  granu nazad.
- **`KamataService.ProknjiziKamatuNalogAsync` zahteva postojeću dugovnu stavku partnera na kontu
  204/120** da bi znao koji `KontoId` da upotrebi (nema "podrazumevani konto 204000" fallback kao
  ERPiFinansije, jer bi to zahtevalo string→FK nagađanje) — kamata na partnera bez ijedne
  proknjižene stavke na kontu kupca ne može da se proknjiži dok se prvi dug ne unese.
  Isto tako, `662000` (Prihodi od zateznih kamata) mora već postojati u kontnom planu firme —
  ne kreira se automatski.
- **Nema F1 help prozora** (`EditHelpWindow` iz ERPiFinansije) na `KamataWindow`/
  `IosIzvestajWindow` — ERPi generalno još nema uspostavljen help-prozor obrazac ni na jednom
  ekranu, ne samo ovde.
- **Kamata/IOS ekrani nisu vizuelno voženi end-to-end kroz UI** (isti razlog kao napomena za
  `ZatvoriStavkeWindow` u §3a — zahteva prethodno proknjižene naloge sa partnerom i konto 204/
  662000 u kontnom planu, dug UI setup za jednu driver sesiju). Build i EF migracija su
  provereni čisti; sam čin obračuna/knjiženja kamate i IOS grupisanja kroz UI još nije vizuelno
  potvrđen — prvi sledeći rad na Partnerima/Finansijama neka to proveri.

## 4. Testiranje

- **`run-erpi-app`** (`ERPiApp/.claude/skills` i `.agents/skills`, mora ostati sinhronizovano
  u oba) — UI Automation driver, `--autologin` kroz fiksnu `AUTOTEST` firmu
  (`%LocalAppData%\ERPi\Baze\AUTOTEST.db`). Screenshot ide preko UIA `BoundingRectangle`, ne
  golog `GetWindowRect` — na skaliranom ekranu (125%/150%) ovaj drugi tiho seče desnu/donju
  ivicu prozora bez greške; koštalo je vremena da se otkrije, ne vraćati taj "pojednostavljeni"
  pristup.
- **`ERPiData.Tests`** (xUnit) — automatizovani unit i integracioni testovi po uzoru na `ERPiFinansijeData.Tests` (EF Core In-Memory baza) za provere proračuna kalkulacija, uravnoteženosti naloga, zatvaranja stavki i modela.

---

## Sledeći koraci

Faza 3.2c (IOS izveštaj za sve partnere, kamate) zaokružuje Partnere, ili se može preskočiti na
Fazu 3.3 (Magacin/PDV) ili napred na Fazu 5 (Zarade, jedini modul sa stvarnim korisnicima danas)
— odluka je na korisniku, ne pretpostavljati.
