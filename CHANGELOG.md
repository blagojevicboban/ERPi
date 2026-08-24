# 📋 Istorija izmena (Changelog) — ERPi

Sve značajne promene i novine u aplikaciji **ERPi** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

## [2.58.0] - 2026-08-24

### 🚀 Nove funkcionalnosti

- **e-Otpremnice i EPP (evidencija prethodnog poreza) — dve nove SEF integracije.**
  Obe su već aktivne pravne obaveze u Srbiji (e-Otpremnice od 1.1.2026, EPP od septembra 2024), ne
  buduće — zato prava SEF API integracija, ne interni model bez slanja. Prave API šeme (endpoint-i,
  JSON/UBL polja) pronađene preko SEF-ovog javnog Swagger-a i zvaničnog UBL primera pre kodiranja,
  ne pretpostavljene. **e-Otpremnice**: podaci o transportu (način otpreme, prevoznik — nov
  „Prevoznik" fleg na partneru, vozač, registarski broj, adrese) na Računima-otpremnicama, slanje
  UBL DespatchAdvice XML-a i praćenje asinhronog statusa preko zasebnog `EOtpremnicaModal.tsx`.
  **EPP**: peti pod-tab u SEF ekranu — unos, slanje i otkazivanje Pojedinačne evidencije PDV-a po
  poreskom periodu. 27 novih testova, šema potvrđena na kopiji prave baze. Namerno van obima ove
  faze: Zbirna evidencija, korekcije/storno, UBL Prijemnica.
- **Optimistic concurrency (`RowVersion`) na Nalog/Kalkulacija/WebPorudzbina.** Sprečava da desktop
  i web tiho pregaze međusobne izmene istog dokumenta — drugi od dva konteksta koja učitaju i
  izmene isti zapis sad dobija grešku umesto da prvi tiho izgubi izmenu. Ručno održavan
  `RowVerzija` token (isti obrazac kao postojeći `EsirBrojac.Verzija`, pravi `rowversion` ne
  postoji na SQLite/PostgreSQL) — `ErpiDbContext` ga generički prijavljuje i sam uvećava,
  pozivaoci ga ne diraju ručno.
- **Globalni format grešaka na webu — svih 14 `*Api.ts` fajlova.** Do sada je samo osnovni
  `api.ts` imao rešen obrazac (globalni Toast na grešku); preostalih 13 (Magacin/Finansije/
  Zarade/Sredstva/SEF/Blagajna/DMS/Firma/Kasa/Kompenzacija/Korisnici/Proizvodnja/PutniNalog,
  ~338 mesta) su radili sirov `fetch()` sa ručnom, nekonzistentnom proverom po pozivu — neke
  greške su gubile server-side detalj iza generičke poruke, nijedna nije pokretala Toast. Na
  API-ju nov `KonkurentnostIzuzetakHandler` prevodi `DbUpdateConcurrencyException` u čitljiv
  409 umesto generičkog 500; u WPF-u `DispatcherUnhandledException` dobija ciljanu granu za
  istu grešku.
- **Lazy loading svih admin tabova i health checks (`/healthz`, `/ready`) na `ERPiApi`.**
- **F1 interaktivni Help Drawer sa kontekstualnim uputstvima za sve module.**
- **Web Admin meni redizajniran po ugledu na WPF sidebar** — sklopivi 64px mini-meni (dugme za
  sklapanje, prečica Ctrl+B, pamćenje stanja u `localStorage`), ugnježdeni podmeniji, breadcrumbs
  header sa statusnim bedžom i profil dropdown menijem, „Unified Dark Shell" tema primenjena i na
  gornji header i profilni dropdown.
- **Finansijski dashboard sa brzim akcijama** na webshop radnoj tabli, uklonjen višak KPI panela sa
  tabela, kompaktno stablo kategorija.
- **Unapređenje Admin UI/UX-a, pretraga i reorganizacija menija.**
  - Stavka **CMS & Brending** (`/admin/cms`) premeštena iz grupe `⚙️ Sistem` u grupu `🌐 WebShop (B2C / B2B)`.
  - Dodata brza pretraga modula u realnom vremenu na vrhu bočne trake sidebara sa trenutnim filtriranjem i otvaranjem grupa.
  - Implementirano automatsko resetovanje skrola na vrh ekrana (`scrollTop = 0`) pri prelasku na bilo koji tab ili pod-tab Admin panela.
  - Modernizovan `ErpiDataGrid`: automatsko desno poravnanje (`text-right font-mono tabular-nums`) za numeričke/valutne kolone i fiksirana zaglavlja tabela (`sticky header`) pri skrolovanju dugačkih tabela.
- **Srpski format datuma svuda u Web Adminu, umesto browser-zavisnog `<input type="date">`.**
  Novi zajednički `DatumInput` (dd.mm.gggg., srpski kalendar sa Pon–Ned, „Danas"/„Obriši") zamenio
  native date input na **svih 67 mesta u 40 ekrana** — dotad je format i jezik kalendara zavisio
  od OS/browser lokala korisnika (Chrome ga ne poštuje pouzdano ni uz `lang` na `<html>`), pa je
  isti ekran kod jednog korisnika prikazivao 24.08.2026, a kod drugog 08/24/2026.
- **Proknjiži/Rasknjiži rade nad selekcijom, iz bilo kog filtera — web i WPF.**
  Dosad su oba dugmeta bila namerno ograničena na odgovarajuću karticu filtera (Proknjiži samo iz
  „Neproknjiženi", Rasknjiži samo iz „Proknjiženi") i samo nad jednim selektovanim nalogom. Sad
  rade nad celom selekcijom (čekirano ili običan klik na red) bez obzira na aktivan filter — knjiže/
  otknjižavaju samo podskup koji je za to podoban, tiho preskačući ostalo. Nov `NalogService.
  ProknjiziViseAsync`/`RasknjiziViseAsync` (`ERPiData`) i API `nalozi/masovno-proknjizi`/
  `masovno-rasknjizi`, isto ponašanje u WPF `NaloziView.BtnProknjizi_Click`/`BtnRasknjizi_Click`.
- **Rute za sve podmenije Web Admina.** `/admin/finansije/nalozi`,
  `/admin/magacin/kalkulacije` i slično za svih 6 modula (Finansije, Magacin, Materijalno, Zarade,
  Proizvodnja, Sredstva) su sad pravi URL-ovi — refresh, dugmad Nazad/Napred i deljeni linkovi
  vode na tačan ekran, ne na podrazumevani podtab modula. Isti obrazac kao postojeći `tabIzPutanje`
  za gornji nivo menija (`podTabIzPutanje`/`putanjaPodTaba` u `AdminKontekst.tsx`, 16 novih testova).
- **Stilizovan potvrdni dijalog (`useErpiPotvrda`) umesto browser `window.confirm`.** Prvo mesto:
  brisanje DMS priloga i brisanje/proknjiženje/rasknjiženje naloga u `NaloziPodTab`.
- **Unapređenje WebShop izloga** — modernizovane kartice artikala sa ambijentalnim sjajem, Hero
  baner, Bento grid i glatki auto-scroll na katalog pri promeni kategorije.
- **Lepljenje slika (Ctrl+V) u admin formi artikla** i auto-osvežavanje liste artikala posle izmene.

### 🐛 Ispravke grešaka

- **Delete dugme u Nalozima nije reagovalo na običan klik na red**, samo na čekiranu kučicu — u WPF-u
  `DataGrid.SelectedItems` uvek sadrži i single-click, pa je web selekcija sad usklađena (i
  „Proknjiži"/„Rasknjiži" koriste isti obrazac).
- **DMS prilozi i izmena naloga nisu osvežavali tabelu** posle zatvaranja modala (📎 ikonica i broj
  priloga ostajali stari) — ispravljeno na svih 6 ekrana koji koriste `DmsPrilogModal`.
- **Vrsta naloga** je bio combo sa 4 fiksne opcije koje se nisu poklapale sa stvarnim vrednostima
  koje ~15 servisa upisuje (`IZV`, `BL`, `KALKULACIJA`...) — zamenjen slobodnim tekstualnim poljem
  sa `datalist` predlozima, isto ponašanje kao WPF textbox.
- **Tastaturna navigacija (strelice/Enter/Esc) u pretrazi konta** (`KontoAutocomplete`) u formi naloga.
- **WPF `DmsWindow` „Skeniraj" dugme sečeno teksta** — toolbar red je zahtevao ~1150px a prozor je
  bio 960px; proširen na 1250px.
- **Klik na kalendar ikonicu `DatumInput`-a nije otvarao meni** — `.focus()` sinhrono okida
  `onFocus` (otvara meni) pre nego što funkcionalni toggle stigne da se izvrši, pa je uvek
  poništavao upravo to otvaranje; ikonica je efektivno bila mrtva, radio je samo klik u tekst.
- **Radna tabla Magacina imala suvišan Robno/Materijalno toggle** unutar `MagacinDashboardPodTab` —
  Materijalno knjigovodstvo je od 23.08.2026 već zaseban tab sa sopstvenom radnom tablom
  (`MaterijalnoTab.tsx`), toggle je ostao kao rudiment i duplirao taj meni.

### 🔧 Tehničke izmene

- `IImaRowVerziju` interfejs (`ERPiData/Models/Core`), migracija `DodajRowVerzijuKonkurentnost` +
  `EnsureColumn` za zatečene SQLite baze (`Nalozi`/`Kalkulacije`/`WebPorudzbine`); 3 nova testa
  (`RowVerzijaKonkurentnostTests`).
- `@tanstack/react-query` razmotren i odbijen — admin tabovi već imaju rešen `useUcitavanje`
  obrazac (loading/greška-toast/otkazivanje/refetch u jednom pozivu, korišćen na ~60 admin tab
  fajlova), puna zamena bi bila churn bez stvarne koristi.
- Novi `proveriOdgovor`/`obradiJsonOdgovor`/`emitujAkoNijeOk` helperi u `api.ts` (isti obrazac kao
  postojeći `dohvatiJson`/`emitujApiGresku`).

### Provera

`dotnet build ERPi.slnx` i `-c Release` — oba 0/0. `dotnet test ERPiData.Tests` — **1272/1272**.
`npm run build` (ERPiWebShop) — čist. `npx vitest run` (iz `ERPiWebShop`) — **189/189**.

## [2.57.0] - 2026-08-23

### 🚀 Nove funkcionalnosti

- **Web Admin meni po ugledu na WPF i 100% paritet svih modula (23.08.2026).** Bočni meni Web Admin
  panela u potpunosti unifikovan sa enterprise WPF izgledom (`#0F172A` tamna tema, company header, user
  card na dnu, `SidebarExpanderStyle` naslovi grupa u `#38BDF8`, `NavButtonStyle` dugmad). Svi pod-meniji
  (Finansije, Robno, Materijalno, Proizvodnja, Sredstva, Zarade) izloženi direktno u bočnom meniju,
  uklonjene redundantne horizontalne trake sa dugmadima iz svih ekrana, i ugrađen automatski single-open
  accordion. Sproveden detaljan statički audit koda: svih 176 WPF XAML pogleda i 1.150 C# metoda u
  potpunosti pokriveni kroz 178 backend REST API endpointa i 680 Web Admin interaktivnih handlera.

- **Tekst ugovora van radnog odnosa iz šablona, na webu i u desktopu.** Nov prikaz „Šabloni
  ugovora" u pod-tabu Ugovori (spisak šablona, editor teksta, spisak od 26 polja koja se klikom
  ubacuju na mesto kursora) i editor teksta po ugovoru — izbor šablona, „Generiši" (uz potvrdu
  prepisivanja ručnih izmena), uređivanje, čuvanje i PDF za štampu i potpis. `UgovorTekstService`
  i `UgovorDocument` premešteni `ERPiApp` → `ERPiData`, pa **web i desktop štampaju isti dokument**;
  pravila šifarnika šablona (obavezna šifra/naziv, jedinstvena šifra, fabrički šablon se isključuje
  a ne briše) izdvojena iz WPF code-behind-a u servis. Polje koje nije popunjeno ostaje vidljivo u
  tekstu i prijavljuje se, umesto da se tiho obriše. Prvo mesto gde je `<ErpiPdfViewerModal/>`
  stvarno ožičen u web ERP-u.
- **Fabrički šabloni ugovora se konačno mogu uneti.** `SabloniUgovoraSeed` (gotovi tekstovi za
  ugovor o delu, autorski ugovor, privremene i povremene poslove i naknadu članu organa upravljanja)
  postojao je u kodu od Faze 5 ali **nije imao nijedno pozivno mesto** — firme koje šablone imaju
  dobile su ih legacy uvozom iz ERPiZarada, a na svakoj drugoj bazi šifarnik je bio prazan. Dodato
  dugme „Fabrički šabloni" na webu i 📥 u WPF ekranu „Šabloni ugovora": unosi samo šablone kojih po
  šifri nema, zatečene ne dira jer im je tekst mogao biti prilagođen firmi.
- **Zarade i ostatak ERP-a na webu (22.08.2026, devet celina).** Radni sati kao samostalan ekran
  nad pravom `RadniSati` tabelom, PPP-PD prijava, Storno i Knjiženje, evidencija isplata („više
  isplata u mesecu"), Ugovori van radnog odnosa (CRUD + obračun naknada); Magacin — uvozne i
  maloprodajne kalkulacije, robni/materijalni bilans, robne kartice, radna tabla; Finansije —
  Zaključni list, SI, Cash Flow, Promene na kapitalu, Poreski bilans; Kasa — izveštaj dnevnog
  pazara. Detalji svake celine u `PLAN_WEB_ERP.md`.
- **Finansije: Kartice konta, Dnevnik glavne knjige i Nalozi u punom paritetu sa WPF-om
  (23.08.2026, četiri koraka).** Usput ispravljena tri prava baga u kartici konta i dashboard-u
  (saldo se filtrirao pre akumulacije umesto posle, „Krajnji saldo" bio promet perioda a ne
  stanje, kolona „Promena" trajno prazna i u WPF PDF-u; potraživanja/obaveze na dashboard-u
  netovala partnere međusobno). Kartice konta dobile pun ekran (kontni plan, opseg, grupna
  štampa); Dnevnik glavne knjige postoji prvi put na webu. Nalozi dobili master-detail (stavke
  po nalogu), pun toolbar (proknjiži/proknjiži sve/rasknjiži/obriši/preknjižavanje/DMS/štampa) i
  pretraživo polje za konto umesto liste ograničene na 100 stavki. `PreknjiziKontoAsync` je
  ranije menjao konto i na proknjiženim nalozima bez ikakve zaštite — sad odbija ceo zahtev ako
  je bar jedna pogođena stavka proknjižena, na WPF-u i na webu. Detalji u `PLAN_WEB_ERP.md`.
- **Magacin: pun paritet sa WPF-om, svih pet koraka (23.08.2026).** Ispravljena tvrdnja iz ranijih
  zapisa da je „Magacin modul potpuno portovan" — tačno na nivou ekrana (svih 32+ WPF Magacin
  ekrana ima svoj web pod-tab), ali ne i na nivou CRUD-a/dugmadi, isti obrazac kao kod Finansija.
  Pet koraka zatvoreno: **(1)** tri `Take()`-trunkacije bez upozorenja korisniku (`lager`/
  `artikli-lookup`/`materijali-lookup`) zamenjene `OrderBy` + pretragom u deset formi (ARHIBEL ima
  1369 artikala, 92% kataloga je bilo van domašaja pre popravke); **(2)** Računi & Otpremnice, do
  sada read-only na webu, dobili pun CRUD (kreiraj/izmeni/knjiži/rasknjiži/masovno knjiženje/
  pretvori predračun→račun/popravi partnere); **(3)** VP Kalkulacija dobila izmenu i brisanje,
  uklonjena TS-side duplikacija formule cene (live preview sad ide isključivo preko backend
  `preracunaj` endpointa); **(4)** Šifarnik magacina dobio pun CRUD preko novog `MagacinService`
  (ERPiData), koji sad deli i WPF `MagaciniView`/`MagacinEditWindow`; **(5)** Radna tabla — KPI
  pločica „Negativna stanja" postala klikabilan drill-down (nov `NegativnaStanjaModal.tsx`) nad
  već postojećim, do sada nekorišćenim `MaterijalnaKarticaService.GetNegativnaStanjaAsync()`.
  Pod-tab navigacija u `MagacinTab.tsx` usput grupisana po WPF sidebar hijerarhiji (Robno →
  Materijalno) umesto ravne liste od 18 pilula. Verifikovano nad izolovanom kopijom prave ARHIBEL
  baze na svakom koraku (knjiženje/rasknjiženje stvarno menja stanje zaliha, duplikati i guard-ovi
  vraćaju 400/404/409). Detalji u `PLAN_WEB_ERP.md`.

### 🔧 Tehničke izmene

- **Dizajn-blokada `AuditService`/`AppSession` rešena** — servisi Zarada koji su čitali identitet
  prijavljenog korisnika iz WPF `AppSession`-a sada ga primaju kao parametar, pa ih web može zvati.
  Time je otključan ceo niz gore navedenih Zarade celina.
- Servisi premešteni `ERPiApp` → `ERPiData` (bez izmene poslovne logike): `KreditRateService`,
  `XmlExportService`, `StornoService`, `KnjizenjeService`, `IsplataService`,
  `UgovorObracunService`, `PreFlightService`, `UgovorTekstService`, `UgovorDocument`.
- Nov `NalogService` (`ERPiData`) — (raz)knjiženje, brisanje i preknjižavanje naloga glavne
  knjige, do sada razbacano po WPF code-behind-u (`NaloziView`, `PreknjizavanjeWindow`), sad
  deljeno sa web `FinansijeController`-om.

### Provera

`dotnet build ERPi.slnx` i `-c Release` — oba 0/0. `dotnet test ERPiData.Tests` — **1242/1242**.
`npm run build` (ERPiWebShop) — čist. `npx tsc --noEmit` — čist. `npx vitest run` (iz
`ERPiWebShop`) — **182/182**.

## [2.55.0] - 2026-08-21

### 🚀 Nove funkcionalnosti

- **Šifarnik Partnera i Kontni plan na web adminu (Finansije).** Nova dva pod-taba u web
  Finansijama — Partneri (kupci/dobavljači/radnici/banke/Poreska uprava, filter po ulozi,
  pretraga po šifri/nazivu/PIB-u/MB-u) i Konta (kontni plan, klasa/sintetika se izvode iz broja
  konta) — sa formama za unos/izmenu (`PartnerFormaModal`, `KontoFormaModal`) i zaštitom od
  brisanja partnera/konta koji su u upotrebi, isto pravilo kao WPF `PartneriView`/`KontaView`.
  Novi `PartnerService` (`ERPiData`) deli logiku sa `FinansijeController` umesto da je piše
  iznova u kontroleru.
- **Admin bočni meni grupisan po modulu.** Ravna lista tabova zamenjena sklopivim grupama
  (WebShop / ERP — poslovni sistem / Sistem), po uzoru na WPF `Expander` grupisanje u
  `MainWindow.xaml`; grupa aktivnog taba ostaje otvorena i posle osvežavanja stranice.
- **Blagajna (dinarska/devizna) na web adminu (Finansije).** Nov 6. pod-tab: evidencija naloga
  uplata/isplata sa filterom po vrsti blagajne i akcijama Knjiži/Izmeni/Obriši, i dnevnik blagajne
  za period (izbor vrste + opseg datuma, početno/krajnje stanje, tekući saldo po nalogu) — isti
  obim kao WPF `BlagajnaView`/`BlagajnickiNalogEditWindow`. Nov `BlagajnaController` poziva
  postojeći `BlagajnaService` (`ERPiData`) bez izmene servisa.
- **Kompenzacije/poravnanja (dvojna, asignacija, cesija) na web adminu (Finansije).** Nov 7.
  pod-tab: panel „Kandidati za kompenzaciju" (partneri sa obostranim dugovanjem, dvoklik otvara
  formu sa unapred izabranim licem) i evidencija kompenzacija (Knjiži/Izmeni/Obriši) — isti obim
  kao WPF `KompenzacijeView`/`KompenzacijaEditWindow`, do tri lica po kompenzaciji sa nazivima
  uloga koji zavise od vrste. Nov `KompenzacijaController` poziva postojeći `KompenzacijaService`
  (`ERPiData`) bez izmene servisa.
- **Putni nalozi (službena putovanja) na web adminu (Finansije).** Nov 8. pod-tab: evidencija
  naloga (Knjiži/Izmeni/Obriši), forma sa stavkama troškova (Gorivo/Smeštaj/Prevoz/Putarina/
  Taksiji/Ostalo) i live obračunom (sati/broj dnevnica/ukupno za isplatu) — isti obim kao WPF
  `PutniNaloziView`/`PutniNalogEditWindow`. Nov `PutniNalogController` poziva postojeći
  `PutniNalogService` (`ERPiData`) bez izmene servisa; nov `POST obracun` (stateless preview) tako
  da formula za broj dnevnica ne mora da se duplira u frontend kodu. Ovim je osnovni Finansije
  modul (šifarnici + sve knjiženje-celine van izveštaja) u potpunosti na webu.

### 🐛 Ispravke i Validacije

- **`PartnerService.DeletePartnerAsync` sad hvata FK sudar sa bilo kojom od ~10 tabela koje
  referenciraju partnera van stavki naloga** (`ArtikliDobavljaci`, `PartnerCenaArtikla`,
  `Radnik`, `Kalkulacije`, `Ponude`, `Narudžbenice`, `WebPorudzbine`…), ne samo sa `StavkeNaloga`.
  Otkriveno nad kopijom prave baze (ARHIBEL): brisanje partnera sa stvarnim prometom je prolazilo
  pored provere `CanDeletePartnerAsync` i padalo na FOREIGN KEY constraint u SQLite-u kao neuhvaćen
  `DbUpdateException` (HTTP 500 sa stek-tragom umesto jasne poruke).
- **`GET Finansije/dashboard` je bacao HTTP 500 na produkciji** (ne samo u testu) — SQLite
  provajder odbija SQL-preveden `Sum` nad `decimal` kolonom (kolone su `decimal(18,2)` u šemi).
  KPI panel Finansija je bio prazan/u grešci za svakog korisnika. Ispravljeno prebacivanjem tri
  agregacije (ukupan promet, potraživanja kupaca, dugovanja dobavljačima) na obrazac
  „materijalizuj pa saberi LINQ-om" koji ostatak kontrolera već koristi. Otkriveno usput dok se
  proveravala Blagajna nad kopijom prave baze.
- **Isti SQLite-decimal-Sum bag nađen i ispravljen na još tri mesta**, sistemskom pretragom nakon
  gornjeg nalaza: `ZatvaranjeStavkiService.PreostaliIznosAsync` (jezgro IOS zatvaranja — pogađalo je
  ručno zatvaranje u IOS-u, Kompenzacije i automatsko zatvaranje pri uvozu bankovnog izvoda),
  `KreditniLimitService.OtvorenoNaOdlozenoAsync` (B2B kreditni limit — svaka narudžbina na odloženo
  plaćanje za partnera sa limitom je pucala na 500) i `ProizvodnjaService.VrednostSaKarticaAsync`
  (cena koštanja radnog naloga). Svi ispravljeni istim obrascem.
- **`Kompenzacija.Strana` polje tiho gubilo iznos (ne bag koda u produkciji, uhvaćen pre commit-a).**
  Servis proverava tačan string „Potražuje" (sa ž); DTO/frontend su namerno ASCII „Potrazuje", pa je
  `UkupanIznosKompenzacije` ispadao 0 bez ijedne greške dok prevod nije dodat na granici kontrolera.

### Provera

`dotnet build ERPi.slnx -c Release` — 0/0. `dotnet test ERPiData.Tests` — **1027/1027**.
`npm run build` (ERPiWebShop) — čist. `npx vitest run` (iz `ERPiWebShop`) — **177/177** (13 fajlova).

## [2.54.0] - 2026-08-20

### 🚀 Nove funkcionalnosti

- **🎁 Paketi / setovi artikala (Bundle proizvodi).** Artikal može biti komplet sastavljen od više
  komponenti iz šifarnika (`JePaket`, `PaketSastavJson`). Zaliha paketa se računa automatski iz
  komponenti (`Min(stanje / količina)`) ili se vodi fiksno na samom paketu. Katalog nosi bedž
  `🎁 SET`, stranica artikla prikazuje sadržaj paketa sa pojedinačnim cenama i istaknutom uštedom, a
  admin dobija tab „Paket / Set" sa pretragom artikala i podešavanjem količina.
- **📢 Marketing feedovi: Google Shopping XML i Meta Catalog.** Automatski generisani katalozi za
  Google Merchant Center, Meta Commerce Manager (Facebook/Instagram Shopping) i domaće portale za
  upoređivanje cena — `/api/feeds/google-shopping.xml`, `/api/feeds/meta-catalog.xml` i
  `/api/feeds/eponuda.xml`, sa keširanjem od 15 minuta i opcionim `?samoNaStanju=true`. Adrese se
  kopiraju jednim klikom iz CMS-a.
- **📄 Tehnička dokumentacija i PDF prilozi uz artikal.** Uputstva, tehnički listovi, atesti,
  bezbednosni listovi i brošure se prikazuju u zasebnoj sekciji na stranici artikla, sa automatski
  prepoznatom vrstom dokumenta i dugmadima za otvaranje i preuzimanje. Admin ih dodaje prevlačenjem
  (drag & drop) ili linkom ka dokumentaciji proizvođača.
- **📦 PDF nalog za pakovanje (pick-list) i predračun iz detalja porudžbine.** Magacinski nalog sa
  stavkama, poljem za overu, brojem koleta, masom i potpisom; predračun sa IPS QR kodom. Dostupni i
  iz web admina i iz WPF ekrana web porudžbina.

### 🎨 UI / UX i Odzivnost

- Dopunjena F1 pomoć: `uputstvo-erpi.html` (WebShop, maloprodajna kasa, nalog za pakovanje i
  predračun) i `uputstvo-finansije.html` (DMS OCR, uvozne kalkulacije, puni PFR detalji).

### 🐛 Ispravke i Validacije

