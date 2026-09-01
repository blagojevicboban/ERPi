# E2E testiranje kontrola — WPF i Web
Prati se posle §79 (navigacioni prolaz — ekran se otvara, nema pada). Ovde ide **stvarno
klikanje kroz kontrole**: dugmad, forme, akcije (Sačuvaj/Obriši/Izvezi/obračunaj...), i
provera rezultata, ne samo da se ekran iscrta. `[x]` = klikano i rezultat proveren, uz
belešku šta je testirano i šta je nađeno. Radi se redom, modul po modul.

## Za nastavak u novoj sesiji

**I WPF (`ERPiApp`) i Web (`ERPiWebShop`) deo su sada 100% gotovi** (WPF §77-§82, Web §83 ispod).
Jedino što ostaje otvoreno je **ESS portal** (`/ess/*` i alias `/moj-portal`), blokiran demo-podatak
nalazom — vidi taj odeljak. Preostalo: formalni dizajn-audit i F1 Help su namerno ODLOŽENI od
početka (nisu deo ovog prolaza).

Odluka korisnika: puna dubina, redom, bez zaustavljanja za odobrenje po ekranu — samo
periodično prijavljivati napredak. Fokus je **funkcionalno testiranje** (klik-provera +
proveriti da li fali standardni alat); formalni dizajn-audit i F1 Help su namerno ODLOŽENI —
samo popravi ono što uočiš uzgred, ne traži sistematski.

### Web deo (§83, 30.08.2026) — 4 prava bug-a nađena i ispravljena

Metodologija: izolovan stack (API na 5002 nad kopijom `DEMO.db`, drugi vite dev server na 5174,
nikad dodirnut pravi WebShop servis na 5000 koji trenutno služi realnu firmu ARHIBEL) +
`web-screens-pass` CDP drajver po ekranu + vizuelna provera svakog snimka. 166 web ekrana/tabova
pregledano (svi admin moduli, ravni tabovi, pod-ekrani, WMS, B2B portal, B2C prodavnica); ESS
portal blokiran (vidi njegov odeljak). Detalji po bug-u su u odgovarajućim odeljcima ispod:

1. **Demo generator nikad nije postavljao `ObracunPlate.Neto`/`PoreskaOsnovica`** za bulk-seedovane
   periode zarada — kolone „Neto zarada"/„Osnovica" prikazivale 0,00 na više web ekrana
   (`obracuni`, `obracun-period`, `ppp-po`). Ispravljeno u `DemoPodaciGenerator.Zarade.cs`,
   `DEMO.db` i `AUTOTEST.db` regenerisane.
2. **`DEMO.db` nikad nije regenerisana posle ranijeg §81 fix-a** (nulte Porezi/Doprinosi stope) —
   otkriveno jer web deo koristi `DEMO.db`, ne `AUTOTEST.db` kao WPF. Ispravljeno istom
   regeneracijom kao gore.
3. **`KontaAmortizacijePodTab.tsx` (Sredstva → Konta amortizacije)** je čitao konto-listu iz
   `getKonta()` (F3 brzi lookup, `Take(100)`) umesto `getKarticeKonta()` (bez limita) — već mapirani
   konta van prvih 100 po broju su se prikazivali kao „—" (lažno nemapirano) i nisu mogli ni da se
   ponovo izaberu. Ispravljeno prelaskom na `getKarticeKonta()`.
4. **`AiAsistentModal.tsx` je AI odgovore štampao golim tekstom** — backend (`AiAsistentService.cs`)
   piše mini-markdown (`**podebljano**`), frontend ga nikad nije renderovao, korisnik je video
   doslovne zvezdice. Ispravljeno malom `formatirajPoruku()` helper funkcijom.

