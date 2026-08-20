# Billing scenarios — settings and what they print

Twelve demo contracts, one per shape. Open any of them, press **Sample Invoice**, compare.

---

## The four settings

Three live on the **Billing Format** (General Setup → Billing Format). One lives on the machine.

| Setting | Where | Values |
|---|---|---|
| **How many invoices** | Format | One invoice · Rental on its own invoice · One per machine · Per machine, rental apart |
| **Rental lines** | Format | 1 line each (merge all) · per model · per machine |
| **Black & Colour lines** | Format | 1 line each (merge all) · per model · per machine |
| **The machine line says** | Format | Model + line label · Model only · **Line label only** |
| **Line label** | Machine row | Free text — `HEAVY DUTY "65 ppm"` |

Codes used in the tables below: **A** = merge all · **M** = per model · **S** = per machine.

---

## The twelve scenarios

| # | Contract | Format | Invoices | Rental | BK+CL | Machines | Result |
|---|---|---|---|---|---|---|---|
| 1 | DEMO-01 | `1INV-ALL` | One | A | A | 5 | 1 invoice, **3 lines** |
| 2 | DEMO-02 | `1INV-RMODEL` | One | M | A | 6 | 1 invoice, **5 lines** |
| 3 | DEMO-03 | `1INV-MMACHINE` | One | A | S | 4 | 1 invoice, **7 lines** |
| 4 | DEMO-04 | `1INV-BYMODEL` | One | M | M | 6 | 1 invoice, **9 lines** |
| 5 | DEMO-05 | `2INV-ALL` | Rental apart | A | A | 6 | 2 invoices, **3 + 3** |
| 6 | DEMO-06 | `2INV-RMODEL` | Rental apart | M | A | 5 | 2 invoices, **3 + 2** |
| 7 | DEMO-07 | `2INV-MMACHINE` | Rental apart | A | S | 5 | 2 invoices, **1 + 10** |
| 8 | DEMO-08 | `2INV-RMODEL-MM` | Rental apart | M | S | 5 | 2 invoices, **2 + 11** |
| 9 | DEMO-09 | `2INV-EACH` | Rental apart | S | S | 4 | 2 invoices, **4 + 5** |
| 10 | DEMO-10 | `2INV-RMACHINE` | Rental apart | S | A | 5 | 2 invoices, **5 + 2** |
| 11 | DEMO-11 | `PERMACHINE` | Per machine, rental apart | S | S | 3 | **5 invoices**, 6 lines |
| 12 | DEMO-12 | *(none)* | — | — | — | 5 | 1 invoice, **11 lines** |

---

## 1 · DEMO-01 — everything on one line each

`1INV-ALL` · One invoice · Rental **A** · BK+CL **A**

```
MONTHLY RENTAL (13/36)                     5   450.0000    2,250.00
BLACK COPY + PRINT A4 & A3            30,195     0.0250      754.89
COLOUR COPY + PRINT A4 & A3            6,555     0.2500    1,638.75
```

Five machines, three lines. They merge because they share a price.

---

## 2 · DEMO-02 — rental by model, meters together

`1INV-RMODEL` · One invoice · Rental **M** · BK+CL **A**

```
MONTHLY RENTAL (13/36)                     1  1,287.2500   1,287.25
MONTHLY RENTAL (13/36)                     2    655.5000   1,311.00
MONTHLY RENTAL (13/36)                     3    476.9000   1,430.70
BLACK COPY + PRINT A4 & A3            38,073      0.0285   1,085.07
COLOUR COPY + PRINT A4 & A3            7,240      0.2850   2,063.41
```

Three models → three rental lines. Meters all at one rate → one line each.

Note CL covers 5 machines, not 6 — one machine is black-only.

---

## 3 · DEMO-03 — one rental line, meters per machine

`1INV-MMACHINE` · One invoice · Rental **A** · BK+CL **S**

```
MONTHLY RENTAL (13/36)                     4    520.0000   2,080.00
BLACK COPY + PRINT A4 & A3             4,813      0.0240     115.51
COLOUR COPY + PRINT A4 & A3            1,037      0.2400     248.88
BLACK COPY + PRINT A4 & A3             5,426      0.0250     135.65
COLOUR COPY + PRINT A4 & A3            1,174      0.2500     293.50
BLACK COPY + PRINT A4 & A3             6,039      0.0220     132.86
BLACK COPY + PRINT A4 & A3             6,652      0.0230     153.00
```

BK and CL of the same machine sit together. The last two machines are black-only.

---

## 4 · DEMO-04 — everything by model

`1INV-BYMODEL` · One invoice · Rental **M** · BK+CL **M**

```
MONTHLY RENTAL (13/36)   C5880i         3    880.0000   2,640.00
MONTHLY RENTAL (13/36)   C7565i         2    720.0000   1,440.00
MONTHLY RENTAL (13/36)   C910           1  1,150.0000   1,150.00
BLACK ...                C5880i    16,278      0.0200     325.56
COLOUR ...               C5880i     3,522      0.2800     986.16
BLACK ...                C7565i    13,917      0.0200     278.34
COLOUR ...               C7565i     3,033      0.2800     849.24
BLACK ...                C910       7,878      0.0200     157.56
COLOUR ...               C910       1,722      0.2800     482.16
```

Every BK here is at the same 0.0200 — they still split, because the setting is **by model**, not by price.

---

## 5 · DEMO-05 — rental invoice + meter invoice

`2INV-ALL` · Rental apart · Rental **A** · BK+CL **A**

