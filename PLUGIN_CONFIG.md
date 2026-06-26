# ATP Plugin Config — single source of truth

> 📝 **After editing this file:** tell Claude *"regenerate appp"* — Claude rewrites
> `ServiceContractPhotocopier/ServiceContractPhotocopier.appp` from the values below.
> Then run `build-and-install.bat` to build + package + install.
>
> ⚠️ The **Guid** must NEVER change after first install — it identifies the plugin to AutoCount.

## Identity
- **Guid:** `6A996121-169E-4D35-AEED-58CFBB1386B7`   <!-- DO NOT CHANGE -->
- **Name:** Service Contract Photocopier
- **Version:** 1.4.4.0
- **MinimumAccountingVersion:** 2.0.2
- **ScriptLanguage:** C#
- **ProjectFileVersion:** 1.0

## Vendor
- **Manufacturer:** RUISIN PLASTIC INDUSTRIES SDN BHD
- **ManufacturerUrl:** https://www.newpages.com.my/v2/cn/company/190550/Rui_Sin_Plastic_Industries_Sdn__Bhd_.html
  <!-- NOTE: this URL is validated by AppBuilderCmd against AutoCount's developer registry. Do NOT change without re-registering. -->
- **Copyright:** Copyright ©  2026 RUISIN PLASTIC INDUSTRIES SDN BHD
- **SalesPhone:** 0167166663
- **SupportPhone:** 0167216749

