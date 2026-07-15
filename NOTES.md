# ATP — Implementation Notes

Non-interactive notice log. Items Claude would normally ask about but couldn't (because the user is asleep) land here as decisions-with-rationale, so the user can review them in one pass later.

## Session 2026-04-12 (overnight full-send build)

**Goal**: port 30 Service & Contract tables from MariaDB to MSSQL, scaffold all S&C forms except Reports/Charts, populate `prd.json`, rename UI screenshots, produce installable `.app`.

**Rules of engagement**: see `CLAUDE.md` standing rules §1–§5. User is asleep; no interactive questions. Defaults noted below.

### Decisions taken without asking

| # | Decision point | Default chosen | Rationale |
|---|---|---|---|
| 1 | Table prefix | `zSCP_` | User pre-approved before sleep |
| 2 | People tables | 3 separate (ServicePerson, ServiceAdvisor, Mechanic) | User pre-approved |
| 3 | Screenshot rename | Yes, into 10 semantic folders | User pre-approved |
| 4 | Scope | Full send | User pre-approved |
| 5 | Reports/Charts | DEFERRED | User explicit pre-sleep instruction |
| 6 | ... (more added during execution) | | |

### Items to review tomorrow

_(populated as the build progresses)_

### Business-logic validation agent findings (2026-04-12)

Post-build Explore agent ran a validation pass. Findings and actions taken:

**Critical — FIXED**
1. **Default currency code mismatch** — 5 tables defaulted `CurrencyCode` to `'RM'` but core `CURRENCY` table only contains `'MYR'`. Would have caused silent lookup failures. **Fixed** in `02_CreateTable_zSCP_ServiceContract.sql`, `ServiceItem.sql`, `ServiceItemDebtorHistory.sql`, `ServiceNote.sql`, `ServiceNoteDTL.sql` (replaced `DEFAULT('RM')` → `DEFAULT('MYR')`). Live DB dropped and recreated.
2. **Missing FK `zSCP_ServiceContractID` → `zSCP_ServiceContract`** — composite-PK running-number table had no back-reference. **Fixed**: migration order swapped so `zSCP_ServiceContract` is created before `zSCP_ServiceContractID`, and the ID table now declares `FK_zSCP_ServiceContractID_zSCP_ServiceContract` with `ON DELETE CASCADE`.
3. **Missing FK `zSCP_ServiceNoteID` → `zSCP_ServiceNote`** — same fix pattern as #2.

**High — ACCEPTED AS-IS**
- `zSCP_MeterType.MeterMultiPriceCode` is not a hard FK. Accepted: the MariaDB source allows empty string as "no multi-price tier" which an FK would forbid. App-level validation in `ScpValidationHelper` will enforce the lookup. Comment added to SQL file documenting the decision.
- `zSCP_Appointment.ServicePersonCode` / `ServiceNoteKey` not FK-constrained. Accepted: these are nullable/optional references and the plugin treats them as soft links to match source schema semantics.

**Passing**
- All 31 SQL files registered in migration runner (`ScpMigrations_Cls.RunEmbeddedSQLScripts`)
- All 68 access rights (34 SHOW + 34 OPEN pairs) defined in `AccessRightsConsts.cs` and registered in `PluginMain.RegisterAccessRights`
- All 9 lookup list forms inherit `ScpLookupLst_Form`; all 9 lookup editors inherit `ScpLookupEdt_Form`; all 3 people editors inherit `ScpPersonEdt_Form` — form standardization rule §5 satisfied
- 4 access rights defined but not yet attached to a form: `CMD_*_SCP_ITEM_TAG_SEARCH`, `CMD_*_SCP_GENERATE_ITEM_FROM_SERIAL`. These are used by `ServiceItemTagSearch_Form` and `GenerateServiceItemFromSerial_Form` which are scaffolded but not menu-attached. Wire them into menu in Slice 2 when the real UI lands.
- csproj hygiene clean: 53 form triples properly `<SubType>Form</SubType>` + `<DependentUpon>`, 31 SQL as `<EmbeddedResource>`
- No dangling legacy references (FormServiceContract.cs etc. deleted)

### Items to review tomorrow (TL;DR)

