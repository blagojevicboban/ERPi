# 🧭 Plan nastavka razvoja — ERPi

> Radni dokument za nastavak posla u novoj sesiji. Prati fazni roadmap iz
> [`ANALIZA_I_PLAN.md`](ANALIZA_I_PLAN.md) i beleži šta je urađeno, šta je namerno odloženo
> i koje odluke ne treba poništavati bez razloga.
>
> Stanje na dan **05.08.2026** (dopunjeno u istoj sesiji sa Fazom 4), verzija **2.0.0-alpha**.

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
| **3.3a** | Magacin Osnovno (VP Kalkulacije, Šifarnik magacina/artikala, PDV zapisi) | ✅ |
| **3.3b / 3.12** | Robno & Materijalno poslovanje (Materijalne/Robne kartice, Ulazi, Trebovanja, Primopredaje, Računi-Otpremnice, Nivelacije, MP kalkulacije, Uvozne kalkulacije, KEP knjiga, DMS, SEF UBL 2.1, Poreski Bilans PB-1, Nova Godina prenos) | ✅ Sve komponente i servisi uspešno preneseni, reizgrađeni i pokriveni xUnit testovima |
| **3.4** | SEF e-Fakture (UBL 2.1 API) i e-Fiskalizacija (`PfrRacun`) | ✅ |
| **3.5** | Šifarnici Konta & Mesta troška (`KontaView`/`KontoEditWindow`/`MestaTroskaView`/`MestoTroskaEditWindow`) | ✅ |
| **3.6** | Izveštaji Glavne knjige & Bilansi (`BrutoBilansView`, `KarticaKontaView`, `BilansStanjaView`, `BilansUspehaView`, `AprProsireniIzvestajiService`) | ✅ |
| **3.7** | Izvodi banke & Auto-knjiženje (`UvozIzvodaWindow`, `BankIzvodService`, Parsers/MatchingEngine) | ✅ |
| **3.8** | Blagajničko poslovanje (`BlagajnaView`, `BlagajnickiNalogEditWindow`, `BlagajnaService`) | ✅ |
| **3.9** | Devizno knjigovodstvo & Kursne liste (`DeviznoValviranjeWindow`, `DeviznoKnjigovodstvoService`, `KursnaListaService`, `NbsApiClient`) | ✅ |
| **3.10**| Putni nalozi (`PutniNaloziView`, `PutniNalogModels`, `PutniNalogService`) | ✅ |
| **3.11**| Kompenzacije (`KompenzacijeView`, `KompenzacijaModels`, `KompenzacijaService`, Pametno skeniranje) | ✅ |
| **3.12**| Komercijala, Trgovina & DMS (`RacuniOtpremnice`, `Nivelacije`, `Maloprodaja`, `UvoznaKalkulacija`, `PdvEvidencija`, `PpPdvXmlGenerator`) | ✅ Sve komponente prenesene, ožičene u sidebar/tabove i pokrivene xUnit testovima |
| **4** | Osnovna sredstva | 🔶 (jezgro preneto, vidi §3h — Popis/Revalorizacija/Izveštaji hub odloženi) |
| **5** | Obračun zarada — jedini modul sa realnim produkcionim korisnicima danas | 🔶 (u toku, vidi §3e) |
| **6** | Automatsko knjiženje (Zarade/Sredstva → Nalog) | ⬜ (šema već ima kuku: `Nalog.IzvorModula`/`IzvorId`) |
| **7.1** | `ERPiMigration` — direktan `ErpiFinansijeImporter` (uvoz iz `baza.db` / `AccountingDbContext` u `ErpiDbContext`) + `UvozWizardView` | ✅ |
| 7.2a | DOS import Zarade — `ZaradeDbfMigrator` (DBF → privremena ERPiZaradeData baza → `ErpiZaradeProdukcijaImporter`) + `PodesavanjaZaradeView` | ✅ (vidi §3f) |
| 7.2b | DOS import Finansije/Sredstva | ⬜ |
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
- **Loader koji na kraju radi `DataGrid.SelectedIndex = 0` (auto-izbor prvog reda) se NIKAD
  ne zove direktno iz konstruktora** — mora ići kroz `Loaded += (_, _) => LoadXxx();`. Razlog
  je specifičan za ERPi (ne postoji u ERPiFinansije): ERPi ekrani dele već otvoren
  `ErpiDbContext _db`, pa upit ume da se završi *sinhrono*, i ceo lanac
  učitavanje→SelectedIndex→SelectionChanged odradi se još unutar konstruktora, pre nego što je
  kontrola u vizuelnom stablu — `NullReferenceException` u WPF `DataGrid`-u. Uhvaćeno u
  `KarticaKontaView` (Faza 3.6); `KompenzacijeView`/`PutniNaloziView` su to već radile ispravno.
  Pun opis i primer u `import-from-source-apps` skill fajlu.
- **Pre nego što se otvori novoportovan ekran, proveri da li `ERPiApp/App.xaml` ima svaki
  `StaticResource` koji ekran koristi** — App.xaml nije prenet 1:1 iz izvornih app-ova, pa
  ekrani mogu graditi čisto (`dotnet build` ne hvata ovo) i puknuti tek pri otvaranju
  (`XamlParseException: Cannot find resource named '...'`). Nađeno i ispravljeno za
  `SearchTextBoxStyle`/`PrimaryButton` u Fazi 3.5–3.12 (i prateći `Helpers/SearchInputHelper.cs`
  koji `SearchTextBoxStyle` zahteva). Detalji u `import-from-source-apps` skill fajlu.

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

## 3d. Poznati nedostaci u Fazi 3.5–3.12 (primećeno u sesiji od 05.08.2026, ne blokira)

- **`KontaView` još prikazuje legacy DBF kolone** (Stari konto, Ulica, Mesto, Žiro račun,
  Telefon) preko cele širine grida — `import-from-source-apps` skill izričito kaže da se ova
  polja izostave osim ako novoj šemi zaista trebaju (§ "Trim, don't transplant whole"). Nisu
  uklonjena, samo primećena — sledeći rad na Kontnom planu neka proveri da li se ijedno od
  njih zapravo koristi pre nego što ih ukloni ili svesno ostavi.
- **`KontaView`'s toolbar dugmad ("+ Novi konto", "✏ Izmeni", "🗑 Obriši") su ikona+tekst**, što
  je suprotno standardnom obrascu ovog projekta (toolbar akcije su ikona-samo + `ToolTip`, vidi
  `IconButtonStyle` u `import-from-source-apps` skill fajlu). Primećeno vizuelno, nije
  ispravljeno — proveriti i ostale novoportovane ekrane (Bilansi, Blagajna, Devizno, Izvodi,
  Kompenzacije, MestaTroska, PutniNalozi) za isto odstupanje pre sledećeg rada na njima.
- **Kartice konta, Bruto bilans, Bilans stanja/uspeha, Izvodi banke, Blagajna, Devizno,
  Putni nalozi, Kompenzacije nisu vizuelno provereni end-to-end kroz UI** sa stvarnim
  proknjiženim podacima — isti razlog kao napomena za `ZatvoriStavkeWindow`/`KamataWindow` u
  §3a/§3b. Build je čist i `KarticaKontaView`-ov crash od otvaranja je ispravljen (vidi §2),
  ali niko od ovih ekrana nije proveren dugme-po-dugme.
- **Uvoz je testiran samo za osnovne entitete** (Konta, Partneri, Magacini, Artikli, Nalozi/
  Stavke, Kalkulacije) — `ErpiFinansijeImporter` ne uvozi još Izvode/Blagajnu/Devizno/Putne
  naloge/Kompenzacije/Robno-materijalno iz stare baze. Prava produkciona baza
  (`firma_TESTNEW_ARHIBEL_NEW.db`) je uvezena u AUTOTEST i brojevi potvrđeni upitom nad bazom
  (3207 konta, 4 partnera, 340 naloga, 5609 stavki, 134 magacina, 1369 artikala, 0 kalkulacija —
  sve se poklapa sa izvorom), ali samo za taj osnovni skup.

---

## 3e. Faza 5 (Obračun zarada) — stanje 05.08.2026, u toku, NIJE commit-ovano

