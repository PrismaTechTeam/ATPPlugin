# ATP — AutoCount Plugin (Service & Contract for Photocopier)

## 🛡️ Standing rules (ALWAYS follow)

1. **Database-change policy.** Direct changes to MSSQL `AED_ATPLUGIN001` are allowed (the plugin runs migrations on load via `ScpMigrations_Cls`), BUT every schema change MUST be captured in a versioned `.sql` file under `ServiceContractPhotocopier\SQL\` — no ad-hoc SSMS changes without a corresponding file. File naming: `02_CreateTable_zSCP_<Name>.sql` for new tables, `02_UpdateTable_zSCP_<Name>_v<x.y.z>.sql` for alterations, `03_CreateView_zvSCP_<Name>.sql` for views, `04_Seed_*.sql` for seed data.
2. **Form triple-file rule.** Every new Windows Form MUST ship as a triple: `<FormName>.cs` + `<FormName>.Designer.cs` + `<FormName>.resx`. Single-file placeholder forms are forbidden for shipping code. The designer file contains `InitializeComponent()` and control field declarations; `.resx` is kept valid even if empty. Csproj entries must include `<SubType>Form</SubType>`, `<DependentUpon>` on the designer, and `<EmbeddedResource>` on the resx.
3. **Harness engineering / 不断自我演进.** When doing implementation work in this repo, proceed in a self-correcting loop: write → compile → read error → fix → re-compile → advance. Do not stop and ask the user between steps; push through to a working build. Any unresolved issues that would normally become a question go into `C:\Dev\Plugin\ATP\NOTES.md` as a note-to-self instead, and the session continues with the best judgment default.
4. **Business-logic validation agent.** After any large build (multiple new forms + schema changes), launch an Explore subagent to review the generated forms, data layer classes, and DB schema for business-logic correctness and relationship integrity (FK consistency, cascade behavior, missing validations, orphaned references). The agent's findings are appended to `NOTES.md` under a dated section. This is a standing convention, not a one-shot.
5. **Form standardization.** When two or more forms have substantially identical UI (e.g. the 9 lookup editors `zSCP_LK_*`), they MUST share a base class (e.g. `SimpleLookupLst_Form` + `SimpleLookupEdt_Form` in `Classes\BaseForms\`) or be generated from the same template. No duplicated hand-written variants of the same layout.
6. **100% UI fidelity to `UI/` screenshots.** Before writing or modifying any form, READ the matching screenshot(s) in `C:\Dev\Plugin\ATP\UI\<submodule-folder>\`. The finished form's field labels, layout order, tab names, button positions, and grid columns MUST match the screenshot pixel-for-pixel (within DevExpress skin limits). If a screenshot shows 4 tabs named "1.Main / 2.Service Item / 3.Spare Part / 4.Remark" — the form has those 4 tabs with those exact names, in that order. If the screenshot shows a lookup edit for DebtorCode with a button next to it — the form has a LookUpEdit (not TextEdit) with that button. Check the screenshot BEFORE coding, not after. Re-read if uncertain.
7. **VS Designer-compatible `.Designer.cs` format (STRICT).** The Visual Studio WinForms Designer does NOT execute `InitializeComponent()` — it parses it with a deterministic code-DOM parser and silently bails (showing "the designer could not be shown for this file") on any pattern it doesn't recognize. Every `.Designer.cs` in this repo MUST follow these rules so Design view renders:
   - **One field per control.** Every control, grid column, repository item, and image collection is declared as its own `private` field at the top of the class (e.g. `private DevExpress.XtraGrid.Columns.GridColumn ColSPNo;`). No arrays, no lists, no dictionaries of controls.
   - **No helpers, no loops, no `var`.** `InitializeComponent()` is a flat sequence of `this.X = new ...();`, `this.X.Property = value;`, `this.Parent.Controls.Add(this.X);`. No `SetupLbl(...)`, `AddCol(...)`, `foreach`, `for`, LINQ, ternary assignments, method extraction, or `var` locals. Every local is an explicit type. If you catch yourself writing a helper to "reduce duplication" in the designer file — stop; the parser needs the duplication.
   - **`ISupportInitialize` pairs.** Every `GridControl`, `GridView`, `RepositoryItem*`, `*Edit.Properties`, `XtraTabControl`, `LayoutControl`, `BarManager`, etc. gets `((System.ComponentModel.ISupportInitialize)(this.X)).BeginInit();` at the top of `InitializeComponent()` and a matching `EndInit();` at the bottom, in mirrored order. Missing BeginInit/EndInit is one of the top reasons Design view fails.
   - **`SuspendLayout()` / `ResumeLayout(false)` / `PerformLayout()`** wrap the whole body on the form and every container (`TabPage`, `Panel`, `GroupControl`, `LayoutControl`).
   - **Designer-generated header comments preserved.** Keep the `/// <summary> Required designer variable. </summary>`, `protected override void Dispose(bool disposing)`, and `#region Windows Form Designer generated code` / `#endregion` exactly as VS emits them.
   - **Code-behind (`<Form>.cs`) also avoids `var`.** Use explicit types throughout the partial class too (matches the Designer's style and keeps diffs reviewable).
   - **Test after every Designer edit.** After modifying `<Form>.Designer.cs`, reload the form in VS (close the tab, reopen, switch to Design view) and confirm it renders. If it fails, the error pane at the top of Design view names the offending line — fix before continuing. A green `msbuild` is NECESSARY but NOT SUFFICIENT; the C# compiler accepts patterns the Designer parser rejects.
   - **Reference pattern:** `ServiceContractPhotocopier\Service Contract\Operation Forms\ServiceContract_Form.Designer.cs` (986 lines) is the canonical example — copy its structure for any new complex form with grids.

8. **Save / Close confirmation (mirror AutoCount entry behavior).** Every create/edit form (contract editor, service item editor, and any future entry form) MUST mirror AutoCount's create/edit UX: a `Save` action that persists + clears the dirty flag, and a `Close` that — if there are **unsaved changes** — prompts "You have unsaved changes. Discard them and close?" (Yes closes, No cancels). Implement with a `_dirty` flag set on any field/grid edit (wired AFTER the initial load so loading a record doesn't set it), a `_savedOk` flag set on a successful save, and a `FormClosing` handler that cancels the close when `_dirty && !_savedOk` and the user answers No. Never lose user input silently on close. Reference: `zSCP2_Contract_Form.cs` (`WireDirtyTracking` + `OnFormClosing`).

