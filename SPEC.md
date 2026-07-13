# ATP — Service & Contract: Behaviour Spec Log

Living log of the plugin's business rules. One entry per rule so behaviour is confirmed,
verifiable, and changed deliberately (not by accident). Add new rules at the end of each area.

**Status key:** ✅ implemented & verified · 🟡 implemented, needs a decision · ⚪ proposed · 🐛 known gap

---

## Meter Reading & Billing

### SPEC-MR-001 — Expired machines drop out of a billing month *after* they expire
**Rule.** When fetching/billing month **M**, a service item appears only if it has **no** expiry, or
`ServiceExpiryDate >= first day of month M`. So a machine expiring **31 May 2026**:
- **shows** when billing **May 2026** (collect the final May period), and
- **is hidden** when billing **June 2026** (already expired before June starts).

**Why.** Never bill a machine for a period after its contract ended — but still allow the expiry
month's final bill.

**Implementation.** `MeterReadingIntegration_Form.LoadData()`:
`AND (i.ServiceExpiryDate IS NULL OR i.ServiceExpiryDate >= DATEFROMPARTS(<year>, <month>, 1))`
Applied **only when "Include expired Service Item" is OFF** (Meter Reading ▸ Setting).

**Verified** (AED_ATPTEST, 2026-07): 32 machines expire in May 2026 (e.g. CSSI 00000686/687/688 @
2026-05-31) → all 32 shown for May, all 32 hidden for June. ✅

**DECISION NEEDED (D-1) 🟡.** Default of "Include expired" is currently **ON (show all)**, so this rule
only kicks in after you turn it off. Flip default to **OFF (exclude expired)** so it applies out of
the box?

---

### SPEC-MR-002 — Billing day beyond month length clamps to month-end
**Rule.** Billing day 31 (or 29/30) in a shorter month bills on that month's **last day**
(31 → 30 in June, → 28/29 in Feb).
**Implementation.** LoadData clamps BOTH the selected target day and the effective day; Fetch clamps
its audit-day cutoff the same way. ✅ (was latent 🐛; verified consistent 2026-07-11)

---

### SPEC-MR-009 — A meter + billing period can be invoiced only ONCE
**Rule.** When an invoice saves, each line's staging row (`zSCP2_MeterEntry`, meter + year + month)
is stamped `InvoicedDocKey/DocNo/At`. Stamped rows: show `INVOICED <docno> <date>` on reopen, are
skipped by Fetch (reading not overwritten) and by Generate (reported as "already invoiced"). Re-running
Generate for the same period can never double-bill.
**Implementation.** `02_Update_zSCP2_MeterEntry_v2.sql` + `MeterInvoiceGenerator` stamp +
`PrefillFromStaging` / `BtnFetch_Click` / `BtnGenerateInvoice_Click` guards. ✅

