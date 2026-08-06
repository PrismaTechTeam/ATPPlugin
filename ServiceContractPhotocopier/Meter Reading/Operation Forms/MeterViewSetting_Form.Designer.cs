namespace ServiceContractPhotocopier
{
    partial class MeterViewSetting_Form
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
            this.ListCols = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnShowAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnHideAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.ListCols)).BeginInit();
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
            this.LblHint.Size = new System.Drawing.Size(340, 44);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Tick the columns you want to see. The working columns (Select, Current Reading, Me" +
    "ter Usage, Total Charges, prices) always stay on - they follow the tab you are on" +
    ". Your choice is remembered for you the next time you open this screen.";
            //
            // ListCols
            //
            this.ListCols.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ListCols.CheckOnClick = true;
            this.ListCols.Location = new System.Drawing.Point(12, 59);
            this.ListCols.Name = "ListCols";
            this.ListCols.Size = new System.Drawing.Size(340, 380);
            this.ListCols.TabIndex = 1;
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.BtnShowAll);
            this.PanelBottom.Controls.Add(this.BtnHideAll);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 449);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(364, 80);
            this.PanelBottom.TabIndex = 2;
            //
            // BtnShowAll
            //
            this.BtnShowAll.Location = new System.Drawing.Point(12, 9);
            this.BtnShowAll.Name = "BtnShowAll";
            this.BtnShowAll.Size = new System.Drawing.Size(85, 26);
            this.BtnShowAll.TabIndex = 0;
            this.BtnShowAll.Text = "Show All";
            this.BtnShowAll.Click += new System.EventHandler(this.BtnShowAll_Click);
            //
            // BtnHideAll
            //
            this.BtnHideAll.Location = new System.Drawing.Point(103, 9);
            this.BtnHideAll.Name = "BtnHideAll";
            this.BtnHideAll.Size = new System.Drawing.Size(85, 26);
            this.BtnHideAll.TabIndex = 1;
            this.BtnHideAll.Text = "Hide All";
            this.BtnHideAll.Click += new System.EventHandler(this.BtnHideAll_Click);
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.ImageOptions.ImageUri.Uri = "Apply;Size16x16";
            this.BtnOK.Location = new System.Drawing.Point(178, 45);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 26);
            this.BtnOK.TabIndex = 2;
            this.BtnOK.Text = "OK";
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(269, 45);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 26);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            //
            // MeterViewSetting_Form
            //
            this.AcceptButton = this.BtnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(364, 529);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.ListCols);
            this.Controls.Add(this.PanelBottom);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(320, 400);
            this.Name = "MeterViewSetting_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "View Setting - choose the columns to show";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ListCols)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.CheckedListBoxControl ListCols;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnShowAll;
        private DevExpress.XtraEditors.SimpleButton BtnHideAll;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
