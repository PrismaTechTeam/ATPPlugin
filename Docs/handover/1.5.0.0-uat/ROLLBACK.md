# Taking 1.5.0.0 back out

## The quick way — stop it loading

**Tools → Plug-in Manager → Service Contract Photocopier → Uninstall**, then restart AutoCount.

The **Service & Contract** menu disappears and nothing in the module runs again — no auto-fetch, no
credit-note watching, no billing screens.

## What stays behind in the account book

Uninstalling removes the plug-in, not its data. These remain:

- the `zSCP_*` and `zSCP2_*` tables, with every contract, machine, reading and billing record in them
- the trigger `TR_zSCP2_IV_NoDeleteEarlierBilledMonth` on `dbo.IV`
- the user-defined field `ServiceItemNo` on stock issue detail
- **any AutoCount invoice the module created** — those are ordinary invoices now and behave like any
  other

This is on purpose: if you re-install, everything is where you left it.

### Removing the trigger

The one thing that keeps acting after the plug-in is gone is the trigger — it will still refuse to
delete a meter invoice while a later month of the same contract is invoiced. To drop it:

```sql
DROP TRIGGER [dbo].[TR_zSCP2_IV_NoDeleteEarlierBilledMonth]
```

Re-installing the plug-in puts it back.

### Removing everything

Restore the SQL backup you took before installing. That is the only clean way — the tables reference
each other, and the invoices reference the readings.

## Going back to an earlier build

Uninstall, then install the older `.app`. AutoCount compares version numbers, so going **backwards**
needs the current one uninstalled first.

## Rebuilding this exact build

Source is at git tag **`v1.0.0-uat`** on `main`. The file you were given is:

```
ATP-ServiceContract-1.5.0.0-uat.app
SHA256  87117F48DB49D872BADCD0729FC897F2BDAA5A5189B9EB0085FD84D60E1930CB
```

Check it with `Get-FileHash <file> -Algorithm SHA256` before installing if it reached you by e-mail
or a shared drive.
