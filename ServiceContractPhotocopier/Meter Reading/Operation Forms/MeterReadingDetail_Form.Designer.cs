namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterReadingDetail_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblTitle;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridDetail;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewDetail;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoCheck;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnOk;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.SimpleButton BtnAcceptAll;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTop = new DevExpress.XtraEditors.PanelControl();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.LblTitle = new DevExpress.XtraEditors.LabelControl();
            this.GridDetail = new DevExpress.XtraGrid.GridControl();
            this.GridViewDetail = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.RepoCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnAcceptAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridDetail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewDetail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoCheck)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelTop
            //
            this.PanelTop.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.PanelTop.Controls.Add(this.LblHint);
            this.PanelTop.Controls.Add(this.LblTitle);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(1000, 62);
            this.PanelTop.TabIndex = 0;
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Location = new System.Drawing.Point(16, 38);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(560, 13);
            this.LblHint.TabIndex = 1;
            this.LblHint.Text = "Tick \'Accept fetched (override)\' to replace the saved / manual reading with the freshly-fetched API value.";
            //
            // LblTitle
            //
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(14, 12);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(80, 21);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Contract";
            //
            // GridDetail
            //
            this.GridDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridDetail.Location = new System.Drawing.Point(0, 62);
            this.GridDetail.MainView = this.GridViewDetail;
            this.GridDetail.Name = "GridDetail";
            this.GridDetail.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoCheck});
            this.GridDetail.Size = new System.Drawing.Size(1000, 478);
            this.GridDetail.TabIndex = 1;
            this.GridDetail.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewDetail});
            //
            // GridViewDetail
            //
            this.GridViewDetail.GridControl = this.GridDetail;
            this.GridViewDetail.Name = "GridViewDetail";
            this.GridViewDetail.OptionsView.ShowGroupPanel = false;
            //
            // RepoCheck
            //
            this.RepoCheck.AutoHeight = false;
            this.RepoCheck.Name = "RepoCheck";
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.BtnAcceptAll);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Controls.Add(this.BtnOk);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 540);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(1000, 60);
            this.PanelBottom.TabIndex = 2;
            //
            // BtnAcceptAll
            //
            this.BtnAcceptAll.Location = new System.Drawing.Point(16, 12);
            this.BtnAcceptAll.Name = "BtnAcceptAll";
            this.BtnAcceptAll.Size = new System.Drawing.Size(180, 36);
            this.BtnAcceptAll.TabIndex = 0;
            this.BtnAcceptAll.Text = "Use API for All";
            this.BtnAcceptAll.Click += new System.EventHandler(this.BtnAcceptAll_Click);
            //
            // BtnOk
            //
            this.BtnOk.Location = new System.Drawing.Point(740, 12);
            this.BtnOk.Name = "BtnOk";
            this.BtnOk.Size = new System.Drawing.Size(120, 36);
            this.BtnOk.TabIndex = 1;
            this.BtnOk.Text = "OK";
            this.BtnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(866, 12);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(120, 36);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // MeterReadingDetail_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.GridDetail);
            this.Controls.Add(this.PanelBottom);
            this.Controls.Add(this.PanelTop);
            this.Name = "MeterReadingDetail_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meter Reading — Contract Detail";
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            this.PanelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridDetail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewDetail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoCheck)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
