using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    /// <summary>
    /// View Log form — browse Z_PumsLog rows by type + date range. Double-click a
    /// row to see the payload / response / error detail.
    /// </summary>
    public partial class StockRequestLog_Form : XtraForm
    {
        private readonly DBSetting _db;

        public StockRequestLog_Form() { InitializeComponent(); }

        public StockRequestLog_Form(DBSetting db) : this()
        {
            _db = db;
            this.CmbLogType.Properties.Items.AddRange(new object[] { "All", "Information", "Warning", "Error" });
            this.CmbLogType.SelectedItem = "All";
            this.DtFrom.DateTime = DateTime.Today.AddDays(-7);
            this.DtTo.DateTime   = DateTime.Today.AddDays(1);
            this.GridViewLog.DoubleClick += GridViewLog_DoubleClick;
            Reload();
        }

        private void BtnReload_Click(object sender, EventArgs e) => Reload();
        private void BtnClose_Click(object sender, EventArgs e)  => this.DialogResult = DialogResult.OK;

        private void Reload()
        {
            if (_db == null) return;
            string type = Convert.ToString(this.CmbLogType.SelectedItem ?? "All");
            DateTime fromUtc = this.DtFrom.DateTime.Date.ToUniversalTime();
            DateTime toUtc   = this.DtTo.DateTime.Date.AddDays(1).ToUniversalTime();
            try
            {
                DataTable dt = PumsLog.Query(_db, type, fromUtc, toUtc);
                this.GridLog.DataSource = dt;
                HideCol("Payload");
                HideCol("Response");
                FormatLoggedAt();
                this.GridViewLog.BestFitColumns();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Failed to load logs:\r\n" + ex.Message,
                    "View Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HideCol(string field)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = this.GridViewLog.Columns.ColumnByFieldName(field);
            if (c != null) c.Visible = false;
        }

        private void FormatLoggedAt()
        {
            DevExpress.XtraGrid.Columns.GridColumn c = this.GridViewLog.Columns.ColumnByFieldName("LoggedAt");
            if (c == null) return;
            c.DisplayFormat.FormatType   = DevExpress.Utils.FormatType.DateTime;
            c.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss";
        }

        private void GridViewLog_DoubleClick(object sender, EventArgs e)
        {
            DevExpress.XtraGrid.Views.Grid.GridView v = this.GridViewLog;
            if (v.FocusedRowHandle < 0) return;
            string type     = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "LogType"));
            string source   = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Source"));
            string refId    = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "ReferenceId"));
            string message  = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Message"));
            string payload  = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Payload"));
            string response = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Response"));
            object whenObj  = v.GetRowCellValue(v.FocusedRowHandle, "LoggedAt");
            string when     = whenObj is DateTime dt
                              ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                              : Convert.ToString(whenObj);
            string body =
                "Type:       " + type        + "\r\n" +
                "Source:     " + source      + "\r\n" +
                "Reference:  " + refId       + "\r\n" +
                "LoggedAt:   " + when        + "\r\n" +
                "------ Message ------\r\n"  + message  + "\r\n\r\n" +
                "------ Payload ------\r\n"  + payload  + "\r\n\r\n" +
                "------ Response ------\r\n" + response;
            ShowDetailPopup("Log #" + Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "LogKey")), body);
        }

        private static void ShowDetailPopup(string title, string body)
        {
            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = title;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new System.Drawing.Size(820, 520);
                dlg.MinimizeBox = false;
                MemoEdit memo = new MemoEdit
                {
                    Dock = DockStyle.Fill,
                    Text = body
                };
                memo.Properties.ReadOnly = true;
                memo.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9F);
                memo.Properties.Appearance.Options.UseFont = true;
                dlg.Controls.Add(memo);
                dlg.ShowDialog();
            }
        }
    }
}
