using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// The invoice layouts this company bills under. Eleven are seeded from the customer's own
    /// July-2026 invoices as starting points; the names are theirs to change and new ones are
    /// theirs to add.
    ///
    /// <para>The grid says each layout in words rather than in codes, and shows how many contracts
    /// use it — so the consequence of editing or retiring one is visible before it is edited.</para>
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Billing Format",
    ParentMenuCaption = "General Setup", MenuOrder = 135, ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_BILLING_FORMAT,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_BILLING_FORMAT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Normal, true)]
    public partial class BillingFormatLst_Form : XtraForm
    {
        private DBSetting _db;
        private DataTable _dt;

        public BillingFormatLst_Form() { InitializeComponent(); }

        public BillingFormatLst_Form(UserSession userSession) : this()
        {
            if (userSession != null) _db = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public BillingFormatLst_Form(DBSetting db) : this()
        {
            _db = db;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            BuildGrid();
            Reload("");
        }

        private void BuildGrid()
        {
            GridViewFormats.OptionsBehavior.AutoPopulateColumns = false;
            GridViewFormats.OptionsBehavior.Editable = false;
            GridViewFormats.OptionsView.ShowGroupPanel = false;
            GridViewFormats.OptionsView.ShowAutoFilterRow = true;
            GridViewFormats.OptionsSelection.EnableAppearanceFocusedCell = false;
            GridViewFormats.Columns.Clear();
            AddCol("FormatCode", "Code", 130);
            AddCol("FormatName", "Name", 250);
            AddCol("Invoices", "Invoices", 130);
            AddCol("Rental", "Rental lines", 110);
            AddCol("Meters", "BK & CL lines", 115);
            AddCol("UsedBy", "Contracts", 70);
            AddCol("Remark", "Note", 300);
            AddCol("Inactive", "Inactive", 60);
            GridViewFormats.DoubleClick += new EventHandler(GridViewFormats_DoubleClick);
        }

        private void AddCol(string field, string caption, int width)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewFormats.Columns.AddVisible(field);
            c.Caption = caption;
            c.Width = width;
        }

        private void Reload(string selectCode)
        {
            try
            {
                _dt = _db.GetDataTable(
                    "SELECT f.FormatCode, f.FormatName, f.InvoiceSplit, f.RentalLineMode, f.MeterLineMode, " +
                    "ISNULL(f.Remark,'') AS Remark, f.Inactive, " +
                    "(SELECT COUNT(*) FROM dbo.zSCP2_Contract c WHERE c.BillingFormatCode = f.FormatCode) AS UsedBy " +
                    "FROM dbo.zSCP2_BillingFormat f ORDER BY f.FormatCode", false);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not load billing formats:\r\n" + ex.Message,
                    "Billing Format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Say the three answers in words, so the codes never have to be decoded by eye.
            _dt.Columns.Add("Invoices", typeof(string));
            _dt.Columns.Add("Rental", typeof(string));
            _dt.Columns.Add("Meters", typeof(string));
            foreach (DataRow r in _dt.Rows)
            {
                r["Invoices"] = ScpBillingFormat.DescribeSplit(Convert.ToString(r["InvoiceSplit"]));
                r["Rental"] = ScpBillingFormat.DescribeLineMode(FirstChar(r["RentalLineMode"]));
                r["Meters"] = ScpBillingFormat.DescribeLineMode(FirstChar(r["MeterLineMode"]));
            }
            GridFormats.DataSource = _dt;
            GridViewFormats.BestFitColumns();

            if (!string.IsNullOrEmpty(selectCode))
                for (int i = 0; i < GridViewFormats.RowCount; i++)
                    if (string.Equals(Convert.ToString(GridViewFormats.GetRowCellValue(i, "FormatCode")),
                                      selectCode, StringComparison.OrdinalIgnoreCase))
                    { GridViewFormats.FocusedRowHandle = i; break; }
        }

        private static char FirstChar(object o)
        {
            string s = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            return s.Length == 0 ? 'A' : char.ToUpperInvariant(s[0]);
        }

        private string FocusedCode()
        {
            int rh = GridViewFormats.FocusedRowHandle;
            if (rh < 0) return "";
            object v = GridViewFormats.GetRowCellValue(rh, "FormatCode");
            return v == null || v == DBNull.Value ? "" : Convert.ToString(v);
        }

        private void GridViewFormats_DoubleClick(object sender, EventArgs e) { Edit(); }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            using (BillingFormat_Form f = new BillingFormat_Form(_db, ""))
                if (f.ShowDialog(this) == DialogResult.OK) Reload(f.SavedCode);
        }

        private void BtnEdit_Click(object sender, EventArgs e) { Edit(); }

        private void Edit()
        {
            string code = FocusedCode();
            if (code.Length == 0) return;
            using (BillingFormat_Form f = new BillingFormat_Form(_db, code))
                if (f.ShowDialog(this) == DialogResult.OK) Reload(code);
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            string code = FocusedCode();
            if (code.Length == 0) return;
            // Most new layouts are a small change to one that already exists, so copying beats
            // starting from nothing.
            try
            {
                string newCode = NextCopyCode(code);
                _db.ExecuteNonQuery(
                    "INSERT INTO dbo.zSCP2_BillingFormat " +
                    "(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark, Inactive) " +
                    "SELECT N'" + newCode.Replace("'", "''") + "', LEFT(FormatName + N' (copy)', 100), " +
                    "InvoiceSplit, RentalLineMode, MeterLineMode, Remark, 'N' " +
                    "FROM dbo.zSCP2_BillingFormat WHERE FormatCode = N'" + code.Replace("'", "''") + "'");
                Reload(newCode);
                using (BillingFormat_Form f = new BillingFormat_Form(_db, newCode))
                    if (f.ShowDialog(this) == DialogResult.OK) Reload(f.SavedCode);
                    else Reload(newCode);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Copy failed:\r\n" + ex.Message, "Billing Format",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string NextCopyCode(string code)
        {
            string baseCode = (code.Length > 16 ? code.Substring(0, 16) : code) + "-C";
            for (int n = 1; n < 100; n++)
            {
                string candidate = n == 1 ? baseCode : baseCode + n;
                object dup = _db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_BillingFormat " +
                    "WHERE FormatCode = N'" + candidate.Replace("'", "''") + "'");
                if (dup == null || dup == DBNull.Value || Convert.ToInt32(dup) == 0) return candidate;
            }
            throw new InvalidOperationException("Too many copies of " + code + ".");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            string code = FocusedCode();
            if (code.Length == 0) return;
            int used = ScpBillingFormat.UsedByContractCount(_db, code);
            if (used > 0)
            {
                // Deleting would leave those contracts pointing at nothing. Retiring keeps their
                // history readable and stops the format being chosen again.
                if (XtraMessageBox.Show(this,
                        used + " contract(s) use '" + code + "', so it cannot be deleted.\r\n\r\n" +
                        "Mark it Inactive instead? Those contracts keep billing as they do now, and " +
                        "the format stops appearing when someone picks one.",
                        "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                _db.ExecuteNonQuery("UPDATE dbo.zSCP2_BillingFormat SET Inactive='Y', LastModified=GETDATE() " +
                                    "WHERE FormatCode = N'" + code.Replace("'", "''") + "'");
                Reload(code);
                return;
            }
            if (XtraMessageBox.Show(this, "Delete billing format '" + code + "'?",
                    "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_BillingFormat WHERE FormatCode = N'" +
                                code.Replace("'", "''") + "'");
            Reload("");
        }

        private void BtnRefresh_Click(object sender, EventArgs e) { Reload(FocusedCode()); }

        private void BtnClose_Click(object sender, EventArgs e) { this.Close(); }
    }
}
