#!/usr/bin/env python
"""Diff what the engine produced against what Master Accounting issued in July 2026.

Two inputs:

  tests/golden/2607/*.json   the answer key, read out of the customer's own PDFs by extract.py
  --actual <dir>             the same shape, dumped from a test book after running Generate

The point is not to check the engine against itself. It is to be able to say to the customer, per
customer and to the sen, that the new invoices are the invoices they already send -- or exactly
where they are not.

Levels, so a failure says WHICH kind of wrong:

  L0  invoice count, and which machines landed on which invoice
  L1  line count and order
  L2  quantity, unit price, amount     (exact -- money is not approximately right)
  L3  net total
  L4  the reading rows: current, previous, usage, FOC, rebate

Run:  py tests/golden/compare.py --actual out/2607
      py tests/golden/compare.py --actual out/2607 --only "HOSPITAL PASIR GUDANG"
"""

import argparse
import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
GOLDEN_DIR = os.path.join(HERE, "2607")

# Money is compared exactly. The only tolerated difference is the one the panel documented and the
# customer must sign off: a merged line rounds once, so it can sit a sen or two away from the sum of
# its machines (HSI prints 3,048.36 where the per-machine charges add to 3,048.37). Anything wider
# than this is a real difference, not rounding.
TOLERANCE = 0.05


def load(path):
    with io.open(path, encoding="utf-8") as fh:
        return json.load(fh)


def close(a, b, tol=0.0):
    if a is None and b is None:
        return True
    if a is None or b is None:
        return False
    return abs(float(a) - float(b)) <= tol


def fmt(v):
    return "-" if v is None else ("%g" % v if isinstance(v, float) else str(v))


def diff_invoice(want, got, out):
    """L1-L4 for one invoice."""
    tag = want.get("docNo") or "?"
    wl, gl = want.get("lines", []), got.get("lines", [])
    if len(wl) != len(gl):
        out.append(("L1", tag, "line count %d, expected %d" % (len(gl), len(wl))))
        return
    for i, (w, g) in enumerate(zip(wl, gl), 1):
        where = "%s line %d" % (tag, i)
        for field, tol in (("qty", 0.0), ("unitPrice", 0.0), ("amount", TOLERANCE)):
            if not close(w.get(field), g.get(field), tol):
                out.append(("L2", where, "%s %s, expected %s"
                            % (field, fmt(g.get(field)), fmt(w.get(field)))))
        for field in ("current", "previous", "usage", "focQty", "rebateQty"):
            if w.get(field) is None and g.get(field) is None:
                continue
            if not close(w.get(field), g.get(field)):
                out.append(("L4", where, "%s %s, expected %s"
                            % (field, fmt(g.get(field)), fmt(w.get(field)))))
    if not close(want.get("netTotal"), got.get("netTotal"), TOLERANCE):
        out.append(("L3", tag, "net total %s, expected %s"
                    % (fmt(got.get("netTotal")), fmt(want.get("netTotal")))))


def diff_customer(name, want, got):
    out = []
    wi = {i["docNo"]: i for i in want.get("invoices", [])}
    gi = {i["docNo"]: i for i in got.get("invoices", [])}
    if len(wi) != len(gi):
        out.append(("L0", name, "%d invoice(s), expected %d  [%s vs %s]"
                    % (len(gi), len(wi), ", ".join(sorted(gi)) or "-", ", ".join(sorted(wi)))))
    # Match on document number where possible; otherwise position, so a renumbered run still
    # compares its lines instead of reporting nothing but the count.
    if set(wi) == set(gi):
        pairs = [(wi[k], gi[k]) for k in sorted(wi)]
    else:
        pairs = list(zip(want.get("invoices", []), got.get("invoices", [])))
    for w, g in pairs:
        diff_invoice(w, g, out)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--actual", required=True, help="directory of engine-produced JSON")
    ap.add_argument("--only", default=None, help="substring: compare just this customer")
    args = ap.parse_args()

    golden = sorted(f for f in os.listdir(GOLDEN_DIR) if f.endswith(".json"))
    if args.only:
        golden = [f for f in golden if args.only.lower() in f.lower()]
    if not golden:
        print("no answer key matched", file=sys.stderr)
        return 2

    total_fail = 0
    missing = []
    for f in golden:
        name = os.path.splitext(f)[0]
        actual = os.path.join(args.actual, f)
        if not os.path.isfile(actual):
            missing.append(name)
            continue
        problems = diff_customer(name, load(os.path.join(GOLDEN_DIR, f)), load(actual))
        if problems:
            total_fail += len(problems)
            print("\n%s  --  %d difference(s)" % (name, len(problems)))
            for level, where, what in problems:
                print("  %-3s %-28s %s" % (level, where, what))
        else:
            print("ok   %s" % name)

    if missing:
        print("\n%d customer(s) not generated yet:" % len(missing))
        for m in missing:
            print("  " + m)
    print("\n%d difference(s) across %d compared customer(s)"
          % (total_fail, len(golden) - len(missing)))
    return 1 if (total_fail or missing) else 0


if __name__ == "__main__":
    sys.exit(main())
