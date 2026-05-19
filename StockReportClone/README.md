# Stock Report Clone

Clone of `https://atgroup.asia/stockReport/` with all 3,821 entries captured at the time of scrape.

## Run

```
python serve.py
```

Opens `http://localhost:8000/` automatically.

## Files

- `index.html` — page layout (header, nav, filter bar, table)
- `app.js` — fetches `data.json`, renders DataTable, wires filters
- `data.json` — `{ headers, total, rows: [{ id, cells: [...] }] }` — 3,821 rows × 31 columns
- `serve.py` — minimal Python http.server (no install, stdlib only)

## Each row has a "Push" button

The first column of every row renders a green `Push` button. **No click handler is attached** —
the placeholder in `app.js` (`'.btn-push' click`) is empty by design, ready for you to wire up later.

## Filters

- Year (cosmetic only — data is static)
- Technician (filtered against the `Description` column)
- Item Code (column filter)
- Serial No (column filter)
- Date range (From / To)
- Global Search (DataTables built-in)
- Reset All

## Notes

Data was extracted from the live DataTables instance via Chrome DevTools — column count
is 31, matching the source page. The original `Push` flow (whatever it should do) is the
next thing to design.