Kompletan port `ERPiZarade`/`ERPiZaradeApp` u `ERPiApp/Views/Zarade` i
`ERPiApp/Services/Zarade` je već urađen (svi Zarade meniji su ožičeni u `MainWindow`
sidebar — Obračunski periodi, Radnici, Radni sati, Primanja, Porezi, Doprinosi, Platni
razredi, Isplate, Obračun plate, Listići, Bolovanja, PPP-PD, Nalozi, Knjiženje, Ugovori,
Krediti, Banke, Praznici, Vrste primanja/ugovora, Štampe/izveštaji, PPP-PO) — ovo je
zatečeno kao već-uveliko-urađen, ali necommit-ovan i nedovršen rad iz ranije sesije, ne
nešto što je ova sesija pokrenula od nule.

Ova sesija je (`fix_ultimate.ps1`, sada dovršen i idempotentan):
- Dopunila listu servisnih imena za namespace fix (nedostajali `DbfHelper`,
  `PdfZastitaService`, `PrevodStavkiService`, `PutniNaloziImportService`).
- Dodala fix za relativno-kvalifikovane reference ostale iz izvornog namespace-a
  (`Views.Pomoc.X` / `Views.Stampe.X`, koje su se razrešavale na nepostojeći
  `ERPiApp.Views.Pomoc`/`.Stampe` umesto na `ERPiApp.Views.Zarade.Pomoc`/`.Stampe`).
- **Prekopirala ceo `Views/Zarade/Pomoc` folder** (`ContextHelpFix`, `EditHelpWindow`,
  `PomocPage`, `PomocTema`) iz `ERPiZaradeApp/Views/Pomoc` — ranija migracija ga uopšte
  nije prenela, iako ga je više ekrana (Krediti/Obracun/RadniSati F1 help) već
  referenciralo; namespace prilagođen na `ERPiApp.Views.Zarade.Pomoc`.
- Ručno ispravila `ListiciPage.UcitajSmtpPodesavanja()` (stari `UserSettings.Instance`
  placeholder ostavljao `null`-referencu koja se nije kompajlirala) na hardkodovane
  podrazumevane SMTP vrednosti — pravo rešenje (čitanje iz konfiguracije) čeka dok ERPiApp
  ne dobije neki oblik per-firma podešavanja.
- Usput otkrila i ispravila mojibake (dupli UTF-8→cp1252→UTF-8) u dva komentara koje je
  `fix_ultimate.ps1` sâm ubacio pri prvom pokretanju — uzrok: `.ps1` fajl bez UTF-8 BOM-a,
  pa ga Windows PowerShell 5.1 čita kao ANSI; BOM je sada dodat fajlu da se ne ponovi.
- Obrisala zalutali prazan fajl `.FullName` iz korena repoa (artefakt neke ranije
  pogrešno-piped komande).

**Rezultat: `dotnet build ERPi.slnx` je čist — 0 grešaka, 0 upozorenja.**

Šta OSTAJE pre nego što se Faza 5 može zvati gotovom:
- **Ništa od ovoga nije commit-ovano** (celo stablo `Views/Zarade`, `Services/Zarade`,
  `ERPiData/Models/Zarade`, migracija `DodajObrazunZarada`, `ErpiZaradeProdukcijaImporter`
  su i dalje untracked/modified u `git status`) — ne commit-ovati dok korisnik ne kaže.
- **Nijedan Zarade ekran nije vizuelno proveren kroz UI** — isti razlog kao §3d, ovde
  još izraženiji jer je port veliki i `dotnet build` ne hvata XAML runtime greške
  (`StaticResource`, `IsChecked` gotcha, `Loaded+=` gotcha — vidi §2). Korisnik sam
  testira kroz UI (vidi §4) — sledeći rad ovde neka krene od toga.
- **Globalni F1 help (`PomocPage`) nije ožičen u `ERPiApp`-ovom `MainWindow`-u** — u
  izvornom `ERPiZaradeApp`-u se otvara preko globalnog F1 keydown handlera na shell
  nivou (`MainWindow.xaml.cs`), navigacijom `MainFrame.Navigate(new PomocPage(...))`;
  ERPiApp ima samo per-dialog F1 (`EditHelpWindow`) na par ekrana. Nije blokirajuće za
  build, samo nedostajuća funkcionalnost.
- **Desetak jednokratnih `fix_*.ps1` skripti u `ERPiApp/`** (`fix_zarade_*`,
  `fix_master`, `fix_final_comprehensive`, itd.) su istorijski debug alat za dovođenje
  ovog porta do čistog build-a — sad kad `fix_ultimate.ps1` (poslednji u nizu) radi
  posao čisto, ostale su suvišne; brisanje ili arhiviranje pre commit-a je razumno, ali
  nije urađeno ovom sesijom (nije traženo).
- `ErpiZaradeProdukcijaImporter` (Faza 7.2a, direktan uvoz produkcionih Zarade podataka)
  je provezan (`UvozWizardView` i novi `PodesavanjaZaradeView`) i **testiran end-to-end
  protiv prave produkcione baze** (PSSS PIROT) — vidi §3f za detalje i bagove nađene/
  ispravljene pri tom testu.

---

## 3f. Faza 7.2a — Uvoz Zarada (ERPiZarade EF-to-EF + DOS) — stanje 05.08.2026

Dodato u ovoj sesiji, **NIJE commit-ovano**:

- **`PodesavanjaZaradeView`** (`ERPiApp/Views/Zarade/Podesavanja/`) — novi ekran, ožičen
  pod "PODEŠAVANJA" sekcijom Zarade sidebar-a (`NavZaradePodesavanja_Click`). Sadrži tab
  "📥 Uvoz podataka" sa dve kartice:
  - **Uvoz iz ERPiZarade** — bira se `plata.db`/`firma_*.db` (isti EF-to-EF put kao
    postojeći `UvozWizardView`, poziva `ErpiZaradeProdukcijaImporter` direktno).
  - **DOS uvoz** — novo, `ZaradeDbfMigrator` (`ERPiMigration/Importers/`), 1:1 port DBF→SQLite
    logike iz `ERPiZaradeMigration/Program.cs` (konzolni alat) svedene na pozivnu metodu.
    Čita DOS/Clipper DBF fajlove (RADNICI, OBRACUN(I), RAD_SATI, POREZI, DOPRINOS, BANKE,
    KORISNIC, RAZREDI...) u privremenu ERPiZaradeData SQLite bazu, pa tu privremenu bazu
    odmah prosleđuje kroz isti `ErpiZaradeProdukcijaImporter` — ne duplira mapiranje logiku.
    Zahteva `DbfDataReader` paket (dodat u `ERPiMigration.csproj`, verzija usklađena na 2.2.0
    da se poklopi sa `ERPiFinansijeData`-inom transitivnom referencom, inače NU1605 downgrade
    greška). **Nije testirano sa stvarnim DBF fajlovima** (nema dostupnih DBF test podataka u
    ovoj sesiji) — testiran je samo EF-to-EF put.
- **Boja sidebar-a po modulu** — `MainWindow.xaml`-ov sidebar gradient (`GradStopSidebar1/2`)
  se sada menja u `TabModulXxx_Click` prema aktivnom modulu, da svaki modul zadrži boju
  svog izvornog samostalnog app-a: Finansije = teget/plavo (podrazumevano, nepromenjeno),
  Zarade = ljubičasto (`#2D1B42`→`#43305F`, iz `ERPiZaradeApp`), Sredstva = zeleno
  (`#1B4332`→`#2D6A4F`, iz `ERPiSredstvaApp`). Novi stilovi `NavButtonStyleZarade`/
  `NavButtonStyleSredstva` u `App.xaml` (translucent-white selekcija kao original, ne
  solid-blue kao `NavButtonStyle`).

**Tri bag-a nađena i ispravljena u `ErpiZaradeProdukcijaImporter`-u** (postojao je pre ove
sesije, ali nikad nije bio testiran protiv baze koja već ima podatke — vidi test niže):

1. **Nije bio idempotentan.** `ObracunPlate`/`Ugovor`/`Kredit` nemaju jedinstven indeks u
   `ErpiDbContext`-u, pa je ponovno pokretanje uvoza nad već uvezenom firmom tiho dupliralo
   svaki red (uhvaćeno: 5002 obračuna → 10004 na drugom pokretanju). `RadniSat`/
   `PppPdPrijava`/`Bolovanje` IMAJU jedinstven indeks, pa je isti scenario bacao
   UNIQUE constraint izuzetak koji je obarao ceo preostali `SaveChanges` (uključujući
   nepovezane tabele u istom pozivu). Ispravljeno: svaki korak sad gradi `HashSet`
   postojećih ključeva iz `_destDb` pre upisa i preskače već uvezene redove — isti
   obrazac koji su `Radnici`/`Isplate` već koristili.
