# Service Contract Photocopier — 1.5.0.9 (UAT)

The AutoCount plug-in for photocopier service, rental and meter billing.
This build is for **user acceptance testing**: use it on a test copy of the account book,
not on the live one.

| | |
|---|---|
| Version | **1.5.0.9** |
| File | `ATP-ServiceContract-1.5.0.9-uat.app` |
| Built | 22 September 2026 |
| Needs | AutoCount Accounting **2.0.2** or later, DevExpress 22.2.7 |
| Source | git tag `v1.0.9-uat` |

**1.5.0.9 replaces 1.5.0.8**: a testing shortcut (Ctrl+Shift+T, TEST Fetch) works again inside Meter Invoice Run. Nothing else changes. Install it straight over 1.5.0.8.

**1.5.0.8 replaced 1.5.0.7**: when a model has no serial in AutoCount's stock, the Machine Serial cell says so instead of opening an empty list. Install it straight over 1.5.0.7.

**1.5.0.7 replaced 1.5.0.6**: a machine's Item Code offers only the stock items with Has Serial No ticked (a tick in Plugin Option > 4. Contract & Item No.; untick to list every item). Install it straight over 1.5.0.6.

**1.5.0.6 replaced 1.5.0.5**: Maintain Service Contract has a **Find** button - a contract is found by its number, its customer, or any machine on it (service item no, serial, model). Install it straight over 1.5.0.5.

**1.5.0.5 replaced 1.5.0.4**: in Calculation Test a tiered meter's FOC shows the tier's free copies, as the meter configuration does. Install it straight over 1.5.0.4.

**1.5.0.4 replaced 1.5.0.3**: Calculation Test uses the prices set on the contract screen, saved or not, and they can still be changed in the test. Install it straight over 1.5.0.3.

**1.5.0.3 replaced 1.5.0.2**: adds **Calculation Test** on the contract -- key in readings and see what the contract bills, worked out step by step. Nothing in the database changes; install it straight over 1.5.0.2.

**1.5.0.2 replaced 1.5.0.1**: on a contract that sends the rental on its own invoice, "Waive the rental" can no longer be ticked (it used to waive the rental every month). Install it straight over 1.5.0.1.

**1.5.0.1 replaced 1.5.0.0**, which would not install on an account book that had never had the
plug-in. Nothing else changed. If 1.5.0.0 was already tried on this book, install 1.5.0.1 over it.

Four documents sit beside this one:

- **[INSTALL.md](INSTALL.md)** — how to put it in, and what it does to the account book
- **[WHAT-TO-TEST.md](WHAT-TO-TEST.md)** — the flows to walk through, in order
- **[KNOWN-LIMITATIONS.md](KNOWN-LIMITATIONS.md)** — what it does not do yet, and what to keep away from
- **[ROLLBACK.md](ROLLBACK.md)** — how to take it out again

## Read this first

**Two settings are deliberately set to protect you during UAT.**

1. **Meter readings are simulated.** The meter API starts in MOCK, so Fetch invents readings from
   the machines' real serial numbers. They look completely real. Nothing on screen says which mode
   it is in — so until somebody switches it to LIVE and gives it the real address and token,
   **treat every fetched reading as fake**. Readings keyed in by hand are your own and are real.

2. **Bulk Email cannot reach your customers.** The whitelist is on and holds three of our addresses;
   every other recipient is skipped and the send log says so. This is so a test run against a book
   full of real addresses cannot mail them. If bulk email looks like it "did nothing", this is why.

**Report a problem with the version on it.** `1.5.0.9` — please include it in any screenshot,
along with the contract number and the month.
