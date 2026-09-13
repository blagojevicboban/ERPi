# 🧾 Dizajn — PWA / offline Web Kasa

> Dizajn-korak pre koda, po `PLAN_NASTAVKA.md` „Arhitektura" stavci 12 („PWA/offline Web Kasa —
> proveriti prvo da li Web `KasaTab` ikad treba da radi sa fizičkim fiskalnim uređajem... offline
> fiskalizacija je suštinski teža od 'sinhronizuj po povratku veze'"). Korisnik je 13.09.2026
> potvrdio preduslovno pitanje: **Web Kasa mora ikad da fiskalizuje na pravom fiskalnom uređaju**
> — ovaj dokument fiksira šta to znači za offline rad, pre nego što bilo šta uđe u kod.

## 1. Šta danas postoji (provereno u kodu, ne pretpostavljeno)

### Fiskalizacija je HTTP-do-HTTP, ne serijski/USB port iz aplikacije

Ni WPF ni Web Kasa ne pričaju direktno sa fiskalnim uređajem preko COM/USB-a. Ceo protokol je
HTTP REST v3 ka **PFR** (Poresko-Fiskalni Račun procesor), koji je ili:

- **L-PFR** — softver/uređaj kod obveznika, tipično na `http://localhost:8888` (adresa se ručno
  unosi u *Podešavanja → e-Fiskalizacija*, polje `Firma.PfrUrl`), ili
- **V-PFR** — servis Poreske uprave u oblaku, adresu bira sistem, autentifikacija klijentskim
  `.p12` sertifikatom (`Firma.PfrSertifikatPutanja`/`PfrSertifikatLozinka`).

Poziva ga `PfrApiClient.cs` (4 putanje: `GET status`, `POST pin`, `POST invoices`,
`GET attention`) — sloj koji „nikad ne baca na mrežnu grešku, vraća `Success`/`Message`" (već
danas gradi za neuspeh, ne za crash).

**Ključna činjenica iz `docs/KASA.md` §Pojmovi, presudna za ovaj dizajn:**

| Režim | Ko je | Radi bez interneta? |
| --- | --- | --- |
| **L-PFR** | uređaj/servis kod obveznika | ✅ da — potpisuje lokalno, sam sinhronizuje sa Poreskom upravom kad može |
| **V-PFR** | servis Poreske uprave (cloud) | ❌ ne — „traži stalnu vezu" |

Ovo znači da **pojam „offline fiskalizacija" ima potpuno različit odgovor zavisno od režima
firme** — nije jedan zajednički problem koji app rešava jednim mehanizmom (vidi §2).

### Ko poziva koga — oba klijenta idu kroz isti servis

```text
WPF:  KasaView → FiskalniRacunWindow → PfrService.FiskalizujRacunOtpremnicuAsync
Web:  KasaTab → kasaApi.zakljuciRacun() → HTTP → KasaController.ZakljuciRacun
                                            → PosService.ZakljuciRacunAsync
                                            → PfrService.FiskalizujRacunOtpremnicuIzKaseAsync
```

`PfrService` je zajednički — **Web Kasa već ume da fiskalizuje danas, nije prazno mesto.**
Razlika je topološka: WPF poziva PFR sa **iste mašine** na kojoj radi kasirka (L-PFR na
`localhost` te mašine ima smisla). Web Kasa poziva PFR **sa `ERPiApi` servera**, koji možda nije
ista mašina kao browser kasirke — `PfrUrl` je **jedno polje po firmi** (`Firma.PfrUrl`), ne po
terminalu/sesiji. To znači: da bi Web Kasa fiskalizovala preko L-PFR-a, `ERPiApi` proces mora
imati mrežni pristup baš do tog jednog L-PFR uređaja — realno je to slučaj kad `ERPiApi` radi na
istoj mašini/LAN-u kao L-PFR (današnji model: jedna instalacija `ERPiApi_<šifra>` po firmi,
tipično u istoj prodavnici).

### Redosled je već ispravan i mora ostati takav