2. **Nedostajao je uvoz 8 tabela** koje `ErpiDbContext` već podržava a importer ih
   nikad nije dirao: `Samodoprinosi`, `Porezi`, `Doprinosi`, `DoprinosiPoslodavca`,
   `Banke`, `PlatniRazredi`, `PoreskeOlaksice`, `SabloniUgovora`. Sve dodato, sa dedup-om
   po najbližem prirodnom ključu (nema svaka tabela DB unique indeks, pa se ključ bira
   ručno — npr. `Kredit` po `RadnikId+Opis+UkupanIznos+DatumPocetka`).
3. **`ERPiData.Models.Zarade.DoprinosiPoslodavca` je bio krnj model** — imao je samo
   `Zar1..9`, nedostajale su `Bol1..9`/`Nak1..9`/`Nep1..9`/`B60F1..9`/`B601..9`/`Inv1..9`/
   `Por1..9` (poređeno sa izvornim `ERPiZaradeData` modelom, koji ih ima sve). Dopunjeno +
   nova migracija `DopuniDoprinosiPoslodavcaKolone` (aditivna, samo `AddColumn`, proverena
   `dotnet ef database update --connection` na privremenoj bazi).

**Usput nađen i ispravljen četvrti, opštiji bag u `ErpiDbContext.Create(dbPath)`:** catch
blok za "baza je kreirana van EF migracija pa tabela već postoji" je hvatao
`ex.SqliteErrorCode == 1` (SQLITE_ERROR — generički kod za skoro svaku SQL grešku), ne samo
"already exists" poruku. Prava produkciona baza (PSSS PIROT) ima potpuno praznu
`__EFMigrationsHistory` tabelu (nikad nije praćena kroz EF migracije, verovatno kreirana
nekim drugim putem) — `Database.Migrate()` je pokušao da ponovo primeni SVE migracije od
`InitialCreate`, pukao na prvoj ("tabela već postoji"), taj širi catch je to nečujno progutao,
i **nijedna migracija se stvarno nije primenila** (uključujući i novu
`DopuniDoprinosiPoslodavcaKolone`) — otkriveno tek kad je `INSERT` pukao na
"no column named B601". Catch je sveden samo na `ex.Message.Contains("already exists")`.
**Ako se neka druga lokalna baza (`ARHIBEL_ARHIBEL_doo_Pirot.db`, `AUTOTEST.db`) otvori i ima
isti prazan `__EFMigrationsHistory`, sad će glasno puknuti umesto da se tiho progutа** — to je
namerno (bolje glasan pad nego tiha šema koja zaostaje), ali ih treba proveriti/popraviti
(ručni `INSERT` postojećih ID-jeva migracija u `__EFMigrationsHistory`, isti postupak kao za
PSSS PIROT ispod) pre sledećeg rada koji te baze otvara.

**Peti, najozbiljniji bag u ovoj sesiji: `AppConfig.DbPath` nikad nije bio postavljen —
ceo Zarade modul je bio nepovezan sa izabranom firmom.** Skoro svaki Zarade ekran
(~40 fajlova pod `Views/Zarade/**`, nasleđe iz samostalnog `ERPiZaradeApp`-a gde je imalo
smisla imati jednu statičku putanju do baze) otvara **SVOJ NEZAVISAN**
`ErpiDbContext.Create(AppConfig.DbPath)` umesto da deli `_db` iz `MainWindow`-a (koji je
vezan za firmu izabranu u `CompanySelectWindow`-u). `AppConfig.DbPath`-ov getter, kad
`_dbPath` nije eksplicitno postavljen, pada na "prvi `.db` fajl u
`%LocalAppData%\ERPiApp\Baze`" — **potpuno drugi folder** od
`%LocalAppData%\ERPi\Baze\firma_*.db` gde žive prave baze po firmi — a ništa u čitavom
`ERPiApp`-u to nikad nije eksplicitno postavljalo. Rezultat: svaki Zarade ekran je tiho
otvarao/kreirao praznu `erpi.db` u pogrešnom folderu, bez obzira koja je firma stvarno
aktivna — **svi Zarade ekrani su izgledali prazni**, čak i kad je prava firma imala
kompletno uvezene podatke (uhvaćeno upravo pri proveri ovog uvoza — "Sve su prazne").
Ispravljeno postavljanjem `AppConfig.DbPath = selected.DbPath;` u
`CompanySelectWindow.Otvori()` (i analogno u `App.xaml.cs`-ovom `--autologin` putu), tačno
tamo gde se bira i otvara baza izabrane firme, pre nego što se prikaže ijedan Zarade ekran.
**Pravi, temeljniji fix** bi bio da svih ~40 Zarade ekrana prime `_db` kroz konstruktor
(kao što Finansije ekrani već rade, npr. `new NaloziView(_db)`) umesto da se oslanjaju na
statički `AppConfig.DbPath` — to bi i eliminisalo rizik da neki budući ekran otvori TREĆU,
opet nezavisnu konekciju. Ova sesija je uzela brži/pliđi fix (jedna linija na ulazu) da
odmah odblokira testiranje; dublji refaktor ka `_db` konstruktor-injekciji ostaje kao
tehnički dug za sledeći rad na Zarade modulu.
Uzgredna posledica: `%LocalAppData%\ERPiApp\Baze\erpi.db` je artefakt ovog buga (kreiran
tokom testiranja pre fixa) — bezopasan, sad orfan, nije obrisan (nije neophodno, taj put
se više ne pogađa).

