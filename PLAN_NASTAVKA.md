# 🧭 Plan nastavka razvoja — ERPi

> Radni dokument za nastavak posla u novoj sesiji. Prati fazni roadmap iz
> [`ANALIZA_I_PLAN.md`](ANALIZA_I_PLAN.md) i beleži šta je urađeno, šta je namerno odloženo
> i koje odluke ne treba poništavati bez razloga.
>
> Stanje na dan **05.08.2026**, verzija **2.0.0-alpha**.

---

## 1. Gde smo

| Faza | Stavka | Status |
| :--- | :--- | :--- |
| **1** | Core šema (`ERPiData`: Firma, Korisnik, Partner, Konto, MestoTroska) + početna migracija | ✅ |
| **2** | WPF Shell (`MainWindow`), `LoginWindow`, `CompanySelectWindow` — izbor/kreiranje firme, jedna baza po firmi | ✅ |
| **3.1** | Finansije — Glavna knjiga: `Nalog`/`StavkaNaloga`, `NaloziView`, `NalogEditWindow` (MVP) | ✅ |
| 3.2 | Partneri i otvorene stavke (IOS, kamate) | ⬜ |
| 3.3 | Magacin (kalkulacije, nivelacije, fakture) i PDV evidencija | ⬜ |
| 3.4 | SEF e-Fakture i e-Fiskalizacija (PFR) | ⬜ |
| **4** | Osnovna sredstva | ⬜ |
| **5** | Obračun zarada — jedini modul sa realnim produkcionim korisnicima danas | ⬜ |
| **6** | Automatsko knjiženje (Zarade/Sredstva → Nalog) | ⬜ (šema već ima kuku: `Nalog.IzvorModula`/`IzvorId`) |
| **7** | `ERPiMigration` — DOS import (Finansije/Sredstva/Zarade) + direktan `ErpiZaradeProdukcijaImporter` | ⬜ |
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

---

## 3. Poznati nedostaci u Fazi 3.1 (MVP, namerno odloženo)

- Nema UI za devizno knjiženje (`Valuta`/`KursValute`/`DevizniDuguje`/`DevizniPotrazuje` postoje
  u modelu, ne u `NalogEditWindow` gridu).
- Nema UI za PDV `Osnovica`/`StopaPdv` (polja postoje u modelu za kasniju PDV podfazu — 3.3).
- `ColPartner`/`ColMestoTroska` u editoru nemaju način da se izbor vrati na prazno posle prvog
  izbora bez zatvaranja dijaloga — sitna UX mana, ne blokira unos.
- Nema F2 pretrage konta ni šifarnika opisa (ERPiFinansije ih ima) — obična padajuća lista.
- Nema Partneri/Konta/MestaTroska CRUD ekrana još — `NalogEditWindow` pretpostavlja da već
  postoje (unose se direktno u bazu ili preko budućih šifarničkih ekrana).

---

## 4. Testiranje

- **`run-erpi-app`** (`ERPiApp/.claude/skills` i `.agents/skills`, mora ostati sinhronizovano
  u oba) — UI Automation driver, `--autologin` kroz fiksnu `AUTOTEST` firmu
  (`%LocalAppData%\ERPi\Baze\AUTOTEST.db`). Screenshot ide preko UIA `BoundingRectangle`, ne
  golog `GetWindowRect` — na skaliranom ekranu (125%/150%) ovaj drugi tiho seče desnu/donju
  ivicu prozora bez greške; koštalo je vremena da se otkrije, ne vraćati taj "pojednostavljeni"
  pristup.
- Nema još automatizovanih (xUnit) testova — `ERPiData.Tests` projekat tek treba napraviti, kad
  se pojavi prvi netrivijalan servis vredan testiranja (npr. knjiženje ili uvoz).

---

## Sledeći koraci

Faza 3.2 (Partneri i otvorene stavke) nastavlja Finansije modul redom. Alternativa je preskočiti
napred na Fazu 5 (Zarade) jer je to jedini modul sa stvarnim korisnicima danas — odluka je na
korisniku, ne pretpostavljati.
