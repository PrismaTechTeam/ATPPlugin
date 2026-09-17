# Inter-billing — billing off another company's machines

Two AutoCount account books, one owner.

- **The parent** holds the contract with the machines on it, and takes the readings.
- **The subsidiary** has the real end customer, and issues the bill.

The parent bills the subsidiary. The subsidiary bills the customer. **The two prices are different**,
and neither company keys the same machines, the same serial numbers or the same readings twice.

---

## The rule the whole thing hangs on

> **They own the meters. You own the money.**

| Fact | Owner |
|---|---|
| Which machines, which counters, which serial numbers | **the parent** |
| The readings | **the parent** |
| Prices, ladders, minimums, waives, bill groups, merging, how many invoices | **you** |

A contract taken from the other book is **your own contract that shares their machines and
readings** — not a copy of theirs. That one distinction decides everything else on this page.

---

## Setting it up

**General Setup ▸ Inter-Billing Setup**

1. **New**.
2. **Name for this connection** — what every other screen will call the other book ("HQ").
3. **SQL server** and **Account book** — where their book lives.
4. **User name** and **Password**, or tick to sign in as the Windows user.
5. **Test connection**.

The test is not a convenience button. It reads the other book's own permanent identity, and until a
book has been reached it has no identity here — nothing can be linked to a book that cannot be named.
The test also tells you:

- what company you actually connected to, so you can see you pointed it at the right one;
- that the book does not have this plug-in installed, if it does not;
- that you pointed it at **this** book, which is refused — a book linked to itself would show its own
  contracts as incoming and let one machine be billed to one customer twice.

The password is stored encrypted and never shown again. Leave it blank when you edit the row later
and the stored one is kept.

---

## Taking a contract

**Inter-Billing ▸ 1. Their contracts**

1. Pick the other book at the top.
2. Pick one of their contracts. Its machines and counters are listed on the right.
3. **Bill it to** — choose *your* customer.
4. **Make my contract from this**.

Step 3 is the whole reason this is a decision and not a synchronisation. On their contract the
customer is **your company**, because you are who they bill. The contract made here names the customer
**you** bill.

### What comes across, and what does not

| | |
|---|---|
| machines, serial numbers, models | **yes** — facts about equipment both companies must agree on |
| counters (rental / black / colour / anything else) | **yes** |
| opening readings | **yes** — without them the first month bills the meter's whole life |
| meter types the book has never seen | **created**, with their name and meaning — never their rates |
| **prices, minimums, waives** | **no** |
| their bill groups, merge groups, line labels | **no** |
| their customer | **no** |

**A contract taken from them arrives unpriced and bills nothing.** That is deliberate and it is the
point of the module: what a copy through their counter is worth is an agreement between *you* and
*your* customer, and there is no honest way to guess it. An unpriced counter shows as zero on the
preview, which is a problem somebody can see; a price quietly inherited from the other company is one
nobody sees until the customer does.

So the next step is always: open the contract, **Meters & Pricing**, and put in the prices you agreed.

---

## Their readings

**Inter-Billing ▸ 3. Their readings**

1. Set the year and month.
2. **See what they have** — their reading beside yours, for every linked counter.
3. **Bring them over**.

Readings are the one thing that always follows. They were never yours: the counter is on their
machine, they read it, and a billing book that argued with the number would be inventing usage.

The only thing that is never overwritten is **a reading you have already invoiced**. That one has been
sent to a customer. The screen says so and leaves it.

Brought readings are stamped `INTERBILL`, so where a figure came from is answerable a year later —
which is the first question asked when a customer disputes a copy count.

---

## When they change something

**Inter-Billing ▸ 2. What they changed**

Nothing here touches your contract until you say so. Each line offers two answers:

- **Take this change too**
- **Leave mine as it is** — and it stops being reported

| What they did | What the list says | What "take it" does |
|---|---|---|
| put another machine on the contract | it is not on yours | adds it here, unpriced, and links it |
| added a counter to a machine | they added this counter | adds it here, unpriced |
| took a machine off hire | yours is still billing it | marks yours inactive; everything it has billed stays |
| removed a counter | no more readings will come for it | notes it; nothing on your contract changes |
| changed a serial, a model, an end date | the old value → the new one | records that you have seen it |
| the contract is gone from their book | it is no longer there | notes it; your contract is yours |

Nothing in their book can undo a document you have issued. If they cancel an invoice you billed
against, you are told, and **you** decide whether to raise a credit note.

---

## What you may and may not do to a borrowed contract

**You may** price it, group it, merge its lines, split it across bill groups, put it on as many
invoices as you like, and **add counters of your own** — you may have sold the customer something the
other company knows nothing about.

**You may not delete a machine or a counter that came from them.** It is their equipment, and this
book does not get to decide it stopped existing. The refusal stands in three places:

1. the counter tick in **Meters & Pricing**,
2. the **−** on the **Meter Configuration** panel,
3. the **contract save**, as the last gate — because greying a control has never stopped a save from
   writing.

If they have genuinely taken it off hire, it appears under *What they changed*, and you stop billing
it there.

### How you can tell

- the contract window is titled **Service Contract — machines from HQ**;
- borrowed counters are shaded blue on the Meter Configuration panel, and the panel's own caption
  says **counters from HQ — they cannot be removed here**.

