# Installing 1.5.0.10

## Before you start

- Install into a **restored copy** of the account book, not the live one.
- Take a SQL backup of that copy first. The plug-in creates tables and one trigger in the book
  (see below), and uninstalling the plug-in does not remove them.
- Close AutoCount on every other machine that opens the same book.

## If 1.5.0.0 was installed and would not load

1.5.0.0 stopped while preparing a new account book with:

> Invalid column name 'MachineLineShows'

and the plug-in refused to load. **Install 1.5.0.1 straight over it** — there is nothing to
uninstall or clean up first. 1.5.0.1 picks up exactly where 1.5.0.0 stopped and finishes the job;
it was tested on a book left in that half-done state, and on a book that never had the plug-in.

## Steps

1. Double-click **`ATP-ServiceContract-1.5.0.10-uat.app`**.
   AutoCount opens its Plug-in Manager install dialog.
2. Check it reads **Service Contract Photocopier, version 1.5.0.10**, then press **Install**.
3. Start AutoCount and log in to the book you are testing.
4. The first load does the database work by itself — this takes a few seconds on a large book.
   If anything fails here the plug-in refuses to load and tells you why, rather than half-installing.
5. A **Service & Contract** menu appears on the menu bar. That is the module.

## What the first load does to the account book

It changes one thing that already exists, once -- see **Free copies move into Free Qty** below.
Otherwise nothing existing is altered or deleted. It adds:

- **Its own tables**, all named `zSCP_*` / `zSCP2_*` — contracts, machines, meters, readings,
  prices, the billing log, e-mail jobs. None of AutoCount's own tables are changed.
- **One trigger on `dbo.IV`**, `TR_zSCP2_IV_NoDeleteEarlierBilledMonth`. It refuses to delete a
  meter invoice while a later month of the same contract is still invoiced — including when the
  delete is done inside AutoCount's own Invoice screen, where the plug-in is never asked.
  It never blocks anything else: a normal invoice, a normal edit, a normal save are untouched.
- **One user-defined field**, `ServiceItemNo` on stock issue detail.
- **Access rights and lookup data** for the module's own screens.

Every one of these is created only if it is not already there, so loading the plug-in again — or
installing a later version — repeats the check and changes nothing that is already correct.

### Free copies move into Free Qty (1.5.0.10, first load only)

From 1.5.0.10 a meter's free copies are its **Free Qty** on the contract's meter grid, whether or
not it has a tier price, and a tier price may not start at 0.00. A tier price written the old way --
"the first 100 copies at 0.00, then up to 1,000 at 0.023, then 0.021" -- is converted:

- each meter priced by it takes the 0.00 tier's copies as its Free Qty (100 in the example);
- a meter type whose default tier price it is takes them as its default Free Qty, for meters added later;
- the 0.00 tier is removed, leaving "up to 1,000 at 0.023, then 0.021".

The amounts do not change: the tier reached is still decided by all the copies read, and the same
copies come off free. Checked on a book with 98,005 billed meter lines: every one the same before and
after. Left as they are, and billing as before: a tier price with nothing but a 0.00 tier (a meter
that is free altogether), and a tier price that a copy group uses in Billing Setup -- the group's free
copies are that tier, and no single meter could take them over.

The same first load clears two Free Qty values that were sitting unused and would otherwise start
giving copies away: on a meter whose tier price starts at a paid rate (until now a meter with a tier
price ignored its Free Qty), and on a machine priced by its copy group's tiers (until now the group
took its free copies from its tiers alone). Rental meters are never touched -- their Free Qty is free
months. Nothing is cleared on any later load: a Free Qty keyed in after the upgrade stays.

## Check it worked

- The **Service & Contract** menu is on the menu bar.
- **Help → About** (or the module's About) reads **1.5.0.10**.
- **Service & Contract → Maintain Service Contract** opens a list, even if it is empty.

If the menu is missing, the plug-in did not load: AutoCount's Plug-in Manager will say whether it
is installed, and the error it showed at startup says why.
