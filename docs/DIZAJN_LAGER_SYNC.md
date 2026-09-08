# 📦 Dizajn — Live lager & SEF status (SignalR v2, 11.K nastavak)

> Dizajn-korak pre koda, isti obrazac kao [`DIZAJN_SIGNALR.md`](DIZAJN_SIGNALR.md) i
> [`DIZAJN_MULTI_TENANT.md`](DIZAJN_MULTI_TENANT.md): fiksira obim, odluke i ono što je namerno van
> njega. `PLAN_SEPTEMBAR_2026.md` stavka **11.K** vodi „pun lager sync" i „SEF-status event" kao
> svesno odložene jer traže baš ovaj korak — izvor promene lagera je WPF, koji ne zove `ERPiApi`.
>
> **Status (08.09.2026): odluke zaključane, ide u kod.**
>
> - Varijanta **A** (pozadinski dif prolaz), interval **10 s** — §3.1 → §131 (`311883d`)
> - Primaoci: **Web Admin + POS kasa** — §3.4
> - **SEF-status poller** — realizovan odmah posle, §132 — §3.5
> - Hub grupa ostaje `"admin"` kao i za postojeće evente; per-tenant grupisanje je zaseban
>   cross-cutting korak za SVE evente — §3.3

## 1. Šta danas postoji (provereno u kodu, ne pretpostavljeno)

### SignalR infrastruktura — stoji od §110, proširena u 11.K.1/3

- **Hub** [`ErpiLiveHub`](../ERPiApi/Hubs/ErpiLiveHub.cs) — prazna klasa, `[Authorize(Roles = "Admin")]`,
  jedna fiksna grupa `"admin"` u `OnConnectedAsync`/`OnDisconnectedAsync`. Server nikad ne prima
  poruke OD klijenta.
- **Emiter** [`ErpiLiveNotifier`](../ERPiApi/Services/ErpiLiveNotifier.cs) — Singleton, tanak omotač
  oko `IHubContext<ErpiLiveHub>`, **best-effort** (greška se guta, kao
  `WebPorudzbinaObavestenjaService`). Danas dva eventa: `novaPorudzbina`, `statusPorudzbine`.
