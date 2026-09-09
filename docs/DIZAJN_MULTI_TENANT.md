# 🏢 Dizajn — Multi-Tenant `TenantProvider`

> Dizajn-korak pre koda, po `PLAN_NASTAVKA.md` stavci 10 ("Dugoročno — ovo je promena proizvoda,
> ne refaktor: prvo dizajn-dokument, tek onda kod"). Ovaj dokument fiksira obim v1, odluke i ono
> što je namerno van njega — kad se odobri, implementacija ide po ovome, ne ad-hoc.
>
> ## ✅ v1 IMPLEMENTIRAN (04.09.2026, §121)
>
> Korisnik je odgovorio na sva tri otvorena pitanja iz §7: (1) rešava **stvaran, tražen slučaj** —
> ide u kod; (2) **header/path**, ne subdomen; (3) tri pozadinska servisa **moraju od prvog dana da
> rade za sve zakupce** (širi obim od preporuke §5 t.3).
>
> **Dve stvari u ovom dokumentu su se pri implementaciji pokazale netačnim i ispravljene su u kodu
> drugačije nego što piše ispod — tekst §4/§5 je ostavljen nepromenjen kao trag odluke, ovde je
> šta stvarno važi:**
>
> 1. **Provera ukrštanja firmi ide po `Sifra`, NE po `FirmaId`** (suprotno §4 i §5 t.1).
>    `Firma.FirmaId` je autoinkrementni PK *unutar baze te firme*, a svaka baza ima tačno jedan
>    `Firma` red — pa je vrednost praktično uvek `1` u svakoj bazi. Potvrđeno na živom E2E-u: obe
>    test-baze vraćaju `"firmaId": 1`. Poređenje po njemu bi propustilo token firme A na bazu firme
>    B, tj. „glavna odbrana od ukrštanja podataka" bila bi kozmetička. JWT zato nosi claim
>    `TenantSifra`, a `TenantInfo` uopšte nema `FirmaId` (ima `Naziv`, samo za log).
> 2. **Nije upotrebljen `IHttpContextAccessor`** (suprotno skici u §4). Tri pozadinska servisa rade
>    van HTTP zahteva i nemaju `HttpContext`, pa bi fabrika `ErpiDbContext`-a koja čita iz njega
>    pukla baš za njih — a odgovor na pitanje 3 traži da rade za sve zakupce. Umesto toga postoji
>    **scoped `CurrentTenantAccessor`**, koji pune i middleware (HTTP put) i petlja pozadinskog
>    servisa (po zakupcu). Jedan mehanizam za oba puta.
>
> **Uz to, dve tačke koje dokument nije predvideo, a morale su se rešiti:** `AddDbContextCheck`
> (health check nad „tom" bazom) se u tenant režimu ne registruje, jer `/healthz` namerno ide van
> razrešavanja zakupca; i mount lokalnih slika artikala (`/slike`) je prvo bio samo isključen (v1,
> §121) jer se izvodi iz jedne konekcije a firme ih imaju N — **11.K je to zatvorio**:
> `TenantSlikeMiddleware` očekuje `/slike/<šifra-zakupca>/<ostatak>` i servira iz foldera baš te
> firme (`SlikeArtikalaStorage.KoreniFolder` je već per-baza); nepoznata šifra ili izlazak iz
> korenog foldera → 404, nikad fallback na „prvu" bazu.
>
> Kod: [`ERPiApi/Services/Tenancy/`](../ERPiApi/Services/Tenancy/). Testovi:
> `ERPiData.Tests/MultiTenantRazresavanjeTests.cs` + `MultiTenantIzolacijaTests.cs` +
> `TenantSlikeMiddlewareTests.cs`.
>
> ## ✅ FRONTEND TENANT-AWARENESS (11.K.3)
>
> Backend je od 11.E/11.K.2 spreman, ali `ERPiWebShop` je slao `/api` i `/hubs` pozive bez
> `X-Tenant-Id` (→ 404 iz `TenantResolutionMiddleware`) i tražio slike sa `/slike/<id>/…` bez
> prefiksa šifre (→ 404 iz `TenantSlikeMiddleware`). 11.K.3 to zatvara:
>
> - **Izvor šifre:** `VITE_TENANT_ID` env — jedan frontend build/instanca po firmi, isti model
>   kao `VITE_PORT`/`VITE_API_TARGET` u `vite.config.ts`. Prazna → jednofirmsko ponašanje,
>   bajt-identično (kao `tenantMode` u `Program.cs`). **Bez login birača firme** (§2: registar
>   puni ops; birač bi tražio izuzet endpoint i otkrivao spisak klijenata).
> - **`X-Tenant-Id` na svaki poziv:** monkey-patch `window.fetch` u
>   [`src/services/tenant.ts`](../ERPiWebShop/src/services/tenant.ts) (`instalirajTenantFetch`,
>   zvan iz `main.tsx`) — jedini chokepoint za ~20 `*Api.ts` modula + `dohvatiJson` + SignalR
>   `negotiate`. Ne dira strane URL-ove ni ne-`/api` putanje; ne gazi header koji je poziv već
>   postavio.
> - **SignalR WebSocket** ne nosi custom header iz pretraživača, pa `useErpiLiveHub` dodaje
>   `?tenant=<šifra>` na hub URL. `HeaderTenantResolver` sad čita i `?tenant=` (header preteže) —
>   isti obrazac kao `access_token` kroz query.
> - **Slike:** `tenantSlikaUrl` (u `tenant.ts`) ubacuje šifru u `/slike/<id>/…` → `/slike/<šifra>/<id>/…`;
>   `slicicaUrl`/`slikaUrl` u `utils/placeholderSlika.ts` ga provlače, ostala render-mesta
>   (kartice, detalji, korpa, SEO/OG) direktno.
>
> Kod: `ERPiWebShop/src/services/tenant.ts`, `utils/placeholderSlika.ts`, `hooks/useErpiLiveHub.ts`;
> `ERPiApi/Services/Tenancy/ITenantResolver.cs`. Testovi: `src/services/tenant.test.ts`,
> `src/utils/placeholderSlika.test.ts`, `src/hooks/useErpiLiveHub.test.ts`,
> `MultiTenantRazresavanjeTests.HeaderResolver_QueryParametarKaoRezerva_HeaderPreteze`.

## 1. Šta danas postoji (provereno u kodu, ne pretpostavljeno)

- **`ERPiApi` je danas per-firm proces, bukvalno.** [Program.cs:313](../ERPiApi/Program.cs#L313)
  registruje `AddDbContext<ErpiDbContext>` **jednom, pri startu**, sa jednim konekcionim stringom
  (`connStr`) izvedenim iz `--db` argumenta ili markera aktivne firme
  (`aktivna_baza.json`, koji `ERPiApp` upisuje pri ulasku u firmu). Svaka firma koja hostuje web
  dobija sopstvenu instalaciju Windows servisa `ERPiApi_<šifra>` na svom portu — to je izolacija
  na nivou OS procesa, ne aplikacije.
- **Tri pozadinska servisa pretpostavljaju tačno jednu bazu za ceo život procesa**:
  `NapusteneKorpeBackgroundService`, `ObavestenjaOZalihiBackgroundService`,
  `PretplataBackgroundService` ([Program.cs:328-336](../ERPiApi/Program.cs#L328-L336)) — svaki je
  `AddHostedService` singleton koji kroz `IServiceScopeFactory` otvara `ErpiDbContext` vezan za
  isti, jedini registrovan `connStr`.
- **Desktop (`ERPiApp`) već ima dvoslojni multi-firm model, ali unutar JEDNOG procesa u JEDNOM
  trenutku** (`docs/ARCHITECTURE.md` §2.1): `GlobalLoginWindow` (prijava operatera, PBKDF2 nad
  `master_users.json`) → `CompanySelectWindow` bira firmu iz `CompanyRegistryService`
  (`companies.json`, `CompanyEntry` sa `Naziv`/`Provider`/`DbPath`|`ConnectionString`) → otvara se
  `MainWindow` nad TAČNO JEDNOM `ErpiDbContext`-u. Nema paralelnog rada sa dve firme u istom
  procesu — ni ovaj sloj to ne radi, samo bira koju bazu otvoriti PRE nego što se bilo šta drugo
  desi. `CompanyEntry`/`CompanyRegistryService` žive u `ERPiApp`, `ERPiApi` ih ne referenciše.
- **`ErpiDbContext.ConfigureOptions`/`DetectProvider` već podržavaju sva tri provajdera** (SQLite/
  PostgreSQL/SQL Server) sa istim EF Core modelom/migracijama (`docs/ARCHITECTURE.md` §2.3) — "rad
  na serveru" (Faza 13, §3bu/§3by) znači da firma ume da živi na Postgres/MSSQL umesto SQLite fajla,
  ali i dalje jedna firma = jedan `ErpiDbContext` = jedan connection string, birano pri startu
  procesa, ne po zahtevu.
- **JWT nema `firmaId` claim.** `JwtService.GenerisiToken`/`GenerisiTokenZaOsoblje`
  ([JwtService.cs:87](../ERPiApi/Services/JwtService.cs#L87),
  [:132](../ERPiApi/Services/JwtService.cs#L132)) nose `NameIdentifier`/`Email`/`Role`/`PartnerId`
  — nema pojma "koja firma" jer danas nije potrebno, proces JE ta firma.
- **SignalR Live Hub v1** (§110, `docs/DIZAJN_SIGNALR.md` §5) je eksplicitno odložio "multi-tenant
  grupisanje unutar procesa" na baš ovaj dizajn — kad ovaj dokument uđe u kod, hub grupe postaju
  `firma-{firmaId}` umesto fiksnog `"admin"`.

## 2. Šta ovo NIJE

**Ovo nije refaktor postojećeg modela, nego nov, opciони hosting režim.** Svaka firma koja danas
ima svoju Velopack instalaciju i `ERPiApi_<šifra>` servis nastavlja identično, bez ijedne izmene u
ponašanju — v1 ne dira podrazumevani (single-tenant) put uopšte. Motivacija je konkretna: agencija
koja vodi knjigovodstvo za N malih firmi ne treba da instalira i održava N Windows servisa na
sopstvenom serveru da bi im ponudila web/B2B pristup — jedan pokrenut proces treba da opsluži više
firmi, svaku sa svojom bazom (SQLite/Postgres/MSSQL, bilo koja), strogo izolovanih jedna od druge.

**Van obima v1** (razlog isti kao "ne raditi uzgred" u §5 SignalR dokumenta):
- Desktop `ERPiApp`/WPF — ostaje potpuno nedirano, i dalje jedan proces = jedna firma.
- Agregatni pogled preko više firmi u istom ekranu (npr. "sve porudžbine svih klijenata agencije
  na jednoj tabli") — v1 je strogo per-zahtev izolacija na tačno jednu firmu, bez agregacije.
- Samoposlužno kreiranje nove firme kroz web UI ("registruj se kao nova firma") — registar zakupaca
  (tenant registry) se u v1 puni ručno/kroz ops proces, nema "Nova firma" dugmeta.
- Migracija postojećih server-firmi (Postgres/MSSQL) NA multi-tenant hosting — v1 samo omogućava
  novim/premeštenim firmama da uđu u zajednički proces; ne postoji automatski "prebaci firmu sa
  sopstvenog servisa u deljeni proces" alat.
- Multi-tenant grupe u SignalR hubu — ~~poseban, mali sledeći korak, ne deo v1~~ ✅ *realizovano
  08.09.2026 (§133).* `ErpiLiveHub` čita `TenantSifra` claim i konektuje admina u
  `tenant-{sifra}-admin` grupu; `ErpiLiveNotifier` emituje u tu grupu (šifra iz `CurrentTenantAccessor`
  za kontrolere, iz petlje po zakupcima za pozadinske servise). Van `--tenants` grupa ostaje
  `"admin"`. Vidi `docs/DIZAJN_LAGER_SYNC.md` §3.3.

## 3. Tenant identifikacija — pre autentifikacije, ne posle

Ovo je centralna teškoća, ne detalj: `WebKorisnik`/`Korisnik` (nalozi koji se prijavljuju) **žive
unutar baze firme koju predstavljaju** — isto kao danas. Da bi `POST /api/auth/login` uopšte znao
KOJU bazu da upita za taj email, mora znati firmu **pre** provere lozinke, ne posle. Nema
zajedničke "master" tabele naloga (namerno — isti razlog kao desktop §2.1: dva odvojena sloja,
ne jedan spojen).

Dve realne opcije za "kako zahtev kaže koja je firma":

| Opcija | Prednost | Mana |
|---|---|---|
| **Subdomen** (`pssspirot.erpi-hosting.rs`) | Čisto za pravi javni SaaS, prirodno se uklapa u `docs/ARCHITECTURE.md` §2.3 (Docker+Caddy je već predviđena opcija za server hosting) | Traži wildcard DNS + sertifikat; ne radi na `localhost`/LAN gde većina klijenata danas živi (Velopack, port po firmi) |
| **Eksplicitan header/path** (`X-Tenant-Id: pssspirot` ili `/t/pssspirot/api/...`) | Radi svuda odmah, nema DNS zavisnost, testira se kao i sve ostalo | Ružnije za javni multi-klijent portal; mora se štititi da vrednost ne bude proizvoljno verovana (v. §5) |

**Odluka za v1: header/path, ne subdomen.** Razlog nije tehnički nego situacioni — danas nijedan
stvaran klijent ne radi produkciono preko reverse proxy-ja (`docs/DIZAJN_SIGNALR.md` §7, potvrđeno
01.09.2026: "trenutno niko ne koristi produkciono, sve je test/razvoj"), pa DNS/sertifikat
infrastruktura za subdomene nema ko da je iskoristi još. **Resolver se piše iza interfejsa**
(`ITenantResolver`, v. §4) baš zato da subdomen-varijanta može da se doda kasnije bez diranja
ičega nizvodno — isti "uzak v1, ostatak upisan" obrazac kao SignalR §2.

## 4. Arhitektura v1

```
Zahtev stiže (uključujući /api/auth/login, PRE bilo kakve autentifikacije)
        │
        ▼
TenantResolutionMiddleware (nov, rano u pipeline-u, PRE UseAuthentication)
        │  čita X-Tenant-Id header (ili /t/{sifra} path segment)
        │  traži šifru u TenantRegistryService (nov, v. ispod)
        ▼
TenantRegistryService.Pronadji(sifra) → TenantInfo (FirmaId, ConnectionString, Provider)
        │  nepoznata šifra → 404 odmah, ne stiže do kontrolera
        ▼
HttpContext.Items["Tenant"] = TenantInfo   (dostupno ostatku pipeline-a, uklj. DbContext factory)
        │
        ▼
ErpiDbContext se KONSTRUIŠE po zahtevu sa TenantInfo.ConnectionString
        (AddDbContext OSTAJE Scoped — v. §5, NE AddDbContextPool)
        │
        ▼
[Authorize] JWT middleware validira token KAO DANAS, plus:
        nova provera — token.firmaId claim MORA da se poklopi sa HttpContext.Items["Tenant"].FirmaId
        (v. §5 — ovo je glavna odbrana od ukrštanja podataka firmi)
        │
        ▼
Kontroler/servis rade identično kao danas — ne znaju da su multi-tenant, dobijaju već ispravan
ErpiDbContext kroz DI kao i uvek.
```

- **`TenantRegistryService`** — nov, mali servis, **odvojen od `ERPiApp`-ovog `CompanyRegistryService`/
  `companies.json`** (namerno: to je desktop-mašina-lokalni fajl za operatera koji bira firmu na
  SVOM računaru; ovo je serverska konfiguracija procesa koji hostuje N firmi, drugačiji životni
  vek i drugačiji pristup — ako se pokaže da je format dovoljno sličan, spajanje je razmatranje ZA
  KASNIJE, ne pretpostavka sad). Format: JSON fajl (isti stil kao `companies.json`, isti razlog —
  repo već ima presedan da konfiguracija firmi ide kroz čitljiv JSON, ne bazu) sa listom
  `{ Sifra, FirmaId, ConnectionString, Provider }`, čita se pri startu procesa, `--tenants <put>`
  CLI argument (isti obrazac kao postojeći `--db`).
- **`ErpiDbContext` registracija prelazi sa fiksnog `connStr` na tenant-svesnu fabriku** —
  `AddDbContext<ErpiDbContext>((sp, options) => { var tenant = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Items["Tenant"] as TenantInfo; ErpiDbContext.ConfigureOptions(options, tenant.ConnectionString, tenant.Provider); })`.
  Ostaje **Scoped** (podrazumevano za `AddDbContext`) — svaki HTTP zahtev već dobija sopstveni
  `DbContext`, što je i danas slučaj; jedina promena je ŠTA se prosledi kao connection string.
- **Ceo ovaj sloj je opcioni, feature-flagged.** Ako `--tenants` nije prosleđen, `Program.cs`
  ostaje TAČNO na današnjem putu (jedan `connStr`, bez `TenantResolutionMiddleware`-a u pipeline-u)
  — postojeći single-firm hosting ima nula dodirnutog koda na svom putu.

## 5. Bezbednosni rizici — imenovani, ne uopšteno "biti pažljiv"

Ovo je stavka koju plan eksplicitno traži ("striktna validacija firmaId po zahtevu — bezbednosni
rizik ukrštanja podataka firmi ako se to preskoči"). Četiri konkretna mesta gde curenje između
firmi može da uđe:

1. **JWT token firme A ponovo upotrebljen sa `X-Tenant-Id` firme B.** Bez provere iz §4
   ("firmaId claim MORA da se poklopi sa resolved tenant-om"), ukraden/kopiran token važećeg
   korisnika firme A bi mogao da čita bazu firme B ako napadač samo promeni header — token je
   potpisan i validan, samo nije NAMENJEN toj firmi. Provera mora biti **na svakom zahtevu**, ne
   samo pri loginu (token se pamti u `localStorage` i preživljava promenu tenant-a u istom
   pretraživaču).
2. **`AddDbContextPool` umesto `AddDbContext`.** Poolovan kontekst se reciklira između zahteva radi
   performansi — ako bi neko "optimizovao" ovo kasnije, connection string postavljen za firmu A pri
   kreiranju objekta mogao bi ostati zalepljen na recikliranu instancu koju sledeći zahtev (firma B)
   dobije iz pool-a, ako reset nije eksplicitno proveren. **Eksplicitna odluka: ostaje `AddDbContext`
   (Scoped, novi objekat po zahtevu), ne `AddDbContextPool`**, dok se ne dokaže da je potreban i da
   je pooling bezbedan sa promenljivim connection string-om.
3. **Tri pozadinska servisa (§1) danas pretpostavljaju jednu bazu za ceo život procesa.** U
   multi-tenant režimu moraju da **iteriraju SVE aktivne zakupce u svakom prolazu** (`foreach tenant
   in TenantRegistryService.Svi()`), ne jednu. Ovo je stvarna dodata složenost, ne detalj — svaki od
   tri servisa (napuštene korpe, obaveštenja o zalihama, pretplate) treba posebnu proveru pri
   implementaciji da li njegova logika po firmi ostaje ispravna kad se pokrene N puta u istom tick-u
   umesto jednom. Otvoreno pitanje za implementacioni korak, ne rešeno ovim dokumentom.
4. **Structured logging gubi "port u imenu fajla" kao razdvajač.** §103 je rešio da
   `%ProgramData%\ERPiApi\Logs\erpiapi-{port}-.log` razdvaja instance po portu, jer je danas
   port = firma (jedan proces po firmi). U multi-tenant režimu jedan proces opslužuje N firmi na
   JEDNOM portu — port razdvajanje gubi smisao. `firmaId` mora ući kao Serilog structured
   property (`LogContext.PushProperty("FirmaId", tenant.FirmaId)` u `TenantResolutionMiddleware`-u)
   da operater i dalje može da filtrira log po firmi.

## 6. Testiranje (pre koda, da se zna šta zaključava ponašanje)

- Isti obrazac kao `KontroleriTestHost`/`DemoBazaFixture` — nov test fixture sa **dva** fake
  zakupca, svaki sopstveni privremeni SQLite fajl sa različitim, prepoznatljivim podacima (npr.
  drugačiji naziv firme/šifra artikla), registrovan u test `TenantRegistryService`.
- Ključni test nije "da li rade dva konteksta" (trivijalno) nego **da li se curenje zaista
  sprečava**: (1) token mintovan sa `firmaId=A` odbijen (403) kad je resolved tenant `B`; (2)
  zahtev bez `X-Tenant-Id` (ili sa nepostojećom šifrom) ne pada na "podrazumevanu" bazu nego 404
  odmah; (3) dva paralelna zahteva (jedan po tenant-u, pokrenuta konkurentno da uhvate eventualni
  DI/scope bug) vraćaju SVAKI podatke SVOJE firme, nikad tuđe.
- Pozadinski servisi (§5 tačka 3): test da servis pokrenut nad dva zakupca upiše/pošalje efekat u
  OBE baze, ne samo u prvu koju nađe.

## 7. Otvorena pitanja za korisnika pre implementacije

1. **Da li ovo rešava stvaran, tražen slučaj danas, ili je unapred rad na spekulaciju?** Ako
   nijedna knjigovodstvena agencija još nije tražila deljeni hosting, ovo možda treba da ostane
   dizajn-referenca u backlog-u (kao ovaj dokument), ne nešto što ulazi u kod ove sesije.
2. **Header/path (v1 odluka iz §3) ili odmah subdomen?** Ako je poznato da će prvi stvaran klijent
   ipak biti javno dostupan portal (ne LAN/localhost), možda vredi uložiti u subdomen odmah umesto
   dva prolaza kroz isti problem.
3. **Da li je prihvatljivo da v1 NE dira tri pozadinska servisa** (ostave se da rade samo za
   "primarnog" — ako uopšte postoji — zakupca dok se §5 tačka 3 ne reši posebno), ili moraju od
   prvog dana da rade tačno za sve zakupce?
