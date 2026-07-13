using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.Classes.BaseForms
{
    /// <summary>
    /// Generic editor for any zSCP_LK_* code/description lookup. Subclasses supply
    /// the table name and key/description columns. All LK editors share this
    /// implementation per the form standardization rule in CLAUDE.md.
    /// </summary>
    public class ScpLookupEdt_Form : XtraForm
    {
        protected LabelControl LblCode, LblDesc, LblInactive;
        protected TextEdit TxtCode;
        protected TextEdit TxtDesc;
        protected CheckEdit ChkInactive;
        protected SimpleButton BtnSave, BtnCancel;

        protected DBSetting _dbSetting;
        protected DataRow _existing;

        protected virtual string TableName   { get { return "zSCP_LK_Unknown"; } }
        /// <summary>The code that was saved (set on OK) — lets a caller select the newly created row.</summary>
        public string SavedCode { get; private set; }

        protected virtual string KeyColumn   { get { return "Code"; } }
        protected virtual string DescColumn  { get { return "Description"; } }
        protected virtual string FormCaption { get { return "Edit Lookup"; } }

        public ScpLookupEdt_Form() { InitializeBaseLayout(); }

        public ScpLookupEdt_Form(DBSetting dbSetting, DataRow existing) : this()
        {
            _dbSetting = dbSetting;
            _existing = existing;
            this.Load += delegate { PopulateFromRow(); };
        }

        private void InitializeBaseLayout()
        {
            this.Text = FormCaption;
            this.Width = 500;
            this.Height = 240;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            LblCode     = new LabelControl(); LblCode.Text = "Code";        LblCode.Location = new Point(20, 25);
            TxtCode     = new TextEdit();     TxtCode.Location = new Point(140, 22); TxtCode.Width = 320;
            LblDesc     = new LabelControl(); LblDesc.Text = "Description"; LblDesc.Location = new Point(20, 60);
            TxtDesc     = new TextEdit();     TxtDesc.Location = new Point(140, 57); TxtDesc.Width = 320;
            LblInactive = new LabelControl(); LblInactive.Text = "Inactive"; LblInactive.Location = new Point(20, 95);
            ChkInactive = new CheckEdit();    ChkInactive.Location = new Point(138, 93); ChkInactive.Properties.Caption = "";

            BtnSave   = new SimpleButton(); BtnSave.Text = "&Save";   BtnSave.Location = new Point(300, 155); BtnSave.Width = 75; BtnSave.Height = 28;
            BtnCancel = new SimpleButton(); BtnCancel.Text = "&Cancel"; BtnCancel.Location = new Point(385, 155); BtnCancel.Width = 75; BtnCancel.Height = 28;

            BtnSave.Click += new EventHandler(OnSave);
            BtnCancel.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(LblCode);
            this.Controls.Add(TxtCode);
            this.Controls.Add(LblDesc);
            this.Controls.Add(TxtDesc);
            this.Controls.Add(LblInactive);
            this.Controls.Add(ChkInactive);
            this.Controls.Add(BtnSave);
            this.Controls.Add(BtnCancel);
            this.AcceptButton = BtnSave;
            this.CancelButton = BtnCancel;
        }

        protected virtual void PopulateFromRow()
        {
            if (_existing == null)
            {
                ChkInactive.Checked = false;
                return;
            }
            TxtCode.Text = _existing[KeyColumn] == null ? "" : _existing[KeyColumn].ToString();
            TxtCode.Properties.ReadOnly = true;
            TxtDesc.Text = _existing[DescColumn] == null ? "" : _existing[DescColumn].ToString();
            string inactive = _existing.Table.Columns.Contains("Inactive") && _existing["Inactive"] != DBNull.Value
                ? _existing["Inactive"].ToString()
                : "N";
            ChkInactive.Checked = (inactive == "Y");
        }

        protected virtual void OnSave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text))
            {
                XtraMessageBox.Show("Code is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string code = SQLString(TxtCode.Text.Trim());
                string desc = SQLString(TxtDesc.Text ?? "");
                string inac = ChkInactive.Checked ? "Y" : "N";

                string sql;
                if (_existing == null)
                {
                    sql = "INSERT INTO [dbo].[" + TableName + "] ([" + KeyColumn + "], [" + DescColumn + "], [Inactive]) " +
                          "VALUES (N'" + code + "', N'" + desc + "', '" + inac + "')";
                }
                else
                {
                    sql = "UPDATE [dbo].[" + TableName + "] " +
                          "SET [" + DescColumn + "] = N'" + desc + "', " +
                          "    [Inactive] = '" + inac + "', " +
                          "    [LastModified] = GETDATE() " +
                          "WHERE [" + KeyColumn + "] = N'" + code + "'";
                }
                _dbSetting.ExecuteNonQuery(sql);

                SavedCode = TxtCode.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