- **`ERPiApi` nije kompajlirao** — `FeedsController` je koristio nepostojeća polja modela
  (`Artikal.PoreskaStopa`, `Artikal.Opis`, `Artikal.WebOpisHtml`, `Artikal.BarKod`,
  `WebKategorija.RoditeljId`). Ispravljeno na stvarna (`PdvStopa`, `WebOpis`, `Barkod`,
  `RoditeljKategorijaId`). Greška je promakla jer `ERPiData.Tests` ne referencira `ERPiApi`, pa
  `dotnet test` prolazi i kad API projekat ne gradi — build celog rešenja je jedina provera koja to
  hvata.

### ⚠️ Migracije i Baza Podataka

- `Artikal.JePaket`, `PaketSastavJson`, `PaketZaliheRezim` — migracija `DodajPaketeArtikala`, uz
  odgovarajući `EnsureColumn` za zatečene baze. Nadogradnja proverena nad kopijom prave baze firme,
  u dva prolaza zaredom, bez gubitka podataka.

## [2.53.0] - 2026-08-20

- **🎁 Paketi / Bundle proizvodi — setovi i kompleti artikala (PrestaShop uzor).** Implementirano kreiranje
  promotivnih i tematskih setova artikala sastavljenih od više pojedinačnih komponenti iz šifarnika
  (`JePaket`, `PaketSastavJson`, `PaketZaliheRezim`). Podržana su dva režima zaliha: `AutomatskiIzKomponenti` (0)
  gde se raspoloživo stanje paketa dinamički izvodi iz zaliha komponenti (`Min(Zaliha / Količina)`), i `FiksnoNaPaketu` (1)
  gde se set vodi kao fizički unapred upakovana jedinica. Na javnoj prodavnici uveden je `🎁 SET` bedž na karticama
  proizvoda, a na stranici artikla sekcija „Sadržaj ovog paketa / seta” sa listom komponenti i istaknutim banerom
  uštede za kupca (poređenje zbira pojedinačnih cena naspram cene paketa sa procentom popusta). U Web Admin panelu
  dopunjen je editor artikla tabom `🎁 Paket / Set` sa pretragom komponenti, podešavanjem količina i pregledom uštede.
- **📢 Google Shopping XML & Meta Catalog feedovi (PrestaShop uzor).** Implementirano automatsko generisanje
  standardizovanih XML feedova proizvoda (`ProductFeedService`, `FeedsController`) za Google Merchant Center
  (`RSS 2.0 XML` sa Google namespace-om), Meta Commerce Manager (`Facebook / Instagram Shopping Catalog`) i
  domaće portale za poređenje cena (`ePonuda / Pametno.rs XML`). Feedovi automatski eksportuju šifre, nazive,
  očišćene tekstualne opise bez HTML tagova, cene sa PDV-om u RSD, akcijske cene, stanje zaliha (`in_stock`/`out_of_stock`),
  brendove, GTIN/barkodove, slike i hijerarhiju kategorija. U Web Admin CMS panelu dodata je kartica sa 1-klik
  kopiranjem URL adresa feedova i brzim pregledom.
- **📦 PDF Nalog za pakovanje (Pick-list) i Predračun iz detalja porudžbine (PrestaShop uzor).**
  Uveden namenski A4 QuestPDF dokument za komisioniranje i magacinsko pakovanje robe (`WebPorudzbinaNalogZaPakovanjeDocument`),
  sa tabelom artikala, varijanti, količina, poljem za potvrdu komisioniranja `[ ] OK`, i zbirnom magacinskom
  kontrolom (broj paketa, masa pošiljke, potpis i datum). U Web Admin panelu (`PorudzbinaDetaljiStranica` i
  `PorudzbineTab`) i WPF aplikaciji (`WebPorudzbineView`) dodata 1-klik dugmad za preuzimanje i štampu
  PDF predračuna sa IPS QR kodom i magacinskog naloga za pakovanje.
- **📄 Tehnička dokumentacija i PDF prilozi uz artikal (PrestaShop uzor).** Artikli sada imaju punu
  podršku za priloženu dokumentaciju (uputstva za upotrebu, tehničke listove, ateste/sertifikate,
  bezbednosne listove/MSDS i brošure). Na stranici proizvoda kupac dobija namensku sekciju sa PDF
  karticama, prepoznavanjem vrste dokumenta, otvaranjem u novom prozoru i direktnim preuzimanjem.
  Admin menadžer priloga dobio je Drag & Drop zonu, unos spoljnih URL adresa i brzo kopiranje linkova.
- **⚡ „Kupi odmah" — ekspresna kupovina jednim klikom.** Pored „Dodaj u korpu" (stranica artikla,
  kartica u mreži, sticky mobilna traka) stoji dugme koje otvara mini-dijalog sa četiri polja —
  ime, telefon, adresa, grad — i šalje porudžbinu za jedan artikal, preskačući korpu i kasu. Email
  je opcion. Plaćanje je uvek pouzećem, isporuka kurirom sa podrazumevanom službom; bez kupona,
  loyalty poena i Click & Collect-a. Korpa se ne dira ni pre ni posle. Uneti podaci se pamte, pa je
  druga ekspres kupovina stvarno jedan klik. Prekidač je u CMS-u; dugme se ne prikazuje u B2B
  režimu, na varijabilnom artiklu sa kartice, na rasprodatom artiklu, ni kad je pouzeće isključeno.
- **🔔 „Obavesti me kada artikal bude na stanju" (Back-in-Stock).** Na rasprodatom artiklu kupac
  ostavlja email; kada se roba vrati na zalihu, sistem šalje jedan email sa linkom pravo na artikal.
  Prijava je jednokratna, starije od 180 dana se preskaču, a neuspelo slanje ostaje za sledeći
  prolaz. Nov admin tab „Čekaju robu" sa spiskom **najtraženijih rasprodatih artikala** — lista za
  nabavku, ne samo za slanje. Obaveštenje šalje pozadinski prolaz nad raspoloživošću (svakih 10
  minuta), ne okidač u knjiženju — zalihu diže sedam različitih putanja.