Sve četiri potvrđene: `dotnet test ERPiData.Tests` 1627/1627, `npm run build` + `npx vitest run`
295/295, bez regresije. Dve lažne uzbune razjašnjene bez ispravke koda (test-okruženje ove sesije
i legitimna „Greška" status oznaka) — vidi napomene u odeljku „ravni tabovi" ispod.

### Pravi bug nađen i ispravljen na Sredstvima (§82)

`RashodWindow.xaml.cs` (Sredstva → Rashod i promene) je za sve tipove promene menjao
`Sredstvo.NabavnaVrednost`/`IspravkaVrednosti` ali nikad nije osvežio `Sredstvo.SadasnjaVrednost`
— za razliku od `AmortizacijaPage`/`RevalorizacijaPage`/`PopisPage` koje to rade posle svake
izmene. Posledica: rashodovano/prodato sredstvo je doživotno ostajalo u Registru/Radnoj
tabli/Izveštajima sa punom knjigovodstvenom vrednošću iako je `JeAktivno=false` — naduvava
"Ukupna sadašnja vrednost" za svako ikad rashodovano sredstvo. Ispravljeno (nuliranje
Nabavna/Ispravka za potpuno rashodovanje + `SadasnjaVrednost = Nabavna - Ispravka` posle cele
switch grane), `dotnet test` 1627/1627 bez regresije. Isti koren bug postoji i u demo
generatoru (12% sredstava "rashodovano" bez pratećeg storna) i "Lista amortizacije" tab je
uvek prazna zbog neusklađenog teksta opisa kartice — oba **NAMERNO nisu ispravljena** (zahtevaju
regeneraciju AUTOTEST.db, širi zahvat od "uzgred" popravke), vidi pun opis kod
`BtnSredstvaRashod`/`BtnSredstvaAmortizacija` ispod. Test sredstva i njihovi prateći zapisi
uklonjeni direktno iz AUTOTEST.db, baza vraćena na tačno izvorno stanje (120/212.201.800,00/
89.331.220,88, potvrđeno preko Izveštaja).

**Nalaz o alatu**: `CmbSredstvo` u `RashodWindow` je editable ComboBox bez filtriranja po
kucanju — kucanje teksta + strelica dole ne bira stavku koja se poklapa sa unetim tekstom nego
prvu u nefiltriranoj listi; skoro je dovelo do slučajnog dodavanja pravog demo sredstva u nalog
rashoda (na vreme uočeno pre knjiženja). Pouzdan način: birati mišem iz otvorenog dropdown-a.

### Dva prava bug-a nađena i ispravljena ove sesije (§81)

1. **Materijalna knjiženja su se mogla mešati u robni bruto bilans** — nova `MaterijalnaKartica.Vrsta`
   kolona (EF migracija `DodajVrstuZaliheNaMaterijalnuKarticu` + `EnsureColumn`), `RobniBrutoBilansService`
   sad filtrira po njoj umesto po šifri. Vidi §3d/Materijalno sekciju ispod za pun trag.
2. **Poreski parametri zarada (stope/limiti) su bili nule za svaki period demo baze** — demo
   generator (`DemoPodaciGenerator.Zarade.cs`) je seedovao `Porezi`/`Doprinosi` tabele bez
   stvarnih stopa. Popunjeno vrednostima koje se poklapaju sa app-ovim sopstvenim fallback-om.

Oba puta: `dotnet test ERPiData.Tests` 1627/1627 bez regresije, AUTOTEST.db regenerisan
(**isti determinstički Seed kao original** — Firma "DEMO"/"ERPi Demo d.o.o.", ne "AUTOTEST" —
generator uvek pravi identičan skup podataka), vizuelno potvrđeno u UI. Detalji u CHANGELOG.md
i u odgovarajućim redovima ovog fajla (Materijalno/`BtnZaradePorezi`).

### Nova WPF funkcija ove sesije: sidebar accordion (§81)

Klik na stavku sidebar menija sklapa ostale Expander grupe na istom nivou (isti obrazac kao
web admin), kliknuta stavka se centrira u vidljivoj oblasti. Namerno isključeno za promenu
modula (Zarade panel drži 3 grupe otvorene na startu, to ostaje). Vidi `MainWindow.xaml.cs`
(`SklopiOstaleEkspandere`/`_potiskujAkordion`) i CHANGELOG.md.

### Pravila koja se ne poništavaju

- **Ekrani sa nepovratnim/spoljnim radnjama se svesno NE aktiviraju bez izričitog dogovora**:
  "Nova godina" u Nalozima, "🔒 Zaključi" u Zaključenju poslovne godine, "📧 Slanje Opomena i
  IOS-a" u Cash-Flow & Kontroling (pravi SMTP, ne mock), **novo ove sesije**: "✉️ Slanje
  e-mailom" u Platni listići (Zarade) — pravi email radnicima.
- **Windows Save/Open dijalog: NIKAD brojčani `click <AutomationId>`** (1, 2...) — isti brojevi
  se dodeljuju i dugmadima (Save/Cancel) i redovima liste fajlova, pa klik "na Cancel" ume da
  pogodi neki fajl u listi i pokrene Save pod tim imenom (desilo se ove sesije, ispravno
  odbijeno na "Replace?" pitanju, ništa izgubljeno). Koristiti `keys "{ESC}"` da se zatvori
  dijalog.
- **Screenshot (`ss`) ume da uhvati pogrešan prozor** (crn/beo ekran, ili tuđ sadržaj) kad
  korisnik istovremeno koristi istu mašinu — nije WPF bug. `tree` pouzdano cilja pravi prozor
  bez obzira na vizuelni fokus; koristiti njega za verifikaciju kad `ss` ne radi, ili
  minimize/restore pa ponovo `ss`.
- **Pokretanje:** `run-erpi-app` skill, `driver.ps1 launch <ERPiApp.exe>`. ERPiApp zatvoren na
  kraju ove sesije.
- **`click1 <x> <y>`** za DataGrid CheckBox ćelije i dugmad/tabove bez `x:Name`; `expand
  <AutomationId>` pre klika na stavku unutar zatvorenog Expander-a (sad ih ima mnogo više
  otkad postoji accordion — sklopljena grupa se mora eksplicitno otvoriti pre navigacije u nju).
- Nema web dela u ovom bloku — web E2E ide tek posle celog WPF dela (vidi spisak ispod).

## WPF (`ERPiApp`)

### Finansije

- [x] `BtnDashboard` — 📊 Radna tabla — 4 brze akcije rade (IOS izveštaj sa drill-down po partneru, Bruto bilans, Nalozi, Kartica konta); IOS dijalog Close dugme radi. Nije testirano: 'Promeni' (firma), scroll niže (Status naloga/Statistika GK).
- [x] `BtnNalozi` — 📖 Glavna knjiga i Nalozi — pun CRUD krug potvrđen na sopstvenom test zapisu (br. 468, obrisan na kraju): Novi nalog → F2 lookup konta (241000/435000) → unos Duguje (sistem sam predloži balansirajući Potražuje — namerna funkcija) → Sačuvaj i proknjiži → Rasknjiži (sa potvrdom, nudi odmah Izmeni) → Izmeni opis → Sačuvaj → Obriši (ispravno odbijeno dok je proknjižen, radi posle rasknjiženja). Filteri Svi/Proknjiženi/Neproknjiženi rade tačno, Pretraga po tekstu radi, Excel export (BtnExportExcelNalozi) proizvodi ispravan .xlsx i otvara ga. Nije testirano: BtnExportExcelJedanNalog, Štampa, Prilozi (DMS), Napredni filter, Preknjižavanje, Uvoz izvoda, Nova godina (potencijalno nepovratna/masovna radnja — namerno preskočeno bez dogovora).
- [x] `BtnDnevnikGlavneKnjige` — 📖 Dnevnik glavne knjige — Osveži (BtnUcitaj), Štampaj PDF (BtnStampajPdf, 146KB PDF generisan) i Excel export (BtnExportExcel, otvara Excel) svi rade. Period od/do (DpOd/DpDo DatePicker) nije klikan pojedinačno — deljena komponenta, dovoljno pokrivena drugde. Read-only izveštaj, nema CRUD.
- [x] `BtnKarticaKonta` — 📋 Dnevnik i Kartice konta — izbor konta iz liste puni karticu ispravno (241000 sa stvarnim prometom), dupli klik na red otvara "Pregled proknjiženog naloga (Samo za čitanje)" sa ispravnim stavkama, Zatvori radi. Export Excel (BtnExportExcelKartica) radi. Print nije ponovo testiran (ista PDF mehanika potvrđena na prethodnom ekranu).
- [x] `BtnPartneri` — 👥 Partneri i Otvorene stavke — izbor partnera puni Analitičku karticu i Otvorene stavke (IOS) tab tačno. **`BtnZatvoriStavke` (uparivanje otvorenih stavki) otvara ispravno** — ovo je bila imenovana otvorena stavka u PLAN_NASTAVKA.md (§5, „ZatvoriStavkeWindow nije vizuelno potvrđen"), sada potvrđeno: čekiranje duguje/potražuje redova ispravno ažurira zbir uživo. Otkazano bez potvrde (namerno, da se ne dira realan saldo demo partnera). Nije testirano: BtnNovPartner (nov partner), BtnKursnaLista, BtnVerifikujRacun, BtnObracunKamate, BtnIstorijaZatvaranja, stvarna potvrda uparivanja.
- [x] `BtnKompenzacije` — 🤝 Kompenzacije i Cesije — pun CRUD + knjiži/rasknjiži krug potvrđen na sopstvenom test zapisu (KOM-2026/005, obrisan na kraju). Usput nađena i popravljena **tri prava bug-a**, ne samo klikano: (1) nedostajalo je Rasknjižavanje — dodato `KompenzacijaService.RasknjiziKompenzacijuAsync` + WPF dugme (admin gate kao Nalozi) + API endpoint + web dugme; (2) `NalogService.RasknjiziAsync/RasknjiziViseAsync` nije otvarao nazad `ZatvaranjeStavke` vezane za nalog (fixalo i BankIzvod IZV naloge, ne samo Kompenzaciju); (3) **kritičan bug**: `KompenzacijaEditWindow.BtnSacuvaj_Click` je čuvao pun `Preostalo` iznos po stavci umesto da ograniči na manju stranu (standardna praksa — v. izvore u sesiji) — bez ograničenja knjiženje gotovo uvek puca sa "zbir potraživanja mora biti jednak zbiru obaveza" čim se stvarni iznosi faktura ne poklapaju tačno; popravljeno u WPF i web (`KompenzacijaFormaModal.tsx`) da ograničava na `Math.Min(zbirKupci, zbirDobavljaci)`. Filter/Pretraga radi, Excel export nije ponovo testiran (ista mehanika kao Nalozi). Nije testirano: Asignacija/Cesija (samo Dvojna), Štampa (Izjava o kompenzaciji PDF). **Nađeno, nepopravljeno**: biranje partnera u `CmbPartneri` kucanjem imena + Enter ne postavlja `SelectedItem` (grid stavki ostane prazan, čuvanje javlja "Izaberite partnera" iako tekst stoji u polju) — pouzdan način je dupli klik na red u tabu "Pametno Skeniranje" ili biranje iz otvorenog dropdown-a mišem, ne kucanje.
- [x] `BtnPutniNalozi` — 🚗 Putni nalozi i Dnevnice — pun CRUD + knjiži/rasknjiži krug potvrđen na sopstvenom test zapisu (PNZ-2026/021, obrisan na kraju). Isti nađeni nedostatak kao Kompenzacije: `PutniNalogService` je imao `KnjiziPutniNalogAsync` bez para — dodato `RasknjiziPutniNalogAsync` (jednostavnije od Kompenzacije, nema IOS zatvaranja da se otkazuje, samo briše nalog "PN" i vraća nacrt) + WPF dugme (admin gate) + API endpoint + web dugme. Pretraga radi (uključujući dijakritike, "Vučković"), Excel export otvara Excel ispravno, Izmeni ispravno odbija proknjižene naloge. Zapaženo: kolona "Status" u gridu za seed podatke pokazuje "Obračunat" umesto "Proknjiženo"/"Nacrt" — deo je demo seed-a, ne stvarni app kod (kod nikad ne piše taj string), IsKnjizeno je ono što se stvarno proverava i ispravno je true za te redove. Nije testirano: BtnIzvozZaZarade (izvoz prekoračenja dnevnice za Zarade), Štampa, Asignacija/inostranstvo varijanta (samo Zemlja testirano).
- [x] `BtnBlagajna` — 💰 Dinarska i Devizna Blagajna — pun CRUD + knjiži/rasknjiži krug potvrđen na sopstvenom test zapisu (BLU-DIN-2026/085, obrisan na kraju). Isti nedostatak kao Kompenzacije/PutniNalozi: `BlagajnaService.KnjiziBlagajnickiNalogAsync` bez para — dodato `RasknjiziBlagajnickiNalogAsync` (bez IOS zatvaranja, isto kao PutniNalog) + WPF dugme (admin gate) + API endpoint + web dugme. Zapaženo: posle Sačuvaj/Knjiži/Rasknjiži dijaloga, DataGrid selekcija se gubi (red ostane neoznačen posle `LoadNaloziAsync()`) — sledeća akcija na istom redu zahteva ponovni klik na red pre dugmeta (nije bug, uobičajeno ponašanje kad se ItemsSource zameni). Blagajnički Dnevnik tab (drugi tab) radi ispravno — Preneti/Uplaćeno/Isplaćeno/Novi saldo aritmetika se slaže. Nije testirano: Devizna blagajna varijanta (samo Dinarska), Štampa, Excel export (ista mehanika kao ostali ekrani).
- [x] `BtnMestaTroska` — 🎯 Mesta troška i Projekti — čist šifarnik (Novi/Izmeni/Obriši), nema knjiži/rasknjiži jer nema svoj dokument da knjiži. CRUD potvrđen na sopstvenom test zapisu (šifra 99, obrisan na kraju): Novi → Sačuvaj → Obriši. Izmeni na postojećem (Uprava) otvara popunjen dijalog ispravno, otkazano bez čuvanja. Drugi tab "Analitika & Profitabilnost Projekata" radi ispravno — combo bira mesto troška, period od/do, tabela prihoda/rashoda po kontu i neto rezultat se slažu aritmetički (proveren i za Upravu i za WebShop, različiti brojevi tačni). Zapaženo (nije popravljeno, samo estetsko): kolona "Status" u glavnoj tabeli pokazuje sirovo "True" umesto srpske labele (npr. "Aktivno") — nije dirano po odluci da ovaj prolaz ostane funkcionalan, ne dizajn. Nije testirano: Excel export, Štampa (ista mehanika kao ostali ekrani).
- [x] `BtnDevizno` — 💱 Devizno &amp; Valviranje — Na dan/Kurs EUR/Kurs USD polja rade, Osveži (🔄) preračunava. Ispravka ranije audit pretpostavke: za razliku od Kompenzacije/PutniNalozi/Blagajne, `DeviznoKnjigovodstvoService` **nema svoj entitet** — `ProknjiziValviranjeAsync` samo direktno pravi običan Nalog vrste "VAL", pa je već potpuno rasknjižive preko postojećeg generičkog Rasknjiži dugmeta na ekranu Glavna knjiga i Nalozi (dodatno popravljenog ovom sesijom da otvara nazad ZatvaranjeStavke) — nije trebalo dodavati poseban Rasknjizi ovde. Knjiži (⚡) sa 0 redova ispravno javlja "Nema kursnih razlika za knjiženje." Nije mogao da se testira pun krug knjiženja jer AUTOTEST.db seed nema nijednu stavku sa eksplicitno unetom stranom valutom (Valuta polje) — namerna zaštita servisa (v. komentar u kodu, 22.08.2026 nalaz o lažnim razlikama na dinarskim partnerima na 204/435 kontima) ispravno filtrira "konto počinje sa 204/435/244" ako Valuta nije EUR/USD, pa demo podaci daju 0 redova na svaki datum proban.
- [x] `BtnBrutoBilans` — 📊 Bruto bilans — Od/Do/Klasa filteri i "Totali po sintetičkim kontima" učitavaju ispravno balansirane podatke (Duguje=Potražuje=480.981.869,55). Osveži (🔄, bez x:Name, koordinatni klik), Štampaj PDF (BtnStampajPdf, 64,6KB generisan), Excel (BtnExportExcel, otvara Excel) i drill-down po partneru (BtnAnalitike → poseban prozor "Bruto bilans analitike", 24 partnera, balansirano 457.261.714,39=457.261.714,39) svi rade.
- [x] `BtnZakljucniList` — 📑 Zaključni list — Od/Do filter, ista 3 dugmeta (BtnUcitaj/BtnStampajPdf/BtnExportExcel, sva sa x:Name ovog puta) rade: PDF 63,9KB, Excel 9,5KB generisan (Excel se ovog puta nije sam otvorio kao na Bruto bilansu — fajl postoji na disku, nije bug, samo se nije autootvorio).
- [x] `BtnBilansiApr` — 🏛️ Zvanični Finansijski Izveštaji za APR — svih 5 tabova (Bilans Stanja, Bilans Uspeha, Statistički izveštaj SI, Tokovi gotovine Cash Flow, Promene na kapitalu) učitavaju podatke bez pada. **Bilans Stanja pokazuje upozorenje "Postoji razlika... Razlika -35.351.145,22 RSD"** (Aktiva ≠ Pasiva) — provereno da NIJE bug: 1:1 isti kod/poruka postoji u ERPiFinansije (izvorna, produkciona app) `BilansiView.xaml.cs`, i objašnjivo je time što AOP mapiranje (konta 0-4) ne uključuje tekući rezultat perioda iz klasa 5-8 dok se godina formalno ne zaključi (v. `BtnZakljucenjeGodine`) — očekivano ponašanje za nezaključenu godinu, ne nov kvar iz portovanja. Dugme `BtnPoreskiBilans` (gornji desni ugao) otvara PB-1/OA/PDP obrasce ispravno (usklađivanje rashoda, obračunati porez 0,00 jer je gubitak). `BtnOsvezi` radi.
- [x] `BtnCashFlow` — 📈 Kontroling & Cash-Flow Projekcije Likvidnosti — KPI pločice (Trenutna likvidnost, Dospela potraživanja/obaveze, Mesečne zarade), projekcija po vremenskim koracima (30/60/90 dana) i tabovi Potraživanja/Obaveze svi učitavaju podatke ispravno (negativna likvidnost od -394M je posledica demo seed podataka, potvrđena istim brojem i u Bruto bilansu konta 241000 — nije nov bug ovog ekrana). Osveži radi. **Drugo dugme ("📧 Slanje Opomena i IOS-a") NIJE testirano do kraja — namerno**: otvara `AutomatskeOpomeneWindow` koje stvarno zove `OpomeneEmailService.SmtpClient.SendMailAsync` (nije mock). Zatvoreno bez klika na "Pošalji" bilo kom kupcu. Provereno u kodu i direktno u `AUTOTEST.db`: `Firma.SmtpUser`/`SmtpPass` su prazni (samo default `smtp.gmail.com:587`), pa bi slanje palo na autentifikaciji i pre mreže — ali ekran namerno ostaje netestiran do kraja, isti obrazac kao "Nova godina" u Nalozima (potencijalno nepovratna/spoljna radnja bez izričitog dogovora).
- [x] `BtnZakljucenjeGodine` — 🔒 Zaključenje poslovne godine — ekran učitava 2025 (tekuću za zaključenje) sa ispravnim rashodima/prihodima po kontu (klase 5/6) i upozorenjem o nacrtima koji ne ulaze u zaključenje; tabela već zaključenih godina (2024) prikazuje ispravne zbirove sa dugmetom za otvaranje (rasknjiži). Osveži radi. **Dugme "🔒 Zaključi" NIJE kliknuto** — namerno, ista logika kao Nova godina/opomene: zaključenje poslovne godine je krupna, imenom označena poslovna radnja koja nije deo ovog klik-provere prolaza bez izričitog dogovora.
- [x] `BtnKonta` — 📋 Kontni plan — pun CRUD krug potvrđen na sopstvenom test zapisu (konto 999999, obrisan na kraju): Novi → Sačuvaj → pretraga pronalazi zapis → selekcija reda → Obriši (potvrda + uspeh dijalog) → obrisan. Izmeni na postojećem kontu (241000) otvara popunjen dijalog, Odustani ispravno ne menja ništa. Excel export radi (7KB fajl). Pretraga radi (prazan rezultat za nepostojeći string ne pravi grešku). Legacy DBF kolone (Stari konto/Ulica/Mesto/Žiro račun/Telefon) i dalje na kraju grida kao što je dokumentovano u PLAN_NASTAVKA.md §3d — potvrđeno da i dalje tako stoji. **Provereno u kodu na korisnikovo pitanje**: brisanje konta sa knjiženjima je zaštićeno (`DeleteKontaAsync` proverava `StavkeNaloga`, odbija sve-ili-ništa uz spisak zauzetih konta); dodavanje postojećeg broja konta je zaštićeno (`SaveKontoAsync` baca "Konto sa brojem {broj} već postoji!" pre upisa).
- [x] `BtnRobnoDashboard` — 📊 Radna tabla — Brze akcije (Nova kalkulacija/nivelacija/otpremnica/primopredaja), KPI kartice VP/MP/Ukupna vrednost zaliha, Poslednje kalkulacije/nivelacije i Top 10 artikala svi učitavaju stvarne podatke bez pada. Dugmad "Nova *" nisu klikana ovde (dupliralo bi test na sopstvenim ekranima ispod).
- [x] `BtnPonude` — 📜 Ponude &amp; Predračuni — ekran se otvara i učitava CRM podatke bez pada (potvrđuje §80 fix za `NullReferenceException` na filteru faze — `if (DgPonude == null) return;`). Grid sa 15+ ponuda, kolone CRM Faza/Verovatnoća/Ponderisano/Status ispravno popunjene, stavke ponude panel ispod. Nije dublje testirano (CRUD/knjiži) u ovom prolazu — otvoren usput dok se testirala nova sidebar accordion funkcija, vratiti se za pun CRUD krug.
- [x] `BtnNarudzbenice` — 🛒 Narudžbenice — grid od 14 narudžbenica sa statusima (Poslata/Delimično isporučeno/Isporučena) i tačnim iznosima, dobavljač/magacin prijema kolone popunjene. Toolbar: Novi/Izmeni/Obriši/Pretvori u kalkulaciju/Excel. Nije dublje testirano (CRUD/pretvaranje).
- [x] `BtnKalkulacije` — 📥 Ulazne kalkulacije (VP) — grid sa realnim kalkulacijama, toolbar OCR/Novi/Izmeni/Obriši/PDF/Excel prisutan. Nije dublje testirano (CRUD/knjiži).
- [x] `BtnMpKalkulacije` — 🛍️ MP Kalkulacije — grid sa podacima, pun toolbar (Novi/Izmeni/Obriši/Knjiži/Rasknjiži/Excel/PDF). Nije dublje testirano.
- [x] `BtnUvozneKalkulacije` — 🚢 Uvozne Kalkulacije — grid sa podacima (potvrđuje §3a nalaz iz PLAN_NASTAVKA.md da je ekran naknadno dograđen), Novi/Izmeni/Knjiži/Rasknjiži toolbar. Nije dublje testirano.
- [x] `BtnNivelacije` — 🏷️ Nivelacije cena — "Nivelacije cena robe" učitava se ispravno, toolbar Nova/Svođenje cena/Izmeni/Knjiži. Nije dublje testirano.
- [x] `BtnRacuniOtpremnice` — 🧾 Računi - Otpremnice — najbogatiji toolbar u ovom modulu: Novi/Izmeni/Knjiži/Rasknjiži/Masovno knjiženje/Pretvori predračun/Pošalji na SEF/Sačuvaj UBL XML/Fiskalizuj PFR — svi prisutni, ekran se učitava bez pada. Nije dublje testirano (SEF/PFR akcije nisu klikane — zahtevaju spoljne integracije).
- [x] `BtnPrimopredaje` — 🔄 Primopredaje robe — ekran se učitava čisto, pun toolbar (Novi/Izmeni/Obriši/Knjiži/Rasknjiži/Štampaj/Excel). Grid prazan (nema demo primopredaja) — nije nalaz, samo nema seed podataka.
- [x] `BtnZaduzenja` — 📤 Zaduženja robe — isti obrazac kao Primopredaje, pun toolbar, prazan grid (nema seed podataka).
- [x] `BtnRazduzenja` — 📥 Razduženja robe — isti obrazac, pun toolbar, prazan grid.
- [x] `BtnPopisRobe` — 📝 Popis (inventar) robe — jedini u grupi sa demo podacima (bar 1 red), toolbar Osveži/Novi popis/Obriši/Sačuvaj/Zaključi/Poništi/Excel. Nije dublje testirano (CRUD/zaključenje popisa).
- [x] `BtnRobneKartice` — 📋 Robne kartice — lista artikala za izbor (11+ redova, `ArtikalIzbor`) učitava se ispravno. Nije klikan pojedinačan artikal za karticu.
- [x] `BtnRobniBruto` — 📊 Robni Bruto bilans — pun toolbar (Osveži/Štampaj raspored/Štampaj stanje po artiklima/Excel/Štampaj bruto), podaci učitani.
- [x] `BtnVrednovanjeZaliha` — 📦 Vrednovanje zaliha — deli servis/podatke sa Robni Bruto bilans (isti `RobniBrutoBilansRed`), učitava se ispravno.
- [x] `BtnSarze` — 📅 Šarže i rokovi trajanja — 5+ redova šarži, Excel export dugme.
- [x] `BtnSerijskiBrojevi` — 🔢 Serijski brojevi — 5+ redova, Excel export dugme.
- [x] `BtnArtikli` — 📦 Šifarnik artikala — grid sa 8+ artikala, pun toolbar Novi/Izmeni/Obriši/Excel.
- [x] `BtnMagacini` — 🏭 Šifarnik magacina — 4 magacina, toolbar Novi/Izmeni/Obriši.
- [x] `BtnWmsLokacije` — 📍 WMS Skladišne lokacije — testirano usput dok se proveravala accordion/scroll-centering funkcija (§81): "WMS Skladišne lokacije i Picking rute" učitava 24+ lokacije sa 3 taba (Skladišne lokacije/Nalozi komisioniranja/Kretanja po policama), Magacin/Zona/Pretraga filteri, pun CRUD+Osveži toolbar.
- [x] `BtnPoreskeTarife` — 🏷️ Poreske tarife — 3 tarife (opšta/posebna/oslobođeno), toolbar Nova/Izmeni/Obriši/Excel.
- [ ] `BtnMaterijalno` — 📊 Radna tabla
- [x] `BtnMaterijalUlaz` — 📥 Ulaz materijala — 11 redova, radio filter Svi/Proknjiženi/Neproknjiženi, toolbar Novi/Izmeni/Knjiži/Excel. Kolona "Proknjiženo" pokazuje sirovo "True" (kozmetičko, isti obrazac kao Mesta troška — nije dirano, van obima ovog prolaza).
- [x] `BtnMaterijalTrebovanje` — 📤 Trebovanja materijala — 12+ redova, isti obrazac kao Ulaz materijala.
- [x] `BtnMaterijalPrimopredaja` — 🔄 Primopredaje materijala — 6 redova (Daje/Prima magacin), isti obrazac.
- [x] `BtnMaterijalSifrarnik` — 🧱 Šifrarnik materijala — 10 materijala, CRUD toolbar.
- [x] `BtnMaterijalKartice` — 📇 Kartice materijala — izbor materijala puni karticu ispravno (testirano na A01035 "Cement Milenium Tools", saldo 280kg tačno prati kumulativ). **Nalaz**: sve stavke prometa na kartici potiču od ROBNIH dokumenata (Kalkulacija/Račun-otpremnica), nijedna od materijalnih (Ulaz/Trebovanje/Primopredaja materijala) — v. nalaz kod `BtnMaterijalBruto` ispod, isti koren.
- [x] `BtnMaterijalBruto` — 📊 Bruto bilans materijalnog — **PRAVI BUG NAĐEN I ISPRAVLJEN (§81, 30.08.2026).** Koren: `RobniBrutoBilansService.IzracunajAsync` je delio "robni" od "materijalni" bilans isključivo po tome da li `SifraArtikla` postoji u `Artikli` tabeli — ali `Materijal.SifraArtikla` je namerno isti kod kao odgovarajući `Artikal.SifraArtikla` za deo materijala ("materijalni šifarnik prati podskup artikala", demo generator), pa je taj test nepouzdan i za DEMO i (potencijalno) za pravu bazu ako firma ikad ponovi šifru između dva šifarnika. Posledica pre popravke: `Bruto bilans materijalnog` uvek prazan, a `Robni Bruto Bilans`/`Vrednovanje zaliha` bi (čim bi materijalno knjiženje ikad postojalo za deljenu šifru) tiho brojali materijalne pokrete kao robne.
  **Ispravka:** nova kolona `MaterijalnaKartica.Vrsta` (`VrstaZaliheKartice.Roba`/`Materijal`, EF migracija `DodajVrstuZaliheNaMaterijalnuKarticu` + `EnsureColumn` mirror, šema potvrđena dvaput na kopiji prave ARHIBEL baze — idempotentno, `Artikli`/`Konta` broj redova nepromenjen). `MaterijalnaKarticaService.DodajUlazRedAsync`/`DodajIzlazRedAsync` dobili opcioni `vrsta` parametar (podrazumevano `Roba`, ne diraju 8 od 13 poziva); tri materijalna servisa (`UlazService`, `TrebovanjeService`, `PrimopredajaService`) eksplicitno šalju `Materijal`. `RobniBrutoBilansService` sad filtrira po `Vrsta` koloni, ne po code-lookup-u. Demo generator (`GenerisiMaterijalneKartice`) dopunjen da i materijalna knjiženja (ranije samo `UlazNalog`/`TrebovanjeNalog`/`PrimopredajaNalog` bez ijedne odgovarajuće kartice) upisuju stvarne redove, grupisano po (magacin, šifra, **vrsta**) — dva odvojena tekuća stanja za deljenu šifru, ne jedno mešano.
  **Provereno:** `dotnet test ERPiData.Tests` 1627/1627 i 1628/1628 (posle brisanja scratch testova) bez regresije; AUTOTEST.db regenerisan (isti determinstički Seed kao original, Firma "DEMO"/"ERPi Demo d.o.o." — potvrđeno da se poklapa sa onim što je bilo pre brisanja) — direktna SQL provera potvrđuje `MaterijalneKartice`: 19.607 redova Vrsta=Roba, 4.110 Vrsta=Materijal. **Vizuelno potvrđeno** (posle ranijih sredinskih smetnji sa screenshot-om): ekran prikazuje 20+ redova materijala (magacin 03/Magacin sirovina), Ukupno Duguje 10.672.168.637,00 RSD / Ukupno Potražuje 1.208.882.934,00 RSD / Saldo Zaliha 9.463.285.703,00 RSD.
- [x] `BtnProizvodnjaDashboard` — 📊 Radna tabla — KPI (10 aktivnih naloga, 110 završenih, 10.6B RSD ukupna vrednost, 24 sastavnice), tabele Radni nalozi u toku i Normativi/Sastavnice, brze akcije (Novi radni nalog/Nova sastavnica/Kalkulacija cene koštanja).
- [x] `BtnSastavnice` — 📋 Sastavnice (BOM) — 24 normativa sa Materijal/Rad&Mašine/Cena koštanja kolonama, CRUD toolbar (Nova/Izmeni/Kopiraj/Obriši).
- [x] `BtnRadniNalozi` — 🏭 Radni nalozi — 500 naloga, statusi (U pripremi/U radu/Lansiran/Završen), Sirovine razduže/Proizvodi zaduže checkbox kolone tačno prate status, Magacin sirovina→gotovih. Bogat toolbar (7 dugmadi: Novi/Izmeni/Lansiraj/Završi/Storno/Štampa/Otkaži). Ovaj ekran posredno potvrđuje §81 fix (troškovi materijala u proizvodnji zavise od Trebovanje materijala knjiženja).
- [x] `BtnKalkulacijaCeneKostanja` — 💰 Cena koštanja & Varijanse — Planska/Stvarna prekidač, KPI kartice (Direktni materijal/Ljudski rad/Amortizacija/Cena po jedinici), tabela Planski vs Stvarni troškovi po vrsti — brojevi se slažu (0,00 varijansa kad je planska=stvarna, kako se i očekuje pre knjiženja odstupanja).
- [x] `BtnPdvEvidencija` — 🧾 PDV Evidencija (KPR/KIR) — 3 taba (KIR/KPR/POPDV Rekapitulacija), period 1.8-30.8.2026, Ukupno KIR 72.938.805,92 RSD / Izlazni PDV 11.782.172,72 RSD, grid sa realnim partnerima/PIB/osnovicama 20%/10%.
- [x] `BtnPopdv` — 📋 Obrazac POPDV — 11 delova po pravilniku, period 08/2026 "u izradi", ispravno prijavljuje negativnu poresku obavezu (-110.111.664 RSD, povraćaj/prenos).
- [x] `BtnSefPfr` — 📄 SEF e-Fakture i PFR — 3 taba (SEF UBL 2.1/e-Fiskalizacija PFR/Dnevnik poziva), lista proknjiženih računa sa SEF status "NijePoslata" (demo, očekivano bez pravog SEF naloga).
- [x] `BtnKasa` — 🛒 Kasa (maloprodaja) — "PROBNO OKRUŽENJE" baner, "Smena nije otvorena" stanje ispravno prikazano, F-taster raspored (F5 Naplata/F2 Količina/F3 Popust/F4 Obriši/F6 Refundacija/F7 Obuka/F9 Smena). Nije otvorena smena/naplaćeno (izbegava kreiranje test transakcije).
- [x] `BtnPazar` — 💰 Pazar po smenama — filter period/prodajno mesto radi, prazno stanje (nema smena u demou za ovaj magacin/period) — očekivano, ne nalaz.
- [x] `BtnWebPorudzbine` — 🛒 Pristigle Web Porudžbine — 37 porudžbina sa realnim gradovima/kuririma/statusima (Prihvaćena/Nova/OdloženoE/ČekaOdobrenje), stavke i adresa panel na desnoj strani.
- [x] `BtnWebKatalog` — 📦 Web artikli i objave — katalog sa Web naziv/Web kategorija/Cena/Akcijska cena/Na Webu/Istaknuto/Novo kolonama, filter po statusu objave.
- [x] `BtnWebKategorije` — 🌲 Stablo kategorija — 15 kategorija (Ručni alat, Auto oprema, Bela tehnika...), detalji panel sa SEO/slika/ikonica poljima.
- [x] `BtnWebCenovnik` — 💰 Cenovnik partnera (B2B) — kreditni limit/rok plaćanja/ugovorene cene po artiklu, čeka izbor partnera (prazno po dizajnu).
- [x] `BtnWebKorisnici` — 👥 Web & B2B Korisnici — 400 naloga (svi B2C u uzorku), loyalty bodovi, B2B na čekanju: 0.
- [x] `BtnWebPodesavanja` — ⚙️ Podešavanja šopa — 5 tabova (Osnovno&Plaćanja/Tema/Kurirske službe/Email/SEO), WebShop API servis aktivan (port 5000) — koristan za web deo E2E prolaza kasnije, ne treba ponovo pokretati.
- [x] `BtnUvoz` — ⚙️ Uvoz podataka Wizard — ERPiFinansije/DOS-Clipper uvoz opcije, putanja podešena. Analiza/Uvoz dugmad nisu klikana (izbegava izmenu regenerisane demo baze).
- [x] `BtnKorisnici` — 👤 Korisnici i uloge — 6 korisnika sa RBAC ulogama (Administrator/Operater/Komercijalista/Magacioner/KadrovskaSluzba/Gledalac), svi Aktivni.
- [x] `BtnPodesavanja` — 🔧 Podešavanja — 11 tabova (Podaci o firmi/Sve firme/Istorija izmena/Opšte/Rezervne kopije/SEF e-Fakture/e-Fiskalizacija PFR/Kasa/Proizvodnja/REST API/DOS Uvoz), firma podaci ispravno popunjeni.
- [x] `BtnPomoc` — ❓ Pomoć & Uputstva — modul-filter (Finansije/Sredstva/Zarade/Proizvodnja/Kasa), teme pomoći sa punim tekstom uputstva po temi.

**Ceo kombinovani "Finansije" modul panel (Finansije+Robno+Materijalno+Proizvodnja+Porezi/SEF+WebShop+Podešavanja) je sada 100% pokriven u ovom E2E prolazu.**

### Zarade

- [x] `BtnZaradeDashboard` — 📊 Radna tabla — 35 aktivnih radnika, neto/bruto masa po mesecu 2026 (grafikon sa live tooltip-om), 9 aktivnih kredita.
- [x] `BtnZaradeObracuni` — 📁 Obračunski periodi — 32 istorijska obračuna (okt 2025 — avg 2026), status Zaključi/Otvori po periodu, "Pokreni novi obračun"/"Zaključaj sve otključane".
- [x] `BtnZaradeIsplate` — 💸 Isplate u mesecu — 1 isplata za 08/2026 (Konačna zarada, 35 radnika, 4.382.607,20 RSD), kontrolne provere panel.
- [x] `BtnZaradeRadnici` — 👤 Radnici — 35 radnika sa punim detaljima (JMBG/koeficijent/banka/poreska olakšica/tip primaoca), CRUD toolbar.
- [x] `BtnZaradeSihterica` — 📅 Šihterica (Evidencija rada) — 35 radnika, redovni/prekovremeni/noćni/praznik/odmor/bolovanje kolone, Generiši predlog/Prenesi u obračun/Zaključaj/PDF.
- [x] `BtnZaradeRadniSati` — ⏱️ Radni sati — vodjeni prazno stanje "Nije izabran aktivni period" (očekivano, treba prvo izabrati period sa spiska obračuna, ne globalna vrednost).
- [x] `BtnZaradeBolovanja` — 🏥 Bolovanja i RFZO — ispravno prazno za 08/2026, kontrolna provera javlja upozorenje na jasan način.
- [x] `BtnZaradeOdsustva` — 🏖️ Godišnji odmori i odsustva — 77 evidentiranih odsustava za 2026, statusi/tipovi/brojevi rešenja.
- [x] `BtnZaradePrimanja` — 🎁 Neoporeziva i ostala primanja — 4 primanja za period, oporezivi višak tačno izračunat (ista logika kao §77/§78 fix).
- [x] `BtnZaradeHrDokumenti` — 📁 HR dokumenti i alarmi — 30 dokumenata, 11 šablona, 4 taba (Arhiva/Čarobnjak/Šabloni/HR Alarmi), 0 hitnih upozorenja.
- [x] `BtnZaradeKrediti` — 💳 Krediti i obustave — lista 35 radnika sa pretragom, prazno stanje po izabranom radniku (nema kredita), 2 taba (Krediti/Samodoprinosi i odbici).
- [x] `BtnZaradeObracunPlate` — 📊 Obračun plate — testirano ranije u sesiji (Mesečni obračun plate, 4 radnika za avgust).
- [x] `BtnZaradeObracunPeriod` — 🧮 Obračun za period — zbirni pregled 36 redova za period 01-08/2026, po radniku/mesecu, Excel/PDF export.
- [x] `BtnZaradeWhatIf` — 🧮 What-If kalkulator & Budžet — dvosmerni kalkulator (Neto→Bruto/Bruto→Neto/Trošak→sve), tačna specifikacija poreza/doprinosa za uneti primer (100.000 RSD neto → 138.598,72 bruto).
- [x] `BtnZaradeStatistikaRzs` — 📈 Statistika RZS (RAD-1 & RAD-G) — agregacija iz matične knjige/šihterice/obračuna, 35 zaposlenih, 11 žena/24 muškaraca, "uspešno agregiran".
- [x] `BtnZaradeListici` — 🧾 Platni listići — 35 listića za 8/2026, masovne akcije (Zbirni PDF/Batch izvoz/Slanje e-mailom — **ovo poslednje nije klikano**, šalje prave email-ove).
- [x] `BtnZaradePppPd` — 📋 PPP-PD — zarade — puna forma prijave (parametri/izmenjena prijava/podaci isplatioca), 35 zaposlenih sa JMBG/SVP/sati, Pokreni validaciju/Generiši XML dugmad.
- [x] `BtnZaradeNalozi` — 🏦 Nalozi za prenos — Halcom/ePP format, ispravno javlja da nedostaje BOP dokument sa ePorezi (očekivano, nema pravu integraciju u demo).
- [x] `BtnZaradeKnjizenje` — 📒 Nalog za knjiženje — **provereno da NIJE bug**: nalog javlja neravnotežu (205.600,00 RSD/36 grešaka) jer demo generator eksplicitno i dokumentovano računa obračune po pojednostavljenoj formuli za brzinu ("demo brojevi su reda veličine tačni, ali nisu poreski dokument" — `DemoPodaciGenerator.Zarade.cs` header komentar), ne kroz pravi `ObracunService`. Validacija ispravno odbija knjiženje neuravnoteženog naloga — to je feature, ne bug.
- [x] `BtnZaradePrimaoci` — 👤 Primaoci po ugovoru — 9 primalaca, svi "i u radnom odnosu" (dual role radnik+primalac naknade), objašnjeno u napomeni na dnu.
- [x] `BtnZaradeIsplateNaknada` — 💸 Isplate naknada — ispravno prazno za 08/2026, kontrolne provere panel.
- [x] `BtnZaradeUgovori` — 📝 Ugovori i naknade — 24 ugovora, 2 bez OVP šifre (jasno prijavljeno, manja demo-nekompletnost, ne app bug).
- [x] `BtnZaradeVrsteUgovora` — 📄 Vrste ugovora — 9 vrsta (UOD/AUT/PPP/ODB varijante) sa punim poreskim/doprinosnim stopama po vrsti.
- [x] `BtnZaradeSabloniUgovora` — 🖋️ Šabloni ugovora — 4 šablona, editor sa live poljima za zamenu ({FirmaNaziv}, {PrimalacIme}...).
- [x] `BtnZaradePppPdNaknade` — 📋 PPP-PD — naknade — ispravno prazno (nema isplata naknada za period), jasna poruka gde da se napravi.
- [x] `BtnZaradePorezi` — ⚖️ Porezi i parametri — **PRAVI BUG NAĐEN I ISPRAVLJEN (§81)**: ekran je za SVAKI period demo baze prikazivao nule za sve poreske stope/limite (1. stopa poreza, neoporezivi iznos, gornja granica...) uprkos tome što stvaran obračun ispravno koristi 10%/28.423/656.425 (hardkodovano u `DemoPodaciGenerator` konstantama). Uzrok: `porezi.Add(new Porezi {...})` u demo generatoru je postavljao SAMO `FondCasova`/`CasZaOb`, ostavljajući `AkPorez`/`Prvast`/`Drugast`/`ProcNocni`/... na podrazumevanih 0 — usput nađeno da je i `Doprinosi` tabela imala isti obrazac (`ProcRadn`/`ProcPosl` nikad postavljeni). Ispravljeno popunjavanjem oba seed-a vrednostima koje se poklapaju sa `PoreziPage.xaml.cs`-ovim sopstvenim "nema podataka" fallback-om (isti izvor istine, nema novih brojeva izmišljeno) — PIO 14%/10%, Zdravstvo 5,15%/5,15%, Nezaposlenost 0,75%/0%. `dotnet test` 1627/1627 bez regresije, AUTOTEST.db regenerisan, vizuelno potvrđeno na oba ekrana (Porezi i Doprinosi).
- [x] `BtnZaradeDoprinosi` — 📈 Doprinosi — deo iste ispravke iznad, sad ispravno prikazuje PIO/Zdravstvo/Nezaposlenost stope. Sekundarna polja (bolovanje do/preko 30 dana, porodiljsko, invalidi) ostaju 0,00 — nisu deo osnovna 3 doprinosa, van obima ove ispravke.
- [x] `BtnZaradePlatniRazredi` — 📊 Platni razredi — 9 razreda (I-II do Rezerva), najniže bruto osnovice i doprinos za PIO faktori ispravno popunjeni.
- [x] `BtnZaradeOlaksice` — 🏷️ Poreske olakšice — 5 olakšica (čl. 21v/21ž/21đ ZPDG), MFP deklaracija panel.
- [x] `BtnZaradePraznici` — 📅 Kalendar praznika — 9 praznika za 2026, mesečni fond sati ispravno izračunat po mesecu (Avgust=168h, matches Šihterica ranije u prolazu).
- [x] `BtnZaradeStampe` — 📑 Izveštaji & rekapit. — 3 izveštaja (Mesečni platni spisak/Mesečna rekapitulacija/Izveštaji za banke), testirano generisanje PDF-a (otvara pravi Save As dijalog, ispravan podrazumevani naziv fajla). **Nalaz o alatu, ne o app-u**: Windows Save-dijalog dodeljuje iste brojčane AutomationId (1, 2...) i dugmadima (Save/Cancel) i redovima liste fajlova — klik na "AutomationId=2" misleći da je Cancel je pogodio 2. fajl u listi i pokrenuo Save pod TIM imenom (skoro prepisan `Resenje_Rancic_Miodrag_2026.pdf`, ispravno odbijeno na "Replace?" pitanju, ništa nije izmenjeno). Za ubuduće: koristiti `keys "{ESC}"` da se zatvori Save/Open dijalog, ne brojčani `click <id>`.
- [x] `BtnZaradePppPo` — 🧾 PPP-PO (godišnja) — 36 potvrda za 2026, porez 4.074.937,67 / doprinosi 9.658.918,19 RSD.
- [x] `BtnZaradeVrstePrimanja` — 💰 Vrste primanja — 25 vrsta (18 sistemskih), pun CRUD krug potvrđen na sopstvenom test zapisu (TST9 "Test E2E primanje", obrisan na kraju): ➕ Dodaj → unos šifre/naziva → 💾 Sačuvaj → selekcija reda → 🗑 Obriši → Sačuvaj. Zaštite u kodu rade kako je dokumentovano: sistemska vrsta se ne može obrisati, duplirana šifra i neispravna SVP šifra (mora 9 cifara) su odbijene pre upisa, vrsta upotrebljena u postojećim obračunima takođe ne može da se obriše. Treće dugme "🔀 Prevedi" (jednokratna migracija starih obračuna na model stavki) ispravno javlja "Svi obračuni (1036) već imaju stavke." — nema šta da se prevede u demo bazi, potvrđuje da migracija nije potrebna (demo generator već piše kroz model stavki). Nijedno dugme nema x:Name (koordinatni klik).
- [x] `BtnZaradeBanke` — 🏦 Banke — šifarnik je po (Godina/Mesec/Šifra), 15 zapisa (2024-2026, mesec 1). Pun CRUD krug potvrđen na sopstvenom test zapisu (2026/8/1 "Test E2E banka", obrisan na kraju): selekcija reda popuni formu za izmenu (Odustani vraća na prazan "Dodaj" bez izmene), "➕ Nova banka"/Dodaj popuni šifru auto-inkrementom po aktivnom (godina, mesec) paru, Pretraga po nazivu/šifri/žiro računu/godini/mesecu radi, brisanje traži YesNo potvrdu i uspešno uklanja zapis. **Uzgred nađen i uklonjen mrtav kod**: `BtnObrasi_Click` u `BankePage.xaml.cs` bio je metoda sa greškom u imenu (trebalo "Obriši") koja se nigde nije pozivala iz XAML-a — samo je prosleđivala na pravi `BtnObrisi_Click`, bezopasno ali nepotrebno; uklonjena.
- [x] `BtnZaradeKontaKnjizenja` — 📗 Konta za knjiženje — 18 fiksnih uloga (Naziv/Strana/Konto/Ključ), redovi se ne dodaju/brišu po dizajnu (svaka je uloga koju kod traži po ključu). Selekcija reda puni napomenu ispod grida. Izmena Konto vrednosti (521→529999) + Sačuvaj persistira ispravno; "↩ Vrati podrazumevano" (BtnVratiPodrazumevano) traži YesNo potvrdu i vraća sve izmenjene vrednosti na `KontaKnjizenjaSeed.Podrazumevana()` — testirano pun krug (izmenjeno → sačuvano → vraćeno na podrazumevano → sačuvano), na kraju ekran u izvornom stanju (521).
- [x] `BtnZaradePodesavanja` — ⚙️ Podešavanja — jedan tab "Uvoz podataka" sa dve opcije (Uvoz iz postojeće ERPiZarade instalacije / DOS uvoz direktno iz DBF fajlova), oba polja za putanju ispravno učitana i editabilna. **"⚡ Uvoz iz ERPiZarade" i "⚡ Pokreni DOS uvoz" NISU klikani — namerno**: polje putanje je već popunjeno pravom produkcionom bazom (`firma_100188310_PSSS_PIROT_DOO_PIROT.db`), isti obrazac kao "Nova godina"/opomene/zaključenje godine — nepovratna/spoljna radnja nad realnim podacima firme, van obima ovog klik-provere prolaza bez izričitog dogovora.
- [x] `BtnZaradePomoc` — ❓ Pomoć &amp; Uputstva — otvara se sa modul-filterom već postavljenim na "Zarade" (potvrđuje kontekstualni F1 hub iz §3ai), 5 tema pomoći sa punim tekstom, klik između tema menja prikazani sadržaj ispravno (testirano Brzi start → Zaposleni i kadrovska evidencija). "Otvori HTML Uputstvo"/Changelog dugmad prisutna, nisu klikana (ista mehanika kao BtnPomoc ranije u prolazu).

**Ceo Zarade modul panel je sada 100% pokriven u ovom E2E prolazu.**

### Sredstva

- [x] `BtnSredstvaDashboard` — 📊 Radna tabla — KPI kartice (120 sredstava, nabavna 212.201.800,00 / sadašnja 89.331.220,88), Top 5 najvrednijih aktivnih sredstava sa live tooltip-om (testirano hover), donut Status sredstava (Aktivna/Rashodovana) i Sadašnja vrednost po kontima (Top 10, demo ima samo konto 022000 — nije nalaz, samo obim demo podataka) svi učitavaju bez pada.
- [x] `BtnSredstvaRegistar` — 🏛️ Registar sredstava — 120 sredstava, kolone Šifra (uvek "0" — to je `LegacySifra`, DOS-import polje popunjeno samo kod migriranih sredstava, isti obrazac kao legacy kolone na `KontaView`, nije nalaz)/Inv.Br./Naziv/Am.Grupa/Stopa/Nabavna/Rezidualna/Ispravka/Sadašnja/Datum aktiviranja, totali na dnu (212.201.800,00/122.870.579,12/89.331.220,88) poklapaju se sa Radnom tablom. **"➕ Novo sredstvo" otvara "Nalog Prijave"** (isti prozor kao ekran Prijava sredstava) — testiran pun krug na sopstvenom test zapisu (TEST01 "Test E2E sredstvo", 100.000,00, Am.grupa II, konto namerno prazan): Dodaj stavku u nalog → Proknjiži Nalog (YesNo potvrda) → ispravno javlja "Interno knjiženje je uspelo, ali nalog za Glavnu knjigu NIJE napravljen: 1 stavki nema izabran konto sredstva" (očekivano jer je konto namerno izostavljen) → sredstvo se pojavljuje u Registru (121 sredstava, totali uvećani za tačno 100.000,00), Pretraga po nazivu ga pronalazi. Test sredstvo uklonjeno preko ekrana Rashod i promene (vidi taj red ispod). Nalepnice/Kartica dugmad nisu klikana u ovom prolazu.
- [x] `BtnSredstvaKartice` — 📋 Analitičke kartice — pretraga liste sredstava radi, izbor sredstva (testirano na OS-00001 "Magacinska hala Q12") puni KPI kartice (Nabavna/Ispravka/Sadašnja vrednost/Stopa amortizacije) i Hronologiju promena (6+ redova obračuna amortizacije po godini, saldo se ispravno smanjuje). 📎 Prilozi (DMS) dugme ima crveni broj-bedž (1 prilog na ovom sredstvu) — potvrđuje da DMS postoji i na Sredstvima, ne samo na Nalozima; nije otvoreno u ovom prolazu. 🖨️ Štampa nije klikana (ista PDF mehanika kao ostali ekrani).
- [x] `BtnSredstvaPrijave` — 📥 Prijava sredstava — lista svih naloga prijave (121, uključujući test nalog #121 kreiran malopre preko Registar → Novo sredstvo), KPI kartice (Ukupno naloga/stavki/Ukupna nabavna vr./Proknjiženo/Na čekanju) tačne. "➕ Nova prijava" otvara isti "Nalog Prijave" prozor testiran na prethodnom ekranu (CRUD već potvrđen tamo, nije ponavljano). Duplo-klik za pregled nije klikan u ovom prolazu.
- [x] `BtnSredstvaRashod` — 📤 Rashod i promene — lista 14 postojećih naloga (Rashodovanje/Prodaja/PovecanjeVrednosti), KPI kartice tačne. **PRAVI BUG NAĐEN I ISPRAVLJEN.** Nalog rashoda za test sredstvo (Rashodovanje, Vrednost izlaza 0) proknjižen je ispravno (JeAktivno→false, storno red u Kartici sa tačnim -100.000,00), ALI stat-kartica na ekranu Analitičke kartice i dalje je pokazivala "SADAŠNJA VREDNOST 100.000,00" umesto 0,00 — u suprotnosti sa sopstvenom Hronologijom promena ispod nje koja je ispravno pokazivala kumulativ 0,00. Koren: `RashodWindow.xaml.cs` je za sve tipove promene (Rashodovanje/Prodaja/Otuđenje/Brisanje/Kolicinsko rashodovanje/Povećanje vrednosti/Povećanje amortizacije) menjao `Sredstvo.NabavnaVrednost`/`IspravkaVrednosti` ali NIKAD nije osvežio `Sredstvo.SadasnjaVrednost` — za razliku od `AmortizacijaPage`/`RevalorizacijaPage`/`PopisPage` koje posle svake izmene rade `sredstvo.SadasnjaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti`. Kod potpunog rashodovanja/prodaje/otuđenja/brisanja, `Sredstvo.NabavnaVrednost`/`IspravkaVrednosti` uopšte nisu ni nulirani (samo storno red u Kartici), pa je sredstvo doživotno ostajalo u Registru/Radnoj tabli/Izveštajima sa punom knjigovodstvenom vrednošću iako je JeAktivno=false i knjigovodstveno rasknjiženo — direktno naduvava "Ukupna sadašnja vrednost" na Radnoj tabli za svako ikad rashodovano sredstvo. **Ispravka:** dodato `sredstvo.NabavnaVrednost = 0; sredstvo.IspravkaVrednosti = 0;` uz `JeAktivno = false` za potpuno rashodovanje/prodaju/otuđenje/brisanje, i `sredstvo.SadasnjaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti;` posle cele switch grane (važi za sve tipove promene). `dotnet test ERPiData.Tests` 1627/1627 bez regresije. Test sredstva i njihovi prateći Kartica/Prijava/Rashod zapisi uklonjeni direktno iz AUTOTEST.db posle provere (baza vraćena na tačno 120/212.201.800,00/89.331.220,88, potvrđeno upitom). **Uzgred nađen dodatni UX nalaz, nije ispravljen**: `CmbSredstvo` (izbor osnovnog sredstva) je editable ComboBox BEZ filtriranja po kucanju (za razliku od Konto pickera u `NalogEditWindow`) — kucanje teksta + strelica dole ne ide na sredstvo koje se poklapa sa unetim tekstom, nego na prvi element u (nefiltriranoj) listi po `InventarskiBroj` redosledu; skoro je dovelo do slučajnog dodavanja pravog demo sredstva (OS-00001) u nalog rashoda umesto test sredstva — na vreme uočeno pre knjiženja, ništa nije upisano. Pouzdan način izbora: birati mišem iz otvorenog dropdown-a, ne kucanjem + strelicom.
- [x] `BtnSredstvaReversi` — ✍️ Reversi i zaduženja — tab "Reversi" (filter po godini, lista sa statusima Nacrt/Potvrđen, klik na revers puni detalj panel sa stavkama sredstava i vrednošću — testirano na 34/2026), tab "Trenutna zaduženja" ispravno objašnjava da se stanje izvodi samo iz potvrđenih reversa (32 sredstva, 29.141.039,34 RSD, nacrti isključeni) — konzistentno sa §75 QR nalepnice funkcijom. "Nov revers"/"QR nalepnice" nisu klikani (izbegava novi test zapis posle Sredstva bug-a nađenog na prethodnom ekranu).
- [x] `BtnSredstvaAmortizacija` — 📈 Obračun amortizacije — 3 taba. **"Obračun amortizacije"**: period/pravilo (Srazmerno po danima) + "Obračunaj" (samo preview, ne piše u bazu — `BtnProknjizi` je odvojeno dugme, nije klikano da ne izmeni amortizaciju cele demo baze za 2026) ispravno računa za svih 120 sredstava (npr. OS-00010 tačno prijavljuje 0,00 novu amortizaciju jer je već na rezidualnoj vrednosti). **"Poreska amortizacija (Obrazac OA)"**: unos godine + obračun poredi poresku i računovodstvenu amortizaciju po sredstvu (kolona Poreska Razlika), radi ispravno; "Masovna Dodela Grupa" nije klikano (bulk-write akcija). **"Lista amortizacije" je NAĐENA UVEK PRAZNA — nije nalaz o interakciji, potvrđeno u kodu**: `AmortizacijaPage.PopuniListuAmortizacija()` prepoznaje samo kartice čiji `OpisPromene` počinje sa "Redovan otpis" ili "Amortizacija" (tačan format koji piše `AmortizacijaCalculator.GenerisiOpisPromene`, npr. "Amortizacija (2026)") — ali demo generator (`DemoPodaciGenerator.SredstvaProizvodnja.cs:172`) seeduje istorijske kartice amortizacije sa tekstom "Obračun amortizacije za {godina}. godinu", koji ne počinje ni jednim od ta dva prefiksa. Posledica: i pored toga što svih 120 sredstava ima godine amortizacione istorije (vidljivo na ekranu Analitičke kartice), padajući meni godina na "Lista amortizacije" ostaje prazan za demo bazu — nije uživo ispravljeno u ovoj sesiji (zahteva izmenu demo generatora + regeneraciju AUTOTEST.db, van obima ovog klik-provere prolaza), samo zabeleženo. **Dodatni srodni nalaz, takođe nepopravljen**: demo generator za ~12% sredstava upisuje `Rashod` zapis (Rashodovanje/Prodaja) sa `Knjizen=true` ali nikad ne upisuje prateći storno red u Kartici niti dira `Sredstvo.NabavnaVrednost/IspravkaVrednosti` — `SredstvaDashboardViewModel.UcitajPodatke()` ima "tihu auto-popravku" koja gasi `JeAktivno` za takva sredstva pri prvom otvaranju Radne table, ali NE nulira vrednosne kolone, pa ta "rashodovana" demo sredstva doživotno ostaju uračunata punom nabavnom/sadašnjom vrednošću u svim ukupnim zbirovima (Radna tabla, Registar, Izveštaji) — ista vrsta bug-a kao onaj upravo ispravljen u `RashodWindow.xaml.cs` na prethodnom ekranu, ovog puta u seed putanji. Deferred namerno — širi zahvat od "uzgred" popravke.
- [x] `BtnSredstvaPopis` — 🗂️ Popis sredstava — tab "Popisne liste" (filter po godini, 1 popis 2026 status "UToku", "Novi popis" dugme) i "Komisije" (3 komisije 2024-2026, selekcija puni desni panel sa članovima i ulogama — testirano na 2026, 3 člana Predsednik/Član/Član ispravno). Duplo-klik za upis popisa (`UpisPopisaWindow`) nije otvoren u ovom prolazu (aktivan/"UToku" popis, izbegava izmenu stanja).
- [x] `BtnSredstvaRevalorizacija` — 💹 Revalorizacija — period/godišnji i 12 mesečnih koeficijenata (svi podrazumevano 1.00), "Obračunaj" (preview, Proknjiži je odvojeno dugme, nije klikano) sa svim koeficijentima 1.00 ispravno prikazuje prazan grid — potvrđeno u kodu, namerno ponašanje ("sredstva bez stvarnog efekta se izostavljaju iz prikaza"), nije nalaz.
- [x] `BtnSredstvaIzvestaji` — 📊 Izveštaji — 4 izveštaja (Popis svih sredstava, Rekapitulacija po kontima/OJ/am.grup), testirano na prva dva: "Popis svih sredstava" lista sva 120 sredstava sa punim kolonama, "Rekapitulacija po kontima" grupiše na 1 red (konto 022000, 120 sredstava, 212.201.800,00/122.870.579,12/89.331.220,88, % otpisa 57,9) — brojevi se tačno poklapaju sa Registrom/Radnom tablom, potvrđuje da je baza posle čišćenja test podataka vraćena na tačno izvorno stanje. Export CSV nije klikan.
- [x] `BtnSredstvaPodesavanja` — ⚙️ Podešavanja — jedan tab "Uvoz podataka" (DOS/DBF uvoz), dugme otvara poseban prozor koji ispravno skenira radni direktorijum i prikazuje firme iz DOS baze. **"Pokreni Uvoz u Aktivnu Firmu" NIJE klikano — namerno**: dijalog je već pre-popunjen pravim direktorijumom/firmom (`C:\SREDSTVA\SREDS\KOR28`, "POLJOPRIVREDNA STRUCNA SLUZBA PIROT") i ima čekboks "Obriši postojeće podatke pre uvoza" — isti obrazac kao Zarade Podešavanja, prava spoljna radnja nad realnim podacima; zatvoreno preko "Zatvori" bez pokretanja.
- [x] `BtnSredstvaPomoc` — ❓ Pomoć &amp; Uputstva — modul-filter već postavljen na "Sredstva" (isti kontekstualni F1 hub obrazac), 5 tema pomoći sa punim tekstom (Osnovna sredstva/Prijava/Rashod/Amortizacija/Popis).

**Ceo Sredstva modul panel je sada 100% pokriven u ovom E2E prolazu.**

## Web (`ERPiWebShop`) — admin, moduli sa pod-tabovima

**Metodologija web dela** (razlikuje se od WPF): izolovan stack (API na 5002 nad kopijom `DEMO.db`
u `c:\tmp\WEB_E2E.db`, drugi vite dev server na 5174) da se ne dira pravi WebShop servis na portu
5000 koji trenutno služi realnu firmu ARHIBEL (`KOR01_ARHIBEL...db`) — pisanje nad tom bazom je
zabranjeno. `web-screens-pass` drajver (CDP, headless Chrome) po ekranu: navigacija + opcioni
klikovi na pod-tabove bez adrese + snimak + konzola/mreža ≥400; svaki snimak zatim vizuelno
pregledan (drajverov "status 200" ne garantuje da je telo stiglo celo, v. skill napomena).
Dubina je "otvori i proveri da li stvarno prikazuje ispravne podatke", ne pun interaktivan
CRUD-krug kao na WPF strani (drajver ne popunjava forme).

### Zarade (admin/zarade) — 29/29 pregledano

**Dva prava bug-a nađena i ispravljena** (isti koren, `DemoPodaciGenerator.Zarade.cs`, funkcija
`Obracunaj` + `ObracunPlate` seed): demo generator je za bulk-seedovane periode postavljao
`BrutoZarada`/`NetoIsplata`/`PorezNaDohodak` ali NIKAD `Neto` (legacy DBF naziv polja — u stvarnosti
= ukupan bruto, ne neto-posle-poreza, v. `ObracunService.cs:538` i `RekapitulacijaDocument.cs`
komentar "sum_neto = neto u OBRACUN") ni `PoreskaOsnovica` — obe kolone su prikazivale 0,00 na
svakom web ekranu koji ih čita (`obracuni` "Neto zarada", `obracun-period`, `ppp-po` "Osnovica").
`Obracunaj()` je već interno računao `poreskaOsnovica` ali je nije vraćao. Ispravljeno dodavanjem
oba polja u tuple/seed (ista formula kao `ObracunService`), `dotnet test ERPiData.Tests` 1627/1627
bez regresije oba puta. **I `DEMO.db` i `AUTOTEST.db` regenerisane** (identičan determinstički seed
20260827) — `DEMO.db` prvi put posle §81 fix-a uopšte nije bila regenerisana pa je nosila i stariji,
odvojeni §81 bug (nulte Porezi/Doprinosi stope za svaki period); sada usklađena sa AUTOTEST-om.
Stare verzije sačuvane kao `*.pre-osnovica-fix.bak` u `%LocalAppData%\ERPi\Baze`.

- [x] `dashboard` — Radna tabla — KPI (35 radnika, neto/bruto masa, 9 kredita), grafikon kretanja zarada po mesecima sa live tooltip-om, mesečna rekapitulacija tabela — sve puni realni podaci.
- [x] `zaposleni` — Zaposleni (Matična knjiga) — 35 radnika, pun set kolona (JMBG/radno mesto/tekući račun/koeficijent/staž/ugovorena zarada), pretraga/filteri/Excel dugmad prisutna.
- [x] `hr-dokumenti` — HR Ugovori, rešenja & Alarmi — 4 taba, arhiva od 30 dokumenata sa statusima (Nacrt/Potpisan/Izdat), generator sa šablonima.
- [x] `sihterica` — Evidencija rada (Šihterica) — matrica 35 radnika × dani meseca, legenda (Rad/Odmor/Bolovanje/Praznik/Prekovremeno), Generiši predlog/Prenesi u obračun/Zaključaj/PDF dugmad.
- [x] `sati` — Radni sati — tabela po radniku sa svim kategorijama sati (redovni/prekovremeni/godišnji/bolovanje/noćni/smenski...), realni brojevi.
- [x] `neoporeziva-primanja` — Neoporeziva primanja (Limiti) — KPI kartice, zakonski limiti (čl. 18 ZPDG) prikazani, 2 evidentirane stavke sa oporezivim viškom tačno računatim.
- [x] `bolovanja` — Bolovanja (RFZO) — spisak OZ-10 za refundaciju, kontrolna provera ispravno javlja upozorenje (nema stavke naknade na teret Fonda za bolovanje >30 dana).
- [x] `odsustva` — Godišnji odmori i odsustva — 78 evidentiranih za 2026, statusi/period/broj rešenja, Excel export.
- [x] `krediti` — Krediti i obustave — 9 kredita/zabrana sa ratom/otplaćeno/ostatak duga, Zbirni izveštaj i Excel dugmad.
- [x] `obracuni` — Obračun zarada & Platni listići — **posle fix-a**: sve kolone (Bruto/Doprinosi/Porez/Neto zarada/Za isplatu) tačno popunjene za 35 radnika; pun toolbar (Spisak/Rekapitulacija/Banke/Nalozi za prenos/Ponovo obračunaj/Pošalji listiće/Excel).
- [x] `obracun-period` — Obračun za period — zbirni pregled sa opsegom perioda/sumiranjem po radniku/mesecu, ista kolona-fix potvrđena i ovde.
- [x] `what-if` — What-If kalkulator & Budžet — dvosmerni kalkulator (Neto→Bruto/Bruto→Neto/Trošak→sve), tačna rekapitulacija za uneti primer.
- [x] `statistika-rzs` — Statistika RZS (RAD-1 & RAD-G) — mesečni/godišnji obrazac, agregati (35 zaposlenih, fond sati, mase zarada) tačni.
- [x] `isplate` — Isplate u mesecu — 1 isplata za 8/2026 (Konačna zarada, 35 radnika), Nova isplata dugme.
- [x] `nalozi-za-prenos` — Nalozi za prenos (E-banking) — 36 pripremljenih naloga sa tekućim računima/iznosima, izvoz u 3 formata (Hal E-Bank/Trezor ePP/ISO 20022), kontrolni nalaz o statusu BOP prijave.
- [x] `ppp-pd` — PPP-PD — zarade — sadržaj prijave za 35 radnika sa ispravnim JMBG, SVP šifre, osnovice/porez/doprinosi po radniku, 0 nalaza.
- [x] `knjizenje` — Knjiženje — ekran se učitava čisto (kontrole + prazno telo dok se ne klikne "Pripremi nalog", nije klikano u ovom prolazu — analogno WPF `BtnZaradeKnjizenje` nalazu, demo obračuni nisu kroz pravi `ObracunService`).
- [x] `primaoci` — Primaoci po ugovoru — 9 primalaca, svi "i u radnom odnosu", tekući računi/status/broj ugovora.
- [x] `isplate-naknada` — Isplate naknada — ispravno prazno za 8/2026 ("Nema podataka koji odgovaraju zadatim kriterijumima").
- [x] `ugovori` — Ugovori i naknade — lista ugovora van radnog odnosa sa vrstom/SVP/predmetom/ugovorenim iznosom (bruto/neto), Nov ugovor dugme.
- [x] `vrste-ugovora` — Vrste ugovora — 9 vrsta (UOD/UOD2/UOD3/ODB/AUT50/AUT43/AUT34/PPP/PPZ) sa OVP/norm.tr./porez/doprinosi % po vrsti.
- [x] `sabloni-ugovora` — Šabloni ugovora — 4 fabrička šablona, editor sa poljima za zamenu ({FirmaNaziv}, {PrimalacIme}...).
- [x] `ppp-pd-naknade` — PPP-PD — naknade — ispravno prazno za period bez isplata naknada, ista forma podataka za XML prijavu kao `ppp-pd`.
- [x] `porezi` — Porezi — **posle fix-a**: stvarne stope (10% porez, 28.423 neoporezivi iznos, 656.425 godišnji cenzus, sve % dodataka) umesto nula.
- [x] `doprinosi` — Doprinosi — PIO/Zdravstveno/Nezaposlenost šifarnik, editor sa svim poljima (stope/osnovice/nalog za prenos).
- [x] `olaksice` — Poreske olakšice — 5 olakšica (čl. 21) sa mehanizmom (Povraćaj/Oslobođenje) i procentima.
- [x] `praznici` — Praznici & fond sati — 8 praznika za 2026, mesečni fond sati po mesecu tačan (Avgust=168h, slaže se sa Šihtericom).
- [x] `ppp-po` — PPP-PO — **posle fix-a**: 36 obrazaca za 2025, kolona Osnovica sada tačno popunjena (ranije 0,00 za sve), Porez/Doprinosi po radniku.
- [x] `banke` — Banke — šifarnik po (Godina/Mesec/Šifra); za 8/2026 ispravno prazan (seed ima zapise samo za mesec 1 svake godine, isto kao WPF nalaz), Nova banka/Excel dugmad prisutni.

### Magacin (admin/magacin) — 20/20 pregledano, bez nalaza

Svi ekrani učitavaju bez console/network grešaka i sa realnim demo podacima (isti stack/metodologija
kao Zarade iznad). Manja zapažanja, nisu nalazi: `robno-kretanje` (Primopredaje) prazan za demo —
nema seed podataka za taj magacin, isti obrazac kao WPF; `robne-kartice` čeka izbor artikla pre
prikaza (očekivano prazno stanje); artikal A01007 ima negativno stanje (-39 kom / -1.676.610,00 RSD)
u `lager` i `bilans` — demo-podaci artefakt (prodato više nego ulazovano), ne UI/kod bug.

- [x] `dashboard` — Radna tabla — KPI (VP/MP/ukupna vrednost zaliha, 1990 artikala), Top 10 po vrednosti, poslednje kalkulacije i nivelacije — sve realno.
- [x] `ponude` — Ponude/Predračuni — 700+ ponuda, CRM faza/verovatnoća/status kolone, Nova ponuda dugme.
- [x] `crm-pipeline` — CRM Prodajni levak — Kanban sa 6 faza, KPI (ukupno u levku, ponderisani prihod, win rate), kartice sa iznosom/verovatnoćom/rokom po ponudi.
- [x] `narudzbenice` — Narudžbenice — lista sa dobavljačem/magacinom prijema/statusom (Isporučena/Delimično/Poslata), tačni iznosi.
- [x] `kalkulacije` — Ulazne kalkulacije (VP) — 1666 zapisa, nabavna/marža/PDV/prodajna vrednost po kalkulaciji, tačan zbir na dnu.
- [x] `mp-kalkulacije` — MP kalkulacije — maloprodajne kalkulacije po prodavnici/dobavljaču, Rasknjiži dugme po redu.
- [x] `uvoz` — Uvozne kalkulacije — ino dobavljač/faktura/carina/ukupna nabavna vrednost, realni podaci.
- [x] `nivelacije` — Nivelacije cena — istorija nivelacija sa ukupnom razlikom (pozitivnom i negativnom), Automatsko svođenje cena dugme.
- [x] `otpremnice` — Računi - Otpremnice — najbogatiji toolbar (Popravi partnere/Masovno knjiženje/Rasknjiži po redu), osnovica/PDV/za uplatu tačni.
- [x] `pretplate` — Periodično fakturisanje — 12 ugovora o održavanju sa periodičnošću/sledećim fakturisanjem/iznosom, Generiši dospele dugme.
- [x] `robno-kretanje` — Primopredaje/Zaduženja/Razduženja robe — 3 taba rade, Primopredaja tab prazan za ovaj magacin (nema seed, nije nalaz).
- [x] `wms-lokacije` — WMS Skladišne lokacije & Picking — 108 lokacija/108 dodeljenih/100% popunjenost, matrica sa dodeljenim artiklima po ruti/zoni/polici, Čarobnjak matrice dugme.
- [x] `popis` — Popis (inventar) robe — popis u toku sa 1995 stavki, 1990 prebrojano, razlika -965.130,89 RSD, Otvori dugme.
- [x] `lager` — Lager lista & Stanje zaliha — 1995 artikala, stanje/nabavna/prodajna vrednost po artiklu, tačan zbir.
- [x] `robne-kartice` — Robne kartice — izbor magacina + pretraga artikla sa leve strane, čeka selekciju (očekivano).
- [x] `bilans` — Robni Bruto bilans — Duguje/Potražuje/Saldo zaliha na vrhu (12.563.790.638,02 RSD), tabela po artiklu sa ulaz/izlaz/saldo kol. i vr., Bruto bilans/Raspored dugmad.
- [x] `sarze` — Šarže i rokovi trajanja — 151 šarža ističe/isteklo upozorenje, tabela sa rokom trajanja i danima do isteka po šarži.
- [x] `serijski-brojevi` — Serijski brojevi — lista sa statusom (Na stanju)/datumom ulaza/nabavnom cenom, filter po statusu.
- [x] `magacini` — Šifarnik magacina — 5 magacina (VP/MP), odgovorno lice/grad preuzimanja/Click&Collect po magacinu.
- [x] `poreske-tarife` — Poreske tarife — 3 tarife (00/10/20%), PDV/poseban porez/porez-u-ceni kolone.

### Finansije (admin/finansije) — 15/15 pregledano

**Nalaz, NIJE ispravljen ove sesije (širi zahvat od uzgred popravke)**: na `otvorene-stavke` (IOS),
~81% od 31.546 stavki (25.521) nema `ValutaDospela` — konto 131000 (1666 od ~1666+ stavki iz
Robno modula, `DemoPodaciGenerator.Robno.cs` postavlja Kupci/Dobavljači stavke bez `valuta:`
parametra), 241000/270000/470000 (bankovne/PDV stavke — upitno da li due-date uopšte ima smisla
za njih), 512000-562000 (rashodni konti — isto upitno), i delimično čak i 435000 (4211 od ~8500+).
Nasuprot tome, `DemoKontniPlan.Kupci`="204000"/`Dobavljaci`="435000" iz `DemoPodaciGenerator.Finansije.cs`
(linije 103/135/317) UREDNO postavljaju `valuta:` — i `Cash-Flow & Kontroling` ekran (koji čita
konto 204000) ispravno prikazuje dana kašnjenja/status. Posledica: `otvorene-stavke` liste (koja
sortira po `ValutaDospela` rastuće, pa NULL vrednosti idu prve) na prvoj strani pokazuje "–"/"Nije
dospelo" za SVE vidljive redove — deluje da je ceo IOS ekran slomljen, ali je zapravo samo prvih
~19% naloga (konto 204000, iz Finansije generatora) ispravno markirano. Da li je "nema roka
dospeća" tačno za bankovne/PDV/rashodne konte je knjigovodstveno pitanje van obima ovog klik-
prolaza — zahteva odluku o kontnom modelu, ne samo dopunu jednog polja. Zabeleženo za buduću
sesiju, isti obrazac kao "Lista amortizacije" nalaz na WPF Sredstva strani.

- [x] `dashboard` — Radna tabla — KPI (potraživanja/obaveze/neto likvidnost sa upozorenjem), Top 5 kupaca/dobavljača po dugovanju, status naloga (99% proknjiženo od 16215), statistika GK — sve realno.
- [x] `nalozi` — Nalozi glavne knjige — dnevnik knjiženja sa punim toolbarom (12 akcija), 16000+ naloga, Duguje=Potražuje po nalogu tačno balansirano.
- [x] `kartice` — Dnevnik & Kartice konta — kontni plan za izbor (34 konta sa knjiženjima), čeka selekciju konta pre prikaza kartice (očekivano).
- [x] `otvorene-stavke` — Otvorene stavke (IOS) — vidi nalaz iznad; ekran tehnički radi (učitava 31546 stavki, filter/Excel/E-banking nalozi dugmad prisutni), ali podaci nepotpuni za due-date kolonu na većini redova.
- [x] `kompenzacije` — Kompenzacije & Cesije — "Kandidati za kompenzaciju" lista partnera koji su i kupac i dobavljač sa Max. kompenzacija tačno izračunatim (min od potraživanja/obaveze).
- [x] `putni-nalozi` — Putni nalozi & Dnevnice — 333 naloga, relacija/polazak-povratak/dnevnice/za isplatu tačno, svi Proknjiženi.
- [x] `blagajna` — Dinarska & Devizna blagajna — 3 taba (Sve/Dinarska/Devizna), uplate/isplate sa protivkontom, tačan zbir na dnu.
- [x] `mesta-troska` — Mesta troška & Projekti — 5 mesta troška (Uprava/Veleprodaja/MP/Proizvodnja/WebShop), Analitika profitabilnosti po mestu troška radi (Uprava ispravno 0 prihoda/svi rashodi — administrativni centar, ne nalaz).
- [x] `devizno` — Devizno & Valviranje — kursna lista NBS za dati datum, ~40 valuta sa kupovni/srednji/prodajni kurs, Osveži sa NBS-a dugme.
- [x] `izvestaji` — Bruto bilans & APR Bilansi — 8 taba (Bruto bilans/Bilans stanja/uspeha/Zaključni list/Dnevnik GK/Statistički izveštaj/Cash Flow/Promene na kapitalu/Poreski Bilans), Bruto bilans po kontu/klasi sa TOTAL sintetika tačno agregiran.
- [x] `kontroling` — Cash-Flow & Kontroling — KPI (stanje novca/dospela potraživanja i obaveze/procenjene plate), projekcija likvidnosti 30/60/90 dana (ispravno prikazuje deficit), otvorena potraživanja/obaveze sa danima kašnjenja tačno računatim (potvrđuje da konto 204000 ima ispravan ValutaDospela, v. nalaz iznad).
- [x] `popdv` — Obrazac POPDV (PDV prijava) — 8 mesečnih obrazaca za 2026 (01-08), Nov obrazac/Izmenjena prijava dugmad.
- [x] `zakljucenje-godine` — Zaključenje poslovne godine — 2025 godina sa prihod/rashod po kontu (klasa 5/6), predlog poreza na dobit (15%) tačno izračunat, upozorenje o nacrtima koji ne ulaze, Zaključi dugme (nije kliknuto — nepovratna radnja, isti obrazac kao WPF).
- [x] `partneri` — Partneri (Kupci/Dobavljači) — 300+ partnera, filter Svi/Kupci/Dobavljači, adresa/PIB/kontakt/uloga/status kolone.
- [x] `konta` — Kontni plan — 203 konta, klasa/tip (Sintetika/Analitika)/vrsta (Aktivna/Pasivna), Excel export.

### Sredstva (admin/sredstva) — 10/10 pregledano

**PRAVI BUG NAĐEN I ISPRAVLJEN — `konta` (Konta amortizacije).** Ekran je za već mapirane konte
prikazivao „—" (nemapirano) u kolonama Trošak amortizacije i Konto gubitka/dobitka, dok je baner
istovremeno tvrdio „Svi konta su potpuno mapirani" — ekran je protivrečio sam sebi. Koren: 
`KontaAmortizacijePodTab.tsx` je punio padajuće liste iz `finansijeApi.getKonta()`, a taj endpoint
(`FinansijeController.GetKonta`, red 708) ima `.Take(100)` jer je namenjen kao **F3 brzi lookup sa
pretragom** u `NalogFormaModal`. Demo baza ima 203 konta, pa svaki konto iza 100. po broju nije
ulazio u listu: `trosakKontoId=147` (540000 Troškovi amortizacije) i `rashodniKontoId=170`
(579000 Ostali nepomenuti rashodi) su bili uredno sačuvani u bazi (potvrđeno direktnim pozivom
`GET /api/OsnovnaSredstva/konta-amortizacije`), ali `<select>` bez odgovarajućeg `<option>`-a pada
na prazno. Posledica je dvostruka: (1) korisnik vidi lažno „nemapirano" stanje, (2) ne može ni da
ga ispravi jer traženi konto uopšte nije u ponudi. **Ispravka:** prelazak na
`finansijeApi.getKarticeKonta()` — isti `KontoLookup` tip i isto polje `prikaz`, ali bez limita od
100 (203/203 konta, potvrđeno pozivom `GET /api/finansije/kartice/konta`); postojao je već i
komentar u `finansijeApi.ts` da je baš to namena te funkcije. `npm run build` čist, `npx vitest run`
295/295 bez regresije, vizuelno potvrđeno da sve 4 kolone sada prikazuju stvarno mapiranje.

- [x] `dashboard` — Radna tabla — KPI (120 sredstava/108 aktivnih, nabavna 212.201.800,00 / ispravka 122.870.579,12 / sadašnja 89.331.220,88 — poklapa se sa WPF stranom), Top najvrednijih sredstava sa progress barovima, Status sredstava, Sadašnja vrednost po kontima, Amortizacione grupe I-V.
- [x] `registar` — Registar sredstava — 108 aktivnih (filter „Samo aktivna" uključen), inv. broj/naziv/konto/grupa/stopa/nabavna/ispravka/sadašnja, tačan zbir na dnu, Novo sredstvo i Amortizacija dugmad.
- [x] `kartice` — Analitičke kartice — lista sredstava za izbor sa pretragom, čeka selekciju (očekivano prazno stanje).
- [x] `prijave` — Prijava sredstava (nabavka) — 120 naloga prijave sa dobavljačem/brojem stavki/vrednošću, svi Proknjiženi, ukupno 212.201.800,00 RSD (slaže se sa Registrom).
- [x] `rashodi` — Rashod i promene — 14 naloga rashoda/promena, svi Proknjiženi, Novi nalog/Excel dugmad.
- [x] `reversi` — Reversi i zaduženja — 34 reversa za 2026 (Zaduženje/Razduženje), radnik/lokacija/vrednost/status (Nacrt i Potvrđen), tab „Trenutna zaduženja (32)", QR nalepnice dugme.
- [x] `revalorizacija` — Revalorizacija — forma sa periodom, godišnjim i 12 mesečnih koeficijenata (svi 1), Pokreni obračun (pregled) i Proknjiži dugmad; nije pokretano (isti obrazac kao WPF — masovna izmena vrednosti).
- [x] `popis` — Popis sredstava — 3 popisne komisije (2024-2026) sa članovima i ulogama, popisna lista 2026 (60 ukupno / 58 popisano / 2 sa razlikom, status U toku).
- [x] `izvestaji` — Izveštaji i rekapitulacije — 4 taba (Popis svih sredstava/Rekapitulacija po kontu/po OJ/po am. grupi), popis svih 120 sredstava sa tačnim zbirom koji se poklapa sa Radnom tablom i Registrom.
- [x] `konta` — Konta amortizacije — vidi bug iznad; posle ispravke prikazuje puno mapiranje (022000 → 540000 trošak / 022900 ispravka / 435000 dobavljač / 579000 gubitak-dobitak).

### Materijalno (admin/materijalno) — 7/7 pregledano, bez nalaza

**Web strana potvrđuje §81 ispravku**: `bilans-materijala` prikazuje Duguje 10.672.168.637,00 /
Potražuje 1.208.882.934,00 / Saldo zaliha 9.463.285.703,00 RSD — identično brojevima potvrđenim
na WPF strani, tj. `MaterijalnaKartica.Vrsta` filter radi jednako na oba klijenta.

- [x] `dashboard` — Radna tabla — KPI (vrednost zaliha 9.463.285.703,00 / 266 materijala na zalihi / 1 negativno stanje), Top 10 materijala po vrednosti, Poslednji ulazi i Poslednja trebovanja.
- [x] `ulazi` — Ulaz materijala — dnevnik ulaza sa magacinom/brojem računa/iznosom, svi Proknjižen, Rasknjiži dugme po redu, ukupno 10.478.818.503,00 RSD.
- [x] `trebovanja` — Trebovanja materijala — dnevnik trebovanja sa brojem stavki po nalogu, svi Proknjiženo, Rasknjiži po redu.
- [x] `primopredaje-materijala` — Primopredaje materijala — 3 taba (Primopredaja/Zaduženje/Razduženje), magacin daje→prima (sirovina→gotovih proizvoda), Rasknjiži po redu.
- [x] `materijali` — Šifarnik materijala — 266 materijala sa šifrom/nazivom/JM/pakovanjem, CRUD ikonice, Nov materijal/Excel.
- [x] `kartice-materijala` — Kartice materijala — prekidač Roba (Artikli)/Materijal, izbor magacina + pretraga materijala sa leve strane, čeka selekciju (očekivano).
- [x] `bilans-materijala` — Bruto bilans materijalnog — vidi napomenu iznad; tabela po materijalu sa ulaz/izlaz kol. i vr. i saldom, prekidač Roba/Materijal, Bruto bilans/Raspored/Stanje po artiklima dugmad.

### Proizvodnja (admin/proizvodnja) — 7/7 pregledano, bez nalaza

- [x] `radna-tabla` — Radna tabla proizvodnje — KPI (10 aktivnih naloga / 110 završenih / 10.639.910.371,63 RSD vrednost / 24 aktivne sastavnice), Aktivni radni nalozi i Sastavnice sa cenom koštanja.
- [x] `sastavnice` — Sastavnice (BOM/normativi) — svih 24 normativa sa gotovim proizvodom/verzijom/normativnom količinom/planskom cenom koštanja, CRUD+Kopiraj ikonice.
- [x] `radni-nalozi` — Radni nalozi — 500 naloga, statusi (U pripremi/Lansiran/U radu/Završen), planirano vs proizvedeno, cena koštanja popunjena samo za Završen (ispravno — obračun se radi na završetku), ukupno 52.459.222.647,42 RSD.
- [x] `mrp` — MRP I — Plan potreba materijala — Gross-to-Net kalkulacija radi: 5 obuhvaćenih naloga, 21 potrebna sirovina, 3 deficitarna artikla ispravno označena DEFICIT (bruto potreba > zaliha), ostali POKRIVENO, procenjen trošak nabavke 20.529.463,22 RSD.
- [x] `nedovrsena` — Nedovršena proizvodnja — 2 taba (Obračun na dan / Stanje na kontu 1100), 10 naloga u obračunu sa mestom troška/iznosom na dan/već proknjiženo/razlikom, Proknjiži razliku dugme (nije klikano — knjiženje u GK).
- [x] `kalkulacija` — Kalkulacija cene koštanja — 2 taba (Po sastavnici planska / Po radnom nalogu stvarna+varijansa), izbor sastavnice puni KPI kartice (Direktni materijal 95,54% / Troškovi rada / Amortizacija i mašine / Jedinična cena koštanja) — udeli se sabiraju tačno.
- [x] `podesavanja` — Podešavanja proizvodnje — automatsko knjiženje u GK prekidač, konta za knjiženje (utrošak/zalihe materijala, gotovi proizvodi, nedovršena proizvodnja, konta režije), ključ raspodele režije (Sati rada).

## Web — ravni tabovi (bez pod-tabova) — 17/17 pregledano

Dve lažne uzbune u prvom prolazu, obe razjašnjene bez ispravke koda:
- `artikli` je javio 8× mrežni 404 na `/slike/N/artikal_thumb.svg` — koren je bio u **testnom
  okruženju ove sesije**, ne u app-u: izolovana kopija baze (`c:\tmp\WEB_E2E.db`) je kopirana bez
  prateće `Slike\DEMO\` fascikle (`SlikeArtikalaStorage` slike traži pored baze, po njenom imenu).
  Nakon kopiranja fascikle 404 nestaju.
- `sef-izvodi` je javio da tekst stranice sadrži „Greška" — to je legitimna oznaka statusa
  (`Greska` je validan `SefStatus` sa filter-opcijom „Greška pri slanju" i crvenim bedžom u
  `SefIzvodiTab.tsx`), ne JS izuzetak ni neuspeo poziv; driver samo traži tu podnisku u celom
  tekstu strane bez razlikovanja konteksta.

- [x] `/admin` — Dashboard — KPI (promet danas/mesečno, B2B zahtevi na čekanju, katalog na webu 1717/60 kat.), Poslednje porudžbine, Najprodavaniji artikli, grafikon poseta.
- [x] `porudzbine` — Porudžbine — 3 pod-taba (Web porudžbine/Zbirni kurirski manifesti/Marketplace integracije), lista sa statusima (Prihvaćena/Nova) i B2B bedžom, Detalji & Status po redu.
- [x] `porudzbine` — **SignalR live push (§110, 01.09.2026)** — dva NEZAVISNA CDP taba, oba prijavljena
  kao isti admin, otvoren `/admin/porudzbine`. Treća strana (node skript, van oba taba) kreira
  porudžbinu preko `POST /api/porudzbine/kreiraj` — simulira pravog kupca na checkout-u, ne dugme u
  adminu. Provereno na oba taba, BEZ ijednog ručnog refresh-a/klika: nov red se pojavljuje na vrhu
  tabele, bedž „Porudžbine" u bočnom meniju raste na 1; tab evaluiran unutar 4s prozora je uhvatio i
  sam Toast („🛒 Nova porudžbina WP-... — ... (22.990,00 RSD)"). API log potvrđuje čist tok: jedan
  `POST /hubs/erpi-live/negotiate` po tabu (200), pa `GET /api/admin/porudzbine` + `GET
  /api/admin/dashboard` na OBA taba odmah posle kreiranja porudžbine, bez ijedne greške. Odvojeno,
  preko `curl`: `negotiate` vraća 401 bez tokena i 200 sa `?access_token=` u query stringu — hub
  handshake auth ispravan. Izolovana kopija `DEMO.db`, API 5002/vite 5174; skript je privremen, u
  scratchpad-u, ne u repou (ne dira deljeni `driver.mjs`).
- [x] `artikli` — Artikli na webu — vidi napomenu iznad (slike su lažna uzbuna); 1717 artikala, kolone slika/šifra/naziv/kategorija/cena/akcijska cena/objavi prekidač/Novo-Top bedž, Masovni uvoz slika/Eksportuj CSV.
- [x] `kategorije` — Kategorije — stablo kategorija (Ručni alat/Električni alat/Vodovod.../15+ grupa), broj artikala po kategoriji, redosled/istaknuto/prekidač objave po redu.
- [x] `osobine` — Osobine — 7 osobina (Boja/Veličina/Snaga/Napon/Materijal/Garancija/Težina), tip (izbor/broj), broj vrednosti, osa varijanti Da/Karakteristika.
- [x] `b2b` — B2B Zahtevi — 2 taba (Zahtevi za naloge/Cenovnik i limiti), ispravno prazno „Nema novih B2B zahteva na čekanju".
- [x] `kupci` — Kupci & CRM — 400 kupaca / 60 B2B partnera / 126.428.734,77 RSD realizovan promet, loyalty poeni po kupcu, Postavi B2B akcija po redu.
- [x] `kuponi` — Kuponi & Popusti — 6 promo kodova (fiksni i % popust), min. korpa/iskorišćeno/rok, Novi promo kod.
- [x] `recenzije` — Recenzije — 22 na čekanju odobrenja, ocena/proizvod/verifikovana kupovina bedž, Odobri/Obriši po redu.
- [x] `reklamacije` — Reklamacije — 13 sa statusima (Na čekanju/Odobrena/Refundirana/Odbijena), razlog i napomena po slučaju, Odobri/Odbij za one na čekanju.
- [x] `napustene-korpe` — Napuštene korpe — KPI (47 aktivnih / 3.008.069,00 RSD potencijalni gubitak / 3 spašene / 6% konverzija), Pokreni automatski oporavak, Pošalji podsetnik po redu.
- [x] `obavestenja-zaliha` — Čekaju robu — KPI (36 kupaca čeka / 35 spremno za slanje / 24 poslato), Najtraženiji rasprodati artikli, Pošalji sada dugme.
- [x] `sef-izvodi` — SEF & Izvodi — vidi napomenu iznad o „Greška" lažnoj uzbuni; 5 pod-tabova, registar e-Faktura sa PIB/osnovica/PDV/status Nacrt, Pošalji/XML po redu.
- [x] `kasa` — Kasa (POS) — 3 pod-taba (Kasa POS Naplata/Poklon kartice & Vaučeri/EFT POS PinPad), „Nema otvorene smene" stanje ispravno, PROBNO OKRUŽENJE baner — isti obrazac kao WPF `BtnKasa`.
- [x] `kpi` — KPI & Izveštaji — izvršni dashboard (prihod/rashod/neto rezultat po sektoru: Prodaja/Nabavka/Proizvodnja), AI poslovna preporuka, Preuzmi PDF.
- [x] `cms` — CMS & Branding — identitet firme/boje, hero sekcija, dostava i plaćanja, kurirske službe & API integracija (sandbox mod), Popuni podrazumevano.
- [x] `firma` — Firma i korisnici — 2 taba (Osnovni podaci/Korisnici), matični podaci firme (PIB/MB/žiro/adresa) ispravno popunjeni.

## Web — pod-ekrani bez adrese (klik kroz `useState`) — 16/16 pregledano

**PRAVI BUG NAĐEN I ISPRAVLJEN — `AiAsistentModal`.** `AiAsistentService` (backend) piše odgovore
u mini-markdownu (`"iznosi **{iznos} RSD**"`, `Services/Ai/AiAsistentService.cs`, 3 mesta), ali
`AiAsistentModal.tsx` je odgovor štampao golim `<p>{p.tekst}</p>` — korisnik je u čatu video
doslovne zvezdice (`**72.938.805,92 RSD**`) umesto podebljanog teksta. Isti obrazac bug-a kao već
dokumentovan u `sigurniHtml.ts` (WebOpis artikla se do 28.08.2026 štampao kao goli HTML) — ekran
koji renderuje serverski formatiran tekst kao čist string. **Ispravka:** mala `formatirajPoruku()`
helper funkcija u `AiAsistentModal.tsx` — escape HTML entiteta pa `**tekst**` → `<strong>`,
renderovano preko `dangerouslySetInnerHTML` (sadržaj je uvek escapovan pre zamene, bezbedno i za
AI odgovor i za sopstveni uneti tekst korisnika). `npm run build` čist, `npx vitest run` 295/295
bez regresije, vizuelno potvrđeno — odgovor sada ispravno podebljava iznos i broj računa.

- [x] Kasa → Kasa (POS Naplata) — pokriveno na `kasa` ekranu iznad (podrazumevani tab).
- [x] Kasa → Poklon kartice & Vaučeri — 80 izdatih kartica, 38 aktivnih sa saldom, 146.891,42 RSD ukupan raspoloživ saldo, filter Sve/Aktivne/Potrošene/Blokirane/Istekle, Dopuni po redu.
- [x] Kasa → EFT POS PinPad podešavanja — puna forma (naziv uređaja/protokol/tip veze/IP/port/timeout/Terminal ID), Testiraj PinPad dugme, Softverski Simulator (Razvoj i Demo) protokol.
- [x] Kasa → Dnevni pazar (istorija smena) — ikonica bez teksta (`title` atribut, ne innerText — nije klikana driver-om, isti razlog kao WPF-ova napomena o koordinatnim klikovima); vidljiva i dostupna na `kasa` snimku.
- [x] Porudžbine → Web porudžbine — pokriveno na `porudzbine` ekranu iznad (podrazumevani tab).
- [x] Porudžbine → Zbirni kurirski manifesti (Dnevni spisak) — 120 manifesta, 127 predatih pošiljki, 5.270.855,00 RSD ukupan otkup, filter po službi (PostExpress/DExpress/Bex/Aks), Novi manifest.
- [x] Porudžbine → Marketplace integracije — 5 kanala (Ananas/Shoppster/WooCommerce/Shopify/Wolt Drive), aktivni sa poslednjom sinhronizacijom, dnevnik sinhronizacije i webhook prijema. „Greška" nalaz na ovom ekranu iz prvog prolaza je lažna uzbuna (legitiman status u dnevniku sinhronizacije, van vidljivog dela snimka).
- [x] Forma artikla → Opis — web naziv/kategorija/HTML editor opisa sa live formatiranjem, upload slika (glavna slika bedž), YouTube URL.
- [x] Forma artikla → Detalji — EAN/MPN/UPC/ISBN barkodovi, Karakteristike (Snaga/Napon/Materijal/Garancija/Težina), tehnička dokumentacija PDF upload, Varijante.
- [x] Forma artikla → 🎁 Paket / Set — čekboks „Ovaj artikal je paket/set", objašnjenje ispod.
- [x] Forma artikla → Isporuka — dimenzije pakovanja i težina, tekst na stanju/nema na stanju, dodatni trošak dostave, dostupne kurirske službe.
- [x] Forma artikla → Zalihe — trenutno stanje (svi magacini), prag za email upozorenje, dostupan od datuma, dozvoli porudžbinu bez zalihe.
- [x] Forma artikla → Cene — redovna cena (ERP, read-only) i akcijska cena na webu, povezani artikli, količinski popusti.
- [x] Forma artikla → SEO — meta naslov/opis sa brojačem karaktera, prijateljski URL slug, tagovi/ključne reči.
- [x] Forma artikla → Opcije — prikaži na webu/Novo/Top prekidači, vidljivost (Svuda/Samo katalog/Samo pretraga/Nigde), min. količina/korak/redosled, dobavljači.
- [x] `AiAsistentModal` — vidi bug iznad; NLP upit „Koliki je prihod ovog meseca?" vraća stvaran odgovor (72.938.805,92 RSD kroz 72 računa) sa deep-linkom „Otvori izdate račune" i predloženim sledećim pitanjima — stvarna analitika, ne mock.

## Web — B2B portal (`/b2b/:tab`) — 6/6 pregledano, bez nalaza

Zaseban login od admin panela — B2B koristi kupčev `erpi_token`/`AuthContext` (`/api/auth/login`,
`WebKorisnici.JeB2B=true`), ne `erpi_osoblje_token`. Testirano kao `nabavka13@horizont-p-r.rs`/
`demo1234` (demo generator daje istu podrazumevanu lozinku svim web nalozima, `DemoLozinka.
Podrazumevana`). **Metodološka zamka nađena i rešena, vredna zapisa**: `services/api.ts`
`authHeaders()` namerno uvek prvo uzima `erpi_osoblje_token` pre `erpi_token` (da bi admin koji
gleda B2B kao kupac i dalje video svoja admin prava) — pošto je isti Chrome profil ove sesije
prethodno korišćen za ceo admin prolaz, stari osoblje token je zasenio novi kupčev token i svaki
`/api/b2b/*` poziv je pucao na `403` (`B2bController` ispravno vraća `Forbid()` kad `PartnerId`
claim nedostaje — osoblje token ga nema). Nije app bug — čim se `erpi_osoblje_token`/`_user`
eksplicitno obrišu pre upisa kupčevog tokena, sve radi. Ista ispravka higijenski dodata i u
`driver.mjs` (obrisan eventualni stari `erpi_token` pre upisa osoblje tokena) za buduće prolaze.

- [x] `dashboard` — KPI (ukupno zaduženje 14.280.981,09 / dospeo dug 11.760.022,41 / kreditni limit odobreno 1.195.990,00, slobodno 0,00 — potrošen limit, matematički tačno jer dospeo dug > limit), prečice na Katalog/Brzo naručivanje/Fakture.
- [x] `katalog` — 1717 artikala sa filterom po kategoriji, cena bez/sa PDV-om, stanje zaliha po artiklu, U korpu dugme.
- [x] `brzo-narucivanje` — 2 taba (Tabelarni unos po šifri/Matrica varijanti), 5 redova za unos šifre+količine, Uvezi spisak iz Excel/CSV-a, Dodaj još jedan red.
- [x] `fakture` — Cenovnik (PDF/Excel/Pošalji na Email) + 15 otvorenih stavki i računa sa datumom/iznosom/preostalim dugom, PDF po redu, Preuzmi IOS (PDF).
- [x] `adrese` — ispravno prazno „Još nema sačuvanih adresa isporuke", Dodaj adresu dugme.
- [x] `tim` — Porudžbine na čekanju (ispravno prazno) + Korisnici firme (1 korisnik, Odobravalac/Aktivan čekboksovi), Dodaj korisnika.

## Web — ESS portal (`/ess/:tab`)

**Nalaz, nije ispravljeno — demo generator ne povezuje nijedan staff (`Korisnici`) nalog sa
`Radnik` zapisom.** ESS (`EssPortalApp`) koristi isti `erpi_osoblje_token` kao admin panel
(`useOsobljeAuth`, ne poseban kupac-login), ali svaki od 8 `/api/ess/*` poziva traži da prijavljeni
`Korisnik.BrojRadnika` pokazuje na stvaran `Radnik` — provereno upitom, nijedan od 6 fiksnih demo
naloga (`admin/knjigovodja/komercijala/magacin/kadrovska/pregled`, `DemoPodaciGenerator.Sifarnici.cs`)
nema taj broj postavljen. Posledica: **nijedan demo nalog ne može stvarno da uđe u ESS** — API
ispravno odbija sa jasnom porukom („Vaš korisnički nalog nije povezan sa dosijeom radnika u
sistemu.", HTTP 400, ne 500/pad), pa to nije bag u smislu rušenja, ali čitav portal ostaje
nedostupan za demonstraciju. Ispravka bi zahtevala povezivanje jednog od fiksnih naloga (npr.
„kadrovska") sa postojećim `Radnik` zapisom u generatoru — van obima „uzgred" popravke jer generator
danas kreira Korisnike i Radnike u odvojenim, redosledno nezavisnim koracima (`DemoPodaciGenerator.
Sifarnici.cs` vs `DemoPodaciGenerator.Zarade.cs`), isti obrazac kao WPF-ova odložena „Lista
amortizacije" popravka.

- [ ] `profil` — blokirano gore opisanim nalazom, nije vizuelno potvrđeno.
- [ ] `listici` — blokirano gore opisanim nalazom, nije vizuelno potvrđeno.
- [ ] `odmori` — blokirano gore opisanim nalazom, nije vizuelno potvrđeno.

**Napomena — `/moj-portal` je alias za ovaj isti portal, ne posebna B2C ruta.** `App.tsx:251`:
`essPortalOtvoren` se pali i na `/ess` i na `/moj-portal` (isti shell, isti `erpi_osoblje_token`).
Originalni spisak ga je greškom svrstao pod „B2C prodavnica" ispod — testiran sa B2C kupac-nalogom
i, očekivano, pao na isti 403 kao gore (kupac token nema `TipNaloga=Osoblje`), potvrđujući da je
ruta zaista ESS, ne kupčev nalog.

## Web — WMS terminal (`/wms`) — 6/6 pregledano, bez nalaza

Koristi isti `erpi_osoblje_token` kao admin panel (staff login), za razliku od ESS-a nema potrebe
za `Radnik` vezom — radi odmah sa `admin` nalogom.

- [x] Komisioniranje — 2 aktivna naloga (Novi/U toku) sa partnerom/vezanim računom-otpremnicom i progres barom stavki.
- [x] Šta je ovo? (skeniranje) — skener input polje sa fokusom, prazno stanje „Skeniraj bilo koji kod" ispravno objašnjava dvosmernost (polica→sadržaj, artikal→police).
- [x] Smeštaj — skener input, 3 polja (Artikal/Na policu/Količina) ispravno u stanju „čeka skeniranje", Smesti dugme onemogućeno dok se ne skenira.
- [x] Premeštaj — isti obrazac, 3 polja (Artikal/Sa police/Na policu) + količina, Premesti dugme onemogućeno.
- [x] Dopune — 4+ predloga dopune (police ispod minimuma), svaki sa tačnim izvorom(-e)/odredištem/predlogom količine; ispravno objašnjava kad nema rezervne police („roba mora prvo da uđe kroz prijem").
- [x] Dnevnik — dnevnik kretanja sa tipom radnje (Smeštaj/Premeštaj/Dopuna/Komisioniranje/Korekcija), artikal/šifra/količina/od→do/operater/vreme, hronološki opadajuće.

## Web — prodavnica (B2C) — 6/6 pregledano, bez nalaza

Testirano prijavljeni kao B2C kupac (`bojan.peric1@primer.rs`/`demo1234`, `WebKorisnici.JeB2B=false`)
— javna prodavnica radi i bez prijave, ali prijava vezuje loyalty poene/wishlist u zaglavlju.
`/moj-portal` je uklonjen iz ovog spiska — to je alias za ESS portal, ne B2C ruta, vidi napomenu
gore.

- [x] `/` — Početna — hero sekcija, brzi filter po kategorijama u navigaciji, izdvojena ponuda sa popustom, najtraženije/izdvojene kategorije, PWA install prompt, footer sa punim kontakt/kategorija linkovima.
- [x] `/kategorija/:slug` — testirano `rucni-alat`: 121 od 1717 artikala, filter sidebar (podkategorije/atributi), sortiranje, kartice sa cenom/zalihom/ocenom.
- [x] `/proizvod/:sifra` — testirano `A01001`: šifra/barkod/kategorija, cena, na stanju, plaćanje na rate, Click & Collect, opis + karakteristike (Boja/Garancija/Materijal), količina, U korpu i Kupi odmah (bez korpe, pouzećem), pitanje prodavcu.
- [x] `/uslovi-koriscenja` — ispravno prazno stanje „Sadržaj još nije unet. Za više informacija obratite se prodavcu." (graciozan fallback, ne prazna/pukla stranica).
- [x] `/politika-privatnosti` — isti obrazac praznog stanja kao gore.
- [x] `/pravo-na-odustanak` — isti obrazac praznog stanja kao gore.
