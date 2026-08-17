#!/usr/bin/env python
"""Turn the July-2026 invoice PDFs into the answer key the billing engine is graded against.

The PDFs in ``Type Of BillingFormat`` are what Master Accounting actually issued and what the
hospitals actually paid.  Nothing else is authoritative -- in particular page 1 of each PDF is
Jean's worksheet, which does NOT always agree with the invoice beside it (Kensington's sheet
totals 1,764 where its invoice says 2,236.56), so only the invoice pages are read here.

Output: one JSON per customer under ``tests/golden/2607/``:

    { "source": "HOSPITAL PASIR GUDANG.pdf",
      "invoices": [ { "docNo": "MR2607.1106", "date": "31/7/2026", "debtor": "3000-A0200",
                      "ref": "PUMS260724-071", "netTotal": 4273.75, "totalQty": 118802,
                      "lines": [ { "no": 1, "itemCode": "01.MR.BK. HOSPITAL PG",
                                   "description": "...", "qty": 60249.0, "uom": "PCS",
                                   "unitPrice": 0.019, "amount": 1144.73,
                                   "current": 594908.0, "previous": 534659.0,
                                   "focQty": None, "rebateQty": None, "rebatePct": None,
                                   "usage": 60249.0 } ] } ] }

Run:  py tests/golden/extract.py
"""

import io
import json
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
PDF_DIR = os.path.join(REPO, "Type Of BillingFormat")
OUT_DIR = os.path.join(HERE, "2607")

NUM = r"[-+]?[\d,]*\.?\d+"


def money(s):
    if s is None:
        return None
    s = s.replace(",", "").strip()
    if not s:
        return None
    try:
        return float(s)
    except ValueError:
        return None


def pdf_text(path):
    """-table, not -layout.

    Two invoice templates are in use and -layout mangles both. On ATP's it interleaves the Item Code
    and Description columns, which physically overlap ("01.MR.BK. HOSPIT0A1.LMPRG.BK.HOSPITAL...").
    On ASN's it scatters one charge line across three rows -- amount beside the line number,
    quantity and price two rows below. -table reconstructs the grid and puts each charge line back
    on a single row in both.
    """
    out = subprocess.check_output(["pdftotext", "-table", path, "-"])
    return out.decode("utf-8", "replace")


def split_invoices(text):
    """One chunk per invoice. Page 1 of every PDF is Jean's worksheet and has no document number,
    so it drops out on its own."""
    pages = text.split("\f")
    chunks = []
    for page in pages:
        # The word sits in a box beside the company name rather than on its own line, so it cannot
        # be anchored; a document number is the reliable signal that this is an invoice page.
        if "Invoice" not in page:
            continue
        m = re.search(r"No\.\s*:?\s*([A-Z]{2,5}\d{4}\.\d+)", page)
        if not m:
            continue
        docno = m.group(1)
        # Continuation pages ("Page 2 of 4") repeat the same number and belong to the invoice above.
        if chunks and chunks[-1][0] == docno:
            chunks[-1][1].append(page)
        else:
            chunks.append((docno, [page]))
    return [(d, "\n".join(p)) for d, p in chunks]


# The Item Code and Description columns physically OVERLAP in these PDFs, so -layout interleaves
# them into unusable text ("01.MR.BK. HOSPIT0A1.LMPRG.BK.HOSPITAL PASIR GUDANG"). No item code is
# extracted for that reason -- and it would be the wrong thing to compare anyway, since replacing
# those codes with canonical ones is the point of the exercise. The money is what has to match.
#
# The unit of measure is the one dependable landmark on a charge row: everything left of it is the
# line number, the mangled code/description and the quantity; everything right of it is the unit
# price and the amount.
# A charge row after -table:
#     1.  01.MR.BK. HOSPIT0A1.LM...GUDANG    60249   PCS    0.019    1,144.73
#     1.  RA-4WE04767_E  KASTAM TG - KPSM    1       MTH    900.00   900.00
#     2.  124-COLOR C+ P COLOR COPY + PRINT          PCS    0.30
#     3.  03.RA-3 UNIT HOSPITAL PASIR...     1       UNIT            FOC
#
# The unit of measure is the landmark. No item code is extracted: on ATP's template the Item Code
# and Description columns overlap in the PDF itself and cannot be separated, and comparing codes
# would be beside the point anyway -- replacing them with canonical ones is the whole exercise.
# The money is what has to match.
LINE_START = re.compile(r"^\s*(\d+)\.\s")
CHARGE_RE = re.compile(
    r"^\s*(\d+)\.\s+(.*?)\s{2,}(?:(" + NUM + r")\s+)?(PCS|MTH|UNIT|NOS|SET|EA)\b\s*(.*?)\s*$")
END_RE = re.compile(r"Net Total|RINGGIT MALAYSIA|Total Quantity")


