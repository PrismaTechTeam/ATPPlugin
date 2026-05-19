namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    partial class StockRequestLog_Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private DevExpress.XtraEditors.PanelControl PanelHeader;
        private DevExpress.XtraEditors.LabelControl LblLogType;
        private DevExpress.XtraEditors.ComboBoxEdit CmbLogType;
        private DevExpress.XtraEditors.LabelControl LblFrom;
        private DevExpress.XtraEditors.DateEdit DtFrom;
        private DevExpress.XtraEditors.LabelControl LblTo;
        private DevExpress.XtraEditors.DateEdit DtTo;
        private DevExpress.XtraEditors.SimpleButton BtnReload;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
        private DevExpress.XtraGrid.GridControl GridLog;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewLog;

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.PanelHeader = new DevExpress.XtraEditors.PanelControl();
            this.LblLogType  = new DevExpress.XtraEditors.LabelControl();
            this.CmbLogType  = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblFrom     = new DevExpress.XtraEditors.LabelControl();
            this.DtFrom      = new DevExpress.XtraEditors.DateEdit();
            this.LblTo       = new DevExpress.XtraEditors.LabelControl();
            this.DtTo        = new DevExpress.XtraEditors.DateEdit();
            this.BtnReload   = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose    = new DevExpress.XtraEditors.SimpleButton();
            this.GridLog     = new DevExpress.XtraGrid.GridControl();
            this.GridViewLog = new DevExpress.XtraGrid.Views.Grid.GridView();

            ((System.ComponentModel.ISupportInitialize)(this.PanelHeader)).BeginInit();
            this.PanelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CmbLogType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLog)).BeginInit();
            this.SuspendLayout();

            // PanelHeader
            this.PanelHeader.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelHeader.Controls.Add(this.LblLogType);
            this.PanelHeader.Controls.Add(this.CmbLogType);
            this.PanelHeader.Controls.Add(this.LblFrom);
            this.PanelHeader.Controls.Add(this.DtFrom);
            this.PanelHeader.Controls.Add(this.LblTo);
            this.PanelHeader.Controls.Add(this.DtTo);
            this.PanelHeader.Controls.Add(this.BtnReload);
            this.PanelHeader.Controls.Add(this.BtnClose);
            this.PanelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeader.Location = new System.Drawing.Point(0, 0);
            this.PanelHeader.Name = "PanelHeader";
            this.PanelHeader.Size = new System.Drawing.Size(1000, 56);
            this.PanelHeader.TabIndex = 0;

            // LblLogType
            this.LblLogType.Location = new System.Drawing.Point(12, 18);
            this.LblLogType.Name = "LblLogType";
            this.LblLogType.Size = new System.Drawing.Size(55, 15);
            this.LblLogType.Text = "Log Type:";
            // CmbLogType
            this.CmbLogType.Location = new System.Drawing.Point(76, 14);
            this.CmbLogType.Name = "CmbLogType";
            this.CmbLogType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbLogType.Size = new System.Drawing.Size(130, 22);
            this.CmbLogType.TabIndex = 1;

            // LblFrom
            this.LblFrom.Location = new System.Drawing.Point(228, 18);
            this.LblFrom.Name = "LblFrom";
            this.LblFrom.Size = new System.Drawing.Size(32, 15);
            this.LblFrom.Text = "From:";
            // DtFrom
            this.DtFrom.EditValue = null;
            this.DtFrom.Location = new System.Drawing.Point(266, 14);
            this.DtFrom.Name = "DtFrom";
            this.DtFrom.Properties.CalendarTimeProperties.DisplayFormat.FormatString = "HH:mm";
            this.DtFrom.Properties.CalendarTimeProperties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.CalendarTimeProperties.EditFormat.FormatString = "HH:mm";
            this.DtFrom.Properties.CalendarTimeProperties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtFrom.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtFrom.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Size = new System.Drawing.Size(120, 22);
            this.DtFrom.TabIndex = 2;

            // LblTo
            this.LblTo.Location = new System.Drawing.Point(394, 18);
            this.LblTo.Name = "LblTo";
            this.LblTo.Size = new System.Drawing.Size(16, 15);
            this.LblTo.Text = "To:";
            // DtTo
            this.DtTo.EditValue = null;
            this.DtTo.Location = new System.Drawing.Point(416, 14);
            this.DtTo.Name = "DtTo";
            this.DtTo.Properties.CalendarTimeProperties.DisplayFormat.FormatString = "HH:mm";
            this.DtTo.Properties.CalendarTimeProperties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.CalendarTimeProperties.EditFormat.FormatString = "HH:mm";
            this.DtTo.Properties.CalendarTimeProperties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtTo.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtTo.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Size = new System.Drawing.Size(120, 22);
            this.DtTo.TabIndex = 3;

            // BtnReload
            this.BtnReload.Location = new System.Drawing.Point(548, 12);
            this.BtnReload.Name = "BtnReload";
            this.BtnReload.Size = new System.Drawing.Size(90, 28);
            this.BtnReload.Text = "Reload";
            this.BtnReload.Click += new System.EventHandler(this.BtnReload_Click);
            // BtnClose
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Location = new System.Drawing.Point(900, 12);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(85, 28);
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);

            // GridLog (fills the area below the header panel)
            this.GridLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridLog.Location = new System.Drawing.Point(0, 56);
            this.GridLog.MainView = this.GridViewLog;
            this.GridLog.Name = "GridLog";
            this.GridLog.Size = new System.Drawing.Size(1000, 544);
            this.GridLog.TabIndex = 1;
            this.GridLog.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { this.GridViewLog });
            // GridViewLog
            this.GridViewLog.GridControl = this.GridLog;
            this.GridViewLog.Name = "GridViewLog";
            this.GridViewLog.OptionsView.ShowGroupPanel = false;
            this.GridViewLog.OptionsBehavior.Editable = false;

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.GridLog);
            this.Controls.Add(this.PanelHeader);
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.Name = "StockRequestLog_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "View Log";

            ((System.ComponentModel.ISupportInitialize)(this.PanelHeader)).EndInit();
            this.PanelHeader.ResumeLayout(false);
            this.PanelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CmbLogType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLog)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}
