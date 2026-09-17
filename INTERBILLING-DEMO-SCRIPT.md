# Inter-billing — recording script

Everything below is set up and ready. The parent book has three machines and a month of readings;
the billing book has no connection and nothing taken, so every step is done for real on camera.

**Reset between takes:** `powershell tests\interbill-check\run.ps1 -Mode clean`
That empties the billing book and puts the parent back to its three machines. Nothing else to undo.

---

## The deal, in the customer's words

> "My holding company owns the copiers and reads the meters. My subsidiary has the actual customer.
> The holding company bills the subsidiary at one price, the subsidiary bills the customer at
> another — and right now somebody types the same twelve machines and the same readings into two
> systems every month."

The whole video is one sentence:

> **They own the meters. You own the money.**

---

## The two books

| | |
|---|---|
| **ASNDUMMY** | the parent — owns the machines, takes the readings |
| **ATPDEMO0001** (`AED_ATPTEST`) | the subsidiary — has the end customer, issues the bill |

The parent's contract **HQ-2026-001** is billed to *THE SUBSIDIARY SDN BHD* — because on the parent's
books, the subsidiary **is** the customer. That is worth pointing at on camera; it is why taking a
contract needs a decision and not a copy.

| machine | model | rental | black | colour |
|---|---|---|---|---|
| HQA-001 | iR-ADV C3560i | 380.00 | 0.0180 | 0.1800 |
| HQA-002 | iR-ADV C3560i | 380.00 | 0.0180 | 0.1800 |
| HQA-003 | iR-ADV C3560i | 380.00 | 0.0180 | 0.1800 |

September 2026 usage, every machine: **8,400 black, 1,250 colour**.

---

## 1 — Connect to the other book  *(about 60 seconds)*

1. **General Setup ▸ Inter-Billing Setup**
2. **New**
3. Name for this connection: `HQ`
4. SQL server: `localhost,1433`
5. Account book: `AED_ASNDUMMY`
6. User name: `sa` — Password: `rs6663`  *(blur this in the edit if you like)*
7. **Test connection**

> **Connected to ASNDUMMY.**

**Say:** the test is not a "does the network work" button. It reads the other book's own permanent
identity — a GUID that book issued itself and never reissues. Every link made afterwards names that
GUID, not the database name, because a restored backup gets whatever database name somebody typed.

8. **Save**

**Worth showing, once it is saved:** change *Account book* to `AED_ATPTEST` — this book — and press
Test.

> **That is this book. Inter-billing connects TWO books — point this at the other company's account book.**

Then change it back to `AED_ASNDUMMY`, **press Test again**, and **Save**.

> Test again is not ceremony. The row now says `AED_ATPTEST` on screen, and saving it there would
> leave the connection pointing at the wrong book with the right book's identity still attached to
> it. Retesting puts both back in step.

---

## 2 — Take the contract  *(about 90 seconds — the centre of the video)*

1. **Inter-Billing** (top-level menu)
2. Top left, the other book is already chosen: `HQ — ASNDUMMY`
3. Tab **1. Their contracts** — one row:

   | Their contract | Billed to (over there) | Machines | Here |
   |---|---|---|---|
   | HQ-2026-001 | THE SUBSIDIARY SDN BHD | 3 | *(blank)* |

4. Click it. The right-hand grid fills with **HQA-001 / 002 / 003**, each showing `RENTAL, BK, CL`.
5. **Bill it to** — pick any customer of *this* book.
6. **Make my contract from this**

The confirmation says what is about to happen:

> Make a contract here from HQ-2026-001?
> 3 machines, 9 counters, billed to …
> **Prices do not come across. The contract arrives unpriced and bills nothing until you agree its
> prices in Meters & Pricing.**

7. **Yes**

> Contract SC 0000000xx made — 3 machines, 9 counters.

**Say, while the message is on screen:** the machines came, the serial numbers came, the opening
readings came. **Not one price came.** That is not a limitation — it is the point. What a copy is
worth to *your* customer is an agreement between you and them; the holding company's rate is a
different agreement with a different company.

Notice the **Here** column now names your contract, so nobody takes it twice by accident.

---

## 3 — It really did arrive unpriced  *(about 45 seconds)*

1. **Maintain Service Contract** → open the contract just made
2. The window title:

   > **Service Contract — EDIT · SC 0000000xx     ·     machines from HQ**

3. Machines tab — three machines, the right serial numbers, the right models
4. Click a machine. On the **Meter Configuration** panel below, the RENTAL / BK / CL rows are
   **shaded blue**, and the panel's caption reads:

   > Meter Configuration — SC 0000000xx-001   ·   **counters from HQ — they cannot be removed here**

