# Rental waive — the four shapes, and how to set each one up

A **rental waive** is a deal that gives the rental back when the machine prints enough:
*"print RM 1,000 of copies this month and the rental is on us."* It bills as a negative line —
a credit — on the same invoice as the rental it comes off.

There are four ways a customer asks for it, and the contract carries whichever one they signed.
Each demo below is one of them, set up end to end.

| Demo | The deal in the customer's words | Invoices | Lines |
|---|---|---|---|
| **DEMO-19** | "All three machines together print RM 1,000 and the whole rental is free." | 1 | 4 |
| **DEMO-20** | "Each machine that prints RM 350 gets **its own** rental free. The rest pay." | 1 | 4 |
| **DEMO-15** | "Different deal on each machine — one by usage, one by a bigger target, one free for two years." | 1 | 10 |
| **DEMO-21** | "Bill each machine on its own page: rental, the waive, the minimum, the copies." | 3 | 5 + 5 + 5 |

---

## Before any of them — the two rules

**1. The rental and the copies must land on the same invoice.**
A waive is a credit against rent, decided by what the copies came to. Send the rental to an
invoice of its own and the customer gets a credit on one page whose reason is printed on another.
So the waive is offered under **one invoice for everything** and **one invoice per machine**, and
refused under the two splits that separate the rental. When it is refused the screen says so:
*Not while the rental is on an invoice of its own.*

**2. The waive is judged before the minimum tops anything up.**
A machine short of its floor is topped up to it — but the top-up does not count towards the waive
target. Otherwise printing too little would earn a free rental, which is the opposite of the deal.

---

## DEMO-19 — one waive for the whole line

> **The deal.** Three machines, RM 450 each, RM 1,350 a month. The customer says: *"Between the
> three of them we'll print RM 1,000 worth. Do that and you waive the month's rental."* Everything
> the three machines print counts, added up, once.

### Setting it up

1. **Service & Contract → Service Contract → New**. Fill Debtor, Contract No `DEMO-19`, dates.
2. Press **Quick Add Row** three times. Give each row a Model and a Machine Serial.
3. Press **Meters...**. Tick all three machines, then:
   - **+ Rental** → `450.00` a month
   - **+ Black (BK)** → `0.0200` per copy
   - **+ Colour (CL)** → `0.2000` per copy
   - **OK**
4. Press **Billing Setup** on the ribbon.
5. Under **This contract sends**, choose **one invoice for everything**.
6. Tick all three machines in the top grid, then in **Ticked machines** press **Merge both** —
   one rental line and one BK line and one CL line.
7. In the lower grid, click the rental row's **Minimum / waive** cell.
8. Tick **Waive the rental**, choose **One waive for the whole line**, and fill in:
   - `When their black and colour reach` → **1,000.00**
   - `take this much off the rental` → **1,350.00**
9. **OK**. The cell now reads `reach 1,000.00 -> waive 1,350.00`.
10. **OK** on Billing Setup, then **Save**.

### What it bills

Every machine prints 10,000 black and 1,000 colour.

```
RENTAL   MONTHLY RENTAL (9/36)                       3     450.0000    1,350.00
         Model: iR-ADV C3560i  (3 UNIT)
         S/N: ABY59531, 3BX23218, GSZ08461

WAIVE    RENTAL WAIVE                                1   -1350.0000   -1,350.00
         Model: iR-ADV C3560i  (3 UNIT)
         S/N: ABY59531, 3BX23218, GSZ08461
         RENTAL WAIVE - charges 1200.00 >= target 1000.00

BK       BLACK COPY + PRINT A4 & A3              30,000      0.0200      600.00
CL       COLOUR COPY + PRINT A4 & A3              3,000      0.2000      600.00
                                                              Total    1,200.00
```

600 + 600 = 1,200, which clears 1,000, so the whole 1,350 comes off. The customer pays for
copies only.

**One waive, one meter, one decision.** The waive line names all three machines because it is
about all three — it is the rental of that line that it takes away.

---

## DEMO-20 — a waive on each machine, judged on its own copies

> **The deal.** Same three machines, same RM 450 each. But the customer says: *"Each machine earns
> its own. Whichever one prints RM 350 gets its rental free that month. The others pay."*

### Setting it up

Steps 1–6 as DEMO-19 (contract `DEMO-20`, three machines, rental 450, BK 0.0200, CL 0.2000,
one invoice, **Merge both**). Then:

7. Click the rental row's **Minimum / waive** cell.
8. Tick **Waive the rental**, choose **Each machine has its own**.
9. A sheet appears, one row per machine. Fill in each:

   | Machine | Model | Copies reach (RM) | Take off rental (RM) |
   |---|---|---|---|
   | DEMO-20-001 | iR-ADV C3560i | 350.00 | 450.00 |
   | DEMO-20-002 | iR-ADV C3560i | 350.00 | 450.00 |
   | DEMO-20-003 | iR-ADV C3560i | 350.00 | 450.00 |

