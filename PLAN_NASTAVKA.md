# 🧭 Plan nastavka razvoja — ERPi

> Radni dokument za nastavak posla u novoj sesiji. Prati fazni roadmap iz
> [`ANALIZA_I_PLAN.md`](ANALIZA_I_PLAN.md) i beleži šta je urađeno, šta je namerno odloženo
> i koje odluke ne treba poništavati bez razloga.
>
> Stanje na dan **05.08.2026** (dopunjeno u istoj sesiji sa Fazom 4, pa Ponude/Predračuni i
> Narudžbenice — vidi §6), verzija **2.0.0-alpha**.

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
| **4** | Osnovna sredstva | 🔶 (kompletan UI preneto, vidi §3h/§3j — ostaje DOS uvoz i pun F1 hub, ništa commit-ovano) |
| **5** | Obračun zarada — jedini modul sa realnim produkcionim korisnicima danas | 🔶 (u toku, vidi §3e) |
| **6** | Automatsko knjiženje (Zarade/Sredstva → Nalog) | ⬜ (šema već ima kuku: `Nalog.IzvorModula`/`IzvorId`) |
| **7.1** | `ERPiMigration` — direktan `ErpiFinansijeImporter` (uvoz iz `baza.db` / `AccountingDbContext` u `ErpiDbContext`) + `UvozWizardView` | ✅ |
| 7.2a | DOS import Zarade — `ZaradeDbfMigrator` (DBF → privremena ERPiZaradeData baza → `ErpiZaradeProdukcijaImporter`) + `PodesavanjaZaradeView` | ✅ (vidi §3f) |
| 7.2b | DOS import Finansije/Sredstva | ✅ (Sredstva vidi §3k; Finansije Robno/Materijalno dopunjeno na paritet sa ERPiFinansije u §3r) |
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
- **`NalogEditWindow` ima readonly/pregled režim za proknjižene naloge** (07.08.2026):
  `NalogEditWindow(db, nalog, isReadOnly, fokusRedniBroj)` — kad je `isReadOnly` true, naslov
  postaje "📖 Pregled proknjiženog naloga #X (Samo za čitanje)", polja/grid se zaključavaju,
  jedino aktivno dugme je "🔓 Rasknjiži i izmeni" (admin-only, YesNo potvrda, isti obrazac kao
  `NaloziView.BtnRasknjizi_Click`) — klikom se nalog rasknjiži i PROZOR SE NE ZATVARA, nego
  prelazi u editabilan režim (`PrebaciURezimIzmene()`), isto ponašanje kao ERPiFinansije.
  Aktivirano iz oba mesta koja otvaraju `NalogEditWindow`: `KarticaKontaView` (dupli-klik/desni-
  klik na stavku — `isReadOnly = nalog.IsKnjizen && samoPregled`) i `NaloziView.BtnIzmeniNalog_Click`
  (`isReadOnly = nalog.Status == StatusNaloga.Proknjizen`). **Ne poništavati ovu simetriju** —
  ako se doda treće mesto koje otvara `NalogEditWindow` na proknjižen nalog, mora proći kroz isti
  `isReadOnly` gate, ne direktno editabilno.

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
- **`ObracunskaJedinica`** (int na `Sredstvo`/`Kartica`/`Prijava`/`Rashod`) je ostala kao goli
  numerički kod, bez FK — u izvornom ERPiSredstva takođe nema svoj šifarnik/tabelu, pa nije bilo
  šta da se poveže. Ako se u budućnosti pokaže da "obračunska jedinica" treba da bude pravo mesto
  troška, to je nova modelska odluka (mapiranje na `Core.MestoTroska`), ne prost string→FK port.
- **Nijedan Sredstva ekran nije vizuelno proveren kroz UI** — isti razlog kao §3d/§3g za druge
  module (korisnik testira sam, vidi §4). Build je čist (`dotnet build ERPi.slnx`, 0 grešaka), EF
  migracija primenjena end-to-end na scratch bazi, 55/55 xUnit testova prolazi — ali dugme-po-dugme
  provera (posebno `PrijavaWindow`/`RashodWindow` transakciona knjiženja, `AmortizacijaPage`-ov
  poreski tab i novi `PopisPage`/`RevalorizacijaPage`/`IzvestajiPage` iz §3j ispod) nije urađena.
- **DOS uvoz Sredstava — urađeno, vidi §3k.** Ispostavilo se da izvor DA IMA produkcione DBF
  podatke (korisnik ih ima na disku), tako da prethodna pretpostavka "Sredstva nema produkcione
  podatke" iznad nije bila tačna — ista situacija kao Zarade (PSSS PIROT).
- **F1 Pomoc — samo per-dialog, ne pun hub.** `UpisPopisaWindow` (§3j) sada ima F1 help preko
  **deljenog** `ERPiApp.Views.Zarade.Pomoc.EditHelpWindow` (generička klasa, nula Zarade-specifične
  logike — namerno se NIJE napravila bit-za-bit identična kopija pod `Views/Sredstva/Pomoc`, to bi
  bila čista duplikacija). Pun `PomocPage`/`ChangelogWindow` hub (globalni F1, sadržajna stranica sa
  temama) i dalje nije prenet za Sredstva — isti opštiji nedostatak kao za Finansije (§3g) i kao
  Zaradin još-neožičeni globalni hub (§3e).

---

## 3j. Faza 4 (Osnovna sredstva) — Popis, Revalorizacija, Izveštaji portovani (05.08.2026, nastavak)

Preneta preostala tri UI ekrana iz `ERPiSredstvaApp` čiji su modeli/servisi/xUnit testovi već bili
preneti u ranijoj sesiji (§3h) — samo je UI sloj nedostajao:

- **`Views/Sredstva/Popis/`** — `PopisPage` (dva taba: Popisne liste + Komisije/članovi),
  `UpisPopisaWindow` (masovni unos stvarno popisanih količina, zaključivanje popisa),
  `Stampe/PopisIzvestajDocument` i `Stampe/PraznaPopisnaListaDocument` (PDF, `CoreFirma` obrazac
  isti kao Prijava/Rashod/Kartice). Ožičeno u sidebar kao "🗂️ Popis sredstava".
  - `PopisPage.SyncSredstvaSaKarticama()` prenet i **adaptiran na `KontoId` FK** (izvor je sinhronizovao
    string `Sredstvo.Konto`/`ObracunskaJedinica` iz poslednje `Kartica` pre generisanja popisa) —
    ovo NIJE bio legacy-only kod: `PrijavaWindow`/`RashodWindow` u ERPi upisuju `KontoId`/
    `ObracunskaJedinica` samo na `Kartica` zapis, nikad na sâm `Sredstvo` (vidi doc komentar na
    `Sredstvo.KontoId`), pa bez ove sinhronizacije `Sredstvo.KontoId` ostaje `null` zauvek i popis/
    izveštaji ne mogu da grupišu po kontu. Grupisanje u oba PDF dokumenta prebačeno sa string
    `Sredstvo.Konto` na `Sredstvo.Konto.BrojKonta` (FK navigacija).
  - Izvorni `UpisPopisaWindow.xaml`-ov `StaticResource OutlineButton` (ne postoji ni u izvornom
    `ERPiSredstvaApp/Resources/Styles.xaml`, već zabeleženo u §3h) zamenjen sa `SecondaryButton`.
  - F1 help na `UpisPopisaWindow` koristi **deljeni** `ERPiApp.Views.Zarade.Pomoc.EditHelpWindow`
    (generička klasa bez Zarade-specifične logike) — vidi napomenu u "Poznati nedostaci" iznad.
- **`Views/Sredstva/Revalorizacija/RevalorizacijaPage`** (obračun po godišnjem + 12 mesečnih
  koeficijenata, knjiženje efekta kao nova `Kartica` stavka, PDF `Stampe/RevalorizacijaDocument`,
  CSV export). Ožičeno kao "💹 Revalorizacija". `Kartica.KontoId`/`ObracunskaJedinica`/
  `AmortizacionaGrupa1/2` pri knjiženju preuzimaju se iz poslednje kartice sredstva (izvor je isto
  radio sa string `Konto`).
- **`Views/Sredstva/Izvestaji/IzvestajiPage`** (Popis svih sredstava, rekapitulacije po kontu/OJ/
  amortizacionoj grupi, CSV export). Ožičeno kao "📊 Izveštaji". **Ispravljen bag iz izvora**:
  izvorni ERPiSredstva je i "po kontu" i "po OJ" rekapitulaciju grupisao po istoj
  `AmortizacionaGrupa` (dead-end kod — "po OJ" je čak grupisao sve u jednu grupu `"1"`), verovatno
  nedovršen ekran. Ovde grupisanje stvarno koristi `Sredstvo.Konto.BrojKonta` (FK) i
  `Sredstvo.ObracunskaJedinica` (postojeće polje, ranije nekorišćeno u ovom izveštaju) — moguće jer
  ERPi ima prave FK/popunjena polja koja izvor nije imao, ne slepo kopiranje bug-a.
  - Lokalni `ReportNavButton` stil dodat u `IzvestajiPage.xaml`-ov `Page.Resources` (izvorni
    `NavButton` iz `ERPiSredstvaApp/Resources/Styles.xaml` je pravljen za tamni app-sidebar, ne
    uklapa se u svetlu `Card` pozadinu ovde) — namerno lokalni, ne dodat u `App.xaml` jer ga
    nijedan drugi ekran ne koristi.

Nije potrebna nova EF migracija — `Popisi`/`PopisneStavke`/`Komisije`/`ClanoviKomisije` DbSet-ovi i
migracija već postoje iz ranije sesije (§3h). `dotnet build ERPi.slnx` čist, 0 grešaka.
**Nije commit-ovano.** Nijedan od tri nova ekrana nije vizuelno proveren kroz UI (korisnik testira
sam, vidi §4).

Sa ovim, Faza 4 nema više poznatih UI-nedostataka iz originalnog ERPiSredstva osim punog F1 Pomoc
huba (opštiji nedostatak, vidi iznad) — DOS uvoz je urađen u nastavku iste sesije (§3k). Portovanje
iz ERPiSredstva se može smatrati završenim za sve module koji imaju gotov servisni sloj.

---

## 3m. SEF/PFR podešavanja + e-Fiskalizacija + REST API/Web Dashboard u `PodesavanjaView` (05.08.2026, nastavak)

Faza 3.4 je ranije označena ✅ jer `SefService.PosaljiNaSefAsync` (koristi se iz Komercijala/
`RacunOtpremnicaView`) je bio potpuno ožičen na pravi SEF API (`SefApiClient` + `SefUblGenerator`,
čita `Firma.SefApiKey`/`SefEnvironment`) — ali **ekran na kome se taj ključ zapravo unosi nikad
nije bio prenet**: `ERPiApp/Views/Shell/PodesavanjaView` je imao samo Info traku i "O aplikaciji",
bez ijednog SEF polja. `SefService`-ova greška ("Idite u Podešavanja -> SEF e-Fakture...") je
pokazivala na ekran koji nije postojao. Primećeno kad je korisnik pokazao odgovarajući tab u
izvornom `ERPiFinansije/ERPiFinansijeApp/Views/Podesavanja/PodesavanjaView`. Urađeno u dva
poteza istog dana: prvo samo SEF tab, pa je korisnik na "proveri da li i ostalo postoji u
ERPiFinansije" rekao "Može" — pa je preneto i e-Fiskalizacija i REST API/Web Dashboard.

`PodesavanjaView` je pretvoren iz jedne kolone u `TabControl` sa **četiri** taba:

1. **🔧 Opšte** — stari sadržaj (Info traka, O aplikaciji) + novo: toggle "🖥️ Pokreni preko
   celog ekrana" (`AppConfig.StartMaximized`, JSON-perzistovano isto kao `PrikaziInfoTraku`,
   default `true`) — čita ga `MainWindow`-ov konstruktor (`WindowState = AppConfig.StartMaximized
   ? Maximized : Normal`). Prenet iz izvornog `UserSettings.StartMaximized`, jedino polje iz
   `UserSettings` koje je u samom izvoru stvarno na nešto uticalo (`MainWindow.xaml.cs` tamo)
   — ostala UserSettings polja (`NazivServisa`/`OvlascenoLice`/`PotvrdaZaRasknjizavanje`/
   `PotvrdaZaBrisanje`) su, provereno grep-om kroz `ERPiFinansijeApp`, upisana ali **nigde
   pročitana** čak ni u samom izvoru (mrtva polja, verovatno pripremljena za PDF zaglavlje/
   potvrdne dijaloge koji nikad nisu implementirani) — namerno NISU prenesena, nema smisla
   portovati mrtvu formu. `AutoBackupFrequency`/`CustomBackupFolder`/`LastAutoBackupDate` JESU
   žive u izvoru (auto-backup pri startu, `BackupView`), ali pripadaju ERPi-jevom već postojećem
   zasebnom "Rezervne kopije" ekranu, ne ovom tabu — nije diran.
2. **⚡ SEF e-Fakture** — API ključ/okruženje/JBKJS/email, čita/piše `Firma` red preko
   `_db.Firme` (polja već postojala, nema migracije). "Testiraj SEF Konekciju" zove postojeći
   `SefApiClient.TestConnectionAsync()`.