- **Frontend** [`useErpiLiveHub`](../ERPiWebShop/src/hooks/useErpiLiveHub.ts) — kačen **samo** u
  [`AdminPanel.tsx:609`](../ERPiWebShop/src/components/AdminPanel.tsx#L609) na `jePunAdmin`.
  Tenant-aware: `?tenant=<šifra>` na hub URL-u + `X-Tenant-Id` header (WebSocket ne nosi header).
  `withAutomaticReconnect()`, čisto aditivan — bez konekcije Web Admin radi identično (fetch-na-zahtev).
- **Program.cs**: `AddSignalR()`, `AddSingleton<ErpiLiveNotifier>()`, `MapHub("/hubs/erpi-live")`,
  JWT-preko-query-stringa grana u `JwtBearerEvents` ograničena na `/hubs`.

### Lager — jedna append-only knjiga, tekuće stanje = poslednji red

- [`MaterijalnaKartica`](../ERPiData/Models/Magacin/Materijal.cs) je **append-only knjigovodstvena
  traka** (jedan red po promeni), ključ `SifraMagacina` + `SifraArtikla` (string, kao u legacy
  izvoru). Tekuće stanje = `Stanje` poslednjeg reda po (magacin, artikal).
- [`MaterijalnaKarticaService`](../ERPiData/Services/MaterijalnaKarticaService.cs):
  - `DodajUlazRedAsync` / `DodajIzlazRedAsync` — jedini upis u knjigu (+ `UkloniPoslednjiRedAsync` za rasknjiženje).
  - `GetRaspolozivoZaWebAsync(sifre, sifraMagacina)` = stanje na kartici − rezervacije u
    neispunjenim web porudžbinama (`GetRezervisanoUNeobradjenimPorudzbinamaAsync`). **Ovo je broj
    koji katalog prikazuje.**
- [`KatalogController`](../ERPiApi/Controllers/KatalogController.cs) računa `raspolozivoStanje` po
  artiklu na **svaki** zahtev liste/detalja (nije keširano na serveru; frontend ima React Query
  `staleTime`).
- **Lager se menja iz OBE strane procesa:**

  | Put promene | Proces | Servis |
  |---|---|---|
  | Kalkulacija (VP/MP, uvozna), nivelacija | WPF | `KalkulacijaService`, `MaloprodajnaKalkulacijaService`, `NivelacijaService` |
  | Ulaz / trebovanje / primopredaja (materijalno) | WPF | `UlazService`, `TrebovanjeService`, `PrimopredajaService` |
  | Popis, robno kretanje, proizvodnja (gotov proizvod / utrošak) | WPF | `PopisRobeService`, `RobnoKretanjeService`, `ProizvodnjaKnjizenjeService` |
  | Knjiženje računa-otpremnice (izlaz) | WPF *i* API | `RacunOtpremnicaService` |
  | POS kasa (maloprodaja) | API *i* WPF | `PosService` |
  | Web porudžbina → faktura → knjiženje | API | `WebPorudzbinaFakturisanjeService` + `RacunOtpremnicaService` |
  | Komisiona odjava | API *i* WPF | `KomisionoPoslovanjeService` |

  **WPF ne zove `ERPiApi` ni za šta** — deli samo isti SQLite fajl / server bazu.

### Postojeći presedan #1 — „prati ishod, ne callsite"

[`ObavestenjaOZalihiService`](../ERPiData/Services/ObavestenjaOZalihiService.cs) +
[`ObavestenjaOZalihiBackgroundService`](../ERPiApi/Services/ObavestenjaOZalihiBackgroundService.cs)
(back-in-stock email). Doc komentar na servisu je izričit:

> „Zalihu diže više putanja, ne samo kalkulacija: ulaz robe, uvozna kalkulacija, nivelacija, povrat
> od kupca, prenos između magacina, gotov proizvod iz proizvodnje, pa i otkazivanje web porudžbine…
> Okidač zakačen na jedno od tih mesta ćutao bi na ostalima. Zato se u svakom prolazu gleda ISTA
> raspoloživost koju vidi i katalog."

Prolaz je `BackgroundService`, `PeriodicTimer` 10 min, `PocetnoOdlaganje` 3 min, tenant-aware
petljom kroz `TenantRegistryService.Svi()` sa scope-om po firmi. **Ovo je gotov šablon za lager sync.**

### Postojeći presedan #2 — cross-cutting write hook

[`ErpiDbContext.SaveChanges`](../ERPiData/ErpiDbContext.cs#L575) override zove `OsveziNazivePretrage`,
`OsveziRowVerzije`, `ProveriZakljuceneGodine`; plus
[`AuditSaveChangesInterceptor`](../ERPiData/Services/AuditSaveChangesInterceptor.cs). Obrazloženje
(ponovljeno na sva tri mesta): *„radnje ulaze sa desetak strana (WPF ekrani, web kontroleri,
servisi, uvoz) — provera po pozivaocu bi pre ili kasnije negde nedostajala."* Interceptor je
**namerno uzak** (6 šifarnika) — `AuditLog` klasni komentar izričito ostavlja transakcione dokumente
Robnog/Materijalnog za „širi, noviji obim koji bi trebalo posebno razmotriti".

### SEF status — samo na zahtev, nema poller

[`SefService.OsveziStatusNaSefuAsync`](../ERPiData/Services/SefService.cs#L120) (jedan račun) i
`OsveziStatuseAsync` (batch, ima `pauzaMs` jer SEF ograničava učestalost) — oba se pozivaju **samo
iz dugmeta** u SEF/Izvodi tabu. Nema pozadinskog pollera. Status se drži u
`RacunOtpremnica.SefStatus` (`SefStatusFakture` enum). SEF API ne vraća stabilan enum kod — status
se izvodi poređenjem ključnih reči (`approved`/`odobren`…).

### Background servisi već registrovani

`NapusteneKorpeBackgroundService`, `ObavestenjaOZalihiBackgroundService`, `PretplataBackgroundService`
— svi `BackgroundService`, svi tenant-aware istim obrascem.

## 2. Šta se traži (iz `PLAN_SEPTEMBAR_2026.md` 11.K)

1. **`StanjeZalihaIzmenjeno`** (artikal, magacin, novo stanje) → Web Admin i POS kasa osveže
   prikazane zalihe bez ručnog refresh-a.
2. **`SefFakturaStatusPromenjen`** (broj fakture, nov status) → Web Admin.

Plan pominje i B2C prodavnicu kao primaoca lagera — vidi §5 (van v2 obima).

## 3. Odluke

### 3.1 Kako promena lagera stiže do API procesa — TRI VARIJANTE

Ovo je jedina prava dizajn-dilema. Sve tri rešavaju „WPF proknjižio nešto, API to nije video".
**Izabrana je Varijanta A** (08.09.2026) — obrazloženje ispod tabela.

---

#### Varijanta A — pozadinski prolaz koji dif-uje raspoloživost ✅ *(izabrano)*

Nov `LagerSyncBackgroundService` (`BackgroundService`), interval ~10 s. Svaki tik:
`GetRaspolozivoZaWebAsync` za sve `PrikaziNaWebu` artikle → poredi sa snapshot-om u memoriji procesa
→ za svaki promenjen artikal `ErpiLiveNotifier.StanjeZalihaIzmenjenoAsync(dto)`.

**Doslovno isti obrazac koji `ObavestenjaOZalihiBackgroundService` već vrti u produkciji**, samo
kraći interval i „dif" umesto „šalji email".

| ✅ Za | ❌ Protiv |
|---|---|
| Nula šeme, nula migracije, nula okidača — **nula rizika po zatečene baze** | Latencija = interval (~10 s), ne trenutno |
| Hvata SVE puteve promene (WPF + API) jer gleda ishod, ne callsite | Upit `O(svi web artikli)` na svaki tik — jedan `GROUP BY` prolaz kroz `MaterijalneKartice` + `WebPorudzbineStavke`; jeftino na 10 s, osetno na 2 s |
| Radi identično na SQLite / Postgres / MSSQL | Snapshot je per-proces u memoriji: posle restarta API-ja prvi tik samo napuni snapshot (ne emituje) — ne gubi tačnost, preskoči jedno „zvono" |
| Dokazani obrazac, minimalna površina za bag | Ne zna „koji dokument" je promenio stanje — samo „novo stanje je X" |
| Nadogradiv na Varijantu B kasnije bez bacanja koda | — |

---

#### Varijanta B — outbox red koji `ErpiDbContext` upiše u istoj transakciji + API pumpa

`SaveChanges` hook (zaseban, **ne** dopuna `AuditSaveChangesInterceptor`-a) detektuje `Added`
`MaterijalnaKartica` redove → upiše kompaktan `LiveOutbox` red (`Tip`, `SifraMagacina`,
`SifraArtikla`, `Vreme`) u ISTOJ transakciji. Nov `LiveOutboxPumpService` (`BackgroundService`) u
API-ju čita neobrađene redove ~2 s, emituje SignalR, briše ih.

| ✅ Za | ❌ Protiv |
|---|---|
| Niža latencija (~2 s), i red može nositi „koji dokument" | **Nova tabela → migracija + `EnsureLiveOutboxTable`** (dve staze šeme, `erpi-schema-and-migrations`) |
| Transakciono tačno — outbox red i promena kartice padnu/prođu zajedno | Čišćenje starih redova (brisanje u pumpi ili zaseban posao) |
| Preživljava restart API-ja (redovi čekaju u bazi) | `MaterijalnaKartica` je high-frequency traka — `AuditSaveChangesInterceptor` je taj obim **svesno izbegao**; ovde ga uvodimo |
| Hvata sve puteve automatski (hook je u deljenom `ErpiDbContext`) | WPF sad piše red čija je jedina svrha da API nešto uradi — veza između procesa kroz tabelu (doduše, to outbox obrazac i jeste) |

---

#### Varijanta C — WPF postaje SignalR klijent / pozivač

**Odbačeno**, isti razlog kao [`DIZAJN_SIGNALR.md`](DIZAJN_SIGNALR.md) §5: WPF ne zna da li `ERPiApi`
za tu firmu uopšte radi ni na kom portu (danas taj podatak ne postoji na desktop strani — WPF
markira bazu, API je otvara). Marker fajl bi morao da nosi port; menja WPF iz „nezavisan proces" u
„zavisi od API-ja" zbog kozmetike. Ne za ovaj korak.

---

**Izabrano: Varijanta A za v2.** Obrazac je već u produkciji i dokazan, nema dodira sa šemom, a
~10 s zaostatak je i dalje „live" naspram današnjeg „osveži ručno / na sledeći fetch". Ako se u
praksi pokaže potreba za <3 s i „koji dokument", Varijanta B je čist nadogradni korak (dodaš outbox
i pumpu, a prolaz ostaje kao rezerva).

### 3.2 Obim emitovanja

- **Samo `PrikaziNaWebu` artikli** — isti skup koji `ObavestenjaOZalihi` već računa. Admin lager
  pregled celog magacina van web kataloga ostaje na „Osveži" (danas je isto — nije regresija).
- **Svaka promena brojke** raspoloživosti okida event; dedup je u snapshotu (ne emituj ako je
  `raspolozivo == poslednjeViđeno` za taj artikal).
- **Magacin:** event nosi `sifraMagacina` iz `WebShopMagacinService.GetWebShopSifraMagacinaAsync`
  (magacin iz kog WebShop prodaje) — isti onaj koji `GetRaspolozivoZaWebAsync` već koristi. Kad je
  `null` (magacin nije podešen), prolaz gleda zbir svih magacina, isto kao `GetRaspolozivoZaWebAsync`.

### 3.3 Grupe i multi-tenant

**Odluka (08.09.2026): lager event ide u istu grupu `"admin"` kao i postojeći `novaPorudzbina` /
`statusPorudzbine`.**

- **Van `--tenants`** (svi klijenti danas): grupa `"admin"`, proces je per-firm — 100% tačno.
- **U `--tenants`:** pozadinski prolaz svejedno petlja kroz `TenantRegistryService.Svi()` (mora — da
  otvori bazu svake firme), ali emituje u zajedničku grupu `"admin"`. To znači da bi admin firme A
  video „zvono" za promenu lagera firme B. **Isto važi i za postojeće evente** — `ErpiLiveNotifier`
  je Singleton koji šalje u `Clients.Group("admin")` procesno, bez tenant konteksta.
- **Per-tenant hub grupisanje** (`$"tenant-{sifra}-admin"`, `ErpiLiveHub.OnConnectedAsync` čita
  `JwtService.ClaimTenantSifra` claim) je **zaseban cross-cutting korak koji pokriva SVE evente
  odjednom** — ne uvoditi ga samo za lager (napravilo bi nedoslednost). `DIZAJN_SIGNALR.md` §5 i
  `DIZAJN_MULTI_TENANT.md` §2 su ga već ostavili po strani; ostaje tamo. Nizak prioritet dok
  `--tenants` niko ne koristi produkciono.

### 3.4 Frontend prijem — lager

- `useErpiLiveHub` dobija handler `naStanjeZalihe?: (dto: StanjeZaliheEvent) => void` +
  `connection.on('stanjeZalihe', …)`.
- **Web Admin** ([`AdminPanel.tsx`](../ERPiWebShop/src/components/AdminPanel.tsx#L609)): handler
  poziva `setOsveziSignal(n => n + 1)` — **isti mehanizam kao dugme „Osveži"** i kao reakcija na
  `novaPorudzbina`. Svaki otvoren `useUcitavanje` tab (magacin lager pregledi:
  [`RobneKarticePodTab`](../ERPiWebShop/src/components/admin/magacin/RobneKarticePodTab.tsx),
  `MaterijalnoTab`) se sam re-fetuje jer drži `osveziSignal` u zavisnostima. Tab koji nije otvoren
  ne radi ništa. React 18 batching skuplja rafal event-ova iz jednog prolaza u jedan re-render.
- **POS kasa web** ([`KasaTab.tsx`](../ERPiWebShop/src/components/admin/kasa/KasaTab.tsx)): `KasaTab`
  ne koristi `useUcitavanje`, pa čita `osveziSignal` iz `useAdmin()` konteksta i dodaje ga u
  zavisnosti svog „živi predlozi" `useEffect`-a — na event se tekuća pretraga ponovo izvrši i
  `p.stanje` / `p.nemaNaStanju` se osveže. (Pretraga i inače ide sveže na svaki otkucaj; ovo pokriva
  prozor dok predlozi stoje otvoreni a kasirka ne kuca.)
- **Bez Toast-a za lager** — jedan prolaz posle bulk kalkulacije okine desetine artikala; Toast bi
  bio spam. Samo tiho osvežavanje prikaza. (Suprotno od porudžbine, gde je Toast poenta.)

### 3.5 SEF status — pozadinski poller ✅ *(realizovano, §132)*

- `SefStatusPollerBackgroundService` (`BackgroundService`, tenant-aware), interval **10 min**,
  `PocetnoOdlaganje` 2 min. Isti tenant plumbing kao `LagerSyncBackgroundService`.
- Prekidač: radi samo ako `Firma.SefApiKey` postoji (isti obrazac kao `ObavestenjaOZalihiOmogucena`).
- `SefStatusSyncService` (`ERPiData`): `KandidatiAsync` (`RacuniOtpremnice` sa `SefId != null` i
  `SefStatus == Poslata`), `ProzoviIVratiPromeneAsync` (zove `SefService.OsveziStatuseAsync` —
  koji sam upisuje nov status — pa pročita koje su faktura izašle iz `Poslata`). Kandidati su po
  definiciji `Poslata`, pa je stari status uvek `Poslata`.
- Za svaku promenu `ErpiLiveNotifier.SefStatusPromenjenAsync` → event `"sefStatus"` u grupu `"admin"`.
- Frontend: `useErpiLiveHub` handler `naSefStatus`, `AdminPanel` → `osveziSignal` **+ Toast**
  („🧾 Faktura X → SEF: Odobrena") — ovde Toast IMA smisla (retko, bitno).
- Testovi: `SefStatusSyncServiceTests` (5, `LazniSefHandler`), `ErpiLiveNotifierTests` (+2),
  `useErpiLiveHub.test.ts` (+1).

### 3.6 `ErpiLiveNotifier` / DTO

Jedan nov metod na `ErpiLiveNotifier` za v2 (`StanjeZaliheAsync`, best-effort `try/catch` kao
postojeći), event `"stanjeZalihe"` u grupu `"admin"`. Jedan `record` DTO u
[`WebShopDtos.cs`](../ERPiApi/DTOs/WebShopDtos.cs) (camelCase serijalizacija), jedan `connection.on`
na frontendu + tip u `useErpiLiveHub.ts` (isti obrazac kao `StatusPorudzbineEvent`).

**Jedan event po prolazu nosi listu svih promena** (ne N pojedinačnih poruka) — manje chatter-a,
jedan `connection.on` poziv, jedan `osveziSignal` bump:

```text
StanjeZaliheEventDto   { sifraMagacina, promene: StanjeZaliheStavkaDto[], vreme }
StanjeZaliheStavkaDto  { sifraArtikla, nazivArtikla, raspolozivo }
```

SEF-status DTO (`SefStatusEventDto { racunOtpremnicaId, brojRacuna, stariStatus, noviStatus }`) i
`SefStatusPromenjenAsync` metod dolaze u zasebnom kasnijem koraku (§3.5).

## 4. Testiranje (pre koda, da se zna šta zaključava ponašanje)

- **`LagerSyncService`** (diff logika, `ERPiData`) — `ERPiData.Tests/LagerSyncServiceTests.cs`, isti
  in-memory obrazac kao `ObavestenjaOZalihiTests` (deli `MaterijalnaKarticaService` put): napravi
  kartica-stanje, `SnimiRaspolozivostAsync` → snimak → proknjiži izlaz preko
  `MaterijalnaKarticaService.DodajIzlazRedAsync` → drugi `SnimiRaspolozivostAsync` → `Uporedi(staro, novo)`
  vraća tačno taj jedan artikal, ćuti za nepromenjene i za artikle van `PrikaziNaWebu`; `ZigAsync`
  se menja/ne-menja sa novim redom. 8 testova. Probijeni (privremeno uklonjen dedup) → 3 testa padnu
  → nisu vacuous.
- **`ErpiLiveNotifier.StanjeZaliheAsync`** — dopuna `ErpiLiveNotifierTests.cs` (+2): šalje
  `"stanjeZalihe"` u grupu `"admin"` sa tačnim payload-om; guta grešku (best-effort).
- **Frontend** `useErpiLiveHub.test.ts` (+1) — nov handler `naStanjeZalihe`, isti mock
  `@microsoft/signalr` obrazac kao postojećih 7 testova.
- **E2E / vizuelno** — proširiti skill [`erpi-signalr-live-check`](../.claude/skills/erpi-signalr-live-check/)
  ili nov scenario: dva admin taba, treća strana (skript) knjiži izlaz kroz
  `MaterijalnaKarticaService.DodajIzlazRedAsync` nad izolovanom kopijom `DEMO.db`, oba taba dobiju
  osveženo stanje u < (interval + 1 s), bez ručnog refresh-a. Zapis u `docs/E2E_TESTIRANJE.md`.
  **Env promenljive `API_BASE`/`WEB_BASE` MORAju biti eksplicitne** (5002/5174) — podrazumevane
  5000/5173 su portovi pravog instaliranog servisa (v. `SKILL.md` `[!WARNING]`).

## 5. Namerno van obima v2 (upisano da se ne zaboravi)

- **B2C prodavnica kao primalac.** Storefront je anoniman — nije na hubu (`Roles=Admin`). Katalog
  ima `staleTime` keš (§106–109), „osveži na sledeći fetch" je prihvatljivo. Ako se traži: zaseban
  **anoniman** hub ili grupa sa samo grubim „nešto se promenilo, povuci ponovo" signalom bez detalja
  o stanju/ceni. Eksplicitan zahtev, kao B2B u `DIZAJN_SIGNALR.md` §5.
- **„Koji dokument" je promenio lager** — Varijanta B nadogradnja.
- **WPF kao primalac** lager/status eventa — WPF ima svoj 60 s DB-poll (`MainWindow.xaml.cs`),
  isti razlog kao SignalR §5.
- **Prag-throttling emitovanja** (npr. samo prelazak 0 ⇄ >0) — uvesti tek ako se u praksi pokaže da
  je „svaka promena brojke" bučno na velikom katalogu.
- **Cena artikla uživo** (ne samo stanje) — nije traženo; ista cev kad zatreba.

## 6. Odluke (bile otvorena pitanja — potvrđeno 08.09.2026)

1. **Interval za lager prolaz — 10 s.** `PeriodicTimer(10s)`, `PocetnoOdlaganje` ~1 min (kraće od
   `ObavestenjaOZalihi` 3 min jer je ovo „live", ali dovoljno da ne udari u startup migracije).
2. **SEF status poller — realizovan u §132** (drugi PR, odmah posle lagera). Vidi §3.5.
3. **`--tenants` grupe — ne u ovom PR-u.** Lager prati postojeće evente (grupa `"admin"`, §3.3).
   Per-tenant grupisanje je zaseban cross-cutting zadatak za sve evente.
4. **POS kasa — DA, prima event.** `KasaTab` čita `osveziSignal` iz `useAdmin()` (§3.4).
