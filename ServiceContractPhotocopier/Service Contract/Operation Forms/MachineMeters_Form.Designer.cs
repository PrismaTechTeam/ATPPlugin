namespace ServiceContractPhotocopier
{
    partial class MachineMeters_Form
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
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRental = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRentalRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBK = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBKRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCL = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCLRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBKLadder = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCLLadder = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMin = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMinAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMinScope = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColWaive = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColWaiveDeal = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoScope = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.RepoState = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.RepoMoney = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoRate = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelActions = new DevExpress.XtraEditors.PanelControl();
            this.LblGive = new DevExpress.XtraEditors.LabelControl();
            this.BtnAddRental = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAddBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAddCL = new DevExpress.XtraEditors.SimpleButton();
            this.LblTake = new DevExpress.XtraEditors.LabelControl();
            this.BtnDelRental = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelCL = new DevExpress.XtraEditors.SimpleButton();
            this.LblRate = new DevExpress.XtraEditors.LabelControl();
            this.TxtRate = new DevExpress.XtraEditors.SpinEdit();
            this.BtnRateRental = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRateBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRateCL = new DevExpress.XtraEditors.SimpleButton();
            this.LblMin = new DevExpress.XtraEditors.LabelControl();
            this.TxtMinAmount = new DevExpress.XtraEditors.SpinEdit();
            this.CmbMinScope = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnAddMin = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelMin = new DevExpress.XtraEditors.SimpleButton();
            this.LblWaive = new DevExpress.XtraEditors.LabelControl();
            this.BtnSetWaive = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelWaive = new DevExpress.XtraEditors.SimpleButton();
            this.LblLadder = new DevExpress.XtraEditors.LabelControl();
            this.CmbLadder = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnLadderBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnLadderCL = new DevExpress.XtraEditors.SimpleButton();
            this.BtnLadderClear = new DevExpress.XtraEditors.SimpleButton();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnSelectAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSameModel = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoState)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoScope)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelActions)).BeginInit();
            this.PanelActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinAmount.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMinScope.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbLadder.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblHint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblHint.Appearance.Options.UseTextOptions = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(12, 9);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(1080, 58);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Tick the machines, then press what they need. Rent, black and colour are independent — " +
    "a machine can have all three, one, or none at all, and a machine nobody reads still bills its rent.\r\n" +
    "One model is usually one deal: click a machine, press \"Tick same model\", type the rate once and set it for all of them. " +
    "Machines that agree on price are also what lets the invoice print ONE line instead of one per machine.";
            //
            // GridMachines
            //
            this.GridMachines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridMachines.Location = new System.Drawing.Point(12, 73);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel,
            this.RepoState,
            this.RepoMoney,
            this.RepoRate,
            this.RepoScope});
            this.GridMachines.Size = new System.Drawing.Size(1080, 300);
            this.GridMachines.TabIndex = 1;
            this.GridMachines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMachines});
            //
            // GridViewMachines
            //
            this.GridViewMachines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColSel,
            this.ColItemNo,
            this.ColSerial,
            this.ColModel,
            this.ColRental,
            this.ColRentalRate,
            this.ColBK,
            this.ColBKRate,
            this.ColCL,
            this.ColCLRate,
            this.ColBKLadder,
            this.ColCLLadder,
            this.ColMin,
            this.ColMinAmount,
            this.ColMinScope,
            this.ColWaive,
            this.ColWaiveDeal});
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
            this.GridViewMachines.OptionsView.ColumnAutoWidth = true;
            //
            // ColSel
            //
            this.ColSel.Caption = "Select";
            this.ColSel.ColumnEdit = this.RepoSel;
            this.ColSel.FieldName = "Sel";
            this.ColSel.MaxWidth = 55;
            this.ColSel.Name = "ColSel";
            this.ColSel.Visible = true;
            this.ColSel.VisibleIndex = 0;
            this.ColSel.Width = 55;
            //
            // ColItemNo
            //
            this.ColItemNo.Caption = "Service Item No";
            this.ColItemNo.FieldName = "ServiceItemNo";
            this.ColItemNo.Name = "ColItemNo";
            this.ColItemNo.OptionsColumn.AllowEdit = false;
            this.ColItemNo.Visible = true;
            this.ColItemNo.VisibleIndex = 1;
            this.ColItemNo.Width = 130;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial No";
            this.ColSerial.FieldName = "SerialNumber";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 2;
            this.ColSerial.Width = 110;
            //
            // ColModel
            //
            this.ColModel.Caption = "Model";
            this.ColModel.FieldName = "ItemCode";
            this.ColModel.Name = "ColModel";
            this.ColModel.OptionsColumn.AllowEdit = false;
            this.ColModel.Visible = true;
            this.ColModel.VisibleIndex = 3;
            this.ColModel.Width = 160;
            //
            // ColRental
            //
            this.ColRental.Caption = "Rental";
            this.ColRental.ColumnEdit = this.RepoState;
            this.ColRental.FieldName = "HasRental";
            this.ColRental.MaxWidth = 60;
            this.ColRental.Name = "ColRental";
            this.ColRental.Visible = true;
            this.ColRental.VisibleIndex = 4;
            this.ColRental.Width = 60;
            //
            // ColRentalRate
            //
            this.ColRentalRate.Caption = "a month";
            this.ColRentalRate.ColumnEdit = this.RepoMoney;
            this.ColRentalRate.DisplayFormat.FormatString = "n2";
            this.ColRentalRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColRentalRate.FieldName = "RentalRate";
            this.ColRentalRate.MaxWidth = 90;
            this.ColRentalRate.Name = "ColRentalRate";
            this.ColRentalRate.Visible = true;
            this.ColRentalRate.VisibleIndex = 5;
            this.ColRentalRate.Width = 90;
            //
            // ColBK
            //
            this.ColBK.Caption = "BK";
            this.ColBK.ColumnEdit = this.RepoState;
            this.ColBK.FieldName = "HasBK";
            this.ColBK.MaxWidth = 45;
            this.ColBK.Name = "ColBK";
            this.ColBK.Visible = true;
            this.ColBK.VisibleIndex = 6;
            this.ColBK.Width = 45;
            //
            // ColBKRate
            //
            this.ColBKRate.Caption = "per copy";
            this.ColBKRate.ColumnEdit = this.RepoRate;
            this.ColBKRate.DisplayFormat.FormatString = "n4";
            this.ColBKRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColBKRate.FieldName = "BKRate";
            this.ColBKRate.MaxWidth = 85;
            this.ColBKRate.Name = "ColBKRate";
            this.ColBKRate.Visible = true;
            this.ColBKRate.VisibleIndex = 7;
            this.ColBKRate.Width = 85;
            //
            // ColCL
            //
            this.ColCL.Caption = "CL";
            this.ColCL.ColumnEdit = this.RepoState;
            this.ColCL.FieldName = "HasCL";
            this.ColCL.MaxWidth = 45;
            this.ColCL.Name = "ColCL";
            this.ColCL.Visible = true;
            this.ColCL.VisibleIndex = 8;
            this.ColCL.Width = 45;
            //
            // ColCLRate
            //
            this.ColCLRate.Caption = "per copy";
            this.ColCLRate.ColumnEdit = this.RepoRate;
            this.ColCLRate.DisplayFormat.FormatString = "n4";
            this.ColCLRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCLRate.FieldName = "CLRate";
            this.ColCLRate.MaxWidth = 85;
            this.ColCLRate.Name = "ColCLRate";
            this.ColCLRate.Visible = true;
            this.ColCLRate.VisibleIndex = 9;
            this.ColCLRate.Width = 85;
            //
            // ColBKLadder
            //
            this.ColBKLadder.Caption = "BK tiers";
            this.ColBKLadder.FieldName = "BKLadder";
            this.ColBKLadder.Name = "ColBKLadder";
            this.ColBKLadder.OptionsColumn.AllowEdit = false;
            this.ColBKLadder.Visible = true;
            this.ColBKLadder.VisibleIndex = 8;
            this.ColBKLadder.Width = 110;
            //
            // ColCLLadder
            //
            this.ColCLLadder.Caption = "CL tiers";
            this.ColCLLadder.FieldName = "CLLadder";
            this.ColCLLadder.Name = "ColCLLadder";
            this.ColCLLadder.OptionsColumn.AllowEdit = false;
            this.ColCLLadder.Visible = true;
            this.ColCLLadder.VisibleIndex = 11;
            this.ColCLLadder.Width = 110;
            //
            // ColMin
            //
            this.ColMin.Caption = "MIN";
            this.ColMin.ColumnEdit = this.RepoState;
            this.ColMin.FieldName = "HasMin";
            this.ColMin.MaxWidth = 45;
            this.ColMin.Name = "ColMin";
            this.ColMin.ToolTip = "A committed minimum: bills the shortfall when the print charges fall below it";
            this.ColMin.Visible = true;
            this.ColMin.VisibleIndex = 12;
            this.ColMin.Width = 45;
            //
            // ColMinAmount
            //
            this.ColMinAmount.Caption = "committed";
            this.ColMinAmount.ColumnEdit = this.RepoMoney;
            this.ColMinAmount.DisplayFormat.FormatString = "n2";
            this.ColMinAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMinAmount.FieldName = "MinAmount";
            this.ColMinAmount.MaxWidth = 90;
            this.ColMinAmount.Name = "ColMinAmount";
            this.ColMinAmount.Visible = true;
            this.ColMinAmount.VisibleIndex = 13;
            this.ColMinAmount.Width = 90;
            //
            // ColMinScope
            //
            this.ColMinScope.Caption = "measured over";
            this.ColMinScope.ColumnEdit = this.RepoScope;
            this.ColMinScope.FieldName = "MinScope";
            this.ColMinScope.MaxWidth = 120;
            this.ColMinScope.Name = "ColMinScope";
            this.ColMinScope.ToolTip = "Whose print charges the minimum is measured against";
            this.ColMinScope.Visible = true;
            this.ColMinScope.VisibleIndex = 14;
            this.ColMinScope.Width = 120;
            //
            // ColWaive
            //
            this.ColWaive.Caption = "Waive";
            this.ColWaive.ColumnEdit = this.RepoState;
            this.ColWaive.FieldName = "HasWaive";
            this.ColWaive.MaxWidth = 55;
            this.ColWaive.Name = "ColWaive";
            this.ColWaive.ToolTip = "The contra line that gives the rent back";
            this.ColWaive.Visible = true;
            this.ColWaive.VisibleIndex = 15;
            this.ColWaive.Width = 55;
            //
            // ColWaiveDeal
            //
            this.ColWaiveDeal.Caption = "waive deal";
            this.ColWaiveDeal.FieldName = "WaiveDeal";
            this.ColWaiveDeal.Name = "ColWaiveDeal";
            this.ColWaiveDeal.OptionsColumn.AllowEdit = false;
            this.ColWaiveDeal.Visible = true;
            this.ColWaiveDeal.VisibleIndex = 16;
            this.ColWaiveDeal.Width = 200;
            //
            // RepoScope
            //
            this.RepoScope.AutoHeight = false;
            this.RepoScope.Name = "RepoScope";
            this.RepoScope.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // RepoState
            //
            this.RepoState.AutoHeight = false;
            this.RepoState.Name = "RepoState";
            //
            // RepoMoney
            //
            this.RepoMoney.AutoHeight = false;
            this.RepoMoney.Mask.EditMask = "n2";
            this.RepoMoney.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoMoney.Mask.UseMaskAsDisplayFormat = true;
            this.RepoMoney.Name = "RepoMoney";
            //
            // RepoRate
            //
            this.RepoRate.AutoHeight = false;
            this.RepoRate.Mask.EditMask = "n4";
            this.RepoRate.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoRate.Mask.UseMaskAsDisplayFormat = true;
            this.RepoRate.Name = "RepoRate";
            //
            // PanelActions
            //
            this.PanelActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelActions.Controls.Add(this.LblGive);
            this.PanelActions.Controls.Add(this.BtnAddRental);
            this.PanelActions.Controls.Add(this.BtnAddBK);
            this.PanelActions.Controls.Add(this.BtnAddCL);
            this.PanelActions.Controls.Add(this.LblTake);
            this.PanelActions.Controls.Add(this.BtnDelRental);
            this.PanelActions.Controls.Add(this.BtnDelBK);
            this.PanelActions.Controls.Add(this.BtnDelCL);
            this.PanelActions.Controls.Add(this.LblRate);
            this.PanelActions.Controls.Add(this.TxtRate);
            this.PanelActions.Controls.Add(this.BtnRateRental);
            this.PanelActions.Controls.Add(this.BtnRateBK);
            this.PanelActions.Controls.Add(this.BtnRateCL);
            this.PanelActions.Controls.Add(this.LblMin);
            this.PanelActions.Controls.Add(this.TxtMinAmount);
            this.PanelActions.Controls.Add(this.CmbMinScope);
            this.PanelActions.Controls.Add(this.BtnAddMin);
            this.PanelActions.Controls.Add(this.BtnDelMin);
            this.PanelActions.Controls.Add(this.LblWaive);
            this.PanelActions.Controls.Add(this.BtnSetWaive);
            this.PanelActions.Controls.Add(this.BtnDelWaive);
            this.PanelActions.Controls.Add(this.LblLadder);
            this.PanelActions.Controls.Add(this.CmbLadder);
            this.PanelActions.Controls.Add(this.BtnLadderBK);
            this.PanelActions.Controls.Add(this.BtnLadderCL);
            this.PanelActions.Controls.Add(this.BtnLadderClear);
            this.PanelActions.Location = new System.Drawing.Point(12, 379);
            this.PanelActions.Name = "PanelActions";
            this.PanelActions.Size = new System.Drawing.Size(1080, 214);
            this.PanelActions.TabIndex = 2;
            //
            // LblGive
            //
            this.LblGive.Location = new System.Drawing.Point(12, 12);
            this.LblGive.Name = "LblGive";
            this.LblGive.Size = new System.Drawing.Size(120, 13);
            this.LblGive.TabIndex = 0;
            this.LblGive.Text = "Ticked machines get:";
            //
            // BtnAddRental
            //
            this.BtnAddRental.Location = new System.Drawing.Point(160, 7);
            this.BtnAddRental.Name = "BtnAddRental";
            this.BtnAddRental.Size = new System.Drawing.Size(110, 24);
            this.BtnAddRental.TabIndex = 1;
            this.BtnAddRental.Text = "+ Rental";
            this.BtnAddRental.Click += new System.EventHandler(this.BtnAddRental_Click);
            //
            // BtnAddBK
            //
            this.BtnAddBK.Location = new System.Drawing.Point(278, 7);
            this.BtnAddBK.Name = "BtnAddBK";
            this.BtnAddBK.Size = new System.Drawing.Size(110, 24);
            this.BtnAddBK.TabIndex = 2;
            this.BtnAddBK.Text = "+ Black (BK)";
            this.BtnAddBK.Click += new System.EventHandler(this.BtnAddBK_Click);
            //
            // BtnAddCL
            //
            this.BtnAddCL.Location = new System.Drawing.Point(396, 7);
            this.BtnAddCL.Name = "BtnAddCL";
            this.BtnAddCL.Size = new System.Drawing.Size(110, 24);
            this.BtnAddCL.TabIndex = 3;
            this.BtnAddCL.Text = "+ Colour (CL)";
            this.BtnAddCL.Click += new System.EventHandler(this.BtnAddCL_Click);
            //
            // LblTake
            //
            this.LblTake.Location = new System.Drawing.Point(12, 42);
            this.LblTake.Name = "LblTake";
            this.LblTake.Size = new System.Drawing.Size(120, 13);
            this.LblTake.TabIndex = 4;
            this.LblTake.Text = "...or lose:";
            //
            // BtnDelRental
            //
            this.BtnDelRental.Location = new System.Drawing.Point(160, 37);
            this.BtnDelRental.Name = "BtnDelRental";
            this.BtnDelRental.Size = new System.Drawing.Size(110, 24);
            this.BtnDelRental.TabIndex = 5;
            this.BtnDelRental.Text = "- Rental";
            this.BtnDelRental.Click += new System.EventHandler(this.BtnDelRental_Click);
            //
            // BtnDelBK
            //
            this.BtnDelBK.Location = new System.Drawing.Point(278, 37);
            this.BtnDelBK.Name = "BtnDelBK";
            this.BtnDelBK.Size = new System.Drawing.Size(110, 24);
            this.BtnDelBK.TabIndex = 6;
            this.BtnDelBK.Text = "- Black (BK)";
            this.BtnDelBK.Click += new System.EventHandler(this.BtnDelBK_Click);
            //
            // BtnDelCL
            //
            this.BtnDelCL.Location = new System.Drawing.Point(396, 37);
            this.BtnDelCL.Name = "BtnDelCL";
            this.BtnDelCL.Size = new System.Drawing.Size(110, 24);
            this.BtnDelCL.TabIndex = 7;
            this.BtnDelCL.Text = "- Colour (CL)";
            this.BtnDelCL.Click += new System.EventHandler(this.BtnDelCL_Click);
            //
            // LblRate
            //
            this.LblRate.Location = new System.Drawing.Point(12, 76);
            this.LblRate.Name = "LblRate";
            this.LblRate.Size = new System.Drawing.Size(120, 13);
            this.LblRate.TabIndex = 8;
            this.LblRate.Text = "...or all charge this:";
            //
            // TxtRate
            //
            this.TxtRate.EditValue = new decimal(new int[] {0, 0, 0, 0});
            this.TxtRate.Location = new System.Drawing.Point(160, 72);
            this.TxtRate.Name = "TxtRate";
            this.TxtRate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.TxtRate.Properties.DisplayFormat.FormatString = "n4";
            this.TxtRate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.TxtRate.Properties.EditFormat.FormatString = "n4";
            this.TxtRate.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.TxtRate.Properties.Increment = new decimal(new int[] {1, 0, 0, 0});
            this.TxtRate.Properties.MaxValue = new decimal(new int[] {999999, 0, 0, 0});
            this.TxtRate.Properties.MinValue = new decimal(new int[] {0, 0, 0, 0});
            this.TxtRate.Size = new System.Drawing.Size(110, 20);
            this.TxtRate.TabIndex = 9;
            this.TxtRate.ToolTip = "The figure every ticked machine should charge — a monthly rent, or a per-copy rate";
            //
            // BtnRateRental
            //
            this.BtnRateRental.Location = new System.Drawing.Point(278, 71);
            this.BtnRateRental.Name = "BtnRateRental";
            this.BtnRateRental.Size = new System.Drawing.Size(110, 24);
            this.BtnRateRental.TabIndex = 10;
            this.BtnRateRental.Text = "set Rental";
            this.BtnRateRental.ToolTip = "Give every ticked machine this monthly rent";
            this.BtnRateRental.Click += new System.EventHandler(this.BtnRateRental_Click);
            //
            // BtnRateBK
            //
            this.BtnRateBK.Location = new System.Drawing.Point(396, 71);
            this.BtnRateBK.Name = "BtnRateBK";
            this.BtnRateBK.Size = new System.Drawing.Size(110, 24);
            this.BtnRateBK.TabIndex = 11;
            this.BtnRateBK.Text = "set BK rate";
            this.BtnRateBK.ToolTip = "Give every ticked machine this per-copy black rate";
            this.BtnRateBK.Click += new System.EventHandler(this.BtnRateBK_Click);
            //
            // BtnRateCL
            //
            this.BtnRateCL.Location = new System.Drawing.Point(514, 71);
            this.BtnRateCL.Name = "BtnRateCL";
            this.BtnRateCL.Size = new System.Drawing.Size(110, 24);
            this.BtnRateCL.TabIndex = 12;
            this.BtnRateCL.Text = "set CL rate";
            this.BtnRateCL.ToolTip = "Give every ticked machine this per-copy colour rate";
            this.BtnRateCL.Click += new System.EventHandler(this.BtnRateCL_Click);
            //
            // LblMin
            //
            this.LblMin.Location = new System.Drawing.Point(12, 110);
            this.LblMin.Name = "LblMin";
            this.LblMin.Size = new System.Drawing.Size(130, 13);
            this.LblMin.TabIndex = 13;
            this.LblMin.Text = "...or commit to a minimum:";
            //
            // TxtMinAmount
            //
            this.TxtMinAmount.EditValue = new decimal(new int[] {0, 0, 0, 0});
            this.TxtMinAmount.Location = new System.Drawing.Point(160, 106);
            this.TxtMinAmount.Name = "TxtMinAmount";
            this.TxtMinAmount.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.TxtMinAmount.Properties.DisplayFormat.FormatString = "n2";
            this.TxtMinAmount.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.TxtMinAmount.Properties.MaxValue = new decimal(new int[] {99999999, 0, 0, 0});
            this.TxtMinAmount.Properties.MinValue = new decimal(new int[] {0, 0, 0, 0});
            this.TxtMinAmount.Size = new System.Drawing.Size(110, 20);
            this.TxtMinAmount.TabIndex = 14;
            this.TxtMinAmount.ToolTip = "The committed amount a month";
            //
            // CmbMinScope
            //
            this.CmbMinScope.Location = new System.Drawing.Point(278, 106);
            this.CmbMinScope.Name = "CmbMinScope";
            this.CmbMinScope.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbMinScope.Size = new System.Drawing.Size(130, 20);
            this.CmbMinScope.TabIndex = 15;
            this.CmbMinScope.ToolTip = "Whose print charges it is measured against. A minimum shared by a group belongs on ONE machine of it.";
            //
            // BtnAddMin
            //
            this.BtnAddMin.Location = new System.Drawing.Point(416, 105);
            this.BtnAddMin.Name = "BtnAddMin";
            this.BtnAddMin.Size = new System.Drawing.Size(110, 24);
            this.BtnAddMin.TabIndex = 16;
            this.BtnAddMin.Text = "+ MIN";
            this.BtnAddMin.Click += new System.EventHandler(this.BtnAddMin_Click);
            //
            // BtnDelMin
            //
            this.BtnDelMin.Location = new System.Drawing.Point(534, 105);
            this.BtnDelMin.Name = "BtnDelMin";
            this.BtnDelMin.Size = new System.Drawing.Size(110, 24);
            this.BtnDelMin.TabIndex = 17;
            this.BtnDelMin.Text = "- MIN";
            this.BtnDelMin.Click += new System.EventHandler(this.BtnDelMin_Click);
            //
            // LblWaive
            //
            this.LblWaive.Location = new System.Drawing.Point(12, 144);
            this.LblWaive.Name = "LblWaive";
            this.LblWaive.Size = new System.Drawing.Size(130, 13);
            this.LblWaive.TabIndex = 18;
            this.LblWaive.Text = "...or a rental waive:";
            //
            // BtnSetWaive
            //
            this.BtnSetWaive.Location = new System.Drawing.Point(160, 139);
            this.BtnSetWaive.Name = "BtnSetWaive";
            this.BtnSetWaive.Size = new System.Drawing.Size(248, 24);
            this.BtnSetWaive.TabIndex = 19;
            this.BtnSetWaive.Text = "Set waive deal...";
            this.BtnSetWaive.ToolTip = "One deal, given to every ticked machine that has a rental to waive";
            this.BtnSetWaive.Click += new System.EventHandler(this.BtnSetWaive_Click);
            //
            // BtnDelWaive
            //
            this.BtnDelWaive.Location = new System.Drawing.Point(416, 139);
            this.BtnDelWaive.Name = "BtnDelWaive";
            this.BtnDelWaive.Size = new System.Drawing.Size(110, 24);
            this.BtnDelWaive.TabIndex = 20;
            this.BtnDelWaive.Text = "- Waive";
            this.BtnDelWaive.Click += new System.EventHandler(this.BtnDelWaive_Click);
            //
            // LblLadder
            //
            this.LblLadder.Location = new System.Drawing.Point(12, 178);
            this.LblLadder.Name = "LblLadder";
            this.LblLadder.Size = new System.Drawing.Size(130, 13);
            this.LblLadder.TabIndex = 21;
            this.LblLadder.Text = "...or tiered pricing:";
            //
            // CmbLadder
            //
            this.CmbLadder.Location = new System.Drawing.Point(160, 174);
            this.CmbLadder.Name = "CmbLadder";
            this.CmbLadder.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbLadder.Size = new System.Drawing.Size(248, 20);
            this.CmbLadder.TabIndex = 22;
            this.CmbLadder.ToolTip = "A Meter Multi Pricing scheme — free band plus per-copy bands";
            //
            // BtnLadderBK
            //
            this.BtnLadderBK.Location = new System.Drawing.Point(416, 173);
            this.BtnLadderBK.Name = "BtnLadderBK";
            this.BtnLadderBK.Size = new System.Drawing.Size(110, 24);
            this.BtnLadderBK.TabIndex = 23;
            this.BtnLadderBK.Text = "set BK tiers";
            this.BtnLadderBK.Click += new System.EventHandler(this.BtnLadderBK_Click);
            //
            // BtnLadderCL
            //
            this.BtnLadderCL.Location = new System.Drawing.Point(534, 173);
            this.BtnLadderCL.Name = "BtnLadderCL";
            this.BtnLadderCL.Size = new System.Drawing.Size(110, 24);
            this.BtnLadderCL.TabIndex = 24;
            this.BtnLadderCL.Text = "set CL tiers";
            this.BtnLadderCL.Click += new System.EventHandler(this.BtnLadderCL_Click);
            //
            // BtnLadderClear
            //
            this.BtnLadderClear.Location = new System.Drawing.Point(652, 173);
            this.BtnLadderClear.Name = "BtnLadderClear";
            this.BtnLadderClear.Size = new System.Drawing.Size(150, 24);
            this.BtnLadderClear.TabIndex = 25;
            this.BtnLadderClear.Text = "back to a flat rate";
            this.BtnLadderClear.ToolTip = "Take the tiers off — the per-copy rate applies again";
            this.BtnLadderClear.Click += new System.EventHandler(this.BtnLadderClear_Click);
            //
            // LblSummary
            //
            this.LblSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblSummary.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(12, 601);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(1080, 18);
            this.LblSummary.TabIndex = 3;
            this.LblSummary.Text = "";
            //
            // PanelBottom
            //
            this.PanelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelBottom.Controls.Add(this.BtnSelectAll);
            this.PanelBottom.Controls.Add(this.BtnSameModel);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Location = new System.Drawing.Point(12, 625);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(1080, 44);
            this.PanelBottom.TabIndex = 4;
            //
            // BtnSelectAll
            //
            this.BtnSelectAll.Location = new System.Drawing.Point(9, 9);
            this.BtnSelectAll.Name = "BtnSelectAll";
            this.BtnSelectAll.Size = new System.Drawing.Size(130, 24);
            this.BtnSelectAll.TabIndex = 0;
            this.BtnSelectAll.Text = "Select all / none";
            this.BtnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            //
            // BtnSameModel
            //
            this.BtnSameModel.Location = new System.Drawing.Point(147, 9);
            this.BtnSameModel.Name = "BtnSameModel";
            this.BtnSameModel.Size = new System.Drawing.Size(140, 24);
            this.BtnSameModel.TabIndex = 1;
            this.BtnSameModel.Text = "Tick same model";
            this.BtnSameModel.ToolTip = "Tick every machine of the same model as the one you clicked — one model is usually one deal";
            this.BtnSameModel.Click += new System.EventHandler(this.BtnSameModel_Click);
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.ImageOptions.ImageUri.Uri = "Save;Size16x16";
            this.BtnOK.Location = new System.Drawing.Point(894, 9);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 24);
            this.BtnOK.TabIndex = 2;
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(985, 9);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 24);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            //
            // MachineMeters_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(1104, 681);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.GridMachines);
            this.Controls.Add(this.PanelActions);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1000, 640);
            this.Name = "MachineMeters_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meters — which machines have rent, black and colour, and what they charge";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbLadder.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMinScope.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinAmount.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRate.Properties)).EndInit();
            this.PanelActions.ResumeLayout(false);
            this.PanelActions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoScope)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoState)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColModel;
        private DevExpress.XtraGrid.Columns.GridColumn ColRental;
        private DevExpress.XtraGrid.Columns.GridColumn ColRentalRate;
        private DevExpress.XtraGrid.Columns.GridColumn ColBK;
        private DevExpress.XtraGrid.Columns.GridColumn ColBKRate;
        private DevExpress.XtraGrid.Columns.GridColumn ColCL;
        private DevExpress.XtraGrid.Columns.GridColumn ColCLRate;
        private DevExpress.XtraGrid.Columns.GridColumn ColBKLadder;
        private DevExpress.XtraGrid.Columns.GridColumn ColCLLadder;
        private DevExpress.XtraGrid.Columns.GridColumn ColMin;
        private DevExpress.XtraGrid.Columns.GridColumn ColMinAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColMinScope;
        private DevExpress.XtraGrid.Columns.GridColumn ColWaive;
        private DevExpress.XtraGrid.Columns.GridColumn ColWaiveDeal;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox RepoScope;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoState;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoMoney;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoRate;
        private DevExpress.XtraEditors.PanelControl PanelActions;
        private DevExpress.XtraEditors.LabelControl LblGive;
        private DevExpress.XtraEditors.SimpleButton BtnAddRental;
        private DevExpress.XtraEditors.SimpleButton BtnAddBK;
        private DevExpress.XtraEditors.SimpleButton BtnAddCL;
        private DevExpress.XtraEditors.LabelControl LblTake;
        private DevExpress.XtraEditors.SimpleButton BtnDelRental;
        private DevExpress.XtraEditors.SimpleButton BtnDelBK;
        private DevExpress.XtraEditors.SimpleButton BtnDelCL;
        private DevExpress.XtraEditors.LabelControl LblRate;
        private DevExpress.XtraEditors.SpinEdit TxtRate;
        private DevExpress.XtraEditors.SimpleButton BtnRateRental;
        private DevExpress.XtraEditors.SimpleButton BtnRateBK;
        private DevExpress.XtraEditors.SimpleButton BtnRateCL;
        private DevExpress.XtraEditors.LabelControl LblMin;
        private DevExpress.XtraEditors.SpinEdit TxtMinAmount;
        private DevExpress.XtraEditors.ComboBoxEdit CmbMinScope;
        private DevExpress.XtraEditors.SimpleButton BtnAddMin;
        private DevExpress.XtraEditors.SimpleButton BtnDelMin;
        private DevExpress.XtraEditors.LabelControl LblWaive;
        private DevExpress.XtraEditors.SimpleButton BtnSetWaive;
        private DevExpress.XtraEditors.SimpleButton BtnDelWaive;
        private DevExpress.XtraEditors.LabelControl LblLadder;
        private DevExpress.XtraEditors.ComboBoxEdit CmbLadder;
        private DevExpress.XtraEditors.SimpleButton BtnLadderBK;
        private DevExpress.XtraEditors.SimpleButton BtnLadderCL;
        private DevExpress.XtraEditors.SimpleButton BtnLadderClear;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnSelectAll;
        private DevExpress.XtraEditors.SimpleButton BtnSameModel;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