10. **OK**. The cell reads `own waive each — 3 of 3 machines, up to 1,350.00 off`.
11. **OK**, then **Save**.

### What it bills

| Machine | Black | Colour | Its own copies | Reached 350? |
|---|---|---|---|---|
| 001 HSR60599 | 12,000 × 0.02 = 240 | 700 × 0.20 = 140 | **380** | yes → 450 off |
| 002 8YJ43775 | 15,000 × 0.02 = 300 | 400 × 0.20 = 80 | **380** | yes → 450 off |
| 003 FLL34589 | 8,000 × 0.02 = 160 | 500 × 0.20 = 100 | **260** | no → rental charged |

```
RENTAL   MONTHLY RENTAL (9/36)                       3     450.0000    1,350.00
WAIVE    RENTAL WAIVE                                1    -900.0000     -900.00
         Model: iR-ADV C3560i  (3 UNIT)
         S/N: HSR60599, 8YJ43775, FLL34589
         RENTAL WAIVE · 2 of 3 machines reached its target · 900.00 off the rental
           DEMO-20-001 HSR60599 : waived 450.00
           DEMO-20-002 8YJ43775 : waived 450.00
           DEMO-20-003 FLL34589 : target not reached, rental charged
BK       BLACK COPY + PRINT A4 & A3              35,000      0.0200      700.00
CL       COLOUR COPY + PRINT A4 & A3               1,600      0.2000      320.00
                                                              Total    1,470.00
```

**Three deals, one printed line.** All three credits come off the same rental line, so they print
as one figure — and the three rows underneath say which machines earned it. The quantity is **1**,
not 3: the line is a sum of separate amounts, not a count times a rate.

---

## DEMO-15 — three machines, three different waives

> **The deal.** The three machines were signed at different times on different terms. Machine one
> waives RM 495 once it prints RM 300. Machine two waives RM 815, but only at RM 900. Machine three
> is on a promotion — rental free for the first 24 months, whatever it prints.

### Setting it up

1. Contract `DEMO-15`, three machines. In **Meters...** give each its own rental:
   `495.00`, `815.00`, `640.00`; BK `0.0250`, CL `0.2500` on all three.
2. **Billing Setup** → **one invoice for everything**.
3. Tick all three → **Merge rental**. (Leave BK+CL un-merged — this customer wants a reading line
   per machine.)
4. Click the rental row's **Minimum / waive** cell → **Waive the rental** →
   **Each machine has its own**, and fill in the sheet:

   | Machine | Copies reach (RM) | Take off rental (RM) |
   |---|---|---|
   | DEMO-15-001 | 300.00 | 495.00 |
   | DEMO-15-002 | 900.00 | 815.00 |
   | DEMO-15-003 | — free months — | 640.00 |

5. **OK**, **Save**.

### What it bills

| Machine | Its deal | Printed | Result |
|---|---|---|---|
| 001 ZVY38710 | reach 300 → 495 off | 200 + 150 = **350** | waived 495.00 |
| 002 6ZJ59257 | reach 900 → 815 off | 250 + 250 = **500** | short — rental charged |
| 003 GHD47008 | free for 24 months | — | waived 640.00 (month 9 of 24) |

```
RENTAL   MONTHLY RENTAL (9/36)   S/N: ZVY38710       1     495.0000      495.00
RENTAL   MONTHLY RENTAL (9/36)   S/N: 6ZJ59257       1     815.0000      815.00
RENTAL   MONTHLY RENTAL (9/36)   S/N: GHD47008       1     640.0000      640.00

WAIVE    RENTAL WAIVE                                1   -1135.0000   -1,135.00
         Model: iR-ADV C3560i  (3 UNIT)
         S/N: ZVY38710, 6ZJ59257, GHD47008
         RENTAL WAIVE · 2 of 3 machines reached its target · 1,135.00 off the rental
           DEMO-15-001 ZVY38710 : waived 495.00
           DEMO-15-002 6ZJ59257 : target not reached, rental charged
           DEMO-15-003 GHD47008 : waived 640.00

BK / CL  per machine — six lines                                     1,350.00
                                                              Total    2,165.00
```

**Why three rental lines and only one waive line.** A printed line is one `Qty × Unit Price`, so
machines at 495, 815 and 640 cannot share one — the rental prints three times. The three credits
*can* share a line, because a merged credit prints as one amount rather than a count times a rate.

To make the rentals merge as well, put an agreed figure in **Use this price instead** on the rental
row. That is a pricing decision, so the screen asks for it rather than choosing one.

---

## DEMO-21 — one invoice per machine, carrying everything