| Invoice | Lines |
|---|---|
| Rental | 3 (three different rental prices) |
| Meters | 3 |

---

## 6 · DEMO-06 — same, rental by model, with a waive

`2INV-RMODEL` · Rental apart · Rental **M** · BK+CL **A**

| Invoice | Lines |
|---|---|
| Rental | 2 rental + **1 waive** = 3 |
| Meters | 2 |

The waive line is a credit against the rental it belongs to. **The Sample Invoice does not show it** — whether it fires is decided at Generate against the real month.

---

## 7 · DEMO-07 — one rental line, meters per machine

`2INV-MMACHINE` · Rental apart · Rental **A** · BK+CL **S**

| Invoice | Lines |
|---|---|
| Rental | 1 (five machines merged) |
| Meters | 10 (five machines × BK + CL) |

---

## 8 · DEMO-08 — rental by model, meters per machine

`2INV-RMODEL-MM` · Rental apart · Rental **M** · BK+CL **S**

| Invoice | Lines |
|---|---|
| Rental | 2 |
| Meters | 11 |

---

## 9 · DEMO-09 — nothing merges

`2INV-EACH` · Rental apart · Rental **S** · BK+CL **S**

| Invoice | Lines |
|---|---|
| Rental | 4 (one per machine) |
| Meters | 5 |

The most itemised layout. Every line names its machine.

---

## 10 · DEMO-10 — rental per machine, meters merged

`2INV-RMACHINE` · Rental apart · Rental **S** · BK+CL **A**

| Invoice | Lines |
|---|---|
| Rental | 5 |
| Meters | 2 |

The reverse of #7.

---

## 11 · DEMO-11 — one invoice per machine

`PERMACHINE` · Per machine, rental apart · Rental **S** · BK+CL **S**

3 machines → **5 invoices**, not 6:

| Invoice | Contents |
|---|---|
| DEMO-11-001 rental | RENTAL 990.00 |
| DEMO-11-001 meters | BK |
| DEMO-11-002 rental | RENTAL 640.00 |
| DEMO-11-002 meters | BK |
| DEMO-11-003 meters | BK + CL |

The third machine has **no rental meter**, so it gets no rental invoice. Invoice count follows what exists, not machines × 2.

---

## 12 · DEMO-12 — no Billing Format (the control)

*(no format)* · Same five machines and prices as DEMO-01

| | DEMO-01 (with format) | DEMO-12 (no format) |
|---|---|---|
| Lines | **3** | **11** |
| Rental | 5 machines → 1 line | 5 machines → 1 line |
| BK | 1 line | **5 lines** |
| CL | 1 line | **5 lines** |
| Money | same | same |

Without a format the old rules apply: rental still merges (same type, same price, same contract), **usage never merges**. Existing contracts print exactly as they did before the plugin.

---

## What splits a line, what does not

| Splits a line | Does **not** split a line |
|---|---|
| A different **price** | A different **line label** |
| The **model**, when the mode is *per model* | A different **serial** |
| A **hand-made merge group** | A different **FOC** or **rebate** |
| A different **strategy note** | A different **reading date** |
| A **committed minimum** or **waive** (always alone) | |

**Two machines at different prices can never share a line** — a printed line is one `Qty × Unit Price = Amount`, and one line can only carry one price.

---

## Line order on the invoice

1. All rentals
2. Waives (they reduce the rentals above)
3. Black and Colour, **grouped by machine** — BK, CL, BK, CL
4. Committed minimum last (it measures the charges above it)

---

## The machine line

Set on the format: **The machine line says**.

| Setting | A merged line | A per-machine line |
|---|---|---|
| Model + line label | `MONTHLY RENTAL (13/36)  HEAVY DUTY "80 ppm"`<br>`MODEL:iR-ADV DX C5880i  (3 UNIT)` | `…`<br>`MODEL:imageFORCE 6170  S/N:ULS64059` |
| Model only | `MONTHLY RENTAL (13/36)`<br>`MODEL:iR-ADV DX C5880i  (3 UNIT)` | `…`<br>`MODEL:imageFORCE 6170  S/N:ULS64059` |
| **Line label only** | `MONTHLY RENTAL (13/36)`<br>`HEAVY DUTY "80 ppm"  (3 UNIT)` | `…`<br>`HEAVY DUTY "70 ppm"  S/N:ULS64059` |

Two rules inside **Line label only**:

- A line that stands for **one machine** still carries its serial. A merged line does not — `(3 UNIT)` already describes it.
- A machine with **no line label** falls back to model + serial, line by line.

All twelve demo contracts are currently set to **Line label only**.

---

## Meter lines carry their readings

Every BK and CL line prints its breakdown underneath:

```
BLACK COPY + PRINT A4 & A3            4,813     0.0190      91.45
HEAVY DUTY "70 ppm"  S/N:ULS64059
Current Meter Reading (01/08/2026) : 145421
Previous Meter Reading (01/07/2026) : 140608
Meter Charges Usage : 4813
```

A merged meter line adds `S/N : …` listing every machine on it.

Rental, waive and committed-minimum lines print **no readings** — there is no meter behind them.

---

## Reading the Sample Invoice

The header line tells you what it did:

```
Format: 2 invoices, rental per model, BK+CL per machine    2 invoices, 13 lines, 6,475.56 a month
```

Real: machines, meters, prices, grouping, how many invoices, how many lines, every word on every line.

Invented: the **meter readings** only. So the copy counts and the meter money are illustrative — the rental money is exactly what you set.

Nothing is written to the account book.