def parse_lines(chunk):
    raw = chunk.split("\n")
    end = len(raw)
    for i, r in enumerate(raw):
        if END_RE.search(r):
            end = i
            break

    lines = []
    for i in range(end):
        m = CHARGE_RE.match(raw[i])
        if not m:
            continue
        no, desc, qty, uom, tail = m.groups()
        nums = [money(n) for n in re.findall(NUM, tail)]
        foc = "FOC" in tail
        price = amount = None
        if len(nums) >= 2:
            price, amount = nums[0], nums[-1]
        elif len(nums) == 1:
            # One figure and no quantity is a price on a line that billed nothing -- Rompin's
            # colour line, where the meter read backwards. With a quantity it is the amount.
            if qty is None:
                price = nums[0]
            else:
                amount = nums[0]
        if foc:
            amount = 0.0

        # The reading rows sit under the charge row, up to the next charge row.
        block = []
        for j in range(i + 1, end):
            if CHARGE_RE.match(raw[j]):
                break
            block.append(raw[j])
        text = "\n".join(block)

        lines.append({
            "no": int(no),
            "description": re.sub(r"\s{2,}", " ", desc).strip(),
            "qty": qty if qty is None else money(qty),
            "uom": uom,
            "unitPrice": price,
            "amount": amount,
            "foc": foc,
            "current": grab(text, r"Current Meter Reading[^:]*:\s*(" + NUM + ")"),
            "previous": grab(text, r"Previous Meter Reading[^:]*:\s*(" + NUM + ")"),
            "usage": grab(text, r"Meter Charges Usage\s*:\s*(" + NUM + ")"),
            "focQty": grab(text, r"Meter FOC Qty\s*:\s*(" + NUM + ")"),
            "rebateQty": grab(text, r"Meter Rebate Qty[^:]*:\s*(" + NUM + ")"),
            "rebatePct": grab(text, r"Meter Rebate Qty\s*\((" + NUM + r")%\)"),
        })
    return lines


def grab(text, pattern):
    m = re.search(pattern, text)
    return money(m.group(1)) if m else None


def parse_invoice(docno, chunk):
    # -table pads inside labels ("Net Total   (MYR) :", "Total  Quantity  :"), so every label here
    # tolerates arbitrary internal whitespace.
    return {
        "docNo": docno,
        "date": grab_str(chunk, r"Date\s*:\s*(\S+)"),
        "debtor": grab_str(chunk, r"Debtor\s+Code\s*:\s*(\S+)"),
        # Cut at a run of spaces: -table keeps the right-hand header column (Terms, Page No.) on the
        # same row, so reading to end-of-line would swallow it.
        "ref": grab_str(chunk, r"Ref\s*No\./\s*PO\s*No\.\s*:\s*(\S(?:[^\s]|\s(?!\s))*)"),
        "branch": grab_str(chunk, r"Branch\s*:\s*(\S(?:[^\s]|\s(?!\s))*)"),
        "netTotal": grab(chunk, r"Net\s+Total\s*\(MYR\)\s*:\s*(" + NUM + ")"),
        "totalQty": grab(chunk, r"Total\s+Quantity\s*:\s*(" + NUM + ")"),
        "tax": grab(chunk, r"(?:SST|GST)\s*@\s*\d+%\s*:\s*(" + NUM + ")"),
        "einvoiceUuid": grab_str(chunk, r"EINV\s+UUID\s*:\s*(\S+)"),
        "lines": parse_lines(chunk),
    }


def grab_str(text, pattern):
    m = re.search(pattern, text, re.M)
    if not m:
        return None
    v = m.group(1).strip()
    # An empty field lets the capture run into the next header column, whose text is a label.
    # "Page No. :" is not a purchase order number.
    if not v or v.endswith(":"):
        return None
    return v


def check(inv):
    """Flag an invoice whose own lines do not add up to its printed total -- that means the
    parser mis-read it, and a wrong answer key is worse than none."""
    if inv["netTotal"] is None:
        return "no net total"
    total = sum(l["amount"] or 0.0 for l in inv["lines"])
    tax = inv.get("tax") or 0.0
    if abs(total + tax - inv["netTotal"]) > 0.02:
        return "lines %.2f + tax %.2f != net %.2f" % (total, tax, inv["netTotal"])
    return None


def main():
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    pdfs = sorted(f for f in os.listdir(PDF_DIR) if f.lower().endswith(".pdf"))
    total_inv = total_lines = 0
    problems = []
    worksheet_only = []
    for name in pdfs:
        text = pdf_text(os.path.join(PDF_DIR, name))
        invoices = [parse_invoice(d, c) for d, c in split_invoices(text)]
        if not invoices:
            # Some of these files are Jean's worksheet with no invoice attached (the "METER" ones),
            # and MBJB is issued on a bespoke three-page template with no document-number field in
            # the usual place. Neither is a parsing failure; both are stated instead of hidden.
            worksheet_only.append(name)
            continue
        for inv in invoices:
            bad = check(inv)
            if bad:
                problems.append("%s %s: %s" % (name, inv["docNo"], bad))
        total_inv += len(invoices)
        total_lines += sum(len(i["lines"]) for i in invoices)
        out = os.path.join(OUT_DIR, os.path.splitext(name)[0] + ".json")
        with io.open(out, "w", encoding="utf-8") as fh:
            fh.write(json.dumps({"source": name, "invoices": invoices},
                                indent=2, ensure_ascii=False, sort_keys=True))
        print("%-70s %2d invoice(s) %3d line(s)" % (name, len(invoices), total_lines))

    print("\n%d PDFs -> %d invoices, %d charge lines" % (len(pdfs), total_inv, total_lines))
    if worksheet_only:
        print("\n%d file(s) carry no invoice page (worksheet only, or a bespoke template):"
              % len(worksheet_only))
        for w in worksheet_only:
            print("  " + w)
    if problems:
        print("\n%d PROBLEM(S) -- the answer key is only as good as this list is empty:" % len(problems))
        for p in problems:
            print("  " + p)
        return 1
    print("\nEvery extracted invoice's lines add up to its own printed total.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