> **The deal.** *"Bill each machine on its own page. I want to see that machine's rental, its waive,
> its minimum and its copies, all together, so I can hand the page to whoever runs it."*

### Setting it up

1. Contract `DEMO-21`, three machines. In **Meters...**, tick all three:
   **+ Rental** `450.00`, **+ Black (BK)** `0.0200`, **+ Colour (CL)** `0.2000`.
2. **Billing Setup** → under **This contract sends**, choose **one invoice per machine**.
3. Leave the machines un-merged — each one bills alone, so there is nothing to merge.
4. For each machine's rental row, click **Minimum / waive** and set both halves:

   | Machine | Waive the rental | Charge a minimum |
   |---|---|---|
   | DEMO-21-001 | reach **300.00** → **450.00** off | at least **300.00** a month |
   | DEMO-21-002 | reach **300.00** → **450.00** off | at least **300.00** a month |
   | DEMO-21-003 | reach **250.00** → **450.00** off | at least **400.00** a month |

5. **OK**, **Save**.

### What it bills

| Machine | Printed | Waive | Minimum |
|---|---|---|---|
| 001 FTK56926 | **400** | fires — 450 off | met, nothing to add |
| 002 ETL78554 | **150** | misses 300 | short 150, topped up |
| 003 6CZ62045 | **300** | fires (target 250) | short of 400, topped up 100 |

**Invoice 1 — FTK56926**
```
RENTAL   MONTHLY RENTAL (9/36)                       1     450.0000      450.00
WAIVE    RENTAL WAIVE                                1    -450.0000     -450.00
         RENTAL WAIVE - charges 400.00 >= target 300.00
BK       BLACK COPY + PRINT A4 & A3              15,000      0.0200      300.00
CL       COLOUR COPY + PRINT A4 & A3                 500      0.2000      100.00
COMMIT   MINIMUM COMMITTED PRINT CHARGES             1       0.0000        0.00
         MINIMUM 300.00 · copies 400.00 · over the minimum
                                                              Total      400.00
```

**Invoice 2 — ETL78554**
```
RENTAL   MONTHLY RENTAL (9/36)                       1     450.0000      450.00
WAIVE    RENTAL WAIVE                                1       0.0000        0.00
         RENTAL WAIVE 450.00 - charges 150.00 short of target 300.00
BK       BLACK COPY + PRINT A4 & A3               5,000      0.0200      100.00
CL       COLOUR COPY + PRINT A4 & A3                 250      0.2000       50.00
COMMIT   MINIMUM COMMITTED PRINT CHARGES             1     150.0000      150.00
         MINIMUM 300.00 · copies 150.00 · short 150.00
                                                              Total      750.00
```

**Invoice 3 — 6CZ62045**
```
RENTAL   MONTHLY RENTAL (9/36)                       1     450.0000      450.00
WAIVE    RENTAL WAIVE                                1    -450.0000     -450.00
         RENTAL WAIVE - charges 300.00 >= target 250.00
BK       BLACK COPY + PRINT A4 & A3              12,000      0.0200      240.00
CL       COLOUR COPY + PRINT A4 & A3                 300      0.2000       60.00
COMMIT   MINIMUM COMMITTED PRINT CHARGES             1     100.0000      100.00
         MINIMUM 400.00 · copies 300.00 · short 100.00
                                                              Total      400.00
```

Three invoices, **1,550.00** in all.

**Invoice 3 is the one to look at.** The waive fires *and* the minimum tops up, on the same machine,
in the same month — because they do different things. The waive credits the **rent**; the minimum
tops up the **copies**. And the waive was judged on the 300 the machine actually printed, not on the
400 it was topped up to.

**A waive that misses still prints**, at 0.00, with the reason. The customer signed a deal; the
invoice says it was measured and what the answer was. A silent row would leave them guessing.

---

## Reading the waive line

| What you see | What it means |
|---|---|
| `Qty 1 × -1350.00` | One waive, one decision, for the whole line |
| `Qty 1 × -900.00` with rows underneath | Several waives, judged one at a time, printed as one credit |
| `Qty 1 × 0.00` with a `short of target` note | The deal was measured this month and did not fire |
| `(3 UNIT)` and three serials | The credit belongs to those three machines' rental |
| `charges 1200.00 >= target 1000.00` | What was counted, and what it had to beat |

A waive line never prints `3 × -450`. Three machines each freeing their own rental are three
separate decisions, and two of them firing is not "three times one rate" — it is a sum.

---

## Testing them

Ready-made meter readings are in `tests/meter-json/` — `DEMO-15.json`, `DEMO-19.json`,
`DEMO-20.json`, `DEMO-21.json`. On the **Meter Reading Integration** screen, pick the billing
month, press `Ctrl+Shift+T` to reveal **TEST Fetch (JSON)**, paste one in, tick the rows and
**Generate**. The figures above are what should come out.