### SPEC-MR-010 — Billing period is year-aware; readings carry their true dates
**Rule.** The billing YEAR = current year, unless the selected month is later than the current month
(then it's the previous year — December billed in January = last December). All period touchpoints use
it (baseline cutoff, expiry filter, staging period, offline `?month=YYYY-MM`, `QualifiesByDate` which
also compares the year). The filter panel shows the resolved period ("billing period: Dec 2026").
`zSCP_MeterTrans` rows are dated with the API **LastAuditDate** (fallback: now), so the next period's
"Last Read Date" is the real meter-read date. Zero-usage rows that still bill (minimum charges,
TotalCharges > 0) are never hidden by the "Include 0 Meter Usage" filter.
**Implementation.** `SelectedYear()` + year-aware `IMeterReadingApiClient.GetReadings(status, year,
month)` + `MeterBillLine.AuditDate` + ApplyTabFilter exemption. ✅

---

### SPEC-MR-003 — "Last reading" is the newest reading strictly *before* the billing month
**Rule.** The previous-reading baseline for month M = latest `zSCP_MeterTrans` dated **< first day of
month M** (never the current/future month, so re-billing a month stays consistent).
**Implementation.** LoadData last-reading sub-query bounded by
`MeterTransDate < DATEFROMPARTS(year, month, 1)`. ✅

---

### SPEC-MR-004 — Day filter lists only billing days actually in use
**Rule.** The Day dropdown shows the distinct effective billing days across active items
(e.g. 7/20/21/25/28/30), not a fixed 1–31.
**Implementation.** `PopulateDayCombo()`. ✅

---

### SPEC-MR-005 — Fetch merges Online + Offline (Online wins) filtered by audit date
**Rule.** Fetch reads both endpoints for month M; a reading counts only if its `LastAuditDate` is in
month M and on/before the selected day; Online beats Offline for the same code.
**Implementation.** `BtnFetch_Click` + `QualifiesByDate`. ✅

---

### SPEC-MR-006 — Fetch is non-blocking, resilient, and shows API health
**Rule.** Fetch runs off the UI thread with a "thinking" indicator (marquee + cycling wording).
Online & Offline are fetched **in parallel**, each **retried 3×** on transient connection errors.
The footer shows the API URL: **green** = reachable, **red** = unreachable.
**Implementation.** async `BtnFetch_Click` + `LiveMeterReadingApiClient` retry + footer status. ✅
*Note:* the server-side endpoint itself can be slow (~15s) — that is the API host, not the plugin.

---

### SPEC-MR-007 — Generate Invoice runs headless behind a progress dialog
**Rule.** Generate Invoice **saves each invoice programmatically** (no per-invoice AutoCount dialog).
It groups selected meter lines (Group = 1 invoice/contract, Separate = 1/item), then runs on a worker
task behind a progress dialog (Total / Done / Failed + progress bar + current label + Cancel + error
log). A per-invoice failure is logged and does **not** stop the batch. `zSCP_MeterTrans` is written
only after each invoice's confirmed `Save()`.
**Implementation.** `MeterInvoiceGenerator` + `MeterInvoiceGenerateProgress_Form` (mirrors
`SiStGenerator` / `StockRequestGenerateProgress_Form`). ✅
**Was 🐛:** old flow opened `FormInvoiceEntry.ShowDialog` per group → had to click through every
invoice manually (unusable at scale). Fixed.

---

### SPEC-MR-008 — Fetched readings persist; Fetch = update, not required every open
**Rule.** Fetch **saves** each matched Online/Offline reading to `zSCP2_MeterEntry` (staging, keyed by
meter + year + month). Reopening the module auto-loads the saved readings (`PrefillFromStaging`) shown
as `Online (saved)` / `Offline (saved)` — **no need to Fetch again**. Fetch is only pressed to **update**
(re-pull from the API, overwriting the staged values). Manual / conflict rows are never overwritten.
Switching the billing-day filter keeps all fetches (staging is per-month; the Day filter shows a subset),
so **no separate tab per billing day** is needed.
**Implementation.** `BtnFetch_Click` collects matched rows → `StageReadings()` (UpsertStaging batch);
`LoadData` → `PrefillFromStaging`. ✅
**Was 🐛:** Fetch only updated the grid in-memory, so readings were lost on close → had to re-fetch
every open. Fixed.

---

## Service Items / Contract

### SPEC-SC-001 — Per-item expiry shown + colour-coded in the contract editor
**Rule.** The contract's Service Items grid has an **Expiry** column: **red bold** if expired,
**green bold** if active, blank if no expiry.
**Implementation.** `zSCP2_Contract_Form` ColExpiry + `GridViewItems_RowCellStyle`. ✅

### SPEC-SC-002 — Expiry + active/inactive mirror the V8 master
**Rule.** `zSCP2_Item.ServiceExpiryDate` and `Inactive` mirror the V8 master
(`v8_atp_main.serviceitem`) by service-item code.
**Status.** ✅ imported to AED_ATPTEST (2,981 expiry, 1,199 already expired; 86 have none because the
master has none). Inactive items/contracts are already excluded from the billing list.

### SPEC-SC-004 — Legacy data: ONE contract per CSSI; only new contracts may group
**Rule.** The customer's legacy CSSI (imported from the V8 master) each live under their **own
contract** — we have no authority to group a customer's existing machines. Only contracts **created
in the plugin** may hold multiple CSSI.
**Status.** ✅ data migration applied to AED_ATPTEST (2026-07-07): 379 multi-CSSI contracts split →
2,173 new contracts cloned from their parents (SC-000895…SC-003067); now 3,067 contracts = 3,067 CSSI.
Backups: `zSCP2_Contract_bak20260707`, `zSCP2_ItemContract_bak20260707`.

### SPEC-SC-003 — Contract billing day = the most common item override
**Rule.** A contract's `BillingDay` is set to the **mode** (most frequent) of its items'
`BillingDayOverride` (e.g. items 7,7,28 → contract day 7). Ties → the larger day.
**Status.** ✅ applied to AED_ATPTEST (688 contracts; 8 ties).

---

## Open decisions
- **D-1 (SPEC-MR-001):** default "Include expired" ON (show all, billing unchanged) vs OFF (exclude
  expired by default). — *pending decision.*
