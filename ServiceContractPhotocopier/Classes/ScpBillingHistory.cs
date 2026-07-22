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
                return db.GetDataTable(
                    "SELECT iv.DocKey, iv.DocNo AS [Invoice No], iv.DocDate AS [Invoice Date], " +
                    "RIGHT('0' + CAST(me.PeriodMonth AS VARCHAR(2)), 2) + '/' + CAST(me.PeriodYear AS VARCHAR(4)) AS [Period], " +
                    "ISNULL(iv.NetTotal, 0) AS [Amount], " +
                    "CASE WHEN ISNULL(iv.Cancelled,'F') = 'T' THEN 'YES' ELSE '' END AS [Cancelled], " +
                    "COUNT(DISTINCT me.ItemMeterKey) AS [Meters], " +
                    "MAX(ISNULL(me.StrategyCode, '')) AS [Strategy], " +
                    "MAX(me.InvoicedAt) AS [Generated At] " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = me.ItemMeterKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.IV iv ON iv.DocKey = me.InvoicedDocKey " +
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
            dt.Columns.Add("Meters", typeof(int));
            dt.Columns.Add("Strategy", typeof(string));
            dt.Columns.Add("Generated At", typeof(DateTime));
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