3. **🧾 e-Fiskalizacija (PFR)** — novo. Portovano iz `ERPiFinansijeData/Services/PfrApiClient.cs`:
   - [`ERPiData/Services/PfrApiClient.cs`](ERPiData/Services/PfrApiClient.cs) — `PfrApiClient`
     (HTTP klijent ka lokalnom LPFR/VPFR servisu) + DTO-i `PfrPostavke`/`PfrZahtev`/
     `PfrZahtevStavka`/`PfrZahtevPlacanje`/`PfrOdgovor`, 1:1 protokol iz izvora (izvor ih je držao
     u `Models/EsirModels.cs`, ovde su uz klijenta u `Services/` — nema drugog potrošača modela).
   - [`ERPiData/Services/PfrService.cs`](ERPiData/Services/PfrService.cs) — novo, orkestrira
     `PfrApiClient` nad `PfrRacun` zapisima (izvor nije imao ekvivalentan servisni sloj jer je
     `PfrRacuniView` tamo pozivao `PfrApiClient` direktno iz code-behind-a). Bitna razlika od
     izvora: ERPi-jev `PfrRacun` je namerno pojednostavljen (nema stavke po artiklu kao izvorni
     `FiskalniRacunLog`), pa `PfrService` gradi `PfrZahtev` kao **jednu stavku** koja nosi ukupan
     `Iznos` — dovoljno da se račun izda, nedovoljno za PDV razrez po stopi po stavci ako PFR to
     ikad zatraži.
   - `PfrRacuniView.BtnFiskalizuj_Click` (`Views/SefPfr/`) više NE piše lažan `PfrBroj`/QR lokalno
     — sada zove `PfrService.FiskalizujRacunAsync`, pravi HTTP poziv, upisuje pravi odgovor
     (ili SIMULACIJA status ako je `Firma.PfrSimulatorMod` uključen i PFR nije dostupan).
   - Podešavanja tab: PFR URL/PAC kod/ime kasira/simulator-mod checkbox, čita/piše iste
     `Firma.PfrUrl`/`PfrPacKod`/`PfrKasirName`/`PfrSimulatorMod` kolone koje su već postojale
     (portovane u ranijoj Fazi 3.4, ali dotad ničim čitane/pisane iz UI-ja). "Testiraj PFR
     Konekciju" zove `PfrApiClient.TestirajPfrKonekcijuAsync()` direktno sa poljima iz forme.
4. **🌐 REST API i Web Dashboard** — novo. Portovano iz
   `ERPiFinansijeData/Services/AccountingWebServer.cs` →
   [`ERPiData/Services/ErpiWebServer.cs`](ERPiData/Services/ErpiWebServer.cs) — **ispravka
   ranije pretpostavke** iz prve verzije ove beleške: izvor NIJE ASP.NET Kestrel host, nego čist
   `System.Net.HttpListener` (ugrađen u .NET, bez novog NuGet paketa) — port je bio mnogo manji
   posao nego prvobitno procenjeno. Endpoint-i `/api/status`, `/api/dashboard` (prihodi/rashodi/
   broj naloga/partnera/artikala tekuće godine), `/api/partneri`, i default ruta vraća
   samostalnu HTML5 dashboard stranicu (Tailwind CDN, poll na 10s) — token u
   `Authorization: Bearer` ili `?token=` query, `CryptographicOperations.FixedTimeEquals`
   poređenje. Prilagođeno šemi: `StavkaNaloga.BrojKonta` (string u izvoru) → navigacija
   `s.Konto.BrojKonta` (FK); `Nalog.IsKnjizen`/`RacunOtpremnica.IsKnjizen` su `[NotMapped]`
   računate osobine u ERPi šemi pa se NE koriste u LINQ `Where` pre materijalizacije (za razliku
   od par postojećih mesta u ERPi-ju, npr. `MestaTroskaService`/`PdvService`, koja to rade i nisu
   proverena da li se stvarno prevode u SQL ili bi pukla — nije ovom sesijom diran taj rizik,
   samo izbegnut u novom kodu) — umesto toga upit direktno poredi `n.Status == StatusNaloga.
  Proknjizen`, garantovano prevodivo. Izvorov `Serilog.Log.Error` pozivi su izostavljeni (ERPiApp
   nema Serilog uveden) — greške se lokalno gutaju/vraćaju kao HTTP 500, bez logovanja na disk.
   - Podešavanja tab: port (default 5050), status (🟢/🔴 sa klikabilnim linkom kad radi), token
     polje (samo za čitanje), dugmad Pokreni/Zaustavi/Otvori u pregledaču — 1:1 UX kao izvor.
   - Server se NE gasi eksplicitno pri zatvaranju `MainWindow`-a (izvor to isto ne radi — oslanja
     se na to da se `HttpListener` zatvori sa gašenjem procesa). Ako se ovo ikad pokaže kao
     problem (npr. port ostaje "zauzet" posle pada aplikacije), dodati `Closed += (_,_) =>
     ErpiWebServer.Stop();` u `MainWindow`.

**Namerno i dalje NIJE preneto** ("Trim, don't transplant whole"):
- **Rezervna kopija** kao tab — ERPi već ima zaseban sidebar ekran za backup.
- **Uvoz iz starog programa** kao tab — ERPi već ima `UvozWizardView` na drugom mestu u sidebar-u.
- Mrtva `UserSettings` polja (`NazivServisa`/`OvlascenoLice`/potvrdni dijalozi) — vidi obrazloženje
  gore uz tab 1.

**Nije vizuelno provereno kroz UI** (korisnik testira sam, vidi §4) — `dotnet build ERPi.slnx` je
čist (0 grešaka) za sve dodate/izmenjene fajlove. Posebno neprovereno: da li lokalni PFR servis
(ili njegov nedostatak + simulator mod) i `HttpListener` na portu 5050 rade bez sukoba sa
firewall-om/postojećim procesima na ovoj mašini — prva stvarna proba je na korisniku.

---

## 3k. Faza 7.2b — DOS uvoz Sredstava (05.08.2026, nastavak)

Korisnik je javio da su Popis/Revalorizacija/Izveštaji (§3j) prazni čak i posle uvoza — uzrok:
`UvozWizardView` uvozi samo Finansije/Zarade, Sredstva nikad nije imalo nijedan uvozni put.
Korisnik je potvrdio da postoji stvarna (ne samo test) DOS baza za Sredstva na disku, pa
pretpostavka u §3h ("Sredstva nema produkcione podatke") **nije bila tačna** — ista situacija kao
Zarade (PSSS PIROT).

Korisnik je izričito tražio DOS uvoz (ne EF-to-EF uvoz iz žive `ERPiSredstvaApp` instalacije) —
isti dvostepeni obrazac kao `ZaradeDbfMigrator`/`ErpiZaradeProdukcijaImporter` (Faza 7.2a, §3f):
DBF → privremena `ERPiSredstvaData` (`SredstvaDbContext`) SQLite baza → EF-to-EF u `ErpiDbContext`.
EF-to-EF stage nije izložen kao zaseban "uvoz iz postojeće ERPiSredstvaApp instalacije" korisnički
put (za razliku od Zarade, koja ima obe kartice) — samo je interni plumbing koji DOS uvoz poziva.

**Dodato:**
- `ERPiMigration/Importers/SredstvaDbfMigrator.cs` — 1:1 port `ERPiSredstvaMigration/Program.cs`
  (konzolni alat) svedene na pozivnu `MigrateAsync(dbfDir, sqliteDb, log)` metodu, isti obrazac kao
  `ZaradeDbfMigrator`. Čita `SREDSTVA.DBF`/`KARTICA.DBF`/`RASHOD.DBF`/`PRIJAVA.DBF`/`KONTPLAN.DBF`/
  `KORISNIC.DBF` (cp852 encoding) u svežu `SredstvaDbContext` bazu. `KORISNIC.DBF` (deljeni registar
  firmi u originalnom DOS rasporedu, jedan nivo iznad `KOR**` foldera) se traži i u izabranom
  folderu i u roditeljskom — robusnije od izvora koji je imao hardkodovanu apsolutnu putanju.
- `ERPiMigration/Importers/ErpiSredstvaProdukcijaImporter.cs` — EF-to-EF druga faza (isti obrazac
  kao `ErpiZaradeProdukcijaImporter`): `Konto` (string, sve tri tabele: `Sredstvo`/`Kartica`/
  `Prijava`) razrešava se u `KontoId` FK, auto-kreira `Konto` ako broj ne postoji (isti obrazac kao
  `ErpiFinansijeImporter`); izvorni zaseban `Dobavljac` model postaje `Partner` (`JeDobavljac =
  true`, `SifraPartnera = "SR-DOB-{Konto}"` stabilan dedup ključ) — ERPi namerno nije preneo
  Dobavljac kao zaseban entitet (§3h odluka). Dedup: `Sredstvo` po `InventarskiBroj` (nema DB
  unique indeks), `Kartica`/`Prijava`/`Rashod` po (dest `SredstvoId`/`BrojNaloga`, `RedBroj`),
  `Komisija` po (`Naziv`, `DatumKreiranja`), `Popis` po (`Godina`, dest `KomisijaId`,
  `DatumPopisa`) — isti stil kao Zarade importer (HashSet pre-check pre svakog batch-a).
- `ERPiApp/Views/Sredstva/Podesavanja/PodesavanjaSredstvaView` — nova UI, ožičena kao "⚙️
  Podešavanja" pod novom PODEŠAVANJA sekcijom Sredstva sidebar-a. Samo jedna kartica (DOS uvoz,
  folder-picker + log panel) — za razliku od `PodesavanjaZaradeView` NEMA karticu "Uvoz iz
  postojeće instalacije" (korisnikova odluka, vidi gore).
- `ERPiMigration.csproj` — dodata `ProjectReference` ka `ERPiSredstvaData.csproj` (bila je
  nedostajala; verzije EF Core/DbfDataReader se poklapaju sa ostatkom `ERPiMigration`-a, nije
  bio potreban version pin kao kod Zarade DbfDataReader-a u Fazi 7.2a).

**Odluka koju ne treba poništavati**: EF-to-EF `ErpiSredstvaProdukcijaImporter` postoji SAMO kao
plumbing za DOS uvoz, ne kao zaseban dugme/korisnički put — korisnik je to eksplicitno tražio
("Ne treba mi importer iz ERPiSredstva"). Ako se ikad ipak zatraži direktan uvoz iz žive
`ERPiSredstvaApp` SQLite instalacije (analogno Zarade drugoj kartici), importer je već spreman,
treba samo dodati UI karticu.

**Nije testirano sa stvarnim DBF fajlovima** (isti status kao Zarade DOS uvoz u §3f — testiran je
samo EF-to-EF put suvim modelima, ne i čitanje pravih DBF fajlova) — `dotnet build ERPi.slnx` čist,
0 grešaka/upozorenja. Korisnik ima realne DBF podatke na disku; sledeći test treba da pokrene DOS
uvoz kroz UI protiv njih.

**Uzgredni nalaz o Zarade DOS uvozu**: korisnik je pitao da li nam "slično treba i za Zarade" —
provereno, `PodesavanjaZaradeView` VEĆ ima identičan dvostepeni DOS uvoz obrazac (folder-picker +
`ZaradeDbfMigrator` + `ErpiZaradeProdukcijaImporter`, Faza 7.2a) potpuno ožičen i dostupan u UI-ju
(Zarade sidebar → Podešavanja). Nije bilo šta da se dodaje — ako "slično" znači da i Zarade DOS
uvoz treba stvarno isproban sa pravim DBF fajlovima (§3f već beleži da nikad nije testiran), to
ostaje otvoreno kao zaseban zadatak, ne kao nedostajuća funkcionalnost.

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
| Ponude & Predračuni | ✅ portovano (05.08.2026, nova sesija) — vidi dole |
| Narudžbenice Dobavljačima | ✅ portovano (05.08.2026, nova sesija) — vidi dole |
| **Računopolagači** | ✅ **nije stvarni nedostatak** — u izvoru je ovo isti `Magacin` šifarnik (`DgRacunopolagaci` binduje `SifraMagacina`/`NazivMagacina`/`OdgovornoLice`/`VrstaMagacina`, `LoadRacunopolagace()` čita `db.Magacini`), samo drugačije nazvan tab u istom ekranu. ERPi već ima identične kolone (uključujući `OdgovornoLice`) u `MagaciniView` (tab "Šifarnik magacina" u `MagacinMainView`) — nema šta dodatno da se portuje, eventualno samo dodati alias/tooltip ako korisnik želi da i ERPi ima tab pod imenom "Računopolagači".
| Šifarnik artikala | ✅ `ArtikliView` |
| Poreske tarife | ✅ portovano (05.08.2026, iste sesije) — `PoreskaTarifa` model + `PoreskeTarifeView`/`PoreskaTarifaEditWindow`, tab u `MagacinMainView`, migracija `DodajPoreskeTarife` verifikovana na scratch bazi |
| **Zaduženja** / **Razduženja** / **Primopredaje** (Robno) | ✅ portovano (05.08.2026, treća sesija) — vidi §3j |
| Kalkulacije | ✅ `KalkulacijeView` |
| Robne kartice | ✅ portovano (05.08.2026, treća sesija) — `RobneKarticeView`, vidi §3j |
| Robni Bruto bilans | ✅ portovano (05.08.2026, treća sesija) — `RobniBrutoBilansView`, servis je već postojao ali ekran ne, vidi §3j |

**Ponude & Predračuni / Narudžbenice Dobavljačima — portovano (05.08.2026, nova sesija).**
Izvor (`ERPiFinansijeData/Models/KomercijalaModels.cs`) je imao zaseban `PonudaPredracun`/
`NarudzbenicaDobavljacu` model (ne `VrstaDokumenta` na zajedničkom entitetu) — isti obrazac
prenet u `ERPiData/Models/Magacin/PonudaPredracun.cs` i `NarudzbenicaDobavljacu.cs`, sa
`SifraArtikla`/cache-ovan `NazivPartnera`/`NazivDobavljaca` pretvorenim u prave FK-ove
(`ArtikalId`, `PartnerId`) po `import-from-source-apps` pravilu (vidi §2). Servis:
`ERPiData/Services/KomercijalaService.cs` (CRUD + `PretvoriPonuduURacunAsync`/
`PretvoriNarudzbenicuUKalkulacijuAsync`, 1:1 logika iz izvora). UI: `PonudeView`/
`PonudaEditWindow` i `NarudzbeniceView`/`NarudzbenicaEditWindow` u `ERPiApp/Views/Magacin`, kao
dva nova taba u `MagacinMainView` (ispred "Ulazne kalkulacije"). Editor stavki koristi
`DataGridComboBoxColumn` artikal-piker + DTO model sa računatim `IznosNeto/Pdv/Bruto`
propertijima (isti obrazac kao `KalkulacijaEditWindow`), ne izvornu WrapPanel
"unesi pa dodaj red" traku — laganiji, doslednije sa ostatkom ERPi-ja.
Migracija `DodajPonudeNarudzbenice`, verifikovana end-to-end na scratch bazi.
Arhitekturna razlika od izvora: `NarudzbenicaDobavljacu` u ERPi ima i `MagacinId` (magacin
prijema) — ERPi-jev `Kalkulacija` je magacinski vezan (nenullable `MagacinId`), izvorni
ERPiFinansije `Kalkulacija` to nije imao, pa konverzija narudžbenice u kalkulaciju zahteva
magacin unapred izabran na samoj narudžbenici (ne postoji "podrazumevani magacin" fallback).
`PretvoriNarudzbenicuUKalkulacijuAsync` takođe zahteva da sve stavke imaju izabran artikal
(bez toga vraća `Success=false` sa objašnjenjem, ne baca izuzetak).
Nije vizuelno provereno kroz UI (korisnik testira sam, vidi §4) — build je čist (0 CS grešaka;
poslednji `dotnet build ERPi.slnx` u ovoj sesiji je pukao samo na kopiranju `ERPiData.dll/.pdb`
jer je `ERPiApp` bio pod debug-erom u drugoj sesiji, ne na kompajliranju), migracija primenjena
end-to-end na scratch bazi, ali dugme-po-dugme provera CRUD-a i konverzija nije urađena.

