# ATP Plugin Config — single source of truth

> 📝 **After editing this file:** tell Claude *"regenerate appp"* — Claude rewrites
> `ServiceContractPhotocopier/ServiceContractPhotocopier.appp` from the values below.
> Then run `build-and-install.bat` to build + package + install.
>
> ⚠️ The **Guid** must NEVER change after first install — it identifies the plugin to AutoCount.

## Identity
- **Guid:** `6A996121-169E-4D35-AEED-58CFBB1386B7`   <!-- DO NOT CHANGE -->
- **Name:** Service Contract Photocopier
- **Version:** 1.5.0.10
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
    v1.5.0.10 (2026-09-23) — the contract header, which copies a minimum counts, tier pricing two ways, rental in advance, and a tidier run:
    1. On a contract, the Address box is taller and the Description box shorter, so a four-line address reads without scrolling.
    2. Meters / Pricing, Minimum / waive: a minimum charge can count black only, colour only, or both, and black and colour can each have their own. Set it from the row it is for: the Black copies row's ... sets the black minimum, the Colour copies row's ... the colour minimum - black at least 200.00 and colour at least 100.00, each topped up on its own copies - and "black and colour" sets one minimum for the two, shown on both rows. The invoice line says which copies it measured (MINIMUM 200.00 black - copies 150.00 - short 50.00).
    3. Meter Invoice Run leaves out a contract that has not started yet: while its start date is still in the future it is off Ready to Invoice and Need Manual Key-In, and it appears on the day it starts.
    4. The day a keyed reading was taken can be corrected: Last Audit Date is now typed in, on the rows whose reading was keyed by hand. The invoice prints that day and the next period counts from it. A reading that came from the machine, a locked billing-day snapshot and an invoiced period keep their dates.
    5. "Key in myself", in Meter Invoice Run (and on the right-click menu of the Meters view): the machine broke down, was swapped or reported a stale counter, so its reading becomes the operator's -- same number, same date, but the date can then be corrected and the next fetch raises a conflict instead of overwriting it.
    6. Reference No for the invoice: in Meter Invoice Run, pick an invoice and type its Reference No once, under Invoice date (up to 30 characters, e.g. the slip or report number). Generate prints it as the invoice's Ref. Left empty, the Ref is what it always was - PUMS's report no. for a fetched reading, else the machine or contract no. The readings grid shows PUMS's report no. per meter as PUMS Report No, and it is not typed there.
    7. The contract's machine grid has a Branch column beside Bill Group: each machine can take its own branch of the customer (the branches registered in AutoCount, A/R > Debtor > Branch tab), and picking one also fills the machine's delivery address. Empty = the machine follows the contract's branch.
    8. Transfer Machine to DO: on a saved contract, press Delivery > Transfer Machine to DO. A list shows every machine of the contract with its model, serial and whether it can go out; the ones that can are ticked, and a machine with no serial, not in stock or already on a DO cannot be ticked and says why. Untick what stays behind, pick the DO date and press Create DO: one delivery order for them, each machine at no price with its serial, the stock checked again as it is written. It then asks whether to open the DO. The machine grid and the contract list show each machine's / contract's DO, or an amber No DO yet; double-click that DO to open it in AutoCount's Delivery Order screen. Deleting or detaching a machine that is on a DO warns that the DO and the stock do not follow it.
    9. The invoice date is the due date only by default: in Meter Invoice Run each invoice's date can be moved within its month, or set to its last reading's date with one button -- a machine that broke down on the 15th is billed to the 14th and dated the 14th. The list shows the date each invoice will carry, in blue when moved.
    10. Tier pricing, two ways, chosen per machine: the tier price window (the meter's price button, or Tier pricing in Meters / Pricing) has a Tier pricing choice under the tiers, so two machines on one contract - or a machine's black and colour - can each price their tiers their own way. "One price for all copies" (every copy at the price of the tier the month reaches) is the rule every contract has today: 1,648 copies with 100 free, on "up to 1,000 at 0.024, then 0.020", bill 1,548 x 0.020 = 30.96. "Split price by tier" bills 1,000 x 0.024 + 548 x 0.020 = 34.96, and the invoice prints one row per tier. Free copies are the meter's Free Qty on the contract's meter grid, with a tier price or without; a tier price can no longer start at 0.00. A tier price that started with free copies (a 0.00 first tier) is converted when this version first opens the book: those copies become the meter's Free Qty, and every invoice comes to the same amount as before.
    11. Rental in advance, chosen per contract (Billing > 1. The invoice > Rental billed). "With the month's copies" is the rule every contract has today. "In advance - one month ahead": the month before the contract starts bills the first month's rental alone, every bill after it carries that month's copies and next month's rental - "MONTHLY RENTAL (2/36) NOV 2026" - and the last month bills copies only. A machine added after the bill that should have carried its first month pays that month and the next on its next bill. Once a rental of the contract is invoiced the setting is locked, so no month is billed twice or missed. In Meter Invoice Run every rental row says which of its months it bills, in a Rental for column: 2/36 - OCT 2026 in advance, 22/36 - SEP 2026 with the month's copies.
    12. Meters / Pricing keeps the invoice split you pick. On a contract billed one invoice per machine, choosing "a rental invoice and a meter invoice" or "one invoice for everything" came back as per machine; it now saves as chosen. Cancel in Meters / Pricing now really discards: a waive or a minimum set there, or a waive taken away by choosing a split that bills the rental apart, is put back.
    13. Inter-Billing: a contract's Readings list also shows what has no counter - each machine's rent (with the month it pays, Rental - 1/37 - SEP 2026), a minimum and a waive - with its amount and the invoice it went on, so the list shows everything the contract bills, not only the copies.
    14. Inter-Billing: taking a contract brings its whole deal from head office - free copies and rebate, tier prices (one price or split by tier), the waive's conditions, a black or colour minimum, the rental's months, each machine's own billing day and dates, line prices and the contract's strategy rules - with every sum of money through the margin. A contract whose meter types have no item code in this book is not taken until each has one, and the board shows No item code on a contract that would bill a line without one. When head office changes a counter's terms, the board says so and Take HQ's terms brings them over.
    15. Meter Invoice Run: a free rental month printed on the invoice at 0.00 counts down the free months left, the same as one stamped RENTAL FREE - the rent no longer stays free after the agreed months.

    v1.5.0.9 (2026-09-22) — Meter Invoice Run shows the minimum and the waive:
    1. Meter Invoice Run works out the committed minimum's top-up and the rental waive from the readings, the way the invoice will: Total Charges and the invoice Amount show them before Generate, and a new reading updates them at once.
    2. A copy line with a rebate, on a contract on the new layout, shows the same cents as its invoice (3,710.00, where it showed 3,709.94).
    3. For testing: Ctrl+Shift+U in Meter Invoice Run writes the TEST Fetch JSON for the picked invoice's contract, to copy; Ctrl+Shift+T shows TEST Fetch on the Meters view wherever the focus is.

    v1.5.0.8 (2026-09-22) — Machine Serial says when there is none in stock:
    1. The Machine Serial list is the item's serials in AutoCount's stock. When a model has none (never received with serial numbers) the cell now says so - "No BIZHUB 651I serial in stock - type it, or receive it in AutoCount first" - instead of opening an empty list.

    v1.5.0.7 (2026-09-22) — a machine is a serial-numbered item:
    1. A machine's Item Code - on the contract's machine list and in the service item - offers only the stock items with Has Serial No ticked. A machine saved earlier on another item keeps it and still shows it. Toner and spare parts keep the full list.
    2. Plugin Option > 4. Contract & Item No. has the tick for it: untick to list every stock item again.

    v1.5.0.6 (2026-09-22) — Find Service Contract:
    1. Maintain Service Contract has a Find button. Type what you have in hand - a contract number, a customer, a service item number, a serial number or a model - and tick where to look: it finds the contract and says what it was found in.
    2. Filters for status, contract expiry, one invoice or one per machine, and the rental invoice; Google-like search with OR / AND; Keep Search Result; Check All and Uncheck; Advanced Search. Edit and Delete open or delete the ticked contracts exactly as the list does.

    v1.5.0.5 (2026-09-22) — Calculation Test, tier FOC:
    1. On a meter priced by tier price, FOC shows the tier's free copies (its 0.00 band), as the meter configuration does. Those are changed in Tier Price below the list.
    2. The working below the list mentions the minimum charge only on a meter that has one.

    v1.5.0.4 (2026-09-21) — Calculation Test uses the screen's prices:
    1. Calculation Test uses the prices set on the contract screen, saved or not: each meter's price, tier price, FOC, rebate and minimum charge, Billing Setup's agreed line prices, and the invoice split. Machines and meters added but not saved yet are included; ones taken off are left out. Every figure can still be changed in the test to try another deal. 1.5.0.3 read only the saved contract, so a contract priced but not yet saved tested at 0.00.

    v1.5.0.3 (2026-09-21) — Calculation Test:
    1. A contract now has a Calculation Test button (ribbon, Billing group). Key in each meter's initial and current reading and it shows what the contract bills: the copies, FOC, tier price, rebate and minimum charge, the amount of every meter, the invoice each line goes on and the total, with the working written out step by step. Price, tier price, FOC, rebate and minimum charge can be changed there to try another deal. Nothing is saved and no invoice is created.

    v1.5.0.2 (2026-09-21) — rental waive fix:
    1. On a contract that sends the rental on an invoice of its own, "Waive the rental" can no longer be ticked: it says why instead. It used to switch on a waive with no copy threshold at all, which waived the rental every month. Free months still work there.
    2. Changing a contract to "one invoice per machine" no longer takes its rental waive away: each machine's rental and copies are still on the same invoice. Only "a rental invoice and a meter invoice" and "per machine, rental apart" remove it, and they still ask first.

    v1.5.0.1 (2026-09-18) — install fix:
    1. The plug-in now installs on an account book that has never had it. 1.5.0.0 stopped while preparing the database ("Invalid column name 'MachineLineShows'") and would not load. A book where 1.5.0.0 already stopped half-way is finished off by this version; nothing has to be cleaned up first.

    v1.5.0.0 (2026-09-17) — Service & Contract billing:
    1. Billing engine: a month's readings, prices and free copies become the invoices they should — one per machine, per bill group, per customer, or the rental billed apart. One invoice per machine and rental-apart outrank a bill group.
    2. Meter Invoice Run: every invoice the day would produce, what each is waiting for, and "Show all overdue" for every billing date already gone by, this month and the five before it.
    3. The months go out in order. A month cannot be billed while an earlier month of the same contract has never been invoiced, and cannot be deleted while a later month still stands — enforced in the plugin AND by a trigger in the account book, so it holds even when the invoice is deleted inside AutoCount.
    4. Deleting an invoice takes the readings back with it: the payments and credit notes knocked off it are cleared first, the meters are released, and the month can be run again.
    5. Summary Sales Invoice Meter Listing (the customer's Appendix A): one row per machine, a total row per contract, the rebate printed as the copies it comes to, and figures that tie to the invoices standing in the book.
    6. Inter-Billing: a branch book can take a contract from head office and raise the invoice locally, with the readings read across.
    7. Billing Start: a contract carried over from an older system says which month this book starts billing and what each counter stood on when it took over.
    8. Contract screens: Meters & Pricing shows what each invoice line will print per bill group; the contract list gained Created By / Modified By and stopped scrolling sideways.

    v1.4.6.0 (2026-06-27):
    1. New "Select All Request" toolbar button — toggles selection of every request with no generated document yet (across both grids), for one-click bulk Generate.
    2. The Stock Request Task toolbar (all buttons, Hide Ignore, colour legend) is now defined in the WinForms designer so it renders in Visual Studio Design view.

    v1.4.5.0 (2026-06-27):
    1. Bulk actions: "Select All Update" / "Select All Cancel" tick every matching row across both grids; "Approve Change" then applies them in one go (update + cancel, both Stock Issue and Stock Transfer).
    2. Ctrl+Shift+Delete on a focused row → password-gated (atp09) hard delete of the row, and its generated AutoCount document if any.
    3. Full DevExpress grid right-click menu (sort / group / column chooser / best fit / filter editor / find panel) plus Export to Excel / PDF / CSV / Text and Save/Load/Reset Layout.
    4. UI: uniform button size + aligned two-row toolbar; clearer one-line row-colour legend.

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
