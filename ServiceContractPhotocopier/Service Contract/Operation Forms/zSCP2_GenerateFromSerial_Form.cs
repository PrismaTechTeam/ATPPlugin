using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// "Generate From Serial No" picker: lists DO / IV detail lines that carry a serial number
    /// (dbo.SerialNoTrans, single-serial rows) so the contract editor can auto-fill the customer
    /// header and auto-create one service item per chosen machine (ItemCode + Machine Serial).
    /// </summary>
    public partial class zSCP2_GenerateFromSerial_Form : XtraForm
    {
        /// <summary>One chosen DO/IV serial line.</summary>
        public class PickedSerial
        {
            public string DocType = "";
            public string DocNo = "";
            public DateTime? DocDate;
            public string DebtorCode = "";
            public string DebtorName = "";
            public string ItemCode = "";
            public string ItemDesc = "";
            public string SerialNo = "";
        }

        private readonly DBSetting _db;
        private DataTable _dt;
        private DevExpress.XtraEditors.CheckEdit _chkShowTransferred;   // demo 28/07 #13

        public List<PickedSerial> Picked = new List<PickedSerial>();

        public zSCP2_GenerateFromSerial_Form()
        {
            InitializeComponent();
        }

        public zSCP2_GenerateFromSerial_Form(DBSetting db)
        {
            InitializeComponent();
            _db = db;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            // Demo 28/07 #13: transferred serials are HIDDEN by default ("只显示 available 的") —
            // tick to review them; each shows WHICH saved service item is using it.
            _chkShowTransferred = new DevExpress.XtraEditors.CheckEdit();
            _chkShowTransferred.Properties.Caption = "Show transferred";
            _chkShowTransferred.Location = new System.Drawing.Point(600, 10);
            _chkShowTransferred.Size = new System.Drawing.Size(150, 22);
            this.CmbDocType.Parent.Controls.Add(_chkShowTransferred);
            _chkShowTransferred.BringToFront();
            this.CmbDocType.SelectedIndex = 0;
            LoadData();
            this.TxtSearch.EditValueChanged += new EventHandler(Filter_Changed);
            this.CmbDocType.SelectedIndexChanged += new EventHandler(Filter_Changed);
            this._chkShowTransferred.CheckedChanged += new EventHandler(Filter_Changed);
            Filter_Changed(this, EventArgs.Empty);   // apply the default available-only filter
            this.GridViewSerial.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewSerial_CellValueChanged);
            this.GridViewSerial.DoubleClick += new EventHandler(GridViewSerial_DoubleClick);
            this.GridViewSerial.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewSerial_RowStyle);
            this.GridViewSerial.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewSerial_ShowingEditor);
        }

        private bool IsTransferredRow(int rowHandle)
        {
            if (rowHandle < 0) return false;
            string usedBy = Convert.ToString(this.GridViewSerial.GetRowCellValue(rowHandle, "TransferredTo"));
            return usedBy != null && usedBy.Trim().Length > 0;
        }

        private void GridViewSerial_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (IsTransferredRow(e.RowHandle))
            {
                e.Appearance.BackColor = System.Drawing.Color.LightYellow;
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(90, 80, 0);
            }
        }

        // #13: a transferred serial cannot be ticked at all — the checkbox editor never opens.
        private void GridViewSerial_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.GridViewSerial.FocusedColumn == null || this.GridViewSerial.FocusedColumn.FieldName != "Sel") return;
            if (IsTransferredRow(this.GridViewSerial.FocusedRowHandle)) e.Cancel = true;
        }

        private void LoadData()
        {
            // Single-serial rows only (ToSerialNo empty = one serial per row — AutoCount range rows are
            // for numeric serial runs, which photocopier machines never use).
            string sql =
                "SELECT CAST(0 AS bit) AS Sel, st.DocType, h.DocNo, h.DocDate, h.DebtorCode, " +
                "ISNULL(d.CompanyName,'') AS DebtorName, st.ItemCode, ISNULL(i.Description,'') AS ItemDesc, " +
                "st.FromSerialNo AS SerialNo, ISNULL(zi.ServiceItemNo,'') AS TransferredTo, " +
                "ISNULL(zi.ContractNo,'') AS TransferredContract " +
                "FROM dbo.SerialNoTrans st " +
                "JOIN (SELECT 'DO' AS DocType, DocKey, DocNo, DocDate, DebtorCode, Cancelled FROM dbo.DO " +
                "      UNION ALL " +
                "      SELECT 'IV', DocKey, DocNo, DocDate, DebtorCode, Cancelled FROM dbo.IV) h " +
                "  ON h.DocType = st.DocType AND h.DocKey = st.DocKey " +
                "LEFT JOIN dbo.Debtor d ON d.AccNo = h.DebtorCode " +
                "LEFT JOIN dbo.Item i ON i.ItemCode = st.ItemCode " +
                // Demo 28/07 #13: a serial already on a SAVED, ACTIVE service item is "transferred".
                // Inactivating that CSSI (machine came back) frees the serial again automatically.
                "OUTER APPLY (SELECT TOP 1 z.ServiceItemNo, c.ContractNo FROM dbo.zSCP2_Item z " +
                "  LEFT JOIN dbo.zSCP2_Contract c ON c.ContractKey = z.ContractKey " +
                "  WHERE z.SerialNumber = st.FromSerialNo AND ISNULL(z.Inactive,'N') = 'N' " +
                "  ORDER BY z.ItemKey DESC) zi " +
                "WHERE st.DocType IN ('DO','IV') " +
                "AND ISNULL(st.Cancelled,'F') <> 'T' AND ISNULL(h.Cancelled,'F') <> 'T' " +
                "AND ISNULL(st.FromSerialNo,'') <> '' AND ISNULL(st.ToSerialNo,'') = '' " +
                // A machine credited BACK (CN with the serial, after this delivery) is no longer at
                // the customer — its DO row leaves the pick list until it is delivered again.
                "AND NOT EXISTS (SELECT 1 FROM dbo.SerialNoTrans cns " +
                "  JOIN dbo.CN cnh ON cnh.DocKey = cns.DocKey AND cns.DocType = 'CN' " +
                "  WHERE cns.FromSerialNo = st.FromSerialNo AND ISNULL(cns.Cancelled,'F') <> 'T' " +
                "    AND ISNULL(cnh.Cancelled,'F') <> 'T' AND cnh.DocDate >= h.DocDate) " +
                "ORDER BY h.DocDate DESC, h.DocNo, st.ItemCode";
            _dt = _db.GetDataTable(sql, false);
            _dt.Columns["Sel"].ReadOnly = false;
            this.GridSerial.DataSource = _dt;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            List<string> parts = new List<string>();
            string docType = (this.CmbDocType.EditValue ?? "All").ToString();
            if (docType == "DO" || docType == "IV")
                parts.Add("[DocType] = '" + docType + "'");
            if (_chkShowTransferred == null || !_chkShowTransferred.Checked)
                parts.Add("[TransferredTo] = ''");
            string s = (this.TxtSearch.EditValue ?? "").ToString().Trim().Replace("'", "''");
            if (s.Length > 0)
                parts.Add("([DocNo] LIKE '%" + s + "%' OR [DebtorCode] LIKE '%" + s + "%' OR [DebtorName] LIKE '%" + s +
                          "%' OR [ItemCode] LIKE '%" + s + "%' OR [SerialNo] LIKE '%" + s + "%')");
            this.GridViewSerial.ActiveFilterString = string.Join(" AND ", parts.ToArray());
        }

        private void GridViewSerial_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "Sel") UpdateCount();
        }

        private void GridViewSerial_DoubleClick(object sender, EventArgs e)
        {
            int rh = this.GridViewSerial.FocusedRowHandle;
            if (rh < 0) return;
            if (IsTransferredRow(rh))
            {
                string cssi = Convert.ToString(this.GridViewSerial.GetRowCellValue(rh, "TransferredTo")).Trim();
                string cn = Convert.ToString(this.GridViewSerial.GetRowCellValue(rh, "TransferredContract")).Trim();
                XtraMessageBox.Show("This serial is already transferred to " +
                    (cn.Length > 0 ? cn + " / " : "") + cssi +
                    ".\r\nInactivate that service item first if the machine really came back.",
                    "Generate From Serial No", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool cur = false;
            object v = this.GridViewSerial.GetRowCellValue(rh, "Sel");
            if (v != null && v != DBNull.Value) cur = Convert.ToBoolean(v);
            this.GridViewSerial.SetRowCellValue(rh, "Sel", !cur);
            UpdateCount();
        }

        private void UpdateCount()
        {
            this.GridViewSerial.PostEditor();
            this.GridViewSerial.UpdateCurrentRow();
            int n = 0;
            foreach (DataRow r in _dt.Rows)
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) n++;
            this.LblCount.Text = n + " serial(s) selected";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            this.GridViewSerial.PostEditor();
            this.GridViewSerial.UpdateCurrentRow();
            Picked.Clear();
            List<string> inUse = new List<string>();
            foreach (DataRow r in _dt.Rows)
            {
                if (r["Sel"] == DBNull.Value || !Convert.ToBoolean(r["Sel"])) continue;
                // #13: a serial already on a saved active service item must not be transferred twice.
                string usedBy = Convert.ToString(r["TransferredTo"]).Trim();
                if (usedBy.Length > 0)
                {
                    inUse.Add(Convert.ToString(r["SerialNo"]) + "  (in use by " + usedBy + ")");
                    continue;
                }
                PickedSerial p = new PickedSerial();
                p.DocType = r["DocType"].ToString();
                p.DocNo = r["DocNo"].ToString();
                p.DocDate = r["DocDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["DocDate"]);
                p.DebtorCode = r["DebtorCode"].ToString();
                p.DebtorName = r["DebtorName"].ToString();
                p.ItemCode = r["ItemCode"].ToString();
                p.ItemDesc = r["ItemDesc"].ToString();
                p.SerialNo = r["SerialNo"].ToString();
                Picked.Add(p);
            }
            if (inUse.Count > 0)
                XtraMessageBox.Show("Skipped - already transferred to a saved service item:\r\n\r\n" +
                    string.Join("\r\n", inUse.ToArray()) + "\r\n\r\n(Inactivate that service item first if the machine really came back.)",
                    "Generate From Serial No", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (Picked.Count == 0)
            {
                if (inUse.Count == 0)
                    XtraMessageBox.Show("Tick at least one serial number to generate from.", "Generate From Serial No",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
