namespace ServiceContractPhotocopier
{
    partial class zSCP2_TransferToDO_Form
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
            this.LblCustomerCap = new DevExpress.XtraEditors.LabelControl();
            this.LblCustomer = new DevExpress.XtraEditors.LabelControl();
            this.LblDate = new DevExpress.XtraEditors.LabelControl();
            this.DateDoc = new DevExpress.XtraEditors.DateEdit();
            this.BtnAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnNone = new DevExpress.XtraEditors.SimpleButton();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColServiceItemNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerialNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColLocation = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.LblCount = new DevExpress.XtraEditors.LabelControl();
            this.LblNote = new DevExpress.XtraEditors.LabelControl();
            this.BtnCreate = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DateDoc.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DateDoc.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelTop
            //
            this.PanelTop.Controls.Add(this.LblCustomerCap);
            this.PanelTop.Controls.Add(this.LblCustomer);
            this.PanelTop.Controls.Add(this.LblDate);
            this.PanelTop.Controls.Add(this.DateDoc);
            this.PanelTop.Controls.Add(this.BtnAll);
            this.PanelTop.Controls.Add(this.BtnNone);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(844, 48);
            this.PanelTop.TabIndex = 0;
            //
            // LblCustomerCap
            //
            this.LblCustomerCap.Location = new System.Drawing.Point(12, 17);
            this.LblCustomerCap.Name = "LblCustomerCap";
            this.LblCustomerCap.Size = new System.Drawing.Size(47, 13);
            this.LblCustomerCap.TabIndex = 0;
            this.LblCustomerCap.Text = "Deliver to";
            //
            // LblCustomer
            //
            this.LblCustomer.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblCustomer.Appearance.Options.UseFont = true;
            this.LblCustomer.Location = new System.Drawing.Point(68, 17);
            this.LblCustomer.Name = "LblCustomer";
            this.LblCustomer.Size = new System.Drawing.Size(0, 13);
            this.LblCustomer.TabIndex = 1;
            this.LblCustomer.Text = "";
            //
            // LblDate
            //
            this.LblDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LblDate.Location = new System.Drawing.Point(420, 17);
            this.LblDate.Name = "LblDate";
            this.LblDate.Size = new System.Drawing.Size(42, 13);
            this.LblDate.TabIndex = 2;
            this.LblDate.Text = "DO date";
            //
            // DateDoc
            //
            this.DateDoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DateDoc.EditValue = null;
            this.DateDoc.Location = new System.Drawing.Point(470, 14);
            this.DateDoc.Name = "DateDoc";
            this.DateDoc.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DateDoc.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DateDoc.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DateDoc.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DateDoc.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DateDoc.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DateDoc.Properties.MaskSettings.Set("mask", "dd/MM/yyyy");
            this.DateDoc.Size = new System.Drawing.Size(110, 20);
            this.DateDoc.TabIndex = 3;
            //
            // BtnAll
            //
            this.BtnAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnAll.Location = new System.Drawing.Point(624, 12);
            this.BtnAll.Name = "BtnAll";
            this.BtnAll.Size = new System.Drawing.Size(100, 24);
            this.BtnAll.TabIndex = 4;
            this.BtnAll.Text = "Select all";
            this.BtnAll.Click += new System.EventHandler(this.BtnAll_Click);
            //
            // BtnNone
            //
            this.BtnNone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnNone.Location = new System.Drawing.Point(730, 12);
            this.BtnNone.Name = "BtnNone";
            this.BtnNone.Size = new System.Drawing.Size(100, 24);
            this.BtnNone.TabIndex = 5;
            this.BtnNone.Text = "Clear";
            this.BtnNone.Click += new System.EventHandler(this.BtnNone_Click);
            //
            // GridMachines
            //
            this.GridMachines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridMachines.Location = new System.Drawing.Point(0, 48);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel});
            this.GridMachines.Size = new System.Drawing.Size(844, 345);
            this.GridMachines.TabIndex = 1;
            this.GridMachines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMachines});
            //
            // GridViewMachines
            //
            this.GridViewMachines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColSel,
            this.ColServiceItemNo,
            this.ColItemCode,
            this.ColSerialNo,
            this.ColLocation,
            this.ColStatus});
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewMachines.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewMachines.OptionsView.ShowAutoFilterRow = true;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
            this.GridViewMachines.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.GridViewMachines_CellValueChanged);
            this.GridViewMachines.ShowingEditor += new System.ComponentModel.CancelEventHandler(this.GridViewMachines_ShowingEditor);
            this.GridViewMachines.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.GridViewMachines_RowCellStyle);
            //
            // ColSel
            //
            this.ColSel.Caption = "Send";
            this.ColSel.ColumnEdit = this.RepoSel;
            this.ColSel.FieldName = "Sel";
            this.ColSel.MaxWidth = 55;
            this.ColSel.Name = "ColSel";
            this.ColSel.OptionsFilter.AllowAutoFilter = false;
            this.ColSel.Visible = true;
            this.ColSel.VisibleIndex = 0;
            this.ColSel.Width = 55;
            //
            // ColServiceItemNo
            //
            this.ColServiceItemNo.Caption = "Service Item No";
            this.ColServiceItemNo.FieldName = "ServiceItemNo";
            this.ColServiceItemNo.Name = "ColServiceItemNo";
            this.ColServiceItemNo.OptionsColumn.AllowEdit = false;
            this.ColServiceItemNo.Visible = true;
            this.ColServiceItemNo.VisibleIndex = 1;
            this.ColServiceItemNo.Width = 130;
            //
            // ColItemCode
            //
            this.ColItemCode.Caption = "Item Code";
            this.ColItemCode.FieldName = "ItemCode";
            this.ColItemCode.Name = "ColItemCode";
            this.ColItemCode.OptionsColumn.AllowEdit = false;
            this.ColItemCode.Visible = true;
            this.ColItemCode.VisibleIndex = 2;
            this.ColItemCode.Width = 140;
            //
            // ColSerialNo
            //
            this.ColSerialNo.Caption = "Serial No";
            this.ColSerialNo.FieldName = "SerialNo";
            this.ColSerialNo.Name = "ColSerialNo";
            this.ColSerialNo.OptionsColumn.AllowEdit = false;
            this.ColSerialNo.Visible = true;
            this.ColSerialNo.VisibleIndex = 3;
            this.ColSerialNo.Width = 150;
            //
            // ColLocation
            //
            this.ColLocation.Caption = "Location";
            this.ColLocation.FieldName = "Location";
            this.ColLocation.Name = "ColLocation";
            this.ColLocation.OptionsColumn.AllowEdit = false;
            this.ColLocation.Visible = true;
            this.ColLocation.VisibleIndex = 4;
            this.ColLocation.Width = 90;
            //
            // ColStatus
            //
            this.ColStatus.Caption = "Status";
            this.ColStatus.FieldName = "Status";
            this.ColStatus.Name = "ColStatus";
            this.ColStatus.OptionsColumn.AllowEdit = false;
            this.ColStatus.Visible = true;
            this.ColStatus.VisibleIndex = 5;
            this.ColStatus.Width = 260;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            this.RepoSel.EditValueChanged += new System.EventHandler(this.RepoSel_EditValueChanged);
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.LblCount);
            this.PanelBottom.Controls.Add(this.LblNote);
            this.PanelBottom.Controls.Add(this.BtnCreate);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 393);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(844, 52);
            this.PanelBottom.TabIndex = 2;
            //
            // LblCount
            //
            this.LblCount.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblCount.Appearance.Options.UseFont = true;
            this.LblCount.Location = new System.Drawing.Point(12, 10);
            this.LblCount.Name = "LblCount";
            this.LblCount.Size = new System.Drawing.Size(104, 13);
            this.LblCount.TabIndex = 0;
            this.LblCount.Text = "0 machine(s) selected";
            //
            // LblNote
            //
            this.LblNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.LblNote.Appearance.Options.UseForeColor = true;
            this.LblNote.Location = new System.Drawing.Point(12, 29);
            this.LblNote.Name = "LblNote";
            this.LblNote.Size = new System.Drawing.Size(319, 13);
            this.LblNote.TabIndex = 1;
            this.LblNote.Text = "Each goes out at no price - the machine is billed through this contract.";
            //
            // BtnCreate
            //
            this.BtnCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCreate.Location = new System.Drawing.Point(628, 10);
            this.BtnCreate.Name = "BtnCreate";
            this.BtnCreate.Size = new System.Drawing.Size(100, 32);
            this.BtnCreate.TabIndex = 2;
            this.BtnCreate.Text = "Create DO";
            this.BtnCreate.Click += new System.EventHandler(this.BtnCreate_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(734, 10);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(100, 32);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            //
            // zSCP2_TransferToDO_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(844, 445);
            this.Controls.Add(this.GridMachines);
            this.Controls.Add(this.PanelTop);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.Name = "zSCP2_TransferToDO_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Transfer Machine to DO";
            this.Load += new System.EventHandler(this.OnFormLoad);
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            this.PanelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DateDoc.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DateDoc.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            this.PanelBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblCustomerCap;
        private DevExpress.XtraEditors.LabelControl LblCustomer;
        private DevExpress.XtraEditors.LabelControl LblDate;
        private DevExpress.XtraEditors.DateEdit DateDoc;
        private DevExpress.XtraEditors.SimpleButton BtnAll;
        private DevExpress.XtraEditors.SimpleButton BtnNone;
        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColServiceItemNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerialNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColLocation;
        private DevExpress.XtraGrid.Columns.GridColumn ColStatus;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.LabelControl LblCount;
        private DevExpress.XtraEditors.LabelControl LblNote;
        private DevExpress.XtraEditors.SimpleButton BtnCreate;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