**Poreske tarife** — portovano (vidi tabelu gore), takođe nije vizuelno provereno (isti razlog).

## 3j. Zatvaranje §3i liste — Zaduženja/Razduženja/Primopredaje (Robno), Robne kartice, Robni Bruto Bilans, Robno Dashboard (05.08.2026, treća sesija istog dana, na zahtev korisnika uz oba screenshot-a iz §3i)

Korisnik je ponovo pokazao ista dva screenshot-a (izvorni 13-tabni `TrgovinaView` naspram
`MagacinMainView`-a) i zatražio da se zatvori ostatak liste, plus da se portuje i Robna
"Radna tabla" (dashboard) sa svojom meni stavkom. Sve niže je **novo u ovoj sesiji, build čist
(`dotnet build ERPi.slnx` 0 grešaka/0 upozorenja), migracija verifikovana end-to-end na scratch
bazi, 55/55 xUnit testova prolazi** — ali **ništa nije vizuelno provereno kroz UI** (korisnik
testira sam, vidi §4).

- **Nov model `RobnoKretanjeNalog`/`RobnoKretanjeStavka`** (`ERPiData/Models/Magacin/RobnoKretanje.cs`,
  migracija `DodajRobnoKretanje`) — namerno ODVOJEN od `PrimopredajaNalog` (koji ostaje
  Materijalno/`MaterijalId`-bazirano, §3g odluka). Ima `ArtikalId` FK i `VrstaDokumenta`
  diskriminator (`VrstaRobnogKretanja.Primopredaja/Zaduzenje/Razduzenje` — vrednosti
  "Primopredaja"/"Zaduženje"/"Razduženje") — isti obrazac kao izvorni `TrgovinaView`, jedna
  tabela/tri filtrirana taba, ne tri skoro-identična ekrana.
- **`RobnoKretanjeService`** (`ERPiData/Services/RobnoKretanjeService.cs`) — 1:1 struktura sa
  `PrimopredajaService` (CRUD, `KnjiziKretanjeAsync`/`RasknjiziKretanjeAsync` sa VP↔MP PDV
  prelaznim nalogom preko `RobnaKonta`), ali knjiži preko **iste** `MaterijalnaKarticaService`
  instance (njeni `DodajUlazRedAsync`/`DodajIzlazRedAsync`/`UkloniPoslednjiRedAsync` su
  string-ključni, generički nad `MaterijalnaKartica` tabelom — ta tabela je knjigovodstveno
  zajednička za Robno i Materijalno, isto kao što `RobniBrutoBilansService` već dokazuje
  filtriranjem `robaMap`/`materijalMap`). Nije trebalo nova "robna kartica" tabela.
- **UI**: `RobnoKretanjeEditWindow` (editor, parametrizovan `vrsta` stringom) + `RobnoKretanjaView`
  (lista sa "Svi/Proknjiženi/Neproknjiženi" filterom + master-detail stavke, takođe
  parametrizovana `vrsta`-om) — jedan par ekrana pokriva sve tri nove tabove. `RadioButton`
  "Svi" IsChecked se namerno postavlja u `Loaded`, ne XAML literalu (isti gotcha kao §2).
  Ožičeno kao 3 nova taba u `MagacinMainView` ("🔄 Primopredaje", "📥 Zaduženja", "📤 Razduženja").
- **`RobniBrutoBilansView`** (`ERPiApp/Views/Magacin/RobniBrutoBilansView.xaml`) — servisni sloj
  (`RobniBrutoBilansService.GetRobniBrutoBilansAsync`) je već postojao od Faze 3.12/3g, ali
  ekran nikad nije bio napisan (§3i-ova "pretpostavlja se ✅" je bila netačna pretpostavka —
  provereno `grep` da `RobniBrutoBilansView` klasa ranije nije postojala nigde u `ERPiApp`).
  Filter po magacinu/datumu/pretrazi, isti izgled kao izvorni `TrgovinaView`-ov tab. Novi tab
  u `MagacinMainView` ("📊 Robni Bruto bilans").
- **`RobneKarticeView`** (`ERPiApp/Views/Magacin/RobneKarticeView.xaml`) — master-detail
  (magacin + artikal lista levo, hronologija kartice desno), analogan izvornom `TrgovinaView`
  tabu "Robne kartice". Čita `MaterijalneKartice` tabelu DIREKTNO (ne preko
  `MaterijalnaKarticaService`, da se izbegne uvoz Materijal-specifičnog servisa u Robni ekran —
  vidi komentar u fajlu). Novi tab u `MagacinMainView` ("📇 Robne kartice"). Materijalna strana
  i dalje nema svoj pandan (`MaterijalneKarticeView` ostaje otvoren nedostatak, §3g/§3i).
- **`RobnoDashboardView`** (`ERPiApp/Views/Magacin/RobnoDashboardView.xaml`) — "Radna tabla"
  Robnog knjigovodstva, port iz `ERPiFinansijeApp/Views/Trgovina/RobnoDashboardView`, isti
  obrazac kao već postojeći `MaterijalnoDashboardView` (KPI karte vrednosti zaliha VP/MP preko
  `RobniBrutoBilansService.GetRobniBrutoBilansAsync` + `Magacin.VrstaMagacina` grupisanje,
  poslednjih 8 kalkulacija/nivelacija, top 10 artikala, brze akcije). Razlika od izvora: brza
  akcija "Nova kalkulacija (MP)" NIJE dodata — `MaloprodajneKalkulacijeView` još nema
  create-dijalog (samo knjiži/rasknjiži postojećih), pa referenciranje nepostojećeg prozora
  nije uvedeno; "Nova kalkulacija (VP)"/"Nova nivelacija"/"Nova otpremnica"/"Nova primopredaja"
  (Robno) rade. Nova top-level nav stavka `BtnRobnoDashboard` ("📊 Radna tabla") u sekciji
  "ROBNO KNJIGOVODSTVO", ispred postojećeg `BtnMagacin`, isti raspored kao Materijalna sekcija
  (`BtnMaterijalno`/`BtnMaterijalnoSkladiste`) — ta sekcijska podela sidebar-a je zatečena
  već urađena od strane druge/paralelne sesije u međuvremenu (`BtnMaterijalnoSkladiste`+
  `MaterijalnoSkladisteView` su se pojavili u `MainWindow.xaml(.cs)` kao tuđa promena, nisu
  dirani, samo iskorišćeni kao obrazac za novo dugme).
- **NAPOMENA korisniku (nije bug, samo objašnjenje "prazne tabele")**: korisnik je usput
  prijavio da su Ulazne kalkulacije/Nivelacije/MP/Uvozne kalkulacije prazne dok Artikli/Magacini
  imaju podatke — ovo je OČEKIVANO, ne kvar: `ErpiFinansijeImporter` (Faza 7.1) uvozi samo
  "osnovne entitete" (Konta/Partneri/Magacini/Artikli/Nalozi/Stavke/Kalkulacije), Robno-
  materijalna dokumenta (Nivelacije/MP/Uvozne kalkulacije/Primopredaje/itd.) **nisu deo uvoza**
  (vidi §3d) — prazne su jer nikad nisu migrirane iz stare baze, ne zato što ekran ne radi.
  Ako korisnik želi tu istoriju u ERPi, sledeći korak je proširenje `ErpiFinansijeImporter`-a
  da uvozi i Robno-materijalna dokumenta, ne popravka ovih ekrana.

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
- ~~**Putni nalozi** — `IzvozZaZaradeWindow` (izvoz putnih naloga u Zarade) i
  `PutniNalogEditWindow` (zaseban dijalog izmene) nemaju pandan.~~ Oboje sad postoje u ERPi
  (`PutniNalogEditWindow` je preneto u međuvremenu; `IzvozZaZaradeWindow` urađeno u §3v).
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
    - `⚙️ Napredni filter` je do sada bio samo klonirano dugme bez funkcije (`TxtPretraga.Focus()` stub) — dovršeno 05.08.2026: portovan `NaprednaPretragaWindow`/`NapredniFilterCriteria` iz ERPiFinansije (`Views/Shared`) u `ERPiApp/Views/Finansije/Shared/`, prilagođen `ErpiDbContext`-u i pravim FK-ovima (`Konto`/`Partner` na `StavkaNaloga`, umesto string `BrojKonta`); ne otvara sopstvenu SQLite konekciju kao izvor, deli već otvoren `_db`. Filtrira po rasponu datuma/iznosa, broju naloga/opisu, kontu, partneru i statusu knjiženja, kombinovano sa postojećom pretragom/radio dugmadima; dugme menja boju u `DarkOrange` kad je filter aktivan (originalna boja se sad čuva iz XAML-a pri konstrukciji, ne hardkoduje se `PrimaryLightBrush` kao u izvoru). `Trgovina`-kontekst (Kalkulacije) iz ERPiFinansije nije prenet — ERPi nema odgovarajući ekran s istim dugmetom još. **Nije vizuelno provereno kroz UI** (isti razlog kao ostali novoportovani ekrani, korisnik testira sam).
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

**Faza 4 (Osnovna sredstva) je sada 🔶 — kompletan UI preneto I DOS uvoz dodat** (registar, kartice,
prijava, rashod, amortizacija + poreska amortizacija/Obrazac OA iz §3h; Popis, Revalorizacija,
Izveštaji iz §3j; DOS/DBF uvoz — `SredstvaDbfMigrator`/`ErpiSredstvaProdukcijaImporter`/
`PodesavanjaSredstvaView` — iz §3k, sve u nastavku iste sesije). Ostaje samo pun F1 Pomoc hub
(opštiji nedostatak, isti kao Finansije/Zarade) — portovanje iz ERPiSredstva se može smatrati
završenim. **Nije commit-ovano** — celo stablo `ERPiData/Models/Sredstva`,
`ERPiData/Services/Sredstva`, `ERPiApp/Views/Sredstva`, `ERPiMigration/Importers/SredstvaDbfMigrator.cs`
+ `ErpiSredstvaProdukcijaImporter.cs`, migracija `DodajOsnovnaSredstva` i prateća migracija
`PdvZapisRacunOtpremnicaSefPolja` (pre-postojeća neprimenjena šema izdvojena u sopstvenu migraciju
pri generisanju — videti §3h) su i dalje untracked/modified u `git status`; ne commit-ovati dok
korisnik ne kaže.

Preporučeni redosled sledećeg rada (bilo koji redosled je razuman, ovo je samo predlog):
1. **Korisnik pokrene DOS uvoz** (§3k) protiv pravih DBF fajlova i vizuelno proveri Fazu 4 kroz UI
   (registar → prijava → kartica → amortizacija → rashod → popis → revalorizacija → izveštaji, tim
   redosledom prati zavisnost podataka) pre nego što se nastavi dalje na tom modulu.
2. §3g ("Preporučeni redosled sledećeg rada" za Finansije): Robno-materijalno ostatak servisa/
   ekrana, PDV evidencija, Korisnici/prava pristupa.
