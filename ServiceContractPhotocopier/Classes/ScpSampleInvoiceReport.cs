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
        public string Title = "TAX INVOICE";
        public string DebtorCode = "";
        public string DebtorName = "";
        public string DebtorAddress = "";    // the four address lines, already joined by newline
        public string Attention = "";
        public string ContractNo = "";
        public string Note = "";             // "rental only", "meters only", the machine it belongs to
        public string DocNo = "";
        public DateTime DocDate = DateTime.Today;
        public string Terms = "";
        public string Agent = "";
        public readonly List<SampleInvoiceLine> Lines = new List<SampleInvoiceLine>();
    }

    /// <summary>The company block an invoice is printed under — read from the book's own profile so
    /// the sample carries the same letterhead the real document does.</summary>
    public class SampleInvoiceCompany
    {
        public string Name = "";
        public string RegisterNo = "";
        public string Address = "";          // the four lines plus postcode, joined by newline
        public string Phone = "";
        public string Fax = "";
        public string Email = "";

        /// <summary>Reads dbo.Profile. Never throws — a book with a bare profile simply prints a
        /// thinner letterhead, which is what it would print for real.</summary>
        public static SampleInvoiceCompany Load(AutoCount.Data.DBSetting db)
        {
            SampleInvoiceCompany c = new SampleInvoiceCompany();
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT TOP 1 ISNULL(CompanyName,'') AS CompanyName, ISNULL(RegisterNo,'') AS RegisterNo, " +
                    "ISNULL(Address1,'') AS A1, ISNULL(Address2,'') AS A2, ISNULL(Address3,'') AS A3, " +
                    "ISNULL(Address4,'') AS A4, ISNULL(PostCode,'') AS PostCode, " +
                    "ISNULL(Phone1,'') AS Phone1, ISNULL(Fax1,'') AS Fax1, ISNULL(EmailAddress,'') AS Email " +
                    "FROM dbo.Profile", false);
                if (t.Rows.Count == 0) return c;
                DataRow r = t.Rows[0];
                c.Name = Convert.ToString(r["CompanyName"]).Trim();
                c.RegisterNo = Convert.ToString(r["RegisterNo"]).Trim();
                List<string> lines = new List<string>();
                foreach (string col in new string[] { "A1", "A2", "A3", "A4" })
                {
                    string v = Convert.ToString(r[col]).Trim();
                    if (v.Length > 0) lines.Add(v);
                }
                string pc = Convert.ToString(r["PostCode"]).Trim();
                if (pc.Length > 0) lines.Add(pc);
                c.Address = string.Join("\r\n", lines.ToArray());
                c.Phone = Convert.ToString(r["Phone1"]).Trim();
                c.Fax = Convert.ToString(r["Fax1"]).Trim();
                c.Email = Convert.ToString(r["Email"]).Trim();
            }
            catch { }
            return c;
        }
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
        private const float W_NO = 26f;
        private const float W_DESC = 310f;
        private const float W_QTY = 80f;
        private const float W_PRICE = 90f;
        private const float W_AMOUNT = 100f;

        /// <summary>Builds the report over the given invoices. Show it with ShowPreview(), or hand
        /// its PrintingSystem to a PrintControl.</summary>
        public static ScpSampleInvoiceReport Create(List<SampleInvoiceDoc> invoices, string footer,
            SampleInvoiceCompany company)
        {
            ScpSampleInvoiceReport r = new ScpSampleInvoiceReport();
            r._co = company ?? new SampleInvoiceCompany();
            r.Build(invoices ?? new List<SampleInvoiceDoc>(), footer ?? "");
            return r;
        }

        private SampleInvoiceCompany _co = new SampleInvoiceCompany();

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
            head.HeightF = 196f;
            // Every invoice starts a new sheet, the way they are issued and read.
            head.PageBreak = PageBreak.BeforeBand;
            Bands.Add(head);

            // ---------- letterhead ----------
            head.Controls.Add(Bound("[CoName]", 0, 0, w - 190, 20, 13f, FontStyle.Bold, Color.Black, TextAlignment.TopLeft));
            XRLabel coReg = Bound("[CoReg]", 0, 20, w - 190, 12, 7.5f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft);
            coReg.CanShrink = true;
            head.Controls.Add(coReg);
            XRLabel coAddr = Bound("[CoAddr]", 0, 32, w - 190, 34, 7.5f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft);
            coAddr.Multiline = true; coAddr.CanShrink = true;
            head.Controls.Add(coAddr);
            XRLabel coTel = Bound("[CoTel]", 0, 66, w - 190, 12, 7.5f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft);
            coTel.CanShrink = true;
            head.Controls.Add(coTel);

            // The document title, where every AutoCount invoice carries it.
            head.Controls.Add(Bound("[Title]", w - 190, 0, 190, 22, 14f, FontStyle.Bold, Color.Black, TextAlignment.TopRight));
            head.Controls.Add(Label("SAMPLE — nothing is posted", w - 190, 22, 190, 12,
                7f, FontStyle.Regular, Color.Firebrick, TextAlignment.TopRight));

            // ---------- bill to ----------
            head.Controls.Add(Label("To:", 0, 88, 28, 12, 8f, FontStyle.Bold, Color.Black, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[Customer]", 28, 88, 300, 13, 9f, FontStyle.Bold, Color.Black, TextAlignment.TopLeft));
            XRLabel custAddr = Bound("[CustAddr]", 28, 101, 300, 40, 8f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft);
            custAddr.Multiline = true; custAddr.CanShrink = true;
            head.Controls.Add(custAddr);
            XRLabel attn = Bound("[Attn]", 28, 141, 300, 12, 8f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft);
            attn.CanShrink = true;
            head.Controls.Add(attn);

            // ---------- the document box, right, the way AutoCount lays it out ----------
            float bx = w - 250, bw = 250, by = 86;
            head.Controls.Add(Boxed(bx, by, bw, 62));
            DocField(head, bx, by + 6, bw, "Invoice No", "[DocNo]");
            DocField(head, bx, by + 24, bw, "Date", "[DocDateText]");
            DocField(head, bx, by + 41, bw, "Terms", "[Terms]");
            DocField(head, bx, by + 69, bw, "Contract", "[ContractNo]");

            head.Controls.Add(Label("This invoice", 0, 160, 80, 12, 7.5f, FontStyle.Bold, Color.Gray, TextAlignment.TopLeft));
            head.Controls.Add(Bound("[InvNote]", 80, 160, w - 80, 12, 7.5f, FontStyle.Regular, Color.Gray, TextAlignment.TopLeft));

            // ---------- column heads ----------
            float x = 0;
            head.Controls.Add(ColHead("NO", x, 178, W_NO, TextAlignment.BottomLeft)); x += W_NO;
            head.Controls.Add(ColHead("DESCRIPTION", x, 178, W_DESC, TextAlignment.BottomLeft)); x += W_DESC;
            head.Controls.Add(ColHead("QTY", x, 178, W_QTY, TextAlignment.BottomRight)); x += W_QTY;
            head.Controls.Add(ColHead("U/PRICE", x, 178, W_PRICE, TextAlignment.BottomRight)); x += W_PRICE;
            head.Controls.Add(ColHead("AMOUNT", x, 178, W_AMOUNT, TextAlignment.BottomRight));

            // ---- the lines ----
            DetailBand detail = new DetailBand();
            detail.HeightF = 44f;
            Bands.Add(detail);

            detail.Controls.Add(Bound("[LineNo]", 0, 2, W_NO, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            detail.Controls.Add(Bound("[Description]", W_NO, 2, W_DESC, 14, 9f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            x = W_NO + W_DESC;
            detail.Controls.Add(Money("[QtyText]", x, 2, W_QTY)); x += W_QTY;
            detail.Controls.Add(Money("[PriceText]", x, 2, W_PRICE)); x += W_PRICE;
            detail.Controls.Add(Money("[AmountText]", x, 2, W_AMOUNT));

            XRLabel sub = Bound("[Sub]", W_NO, 17, w - W_NO, 12, 8f, FontStyle.Regular,
                Color.FromArgb(70, 70, 70), TextAlignment.TopLeft);
            sub.CanShrink = true;
            detail.Controls.Add(sub);

            XRLabel note = Bound("[NoteLine]", W_NO, 30, w - W_NO, 12, 7.5f, FontStyle.Regular,
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
            foot.HeightF = 118f;
            Bands.Add(foot);

            float tx = W_NO + W_DESC + W_QTY;
            XRLabel totalLbl = Label("TOTAL", 0, 4, tx + W_PRICE - 8, 18,
                9f, FontStyle.Bold, Color.Black, TextAlignment.TopRight);
            totalLbl.Borders = BorderSide.Top;
            foot.Controls.Add(totalLbl);

            XRLabel totalVal = new XRLabel();
            totalVal.LocationF = new PointF(tx + W_PRICE, 4);
            totalVal.SizeF = new SizeF(W_AMOUNT, 18);
            totalVal.Font = new Font("Consolas", 9f, FontStyle.Bold);
            totalVal.TextAlignment = TextAlignment.TopRight;
            totalVal.Borders = BorderSide.Top | BorderSide.Bottom;
            // The total is worked out when the table is built and carried on every row, rather than
            // aggregated by the report. Two report-side ways of doing it were tried and both printed
            // a wrong number silently: an XRSummary is overridden by an expression binding on Text
            // (it printed the LAST line's amount and called it the total), and sumSum() in the
            // binding evaluated to nothing at all. A total that is quietly wrong is worse than no
            // total, so it is computed where it can be checked.
            totalVal.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[InvTotal]"));
            foot.Controls.Add(totalVal);

            // Ringgit in words, the way a Malaysian tax invoice carries it.
            XRLabel words = Bound("[AmountWords]", 0, 26, w, 13, 8f, FontStyle.Bold,
                Color.Black, TextAlignment.TopLeft);
            words.CanShrink = true;
            foot.Controls.Add(words);

            // Signature blocks.
            foot.Controls.Add(Label("This is a computer generated document. No signature is required.",
                0, 56, w, 12, 7f, FontStyle.Regular, Color.Gray, TextAlignment.TopLeft));
            XRLine sig1 = new XRLine();
            sig1.LocationF = new PointF(0, 92); sig1.WidthF = 190; sig1.HeightF = 2;
            sig1.ForeColor = Color.Black;
            foot.Controls.Add(sig1);
            foot.Controls.Add(Label("Authorised Signature", 0, 95, 190, 12,
                7.5f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));
            XRLine sig2 = new XRLine();
            sig2.LocationF = new PointF(w - 190, 92); sig2.WidthF = 190; sig2.HeightF = 2;
            sig2.ForeColor = Color.Black;
            foot.Controls.Add(sig2);
            foot.Controls.Add(Label("Received in good order and condition by", w - 190, 95, 190, 12,
                7.5f, FontStyle.Regular, Color.Black, TextAlignment.TopLeft));

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
        private DataTable Flatten(List<SampleInvoiceDoc> invoices)
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
            t.Columns.Add("AmountWords", typeof(string));
            t.Columns.Add("CustAddr", typeof(string));
            t.Columns.Add("Attn", typeof(string));
            t.Columns.Add("DocNo", typeof(string));
            t.Columns.Add("DocDateText", typeof(string));
            t.Columns.Add("Terms", typeof(string));
            t.Columns.Add("CoName", typeof(string));
            t.Columns.Add("CoReg", typeof(string));
            t.Columns.Add("CoAddr", typeof(string));
            t.Columns.Add("CoTel", typeof(string));

            for (int i = 0; i < invoices.Count; i++)
            {
                SampleInvoiceDoc d = invoices[i];
                string key = (i + 1) + "|" + d.Note;
                decimal total = 0m;
                foreach (SampleInvoiceLine tl in d.Lines) total += tl.Amount;
                string totalText = total.ToString("n2");
                string totalWords = InWords(total);
                int n = 0;
                foreach (SampleInvoiceLine l in d.Lines)
                {
                    n++;
                    DataRow r = t.NewRow();
                    Header(r, key, d, i, invoices.Count, totalWords);
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
                    Header(r, key, d, i, invoices.Count, totalWords);
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

        private void Header(DataRow r, string key, SampleInvoiceDoc d, int i, int of, string totalWords)
        {
            r["Invoice"] = key;
            r["Title"] = d.Title;
            r["InvNo"] = of > 1 ? "invoice " + (i + 1) + " of " + of : "";
            r["Customer"] = (d.DebtorCode + "   " + d.DebtorName).Trim();
            r["CustAddr"] = d.DebtorAddress ?? "";
            r["Attn"] = string.IsNullOrEmpty(d.Attention) ? "" : "Attn: " + d.Attention;
            r["ContractNo"] = d.ContractNo;
            r["InvNote"] = d.Note;
            r["DocNo"] = d.DocNo.Length > 0 ? d.DocNo : "(not issued)";
            r["DocDateText"] = d.DocDate.ToString("dd/MM/yyyy");
            r["Terms"] = d.Terms ?? "";
            r["AmountWords"] = totalWords;
            r["CoName"] = _co.Name;
            r["CoReg"] = _co.RegisterNo.Length > 0 ? "Co. Reg. No: " + _co.RegisterNo : "";
            r["CoAddr"] = _co.Address;
            List<string> tel = new List<string>();
            if (_co.Phone.Length > 0) tel.Add("Tel: " + _co.Phone);
            if (_co.Fax.Length > 0) tel.Add("Fax: " + _co.Fax);
            if (_co.Email.Length > 0) tel.Add(_co.Email);
            r["CoTel"] = string.Join("    ", tel.ToArray());
        }

        /// <summary>The total in words, the way a Malaysian tax invoice carries it.</summary>
        private static string InWords(decimal amount)
        {
            bool neg = amount < 0m;
            decimal v = Math.Abs(Math.Round(amount, 2, MidpointRounding.AwayFromZero));
            long ringgit = (long)Math.Floor(v);
            int sen = (int)Math.Round((v - ringgit) * 100m, 0, MidpointRounding.AwayFromZero);
            string t = "RINGGIT MALAYSIA " + (neg ? "MINUS " : "") + Words(ringgit).ToUpperInvariant();
            if (sen > 0) t += " AND " + Words(sen).ToUpperInvariant() + " SEN";
            return t + " ONLY";
        }

        private static readonly string[] ONES = {
            "zero","one","two","three","four","five","six","seven","eight","nine","ten","eleven",
            "twelve","thirteen","fourteen","fifteen","sixteen","seventeen","eighteen","nineteen" };
        private static readonly string[] TENS = {
            "","","twenty","thirty","forty","fifty","sixty","seventy","eighty","ninety" };

        private static string Words(long n)
        {
            if (n < 0) return "minus " + Words(-n);
            if (n < 20) return ONES[n];
            if (n < 100) return TENS[n / 10] + (n % 10 > 0 ? "-" + ONES[n % 10] : "");
            if (n < 1000) return ONES[n / 100] + " hundred" + (n % 100 > 0 ? " and " + Words(n % 100) : "");
            if (n < 1000000) return Words(n / 1000) + " thousand" + (n % 1000 > 0 ? " " + Words(n % 1000) : "");
            if (n < 1000000000) return Words(n / 1000000) + " million" + (n % 1000000 > 0 ? " " + Words(n % 1000000) : "");
            return Words(n / 1000000000) + " billion" + (n % 1000000000 > 0 ? " " + Words(n % 1000000000) : "");
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

        /// <summary>The ruled box the document details sit in, top right, the way AutoCount frames
        /// invoice number / date / terms.</summary>
        private static XRLabel Boxed(float x, float y, float w, float h)
        {
            XRLabel l = new XRLabel();
            l.LocationF = new PointF(x, y);
            l.SizeF = new SizeF(w, h);
            l.Text = "";
            l.Borders = BorderSide.All;
            l.BorderColor = Color.FromArgb(120, 120, 120);
            return l;
        }

        /// <summary>One "label: value" row inside that box.</summary>
        private static void DocField(GroupHeaderBand band, float x, float y, float w,
            string label, string expression)
        {
            band.Controls.Add(Label(label, x + 6, y, 72, 15, 7.5f, FontStyle.Bold,
                Color.FromArgb(90, 90, 90), TextAlignment.TopLeft));
            band.Controls.Add(Bound(expression, x + 80, y, w - 86, 15, 8.5f, FontStyle.Regular,
                Color.Black, TextAlignment.TopLeft));
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
