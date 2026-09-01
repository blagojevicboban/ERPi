# 🔌 Dizajn — SignalR Live Hub (`/hubs/erpi-live`)

> Dizajn-korak pre koda, po `PLAN_NASTAVKA.md` stavci 9 ("Dugoročno — traži poseban dizajn-korak
> pre razvoja, ne ubacivati direktno kao zadatak"). Ovaj dokument fiksira obim v1, odluke i ono što
> je namerno van njega — kad se odobri, implementacija ide po ovome, ne ad-hoc.

## 1. Šta danas postoji (provereno u kodu, ne pretpostavljeno)

- **Nigde u repou nema SignalR-a** — `grep` na `Hub`/`SignalR`/`WebSocket` pogađa samo prozu u
  `.md`/Help fajlovima, ne kod.
- **WPF već ima svoju zamenu za "push"**, ali potpuno odvojenu od `ERPiApi`:
  [MainWindow.xaml.cs:138](../ERPiApp/Views/Shell/MainWindow.xaml.cs#L138) drži
  `DispatcherTimer` na 60s koji zove `OsveziBrojPristiglihPorudzbinaAsync()` —
  taj metod otvara **sopstveni `ErpiDbContext`** i broji `WebPorudzbine` direktno nad bazom.
  WPF ne zove `ERPiApi` ni danas, ni za ovo ni za bilo šta drugo — potpuno je nezavisan proces koji
  samo deli isti SQLite fajl/server bazu.
- **Web Admin nema ekvivalent** — nula pogodaka za bell/zvuk/live-notifikaciju u
  `ERPiWebShop/src/components/admin`. Lista porudžbina se osvežava samo na akciju korisnika
  (otvaranje taba, `invalidateQueries` posle sopstvene mutacije — vidi §106-109 React Query rad).
- **Postojeća tačka gde se "nešto se desilo" već emituje na dve strane** (email + SMS):
  `WebPorudzbinaObavestenjaService` (§91, `ERPiData/Services`) — poziva se iz
  `AdminController.PromeniStatusPorudzbine`/`KreirajPosiljku`. Prirodno mesto da se doda i treći
  "kanal" (SignalR) bez nove arhitekture — isti poziv, jedan dodatni red.
- **`ERPiApi` je već per-firm proces** (`ERPiApi_<šifra>` Windows servis, svoj port) — hub je time
  *već* prirodno izolovan po firmi, bez ikakvog dodirivanja sa otvorenim multi-tenant pitanjem
  (`PLAN_NASTAVKA.md` stavka 10). Ne treba posebna zaštita za to ovde.
- **JWT bearer je jedini auth danas** ([Program.cs:336](../ERPiApi/Program.cs#L336)), sa
  `TokenOpozivValidator.ProveriGeneraciju` da opozvani/izmenjeni nalog ne ostane važeći do isteka
  tokena. Politika `"Osoblje"` (claim `TipNaloga=Osoblje`) razdvaja WebShop kupca od admin/B2B
  osoblja — isti razdvajač treba i hub.

## 2. Obim v1 — namerno usko

**Samo Web Admin, samo jedan događaj: nova web porudžbina.** Razlog za ne-širi obim:

| Kandidat iz arhitekturne liste | Odluka za v1 | Zašto |
|---|---|---|
| Nova web porudžbina → Web Admin | ✅ u obimu | Već postoji hook (`WebPorudzbinaObavestenjaService`), već postoji React Query keš da se samo invalidira, jasna poslovna vrednost (operater vidi porudžbinu bez ručnog refresh-a) |
| Nova web porudžbina → **WPF desktop** | ❌ van v1 | WPF ne zove `ERPiApi` ni za šta danas — dodavanje SignalR klijenta znači nov, hard dependency ("da li API uopšte radi", "na kom portu") za ekran koji već ispravno radi na 60s DB-poll-u. Realan dobitak (59s ranije obaveštenje) ne opravdava novi failure mode. Ostaje otvoreno kao Faza 2 — vidi §5. |
| Promena statusa porudžbine (Poslata/Isporučena...) | ❌ van v1 | Ista cev kao "nova porudžbina" tehnički, ali dodaje grane bez dokazane potrebe — dodati kad v1 pokaže da osnovni kanal radi pouzdano. |
| Live sinhronizacija lagera (ulaz/izlaz robe) | ❌ van v1 | Izvor promene je **WPF** (knjiženje kalkulacije/ulaza), koji — vidi gore — ne zove `ERPiApi`. Da bi web katalog dobio push, WPF bi prvo morao da postane SignalR *klijent ili pozivalac*, što je isti otvoreni problem kao red iznad, samo sa suprotnim smerom podataka. Web katalog danas već ima ispravan `staleTime`-zasnovan keš (§106-109); "osveži na sledeći fetch" je prihvatljivo dok se to pitanje ne reši. |
| SEF status e-Fakture/PFR | ❌ van v1 | SEF status se danas proverava na zahtev (dugme/manuelna sinhronizacija u `SefService`), nema pozadinski poller čiji bi rezultat imao gde da "gurne" event. Uvođenje pozadinskog SEF pollera je zaseban zadatak, ne deo hub-a. |

Ako v1 (nova porudžbina → Web Admin) radi pouzdano u produkciji bar jednu firmu-sezonu, sledeći
kandidat po vrednosti je status porudžbine (isti kanal, nova poruka), ne lager/SEF.

## 3. Arhitektura v1

```
AdminController.PromeniStatusPorudzbine / PorudzbineController (nova porudžbina)
        │
        ▼
WebPorudzbinaObavestenjaService.ObavestiONovojPorudzbini(...)   ← postojeći servis, §91
        │  (nov poziv, dodat pored postojećeg email/SMS)
        ▼
IHubContext<ErpiLiveHub>.Clients.Group($"firma-osoblje").SendAsync("novaPorudzbina", dto)
        │
        ▼
Web Admin: useErpiLiveHub() hook → queryClient.invalidateQueries(['porudzbine'])
                                  → CustomEvent (isti obrazac kao erpi:api-error) → Toast + zvuk
```

- **Hub:** `ERPiApi/Hubs/ErpiLiveHub.cs`, mapiran `/hubs/erpi-live` u `Program.cs`,
  `[Authorize(Roles = "Admin")]` na hub nivou — **ne** politika `"Osoblje"` (ispravka posle provere
  koda): ta politika je uža, rezervisana za ESS, i isključuje legacy `WebKorisnik.IsAdmin` admin
  nalog (nema `TipNaloga=Osoblje` claim). `AdminController` (odakle `PorudzbineTab` čita listu) sam
  traži `Roles = "Admin"`, i oba puta prijave koja Web Admin panel uopšte pušta unutra
  (`JwtService.GenerisiToken` za `WebKorisnik.IsAdmin`, `GenerisiTokenZaOsoblje` za
  `Korisnik.PravoAdministracije`) izdaju taj isti role claim — `Roles = "Admin"` na hubu je tačno
  ista publika, ne uža ni šira.
- **Grupa, ne broadcast:** klijenti se pri konekciji dodaju u jednu fiksnu grupu (`"admin"`) —
  dovoljno za v1 jer je proces već per-firm; nema potrebe za per-firm grupisanjem *unutar* procesa.
- **JWT preko query stringa za hub handshake** — WebSocket upgrade iz browsera ne može da nosi
  `Authorization` header, pa SignalR standardno čita `access_token` iz query stringa. Treba dodati
  granu u postojeći `JwtBearerEvents.OnMessageReceived` (danas nema tog handler-a, samo
  `OnTokenValidated`) koja to čita SAMO za putanje koje počinju sa `/hubs/`, ne globalno — inače bi
  token u URL-u (koji ume da završi u access/proxy logovima) postao prihvatljiv svuda, ne samo za
  hub.
- **Klijent:** nov `ERPiWebShop/src/hooks/useErpiLiveHub.ts` — `@microsoft/signalr` (nov paket),
  `withAutomaticReconnect()`. Diskonekcija ne sme ništa da pokvari — hub je čisto aditivan; bez
  konekcije, Web Admin radi tačno kao danas (fetch-na-zahtev).
- **Reakcija na event — ispravka posle provere koda, ne React Query.** `PorudzbineTab.tsx` (gde se
  nova porudžbina stvarno vidi) NIJE na React Query, i dalje koristi `useUcitavanje` (`AdminKontekst.tsx`,
  §106 klasifikacija). Taj hook već UVEK uključuje `osveziSignal` iz `AdminKontekst`-a u svoj
  dependency niz (`AdminKontekst.tsx:137`, bezuslovno — ne treba da ga tab eksplicitno navede), i to
  je TAČNO ono što dugme "Osveži" u zaglavlju danas radi (`AdminPanel.tsx:582`,
  `setOsveziSignal(n => n + 1)`) — uključujući sporedni efekat da isti brojač okida i ponovno
  učitavanje dashboard bedževa (`AdminPanel.tsx:578`). `useErpiLiveHub` zato na "novaPorudzbina"
  event poziva baš tu istu funkciju (prosleđenu kroz kontekst/prop), ne uvodi paralelan mehanizam —
  jedan hub-event = kao da je korisnik kliknuo "Osveži". Toast/zvuk ide kroz isti `erpi:api-error`-stil
  CustomEvent koji `AdminPanel.tsx` već sluša (nov naziv eventa, isti mehanizam) — bez novog UI
  sistema za notifikacije. (React Query `invalidateQueries` ostaje ispravan alat za BUDUĆE hub
  evente koji pogode RQ-konvertovan ekran — ovde prosto nije taj slučaj.)

## 4. Deployment napomena (van koda, ali blokirajuće za self-host klijente)

`docs/ARCHITECTURE.md` §2.3 pominje Docker/Postgres/Caddy kao postojeću server opciju za tehnički
napredniju manjinu klijenata. WebSocket upgrade mora biti eksplicitno propušten kroz svaki reverse
proxy ispred `ERPiApi` (Caddy to radi podrazumevano, Nginx **ne** bez `proxy_set_header Upgrade`/
`Connection` direktiva) — dodati napomenu u `docs/WEBSHOP_HOSTING_GUIDE.md` kad v1 uđe u kod, inače
je hub tih pad (SignalR sam pada nazad na long-polling, radi ali gubi svrhu — "live" postaje opet
sekunde-do-minuta kašnjenje bez ikakve poruke da je proxy kriv).

Za većinu klijenata (Windows Velopack, `localhost`/LAN) ovo ne važi — Kestrel direktno servira,
WebSocket radi bez ičega dodatnog.

## 5. Namerno van obima v1 (upisano da se ne zaboravi, ne da se sad radi)

- **WPF kao klijent huba.** Zahteva da WPF prvo zna da li je `ERPiApi` za tu firmu uopšte pokrenut
  i na kom portu — danas taj podatak ne postoji na desktop strani (WPF ne otvara API, API otvara
  bazu koju WPF markira). Marker fajl (`aktivna_baza.json`) bi morao da nosi i port ako ga
  `ERPiApi` piše nazad, ili WPF mora da pogodi/proba portove — pravi mali dizajn-pod-problem, ne
  rešiti uzgred unutar ovog dokumenta.
- **Status porudžbine, lager, SEF** — vidi tabelu u §2.
- **Multi-tenant grupisanje unutar jednog procesa** — ne postoji dok ne postoji sam multi-tenant
  (`PLAN_NASTAVKA.md` stavka 10); kad taj dizajn krene, hub grupe postaju `firma-{firmaId}` umesto
  fiksnog `"osoblje"`.
- **B2B portal kao primalac** (npr. "vaša porudžbina je isporučena") — ista cev, drugi predznak
  (kupac umesto osoblja); nije tražen ovom analizom, dodati tek na eksplicitan zahtev.

## 6. Testiranje (pre koda, da se zna šta zaključava ponašanje)

- Repo nema presedan za SignalR integracioni test. Hub treba držati **tanak** (samo prosleđuje već
  testiranu logiku iz `WebPorudzbinaObavestenjaService`) — sama poslovna grana (kad se šalje, šta
  nosi DTO) se testira kao i danas, direktnim pozivom servisa, bez huba u putanji.
  - Za sam hub: minimalan test preko `WebApplicationFactory`/`TestServer` +
    `HubConnectionBuilder().WithUrl(..., HttpTransportType.LongPolling)` (LongPolling u testu
    zaobilazi WebSocket-specifične probleme `TestServer`-a) — dokazuje da autentifikovan klijent u
    grupi PRIMI poruku kad servis emituje, i da neautentifikovan klijent dobije odbijenu konekciju.
  - Frontend: `useErpiLiveHub.test.ts` mock-uje `@microsoft/signalr` konekciju, dokazuje da primljen
    event zove `invalidateQueries` sa tačnim `queryKey`-jem (isti obrazac kao `useAdreseIsporuke`
    invalidateQueries test iz §109).
- **Vizuelna provera pre zatvaranja:** dva otvorena admin taba (ili admin + druga sesija) — kreirana
  porudžbina preko checkout toka mora da se pojavi u oba bez ručnog refresh-a, u razumnom roku
  (< 2s na `localhost`).

## 7. Otvoreno pitanje za korisnika pre implementacije

Da li `ERPiApi` u produkciji ide iza reverse proxy-ja kod ijednog stvarnog klijenta danas (ne
buduće Docker/Postgres opcije, nego *danas*)? Ako je odgovor "ne, svi su Velopack na
`localhost`/LAN", §4 napomena postaje čisto buduća, ne blokirajuća za v1.

**Odgovor korisnika (01.09.2026): trenutno niko ne koristi produkciono, sve je test/razvoj.**
§4 (reverse-proxy/WebSocket napomena) ostaje upisana za `docs/WEBSHOP_HOSTING_GUIDE.md` kad se
prvi self-host klijent pojavi, ali **ne blokira v1** — implementacija ide dalje po obimu iz §2-§3.
