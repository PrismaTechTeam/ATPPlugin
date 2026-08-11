namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class ReportDesignList_Form
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
            this.LblPrompt = new DevExpress.XtraEditors.LabelControl();
            this.GridDesigns = new DevExpress.XtraGrid.GridControl();
            this.GridViewDesigns = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColReportName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColKind = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDefault = new DevExpress.XtraGrid.Columns.GridColumn();
            this.BtnNew = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDesign = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRename = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSetDefault = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.BtnUse = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            this.LblFoot = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.GridDesigns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewDesigns)).BeginInit();
            this.SuspendLayout();
            //
            // LblPrompt
            //
            this.LblPrompt.Location = new System.Drawing.Point(12, 12);
            this.LblPrompt.Name = "LblPrompt";
            this.LblPrompt.Size = new System.Drawing.Size(784, 16);
            this.LblPrompt.TabIndex = 0;
            this.LblPrompt.Text = "Report designs";
            //
            // GridDesigns
            //
            this.GridDesigns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridDesigns.Location = new System.Drawing.Point(12, 34);
            this.GridDesigns.MainView = this.GridViewDesigns;
            this.GridDesigns.Name = "GridDesigns";
            this.GridDesigns.Size = new System.Drawing.Size(784, 320);
            this.GridDesigns.TabIndex = 1;
            this.GridDesigns.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewDesigns});
            //
            // GridViewDesigns
            //
            this.GridViewDesigns.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewDesigns.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewDesigns.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColReportName,
            this.ColKind,
            this.ColDefault});
            this.GridViewDesigns.GridControl = this.GridDesigns;
            this.GridViewDesigns.Name = "GridViewDesigns";
            this.GridViewDesigns.OptionsBehavior.Editable = false;
            this.GridViewDesigns.OptionsView.ColumnAutoWidth = true;
            this.GridViewDesigns.OptionsView.ShowAutoFilterRow = true;
            this.GridViewDesigns.OptionsView.ShowGroupPanel = false;
            this.GridViewDesigns.DoubleClick += new System.EventHandler(this.GridViewDesigns_DoubleClick);
            //
            // ColReportName
            //
            this.ColReportName.Caption = "Report Design";
            this.ColReportName.FieldName = "ReportName";
            this.ColReportName.Name = "ColReportName";
            this.ColReportName.Visible = true;
            this.ColReportName.VisibleIndex = 0;
            this.ColReportName.Width = 460;
            //
            // ColKind
            //
            this.ColKind.Caption = "Kind";
            this.ColKind.FieldName = "Kind";
            this.ColKind.Name = "ColKind";
            this.ColKind.Visible = true;
            this.ColKind.VisibleIndex = 1;
            this.ColKind.Width = 110;
            //
            // ColDefault
            //
            this.ColDefault.Caption = "Default";
            this.ColDefault.FieldName = "Default";
            this.ColDefault.Name = "ColDefault";
            this.ColDefault.Visible = true;
            this.ColDefault.VisibleIndex = 2;
            this.ColDefault.Width = 110;
            //
            // BtnNew
            //
            this.BtnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnNew.Location = new System.Drawing.Point(12, 364);
            this.BtnNew.Name = "BtnNew";
            this.BtnNew.Size = new System.Drawing.Size(104, 30);
            this.BtnNew.TabIndex = 2;
            this.BtnNew.Text = "New";
            this.BtnNew.Click += new System.EventHandler(this.BtnNew_Click);
            //
            // BtnDesign
            //
            this.BtnDesign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnDesign.Location = new System.Drawing.Point(122, 364);
            this.BtnDesign.Name = "BtnDesign";
            this.BtnDesign.Size = new System.Drawing.Size(104, 30);
            this.BtnDesign.TabIndex = 3;
            this.BtnDesign.Text = "Design";
            this.BtnDesign.Click += new System.EventHandler(this.BtnDesign_Click);
            //
            // BtnRename
            //
            this.BtnRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnRename.Location = new System.Drawing.Point(232, 364);
            this.BtnRename.Name = "BtnRename";
            this.BtnRename.Size = new System.Drawing.Size(104, 30);
            this.BtnRename.TabIndex = 4;
            this.BtnRename.Text = "Rename";
            this.BtnRename.Click += new System.EventHandler(this.BtnRename_Click);
            //
            // BtnSetDefault
            //
            this.BtnSetDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnSetDefault.Location = new System.Drawing.Point(342, 364);
            this.BtnSetDefault.Name = "BtnSetDefault";
            this.BtnSetDefault.Size = new System.Drawing.Size(120, 30);
            this.BtnSetDefault.TabIndex = 5;
            this.BtnSetDefault.Text = "Set as Default";
            this.BtnSetDefault.Click += new System.EventHandler(this.BtnSetDefault_Click);
            //
            // BtnDelete
            //
            this.BtnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnDelete.Location = new System.Drawing.Point(468, 364);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(104, 30);
            this.BtnDelete.TabIndex = 6;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            //
            // BtnUse
            //
            this.BtnUse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnUse.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnUse.Appearance.Options.UseFont = true;
            this.BtnUse.Location = new System.Drawing.Point(580, 364);
            this.BtnUse.Name = "BtnUse";
            this.BtnUse.Size = new System.Drawing.Size(104, 30);
            this.BtnUse.TabIndex = 7;
            this.BtnUse.Text = "Use Selected";
            this.BtnUse.Visible = false;
            this.BtnUse.Click += new System.EventHandler(this.BtnUse_Click);
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Location = new System.Drawing.Point(692, 364);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(104, 30);
            this.BtnClose.TabIndex = 8;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // LblFoot
            //
            this.LblFoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LblFoot.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.LblFoot.Appearance.Options.UseForeColor = true;
            this.LblFoot.Location = new System.Drawing.Point(12, 402);
            this.LblFoot.Name = "LblFoot";
            this.LblFoot.Size = new System.Drawing.Size(784, 16);
            this.LblFoot.TabIndex = 9;
            this.LblFoot.Text = "New starts an A4 landscape layout with today\'s columns already laid out. Save inside the designer to name it.";
            //
            // ReportDesignList_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(808, 428);
            this.Controls.Add(this.LblFoot);
            this.Controls.Add(this.BtnClose);
            this.Controls.Add(this.BtnUse);
            this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.BtnSetDefault);
            this.Controls.Add(this.BtnRename);
            this.Controls.Add(this.BtnDesign);
            this.Controls.Add(this.BtnNew);
            this.Controls.Add(this.GridDesigns);
            this.Controls.Add(this.LblPrompt);
            this.MinimizeBox = false;
            this.Name = "ReportDesignList_Form";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Report Design";
            ((System.ComponentModel.ISupportInitialize)(this.GridViewDesigns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridDesigns)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblPrompt;
        private DevExpress.XtraGrid.GridControl GridDesigns;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewDesigns;
        private DevExpress.XtraGrid.Columns.GridColumn ColReportName;
        private DevExpress.XtraGrid.Columns.GridColumn ColKind;
        private DevExpress.XtraGrid.Columns.GridColumn ColDefault;
        private DevExpress.XtraEditors.SimpleButton BtnNew;
        private DevExpress.XtraEditors.SimpleButton BtnDesign;
        private DevExpress.XtraEditors.SimpleButton BtnRename;
        private DevExpress.XtraEditors.SimpleButton BtnSetDefault;
        private DevExpress.XtraEditors.SimpleButton BtnDelete;
        private DevExpress.XtraEditors.SimpleButton BtnUse;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
        private DevExpress.XtraEditors.LabelControl LblFoot;
    }
}
