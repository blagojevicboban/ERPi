# 🚀 ERPi — Integrisani Poslovni Sistem

![Version](https://img.shields.io/badge/version-2.0.0--alpha-blue.svg)
![NET](https://img.shields.io/badge/.NET-8.0--windows-purple.svg)
![UI](https://img.shields.io/badge/UI-WPF%20%7C%20Modern%20Design-success.svg)
![Database](https://img.shields.io/badge/Database-SQLite%20%7C%20EF%20Core%208-green.svg)
![Updater](https://img.shields.io/badge/Auto--Update-Velopack-orange.svg)

> **ERPi** je jedinstvena integrisana desktop aplikacija za finansijsko knjigovodstvo (glavna knjiga, bilansi, e-Fakture, e-Fiskalizacija), robno-materijalno poslovanje, osnovna sredstva i obračun zarada — razvijena u **C# / .NET 8 / WPF**.

---

## 📄 Arhitektura i Plan Implementacije

Detaljan arhitekturni plan, model ujedinjene baze podataka i fazni roadmap implementacije nalaze se u dokumentu:
👉 **[ANALIZA_I_PLAN.md](ANALIZA_I_PLAN.md)**

---

## 🛠️ Tehnološki Stog

- **Jezik / Okvir:** C# 12 / .NET 8.0 WPF
- **Baza podataka:** SQLite (po jedna baza po firmi) sa EF Core 8
- **Izveštaji / PDF:** QuestPDF
- **Excel Izvoz:** ClosedXML
- **Bar-kodovi:** ZXing.Net
- **Pakovanje / Auto-Update:** Velopack (`vpk`)
- **Testiranje:** xUnit