## Description
- **Description:** This Plugin handles Service & Contract management for the Photocopier business in AutoCount.
- **WhatsNew:** |
    v1.4.4.0 (2026-06-26):
    1. Stock Transfer change/update: a re-sent RequestId still approved (Yes) but with a different qty is flagged "Update" and "Approve Change" updates the existing transfer document (approval=No remains "Cancel"). Mirrors the Stock Issue change/cancel flow.
    2. UI: colourful toolbar icons; flat (non-gradient) row highlighting; a row-colour legend (Normal / Update / Cancel); a "Hide Ignore" filter option.

    v1.4.3.0 (2026-06-19):
    1. Stock Issue change/cancel: when the same StockIssueId is received again with a different quantity, the row is flagged "Update" (yellow); an "Approve Change" button applies the new quantity to the existing AutoCount Stock Issue document. If the re-sent quantity is 0, the row is flagged "Cancel" (red) and approving cancels the document. A re-sent id can no longer generate a duplicate document via Generate.

    v1.4.2.0 (2026-06-19):
    1. Stock Transfer revoke/cancel: when the same RequestId is received again with approval=No after a transfer was already generated, the row is flagged "Cancel Requested" (red). A new "Cancel Transfer" button cancels the AutoCount Stock Transfer document (marks it Cancelled, reverses stock) and sets the rows to Cancelled. An approval=No row can never generate a transfer.

    v1.4.1.0 (2026-06-19):
    1. Customer release: menu trimmed to Stock Request Task + About / Check for Updates. Other modules are hidden (menu only — all code remains; re-enable by uncommenting the [MenuItem] attribute).

    v1.4.0.0 (2026-06-19):
    1. Combined Service Contract module v2 (zSCP2_*) — one contract per customer with service items inline; native AutoCount list + ribbon editor with debtor auto-fill.
    2. Meter Reading Integration — fetch meter readings from the PUMS API with one swappable client; ONLINE and OFFLINE machine types via a single interface; grouped grid (by contract) with per-contract colour bands, footer totals, and With Meter Data / Online / Offline / No API Data / Conflicts / All tabs.
    3. Manual key-in + staging (zSCP2_MeterEntry): type readings for machines with no API data and Save; values persist per billing period and survive restart.
    4. Re-fetch conflict handling: saved manual readings are kept and flagged when the API later returns a value; double-click a contract to review Manual vs Fetched and accept the override.
    5. Billing-day-driven invoice generation (group per contract or separate per item) reusing the AutoCount invoice pipeline.
    6. Stock Request — webhooks now capture the PUMS "Serial Number" on Stock Issue; unknown items are clearly flagged (Item OK? column + clear error, no silent master-data changes); technician names missing from Stock Location can be auto-created from a one-click banner; View Log timestamps fixed to local time.

    v1.3.0.0 (2026-05-04):
    1. Meter Type Transaction Entry — Invoice No Format combo now reads from AutoCount's standard DocNoFormat table (DocType "IV") using the [Name] column and auto-selects the IsDefault='T' format on load.
    2. New ellipsis (…) button next to Invoice No Format opens AutoCount's standard FormDocumentNoMaintenance for in-place format management; combo refreshes on close.
    3. The chosen format name is now stamped onto Invoice.DocNoFormatName before the Invoice Entry dialog opens, so AutoCount uses that format's running number for DocNo (matching FormInvoiceEntry behavior).
    4. New ATPShadowMain dev launcher — a NavBar-driven home form replaces the hard-coded single-form launch in Program.cs; lists every plugin form grouped by module so dev iteration no longer requires editing Program.cs.

    v1.2.0.0 (2026-04-17):
    1. ServiceItem_Form.Designer.cs rewritten to canonical VS-compatible format (one field per control, ISupportInitialize pairs, SuspendLayout/ResumeLayout, no helpers, no var) — form now opens in Visual Studio Design view.
    2. Tab 7 Meter Type grid column widths re-balanced with ColumnAutoWidth; all 10 columns visible in one view without horizontal scroll.
    3. Grade Code dropdown now populates — new seed migration 04_Seed_zSCP_LK_ServiceItemGrade (A, B, C, REFURB). Job lookup switched from [Job] (absent in AutoCount 2.x) to [Project].
    4. Fill Test Data orange button on both forms now populates every field including Tab 2 More Header and auto-adds one row to each child grid (Meter Type / Spare Parts / Service Items). Guarded against duplicate-row on repeat clicks.
    5. Service Contract: new "Auto (F12)" button next to Contract No (mirrors Service Item's Auto Tag) — one-click next running number.
    6. Bilingual (EN + 中文) demo guide at Docs/demo-service-module.md with full field-by-field explanation, 18-minute demo script, FAQ, and analysis of how V8 master DB actually uses Item Code vs Service Item Code.

    v1.1.0.0 (2026-04-16):
    1. Maintain Service Item & Maintain Service Contract brought to V8 layout parity — full header, More Header tab, Tab 1 (Department / Job / Location + Next Service Date), and 10-column Meter Type grid.
    2. Full CRUD lifecycle with transactional save/delete, optimistic concurrency via LastModified, dirty tracking, OnClosing save prompt.
    3. Audit columns (Created/Modified/CreatedBy/ModifiedBy) via migrations v1.2.0 and v1.3.0.
    4. ATPCli (atp.exe) CRUD CLI for headless testing; 52-case regression suite under tests/.

## Build
- **CsprojPath:** `ServiceContractPhotocopier\ServiceContractPhotocopier.csproj`
- **ApppPath:**   `ServiceContractPhotocopier\ServiceContractPhotocopier.appp`
- **OutputApp:**  `ServiceContractPhotocopier\ServiceContractPhotocopier.app`
- **Configuration:** Debug
- **Platform:** AnyCPU
- **AssemblyFile:** `ServiceContractPhotocopier.dll`   <!-- TODO: likely rename to VecTech.SCPACPlugin.dll to match VecTech naming convention — confirm with user, then regenerate .appp -->
- **BinDir:** `.\bin\Debug`

> ⚠️ **PENDING RENAME:** main DLL is currently `ServiceContractPhotocopier.dll` but will likely be renamed to `VecTech.SCPACPlugin.dll` to match the VecTech.* convention used by ACPluginBase / BHACPlugin / KHACPlugin. When confirmed, update `AssemblyFile` + the two `Files to package` entries + `CsprojPath`/`ApppPath`/`OutputApp` if the folder name also changes, then regenerate the .appp.

## Files to package
> One entry per DLL/PDB to ship inside the .app.
> Paths are relative to the .appp file (i.e. relative to the csproj folder).
> Always include the main plugin DLL/PDB and the AC plugin base DLL/PDB.
> Add DevExpress / WPF / third-party DLLs only if the plugin actually depends on them.

- `.\bin\Debug\ServiceContractPhotocopier.dll`
- `.\bin\Debug\ServiceContractPhotocopier.pdb`
- `.\bin\Debug\VecTech.ACPluginBase.dll`
- `.\bin\Debug\VecTech.ACPluginBase.pdb`