`PosService`: snimi dokument (nefiskalizovan) → **fiskalizuj** → tek ako uspe, knjiži (razduži
robu, napravi nalog). Fiskalizacija koja padne ostavlja dokument čekajući, ne razdužuje robu bez
PFR broja. Bilo koji offline mehanizam mora poštovati isti redosled — ne sme knjižiti pre
potvrđene fiskalizacije.

### Šta je već simulirano (razvoj bez opreme)

`PfrSimulatorServer.cs` — pravi lokalni HTTP server na proizvoljnom portu, govori isti v3
protokol, odbija loše zahteve kao pravi PFR. Postojeći scenariji (i testovi) mogu se koristiti
i za offline-scenario testiranje (npr. gasiti/paliti simulator da se simulira nestanak PFR-a).

## 2. Šta „offline" stvarno može da znači ovde

Tri različita uzroka prekida, sa različitim posledicama:

1. **Browser ↔ `ERPiApi` prekinut** (WiFi/LAN hiccup na terminalu, `ERPiApi` proces pao) —
   PFR/L-PFR i dalje rade, samo ih Web Kasa trenutno ne može dozvati. Ovo je **jedini scenario
   gde PWA offline red ima smisla** — sve se može bezbedno queue-ovati u browseru i poslati kad
   se `ERPiApi` vrati, JER fiskalizacija se i dalje dešava na serveru čim zahtev stigne (redosled
   iz §1 ostaje netaknut, ništa se ne fiskalizuje u browseru).
2. **`ERPiApi` ↔ PFR prekinut, firma je na L-PFR** — L-PFR je dizajniran da radi i bez interneta
   ka Poreskoj upravi, ali ako je **sam L-PFR ugašen/nedostupan** (npr. restartovan uređaj), ni
   WPF ni Web ne mogu da fiskalizuju dok se ne vrati — to nije nešto što aplikacija treba da
   zaobiđe (izdavanje fiskalnog računa BEZ PFR potpisa nije zakonski dozvoljeno ni na WPF-u danas).
   Jedina razlika koju app može ponuditi je **UX**: red čekajućih prodaja na terminalu (browser),
   sa jasnom porukom „čeka fiskalizaciju", umesto da se prodavac blokira ili gubi unetu korpu.
3. **`ERPiApi` ↔ PFR prekinut, firma je na V-PFR** — nema šta app da uradi, V-PFR **traži stalnu
   vezu ka internetu** po dizajnu PU-a. Isto važi i za WPF danas — ovo nije regresija Web Kase,
   nego zakonsko ograničenje V-PFR režima.

**Zaključak koji menja obim zadatka:** „offline fiskalizacija" ne postoji kao mogućnost koju
ERPi može dodati — fiskalizacija je uvek ili trenutno moguća (PFR dostupan) ili nije (i tada ni
WPF ne može ništa). Ono što PWA *stvarno* može doneti je **otpornost terminala na mrežni hiccup
između kasirkinog uređaja i `ERPiApi`-ja** (scenario 1), plus **korisno čekanje** kad je PFR sâm
nedostupan (scenario 2), ne novi fiskalni mehanizam.

## 3. Predložena varijanta — red čekanja u browseru, fiskalizacija ostaje isključivo na serveru

Nema alternativnih „gde se fiskalizuje" varijanti — §2 pokazuje da fiskalizacija u browseru
(offline, bez PFR-a) nikad nije opcija. Predlog je za MEHANIZAM reda čekanja:

1. **Service Worker + IndexedDB na `KasaTab`** — svaki `zakljuciRacun()` poziv koji ne uspe zbog
   mrežne greške (ne poslovne — razlikovati "fetch failed" od 4xx/5xx sa telom) upada u lokalni
   red (`PosStavka`/korpa/plaćanja, sve što je već u `KasaPlacanjeModal` state-u pre slanja).
   Korpa i UI se NE brišu — kasirka nastavlja da radi na sledećoj prodaji dok prva čeka.