**Uvoz je testiran end-to-end i zadržan** u pravoj produkcionoj bazi
`%LocalAppData%\ERPi\Baze\firma_100188310_PSSS_PIROT_DOO_PIROT.db`, izvor
`%LocalAppData%\ERPiZaradeApp\Baze\firma_100188310_PSSS_PIROT_DOO_PIROT.db`:
- Pre uvoza je napravljen pun backup (van repoa, u scratchpad-u), baza je ručno popravljena
  (upisani ID-jevi svih 10 postojećih migracija u `__EFMigrationsHistory`, vidi bag #4 gore),
  pa je uvoz pokrenut **dva puta zaredom** da se potvrdi idempotentnost.
- Prvi prolaz: `Uspesno: True` — 18 novih radnika (istorijski periodi kojih nije bilo),
  3349 samodoprinosa, 304 doprinosa poslodavca, 316 poreza, 1180 doprinosa, 617 banaka,
  1 platni razred, 5 poreskih olakšica, 4 šablona ugovora.
- Drugi prolaz: **svih 16 brojača na 0** — potvrđena puna idempotentnost.
- WAL fajl checkpoint-ovan (`PRAGMA wal_checkpoint(TRUNCATE)`) posle uvoza, baza ostala
  samo kao `.db` fajl bez pratećih `-shm`/`-wal`.

---

## 3h. Faza 4 (Osnovna sredstva) — stanje 05.08.2026, jezgro preneto iz ERPiSredstva

Preneto u ovoj sesiji (`ERPiData/Models/Sredstva`, `ERPiData/Services/Sredstva`,
`ERPiApp/Views/Sredstva/**`), migracija `DodajOsnovnaSredstva` (verifikovana na scratch bazi),
55 xUnit testova (17 postojećih + 38 novih, port iz `ERPiSredstvaData.Tests`, svi prolaze):

- **Registar sredstava** (`Sredstva/SredstvaPage`) — šifarnik, pretraga, ukupne vrednosti,
  bar-kod nalepnice (`NalepniceDocument`, ZXing.Net — paket dodat u `ERPiApp.csproj`).
- **Analitičke kartice** (`Kartice/KarticePage`) — hronologija promena po sredstvu (master-detail),
  PDF štampa (`AnalitickaKarticaDocument`). `MainWindow.NavigateToSredstvaKartica(sredstvoId)` je
  novi javni helper (isti obrazac kao Zaradin `NavigateToObracun`) — poziva ga `SredstvaPage` pri
  dupl-kliku/dugmetu "Kartica" da otvori karticu konkretnog sredstva iz drugog ekrana.
- **Prijava sredstava** (`Prijave/PrijavaPage` + `PrijavaWindow`) — nalog za prijem/aktiviranje,
  PDF štampa (`PrijavaDocument`).
- **Rashod i promene** (`Rashod/RashodPage` + `RashodWindow`) — rashodovanje/prodaja/otuđenje/
  prenos OJ/brisanje/povećanje vrednosti-količine-amortizacije, sa automatskim obračunom srazmerne
  amortizacije do datuma rashoda (MRS 16), PDF štampa (`RashodDocument`).
- **Amortizacija** (`Amortizacija/AmortizacijaPage`, 3 taba) — obračun i knjiženje računovodstvene
  amortizacije po periodu, lista po godinama, poreska amortizacija (Obrazac OA po Pravilniku za
  sredstva od 2019.) sa masovnom dodelom poreskih grupa i PDF izveštajima (`AmortizacijaDocument`,
  `ObrazacOADocument`, `ObrazacPB1Document`).
- Sidebar Sredstva sekcija (`MainWindow.xaml` `PnlNavSredstva`) ožičena sa 5 stavki
  (Registar/Kartice/Prijave/Rashod/Amortizacija), zamenjuje raniji "USKORO" placeholder.
  `NavButtonStyleSredstva` (zeleni sidebar) je već postojao iz Faze 7.2a, sada se prvi put koristi.
- Dopunjen `ERPiApp/App.xaml`: `AccentBrush`/`SuccessBrush`/`WarningBrush`/`DangerBrush`,
  `StatCard`/`DangerButton` stilovi i tri status-boja konvertera (`ERPiApp/Converters/StatusConverters.cs`)
  preneti iz `ERPiSredstvaApp`-ovog `Styles.xaml`/`Converters` — nedostajali su za Sredstva ekrane.

**Odluke koje ne treba poništavati (specifične za Fazu 4):**
- **`Sredstvo.Konto`/`Kartica.Konto`/`Prijava.Konto`** (string u izvoru) su postali **`KontoId`**
  FK ka `Core.Konto` — isti obrazac string→FK kao svuda drugde (vidi §2). `AmortizacionaGrupa`/
  `PoreskaGrupa` OSTAJU stringovi (katalog kodovi I–V iz `PoreskaGrupaCatalog`, nema svoju tabelu
  ni u izvoru) — to nije previd, nego namerno: nema šta da se referencira.
- **`Dobavljac` (zaseban model u ERPiSredstvaData, samo `Konto`+`OpisKonta`+adresa) namerno NIJE
  prenet.** `Prijava.DobavljacId` je postao `Prijava.PartnerId` FK ka `Core.Partner`
  (`JeDobavljac = true`) — dobavljač je ovde samo partner, isti obrazac kao Finansije/Zarade.
  Posledica: `PrijavaWindow`-ov "+ Novi dobavljač" brzi unos (izvor je otvarao `DobavljacWindow`)
  je uklonjen — novi dobavljač/partner se unosi kroz postojeći ekran Partneri pre otvaranja
  Prijave. `DobavljaciPage`/`DobavljacWindow` iz izvora nisu portovani.
- **`Firma`/`Korisnik`/`LoginWindow`/`Dashboard`/`Podesavanja` ekrani iz ERPiSredstvaApp nisu
  portovani** — ERPi već ima svoje (Core `Firma`/`Korisnik`, `CompanySelectWindow`/`LoginWindow`,
  `Shell/DashboardView`, `Podesavanja/UvozWizardView`), isti obrazac kao "Šta NIJE nedostatak" u
  §3g za Finansije.

**Poznati nedostaci (namerno odloženo, "Trim, don't transplant whole"):**
- **`PopisPage`/`UpisPopisaWindow`** (godišnji popis, `Komisija`/`ClanKomisije`/`Popis`/
  `PopisnaStavka` modeli i `PopisCalculator` servis VEĆ postoje i imaju migraciju i xUnit
  testove — samo UI ekrani nisu preneti). Napomena: izvorni `UpisPopisaWindow.xaml` referencira
  `StaticResource OutlineButton` koji **ne postoji ni u izvornom `ERPiSredstvaApp/Resources/
  Styles.xaml`** — pre porta ovog ekrana ili definisati taj stil ili zameniti sa `SecondaryButton`.
- **`RevalorizacijaPage`** (revalorizacija/indeksacija po koeficijentima) — `RevalorizacijaCalculator`
  servis već postoji i pokriven je xUnit testovima, UI ekran nije prenet.
- **`IzvestajiPage`** (zbirna izveštajna stranica) nije prenet — isti opštiji nedostatak kao
  "Izveštaji hub" za Finansije u §3g (ERPi generalno još nema centralnu izveštajnu stranicu ni za
  jedan modul).
- **F1 Pomoc** — Sredstva nema `Views/Sredstva/Pomoc` (ERPiSredstvaApp ima `Pomoc/EditHelpWindow`
  itd.) — isti opštiji nedostatak kao za Finansije u §3g, Zarade je jedini modul koji ga ima
  (prenet u Fazi 5).
- **`ObracunskaJedinica`** (int na `Sredstvo`/`Kartica`/`Prijava`/`Rashod`) je ostala kao goli
  numerički kod, bez FK — u izvornom ERPiSredstva takođe nema svoj šifarnik/tabelu, pa nije bilo
  šta da se poveže. Ako se u budućnosti pokaže da "obračunska jedinica" treba da bude pravo mesto
  troška, to je nova modelska odluka (mapiranje na `Core.MestoTroska`), ne prost string→FK port.
- **Nijedan Sredstva ekran nije vizuelno proveren kroz UI** — isti razlog kao §3d/§3g za druge
  module (korisnik testira sam, vidi §4). Build je čist (`dotnet build ERPi.slnx`, 0 grešaka), EF
  migracija primenjena end-to-end na scratch bazi, 55/55 xUnit testova prolazi — ali dugme-po-dugme
  provera (posebno `PrijavaWindow`/`RashodWindow` transakciona knjiženja i `AmortizacijaPage`-ov
  poreski tab) nije urađena.
- **DOS uvoz Sredstava** (`DosSredstvaImporter` iz `ERPiMigration`, planiran u ANALIZA_I_PLAN §4
  kao Faza 7.2b) i dalje nije napisan — modeli/migracija sada postoje pa je uvoznik sledeći
  logičan korak kad zatreba (Sredstva nema produkcione podatke, DOS import će biti dovoljan, isti
  status kao Finansije).

---

## 4. Testiranje

- **`run-erpi-app`** (`ERPiApp/.claude/skills` i `.agents/skills`, mora ostati sinhronizovano
  u oba) — UI Automation driver, `--autologin` kroz fiksnu `AUTOTEST` firmu
  (`%LocalAppData%\ERPi\Baze\AUTOTEST.db`). Screenshot ide preko UIA `BoundingRectangle`, ne
  golog `GetWindowRect` — na skaliranom ekranu (125%/150%) ovaj drugi tiho seče desnu/donju
  ivicu prozora bez greške; koštalo je vremena da se otkrije, ne vraćati taj "pojednostavljeni"
  pristup. **Korisnik sam testira kroz UI od avgusta 2026. — ne pokretati/voziti ovaj driver
  samoinicijativno da bi se "prošetalo" kroz ekrane; koristiti ga samo ako korisnik izričito
  traži screenshot ili automatizovan prolaz.**
- **`ERPiData.Tests`** (xUnit) — automatizovani unit i integracioni testovi po uzoru na `ERPiFinansijeData.Tests` (EF Core In-Memory baza) za provere proračuna kalkulacija, uravnoteženosti naloga, zatvaranja stavki i modela.

---

## 3c. Plan kloniranja preostalih funkcionalnosti iz ERPiFinansije (Faze 3.5 – 3.12)

Da bi `ERPi` u potpunosti zamenio `ERPiFinansije`, sve preostale funkcionalnosti iz `ERPiFinansije` (ERPiFinansijeData / ERPiFinansijeApp) se sinhronizovano prenose i prilagođavaju novom `ErpiDbContext` sa pravim FK relacijama i modernim WPF UI-jem:

1. **Faza 3.5 — Šifarnici Konta & Mesta troška**
   - Servisi: `KontaService`, `MestaTroskaService`
   - Pogledi: `KontaView`, `KontoEditWindow`, `UvozKontnogPlanaWindow`, `MestaTroskaView`, `MestoTroskaEditWindow`
2. **Faza 3.6 — Izveštaji Glavne knjige & Bilansi**
   - Servisi: `BrutoBilansService`, `KarticaService`, `BilansService`, `AprProsireniIzvestajiService`
   - Pogledi: `BrutoBilansView`, `KarticaKontaView`, `DnevnikView`, `BilansStanjaView`, `BilansUspehaView`
3. **Faza 3.7 — Bankarski izvodi & Automatsko knjiženje**
   - Modeli & Servisi: `BankIzvodModels` (`BankIzvod`, `StavkaBankIzvoda`), `BankIzvodService`, `BankIzvodParsers` (Halcom TXT, Asseco XML, ISO 20022), `BankIzvodMatchingEngine`
   - Pogledi: `IzvodiView`, `IzvodEditWindow`, `UvozIzvodaWindow`
4. **Faza 3.8 — Blagajničko poslovanje**
   - Modeli & Servisi: `BlagajnaModels` (`Blagajna`, `BlagajnickiNalog`, `StavkaBlagajnickogNaloga`), `BlagajnaService`
   - Pogledi: `BlagajneView`, `BlagajnaEditWindow`, `BlagajnickiNalogWindow`
5. **Faza 3.9 — Devizno knjigovodstvo & Kursne liste**
   - Modeli & Servisi: `KursnaListaStavka`, `DeviznoKnjigovodstvoService`, `KursnaListaService`, `NbsApiClient`
   - Pogledi: `DevizniNaloziView`, `KursnaListaView`
6. **Faza 3.10 — Putni nalozi**
   - Modeli & Servisi: `PutniNalogModels` (`PutniNalog`, `StavkaPutnogNaloga`), `PutniNalogService`
   - Pogledi: `PutniNaloziView`, `PutniNalogEditWindow`
7. **Faza 3.11 — Kompenzacije**
   - Modeli & Servisi: `KompenzacijaModels` (`Kompenzacija`, `StavkaKompenzacije`), `KompenzacijaService`
   - Pogledi: `KompenzacijeView`, `KompenzacijaEditWindow`
8. **Faza 3.12 — Robno & Materijalno poslovanje, Komercijala, Trgovina & DMS**
   - Modeli: `MaterijalnaKartica`, `Materijal`, `UlazNalog`, `TrebovanjeNalog`, `PrimopredajaNalog`, `MaloprodajnaKalkulacija`, `UvoznaKalkulacija`, `NivelacijaCena`, `RacunOtpremnica`, `DokumentPrilog`
   - Servisi: `MaterijalnaKarticaService`, `RobniBrutoBilansService`, `UlazService`, `TrebovanjeService`, `PrimopredajaService`, `MaloprodajnaKalkulacijaService`, `UvoznaKalkulacijaService`, `NivelacijaService`, `RacunOtpremnicaService`, `DmsService`
   - Pogledi: `MaterijalneKarticeView`, `RobniBrutoBilansView`, `UlaziView`, `TrebovanjaView`, `MaloprodajneKalkulacijeView`, `NivelacijeView`, `KEPKnjigaView`, `RacuniOtpremniceView`, `DmsView`

---

## 3g. Uporedna revizija ERPi vs ERPiFinansije (05.08.2026, na zahtev korisnika — "dosta stvari nisu implementirane")

Korisnik je zatražio proveru da li ERPi zaista pokriva sve što ima ERPiFinansije. Revizija je
rađena upoređivanjem foldera 1:1 (`ERPiFinansijeApp/Views` naspram `ERPiApp/Views/Finansije` +
`Views/Magacin`, `ERPiFinansijeData/Services` naspram `ERPiData/Services` + `ERPiApp/Services`) —
**§1 tabela je bila netačna za Fazu 3.12**: migracija i modeli postoje, ali servisni i UI sloj
skoro potpuno nedostaju. Ispravljeno na 🔶 gore. Puna lista, po modulu:

**Robno & Materijalno (Faza 3.12) — najveći nalaz, delimično zatvoreno u ovoj sesiji:**
- Modeli su zatečeni (`TrebovanjeNalog`, `UlazNalog`, `PrimopredajaNalog`, `NivelacijaCena`,
  `MaloprodajnaKalkulacija`, `UvoznaKalkulacija`), ali **nijedan od 8 servisa iz
  `ERPiFinansijeData/Services`** bio prenet u `ERPiData/Services`, niti ijedan odgovarajući
  ekran u `ERPiApp/Views`. Modeli su bili potvrđeno **nekorišćeni nigde u ERPiApp-u** pre ove
  sesije (`grep` za `TrebovanjeNalog`/`UlazNalog`/itd. van `ERPiData` — nula pogodaka).
- **Usput nađeno i ispravljeno u ovoj sesiji**: `TrebovanjeNalog.SifraMagacina`,
  `TrebovanjeStavka.SifraArtikla`, `UlazNalog.SifraMagacina`, `UlazStavka.SifraArtikla`,
  `PrimopredajaNalog.SifraMagacinaDaje/Prima`, `PrimopredajaStavka.SifraArtikla`,
  `MaloprodajnaKalkulacija.SifraMagacinaPrima/Daje/SifraDobavljaca`,
  `MaloprodajnaKalkulacijaStavka.SifraArtikla` su i dalje bili DBF-stil string kodovi umesto
  pravih FK-ova — kršenje pravila iz `import-from-source-apps` skill-a ("string reference → real
  foreign key", vidi i §2 gore). `NivelacijaCena`/`UvoznaKalkulacija` su, za razliku od ostalih,
  već imale ispravne FK-ove — nekonzistentnost je nastala jer su modeli dodavani u različitim
  ranijim sesijama. Ispravljeno na prave FK-ove: `MagacinId` svuda, i **`MaterijalId`** (ne
  `ArtikalId`!) na `TrebovanjeStavka`/`UlazStavka`/`PrimopredajaStavka` — Ulaz/Trebovanje/
  Primopredaja su Materijalno (ne Robno) knjigovodstvo, rade nad `Materijal` šifarnikom, isto
  kao ERPiFinansije-in izvorni `MaterijalnaKarticaService.GetArtikliAsync()` (koji uprkos imenu
  vraća `List<Materijal>`, ne `List<Artikal>`) — `MaloprodajnaKalkulacijaStavka.ArtikalId`
  ostaje na `Artikal` jer je to zaista Robno (maloprodaja). Migracija `PopraviRobnoFkVezama`
  (regenerisana posle ove ispravke), proverena na scratch bazi.
- **Portovano i radi (build čist, 4 nova xUnit testa prolaze)**: `MaterijalnaKarticaService`
  (ponderisana prosečna cena — ista formula, testirana protiv iste legacy logike kao izvor),
  `UlazService`, `TrebovanjeService`, `RobniBrutoBilansService` (u `ERPiData/Services`) +
  `UlazEditWindow`, `TrebovanjeEditWindow`, `MaterijalnoDashboardView` (u
  `ERPiApp/Views/Magacin`). Razlike od izvora: dele već otvoren `ErpiDbContext` (ne otvaraju
  sopstvenu konekciju), grid kolona za materijal je pravi FK combo (ne slobodan tekst šifre),
  "Knjiži"/"Rasknjiži" akcija je inline dugme na redu u dashboard-u (izvor je ima na posebnom
  tabu u `MagacinView`, koji ovde NIJE portovan).
- **Meni (05.08.2026, druga provera)**: korisnik je javio da nema stavku menija za "Robno" —
  potvrđeno, `MainWindow.xaml`-ov sidebar je bio flat lista bez sekcijskih grupa (za razliku od
  ERPiFinansije koje ima jasno odvojene sekcije FINANSIJE/ROBNO/MATERIJALNO/Šifarnici/
  Administracija), a jedino `BtnMagacin` ("📦 Magacin i PDV") je pokrivao i Robno i Materijalno
  odjednom kroz tabove. Ispravljeno: `BtnMagacin` preimenovan u "📦 Robno (Kalkulacije, Magacini,
  Artikli)", i dodato novo `BtnMaterijalno` ("🏭 Materijalno (Ulazi, Trebovanja)") kao svoj
  top-level nav item koji otvara `MaterijalnoDashboardView` direktno (ne kao tab unutar
  `MagacinMainView` — probano prvo kao tab pa vraćeno, da IA liči na izvor gde su ROBNO i
  MATERIJALNO odvojene sekcije, ne ugnježdene). Sekcijski header-i (vizuelno grupisanje kao u
  izvoru) nisu dodati — samo dva nova/preimenovana dugmeta, manji zahvat. Nije vizuelno
  provereno kroz UI (korisnik testira sam, vidi §4).
- **Bag nađen i ispravljen (05.08.2026, prijavio korisnik uz screenshot)**:
  `KarticaKontaView`'s `ChkSamoSaPrometom` je imao `IsChecked="True"` kao XAML literal —
  isti obrazac bug kao `NaloziView`-ov `RadioButton` slučaj iz §2, samo ovde je žrtva bila
  `_service`/`_db` polje umesto sibling kontrole: `Checked="Filter_Changed"` se okinuo
  SINHRONO usred `InitializeComponent()`, pre nego što je `_service = new KarticaService(_db)`
  uopšte izvršeno u telu konstruktora (dodeljuje se POSLE `InitializeComponent()` poziva) —
  `NullReferenceException` na `_service` unutar `LoadKonta()`, uhvaćen u try/catch i prikazan
  kao "Greška pri učitavanju kontnog plana: Object reference not set to an instance of an
  object." Ovo je PRVI put da je neko realno otvorio ovaj ekran (§3d ga je već imenovao kao
  "nije vizuelno proveren"). Ispravljeno: `IsChecked="True"` uklonjen iz XAML-a, postavlja se
  u `Loaded` handleru (posle `InitializeComponent()` I posle prve dodele `_db`/`_service`),
  zamenjujući eksplicitan `LoadKonta()` poziv (isti `Checked` handler ga ionako zove). Grep
  cele `ERPiApp/Views` stabla za `IsChecked="True".*Checked=` nije našao drugih instanci ovog
  obrasca — ovo je bio jedini slučaj.
