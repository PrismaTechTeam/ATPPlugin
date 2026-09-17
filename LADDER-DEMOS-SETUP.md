# Tier pricing — the two shapes, and how to set each one up

**Tier pricing** is a volume deal: *"the more you print, the cheaper each copy."* The month's copies
decide which band the customer lands in, and every billed copy that month is at that band's rate.

There are two shapes, and the difference is **whose copies are counted**.

| Demo | The deal in the customer's words | Counted over | Invoices | Lines |
|---|---|---|---|---|
| **DEMO-22** | "All three machines together — count them as one and give us the volume rate." | the whole group | 1 | 3 |
| **DEMO-23** | "Each machine earns its own rate. And bill each one on its own page." | one machine | 3 | 3 + 3 + 3 |

---

## How a ladder is read

A ladder is a list of bands: an upper boundary and a price.

```
up to  3,000   →   0.0000     ← the free allowance
up to 10,000   →   0.0250
unlimited      →   0.0200
```

Two rules, and they are not the same rule:

**1. A 0.00 band is an allowance.** Those copies come off the top and are never billed. "First 3,000
free" means the first 3,000 are free, whatever else happens. They show on the invoice as the
**FOC Qty**, not as a row priced at nothing.

**2. Every other band is a threshold.** The copies decide which band applies, and then *all* the
billed copies are at that rate. Print 12,000 and the month is past 10,000, so the rate is **0.0200** —
on all of it, not just on the copies above 10,000.

```
printed 12,000
  less the free allowance          3,000
  billed                           9,000  ×  0.0200  =  180.00
```

One rate, one line, a figure the customer can multiply back to the amount.

**The band is chosen by what was printed, not by what is billed.** 12,000 copies reach the 0.0200
band even though only 9,000 of them are charged. The customer ran the machine 12,000 times; that is
the volume they earned the rate with.

---

## DEMO-23 — each machine on its own ladder, one invoice each

> **The deal.** Three machines, three different volume agreements, and the customer wants each
> machine billed on its own page so they can hand it to whoever runs it.

### Setting it up

1. **Service & Contract → Service Contract → New**. Debtor, Contract No `DEMO-23`, dates.
2. **Quick Add Row** three times. Give each row a Model and a Machine Serial.
3. Press **Meters...**, tick all three machines and give them the meters they share:
   **+ Rental** `450.00`, **+ Black (BK)**, **+ Colour (CL)** `0.2000`. **OK**.
4. Back on the contract, click the **first service item** in the grid. Its meters appear in the
   panel underneath.
5. On that item's **BK** row, click the **Multi-Price** cell and press its **price button**.
6. The tier dialog opens. Fill in the bands with **+ Add Row**:

   | Meter Reading (<=) | Unit Price (Base UOM) |
   |---|---|
   | 3,000 | 0.0000 |
   | 10,000 | 0.0250 |
   | *(press* **∞ Unlimited**) | 0.0200 |

   The last row must be unlimited, so heavy usage always finds a band. **OK**.
7. Repeat for the second machine — `5,000 → 0.0300`, then **∞ Unlimited** `0.0220`. No free band:
   this customer pays from the first copy.
8. The third machine is on a ladder the price list already has. Open its **Multi-Price** cell and
   pick the scheme **`BK +P - 0.028 FOC300`** from **Multi Pricing** at the top of the dialog —
   300 free, then 0.028. Leave the bands alone. **OK**.
9. **Billing Setup** → under **This contract sends**, choose **one invoice per machine**.
   Leave the machines un-merged — each one bills alone.
10. **OK**, then **Save**.

> **Free Qty locks itself.** Once a ladder is on a meter, its **Free Qty** and **Unit Price** cells
> stop accepting edits — the ladder's 0.00 band is the allowance now, and two places to type it is
> how the two disagree.

### What it bills

Machines print 12,000 / 8,000 / 5,300 black.

| Machine | Its ladder | Printed | Free | Billed | Rate | Amount |
|---|---|---|---|---|---|---|
| 001 UJC98394 | 3,000 free · 10,000 @ .025 · ∞ @ .020 | 12,000 | 3,000 | 9,000 | **0.0200** | 180.00 |
| 002 JVW98852 | 5,000 @ .030 · ∞ @ .022 | 8,000 | — | 8,000 | **0.0220** | 176.00 |
| 003 4DA44565 | `BK +P - 0.028 FOC300` | 5,300 | 300 | 5,000 | **0.0280** | 140.00 |

