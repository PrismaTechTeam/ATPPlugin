using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Document Numbering Format — the plugin's own numbering maintenance (mirrors AutoCount's
    /// Document Numbering Format Maintenance idea). One row per document type (SC = Service Contract,
    /// SI = Service Item): edit the format string ('SC-&lt;000000&gt;') and the next running number.
    /// The Auto buttons on the Contract / Service Item editors draw from here via ScpDocNo.Next().
    /// </summary>
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    [AutoCount.PlugIn.MenuItem("Document Numbering Format", MenuOrder = 890, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class DocNoFormat_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dt;

        public DocNoFormat_Form() { InitializeComponent(); ApplyAutoCountToolbarImages(); }

        public DocNoFormat_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public DocNoFormat_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void ApplyAutoCountToolbarImages()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                this.BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                this.BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                this.BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            LoadGrid();
        }

        private void LoadGrid()
        {
            try
            {
                _dt = _dbSetting.GetDataTable(
                    "SELECT DocType, Description, FormatString, NextNumber FROM [dbo].[zSCP2_DocNoFormat] ORDER BY DocType", false);
                Grid.DataSource = _dt;
                if (GridView.Columns["DocType"] != null)
                {
                    GridView.Columns["DocType"].OptionsColumn.AllowEdit = false;
                    GridView.Columns["DocType"].OptionsColumn.ReadOnly = true;
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_dbSetting == null || _dt == null) return;
            GridView.CloseEditor();
            GridView.UpdateCurrentRow();

            // Validate before writing anything.
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string fmt = r["FormatString"] == null ? "" : r["FormatString"].ToString().Trim();
                if (fmt.Length == 0)
                { XtraMessageBox.Show("Format String is required (e.g. SC-<000000>).", "Validation"); return; }
                if (fmt.IndexOf('<') < 0 || fmt.IndexOf('>') <= fmt.IndexOf('<'))
                { XtraMessageBox.Show("Format String needs a <000...> placeholder, e.g. SC-<000000>.", "Validation"); return; }
                int n;
                if (!int.TryParse(r["NextNumber"].ToString(), out n) || n < 1)
                { XtraMessageBox.Show("Next Number must be 1 or greater.", "Validation"); return; }
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("DocNoFormat"))
                    {
                        try
                        {
                            foreach (DataRow r in _dt.Rows)
                            {
                                if (r.RowState == DataRowState.Deleted) continue;
                                using (SqlCommand cmd = new SqlCommand(
                                    "UPDATE [dbo].[zSCP2_DocNoFormat] SET Description=@d, FormatString=@f, " +
                                    "NextNumber=@n, LastModified=GETDATE() WHERE DocType=@t", cn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@d", r["Description"] == null ? "" : r["Description"].ToString().Trim());
                                    cmd.Parameters.AddWithValue("@f", r["FormatString"].ToString().Trim());
                                    cmd.Parameters.AddWithValue("@n", Convert.ToInt32(r["NextNumber"]));
                                    cmd.Parameters.AddWithValue("@t", r["DocType"].ToString());
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                XtraMessageBox.Show("Numbering formats saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