1. **11 unsorted screenshots** in `UI/_unsorted/` — explorer couldn't confidently categorize. Open each and move (see `UI/_rename-log.md`).
2. **Placeholder forms need real UI** (Slice 2):
   - `ServiceContract_Form`, `ServiceItem_Form`, `ServiceNote_Form`, `Appointment_Form` — currently display a "scaffolded / not yet implemented" label
   - `ServiceNoteQuickEntry_Form`, `ServiceNoteClosing_Form`, `ServiceNoteAssignment_Form`, `PreventiveMaintenance_Form`, `MeterTypeTransactionEntry_Form`, `ResetServiceItemDebtorOwnership_Form`, `GenerateServiceItemFromSerial_Form`, `ServiceItemTagSearch_Form`, `ServiceQuickView_Form`, `ServiceOption_Form`, `AppointmentCalendar_Form`
3. **ShadowMain smoke test was NOT run this session** (would require interactive dismissal). Tomorrow: `bash build-and-install.bat` or run `ATPShadowMain\bin\Debug\ATPShadowMain.exe` to verify migration runs and the hero form opens.
4. **Reports submodule** deferred per user instruction — menu placeholders for Reports menu items are NOT in the new code (were in deleted `MenuItemPlaceholders.cs`). If you want them back as placeholders, create `Reports_Form.cs` triples using `ScpPlaceholder_Form` base.
5. **`.appp` file** unchanged (same 4 files shipped: `ServiceContractPhotocopier.dll/.pdb` + `VecTech.ACPluginBase.dll/.pdb`). No change required.
6. **Fresh `.app` at** `C:\Dev\Plugin\ATP\ServiceContractPhotocopier\ServiceContractPhotocopier.app` (~785 KB, timestamped 2026-04-12 03:14) — double-click to install via AutoCount Plug-in Manager.

## Session 2026-04-16 — ServiceItem_Form brought to V8 parity

**Goal**: align `ServiceItem_Form` (ATP plugin) with AutoCount V8 *Maintain Service Item* per screenshot comparison (user-supplied). Plan file: `C:\Users\ndscd\.claude\plans\partitioned-hatching-hejlsberg.md`.

**What shipped this pass**
- Header button bar re-ordered to V8 scheme with updated hotkeys: `Generate Service Item | Add (F5) | Edit (F6) | Save (F7) | Cancel (F8) | Delete (F9) | Print (F11)` on the left; `Attachments | Copy From... | Search (F3) | Exit (F2) | |<<  <  >  >>|` right-anchored. `Preview` button removed.
- Reset Contract moved from top bar to inline next to Contract No field.
- Header left column restructured: `Purchases Date + Inactive`, `Service Tag + Auto (F12)`, `Stock Code + desc label`, `Debtor Code + name label`, `Agent Code + name + Term`, `Reference No + Area`, `Grade Code + Unit Price`, `Description`.
- Header right column: Address now has `Reset Debtor Ownership` button adjacent.
- Tabs renamed: `3. Note`, `5. Service Note History`, `6. Debtors Ownership History`.
- Tab 7 Meter Type grid expanded from 7 → 10 columns in V8 order: Meter Type, **Meter Type Name** (new, joined from `zSCP_MeterType.Description`), Multi Price Code, Charges Rate, Minimum Charges, Free Qty (renamed from FOC Qty), Rebate Qty (%), Initial Meter (renamed from Initial Reading), **Last Reading Date** (new, OUTER APPLY on `zSCP_MeterTrans`), **Last Reading Meter** (new, same source).
- New LoadLookups wiring for Agent Code (SalesAgent), Term (Terms), Area (Area), and `EditValueChanged` handlers that populate the desc/name display labels.
- `OnSave` extended to persist 5 new fields (`Inactive`, `StaffCode`, `TermCode`, `AreaCode`, `UnitPrice`) — all columns already exist in `zSCP_ServiceItem`, no DDL needed.

