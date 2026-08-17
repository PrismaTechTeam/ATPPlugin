# Golden harness — proving the new engine issues the old invoices

The one question the customer will ask before letting the plugin issue a real invoice is: *does it
produce what we already send?* This answers it per customer, to the sen.

```
extract.py   the 22 July-2026 PDFs  ->  tests/golden/2607/*.json      the answer key
compare.py   answer key  vs  engine output                            the verdict
```

## Why the PDFs and not the worksheets

Page 1 of each PDF is Jean's Excel. It is **not** authoritative and is deliberately ignored:
Kensington's sheet totals `1,764` where the invoice beside it says `2,236.56`. Only the invoice
pages are read — those are what the hospital received and paid.

## Running it

```bash
py tests/golden/extract.py                       # rebuild the answer key
py tests/golden/compare.py --actual out/2607     # grade a generated run
py tests/golden/compare.py --actual out/2607 --only "PASIR GUDANG"
```

`extract.py` refuses to produce a quiet answer key. Every invoice it reads must have its own lines
add up to its own printed total, and it exits non-zero listing any that do not — a wrong answer key
is worse than none.

Current state: **35 invoices, 189 charge lines, all internally consistent.**

Three files carry no invoice page and are reported rather than hidden:

| File | Why |
|---|---|
| `3000-F0019 METER 2607.pdf` | worksheet only, no invoice attached |
| `3000-J0056 METER 2607.pdf` | worksheet only |
| `MBJB.pdf` | bespoke three-page template, no document-number field in the usual place |

MBJB is the layout the rollout plan already puts last and keeps manual until then.

## What is compared

| Level | What | Tolerance |
|---|---|---|
| L0 | invoice count, and which machines are on which invoice | exact |
| L1 | line count and order | exact |
| L2 | quantity, unit price, amount | qty and price exact; amount ±0.05 |
| L3 | net total | ±0.05 |
| L4 | current, previous, usage, FOC qty, rebate qty | exact |

The ±0.05 on amounts exists for one documented reason and no other: a merged line rounds once, so
it can land a sen or two from the sum of its machines — HSI prints `3,048.36` where the per-machine
charges add to `3,048.37`, and the issued invoice is the truth. Anything wider is a real
difference. Quantities and unit prices have no tolerance at all.

## What is not compared, and why

**Item codes.** On ATP's template the Item Code and Description columns physically overlap in the
PDF, so no extractor can separate them (`01.MR.BK. HOSPIT0A1.LMPRG.BK.HOSPITAL PASIR GUDANG`).
They would be the wrong thing to compare in any case — replacing `RA-3 UNIT` and
`MR.BK.4MU10545` with canonical codes is the point of the migration, so the codes are *expected* to
differ while every figure stays identical.

**Reading dates.** The legacy dates are not always real: Rompin's `AMR2607.0087` prints readings
dated 25/07 against service vouchers taken on 24/07, and MBJB prints 01/07–31/07 regardless of when
anyone visited.

## Producing the `--actual` side

Restore the production book, seed each machine's June and July readings from its service voucher
(**not** from the last invoice — a machine that inherits a summed `.C` reading goes negative the
next month and clamps to a silent RM0.00 invoice), assign each of the 21 contracts its Billing
Format, run Generate for July, and dump `IV`/`IVDTL` into the same JSON shape.

## The six that have to pass before anyone goes live

Between them they cover all eleven layouts and both flags:

| Customer | What it proves |
|---|---|
| PASIR GUDANG | two invoices, rental per model, BK merged across four models, FOC rental, an RM0.00 e-invoice |
| TANGKAK | one set of invoices per machine, FOC and 3% rebate, three RM0.00 meter invoices |
| ROMPIN | a machine split onto its own invoice, negative usage clamped, summed readings, ASN entity |
| MARA | BK and CL paired per machine, SST on the rental only |
| JPJ | rental split by model while all twelve machines share one BK line |
| KENSINGTON | committed minimum, no rental line, four-decimal unit price |

All 21 must pass L0–L3; these six must also pass L4 and survive a real MyInvois pre-production
submission.