- **AŽURIRANO 05.08.2026 (kasnije u istoj sesiji, commit `3491df9`)** — sledeće više NIJE
  nedostatak, prethodna verzija ovog pasusa je bila zastarela: `PrimopredajaService`+
  `PrimopredajaEditWindow` (Materijalno, `MaterijalId` — ožičeno kao brza akcija u
  `MaterijalnoDashboardView`), `NivelacijaService`+`NivelacijaEditWindow`/`NivelacijeView`,
  `MaloprodajnaKalkulacijaService`+`MaloprodajneKalkulacijeView`, `UvoznaKalkulacijaService`+
  `UvozneKalkulacijeView`, `RacunOtpremnicaService`+`RacuniOtpremniceView`/
  `RacunOtpremnicaEditWindow` (model `RacunOtpremnica` kreiran, migracija
  `DodajRacunOtpremnica`), `DmsService` (servisni sloj postoji, bez UI ekrana — vidi dole).
  Sve pod `MagacinMainView` hub-om (taboovi: Ulazne kalkulacije/Nivelacije/MP kalkulacije/
  Uvozne kalkulacije/Šifarnik artikala/Šifarnik magacina) + `BtnRacuniOtpremnice` kao zaseban
  top-level nav. Build čist, xUnit testovi (`RobnoMaterijalnoTests`, `PdvTests`) prolaze.
  **I dalje nedostaje** (provereno ponovo 05.08.2026, uz uporedbu izvornog `TrgovinaView.xaml`
  tab-po-tab — vidi §3i za pun detalj i za novi nalaz Zaduženja/Razduženja):
  - `MaterijalnaKarticaService`'s `MaterijalneKarticeView`/`ProveraKarticaWindow` (pregled/
    provera same kartice — servisni sloj postoji, ekran ne).
  - `KEPKnjigaView`, `RobnoDashboardView` (Robna strana — Materijalna je gotova).
  - DMS UI (`DmsWindow`, `DmsOcrPreviewWindow`, `DmsOcrInvoiceParser`, `DmsOcrMatchingService`)
    — `DmsService` osnovni servis postoji, OCR/matching sloj i ekran ne.
  - Trgovina extra ekrani bez pandana: `NarudzbenicaEditWindow`, `PonudaEditWindow`,
    `PoreskaTarifaEditWindow` — vidi §3i.
  - Puna tabelarna lista Ulaza/Trebovanja/Primopredaja sa filterima (originalni `MagacinView`,
    odvojen od `MaterijalnoDashboardView`) takođe nije portovana — dashboard pokriva poslednjih
    8 + brze akcije, ne punu istoriju.