**Deferred (stubbed handlers — MessageBox only)**
- `Attachments` (top bar) — real attachment viewer pending.
- `Copy From...` (top bar) — "copy from existing service item" workflow pending.
- `Auto (F12)` on Service Tag — currently auto-generates a running number `SI-000001`; might need to mirror the V8 `XXCSSI 00001632.C` schema — confirm with user.
- `Reset Debtor Ownership` — behaviour pending (archive current debtor-history row, open edit dialog?).
- Navigation `|<<  <  >  >>|` — no list context yet. When ServiceItemLst_Form is the launch point, pass an `IEnumerator<DataRow>` so nav works; otherwise grey them out.
- Tab 7 `Edit` / `Transaction` buttons — Edit opens the inline editor; Transaction is still stubbed (should open `MeterTypeTransactionEntry_Form` but requires a saved row with `ServiceItemMeterTypeKey`; wait for real data to wire).
- Tab 5 **Service Note History** — caption renamed; grid still displays `MeterTrans*` columns from the pre-rename implementation. Needs a V8 screenshot of tab 5 to determine the intended columns (likely Service Note No, Date, Status, Debtor, etc.). Do not ship until clarified.
- `Add (F5)` is rendered as a plain button; V8 shows a split-button with dropdown arrow. Cosmetic only.

**Rule-7 Designer.cs compliance**
The file still uses `Tb()`/`Lbl()`/`AddCol()` helpers + comma-grouped field declarations, inherited from the 2026-04-12 scaffold. CLAUDE.md rule 7 forbids these patterns for VS Design-view compatibility. The form builds and runs via ShadowMain, but opening it in VS Design view will likely fail. Separate task: full rewrite of `ServiceItem_Form.Designer.cs` to rule-7 compliance (flat `this.X = new ...(); ...; this.Controls.Add(this.X);` sequence, no helpers, no `var`, one field per control, BeginInit/EndInit pairs). Applies to most other forms in this project as well.

## Session 2026-04-16 (afternoon) — Full CRUD scaffold for ServiceItem_Form & ServiceContract_Form

Plan: `C:\Users\ndscd\.claude\plans\partitioned-hatching-hejlsberg.md`. User asked for the canonical save/CRUD logic, modelled against the V8 master schema (`v8_atp_main` MariaDB).

**What shipped**
- Two new migrations applied to AED_ATPLUGIN001 and registered as `EmbeddedResource`:
  - `02_UpdateTable_zSCP_ServiceItem_v1.3.0.sql` — `CreatedBy`, `ModifiedBy` (nvarchar(40) NULL) + `DEFAULT SYSUTCDATETIME()` on Created/Modified.
  - `02_UpdateTable_zSCP_ServiceContract_v1.3.0.sql` — same audit additions.