```
DEMO-23-001                                      Qty   Unit Price    Amount
  MONTHLY RENTAL (9/36)                            1     450.0000    450.00
  BLACK COPY + PRINT A4 & A3                   9,000       0.0200    180.00
    Meter FOC Qty : 3000
  COLOUR COPY + PRINT A4 & A3                    500       0.2000    100.00
                                                              Total  730.00

DEMO-23-002
  MONTHLY RENTAL (9/36)                            1     450.0000    450.00
  BLACK COPY + PRINT A4 & A3                   8,000       0.0220    176.00
  COLOUR COPY + PRINT A4 & A3                    400       0.2000     80.00
                                                              Total  706.00

DEMO-23-003
  MONTHLY RENTAL (9/36)                            1     450.0000    450.00
  BLACK COPY + PRINT A4 & A3                   5,000       0.0280    140.00
    Meter FOC Qty : 300
  COLOUR COPY + PRINT A4 & A3                    300       0.2000     60.00
                                                              Total  650.00
```

Three invoices, **2,086.00** in all.

**Machine 001 is the one to look at.** It printed 12,000 but is billed for 9,000 — and the rate is
0.0200 because 12,000 is past the 10,000 boundary. Had it printed 9,000 instead, it would have been
6,000 billed at **0.0250**, because 9,000 does not reach the third band.

---

## DEMO-22 — one ladder for the whole group

> **The deal.** Three machines, each printing about 5,000 a month. The customer says: *"Don't judge
> them one at a time. Between them we do fifteen thousand — give us the fifteen-thousand rate."*

### Setting it up

1. Contract `DEMO-22`, three machines, **Meters...** → all three get **+ Rental** `450.00`,
   **+ Black (BK)**, **+ Colour (CL)** `0.2000`.
   Leave BK with no ladder of its own — the group's ladder is what will price it.
2. **Billing Setup**. Under **This contract sends**, choose **one invoice for everything**.
3. Tick all three machines, then in **Ticked machines** press **Merge both**.
   The lower grid now shows one rental line, one black line, one colour line.
4. Click the **black** line's **Tier pricing** cell.
5. The dialog opens titled for the line — *Tier pricing for ALL MACHINES (3 machines)*. Fill in:

   | Meter Reading (<=) | Unit Price (Base UOM) |
   |---|---|
   | 2,000 | 0.0300 |
   | 5,000 | 0.0250 |
   | *(press* **∞ Unlimited**) | 0.0200 |

6. **OK**. The cell reads `3 bands`, and **Use this price instead** clears itself — bands and a flat
   rate are two answers to one question, and the bands win.
7. **OK** on Billing Setup, then **Save**.

### What it bills

```
                                                 Qty   Unit Price     Amount
MONTHLY RENTAL (9/36)                              3     450.0000   1,350.00
  Model: iR-ADV C3560i  (3 UNIT)
  S/N: DNR50913, YWG39950, LXY95951
BLACK COPY + PRINT A4 & A3                    15,000       0.0200     300.00
  Current 434,860 / Previous 419,860 / Usage 15,000
COLOUR COPY + PRINT A4 & A3                    1,500       0.2000     300.00
                                                             Total  1,950.00
```

Three machines at 5,000 each. **Alone**, every one of them would have stopped at the 0.0250 band and
paid 135.00 — 405.00 between them. **Together**, the 15,000 reaches 0.0200 and the line is 300.00.
Same machines, same copies, same bands, **105.00 apart**. That difference is the whole point of a
group ladder.

---

## Which machines belong to the group

A machine that carries **its own** ladder is priced unlike the others on purpose. It keeps its own
ladder, it keeps its own printed line, and **its copies stay out of the group's total** — they were
never part of the deal the group struck.

Set a group ladder on a line that holds such a machine and the screen says so before writing
anything, naming the machines that will stand apart.

Two machines pointing at the same **master ladder code** are a different case: they still climb it
one at a time, as they always have. A contract that wants the combined volume says so by agreeing a
group ladder here. Making a shared code mean something new would have moved money on contracts
nobody touched.

---

## Where each kind of ladder lives

| Set where | Belongs to | Counted over | Stored in |
|---|---|---|---|
| Contract → item's meter panel → **Multi-Price** cell | one meter on one machine | that machine's copies | `zSCP2_ItemMeterPrice` (its own bands) or a scheme code |
| **Meters...** → *...or tiered pricing* → **set BK tiers** | the ticked machines, one meter each | each machine's own copies | the scheme code on each meter |
| **Billing Setup** → the line's **Tier pricing** cell | a merged line | the line's machines added up | `zSCP2_ContractRentalPrice` |
| **General Setup → Meter Multi Pricing** | every contract that names the code | each meter that uses it | `zSCP_MeterMultiPriceItem` |

Editing a scheme in **Meter Multi Pricing** changes it for every contract that names it. To change
one machine only, open its **Multi-Price** cell and edit the bands there — that writes a copy that
belongs to that meter and leaves the scheme alone.

---

## Testing them

Ready-made meter readings are in `tests/meter-json/` — `DEMO-22.json` and `DEMO-23.json`. On
**Meter Reading Integration**, pick the billing month, press `Ctrl+Shift+T` to reveal
**TEST Fetch (JSON)**, paste one in, tick the rows and **Generate**. The figures above are what
should come out.