## 3i. Robno (Trgovina) tab-po-tab revizija (05.08.2026, na zahtev korisnika uz screenshot)

Korisnik je pokazao screenshot izvornog `ERPiFinansijeApp`-ovog `TrgovinaView` (13 tabova) i
pitao šta u ERPi-ju nedostaje. Puna uporedba tab-po-tab sa izvornim
`ERPiFinansijeApp/Views/Trgovina/TrgovinaView.xaml`:

| Tab u izvoru | Status u ERPi |
| :--- | :--- |
| Ponude & Predračuni | ⬜ nedostaje (vidi dole) |
| Narudžbenice Dobavljačima | ⬜ nedostaje (vidi dole) |
| **Računopolagači** | ✅ **nije stvarni nedostatak** — u izvoru je ovo isti `Magacin` šifarnik (`DgRacunopolagaci` binduje `SifraMagacina`/`NazivMagacina`/`OdgovornoLice`/`VrstaMagacina`, `LoadRacunopolagace()` čita `db.Magacini`), samo drugačije nazvan tab u istom ekranu. ERPi već ima identične kolone (uključujući `OdgovornoLice`) u `MagaciniView` (tab "Šifarnik magacina" u `MagacinMainView`) — nema šta dodatno da se portuje, eventualno samo dodati alias/tooltip ako korisnik želi da i ERPi ima tab pod imenom "Računopolagači".
| Šifarnik artikala | ✅ `ArtikliView` |
| Poreske tarife | ✅ portovano (05.08.2026, iste sesije) — `PoreskaTarifa` model + `PoreskeTarifeView`/`PoreskaTarifaEditWindow`, tab u `MagacinMainView`, migracija `DodajPoreskeTarife` verifikovana na scratch bazi |
| **Zaduženja** / **Razduženja** | ⬜ nedostaje, **ali nije novi entitet** — u izvoru dele istu `PrimopredajaNalog`/`PrimopredajaStavka` tabelu kao tab "Primopredaje", razlikovane samo preko `VrstaDokumenta` ("Zaduženje"/"Razduženje"/"Primopredaja", vidi `TrgovinaView.xaml.cs` komentar oko L1584 i `ApplyFilterPrimopredaje` koje filtrira `_svePrimopredaje.Where(p => p.VrstaDokumenta == vrsta)`). **Važna arhitekturna razlika**: ta izvorna `PrimopredajaNalog` je Robno/Artikal-bazirana (`PrimopredajaStavka.SifraArtikla`, koristi se u `Views/Trgovina/PrimopredajaEditWindow`) — DRUGAČIJA od ERPi-jevog već portovanog `PrimopredajaNalog`-a (`ERPiData/Models/Magacin/UlazNalog.cs`), koji je namerno Materijalno/`MaterijalId`-bazirano (§3g odluka, port izvorne `Views/Magacin/PrimopredajaEditWindow`, koja radi nad `Materijali`). Dakle Zaduženje/Razduženje/Robna-Primopredaja **ne mogu da se dodaju kao filter na postojeći ERPi `PrimopredajaService`** — treba nov model (npr. `RobnoInternoKretanje`/`RobnaStavkaKretanja` sa `ArtikalId` FK, `MagacinIdDaje`/`MagacinIdPrima`, `VrstaDokumenta` diskriminator "Primopredaja"/"Zaduženje"/"Razduženje", analogno postojećem Materijalnom pandanu), nov servis (kopija `PrimopredajaService`-ove VP↔MP PDV logike, ali nad `RobnaKarticaService`/`MaterijalnaKarticaService`-ovim Robno-pandanom ako postoji, ili direktno nad `RobnaKartica` ako je već portovano — proveriti pre pisanja) i UI (1 edit prozor + 1 list view sa "Svi/Proknjiženi/Neproknjiženi" filterom, parametrizovan po `VrstaDokumenta`, po uzoru na izvorni `TrgovinaView`-ov `NovaPrimopredaja(vrsta)`/`ApplyFilterPrimopredaje(vrsta)` obrazac — ne 3 odvojena skoro-identična ekrana).
| Kalkulacije | ✅ `KalkulacijeView` |
| Robne kartice | pretpostavlja se ✅ preko `RobniBrutoBilansService`/`RobniBrutoBilansView` — nije posebno provereno da li postoji i pojedinačna kartična (analitička) pretraga po artiklu, samo zbirni bruto bilans; vidi i `MaterijalneKarticeView` nedostatak gore (Materijalna strana ima isti otvoren nedostatak).

