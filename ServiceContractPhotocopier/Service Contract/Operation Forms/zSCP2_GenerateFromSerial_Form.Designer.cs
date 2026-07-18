namespace ServiceContractPhotocopier
{
    partial class zSCP2_GenerateFromSerial_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTop = new DevExpress.XtraEditors.PanelControl();
            this.LblSearch = new DevExpress.XtraEditors.LabelControl();
            this.TxtSearch = new DevExpress.XtraEditors.TextEdit();
            this.LblDocType = new DevExpress.XtraEditors.LabelControl();
            this.CmbDocType = new DevExpress.XtraEditors.ComboBoxEdit();
            this.GridSerial = new DevExpress.XtraGrid.GridControl();
            this.GridViewSerial = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDocType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDocNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDocDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDebtorCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDebtorName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemDesc = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerialNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.LblCount = new DevExpress.XtraEditors.LabelControl();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbDocType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridSerial)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewSerial)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelTop
            //
            this.PanelTop.Controls.Add(this.LblSearch);
            this.PanelTop.Controls.Add(this.TxtSearch);
            this.PanelTop.Controls.Add(this.LblDocType);
            this.PanelTop.Controls.Add(this.CmbDocType);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(984, 44);
            this.PanelTop.TabIndex = 0;
            //
            // LblSearch
            //
            this.LblSearch.Location = new System.Drawing.Point(12, 14);
            this.LblSearch.Name = "LblSearch";
            this.LblSearch.Size = new System.Drawing.Size(37, 13);
            this.LblSearch.TabIndex = 0;
            this.LblSearch.Text = "Search";
            //
            // TxtSearch
            //
            this.TxtSearch.Location = new System.Drawing.Point(60, 11);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Properties.NullValuePrompt = "Doc No / Customer / Item Code / Serial No";
            this.TxtSearch.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtSearch.Size = new System.Drawing.Size(320, 20);
            this.TxtSearch.TabIndex = 1;
            //
            // LblDocType
            //
            this.LblDocType.Location = new System.Drawing.Point(400, 14);
            this.LblDocType.Name = "LblDocType";
            this.LblDocType.Size = new System.Drawing.Size(46, 13);
            this.LblDocType.TabIndex = 2;
            this.LblDocType.Text = "Doc Type";
            //
            // CmbDocType
            //
            this.CmbDocType.Location = new System.Drawing.Point(456, 11);
            this.CmbDocType.Name = "CmbDocType";
            this.CmbDocType.Properties.Items.AddRange(new object[] {
            "All",
            "DO",
            "IV"});
            this.CmbDocType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbDocType.Size = new System.Drawing.Size(90, 20);
            this.CmbDocType.TabIndex = 3;
            //
            // GridSerial
            //
            this.GridSerial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridSerial.Location = new System.Drawing.Point(0, 44);
            this.GridSerial.MainView = this.GridViewSerial;
            this.GridSerial.Name = "GridSerial";
            this.GridSerial.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel});
            this.GridSerial.Size = new System.Drawing.Size(984, 425);
            this.GridSerial.TabIndex = 1;
            this.GridSerial.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewSerial});
            //
            // GridViewSerial
            //
            this.GridViewSerial.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColSel,
            this.ColDocType,
            this.ColDocNo,
            this.ColDocDate,
            this.ColDebtorCode,
            this.ColDebtorName,
            this.ColItemCode,
            this.ColItemDesc,
            this.ColSerialNo});
            this.GridViewSerial.GridControl = this.GridSerial;
            this.GridViewSerial.Name = "GridViewSerial";
            this.GridViewSerial.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewSerial.OptionsView.ShowAutoFilterRow = true;
            this.GridViewSerial.OptionsView.ShowGroupPanel = false;
            //
            // ColSel
            //
            this.ColSel.Caption = "Select";
            this.ColSel.ColumnEdit = this.RepoSel;
            this.ColSel.FieldName = "Sel";
            this.ColSel.MaxWidth = 55;
            this.ColSel.Name = "ColSel";
            this.ColSel.OptionsFilter.AllowAutoFilter = false;
            this.ColSel.Visible = true;
            this.ColSel.VisibleIndex = 0;
            this.ColSel.Width = 55;
            //
            // ColDocType
            //
            this.ColDocType.Caption = "Type";
            this.ColDocType.FieldName = "DocType";
            this.ColDocType.MaxWidth = 50;
            this.ColDocType.Name = "ColDocType";
            this.ColDocType.OptionsColumn.AllowEdit = false;
            this.ColDocType.Visible = true;
            this.ColDocType.VisibleIndex = 1;
            this.ColDocType.Width = 50;
            //
            // ColDocNo
            //
            this.ColDocNo.Caption = "Doc No";
            this.ColDocNo.FieldName = "DocNo";
            this.ColDocNo.Name = "ColDocNo";
            this.ColDocNo.OptionsColumn.AllowEdit = false;
            this.ColDocNo.Visible = true;
            this.ColDocNo.VisibleIndex = 2;
            this.ColDocNo.Width = 110;
            //
            // ColDocDate
            //
            this.ColDocDate.Caption = "Date";
            this.ColDocDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColDocDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColDocDate.FieldName = "DocDate";
            this.ColDocDate.Name = "ColDocDate";
            this.ColDocDate.OptionsColumn.AllowEdit = false;
            this.ColDocDate.Visible = true;
            this.ColDocDate.VisibleIndex = 3;
            this.ColDocDate.Width = 85;
            //
            // ColDebtorCode
            //
            this.ColDebtorCode.Caption = "Debtor";
            this.ColDebtorCode.FieldName = "DebtorCode";
            this.ColDebtorCode.Name = "ColDebtorCode";
            this.ColDebtorCode.OptionsColumn.AllowEdit = false;
            this.ColDebtorCode.Visible = true;
            this.ColDebtorCode.VisibleIndex = 4;
            this.ColDebtorCode.Width = 95;
            //
            // ColDebtorName
            //
            this.ColDebtorName.Caption = "Customer Name";
            this.ColDebtorName.FieldName = "DebtorName";
            this.ColDebtorName.Name = "ColDebtorName";
            this.ColDebtorName.OptionsColumn.AllowEdit = false;
            this.ColDebtorName.Visible = true;
            this.ColDebtorName.VisibleIndex = 5;
            this.ColDebtorName.Width = 220;
            //
            // ColItemCode
            //
            this.ColItemCode.Caption = "Item Code";
            this.ColItemCode.FieldName = "ItemCode";
            this.ColItemCode.Name = "ColItemCode";
            this.ColItemCode.OptionsColumn.AllowEdit = false;
            this.ColItemCode.Visible = true;
            this.ColItemCode.VisibleIndex = 6;
            this.ColItemCode.Width = 130;
            //
            // ColItemDesc
            //
            this.ColItemDesc.Caption = "Item Description";
            this.ColItemDesc.FieldName = "ItemDesc";
            this.ColItemDesc.Name = "ColItemDesc";
            this.ColItemDesc.OptionsColumn.AllowEdit = false;
            this.ColItemDesc.Visible = true;
            this.ColItemDesc.VisibleIndex = 7;
            this.ColItemDesc.Width = 180;
            //
            // ColSerialNo
            //
            this.ColSerialNo.Caption = "Serial No";
            this.ColSerialNo.FieldName = "SerialNo";
            this.ColSerialNo.Name = "ColSerialNo";
            this.ColSerialNo.OptionsColumn.AllowEdit = false;
            this.ColSerialNo.Visible = true;
            this.ColSerialNo.VisibleIndex = 8;
            this.ColSerialNo.Width = 120;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.LblCount);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 469);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(984, 52);
            this.PanelBottom.TabIndex = 2;
            //
            // LblCount
            //
            this.LblCount.Location = new System.Drawing.Point(12, 20);
            this.LblCount.Name = "LblCount";
            this.LblCount.Size = new System.Drawing.Size(90, 13);
            this.LblCount.TabIndex = 0;
            this.LblCount.Text = "0 serial(s) selected";
            //
            // BtnOK
            //
            this.BtnOK.Location = new System.Drawing.Point(768, 10);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(100, 32);
            this.BtnOK.TabIndex = 1;
            this.BtnOK.Text = "Generate";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(874, 10);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(100, 32);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancel";
            //
            // zSCP2_GenerateFromSerial_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(984, 521);
            this.Controls.Add(this.GridSerial);
            this.Controls.Add(this.PanelTop);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.Name = "zSCP2_GenerateFromSerial_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate From Serial No";
            this.Load += new System.EventHandler(this.OnFormLoad);
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            this.PanelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbDocType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridSerial)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewSerial)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            this.PanelBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraEditors.LabelControl LblDocType;
        private DevExpress.XtraEditors.ComboBoxEdit CmbDocType;
        private DevExpress.XtraGrid.GridControl GridSerial;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColDocType;
        private DevExpress.XtraGrid.Columns.GridColumn ColDocNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColDocDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColDebtorCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColDebtorName;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemDesc;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerialNo;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.LabelControl LblCount;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
