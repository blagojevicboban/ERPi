---
name: import-from-source-apps
description: Port a feature/module (models, views, services) from ERPiFinansije, ERPiSredstva, or ERPiZarade into the unified ERPi. Use whenever adding a module listed as ⬜ in PLAN_NASTAVKA.md, or any time work involves copying/adapting code from one of those three source repos into ERPi.
---

ERPi (`c:\ERPi\ERPi`) is not a from-scratch rewrite — every module has a mature reference
implementation already living in `c:\ERPi\ERPiFinansije`, `c:\ERPi\ERPiSredstva`, or
`c:\ERPi\ERPiZarade`. The job is almost never "design a new feature" — it's "port this
one, but onto the shared Core schema instead of that app's private one." Read
[`ANALIZA_I_PLAN.md`](../../../ANALIZA_I_PLAN.md) and [`PLAN_NASTAVKA.md`](../../../PLAN_NASTAVKA.md)
first for which phase is next and which source app it comes from.

## The one recurring transformation: string reference → real foreign key

The three source apps each run on their own SQLite file, so anything that would be a
cross-entity FK in a normal schema is instead a **string code** there — e.g.
`StavkaNaloga.BrojKonta` (ERPiFinansije) or `Radnik.SifraMestaTroska` (ERPiZarade, see the
comment on that field — it says explicitly the string is a workaround for the split-DB
world, not the intended design). In ERPi, that workaround's reason is gone: **replace the
string with a real `int`/`int?` FK to the Core table** (`KontoId`, not `BrojKonta`;
`PartnerId`, not a partner name string; etc.). This is the actual point of unifying the
databases, not a nice-to-have — do it every time, don't carry the string forward "to stay
close to the original."

Worked example: `ERPiFinansijeData/Models/StavkaNaloga.cs` (`BrojKonta` string) →
[`ERPiData/Models/Finansije/StavkaNaloga.cs`](../../../ERPiData/Models/Finansije/StavkaNaloga.cs)
(`KontoId` FK, see its doc comment).

## Where a ported model's fields go: Core vs module-specific

Don't assume a source app's single table maps to one new table. Split it:

- **Shared identity/master data** (used or usable by more than one module — partner
  identity, chart of accounts, cost centers, company, users) → lives in
  `ERPiData/Models/Core/`, already mostly built (Faza 1). Extend it, don't duplicate it.
- **Module-specific operational data** (e.g. a payroll employee's coefficients and tax
  fields, a fixed asset's depreciation schedule) → its own `ERPiData/Models/<Modul>/`
  table, linked to Core via a FK (e.g. future `Radnik.PartnerId`) — do **not** inline it
  into `Partner`/`Konto`/etc. `Partner` in particular is deliberately lean (see its doc
  comment and `PLAN_NASTAVKA.md` §2) specifically to avoid becoming a "god table" that
  every module bolts fields onto. If you're tempted to add a module-specific column to a
  Core table, put it in a module table with a FK instead.

## Trim, don't transplant whole

The source apps carry years of accumulated features (F2 search popups, DMS attachments,
devizno/currency columns, Excel export, advanced filters, "nova godina" rollover...). Do
**not** port a module 1:1 in one pass — it is large, tightly coupled to the old
string-keyed schema, and mostly unverifiable in one sitting. Instead:

1. Port the **core entity + the single most-used screen** (e.g. for Nalozi: the ledger
   list + create/edit dialog with balance validation — not the F2 konto search, not DMS,
   not devizno, not PDV auto-fill).
2. List what you deliberately left out under "Poznati nedostaci" for that phase in
   `PLAN_NASTAVKA.md`, so it reads as a scoped decision, not a forgotten gap.
3. Also drop legacy-only fields that only existed to carry raw DBF import columns (e.g.
   `Konto.Ulica`/`Mesto`/`Telefon` in the old model) unless something in the *new* schema
   still needs them — check before dropping, but default to leaving them out.

## UI conventions already established — reuse, don't reinvent

`ERPiApp/App.xaml` already has the shared style resources (ported from
ERPiFinansije's, same palette): `NavButtonStyle`, `ActionButtonStyle`, `SecondaryButton`,
`IconButtonStyle`, `NumericColumnElementStyle`/`NumericColumnEditingStyle`. Use them as-is.

- **Toolbar action buttons are icon-only with a `ToolTip`** (`Style="{StaticResource
  IconButtonStyle}"`, `Content="➕"` etc.) — not icon+text like the source apps'
  `ActionButtonStyle` buttons. This is a standing preference for this project, not
  specific to one screen.
- Master-detail list screens follow `NaloziView`'s layout: toolbar Border → list DataGrid
  Border → GridSplitter → detail DataGrid Border.
- Edit dialogs follow `NalogEditWindow`'s layout: header fields Border → editable stavke
  DataGrid Border → live balance/status Border → action buttons row.
- `DataGridComboBoxColumn` for a FK column: `SelectedValueBinding="{Binding XxxId}"`,
  `SelectedValuePath="XxxId"`, `DisplayMemberPath="Prikaz"` (or `Naziv` if the entity has
  no `Prikaz`), `ItemsSource` set in code-behind after `InitializeComponent()`.

## Wiring a new module into MainWindow's sidebar

Add a `RadioButton` under the right section header in `MainWindow.xaml` (`GroupName`
already shared across all nav buttons) with a `Click="NavXxx_Click"` handler in
`MainWindow.xaml.cs` that sets `TxtHeaderTitle.Text` and swaps `MainContentHost.Content`
for a `new <ModuleName>View(_db)`. Move its `TextBlock` placeholder out of the "USKORO"
list when you do.

**Never set `IsChecked="True"` as a XAML literal on a `RadioButton`/`CheckBox` whose
`Checked` handler touches sibling elements declared later in the same XAML file.** WPF
fires `Checked` synchronously during `InitializeComponent()`, before those later siblings
exist yet — `NullReferenceException`, crashes the whole app on first click. Set the
initial checked state in code-behind, after `InitializeComponent()`, instead. (Caught live
in `NaloziView` while building Faza 3.1 — same bug class documented in ERPiFinansije's
`run-accounting-app` skill.)

## After the model changes: migration, then build, then actually run it

1. `dotnet build` the whole `ERPi.slnx` after adding/editing `ERPiData/Models/**`.
2. `cd ERPiData && dotnet ef migrations add <Name>` — name it for what changed
   (`DodajNaloge`, not `Migration3`).
3. Verify it actually applies clean before trusting it:
   ```powershell
   cd ERPiData
   dotnet ef database update --connection "Data Source=_verify.db"
   # inspect if useful, then:
   rm _verify.db
   ```
4. Build `ERPiApp`, then **drive it for real** with the `run-erpi-app` skill
   (`ERPiApp/.claude/skills/run-erpi-app`) — `launch` (autologin), `click` the new nav
   item, `ss` a screenshot. A clean `dotnet build` says nothing about XAML runtime
   crashes (`StaticResource` lookups, the `IsChecked` gotcha above, null `ItemsSource`) —
   only actually opening the screen catches those, as it did here.

## Closing a phase

Update `PLAN_NASTAVKA.md`: move the row from ⬜ to ✅ in the phase table, add anything
non-obvious to §2 ("Odluke koje ne treba poništavati") or §3 ("Poznati nedostaci") if
scope was deliberately trimmed per the "Trim, don't transplant whole" section above.