2. **Retry** — Background Sync API gde postoji (Chrome/Edge desktop i Android), inače prost
   `setInterval`/`online` event fallback (Safari nema Background Sync) koji ponovo pokušava
   `zakljuciRacun()` za svaku stavku u redu, po redosledu unosa (FIFO — bitno za ESIR redne
   brojeve).
3. **Status po redu** — svaka čekajuća prodaja u UI nosi značku „Čeka fiskalizaciju" dok ne
   dobije PFR broj; kasirka može da vidi red (koliko čeka, od kada), ne samo da nagađa da li je
   pošlo.
4. **Idempotentnost** — `zakljuciRacun()` mora nositi klijentski generisan `RequestId`
   (`PfrSimulatorServer` ga već prepoznaje — vidi §1) da ponovljeni retry posle spore/izgubljene
   potvrde ne izda DVA fiskalna računa za istu prodaju.
5. **Eksplicitna granica poruke prema kasirki**: kad PFR/L-PFR sâm nije dostupan (scenario 2 iz
   §2), red čekanja raste ali se NIKAD ne prikazuje kao „prodato" — mora biti vizuelno drugačije
   od uspešne fiskalizacije, da se ne pomeša sa gotovom prodajom (rizik: kasirka pusti kupca sa
   robom misleći da je račun izdat).

**Namerno van obima:** offline rad **magacina/artikala/cena** (PWA cache kataloga da kasa uopšte
može da radi bez mreže do trenutka naplate) — to je poseban, širi problem (stale cene/zalihe u
kešu) i nije specifičan za fiskalizaciju; ako se traži, zaslužuje sopstveni dizajn-korak.

## 4. Testiranje (kad/ako se odobri)

- Vitest: red čekanja u IndexedDB (mock), FIFO redosled, idempotentni `RequestId` na retry,
  razlikovanje mrežne greške od poslovnog odbijanja (403/409 se NE stavljaju u red — kasirka mora
  odmah da vidi da je nešto pogrešno uneto, ne da čeka mrežu koja se neće popraviti).
- `web-screens-pass`/ručna vožnja: ugasiti `PfrSimulatorServer` usred prodaje → red čeka →
  upaliti simulator → prodaja se fiskalizuje sama, značka se menja.
- Ne treba novi backend test — `PosService`/`PfrService` se ne menjaju, samo klijent koji ih
  poziva kasnije/ponovo.

## 5. Odluke korisnika (13.09.2026)

1. **Topologija:** danas nijedan klijent nema `ERPiApi` na drugoj mašini od kasirkinog terminala
   — sve je na istoj mašini (isti model kao WPF danas). Ovo **slabi** motivaciju za scenario 1 iz
   §2 (browser↔`ERPiApi` hiccup je redak kad je poziv čist `localhost`), ali obim ipak ide šire —
   vidi tačku 2. Dizajn u §3/§6 ostaje generičan (radi i kad `ERPiApi` nije na istoj mašini), jer
   ništa u njemu ne pretpostavlja `localhost`.
