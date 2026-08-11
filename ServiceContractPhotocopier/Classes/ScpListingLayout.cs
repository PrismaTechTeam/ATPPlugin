using System;
using System.Collections.Generic;
using System.Data;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Turns a set of on-screen columns into a printable listing layout: A4 landscape, a title block,
    /// a repeating heading row and a bound detail row.
    ///
    /// Column widths are MEASURED against the real text — headings and actual data — not copied from
    /// the grid. A grid column is sized for a mouse and a scrollbar; scaling twenty-five of those onto
    /// one page gives columns narrower than the text they hold, which is how a heading ends up reading
    /// "Previo" and a figure "1,71".
    ///
    /// Separate from the form that calls it so a layout can be built, saved and rendered without a UI.
    /// </summary>
    public static class ScpListingLayout
    {
        /// <summary>One column to lay out: what to bind, what to call it, and how to format it.</summary>
        public class Column
        {
            public string Field = "";
            public string Caption = "";
            public int Width = 100;         // the on-screen width — a hint only, not the printed width
            public string Format = "";      // "n0", "n2", "dd/MM/yyyy"; empty = plain text
            public bool RightAlign = false;
        }

        private const float MAX_PT = 8f;       // preferred size when the columns leave room for it
        private const float MIN_PT = 5.5f;     // smaller than this stops being a document
        private const float PADDING = 9f;      // cell padding (4) plus slack, so nothing sits on a border

        /// <summary>
        /// Build the whole layout onto a fresh report. <paramref name="sample"/> is the data the
        /// widths are measured against — pass the rows that will actually be printed.
        /// </summary>
        public static void Apply(XtraReport xr, string title, List<Column> columns, DataTable sample)
        {
            if (xr == null) return;

            // AutoCount seeds every new report with a script that touches AutoCount.Report.BaseReport,
            // which drags AutoCount.UI.dll in as a script reference — the one reference most likely not
            // to resolve, and when it does not the whole preview is replaced by "There are errors in
            // scripts". A listing has no use for __report, so the handler stays (Scripts.OnBeforePrint
            // names it by string) and the dependency goes.
            xr.ScriptsSource =
                "private void Report_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)\r\n" +
                "{\r\n" +
                "}\r\n";
            xr.Scripts.OnBeforePrint = "Report_BeforePrint";

            xr.Landscape = true;
            xr.PaperKind = System.Drawing.Printing.PaperKind.A4;
            xr.Margins = new System.Drawing.Printing.Margins(40, 40, 45, 45);

            TopMarginBand top = new TopMarginBand();
            top.HeightF = 45f;
            xr.Bands.Add(top);

            ReportHeaderBand rh = new ReportHeaderBand();
            rh.HeightF = 40f;
            xr.Bands.Add(rh);

            PageHeaderBand ph = new PageHeaderBand();
            ph.HeightF = 28f;
            xr.Bands.Add(ph);

            DetailBand detail = new DetailBand();
            detail.HeightF = 16f;
            xr.Bands.Add(detail);

            BottomMarginBand bottom = new BottomMarginBand();
            bottom.HeightF = 45f;
            xr.Bands.Add(bottom);

            float usable = UsableWidth(xr);

            XRLabel titleLbl = new XRLabel();
            titleLbl.Text = title ?? "";
            titleLbl.Font = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
            titleLbl.LocationF = new DevExpress.Utils.PointFloat(0f, 6f);
            titleLbl.SizeF = new System.Drawing.SizeF(usable, 26f);
            rh.Controls.Add(titleLbl);

            if (columns == null || columns.Count == 0) return;

            float pt;
            float[] w = FitColumns(columns, sample, usable, out pt);

            float headHeight = (float)Math.Ceiling(pt * 3.4f);   // two lines plus breathing room
            float rowHeight = (float)Math.Ceiling(pt * 2.1f);
            XRTable headRow = NewTable(usable, headHeight);
            XRTable dataRow = NewTable(usable, rowHeight);

            // BeginInit is not optional here. Assigning WidthF to a cell that is already live in a
            // table makes DevExpress resize its NEIGHBOURS to preserve the row width, so each column
            // undoes the one before it and the widths that come out bear no relation to the ones
            // asked for — which is exactly how twenty-five columns end up piled on top of each other.
            // Inside BeginInit/EndInit the widths are taken as given.
            headRow.BeginInit();
            dataRow.BeginInit();

            XRTableRow hr = new XRTableRow();
            hr.HeightF = headHeight;
            headRow.Rows.Add(hr);

            XRTableRow dr = new XRTableRow();
            dr.HeightF = rowHeight;
            dataRow.Rows.Add(dr);

            for (int i = 0; i < columns.Count; i++)
            {
                Column c = columns[i];

                XRTableCell hc = new XRTableCell();
                hc.Text = c.Caption;
                hc.WidthF = w[i];
                hc.Font = new System.Drawing.Font("Segoe UI", pt, System.Drawing.FontStyle.Bold);
                hc.WordWrap = true;
                hc.Multiline = true;
                hc.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100f);
                hc.Borders = DevExpress.XtraPrinting.BorderSide.Bottom | DevExpress.XtraPrinting.BorderSide.Top;
                hc.TextAlignment = c.RightAlign
                    ? DevExpress.XtraPrinting.TextAlignment.BottomRight
                    : DevExpress.XtraPrinting.TextAlignment.BottomLeft;
                hr.Cells.Add(hc);

                XRTableCell dc = new XRTableCell();
                dc.WidthF = w[i];
                dc.Font = new System.Drawing.Font("Segoe UI", pt);
                dc.WordWrap = false;
                dc.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100f);
                dc.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                dc.TextAlignment = c.RightAlign
                    ? DevExpress.XtraPrinting.TextAlignment.MiddleRight
                    : DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
                if (c.Format.Length > 0) dc.TextFormatString = "{0:" + c.Format + "}";
                dc.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[" + c.Field + "]"));
                dr.Cells.Add(dc);
            }

            headRow.EndInit();
            dataRow.EndInit();

            ph.Controls.Add(headRow);
            detail.Controls.Add(dataRow);
        }

        /// <summary>
        /// Choose the largest readable font at which every column can hold its own text, and the
        /// widths that go with it.
        ///
        /// The lever is the FONT, not the widths. Squeezing columns to fit the page is what turns
        /// 0054447 into 005444 and wraps "Previous" as "Previou / s" — a report that fits the paper
        /// and lies about the data. Stepping the type down instead keeps every value whole.
        /// </summary>
        private static float[] FitColumns(List<Column> columns, DataTable data, float usable, out float pt)
        {
            float[] need = null;
            for (pt = MAX_PT; pt >= MIN_PT; pt -= 0.5f)
            {
                need = Measure(columns, data, pt);
                if (Sum(need) <= usable) return Spread(need, columns, usable);
            }

            // Even the smallest type will not hold this many columns. Squeeze — and accept that some
            // text will be clipped — rather than silently spilling onto a second sheet nobody wants.
            pt = MIN_PT;
            return Squeeze(need, usable, MinWidth(pt));
        }

        /// <summary>What each column needs at this size: the widest real value, and the heading —
        /// which wraps, so its floor is its longest single word.</summary>
        private static float[] Measure(List<Column> columns, DataTable data, float pt)
        {
            int n = columns.Count;
            float[] need = new float[n];
            float min = MinWidth(pt);

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(1, 1))
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            using (System.Drawing.Font fData = new System.Drawing.Font("Segoe UI", pt))
            using (System.Drawing.Font fHead = new System.Drawing.Font("Segoe UI", pt, System.Drawing.FontStyle.Bold))
            {
                float toUnits = 100f / g.DpiX;   // MeasureString gives pixels; report units are 1/100"
                for (int i = 0; i < n; i++)
                {
                    Column c = columns[i];
                    float longestWord = g.MeasureString(LongestWord(c.Caption), fHead).Width * toUnits;
                    float wholeCaption = g.MeasureString(c.Caption, fHead).Width * toUnits;
                    // A two-line heading needs about half its single-line width — but never less than
                    // its longest word, which cannot be broken.
                    float headWidth = Math.Max(longestWord, wholeCaption / 2f);

                    float valWidth = 0f;
                    if (data != null && data.Columns.Contains(c.Field))
                    {
                        int rows = Math.Min(data.Rows.Count, 300);
                        for (int r = 0; r < rows; r++)
                        {
                            object v = data.Rows[r][c.Field];
                            if (v == null || v == DBNull.Value) continue;
                            string s = c.Format.Length > 0
                                ? string.Format("{0:" + c.Format + "}", v)
                                : Convert.ToString(v);
                            if (s.Length == 0) continue;
                            float ww = g.MeasureString(s, fData).Width * toUnits;
                            if (ww > valWidth) valWidth = ww;
                        }
                    }
                    // GDI+ measures a shade narrower than the report engine draws; the margin is what
                    // stands between a value and losing its last character.
                    need[i] = Math.Max(min, Math.Max(headWidth, valWidth) * 1.06f + PADDING);
                }
            }
            return need;
        }

        /// <summary>It fits: hand the slack to the text columns, which read badly when tight, rather
        /// than padding every numeric column with white space.</summary>
        private static float[] Spread(float[] need, List<Column> columns, float usable)
        {
            float spare = usable - Sum(need);
            if (spare > 0f)
            {
                float textTotal = 0f;
                for (int i = 0; i < need.Length; i++) if (!columns[i].RightAlign) textTotal += need[i];
                if (textTotal > 0f)
                    for (int i = 0; i < need.Length; i++)
                        if (!columns[i].RightAlign) need[i] += spare * (need[i] / textTotal);
            }
            return Fit(need, usable);
        }

        /// <summary>Last resort: scale down, lift anything under the legible floor, and recover those
        /// units from the columns that still have room above it.</summary>
        private static float[] Squeeze(float[] need, float usable, float min)
        {
            float scale = usable / Sum(need);
            float deficit = 0f, giveable = 0f;
            for (int i = 0; i < need.Length; i++)
            {
                need[i] *= scale;
                if (need[i] < min) { deficit += min - need[i]; need[i] = min; }
                else giveable += need[i] - min;
            }
            if (deficit > 0f && giveable > 0f)
                for (int i = 0; i < need.Length; i++)
                    if (need[i] > min) need[i] -= deficit * ((need[i] - min) / giveable);
            return Fit(need, usable);
        }

        /// <summary>Roughly four characters at this size — narrower than that is not a column.</summary>
        private static float MinWidth(float pt) { return pt * 4f; }

        private static float Sum(float[] v)
        {
            float t = 0f;
            for (int i = 0; i < v.Length; i++) t += v[i];
            return t;
        }

        /// <summary>Round-off across twenty-five columns is enough to push the table past the page edge
        /// and spill a sliver of one column onto a second sheet. Settle it on the widest column.</summary>
        private static float[] Fit(float[] w, float usable)
        {
            float total = 0f;
            int widest = 0;
            for (int i = 0; i < w.Length; i++)
            {
                total += w[i];
                if (w[i] > w[widest]) widest = i;
            }
            w[widest] += usable - total;
            return w;
        }

        private static string LongestWord(string caption)
        {
            string s = caption ?? "";
            string[] parts = s.Split(' ');
            string longest = "";
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > longest.Length) longest = parts[i];
            return longest.Length == 0 ? s : longest;
        }

        private static XRTable NewTable(float width, float height)
        {
            XRTable t = new XRTable();
            t.LocationF = new DevExpress.Utils.PointFloat(0f, 0f);
            t.SizeF = new System.Drawing.SizeF(width, height);
            return t;
        }

        /// <summary>Printable width in report units. PageWidth/PageHeight describe the PAPER, so the
        /// landscape swap has to be applied here rather than assumed.</summary>
        public static float UsableWidth(XtraReport xr)
        {
            int w = xr.PageWidth, h = xr.PageHeight;
            int across = xr.Landscape ? Math.Max(w, h) : Math.Min(w, h);
            float usable = across - xr.Margins.Left - xr.Margins.Right;
            return usable < 100f ? 100f : usable;
        }
    }
}
