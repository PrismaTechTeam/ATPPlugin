# What to test

Work down the list. Each one builds on the one before, so a failure early on explains the rest.
Everything is under **Service & Contract**.

---

## 1. A contract, and what it will charge

1. **Maintain Service Contract** → open a contract (or make one). **Find** finds one by what you have
   in hand: type a service item no or a serial number off a machine and it finds the contract, with
   "Found In" saying which machine it was on.
2. **Service Item Under Contract** — the machines, their serials and their counters. A machine's
   Item Code lists only the stock items that have Has Serial No ticked (Plugin Option >
   4. Contract & Item No. can switch it back to every item).
3. **Meters & Pricing** — this is the deal. Each line says what the invoice will print and which
   machines it covers. The heading above tells you **how many invoices this contract sends**.
4. **View Sample Invoice** — the real invoice layout, with no reading and no posting.
5. **Calculation Test** — key in each meter's initial and current reading and see what the contract
   charges: the copies, FOC, tier price, rebate, minimum charge, the amount of every meter, which
   invoice it goes on and the total, with the working written out. Change a price, the FOC or the
   minimum there to try another deal -- the committed minimum and the waive keep their amounts in
   Min. Charge, so To Pay shows what they really do. It uses the prices as they are on the contract screen,
   saved or not, so a deal can be checked before it is saved. Nothing is saved.

**The invoice split sticks.** On a contract that bills one invoice per machine, open **Meters Pricing**, choose
**a rental invoice and a meter invoice**, press OK and Save; open Meters Pricing again -- it still says so (it used
to come back as "per machine, rental apart"). Choose a split, set a waive there, then press **Cancel** and discard:
the contract is exactly as it was before Meters Pricing was opened.

**Look for:** does the sample match what you agreed with this customer — one invoice or several,
the rental together or apart, black and colour on one line or two?
Does Calculation Test give the amount you would work out by hand for the same readings?

**Tier pricing.** Take a contract with a meter on a tier price.

1. In the meter grid under the machines, that meter's **Free Qty** is white and takes a number; its
   Unit Price stays grey (the tier price sets it). A tier price that used to start with free copies
   now shows them here -- "first 100 at 0.00" reads as Free Qty 100.
2. **Billing → 1. The invoice → Tier pricing** reads **Whole month at the tier reached**. Run
   **Calculation Test** on that meter: key 0 and 1,648 on a meter with 100 free and tiers "up to
   1,000 at 0.024, then 0.020" (Tier Price below the list sets them for the test) -- it bills
   1,548 x 0.020 = **30.96**.
3. Change Tier pricing to **Each tier at its own rate** and run Calculation Test again, without
   saving: 1,000 x 0.024 + 548 x 0.020 = **34.96**, with the working showing each tier.
4. **View Sample Invoice**: the meter prints one row per tier, "(Tier 1)", "(Tier 2)", each row's
   Qty x Unit Price = its Amount, and the rows add up to 34.96.
5. Try to give a tier price a first tier of 0.00 -- on the meter's price button, or in
   **Meter Multi Pricing** -- it is refused, pointing to Free Qty.

Save the contract with Each tier at its own rate and generate its invoice: the same rows as the sample.

**Rental in advance.** Take (or make) a contract that has a rental and has not been billed yet -- say from
1 October, billing day 30.

1. **Billing -> 1. The invoice -> Rental billed** -> **In advance - one month ahead**, then **Save**.
   **View Sample Invoice**: the rental line pays for next month, e.g. `MONTHLY RENTAL (2/36) NOV 2026`.
2. **Meter Invoice Run** in **September** (the month before the start), day 30: the contract is there, Ready,
   with its rental only -- `MONTHLY RENTAL (1/36) OCT 2026` -- and none of its counters. **Generate Invoice**.
3. Back on the contract, **Rental billed** is now grey: hover over it and it names the invoice that locked it.
4. **October**, day 30: key October's readings. The invoice carries October's copies and
   `MONTHLY RENTAL (2/36) NOV 2026`.
5. The contract's **last month** bills its copies only -- no rental line (it was paid the month before).

A machine added to such a contract after the bill that should have carried its first month pays that month and
the next on its next bill: `MONTHLY RENTAL (1-2/36) DEC 2026 - JAN 2027`, twice its rental. A contract billed
with the month's copies -- every contract today -- bills exactly as before.

---

## 2. A reading

1. **Meter Invoice Run** → **Meters — fetch & key in**.
2. Pick the month and the billing day, then key a Current Reading on a machine.

**Look for:** the copies and the charge appear as you type, and the row turns Ready.

> Fetch invents readings — see the warning in [README.md](README.md). Key them by hand for UAT.

---

A contract whose start date is still in the future is not on **Ready to Invoice** or
**Need Manual Key-In** -- there is nothing to read or bill yet. It appears on its start date.

