namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class BulkEmailJob_Form
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
            this.components = new System.ComponentModel.Container();
            this.PanelTop = new DevExpress.XtraEditors.PanelControl();
            this.LblHeadline = new DevExpress.XtraEditors.LabelControl();
            this.LblSub = new DevExpress.XtraEditors.LabelControl();
            this.PanelStatus = new DevExpress.XtraEditors.PanelControl();
            this.LblBig = new DevExpress.XtraEditors.LabelControl();
            this.LblCounts = new DevExpress.XtraEditors.LabelControl();
            this.Marquee = new DevExpress.XtraEditors.MarqueeProgressBarControl();
            this.GridItems = new DevExpress.XtraGrid.GridControl();
            this.GridViewItems = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnConfirm = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            this.Ticker = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).BeginInit();
            this.PanelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Marquee.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelTop
            //
            this.PanelTop.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(249)))), ((int)(((byte)(196)))));
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.PanelTop.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTop.Controls.Add(this.LblHeadline);
            this.PanelTop.Controls.Add(this.LblSub);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(940, 84);
            this.PanelTop.TabIndex = 0;
            //
            // LblHeadline
            //
            this.LblHeadline.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.LblHeadline.Appearance.Options.UseFont = true;
            this.LblHeadline.Location = new System.Drawing.Point(16, 12);
            this.LblHeadline.Name = "LblHeadline";
            this.LblHeadline.Size = new System.Drawing.Size(400, 20);
            this.LblHeadline.TabIndex = 0;
            this.LblHeadline.Text = "Confirm the invoices to be emailed";
            //
            // LblSub
            //
            this.LblSub.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.LblSub.Appearance.Options.UseForeColor = true;
            this.LblSub.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblSub.Appearance.Options.UseTextOptions = true;
            this.LblSub.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSub.Location = new System.Drawing.Point(16, 36);
            this.LblSub.Name = "LblSub";
            this.LblSub.Size = new System.Drawing.Size(908, 40);
            this.LblSub.TabIndex = 1;
            this.LblSub.Text = "One email per customer, with that customer\'s invoices attached.";
            //
            // PanelStatus
            //
            this.PanelStatus.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelStatus.Controls.Add(this.LblBig);
            this.PanelStatus.Controls.Add(this.LblCounts);
            this.PanelStatus.Controls.Add(this.Marquee);
            this.PanelStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelStatus.Location = new System.Drawing.Point(0, 84);
            this.PanelStatus.Name = "PanelStatus";
            this.PanelStatus.Size = new System.Drawing.Size(940, 78);
            this.PanelStatus.TabIndex = 1;
            this.PanelStatus.Visible = false;
            //
            // LblBig
            //
            this.LblBig.Appearance.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.LblBig.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblBig.Appearance.Options.UseFont = true;
            this.LblBig.Appearance.Options.UseForeColor = true;
            this.LblBig.Location = new System.Drawing.Point(16, 10);
            this.LblBig.Name = "LblBig";
            this.LblBig.Size = new System.Drawing.Size(500, 30);
            this.LblBig.TabIndex = 0;
            this.LblBig.Text = "Sending email...";
            //
            // LblCounts
            //
            this.LblCounts.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.LblCounts.Appearance.Options.UseFont = true;
            this.LblCounts.Location = new System.Drawing.Point(18, 46);
            this.LblCounts.Name = "LblCounts";
            this.LblCounts.Size = new System.Drawing.Size(600, 18);
            this.LblCounts.TabIndex = 1;
            //
            // Marquee
            //
            this.Marquee.EditValue = null;
            this.Marquee.Location = new System.Drawing.Point(640, 18);
            this.Marquee.Name = "Marquee";
            this.Marquee.Properties.EndColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.Marquee.Properties.StartColor = System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(214)))), ((int)(((byte)(167)))));
            this.Marquee.Size = new System.Drawing.Size(280, 20);
            this.Marquee.TabIndex = 2;
            //
            // GridItems
            //
            this.GridItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridItems.Location = new System.Drawing.Point(0, 162);
            this.GridItems.MainView = this.GridViewItems;
            this.GridItems.Name = "GridItems";
            this.GridItems.Size = new System.Drawing.Size(940, 382);
            this.GridItems.TabIndex = 2;
            this.GridItems.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewItems});
            //
            // GridViewItems
            //
            this.GridViewItems.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewItems.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewItems.GridControl = this.GridItems;
            this.GridViewItems.Name = "GridViewItems";
            this.GridViewItems.OptionsBehavior.Editable = false;
            this.GridViewItems.OptionsBehavior.AutoExpandAllGroups = true;
            this.GridViewItems.OptionsView.ColumnAutoWidth = false;
            this.GridViewItems.OptionsView.ShowGroupPanel = false;
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.BtnConfirm);
            this.PanelBottom.Controls.Add(this.BtnClose);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 544);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(940, 56);
            this.PanelBottom.TabIndex = 3;
            //
            // BtnConfirm
            //
            this.BtnConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnConfirm.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnConfirm.Appearance.Options.UseFont = true;
            this.BtnConfirm.Location = new System.Drawing.Point(690, 12);
            this.BtnConfirm.Name = "BtnConfirm";
            this.BtnConfirm.Size = new System.Drawing.Size(130, 32);
            this.BtnConfirm.TabIndex = 0;
            this.BtnConfirm.Text = "Confirm && Send";
            this.BtnConfirm.Click += new System.EventHandler(this.BtnConfirm_Click);
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Location = new System.Drawing.Point(828, 12);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(100, 32);
            this.BtnClose.TabIndex = 1;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // Ticker
            //
            this.Ticker.Interval = 1000;
            this.Ticker.Tick += new System.EventHandler(this.Ticker_Tick);
            //
            // BulkEmailJob_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(940, 600);
            this.Controls.Add(this.GridItems);
            this.Controls.Add(this.PanelBottom);
            this.Controls.Add(this.PanelStatus);
            this.Controls.Add(this.PanelTop);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(760, 460);
            this.Name = "BulkEmailJob_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Send Invoices by Email";
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Marquee.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).EndInit();
            this.PanelStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblHeadline;
        private DevExpress.XtraEditors.LabelControl LblSub;
        private DevExpress.XtraEditors.PanelControl PanelStatus;
        private DevExpress.XtraEditors.LabelControl LblBig;
        private DevExpress.XtraEditors.LabelControl LblCounts;
        private DevExpress.XtraEditors.MarqueeProgressBarControl Marquee;
        private DevExpress.XtraGrid.GridControl GridItems;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewItems;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnConfirm;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
        private System.Windows.Forms.Timer Ticker;
    }
}