2. **Obim:** korisnik traži i **puni offline katalog/cene/zalihe** (šire od §3 „namerno van
   obima"), ne samo zaštitu prodaje u toku. Ovo otvara novu, težu celinu — §6.

**Napomena o tenziji između 1. i 2.:** ako je `ERPiApi` uvek na istoj mašini kao browser, poziv
ide preko `localhost` i praktično nikad ne „vidi" spoljni internet koji nestane — mašina bi
morala izgubiti sopstvenu mrežnu petlju (retko: pad `ERPiApi` procesa/servisa, restart mašine)
da bi offline scenario uopšte nastupio. Puna vrednost punog offline kataloga dolazi tek ako se
proizvod pomeri ka **tablet/mobilnom terminalu preko WiFi-ja** koji NIJE ista mašina kao
`ERPiApi` (isti pravac kao odloženi 11.I „mobilni interfejs za terensku komercijalu") — vredi
imati to na umu kao pravi motiv, ne graditi kao apstraktnu vežbu. Dizajn ispod važi za oba slučaja
bez izmene.

## 6. Puni offline katalog/cene/zalihe — nova celina, veći zahvat

Ovo NIJE isto što i red čekanja iz §3 (koji samo čuva već unetu prodaju). Ovde Kasa mora moći da
**otvori korpu i traži artikal po šifri/barkodu/nazivu, sa ispravnom cenom i stanjem, bez ijednog
poziva ka `ERPiApi`-ju**, sve dok se ne dođe do naplate.

### Odluke korisnika (13.09.2026) — **ispravljeno** (korisnik je promenio prvi odgovor u istoj poruci)

1. **Prihvatljiva starost keša — minuti, ne sati.** Očekivani offline period je kratak (WiFi
   hiccup, ne planiran prekid), pa keš brzo zastareva: upozorenje već posle **~30 min** bez
   sinhronizacije („cene/zalihe mogu biti zastarele"). Ovo je stroža granica od prvobitno
   razmatranih „sati" — implementacija treba konfigurabilan prag (podešavanje, ne konstanta u
   kodu), jer 30 min je razuman podrazumevani, ne nužno univerzalan za svaku firmu. Tvrda blokada
   posle isteka duže granice (predlog: ~2h, da odluči korisnik pri implementaciji — nije ovde
   fiksirano brojem) sprečava rad na potpuno zastarelim podacima ako se WiFi hiccup pretvori u
   duži ispad.
2. **Sukob zalihe posle sinhronizacije (negativan rezultat) mora ODMAH da obavesti kasirku** —
   ne tih izveštaj za magacionera naknadno, nego vidljivo upozorenje na terminalu čim se veza
   vrati i sinhronizacija otkrije preklapanje. Prirodan mehanizam: postojeći `ErpiLiveHub`/
   `ErpiLiveNotifier` (isti obrazac kao §131 „live lager" event `stanjeZalihe`) — kad merge posle
   offline perioda proizvede negativnu zalihu, terminal koji je bio offline (i svaki drugi otvoren
   na tom magacinu) dobija push, ne samo pasivan red u izveštaju.
3. **Posledica na obim keša:** kratak očekivan offline prozor (minuti) i dalje ne znači uzak
   katalog — kasirka u tih 30 minuta i dalje mora moći da proda BILO ŠTA iz redovne ponude, ne
   samo skoro prodavano. Puni snapshot asortimana ostaje ispravan obim, samo se osvežava češće
   (kraći interval pozadinske sinhronizacije nego kod „sati" varijante).

### Šta danas postoji, provereno u kodu

- `ERPiWebShop/public/sw.js` (postojeći storefront service worker) **eksplicitno preskače svaki
  `/api/` poziv** („uvek idu na mrežu") i keš-uje samo statičku ljusku (`/`, `/index.html`,
  manifest) — nema keširanja podataka nigde u repou danas. Puni offline katalog je nova
  infrastruktura, ne proširenje postojećeg.
- `KasaTab` danas čita artikle/cene/zalihe direktno preko `/api/Kasa/...` po svakom pretragom
  unosu — nema lokalnog snapshot-a asortimana.

### Predložen mehanizam

1. **IndexedDB snapshot asortimana** — periodičan pun/inkrementalni preuzimanje (šifra, naziv,
   barkod, prodajna cena po cenovniku magacina, PDV stopa, trenutno stanje) u pozadini dok je
   veza dobra; `KasaTab` pretraga prvo gleda IndexedDB, ne mrežu, kad je offline.
2. **Staleness je fundamentalni rizik, ne detalj:** cena i stanje u kešu mogu biti zastareli u
   trenutku offline prodaje. Za **cenu** je posledica blaga (kasirka proda po staroj ceni —
   retko i uglavnom bezopasno, rešivo naknadnom korekcijom naloga ako se dogodi). Za **stanje**
   je posledica ozbiljnija: dve kase (ili kasa + web porudžbina) mogu prodati poslednji komad
   istog artikla dok su obe offline — sistem to ne može sprečiti u trenutku prodaje, samo
   otkriti pri sinhronizaciji (negativna zaliha posle merge-a, sličan slučaj kao već poznati
   §67 nalaz o negativnim zalihama na demo bazi).
3. **Sinhronizacija pri povratku veze, istim redom čekanja iz §3** — svaka offline prodaja se šalje
   `zakljuciRacun()` istim putem, PFR fiskalizuje tek tada (fiskalizacija je i dalje uvek
   online-only, §2 se ne menja). Sukob zalihe (rezultat < 0 posle knjiženja) ide preko
   `ErpiLiveNotifier` kao push kasirki, po odluci ispod — ne tih zapis.
4. **Osvežavanje keša** ima eksplicitnu granicu starosti (30 min podrazumevano, konfigurabilno) —
   posle nje Kasa upozorava „cene/zalihe mogu biti zastarele" umesto da tiho nastavi kao da je
   sve sveže; posle tvrde granice (predlog ~2h) blokira dalju offline prodaju.

## 7. Sažetak spreman za implementaciju

Dizajn je fiksiran (§1-§6), spreman za implementacioni korak kad korisnik da zeleno svetlo:

- **§3 (red čekanja za prodaju u toku)** — manji, nezavisan zahvat; može ići prvi, testabilan
  gašenjem/paljenjem `PfrSimulatorServer` usred prodaje.
- **§6 (offline katalog)** — veći zahvat: periodičan IndexedDB snapshot celog aktivnog
  asortimana (šifra/naziv/barkod/cena/PDV/stanje), vidljiv timestamp poslednje sinhronizacije,
  upozorenje na 30 min, blokada na ~2h (oba konfigurabilna), i push kasirki preko
  `ErpiLiveNotifier`/`ErpiLiveHub` (isti obrazac kao §131 „live lager") čim sinhronizacija otkrije
  negativnu zalihu nastalu tokom offline perioda.
- **Fiskalizacija se nigde ne menja** (§1/§2) — offline period samo odlaže poziv
  `zakljuciRacun()`, nikad je ne zaobilazi niti izdaje račun bez PFR potpisa.

Isti obrazac kao `docs/DIZAJN_SIGNALR.md` i `docs/DIZAJN_MULTI_TENANT.md` — dizajn se fiksira pre
koda, ne menja se ad-hoc usred implementacije.

## 8. Implementirano (13.09.2026, §140/§141) — §3 i §6 oba zatvorena

Obe celine iz §7 su urađene. Jedna pretpostavka iz §6 se pri implementaciji pokazala netačnom i
ispravljena je u kodu drugačije nego što je pisalo gore — tekst §6 je ostavljen nepromenjen kao
trag odluke, ovde je šta stvarno važi:

**`KnjiziRacunAsync` ODBIJA knjiženje kad zaliha ne pokriva prodaju** ("Nedovoljno stanje..."),
suprotno pretpostavci u §6 da zaliha može "otići u minus" i da se to otkriva posle-fakta. Zaliha
zato ne može stvarno pasti ispod nule kroz normalan tok. Prava (i opasnija) situacija koju offline
red čekanja uvodi: **račun je već fiskalizovan kod PFR-a (pravno obavezujuć, ne može se
poništiti) ali NIJE knjižen** — kupac ima fiskalni račun za robu koja fizički ne postoji, jer ju
je neko drugi prodao dok je ova čekala u redu. `PosService.ZakljuciRacunAsync` je to oduvek vraćao
kao `Uspeh=true, Proknjizen=false` (posle §140 fiks-a, dostupno i za retry), samo do sada nije
imalo push mehanizam. Event je preimenovan iz `kasaNegativnaZaliha` u **`kasaKnjizenjeNijeUspelo`**
(`KasaKnjizenjeNijeUspeloEventDto`: MagacinId/RacunOtpremnicaId/BrojRacuna/Poruka) i okida se za
SVAKU prodaju (ne samo offline-replay) kad je `Uspeh && !Proknjizen && !JeObuka` — obuka se
namerno isključuje jer je tamo "nije knjiženo" očekivano ponašanje, ne sukob.

**Otkriven usput, ispravljen u istom zahvatu:** `KasaTab.flushRed()` je posle uspešnog HTTP poziva
(bez izuzetka) BEZUSLOVNO brisao prodaju iz reda, ne gledajući `ishod.uspeh`/`ishod.proknjizen` —
pošto `KasaController.ZakljuciRacun` uvek vraća HTTP 200 (poslovni ishod je u telu, ne u statusu),
prodaja koja je fiskalizovana-ali-nije-knjižena (ili čak poslovno odbijena) bi tiho nestala iz reda
a da kasirka na TERMINALU KOJI JE POSLAO nikad ne sazna. Popravljeno: `flushRed` čita `ishod` i, kad
nije čist uspeh, dodaje u novo lokalno stanje `problematicneIzReda` (trajan crveni baner na tom
terminalu, ne auto-sakriven) — `ErpiLiveHub` event pokriva SVE OSTALE otvorene terminale/tabove,
ovaj lokalni trag pokriva terminal koji je stvarno poslao zahtev.

### §3 — konačan obim

Sve iz §3 gore ostaje tačno (idempotentnost preko `RacunOtpremnica.KlijentRequestId`, IndexedDB
red `offlineRed.ts`, FIFO flush na `online`/tajmer/Background Sync), plus ispravka `flushRed`-a
iznad.

### §6 — konačan obim

- **Server:** `Firma.KasaOfflineUpozorenjeMinuti`/`KasaOfflineBlokadaMinuti` (pravo podešavanje,
  ne konstanta — korisnikova odluka), migracija `DodajKasaOfflinePragoveNaFirmu` + `EnsureColumn`.
  `PosService.KatalogSnapshotAsync(magacinId)` — pun katalog bez limita (korisnikova odluka), bulk
  razrešavanje cenovnika/poreskih tarifa u memoriji (ne `RazresiCenuIPorezAsync` po artiklu — taj
  radi 1-2 upita PO POZIVU, neupotrebljivo za ceo šifarnik). Endpoint `GET
  api/Kasa/katalog-snapshot`.
- **Web:** `offlineKatalog.ts` (IndexedDB snapshot + lokalna pretraga + `preracunajOffline`, tačan
  port `IzracunajOsnovicuPdvUkupno` formule + `statusStarostiKesa`). `KasaTab` periodično
  sinhronizuje (5 min tajmer + `online` event), pada na lokalni keš za pretragu/`obradiKod`/
  dodavanje u korpu SAMO kad je prava mrežna greška i keš nije `blokiran`; amber/rose baner
  starosti keša.
- **UI za pragove:** samo WPF (`PodesavanjaView` → tab „Kasa", nova kartica „Web Kasa —
  offline rad") — isti obrazac kao sva ostala PFR podešavanja, koja nemaju web formu ni danas
  (`docs/KASA.md`: „Web: nema vidljive forme, samo čitanje `getPfrStatus`"). Dodavanje web forme
  samo za ova dva polja bilo bi nekonzistentno sa tim postojećim obrascem — ako se web forma za PFR
  podešavanja ikad doda, ova dva polja idu u nju istovremeno.
- **Namerno pojednostavljeno offline:** nema parsiranja vagane robe (barkod sa ugrađenom
  težinom/cenom) ni ponude izbora kad ima više pogodaka za isti kod — tačan barkod/šifra,
  količina 1. Prihvatljivo za kratak offline prozor (odluka 13.09.2026: minuti, ne sati).

### Testovi

5 xUnit (`KatalogSnapshotAsync` — cena/PDV/stanje, magacin-specifičan cenovnik ima prednost,
fallback na `Artikal.ProdajnaCena`, poreska tarifa preglašava, drugi magacin bez kartice → 0), 4
xUnit (`ErpiLiveNotifierTests` — `KasaKnjizenjeNijeUspeloAsync` šalje/guta grešku/poštuje tenant),
2 xUnit (`KasaControllerTests` — pravi push kad je fiskalizovano-nije-knjiženo, NE šalje za
obuku), 18 Vitest (`offlineKatalog.test.ts`), 1 Vitest (`useErpiLiveHub` novi event). `dotnet
test`/`vitest` pun set zelen, build 0/0 (Debug+Release), `tsc`/`eslint` čisto.
