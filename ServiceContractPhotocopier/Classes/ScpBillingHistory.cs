using System;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// "What billing was generated" — one implementation shared by the contract form's and the item
    /// form's Billing History tabs. Reads the zSCP2_MeterEntry invoice stamps joined to the native
    /// AutoCount invoice table (dbo.IV) grouped per invoice + billing period.
    /// </summary>
    public static class ScpBillingHistory
    {
        /// <summary>filterCol = "i.ContractKey" or "i.ItemKey".</summary>
        public static DataTable Load(DBSetting db, string filterCol, long key)
        {
            try
            {
                // #10: overrides whose CN was deleted/cancelled in AutoCount roll back before display.
                ScpCreditNoteBuilder.ReconcileDeletedCreditNotes(db);
                return db.GetDataTable(
                    "SELECT iv.DocKey, iv.DocNo AS [Invoice No], iv.DocDate AS [Invoice Date], " +
                    "RIGHT('0' + CAST(me.PeriodMonth AS VARCHAR(2)), 2) + '/' + CAST(me.PeriodYear AS VARCHAR(4)) AS [Period], " +
                    "ISNULL(iv.NetTotal, 0) AS [Amount], " +
                    "CASE WHEN ISNULL(iv.Cancelled,'F') = 'T' THEN 'YES' ELSE '' END AS [Cancelled], " +
                    // #10: live CNs correcting this invoice (matched by OurInvoiceNo), comma-joined.
                    "ISNULL(STUFF((SELECT ', ' + c.DocNo FROM dbo.CN c " +
                    "  WHERE c.OurInvoiceNo = iv.DocNo AND ISNULL(c.Cancelled,'F') <> 'T' " +
                    "  ORDER BY c.DocDate, c.DocNo FOR XML PATH('')), 1, 2, ''), '') AS [CN], " +
                    "COUNT(DISTINCT me.ItemMeterKey) AS [Meters], " +
                    "MAX(ISNULL(me.StrategyCode, '')) AS [Strategy], " +
                    "MAX(me.InvoicedAt) AS [Generated At], " +
                    "MAX(el.LastEmailAt) AS [Emailed] " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = me.ItemMeterKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.IV iv ON iv.DocKey = me.InvoicedDocKey " +
                    "LEFT JOIN (SELECT DocKey, MAX(SentAt) AS LastEmailAt FROM dbo.zSCP2_EmailLog GROUP BY DocKey) el " +
                    "  ON el.DocKey = iv.DocKey " +
                    "WHERE me.InvoicedDocKey IS NOT NULL AND " + filterCol + " = " + key + " " +
                    "GROUP BY iv.DocKey, iv.DocNo, iv.DocDate, iv.NetTotal, iv.Cancelled, me.PeriodYear, me.PeriodMonth " +
                    "ORDER BY me.PeriodYear DESC, me.PeriodMonth DESC, iv.DocNo DESC", false);
            }
            catch { return Empty(); }
        }

        /// <summary>Same column shape as Load() so an empty tab still renders headers.</summary>
        public static DataTable Empty()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DocKey", typeof(long));
            dt.Columns.Add("Invoice No", typeof(string));
            dt.Columns.Add("Invoice Date", typeof(DateTime));
            dt.Columns.Add("Period", typeof(string));
            dt.Columns.Add("Amount", typeof(decimal));
            dt.Columns.Add("Cancelled", typeof(string));
            dt.Columns.Add("CN", typeof(string));
            dt.Columns.Add("Meters", typeof(int));
            dt.Columns.Add("Strategy", typeof(string));
            dt.Columns.Add("Generated At", typeof(DateTime));
            dt.Columns.Add("Emailed", typeof(DateTime));
            return dt;
        }

        /// <summary>Builds the read-only Billing History grid into a tab page (BuildDebtorHistoryTab
        /// pattern). Double-clicking a row opens the invoice in AutoCount's own editor.</summary>
        public static void BuildTab(System.Windows.Forms.Control page, DBSetting db, string filterCol, long key)
        {
            page.Controls.Clear();
            DevExpress.XtraGrid.GridControl grid = new DevExpress.XtraGrid.GridControl();
            DevExpress.XtraGrid.Views.Grid.GridView view = new DevExpress.XtraGrid.Views.Grid.GridView();
            grid.MainView = view;
            view.GridControl = grid;
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            view.OptionsBehavior.Editable = false;
            view.OptionsView.ShowGroupPanel = false;
            view.OptionsView.ShowAutoFilterRow = true;
            page.Controls.Add(grid);

            DataTable dt = key > 0 ? Load(db, filterCol, key) : Empty();
            grid.DataSource = dt;

            // Demo 28/07 #9b: the contract module itself reminds when generated invoices are still
            // waiting to be emailed (send them from Meter Reading > Bulk Email Invoice).
            int notMailed = 0;
            foreach (DataRow r0 in dt.Rows)
                if (dt.Columns.Contains("Emailed") && Convert.ToString(r0["Cancelled"]).Length == 0
                    && r0["Emailed"] == DBNull.Value) notMailed++;
            if (notMailed > 0)
            {
                DevExpress.XtraEditors.LabelControl warn = new DevExpress.XtraEditors.LabelControl();
                warn.Text = "   " + notMailed + " invoice(s) generated but NOT yet emailed  —  send them from " +
                            "Meter Reading > Bulk Email Invoice.";
                warn.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
                warn.Dock = System.Windows.Forms.DockStyle.Top;
                warn.Height = 26;
                warn.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 244, 202);
                warn.Appearance.ForeColor = System.Drawing.Color.FromArgb(150, 90, 0);
                warn.Appearance.Options.UseBackColor = true;
                warn.Appearance.Options.UseForeColor = true;
                warn.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                warn.Appearance.Options.UseTextOptions = true;
                page.Controls.Add(warn);
            }

            // #10: "Correct with CN..." — corrects the focused invoice's readings with a REAL Sales
            // Credit Note (MeterCN_Form); the corrected reading overrides the machine's last reading.
            DevExpress.XtraEditors.PanelControl bar = new DevExpress.XtraEditors.PanelControl();
            bar.Dock = System.Windows.Forms.DockStyle.Top;
            bar.Height = 34;
            DevExpress.XtraEditors.SimpleButton btnCN = new DevExpress.XtraEditors.SimpleButton();
            btnCN.Text = "Correct with CN...";
            btnCN.Location = new System.Drawing.Point(6, 5);
            btnCN.Size = new System.Drawing.Size(130, 24);
            btnCN.ToolTip = "Wrong reading billed and the invoice already sent? Key the CORRECT reading — a real " +
                "Sales Credit Note is created and future billing starts from the corrected reading.";
            bar.Controls.Add(btnCN);
            DevExpress.XtraEditors.SimpleButton btnOpenCN = new DevExpress.XtraEditors.SimpleButton();
            btnOpenCN.Text = "Open CN...";
            btnOpenCN.Location = new System.Drawing.Point(142, 5);
            btnOpenCN.Size = new System.Drawing.Size(100, 24);
            btnOpenCN.ToolTip = "Opens the focused invoice's (latest) correction CN in AutoCount's Credit Note " +
                "module — amend or delete it there; deleting/cancelling rolls the reading override back automatically.";
            bar.Controls.Add(btnOpenCN);
            page.Controls.Add(bar);

            // #10: drift watch — a CN whose quantities were changed DIRECTLY in AutoCount no longer
            // matches the reading correction it was issued for. Detected by comparing the CN's
            // detail qty sum against the correction log; surfaced, never auto-rolled-back.
            try
            {
                DataTable drift = db.GetDataTable(
                    "SELECT DISTINCT t.CNDocNo FROM dbo.zSCP_MeterTrans t " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN (SELECT DocKey, SUM(ISNULL(Qty,0)) AS CnQty FROM dbo.CNDTL GROUP BY DocKey) d " +
                    "  ON d.DocKey = t.CNDocKey " +
                    "JOIN (SELECT DocNo, SUM(ISNULL(-[Usage],0)) AS OurQty FROM dbo.zSCP2_MeterReadingLog " +
                    "  WHERE Source='CN' GROUP BY DocNo) lg ON lg.DocNo = t.CNDocNo " +
                    "WHERE t.CNDocKey IS NOT NULL AND " + filterCol + " = " + key + " " +
                    "AND ABS(ISNULL(d.CnQty,0) - ISNULL(lg.OurQty,0)) > 0.01", false);
                if (drift.Rows.Count > 0)
                {
                    System.Collections.Generic.List<string> nos = new System.Collections.Generic.List<string>();
                    foreach (DataRow r0 in drift.Rows) nos.Add(Convert.ToString(r0["CNDocNo"]));
                    DevExpress.XtraEditors.LabelControl driftWarn = new DevExpress.XtraEditors.LabelControl();
                    driftWarn.Text = "   ⚠ CN modified directly in AutoCount: " + string.Join(", ", nos.ToArray()) +
                                     "  —  its quantity no longer matches the reading correction. Open the CN to review.";
                    driftWarn.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
                    driftWarn.Dock = System.Windows.Forms.DockStyle.Top;
                    driftWarn.Height = 26;
                    driftWarn.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 214, 210);
                    driftWarn.Appearance.ForeColor = System.Drawing.Color.FromArgb(160, 30, 30);
                    driftWarn.Appearance.Options.UseBackColor = true;
                    driftWarn.Appearance.Options.UseForeColor = true;
                    driftWarn.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                    driftWarn.Appearance.Options.UseTextOptions = true;
                    page.Controls.Add(driftWarn);
                }
            }
            catch { }

            grid.BringToFront();   // Fill grid lays out under ALL the Top controls

            btnCN.Click += delegate
            {
                try
                {
                    int rh = view.FocusedRowHandle;
                    if (rh < 0) { DevExpress.XtraEditors.XtraMessageBox.Show("Select an invoice row first.", "Correct with CN"); return; }
                    DataRow row = view.GetDataRow(rh);
                    if (row == null || row["DocKey"] == DBNull.Value) return;
                    if (Convert.ToString(row["Cancelled"]) == "YES")
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show("This invoice is cancelled - nothing to correct.", "Correct with CN");
                        return;
                    }
                    long docKey = Convert.ToInt64(row["DocKey"]);
                    string docNo = Convert.ToString(row["Invoice No"]);
                    using (MeterCN_Form f = new MeterCN_Form(db, docKey, docNo))
                    {
                        if (f.ShowDialog(page.FindForm()) == System.Windows.Forms.DialogResult.OK)
                            // Rebuild AFTER this click handler unwinds — BuildTab clears the very
                            // controls whose event we are currently executing.
                            page.BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate
                            { BuildTab(page, db, filterCol, key); }));
                    }
                }
                catch (Exception ex)
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("Correct with CN failed:\r\n" + ex.Message, "Error");
                }
            };

            btnOpenCN.Click += delegate
            {
                try
                {
                    int rh = view.FocusedRowHandle;
                    if (rh < 0) return;
                    DataRow row = view.GetDataRow(rh);
                    if (row == null) return;
                    string cnList = view.Columns["CN"] != null ? Convert.ToString(row["CN"]).Trim() : "";
                    if (cnList.Length == 0)
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show("This invoice has no correction CN.", "Open CN");
                        return;
                    }
                    // Comma-joined, ordered oldest -> latest; open the LATEST (the one that rules).
                    string[] parts = cnList.Split(',');
                    string cnNo = parts[parts.Length - 1].Trim();
                    DataTable k = db.GetDataTable(
                        "SELECT TOP 1 DocKey FROM dbo.CN WHERE DocNo = '" + cnNo.Replace("'", "''") + "' ORDER BY DocKey DESC", false);
                    if (k.Rows.Count == 0)
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show("CN " + cnNo + " not found — it may have been deleted.", "Open CN");
                        return;
                    }
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand cnCmd =
                        AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                            AutoCount.Authentication.UserSession.CurrentUserSession, db);
                    AutoCount.Invoicing.Sales.CreditNote.CreditNote cnDoc = cnCmd.Edit(Convert.ToInt64(k.Rows[0]["DocKey"]));
                    if (cnDoc == null)
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show("CN " + cnNo + " could not be opened.", "Open CN");
                        return;
                    }
                    using (AutoCount.Invoicing.Sales.CreditNote.FormCreditNoteEntry f =
                        new AutoCount.Invoicing.Sales.CreditNote.FormCreditNoteEntry(cnDoc))
                    {
                        f.ShowDialog(page.FindForm());
                    }
                    // Whatever happened in there (amend / delete / cancel) — rebuild so the reconcile
                    // and the CN/drift banners reflect it.
                    page.BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate
                    { BuildTab(page, db, filterCol, key); }));
                }
                catch (Exception ex)
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("Open CN failed:\r\n" + ex.Message, "Error");
                }
            };

            view.PopulateColumns();
            if (view.Columns["DocKey"] != null) view.Columns["DocKey"].Visible = false;
            if (view.Columns["Amount"] != null)
            {
                view.Columns["Amount"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                view.Columns["Amount"].DisplayFormat.FormatString = "n2";
            }
            if (view.Columns["Invoice Date"] != null)
            {
                view.Columns["Invoice Date"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                view.Columns["Invoice Date"].DisplayFormat.FormatString = "dd/MM/yyyy";
            }
            if (view.Columns["Emailed"] != null)
            {
                view.Columns["Emailed"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                view.Columns["Emailed"].DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            }
            view.BestFitColumns();

            view.DoubleClick += delegate
            {
                try
                {
                    int rh = view.FocusedRowHandle;
                    if (rh < 0) return;
                    DataRow row = view.GetDataRow(rh);
                    if (row == null || row["DocKey"] == DBNull.Value) return;
                    long docKey = Convert.ToInt64(row["DocKey"]);
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                        AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                            AutoCount.Authentication.UserSession.CurrentUserSession, db);
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(docKey);
                    if (doc == null)
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show("Invoice not found — it may have been deleted.", "Open Invoice");
                        return;
                    }
                    using (AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry f =
                        new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc))
                    {
                        f.ShowDialog(page.FindForm());
                    }
                }
                catch (Exception ex)
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("Open invoice failed:\r\n" + ex.Message, "Error");
                }
            };
        }
    }
}