---

## When the other book cannot be reached

Every read says what the server said and changes nothing. You cannot take a new contract or bring new
readings; contracts already taken carry on billing off the machines and readings already here,
because those are yours.

---

## How it is stored

Three tables, and the first two exist in every book whether or not anybody uses this.

| Table | What it holds |
|---|---|
| `zSCP2_BookIdentity` | one row, one GUID: **who this book is**, permanently |
| `zSCP2_InterBillBook` | where the other book is, and its identity as learned by the test |
| `zSCP2_InterBillLink` | what here came from there, and what it looked like when it was taken |

**Why a GUID.** A database name is whatever the person restoring the backup typed; a company name
changes when the company is renamed, and two books on one server can carry the same one. A GUID
issued once is the book, for ever.

**Why the link stores keys, not numbers.** `SourceKey` holds their `ContractKey` / `ItemKey` /
`ItemMeterKey` / `DocKey`. Contract numbers and document numbers are edited, reused and duplicated
across books; a surrogate key cannot move. Their number is kept too, but only so a person reading the
table can tell what they are looking at.

**Why the link stores a snapshot.** Each link keeps their figures as they stood at the moment it was
taken. Without it there is no telling "they changed this" from "it was always like that", and
*What they changed* could not exist. With it the whole list is a set comparison:

```
in their book, not linked here      -> a machine was added
linked here, gone or inactive there -> a machine came off hire
linked both sides, figures differ   -> they changed a detail
```

A link with `LocalKey = 0` is an offer that was turned down, so the same offer is not made again every
month.

An ordinary contract has no links at all, so every question the guard asks answers "no" off an index
seek. A book that never uses inter-billing behaves exactly as it did.

---

## Decisions, and why

All six were agreed 2026-09-07 and all six are built.

1. **The subsidiary pulls. One direction.** Only it knows which contracts it wants, when, and which
   customer to attach. Everything it needs is a read. It never writes to the parent, and the parent
   never writes into its ledger.
2. **Credentials: encrypt in, decrypt out, never shown back.** Both books belong to the same owner, so
   this is a convenience store and not a trust boundary. Deliberately not DPAPI — that ties the stored
   password to one Windows account on one machine, so the clerk who takes over next year would find it
   unreadable with no way to tell why.
3. **Their later changes are told, not followed.** Following automatically would silently change what
   you charge your customer; ignoring it would let you keep billing a machine that went off hire.
   Readings are the exception, and always follow.
4. **A cancelled invoice over there is news, not an action.** Your invoice is a real document to a real
   customer and may already be paid.
5. **A borrowed counter cannot be removed — refused at the action, and again at save.**
   Adding counters is allowed. Only deleting theirs is not.
6. **Your contract carries its own grouping, bill groups and invoice split.** How your invoice prints
   is your business.

---

## Testing it

Both books are real account books. The parent used for testing is **AED_ASNDUMMY**.

```
powershell tests\interbill-checkun.ps1              # the whole module, end to end
powershell tests\interbill-checkun.ps1 -Mode clean  # reset and stop
powershell tests\interbill-check\smoke-forms.ps1      # open every screen off-screen, catch what throws
```

Setting up a parent book from scratch — create it in AutoCount (Manage Account Book), then:

```
powershell tests\interbill-check\install-into-book.ps1 -Database AED_ASNDUMMY
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i tests\interbill-check\seed-charge-items.sql
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i ServiceContractPhotocopier\SQL_Seed_GLMast_Debtors.sql
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i ServiceContractPhotocopier\SQL_Seed_Debtor.sql
sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i tests\interbill-check\seed-parent-book.sql
```

`install-into-book.ps1` does to another book exactly what the launcher and the plug-in do between
them: registers the package, runs the migrations, and grants the access rights — because a right that
is declared and granted to nobody leaves every menu hidden, which reads as a failed install.

`run.ps1` drives the same classes the screens call — the connection test, the reader, the take, the
links, the guard, the readings — so a green run says the screens will do the same. It checks, among
other things, that not one price crossed, that opening readings did, that removing a borrowed counter
is caught, that bringing readings twice makes no second row, and that an invoiced reading is left
alone. Every run resets both books first, so the counts it checks always mean the same thing.

`smoke-forms.ps1` opens each screen without showing it and raises its Load event — the same event the
menu item raises. A designer or load-time fault survives a green compile and a green `run.ps1`; this
is what catches it. It checks the window title afterwards, because a form that throws half way
through its load leaves the designer's caption in place and otherwise looks fine.

## Known gaps

1. **A restored book keeps its identity.** Restore an account book to make a *second* company and both
   books answer to the same GUID. The plug-in cannot tell that restore from a recovery, so it does not
   reissue. The dangerous case — linking a book to itself — is refused by the connection test; the
   remaining case (two books that both claim to be the parent) would need a "this is a new company,
   issue a new identity" action somewhere in setup.
2. **A reading cannot be refused.** If your customer disputes a counter you have their figure and no
   way to change it — the answer today is a credit note in your own book. Whether a dispute should
   travel back to the parent has not been decided.
3. **Their invoices are not yet listed.** The reader can already tell which of their invoices have been
   cancelled; nothing calls it, because the invoice side of the module has not been built. Contracts,
   machines, counters and readings are.