Key a reading in by hand, then type over **Last Audit Date** -- the day you actually read the
counter. The invoice prints that day, and next month's **Last Read Date** is that day. The
cell only opens where the reading was keyed: a reading fetched from a machine, a locked
billing-day snapshot and an already-invoiced period keep the date they have.

If the machine itself could not be trusted that month -- it broke down, it was swapped, it sent a
stale counter -- pick its row and press **Key in myself**. The number and its date stay as they
are; what changes is that the reading is yours, so its date can be corrected, and the next
Fetch raises a conflict instead of overwriting it.

A reading keyed by hand can carry a **Reference No** (the column after Source): type the slip or
report number once and both counters of the machine take it. Generate the invoice and its **Ref**
shows it -- the same place a PUMS reading's report id appears. Leave it empty and the Ref is the
usual contract or machine number.

On a contract, the machine grid has a **Branch** column beside Bill Group. It lists the customer's
branches as registered in AutoCount (A/R > Debtor > Branch tab) -- a customer with none shows an
empty list that says so. Pick one for a machine and Save: the machine's delivery address follows the
branch, and the Service Item screen shows the same branch. The invoice does not change.

**Create DO.** On a saved contract, select one or more machines in the Service Item grid (Ctrl+click for
several) and press **Delivery > Create DO**. It lists each machine with its model and serial and asks; a
machine whose serial is not in stock, or that is already on a DO, is left out with the reason. Say Yes and
check, in AutoCount's Delivery Order:

- one DO to the contract's customer, dated today, Ref = the contract no;
- one line per machine: its item code, qty 1, price 0, and its serial number;
- the serial is now out of stock -- press Create DO for the same machine again and it is refused, naming the DO;
- the contract's **DO** column shows the DO on those machines, and **Maintain Service Contract** shows it in its
  **DO** column (a contract with none shows an amber **No DO yet**).

Delete that test DO in AutoCount afterwards; the serial goes back into stock and the machine back to No DO yet.
The DO is saved by AutoCount itself, so it needs a licensed AutoCount: an evaluation copy past 500 transactions
refuses it with its own message, and nothing is written.

## 3. The invoice

**The invoice date.** Pick an invoice in Meter Invoice Run: under the buttons, **Invoice date** shows the date it
will carry -- its due date. Change it to another day of the same month (a machine that broke down on the 15th is
billed for the 1st-14th and dated the 14th), or press **Use last reading date**. The list's **Invoice Date**
column shows it in blue, a Refresh keeps it, and the generated invoice carries it. A date in another month is
refused: the month decides the invoice's number series and where next month's readings start.

Before Generate, **Total Charges** on the right already shows what a committed minimum tops up and
what a rental waive takes off, and the invoice **Amount** on the left includes them.

1. Back to **Invoices to generate**. Each line is one invoice the day would produce, with its
   status: Ready, waiting on readings, or already invoiced.
2. Press **Generate Invoice**.
3. Open the invoice in AutoCount (A/R or Sales → Invoice).

**Look for:** the amount matches Meters & Pricing; the lines read the way the sample did; the rental
line counts its months, e.g. `MONTHLY RENTAL (4/36)`.

---

## 4. The listing the customer reconciles against

**Inquiry → Summary Sales Invoice Meter Listing** → pick the month → **Inquiry**.

**Look for:** one row per machine, and a `.C` row closing each contract.
Check the arithmetic reads across the page:

```
Current − Previous − FOC − Fixed Rebate = NET        NET × Rate = Charge
```

and that the contract's total equals the invoice you just made.

---

## 5. Taking an invoice back

On an invoiced row: **Delete this invoice** (or `Del`).

**Look for:** it says exactly what it will remove, the month goes back to Ready afterwards, and the
readings are yours to bill again.

Then try to delete an **earlier** month while a later one is still invoiced — it must refuse and
name the invoice in the way:

> This invoice cannot be deleted: a later month is already invoiced.

The same rule the other way round: try to generate a month while an earlier month of that contract
has never been billed — it must refuse before it asks you to confirm.

---

## 6. Months you are behind on

**Meter Invoice Run → Filter Options → Show all overdue** — every billing date already gone by,
this month and the five before it, oldest first, with the month on each row.

**Look for:** the count on the button, and that the oldest month is the one it lets you bill.

---

## 7. A contract from before this system

On a contract → **Billing History** → **Billing start (old data)**.
Say which month this book starts billing, and what each counter stood on when it took over.

**Look for:** the months before that stop being called overdue, and the first month bills one
month's copies rather than the machine's whole life.

---

## What to send back

The version (`1.5.0.10`), the contract number, the month, and what you expected against what you got.
A screenshot of the screen you were on says more than a description.
