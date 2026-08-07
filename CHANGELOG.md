# 📋 Istorija izmena (Changelog) — ERPi

Sve značajne promene i novine u aplikaciji **ERPi** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

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