- **🏢 Ručno otvaranje B2B i B2C naloga iz admina.** Prodavac otvara kupcu pristup direktno iz
  šifarnika partnera, bez čekanja da se kupac sam registruje (`/admin/kupci` i WPF „Web & B2B
  korisnici"). Podaci firme se uvek uzimaju sa partnera; lozinka se generiše i šalje mejlom, a ako
  SMTP nije podešen — prikazuje se adminu tog trenutka.
- **📷 Skener bar-koda i fotografisanje kamerom u admin panelu.** Bar-kod polje i slika artikla se
  popunjavaju direktno kamerom telefona (`/admin/artikli/:id`); do sada je skener postojao samo na
  javnoj strani prodavnice.
- **🏷️ Sistem osobina — upis na artikal, masovna izmena, VišeIzbora, obavezni atributi.**
  Karakteristike (ne-varijantne osobine) se upisuju na konkretan artikal iz web admina; jedna
  izmena se primenjuje na više artikala odjednom; atribut može nositi više vrednosti („VišeIzbora");
  po kategoriji se definišu predloženi i obavezni atributi (nova tabela `KategorijaAtributi`).
- **💰 B2B cenovnik i kreditni limit u web adminu.** Ugovorene cene po partneru i limiti se uređuju
  i sa telefona (pod-tab „Cenovnik i limiti"), deleći `PartnerCenovnikService` sa WPF ekranom.
- **🖼️ Masovni uvoz slika iz foldera** u web adminu, sa istim mapiranjem naziv→šifra kao WPF.
- **📊 Analitika i marketing u CMS-u** — `GoogleAnalyticsId`, `MetaPixelId` i `GoogleClientId`.

### 🎨 UI / UX i Odzivnost

- **Prikaz „Komplet popusta" u korpi i na kasi** — ušteda od 10% za preporučeni komplet se vidi pre
  odlaska na kasu, po istom pravilu prioriteta kao na serveru (jači od količinskog i komplet rabata
  pobeđuje, ne sabiraju se).
- **Sličica artikla u listi „Artikli na webu"**, dinamički BentoGrid, slika kategorije, kalkulator
  rata i brojčani opseg po atributu u filteru.
- Brojčani filter po atributu i **filter po brendu** — generički filter po atributu do sada uopšte
  nije postojao na serveru, iako je brend imao UI.

### ⚡ Optimizacija i Performanse

- **🔗 URL perzistencija filtera u katalogu** — `cenaOd`/`cenaDo`, `naStanju`, brend i paginacija
  žive u adresi stranice, pa se filtriran katalog može podeliti linkom i sačuvati u obeleživačima.
- **🔍 Google Rich Snippets (Schema.org JSON-LD)** — ocene, cene, brend, kategorija, stanje lagera i
  uslovi dostave u strukturiranim podacima za Google Shopping i organske rezultate.

### 🐛 Ispravke i Validacije

- **Popust za „Često se kupuje zajedno" je sada stvaran, ne samo prikaz.** Stranica artikla je
  obećavala 10% popusta, a klik je dodavao artikle u korpu po punoj ceni — kupac je na kasi plaćao
  pun iznos. Popust sada obračunava server pri kreiranju porudžbine.
- **Porudžbina više ne puca na izostavljeno polje.** `KreirajPorudzbinu` je pretpostavljao da klijent
  šalje svako string polje, pa je izostavljen poštanski broj davao HTTP 500 umesto poruke. Uz to
  dodata validacija koje nije bilo: ime i telefon obavezni uvek, adresa i grad obavezni za kurirsku
  isporuku — ranije je prazna adresa prolazila i pucala tek pri kreiranju pošiljke kod kurira.
- Sitne ispravke nađene pri pregledu web admina i prodavnice na telefonu (§3dt, §3du).

### ⚠️ Migracije i Baza Podataka

- `WebShopPodesavanja.EkspresKupovinaOmogucena` (bool, podrazumevano uključen) — migracija
  `DodajEkspresKupovinu`.
- `WebShopPodesavanja.ObavestenjaOZalihiOmogucena` + tabela prijava za back-in-stock — migracija
  `DodajObavestenjaOZalihi`.
- `KategorijaAtributi` (predloženi/obavezni atributi po kategoriji) — migracija `DodajKategorijaAtributi`.
- Sve tri stižu i na **zatečene baze** kroz `EnsureDbSchemaUpdated`; nadogradnja je proverena nad
  kopijom prave baze firme, u dva prolaza zaredom, bez gubitka podataka.

## [2.52.0] - 2026-08-19

### 🌲 URL prodavnice nosi celu putanju kategorije, katalog prikazuje i podkategorije

- **`/kategorija/alati/elektricni-alati/busilice`** umesto samo `/kategorija/busilice` — ruta je
  sad splat (`/kategorija/*`), a link ka bilo kojoj kategoriji (meni, brzo pretraživanje, breadcrumb
  na stranici artikla, kanonički URL i JSON-LD breadcrumb za pretraživače) nosi ceo lanac predaka.
  Stari linkovi sa samo jednim segmentom i dalje rade — čita se samo POSLEDNJI segment putanje,
  ostatak je kozmetički.
- **Otvaranje nadkategorije prikazuje i artikle iz svih njenih podkategorija**, ne samo one
  direktno dodeljene njoj (`KatalogController.PreuzmiProizvode`).
- Usput ispravljen postojeći propust: izbor podkategorije u glavnom katalogu (`App.tsx`) nije
  ažurirao naslov/SEO podatke jer je pretraga rađena samo po korenu stabla, ne po ravnoj listi.

### 🐛 Sortiranje po ceni je bacalo grešku servera

`GET /api/katalog/proizvodi?sortiranje=cena-rastuce` (i opadajuće) je padao sa 500 — SQLite
provajder odbija da prevede `ORDER BY` po `decimal` izrazu (cena je kod nas TEXT kolona), a i da je
prevod uspeo, sortirao bi leksikografski, ne brojevno. Za ta dva režima sortiranja server sad učita
filtrirani skup i sortira ga u memoriji; sortiranje po nazivu i podrazumevano ostaju u bazi. U
razvojnom okruženju je ova greška bila neprimetna — WebShop pri neuspešnom pozivu tiho prelazi na
izmišljene (mock) podatke, pa se katalog i dalje prikazivao, samo sa pogrešnim artiklima.

### 🎨 Uklonjen dupliran dropdown za sortiranje

Katalog je na širim ekranima prikazivao dva identična „Sortiraj po" dropdown-a odjednom (gornja
traka i toolbar rezultata) — oba vezana za isto stanje, ali vizuelno zbunjujuće. Ostaje jedan.

### 🗂️ Top meni: brz prekidač po kategoriji + globalno isključivanje praznih kategorija

- Nova kolona `WebKategorija.PrikaziUGlavnomMeniju` — koje kategorije idu u horizontalni top meni,
  nezavisno od toga da li su uopšte aktivne. Brz pill-switch direktno u redu liste kategorija
  (`KategorijeTab.tsx`), bez otvaranja forme za izmenu; isto polje i u WPF admin ekranu.
  Kategorije bez ijednog objavljenog artikla (ni u podkategorijama) se iz top menija sklanjaju.
- Novo globalno CMS podešavanje `IskljuciPrazneKategorijeIzTopMenija` (podrazumevano uključeno) —
  admin može da isključi to sakrivanje, npr. da testira praznu kategoriju u meniju.

### 🚚 Click & Collect (preuzimanje u prodavnici), reklamacije, pravni tekstovi

- Magacini se mogu otvoriti za lično preuzimanje (adresa, radno vreme, telefon); checkout nudi
  izbor kurir/preuzimanje, dostava je besplatna za preuzimanje, kupac i admin prate status
  (spremno/preuzeto) uz email i SMS obaveštenje.
- Tok reklamacija/povrata robe — kupac prijavljuje iz „Moje porudžbine", admin rešava kroz nov tab.
- Uslovi korišćenja, Politika privatnosti i Pravo na odustanak kao CMS-uredive javne stranice, uz
  traku za saglasnost na kolačiće (cookie consent).

### 🏷️ Šifarnik osobina artikala

Nov ekran „Osobine" (`/admin/osobine`, po uzoru na PrestaShop) — dozvoljene vrednosti atributa
(npr. Boja → Crna, sa hex bojom) biraju se iz šifarnika umesto slobodnog unosa na svakom artiklu.
Atribut sad razlikuje osobine koje postaju dugmad za izbor varijante od onih koje se samo
prikazuju kao specifikacija.

### 🧾 Prošireno uređivanje proizvoda (SEO, isporuka, zalihe, dobavljači)

Admin forma za artikal dobila kartice sa SEO poljima (meta naslov/opis, slug, tagovi), dodatnim
barkodovima (MPN, UPC, ISBN), dimenzijama/težinom pakovanja, tekstom za stanje/nema stanja zaliha,
datumom dostupnosti i listom dobavljača po artiklu — po uzoru na PrestaShop.

### 💳 IPS QR na ponudi/predračunu iz korpe

Ponuda generisana pre nego što porudžbina uopšte postoji u bazi sad ima pravi NBS IPS QR kod
(žiro račun/naziv/PIB firme), ne izmišljen placeholder.

### 🖱️ Ikonica + ToolTip umesto ikonica + tekst na listing ekranima

120 CRUD dugmadi (Novi/Izmeni/Obriši/Proknjiži/Export...) na 29 listing ekrana svedeno na samu
ikonicu, sa tekstom u ToolTip-u — isti obrazac kao ranije na `KontaView`.

### 📱 Admin panel WebShop-a mobile-friendly

Bočni meni admin panela postao pomerljiv panel sa overlay-em i hamburger dugmetom (kao mobilni
meni kategorija u prodavnici); liste na uskom ekranu prikazuju kartice umesto tabela.

**Testovi: 954 (ERPiData.Tests), 108 (vitest).**

## [2.51.0] - 2026-08-18

### 📤 Masovno slanje na SEF stiglo u glavno izdanje

Rad koji je stajao na odvojenoj grani od 17.08. spojen je u glavni tok — *📤 masovno slanje* i *🔁 masovno osvežavanje statusa* na ekranu **SEF e-Fakture**. Pun opis je pod [2.44.0] niže; do sada ta verzija nije bila u instalaciji.

### 📚 Dokumentacija usklađena sa kodom

- `docs/ARCHITECTURE.md`: nov odeljak **2.3c** (prenos firme na drugi DBMS) i **2.5** (tajne u bazi), a **2.3b** dopunjen indeksima na zatečenim bazama i tabelom testova koji čuvaju od povratka drifta.
- `docs/DEVELOPMENT.md`: tabela **šta je ručno, a šta nije** pri dodavanju tabele, kolone ili indeksa, i postupak za novu tajnu.

**Testovi: 945.**

## [2.50.0] - 2026-08-18

### 🔒 Lozinke i ključevi više ne stoje čitljivi u fajlu baze

Firma na SQLite-u je jedan `.db` fajl — kopira se na USB, šalje mejlom knjigovođi, vozi u rezervnoj kopiji. U njemu su do sada u **čistom tekstu** stajali: lozinka mejl naloga, PIN i PAC kod PFR-a, lozinka fiskalnog sertifikata, API ključ SEF-a, tajni ključ kartičnog procesora, pristup kurirskoj službi i SMS provajderu. Svaki pregledač SQLite-a ih je prikazivao.

- Tajne se sada šifruju **Windows zaštitom podataka (DPAPI)**, ključem koji drži sam Windows i koji nigde ne mora da se čuva.
- **Zatečene baze se prevode same**, pri prvom otvaranju firme — bez ijedne radnje korisnika i bez ponovnog unosa.
- **Ako fajl baze završi na drugom računaru, tajne se ne otvaraju.** To je i smisao: podaci firme se vide, tajne ne. Program u tom slučaju traži ponovan unos umesto da prijavi grešku.
- **Firme na PostgreSQL / SQL Serveru se ne diraju** — tamo baza nije fajl koji putuje, a zaštita vezana za jednu mašinu učinila bi tajne nedostupnim sa ostalih računara u mreži.
- Lozinke korisnika nisu obuhvaćene jer se i ne čuvaju — od njih stoji samo heš, koji se poredi, a ne čita.

**Napomena:** zaštita čuva od odnetog fajla, ne od nekoga ko je već prijavljen na tom računaru — jer ista podešavanja mora da čita i WebShop servis, koji radi pod svojim nalogom.

**Testovi: 933.**

## [2.49.0] - 2026-08-18

### 🏗 Nedovršena proizvodnja više ne nosi režiju meseci koji nisu ni došli

Radni nalog nosi režiju kao **jedan zbir za ceo nalog**, iako mu se ona raspoređuje po mesecima u kojima je radio. Obračun nedovršene proizvodnje na dan uzimao je taj zbir ceo, pa je nalog koji radi maj–jul već **na 31. maj imao i junsku i julsku režiju** — trošak koji tog dana još nije nastao, i koji je na kontu nedovršene proizvodnje stajao sve do završetka naloga. Zaliha na bilansu bila je precenjena, a rezultat maja lošiji nego što jeste.

- Režija sada ulazi **srazmerno osnovi koju je nalog do tog dana ostvario** — po istom ključu (sati rada, sati mašina ili utrošen materijal) po kom je i raspoređena, pročitanom iz *Podešavanja → Proizvodnja*, ne pretpostavljenom.
- **Nalog bez ijednog sata zadržava celu režiju** — nema čime da se deli, pa ostaje kako je i bilo pre podele.
- Po ključu *utrošen materijal* režija ide sva ili nimalo, po datumu naloga: materijal nema datum unutar naloga, a lažno tačna podela je gora od poštene.
- Knjiženje **završetka** naloga se ne menja — tada je nalog gotov i cela režija s pravom prelazi na gotove proizvode.

**Testovi: 926.**

## [2.48.0] - 2026-08-18

### 🔑 Zatečene baze konačno dobijaju indekse — i četiri kolone koje su nedostajale

Tabele koje postojeća baza dobija dopunom šeme nastajale su **gole**: `CREATE TABLE` je bio prepisan ručno, `CREATE INDEX` nije, a indeksi su živeli samo u migracijama — koje na takvoj bazi stanu na „already exists" i nikad se ne izvrše. Izmereno: **60 indeksa je nedostajalo**.

- **Među njima i jedinstveni**, dakle nije bila u pitanju samo brzina nego i izostala zaštita od duplikata: `IX_WebKorisnici_Email` (dva naloga na isti mejl), `IX_WebPorudzbine_BrojPorudzbine`, `IX_WebKategorije_Slug`, `IX_Sastavnice_Sifra`, `IX_RadniNalozi_BrojNaloga_Godina`, `IX_ArtikalAtributVrednosti_ArtikalId_AtributId`.
- **Indeksi se sada izvode iz šeme programa**, ne iz spiska naredbi koji se održava rukom — spisak je i bio uzrok, pa bi ga svaki sledeći indeks opet prerastao. Indeks koji uđe u program od sada sam stiže i na zatečene baze.
- Jedinstven indeks koji ne može da nastane zato što u zatečenim podacima već postoji duplikat **preskače se uz poruku u dnevniku** — ostali indeksi ne izostaju zbog njega, i program svakako ulazi u firmu.

### 🐞 Radni nalozi na zatečenoj bazi

Ista provera je otkrila da tabeli `RadniNalozi` na zatečenim bazama nedostaju **četiri kolone** iz ranijih verzija — `RobnoKretanjeZaduzenjeId`, `RobnoKretanjeRazduzenjeId` (knjiženje proizvodnje), `StvarnaVrednostUtroska` i `NabavnaCenaArtiklaPre` (vrednovanje utroška i rasknjižavanje). Ušle su u program i u migraciju, ali ne i u dopunu zatečene šeme, pa je **Proizvodnja na takvoj bazi padala pri svakom otvaranju radnih naloga**. Sada se dodaju same.

**Testovi: 922.**

## [2.47.0] - 2026-08-18

### 🔐 Prelazak na produkciju e-fiskalizacije više nije puko prebacivanje polja

Režim PFR-a i okruženje su do sada postojali u bazi, ali ih **nijedan ekran nije postavljao** — bez ručne izmene baze nije se moglo preći sa L-PFR na V-PFR, ni sa probnog na stvarno okruženje. Sada su u *Podešavanja → e-Fiskalizacija*, zajedno sa brojem odobrenja ESIR-a i klijentskim sertifikatom.

- **Adresa se unosi samo za L-PFR.** Za V-PFR je bira sam sistem prema okruženju (`vsdc.suf.purs.gov.rs` naspram `vsdc.sandbox.suf.purs.gov.rs`), pa se polje sklanja sa ekrana — otvoreno, bilo bi mesto na kome se probni i stvarni promet mogu pomešati jednom pogrešnom slovom.
- **Prelazak na produkciju traži dokaz da se sme.** Provera redom: broj odobrenja ESIR-a, PIB firme, postojeći `.p12` sertifikat (za V-PFR), **uspešan test veze sa produkcionim PFR-om bez simulacije**, i tek onda izričita potvrda koja kaže da se izdat račun ne može poništiti. Ako bilo šta ne prođe, okruženje ostaje probno — **a sva ostala podešavanja se svejedno sačuvaju**, da korisnik ne izgubi unos zato što PFR trenutno nije upaljen.
- **Promena okruženja ulazi u revizioni trag** (`AuditLog`), sa starom i novom vrednošću i brojem odobrenja.
- Povratak sa produkcije na probno se ne proverava — to je uvek bezbedan smer.
- Kvačica za simulirane račune se sivi u produkciji: simulacija je tamo nemoguća po konstrukciji, pa ne treba da izgleda kao izbor koji nešto menja.

### 🛰 Dnevnik poziva prema PFR-u dobio ekran

Zapisi su se skupljali od 2.45.0, ali se nisu mogli pogledati. Nov, treći tab u *SEF e-Fakture i PFR*:

- Lista poziva sa vremenom, putanjom, HTTP statusom i trajanjem; greške su crvene.
- Klik na red pokazuje **poslato i primljeno telo** — jedino mesto na kome se posle vidi šta je tačno otišlo kad PFR odbije račun na kasi. PIN bezbednosnog elementa je maskiran već pri upisu.
- Filteri: samo greške, samo izdavanje računa, po okruženju.
- Dugme za brisanje zapisa starijih od 90 dana — dnevnik na prometnoj kasi naraste brže od svega ostalog u bazi. Fiskalni računi se ne diraju.

### 💰 Pazar po smenama

Presek stanja u prozoru smene pokazuje samo tekuću smenu i nestaje kad se ona zatvori. Nov ekran *Kasa → 💰 Pazar po smenama* je pogled unazad:

- Smene u zadatom razdoblju sa brojem računa i refundacija, pazarom po sredstvu plaćanja i **razlikom u fioci**; manjak je crven, otvorena smena je u kurzivu.
- Zbir razdoblja u zaglavlju.
- **Smena se svrstava po danu otvaranja**, ne po datumu računa: smena koja pređe ponoć pripada danu u kome je počela, isto kao što se i pazar predaje po smeni, a ne po kalendaru.
- Obrnut opseg datuma se tumači kao omaška i granice se zamene, umesto da ekran ostane prazan bez objašnjenja.

**Testovi: 919.**

## [2.46.0] - 2026-08-18

### 🗄 Prelazak firme na server i kopiranje firme prenose **celu** bazu

Prenos na PostgreSQL / SQL Server (*Firma → Migracija baze*) i *Kopiraj firmu* nabrajali su tabele ručno i pokrivali **40 od ~130**. Sve što je u program ušlo posle pisanja tog spiska tiho je izostajalo — a prenos je i dalje javljao „🎉 Uspešno završena migracija!".

- **Izostajalo je, između ostalog:** izlazni računi i otpremnice sa stavkama, cela Kasa/PFR grupa (fiskalni računi, ESIR brojači, dnevnik poziva, smene), **ceo WebShop** (porudžbine, kupci, kuponi, recenzije, B2B cenovnici i adrese), Zaradini krediti, isplate, ugovori, bolovanja i PPP-PD prijave, popisi osnovnih sredstava, nivelacije, maloprodajne i uvozne kalkulacije, robna kretanja, PDV zapisi, SEF dokumenti, putni nalozi i revizioni trag.
- **Spiska više nema.** Obuhvat i redosled se izvode iz same šeme, pa nova tabela ulazi u prenos onog trenutka kad uđe u program. Redosled je topološki po vezama (roditelj pre deteta), jer sve tri baze proveravaju veze pri upisu.
- **Ključevi se zadržavaju**, pa veze između dokumenata preživljavaju prenos.
- **PostgreSQL brojači ključeva se usklađuju posle prenosa.** Bez toga bi prvi novi dokument u prenetoj bazi udario u već postojeći ključ.
- **Kopija više ne izmišlja revizioni trag.** Do sada je prenos artikala, partnera i konta upisivao „Kreiran" za svaki red, dok pravi zapisi iz izvorne baze uopšte nisu prelazili.
- **Zatečeni podaci u ciljnoj bazi se brišu pre prenosa.** Kad brisanje baze ne uspe (klijent-server, neko drugi na bazi), stari kod je ćutke preskakao svaku tabelu koja nije prazna i pravio mešavinu starog i novog.

**Testovi: 910.**

## [2.45.1] - 2026-08-18

### 🖨 Štampa isečka je sada stvarno povezana sa štampačem

U 2.45.0 je generator ESC/POS isečka bio napisan i pokriven sa 23 testa, ali ga **ništa u aplikaciji nije zvalo** — isečak se nije mogao odštampati. Nedostajala je druga polovina posla.

- **`RawPrinterHelper`** — slanje sirovih bajtova kroz `winspool.drv` sa tipom podataka `RAW`. `System.Drawing.Printing` i WPF štampa to ne mogu: oni renderuju stranicu kroz drajver, koji bi ESC/POS komande protumačio kao znakove.
- **Dugme „🖨 Štampaj isečak"** u prozoru posle naplate, uz mogućnost da se isečak štampa sam čim PFR potvrdi račun.
- **Neuspela štampa se ne prijavljuje kao neuspela prodaja.** Račun je već izdat i evidentiran kod Poreske uprave; poruka to izričito kaže, jer bi kasirka inače pokušala da ga izda ponovo. Isto važi i za samostalnu štampu — tiho preskočen isečak nije opcija, isečak se po zakonu izdaje kupcu.
- **Podešavanja → 🛒 Kasa** dobila su izbor štampača (lista instaliranih), širinu trake (58/80 mm), pismo, broj kodne strane, rez papira, otvaranje fioke i **probni isečak** koji štampa Ђ Љ Њ Ћ Џ Ј i QR — da se pre prve prodaje vidi kako izlazi ćirilica.
- Zapamćen štampač koji trenutno nije prijavljen (isključen) zadržava se u listi, da se pri prvom snimanju ne izgubi tiho.

### 🐞 Ispravke

- **A4/PDF fiskalni isečak je stavke sa 10% PDV-a štampao kao 20%.** Rekapitulacija je grupisala po poreskoj oznaci pa stopu vraćala unazad iz oznake, a poređenje je ostalo na **latiničnom `E` (U+0045)** dok oznaka za 10% jeste **ćirilično `Е` (U+0415)** — različiti znakovi, pa je 10% padalo u granu za 20%. Nastalo kad su oznake u 2.45.0 ispravljene na ćirilicu: popravljen je proizvođač oznake, ali ne i ovaj potrošač. Ispravka **uklanja obrnuto mapiranje**: grupiše se po stopi, a oznaka se iz nje izvodi, pa ovakav razlaz više nije moguć. Dodat i test koji oznaku proverava po kodnim tačkama na javnoj metodi koju zovu ekrani za štampu — dotadašnja provera je poredila stringove i prošla bi da oba nose isto pogrešno slovo.

**Testovi: 904.**

## [2.45.0] - 2026-08-17

### 🛒 Maloprodajna kasa (POS) i e-Fiskalizacija — ESIR prema stvarnom protokolu Poreske uprave

Puna dokumentacija: **[`docs/KASA.md`](docs/KASA.md)**.

#### Zatečena fiskalizacija je bila pisana prema API-ju koji ne postoji

`PfrApiClient`, `PfrService` i dugme „Fiskalizuj (PFR)" postojali su od ranije i vodili se kao završeni, ali ne bi radili ni sa jednim stvarnim PFR-om ni prošli pregled Poreske uprave. Jedanaest nalaza, svaki proveren čitanjem koda:

- **Putanje `/api/v1/*`** umesto `/api/v3/*`.
- **Autentifikacija HTTP header-om `PAC`** — takav header ne postoji u specifikaciji. Bezbednosni element se otključava PIN-om na `/api/v3/pin`, a V-PFR uz to traži klijentski TLS sertifikat.
- **Poreske oznake u pogrešnom pismu.** Izmereno po kodnim tačkama: 20% je bilo latinično `Đ` (U+0110), 10% latinično `E` (U+0045). Ispravno je ćirilično `Ђ` (U+0402), `Е` (U+0415), `А` (U+0410) — dve od tri oznake nisu bile ni u istom alfabetu. Oznake se sada uopšte ne kucaju nego čitaju iz `currentTaxRates` i snimaju uz svaki račun, da bi se stari račun mogao reprintovati po jezgru koje je tada važilo.
- **Jedinična cena slata neto, ukupno bruto.** Kad porez nije u ceni, `unitPrice × quantity ≠ totalAmount` — identitet koji PFR proverava.
- **`invoiceType: "Refund"`** ne postoji; refundacija je *vrsta transakcije*, ne vrsta računa.
- **`FiskalniDatum = DateTime.Now`** umesto `sdcDateTime` iz odgovora; na isečku po zakonu stoji vreme PFR-a.
- **Šest od ~20 polja odgovora se čuvalo** — nedostajali su brojač, potpis, oznaka kase, verifikacioni QR i poreska rekapitulacija, sve što se po zakonu štampa.
- **`PfrSimulatorMod` podrazumevano `true`** — obveznik je mogao raditi u produkciji verujući da fiskalizuje.
- Podržane 2 od 10 obaveznih kombinacija tipa računa i transakcije; nije bilo ni ESIR broja odobrenja ni sopstvenog brojača računa.

#### Protokol v3

- `GET /api/v3/status`, `GET /api/v3/attention`, `POST /api/v3/pin` (telo je goli PIN, `text/plain`), `POST /api/v3/invoices`.
- **Podrška za oba režima:** L-PFR (uređaj kod obveznika, radi bez interneta) i V-PFR (servis Poreske uprave, traži klijentski `.p12` sertifikat).
- **Brojčani i tekstualni zapis enumeracija.** Primeri u uputstvu Poreske uprave koriste brojeve (`"invoiceType": 4`), dokumentacije pojedinih V-PFR servisa tekst (`"Advance"`). Oba su „ispravna" zavisno od sagovornika, pa je zapis podešavanje — a **čitanje odgovora je uvek tolerantno na oba oblika**.
- **Idempotentnost.** Svaki zahtev nosi `RequestId`; **istekao poziv se nikad ne simulira**, jer je PFR možda već potpisao račun, a dupli fiskalni račun se ne može poništiti.
- **Dnevnik poziva** (`PfrPozivi`) — jedino mesto na kome se posle vidi šta je tačno poslato kad PFR odbije račun. PIN se maskira pre upisa.
- **Lokalni simulator PFR-a** (*Podešavanja → e-Fiskalizacija → ▶ Pokreni simulator*) govori isti protokol i odbija zahteve koje bi odbio i pravi PFR: nepoznata poreska oznaka, zbir plaćanja koji ne odgovara računu, refundacija bez reference na original.

#### Kasa (POS)

- Nov ekran *Porezi, SEF i fiskalizacija → 🛒 Kasa (maloprodaja)*: unos, korpa, split-plaćanje, refundacija, obuka, smena.
- **Redosled koji je najvažniji u modulu:** sačuvaj dokument → fiskalizuj → **tek na uspeh** proknjiži. Kad bi se prvo knjižilo, neuspela fiskalizacija bi ostavila razduženu robu bez fiskalnog računa — promet koji postoji u magacinu a ne postoji kod Poreske uprave.
- **Kasa nema svoj dokument** — koristi `RacunOtpremnica`, jer `KnjiziRacunAsync` već razdužuje robnu karticu i knjiži nalog. Poseban POS entitet bi značio drugu implementaciju istog knjiženja.
- **Obuka-račun se nikad ne knjiži** i ne ulazi u pazar: PFR ga potpisuje, ali to nije promet.
- **Kusur se oduzima od gotovine pre slanja PFR-u** — PFR-u ide tačan iznos računa, jer kusur nije promet. Preplata bez gotovinskog sredstva se odbija umesto da tiho pošalje pogrešan iznos.
- **Smena i pazar:** otvaranje sa početnom gotovinom, presek stanja (X-izveštaj), zatvaranje sa prebrojanom gotovinom i zabeleženom razlikom.
- **Novo pravo `PravoKasa`**, odvojeno od `PravoRobno` — kasirka ne treba da vidi kalkulacije i nabavne cene. Nemaju ga *Magacioner*, *Kadrovska služba* i *Gledalac*.
- **Traka preko cele širine, bez zatvaranja:** narandžasta za probno okruženje, crvena za obuku. Kasirka mora videti da račun nema pravnu vrednost *pre* naplate.

#### Pretraga artikala na kasi

- **Živa lista predloga** dok se kuca, sa cenom i stanjem na izabranom prodajnom mestu. **Cena se razrešava kroz cenovnik**, ista metoda koju koristi i dodavanje u korpu — lista koja pokazuje jednu cenu a naplati drugu gora je nego lista bez cene.
- **Rangiranje:** tačan barkod → tačna šifra → šifra koja počinje upitom → naziv koji počinje upitom → naziv sa svim rečima upita. `crep les` nalazi „Crep kontinentalni Leskovac", ali ne i „Crep ćeramida".
- **Pretraga zanemaruje kvačice i pismo:** `curka` nalazi „Ćurka", `dzak` nalazi „Џак цемента". Nova kolona `Artikli.NazivPretraga`, jer se takvo poređenje u SQL-u ne može izvesti prenosivo preko SQLite-a, PostgreSQL-a i SQL Servera. Puni je `SaveChanges` — artikli u ERPi ulaze sa desetak strana i svaka bi pre ili kasnije zaboravila; zatečene redove popunjava prvi ulazak u firmu.
- Fokus **namerno ostaje u polju za unos** i dok je lista otvorena; strelice pomeraju izbor. Kad bi fokus prelazio na listu, prvi sledeći sken bi se izgubio.
- `3*1042` dodaje tri komada.

#### Čitač barkoda

- **Sken se hvata na nivou celog ekrana**, pa stiže i kad kursor nije u polju za unos.
- **Znak se čita iz fizičkog tastera, a ne iz teksta** — sa ćiriličnom Windows raspodelom bi barkod sa slovima inače bio neupotrebljiv.
- **Sken se od kucanja razlikuje po ritmu**, ne samo po dužini. Bez te provere bi ručno otkucan naziv prošao istom putanjom i izgubio kvačice („ćurka" bi postalo „urka").
- **Ekran za proveru čitača** u *Podešavanja → 🛒 Kasa*: pokazuje šta je tačno stiglo, koliko milisekundi po znaku i čime se unos završio. Podešavanja su po računaru, jer druga kasa ume da ima drugi model čitača.

#### Barkodovi vagane robe

- Nalepnica sa vage nosi šifru i izmerenu težinu (ili cenu) — svaki paket ima svoj kod, pa se ne traži u šifarniku nego rastavlja po šablonu (`Firma.SablonVaganeRobe`, najčešće `2PPPPPPTTTTTK`).
- **Dve nalepnice istog artikla ostaju dve stavke** — svaki paket je izmeren posebno, sabiranjem bi se izgubile težine.
- Kad nalepnica nosi cenu umesto težine, količina se izvodi iz cene da stavka izađe tačno na iznos koji kupac čita sa nalepnice.

#### Štampa isečka (ESC/POS)

- Generator vraća `byte[]` i živi u `ERPiData`, pa se **ceo isečak proverava testom bez ijednog štampača**; slanje na štampač ostaje u ERPiApp-u.
- **Žurnal koji vrati PFR se štampa doslovno** — deo je onoga što je potpisano. Lokalni raspored (58 mm / 32 znaka, 80 mm / 48) je rezerva kad žurnala nema.
- **Ćirilica na termalnim štampačima** je najveći praktični rizik: podrška varira po proizvođaču. Kodna strana 1251 je jedina sa srpskim Ђ, Љ, Њ, Ћ, Џ i Ј i zato je podrazumevana; uz nju stoje latinica 852 i ASCII režim koji radi na svakom štampaču.
- **QR ide kao rasterska slika** (`GS v 0`), ne ugrađenom komandom `GS ( k` — nju nemaju svi štampači, a QR je zakonska obaveza.

#### Bezbednost i usklađenost

- **Simulacija je u produkciji nemoguća po konstrukciji:** `SimulatorMod` je izvedeno kao `SimulatorMod && Okruzenje == Sandbox`, bez obzira šta piše u bazi. Podrazumevano okruženje je `Sandbox` i za zatečene baze — prelazak na produkciju mora biti svesna radnja korisnika, nikad posledica nadogradnje.
- Probni promet se ne meša sa stvarnim: `FiskalniRacun.Okruzenje` se upisuje uz svaki račun, a brojači su razdvojeni po okruženju.

#### Šema baze

- **8 novih tabela** (`FiskalniRacuni`, `FiskalniRacunStavke`, `FiskalniRacunPorezi`, `FiskalniRacunPlacanja`, `EsirBrojaci`, `PfrPozivi`, `PosSmene`, `RacunOtpremnicaPlacanja`) i **13 novih kolona**. Četiri migracije, **nijedna destruktivna promena**.
- **Fiskalni račun je zaseban entitet**, a ne kolone na dokumentu — jedan promet kroz život dobije više fiskalnih računa (predračun → avans → promet → kopija → refundacija).
- `PravoKasa` u migraciji ima `defaultValue: true`, ručno ispravljeno posle EF scaffold-a koji uvek predlaže `false`; inače bi ista baza imala različitu vrednost zavisno od toga da li je kolona nastala migracijom ili raw SQL putem.
- **Ispravljen drift u raw SQL putu:** `IX_Artikli_NazivPretraga` je postojao u migraciji, ali ne i u `EnsureDbSchemaUpdated`. Otkriveno probom na izolovanoj kopiji žive baze (1369 artikala) — zatečene baze do sada su nove tabele dobijale raw SQL putem, jer `Migrate()` na njima staje na „already exists".

#### Pomoć i dokumentacija

- Nov filter **🛒 Kasa** u ekranu Pomoć, sa 8 tema (kasa, pretraga, čitač, vagana roba, refundacija i obuka, smena, PFR, štampa).
- Nov dokument `docs/KASA.md`; `docs/ARCHITECTURE.md` dopunjen odeljkom 1.9.

**Testovi: 901** (bilo 737), 100% prolaznost.
## [2.44.0] - 2026-08-17

### 📤 Masovno slanje na SEF i masovno osvežavanje statusa (ERPiApp)

- **Fakture su išle na SEF jedna po jedna.** Ekran *SEF e-Fakture* je imao samo operacije nad jednim izabranim redom, pa je dan sa desetak izlaznih računa značio desetak ciklusa klik → poruka → osveži, a status svake fakture se proveravao ručno. Sada se u listi bira više redova (Ctrl/Shift) i ceo izbor ide odjednom — dugmad *📤 masovno slanje* i *🔁 masovno osvežavanje statusa*.
- **Ponovno slanje ne pravi duplu fakturu.** Prolaz šalje samo račune u statusu *NijePoslata* ili *Greška*; sve što je već na SEF-u se preskače uz obrazloženje, jer bi drugo slanje istog prometa na SEF-u otvorilo novu fakturu. Kupci bez PIB-a se preskaču takođe — fizičko lice po Zakonu o fiskalizaciji ide na PFR, ne na SEF.
- **Greška na jednoj fakturi ne obara ostale.** Svaka se beleži zasebno; na kraju ide jedan rezime (uspešno / preskočeno / greške, sa razlogom po fakturi) umesto poruke po fakturi. Nedostajući API ključ se prijavljuje jednom, bez ijednog poziva ka SEF-u.
- **Prolaz se može prekinuti** dugmetom ✋ — prekid važi od sledeće fakture, a ono što je do tada poslato ostaje poslato i vidi se u rezimeu. Između poziva stoji kratka pauza, jer SEF ograničava učestalost.
- **Osvežavanje statusa dira samo ono što čeka ishod** (status *Poslata*, sa dodeljenim SEF ID-jem); odobrene, odbijene i otkazane fakture se ne prozivaju ponovo.
- **Bez duplirane logike:** oba prolaza delegiraju na postojeće `PosaljiNaSefAsync` / `OsveziStatusNaSefuAsync`, pa pravila slanja ostaju na jednom mestu. `SefService` je dobio opcioni `SefApiClient` u konstruktoru (isti obrazac kao `PfrService`), čime je SEF sloj prvi put postao proverljiv bez mreže — 12 novih testova (`SefMasovnoSlanjeTests`), ukupno **830/830**.

## [2.43.0] - 2026-08-17

### 🔔 Bedž sa brojem pristiglih porudžbina u meniju (ERPiApp)
- **Neobrađena porudžbina se više ne primećuje tek ručnim „Osveži”.** Porudžbina rezerviše zalihu čim stigne, pa neprimećena porudžbina drži robu neprodatom. Stavka *WebShop → Pristigle porudžbine* sada nosi crveni bedž sa brojem porudžbina u statusu *Nova* ili *Čeka odobrenje*.
- **Vidljivo i kad je grupa skupljena.** Grupa *WEBSHOP (B2C / B2B)* je podrazumevano zatvorena, pa se broj dopisuje i u njen naziv — bedž na stavci unutar zatvorene grupe bio bi nevidljiv baš kad je najpotrebniji.
- **Osvežava se na 60 sekundi i odmah po obradi** (odobrenje, promena statusa, fakturisanje), preko `WebPorudzbineView.PorudzbineIzmenjene`. Koristi zaseban kratkotrajan `DbContext`, jer glavni dele svi otvoreni ekrani, a EF kontekst ne podnosi dve istovremene operacije. Nedostupna baza ne ruši prozor — bedž samo ostane na prethodnoj vrednosti.
- Bez novih zavisnosti: nije uvođen SignalR, niti se dira postojeći SystemTray put koji API već koristi za javljanje novih porudžbina.

## [2.42.0] - 2026-08-17

### 🧾 Fakturisanje web porudžbina — nalog glavne knjige sada izlazi u ravnoteži

Nijedna web porudžbina još nije bila fakturisana ni u jednoj bazi (provereno: 0 računa, 0 neuravnoteženih naloga), pa se ništa nije moralo popravljati unazad.

- **Nalog glavne knjige se nije slagao.** `KnjiziRacunAsync` gradi nalog iz **zaglavlja** računa — 2040 duguje `UkupnoZaUplatu`, 6120 potražuje `UkupnoOsnovica`, 4700 potražuje `UkupnoPdv` — a zaglavlje se prepisivalo iz porudžbine: osnovica i PDV **pre** popusta i **bez** dostave, dok je iznos za uplatu bio **posle** popusta i **sa** dostavom. Duguje minus potražuje ostajalo je tačno `TrosakDostave − popusti`, i to bez ijedne provere koja bi to prijavila (`Saldo` je samo računato svojstvo, nalog se snimao kao proknjižen). Zaglavlje se sada računa kao zbir stavki.
- **Gost-kupac je fakturisan na tuđeg partnera.** `PartnerId = porudzbina.PartnerId ?? (await _db.Partneri.FirstOrDefaultAsync())?.PartnerId ?? 1` — upit bez `OrderBy`, dakle uvek isti stvarni partner, kome su se tuđe kupovine gomilale u dugovanjima i IOS-u. Gost se sada traži po email adresi, a ako ga nema otvara se nov partner (šifra `WEB-00001`, `SifraPartnera` ima UNIQUE indeks pa se traži najveći postojeći broj).
- **Trošak dostave nije ulazio na račun.** Dodaje se kao uslužna stavka „Troškovi dostave" (bez `ArtikalId`, pa ne razdužuje magacin). `TrosakDostave` je iznos **sa PDV-om** — razdvaja se na osnovicu i PDV umesto da uđe ceo kao osnovica. Stopa prati stavku sa najvećom osnovicom (dostava prati glavno dobro).
- **Kupon i loyalty popust nisu ulazili na račun.** Raspodeljuju se **pro-rata** po bruto udelu svake stavke i ugrađuju u efektivni rabat, pa popust umanjuje osnovicu **po stopi** — jedna zbirna negativna stavka ne bi znala koju osnovicu umanjuje kod mešovitih stopa (20%/10%), a SEF/UBL odbija negativne iznose na stavci. Ostatak zaokruživanja ide na najveću stavku, da zbir stavki pogodi zaglavlje u paru.
- **Fiktivan rok plaćanja na plaćenoj porudžbini.** Kartično plaćena porudžbina dobijala je rok od 15 dana i time odmah ulazila u otvorene stavke i opomene. Sada je rok jednak datumu računa, a napomena nosi datum i autorizacioni kod.
- **Jedan kod umesto dva.** Ista logika je postojala dvaput — `WebPorudzbineView.BtnKreirajRacun_Click` (ERPiApp) i `AdminController.KreirajFakturu` (ERPiApi) — sa istim greškama u oba i već razišlim ponašanjem (samo API je imao zaštitu od dvostrukog fakturisanja, samo API je upisivao `NacinPlacanja`). Izvučeno u `WebPorudzbinaFakturisanjeService`; zaštita od dvostrukog fakturisanja sada važi za oba puta.

## [2.41.0] - 2026-08-17

### 🔒 Bezbednost — revizija javne API površine (porudžbine i plaćanje)

Nastavak revizije započete u 2.36.1. Sva četiri nalaza su potvrđena i na podignutom API-ju, na izolovanoj kopiji baze: prvo je dokazano da su bili iskoristivi na netaknutom kodu, pa da su posle ispravke zatvoreni (21 provera).

- **Tuđi predračun i IPS QR su se skidali po rednom ID-ju.** `GET /api/porudzbine/{id}/predracun-pdf` i `{id}/ips-qr` nisu proveravali ništa — `WebPorudzbinaId` je redni broj, pa je `id=1,2,3…` davalo PDF sa imenom, adresom, telefonom, stavkama i iznosom tuđe porudžbine, bez ikakvog naloga. Uvedeno polje `WebPorudzbina.JavniToken` (32 bajta iz kriptografskog generatora), koje kupac dobija samo u odgovoru na svoju porudžbinu; pristup prolazi ako se token poklapa, ako je pozivalac prijavljen kao vlasnik, ako je ovlašćeno lice iste B2B firme, ili ako je admin. Tuđi ID vraća `404` (isto kao nepostojeći), da redni ID-jevi ne bi ostali upotrebljivi za prebrojavanje prometa.
- **Webhook je proglašavao porudžbinu plaćenom bez ijednog dinara.** Provera potpisa u `POST /api/porudzbine/kartica-webhook` bila je uslovljena i time što je polje `Signature` neprazno — izostavljanjem tog polja ceo blok se preskakao i išlo se pravo na `Status = "APPROVED"` → `PlacenaKarticom`, sa izmišljenim autorizacionim kodom. U live modu je potpis od sada bezuslovan, a nepodešen tajni ključ odbija webhook (`503`) umesto da mu se veruje na reč.
- **Sandbox autorizacija kartice radila je u live modu.** `POST /api/porudzbine/kartica-sandbox-potvrdi` simulira autorizaciju i upisuje `PlacenaKarticom` bez ijednog poziva ka procesoru, a ni kontroler ni `ObradiSandboxAutorizaciju` nisu gledali `KarticeSandboxMod` — bilo je dovoljno pogoditi redni ID i poslati bilo koji broj kartice sa CVV-om. Endpoint u live modu više ne postoji (`404`), a i u sandbox modu traži dokaz vlasništva. Istu proveru dobio je i `inicijalizuj-karticu`.
- **Tuđa korpa se prepisivala poznavanjem tokena.** `POST /api/porudzbine/sinhronizuj-korpu` je korpu nalazio samo po klijentski poslatom `KorpaToken`, bez veze sa prijavljenim korisnikom, pa se tuđoj korpi mogao prepisati sadržaj, iznos i kontakt (email, ime, telefon) — a ti podaci hrane automatske podsetnike za napuštene korpe. Korpa koja pripada nalogu od sada se dira samo iz tog naloga; dodata je i gornja granica dužine tokena.

### 🔧 Ispravke
- **PDF u ERPiApi je padao na 500 osim slučajno.** QuestPDF licencu su postavljali samo `ERPiApp` i statički konstruktor `B2bPdfService`-a; u ERPiApi procesu nije bila postavljena nigde, pa je svako generisanje PDF-a koje ne prolazi kroz `B2bPdfService` (predračun kupca, **admin predračun**, adresnica) padalo — osim ako je u istom procesu pre toga zatražen neki B2B PDF i time je postavio globalno podešavanje. Postavlja se pri pokretanju (`Community`, isto kao na svim ostalim mestima u repou).

## [2.40.0] - 2026-08-17

### 🗺️ Dinamički `sitemap.xml` sa slikama & prošireni `robots.txt` (ERPiApi / WebShop)
- **Slike u mapi sajta.** `sitemap.xml` sada uz svaku adresu emituje i `image:image` zapise (Google image sitemap ekstenzija) — sve slike artikla iz `SlikeJson` i naslovnu sliku kategorije. Lokalne putanje (`/slike/12/a.jpg`) se pretvaraju u pune adrese prodavnice, tuđe CDN adrese prolaze netaknute, a zapisi koje pretraživač ne može da preuzme (`data:`, `file:`) i ponovljene slike otpadaju. Time proizvodi ulaze i u Google Images, ne samo u web pretragu.
- **Varijacije više ne ulaze kao pokvareni linkovi.** Mapa je izlistavala i artikle-varijacije (`RoditeljArtikalId`), koje katalog ne prikazuje kao zasebne proizvode — adresa `/proizvod/{šifra varijacije}` posetiocu javlja „artikal ne postoji". Sada se, kao i u katalogu, emituju samo matični artikli.
- **`lastmod` po stvarnoj izmeni.** Ranije je za **svaki** artikal stajao današnji datum, pa je mapa svakog dana tvrdila da se promenio ceo katalog — signal koji pretraživači posle par provera prestanu da uzimaju u obzir. Datum se sada čita iz audit traga (`AuditSaveChangesInterceptor` beleži kreiranje/izmenu artikla), a artikli bez traga ostaju bez `lastmod`-a.
- **Sitemap indeks za velike kataloge.** Standard dozvoljava najviše 50.000 adresa po fajlu. Preko granice, `sitemap.xml` postaje sitemap indeks koji upućuje na `/sitemap-osnovno.xml` (naslovna + kategorije) i `/sitemap-proizvodi-{n}.xml` delove; manje prodavnice i dalje dobijaju jedan fajl kao do sada.
- **`robots.txt` više ne blokira ono što robotu treba, a blokira ono što ne treba.** Dodate zabrane za `/admin`, `/b2b`, `/swagger` i `/api/admin/`; `/api/katalog/` ostaje namerno dozvoljen jer prodavnica podatke o artiklu povlači tek u pregledaču — zabrana bi Googlebot-u ostavila praznu stranicu. `Sitemap:` sada pokazuje na adresu prodavnice (isti host kao i sam `robots.txt`), a ugašena prodavnica (`WebShopOmogucen = false`) vraća `Disallow: /` umesto da pusti prazne stranice u indeks.
- **Keširanje i testovi.** Odgovori nose `Cache-Control: public, max-age=3600`. Pravila su izdvojena u `SitemapGenerator` (ERPiData) i pokrivena sa 25 testova u `SitemapGeneratorTests`.

## [2.39.0] - 2026-08-17

### 🔍 DMS OCR Automatsko Parsiranje Računa & Ulazne Kalkulacije (ERPiApp Desktop)
- **OCR uvoz na listi kalkulacija.** Dodato dugme *„🔍 OCR Uvoz računa”* u glavni toolbar `KalkulacijeView` za 1-klik učitavanje/skeniranje računa dobavljača (PDF ili slike) sa automatskim kreiranjem nove kalkulacije.
- **OCR popunjavanje u editoru kalkulacije.** Dugme *„🔍 OCR Popuni iz računa”* u `KalkulacijaEditWindow` automatski popunjava broj računa, broj otpremnice, datum izdavanja/prometa, nabavne iznose (osnovicu) i pronalazi/kreira dobavljača po PIB-u.
- **Automatsko arhiviranje u DMS.** Priloženi skenirani fajl se automatski vezuje uz kalkulaciju u bazi dokumenata sa ažuriranjem DMS bedža.
- **Proširena ekstrakcija podataka.** `OcrInvoiceParser` podržava detekciju broja otpremnice, poziva na broj i rabata dobavljača.

## [2.38.0] - 2026-08-17

### 🔐 Google Identity Services (GIS) Zvanična Prijava & Registracija
- **Zvanično Google Sign-In dugme.** Ugrađena `GoogleSignInButton.tsx` komponenta koja dinamički učitava zvanični Google Identity Services SDK (`https://accounts.google.com/gsi/client`) kada je `googleClientId` konfigurisan u CMS podešavanjima.
- **Povezivanje u formama za prijavu i registraciju.** U `NalogForme.tsx` kupci sada mogu jednim klikom da se prijave ili registruju sa svojim verifikovanim Google nalogom.
- **Loyalty nagrada za Google registraciju.** Novi kupci koji se registruju preko Google-a automatski dobijaju +50 dobrodošlica loyalty bonus bodova.
- **Višejezična podrška (i18n).** Svi tekstovi, tooltip-ovi i statusi prilagođeni su za Srpski, Engleski i Nemački jezik.

## [2.37.0] - 2026-08-17

### 🚚 Integracija sa kurirskim API-jem (PostExpress / Bex Live & Sandbox)
- **Live provera statusa pošiljke.** Novi endpoint `GET /api/admin/porudzbine/{id}/kurir-status` i dugme *„⚡ Live Kurir API Status”* u detaljima porudžbine sa vizuelnom vremenskom linijom kretanja paketa (Najavljeno ➔ Preuzeto ➔ U tranzitu ➔ Na dostavi ➔ Uručeno primaocu) sa tačnim datumom, satnicom i lokacijom.
- **Masovno kreiranje pošiljki za 1-klik.** Na tabu *Porudžbine* dodata je mogućnost višestruke selekcije sa checkbox-ovima i trakom za masovno kreiranje tovarnih listova (`POST /api/admin/porudzbine/masovno-kreiraj-posiljke`) za izabranu kurirsku službu (PostExpress, Bex, DExpress, Aks).
- **Proširena podrška za kurirske API-je.** `KurirskaSluzbaService` podržava automatsko kreiranje pošiljki, dodelu tracking kodova, kalkulaciju poštarine i proračun otkupnine sa dinamičkim prebacivanjem između Live API i Sandbox režima.

## [2.36.1] - 2026-08-17

### 🔒 Bezbednost — kredencijali platnog procesora više ne izlaze na javni endpoint
- **`GET /api/katalog/podesavanja` je vraćao tajne svakom anonimnom posetiocu.** Taj endpoint nije (i ne treba da bude) pod prijavom — prodavnica ga zove pri svakom učitavanju stranice — a u odgovoru su bili **Secret Key / 3DS HMAC ključ i API Key platnog procesora**, Merchant i Terminal ID, kao i **SMS API ključ i tajna** te Viber Service ID. Bili su čitljivi golim otvaranjem adrese u pregledaču, bez ikakvog naloga. Uklonjeni su iz javnog odgovora; ostali su samo prekidači i naziv procesora (`karticeProcesor`, `karticeSandboxMod`, `dozvoliKarticePlacanje`), po kojima prodavnica crta način plaćanja.
- **Backoffice forma sada čita podešavanja sa admin endpointa.** CMS forma je polja za tajne punila iz istog javnog odgovora — zato su tajne i bile tamo. Sada se dopunjava sa `GET /api/admin/podesavanja` (pod `[Authorize(Roles = "Admin")]`), pa administrator vidi i uređuje ključeve kao i pre, a čuvanje ih ne prepisuje praznim vrednostima.

### ⚙️ Podešavanja koja se nisu mogla popuniti nigde
- **Google OAuth Client ID.** Provera da je Google token izdat baš za ovu prodavnicu (audience) uvedena je u 2.29.2 i kod ju je čitao, ali polje nije postojalo ni u jednoj formi — moglo se popuniti samo ručnom izmenom baze. Dodato u `ERPiApp` → *Podešavanja → WebShop*, uz Google Analytics i Meta Pixel ID.
- **Podrazumevana masa artikla (kg).** Po njoj kurirske službe daju cenu dostave kad artikal nema unetu masu; postojala je u modelu i u API-ju, ali bez polja u formi. Dodata uz troškove dostave.

## [2.36.0] - 2026-08-17

### 🌍 Multi-jezik (i18n — Srpski 🇷🇸 / Engleski 🇬🇧 / Nemački 🇩🇪)
- **Kompletna višejezičnost na klijentu i serveru.** Podrška za 3 jezika (Srpski, Engleski, Nemački) kroz ceo proces pretrage, navigacije i kupovine.
- **Backend dvojezična/trojezična pretraga.** `KatalogController` u upitima za pretragu proizvoda i Live Search autocomplete-u pretražuje po `NazivEn` i `NazivDe` pored osnovnog naziva i šifre artikla.
- **Lokalizovane forme, korpa, checkout, lista želja i upoređivanje.** Svi tekstovi dugmadi, poruka, praznih stanja, modala i procesa poručivanja prevedeni su i automatski se prilagođavaju izabranom jeziku.
- **Prevod naziva i opisa artikala i kategorija.** Dinamičko mapiranje naziva kategorija i artikala prema izabranom jeziku (`prevediProizvod`, `prevediKategoriju`).

### 🛒 Oporavak napuštenih korpi sada radi sam, a kupon stvarno prolazi na kasi
- **Pozadinski servis, ne samo dugme.** Verzija 2.35.0 je oporavak napuštenih korpi opisala kao „automatski u pozadini”, ali ga je pokretalo isključivo admin dugme — kupac koji napusti korpu preko noći ne bi dobio ništa do jutra. Dodat je `NapusteneKorpeBackgroundService` u `ERPiApi` koji na svakih 15 minuta sam odrađuje prolaz. Dugme *„⚡ Pokreni automatski oporavak”* ostaje i pokreće isti kod.
- **Prekidač u CMS-u sada nešto radi.** `NapusteneKorpeAutomatskiOmoguceno` se do sada upisivao u bazu i imao prekidač u admin panelu, ali ga nijedna linija koda nije čitala — isključivanje automatike bilo je bez efekta. Sada se čita pri svakom prolazu, pa isključivanje odmah zaustavlja slanje, bez restarta API-ja. Ručno slanje podsetnika za pojedinačnu korpu prekidač namerno ignoriše — to je izričita akcija administratora.
- **Promo kupon iz podsetnika više nije mrtav kod.** Podsetnik je reklamirao kod (`VRATISE5`) i procenat popusta, ali kupon sa tim kodom nikad nije bio zaveden u šifarnik — a naplata traži aktivan `WebKupon`, pa je kupac na kasi dobijao odbijanje. Kupon se sada zavodi (i produžava) pri slanju, sa procentom iz podešavanja i rokom od 7 dana. Kupon koji je administrator ručno napravio pod istim kodom se **ne prepisuje** — njegova pravila (procenat, minimalni iznos, ograničenje iskorišćenja) ostaju nedirnuta.
- **Neuspelo slanje ne troši korpu.** Korpa se obeležava kao „podsetnik poslat” samo kad je email ili SMS stvarno otišao. Do sada se obeležavala i kad je SMTP bio nedostupan, čime je trajno izlazila iz svakog sledećeg prolaza — kupac nikad ne bi dobio podsetnik, a administrator to ne bi nigde videlo. Ručno slanje u tom slučaju vraća jasnu grešku umesto poruke „uspešno poslato”.
- **Prag čekanja, popust i kupon kod i u desktop CMS-u.** `ERPiApp` → *Podešavanja → WebShop* dobija prekidač automatike i tri polja koja su do sada postojala samo u web admin panelu.
- Logika je izvučena iz `AdminController` u `NapusteneKorpeOporavakService` (`ERPiData`), pa pozadinski servis, admin dugme i ručni podsetnik dele isti kod umesto tri kopije.

### 🔒 Bezbednost — prijava preko Google naloga
- **Nepotvrđena Google email adresa više ne prolazi.** Provera potpisa uvedena u 2.29.2 dokazuje da je token Google-ov, ali ne i da je adresa u njemu proverena. Pošto se prijava vezuje na postojeći ERPi nalog po email adresi, nalog na tuđem Workspace domenu sa email-om postojećeg kupca mogao je da preuzme njegovu prijavu. Endpoint sada odbija token bez `email_verified`.

### 🔧 Ispravke
- **Service Worker više ne pada na ne-GET zahtevima.** `cache.put` na POST zahtevu baca izuzetak; sada se keširaju samo GET zahtevi.
- **Isključivanje PWA sada važi i za postojeće posetioce.** Registrovan Service Worker je nastavljao da služi keširanu verziju sajta i posle isključivanja PWA u CMS-u — sada se odjavljuje i briše keš, pa isključivanje ne važi samo za nove posetioce.

## [2.35.0] - 2026-08-17

### 🛒 Automatizacija Oporavka Napuštenih Korpi (Abandoned Cart Recovery Automation)
- **Masovno slanje podsetnika iz Backoffice-a.** Sistem identifikuje kupce koji su dodali artikle u korpu a nisu dovršili narudžbinu i šalje im email i SMS podsetnik sa sačuvanim stavkama i podsticajnim kupon kodom. (Prolaz je u ovoj verziji pokretalo isključivo admin dugme — pozadinska automatika je dodata u 2.36.0.)
- **Konfigurabilna pravila u CMS-u.** Administrator može podešavati sate čekanja pre slanja (npr. 2h), procenat popusta (npr. 5%) i promo kupon kod (`VRATISE5`) sa prekidačem za aktivaciju.
- **1-Klik pokretanje automatskog oporavka.** U tabu *Napuštene korpe* u admin panelu dodato je dugme **„⚡ Pokreni automatski oporavak”** za trenutno procesiranje svih kvalifikovanih korpi uz live izveštaj o broju poslatih email i SMS obaveštenja.

### 🔧 Ispravke
- **Dodata EF migracija za nova podešavanja WebShop-a** (mobilni UX, PWA i automatski oporavak korpi). Bez nje su te kolone postojale samo kroz `EnsureColumn` popravku pri pokretanju, pa je baza kreirana migracijama ostajala bez njih. Podrazumevane vrednosti u migraciji usklađene su sa modelom (funkcije uključene, prag 2h, popust 5%, kupon `VRATISE5`) — inače bi baze migrirane EF-om dobile te funkcije isključene, a one popravljene kroz `EnsureColumn` uključene.

## [2.34.0] - 2026-08-17

### 📦 Štampa Kurirskih Adresnica & Slanje PDF Fakture sa NBS IPS QR Kodom
- **1-Klik štampa kurirske adresnice (A6).** Iz liste ili detalja porudžbine u admin panelu jednim klikom se generiše standardna PDF nalepnica A6 sa podacima pošiljaoca, kupca, otkupnine i bar-koda spremna za lepljenje na paket.
- **Zvanični PDF predračun / faktura.** Generisanje A4 memoranduma sa stavkama i NBS IPS QR kodom za instant plaćanje m-bankingom.
- **Slanje računa na email.** Dugme za 1-klik slanje PDF računa na email adresu kupca uz trenutnu potvrdu.

## [2.33.0] - 2026-08-17

### 🔍 Instant Live Search (Autocomplete pretraga u realnom vremenu)
- **Brzi padajući meni sa predlozima.** Čim kupac unese najmanje 2 slova u polje za pretragu u zaglavlju (na desktopu i na mobilnom telefonu), otvara se plutajući meni sa direktnim rezultatima: sličicama artikala, šiframa, nazivima, brendovima, cenama, statusom lagera i pogođenim kategorijama.
- **Tastaturna navigacija.** Podrška za izbor stavki strelicama na tastaturi (`ArrowDown` / `ArrowUp`) i `Enter` za instant otvaranje.

## [2.32.0] - 2026-08-17

### 🏢 B2B Veleprodajni Portal — Matrica varijanti & Slanje cenovnika na Email
- **B2B Matrica za brzo poručivanje varijanti.** Komercijalisti mogu izabrati model artikla (npr. radna odela, majice, obuću) i uneti količine po svim bojama i veličinama odjednom u preglednu 2D tabelu uz prikaz zaliha i međuzbirova, te jednim klikom dodati ceo paket u korpu.
- **Slanje B2B cenovnika/lagera na Email.** Na tabu *Fakture i dugovanja* partner jednim klikom može zatražiti slanje ažurnog cenovnika (PDF, Excel ili oba) sa svojim ugovorenim cenama i raspoloživim zalihama na svoj email.

## [2.31.0] - 2026-08-17

### 📱 Mobilni UX, CRO i PWA podrška
- **Sticky „Dodaj u korpu” traka na dnu ekrana.** Kada kupac na mobilnom uređaju skroluje kroz opis artikla, fiksna traka na dnu omogućava instant kupovinu sa biranjem količine u 1 klik.
- **Touch Swipe galerija & Lightbox Zoom.** Glatko listanje slika prevlačenjem prsta na dodirnim ekranima, tačkasti indikatori i uvećanje visoke rezolucije preko celog ekrana.
- **PWA (Progressive Web App).** Web manifest i Service Worker sa pozivom posetiocu za instalaciju aplikacije na početni ekran bez adresne trake.
- **Dugme „Popuni podrazumevano”.** U CMS administratorskim podešavanjima omogućeno je 1-klik popunjavanje preporučenih postavki.

### 🎨 Varijante artikala (Boja / Veličina / Pakovanje)
- **Grupisanje varijacija proizvoda.** Svaka varijanta je pun artikal u ERP šifarniku sa svojom šifrom, barkodom i zalihom, dok kupac na webu bira varijaciju (boje, veličine) na jednoj kartici uz trenutnu zamenu cene i stanja.

### 🌐 Google Schema.org Rich Snippets & Social Share
- **Strukturirani JSON-LD podaci za Google.** Automatsko generisanje `Product`, `Offer`, `AggregateRating`, `Review`, `BreadcrumbList`, `WebSite` i `Organization` šema za prikaz žutih zvezdica, ocena, cena i stanja lagera u Google rezultatima pretrage.
- **OpenGraph & Twitter kartice.** Dinamički meta tagovi za atraktivan prikaz slike, cene i opisa prilikom deljenja linkova na društvenim mrežama i chat servisima (WhatsApp, Viber, Facebook).
- **Social Share vidžet.** Dugme „Podeli” na stranici artikla sa 1-klik akcijama za WhatsApp, Viber, Facebook i kopiranje linka.

## [2.30.1] - 2026-08-17

### 👁️ Admin dobija uvid u B2B tim i porudžbine na čekanju
- **Admin panel (`/admin`) sada vidi zašto je porudžbina na čekanju.** Lista porudžbina i stranica detalja pokazuju razlog — prekoračen kreditni limit (rešava admin) ili odobrenje ovlašćenog lica firme (rešava kolega na `/b2b/tim`, ne admin) — umesto golog statusa "Čeka odobrenje" bez objašnjenja.
- **Kupci tab pokazuje veličinu B2B tima i broj sačuvanih adresa.** Kad firma ima više naloga na `/b2b/tim`, admin vidi koliko ih je i ko je odobravalac; kad ima sačuvane adrese isporuke, vidi koliko. Sve informativno, bez novih akcija za admina — uređivanje ostaje na B2B portalu.

## [2.30.0] - 2026-08-17

### 🏢 B2B portal — adrese isporuke, personalizovani cenovnik, multi-user odobravanje
- **Firma sa više lokacija konačno ne mora da prekucava adresu svaki put.** Novi tab *Adrese isporuke* na `/b2b/adrese` čuva neograničen broj mesta isporuke (magacini, filijale), svako sa nazivom, kontakt osobom i telefonom; jedno se može označiti kao podrazumevano. Pri poručivanju se bira iz padajuće liste umesto ručnog unosa — ručan unos i dalje radi, ništa nije oduzeto.
- **Partner sada može sam da preuzme svoj cenovnik.** Nova dugmad na *Fakture i dugovanja* generišu ceo cenovnik (PDF i Excel) sa tačno onim cenama koje taj partner vidi — ugovorenim gde postoje, standardnim inače, ista formula kao katalog i porudžbenica.
- **Firme sa više zaposlenih dobijaju pravi tim.** Novi tab *Tim firme* (`/b2b/tim`): ovlašćeno lice dodaje kolegama naloge i bira ko sme da odobrava porudžbine. Porudžbina zaposlenog bez tog prava sad čeka odobrenje kolege pre dalje obrade — vidljivo u istom redu čekanja u kom je do sada čekala samo porudžbina koja prelazi kreditni limit. Pojedinačni B2B nalozi (bez kolega) rade potpuno isto kao do sada, bez ikakvog dodatnog koraka.

## [2.29.2] - 2026-08-17

### 🔒 Bezbednosna ispravka — prijava preko Google naloga
- **Prijava preko Google naloga sada stvarno proverava da je token pravi.** Do sada je `/api/auth/google-login` samo pročitao email iz tokena bez ikakve provere potpisa — teoretski je bilo ko mogao ručno sastaviti izmišljen token sa proizvoljnim email-om i dobiti validan ERPi nalog/prijavu, zaobilazeći lozinku u potpunosti. Endpoint sada kriptografski proverava potpis protiv Google-ovih javnih ključeva (`Google.Apis.Auth`).
- Frontend dugme za Google prijavu i dalje šalje probni (ne pravi Google) token dok se ne završi prava GIS integracija — do tada će prijava preko Google naloga vraćati grešku umesto da tiho propušta neproverene tokene.
- Novo podešavanje `GoogleClientId` (WebShopPodesavanja) — kad se popuni pravim OAuth Client ID-jem iz Google Cloud Console, dodaje se i provera da je token izdat baš za ovu prodavnicu (audience), pored provere potpisa koja važi uvek.

## [2.29.1] - 2026-08-16

### 🔧 Ispravka
- Nedostajala EF Core migracija za tabelu statistike poseta (`WebPosete`) — postojala je samo kao raw SQL šema. Dodata prava migracija, isti obrazac kao ostale WebShop tabele.

## [2.29.0] - 2026-08-16

### 📊 Statistika poseta na admin dashboard-u
- **Dashboard prodavnice sada pokazuje ko je zapravo dolazi.** Do sada nije postojao nikakav uvid u saobraćaj WebShop-a — ni broj poseta, ni koliko je od toga stvarno različitih ljudi. Nova kartica na *Pregled poslovanja* pokazuje broj poseta danas i ovog meseca, broj jedinstvenih posetilaca, i grafikon po danima za poslednjih 30 dana.
- Broje se svi posetioci, i gosti i prijavljeni kupci — sopstvene posete admina kroz *Admin panel* se ne računaju, da ne naduvavaju brojku.

### ✏️ Editovanje web/B2B korisnika
- **Podaci kupca konačno mogu da se isprave posle registracije.** Do sada je jedino moglo da se odobri ili blokira nalog — pogrešan email, telefon ili naziv firme ostajali su trajno pogrešni. Novo dugme za izmenu (WPF *Web & B2B Korisnici* i web admin *Kupci* tab) otvara formu sa kontakt podacima, podacima firme i ručnim povezivanjem sa partnerom iz ERP šifarnika.
- Koristi se i kad automatsko povezivanje partnera pri odobravanju B2B naloga promaši ili je preskočeno — sad postoji način da se to naknadno ispravi bez brisanja i ponovne registracije naloga.

## [2.28.0] - 2026-08-16

### 🏗️ Nedovršena proizvodnja konačno ima svoje mesto u bilansu
- **Radni nalog koji je o zatvaranju meseca još u radu više ne kvari rezultat perioda.** Do sada je takav nalog nosio trošak (razdužen materijal, zarade, amortizaciju), a zaliha koja mu odgovara ulazila je u knjige tek po završetku — pa je mesec ispadao lošiji nego što jeste, a sledeći bolji. Novo dugme **🏗️ Nedovršena proizvodnja** na ekranu *Radni nalozi* radi obračun na zadati dan: duguje konto nedovršene proizvodnje (1100), potražuje isti konto povećanja vrednosti zaliha učinaka (6300) kao i gotov proizvod.
- **Pre knjiženja vidite tabelu** — koji su nalozi bili u radu tog dana, koliko svaki vredi, koliko je za njega već proknjiženo i koliko se sada knjiži. Ništa se ne menja dok ne potvrdite, a nalog Glavne knjige nastaje kao **nacrt**.
- **Knjiži se samo razlika**, ne cela vrednost svaki put. Zato ponovno pokretanje istog obračuna ne knjiži ništa, sledeći mesec knjiži samo prirast, a konto uvek pokazuje tekuće stanje.
- **Storno ne morate da radite.** Kad nalog završite, njegovo knjiženje samo skida iznos sa konta nedovršene proizvodnje — trošak tada prelazi na gotove proizvode. U nalogu završetka to piše kao *Skidanje nedovršene proizvodnje*.
- **Materijal ulazi tek kad je stvarno razdužen sa magacina**, po datumu trebovanja ili razduženja. Dok je sirovina na zalihama, ona je već iskazana na kontu materijala i ovde se ne broji dvaput. Rad i mašine ulaze po fazama zaključno sa danom obračuna.
- Konto se bira u *Podešavanja → Proizvodnja → Nedovršena proizvodnja*; prazno polje znači 1100. Novo poglavlje u *Uputstvu za Proizvodnju* i nova tema u F1 pomoći.

## [2.27.0] - 2026-08-16

### 🖼️ Slike artikala se konačno mogu uneti — i to za ceo katalog odjednom
- **Uvoz slika iz foldera povezuje fotografije sa artiklima po šifri.** `27052.jpg` postaje glavna slika artikla 27052, a `27052_2.jpg` i `27052_3.jpg` njegove dodatne slike. Do sada su se adrese slika kucale rukom, jedna po jedna — za katalog od nekoliko hiljada artikala to niko nije mogao da odradi, pa je prodavnica stajala sa praznim sličicama.
- **Pre bilo kakvog upisa dobijate pregled** — koliko je datoteka povezano, koje šifre ne postoje u katalogu i koji artikli već imaju slike. Ništa se ne menja dok ne potvrdite.
- **Uređivanje slika jednog artikla je sada galerija** (WebShop → Web artikli i objave → *Uredi Web podatke & Slike*): prevucite datoteke mišem ili ih izaberite sa diska, menjajte redosled strelicama, brišite pojedinačno. Prva slika je glavna i vidno je označena.
- **Velike fotografije se same smanjuju** na razumnu veličinu i dobijaju sličicu za prikaz u katalogu, pa slika sa telefona od 5 MB više ne usporava prodavnicu.
- **Slike se čuvaju uz bazu firme i ulaze u rezervnu kopiju**, tako da se pri prenosu na drugi računar ne gube. Starije rezervne kopije, napravljene bez slika, i dalje se vraćaju normalno.

### 📦 Objavljivanje na web više nije artikal po artikal
- **Dugme „Objavi izabrane"** objavljuje sve označene artikle odjednom, uz dodelu web kategorije i popunjavanje web naziva. Uobičajen tok je: filtrirajte „Neobjavljeni na webu", označite sve (Ctrl+A) i objavite.
- Artiklima koji već imaju kategoriju može se ostaviti postojeća, da masovna dodela ne pregazi ručno sređene.

### 🔎 Artikal se otvara kao svoja stranica, ne kao prozorčić preko kataloga
- **Adresa `/proizvod/<šifra>` sada otvara punu stranicu** sa zaglavljem, putanjom (Početna › kategorija › artikal) i podnožjem. Podeljen link vodi na uredno pripremljenu stranicu, a ne na katalog sa prozorčićem preko njega.
- **Google sada vidi stranicu proizvoda** kao proizvod — sa nazivom, šifrom, cenom i podatkom o dostupnosti.
- Dodato stanje kada artikal ne postoji ili je povučen sa weba, umesto praznog prozora.

### 🏷️ Ispravke prikaza cena
- **Nestala je oznaka „--Infinity%"** na artiklima kojima je redovna cena vođena kao nula. Takav artikal se više ne prikazuje kao sniženje i nema precrtanu nulu pored cene.
- **„Ponuda dana" na početnoj više ne izmišlja popust.** Kada nijedan artikal nije stvarno snižen, prikazuje se kao istaknuta ponuda, bez oznake uštede i bez precrtane cene.

### 🖧 Prodavnica konačno radi i kad je firma na serveru
- **Servis više ne odbija firmu na SQL Serveru ili PostgreSQL-u.** Do sada je podatke o vezi sa serverom prosleđivao pogrešno sklopljene, pa se baza nije mogla otvoriti — WebShop je za takve firme bio neupotrebljiv, i preko dugmeta *Pokreni Servis* i ručnim pokretanjem. Firme sa lokalnom bazom (`.db`) ovo nikad nije pogađalo.
- **Podatak o tome koja je firma aktivna sada obuhvata i firme na serveru**, pa servis pokrenut bez argumenata otvara pravu firmu umesto da traži lokalnu datoteku koje nema.
- **Konzola servisa više ne ispisuje lozinku baze** — umesto celih podataka o vezi ispisuje se samo koja je vrsta baze u pitanju.

## [2.26.0] - 2026-08-16

### 🗓️ Režija se deli po mesecima — nalog koji traje preko dva meseca više ne nosi režiju samo jednog
- **Nalog koji ste počeli u maju a završili u junu sada dobija deo režije oba meseca**, srazmerno tome koliko je u kom mesecu odrađeno. Do sada je ceo nalog padao u jedan mesec, pa je i režiju uzimao samo iz njega — nedovršena proizvodnja je time bila sistematski pogrešno opterećena.
- **Podela ide po datumima faza.** Faza bez datuma pripada mesecu naloga, tako da kod koga se datumi faza ne vode ništa se ne menja u odnosu na raniju verziju.
- **Svaki mesec deli svoju režiju sa svojim nalozima** — sati koje je drugi nalog odradio u junu ne ulaze u majski imenilac.
- **Poruka posle pritiska na dugme pokazuje razradu po mesecima** — za svaki mesec režija tog meseca, osnova ovog naloga i iznos koji mu pripada.
- **Mesec koji još nema proknjiženu režiju ne donosi ništa.** Ako nijedan mesec naloga nema režiju, važi staro pravilo — uzima se poslednji raniji mesec sa režijom.
- **Ključ „vrednost direktnog materijala" ostaje u jednom mesecu**, jer ne postoji podatak o tome kog meseca je materijal izdat sa kartice.
- Dopunjeno *Uputstvo za Proizvodnju* i F1 pomoć.

## [2.25.1] - 2026-08-16

### 🛒 WebShop više ne pokazuje praznu prodavnicu kad se servis pokrene ručno
- **ERPi sada pamti u kojoj ste firmi.** Pri ulasku u firmu upisuje se marker aktivne baze, pa servis (ERPiApi) pokrenut ručno iz konzole — bez dugmeta *Pokreni Servis* — otvara **tu** bazu umesto prve datoteke koju nađe. Ranije je to po pravilu bila prazna podrazumevana `erpi.db`, pa je WebShop prikazivao 0 artikala i 0 kategorija iako je firma puna robe, i to bez ijedne poruke o grešci.
- **Ako markera nema** (servis pokrenut pre prvog ulaska u firmu), uzima se **najskorije menjana** baza umesto prve po redu, a u konzoli i dalje stoji upozorenje da je baza pogođena.
- **Korpa na WebShop-u više ne ruši izmenu stavki** kad je kupac ostao prijavljen tokenom izdatim nad drugom bazom — takva korpa se čuva kao anonimna umesto da svaka izmena vrati grešku 500.
- **Neuspela izrada rezervne kopije iz izbora firme** više ne ostavlja aplikaciju prikačenu na bazu tuđe firme.

## [2.25.0] - 2026-08-16

### 🧾 Režija se sada uzima iz Glavne knjige — poslednji ručan unos u ceni koštanja je nestao
- **Opšti troškovi (režija) su bili jedina stavka cene koštanja koju ste morali sami da procenite.** Sada u tabu *Kalkulacija cene koštanja* pritisnete **🧾 Režija iz Glavne knjige** i program upiše deo stvarno proknjižene režije koji pripada tom nalogu. Time sve četiri stavke cene koštanja dolaze iz podataka: materijal sa kartica, rad iz zarada, amortizacija mašina iz osnovnih sredstava, režija iz Glavne knjige.
- **Uzima se samo ono što je proknjiženo**, sa konta koja izaberete u *Podešavanja → Proizvodnja → Konta režije* (podrazumevano grupe **53** i **55**). Nacrti naloga se ne broje, a storno stavka umanjuje režiju umesto da je uveća.
- **Grupe 51, 52 i 54 se namerno ne uzimaju** — materijal, zarade i amortizacija mašina već ulaze u cenu koštanja direktno, pa bi tu ušli po drugi put.
- **Vi birate po čemu se deli**: po ostvarenim satima rada (podrazumevano), po satima mašina ili po vrednosti utrošenog materijala — zavisno od toga šta u vašoj proizvodnji stvarno nosi opšte troškove.
- **Ako režijske stavke nose mesto troška, podela ide po mestu troška** — nalog vuče samo iz režije svog pogona. Ako mesta troška ne koristite, cela režija ide u jednu podelu.
- **Uzima se poslednji mesec koji ima proknjiženu režiju**, zaključno sa mesecom naloga — dakle mesec u kom se radilo, a ne prethodni. Dugme pritisnite po zatvaranju meseca; kao ni satnica iz zarada, ni ovaj iznos se ne osvežava sam.
- **Kad režija ne može da se izvede, poruka kaže zašto** — nema konta režije u kontnom planu, nema proknjižene režije, ili nalog nema osnovu za raspodelu. Ono što ste uneli ručno se u svakom slučaju zadržava.
- Novo poglavlje u *Uputstvu za Proizvodnju* i nova tema u F1 pomoći.

## [2.24.0] - 2026-08-16

### 🛠️ Cena sata mašine se sada računa iz amortizacije
- **Do sada je cena sata mašine bila jedini ručan unos u ceni koštanja pored režije.** Sada u fazi izrade izaberete **mašinu** (osnovno sredstvo) i pritisnete **🛠️ Cene mašine iz amortizacije** — program uzme amortizaciju koju ste za to sredstvo stvarno proknjižili i podeli je satima koje je ta mašina odradila u istom periodu. Isti postupak kao dugme **👷 Satnice iz zarada**, samo za mašinski deo troška.
- **Uzima se poslednji obračunat period, ne tekući.** Sati tekućeg perioda se još skupljaju, pa bi vam ista mašina davala drugu cenu na svako otvaranje naloga.
- **Radi bez obzira kako knjižite amortizaciju** — godišnje, kvartalno ili mesečno. Period se izvodi iz razmaka između dva obračuna, ne iz teksta u kartici.
- **U račun ulaze sati svih naloga** koji su koristili tu mašinu u tom periodu, ne samo sati ovog naloga: amortizacija je zajednički trošak i deli se na sav rad mašine.
- **Kad cena ne može da se izvede, poruka kaže zašto** — nema dodeljene mašine, nema proknjižene amortizacije, ili mašina u tom periodu nema evidentiranih sati. Ono što ste uneli ručno se u svakom slučaju zadržava.

### 🏭 Proizvodnja je dobila svoje uputstvo
- **Novo `Uputstvo za Proizvodnju`** (Pomoć → 🏭 Proizvodnja → *Otvori HTML uputstvo*) — do sada je jedini modul sa punim ekranima bio bez svog uputstva. Trinaest poglavlja: sastavnice i verzionisanje normativa, radni nalozi, faze izrade, kako se sastavlja cena koštanja, satnice iz zarada, cena mašine iz amortizacije, šta se dešava pri završetku naloga, konta za knjiženje, oba režima rasknjižavanja, varijanse i česta pitanja.
- **Sedam novih tema u F1 pomoći** za Proizvodnju, sa novim dugmetom za filtriranje.

### 🐛 Baze na PostgreSQL i SQL Server serveru se konačno nadograđuju
- **Ako vam baza firme stoji na serveru (PostgreSQL ili SQL Server), nova verzija programa joj do sada nije donosila nove tabele ni nova polja.** Program je novu šemu pravio samo ako je baza bila potpuno prazna; svaka baza koja je već korišćena ostajala je na staroj šemi i program bi na novom ekranu prijavio grešku. Sada se zatečena baza dopunjava pri svakom otvaranju — dodaje se ono što nedostaje, a postojeći podaci se ne diraju.
- **Provereno nad živim serverima** (PostgreSQL 17 i SQL Server 2022): i nova i zatečena baza prolaze, ponovljeno otvaranje ne menja ništa.
- Napomena: dopunjavanje je namerno samo *dodavanje* — tabele, polja i indeksi kojih nema. Ništa se ne briše i ne preimenuje.

## [2.23.6] - 2026-08-16

### 🐛 Uvoz zarada više ne gubi drugi obračun istog meseca
- **Radnik koji u jednom mesecu ima dva obračuna dobijao je samo jedan.** U vašoj živoj bazi zarada tako stoje dva obračuna za 7/2026 — neto **100.719,50** i **22.222,00**, nijedan storniran — a u ERPi je prelazio samo prvi. Isto se dešavalo i kad su dva obračuna pod različitim isplatama (akontacija i konačna). Sada prelaze svi: provereno nad pravim podacima, 5.002 obračuna u izvoru → 5.002 u ERPi, a ponovni uvoz i dalje ne dodaje nijedan.
- **Uvoz iz starih DBF fajlova više ne pravi lažne duplikate.** Isti mesec stoji i u tekućem i u istorijskom fajlu (npr. `OBRACUN.DBF` i `OBRACUNI.DBF`), a program je duple zapise odbijao proverom koja ne vidi zapise koji čekaju upis u seriji od 500. Tako je u međubazu ulazilo **24 dupla obračuna, 96 duplih zapisa radnih sati i 23 njihova odbitka**. Sada se odbijaju čim se pojave.
- **Ispravka merenja iz 2.23.5**: od 75 odbitaka koje je uvoz gubio, **23 su bila upravo ta lažna dupla**, a **52 su stvarna** (npr. isti kredit od 550,00 upisan u dva polja istog obračuna). Konačno stanje nad proverenim podacima: **4.984 obračuna, 5.261 zapis radnih sati, 3.400 odbitaka i 378 kredita — identično sa obe strane lanca**.

## [2.23.5] - 2026-08-16

### 🐛 Uvoz zarada više ne gubi ponovljene odbitke
- **Dva ista odbitka istog radnika u istom mesecu spajala su se u jedan.** U proverenoj DOS instalaciji tako je nestajalo **75 od 3.423 obustave**. Dva su stvarna izvora: ista šifra obustave upisana dvaput u isti obračun (npr. dva puta kredit od 550,00), i dve različite obustave iz šifarnika pod istim skraćenim nazivom i sa istim iznosom (naziv u starom programu staje u deset znakova, pa se npr. dva osiguranja vozila zovu isto). U oba slučaja je stari program iznos skinuo **dvaput**, pa oba reda moraju stići i u ERPi — inače bi radniku pri ponovnom obračunu nestala polovina obustave.
- Ponovni uvoz i dalje ne dodaje nijedan zapis: provereno nad pravim podacima, oba prolaza daju istih 3.423 odbitka i 378 kredita.

## [2.23.4] - 2026-08-16

### 💳 Krediti i obustave iz starog DOS programa se konačno uvoze
- **Fajl `KREDIT.DBF` do sada nije ni otvaran.** Iz starog programa su dolazile samo rate skinute u pojedinačnim obračunima, dok registar obustava — od kada do kada kredit traje, koliko ima rata, koliko je otplaćeno i koliko ostaje — nije postojao. Sada se uvozi: posle uvoza ekran *Zarade → Krediti* pokazuje pun plan otplate po radniku.
- **Primalac, žiro račun i poziv na broj** se povlače iz šifarnika obustava, pa nalog za prenos ima kome da uputi ratu — bez toga se rata skidala radniku, a nije se znalo kome ide.
- **Obustave se razvrstavaju** na kredite, administrativne zabrane, sudske zabrane, zakonsko izdržavanje i sindikalnu članarinu. Od te podele zavisi redosled naplate kad neto ne pokrije sve obustave; nepoznat naziv ostaje poslednji u redu, da ne bi dobio prvenstvo bez osnova.
- **Krediti koji još traju nastavljaju da se skidaju u ERPi-ju.** Stari program plan otplate vodi i za mesece unapred, pa se zna tačno šta je naplaćeno do poslednjeg obračuna a šta tek dolazi — preostali dug je zbir nenaplaćenih rata, a ne procena.
- **Provereno nad pravim podacima** (`C:\PLATA\PLATA\KOR28`, 25 godina obračuna): **378 kredita i obustava sklopljeno iz 3.338 pojedinačnih rata** za 33 radnika, ukupno 4.620.468,19 RSD — do dinara isto koliko piše u DBF fajlu. Bez ijednog upozorenja ili greške; ponovni uvoz ne dodaje nijedan zapis.

### 🐛 Ispravke uvoza
- **Dve obustave istog radnika koje se ni po čemu ne razlikuju više se ne spajaju u jednu.** Naziv obustave u starom programu staje u deset znakova, pa isti radnik ume da ima dve različite obustave pod istim nazivom i sa istim planom otplate — u proverenim podacima tri takva slučaja. Druga je do sada tiho nestajala pri prenosu u ERPi.
- **Potvrđena idempotentnost i za uvoz iz žive ERPiZarade baze** (drugi put isti uvoznik, druga vrata): 6.963 kartona radnika i 5.001 obračun; drugi uvoz završi za pola sekunde i ne doda nijedan zapis.

## [2.23.3] - 2026-08-16

### 🔁 Ponovni uvoz iz starih DBF baza (Zarade) više ne prekida posao
- **Uvoz pokrenut po drugi put je padao odmah na početku** i nije prenosio ništa — ni obračune, ni radne sate, ni obustave. Uzrok: radnik bez upisanog JMBG-a (u proverenoj DOS instalaciji 217 od 6.873 kartona) nije mogao da se prepozna kao već uvezen, pa je program pokušavao da ga zavede kao novog partnera pod šifrom koja je već zauzeta. Sada se takav radnik prepoznaje po svojoj šifri i uvoz uredno preskače ono što već postoji.
- Provereno nad pravim podacima: drugi uvoz istih 6.873 radnika i 4.984 obračuna završi za pola sekunde i **ne doda nijedan zapis**.
- Potvrđeno da uvoz **ne gubi podatke**: manji broj obračuna i radnih sati u odnosu na pročitane iz DBF-a (24 i 96) su isključivo dupli zapisi istog perioda — stari program isti mesec drži i u tekućem i u istorijskom fajlu.

## [2.23.2] - 2026-08-16

### 📖 Dokumentacija
- **Proizvodnja konačno ima uputstvo.** Ceo modul do sada nije bio pomenut ni u jednom fajlu pomoći (F1) — dodato je poglavlje sa tokom posla od sastavnice do zaliha, objašnjenjem kako se računa cena koštanja, i šta tačno rade potpuno i delimično rasknjižavanje.
- Ispravljen opis uvoza iz starih DBF baza — zatečeni tekst je opisivao fajlove, klase i mapiranja koja u programu ne postoje. Sada odgovara kodu i dopunjen je rezultatima provere nad pravim DOS podacima.
- Dokumentovan mehanizam nadogradnje šeme baze (migracije + dopuna kolona pri pokretanju) i postupak objavljivanja nove verzije preko CI-ja.

## [2.23.1] - 2026-08-16

### 🐛 DOS uvoz zarada je konačno prohodan
- **Uvoz iz DOS/Clipper obračuna zarada nije radio nikada** — padao je odmah na početku sa greškom *„no such table: Radnici"*. Privremena baza kroz koju uvoz prolazi nije dobijala nijednu tabelu, a pojedinačne greške su usput gutane kao upozorenja, pa se problem video tek na kraju. Ispravljeno; uvoz sada prolazi ceo lanac.
- **Provereno nad pravim podacima** (25 godina DOS obračuna, `C:\PLATA\PLATA\KOR28`): uvezeno **6.873 radnika** i **4.984 obračuna** kroz **302 obračunska perioda (5/2001 – 6/2026)**, bez ijednog upozorenja ili greške. Zbir neto isplata 198,3 miliona RSD, zbir bruto zarada 204,2 miliona RSD.
- Bruto iznosi postoje od 2014. nadalje — stariji DOS periodi su vođeni samo kroz neto, i tako su i uvezeni (neto i sati su popunjeni u svim godinama).

## [2.23.0] - 2026-08-15

### 🏭 Proizvodnja — cena koštanja i rasknjižavanje do kraja
- **Satnica rada iz stvarnog obračuna zarada.** U radnom nalogu, tab *Faze izrade*, faza sada može da nosi **radnika**, a dugme **„👷 Satnice iz zarada"** popunjava satnicu punim troškom poslodavca po satu (bruto + doprinosi na teret poslodavca ÷ ukupno sati) iz poslednjeg obračuna tog radnika. Do sada se satnica unosila napamet, pa je cena koštanja bila tačna samo koliko i procena.
  - Sabira sve isplate u mesecu (akontacija, konačna, bonus), preskače stornirane obračune i po isplati uzima poslednju verziju.
  - Faze bez radnika ili bez obračuna zadržavaju ručno unetu satnicu — ništa se ne nuluje bez podatka.
  - Izmena se ne snima sama; potvrđuje se dugmetom *Sačuvaj*, kao i svaki drugi unos.
- **Rasknjižavanje vraća nabavnu cenu artikla.** Završetak naloga upisuje cenu koštanja u nabavnu cenu gotovog proizvoda; rasknjižavanje je sada vraća na zatečenu vrednost. Ako je cenu u međuvremenu promenio neko drugi (nivelacija, nova kalkulacija), ta vrednost se **ne gazi**.
- **Delimično rasknjižavanje — samo nalog Glavne knjige.** Dugme *Rasknjiži* sada nudi izbor: potpuno poništavanje (kao do sada) ili brisanje samo nacrta naloga Glavne knjige, bez diranja zaliha i statusa *Završen* — za slučaj kad je knjiženje otišlo na pogrešna konta, pa treba samo ponoviti knjiženje.

## [2.22.1] - 2026-08-15

### 🗄️ Zatvoren migracioni drift — WebShop kolone konačno u sistemu migracija
- **60 kolona i tabela `WebNapusteneKorpe` unete u migracije.** Kurirske službe (v2.19.2), kartično plaćanje i loyalty program (v2.19.4), SMS/Viber notifikacije, live chat, marketing i višejezičnost (v2.22.0) dodavani su do sada isključivo „u hodu", pri pokretanju programa. Šema je time bila ispravna, ali je sistem migracija za te kolone znao da nešto nedostaje — pa je svaka naredna izmena baze kretala od pogrešnog stanja.
- **Zatečene baze se ne diraju.** Bazama koje te kolone već imaju migracija se samo evidentira kao primenjena, bez ijedne izmene podataka. Provereno na svih 7 stvarnih baza: 37 migracija, ništa neprimenjeno, podaci netaknuti.
- **Nadogradnja sa preskočene verzije više ne može da pukne.** Baza korisnika koji je preskočio neko od izdanja imala je samo deo kolona; takva baza sada dobija tačno ono što joj nedostaje, umesto da program prijavi grešku „kolona već postoji" pri prvom pokretanju.
- **Regresiona zaštita**: nov test koji puca čim se u model doda polje bez migracije — drift se dvaput skupljao mesecima upravo zato što se ništa nije bunilo.

## [2.22.0] - 2026-08-15

### 🌐 Višejezičnost i Viševalutnost (Multilingual & Multi-Currency)
- **Preklopnik jezika (🇷🇸 SR / 🇬🇧 EN / 🇩🇪 DE)**: Gornja promotivna traka i navigacija sadrže selektor jezika sa perzistencijom u `localStorage`.
- **Kompletna lokalizacija interfejsa**: `LanguageCurrencyContext` sa prevodima za navigaciju, katalog, filtere, korpu, checkout, pretragu, recenzije i chat podršku.
- **Višejezični nazivi i opisi u bazi i API-ju**: Polja `NazivEn`, `WebOpisEn`, `NazivDe`, `WebOpisDe` na `Artikal` i `NazivEn`, `NazivDe` na `WebKategorija` uz DTO prenos i pametni fallback.
- **Preklopnik valuta (RSD / EUR / USD / BAM)**: Live NBS kursna lista preko `KursnaListaService` (`GET /api/katalog/kursevi`) sa automatskim preračunom i formatiranjem svih cena u katalogu i korpi (`formatCena`).

### 🔍 Pametni Quick-Search Modal (Ctrl+K) & Barcode Skener Kamerom
- **Command Palette (`QuickSearchModal.tsx`)**: Globalne prečice `Ctrl+K`, `Cmd+K`, `/` sa live rezultatima, stanjem zaliha u magacinu, tastaturnom navigacijom i istorijom pretraga.
- **Web Barcode & QR Skener (`BarcodeScannerModal.tsx`)**: HTML5 kamera skener za EAN-13, Code-128 i QR kodove sa laserskim nišanom, svetlom (Torch), audio zvučnim signalom i direktnim otvaranjem `ProductModal`-a (`GET /api/katalog/barkod/{kod}`).

### 💬 Live Chat Podrška & WhatsApp / Viber Widget
- **Višekanalni lebdeći vidžet (`LiveChatWidget.tsx`)**: WhatsApp, Viber, direktan poziv i email podrška sa online statusom i radnim vremenom.
- **Direktan upit o artiklu**: 1-klik prenos artikla sa `ProductModal` u formu za upit i automatsko slanje profesionalnog HTML email obaveštenja prodajnom timu (`POST /api/katalog/upit`).

## [2.21.0] - 2026-08-15

### ↩️ Rasknjižavanje i storniranje završenog radnog naloga
- Novo dugme **„↩️ Rasknjiži“** u *Radnim nalozima*: poništava završetak naloga u celini — briše nacrt naloga glavne knjige, vraća zaduženje gotovog proizvoda i razduženje sirovina sa materijalnih kartica i briše automatski nastale dokumente, pa se nalog vraća u status *U radu* i može se ispraviti i ponovo završiti.
- **Završen nalog se konačno može stornirati.** Ranije je storniranje takvog naloga bilo odbijeno („bez poništavanja skladišnih kretanja“); sada se nalog prvo rasknjiži pa označi kao storniran, uz jasno upozorenje šta se poništava.
- Rasknjižavanje se **odbija ako je nalog glavne knjige u međuvremenu proknjižen** (prvo ga treba rasknjižiti u *Nalozima*) ili ako je posle njega bilo kasnijih knjiženja nad istom karticom — u oba slučaja ništa se ne dira.

### 💰 Utrošak materijala se knjiži po stvarnoj, a ne po planskoj vrednosti
- Sirovina sa zaliha izlazi po **ponderisanoj prosečnoj ceni**, a nalog glavne knjige je do sada knjižio plansku nabavnu cenu sa radnog naloga — kod odstupanja cena magacin i knjigovodstvo su govorili dva različita iznosa. Sada se u glavnu knjigu upisuje vrednost koju je magacin **stvarno otpisao**.
- Po istoj vrednosti se preračunava i **cena koštanja gotovog proizvoda**, pa gotov proizvod ulazi na zalihe po ceni koja odgovara utrošenom materijalu.
- Planska vrednost ostaje zapisana na radnom nalogu radi poređenja i koristi se kao rezerva za naloge završene bez skladišnih kretanja.

### 🖱️ Sitnije
- Dugme **„✅ Završi & Zaduži“** više nije puki duplikat dugmeta *Otvori nalog* — otvara nalog sa kursorom na polju proizvedene količine.

## [2.20.0] - 2026-08-15

### 🏭 Završetak radnog naloga konačno stvarno menja zalihe i knjiži se u glavnu knjigu
- **Gotov proizvod sada zaista ulazi na stanje.** Zaduženje magacina gotovih proizvoda se do sada obeležavalo kao proknjiženo, ali **nije upisivalo nijedan red materijalne kartice** — proizvedena roba nije postojala na zalihama, a dokument se više nije mogao ni naknadno proknjižiti jer je već bio označen kao knjižen.
- **Razduženje sirovina više ne promašuje karticu.** Utrošak se upisivao pod *nazivom* komponente umesto pod *šifrom* materijala, pa red nije pripadao kartici tog materijala i zaliha se realno nije smanjivala. Oba koraka sada idu kroz iste servise koje koriste Trebovanja i Robna kretanja, uključujući i **zabranu odlaska zalihe u minus**.
- **Radni nalog se označava kao završen tek kada knjiženje uspe** — ako nema dovoljno sirovine, nalog ostaje u radu umesto da postane „završen“ bez ijednog skladišnog traga. Nad SQLite bazom ceo zahvat ide u jednu transakciju.
- **Komponente bez veze ka šifarniku se više ne preskaču ćutke** — završetak naloga jasno kaže koje komponente treba dopuniti u sastavnici.

### 📒 Automatsko knjiženje proizvodnje (Faza 6)
- Završetak radnog naloga pravi **nalog glavne knjige u statusu Nacrt**: utrošak materijala (duguje 5110 / potražuje 1010) i prijem gotovog proizvoda po ceni koštanja (duguje 1200 / potražuje 6300). Knjigovođa ga pregleda i proknjiži u *Nalozima*, isto kao naloge iz Zarada i Osnovnih sredstava.
- Nalog se **ne pravi dva puta** za isti radni nalog, a broj naloga se upisuje na sam radni nalog.
- Ako knjiženje u glavnu knjigu ne uspe (npr. konto ne postoji u kontnom planu), **radni nalog i skladišna kretanja ostaju** uz izričito upozorenje — dokumenti se ne gube.
- Trošak rada i amortizacije se ovde ne knjiži jer u glavnu knjigu već ulazi kroz obračun zarada i amortizaciju sredstava.

### 📱 Automatske SMS & Viber Notifikacije Kupcima
- **Integracija sa Provajderima**: Podrška za Infobip (SMS / Viber Business API), SMS Gateway RS (MTS, Yettel, A1) i BulkSMS.
- **Normalizacija Telefona**: Automatsko formatiranje brojeva u međunarodni E.164 standard (`+381...`).
- **Tri Automatska Scenarija**:
  - Slanje kurirskog linka za praćenje paketa čim pošiljka krene.
  - SMS čestitka sa iznosom popusta za dodeljene Loyalty nagradne bodove.
  - SMS potvrda prijema porudžbine.
- **Sandbox Simulator & CMS Kontrole**: Prekidač za bezbedno testiranje bez trošenja kredita i interaktivna forma za probno slanje u `/admin` CMS podešavanjima.

### 🛒 Marketing Automatizacija (Cross-Sell, Količinski Popusti & Napuštene Korpe)
- **✨ "Često se kupuje zajedno" (Cross-Sell Bundle)**: Prikaz kompatibilnih artikala na `ProductModal` uz automatski zbir i 1-klik dodavanje celog kompleta u korpu, sa inteligentnim fallback-om na istu kategoriju.
- **🏷️ Količinski Popusti (Volume Discounts)**: Pragovi minimalnih količina sa procentualnim popustima, dinamičko isticanje aktivnog nivoa na proizvodu i u korpi, i transparentan prikaz uštede.
- **🛒 Oporavak Napuštenih Korpi (Abandoned Cart Recovery)**:
  - Automatsko debounced praćenje korpi posetilaca (`WebNapusteneKorpe`).
  - Automatski oporavak čim kupac realizuje bilo koju narudžbinu.
  - Backoffice administracija (`/admin` → *🛒 Napuštene Korpe*) sa 4 KPI metrike i modalom za slanje email/SMS podsetnika sa poklon promo kuponom (npr. `VRATISE5`).
  - Responsivan HTML email predložak sa 1-klik linkom za nastavak kupovine.

### 💬 Live Chat Podrška & WhatsApp / Viber Widget
- **Lebdeći Višekanalni Vidžet (`LiveChatWidget.tsx`)**: Donji desni ugao sa pulsirajućim online indikatorom, brzim kanalima (WhatsApp chat, Viber razgovor, direktan poziv, email) i radnim vremenom podrške.
- **Interaktivni Upit o Artiklu**: Direktan prenos slike, naziva i šifre artikla iz `ProductModal` u formu za upit, sa slanjem formatiranog HTML email obaveštenja službi prodaje (`POST /api/katalog/upit`).
- **CMS Podešavanja**: Konfiguracija brojeva telefona, email adrese, radnog vremena i poruke dobrodošlice u `/admin` panelu.

### 🔍 Pametni Quick-Search Modal (Ctrl+K) & Barcode Skener Kamerom
- **Globalni Command Palette (`QuickSearchModal.tsx`)**: Instant pretraga artikala, kategorija i prečica (`Ctrl+K` / `Cmd+K` / `/`), sa sličicama, cenama, stanjem na zalihama, brzim čip filterima i `localStorage` istorijom pretraga.
- **Web Barcode & QR Skener (`BarcodeScannerModal.tsx`)**: Kamera skener (EAN-13, Code-128, QR Code) sa laserskim nišanom, zvučnim 880Hz signalom, haptičkom vibracijom, blicem i automatskim otvaranjem pronađenog artikla (`GET /api/katalog/barkod/{kod}`).

### ⚙️ Podešavanja → Proizvodnja (novo)
- Nov tab sa **kontima za knjiženje proizvodnje** (prazno polje = podrazumevani konto) i prekidačem za automatsko pravljenje naloga glavne knjige pri završetku radnog naloga.

## [2.19.4] - 2026-08-15

### 💳 Online Kartično Plaćanje (Payment Gateway & 3D Secure 2.0)
- **Kompletna E-Commerce Payment Gateway Integracija**:
  - Podrška za domaće i inostrane platne procesore: **AllSecure**, **CorvusPay**, **Payten / ChipCard (Asseco)**, **NestPay (Banca Intesa, OTP Banka)** i **Stripe**.
  - Dinamičko potpisivanje zahteva i verifikacija digitalnog potpisa (**SHA512** i **HMAC-SHA256**) sa tajnim ključem trgovca.
- **🔐 3D Secure 2.0 Autentifikacija**:
  - Puna podrška za *Mastercard Identity Check*, *Visa Secure* i *DinaCard 3D Secure*.
  - Drastično smanjenje odbijanja/otkazivanja pošiljki i instant naplata za fizička lica i maloprodajne kupce.
- **✨ Interaktivni 3D Kartični Modal (`PaymentGatewayModal.tsx`)**:
  - Dinamički 3D prikaz platne kartice sa vizuelnom rotacijom pri unosu CVV koda i automatskim prepoznavanjem kartičnog brenda (Visa, Mastercard, DinaCard, Maestro).
  - Prečice za brzi unos testnih kartica i simulacija SMS OTP verifikacionog koraka.
- **🔄 Webhook Endpoint za Automatsku Obradu Transakcija**:
  - `POST /api/porudzbine/kartica-webhook`: `[AllowAnonymous]` webhook za prijem asinhronih server-to-server notifikacija sa platnih procesora.
  - Automatsko prebacivanje statusa porudžbine u `WebPorudzbinaStatus.PlacenaKarticom = 6`.
- **🛠️ CMS Podešavanja & Istorija Transakcija**:
  - U `/admin` CMS tabu omogućen izbor procesora, Merchant ID, Terminal ID, API Key, Secret HMAC ključ i preklopnik Sandbox / Live mod.
  - Prikaz autorizacionog koda, ID transakcije i maskiranog broja kartice u detaljima porudžbine i korisničkom panelu *"Moje Porudžbine"*.

## [2.19.3] - 2026-08-15

### 🎁 B2C Korisnički Nalog & Loyalty Program (Program Lojalnosti)
- **👤 Korisnički Profili za Fizička Lica**:
  - **Google Prijava**: Brza prijava u 1 klik preko Google naloga (`POST /api/auth/google-login`).
  - **Email / Lozinka Registracija**: Standardna registracija kupaca sa automatskom dodelom **50 Welcome Bonus Poena**.
  - **Profil & Loyalty Centar Modal**: Prikaz statusa lojalnosti (*Bronzani Kupac 🥉*, *Zlatni Kupac 🥇*, *Platinasti VIP 💎*), stanja raspoloživih bodova, novčane vrednosti u RSD i ličnih podataka.
- **📍 Sačuvane Adrese & Brzi 1-Klik Checkout**:
  - Kupac čuva primarnu adresu za dostavu, grad, poštanski broj, telefon i napomenu za kurira.
  - Sva polja primaoca na Checkout kasi se automatski popunjavaju u 1 klik.
- **🎁 Loyalty Program & Popusti na Kasi**:
  - **Sakupljanje bodova**: Svaka uspešno realizovana kupovina automatski nagrađuje kupca (5% od plaćenog iznosa se vraća kao bodovi).
  - **Korišćenje bodova kao direktan popust**: 1 bod = 1 RSD popusta pri sledećoj kupovini uz prag aktivacije (min. 50 bodova).
  - **Interaktivni Checkout Widget**: Prekidač na kasi za instant primenu loyalty popusta uz istovremeni prikaz novih poena koji se osvajaju tom kupovinom.
- **📄 Istorija Porudžbina & Preuzimanje PDF Računa**:
  - Prikaz istorije sa detaljnim finansijskim izvodom utrošenih i osvojenih poena.
  - **Preuzimanje zvaničnog PDF računa/predračuna** sa integrisanim NBS IPS QR kodom za plaćanje mobilnim bankarstvom (`GET /api/porudzbine/{id}/predracun-pdf`).
- **🛠️ CRM & CMS Administracija**:
  - U `/admin` CRM tabeli kupaca prikazana je kolona *Loyalty Poeni* uz akciju **"+ Dodeli poene"** za ručnu dodelu bonus bodova.
  - U CMS podešavanjima omogućeno je prilagođavanje procenta nagrade (%), vrednosti boda u RSD i minimalnog praga.

## [2.19.2] - 2026-08-15

### 🚚 Kurirske Službe & API Praćenje Pošiljki (PostExpress, DExpress, Bex, Aks)
- **1-Klik Kreiranje Pošiljke**: U `/admin` panelu (i pregledu detalja porudžbine) ugrađen je poseban blok za izbor kurira (*PostExpress*, *DExpress*, *Bex*, *Aks*) i automatsko kreiranje tovarnog lista. Porudžbina automatski dobija status `Poslata` uz upis broja pošiljke.
- **Direktno Live Praćenje**: Servis automatski generiše tačne linkove za praćenje pošiljke na portalima kurirskih službi:
  - *PostExpress* (`https://www.posta.rs/lat/alati/pracenje-posiljke.aspx?broj=...`)
  - *DExpress* (`https://www.dexpress.rs/rs/pracenje-posiljaka/...`)
  - *Bex Express* (`https://bex.rs/pracenje-posiljke?broj=...`)
  - *Aks Express Kurir* (`https://www.aks.rs/pracenje-posiljke/?broj=...`)
- **Sandbox / Test mod**: Omogućeno bezbedno testiranje rada kurirskih službi sa automatskom kalkulacijom kontrolnih cifara i formatiranjem brojeva bez troškova i bez ugovora.
- **Email sa dugmetom za praćenje**: Transakcioni email o slanju pošiljke kupcu sada sadrži istaknuto dugme koje ga direktno vodi na sajt kurira.
- **Kupac Portal ("Moje Porudžbine")**: Prijavljeni kupci na svom profilu vide dugme *"Prati pošiljku uživo"* za svaku poslatu porudžbinu.
- **Dinamički Live API Proračun Cena Transporta**: Na Checkout-u se poštarina automatski kalkuliše u realnom vremenu na osnovu mase artikala u korpi, ugovorenih kurirskih tarifa i provizije na otkupninu za plaćanje pouzećem (1% min 60 RSD).
- **Izbor Kurira na Checkout-u**: Kupac može samostalno izabrati željenu kurirsku službu (*PostExpress*, *DExpress*, *Bex*, *Aks*) uz trenutan uvid u cenu dostave.
- **Štampa PDF Adresnica**: Direktno generisanje i štampa standardnih A6 adresnica sa barkodom i podacima o otkupnini za pouzeće.

## [2.19.1] - 2026-08-15

### 🗃️ Šema baze — WebShop i Proizvodnja konačno u sistemu migracija
- **17 tabela i 32 kolone** su do sada nastajale isključivo kroz interni SQL pri pokretanju, mimo sistema migracija — ceo WebShop, cela Proizvodnja, web polja artikala, korisnička prava, SMTP podaci firme i fiskalna polja računa. Sada su deo zvanične migracije, pa je šema baze konačno u potpunosti opisana na jednom mestu.
- **Postojeće baze se ne diraju.** Pošto sve navedeno već imaju, migracija se nad njima samo evidentira kao primenjena umesto da se izvršava — nema prepravljanja tabela, nema rizika po podatke. Provereno na kopijama svih postojećih baza: sve prolaze, podaci netaknuti.
- **Nove instalacije** dobijaju kompletnu šemu odmah, bez oslanjanja na naknadne dopune.

### 🛠️ Ispravke
- **Ugrađeni `admin` nalog na novoj instalaciji dobijao bi nalog bez prava administracije.** Podrazumevana prava nisu bila navedena uz sistemski nalog, pa bi na sveže instaliranom sistemu administrator ostao bez pristupa administraciji. Ispravljeno i pokriveno testom.

## [2.19.0] - 2026-08-15

### ⭐ Ocene i recenzije artikala
- **Kupci ocenjuju kupljene artikle** ocenom 1–5 uz opcioni komentar, direktno na stranici proizvoda. Ocenu može ostaviti **samo kupac koji je taj artikal stvarno naručio** (provera se radi na serveru nad njegovim porudžbinama, otkazane se ne računaju), pa sve recenzije nose oznaku **Verifikovana kupovina**. Isti kupac ne može oceniti isti artikal dva puta.
- **Moderacija pre objave**: nova recenzija se upisuje kao *na čekanju* i ne vidi se na sajtu dok je administrator ne odobri u novom tabu **`/admin` → Recenzije**. Neodobrene recenzije se ne broje ni u prosečnu ocenu ni u broj recenzija.
- **Brojač na Dashboard-u i u bočnom meniju** pokazuje koliko recenzija čeka odobrenje — bez njega bi, uz pre-moderaciju, ocene lako ostale zauvek neobjavljene.
- **Prikaz na izlogu**: zvezdice i prosečna ocena na kartici artikla i puna sekcija recenzija u prozoru proizvoda. Artikal bez ijedne odobrene recenzije ne prikazuje zvezdice uopšte.
- Time je zaokružena funkcija čiji je model (`WebRecenzija`) i tabela u bazi postojao od ranije, ali nije imao nijedan API endpoint niti ekran.

## [2.18.4] - 2026-08-15

### 🧾 Fakturisanje Web Porudžbine sada odmah i knjiži račun
- **1-klik `🧾 Kreiraj Račun u ERP-u`** (i u `/admin` Backoffice-u i u `ERPiApp → WebShop → Porudžbine`) sada, pored kreiranja računa-otpremnice, **odmah i knjiži** taj račun: razdužuje magacin na materijalnoj kartici i kreira nalog prodaje u glavnoj knjizi (kupac 204 / prihod 612 / obračunati PDV 470, uz nabavnu vrednost prodate robe 501 naspram konta robe). Do sada je knjiženje bilo zaseban ručni korak, pa je prodata roba znala da ostane na zalihama neograničeno dugo.
- **Ako knjiženje ne uspe, račun ostaje kreiran** uz jasno upozorenje da magacin nije razdužen — dokument se ne gubi, a rezervacija zalihe se ne otpušta, pa se roba ne može prodati dva puta. Administrator račun proknjiži ručno u *Računi-Otpremnice*.

### 📦 Podešavanje "Magacin za zalihe" konačno radi
- Podešavanje **`Magacin za zalihe`** (`WebShopPodesavanja`) se do sada čuvalo u ekranu podešavanja, ali ga **nijedan kod nije koristio**. Sada ga koriste sve tri putanje: prikaz zalihe na izlogu, provera raspoloživosti pri poručivanju i kreiranje/knjiženje računa-otpremnice.
- Time je uklonjena neusklađenost u kojoj je prodavnica prikazivala **zbir svih magacina**, a račun se pravio na **proizvoljnom prvom magacinu** iz šifarnika — što je moglo dovesti do neuspelog razduženja pri knjiženju.
- Ako magacin nije eksplicitno podešen, ponašanje ostaje kao ranije (zbir svih magacina + prvi magacin za račun). **Preporučuje se da se magacin eksplicitno podesi.**

## [2.18.3] - 2026-08-15

### 🛠️ Ispravke — Rezervacija Zaliha (sprečena prodaja iste robe više puta)
- **Roba iz primljenih porudžbina se sada rezerviše.** Do sada je WebShop gledao samo sirovo stanje na materijalnoj kartici, a roba se sa kartice skida tek pri *knjiženju* računa-otpremnice — što je zaseban, ručni korak. Između prijema porudžbine i knjiženja, ista roba je izgledala potpuno raspoloživa i mogla se prodati proizvoljan broj puta.
- **Novo `MaterijalnaKarticaService.GetRaspolozivoZaWebAsync()`**: raspoloživo za web = stanje na kartici **minus** količina rezervisana u već primljenim porudžbinama. Rezervacija se otpušta kada je porudžbina `Otkazana` ili kada je njen račun-otpremnica stvarno **proknjižen** (`IsKnjizen`) — status `Fakturisana` sam po sebi je ne otpušta, jer kreiranje računa ne razdužuje magacin.
- **Zatvorena trka pri istovremenim porudžbinama** (`WebShopPorudzbinaLockService`): provera zalihe i upis porudžbine sada se izvršavaju kao jedna nedeljiva sekcija, pa dva kupca koji poruče u istom trenutku ne mogu oba "kupiti" poslednji preostali komad.
- **Izlog prikazuje stvarno raspoloživo** (`KatalogController`) — artikal koji je već rezervisan drugom porudžbinom prelazi u *"Nema na stanju"* umesto da i dalje bude ponuđen.
- **Admin pregledi ostaju na fizičkom stanju**: Dashboard *Low-Stock* vidžet i admin lista artikala namerno prikazuju stvarnu zalihu u magacinu, a ne web-raspoloživost.

### 📝 Dokumentacija
- Ispravljena netačna tvrdnja u `docs/WEBSHOP.md` da 1-klik kreiranje Računa-Otpremnice radi "automatsko skladišno razduženje" — razduženje se dešava tek pri knjiženju računa u `RacuniOtpremniceView`. Dodat nov odeljak *§12 Rezervacija Zaliha* sa dijagramom toka.

## [2.18.2] - 2026-08-15

### 🛠️ WebShop Admin Stranica `/admin` (Multi-Firma Backoffice)
- **Posebna samostalna stranica (`/admin`)**: Umesto modalnog panela, administracija se sada prikazuje kao samostalna stranica (Standalone Page) preko celog ekrana, sa čistom navigacijom i tasterom `← Prodavnica`.
- **Namenski Admin Login ekran**: Automatska zaštita pristupa sa namenskim login formularom, proverom `IsAdmin` uloge i opcijom za brzu prijavu.
- **8 specijalizovanih tabova za operativno vođenje web prodaje**:
  - **Dashboard**: Finansijska analitika, današnji i mesečni promet, broj porudžbina, B2B zahtevi na čekanju, top prodavani artikli i **⚠️ Artikli na izmaku zaliha (Low-Stock Alert)** sa zalihom ≤ 5 komada.
  - **Porudžbine**: Pregled i pretraga pristiglih web porudžbina, modal sa specifikacijom stavki i kupca, promena statusa u 1 klik (`Nova` ➔ `Prihvaćena` ➔ `U pripremi` ➔ `Poslata` ➔ `Fakturisana` ➔ `Otkazana`), štampa kurirskih adresnica i eksport u CSV.
  - **Artikli na webu**: Prekidač *"Objavi na webu"*, uređivanje marketinškog web naziva, opisa, akcijskih cena i bedževa *Novo* i *Top preporuka*, uz eksport kataloga u CSV.
  - **Stablo kategorija**: Upravljanje hijerarhijom kategorija, ikonicama, redosledom i oznakom *Istaknuta na početnoj*.
  - **B2B Zahtevi**: Pregled registracija pravnih lica i odobravanje veleprodajnog pristupa u 1 klik.
  - **Kupci & CRM**: Centralni pregled kupaca i B2B partnera, istorija porudžbina, ukupan promet po kupcu (LTV), pretraga, odobravanje B2B statusa i eksport u CSV.
  - **Kuponi & Promocije**: Kreiranje promotivnih kodova sa procentualnim ili fiksnim popustom, minimalnim iznosom korpe i datumskim rokom važenja.
  - **CMS & Brending**: Izmena naziva šopa, slogana, tema, primarne i sekundarne boje, hero tekstova i pragova za besplatnu dostavu.
- **1-Klik Kreiranje Fakture u ERP-u (`RacunOtpremnica`)**:
  - U modalu detalja porudžbine taster **`🧾 Kreiraj Račun u ERP-u`** poziva `POST /api/admin/porudzbine/{id}/kreiraj-fakturu`, generiše standardni izlazni račun-otpremnicu u ERP bazi sa svim stavkama, obračunatim PDV-om i vezuje status porudžbine na `Fakturisana`.
- **Generisanje i Štampa Kurirskih Adresnica (PDF)**:
  - U modalu detalja porudžbine i tabeli porudžbina u Backoffice-u ugrađeno je dugme **`🖨️ Štampaj Adresnicu (PDF)`**.
  - Generisanje standardne A6 nalepnice za pakete (`WebPorudzbinaAdresnicaDocument`) sa podacima o pošiljaocu, kupcu/primaocu, otkupnini, kuriru, broju za praćenje i QR kodom za brzi sken.
- **Eksport u CSV / Excel**:
  - Ugrađen jednoklikovni eksport formatiranih CSV tabela sa UTF-8 BOM podrškom za tabove *Porudžbine*, *Artikli* i *Kupci*.
- **Konfiguracija Backoffice naloga u ERPiApp-u (`WebShopPodesavanjaView`)**:
  - U desktop aplikaciji (Tab 1: *Osnovno & Plaćanja*) dodata je kartica **🛠️ WebShop Backoffice Administratorski Pristup**.
  - Mogućnost unosa prilagođenog Admin Email-a i postavljanja lozinke za pristup Backoffice-u po firmi (podrazumevano: `admin@erpi.rs` / `admin123`).
  - Dodato dugme `🛠️ /admin` u toolbar-u za brzo otvaranje Backoffice stranice u browseru.
- **REST API Backend (`AdminController.cs` & `AuthController.cs`)**: Zaštićene administratorske rute (`[Authorize(Roles = "Admin")]`) sa automatskom autentifikacijom preko `WebShopPodesavanja` i `WebKorisnici` tabela tekuće firme.

### 🎟️ Sistem Promo Kodova & Kupona
- **Model `WebKupon`**: Podrška za kupone u bazi podataka sa evidencijom iskorišćenja.
- **Validacija na Checkout-u**: Endpoint `POST /api/porudzbine/proveri-kupon` proverava važenje, datum i minimalni iznos i u realnom vremenu primenjuje popust na Checkout modalu.

### 🛠️ Ispravke
- **Pouzdano zaustavljanje ERPiApi servisa (`AppTrayService.ZaustaviApiServis`)**: Servis se sada zaustavlja preko SCM-a kad god postoji instaliran Windows servis (a ne samo kad postoji bundled exe), i uvek se dodatno ubijaju eventualni zaostali `ERPiApi` procesi — sprečava situacije gde je API ostajao aktivan posle "Zaustavi" akcije.

## [2.18.1] - 2026-08-15

### 📈 SEO, Marketing & Analitika
- **Dinamički `sitemap.xml` i `robots.txt`**: `SitemapController` automatski generiše XML sitemap iz baze sa svim kategorijama i odobrenim artiklima za trenutno indeksiranje na pretraživačima (Google, Bing).
- **Google Analytics 4 & Meta Pixel integracija**: Konfigurabilan unos GA4 Measurement ID i Meta Pixel ID direktno iz ERP-a (`WebShopPodesavanjaView`) bez menjanja koda. `AnalyticsContext` na frontendu prati preglede stranica, artikala (`view_item`), dodavanje u korpu (`add_to_cart`) i konverzije (`purchase`).
- **Lista želja (Wishlist)**: Čuvanje omiljenih artikala u `LocalStorage`-u za posetioce i sinhronizacija sa bazom (`WebZelje` tabela) za registrovane kupce, uz brz prenos u korpu.
- **Upoređivanje artikala (Compare)**: Interaktivna matrica za upoređivanje do 4 artikla istovremeno sa tehničkim specifikacijama, cenama i stanjem na zalihama.

### 🚚 Integracija sa Kurirskim Službama (DExpress, Bex, Post Express, AKS)
- **Generisanje adresnica**: Dugme `📦 Generiši adresnicu` u `WebPorudzbineView` otvara dijalog sa automatskim proračunom otkupnine (za pouzeće), brojem paketa i masom.
- **Štampa PDF nalepnica sa bar-kodom**: Standardizovane A6/termo nalepnice (100x150 mm) sa čitljivim `CODE_128` bar-kodom broja pošiljke, podacima pošiljaoca, primaoca, otkupninom i napomenom.
- **Podrška za vodeće kurirske službe**: DExpress, Bex Express, Post Express (Pošta Srbije) i AKS Express Kurir sa formatiranim tracking kodovima.

### 📧 Automatski Transakcioni Email Servis & SystemTray Notifikacije
- **Email potvrda kupcu sa PDF predračunom**: Čim kupac napravi porudžbinu, automatski mu se šalje HTML email sa priloženim PDF predračunom (`WebPorudzbinaPredracunDocument`) i **NBS IPS QR kodom** za instant plaćanje.
- **Email obaveštenje o slanju paketa**: Kupac dobija email čim se paket preda kuriru, sa nazivom kurirske službe i kodom za praćenje pošiljke.
- **Notifikacije administratoru**: Email obaveštenje za svaku novu porudžbinu i **Windows SystemTray Toast Popup sa zvukom** (`SystemSounds.Asterisk`) čim narudžbina stigne.

### 🏢 B2B Veleprodajni Portal — Jačanje Veleprodaje
- **Zahtev za B2B nalog (Registracija pravnih lica)**: Online forma na webshop-u sa unosom PIB-a, matičnog broja, naziva firme i kontakata. Kreira nalog na čekanju i šalje notifikaciju administratoru.
- **Administracija i verifikacija naloga (`WebKorisniciView`)**: Novi ekran u ERPiApp-u za jednoklikovno odobravanje B2B pristupa, automatsko prepoznavanje ili kreiranje novog Partnera u šifarniku sa dodelom šifre (`P1001`...).
- **Preuzimanje PDF e-Faktura & IOS-a**: B2B partneri direktno sa portala preuzimaju originalne PDF račune-otpremnice i zvanične IOS obrasce (Izvod otvorenih stavki) generisane preko `B2bPdfService` (QuestPDF).
- **Ponavljanje prethodne porudžbine (Re-order)**: Dugme `🔄 Naruči ponovo` na istoriji porudžbina ubacuje sve stavke prethodne porudžbine u korpu jednim klikom.

## [2.18.0] - 2026-08-15

### 🛠️ ERPiApi kao pravi Windows Service (nezavisan od ERPiApp-a)
- Do sada je WebShop backend bio dete-proces `ERPiApp`-a — vezan za životni vek WPF prozora (gasio se
  pri promeni firme, autostart je zavisio od `HKCU\...\Run` koji se izvršava samo pri prijavi
  konkretnog Windows korisnika). Sada je `ERPiApi` pravi Windows Service, registrovan po firmi
  (`ERPiApi_{šifra firme}`) — nastavlja da radi i kad je `ERPiApp` potpuno zatvorena, i može da se
  pokreće pri samom podizanju mašine, bez potrebe da se iko uloguje.
- `ERPiApi`: dodata SCM integracija (`Microsoft.Extensions.Hosting.WindowsServices`,
  `UseWindowsService()`) — bez ovoga `sc start` javlja grešku 1053. JWT secret premešten sa
  `%LocalAppData%` na `%ProgramData%\ERPiApi\jwt.secret` (servis podrazumevano radi kao `LocalSystem`,
  čiji je `%LocalAppData%` potpuno drugi, mašinski `systemprofile` folder).
- Nov `WindowsServiceHelper` (ERPiApp) — `sc create/config/start/stop` preko iste `runas` elevacije
  koju već koristi Firewall dugme; status servisa preko `ServiceController` bez elevacije.
- `AppTrayService`: "Pokreni"/"Zaustavi" u produkciji sad idu kroz servis umesto Process handle-a;
  "🚀 Pokreni sa Windows-om" checkbox sad menja pravi start-type servisa (auto/demand) umesto starog
  registry upisa.
- Dev okruženje (pokretanje iz izvornog stabla) nije dirano — i dalje običan dete-proces (`dotnet run`)
  radi brzog F5 ciklusa, bez registrovanja servisa pri svakom lokalnom restartu.

## [2.17.2] - 2026-08-15

### 🐛 Ispravke nakon testiranja v2.17.1 (instalirana verzija)
- **Windows autostart nije radio**: `WindowsStartupHelper` je i dalje tražio `ERPiApi.exe` direktno
  pored `ERPiApp.exe`, dok je v2.17.1 uvela pakovanje u podfolder `ERPiApi\` — putanja se nikad nije
  poklopila, pa se registry Run ključ nikad nije upisao (ništa se nije pokretalo pri boot-u).
- **Tray ikonica je nestajala pri promeni firme**: `AppTrayService.Dispose()` (poziva se pri zatvaranju
  `MainWindow`, pa i pri "Promeni firmu") nije resetovao interni `_isInitialized` flag — sledeći
  `Inicijalizuj(...)` poziv za novu firmu je bio tihi no-op, tray se nije ponovo pravio niti se servis
  pokušavao pokrenuti za novu firmu/bazu/port.
- **`ERPiApi` nije uvek pronalazio `wwwroot`**: content root se oslanjao na `Directory.GetCurrentDirectory()`
  (podrazumevano u ASP.NET Core), a Windows "Run" startup stavke imaju nepredvidiv working directory —
  sad je eksplicitno postavljen na `AppContext.BaseDirectory` (folder samog exe/dll-a), pouzdano bez
  obzira ko/odakle proces pokrene.
- **Podešavanja WebShop-a i dalje su nudila zaseban "WebShop Port"** (npr. 5177) koji u produkciji niko
  ne osluškuje (prodavnicu servira isti proces kao API, na istom portu) — dugme "Otvori WebShop" je
  otvaralo mrtav port (`ERR_CONNECTION_REFUSED`). WebShop Port/URL polja sada u produkciji automatski
  prate API port i zaključana su za unos.

## [2.17.1] - 2026-08-15

### 🐛 Ispravka: WebShop servis se nije pokretao na instaliranoj verziji
- **Koren problema**: ni `publish.ps1` ni CI (`.github/workflows/release.yml`) nikad nisu pakovali
  `ERPiApi` (backend) niti build-ovali/pakovali `ERPiWebShop` (React prodavnicu) u instalacioni
  paket — instalirana verzija je sadržala samo `ERPiApp.exe`. Firewall pravilo se kreiralo (nezavisna
  logika), ali sâm servis nije imao šta da pokrene (`ERPiApi.dll` fizički nije postojao na disku), bez
  ijedne vidljive poruke korisniku.
- **`publish.ps1` / `release.yml`**: sada dodatno publishuju samostalan `ERPiApi.exe` (self-contained,
  win-x64/win-x86) i build-uju `ERPiWebShop` (`npm run build`) čiji se `dist/` kopira u
  `ERPiApi/wwwroot` — oba se pakuju uz `ERPiApp` u isti Velopack paket.
- **`ERPiApi` (`Program.cs`)**: servira statički build prodavnice sa istog Kestrel porta kao API
  (`UseStaticFiles` + SPA fallback na `index.html`) — instalirana verzija više ne zahteva Node.js/npm
  na korisnikovoj mašini niti poseban frontend port. Swagger UI premešten sa root URL-a na `/swagger`
  da ne bi kolidirao sa prodavnicom na `/`.
- **`AppTrayService`**: prepoznaje i pokreće upakovani `ERPiApi.exe` (sa ispravno postavljenim
  `WorkingDirectory`, što ranije nije bio slučaj ni u dev fallback grani); greške pri pokretanju
  (nedostaje exe, proces se odmah ugasio, port zauzet) sada prikazuje korisniku balon-obaveštenjem
  umesto da ih samo tiho ispisuje u Debug output.
- **`WebShopPodesavanjaView`**: dugme "Pokreni WebShop Servis" više ne duplira (pokvarenu) logiku
  pokretanja — deleguje na istu, ispravljenu `AppTrayService` implementaciju.

## [2.16.0] - 2026-08-14

### 🌐 Hibridni B2C & B2B WebShop Modul & e-Commerce Ekosistem
- **`ERPiData/Models/WebShop/`**:
  - `WebKategorija`: Hijerarhijsko stablo kategorija sa neograničenom dubinom, slug-ovima i SEO opisima.
  - `Atribut` & `ArtikalAtributVrednost`: Dinamički tehnički atributi za fasetirano filtriranje.
  - `WebPorudzbina` & `WebPorudzbinaStavka`: Celokupan životni ciklus narudžbina (statusi, načini plaćanja, adrese, kalkulacija PDV-a i troškova dostave).
  - `WebKorisnik`: Nalozi za B2C i B2B kupce sa vezom ka `Partner` entitetu.
  - `WebShopPodesavanja`: Konfiguracija prodavnice po preduzeću (izbor magacina za lager, cene dostave, teme i boje).
  - Proširen model `Artikal` poljima za web galerije slika (`SlikeJson`), PDF tehničke listove (`DokumentiPdfJson`), akcije i istaknute artikle.
- **WPF Desktop Admin Modul (`ERPiApp/Views/WebShop/`)**:
  - `WebPorudzbineView`: Inbox prispelih narudžbina sa brzim generisanjem **Računa-Otpremnice** i automatskim skladišnim razduženjem u 1 klik.
  - `WebShopKatalogView` & `WebArtikalEditWindow`: Pregled i uređivanje artikala za objavu na webu.
  - **Bogati HTML Editor & Live Web Preview**: Ugrađena format traka (Bold, Italic, Podvučeno, H2, H3, Lista, Tabela specifikacija, Info boks) sa dvo-panelnim WebBrowser prikazom u realnom vremenu.
  - `WebKategorijeView`: Vizuelni `TreeView` editor hijerarhije kategorija.
  - `WebShopPodesavanjaView`: Podešavanje brendinga, gotovih tema, boja i parametara dostave.
  - Rešeni XAML stilovi (`ModernTextBoxStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`) i stabilizovana inicijalizacija sa null-guard proverama.
- **ASP.NET Core REST API Servis (`ERPiApi`)**:
  - .NET 8 Web API sa Swagger/OpenAPI dokumentacijom i JWT Bearer autentifikacijom.
  - Kontroleri za katalog, stablo kategorija, autentifikaciju, checkout i B2B ugovorene cene.
  - `NbsIpsQrService`: Generisanje zvaničnog **NBS IPS QR koda** Narodne banke Srbije za instant plaćanje telefonom (m-banking).
- **React Frontend WebShop (`ERPiWebShop`)**:
  - React 18 + Vite + TypeScript sa prilagođenim ultra-modernim UI dizajnom i Dark/Light modom.
  - B2C online prodavnica sa Mega Menijem, fasetiranim filterima, brzom korpom i praćenjem praga za besplatnu dostavu.
  - B2B portal sa veleprodajnim cenama bez PDV-a, uvidom u neizmirene račune i tabelarnim **Quick Order** unosom.
  - **Live Theme Customizer**: Prebacivanje između 4 gotove teme (*Modern Retail*, *Industrial B2B*, *Minimal Luxury*, *Fresh & Green*) u realnom vremenu.
  - Poboljšana navigacija sa preciznim `hoveredKatId` stanjem za otvaranje podkategorija.
- **IDE Run & Debug**: Integrisane `tasks.json` i `launch.json` konfiguracije za pokretanje celog full-stack sistema u 1 klik (`F5`).
- **xUnit testovi**: Dodata 3 nova testa za stablo kategorija, atribute i porudžbine (ukupno **222/222 testova** prolazi, 100%).

## [2.15.0] - 2026-08-14

### 🗄️ Multi-DBMS Podrška (SQLite, PostgreSQL, Microsoft SQL Server 2022)
- **Podrška za Microsoft SQL Server**: Puna podrška za SQL Server 2022 (Express, Developer, Standard, Enterprise i LocalDB) uz rešene kaskadne veze i transakcioni `SET IDENTITY_INSERT` mehanizam.
- **`SqlServerInstallerService`**: Automatska detekcija aktivnih instanci na računaru (`MSSQLSERVER`, `SQLEXPRESS`, `LocalDB`), 1-klik preuzimanje i pokretanje instalacije SQL Express-a i automatsko otvaranje Windows Firewall portova (TCP 1433, UDP 1434).
- **Pametna detekcija parametara**: Dijalozi za novu firmu i migraciju automatski prepoznaju dostupne instance i uspešno testiraju konekciju ka serveru pre fizičkog kreiranja šeme.

### 🔐 Globalna Enterprise Autentifikacija i Dozvole (RBAC)
- **`GlobalLoginWindow`**: Početni prozor za prijavu na nivou sistema pre izbora firme.
- **`MasterAuthService`**: Bezbedno PBKDF2 SHA-256 hesiranje lozinki sa solju (`master_users.json`).
- **`KorisniciDozvoleWindow`**: Administracija globalnih operatera i selektivno dodeljivanje prava pristupa pojedinačnim firmama.
- **Single Sign-On (SSO)**: Automatski prelazak u izabranu firmu bez potrebe za ponovnim unosom lozinke.

### 🌐 Mrežni Klijentski Režim (LAN Radna Stanica)
- **`NetworkClientSetupWindow`**: Čarobnjak za izbor uloge računara — Samostalni / Glavni server ili Mrežna radna stanica u kancelariji/magacinu.
- **Jednokratno povezivanje**: Unos IP adrese servera, test veze u realnom vremenu i automatsko usmeravanje na centralnu bazu.

### 🏭 Modul Proizvodnja (Sastavnice, Radni Nalozi, Cena Koštanja)
- **`Sastavnice` & `SastavniceView`**: Normativi materijala sa jediničnim količinama i tehnološke faze/operacije sa normiranim vremenima i mašinama.
- **`RadniNalozi` & `RadniNaloziView`**: Praćenje proizvodnih naloga (Planiran, U radu, Završen, Storniran), automatsko razduženje sirovina i automatsko zaduženje gotovih proizvoda na zalihe.
- **`KalkulacijaCeneKostanjaService`**: Izračunavanje direktnog materijala, direktnog rada, varijabilne i fiksne režije po jedinici proizvoda sa procentualnim učešćem.
- **`ProizvodnjaDashboardView`**: Vizuelna radna tabla proizvodnje sa KPI karticama, grafikonima statusa i brzim prečicama.

### 📋 Dupliranje i Izmena Podataka Firme
- **`KopirajFirmuWindow`**: 1-klik kreiranje nezavisne kopije baze podataka (šifarnici, nalozi, zalihe, plate, osnovna sredstva) za potrebe arhive, testiranja ili nove poslovne godine.
- **`IzmeniFirmuWindow`**: Izmena naziva, šifre, PIB-a, matičnog broja i adrese sa sinhronizacijom u registru i bazi.

### 🚀 Velopack Auto-Update Unapređenje
- **Asinhrona provera pri startovanju**: Provera nove verzije na GitHub Releases odmah pri pokretanju u `GlobalLoginWindow` pre prijave korisnika, čime se izbegava dupli restart tokom rada.
- **xUnit testovi**: Svih **219/219 testova** uspešno prolazi.

## [2.14.0] - 2026-08-13

### 📈 Kontroling i Cash-Flow Projekcije Likvidnosti
- **`CashFlowForecastService` (`ERPiData/Services/CashFlowForecastService.cs`)**: Automatska analiza trenutnog novca na računima (klasa 24), potraživanja od kupaca (konta 204x/120x) i obaveza prema dobavljačima (konta 435x/220x) po koficama dospeća (već dospelo, 0-30 dana, 31-60 dana, 61-90 dana, >90 dana).
- **Procena zarada i kumulativni Cash-Flow**: Uključivanje mesečnih izdataka za plate i projekcija kumulativnog salda sa detekcijom rizika likvidnosti.
- **`CashFlowForecastView`**: Vizuelna radna tabla kontrolinga sa KPI karticama likvidnosti i tabelom projekcija.

### 📧 Automatsko slanje IOS-a i Opomena na E-mail
- **`OpomeneEmailService` (`ERPiData/Services/OpomeneEmailService.cs`)**: Tri nivoa opomena (Podsetnik, Opomena za dug/IOS usaglašavanje, Opomena pred utuženje) sa stilizovanim HTML i tekstualnim šablonima, tabelom dospelih faktura i instrukcijama za uplatu.
- **`AutomatskeOpomeneWindow`**: Upravljanje dospelim potraživanjima sa podrškom za pojedinačno i masovno (bulk) slanje opomena.
- **Centralizovana SMTP konfiguracija**: Proširen model `Firma` sa SMTP podešavanjima za sve module.
- **xUnit testovi**: Dodato 2 nova testa (ukupno **207/207** prolazi).

## [2.13.0] - 2026-08-13

### 🔍 OCR i Pametni DMS (Čitanje skeniranih računa)
- **`OcrInvoiceParser` (`ERPiData/Services/OcrInvoiceParser.cs`)**: Pametna ekstrakcija i validacija finansijskih podataka sa skeniranih slika i PDF-ova (PIB i MB sa ISO 7064 kontrolnim algoritmom, broj računa, tekući račun, datum izdavanja/prometa/valute, osnovica, stope PDV-a 20%/10%/0% i ukupno za uplatu).
- **`OcrEngineService` (`ERPiData/Services/OcrEngineService.cs`)**: OCR motor za obradu skenirane dokumentacije.
- **`OcrPregledRacunaWindow`**: Podeljeni ekran (skenirani original levo, prepoznati podaci desno) sa jednim klikom za kreiranje ulazne kalkulacije magacina ili naloga Glavne knjige i arhiviranjem u DMS.
- **Integracija u `DmsWindow`**: Brzo OCR prepoznavanje računa sa traci i u tabeli priloga.

### 🔒 Granularna prava pristupa (RBAC po modulima)
- **Predefinisane uloge (`Korisnik.cs`)**:
  - `Magacioner`: Pristup isključivo magacinskom i materijalnom poslovanju bez uvida u plate i finansijske bilanse.
  - `Komercijalista`: Izrada ponuda, predračuna i faktura-otpremnica bez prava brisanja i bez uvida u plate/GK.
  - `KadrovskaSluzba`: Matična evidencija radnika, ugovori i plate bez pristupa Glavnoj knjizi.
  - `Administrator`, `Operater`, `Gledalac`, `Prilagodjeno`.
- **Modularni flegovi prava**: `PravoFinansije`, `PravoRobno`, `PravoMaterijalno`, `PravoZarade`, `PravoOsnovnaSredstva`, `PravoSefPfr`, `PravoBrisanjaDokumenata`, `PravoAdministracije`.
- **Interaktivni UI**: Matrica prava u `KorisnikEditWindow` i dinamičko filtriranje opcija i modula u `MainWindow`.
- **xUnit testovi**: Dodato 12 novih testova (ukupno **205/205** prolazi).

## [2.12.0] - 2026-08-13

### ⚡ SEF i Automatizacija Ulazne Dokumentacije (UBL 2.1)
- **`SefUblParser` (`ERPiData/Services/SefUblParser.cs`)**: Namenski XML parser za OASIS UBL 2.1 e-fakture po srpskom SEF profilu. Parsira zaglavlje, PIB/MB, adrese, tekuće račune dobavljača/kupca, sve stavke (sa količinama, cenama, rabatima i stopama PDV-a) i kompletnu poresku rekapitulaciju.
- **Proširenje SEF API klijenta i servisa (`SefApiClient.cs`, `SefService.cs`)**:
  - Preuzimanje punog UBL XML sadržaja i zvaničnog vizuelnog PDF dokumenta sa SEF portala.
  - Slanje odobrenja (`Approved`) ili odbijanja (`Rejected` uz navođenje razloga) direktno na SEF API.
  - Automatsko pronalaženje ili kreiranje dobavljača u bazi (`Partner`) po PIB-u uz dopunu adrese i tekućeg računa.
  - Automatsko generisanje ulazne kalkulacije cene robe u magacinu (`Kalkulacija`/`StavkaKalkulacije`) uz pametno mapiranje/otvaranje artikala.
  - Automatsko knjiženje troškova usluga i robe u Dnevnik glavne knjige (5xxx Duguje, 2700 Duguje, 4350 Potražuje) kao uravnoteženi Nacrt naloga.
  - Automatsko arhiviranje preuzetog UBL XML-a u **DMS** kao prilog uz kreirani dokument.
- **UI prozor `SefUlaznaFakturaDetaljiWindow`**: Detaljan pregled ulazne fakture sa karticama za stavke, specifikaciju PDV-a, generisanje kalkulacije, knjiženje troškova u Glavnu knjigu i brze SEF akcije (Odobri, Odbij, Preuzmi XML/PDF).
- **Proširenje `SefUlazneFaktureWindow`**: Dodata akciona traka, dvoklik i kontekstni meni za brzu obradu faktura.
- **xUnit testovi**: Dodato 4 nova integraciona testa u `SefUblParserTests.cs` (ukupno **193/193** prolazi).

## [2.11.0] - 2026-08-12

### 🧾 Račun-Otpremnica — cena/PDV paritet sa DOS-om
- Nov `Cenovnik` entitet (istorijska cena po datumu/magacinu) i `RacunOtpremnicaStavka.
  PorezUkljucenUCeni` — PDV se sad izvlači iz cene umesto da se dodaje (legacy `por_u_cen`),
  paritet sa `MAT5.PRG unos()`/`cena_artikla()`. Odvojena `CenaOtpremnice` za samostalnu
  otpremnicu (kad se cena na otpremnici razlikuje od fakturne).
- `RacunOtpremnica.DatumPrometa` + izjava o PDV oslobođenju — zakonski obavezni elementi (čl. 42
  Zakona o PDV). `PdvService` sad poreski period računa po datumu prometa, ne datumu izdavanja.
- PDF štampa: raščlanjivanje PDV-a po stopama kad račun ima stavke sa različitim stopama, datum
  prometa, napomena o PDV uračunatom u cenu i o izjavi o oslobođenju.
- `RacunOtpremnicaStavkaRow` — UI-only `INotifyPropertyChanged` wrapper za DataGrid red, rešava
  crash `Items.Refresh() nije dozvoljen tokom AddNew/EditItem tranzicije`.
- Pretraga/izbor artikla u koloni "Artikal" portovana 1:1 iz konto-obrasca u `NalogEditWindow` —
  pouzdan klik mišem, strelice, F2, nov `ArtikalPickerWindow`.
- **Kritičan EF Core bag** (nije nov, otkriven ovde): `SaveRacunAsync` se rušio pri izmeni
  postojećeg računa jer je pozivalac (isti dugoživeći kontekst koji je učitao `_existingRacun`)
  prepisivao `racun.Stavke` PRE poziva servisa — servis sad prima nove stavke kao poseban
  parametar, ne čita `racun.Stavke`.

### 🏷️ DOS uvoz — kupac na Robnim računima (robna analitika bez finansijskog pandana)
- DOS uvoz nikad nije povezivao `RacunOtpremnica.PartnerId` (samo `KontoKupcaId`) — ispravljeno u
  `ErpiFinansijeImporter` (mečovanje `KontoKupca → Konto.BrojKonta → Partner.SifraPartnera`, ista
  konvencija kao dobavljači na Kalkulacijama). Repair alat za već uvezene račune: dugme 🔧 u
  `RacuniOtpremniceView` → `RacunOtpremnicaService.PopraviNedostajucePartnereAsync()`.
- Otkriveno da mnogi kupci na Robnim dokumentima uopšte ne postoje u ANKONT.DBF (finansijski
  partneri) — to su čisto robno-analitička konta (npr. `201xxx`) koja postoje samo u
  `KONTPLAN.DBF`. `ErpiFinansijeImporter` sad, kad takva konta imaju pravo ime u kontnom planu a
  nemaju partnera, automatski kreira NOVOG Partnera (bez mečovanja po imenu sa postojećim
  partnerima — rizik pogrešnog spajanja različitih pravnih lica sa sličnim imenom).
- `DosImportService` je čitanje kontnog plana/partnera (`KONTPLAN.DBF`/`ANKONT.DBF`) držao striktno
  vezano za čekboks "Finansijsko" — uvoz "samo Robno" zato nikad nije video ta dva šifarnika. Sad
  se čitaju i kad je izabrano samo Robno (GK nalozi ostaju striktno vezani za Finansijsko).
- Kontni plan sad prenosi i adresu/mesto/telefon/žiro-račun (ranije samo naziv), a auto-kreirani
  partneri iz robne analitike nasleđuju i PIB/matični broj iz `KONTPLAN.DBF`. Stari "R"-sufiksirani
  placeholder konti (kreirani pre ove ispravke, bez pravih podataka) se sad backfilluju na licu
  mesta čim se naiđe na pravi red iz kontnog plana, umesto da ostanu duplirani/nepovezani.

### ✨ Sitne ispravke
- Brojevne kolone u tabelama (Blagajna, Partneri, Devizno valviranje, Bruto bilans, IOS, Kompenzacije,
  Mesta troška, Materijalno, Nivelacija, Poreski bilans, Putni nalozi) usklađene sa
  `NumericColumnElementStyle`.
- `MestaTroskaService` — analitika filtrirana po `Nalog.Status == Proknjizen` umesto zastarelog
  `IsKnjizen`; `MestaTroskaView` više ne guta grešku pri učitavanju analitike ćutke.

## [2.10.0] - 2026-08-12

### 📎 DMS — prilozi na Partnere, Sredstva, Račune/Kalkulacije, Ugovore + skeniranje
- `DokumentPrilog` sad podržava šest vlasnika (Nalog/Račun-otpremnica/Kalkulacija + novo Partner/
  Sredstvo/Ugovor). Dugme 📎 sa bedžom broja priloga dodato u `PartnerEditWindow`, Sredstva
  analitička kartica, `RacunOtpremnicaEditWindow`, `KalkulacijaEditWindow`, `UgovorDokumentWindow`.
- Prilog uz nalog knjiženja može opciono da se veže za konkretnu stavku (ne samo ceo nalog), sa
  bedžom i prečicom za DMS direktno iz liste naloga bez otvaranja celog dijaloga.
- Novo dugme "📷 Skeniraj" u DMS prozoru — poziva sistemski Windows WIA dijalog i prilaže rezultat
  istim putem kao ručno izabran fajl.

### 🗂️ Robno — UvoznaKalkulacijaWindow
- `UvozneKalkulacijeView` je bio prazan grid bez načina da se doda nov red — portovan nedostajući
  `UvoznaKalkulacijaWindow` iz ERPiFinansije, prilagođen ERPi šemi. Sitne dopune: `PonudeView`
  "Pretvori u Račun" ograničeno na predračune, ispravljeno omogućavanje Knjiži/Rasknjiži dugmadi u
  `RacuniOtpremniceView`, nova prečica "Kartica Konta" na Dashboard-u.

### 🧭 Navigacija i pomoć
- Kontni plan i Dnevnik glavne knjige premešteni u "Glavna knjiga i Nalozi", ukinut suvišan
  Expander "MATIČNI PODACI".
- `uputstvo-erpi.html` potpuno prepisan kao pravi master hub (rad sa firmama, korisnici i uloge,
  Rezervne kopije, Istorija izmena, dvoslojni F1 sistem, REST API/Web Dashboard).
- Ukinut mrtav duplikat `Views/Zarade/Pomoc/*`, konsolidovano u kanonski `Views/Pomoc`.

### 📜 Audit
- Nov generički EF `SaveChangesInterceptor` za šifarnike bez sopstvenog audit traga (Partner/
  Konto/MestoTroska/Sredstvo/Artikal/Magacin).

### 🧾 Partneri i izveštaji
- IOS izveštaj dobio pravi opseg konta (od–do), oživljen zaboravljen filter ekran
  (`IosIzvestajWindow`) kao pravi korak pre `IosPreviewWindow`.
- PDF izvoz za obračun zatezne kamate.
- Grupno M:N zatvaranje otvorenih stavki partnera (`ZatvoriGrupnoAsync`, FIFO), `ZatvoriStavkeWindow`
  prerađen sa 1:1 na checkbox multi-select.

### ✅ Testovi
- 172/172 (od 158 u 2.9.1) — novi testovi za audit interceptor i grupno zatvaranje stavki.

## [2.9.1] - 2026-08-11

### 🧾 e-Fiskalizacija (PFR) — pregled/štampa fiskalnog isečka
- Novo dugme "🖨️ Štampaj / Pregled isečka" u `FiskalniRacunWindow` — generiše PDF fiskalnog
  isečka (format A5: firma, broj i datum, stavke sa PDV oznakom po stavci, rekapitulacija PDV-a
  po stopi, ukupan iznos, QR kod verifikacionog URL-a) i otvara ga u podrazumevanom PDF čitaču.
  Radi i za pravu fiskalizaciju i za **SIMULACIJU** — simuliran isečak je jasno označen
  ("*** SIMULACIJA ***", bez QR koda), pošto kasa u praksi uvek štampa isečak bez obzira da li je
  PFR uređaj stvarno dostupan. Nova funkcionalnost — izvorni ERPiFinansije nije imao PDF pregled
  za fiskalne isečke, samo tekstualni prikaz žurnala.

## [2.9.0] - 2026-08-11

### 📜 Istorija izmena (generički revizioni trag)
- Nov `AuditLog` model + `AuditLogService` — evidentira ko je i kada izvršio osetljivu radnju nad
  kojim zapisom. Odvojeno od Zaradinog postojećeg `ObracunAudit`/`RevizioniTragWindow` mehanizma
  (koji ostaje kakav je), pokriva ono što Finansije/Korisnici/Firma do sada nisu imali.
- Ožičeno na: proknjiženje/rasknjiženje naloga (pojedinačno i masovno), kreiranje/izmena/brisanje/
  deaktivacija korisničkog naloga, izmena i aktivacija firme, i (novo) svaki ishod ESIR
  fiskalizacije Računa-otpremnice (Fiskalizovan / Fiskalizovan-simulacija / Greška).
- Nov tab "📜 Istorija izmena" u Podešavanjima — pretraga i filter po entitetu, poslednjih 500
  zapisa.

### 🧾 e-Fiskalizacija (PFR) — PDV oznake po stavci
- Fiskalni zahtev za Račun-otpremnicu sad šalje pravu poresku oznaku po stavci (Đ=20%, E=10%,
  А=0%, `PfrService.PdvLabelaZaStopu`) umesto uvek podrazumevane "Đ".

### 🧮 Faza 6 — testovi za Prijava/Rashod Sredstava → Glavna knjiga
- `PrijavaKnjizenjeServiceTests` (8) i `RashodKnjizenjeServiceTests` (9) — paritet sa već
  postojećim testovima za Amortizaciju/Zarade (uravnotežen nacrt nalog, dedup po `IzvorId`,
  greške na nedostajući konto/mapiranje, sabiranje stavki istog konta, odvojen brojač po vrsti
  naloga).

### 📊 PDV evidencija — PDF štampa knjiga
- Novo 🖨️ dugme na KIR i KPR tabu u `PdvEvidencijaView` — štampa PDF knjige (landscape A4, red po
  zapisu + zbirni red), ranije su postojali samo Excel i PP-PDV XML izvoz.

### 🧪 Testovi
- `dotnet test ERPiData.Tests` → 158/158 (28 novih testova u odnosu na 2.8.0-ino 130/130: 4 za
  `AuditLogService`, 17 za Prijava/Rashod GK knjiženje, 3 za PDV KIR/KPR, 4 za PDV oznake po
  stavci).

## [2.8.0] - 2026-08-11

### ❓ F1 Pomoć hub
- Kontekstualni F1 help dodat na svih preostalih 25 edit ekrana (`EditHelpWindow`) — pokriva sve
  module (Finansije, Sredstva, Zarade).
- `uputstvo-sredstva.html` kompletno prepisan (104 → ~730 redova) sa stvarnim nazivima dugmadi,
  kolona i tokom rada, isti dizajn-sistem kao Finansije/Zarade uputstva.

### 🧾 e-Fiskalizacija (PFR) — dopuna
- Zaštita od duple fiskalizacije istog računa.
- Novo polje `RacunOtpremnica.FiskalniStatus` (Fiskalizovan/Simulacija/Greška).
- Izbor načina plaćanja (Gotovina/Kartica/Virman) i nov `FiskalniRacunWindow` sa prikazom
  fiskalnog žurnala i verifikacionog URL-a, otvara se dugmetom "🧾" u Računi-Otpremnice.
- Nova kolona "Fiskalni status" u pregledu računa.

### 💾 Rezervne kopije (Backup)
- Nov tab "💾 Rezervne kopije" u Podešavanjima — ručni backup, vraćanje iz fajla, automatski
  backup pri izlasku (jednom po izlasku ili jednom dnevno), istorija sa vraćanjem/brisanjem po
  redu. Ranije je servis postojao, ali bez ijednog UI ekrana.

### 🗂️ Sve firme (FirmeView)
- Nov tab "🗂️ Sve firme" u Podešavanjima — CRUD nad svim registrovanim firmama (registar, ne
  samo firmama u podrazumevanom folderu): aktiviranje (prelazak na drugu firmu), izmena osnovnih
  podataka, uklanjanje sa liste (baza na disku se ne briše).

### 📦 Računi-Otpremnice — SEF/PFR ožičeni na ekran
- Filter statusa (Svi/Proknjiženi/Neproknjiženi/Predračuni), tabela stavki izabranog računa,
  dugmad Pošalji na SEF / UBL XML / Fiskalizuj (PFR) / Ulazne SEF, i masovno knjiženje —
  backend je već postojao, sada je ožičen na ovaj ekran.

### 🧪 Testovi
- `dotnet test ERPiData.Tests` → 130/130 (3 nova testa za PFR fiskalizaciju).

## [2.7.0] - 2026-08-10

### 🔗 Faza 6 — automatsko knjiženje Zarade/Sredstva → Glavna knjiga
- Zarade: novo dugme "📘 Proknjiži direktno (nacrt)" u Nalog za knjiženje — upisuje nalog pravo u
  `Nalog`/`StavkaNaloga` iste baze (zamena za dosadašnji JSON izvoz/ručni uvoz, ostatak iz doba
  odvojenih aplikacija). Sprečeno dupliranje po periodu/isplati.
- Sredstva (Amortizacija): nov šifarnik "⚙️ Konta za amortizaciju" (po kontu sredstva — trošak/
  ispravka vrednosti) i automatski GK nalog posle internog knjiženja, kao odvojen best-effort
  korak koji ne ruši već potvrđeno interno ažuriranje kartica.
- Sredstva (Prijava/Rashod): šifarnik "⚙️ Konta za amortizaciju" proširen kontom dobavljača i
  rashodnim/dobitnim kontom po kontu sredstva. Prijava sada knjiži nabavku (duguje sredstvo,
  potražuje dobavljač), a Rashodovanje/Prodaja/Otuđenje/Brisanje knjiže rasknjiženje ispravke
  vrednosti i gubitak/dobitak od preostale (neotpisane) vrednosti — isti best-effort obrazac kao
  Amortizacija, ne ruši već potvrđeno interno knjiženje kartice pri neuspehu.
- Oba naloga se upisuju kao nacrt — pregled/knjiženje ostaje na Finansije → Nalozi.
- Novi xUnit testovi za GK knjiženje Zarada i Sredstava (dedup po periodu/nalogu, razrešavanje
  konta/mesta troška, greške na neuravnotežen nalog ili nedostajuće mapiranje, odvojeno brojanje
  naloga po vrsti) — 117/117 testova prolazi.

### 📥 DOS uvoz — Materijalna primopredaja (M_PRIMO.DBF)
- Povezan uvoz M_PRIMO.DBF (pravo Materijalno knjigovodstvo, FK na Materijal) — do sada je
  postojala samo šema bez ijednog pisca. Ponovo koristi postojeći mapер za MAT_NAL/ZADUZ/RAZDUZ,
  razdvojeno po vrsti dokumenta od Robno primopredaje.

### 🏗️ Dokumentacija
- `docs/ARCHITECTURE.md` ispravljen: prethodni opis multi-tenant šeme (`FirmaMasterContext`/
  `FirmaMaster.db`, pogrešna imena modela) nije odgovarao stvarnom kodu — zamenjen tačnim opisom
  (jedan `.db` fajl po firmi, `AppSession` bez DbContext-a) i napomenom o tehničkom dugu
  (Zarade ekrani i dalje ne dele `_db` kroz konstruktor kao Finansije/Sredstva).

## [2.6.2] - 2026-08-09

### 🗂️ Navigacija i UI unifikacija
- Harmonika (accordion) meni po modulima u bočnoj navigaciji — svi tabovi i ekrani Robnog i
  Materijalnog knjigovodstva (14 + 6 ekrana) raspoređeni direktno u sklapajuće grupe umesto
  gornjih tabova. `ModernTabControlStyle`/`NoHeaderTabControlStyle` u `App.xaml`.
- Jedinstvena selekcija u bočnom meniju (`GroupName="SidebarNavGroup"`) — uvek tačno jedna
  aktivna (plava) stavka.
- Unifikacija primarne/pomoćne dugmadi u toolbar-ovima na standardni `IconButtonStyle`
  (🖨️ PDF, 📊 Excel, 🔄 Osveži) i standardne boje po akciji (➕/✏️/⚡/⚙️/🗑️) kroz Magacin ekrane.

### 📊 Robno knjigovodstvo — štampa, izvoz, F2 piker
- Nova PDF štampa za Robno kretanje (Primopredaje/Zaduženja/Razduženja) i Zapisnik o nivelaciji
  cena (`PdfReportService.GenerisiRobnoKretanjePdf`/`GenerisiZapisnikONivelacijiPdf`), portovano
  iz ERPiFinansije.
- Robni Bruto bilans dobija PDF štampu (ceo bilans, raspored artikala, stanje po artiklima) i
  Excel izvoz.
- Dugme "⚙️ Svođenje cena" u Nivelacije cena zaliha — automatski preračun prodajnih cena na
  zalihama iz materijalne kartice (`NivelacijaService.SvodjenjeNaProdajnuVrednostAsync`).
- F2 artikal-piker (`ArtikalPicker.PripremiZaGridIzmenu`) ožičen u grid editorima Kalkulacija/MP
  kalkulacija/Narudžbenica/Ponuda/Račun-otpremnica/Robno kretanje.
- Robne kartice: čekboks višestruka selekcija artikala za grupnu štampu više kartica odjednom.
- Robno dashboard: "Poslednje kalkulacije" sad spaja VP i MP kalkulacije u istu listu.

## [2.6.1] - 2026-08-09

### 📚 Dokumentacija i uputstva
- Novo objedinjeno HTML uputstvo za ERPi (`uputstvo-erpi.html`) i dopuna uputstva za Sredstva
  (`uputstvo-sredstva.html`) u F1 Pomoć hub-u.
- Repo dokumentacija: `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/DEPLOYMENT.md`,
  `docs/DBF_MIGRATION.md`.
- XML doc komentari dodati kroz servise, view-ove i modele (Finansije/Sredstva/Zarade/
  Migration) radi lakšeg snalaženja u kodu — bez funkcionalnih izmena.

## [2.6.0] - 2026-08-09

### 🧾 Nalozi (Glavna knjiga) — devizno knjiženje, PDV, F2 pretraga konta, prilozi
- `NalogEditWindow` dobija devizne kolone (`Valuta`/`KursValute`/`DevizniDuguje`/
  `DevizniPotrazuje`), PDV kolone (`Osnovica`/`StopaPdv`), F2 pretragu konta
  (`KontoPickerWindow`, editabilan uživo filtriran ComboBox u koloni Konto) i šifarnik opisa
  promena (`PromeneWindow`) — paritet sa ERPiFinansije.
- Prilozi/DMS u nalogu (`DmsWindow`), koristi postojeći `DmsService`/`DokumentPrilog`.
- Ispravka: snimanje naloga ranije nije prenosilo Valuta/KursValute/DevizniDuguje/
  DevizniPotrazuje/Osnovica/StopaPdv iz radne kopije stavki — uneto bi se izgubilo.
- `NaloziView`: Proknjiži/Rasknjiži sad zavise od selekcije i aktivnog filtera
  (Svi/Proknjiženi/Neproknjiženi), desni klik na red prvo selektuje red, brisanje/izvoz u Excel
  rade nad više selektovanih naloga odjednom.

### 📊 Magacin — Robne/Materijalne kartice
- Robne kartice dobijaju opciju "🏢 Svi magacini" (zbirni pregled kartice artikla po svim
  magacinima).
- Kartica materijala: dupli klik na stavku otvara izvorni dokument (ulaz/trebovanje/
  primopredaja).

### 🧪 Testovi i uputstvo
- Novi unit testovi za `BankIzvodParsers`/`BankIzvodFormatDetector` (Halcom/Asseco XML,
  CAMT.053, MT940).
- Popunjeno opsežno HTML uputstvo za Finansije (`uputstvo-finansije.html`) i ispravljeno da se
  HTML uputstva kopiraju u build izlaz.

## [2.5.0] - 2026-08-07

### 🧮 Kalkulacije — zavisni troškovi nabavke
- Veleprodajne kalkulacije sad računaju zavisne troškove nabavke (transport, uskladištenje,
  utovar/istovar, transportno osiguranje, ostalo) i srazmerno ih raspodeljuju po stavkama
  artikala — potpuni paritet sa ERPiFinansije.
- Maloprodajne kalkulacije dobijaju sopstveni editor ("➕ Nova"/"✏️ Izmeni",
  `MaloprodajnaKalkulacijaEditWindow`) i polje "Konto dobavljača" (podrazumevano 4350, po
  potrebi drugi konto) umesto fiksnog konta 4350 na svakoj knjiženoj kalkulaciji.
- DOS uvoz iz ERPiFinansije (`ErpiFinansijeImporter`) sad prenosi sve pozicije kalkulacije
  (troškove, razliku u ceni, PDV, konto dobavljača) umesto samo osnovnih polja.
- Novi unit testovi za raspodelu zavisnih troškova i knjiženje u Glavnu knjigu (veleprodaja i
  maloprodaja).

### 🧾 Partner picker
- Nova deljena komponenta `PartnerPicker` (pretraga po šifri/nazivu/PIB-u, isti obrazac kao
  postojeći `KontoPicker` za kontni plan) — prvo uvedena u Račun-otpremnicu.

### 💰 Blagajna, Kompenzacije, Putni nalozi, Mesta troška — PDF štampa i Excel izvoz
- Blagajna: štampa pojedinačnog blagajničkog naloga (uplatnica/isplatnica) i blagajničkog
  dnevnika u PDF, plus izvoz u Excel.
- Kompenzacije, Putni nalozi i Mesta troška/projekti dobijaju izvoz u Excel (do sad nisu imali).
- `ExcelExportService` sad ume da primi višelinijsko zaglavlje (kao u PDF štampi) umesto
  fiksnog naslova/datuma.

### 🏛️ Bilansi (APR) — status poruke
- Bilans Stanja i Bilans Uspeha na ekranu "Zvanični Finansijski Izveštaji za APR" sad prikazuju
  iste statusne poruke kao ERPiFinansije — upozorenje o (ne)ravnoteži Aktiva/Pasiva i poruku o
  neto dobitku/gubitku perioda (do sad su postojali samo tooltip-ovi na dugmadima, bez ijedne
  poruke).

### 🎨 Partneri — icon-only dugmad
- Dugmad za kursnu listu, verifikaciju računa, IOS PDF i obračun kamate su sad icon-only sa
  tooltip-om, u skladu sa ostatkom aplikacije.

### 🛠️ Sitne ispravke
- Robni bruto bilans: zadnje stanje/saldo/cena po artiklu se sad ispravno računaju kad je
  poslednja kartica nulta (isti tip greške kao ranija DBF uvoz agregata za Sredstva) — više se ne
  gubi vrednost zadnjeg stanja.
- Uklonjeno mrtvo dugme "Uvoz iz ERPiZarade" iz Nalozi ekrana i prečac "📥 Uvoz Podataka Wizard"
  sa Dashboard-a (uvoz je dostupan kroz Podešavanja).

## [2.4.1] - 2026-08-07

### 🛠️ Sitne ispravke posle 2.4.0
- DOS uvoz Sredstava — progres traka sad stvarno prati napredak po koraku/fazi umesto da
  stoji zaglavljena na 15%/60%/100%.
- Uklonjeno duplo dugme "Uvoz iz ERPiZarade" iz Finansije uvoznog wizard-a — taj uvoz ide
  isključivo kroz Zarade → Podešavanja, gde već postoji identičan tok.

## [2.4.0] - 2026-08-07

### 📖 Nalog — pregled proknjiženog naloga
- Dupli klik na stavku u Kartici konta sad stvarno otvara nalog (do sad je prikazivao samo
  poruku sa detaljima stavke, iako je tooltip odavno obećavao otvaranje) — dodat i kontekst-meni
  (desni klik) sa "👁️ Pregledaj nalog" / "✏️ Izmeni / Rasknjiži nalog".
- Proknjiženi nalozi se sad otvaraju u readonly/pregled režimu (polja i grid zaključani), sa
  jedinim aktivnim dugmetom "🔓 Rasknjiži i izmeni" (samo administrator, uz potvrdu) — isto
  ponašanje iz Nalozi liste i iz Kartice konta.

### 🧮 Kompenzacije, Putni nalozi — Nova/Izmeni
- Kompenzacije konačno imaju editor ("➕ Nova"/"✏️ Izmeni") — dupli klik na kandidata u
  "Pametnom skeniranju" predpopunjava novu kompenzaciju sa tim partnerom.
- Putni nalozi konačno imaju editor ("➕ Nova"/"✏️ Izmeni") umesto poruke "biće dostupno u
  narednom prikazu".
- Novo: "📤 Izvoz putnih naloga za Zarade" — pravi JSON fajl koji obračun zarada (Isplata
  naknada) ume da učita, po mesecu/godini; uvozni lanac Finansije→Zarade je sada kompletan.

### 👤 Korisnici i uloge
- Nov ekran za upravljanje korisnicima i njihovim ulogama (do sad se korisnici nisu mogli
  administrirati kroz UI, samo kroz prijavu) — dostupan iz nove stavke "👤 Korisnici i uloge" u
  bočnom meniju.

### 💱 Kursna lista
- Nov ekran za pregled kursne liste. Usput ispravljen bag: dugme "Kursna lista" u Partnerima je
  greškom otvaralo prozor za devizno valorizovanje, ne kursnu listu.

### 🧾 Račun-otpremnica — prava PDF štampa, konverzija predračuna, uslužne stavke
- Dugme "🖨️ PDF" na Računima-otpremnicama sad stvarno generiše i otvara PDF (ranije je samo
  prikazivalo poruku o uspehu, bez ijednog fajla).
- Novo dugme "🔄 Pretvori u fakturu" pretvara predračun u pravi račun.
- Stavke računa-otpremnice sad mogu biti i **usluga** (opis + jedinica mere, bez šifre artikla) —
  magacin je obavezan samo ako račun ima bar jednu robnu stavku, čisto uslužni računi se
  knjiže/čuvaju bez magacina.

### 🚀 SEF e-Fakture i PFR fiskalizacija — od mock podataka do pravih poziva
- "Fakture" ekran (SEF) je do sad radio nad izmišljenim podacima (lažni iznosi, lokalna promena
  statusa bez ijednog mrežnog poziva) — sad prikazuje prave proknjižene Račune-otpremnice i zove
  stvarni SEF servis za slanje/status/UBL izvoz.
- SEF i Fiskalizuj dugmad se sad automatski uključuju/isključuju po tipu partnera (pravno lice
  s PIB-om → SEF, fizičko lice/bez partnera → fiskalni račun), u skladu sa Zakonom o
  fiskalizaciji.
- Nova PFR fiskalizacija računa-otpremnice stavka-po-stavka (ne više jedna zbirna stavka).
- Nov ekran za preuzimanje ulaznih e-faktura sa SEF-a ("📥" dugme u Fakturama).

### 📊 Analitički drill-down bruto bilansa
- Novo "🔎" dugme u Bruto bilansu otvara analitički pregled prometa grupisan po partneru (ne
  samo po kontu), sa poštovanjem već izabranog perioda.

## [2.3.0] - 2026-08-07

### 🎨 Zarade modul i sidebar usklađeni sa ERPiZarade
- Dugmad u celom Zarade modulu (PrimaryButton/SecondaryButton) sad nose ljubičastu paletu
  samostalnog ERPiZarade (#2D1B42/#43305F) umesto opšte plave — isti obrazac po kom je ranije
  urađen SredstvaStyles.xaml, preko jedne merge-ovane ZaradeStyles.xaml pa nije trebalo dirati
  pojedinačne strane.
- Uklonjena duplirana ivična margina — `MainContentHost` (Frame) više ne nosi sopstveni
  `Margin="24"` pored margine svake strane, isto kao u sva tri samostalna app-a; ranije se
  zbrajalo (do 48px) i pravilo primetno veće ivice nego u ERPiZarade.
- Širina bočnog menija 240→220px (usklađeno sa ERPiZarade/ERPiSredstva).
- Preklopnik modula (Finansije/Zarade/Sredstva dugmad na vrhu menija) sad ima manju unutrašnju
  marginu (rešen problem sečenja teksta u uskoj koloni) i svaki modul svoju boju kad je aktivan
  — plava/ljubičasta/zelena — umesto zajedničke plave za sva tri; boje neaktivnog stanja takođe
  prate paletu svog modula (ista ljubičasta/zelena nijansa kao u listi stavki menija ispod).
- Broj verzije u dnu menija je sad klikabilan — otvara dijalog "Istorija izmena" (isti prozor
  dostupan i iz Pomoći).

### 🐛 Ispravke
- "Isplate naknada van radnog odnosa" je otvarala isti tok kao "Isplate zarada" (nedostajao
  parametar roda isplate) — prikazivala vrste isplate koje postoje samo kod zarade (akontacija,
  13. plata...) umesto ekrana sa samo datumom, kakav naknade zahtevaju.
- Uvoz Osnovnih sredstava iz DOS/DBF baze (Legacy `SredstvaDbContext`) pravio praznu bazu bez
  ijedne tabele ("no such table: Firme") jer taj kontekst nema svoj Migrations folder pa je
  `Migrate()` bio no-op; zamenjeno sa `EnsureCreated()` (isti fix kao ranije za Finansije).

### 🧭 Navigacija
- Povratak na modul (klik na Finansije/Zarade/Sredstva tab) sad otvara stavku menija na kojoj je
  korisnik poslednji put bio u tom modulu, ne uvek Radnu tablu.
- Kad se bočni meni sklopi na uzanu traku, stavke menija ostaju samo sa vodećom ikonicom
  (umesto da im se tekst seče na pola) — pun naziv se seli u ToolTip.

### 📥 Uvoz Zarada — vizuelna povratna informacija
- Uvoz iz ERPiZarade i DOS/DBF uvoz Zarada sad prikazuju mali dijalog sa indikatorom napretka i
  uživo logom dok traje (operacija zna da potraje po nekoliko minuta bez ijedne druge povratne
  informacije) — isti obrazac kao progres dijalozi u Finansijama/Sredstvima.

## [2.2.0] - 2026-08-06

### 📊 Radna tabla za Zarade modul + redizajn tabela
- Nova početna stranica modula Zarade — "Radna tabla" (KPI kartice: aktivnih radnika, neto/bruto
  masa, aktivnih kredita; grafikon pregleda zarada po mesecima) — port iz ERPiZarade, isti obrazac
  kao Radna tabla u Finansijama i Osnovnim sredstvima.
- Izgled tabela (DataGrid) u celom Zarade modulu prenet iz ERPiZarade — bela pozadina, naizmenično
  osenčeni redovi, uređen header — umesto dotadašnjeg podrazumevanog Windows izgleda.

### 🔄 Automatska provera ažuriranja
- Aplikacija sad pri pokretanju proverava da li postoji nova verzija na GitHub-u
  (`github.com/blagojevicboban/ERPi`) i, ako postoji, nudi preuzimanje i instalaciju u jednom
  kliku — isti mehanizam (Velopack) kao ERPiFinansije/ERPiSredstva/ERPiZarade.
- Broj verzije se sad ispisuje i na prvom ekranu aplikacije (izbor firme), ne samo posle prijave.
- Nov dijalog "📋 Istorija izmena" (dostupan iz Pomoći) sad učitava ovaj fajl uživo, umesto
  ranijeg fiksnog teksta — uvek prikazuje stvarnu istoriju verzija.

### 🧾 DOS uvoz — dalje dopune Robno/Materijalno (nastavak iz 2.1.1)
- DOS/DBF uvoz Finansija dopunjen dodatnim tabelama do pariteta sa ERPiFinansije: Poreske tarife,
  Kalkulacije, Maloprodajne kalkulacije, Računi-otpremnice, Nivelacije cena (Robno), Materijalne
  kartice, Ulazi, Trebovanja, Primopredaje/Zaduženja/Razduženja (Materijalno), Promene (opisi
  stavki naloga).
- Ispravljen pad uvoza (duplirane šifre artikala u ARTIKLI.DBF) i redosled brisanja postojećih
  podataka pri opciji "Obriši postojeće" — brisanje se sad izvršava tek pošto je uvoz iz DBF
  fajlova uspešno završen, ne pre; ranije bi neuspeo pokušaj ostavio aktivnu bazu praznu.

## [2.1.1] - 2026-08-06

### 🧾 DOS uvoz — paritet Robno/Materijalno sa ERPiFinansije
- DOS/DBF uvoz sad čita 16 vrsta fajlova umesto dosadašnjih 6 — dodati Kalkulacije, Poreske
  tarife, Računi-otpremnice, Nivelacije cena, Maloprodajne kalkulacije, Ulazi/Trebovanja/
  Primopredaje za Materijalno.
- Ispravljen redosled brisanja pri uvozu "Obriši postojeće" — brisanje se sad izvršava tek pošto
  je privremena baza uspešno popunjena iz DBF fajlova, ne pre; ranije bi neuspeo uvoz ostavio
  aktivnu bazu praznu.
- Finansijski izveštaji dopunjeni; DOS uvoz Osnovnih sredstava dobio isti izgled (reskin) kao
  ostatak aplikacije.

## [2.1.0] - 2026-08-06

### 📦 Samostalno izdanje
- ERPi postaje potpuno samostalan repozitorijum — uklonjene sve spoljne zavisnosti od
  ERPiFinansije/ERPiSredstva/ERPiZarade repozitorijuma.
- Velopack izdanja za 32-bitne i 64-bitne Windows sisteme.
- Objedinjeno korisničko uputstvo (preko 60 tema, pretraga, izvoz u HTML) za sva tri modula na
  jednom mestu.

## [2.0.0] - 2026-08-06

### 🌟 Prvo objedinjeno izdanje
Spajanje tri samostalne aplikacije — **ERPiFinansije**, **ERPiSredstva** i **ERPiZarade** — u
jedan desktop paket sa jednom SQLite bazom po firmi.

- **Finansije**: Glavna knjiga i nalozi za knjiženje, partneri (CRUD) i otvorene stavke,
  zatvaranje (uparivanje) stavki, IOS izveštaj i zatezna kamata, Magacin/PDV, SEF/PFR
  podešavanja, šifarnici, izveštaji GK, bilansi, izvodi banke, blagajna, devizno poslovanje,
  putni nalozi, kompenzacije, Robno i Materijalno knjigovodstvo.
- **Osnovna sredstva**: kartice sredstava, obračun amortizacije, popis, revalorizacija,
  izveštaji.
- **Obračun zarada**: kompletan port obračuna zarada iz ERPiZarade i uvoz postojećih podataka.
- **Uvoz podataka**: `UvozWizardView` — uvoz iz baze ERPiFinansije ili direktno iz starih DOS/DBF
  fajlova, uz izbor modula i opciju brisanja postojećih podataka pre uvoza.
- Zajednički WPF Shell sa prijavom, izborom firme i navigacijom po modulima, svaki modul
  zadržava prepoznatljivu boju svoje izvorne samostalne aplikacije.
