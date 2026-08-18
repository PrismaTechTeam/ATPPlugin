namespace ServiceContractPhotocopier
{
    partial class SampleInvoice_Form
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
            this.LblHeader = new DevExpress.XtraEditors.LabelControl();
            this.GridLines = new DevExpress.XtraGrid.GridControl();
            this.GridViewLines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColInvoice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColLine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCovers = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColQty = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUnitPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.LblFoot = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // LblHeader
            //
            this.LblHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblHeader.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.LblHeader.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHeader.Location = new System.Drawing.Point(12, 10);
            this.LblHeader.Name = "LblHeader";
            this.LblHeader.Size = new System.Drawing.Size(1030, 20);
            this.LblHeader.TabIndex = 0;
            this.LblHeader.Text = "";
            //
            // GridLines
            //
            this.GridLines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridLines.Location = new System.Drawing.Point(12, 36);
            this.GridLines.MainView = this.GridViewLines;
            this.GridLines.Name = "GridLines";
            this.GridLines.Size = new System.Drawing.Size(1030, 420);
            this.GridLines.TabIndex = 1;
            this.GridLines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewLines});
            //
            // GridViewLines
            //
            this.GridViewLines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColInvoice,
            this.ColLine,
            this.ColDescription,
            this.ColCovers,
            this.ColQty,
            this.ColUnitPrice,
            this.ColAmount});
            this.GridViewLines.GridControl = this.GridLines;
            this.GridViewLines.Name = "GridViewLines";
            this.GridViewLines.OptionsBehavior.Editable = false;
            this.GridViewLines.OptionsView.ShowGroupPanel = false;
            this.GridViewLines.OptionsView.ColumnAutoWidth = true;
            this.GridViewLines.OptionsView.ShowFooter = true;
            //
            // ColInvoice
            //
            this.ColInvoice.Caption = "Invoice";
            this.ColInvoice.FieldName = "Invoice";
            this.ColInvoice.Name = "ColInvoice";
            this.ColInvoice.GroupIndex = 0;
            this.ColInvoice.Visible = true;
            this.ColInvoice.VisibleIndex = 0;
            this.ColInvoice.Width = 200;
            //
            // ColLine
            //
            this.ColLine.Caption = "#";
            this.ColLine.FieldName = "Line";
            this.ColLine.MaxWidth = 40;
            this.ColLine.Name = "ColLine";
            this.ColLine.Visible = true;
            this.ColLine.VisibleIndex = 0;
            this.ColLine.Width = 40;
            //
            // ColDescription
            //
            this.ColDescription.Caption = "Description";
            this.ColDescription.FieldName = "Description";
            this.ColDescription.Name = "ColDescription";
            this.ColDescription.Visible = true;
            this.ColDescription.VisibleIndex = 1;
            this.ColDescription.Width = 380;
            //
            // ColCovers
            //
            this.ColCovers.Caption = "Covers";
            this.ColCovers.FieldName = "Covers";
            this.ColCovers.Name = "ColCovers";
            this.ColCovers.Visible = true;
            this.ColCovers.VisibleIndex = 2;
            this.ColCovers.Width = 260;
            //
            // ColQty
            //
            this.ColQty.Caption = "Qty";
            this.ColQty.DisplayFormat.FormatString = "n0";
            this.ColQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColQty.FieldName = "Qty";
            this.ColQty.MaxWidth = 90;
            this.ColQty.Name = "ColQty";
            this.ColQty.Visible = true;
            this.ColQty.VisibleIndex = 3;
            this.ColQty.Width = 90;
            //
            // ColUnitPrice
            //
            this.ColUnitPrice.Caption = "Unit Price";
            this.ColUnitPrice.DisplayFormat.FormatString = "n4";
            this.ColUnitPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColUnitPrice.FieldName = "UnitPrice";
            this.ColUnitPrice.MaxWidth = 100;
            this.ColUnitPrice.Name = "ColUnitPrice";
            this.ColUnitPrice.Visible = true;
            this.ColUnitPrice.VisibleIndex = 4;
            this.ColUnitPrice.Width = 100;
            //
            // ColAmount
            //
            this.ColAmount.Caption = "Amount";
            this.ColAmount.DisplayFormat.FormatString = "n2";
            this.ColAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColAmount.FieldName = "Amount";
            this.ColAmount.MaxWidth = 110;
            this.ColAmount.Name = "ColAmount";
            this.ColAmount.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] {
            new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Amount", "{0:n2}")});
            this.ColAmount.Visible = true;
            this.ColAmount.VisibleIndex = 5;
            this.ColAmount.Width = 110;
            //
            // LblFoot
            //
            this.LblFoot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblFoot.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblFoot.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblFoot.Appearance.Options.UseTextOptions = true;
            this.LblFoot.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFoot.Location = new System.Drawing.Point(12, 464);
            this.LblFoot.Name = "LblFoot";
            this.LblFoot.Size = new System.Drawing.Size(1030, 58);
            this.LblFoot.TabIndex = 2;
            this.LblFoot.Text = "";
            //
            // PanelBottom
            //
            this.PanelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelBottom.Controls.Add(this.BtnClose);
            this.PanelBottom.Location = new System.Drawing.Point(12, 528);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(1030, 44);
            this.PanelBottom.TabIndex = 3;
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Location = new System.Drawing.Point(935, 9);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(85, 24);
            this.BtnClose.TabIndex = 0;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // SampleInvoice_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnClose;
            this.ClientSize = new System.Drawing.Size(1054, 584);
            this.Controls.Add(this.LblHeader);
            this.Controls.Add(this.GridLines);
            this.Controls.Add(this.LblFoot);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.Name = "SampleInvoice_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sample Invoice — what this contract will print";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHeader;
        private DevExpress.XtraGrid.GridControl GridLines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewLines;
        private DevExpress.XtraGrid.Columns.GridColumn ColInvoice;
        private DevExpress.XtraGrid.Columns.GridColumn ColLine;
        private DevExpress.XtraGrid.Columns.GridColumn ColDescription;
        private DevExpress.XtraGrid.Columns.GridColumn ColCovers;
        private DevExpress.XtraGrid.Columns.GridColumn ColQty;
        private DevExpress.XtraGrid.Columns.GridColumn ColUnitPrice;
        private DevExpress.XtraGrid.Columns.GridColumn ColAmount;
        private DevExpress.XtraEditors.LabelControl LblFoot;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
    }
}
