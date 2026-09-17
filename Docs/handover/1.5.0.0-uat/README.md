# Service Contract Photocopier — 1.5.0.0 (UAT)

The AutoCount plug-in for photocopier service, rental and meter billing.
This build is for **user acceptance testing**: use it on a test copy of the account book,
not on the live one.

| | |
|---|---|
| Version | **1.5.0.0** |
| File | `ATP-ServiceContract-1.5.0.0-uat.app` |
| Built | 17 September 2026 |
| Needs | AutoCount Accounting **2.0.2** or later, DevExpress 22.2.7 |
| Source | git tag `v1.0.0-uat` |

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

**Report a problem with the version on it.** `1.5.0.0` — please include it in any screenshot,
along with the contract number and the month.
