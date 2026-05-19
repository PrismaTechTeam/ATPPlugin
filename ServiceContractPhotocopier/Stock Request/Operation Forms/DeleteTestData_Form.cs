using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Stock.StockIssue;
using AutoCount.Stock.StockTransfer;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    /// <summary>
    /// Hidden Ctrl+Alt+0 popup. Wipes every Z_PumsStockIssue / Z_PumsStockTransfer
    /// row created during testing, AND deletes the matching AutoCount documents
    /// (any row with a non-empty GeneratedDocNo whose Status is Complete or Update).
    /// </summary>
    public partial class DeleteTestData_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly UserSession _session;

        public DeleteTestData_Form() { InitializeComponent(); }

        public DeleteTestData_Form(DBSetting db, UserSession session) : this()
        {
            _db = db;
            _session = session;
            RefreshCounts();
        }

        private void RefreshCounts()
        {
            int issueRows = ScalarInt("SELECT COUNT(*) FROM Z_PumsStockIssue");
            int xferRows  = ScalarInt("SELECT COUNT(*) FROM Z_PumsStockTransfer");
            int issueDocs = ScalarInt("SELECT COUNT(*) FROM Z_PumsStockIssue WHERE GeneratedDocNo IS NOT NULL AND LEN(GeneratedDocNo) > 0");
            int xferDocs  = ScalarInt("SELECT COUNT(*) FROM Z_PumsStockTransfer WHERE GeneratedDocNo IS NOT NULL AND LEN(GeneratedDocNo) > 0");
            this.LblSummary.Text = string.Format(
                "Z_PumsStockIssue: {0} rows ({1} have an AutoCount Stock Issue doc)\r\n" +
                "Z_PumsStockTransfer: {2} rows ({3} have an AutoCount Stock Transfer doc)",
                issueRows, issueDocs, xferRows, xferDocs);
        }

        private int ScalarInt(string sql)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                }
            }
            catch { return -1; }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show(this,
                "This will DELETE every PUMS Stock Issue / Stock Transfer request,\r\n" +
                "AND the matching AutoCount documents. There is no undo.\r\n\r\nContinue?",
                "Delete All Test Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            this.BtnDelete.Enabled = false;
            this.LblStatus.Text = "Working…";
            this.Update();

            int siDeleted = 0, stDeleted = 0, siRows = 0, stRows = 0;
            List<string> errors = new List<string>();

            try
            {
                // 1. Collect doc numbers BEFORE wiping our tables
                List<string> issueDocs = CollectDocs("Z_PumsStockIssue");
                List<string> xferDocs  = CollectDocs("Z_PumsStockTransfer");

                // 2. Delete AutoCount documents one at a time so a single failure doesn't abort all
                StockIssueCommand sic    = StockIssueCommand.Create(_session, _db);
                StockTransferCommand stc = StockTransferCommand.Create(_session, _db);
                foreach (string d in issueDocs)
                {
                    try { sic.Delete(d); siDeleted++; }
                    catch (Exception ex) { errors.Add("StockIssue " + d + ": " + ex.Message); }
                }
                foreach (string d in xferDocs)
                {
                    try { stc.Delete(d); stDeleted++; }
                    catch (Exception ex) { errors.Add("StockTransfer " + d + ": " + ex.Message); }
                }

                // 3. Wipe our PUMS tables
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                {
                    conn.Open();
                    siRows = NonQuery(conn, "DELETE FROM Z_PumsStockIssue");
                    stRows = NonQuery(conn, "DELETE FROM Z_PumsStockTransfer");
                }

                this.LblStatus.Text = string.Format(
                    "Done. Removed {0} Stock Issue + {1} Stock Transfer requests; " +
                    "deleted {2} Stock Issue + {3} Stock Transfer documents in AutoCount." +
                    (errors.Count > 0 ? "  ({4} errors)" : ""),
                    siRows, stRows, siDeleted, stDeleted, errors.Count);

                if (errors.Count > 0)
                {
                    string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                        "delete-test-data-errors-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                    System.IO.File.WriteAllLines(path, errors);
                    try { System.Diagnostics.Process.Start(path); } catch { }
                }

                RefreshCounts();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Fatal: " + ex.Message,
                    "Delete All Test Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.LblStatus.Text = "Failed: " + ex.Message;
            }
            finally
            {
                this.BtnDelete.Enabled = true;
            }
        }

        private void BtnClose_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; }

        private List<string> CollectDocs(string table)
        {
            List<string> list = new List<string>();
            using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT DISTINCT GeneratedDocNo FROM " + table +
                " WHERE GeneratedDocNo IS NOT NULL AND LEN(GeneratedDocNo) > 0", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(r.GetString(0));
            }
            return list;
        }

        private static int NonQuery(SqlConnection conn, string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn)) return cmd.ExecuteNonQuery();
        }
    }
}