9. **Lookup fields: use `SearchLookUpEdit`, never `LookUpEdit`.** Any field that picks from a master/list (Customer/Debtor, Contract Type, Agent, Meter Type, Department, Project, Item Code, etc.) MUST be a `DevExpress.XtraEditors.SearchLookUpEdit` (with a `GridView` popup + auto-filter row so the user can search), NOT a `LookUpEdit`. `LookUpEdit` gives only a cramped combo with no real search; `SearchLookUpEdit` gives a searchable grid popup. Each `SearchLookUpEdit` needs its own `GridView` field, `ISupportInitialize` BeginInit/EndInit on both the `.Properties` and the view, `Properties.PopupView = <view>`, and columns configured on the popup view (populate + hide all but the code/description columns). For a full modal "search everything" picker (large lists, multi-column search) use `Classes\CommonForms\AdvanceSearch_Form.Pick(...)` instead. Do NOT introduce new `LookUpEdit` controls; convert any existing ones when you touch them.


## 🚀 Daily workflow — TWO LOOPS

### ⚡ Inner loop: dev iteration (USE THIS 99% OF THE TIME)
**`ATPShadowMain/`** is a WinExe dev launcher. It builds the plugin via ProjectReference, boots the AutoCount runtime in-process, programmatically logs in via `App.config`, and opens a plugin form directly. **No `.app`. No Plug-in Manager. No login dialog. Zero clicks.**

```
1. Edit code in ServiceContractPhotocopier/Operation Forms/FormXxx.cs
2. Build + run:
     msbuild ATPShadowMain\ATPShadowMain.csproj /v:m
     ATPShadowMain\bin\Debug\ATPShadowMain.exe
   (or just F5 in Visual Studio with ATPShadowMain as startup project)
3. Form opens. Iterate. Close. Repeat.
```

To switch which form opens, edit `ATPShadowMain/Program.cs` → `Run()` → change `new FormServiceContract(...)` to `FormMachineMaster` / `FormServiceJob` / etc.

`bin\Debug\shadowmain.log` shows assembly resolves, login result, exceptions.

