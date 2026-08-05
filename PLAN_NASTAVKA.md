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
| **3.3a** | Magacin Osnovno (VP Kalkulacije, Šifarnik magacina/artikala, PDV zapisi) | ✅ |
| **3.3b / 3.12** | Robno & Materijalno poslovanje (Materijalne/Robne kartice, Ulazi, Trebovanja, MP/Uvozne kalkulacije, Nivelacije, KEP knjiga, Bruto bilansi robe/materijala) | ✅ |
| **3.4** | SEF e-Fakture (UBL 2.1 API) i e-Fiskalizacija (`PfrRacun`) | ✅ |
| **3.5** | Šifarnici Konta & Mesta troška (`KontaView`/`KontoEditWindow`/`MestaTroskaView`/`MestoTroskaEditWindow`) | ✅ |
| **3.6** | Izveštaji Glavne knjige & Bilansi (`BrutoBilansView`, `KarticaKontaView`, `BilansStanjaView`, `BilansUspehaView`, `AprProsireniIzvestajiService`) | ✅ |
| **3.7** | Izvodi banke & Auto-knjiženje (`UvozIzvodaWindow`, `BankIzvodService`, Parsers/MatchingEngine) | ✅ |
| **3.8** | Blagajničko poslovanje (`BlagajnaView`, `BlagajnickiNalogEditWindow`, `BlagajnaService`) | ✅ |
| **3.9** | Devizno knjigovodstvo & Kursne liste (`DeviznoValviranjeWindow`, `DeviznoKnjigovodstvoService`, `KursnaListaService`, `NbsApiClient`) | ✅ |
| **3.10**| Putni nalozi (`PutniNaloziView`, `PutniNalogModels`, `PutniNalogService`) | ✅ |
| **3.11**| Kompenzacije (`KompenzacijeView`, `KompenzacijaModels`, `KompenzacijaService`, Pametno skeniranje) | ✅ |
| **3.12**| Komercijala, Trgovina & DMS (`RacuniOtpremnice`, `Nivelacije`, `Maloprodaja`, `UvoznaKalkulacija`, EF Migracija `DodajRobnoIMaterijalno`) | ✅ |
| **4** | Osnovna sredstva | ⬜ |
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

## Sledeći koraci

Faze 3.5–3.12 su implementirane, commit-ovane i push-ovane na `origin/main` (05.08.2026).
Sledeći rad treba da krene od §3d ("Poznati nedostaci u Fazi 3.5–3.12") pre nego što se ide
dalje — najpre vizuelna provera novih ekrana (korisnik sam kroz UI, vidi §4), zatim čišćenje
legacy kolona/dugmadi u `KontaView`. Tek posle toga: Faza 4 (Osnovna sredstva) i Faza 5
(Obračun zarada).

