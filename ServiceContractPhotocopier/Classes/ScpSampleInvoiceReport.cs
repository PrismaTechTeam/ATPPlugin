using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One printed line of a sample invoice.</summary>
    public class SampleInvoiceLine
    {
        public string Description = "";
        public string SubDescription = "";   // the MODEL: / S/N: / n UNIT block under the wording
        public string Covers = "";           // which machines this line speaks for
        public string Note = "";             // the strategy note a real invoice prints beneath the line
        public decimal Qty;
        public decimal UnitPrice;
        public decimal Amount;
        public bool ShowQty = true;          // a flat line prints no copies
    }

    /// <summary>One of the invoices a contract produces.</summary>
    public class SampleInvoiceDoc
    {
        public string Title = "";
        public string DebtorCode = "";
        public string DebtorName = "";
        public string ContractNo = "";
        public string Note = "";             // "rental only", "meters only", the machine it belongs to
        public readonly List<SampleInvoiceLine> Lines = new List<SampleInvoiceLine>();
    }

    /// <summary>
    /// The sample invoices as a real report — paper, ruled columns, a total, one sheet per invoice.
    ///
    /// <para>A grid of rows answers "how many lines". The question actually being asked when a
    /// contract is set up is "does this read like an invoice" — whether the wording, the units and
    /// the money on the page make sense to the person paying it. Those are different questions, and
    /// only a document answers the second. Being a report also brings zoom, page navigation, print
    /// and PDF export without any of them being written.</para>
    ///
    /// <para>It only LAYS OUT. Every number reaching it was computed by the billing engine.</para>
    /// </summary>
    public class ScpSampleInvoiceReport : XtraReport
    {
        private const float W_DESC = 330f;
        private const float W_QTY = 80f;
        private const float W_PRICE = 90f;
        private const float W_AMOUNT = 100f;

        /// <summary>Builds the report over the given invoices. Show it with ShowPreview(), or hand
        /// its PrintingSystem to a PrintControl.</summary>
        public static ScpSampleInvoiceReport Create(List<SampleInvoiceDoc> invoices, string footer)
        {
            ScpSampleInvoiceReport r = new ScpSampleInvoiceReport();
            r.Build(invoices ?? new List<SampleInvoiceDoc>(), footer ?? "");
            return r;
        }

        private void Build(List<SampleInvoiceDoc> invoices, string footer)
        {
            PaperKind = System.Drawing.Printing.PaperKind.A4;
            Landscape = false;
            Margins = new System.Drawing.Printing.Margins(50, 50, 50, 50);
            Font = new Font("Segoe UI", 9f);

            DataSource = Flatten(invoices);

            float w = PageWidth - Margins.Left - Margins.Right;

            // ---- one invoice per group, one group per sheet ----
            GroupHeaderBand head = new GroupHeaderBand();
            head.GroupFields.Add(new GroupField("Invoice"));
            head.Level = 0;
            head.RepeatEveryPage = true;
            head.HeightF = 118f;
            // Every invoice starts a new sheet, the way they are issued and read.
            head.PageBreak = PageBreak.BeforeBand;
            Bands.Add(head);

            head.Controls.Add(Label("SAMPLE — not a real invoice, and nothing is posted",
                0, 0, w, 13, 7.5f, FontStyle.Regular, Color.Firebrick, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[Title]", 0, 15, w - 170, 26, 15f, FontStyle.Bold, Color.Black, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[InvNo]", w - 170, 22, 170, 14, 7.5f, FontStyle.Regular, Color.Gray, TextAlignment.TopRight));

            head.Controls.Add(Label("Customer", 0, 48, 80, 14, 8f, FontStyle.Bold, Color.Gray, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[Customer]", 80, 48, w - 80, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            head.Controls.Add(Label("Contract", 0, 63, 80, 14, 8f, FontStyle.Bold, Color.Gray, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[ContractNo]", 80, 63, w - 80, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            head.Controls.Add(Label("This invoice", 0, 78, 80, 14, 8f, FontStyle.Bold, Color.Gray, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[InvNote]", 80, 78, w - 80, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));

            float x = 0;
            head.Controls.Add(ColHead("DESCRIPTION", x, 98, W_DESC, TextAlignment.BottomLeft)); x += W_DESC;
            head.Controls.Add(ColHead("QTY", x, 98, W_QTY, TextAlignment.BottomRight)); x += W_QTY;
            head.Controls.Add(ColHead("UNIT PRICE", x, 98, W_PRICE, TextAlignment.BottomRight)); x += W_PRICE;
            head.Controls.Add(ColHead("AMOUNT", x, 98, W_AMOUNT, TextAlignment.BottomRight));

            // ---- the lines ----
            DetailBand detail = new DetailBand();
            detail.HeightF = 44f;
            Bands.Add(detail);

            detail.Controls.Add(Bound("[LineNo] + '.  ' + [Description]",
                0, 2, W_DESC, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            x = W_DESC;
            detail.Controls.Add(Money("[QtyText]", x, 2, W_QTY)); x += W_QTY;
            detail.Controls.Add(Money("[PriceText]", x, 2, W_PRICE)); x += W_PRICE;
            detail.Controls.Add(Money("[AmountText]", x, 2, W_AMOUNT));

            XRLabel sub = Bound("[Sub]", 14, 17, w - 14, 12, 8f, FontStyle.Regular,
                Color.FromArgb(70, 70, 70), TextAlignment.TopLeft);
            sub.CanShrink = true;
            detail.Controls.Add(sub);

            XRLabel note = Bound("[NoteLine]", 14, 30, w - 14, 12, 7.5f, FontStyle.Regular,
                Color.FromArgb(120, 90, 20), TextAlignment.TopLeft);
            note.CanShrink = true;
            detail.Controls.Add(note);

            XRLine rule = new XRLine();
            rule.LocationF = new PointF(0, 42);
            rule.WidthF = w;
            rule.HeightF = 2;
            rule.ForeColor = Color.FromArgb(215, 215, 215);
            detail.Controls.Add(rule);

            // ---- the total ----
            GroupFooterBand foot = new GroupFooterBand();
            foot.Level = 0;
            foot.HeightF = 30f;
            Bands.Add(foot);

            XRLabel totalLbl = Label("TOTAL", 0, 4, W_DESC + W_QTY + W_PRICE, 18,
                9f, FontStyle.Bold, Color.Black, TextAlignment.TopRight);
            totalLbl.Borders = BorderSide.Top;
            foot.Controls.Add(totalLbl);

            XRLabel totalVal = new XRLabel();
            totalVal.LocationF = new PointF(W_DESC + W_QTY + W_PRICE, 4);
            totalVal.SizeF = new SizeF(W_AMOUNT, 18);
            totalVal.Font = new Font("Consolas", 9f, FontStyle.Bold);
            totalVal.TextAlignment = TextAlignment.TopRight;
            totalVal.Borders = BorderSide.Top;
            // The total is worked out when the table is built and carried on every row, rather than
            // aggregated by the report. Two report-side ways of doing it were tried and both printed
            // a wrong number silently: an XRSummary is overridden by an expression binding on Text
            // (it printed the LAST line's amount and called it the total), and sumSum() in the
            // binding evaluated to nothing at all. A total that is quietly wrong is worse than no
            // total, so it is computed where it can be checked.
            totalVal.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[InvTotal]"));
            foot.Controls.Add(totalVal);

            // ---- the standing note, once at the end ----
            if (footer.Length > 0)
            {
                ReportFooterBand rf = new ReportFooterBand();
                rf.HeightF = 60f;
                Bands.Add(rf);
                XRLabel f = Label(footer, 0, 10, w, 48, 7.5f, FontStyle.Regular, Color.Gray, TextAlignment.TopLeft);
                f.Borders = BorderSide.Top;
                f.Multiline = true;
                f.CanGrow = true;
                rf.Controls.Add(f);
            }

            PageFooterBand pf = new PageFooterBand();
            pf.HeightF = 20f;
            Bands.Add(pf);
            XRPageInfo pi = new XRPageInfo();
            pi.LocationF = new PointF(0, 2);
            pi.SizeF = new SizeF(w, 14);
            pi.TextAlignment = TextAlignment.TopRight;
            pi.Font = new Font("Segoe UI", 7.5f);
            pi.ForeColor = Color.Gray;
            pi.Format = "page {0} of {1}";
            pf.Controls.Add(pi);
        }

        /// <summary>The invoices as one flat table — the report groups it back apart. The three
        /// numeric columns are pre-formatted here so a flat line's QTY is genuinely empty rather than
        /// a zero the reader has to interpret.</summary>
        private static DataTable Flatten(List<SampleInvoiceDoc> invoices)
        {
            DataTable t = new DataTable("Lines");
            t.Columns.Add("Invoice", typeof(string));
            t.Columns.Add("Title", typeof(string));
            t.Columns.Add("InvNo", typeof(string));
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("InvNote", typeof(string));
            t.Columns.Add("LineNo", typeof(int));
            t.Columns.Add("Description", typeof(string));
            t.Columns.Add("Sub", typeof(string));
            t.Columns.Add("NoteLine", typeof(string));
            t.Columns.Add("QtyText", typeof(string));
            t.Columns.Add("PriceText", typeof(string));
            t.Columns.Add("AmountText", typeof(string));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("InvTotal", typeof(string));

            for (int i = 0; i < invoices.Count; i++)
            {
                SampleInvoiceDoc d = invoices[i];
                string key = (i + 1) + "|" + d.Note;
                decimal total = 0m;
                foreach (SampleInvoiceLine tl in d.Lines) total += tl.Amount;
                string totalText = total.ToString("n2");
                int n = 0;
                foreach (SampleInvoiceLine l in d.Lines)
                {
                    n++;
                    DataRow r = t.NewRow();
                    Header(r, key, d, i, invoices.Count);
                    r["LineNo"] = n;
                    r["Description"] = l.Description;
                    r["Sub"] = l.SubDescription + (l.Covers.Length > 0
                        ? (l.SubDescription.Length > 0 ? "        " : "") + "covers:  " + l.Covers : "");
                    r["NoteLine"] = l.Note;
                    r["QtyText"] = l.ShowQty ? l.Qty.ToString("n0") : "";
                    r["PriceText"] = l.ShowQty ? l.UnitPrice.ToString("n4") : l.UnitPrice.ToString("n2");
                    r["AmountText"] = l.Amount.ToString("n2");
                    r["Amount"] = l.Amount;
                    r["InvTotal"] = totalText;
                    t.Rows.Add(r);
                }
                if (d.Lines.Count == 0)
                {
                    DataRow r = t.NewRow();
                    Header(r, key, d, i, invoices.Count);
                    r["LineNo"] = 0;
                    r["Description"] = "(nothing to bill)";
                    r["Sub"] = ""; r["NoteLine"] = "";
                    r["QtyText"] = ""; r["PriceText"] = ""; r["AmountText"] = "0.00";
                    r["Amount"] = 0m;
                    r["InvTotal"] = "0.00";
                    t.Rows.Add(r);
                }
            }
            return t;
        }

        private static void Header(DataRow r, string key, SampleInvoiceDoc d, int i, int of)
        {
            r["Invoice"] = key;
            r["Title"] = d.Title;
            r["InvNo"] = of > 1 ? "invoice " + (i + 1) + " of " + of : "";
            r["Customer"] = (d.DebtorCode + "   " + d.DebtorName).Trim();
            r["ContractNo"] = d.ContractNo;
            r["InvNote"] = d.Note;
        }

        // ---- small builders, so the layout above reads as layout ----

        private static XRLabel Label(string text, float x, float y, float w, float h,
            float size, FontStyle style, Color colour, TextAlignment align)
        {
            XRLabel l = new XRLabel();
            l.Text = text;
            l.LocationF = new PointF(x, y);
            l.SizeF = new SizeF(w, h);
            l.Font = new Font("Segoe UI", size, style);
            l.ForeColor = colour;
            l.TextAlignment = align;
            return l;
        }

        private static XRLabel Bound(string expression, float x, float y, float w, float h,
            float size, FontStyle style, Color colour, TextAlignment align)
        {
            XRLabel l = Label("", x, y, w, h, size, style, colour, align);
            l.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));
            return l;
        }

        private static XRLabel ColHead(string text, float x, float y, float w, TextAlignment align)
        {
            XRLabel l = Label(text, x, y, w, 16, 8f, FontStyle.Bold, Color.Black, align);
            l.Borders = BorderSide.Bottom;
            return l;
        }

        private static XRLabel Money(string expression, float x, float y, float w)
        {
            XRLabel l = new XRLabel();
            l.LocationF = new PointF(x, y);
            l.SizeF = new SizeF(w, 14);
            l.Font = new Font("Consolas", 8.5f);
            l.ForeColor = Color.Black;
            l.TextAlignment = TextAlignment.TopRight;
            l.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));
            return l;
        }
    }
}
