namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class MultiPricePicker_Form
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

        private DevExpress.XtraEditors.LabelControl LblScheme;
        private DevExpress.XtraEditors.SearchLookUpEdit SluScheme;
        private DevExpress.XtraGrid.Views.Grid.GridView SluSchemeView;
        private DevExpress.XtraGrid.Columns.GridColumn SluColCode;
        private DevExpress.XtraGrid.Columns.GridColumn SluColDesc;
        private DevExpress.XtraEditors.LabelControl LblTiers;
        private DevExpress.XtraGrid.GridControl GridTiers;
        private DevExpress.XtraGrid.Views.Grid.GridView ViewTiers;
        private DevExpress.XtraGrid.Columns.GridColumn ColReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColPrice;
        private DevExpress.XtraEditors.SimpleButton BtnAddRow;
        private DevExpress.XtraEditors.SimpleButton BtnDelRow;
        private DevExpress.XtraEditors.SimpleButton BtnRowUp;
        private DevExpress.XtraEditors.SimpleButton BtnRowDown;
        private DevExpress.XtraEditors.SimpleButton BtnInfinity;
        private DevExpress.XtraEditors.SimpleButton BtnClear;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LblScheme = new DevExpress.XtraEditors.LabelControl();
            this.SluScheme = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluSchemeView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.SluColCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.SluColDesc = new DevExpress.XtraGrid.Columns.GridColumn();
            this.LblTiers = new DevExpress.XtraEditors.LabelControl();
            this.GridTiers = new DevExpress.XtraGrid.GridControl();
            this.ViewTiers = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.BtnAddRow = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelRow = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRowUp = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRowDown = new DevExpress.XtraEditors.SimpleButton();
            this.BtnInfinity = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClear = new DevExpress.XtraEditors.SimpleButton();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.SluScheme.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluSchemeView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridTiers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ViewTiers)).BeginInit();
            this.SuspendLayout();
            //
            // LblScheme
            //
            this.LblScheme.Location = new System.Drawing.Point(12, 15);
            this.LblScheme.Name = "LblScheme";
            this.LblScheme.Size = new System.Drawing.Size(62, 13);
            this.LblScheme.Text = "Multi Pricing";
            //
            // SluScheme
            //
            this.SluScheme.Location = new System.Drawing.Point(95, 12);
            this.SluScheme.Name = "SluScheme";
            this.SluScheme.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluScheme.Properties.NullText = "(pick a multi pricing scheme)";
            this.SluScheme.Properties.PopupView = this.SluSchemeView;
            this.SluScheme.Size = new System.Drawing.Size(320, 20);
            this.SluScheme.TabIndex = 1;
            //
            // SluSchemeView
            //
            this.SluSchemeView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.SluColCode,
            this.SluColDesc});
            this.SluSchemeView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluSchemeView.Name = "SluSchemeView";
            this.SluSchemeView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluSchemeView.OptionsView.ShowAutoFilterRow = true;
            this.SluSchemeView.OptionsView.ShowGroupPanel = false;
            //
            // SluColCode
            //
            this.SluColCode.Caption = "Multi Pricing Code";
            this.SluColCode.FieldName = "MeterMultiPriceCode";
            this.SluColCode.Name = "SluColCode";
            this.SluColCode.Visible = true;
            this.SluColCode.VisibleIndex = 0;
            this.SluColCode.Width = 160;
            //
            // SluColDesc
            //
            this.SluColDesc.Caption = "Description";
            this.SluColDesc.FieldName = "Description";
            this.SluColDesc.Name = "SluColDesc";
            this.SluColDesc.Visible = true;
            this.SluColDesc.VisibleIndex = 1;
            this.SluColDesc.Width = 300;
            //
            // LblTiers
            //
            this.LblTiers.Location = new System.Drawing.Point(12, 46);
            this.LblTiers.Name = "LblTiers";
            this.LblTiers.Size = new System.Drawing.Size(102, 13);
            this.LblTiers.Text = "Meter Pricing Rates :";
            //
            // BtnAddRow
            //
            this.BtnAddRow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnAddRow.Location = new System.Drawing.Point(268, 41);
            this.BtnAddRow.Name = "BtnAddRow";
            this.BtnAddRow.Size = new System.Drawing.Size(80, 24);
            this.BtnAddRow.TabIndex = 2;
            this.BtnAddRow.Text = "+ Add Row";
            //
            // BtnDelRow
            //
            this.BtnDelRow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnDelRow.Location = new System.Drawing.Point(352, 41);
            this.BtnDelRow.Name = "BtnDelRow";
            this.BtnDelRow.Size = new System.Drawing.Size(90, 24);
            this.BtnDelRow.TabIndex = 3;
            this.BtnDelRow.Text = "- Delete Row";
            //
            // BtnRowUp
            //
            this.BtnRowUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRowUp.Location = new System.Drawing.Point(446, 41);
            this.BtnRowUp.Name = "BtnRowUp";
            this.BtnRowUp.Size = new System.Drawing.Size(28, 24);
            this.BtnRowUp.TabIndex = 4;
            this.BtnRowUp.Text = "↑";
            //
            // BtnRowDown
            //
            this.BtnRowDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRowDown.Location = new System.Drawing.Point(478, 41);
            this.BtnRowDown.Name = "BtnRowDown";
            this.BtnRowDown.Size = new System.Drawing.Size(28, 24);
            this.BtnRowDown.TabIndex = 5;
            this.BtnRowDown.Text = "↓";
            //
            // BtnInfinity
            //
            this.BtnInfinity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnInfinity.Location = new System.Drawing.Point(510, 41);
            this.BtnInfinity.Name = "BtnInfinity";
            this.BtnInfinity.Size = new System.Drawing.Size(100, 24);
            this.BtnInfinity.TabIndex = 6;
            this.BtnInfinity.Text = "∞  Unlimited";
            this.BtnInfinity.ToolTip = "Set the focused row\'s Meter Reading boundary to unlimited (999,999,999,999) so usage can never exceed the last bracket.";
            //
            // GridTiers
            //
            this.GridTiers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridTiers.Location = new System.Drawing.Point(12, 70);
            this.GridTiers.MainView = this.ViewTiers;
            this.GridTiers.Name = "GridTiers";
            this.GridTiers.Size = new System.Drawing.Size(598, 300);
            this.GridTiers.TabIndex = 7;
            this.GridTiers.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.ViewTiers});
            //
            // ViewTiers
            //
            this.ViewTiers.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColReading,
            this.ColPrice});
            this.ViewTiers.GridControl = this.GridTiers;
            this.ViewTiers.Name = "ViewTiers";
            this.ViewTiers.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.ViewTiers.OptionsView.ShowGroupPanel = false;
            //
            // ColReading
            //
            this.ColReading.Caption = "Meter Reading (<=)";
            this.ColReading.FieldName = "MeterReading";
            this.ColReading.Name = "ColReading";
            this.ColReading.Visible = true;
            this.ColReading.VisibleIndex = 0;
            this.ColReading.Width = 280;
            //
            // ColPrice
            //
            this.ColPrice.Caption = "Unit Price (Base UOM)";
            this.ColPrice.FieldName = "UnitPrice";
            this.ColPrice.Name = "ColPrice";
            this.ColPrice.Visible = true;
            this.ColPrice.VisibleIndex = 1;
            this.ColPrice.Width = 280;
            //
            // LblHint
            //
            this.LblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(12, 378);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(598, 30);
            this.LblHint.Text = "A 0.00 Unit Price row is the FREE band (FOC). The last row should be unlimited (∞) so heavy usage always finds a bracket.";
            //
            // BtnClear
            //
            this.BtnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnClear.Location = new System.Drawing.Point(12, 414);
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.Size = new System.Drawing.Size(150, 28);
            this.BtnClear.TabIndex = 8;
            this.BtnClear.Text = "No Multi-Price (clear)";
            this.BtnClear.ToolTip = "Remove the multi-price link from this meter — it bills by its flat Rate again.";
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.Location = new System.Drawing.Point(432, 414);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 28);
            this.BtnOK.TabIndex = 9;
            this.BtnOK.Text = "OK";
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(525, 414);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 28);
            this.BtnCancel.TabIndex = 10;
            this.BtnCancel.Text = "Cancel";
            //
            // MultiPricePicker_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(622, 454);
            this.Controls.Add(this.LblScheme);
            this.Controls.Add(this.SluScheme);
            this.Controls.Add(this.LblTiers);
            this.Controls.Add(this.BtnAddRow);
            this.Controls.Add(this.BtnDelRow);
            this.Controls.Add(this.BtnRowUp);
            this.Controls.Add(this.BtnRowDown);
            this.Controls.Add(this.BtnInfinity);
            this.Controls.Add(this.GridTiers);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.BtnClear);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.BtnCancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 420);
            this.Name = "MultiPricePicker_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Multi-Price for this Meter";
            ((System.ComponentModel.ISupportInitialize)(this.SluScheme.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluSchemeView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridTiers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ViewTiers)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
