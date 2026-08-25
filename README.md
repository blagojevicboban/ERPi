<div align="center">

# ⚡ ERPi Enterprise Business Suite
### Celoviti, Hibridni Poslovni Informacioni Sistem & e-Commerce Platforma

[![Version](https://img.shields.io/badge/version-2.58.3-blue.svg?style=for-the-badge&logo=semver&logoColor=white)](CHANGELOG.md)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512BD4.svg?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF UI](https://img.shields.io/badge/UI-WPF%20Desktop-0078D4.svg?style=for-the-badge&logo=windows&logoColor=white)](ERPiApp)
[![WebShop & Admin](https://img.shields.io/badge/Web-React%20%7C%20Vite%20%7C%20Tailwind-61DAFB.svg?style=for-the-badge&logo=react&logoColor=black)](ERPiWebShop)
[![Database](https://img.shields.io/badge/Database-SQLite%20%7C%20Postgres%20%7C%20MSSQL-4479A1.svg?style=for-the-badge&logo=sqlite&logoColor=white)](docs/ARCHITECTURE.md)
[![Tests](https://img.shields.io/badge/Tests-1461%20Passing%20(1272%20.NET%20%2B%20189%20Web)-10B981.svg?style=for-the-badge&logo=checkmarx&logoColor=white)](ERPiData.Tests)
[![Auto-Update](https://img.shields.io/badge/Updater-Velopack-F97316.svg?style=for-the-badge&logo=githubactions&logoColor=white)](docs/DEPLOYMENT.md)

<p align="center">
  <strong>Objedinjeno finansijsko i materijalno knjigovodstvo, robno poslovanje, proizvodnja, obračun zarada, osnovna sredstva, SEF e-Fakture, e-Fiskalizacija i omni-channel B2C / B2B WebShop.</strong>
</p>

[Ključni Moduli](#-ključni-moduli-sistema) • [Arhitektura](#-arhitektura-i-tehnologije) • [WebShop & B2B](#-integrisani-webshop--b2b-portal) • [Baze Podataka](#-multi-dbms-i-mrežni-rad) • [Brzi Početak](#-brzi-početak-i-razvoj) • [Dokumentacija](#-tehnička-dokumentacija)

</div>

---

## 🌟 Zašto ERPi?

**ERPi** je projektovan od nule kao savremena alternativa zastarelim legacy knjigovodstvenim softverima. Pruža maksimalnu ergonomiju i brzinu rada na tastaturi (F1–F12 prečice, automatski fokus, lookup modali), intuitivan vizuelni doživljaj u tamnom i svetlom režimu, robusnu višekorisničku pouzdanost i 100% usaglašenost sa propisima Republike Srbije:
- **Zakon o računovodstvu i MRS/MSFI** (kontni plan, automatsko dvojno knjiženje, zaključni list, bruto bilans, bilans stanja i uspeha).
- **Zakon o elektronskom fakturisanju (SEF)** — dvosmerna UBL 2.1 integracija, masovno slanje, automatsko preuzimanje i knjiženje ulaznih e-faktura.
- **Zakon o fiskalizaciji** — sertifikovan ESIR sa podrškom za lokalni (L-PFR) i virtuelni (V-PFR) procesor, simulator kase i termalnu štampu (ESC/POS).
- **Zakon o porezu na dodatu vrednost (PDV)** — automatska evidencija prethodnog i izlaznog poreza, POPDV, KIR i KPR obrasci.
- **Zakon o radu i e-Porezi** — obračun zarada i ugovora van radnog odnosa, automatsko generisanje zvaničnih **PPP-PD** i **PPP-PO** XML datoteka.

```mermaid
graph TD
    subgraph "🏢 ERPi Poslovni Ekosistem"
        DESKTOP["🖥️ ERPiApp (WPF Desktop)<br/>Finansije, Robno, Proizvodnja, Zarade, Sredstva, POS Kasa"]
        API["⚡ ERPiApi (ASP.NET Core REST API)<br/>JWT Auth, Katalog, Checkout, NBS IPS QR Service, Web Admin"]
        SHOP["🛍️ ERPiWebShop (React + TypeScript + Vite)<br/>B2C Prodavnica, B2B Veleprodajni Portal & Web Admin Backoffice"]
        DATA["🗄️ ERPiData (EF Core 8 Core Layer)<br/>Domain Modeli, Poslovna Pravila, Multi-Tenant Contexts, QuestPDF"]
    end

    DESKTOP --> DATA
    API --> DATA
    SHOP --> API
```

---

## 🏢 Ključni Moduli Sistema

<div align="center">

| Modul | Ikona | Opis & Ključne Funkcionalnosti |
| :--- | :---: | :--- |
| **Glavna Knjiga & Finansije** | 📊 | Dvojno knjigovodstvo, automatsko kontiranje, masovno proknjižavanje i rasknjižavanje selekcije, IOS obrasci sa e-mail slanjem, zatvaranje otvorenih stavki, kamatni obračun, devizno valviranje, bruto bilans i finansijski izveštaji. |
| **Robno i Materijalno** | 📦 | Veleprodajne i maloprodajne kalkulacije, uvozni troškovi i zavisni troškovi nabavke, nivelacije cena, KEP knjiga, kartice robe/materijala, trebovanja, međumagacinski transferi, računi-otpremnice i e-Transport. |
| **WebShop & B2B Portal** | 🌐 | Omni-channel internet prodavnica sa **NBS IPS QR** plaćanjem, **3D Secure** karticama, **SMS/Viber** notifikacijama, marketing automatizacijom (napuštene korpe), paketima/setovima artikala, B2B partnerskim cenovnicima i 1-klik fakturisanjem. |
| **CMS & Brending** | 🎨 | Vizuelno prilagođavanje web prodavnice direktno u WebShop administraciji: boje teme, slogani, hero baneri, promotivne trake, cenovnici dostave i pragovi besplatne isporuke. |
| **Proizvodnja & Radni Nalozi** | 🏭 | Sastavnice (BOM) sa normativima, tehnološke faze i operacije, radni nalozi, automatsko razduženje sirovina i zaduženje gotovih proizvoda, automatsko kontiranje u Glavnu knjigu. Obračun cene koštanja po **stvarnom** trošku (materijal, rad, mašinski rad, režija). |
| **Obračun Zarada & HR** | 👥 | Evidencija zaposlenih i ugovora (radni odnos, ugovor o delu, autorski, PP poslovi), automatski obračun poreza i doprinosa, generisanje zvaničnih XML fajlova za Poresku upravu (**PPP-PD** / **PPP-PO**), šabloni ugovora sa PDF štampom i isplatni listići. |
| **Osnovna Sredstva** | 🏗️ | Šifarnik opreme i nekretnina, računovodstvena (MRS 16) i poreska amortizacija (čl. 10b, Obrazac OA/PB-1), godišnji popis sa bar-kod skenerima, reversi, rashodovanja i automatsko knjiženje. |
| **SEF e-Fakture & e-Otpremnice** | ⚡ | Direktna dvosmerna integracija sa SEF portalom (UBL 2.1), masovno slanje i preuzimanje faktura, e-Transport otpremnice sa prevoznicima i automatsko evidentiranje poreza. |
| **Maloprodajna Kasa (POS)** | 🧾 | ESIR kasa usklađena sa **PFR v3 protokolom** Poreske uprave (L-PFR i V-PFR), brza pretraga artikala, barkod skeneri, vagana roba, refundacije, smene, pazar i direktna ESC/POS termalna štampa. Ugrađen lokalni PFR simulator za obuku osoblja. |
| **Izvodi Banke & Auto-Knjiženje** | 🏦 | Automatski uvoz i parsiranje elektronskih izvoda (Halcom, Asseco, Pexim), automatsko prepoznavanje partnera po PIB/računu i automatsko zatvaranje otvorenih stavki u Glavnoj knjizi. |
| **DMS & PDF Izveštaji** | 📄 | Digitalna arhiva dokumenata i priloga uz naloge i artikle, profesionalni PDF izveštaji generisani putem **QuestPDF** endžina sa ugrađenim NBS IPS QR kodom. |

</div>

---

## 🌐 Integrisani WebShop & B2B Portal

ERPi sadrži kompletan e-Commerce podsistem koji radi direktno nad centralnom ERP bazom podataka bez potrebe za eksternim sinhronizatorima:

```
                                  ┌───────────────────────────┐
                                  │   ERPiWebShop (React)     │
                                  └─────────────┬─────────────┘
                                                │
                      ┌──────────────────────────┴──────────────────────────┐
                      ▼                                                     ▼
         🛍️ B2C Maloprodaja                                    🏢 B2B Veleprodajni Portal
   • Responzivni katalog sa fasetiranim filterima       • Prikaz ugovorenih partnerskih cena bez PDV-a
   • Dinamičko stablo kategorija sa auto-scrollom       • Registracija i odobravanje B2B partnera
   • 🎁 Paketi i setovi artikala sa uštedom             • Uvid u otvorene stavke, dospeća i limite
   • ⚡ Ekspresna kupovina „Kupi odmah” (1-klik)        • 📄 Preuzimanje PDF e-Faktura & IOS obrazaca
   • 🔔 Obaveštenja o zalihi („Back-in-Stock”)          • Quick Order masovni tabelarni unos šifara
   • 📱 NBS IPS QR Instant plaćanje telefonom           • 🔄 Re-order (ponavljanje prethodnih porudžbina)
   • 💳 3D Secure 2.0 kartično plaćanje                 • Direktan prenos porudžbine u Račun-Otpremnicu
   • 🛒 Praćenje i oporavak napuštenih korpi            • Automatska kontrola kreditnog limita
   • 🚚 Integracija sa kurirskim službama               • Instant generisanje PDF predračuna
   • 📢 Marketing XML feedovi (Google/Meta/ePonuda)     • 📦 Magacinski nalozi za pakovanje robe (Pick-list)
```

---

## 🗄️ Multi-DBMS i Mrežni Rad

ERPi omogućava fleksibilan izbor baze podataka prilagođen infrastrukturi preduzeća:

| Provajder | Režim Rada | Karakteristike |
| :--- | :--- | :--- |
| **SQLite** | 🏠 Lokalno / Jedan računar | Nulta konfiguracija. Baza je jedan fajl unutar `%LocalAppData%\ERPi\Baze\*.db`. Idealan za prenosivost na USB i brzi backup. |
| **Microsoft SQL Server** | 🏢 Lokalna Mreža (LAN) | Puna podrška za SQL Server 2019/2022 (Express, Standard). Ugrađen instalacioni servis i automatsko podešavanje firewall portova. |
| **PostgreSQL** | 🌐 Cloud / Dedicated Server | Skalabilno rešenje za višekorisničke serverske instalacije, remote lokacije i Linux hosting. |
| **Mrežna Radna Stanica** | 💻 Klijentski režim | 1-klik povezivanje radnih stanica u magacinu/kancelarijama na centralni ERPi server. |

---

## 🛠️ Arhitektura Rešenja

```
ERPi Solution (ERPi.slnx)
 ├── 🖥️ ERPiApp          → WPF Desktop aplikacija (.NET 8 Windows, XAML, MVVM)
 ├── ⚡ ERPiApi          → ASP.NET Core 8 Web API (JWT Bearer, REST, Swagger, NBS IPS QR)
 ├── 🛍️ ERPiWebShop      → React 18 + TypeScript + Vite + Tailwind CSS v4 (Storefront & Admin)
 ├── 🗄️ ERPiData         → EF Core 8 biblioteka sa domain modelima, servisima i QuestPDF štampom
 ├── 🔄 ERPiMigration    → Uvoznik starih Clipper/FoxPro DBF baza (CP852 kodni raspored)
 └── 🧪 ERPiData.Tests   → xUnit automatizovani testovi (1272 backend testova prolazi)
```

---

## 🚀 Brzi Početak i Razvoj

### Preduslovi
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js v20+](https://nodejs.org/) i `npm`
- Visual Studio 2022 / VS Code / Antigravity IDE

### 1. Kloniranje i Pokretanje Testova
```powershell
# Klonirajte repozitorijum
git clone https://github.com/username/ERPi.git
cd ERPi/ERPi

# Izgradite celokupno rešenje
dotnet build ERPi.slnx

# Pokrenite .NET backend testove (1272 testa)
dotnet test ERPiData.Tests/ERPiData.Tests.csproj

# Pokrenite frontend testove (189 testova)
cd ERPiWebShop
npm test -- --run
```

### 2. Pokretanje Desktop Aplikacije
```powershell
dotnet run --project ERPiApp/ERPiApp.csproj
```

### 3. Pokretanje WebShop API-ja i Web Admin Portala
```powershell
# U prvom terminalu pokrenite API servis:
dotnet run --project ERPiApi/ERPiApi.csproj

# U drugom terminalu pokrenite React WebShop & Admin:
cd ERPiWebShop
npm install
npm run dev
```
> WebShop je dostupan na `http://localhost:5173`, Web Admin Backoffice na `http://localhost:5173/admin`, a Swagger API dokumentacija na `http://localhost:5000`.

---

## 📚 Tehnička Dokumentacija

Za detaljnije vodiče pogledajte dokumentaciju u direktorijumu `docs/`:

- 🏗️ **[Arhitektura sistema i baze podataka](docs/ARCHITECTURE.md)** — Slojno razdvajanje, konekcije i EF Core šeme.
- 🌐 **[WebShop Vodič & NBS IPS QR](docs/WEBSHOP.md)** — Integracija B2C prodavnice, B2B portala i plaćanja.
- 🧾 **[Maloprodajna kasa i e-Fiskalizacija](docs/KASA.md)** — ESIR ↔ PFR protokol, POS, čitač barkoda i ESC/POS štampa.
- 💻 **[Vodič za razvoj i standarde koda](docs/DEVELOPMENT.md)** — Pravila razvoja, obrasci i testiranje.
- 📦 **[Pakovanje i Objavljivanje (Velopack)](docs/DEPLOYMENT.md)** — CI/CD, kreiranje instalera i auto-update.
- 🔄 **[Uvoz iz starih DBF baza](docs/DBF_MIGRATION.md)** — Migracija sa DOS/FoxPro sistema.
- 📓 **[Dnevnik razvoja (Dnevnik rada)](docs/DNEVNIK_2026-08.md)** — Detaljan tehnički zapisnik svih implementacija.
- 📋 **[Plan nastavka](PLAN_NASTAVKA.md)** — Pregled stanja i roadmap preostalih funkcionalnosti.
- 📝 **[Istorija izmena (Changelog)](CHANGELOG.md)** — Detaljan pregled svih verzija po datumima.

---

<div align="center">

**ERPi — Pouzdan temelj modernog poslovanja.**  
© 2026 ERPi Team. Sva prava zadržana.

</div>