5. Unit Price on every row: **0.00**

**Say:** an unpriced counter bills nothing, and that is visible. A price quietly inherited from the
other company is invisible until the customer sees it.

---

## 4 — You own the money  *(about 90 seconds)*

1. On the machine list, **Meters & Pricing**
2. Type the prices *this* company agreed with *its* customer:

   | | |
   |---|---|
   | Rental | **450.00** |
   | Black | **0.0250** |
   | Colour | **0.2500** |

   Type it on one machine, tick the others, and it follows.
3. **OK**, then **Save** the contract.

**Say:** the parent charges 380 and 0.018. This company charges 450 and 0.025. Both are real prices
somebody agreed — and neither system had to know the other's.

---

## 5 — Their readings  *(about 60 seconds)*

1. **Inter-Billing** → tab **3. Their readings**
2. Year `2026`, month `9`
3. **See what they have**

   | My contract | Serial no | Counter | Their reading | Here |
   |---|---|---|---|---|
   | SC 0000000xx | HQA-001 | BLACK COPY | 109,400 | not here yet |
   | SC 0000000xx | HQA-001 | COLOUR COPY | 31,750 | not here yet |
   | … | | | | |

4. **Bring them over**

   > 6 readings brought over for 9/2026.

**Say:** readings are the one thing that always follows. The counter is on their machine and they
read it — a billing book that argued with the number would be inventing usage. The only thing never
overwritten is a reading this book has already invoiced; that one has gone to a customer.

Press **Bring them over** a second time to show it does nothing — the rows now read *same here
already*.

---

## 6 — What you may not do  *(about 60 seconds — the trust moment)*

Back on the contract, on the Meter Configuration panel:

1. Select the blue **CL** row, press **−**

   > This counter comes from HQ.
   > Which machines are on the contract, and which counters are on them, is theirs to decide — it is
   > their equipment. What is yours is the money: prices, minimums, waives, bill groups and how the
   > invoice is split.

2. Now try the other route: **Meters & Pricing**, untick **Colour** on a machine, **OK**

   > These counters came from another account book and cannot be taken off: …

**Say:** it is refused in three places — the tick, the minus, and the contract save itself. Greying a
control out has never stopped a save from writing, so the save checks again as the last gate.

---

## 7 — They change something  *(about 75 seconds)*

Off camera, run one line:

```
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i tests\interbill-check\demo-add-machine.sql
```

That is the holding company putting a fourth machine on the contract — a **iR-ADV C5850i**, serial
**HQA-005**, deliberately a different model so it is obvious.

1. **Inter-Billing** → **Refresh** → tab **2. What they changed**

   | My contract | What | Which | Changed |
   |---|---|---|---|
   | SC 0000000xx | Machine | HQA-005  iR-ADV C5850i | they put this machine on the contract — it is not on yours |

2. **Take this change too** → **Yes**

   > HQA-005 … added to SC 0000000xx.

3. Show the machine is now on your contract — **and unpriced, like every machine taken from them.**

**Say:** nothing they do reaches into this book on its own. Their changes are *told*, not *followed*.
The one exception is readings, because those were never yours. And if you do not want the machine,
**Leave mine as it is** — it stops being reported and never comes back.

---

## 8 — The invoice  *(optional — rehearse this once before recording)*

Meter Reading Integration → the usual monthly run, September 2026.

Expected, at this company's prices:

| | per machine | × 3 machines |
|---|---|---|
| Monthly rental | 450.00 | 1,350.00 |
| Black — 8,400 × 0.0250 | 210.00 | 630.00 |
| Colour — 1,250 × 0.2500 | 312.50 | 937.50 |
| | **972.50** | **2,917.50** |

At the parent's own prices the same month is **2,268.60** — which is what the holding company bills
this company. The difference, **648.90**, is the subsidiary's margin, and neither book had to know
the other's number to get there.

> **Not yet rehearsed.** Every step above has been run end to end; the invoice run has not been
> driven for an inter-billed contract specifically. Do it once off camera first — and if it does not
> come out at 2,917.50, that is worth knowing before the recording, not during it.

---

## The closing line

> Two companies, one set of machines, two prices, and nobody keys a reading twice.
> They own the meters. You own the money.

---

## If a take goes wrong

```
powershell tests\interbill-check\run.ps1 -Mode clean
```

Back to: no connection, nothing taken, parent at three machines. Start again from step 1.
