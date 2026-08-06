# 📋 Istorija izmena (Changelog) — ERPi

Sve značajne promene i novine u aplikaciji **ERPi** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

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
