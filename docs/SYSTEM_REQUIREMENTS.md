# 🖥️ ERPi — Sistemski zahtevi

Šta je potrebno da bi ERPi radio na jednom radnom mestu i, opciono, na serveru za više
korisnika. Vrednosti označene **(mereno)** dolaze iz koda / build-a; **(procena)** su iskustvene
preporuke i nisu tvrda granica.

---

## 1. Operativni sistem

| | Minimum | Preporučeno |
|---|---|---|
| OS | Windows 10 verzija 21H2 (64-bit ili 32-bit) | Windows 11 64-bit |
| Server (za više korisnika) | Windows Server 2019 | Windows Server 2022 |

- **(mereno)** Isporučuju se dva build-a: `win-x64` i `win-x86`. 32-bitni je za stare mašine;
  na svemu novijem koristiti 64-bitni.
- Windows 8.1 / 7 i stariji **nisu podržani**.

## 2. .NET runtime — **ne instalira se**

**(mereno)** `ERPiApp` (WPF desktop) i `ERPiApi` (REST servis) se pakuju kao *self-contained
single-file* — .NET 8 i WPF su unutar aplikacije. Na čistoj Windows mašini **nije potrebno**
instalirati .NET, Visual C++ redistributable ni bilo šta drugo. Instaler je jedan `Setup.exe`
(Velopack) i ne traži administratorska prava za ažuriranja.

## 3. Procesor (CPU)

| | Minimum (procena) | Preporučeno (procena) |
|---|---|---|
| Radno mesto | 2 jezgra, x86-64, ~2 GHz (bilo koji CPU iz poslednjih ~10 god.) | 4 jezgra, 2.5 GHz+ |
| Server (5–20 korisnika) | 4 jezgra | 6–8 jezgara |

CPU je usko grlo samo pri generisanju velikih PDF izveštaja, uvozu DBF-a i pravljenju demo
firme punog obima. Svakodnevni rad (unos, pregledi, knjiženje) je lagan.

## 4. Radna memorija (RAM)

| | Minimum (procena) | Preporučeno (procena) |
|---|---|---|
| Radno mesto (samo desktop) | 4 GB | 8 GB |
| Radno mesto + lokalni ERPiApi servis + WebShop u pretraživaču | 6 GB | 8–16 GB |
| Server sa PostgreSQL / SQL Server bazom za više firmi | 8 GB | 16 GB+ |

- Na 4 GB radi, ali uz otvoren pretraživač i još koji program postaje tesno.
- ERPiApi kao Windows servis troši ~150–300 MB; desktop aplikacija ~200–500 MB zavisno od
  otvorenih ekrana i veličine tabela.

## 5. Prostor na disku

| Stavka | Prostor (procena) |
|---|---|
| Instalacija aplikacije (instaler + tekuća + jedna prethodna verzija + keš ažuriranja) | **~1 GB** |
| Baza jedne firme — SQLite, prvih par godina prometa | 20–150 MB |
| Demo firma punog obima | **~30 MB** (mereno) |
| Rezervne kopije (automatske, čuva se više generacija) | 2–10× veličina baze |
| Prilozi dokumenata (skenovi faktura, izvodi, slike artikala za WebShop) | zavisi od upotrebe — rezervisati 1–5 GB |

**Ukupno za jedno radno mesto: računati na 5–10 GB slobodno**, više ako se čuva puno priloga.

- **(mereno)** SQLite baze su u `%LocalAppData%\ERPi\Baze` (i `%LocalAppData%\ERPiApp\Baze` za
  stariji raspored) — **to su podaci pravih firmi**; disk sa tim folderom mora imati rezervu i
  ulaziti u backup plan.
- Web frontend (`ERPiWebShop`) se servira iz `ERPiApi/wwwroot` — **Node.js se ne instalira** na
  klijentu ni na serveru; potreban je samo pri razvoju.

### SSD vs HDD

- **SSD se preporučuje.** SQLite + EF Core rade puno sitnih čitanja; na SSD-u su pregledi i
  izveštaji osetno brži, a pravljenje/otvaranje baze skoro trenutno.
- HDD je prihvatljiv za jedno lagano radno mesto; za server sa bazom ili za više firmi na
  jednoj mašini — obavezno SSD.

## 6. Ekran i rezolucija

### Desktop aplikacija (WPF)

| | Rezolucija | Napomena |
|---|---|---|
| Apsolutni minimum | **1280×800** | glavni prozor je tvrdo ograničen na **(mereno)** 1000×650; prozor „DMS — prilozi" traži **1250 px širine** |
| Praktični minimum | **1366×768** | sve staje, ali široke tabele i editori naloga/faktura su tesni |
| Preporučeno | **1920×1080** | editori naloga i faktura se otvaraju maksimizovano i koriste celu širinu |

- **(mereno)** Windows DPI skaliranje 125 %/150 % je podržano — na 150 % efektivno treba
  1920×1080 fizički da bi glavni prozor imao dovoljno mesta.
- Rad na jednom 1366×768 laptopu je moguć; dva monitora ili 1080p su primetno udobniji za
  knjiženje.

### Web (WebShop + Web Admin)

| | Podrška | Napomena |
|---|---|---|
| **Prodavnica (B2C)** | telefon → desktop | **(mereno)** potpuno responzivna (`width=device-width`, Tailwind breakpoint-i) |
| **Web Admin / B2B backoffice** | od ~360 px naviše | **(mereno)** ima mobilni „drawer" meni; radi na tabletu/telefonu, ali su tabele sa mnogo kolona za horizontalno skrolovanje |
| Preporučeno za backoffice | **≥ 1280 px** (laptop/desktop) | svakodnevni rad u backoffice-u — desktop; tablet za brzu proveru |

**Pretraživač:** aktuelni Chrome, Edge, Firefox ili Safari. Skener barkoda u Web Adminu
(`BarcodeDetector` API) radi **samo u Chrome / Edge** (desktop i Android).

## 7. Mreža

| Scenario | Zahtev |
|---|---|
| Jedno radno mesto, SQLite baza | mreža nije nužna za rad; potreban je izlaz na internet za ažuriranja i eksterne servise |
| Više korisnika | zajednička baza na **PostgreSQL / SQL Server** serveru u LAN-u (deljeni SQLite fajl preko mreže se ne preporučuje) |
| e-Fakture (SEF), fiskalizacija (PFR), kursna lista (NBS), kuririske službe, e-Otpremnica | odlazni **HTTPS (443)** ka tim servisima |
| ERPiApi lokalno / u LAN-u | TCP **5000** (mereno — podrazumevani port servisa) |

Osnovno knjigovodstvo, robno i zarade rade i **bez interneta**; samo integracije sa državnim i
eksternim servisima traže vezu.

## 8. Server za više korisnika / WebShop (opciono)

Ako se ERPiApi drži kao centralni servis (Web Admin, B2B portal, javna prodavnica):

- Windows Server 2019/2022, 4+ jezgra, 16 GB RAM, SSD.
- Baza: PostgreSQL 15+ ili SQL Server 2019+ (SQLite nije za konkurentan višekorisnički rad).
- Reverzni proxy (IIS / Nginx) za HTTPS i javno izlaganje — vidi `docs/WEBSHOP_HOSTING_GUIDE.md`.
- Otvoreni portovi prema potrebi (80/443 javno, 5000 interno).

---

> Brojevi za CPU/RAM/disk su procene za tipičnu malu i srednju firmu. Za veliki obim prometa,
> mnogo firmi na jednoj mašini ili intenzivan WebShop saobraćaj — skalirati naviše i meriti na
> konkretnom opterećenju.