**Ponude & Predračuni / Narudžbenice Dobavljačima** (izvor: `ERPiFinansijeData/Models` nema
posebne fajlove za ove — proveriti da li su modelovane kao `VrstaDokumenta` na zajedničkom
"dokument" entitetu ili kao zaseban `Ponuda`/`Narudzbenica` model pre porta; oba tab-a u izvoru
imaju dugme "Pretvori u Fakturu/Kalkulaciju" — zavise od `RacunOtpremnica`/`Kalkulacija` kao
odredišta konverzije, oba već postoje u ERPi, pa je preduslov zadovoljen).

**Poreske tarife** — portovano (vidi tabelu gore). Preostaju tri: **Ponude & Predračuni**,
**Narudžbenice Dobavljačima**, **Zaduženja/Razduženja** (Robno/Artikal varijanta Primopredaje,
zahteva nov model — vidi arhitekturnu napomenu gore). Nije vizuelno provereno kroz UI
(korisnik testira sam, vidi §4) — `PoreskeTarifeView` build je čist, migracija primenjena
end-to-end na scratch bazi, ali dugme-po-dugme provera CRUD-a nije urađena.

**Finansije — ekrani koji postoje u ERPiFinansije a nemaju pandan u ERPi:**
- **Korisnici/prava pristupa** — `KorisniciView`/`KorisnikEditWindow` nemaju NIKAKAV pandan;
  ERPi ima samo `Auth/LoginWindow`, nema ekran za upravljanje korisnicima/ulogama iznutra.
- **PDV evidencija** (`PdvEvidencijaView`, KIR/KPR knjige, PP-PDV XML export za ePorezi) —
  nema pandan. Napomena: `ERPiData.Models.Finansije.PdvZapis` postoji, ali je modelovan kao
  **perzistentni entitet** (upisuje se direktno), dok je ERPiFinansije-in `PdvZapis` **računat
  DTO** izveden iz `RacuniOtpremnice`/`Kalkulacije`/`StavkeNaloga` (nikad se ne upisuje) —
  oblik se ne poklapa, ne može se "samo prekopirati" servis nad postojećim ERPi modelom bez
  redizajna ili prepravke `PdvZapis`-a. Takođe zavisi od `RacunOtpremnica` (Trgovina, vidi
  gore — ne postoji), pa je makar MVP verzija (samo ručne stavke preko konta 4700/2700 na
  `StavkaNaloga.Osnovica`/`StopaPdv`, koje već postoje kao kolone — vidi poznati nedostatak u
  §3 "Nema UI za PDV Osnovica/StopaPdv") realniji prvi korak od punog 1:1 porta.
- **Opšta Podešavanja** (`PodesavanjaView`) — ERPi ima samo `Podesavanja/UvozWizardView`
  (uvoz), nema opšti ekran firme/konfiguracije.
- **Backup** (`BackupView` + `BackupService`) — nema pandan za Finansije/celu bazu (Zarade ima
  sopstveni `Services/Zarade/BackupService.cs`, ali generalni backup ekran ne postoji).
- **Izveštaji hub** (`IzvestajiView` kao centralna stranica) i preview/štampa prozori:
  `DnevnikPreviewWindow` (dnevnik knjiženja), `IosPreviewWindow`, `VrednovanjeZalihaPreviewWindow`,
  `ZakljucniListPreviewWindow`, `BrutoBilansAnalitikePreviewWindow` — ERPi ima samo
  `BrutoBilansView`/`KarticaKontaView`, bez zbirne stranice izveštaja i bez print-preview toka.
- **Izvodi banke** — `IzvodiView` (lista/pregled izvoda) i `IzvodEditWindow` (ručna izmena)
  nemaju pandan, ERPi ima samo `UvozIzvodaWindow` (uvoz) — posle uvoza nema gde da se izvod
  pogleda/ispravi u samom ERPi.
- **Kompenzacije** — `KompenzacijaEditWindow` (izmena pojedinačne kompenzacije) nema pandan,
  `KompenzacijeView` verovatno radi inline unos (nije provereno da li je funkcionalno
  ekvivalentno).
- **Partneri** — `IstorijaZatvaranjaWindow` (istorijat zatvaranja stavki) i `KursnaListaWindow`
  (pregled kursne liste — ERPi ima samo `DeviznoValviranjeWindow` za valorizaciju, ne i pregled
  liste kurseva) nemaju pandan.
- **Putni nalozi** — `IzvozZaZaradeWindow` (izvoz putnih naloga u Zarade) i
  `PutniNalogEditWindow` (zaseban dijalog izmene) nemaju pandan.
- **F1 Pomoc / help sistem za Finansije** — `Pomoc/PomocView`, `Pomoc/EditHelpWindow`,
  `Pomoc/ChangelogWindow`, `Pomoc/DosImportWindow` nemaju pandan na Finansije strani (Zarade
  ima svoj `Views/Zarade/Pomoc`, portovan u Fazi 5, ali Finansije nema ništa — isti nedostatak
  već zabeležen kao opštiji u §3b, ovde potvrđen i za Finansije specifično).
- **Napredna pretraga** (`Shared/NaprednaPretragaWindow` — globalna pretraga kroz više
  entiteta) nema pandan.
- **`KontoPickerWindow`** (F2 brza pretraga konta) — **urađeno u ovoj sesiji**: portovano u
  `ERPiApp/Views/Finansije/Konta/KontoPickerWindow.xaml(.cs)`, ožičeno na F2 u
  `NalogEditWindow` (`DgStavke_PreviewKeyDown`). Build čist, migracija nije bila potrebna
  (koristi postojeći `Konto` model). Nije vizuelno provereno kroz UI (korisnik testira sam,
  vidi §4).

**Šta NIJE nedostatak** (provereno, postoji ekvivalent samo pod drugim imenom/rasporedom):
`Bilansi` (razdvojeno na `BilansStanjaView`/`BilansUspehaView` umesto jedne `BilansiView` —
funkcionalno ekvivalentno), `Kartice` (`KarticaKontaView`), `Dashboard` (`Shell/DashboardView`),
`Firme` (`Firma/CompanySelectWindow`+`NovaFirmaWindow` — nije proveravano da li pokriva CRUD
obim `FirmeView`-a).

**Nije revidirano u ovoj sesiji** (nije stigla ruka, sledeći prolaz treba i ovo): potpuno
poređenje `ERPiSredstva` naspram (još neuvedene) Faze 4, kao ni `PoreskiBilansWindow` (poreski
bilans, odvojen od `BilansStanjaView`/`BilansUspehaView`).

**Preporučeni redosled sledećeg rada** (po uticaju/riziku, ne apsolutno obavezujuće):
1. **Ostatak Robno-materijalno servisa+ekrana** (§3g gore) — `UlazService`+`TrebovanjeService`
   i njihovi ekrani su gotovi (ova sesija), nastaviti sa `PrimopredajaService`+ekran (model već
   ima ispravan FK, isti obrazac kao Ulaz/Trebovanje — najbrži sledeći korak), zatim
   `RacunOtpremnicaService`+ekran (model `RacunOtpremnica` prvo mora da se kreira — preduslov
   za PDV evidenciju), pa ostatak (Nivelacija, MP/Uvozna kalkulacija ekrani, KEP knjiga, DMS).
2. **PDV evidencija** (KIR/KPR) — zakonski bitno za srpsko knjigovodstvo, ali zavisi od #1
   (`RacunOtpremnica`) za pun obim; MVP bez toga je moguć (samo ručne konto 4700/2700 stavke).
3. **Korisnici/prava pristupa** — potpuno nedostaje, bezbednosno/operativno relevantno čim
   firma dobije više od jednog korisnika.
