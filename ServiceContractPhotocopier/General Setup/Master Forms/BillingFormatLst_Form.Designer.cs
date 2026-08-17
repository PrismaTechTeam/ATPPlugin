namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class BillingFormatLst_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridFormats;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewFormats;
        private DevExpress.XtraEditors.SimpleButton BtnNew;
        private DevExpress.XtraEditors.SimpleButton BtnEdit;
        private DevExpress.XtraEditors.SimpleButton BtnCopy;
        private DevExpress.XtraEditors.SimpleButton BtnDelete;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnClose;

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
            this.GridFormats = new DevExpress.XtraGrid.GridControl();
            this.GridViewFormats = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.BtnNew = new DevExpress.XtraEditors.SimpleButton();
            this.BtnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCopy = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).BeginInit();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(14, 14);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(930, 18);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "A billing format decides how many invoices a contract produces and how its lines are grouped. " +
                "Double-click one to edit it and see a sample invoice.";
            //
            // GridFormats
            //
            this.GridFormats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GridFormats.Location = new System.Drawing.Point(14, 38);
            this.GridFormats.MainView = this.GridViewFormats;
            this.GridFormats.Name = "GridFormats";
            this.GridFormats.Size = new System.Drawing.Size(930, 390);
            this.GridFormats.TabIndex = 1;
            this.GridFormats.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewFormats});
            //
            // GridViewFormats
            //
            this.GridViewFormats.GridControl = this.GridFormats;
            this.GridViewFormats.Name = "GridViewFormats";
            this.GridViewFormats.OptionsBehavior.Editable = false;
            this.GridViewFormats.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewFormats.OptionsView.ShowGroupPanel = false;
            //
            // BtnNew
            //
            this.BtnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnNew.Location = new System.Drawing.Point(14, 440);
            this.BtnNew.Name = "BtnNew";
            this.BtnNew.Size = new System.Drawing.Size(90, 30);
            this.BtnNew.TabIndex = 2;
            this.BtnNew.Text = "New";
            this.BtnNew.Click += new System.EventHandler(this.BtnNew_Click);
            //
            // BtnEdit
            //
            this.BtnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnEdit.Location = new System.Drawing.Point(110, 440);
            this.BtnEdit.Name = "BtnEdit";
            this.BtnEdit.Size = new System.Drawing.Size(90, 30);
            this.BtnEdit.TabIndex = 3;
            this.BtnEdit.Text = "Edit";
            this.BtnEdit.Click += new System.EventHandler(this.BtnEdit_Click);
            //
            // BtnCopy
            //
            this.BtnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnCopy.Location = new System.Drawing.Point(206, 440);
            this.BtnCopy.Name = "BtnCopy";
            this.BtnCopy.Size = new System.Drawing.Size(90, 30);
            this.BtnCopy.TabIndex = 4;
            this.BtnCopy.Text = "Copy";
            this.BtnCopy.Click += new System.EventHandler(this.BtnCopy_Click);
            //
            // BtnDelete
            //
            this.BtnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnDelete.Location = new System.Drawing.Point(302, 440);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(90, 30);
            this.BtnDelete.TabIndex = 5;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            //
            // BtnRefresh
            //
            this.BtnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnRefresh.Location = new System.Drawing.Point(398, 440);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(90, 30);
            this.BtnRefresh.TabIndex = 6;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnClose.Location = new System.Drawing.Point(854, 440);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(90, 30);
            this.BtnClose.TabIndex = 7;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // BillingFormatLst_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnClose;
            this.ClientSize = new System.Drawing.Size(958, 484);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.GridFormats);
            this.Controls.Add(this.BtnNew);
            this.Controls.Add(this.BtnEdit);
            this.Controls.Add(this.BtnCopy);
            this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.BtnRefresh);
            this.Controls.Add(this.BtnClose);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(760, 400);
            this.Name = "BillingFormatLst_Form";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Billing Format";
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