3. Faza 5 (Obračun zarada, već u toku — vidi §3e) i Faza 6 (automatsko knjiženje Zarade/Sredstva →
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

---

## 6. Sesija 05.08.2026 (nastavak) — Ponude & Predračuni + Narudžbenice Dobavljačima

Nastavak §5-ove liste, korisnikov odabrani redosled (§3i, §5 stavka 1): **Ponude & Predračuni**
i **Narudžbenice Dobavljačima** portovani u ovoj sesiji.

**Urađeno:**
- `ERPiData/Models/Magacin/PonudaPredracun.cs` (`PonudaPredracun` + `PonudaStavka`) i
  `NarudzbenicaDobavljacu.cs` (`NarudzbenicaDobavljacu` + `NarudzbenicaStavka`) — port iz
  `ERPiFinansijeData/Models/KomercijalaModels.cs`, sa `SifraArtikla`→`ArtikalId` i cache-ovan
  `NazivPartnera`/`NazivDobavljaca`→`PartnerId` navigacijom pretvorenim u prave FK-ove.
- `ERPiData/Services/KomercijalaService.cs` — CRUD za oba dokumenta + 1-klik konverzije
  `PretvoriPonuduURacunAsync` (→ `RacunOtpremnica`) i `PretvoriNarudzbenicuUKalkulacijuAsync`
  (→ `Kalkulacija`), 1:1 logika iz izvornog `KomercijalaService`.
- `ERPiApp/Views/Magacin/PonudeView`+`PonudaEditWindow` i
  `NarudzbeniceView`+`NarudzbenicaEditWindow` — dva nova taba u `MagacinMainView`, ispred
  "Ulazne kalkulacije". Editor stavki: `DataGridComboBoxColumn` artikal-piker + DTO model sa
  računatim `IznosNeto/Pdv/Bruto` propertijima (isti obrazac kao `KalkulacijaEditWindow`), ne
  izvorna WrapPanel "unesi pa dodaj red" traka.
- Migracija `DodajPonudeNarudzbenice`, verifikovana end-to-end na scratch bazi (17. u nizu).

**Arhitekturna razlika od izvora**: `NarudzbenicaDobavljacu` u ERPi ima i `MagacinId` (magacin
prijema robe) — polje koje izvor nema, jer je ERPi-jev `Kalkulacija` magacinski vezan
(nenullable `MagacinId`), a izvorni ERPiFinansije `Kalkulacija` nije. Bez izabranog magacina na
narudžbenici, `PretvoriNarudzbenicuUKalkulacijuAsync` vraća `Success=false` s objašnjenjem
("izaberite magacin prijema") umesto da nagađa podrazumevani magacin ili baci izuzetak — isto
tako i za stavke bez izabranog artikla.

**Nije vizuelno provereno kroz UI** (korisnik testira sam, vidi §4) — build je čist (0 CS
grešaka na celom `ERPi.slnx`; poslednji build u ovoj sesiji je pukao samo na kopiranju
`ERPiData.dll`/`.pdb` jer je `ERPiApp` bio pokrenut pod debug-erom u drugoj sesiji u trenutku
provere — nije problem u kodu, samo file lock), migracija primenjena end-to-end na scratch
bazi, ali CRUD dugme-po-dugme provera i obe konverzije (u Račun / u Kalkulaciju) nisu
urađene. **Nije commit-ovano.**

**Sledeće na redu** (§3i, poslednja preostala stavka sa Robno tab-po-tab revizije):
**Zaduženja/Razduženja** — najsloženiji od tri, zahteva nov Robno/Artikal-bazirani model
(npr. `RobnoInternoKretanje`/`RobnaStavkaKretanja` sa `ArtikalId` FK, `MagacinIdDaje`/
`MagacinIdPrima`, `VrstaDokumenta` diskriminator) jer izvorna Robna `PrimopredajaNalog` (koju
Zaduženje/Razduženje dele preko `VrstaDokumenta` filtera) NIJE ista tabela kao ERPi-jev već
portovan Materijalni `PrimopredajaNalog` (`MaterijalId`-baziran, §3g odluka) — puno objašnjenje
i predlog modela u §3i tabeli iznad, red "Zaduženja/Razduženja".

---

## 7. Sesija 05.08.2026 (nastavak) — Materijalno: "Skladište i Zalihe" (puni `MagacinView` port) + Radna tabla meni stavka

Korisnik je pokazao screenshot izvornog `ERPiFinansijeApp`-ovog `MagacinView` (6 tabova:
Šifrarnik materijala, Ulazi, Trebovanja, Primopredaje, Kartice materijala, Bruto bilans
materijalnog knjigovodstva) i javio da ERPi-jev Materijalno meni nema nijedan tab — tačno,
`BtnMaterijalno` je do sada bio jedina stavka pod "MATERIJALNO KNJIGOVODSTVO" i otvarala je
direktno `MaterijalnoDashboardView` (radna tabla sa poslednjih 8 ulaza/trebovanja + brze akcije,
§3g), bez pune tabelarne istorije sa filterima — baš ta praznina koju je §3g/§3h već ranije
imenovao kao "Puna tabelarna lista ... nije portovana".

**Urađeno u ovoj sesiji (portovan pun `MagacinView`, NIJE commit-ovano):**
- `ERPiApp/Views/Magacin/MaterijalnoSkladisteView.xaml(.cs)` — novi ekran, 6 tabova 1:1 sa
  izvorom (Šifrarnik materijala / Ulazi / Trebovanja / Primopredaje / Kartice materijala / Bruto
  bilans materijalnog knjigovodstva). Deli već otvoren `ErpiDbContext` (konstruktor), ne otvara
  sopstvenu konekciju kao izvor. Ulazi/Trebovanja/Primopredaje master-detail gridovi binduju
  prave FK navigacione property-je (`Magacin.NazivMagacina`, `Materijal.Naziv`,
  `MagacinDaje`/`MagacinPrima`) — izvor je to radio ručnim spajanjem preko string šifara u
  code-behind-u jer njegove stavke nisu imale FK; ERPi-jeve već imaju (§3g odluka), pa je taj
  ceo sloj koda ovde nepotreban.
- `ERPiApp/Views/Magacin/MaterijalEditWindow.xaml(.cs)` — CRUD dijalog šifarnika materijala
  (Šifra/Naziv/JM/Pakovanje), isti obrazac kao postojeći `ArtikalEditWindow`.
- `ERPiApp/Views/Magacin/ProveraKarticaWindow.xaml(.cs)` — prozor za prikaz redova materijalne
  kartice sa negativnim stanjem/cenom (dugme "⚠️" na tabu Kartice materijala); §3g ga je već
  imenovao kao nedostajući ("`ProveraKarticaWindow` — servisni sloj postoji, ekran ne" — servis
  `MaterijalnaKarticaService.GetNegativnaStanjaAsync()` je već postojao, samo ekran nije).
- Sidebar (`MainWindow.xaml`): `BtnMaterijalno` preimenovan iz "🏭 Ulazi i Trebovanja" u
  "📊 Radna tabla" (i dalje otvara `MaterijalnoDashboardView`, ponašanje nepromenjeno), dodata
  nova stavka `BtnMaterijalnoSkladiste` "🏭 Skladište i Zalihe" → `NavMaterijalnoSkladiste_Click`
  → `MaterijalnoSkladisteView(_db)`. Sad MATERIJALNO KNJIGOVODSTVO ima dve stavke kao ROBNO
  KNJIGOVODSTVO sekcija (radna tabla + glavni hub), po izričitom zahtevu korisnika ("sa meni
  stavkom za Radnu tablu").

**Namerno trimovano od izvora ("Trim, don't transplant whole"):**
- **Nema PDF štampu (🖨️)** ni na jednom od 6 tabova — ERPiApp još nema `PdfReportService`
  metode za šifarnik materijala, ulaz/trebovanje/primopredaja nalog, materijalnu karticu ni
  bruto bilans materijala (izvorne `GenerisiSifrarnikMaterijalaPdf`/`GenerisiUlazPdf`/
  `GenerisiTrebovanjePdf`/`GenerisiPrimopredajuPdf`/`GenerisiMaterijalnuKarticuPdf`/
  `GenerisiSveMaterijalneKarticePdf`/`GenerisiRobniBrutoBilansPdf`/
  `GenerisiProveruMaterijalnihKarticaPdf` nisu portovane). Svaki tab umesto toga ima Excel izvoz
  (već postojeći opšti `ExcelExportService.ExportDataGridToExcel`, isti obrazac kao ostatak
  `ERPiApp`-a). Isti opštiji nedostatak kao kod skoro svih ostalih novoportovanih ekrana (§3b/§3g).
- **Tab "Kartice materijala" nema čekiranje/multi-select ni desni-klik kontekstni meni** — u
  izvoru je ta mašinerija (`MaterijalIzbor.IsSelected`, `ChkSviArtikli` tri-state header
  checkbox, `LstArtikli_PreviewMouseRightButtonDown`) postojala isključivo da nahrani grupnu PDF
  štampu više kartica odjednom (`BtnStampajKarticu_Click` grana za >1 čekiran materijal); pošto
  PDF štampa nije portovana (gore), ta mašinerija nema svrhu — tab je sad običan single-select
  grid. `MaterijalIzbor` DTO klasa je zadržana (radi lakšeg budućeg vraćanja checkbox-a kad PDF
  štampa stigne), ali joj je `IsSelected`/`PropertyChanged` neiskorišćen u ovoj verziji.
- **Toolbar dugmad su ikona-samo + `ToolTip`** (`➕`/`✏️`/`🗑️`/`✅`/`🔄`/`⚠️`, `IconButtonStyle`),
  ne ikona+tekst kao u izvoru — po standardnom obrascu ovog projekta (vidi
  `import-from-source-apps` skill i korisnikovu memoriju o UI stilu), iako par novijih ekrana
  (`NarudzbeniceView`, `PonudeView`, `RacuniOtpremniceView`) taj obrazac nije dosledno pratilo —
  primećeno ovde kao odstupanje vredno ispravke u tim ekranima kad im dođe red.
- **Rasknjiženje Ulaza/Trebovanja/Primopredaje je admin-only** (`AppSession.IsAdministrator`
  gate pre `RasknjiziXAsync` poziva) — 1:1 isto ponašanje kao izvor, prvi put iskorišćeno u ovom
  delu `ERPiApp`-a (dosadašnji Materijalno ekrani, `UlazEditWindow`/`TrebovanjeEditWindow`/
  `PrimopredajaEditWindow`, samo blokiraju izmenu proknjiženog naloga bez ponude za
  rasknjiženje — ta grana je živela isključivo u starom, sad zamenjenom `MagacinView`-u iz
  izvora, pa je vraćena ovde gde joj je i mesto).

**Build**: `dotnet build ERPi.slnx` čist za sve fajlove iz ove sesije (jedina preostala greška u
istom prolazu je u `RobnoKretanjaView.xaml.cs` — tuđ, paralelan rad na §3i "Zaduženja/Razduženja"
stavci, van dometa ove sesije).

**Nije vizuelno provereno kroz UI** (korisnik testira sam, vidi §4) — ni novi ekran ni
preimenovana/dodata sidebar stavka nisu klikom provereni. **Nije commit-ovano.**

---

## 3l. Sredstva — "Radna tabla" (KPI + grafikoni), 05.08.2026, nastavak

Korisnik je tražio radnu tablu za Sredstva modul "kao u ERPiSredstva" — izvorni
`ERPiSredstvaApp/Views/Dashboard/DashboardPage(.xaml.cs)`/`DashboardViewModel` nije bio prenet
(§3h je to izričito zabeležio kao namernu odluku, jer ERPi ima svoj `Shell/DashboardView`, ali taj
je Finansije-fokusiran, ne Sredstva).

Dodato (`ERPiApp/Views/Sredstva/Dashboard/`), isti "Radna tabla" naziv/raspored kao Finansije i
Robno/Materijalno knjigovodstvo (§ iznad):
- `SredstvaDashboardPage.xaml(.cs)` + `SredstvaDashboardViewModel.cs` — 1:1 raspored iz izvora
  (3 KPI kartice: ukupno sredstava/nabavna/sadašnja vrednost; grafikoni: Top 5 najvrednijih
  aktivnih sredstava — horizontalni bar, Status sredstava — donut, Vrednost po kontima Top 10 —
  pie). Konstruktor prima deljeni `ErpiDbContext` (isti obrazac kao ostale Sredstva stranice), ne
  samostalni `SredstvaDbContext` iz izvora.
  - Razlika od izvora: "Vrednost po kontima" grupiše po `Sredstvo.KontoId` → `Konto.BrojKonta`
    (prava FK navigacija, učitana kao rečnik unapred da se izbegne N+1 jer `Include` nije
    podešen), ne po string koloni `Sredstvo.Konto` — isti string→FK obrazac kao svuda drugde
    (vidi §2).
- Sidebar (`MainWindow.xaml`): nova prva stavka `BtnSredstvaDashboard` "📊 Radna tabla" u
  `PnlNavSredstva`, iznad postojeće "OSNOVNA SREDSTVA" grupe. `TabModulSredstva_Click` (klik na
  modul-tab u vrhu) sad otvara radnu tablu kao landing ekran (bilo je direktno
  `SredstvaPage`/Registar) — isti obrazac kao `TabModulFinansije_Click` → `DashboardView`.
- Sve potrebne `App.xaml` resurse (`Card`, `SurfaceBrush`, `PrimaryBrush`, `TextSecondaryBrush`,
  `TextPrimaryBrush`, `SuccessBrush`) ERPi već ima iz Faze 4 (§3h) — nije trebalo ništa dodavati.

**Build**: `dotnet build ERPi.slnx` čist za sav novi/izmenjeni kod (samo predikuduće Zarade
upozorenja, nepovezano); finalni copy-to-output korak je odbio da zameni `ERPiApp.exe` jer je
korisnikova instanca bila pokrenuta u trenutku build-a (zaključan fajl) — isto kao poznata
napomena u §3k, ne greška u kodu.

**Nije vizuelno provereno kroz UI** (korisnik testira sam, vidi §4). **Nije commit-ovano.**

---

## 3n. Bag: ERPi registar Sredstava se ne slaže sa ERPiSredstva (05.08.2026, nastavak)

Korisnik je pokazao dva screenshot-a (PSSS PIROT firma) — ERPi i ERPiSredstva prikazuju iste
šifre/nazive sredstava, ali potpuno različite Nabavna/Ispravka/Sadašnja vrednost (npr. "Upravna
zgrada": ERPi 515.154,90 vs. ERPiSredstva 879.628,24).

**Uzrok nađen upoređivanjem baza direktno (sqlite3) — nije bag u računici amortizacije**:
`AmortizacijaCalculator.cs` je bit-za-bit identičan u oba projekta (ERPiData/Services/Sredstva vs.
ERPiSredstvaData/Services), i `SredstvaKartice`/`Kartice` (istorija promena po sredstvu — Pocetno
stanje, Redovan otpis po godini, Revalorizacija po godini) su **identične** u obe baze, red po red,
za period 2001–2025. Problem je u `SredstvaDbfMigrator.cs` (§3k, DOS uvoz): zbirna polja na samom
`Sredstvo` redu (`NabavnaVrednost`/`IspravkaVrednosti`/`SadasnjaVrednost`) su uzimana direktno iz
`SREDSTVA.DBF`-ovih `NABAVNA`/`OTPISANA` kolona, a te kolone u ovom DBF izvoru nose **snimak
početnog stanja** (obično 2001), ne tekuće stanje — tekuće stanje se dobija tek akumulacijom cele
istorije iz `KARTICA.DBF` (isti model kao `AmortizacijaCalculator`: svaka kartica je delta na
prethodno stanje). Rezultat: `Sredstvo` red u ERPi je ostajao zaglavljen na 2001. vrednosti, iako
je `Kartice`/`SredstvaKartice` tabela imala punu ispravnu istoriju. `ErpiSredstvaProdukcijaImporter`
(EF-to-EF druga faza) samo kopira već pogrešnu vrednost iz privremene baze, nije on uzrok.

**Ispravljeno**: `SredstvaDbfMigrator.MigrateAsync` posle uvoza `KARTICA.DBF` sad rekalkuliše
`Sredstvo.NabavnaVrednost`/`IspravkaVrednosti`/`SadasnjaVrednost` kao sumu svih pripadajućih
`Kartica` redova (samo za sredstva koja imaju bar jednu karticu — sredstva bez istorije zadržavaju
sirovu DBF vrednost). Primenjuje se na sledeći DOS uvoz.

**Popravljeni i postojeći podaci**: `firma_100188310_PSSS_PIROT_DOO_PIROT.db` (jedina baza sa
razilaženjem — `ARHIBEL_ARHIBEL_doo_Pirot.db` je proverena, 0 razilaženja) je repair-ovana istim
UPDATE-om direktno nad `Sredstva`/`SredstvaKartice`, posle backup-a
(`firma_100188310_PSSS_PIROT_DOO_PIROT_backup_pre_sredstva_fix_20260805_234434.db` u istom Baze
folderu). Ukupni zbirovi (Nabavna 20.792.222,30 / Ispravka 13.754.792,15 / Sadašnja 7.037.430,15)
sad se poklapaju tačno sa ERPiSredstva.

**Nije commit-ovano** (samo `SredstvaDbfMigrator.cs` izmenjen, baze su lokalni podaci van repoa).

---

## 3o. Klonirana korisnička uputstva iz sva tri programa u ERPi (06.08.2026)

Uspešno klonirana i objedinjena celokupna korisnička uputstva i help sistem iz sva tri programa (`ERPiFinansije`, `ERPiSredstva`, `ERPiZarade`) u novi, deljeni `ERPiApp.Views.Pomoc` modul:

1. **Objedinjena stranica pomoći (`PomocPage`)**:
   - Spojeno preko 60 detaljnih tema pomoći iz sva 3 programa podeljenih po modulima (`💰 Finansije`, `🏗️ Osnovna sredstva`, `👥 Zarade`, `🌐 Opšte`).
   - Pretraga tema u realnom vremenu i filtriranje po modulima.
   - Dugme `🌐 Otvori HTML Uputstvo` za direktan otvor celovitih HTML priručnika u veb pregledaču.
   - Generički `EditHelpWindow` pop-up i `ChangelogWindow`.
2. **HTML Korisnički priručnici (`Resources/Help/`)**:
   - `uputstvo-finansije.html`, `uputstvo-sredstva.html`, `uputstvo-zarade.html` i objedinjeni `uputstvo-erpi.html` priručnik.
3. **Povezivanje u Shell / MainWindow**:
   - Nova stavka `❓ Pomoć & Uputstva` u bočnom meniju i globalni rukovalac tastera **F1** koji otvara ekran pomoći.

---

## 3p. Finansijski izveštaji — Dnevnik glavne knjige, Zaključni list, Vrednovanje zaliha, Bilansi (APR) hub + PB-1/PDP/OA (06.08.2026)

Korisnik je poredio ERPi sidebar sa ERPiFinansije-inim `IzvestajiView`/`BilansiView` kartica-po-kartica
i tražio da se preneseno ono što nedostaje. Preneseno u ovoj sesiji, **nije commit-ovano**:

- **`Views/Finansije/Izvestaji/DnevnikGlavneKnjigeView`** (novo) — hronološki pregled svih
  proknjiženih naloga, stavka po stavka (port iz `IzvestajiView`-ove "📖 Dnevnik glavne knjige"
  kartice + `DnevnikPreviewWindow`), na ERPi-jev obrazac pune stranice umesto zasebnog preview
  prozora. PDF (`Stampe/DnevnikGlavneKnjigeDocument`, QuestPDF) + Excel export, oba icon-only
  dugmad (`IconButtonStyle`) po standardnom obrascu projekta.
- **`Views/Finansije/Izvestaji/ZakljucniListView`** (novo) — totali prometa po sintetičkim
  kontima za period; podaci iz već-portovanog `BrutoBilansService.GetZakljucniListAsync`
  (Faza 3.6) — samo je UI ekran nedostajao, servisni sloj je već postojao neiskorišćen. PDF
  (`Stampe/ZakljucniListDocument`) + Excel export.
- **"📦 Vrednovanje zaliha"** — NIJE novi ekran: to je funkcionalno identično već-postojećem
  `Views/Magacin/RobniBrutoBilansView` (isti `RobniBrutoBilansService.GetRobniBrutoBilansAsync`
  poziva i ERPiFinansije-ina "Vrednovanje zaliha" kartica). Dodat samo drugi nav ulaz iz
  Finansije sekcije (`NavVrednovanjeZaliha_Click`) koji otvara isti `RobniBrutoBilansView(_db)` —
  ne duplirati ekran ako se ovde ponovo dođe.
- **`BrutoBilansView`-ovo "🖨️ PDF" dugme je bilo mrtav kod** — generisalo je putanju fajla i
  prikazivalo poruku o uspehu, ali nikad nije pozvalo `GeneratePdf`. Ispravljeno pravim PDF-om
  (`Stampe/BrutoBilansDocument`, QuestPDF) — prvi pravi PDF export za ovaj ekran otkad je
  portovan u Fazi 3.6.
- **`BilansStanjaView`/`BilansUspehaView` (dva odvojena nav ekrana) OBRISANI, zamenjeni jednim
  `Views/Finansije/Bilansi/BilansiAprView`** (5 tabova: Bilans Stanja, Bilans Uspeha, Statistički
  izveštaj (SI), Cash Flow, Promene na kapitalu) — 1:1 port ERPiFinansije-inog `BilansiView`
  hub ekrana, po eksplicitnom traženju korisnika ("umesto da je odvojeno... u stvari bolje da
  preuzmemo ceo ovaj meni"). Bilans Stanja/Uspeha tabovi koriste već-portovani `BilansService`
  (isti kao obrisani ekrani); SI/CashFlow/Promene na kapitalu koriste već-portovani
  `AprProsireniIzvestajiService` (Faza 3.6 ✅ ga je već pomenula u zagradi, ali dotad nijedan UI
  ekran nije pozivao ta tri metoda — servisni sloj je čekao neiskorišćen). PDF export dodat za
  Stanja/Uspeha tabove (`Stampe/BilansPozicijeDocument`, deljen za oba jer dele isti
  `BilansPozicija` oblik reda); SI/CashFlow/Kapital imaju samo Excel export (isto kao izvor).
- **`PoreskiBilansWindow`** (novo, iz `BilansiAprView`-ovog "📜 Poreski Bilans" dugmeta) —
  Obrasci PB-1 (usklađivanje poreske dobiti), OA (poreska amortizacija po grupama I-V) i PDP
  (poreska prijava). **Napomena**: `PoreskiBilansService`/`PoreskiBilansModels.cs` su otkriveni
  kao VEĆ portovani u ranijoj sesiji (commit `3491df9`, pre ove) — servisni sloj je već postojao
  neiskorišćen, isti obrazac kao `AprProsireniIzvestajiService` gore. Ova sesija je prvo greškom
  prepisala oba fajla bez prethodnog čitanja (skoro identična, samo doc-komentari drugačiji) —
  primećeno po `git status` pokazujući "M" umesto "??" na fajlovima za koje se očekivalo da su
  novi, ispravljeno sa `git checkout --` da se vrati originalni, već-tačan sadržaj. **Pouka**:
  pre `Write`-a fajla čiji naziv zvuči kao da bi već mogao postojati u ERPi, prvo `Grep`/`Glob`
  potvrditi da stvarno ne postoji, ne osloniti se samo na sećanje iz ranijeg dela sesije. Jedino
  je `PoreskiBilansWindow` (UI ekran) stvarno nov — servis je nedirnut, koristi se kakav jeste.
  **Preneta napomena iz izvora bez izmene**: Obrazac OA koristi ilustrativne/hardkodovane
  nabavne vrednosti po poreskoj grupi (npr. "I grupa = 5.000.000 RSD"), NE stvarna sredstva iz
  Faze 4 (Osnovna sredstva) — pravo povezivanje sa registrom sredstava i njihovim amortizacionim
  grupama je poznati nedostatak, nije nešto što je ova sesija pogoršala ili trebalo da reši.
- Svi novi ekrani koriste `IconButtonStyle` (icon-only + `ToolTip`) za akcije, ne
  `ActionButtonStyle` (icon+tekst) kao izvorni ERPiFinansije ekrani — standardni obrazac ovog
  projekta (vidi §2 gore).

`dotnet build ERPi.slnx` čist, 0 grešaka. **Nijedan od ovih ekrana nije vizuelno proveren kroz
UI** (isti razlog kao §3d — korisnik testira sam, vidi §4) — posebno `BilansiAprView`-ov
Poreski Bilans dugme i `DnevnikGlavneKnjigeView`/`ZakljucniListView`-ov PDF export nisu
pokrenuti dugme-po-dugme, samo je build proveren.

---

## 3q. DOS uvoz fix (Konta duplikati), Sredstva DOS uvoz reskin, Podaci o firmi, podrazumevana lozinka (06.08.2026)

- **Bag: DOS uvoz padao na "UNIQUE constraint failed: Konta.BrojKonta"** — `DosImportService`-ov
  KONTPLAN.DBF prolaz je svaki red upisivao u privremenu bazu bez provere duplikata, a
  `Konto.BrojKonta` ima UNIQUE indeks; stari DOS/Clipper kontni planovi znaju da nose dupliran
  broj konta. Ispravljeno dedup-om po `BrojKonta` (prvo pojavljivanje ostaje) u
  [DosImportService.cs](ERPiApp/Services/Finansije/DosImportService.cs).
- **Sredstva DOS uvoz je skrivao pravi uzrok greške** (`ex.Message` bez `ex.InnerException`) —
  ispravljeno da prikazuje i unutrašnju poruku, isti obrazac kao Finansije DOS uvoz.
- **Nov ekran `Views/Sredstva/Podesavanja/SredstvaDosImportWindow`** — Sredstva DOS uvoz sad
  izgleda kao Finansije `DosImportWindow` (skeniranje radnog direktorijuma, lista DOS firmi iz
  KORISNIC.DBF, log), bez checkbox-ova za module (Sredstva ima samo jedan fiksni skup tabela).
  Dodatno: bira se odredište — **aktivna firma** (kao ranije) ili **nova firma** (kreira novu
  ERPi bazu + `Firma` red iz DOS podataka, registruje je u `CompanyRegistryService`, isti put
  kao `NovaFirmaWindow`). `PodesavanjaSredstvaView` sad samo otvara ovaj dijalog.
- **Poznat, NAMERNO neodrađen nedostatak DOS uvoza (Robno)** — **ispravljeno u §3r (isti dan,
  nastavak sesije)**, videti tamo za pun opis. (Istorijska napomena, tačna u trenutku pisanja:
  DOS uvoz je uvozio samo Robni šifarnik — Magacini + Artikli — a ne i transakcione dokumente;
  mapping funkcije su postojale ali nisu bile pozvane niti je postojala merge logika u
  `ErpiFinansijeImporter`.)
- **Nov tab "🏢 Podaci o firmi" u `PodesavanjaView`** — port ERPiFinansije-inog "Izmena firme"
  ekrana (Šifra/Naziv/PIB/Matični broj/Adresa/PTT i Mesto/Telefon/Žiro račun + readonly putanja
  baze), prvi tab u redosledu. `NovaFirmaWindow` je isto dopunjen istim poljima (ranije je imao
  samo Naziv/Šifra/PIB/Matični broj) — nova i postojeća firma sad imaju isti skup podataka.
- **Podrazumevana lozinka promenjena sa `admin123` na `admin`** — nova migracija
  `PromeniPodrazumevanuLozinkuNaAdmin` (PBKDF2 hash generisan van app-a, verifikovan da tačno
  odgovara "admin" a ne staroj vrednosti). **Namerno NIJE `migrationBuilder.UpdateData`**
  (bezuslovan UPDATE) — to bi vratilo na podrazumevanu lozinku i firme gde je admin već
  promenio svoju pravu lozinku. Umesto toga `migrationBuilder.Sql(...)` sa `WHERE LozinkaHash =
  '<stari hash>'` — dira samo nalog koji i dalje ima staru podrazumevanu lozinku. Provereno na
  scratch bazi u oba scenarija (svež seed → dobija novi hash; već-promenjena lozinka → ostaje
  netaknuta). Sve tri UI reference na "admin123" (`LoginWindow` prefill+provera, `MainWindow`
  upozorenje, `PodesavanjaView` opis toggle-a) ažurirane na "admin".
- **Greška ove sesije**: `PoreskiBilansService.cs`/`PoreskiBilansModels.cs` su greškom prepisani
  bez prethodnog čitanja (već postojali, ispravljeno `git checkout --`, vidi §3p) — ponovljena
  pouka, proveriti `Grep`/`Glob` pre `Write`-a fajla za koji "zvuči" da bi mogao već postojati.

## 3r. DOS uvoz Finansije — Robno/Materijalno dopunjeno na paritet sa ERPiFinansije (06.08.2026, nastavak)

Korisnik je posle testiranja uvoza za "ARHIBEL" primetio: "uvezao je samo artikle materijale, a
treba sve zivo" — potvrđuje nedostatak zabeležen u §3q. Upoređen [DosImportService.cs](ERPiApp/Services/Finansije/DosImportService.cs)
(unified ERPi) sa referentnim `ERPiFinansijeApp/Services/DosImportService.cs` (ERPiFinansije,
puna verzija): unified je čitao samo 6 DBF-ova (KONTPLAN, ANKONT, NALOG, MAGACIN, ARTIKLI,
M_SIFR), referenca čita 16 vrsta.

**Dopunjeno, raspoređeno po postojećim checkbox modulima u `DosImportWindow`:**
- **Finansijsko**: PROMENE.DBF → `promeneMap` (in-memory, prosleđuje se u `MapNalogGrupa` da
  popuni `Opis` stavki naloga; ERPi šema nema zaseban `Promena` model jer se šifre razlikuju po
  firmi — vidi napomenu u `ERPiFinansijeData.Models.Promena`, pa se ne čuva kao deljeni rečnik).
- **Robno** (uz postojeće Magacini/Artikli): TARIFE.DBF (`PoreskeTarife`), KALKULAC.DBF+KAL_NAL.DBF
  (`Kalkulacije`+stavke — ovo je bio i najveći gap: `ErpiFinansijeImporter` je već imao merge kod
  za Kalkulacije iz Faze 7.1, ali `DosImportService` nikad nije čitao KALKULAC.DBF u temp bazu, pa
  je uvek bilo 0), MALKULAC.DBF+MAL_NAL.DBF (`MaloprodajneKalkulacije`), RAC_OTP.DBF+RAC_POD.DBF
  (`RacuniOtpremnice`), NIV_NAL.DBF+P_M_NIV.DBF (`NivelacijeCena`).
- **Materijalno** (uz postojeći M_SIFR): MAT_KART.DBF+M_KART.DBF (`MaterijalneKartice`),
  ULAZ.DBF (`UlazNalozi`), TREBOV.DBF (`TrebovanjeNalozi`), MAT_NAL.DBF+ZADUZ.DBF+RAZDUZ.DBF
  (`PrimopredajaNalozi`) — svrstano pod Materijalno a ne Robno jer unified `UlazStavka`/
  `TrebovanjeStavka`/`PrimopredajaStavka` imaju `MaterijalId` FK (ne `ArtikalId`), vidi doc-komentar
  na tim modelima ("Materijalno (ne Robno) knjigovodstvo").

**`ErpiFinansijeImporter.ImportFromDatabaseAsync`** dopunjen sa 9 novih koraka (7–15) koji prenose
gorenavedene tabele iz temp `AccountingDbContext` u aktivnu `ErpiDbContext`, sa dedup-om po
prirodnom ključu (isti obrazac kao postojeći Konta/Kalkulacije koraci):
- Materijali (dedup po `SifraArtikla`) — takođe je bio prisutan bag: M_SIFR se uvozio u temp bazu
  ali se NIKAD nije prenosio u `destDb.Materijali` (importer koraci 1–6 iz Faze 7.1 ga nisu
  dodirivali) — sad ima svoj korak i `materijaliDict` koji koriste Ulaz/Trebovanje/Primopredaja.
- Poreske tarife (dedup po `TarifniBroj`), Materijalne kartice (dedup po tuple
  `SifraMagacina+SifraArtikla+RedniBroj`, istorijski zapisi bez FK-a po dizajnu).
- Ulazi/Trebovanja/Primopredaje: `SifraMagacina`/`SifraArtikla` string kodovi iz temp baze
  (ERPiFinansijeData model ih čuva kao plain string, ne FK) prevedeni na `MagacinId`/`MaterijalId`
  preko `magaciniDict`/`materijaliDict` (isti string→FK obrazac iz `import-from-source-apps`
  skill-a). Dedup po broju naloga (Primopredaja dodatno po `VrstaDokumenta+BrojNaloga` jer
  Primopredaja/Zaduženje/Razduženje dele brojevnu sekvencu).
- Maloprodajne kalkulacije: isti string→FK obrazac (`SifraMagacinaPrima/Daje`, `SifraDobavljaca`,
  stavke `SifraArtikla`), dedup po `(BrojKalkulacije, MagacinIdPrima)`.
- **Računi-Otpremnice i Nivelacije cena su poseban slučaj**: izvorni `ERPiFinansijeData.RacunOtpremnica`/
  `NivelacijaCena` model (za razliku od Kalkulacija/Maloprodajnih kalkulacija) već čuva prave
  `int? MagacinId`/`ArtikalId` FK-ove — ali oni pokazuju na temp bazu SVOJIH Magacina/Artikala, ne
  na `destDb`. Prevod ide u dva koraka: temp `MagacinId`/`ArtikalId` → `Sifra` (učitano iz temp
  `srcDb.Magacini`/`srcDb.Artikli` u `srcMagaciniByIdTemp`/`srcArtikliByIdTemp` mape) → `destDb`
  `MagacinId`/`ArtikalId` (preko `magaciniDict`/`artikliDict`). `RacunOtpremnicaStavka.SifraArtikla`
  je `[NotMapped]` na izvornom modelu pa NE preživljava `AsNoTracking().ToListAsync()` re-query —
  otud ova dvostepena šema umesto direktnog čitanja stringa sa stavke.
- `PartnerId` na Računima-Otpremnicama namerno ostaje `null` (izvorni `MapRacunOtpremnice` ga
  nikad ne popunjava iz DBF-a — trim scope, isto kao u referentnoj ERPiFinansije verziji).

**Build**: `dotnet build ERPiMigration/ERPiMigration.csproj` čisto (0 grešaka). `dotnet build
ERPiApp/ERPiApp.csproj` — 0 `CS####` grešaka (grep potvrđen), jedina greška je MSB3027/MSB3021
zaključavanje `.exe`/`.pdb` fajlova od strane žive `ERPiApp`/`netcoredbg` instance koju korisnik
ima pokrenutu — očekivano, ne compile bag.

**Korisnik je odmah testirao i naišao na dva bug-a u istoj sesiji, oba ispravljena:**

1. **Crash**: uvoz samo Robnog modula za "ARHIBEL" (33 DOS firme, `C:\FIRMEARHSTO\Radni`) pukao
   na 72%, odmah posle "Uvezeno 8 poreskih tarifa" — `System.ArgumentException: An item with the
   same key has already been added. Key: 12005`. Uzrok: `magaciniMapTemp`/`artikliMapTemp` u
   `DosImportService` (nove mape uvedene u ovoj sesiji, koriste se za Kalkulacije/Računi-otpremnice/
   Nivelacije) su građene direktno preko `ToDictionaryAsync(a => a.SifraArtikla, ...)`, a ARTIKLI.DBF
   nosi duplirane šifre artikala (isti obrazac bug-a kao KONTPLAN.DBF u §3q, samo za Artikle —
   Artikal/Magacin dosad nikad nisu bili targetirani dictionary-jem pa se nije primetilo). Ispravljeno
   na dva mesta: (a) dedup pri upisu u temp bazu (`vidjeneSifreArtikala`/`vidjeneSifreMagacina`
   HashSet, isti obrazac kao postojeći `vidjeniBrojeviKonta` za Kontni plan — prvo pojavljivanje
   ostaje, log prijavljuje broj preskočenih duplikata), (b) `magaciniMapTemp`/`artikliMapTemp` sad
   grade se preko `GroupBy(...).ToDictionary(g => g.Key, g => g.First()...)` kao odbrana u dubinu.
2. **Gubitak podataka usled crash-a**: pošto je korisnik štiklirao "Obriši postojeće podatke" a
   brisanje je do sad bilo PRVI korak (commit-uje se odmah, van bilo kakve transakcije), crash iz
   bug-a (1) je značio da su Robni podaci (Magacini/Artikli/Kalkulacije) u aktivnoj `ARHIBEL` bazi
   OBRISANI a nikad ponovo napunjeni — `ErpiFinansijeImporter.ImportFromDatabaseAsync` poziv se
   nalazi POSLE mesta gde je pucalo. Ispravljeno pomeranjem celog brisanja (i za sva tri modula) sa
   početka metode na mesto neposredno PRE poziva `ErpiFinansijeImporter`-a — tj. tek kad je
   privremena baza već uspešno popunjena iz svih izabranih DBF-ova. Ako čitanje/mapiranje DBF-a
   ponovo pukne, brisanje se sad nikad ne izvrši i aktivna baza ostaje netaknuta. Usput dopunjene
   DELETE liste za Robno/Materijalno (ranije su brisale samo staru petorku
   Kalkulacije/StavkeKalkulacije/Artikli/Magacini/RobnaKretanja — sad brišu i sve novo ožičene
   tabele iz ove sesije: RacuniOtpremnice/-Stavke, NivelacijeCena/-Stavke, MaloprodajneKalkulacije/
   -Stavke, PoreskeTarife za Robno; UlazNalozi/-Stavke, TrebovanjeNalozi/-Stavke, PrimopredajaNalozi/
   -Stavke, MaterijalneKartice za Materijalno) — inače bi drugi "čist re-import" posle ove sesije
   ostavljao duple/osirotele zapise u tim tabelama.
   **Poznato preostalo ograničenje** (nije rešeno, uska ivična situacija): ako korisnik uveze SAMO
   Robno sa "Obriši postojeće" dok već postoje Materijalno dokumenti (Ulaz/Trebovanje/Primopredaja)
   koji referenciraju Magacine, brisanje Magacina ih ostavlja sa "obesenim" `MagacinId`-jem (FK je
   isključen tokom brisanja pa SQLite ne prijavljuje grešku) — rešenje bi zahtevalo ili zajedničko
   obuhvatanje oba modula u brisanju ili brojanje referenci; odloženo, retka kombinacija.

**Build ponovo proveren posle oba fix-a**: `dotnet build ERPi.slnx` — "Build succeeded", 0
`CS####` grešaka. **Nije testirano sa stvarnim DBF fajlovima od strane Claude-a** (korisnik sam
testira UI, vidi [[feedback_user_tests_ui_manually]]) — sledeći korak je da korisnik ponovi uvoz
za "ARHIBEL" (restartovati app da pokupi build) i proveri da li se sad pojavljuju podaci u
Kalkulacijama/Nivelacijama/Računima-otpremnicama/Ulazu/Trebovanju/Primopredaji, i da Robni podaci
(Magacini/Artikli) koji su obrisani u pukom pokušaju sad ponovo postoje posle uspešnog uvoza.

---

## 3s. Zarade — "Radna tabla" (KPI + grafikon) + port dizajna tabela iz ERPiZarade (06.08.2026)

Korisnik je tražio da se za Zarade modul preuzme "Radna tabla" (kao kod Finansije/Sredstva, §3l) i
dizajn tabela iz ERPiZarade — Zarade module u ERPi je dosad koristio prazan podrazumevani WPF
izgled `DataGrid`-a (nikad nije bio ni pomenut kao namerno odložen, prosto propušten pri portovanju
Faze 5).

**Radna tabla** (`ERPiApp/Views/Zarade/Dashboard/`), isti obrazac kao susedna §3l stranica:
- `DashboardPage.xaml(.cs)` + `DashboardViewModel.cs` — 1:1 port iz
  `ERPiZaradeApp/Views/Dashboard/DashboardPage(.xaml.cs)`/`DashboardViewModel.cs` (4 KPI kartice:
  aktivnih radnika / ukupna neto masa / ukupna bruto masa / aktivnih kredita; jedan grafikon —
  pregled zarada po mesecima za izabranu godinu, kombinovani bar+bar+linija preko LiveChartsCore,
  već referenciran u `.csproj`). Razlika od izvora: `DashboardViewModel` koristi deljeni
  `ErpiDbContext.Create(AppConfig.DbPath)` umesto samostalnog `PlataDbContext` — isti
  `DbSet`/nazivi polja (`Radnici`, `ObracuniPlata`, `Krediti`), 1:1 poklapanje, nije trebalo
  string→FK transformaciju kao kod Sredstva/Robno.
- Sidebar (`MainWindow.xaml`): nova prva stavka `BtnZaradeDashboard` "📊 Radna tabla" u
  `PnlNavZarade`, iznad postojeće "OBRAČUNI" grupe. `TabModulZarade_Click` sad otvara radnu tablu
  kao landing ekran (bilo je direktno `ObracuniPage`) — isti obrazac kao
  `TabModulFinansije_Click`/`TabModulSredstva_Click`.

**Dizajn tabela** (`ERPiApp/Views/Zarade/ZaradeStyles.xaml`, nov fajl, isti obrazac kao
`Views/Sredstva/SredstvaStyles.xaml`):
- Implicitni `DataGrid`/`DataGridColumnHeader` stilovi 1:1 preneti iz
  `ERPiZaradeApp/Resources/Styles.xaml` (bela pozadina, samo horizontalne linije, naizmenične
  redove `#F9FAFB`, header `#F3F4F6` centriran/wrap, `RowHeight=36`/`ColumnHeaderHeight=50`) —
  oslanjaju se na `BorderBrush`/`TextSecondaryBrush` koje ERPi već ima globalno u `App.xaml`, nije
  trebalo dupliranje boja.
- Merge-ovano u svih 29 Zarade `.xaml` fajlova (23 `Page` + 6 `Window`) koji sadrže `<DataGrid`
  (`../ZaradeStyles.xaml` iz svakog, jedan nivo ispod `Views/Zarade/`) — urađeno skriptovano
  (Python regex, ne ručno), pošto neki fajlovi već imaju `Page.Resources`/`Window.Resources` blok
  (obavijeni u `ResourceDictionary`+`MergedDictionaries` da se sačuvaju postojeći konvertori/stilovi
  kao npr. `RadniciPage`/`ObracunPage`), a neki nemaju nikakav (dodat nov blok). Provereno posle:
  tačno jedna referenca `ZaradeStyles.xaml` po fajlu, nijedan promašaj/duplikat.
  `Krediti/KreditiPage.xaml` već je imao lokalni `DataGrid.Style` sa
  `BasedOn="{StaticResource {x:Type DataGrid}}"` koji je ranije (bez merge-a) tiho padao na OS
  podrazumevani izgled — sad ispravno nasleđuje novi implicitni stil.

**Build**: `dotnet build ERPi.slnx` čist, 0 upozorenja/0 grešaka. **Nije vizuelno provereno kroz
UI** (korisnik testira sam, vidi [[feedback_user_tests_ui_manually]]). **Nije commit-ovano.**

---

## 3t. Automatska provera ažuriranja + verzija na prvom ekranu + živi dijalog "Istorija izmena" (06.08.2026, nastavak)

Korisnik je tražio da se doda automatski update "kao i kod ove tri app" — ERPiFinansije/
ERPiSredstva/ERPiZarade pri pokretanju proveravaju GitHub releases (Velopack) i nude
preuzimanje/instalaciju u jednom kliku; ERPi je do sad imao samo `VelopackApp.Build().Run()`
inicijalizaciju (obradu install/update-apply hook-ova), ali nikad aktivnu proveru.

- **Novi `ERPiApp/UpdateDialog.xaml(.cs)`** (koren projekta, namespace `ERPiApp`, isti obrazac
  kao u sve tri izvorne app) — prikazuje broj nove verzije, dugmad "Kasnije"/"Ažuriraj sada",
  progress bar tokom preuzimanja, pa `ApplyUpdatesAndRestart`. Koristi resurse koje ERPi već ima
  globalno (`CardBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `PrimaryButton`,
  `SecondaryButton`) — nije trebalo dodavati nove.
- **`MainWindow.xaml.cs`**: `CheckForUpdatesAsync()` pozvan iz konstruktora (posle
  `MainContentHost.Content = new DashboardView(_db)`), `GithubSource` pokazuje na
  `https://github.com/blagojevicboban/ERPi` sa `token = null` (repo je javan, isti obrazac kao
  ERPiFinansije/ERPiSredstva — ERPiZarade ima `GetUpdateToken()` fallback jer je taj repo bio
  privatan u nekom trenutku, ERPi to ne treba).
- **Verzija na prvom ekranu**: `CompanySelectWindow` (prvi prozor koji se prikazuje pri pokretanju,
  pre prijave) do sad nije uopšte prikazivao verziju u footeru ("ERPi © 2026 Blagojević Boban" bez
  broja) — dodat `x:Name="TxtVersion"` i ispisivanje `v{verzija}` u konstruktoru, isti format kao
  `LoginWindow`.
- **Živi dijalog "Istorija izmena"**: `ChangelogWindow` je od ranije postojao, ali sa tvrdo
  ukucanim (hardkodovanim) sadržajem u kodu (samo dve stavke, "v3.0.0"/"v2.5.0" — brojevi verzija
  se nisu ni poklapali sa stvarnim `version.txt`). Zamenjen verzijom portovanom iz ERPiFinansije/
  ERPiSredstva koja **učitava `CHANGELOG.md` uživo** (`WebBrowser` + ručni markdown→HTML
  konvertor, bez spoljne zavisnosti) — ista dugmad/naslov/izgled, `TxtAppVersion` čita stvarnu
  verziju iz `Assembly.GetName().Version`.
  - `ERPiApp.csproj`: nov `<Content Include="..\CHANGELOG.md" Link="CHANGELOG.md" CopyToOutputDirectory="PreserveNewest">` — isti obrazac kao ERPiFinansije (jedan fajl u korenu repoa je
    izvor istine, ne duplirana kopija kao u ERPiSredstva).
  - **Nov `CHANGELOG.md` u korenu ERPi repoa** — ERPi ga do sad nije imao uopšte. Sastavljen iz
    git istorije (`git log --reverse`) i postojećih beleški u ovom fajlu — obuhvata v2.0.0
    (prvo objedinjeno izdanje), v2.1.0 (samostalni repo), v2.1.1 (DOS uvoz paritet), i
    "Unreleased" sekciju sa svim izmenama iz ove i prethodne sesije (Radna tabla Zarade + dizajn
    tabela §3s, auto-update + verzija + ovaj dijalog).

**Build**: `dotnet build ERPi.slnx` čist, 0 upozorenja/0 grešaka; provereno da se `CHANGELOG.md`
stvarno kopira u `bin/Debug/net8.0-windows/`. **Nije vizuelno provereno kroz UI** (korisnik testira
sam). **Nije commit-ovano.**

---

## 3t. Kartica konta → Nalog (readonly/pregled) + 4 kritična nedostatka preneta iz ERPiFinansije (07.08.2026)

Dve odvojene celine iz iste sesije, obe **nisu commit-ovane**.

**Deo 1 — Dupli klik u Kartici konta sad stvarno otvara nalog** (originalni zahtev korisnika):
`KarticaKontaView`'s `DgKartica_MouseDoubleClick` je do sad bio placeholder (`MessageBox` sa
detaljima stavke, iako je tooltip grida odavno obećavao "otvara nalog"). Sad:
- Otvara pravi `NalogEditWindow` sa `Include(Stavke).ThenInclude(Konto/Partner)` po `red.NalogId`.
- Dodat kontekst-meni (desni klik) na `DgKartica`: "👁️ Pregledaj nalog" / "✏️ Izmeni / Rasknjiži
  nalog" — isti obrazac kao ERPiFinansije `KarticeView`.
- `NalogEditWindow` dobio readonly/pregled režim za proknjižene naloge — vidi belešku u §2
  ("`NalogEditWindow` ima readonly/pregled režim..."), ne ponavljati ovde. Aktivirano i u
  `NaloziView` radi konzistentnosti (korisnikov izričit izbor kroz pitanje u sesiji).
- Dodato pozicioniranje na kliknutu stavku pri otvaranju (`PozicionirajNaStavku` po `RedniBroj`,
  jer ERPi-jeva radna kopija stavki u `NalogEditWindow` ne nosi `StavkaNalogaId`).

**Deo 2 — 4 "kritična nedostatka" iz šireg istraživanja ERPiFinansije vs ERPi** (korisnik je iz
duže liste kandidata — vidi ispod — izabrao ovu grupu za ovaj krug):

1. **Kompenzacije: Nova/Izmeni** — `KompenzacijaEditWindow` (novo,
   `Views/Finansije/Kompenzacije/`) portovan iz ERPiFinansije, ali **bez sintetičko-partnerske
   logike** (`SlotKljuc`/legacy-konto fallback iz izvora) — ERPi-jev `Kompenzacija`/
   `KompenzacijaStavka` model već ima pravi `PartnerId` FK svuda, pa `SlotKljuc` je uvek
   `"P{PartnerId}"`. Otvorene stavke se čitaju isključivo preko
   `ZatvaranjeStavkiService.GetOtvoreneStavkeZaPartneraAsync` (nikad `GetOtvoreneStavkeZaKontoAsync`
   fallback iz izvora). `KompenzacijeView` dobio "➕ Nova"/"✏️ Izmeni" dugmad (ikona-samo stil) i
   dupli-klik na red u "Pametno skeniranje" tabu (`DgKandidati`) predpopunjava novu kompenzaciju
   sa tim partnerom. **Uhvaćena i ispravljena greška pri portovanju**: `KompenzacijaService.
   SacuvajKompenzacijuAsync` proverava `Strana == "Potražuje"` (sa ž) za obračun `UkupanIznosKompenzacije`
   — prvobitni port je slučajno upisao "Potrazuje" (bez ž), što bi tiho učinilo `zbirObaveza`
   uvek 0.
2. **Putni nalozi: Nova/Izmeni** — `PutniNalogEditWindow` (novo, `Views/Finansije/PutniNalozi/`),
   skoro 1:1 port (ERPi-jev model/servis se poklapaju polje-za-polje sa izvorom, nema string→FK
   adaptacije). `PutniNaloziView.BtnNoviNalog_Click`/`BtnIzmeni_Click` više ne pišu "biće dostupno
   u narednom prikazu" nego stvarno otvaraju editor. Namerno **nije** duplirano računanje
   `TrajanjeSati`/`BrojDnevnica`/`TroskoviXxx`/`UkupnoZaIsplatu` u prozoru — to već radi
   `PutniNalogService.SacuvajPutniNalogAsync` iznova iz `StavkeTroskova` pri svakom snimanju.
3. **Korisnici i uloge (RBAC)** — potpuno nov ekran, `Views/Korisnici/` (`KorisniciView` +
   `KorisnikEditWindow`), do sad nije postojao nijedan način da se korisnici upravljaju kroz UI
   (samo `LoginWindow`). Novi nav unos "👤 Korisnici i uloge" u `MainWindow` sidebar-u, sekcija
   "PODEŠAVANJA I SISTEM" (zajedničko za sve module — `Korisnik` je po dizajnu Single Sign-On,
   ne Finansije-specifičan). Adaptacija: izvor drži `Uloga` kao slobodan string, ERPi već ima
   **enum** `UlogaKorisnika` (namerna ranija popravka) — combo/DataTrigger-i u XAML-u koriste
   `{x:Static core:UlogaKorisnika.X}` umesto string-poređenja, da ne zavise od WPF-ovog
   enum→string XAML koercije. Lozinka ide kroz već postojeći `ErpiDbContext.HashPassword`/
   `VerifyPassword`. Nema F1 help (isti razlog kao svuda u ERPi — hub još ne postoji).
4. **Kursna lista — bug-fix + ekran** — `KursnaListaWindow` (novo,
   `Views/Finansije/Partneri/`), skoro 1:1 port (`KursnaListaService` već identičnog API-ja).
   **Ovo je bio pravi bag, ne samo nedostatak**: `PartneriView.BtnKursnaLista_Click` je otvarao
   `DeviznoValviranjeWindow` (potpuno druga funkcija) umesto kursne liste — dugme/tooltip su i
   dalje govorili "Kursna lista". Ispravljeno da otvara novi `KursnaListaWindow(_db)`.

**Servisni sloj za sve četiri stavke je bio već gotov u `ERPiData`** pre ove sesije (proveren
direktnim čitanjem — `KompenzacijaService`, `PutniNalogService`, `KursnaListaService`,
`ErpiDbContext.HashPassword`) — posao je bio gotovo isključivo UI port + žica, ne nova poslovna
logika.

**Preostale, NEIZABRANE stavke iz istog istraživanja** (šire poređenje `ERPiFinansijeApp/Views/`
vs `ERPiApp/Views/`, fajl-po-fajl) — kandidati za sledeći krug, da se istraživanje ne ponavlja:
- Fiskalizacija/SEF: maloprodajna fiskalizacija (ESIR, `FiskalniRacunWindow` + servis potpuno
  nedostaju), preuzimanje ulaznih e-faktura sa SEF-a (`SefUlazneFaktureWindow`, backend već gotov),
  prava integracija izlaznih e-faktura (trenutni `SefFaktureView` radi sa mock/hardkodovanim
  podacima, ne pravim SEF pozivima).
- Administracija: Rezervne kopije (Backup) tab u Podešavanjima (`BackupService` već postoji,
  samo pod pogrešnim "Zarade" namespace-om), DMS prilozi (`DmsService` postoji, nigde korišćen),
  `FirmeView` (uređivanje bilo koje firme iz liste, ne samo trenutno otvorene), Pomoć za naloge
  (F1) + Istorija izmena/audit (`PromeneWindow` — fali i backend model `Promena`).
  ~~Izveštaji/Zarade: analitički drill-down bruto bilansa, izvoz putnih naloga za Zarade
  (`IzvozZaZaradeWindow` — bez njega je uvozni lanac ka Zarade modulu nepotpun, jer
  `UvozPutnihNalogaWindow` i dalje očekuje baš taj JSON fajl kao ulaz).~~ Oboje urađeno, vidi §3v.

**Build**: `dotnet build ERPi.slnx` čist (0 CS grešaka/upozorenja) — build komanda je tokom sesije
stalno padala na poslednjem koraku kopiranja `.exe`-a (`MSB3027`/`MSB3061`) zato što je `ERPiApp`
bio pokrenut/pod debugerom (`netcoredbg`) u paraleli dok je korisnik testirao — to nije greška u
kodu, samo zaključan fajl; potvrđeno da nema `error CS`/`warning CS` linija nijednom od nekoliko
uzastopnih build-ova. **Nijedan od ovih ekrana nije vizuelno proveren kroz UI** (korisnik testira
sam). **Ništa nije commit-ovano.**

---

## 3u. Račun-otpremnica: prava PDF štampa + Pretvori predračun u fakturu (07.08.2026, nastavak)

Korisnik je tražio "Portuj Račun otpremnica iz robno ERPiFinansije" — ekran je ispalo da **već
postoji** u ERPi (`Views/Magacin/RacuniOtpremniceView` + `RacunOtpremnicaEditWindow`, preneto u
ranijoj Fazi 3.3b/3.12, `KontoKupca`/`SifraArtikla` stringovi već adaptirani na prave
`PartnerId`/`ArtikalId`/`MagacinId` FK-ove), ali poređenjem sa `TrgovinaView.xaml.cs` u izvoru
(2802-linijski monolit, `RacunOtpremnica`-relevantne akcije razbacane po celom fajlu) nađena su
dva konkretna nedostatka koja su i ispravljena:

- **`BtnStampajPdf_Click` je bio čist placeholder** — samo `MessageBox.Show("Priprema PDF
  štampanog dokumenta...")`, ništa se stvarno nije generisalo (isti obrazac lažnog UX-a kao
  ranije nađeni bag u `KarticaKontaView`-ovom dupli-kliku, vidi §3t). `PdfReportService`-ov doc-
  komentar ("Uključuje: ... Račune-Otpremnice ...") je već tvrdio da postoji, a metoda
  `GenerisiRacunOtpremnicuPdf` uopšte nije postojala u `ERPiApp/Services/PdfReportService.cs`.
  Portovana iz `ERPiFinansijeApp/Services/PdfReportService.cs` (QuestPDF, isti obrazac kao
  `GenerisiKarticuPdf`/`GenerisiNalogePdf`), polja adaptirana na FK model:
  `st.SifraArtikla`/`st.NazivArtikla` (string) → `st.Artikal.SifraArtikla`/`st.Artikal.Naziv`
  (navigacija), `st.IznosBezPdv`/`st.PdvIznos`/`st.UkupanIznos` → `st.Osnovica`/`st.IznosPdv`/
  `st.Ukupno`, `partner?.Naziv`/`racun.KontoKupca` fallback → samo `racun.Partner?.Naziv` (ERPi
  nema legacy string-konto kupca u ovom toku, `KontoKupcaId` FK na modelu postoji ali ga
  `RacunOtpremnicaEditWindow` ne popunjava — nije dirano, van dosega ove izmene).
  `RacuniOtpremniceView.BtnStampajPdf_Click` sad učitava pun račun preko
  `RacunOtpremnicaService.GetRacunByIdAsync` (već ima `Include(Stavke).ThenInclude(Artikal)`),
  generiše PDF u temp folder i otvara ga (`Process.Start`) — isti obrazac kao
  `KarticaKontaView.BtnStampajKartice_Click`.
- **"Pretvori predračun u fakturu" nije bio dostupan iz UI** — servisna metoda
  `RacunOtpremnicaService.PretvoriUFakturuAsync` je već postojala (nekorišćena), samo dugme u
  `RacuniOtpremniceView` nije postojalo. Dodato "🔄 Pretvori u fakturu" u toolbar, sa proverom
  da je izabrani dokument stvarno predračun i YesNo potvrdom.

**Namerno van dosega** (isti razlog kao već zabeleženo u §3t "Preostale, NEIZABRANE stavke"):
SEF slanje/status/UBL izvoz i ESIR fiskalizacija za račun-otpremnicu (`BtnPosaljiNaSef`/
`BtnOsveziSefStatus`/`BtnSacuvajUbl`/`FiskalniRacunWindow` u izvoru) — `RacunOtpremnica` model već
ima pripremljena polja (`SefId`/`SefStatus`/`SefDatumSlanja`/`SefPoruka`/`FiskalniBroj`/
`FiskalniQrKod`/`FiskalniDatum`), ali sama SEF integracija je već identifikovana kao širi,
neizabran posao (mock `SefFaktureView`, nedostajući `EsirFiskalizacijaService`) — ne rešavati
parče-po-parče kroz pojedinačne ekrane, čeka zajednički rad na celoj SEF/PFR celini.

**Build**: `dotnet build ERPi.slnx` — potpuno čist (0 grešaka, 0 upozorenja), ovog puta i sam
`.exe` korak prošao (korisnik je zatvorio pokrenutu instancu). **Nije vizuelno provereno kroz UI**
(korisnik testira sam — probaj PDF štampu i pretvaranje predračuna u fakturu). **Nije
commit-ovano.**

---

## 3v. §D.13 Izvoz putnih naloga za Zarade + §D.12 Analitički drill-down bruto bilansa (07.08.2026, nastavak)

Dve stavke iz "Preostale, NEIZABRANE stavke" (§3u/§3t, "Izveštaji/Zarade") urađene u istoj sesiji:

- **Izvoz putnih naloga za Zarade** — `IzvozZaZaradeWindow` portovan iz
  `ERPiFinansijeApp/Views/PutniNalozi/IzvozZaZaradeWindow` u
  `ERPiApp/Views/Finansije/PutniNalozi/`, novo dugme "📤" (ikona-samo + ToolTip,
  `IconButtonStyle`) u `PutniNaloziView` toolbaru. Novi servis
  [`ERPiData/Services/PutniNaloziZaZaradeWriter.cs`](ERPiData/Services/PutniNaloziZaZaradeWriter.cs) —
  1:1 port logike (dnevnica u zemlji, samo proknjiženi nalozi, računa prekoračenje preko
  postojećeg `PutniNalogService.VaziciNeoporeziviIznosAsync`/`PrekoracenjeDnevnice`), ali radi
  direktno nad `ErpiDbContext` (bez posebnog `AccountingDbContext`, jer ERPi nema odvojenu bazu
  za Finansije). **JSON kontrakt je bit-za-bit isti** kao već postojeći uvoznik na drugoj strani
  (`ERPiApp/Services/Zarade/PutniNaloziImportService.cs`, ranije portovan ali dotad bez ičega
  što bi taj fajl proizvelo) — oznaka formata `"ERPi-putni-nalozi-za-zarade"`, verzija 1, isti
  nazivi polja (`Format`/`Verzija`/`Izvor`/`Firma.Naziv`/`Pib`/`MaticniBroj`/`Godina`/`Mesec`/
  `Stavke[].Jmbg`/`ZaposleniIme`/`BrojNaloga`/`DatumPovratka`/`UkupnoDnevnice`/`NeoporeziviDeo`/
  `PrekoracenjeDnevnice`) — **uvozni lanac Finansije→Zarade je sada kompletan**, oba kraja žive
  u istom rešenju.
  - Pojednostavljeno u odnosu na izvor: "nalazi" (upozorenja/greške pri pripremi) su ovde obična
    lista već formatiranih `string` poruka (`"[Greška] ..."`), ne poseban `NalazUvoza` tip sa
    enum težinom — izvorni prozor je i tako te objekte odmah spljoštavao u stringove pre prikaza
    u `ItemsControl`, pa novi tip ne bi dodao ništa sem koda. Prikazna tabela (`DgStavke`) sad
    dobija stavke direktno iz `PutniNaloziZaZaradeWriter.GenerisiAsync`-a (jedan izvor istine),
    umesto da ih izvorni prozor računa drugi put duplirajući `writer`-ovu logiku.
- **Analitički drill-down bruto bilansa** — ispostavilo se da je servisni sloj
  (`OtvoreneStavkeService.GetBrutoBilansAnalitikeAsync`, grupisanje po partneru umesto po kontu)
  **već bio portovan** u ranijoj sesiji, samo nekorišćen (nijedan ekran ga nije zvao) — otud
  "Malo" ocena u planu, posao je bio samo UI. Dodato:
  - `BrutoBilansAnalitikePreviewWindow` portovan iz
    `ERPiFinansijeApp/Views/Izvestaji/BrutoBilansAnalitikePreviewWindow` u
    `ERPiApp/Views/Finansije/Izvestaji/` (export-u-Excel dugme prebačeno na `IconButtonStyle`
    umesto originalnog icon+text "X" dugmeta).
  - Novo dugme "🔎" u `BrutoBilansView` toolbaru otvara taj prozor.
  - `GetBrutoBilansAnalitikeAsync` dopunjen opcionim `odDatuma`/`doDatuma` parametrima (izvor ih
    nije imao — ignorisao je period) da poštuje isti filter period koji je već primenjen na
    glavni bruto bilans u istom ekranu; bez zadatih parametara ponaša se identično originalu.

**Build**: `dotnet build ERPi.slnx` čist (0 grešaka, 0 upozorenja) — jedan build u sredini sesije
je pukao na `SefUlazneFaktureWindow.xaml.cs` (`BtnOsvezi_Click`/`BtnZatvori_Click` "ne postoje"
iako ih fajl sadrži), ponovni build odmah posle bio čist bez ijedne izmene — isti obrazac
tranzijentnog XAML-generisanja koji je već zabeležen u §3u ("build komanda je tokom sesije
stalno padala..."), ne stvarna greška u kodu. **Nijedan od dva ekrana nije vizuelno proveren
kroz UI** (korisnik testira sam). **Ništa nije commit-ovano.**

---

## 3w. Fiskalizacija/SEF grupa: usluge na fakturi + prava SEF/PFR integracija (07.08.2026, nastavak)

Radi se o tri stavke iz "Preostale, NEIZABRANE stavke" (§3u, "Fiskalizacija/SEF") koje je korisnik
sada eksplicitno tražio da se urade: maloprodajna fiskalizacija (ESIR), preuzimanje ulaznih
e-faktura, i prava integracija izlaznih e-faktura (dotad mock). **Ovim se stavlja tačka na
"namerno van dosega" napomenu iz §3u** — SEF/PFR nije više odloženo.

**Prethodno istraženo (web, pre pisanja plana) — Zakon o fiskalizaciji Republike Srbije:**
- Ruta se određuje **po tipu kupca, ne po robi/usluzi**: fizičko lice (krajnji potrošač) →
  fiskalni račun (PFR), pravno lice/preduzetnik/javni sektor → e-Faktura (SEF). "Promet na malo"
  obuhvata robu i usluge podjednako. Izvori: [paragraf.rs](https://www.paragraf.rs/baza-znanja/knjigovodstvo/obveznik-pdv-usluga-prometa-na-malo-korisnicima-sef-obaveza-izdavanja-e-fakture.html),
  [epos.rs FAQ](https://www.epos.rs/najcesca-pitanja-fiskalizacija/).
- Ni ERPiFinansije ni ERPi nisu imali koncept "usluga" u `Artikal` šifarniku — otud odluka da se
  to doda kao deo ovog kruga (ispod).

**A. Usluge na Računu-otpremnici (trajna odluka modela, ne poništavati bez razloga):**
- `RacunOtpremnicaStavka.ArtikalId` je već bio `int?` (opciono) — dodata dva nova polja
  `OpisUsluge`/`JedinicaMereUsluge` (oba `string?`) koja se koriste ISKLJUČIVO kad `ArtikalId`
  nije popunjen. Migracija `DodajUsluguNaRacunOtpremnicuStavku` (čisto aditivna), verifikovana na
  scratch bazi (`dotnet ef database update --connection`, sve migracije do kraja prošle čisto).
- `RacunOtpremnicaEditWindow`: nova editabilna kolona "Opis usluge" pored `ColArtikal`; validacija
  prihvata stavku ako ima `ArtikalId` ILI `OpisUsluge`. **Magacin je sad obavezan SAMO ako račun
  ima bar jednu robnu stavku** — čisto-uslužni račun se čuva/knjiži bez magacina.
- `RacunOtpremnicaService.KnjiziRacunAsync`/`RasknjiziRacunAsync`: petlje ka
  `MaterijalnaKarticaService` filtriraju samo stavke sa `ArtikalId.HasValue` — GL knjiženje
  (kupac/prihod/PDV) ostaje nepromenjeno jer već radi isključivo nad agregatima
  (`UkupnoZaUplatu`/`UkupnoOsnovica`/`UkupnoPdv`), agnostično na robu/uslugu.
- `PdfReportService.GenerisiRacunOtpremnicuPdf`: fallback `st.Artikal?.Naziv ?? st.OpisUsluge`
  (i analogno za šifru/JM) u tabeli stavki.

**B. PFR fiskalizacija Računa-otpremnice — `PfrService.FiskalizujRacunOtpremnicuAsync`:**
- Namerno NIJE portovan poseban `EsirFiskalizacijaService` iz ERPiFinansije (izbegnut dupliran
  PFR HTTP klijent) — nova metoda u postojećem `PfrService` ponovo koristi isti `PfrApiClient`.
- Za razliku od postojeće `FiskalizujRacunAsync(pfrRacunId)` (gradi JEDNU lump-sum stavku za
  samostalni `PfrRacun`), ova metoda gradi **pravu stavku po redu fakture**
  (`Artikal?.Naziv ?? OpisUsluge`, `Kolicina`, `ProdajnaCena`, `Ukupno`) — tačnije, jer
  `RacunOtpremnica` već ima pravu listu stavki. Obe metode namerno koegzistiraju.
- Upisuje rezultat u **već postojeća, do sad mrtva polja** `RacunOtpremnica.FiskalniBroj`/
  `FiskalniQrKod`/`FiskalniDatum` (pripremljena u ranijoj fazi, nigde korišćena do sada).
- Način plaćanja nije još polje na `RacunOtpremnica` — `PfrZahtevPlacanje` ide podrazumevano kao
  `"Cash"` (isti default kao postojeći `FiskalizujRacunAsync`). Pravo polje za način plaćanja je
  odvojena, manja naknadna izmena.

**C. `SefFaktureView` preusmeren sa mock `SefDokument` na prave `RacunOtpremnica` zapise:**
- Ovo je bio pravi bag, ne samo nedostatak: `SefFaktureView` je radio nad potpuno izmišljenim
  `SefDokument` modelom (bez veze ka stvarnoj fakturi) — "➕ Nova faktura" je pravila lažne iznose
  10000/2000/12000, "Pošalji na SEF" je samo lokalno menjao status bez ijednog mrežnog poziva.
  Prava servisna metoda (`SefService.PosaljiNaSefAsync`) je već postojala i radila nad
  `RacunOtpremnica`, ali je nije zvao nijedan ekran u `ERPiApp` (potvrđeno grep-om pre izmene).
- `SefFaktureView.UcitajFakture()` sad čita `_db.RacuniOtpremnice.Where(TipDokumenta==Racun &&
  IsKnjizen)` — samo proknjiženi pravi računi (predračuni i neproknjiženi se ne šalju).
  Dugme "➕ Nova faktura" **uklonjeno u potpunosti** (nova faktura se pravi u `RacuniOtpremniceView`,
  ne ovde).
- Dugmad sad rade prave pozive: "🚀 Pošalji na SEF" (`SefService.PosaljiNaSefAsync`), "🧾 Fiskalizuj"
  (novo, deo B), "🔄 Osveži status" (`SefService.OsveziStatusNaSefuAsync`), "💾 Sačuvaj UBL"
  (novo, `SefService.SacuvajUblXmlFajlAsync` + `SaveFileDialog`).
- **Dugmad se automatski uslovljavaju po tipu partnera** (korisnikova eksplicitna odluka, u skladu
  sa Zakonom iz uvoda ove sekcije): `JeSefKandidat(Partner? p) => p != null &&
  !string.IsNullOrWhiteSpace(p.Pib)` — SEF/UBL dugmad aktivna samo za partnere sa PIB-om (pravna
  lica), Fiskalizuj dugme aktivno samo bez PIB-a (fizičko lice ili bez partnera/maloprodaja).
- **`SefDokument` model/DbSet namerno NIJE obrisan** — ostaje u šemi bez UI potrošača (isti obrazac
  kao već napuštena `UvoznaKalkulacijaWindow` iz §3u istraživanja), da se izbegne rizičnija
  migracija koja briše tabelu. Cleanup kandidat za budućnost.

**D. Preuzimanje ulaznih e-faktura — `SefUlazneFaktureWindow` (novo, `Views/SefPfr/`):**
- Skoro 1:1 port iz `ERPiFinansije/ERPiFinansijeApp/Views/Trgovina/SefUlazneFaktureWindow` —
  čisto-prikazni ekran (datum "od" + dugme Preuzmi + readonly grid), bez perzistencije u bazu.
  Poziva već gotov `SefService.PreuzmiUlazneFaktureAsync`. Konstruktor prima `ErpiDbContext db`
  (isti obrazac kao svuda u ERPi) umesto self-managed konteksta iz izvora.
- Novo dugme "📥" u `SefFaktureView` toolbaru otvara ovaj prozor.

**Build**: `dotnet build ERPi.slnx` čist (0 grešaka, 0 upozorenja) nakon svake celine. EF migracija
verifikovana na scratch bazi (sve migracije do `DodajUsluguNaRacunOtpremnicuStavku` primenjene
čisto). `grep -rn "SefDokumenti" ERPiApp` vraća 0 pogodaka (potvrda potpunog preusmeravanja).
Usput uočena i sama-od-sebe nestala tranzijentna greška (`BrutoBilansAnalitikeRed` duplirana
definicija u `OtvoreneStavkeService.cs`/`BrutoBilansService.cs`) tokom paralelnog rada druge
sesije na §3v stavkama u isto vreme — build je bio čist i pre i posle, nije diranо. **Nijedan
ekran nije vizuelno proveren kroz UI** (korisnik testira sam — posebno proveriti da čisto-uslužni
račun bez magacina radi kroz ceo tok, i da se SEF/Fiskalizuj dugmad ispravno uključuju/isključuju
po tipu partnera). **Ništa nije commit-ovano.**

