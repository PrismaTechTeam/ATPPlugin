# Known limitations — 1.5.0.10 (UAT)

Everything here is known. It is written down so nothing on this list is a surprise, and so a real
surprise is easy to tell apart from a thing we already knew about.

---

## 1. Keep away from these — they can damage the book

### `Ctrl+Shift+3` — "Dev Wipe"

On the **Meters** view inside Meter Invoice Run, and on **Bulk Email Invoice**. It deletes **every
meter invoice in the whole account book**, every reading-correction credit note, and **every
customer payment knocked off those invoices**. It asks once and then does it.

It exists so we can re-run a month over and over while testing, and it is staying in this build
because it is genuinely useful during UAT. **It is not restricted to your login.** Anyone who can
open the screen can press it.

### `Ctrl+Shift+T` — "TEST Fetch (JSON)"

Same screen. Reveals a button that takes pasted JSON and pushes it through the real reading pipeline
as though the meter API had answered. Useful; not something to press by accident.

### No access rights on the billing screens

Meter Invoice Run, Bulk Email Invoice, the listing, Inter-Billing and Maintain Service Contract are
**not restricted by AutoCount user rights** in this build. Any user who can see the menu can generate
and delete invoices, including `Del` and `Ctrl+Shift+Del` on the invoice list. Plan who gets the
menu accordingly.

---

## 2. Things that can bill wrong

| | What |
|---|---|
| 1 | **This build has never been checked against your own past invoices.** We hold the July-2026 figures for 22 contracts as an answer key, and the comparison has not been run. Reconcile UAT invoices against your existing ones before trusting a total. |
| 2 | Billing a month you skipped, **after** a later month was already invoiced, can charge the same copies twice on this book's own counters. Bill in order. |
| 3 | When one invoice covers several machines in a bill group and those machines have **different billing days**, the invoice date is taken from whichever machine happens to be first. |
| 4 | The listing's printed formula (`NET × Rate`) does not show the working for a line priced by a **minimum charge** or a **tier ladder**. The invoice is right; the sheet cannot be added up by hand on those lines. In our test book that was about 57 lines out of 639. |
| 5 | **Group Rental** groups machines by meter type. If your rule is really "same remark and same customer", say so — it has never been checked against real data. |
| 6 | Four rules about a contract's month count were assumed, not agreed: which month counts as month 1, what a period bills, what happens to a contract taken over mid-life, and how contracts imported from V8 start. Please confirm them against a contract you know. |
| 7 | Two places fail quietly: if the listing's query errors it prints a **blank sheet** rather than saying so, and the tidy-up after a deleted invoice can stop half-way without telling anyone. A blank listing is a symptom — tell us, do not print it. |
| 8 | On a contract billing its rental **in advance**, a machine that joins after the bill that should have carried its first month pays two months on its next bill. If that machine also has a rental waive for its first months, the waive gives back one month on that bill, not two. Check such a machine's first invoice. |

---

## 3. Not covered by an automated test

The code that actually **writes** the invoice, the delete itself, the billing sequence, and the whole
e-mail path. **SMTP has never been sent end to end** — bulk e-mail is untested beyond the screen.

---

## 4. Things that look wrong but are not

| | What |
|---|---|
| 9 | **About a third of the module's menu says "(IN MAINTENANCE)"** — 16 Reports and 6 Inquiries. They were written for the old contract module and open a notice instead of an empty screen. They will come back. |
| 10 | Meter readings are **simulated** until somebody switches the API to LIVE. Nothing on screen says which mode it is in. |
| 11 | **Bulk e-mail reaches nobody.** The whitelist is on and holds three of our addresses; everyone else is skipped, and the send log says so per recipient. |
| 12 | The bulk-email template preview shows a real customer's name as its example. |
| 13 | Billing Format's sample preview is permanently dated July 2026. |
| 14 | Contracts saved before 17 September show blank Created By / Modified By. Nothing recorded them until then. |
| 15 | **Cancelling** an invoice inside AutoCount is not covered by the delete-order rule. The money is voided but the readings stay marked as billed, so that month will not come back on the list. Delete it rather than cancel it. |
| 16 | On a contract priced **each tier at its own rate**, the Summary Sales Invoice Meter Listing shows one Rate per machine -- its charge divided by its copies, e.g. 0.022584 -- where the invoice prints a row per tier at the agreed rates. The Charge on the listing is the invoice's. |
| 17 | A tier price whose **name** promises free copies -- "BK +P - 0.02 FOC20K" -- no longer carries them: when this version first opened the book they moved into the Free Qty of each meter on it. A meter given that tier price from now on starts with the Free Qty you key in, not the name's. |
| 18 | A rental billed **in advance** on an invoice of its own, with a Rental invoice day, is dated that day in the month it pays for -- September's run makes October's rental invoice dated 1 October -- and its date can be moved only within October. |

---

## 5. Inter-Billing (a branch billing head office's machines)

Newest part of the module, roughest edges:

- Auto-fetch can lock a counter that belongs to head office, and the contract then cannot be billed
  until the lock is cleared.
- If head office corrects a reading by deleting and re-issuing rather than by credit note, this book
  is not told.
- A contract whose rental is billed apart still waits for head office's copy readings before the
  rental invoice can go out.
- Grouping "one invoice per customer" still produces one invoice per contract on that board.
