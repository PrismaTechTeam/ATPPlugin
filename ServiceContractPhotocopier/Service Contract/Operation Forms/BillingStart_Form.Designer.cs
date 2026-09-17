namespace ServiceContractPhotocopier
{
    partial class BillingStart_Form
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

        private void InitializeComponent()
        {
            this.PanelHead = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle = new DevExpress.XtraEditors.LabelControl();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.LblContract = new DevExpress.XtraEditors.LabelControl();
            this.LkContract = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.GridViewContracts = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColLkContractNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColLkCustomer = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColLkStart = new DevExpress.XtraGrid.Columns.GridColumn();
            this.LblFrom = new DevExpress.XtraEditors.LabelControl();
            this.CmbFromMonth = new DevExpress.XtraEditors.ComboBoxEdit();
            this.SpnFromYear = new DevExpress.XtraEditors.SpinEdit();
            this.LblOldInvoice = new DevExpress.XtraEditors.LabelControl();
            this.TxtOldInvoice = new DevExpress.XtraEditors.TextEdit();
            this.BtnFillDown = new DevExpress.XtraEditors.SimpleButton();
            this.GridMeters = new DevExpress.XtraGrid.GridControl();
            this.GridViewMeters = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMachine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMeter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMeterName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColClosing = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColOldInvoice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoNumber = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            this.RepoText = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelFoot = new DevExpress.XtraEditors.PanelControl();
            this.LblFoot = new DevExpress.XtraEditors.LabelControl();
            this.BtnApply = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelHead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkContract.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbFromMonth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFromYear.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOldInvoice.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoText)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFoot)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHead
            //
            this.PanelHead.Controls.Add(this.LblTitle);
            this.PanelHead.Controls.Add(this.LblHint);
            this.PanelHead.Controls.Add(this.LblContract);
            this.PanelHead.Controls.Add(this.LkContract);
            this.PanelHead.Controls.Add(this.LblFrom);
            this.PanelHead.Controls.Add(this.CmbFromMonth);
            this.PanelHead.Controls.Add(this.SpnFromYear);
            this.PanelHead.Controls.Add(this.LblOldInvoice);
            this.PanelHead.Controls.Add(this.TxtOldInvoice);
            this.PanelHead.Controls.Add(this.BtnFillDown);
            this.PanelHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHead.Location = new System.Drawing.Point(0, 0);
            this.PanelHead.Name = "PanelHead";
            this.PanelHead.Size = new System.Drawing.Size(940, 150);
            this.PanelHead.TabIndex = 0;
            //
            // LblTitle
            //
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(16, 12);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(300, 21);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Where this contract starts billing";
            //
            // LblHint
            //
            this.LblHint.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblHint.Appearance.Options.UseFont = true;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(16, 37);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(900, 30);
            this.LblHint.TabIndex = 1;
            this.LblHint.Text = "For a contract already running before this system: name the first month to bill he" +
                "re, the closing reading each counter finished the month before on, and the invoic" +
                "e the old system issued for it. Earlier months then count as settled.";
            //
            // LblContract
            //
            this.LblContract.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblContract.Appearance.Options.UseFont = true;
            this.LblContract.Location = new System.Drawing.Point(16, 79);
            this.LblContract.Name = "LblContract";
            this.LblContract.Size = new System.Drawing.Size(48, 15);
            this.LblContract.TabIndex = 2;
            this.LblContract.Text = "Contract:";
            //
            // LkContract
            //
            this.LkContract.Location = new System.Drawing.Point(90, 76);
            this.LkContract.Name = "LkContract";
            this.LkContract.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.LkContract.Properties.DisplayMember = "Label";
            this.LkContract.Properties.NullText = "";
            this.LkContract.Properties.PopupView = this.GridViewContracts;
            this.LkContract.Properties.ValueMember = "ContractKey";
            this.LkContract.Size = new System.Drawing.Size(420, 22);
            this.LkContract.TabIndex = 3;
            //
            // GridViewContracts
            //
            this.GridViewContracts.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColLkContractNo,
            this.ColLkCustomer,
            this.ColLkStart});
            this.GridViewContracts.Name = "GridViewContracts";
            this.GridViewContracts.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewContracts.OptionsView.ShowAutoFilterRow = true;
            this.GridViewContracts.OptionsView.ShowGroupPanel = false;
            this.GridViewContracts.OptionsView.ShowIndicator = false;
            //
            // ColLkContractNo
            //
            this.ColLkContractNo.Caption = "Contract";
            this.ColLkContractNo.FieldName = "ContractNo";
            this.ColLkContractNo.Name = "ColLkContractNo";
            this.ColLkContractNo.Visible = true;
            this.ColLkContractNo.VisibleIndex = 0;
            this.ColLkContractNo.Width = 130;
            //
            // ColLkCustomer
            //
            this.ColLkCustomer.Caption = "Customer";
            this.ColLkCustomer.FieldName = "Customer";
            this.ColLkCustomer.Name = "ColLkCustomer";
            this.ColLkCustomer.Visible = true;
            this.ColLkCustomer.VisibleIndex = 1;
            this.ColLkCustomer.Width = 260;
            //
            // ColLkStart
            //
            this.ColLkStart.Caption = "Starts";
            this.ColLkStart.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColLkStart.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColLkStart.FieldName = "ServiceStartDate";
            this.ColLkStart.Name = "ColLkStart";
            this.ColLkStart.Visible = true;
            this.ColLkStart.VisibleIndex = 2;
            this.ColLkStart.Width = 90;
            //
            // LblFrom
            //
            this.LblFrom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFrom.Appearance.Options.UseFont = true;
            this.LblFrom.Location = new System.Drawing.Point(16, 113);
            this.LblFrom.Name = "LblFrom";
            this.LblFrom.Size = new System.Drawing.Size(66, 15);
            this.LblFrom.TabIndex = 4;
            this.LblFrom.Text = "Bill from:";
            //
            // CmbFromMonth
            //
            this.CmbFromMonth.Location = new System.Drawing.Point(90, 110);
            this.CmbFromMonth.Name = "CmbFromMonth";
            this.CmbFromMonth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbFromMonth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbFromMonth.Size = new System.Drawing.Size(130, 22);
            this.CmbFromMonth.TabIndex = 5;
            //
            // SpnFromYear
            //
            this.SpnFromYear.EditValue = new decimal(new int[] { 2026, 0, 0, 0 });
            this.SpnFromYear.Location = new System.Drawing.Point(228, 110);
            this.SpnFromYear.Name = "SpnFromYear";
            this.SpnFromYear.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SpnFromYear.Properties.IsFloatValue = false;
            this.SpnFromYear.Properties.MaskSettings.Set("mask", "N00");
            this.SpnFromYear.Properties.MaxValue = new decimal(new int[] { 2200, 0, 0, 0 });
            this.SpnFromYear.Properties.MinValue = new decimal(new int[] { 2000, 0, 0, 0 });
            this.SpnFromYear.Size = new System.Drawing.Size(82, 22);
            this.SpnFromYear.TabIndex = 6;
            //
            // LblOldInvoice
            //
            this.LblOldInvoice.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblOldInvoice.Appearance.Options.UseFont = true;
            this.LblOldInvoice.Location = new System.Drawing.Point(340, 113);
            this.LblOldInvoice.Name = "LblOldInvoice";
            this.LblOldInvoice.Size = new System.Drawing.Size(84, 15);
            this.LblOldInvoice.TabIndex = 7;
            this.LblOldInvoice.Text = "Old invoice no:";
            //
            // TxtOldInvoice
            //
            this.TxtOldInvoice.Location = new System.Drawing.Point(432, 110);
            this.TxtOldInvoice.Name = "TxtOldInvoice";
            this.TxtOldInvoice.Size = new System.Drawing.Size(160, 22);
            this.TxtOldInvoice.TabIndex = 8;
            //
            // BtnFillDown
            //
            this.BtnFillDown.Location = new System.Drawing.Point(600, 109);
            this.BtnFillDown.Name = "BtnFillDown";
            this.BtnFillDown.Size = new System.Drawing.Size(110, 24);
            this.BtnFillDown.TabIndex = 9;
            this.BtnFillDown.Text = "Put on every row";
            //
            // GridMeters
            //
            this.GridMeters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridMeters.Location = new System.Drawing.Point(0, 150);
            this.GridMeters.MainView = this.GridViewMeters;
            this.GridMeters.Name = "GridMeters";
            this.GridMeters.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoNumber,
            this.RepoText});
            this.GridMeters.Size = new System.Drawing.Size(940, 330);
            this.GridMeters.TabIndex = 1;
            this.GridMeters.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMeters});
            //
            // GridViewMeters
            //
            this.GridViewMeters.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColMachine,
            this.ColSerial,
            this.ColMeter,
            this.ColMeterName,
            this.ColClosing,
            this.ColOldInvoice});
            this.GridViewMeters.ColumnPanelRowHeight = 28;
            this.GridViewMeters.GridControl = this.GridMeters;
            this.GridViewMeters.Name = "GridViewMeters";
            this.GridViewMeters.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewMeters.OptionsView.ColumnAutoWidth = true;
            this.GridViewMeters.OptionsView.ShowGroupPanel = false;
            this.GridViewMeters.OptionsView.ShowIndicator = false;
            this.GridViewMeters.OptionsView.ShowViewCaption = true;
            this.GridViewMeters.RowHeight = 24;
            this.GridViewMeters.ViewCaption = "Counters on this contract";
            //
            // ColMachine
            //
            this.ColMachine.Caption = "Machine";
            this.ColMachine.FieldName = "ServiceItemNo";
            this.ColMachine.Name = "ColMachine";
            this.ColMachine.OptionsColumn.AllowEdit = false;
            this.ColMachine.Visible = true;
            this.ColMachine.VisibleIndex = 0;
            this.ColMachine.Width = 130;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial";
            this.ColSerial.FieldName = "SerialNo";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 1;
            this.ColSerial.Width = 120;
            //
            // ColMeter
            //
            this.ColMeter.Caption = "Meter";
            this.ColMeter.FieldName = "MeterTypeCode";
            this.ColMeter.Name = "ColMeter";
            this.ColMeter.OptionsColumn.AllowEdit = false;
            this.ColMeter.Visible = true;
            this.ColMeter.VisibleIndex = 2;
            this.ColMeter.Width = 90;
            //
            // ColMeterName
            //
            this.ColMeterName.Caption = "Description";
            this.ColMeterName.FieldName = "MeterTypeName";
            this.ColMeterName.Name = "ColMeterName";
            this.ColMeterName.OptionsColumn.AllowEdit = false;
            this.ColMeterName.Visible = true;
            this.ColMeterName.VisibleIndex = 3;
            this.ColMeterName.Width = 220;
            //
            // ColClosing
            //
            this.ColClosing.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColClosing.AppearanceCell.Options.UseTextOptions = true;
            this.ColClosing.Caption = "Closing reading";
            this.ColClosing.ColumnEdit = this.RepoNumber;
            this.ColClosing.FieldName = "Closing";
            this.ColClosing.Name = "ColClosing";
            this.ColClosing.Visible = true;
            this.ColClosing.VisibleIndex = 4;
            this.ColClosing.Width = 120;
            //
            // ColOldInvoice
            //
            this.ColOldInvoice.Caption = "Old invoice no";
            this.ColOldInvoice.ColumnEdit = this.RepoText;
            this.ColOldInvoice.FieldName = "OldInvoice";
            this.ColOldInvoice.Name = "ColOldInvoice";
            this.ColOldInvoice.Visible = true;
            this.ColOldInvoice.VisibleIndex = 5;
            this.ColOldInvoice.Width = 150;
            //
            // RepoNumber
            //
            this.RepoNumber.AutoHeight = false;
            this.RepoNumber.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.RepoNumber.DisplayFormat.FormatString = "n0";
            this.RepoNumber.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.RepoNumber.IsFloatValue = false;
            this.RepoNumber.MaskSettings.Set("mask", "N00");
            this.RepoNumber.MaxValue = new decimal(new int[] { 999999999, 0, 0, 0 });
            this.RepoNumber.Name = "RepoNumber";
            //
            // RepoText
            //
            this.RepoText.AutoHeight = false;
            this.RepoText.Name = "RepoText";
            //
            // PanelFoot
            //
            this.PanelFoot.Controls.Add(this.LblFoot);
            this.PanelFoot.Controls.Add(this.BtnApply);
            this.PanelFoot.Controls.Add(this.BtnClose);
            this.PanelFoot.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelFoot.Location = new System.Drawing.Point(0, 480);
            this.PanelFoot.Name = "PanelFoot";
            this.PanelFoot.Size = new System.Drawing.Size(940, 52);
            this.PanelFoot.TabIndex = 2;
            //
            // LblFoot
            //
            this.LblFoot.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFoot.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblFoot.Appearance.Options.UseFont = true;
            this.LblFoot.Appearance.Options.UseForeColor = true;
            this.LblFoot.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFoot.Location = new System.Drawing.Point(16, 18);
            this.LblFoot.Name = "LblFoot";
            this.LblFoot.Size = new System.Drawing.Size(640, 18);
            this.LblFoot.TabIndex = 0;
            this.LblFoot.Text = "";
            //
            // BtnApply
            //
            this.BtnApply.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnApply.Appearance.Options.UseFont = true;
            this.BtnApply.Location = new System.Drawing.Point(700, 12);
            this.BtnApply.Name = "BtnApply";
            this.BtnApply.Size = new System.Drawing.Size(120, 28);
            this.BtnApply.TabIndex = 1;
            this.BtnApply.Text = "Apply";
            //
            // BtnClose
            //
            this.BtnClose.Location = new System.Drawing.Point(828, 12);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(96, 28);
            this.BtnClose.TabIndex = 2;
            this.BtnClose.Text = "Close";
            //
            // BillingStart_Form
            //
            this.Controls.Add(this.GridMeters);
            this.Controls.Add(this.PanelFoot);
            this.Controls.Add(this.PanelHead);
            this.MinimumSize = new System.Drawing.Size(820, 460);
            this.Name = "BillingStart_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Billing Start (old data)";
            this.ClientSize = new System.Drawing.Size(940, 532);
            ((System.ComponentModel.ISupportInitialize)(this.PanelHead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkContract.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbFromMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFromYear.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOldInvoice.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoText)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFoot)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl PanelHead;
        private DevExpress.XtraEditors.LabelControl LblTitle;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.LabelControl LblContract;
        private DevExpress.XtraEditors.SearchLookUpEdit LkContract;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewContracts;
        private DevExpress.XtraGrid.Columns.GridColumn ColLkContractNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColLkCustomer;
        private DevExpress.XtraGrid.Columns.GridColumn ColLkStart;
        private DevExpress.XtraEditors.LabelControl LblFrom;
        private DevExpress.XtraEditors.ComboBoxEdit CmbFromMonth;
        private DevExpress.XtraEditors.SpinEdit SpnFromYear;
        private DevExpress.XtraEditors.LabelControl LblOldInvoice;
        private DevExpress.XtraEditors.TextEdit TxtOldInvoice;
        private DevExpress.XtraEditors.SimpleButton BtnFillDown;
        private DevExpress.XtraGrid.GridControl GridMeters;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMeters;
        private DevExpress.XtraGrid.Columns.GridColumn ColMachine;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColMeter;
        private DevExpress.XtraGrid.Columns.GridColumn ColMeterName;
        private DevExpress.XtraGrid.Columns.GridColumn ColClosing;
        private DevExpress.XtraGrid.Columns.GridColumn ColOldInvoice;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit RepoNumber;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoText;
        private DevExpress.XtraEditors.PanelControl PanelFoot;
        private DevExpress.XtraEditors.LabelControl LblFoot;
        private DevExpress.XtraEditors.SimpleButton BtnApply;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
    }
}
