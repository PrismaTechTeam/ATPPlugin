namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterInvoicePreview_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private DevExpress.XtraEditors.LabelControl LblHeader;
        private DevExpress.XtraEditors.LabelControl LblNote;
        private DevExpress.XtraGrid.GridControl GridPreview;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewPreview;
        private DevExpress.XtraEditors.SimpleButton BtnCreate;
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
            this.LblHeader = new DevExpress.XtraEditors.LabelControl();
            this.LblNote = new DevExpress.XtraEditors.LabelControl();
            this.GridPreview = new DevExpress.XtraGrid.GridControl();
            this.GridViewPreview = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.BtnCreate = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).BeginInit();
            this.SuspendLayout();
            //
            // LblHeader
            //
            this.LblHeader.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.LblHeader.Location = new System.Drawing.Point(14, 14);
            this.LblHeader.Name = "LblHeader";
            this.LblHeader.Size = new System.Drawing.Size(880, 20);
            this.LblHeader.TabIndex = 0;
            this.LblHeader.Text = "";
            //
            // LblNote
            //
            this.LblNote.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblNote.Location = new System.Drawing.Point(14, 38);
            this.LblNote.Name = "LblNote";
            this.LblNote.Size = new System.Drawing.Size(880, 18);
            this.LblNote.TabIndex = 1;
            this.LblNote.Text = "";
            //
            // GridPreview
            //
            this.GridPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GridPreview.Location = new System.Drawing.Point(14, 62);
            this.GridPreview.MainView = this.GridViewPreview;
            this.GridPreview.Name = "GridPreview";
            this.GridPreview.Size = new System.Drawing.Size(880, 440);
            this.GridPreview.TabIndex = 2;
            this.GridPreview.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewPreview});
            //
            // GridViewPreview
            //
            this.GridViewPreview.GridControl = this.GridPreview;
            this.GridViewPreview.Name = "GridViewPreview";
            this.GridViewPreview.OptionsBehavior.Editable = false;
            this.GridViewPreview.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewPreview.OptionsView.ShowGroupPanel = false;
            this.GridViewPreview.OptionsView.ShowIndicator = false;
            //
            // BtnCreate
            //
            this.BtnCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCreate.Location = new System.Drawing.Point(608, 514);
            this.BtnCreate.Name = "BtnCreate";
            this.BtnCreate.Size = new System.Drawing.Size(196, 30);
            this.BtnCreate.TabIndex = 3;
            this.BtnCreate.Text = "Create invoices";
            this.BtnCreate.Click += new System.EventHandler(this.BtnCreate_Click);
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnClose.Location = new System.Drawing.Point(814, 514);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(80, 30);
            this.BtnClose.TabIndex = 4;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // MeterInvoicePreview_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnClose;
            this.ClientSize = new System.Drawing.Size(908, 558);
            this.Controls.Add(this.LblHeader);
            this.Controls.Add(this.LblNote);
            this.Controls.Add(this.GridPreview);
            this.Controls.Add(this.BtnCreate);
            this.Controls.Add(this.BtnClose);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "MeterInvoicePreview_Form";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preview — what Generate will create";
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