**How it works (don't break this):**
- `AppDomain.AssemblyResolve` redirects AutoCount + DevExpress lookups to `C:\Program Files\AutoCount\Accounting 2.2\` so the exe runs from its own bin\Debug without copying DLLs.
- All AutoCount-using code lives in `Run()` marked `[MethodImpl(MethodImplOptions.NoInlining)]` so the resolver is registered before JIT touches AutoCount types.
- DB + login creds come from `ATPShadowMain/App.config` (`DBSetting.*` + `AutocountLogin.*`). Currently: `localhost,1433` / `AED_ATPLUGIN001` / `sa` / `rs6663` / `ADMIN` / `ADMIN`.

### 🚚 Outer loop: real install (FIRST TIME ONLY, then only when shipping to client)
The ShadowMain inner loop only works if the plugin Guid has been registered with the AutoCount account book at least once. To do that:

```
1. Edit PLUGIN_CONFIG.md (only if metadata changed) → tell Claude "regenerate appp"
2. Double-click build-and-install.bat
3. Click Install in the AutoCount Plug-in Manager dialog
```

After this one-time install you NEVER need to run `build-and-install.bat` again until you ship a new release to a client. All dev iteration is via ShadowMain.

**Required in `PluginMain` constructor (don't remove):**
```csharp
SetMinimumAccountingVersionRequired("2.0.2");
SetDevExpressComponentVersionRequired("22.2.7");  // AC 2.1.7.22+ rejects without this
```

Never edit the `.appp` by hand. Never open GUI AppBuilder.

### Auto-login internals
- `tools\save-credentials.ps1` — prompts for UserID/Password, encrypts password via DPAPI (`ConvertFrom-SecureString`), writes `%APPDATA%\ATP\autocount-login.xml`. Only the current Windows user can decrypt it.
- `tools\auto-login.ps1` — kills running `Accounting.exe`, launches it again, waits up to 60s for the login window, brings it to foreground, then `SendKeys` UserID → Tab → Password → Enter. Special chars in password are escaped for SendKeys.
- **Why SendKeys (not a CLI flag):** `Accounting.exe` accepts no command-line credentials and `FormAutoCountLogin` has no remember-me checkbox (verified against AutoCount source `AutoCount.MainEntry/FormAutoCountLogin.cs`). SendKeys is the only practical option short of a full UIAutomation driver.
- **Known limitation:** if AutoCount shows a non-login dialog first (license warning, "what's new", multi-account-book picker), SendKeys may type into the wrong window. If that happens, dismiss the extra dialogs and re-run `tools\auto-login.ps1` directly.

## Overview
ATP is an AutoCount plugin project focused on **Service & Contract** management for a photocopier business. It is built on top of the user's AutoCount plugin base framework.

## Reference Projects

- **`VTACPluginBase/`** (in-tree) — The user's AutoCount plugin base project (`VTACPluginBase.csproj`), referenced by `ServiceContractPhotocopier.csproj` via ProjectReference. Contains `PlugIn_Cls.cs`, `MainPI_Form`, Base Forms, Common Forms, Classes, Examples. Forked into ATP from `scchang1127/AutoCount-Plugin-Base` — edit freely; no upstream sync.
- **`../AutoCount-Plugin-Sample-BookHub`** (sibling, read-only) — A recent sample plugin (`BookHubACPlugin.sln`) showing how to consume the plugin base. Includes `VTSMainAutocountPlugin`, `VTSSubAutocountPlugin`, `TestSamplePlugins`. Use as the worked example for wiring up an ATP plugin.

## Databases

Two databases are in play. Know which one to query for what.

### 🎯 Plugin target DB — MSSQL (read AND write)
This is the AutoCount Accounting 2.x account book the plugin actually runs against. Same creds are in `ATPShadowMain/App.config`.

| Field    | Value             |
|----------|-------------------|
| Engine   | SQL Server 2022   |
| Host     | `localhost,1433`  |
| Database | `AED_ATPLUGIN001` |
| User     | `sa`              |
| Password | `rs6663`          |

```
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPLUGIN001 -Q "..."
```

Stock AutoCount 2.x ships with ~373 standard accounting tables (AR/AP/GL/Stock/etc.) but **no service / contract / meter / appointment / machine tables**. Those are what this plugin must create via SQL migrations in `ServiceContractPhotocopier/SQL/` and run from `PluginMain.BeforeLoad`.

### 📚 Schema reference DB — MariaDB (READ-ONLY)
A full AutoCount V8 account book that already has the official Service & Contract module schema. Use it as the **source of truth** when designing the plugin's table layout — column names, types, FKs, lookup tables, etc. Mirror these schemas (translated to T-SQL) into the MSSQL plugin DB above. Do NOT write to this DB.

| Field    | Value            |
|----------|------------------|
| Engine   | MariaDB 10.11    |
| Host     | `127.0.0.1:3309` |
| Database | `v8_atp_main`    |
| User     | `root`           |
| Password | `rs6663`         |

```
"C:\Program Files\MariaDB 10.11\bin\mysql.exe" -h 127.0.0.1 -P 3309 -u root -prs6663 v8_atp_main -e "..."
```

Relevant tables to mirror:
- **Contract**: `servicecontract`, `servicecontractid`, `servicecontractserviceitem`, `servicecontractsparepart`, `servicecontracttype`
- **Item / Meter**: `serviceitem`, `serviceitemgrade`, `serviceitemdebtorhistory`, `serviceitemmetertype`, `serviceitemmetertrans`, `metertype`, `metermultiprice`, `metermultipriceitem`
- **Note**: `servicenote`, `servicenoteid`, `servicenoteitem`, `servicestatus`, `serviceseverity`, `servicesolution`, `serviceproblem`, `servicetype`
- **People**: `serviceperson`, `serviceadvisor`, `mechanic`, `supervisor`
- **Appointment**: `appointment`, `appointmenttype`, `appointmentpriority`

## Scope
- Domain: Photocopier **Service & Contract** module
- Type: AutoCount plugin (extends AutoCount Accounting)
- Related app in this folder: `4. AutoCount Accounting Photocopier Management System 1.9.0.1.app`, `Photocopier/`

## Toolchain & Automation

One-click build+package+install via `build-and-install.bat` at the project root. Edit the CONFIG block (CSPROJ / APPP / OUTPUT_APP) once the csproj exists.

Pipeline:
1. **MSBuild** the plugin csproj → `bin\Debug\<plugin>.dll`
   - `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`
2. **AppBuilderCmd** packs the `.appp` project into `.app`
   - `C:\Program Files (x86)\AutoCount\Development\AppBuilder 2.1\AppBuilderCmd.exe <project.appp> <out.app>`
   - Source: `C:\Dev\Autocount\PluginBuilder\AppBuilderCmd\` (uses `AutoCount.Application.AppBuilder.BuildPackage`)
   - Optional auth: `-d developerId::password` before the appp arg
3. **Install** by launching the `.app` (file association → AutoCount Plug-in Manager install dialog)
   - Underlying API used by AutoCount UI: `PlugInManager.InstallPackage(dbSetting, appFilePath, overwriteSameVersion: true)` (see `C:\Dev\Autocount\AutoCount.Tools\AutoCount.PlugIn\FormPlugInManager.cs:723`). Fully silent install would require a small helper that supplies a `dbSetting` and calls this directly — future improvement.

### `.appp` project file (hand-written, no GUI)
The `.appp` is a plain XML manifest — no need to open GUI AppBuilder. We maintain it from a single source of truth and have Claude regenerate it on demand.

**Source of truth:** `PLUGIN_CONFIG.md` in this folder. All plugin properties (Guid, Name, Version, Manufacturer, Description, WhatsNew, AssemblyFile, Files list, etc.) live there.

**Generated file:** `ServiceContractPhotocopier/ServiceContractPhotocopier.appp` — DO NOT hand-edit. Edit `PLUGIN_CONFIG.md` and ask Claude to regenerate.

**Templates:**
- `templates/minimal.appp` — copy-paste starter for a new plugin (placeholders: `{{GUID}}`, `{{NAME}}`, `{{MAIN_DLL}}`, `{{MAIN_PDB}}`, `{{DESCRIPTION}}`)
- `templates/example.appp` — kitchen-sink reference showing every supported field plus the full DevExpress 19.2 / WPF / StdDXForms file list (modeled on the EPPI/Kian Heng plugin)

**Other real-world references** (read-only):
- `../AutoCount-Plugin-Sample-BookHub/VTSMainAutocountPlugin/BookHubMainAccBookPlugins.appp` — minimal real example
- `C:\Users\ndscd\OneDrive\Documents\OneDrive\Project\Work\PS09_Starfox\EPPI_AutoCount_Plugin\PrismaACPlugin\EPPIMESPluginsAnyCPU.appp` — full DevExpress example

**Required fields** (validated by `AppBuilderCmd`): `ProjectFileVersion`, `Guid`, `Name`, `Version`, `Manufacturer`, `AssemblyFile` (main dll filename), `MinimumAccountingVersion`, `ScriptLanguage`. Each shipped file goes in its own `<Files><Filename>.\bin\Debug\xxx.dll</Filename><ExecuteAfterExtracted>false</ExecuteAfterExtracted></Files>` block. Paths are relative to the `.appp` file's folder.

**Generate a new Guid:** `powershell -NoProfile -Command "[guid]::NewGuid().ToString().ToUpper()"` — Guid is permanent, never change after first install.

### Related dev resources
- `C:\Dev\Autocount` — full AutoCount source (read-only reference; only browse when needed)
- `C:\Dev\Autocount\tools\autocount-mcp\AutoCountMcp` — `ac` CLI / MCP for AutoCount data ops (no plugin-install command yet — could add one wrapping `PlugInManager.InstallPackage`)
- `C:\Dev\Autocount\PluginBuilder` — source for AppBuilder/AppBuilderCmd/AppViewer

## Working notes
- When starting implementation, pull structure/patterns from the in-tree `VTACPluginBase/`, and follow the integration style demonstrated in `../AutoCount-Plugin-Sample-BookHub`.
- Keep ATP focused on Service & Contract features; don't re-implement base framework concerns — reference them.