- Both forms now share the same CRUD scaffold (each form keeps its own copy — no shared base class yet):
  - `private enum FormMode { New, Edit, View }` + `_mode`, `_isDirty`, `_rowVersion` (DateTime?), `_currentUserCode`, `_suppressDirty`.
  - `OnFormLoad` resolves `_currentUserCode` from `AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID` (fallback `"ADMIN"` for ShadowMain), auto-picks the next code on `New` (calls `OnAutoTag` / `AutoPickContractCode`), then `WireDirtyTracking()` + `ApplyMode()`.
  - `LoadExisting()` captures `LastModified` into `_rowVersion` for optimistic concurrency.
  - `OnSave` rewrite: opens a single `SqlConnection` from `_dbSetting.ConnectionString`, wraps parent + all child writes in one `SqlTransaction`. UPDATE includes `… AND LastModified = @rv`; if 0 rows affected, throws — caught and rolled back. Audit columns set: INSERT writes Created/Modified/CreatedBy/ModifiedBy; UPDATE writes Modified/ModifiedBy. After commit: re-loads the row (refreshes _rowVersion), switches to View mode.
  - `OnAdd` / `OnNew` (depending on form): dirty-prompt → reset all fields to blank → auto-pick code → New mode.
  - `OnEditClicked` / `OnEdit`: only valid from View → switches to Edit, focuses first editable field.
  - `OnDeleteClicked` / `OnDelete`: only valid from View → confirm → transactional DELETE with `LastModified` predicate → reset to blank New on success.
  - `OnCancel`: in View → close. In Edit → dirty-prompt → re-load to revert → View. In New → dirty-prompt → close.
  - `OnFormClosing` override: if dirty in Edit/New → Yes/No/Cancel prompt with Yes calling OnSave.
  - `ApplyMode()` toggles header buttons (Add/Edit/Save/Cancel/Delete/Print/Search/Attachments) + locks `TxtServiceItemCode` / `TxtContractNo` outside New mode + enables/disables `TabMain` and `PanelHeader` editors.
  - `ValidateForSave()`: required-field checks + Service period sanity (End ≥ Start).
  - `ResetFormToBlankNew()`: full field reset including all More-Header controls, both grids, and `_rowVersion = null`.
  - `WireDirtyTracking()`: `EditValueChanged` / `CheckedChanged` / grid `RowChanged` / `RowDeleted` all flip `_isDirty = true` (gated by `_suppressDirty` so programmatic loads don't trigger).
  - Designer.cs: `BtnEdit` and `BtnDelete` Click handlers wired (previously orphaned in ServiceItem_Form).

**Decisions taken without asking** (per harness rule 3)
1. Existing records open in **View** mode (V8 ergonomics) — explicit `Edit (F6)` click required to mutate. Prevents accidental edits.
2. `_currentUserCode` falls back to `"ADMIN"` if AutoCount session is null — needed because ShadowMain auto-login may resolve later than form construction.
3. Concurrency policy = hard fail. Conflict throws an exception with a "reload and try again" message; the user must close + reopen.
4. Code field locked after Save (matches V8). Renames are a separate `Reset` workflow (not implemented).
5. Auto-pick code fires automatically on Add/New — user can overtype.
6. Kept the existing string-concat SQL with `SQLString()` escaping inside the transaction wrapper. Full parameterization (`SqlParameter`) is a separate cleanup pass — touching ~60 columns × 2 statements would be a much larger diff.
7. CRUD scaffold lives in each form (not refactored into `ScpMasterForm_Base`). If a third form needs the same pattern, refactor then.

**Deferred**
- `Done` workflow (mark-complete + lock against further edits) — needs a UI control + its own state.
- Full parameterized commands (currently still concat-with-SQLString).
- `ServiceContractSVI.ServiceItemCode` → `zSCP_ServiceItem.ServiceItemCode` FK constraint — orphan-prevention skipped.
- `Attachments`, `Copy From...`, navigation, `Reset Debtor Ownership`, `Auto (F12)` (still auto-clicks but no visible spinner) — UI present but behaviour stubs.
- `MeterTypeTransactionEntry_Form` integration via Tab 7 `Transaction` button.
- Designer.cs files still violate CLAUDE.md rule 7 (helpers + comma-grouped fields). Build is green, but VS Design view will likely refuse to render.

**Verification done**
- Both v1.3.0 migrations applied to AED_ATPLUGIN001; `CreatedBy` + `ModifiedBy` columns confirmed via `INFORMATION_SCHEMA`.
- `msbuild ATPShadowMain` clean (one nuisance warning resolved by renaming `Validate` → `ValidateForSave` to avoid hiding the inherited `ContainerControl.Validate()`).
- `ATPShadowMain.exe` launches; `ServiceItem_Form` loads in New mode with auto-picked code; no exceptions in `shadowmain.log`. Full UI walkthrough (Add → Save → View → Edit → Cancel → Edit → Save → Delete → OnClosing prompt) is left for the user to drive.

## Session 2026-04-16 (later) — `ATPCli` CRUD CLI for AI smoke tests

New project `ATPCli\` (Console exe, target `atp.exe`, net48, no AutoCount/DevExpress dependencies — only `System.Data.SqlClient` and `System.Configuration`). Added to `ATP.sln` with Guid `{36DBACA8-0DC5-4F1D-A1F6-EDAF86073F10}`. Reads same DB credentials from its own `App.config`.

Purpose: lets an AI driver (or a human via shell) exercise full CRUD on `zSCP_ServiceItem` / `zSCP_ServiceContract` and their child tables without UI automation. Mirrors the SQL the WinForms emit, so `atp item create` exercises the same INSERT shape the form does (parameterised, with `Created/Modified/CreatedBy/ModifiedBy`).

**Available subcommands**
- `atp item list|count|create|read|update|delete|audit`
- `atp meter add|list|delete-all` (child of item)
- `atp contract list|count|create|read|update|delete|audit`
- `atp schema verify` — checks v1.2.0 + v1.3.0 columns are present
- `atp schema columns --table T`
- `atp cleanup --prefix X --confirm` — purges test rows by code prefix
- `atp sql "SELECT ..."` — SELECT-only safety filter
- All commands accept `--json` for machine-readable output. Exit codes: `0` OK / `1` error / `2` bad args / `3` SQL error / `4` not found / `5` schema mismatch.

**Smoke-tested 9-step round-trip**: schema verify → counts → item create → audit → update (verified Created/CreatedBy preserved, Modified/ModifiedBy advanced) → meter add (FK to MeterType enforced, then succeeded with real seed code) → meter list → item delete (confirmed cascade to child meter row) → contract full cycle → cleanup. All passed.

**Known minor issues (not blocking)**
- `Created` / `Modified` are written from `DateTime.UtcNow` in the CLI but the form code uses `GETDATE()` (local time). Doesn't break concurrency (form re-reads + sends back its own captured value), but audit timestamps will look offset by tz when CLI and form mix-create the same row. Future cleanup: align both on either UTC or local.
- Tabular output uses Unicode box-drawing chars — render as `???` in non-UTF8 consoles. JSON mode unaffected.

**Build/run**
```
msbuild ATPCli\ATPCli.csproj -v:m -nologo
ATPCli\bin\Debug\atp.exe help
```

## Session 2026-04-16 (evening) — Reusable CRUD test suite

Captured all AI-driven CRUD smoke tests as a replayable suite:
- `tests\crud-suite.sh` — bash runner, 52 cases across 11 groups, idempotent (self-cleans `SI-SUITE-*` / `SC-SUITE-*`). Reports `[OK]` / `[FAIL]` per case, exits with count of failures.
- `tests\crud-suite.md` — human catalog with one-liner per test ID + "what's not tested" tickets (transaction rollback, multi-process race, nvarchar overflow, date boundaries, decimal precision, case sensitivity, reports/views, UI walkthrough).

**How to re-run**
```bash
bash tests/crud-suite.sh           # all 52 cases
bash tests/crud-suite.sh -v        # verbose
bash tests/crud-suite.sh T40 T41   # specific cases by ID
```

**Coverage snapshot** — passes as of this session:
- Schema + smoke (5 tests)
- Item happy path (6)
- Validation + error paths (12) — dup codes, missing rows, bad args, no-op updates
- Optimistic concurrency (2) — `--if-modified` correct vs stale
- Meter child + cascade (7) — FK enforced, delete-all, parent-delete CASCADEs children
- Partial sparse update (2)
- List + `--like` + `--top` (2)
- Contract CRUD (6) — mirrors item, including concurrency
- SQL safety filter (6) — SELECT/sp_help pass, DELETE/DROP/INSERT/UPDATE blocked
- Escaping + injection (2) — `'` survives, DROP payload neutralised
- Cleanup idempotency (2)

Two fixes applied during testing:
1. Exit-code cleanup: `FormatException` / `ArgumentException` now exit **2** (bad args) instead of the generic 1, so AI can distinguish user-input from infrastructure errors.
2. JSON date output switched to ISO-8601 (`2026-04-16T08:26:49.000`) so `atp audit --json | jq -r .LastModified` pipes straight into `--if-modified`.

## Session 2026-05-08 — ServiceItemLst tab routing

- Added `ServiceContractPhotocopier\Classes\IFormShellHost.cs` so list forms can detect a tabbed-shell host (`this.FindForm() as IFormShellHost`) and route +New / Edit into a new tab via `OpenFormByTitle(string)` / `OpenFormInTab(string, Form)`. Falls back to `ShowDialog` in production AutoCount.
- ShadowLauncherV2_Form now implements `IFormShellHost`. `EmbedFormInTab` was refactored from `(CatalogEntry, Form)` to `(string title, Form)` so the new method can reuse it without inventing a fake CatalogEntry.
- ServiceItemLst_Form: added `GridView.OptionsView.ShowAutoFilterRow = true` to the Designer; rewrote `OnNew` and `OnEdit` to detect the shell.
- BUILD: ServiceContractPhotocopier.dll compiled clean. ATPShadowMain.exe copy-to-bin failed (`MSB3027`) because the user is still running ATPShadowMain.exe (PID 52556) and Visual Studio Remote Debugger has the dll locked. NOT a code error. User must close ATPShadowMain.exe and rebuild to pick up changes; not killed automatically per CLAUDE.md harness rule (no GUI-popping).
- Followups (other list forms with the same modal-ShowDialog +New/Edit pattern that should be migrated next): see grep audit below — every `*Lst_Form.cs` under Service Contract / Service Note / Appointment / General Setup / Stock Request likely follows this pattern. Pick them off as a follow-up sweep.

## 2026-07-13 — Contract editor epic: Part A done, Part B staged

User request (Image #101/#103/#104 + text): a large multi-feature pass on the Service Contract editor.
Delivered **Part A** this turn (buildable, committed); **Part B** below is the remaining large work,
staged here with a concrete plan.

### Part A — DONE (build green, committed)
- **Save/Close confirmation** on both the contract editor and the service item editor. Dirty tracking
  (`WireDirtyTracking` / `_dirty` / `_savedOk`) wired after load; `FormClosing` prompts "You have
  unsaved changes. Discard them and close?" when dirty & not saved. Codified as **CLAUDE.md rule 8**.
- Ribbon **"Add Service Item" → "Add a New Service Item"**.
- Service Items grid **Delete** now confirms before removing an item from the contract.

### Part B — STAGED (not yet built; needs decisions + schema)

**B-1. OPEN DECISION — "attach a service item with no contract" (blocks the +/- attach feature).**
Requirement 5 says the Service Items tab should have a `+` that adds a row where you pick a *Service
Item No* that is *not attached to any contract*, and a `-` that removes the selected item. BUT the
current schema makes `zSCP2_Item.ContractKey` NOT NULL (legacy split = one contract per CSSI), so no
item is ever "contract-less". Two ways to resolve — needs the user's call:
  (a) Make ContractKey NULLABLE: service items can exist standalone (created without a contract) and
      later be attached. Biggest change; matches the request literally. New: a way to create loose
      items, migration to allow NULL, "unattached items" picker for `+`.
  (b) `+` = move an EXISTING item from its (auto/bare) contract into this one (re-parent), deleting
      the now-empty source contract. No schema change; keeps "one item always has a contract".
Default if forced: (b) — least invasive. Confirm before building.

**B-2. Spare Parts / Services Provided tab (Image #101).** New tab AFTER "Service Item Under
Contract", a grid mirroring AutoCount's document detail grid:
  - Columns: No, Item Code, Description, Unlimited, UOM, Quantity, Discount, Unit Price, Amount,
    Tax Type, Tax Inclusive, Tax(%), Tax Amount, Amount After Tax. "Item Format: Standard GST" combo
    + Customize button (can stub Customize initially).
  - Buttons: Insert Row / Remove Row / Move Up / Move Down. Up/Down reorder logic: follow AutoCount's
    detail-grid pattern (swap the bound rows' Seq/Pos and re-sort; AutoCount uses a `Seq`/`Sequence`
    column and `MoveUp/MoveDown` on the entity — see FormItemBom `sbtnBomUp/Down` and the detail
    entry command in AutoCount source under AutoCount.Invoicing/*Entry).
  - New table: `zSCP2_ContractSparePart` (ContractKey FK, ItemCode, Description, Unlimited, UOM, Qty,
    Discount, UnitPrice, TaxType, TaxInclusive, TaxPct, Pos, ...). Migration + ScpMigrations + csproj.
  - Also appears on the **Service Item** form (spare parts bound to a service item). On the CONTRACT
    it must SHOW item-bound spare parts read-only (cannot delete if bound to a service item) but the
    user can still ADD contract-level spare parts. So the grid is a UNION of (item-bound, read-only) +
    (contract-level, editable).

**B-3. More Header tab (Image #103).** New tab with: City, Postal Code, State, Country, Phone, Fax,
Ref1-4, and a "Delivery Address" group (Branch Code + Search/Copy, Branch Name, Address(multi-line),
State, Country, Phone, Fax, Email, Contact Person, City, Postal Code). Needs new columns on
zSCP2_Contract (or a child table) + a migration. Branch "Search/Copy" mirrors AutoCount's delivery
address picker (can wire to dbo.BranchDeliveryInfo/DeliveryAddress later; stub Search first).

**B-4. Note / Remarks tabs.** Contract already has Remark1/Remark2/Note fields on the existing
"Remark" tab — restructure into "4. Note" + "5. Remarks" tabs to match the screenshot's tab set:
`1. Spare Parts/Services Provided | 2. Service Item Under Contract | 3. More Header | 4. Note | 5. Remarks`.

**B-5. "Add a New Service Item" mapping (Image #104 scenario).** When adding a new service item from
inside a contract, prefill the item editor with the shared contract fields and show the **contract as
read-only** (Contract No read-only). Partly in place (billing day is passed); finish the field-map
(customer/agent/etc. → item context) and lock the contract selector when opened from a contract.

## 2026-07-13 — Spare Parts tab BUILT + two decisions recorded

- DECISION (B-1): user chose **contract-less items** — make zSCP2_Item.ContractKey NULLABLE; `+`
  attaches items WHERE ContractKey IS NULL, `-` detaches (ContractKey=NULL, item survives). NOT yet
  built (the +/- attach UI + the nullable migration is the next item-model chunk).
- DONE: **Spare Parts / Services Provided tab** on the contract editor. New table
  zSCP2_ContractSparePart (contract-cascade FK only; ItemKey is a plain nullable link — a 2nd cascade
  FK to zSCP2_Item is illegal here, "multiple cascade paths", since Item already cascades from
  Contract). Grid columns per Image #101 (No/Item Code/Description/Unlimited/UOM/Quantity/Discount/
  Unit Price/Amount/Tax Type/Tax Inclusive/Tax %/Tax Amount/Amount After Tax) with Insert Row /
  Remove Row / Move Up / Move Down. Amount/Tax computed on edit. Item-bound rows (ItemKey set) show
  read-only and can't be removed on the contract. Save replaces only contract-level (ItemKey NULL)
  rows. Migration auto-creates the table on install (verified on AED_ATPTEST).
- STILL PENDING from the epic: item-form spare parts sub-grid (so item-bound lines exist);
  the "Item Format: Standard GST" combo + Customize button (cosmetic, not added); More Header tab
  (B-3); Note/Remarks tab split (B-4); +/- attach with the nullable model (B-1); add-item mapping
  (B-5).

## 2026-07-13 — NEW request (Image #105/#106): Copy / Clipboard ribbon for Service Contract
Add to the contract editor ribbon, mirroring AutoCount's document entry:
- **Copy group**: "Copy from other Service Contract" (load another contract's data into a NEW unsaved
  contract) + "Copy to a new Service Contract" (clone the current contract into a new one).
- **Clipboard group**: "Copy Whole Document", "Copy Selected Details", "Copy as Spreadsheet",
  "Paste Whole Document", "Paste Item Detail Only". Look at AutoCount's Form*Entry clipboard commands
  (AutoCount.Invoicing / document entry) for the serialization format + paste behavior. NOT built yet.

## 2026-07-13 — Contract-editor epic: remaining 4 items ALL done
- (4) Delivery Address Search (customer branches from dbo.Branch) + Copy (main address -> delivery). DONE.
- (2) "Add a New Service Item" from a contract shows Contract No READ-ONLY, billing day mapped. DONE.
- (3) Copy/Clipboard ribbon (Image #105/#106): Copy from other / Copy to a new Service Contract + Copy
  Whole Document / Copy Selected Details / Copy as Spreadsheet / Paste Whole Document / Paste Item
  Detail Only. Clipboard format = tagged tab-separated (ATP-SCP-DOC-V1); TSV is Excel-pasteable. DONE.
- (1) Service Item editor now has its OWN Spare Parts grid (Insert/Remove/Move Up/Down), stored in
  zSCP2_ContractSparePart with ItemKey set -> shows read-only on the contract's Spare Parts tab.
  Shared column layout + compute reused from the contract (ConfigureSpareView/CreateSparePartsTable/
  ComputeSpareRow made internal static). Contract save + standalone-item save both persist them. DONE.

## 2026-07-15 — Service Item overhaul (3 phases) + validation-agent findings
Rebuilt zSCP2_Item_Form entirely in code (no strict-designer surgery): header Item Code + Grade SLUs,
removed Stock Location + Provided Items grid, 6-tab body (Item & Meter / Preventive / More Header /
Debtors Ownership History / Note / Remarks), ribbon Clipboard group. ~35 new zSCP2_Item columns +
PersistItemExtras (shared by both save paths). Preventive auto-computes Next Service Date; Debtor
History auto-records on customer change.

Validation agent (Explore) reviewed it: 5/6 checks PASS. One CRITICAL bug found and FIXED:
- **C1**: RecordDebtorHistory/BuildDebtorHistoryTab used the legacy zSCP_ServiceItemDebtorHistory whose
  FK targets the v1 zSCP_ServiceItem, but we passed a v2 zSCP2_Item.ItemKey -> on a fresh DB the FK is
  unsatisfiable, so EVERY item save with a customer failed and rolled back. Fixed by creating a proper
  v2 table zSCP2_ItemDebtorHistory (FK -> zSCP2_Item.ItemKey, ON DELETE CASCADE), capturing
  Grade/ContractNo at record time, and repointing both read + write. Verified an ItemKey insert
  succeeds (previously threw).
- Minor (not fixed, cosmetic): same-day double debtor change yields a zero-length period; openDebtor
  trim/case now symmetric after the fix.

---

## 2026-07-15 — Reference No (both forms) + Service Item header now mirrors the Contract

**Request:** Service Item header must have the SAME fields as the Contract, EXCEPT the billing
checkboxes (Last-day-of-month, Billing Mode) and Contract Value; and BOTH forms were missing a
**Reference No** field.

**Delivered:**
- Migration `SQL/02_Update_zSCP2_ItemContract_v7_RefAndContext.sql` (idempotent): `ReferenceNo` on
  `zSCP2_Contract`; and on `zSCP2_Item`: `ReferenceNo, ContractTypeCode, StaffCode, ServiceStartDate,
  Address1, Attention, Phone, TermCode, AreaCode` (ServiceExpiryDate already existed). Registered in
  `ScpMigrations_Cls` (RunDDL) + embedded in csproj. Applied to AED_ATPTEST — all 10 cols verified.
- **Contract** (`zSCP2_Contract_Form`): added Reference No (LblRefNo/TxtRefNo, left col y=265) wired
  through Insert/Update/AddContractParams(@refno)/LoadContract.
- **Service Item** (`zSCP2_Item_Form`): header rebuilt in code to a dense 2-column contract-like
  layout. Added Contract Type (SLU + "+" create → ServiceContractType_Form), Reference No, Service
  Start Date [To] Expiry (DateEdits), Agent (SLU over SalesAgent), Address (memo), Attention, Phone,
  Term, Area. New `ItemEditData` fields + `PersistItemExtras`/`LoadExtrasFromDb` extended. Context
  fields inherit from the parent contract via COALESCE JOIN (existing items) or `PrefillContextFromContract`
  (new embedded items); Reference No stays item-specific. Standalone Contract/Customer row moved to
  y=417; tab control moved to y=448. Expiry≥Start validation added.

**Verification:** msbuild green; migration applied + columns present; plugin reinstalled into full
AutoCount (OPTION A) with menu registered, no exceptions; both editors captured via the PREVIEW
single-form path and render correctly (no overlaps, all fields present, 6 tabs, ribbon intact).

**Validation agent (Explore):** no correctness bugs. Confirmed SQL↔param 1:1 in PersistItemExtras,
unambiguous i./c. qualification + alias resolution in the LoadExtrasFromDb JOIN, all 10 new item
controls read into `_data` in BtnOK_Click, header coordinates collision-free, and Reference No
consistent across contract Insert/Update/Params/Load with correct param count. Behavioral note:
opening+saving an item solidifies inherited contract context onto the item row (intended).