- [x] **Kupci i Dobavljači Kontrole**: Radio dugmad (Svi, Kupci, Dobavljači), NBS provera računa, obračun kamata, IOS i padajući meni konta [c:\ERPi\ERPi\ERPiApp\Views\Finansije\Partneri\PartneriView.xaml](file:///c:/ERPi/ERPi/ERPiApp/Views/Finansije/Partneri/PartneriView.xaml)
- [x] **Piker za artikle**: Pretraga artikala i robe po šifri, nazivu i barkodu u realnom vremenu [c:\ERPi\ERPi\ERPiApp\Services\ArtikalPicker.cs](file:///c:/ERPi/ERPi/ERPiApp\Services\ArtikalPicker.cs)
- [x] **Uređenje bočnog menija**: Restrukturiran meni po sekcijama (Finansijsko, Robno, Materijalno, Porezi/SEF, Šifarnici, Podešavanja) po uzoru na ERPiFinansijeApp [c:\ERPi\ERPi\ERPiApp\Views\Shell\MainWindow.xaml](file:///c:/ERPi/ERPi/ERPiApp\Views\Shell\MainWindow.xaml)
- [x] **Dugmad za štampu / PDF & 1:1 kontrole po View-ovima**: 
  - Ujednačen sistem dizajn ikona (živopisne pozadinske boje, emoji prefiksi, `🖨️ PDF` dugme i `X` zeleno dugme za Excel izvoz) primenjen svuda u `ERPiApp`
  - Centralizovani QuestPDF generator `PdfReportService` sa zvaničnim **Nalog za knjiženje** PDF formatom i blokom sa tri potpisa (izradio, proknjižio, odobrio) [c:\ERPi\ERPi\ERPiApp\Services\PdfReportService.cs](file:///c:/ERPi/ERPi/ERPiApp\Services\PdfReportService.cs)
  - `NaloziView`: Klonirana kompletna traka sa dugmadima (`+ Novi nalog`, `✏️ Izmeni`, `☑️ Proknjiži`, `⚡ Proknjiži sve`, `🔓 Rasknjiži`, `🔄 Preknjižavanje`, `⚙️ Napredni filter`, `🏦 Uvoz izvoda`, `📒 Uvoz zarada`, `🖨️` PDF štampa, `X` Excel izvoz, `📅 Nova godina`) [c:\ERPi\ERPi\ERPiApp\Views\Finansije\Nalozi\NaloziView.xaml](file:///c:/ERPi/ERPi/ERPiApp\Views\Finansije\Nalozi\NaloziView.xaml)
  - `KarticaKontaView`, `BrutoBilansView`, `RacuniOtpremniceView`, `KalkulacijeView`, `ArtikliView`, `KontaView` i `PartneriView` usklađeni sa identičnim dizajnom ikona i dugmadi
- [x] **Klonirani LiveCharts Grafikoni na Radnoj tabli (`DashboardView`)**: 
  - Donut prstenasti grafikon `PieStatusNaloga` (Odnos proknjiženih i nacrta naloga u realnom vremenu)
  - Horizontalni stubičasti grafikon `BarPrometKonta` (Top 10 konta po ukupnom prometu u RSD)
  - [DashboardView.xaml](file:///c:/ERPi/ERPi/ERPiApp/Views/Shell/DashboardView.xaml) i [DashboardView.xaml.cs](file:///c:/ERPi/ERPi/ERPiApp/Views/Shell/DashboardView.xaml.cs)

**Napomena o obimu porta** — korisnik je u ovoj sesiji izričito tražio da se dalji rad radi kao blizak 1:1 klon (Views, servisi, testovi) umesto trimovanog MVP-a, prvenstveno zato što
ERPiFinansije/ERPiZarade strukturu podataka treba da ostane što sličnija zbog budućeg DOS uvoza
(Faza 7.2b). To ne menja pravilo string→FK iz `import-from-source-apps` skill-a (FK-ovi ostaju
— DOS uvoznik već radi to razrešavanje za Konto/Partner, isti obrazac važi i ovde), već znači:
manje trimovanja UI-ja/servisa/testova po "trim, don't transplant whole" default-u, više
direktnog portovanja celih ekrana kao u §3g iznad.

**Test podaci** — korisnik je naveo da se za vizuelnu proveru portovanih ekrana može koristiti
`C:\Users\Admin\AppData\Local\ERPiFinansijeApp\Baze\firma_TESTNEW_ARHIBEL_NEW.db`, ista baza
koja je već uvezena u ERPi kao AUTOTEST/ARHIBEL (vidi §7.1 gore) — znači da uvoz treba ponoviti
(ili proveriti da je već ažuran) za bilo koje nove entitete čim njihov uvoz u
`ErpiFinansijeImporter` bude dodat, pre nego što korisnik može vizuelno da proveri nove Robno
ekrane sa realnim podacima.

---

## Sledeći koraci

Faze 3.5–3.12 su implementirane, commit-ovane i push-ovane na `origin/main` (05.08.2026), ali
**3.12 je delimično netačno označena** — videti §3g za ispravku. §3d ("Poznati nedostaci u Fazi
3.5–3.12", vizuelna provera + čišćenje legacy kolona u `KontaView`) ostaje važeće uporedo.

**Faza 4 (Osnovna sredstva) je sada 🔶 — jezgro preneto u istoj sesiji** (registar, kartice,
prijava, rashod, amortizacija + poreska amortizacija/Obrazac OA), vidi §3h za pun opis i za listu
odloženog (Popis, Revalorizacija, Izveštaji hub, DOS uvoz). **Nije commit-ovano** — celo stablo
`ERPiData/Models/Sredstva`, `ERPiData/Services/Sredstva`, `ERPiApp/Views/Sredstva`, migracija
`DodajOsnovnaSredstva` i prateća migracija `PdvZapisRacunOtpremnicaSefPolja` (pre-postojeća
neprimenjena šema izdvojena u sopstvenu migraciju pri generisanju — videti §3h) su i dalje
untracked/modified u `git status`; ne commit-ovati dok korisnik ne kaže.

Preporučeni redosled sledećeg rada (bilo koji redosled je razuman, ovo je samo predlog):
1. **Korisnik vizuelno proveri Fazu 4** kroz UI (registar → prijava → kartica → amortizacija →
   rashod, tim redosledom prati zavisnost podataka) pre nego što se nastavi dalje na tom modulu.
2. §3g ("Preporučeni redosled sledećeg rada" za Finansije): Robno-materijalno ostatak servisa/
   ekrana, PDV evidencija, Korisnici/prava pristupa.
3. Dovršiti Fazu 4 (Popis/Revalorizacija/Izveštaji hub iz §3h) kad zatreba periodični popis ili
   revalorizacija.
4. Faza 5 (Obračun zarada, već u toku — vidi §3e) i Faza 6 (automatsko knjiženje Zarade/Sredstva →
   Nalog, sad kad oba modula postoje u istoj bazi).

---

## 5. Sesija 05.08.2026 (nastavak, uveče) — Robno tab-po-tab revizija + Poreske tarife

Korisnik je pokazao screenshot izvornog `TrgovinaView`-a i tražio da se proveri šta u ERPi
Robno delu nedostaje. Puna revizija je otkrila da je §3g-ova "I dalje nedostaje" lista bila
**zastarela** — najveći deo (Primopredaje, Nivelacije, MP/Uvozna kalkulacija, Računi-Otpremnice)
je već portovan i commit-ovan u `3491df9`, samo dokument nije ažuriran. Ispravljeno gore u §3g,
puna tab-po-tab tabela u novom §3i.

**Urađeno u ovoj sesiji:**
- Portovane **Poreske tarife** (`PoreskaTarifa` model, `PoreskeTarifeView`/
  `PoreskaTarifaEditWindow`, tab u `MagacinMainView`, migracija `DodajPoreskeTarife`).

**I dalje nedostaje** (§3i, redosled po preporuci — korisnik nije birao dalji redosled u ovoj
sesiji):
1. **Ponude & Predračuni** + **Narudžbenice Dobavljačima** — provereno da im preduslovi
   (`RacunOtpremnica`/`Kalkulacija` kao odredište konverzije) već postoje u ERPi.
2. **Zaduženja/Razduženja** — najsloženiji od preostalih, zahteva nov Robno/Artikal-bazirani
   model (§3i objašnjava zašto se NE može dodati kao filter na postojeći Materijalno/`MaterijalId`
   `PrimopredajaService`).
3. `Računopolagači` **ne treba portovati** — već pokriveno kroz `MagaciniView` (§3i).

**Nije vizuelno provereno kroz UI** (korisnik testira sam, vidi §4) — `PoreskeTarifeView` build
je čist, migracija primenjena end-to-end na scratch bazi (16. u nizu, posle `DodajOsnovnaSredstva`),
ali CRUD dugme-po-dugme provera nije urađena. **Nije commit-ovano.**

