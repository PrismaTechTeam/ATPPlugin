# What to test

Work down the list. Each one builds on the one before, so a failure early on explains the rest.
Everything is under **Service & Contract**.

---

## 1. A contract, and what it will charge

1. **Maintain Service Contract** → open a contract (or make one). **Find** finds one by what you have
   in hand: type a service item no or a serial number off a machine and it finds the contract, with
   "Found In" saying which machine it was on.
2. **Service Item Under Contract** — the machines, their serials and their counters. A machine's
   Item Code lists only the stock items that have Has Serial No ticked.
3. **Meters & Pricing** — this is the deal. Each line says what the invoice will print and which
   machines it covers. The heading above tells you **how many invoices this contract sends**.
4. **View Sample Invoice** — the real invoice layout, with no reading and no posting.
5. **Calculation Test** — key in each meter's initial and current reading and see what the contract
   charges: the copies, FOC, tier price, rebate, minimum charge, the amount of every meter, which
   invoice it goes on and the total, with the working written out. Change a price, the FOC or the
   minimum there to try another deal -- the committed minimum and the waive keep their amounts in
   Min. Charge, so To Pay shows what they really do. It uses the prices as they are on the contract screen,
   saved or not, so a deal can be checked before it is saved. Nothing is saved.

**Look for:** does the sample match what you agreed with this customer — one invoice or several,
the rental together or apart, black and colour on one line or two?
Does Calculation Test give the amount you would work out by hand for the same readings?

---

## 2. A reading

1. **Meter Invoice Run** → **Meters — fetch & key in**.
2. Pick the month and the billing day, then key a Current Reading on a machine.

**Look for:** the copies and the charge appear as you type, and the row turns Ready.

> Fetch invents readings — see the warning in [README.md](README.md). Key them by hand for UAT.

---

## 3. The invoice

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

The version (`1.5.0.1`), the contract number, the month, and what you expected against what you got.
A screenshot of the screen you were on says more than a description.
