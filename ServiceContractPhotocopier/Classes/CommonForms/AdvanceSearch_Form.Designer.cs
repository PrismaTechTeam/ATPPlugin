namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class AdvanceSearch_Form
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

        private DevExpress.XtraGrid.GridControl Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView View;
        private DevExpress.XtraEditors.PanelControl PnlBottom;
        private DevExpress.XtraEditors.SimpleButton BtnOk;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.LabelControl LblCount;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Grid = new DevExpress.XtraGrid.GridControl();
            this.View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.PnlBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.LblCount = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PnlBottom)).BeginInit();
            this.PnlBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // Grid
            //
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 0);
            this.Grid.MainView = this.View;
            this.Grid.Name = "Grid";
            this.Grid.Size = new System.Drawing.Size(760, 452);
            this.Grid.TabIndex = 0;
            this.Grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.View});
            //
            // View
            //
            this.View.GridControl = this.Grid;
            this.View.Name = "View";
            this.View.OptionsBehavior.Editable = false;
            this.View.OptionsFind.AlwaysVisible = true;
            this.View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.View.OptionsView.ShowAutoFilterRow = true;
            this.View.OptionsView.ShowGroupPanel = false;
            //
            // PnlBottom
            //
            this.PnlBottom.Controls.Add(this.LblCount);
            this.PnlBottom.Controls.Add(this.BtnCancel);
            this.PnlBottom.Controls.Add(this.BtnOk);
            this.PnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PnlBottom.Location = new System.Drawing.Point(0, 452);
            this.PnlBottom.Name = "PnlBottom";
            this.PnlBottom.Size = new System.Drawing.Size(760, 40);
            this.PnlBottom.TabIndex = 1;
            //
            // BtnOk
            //
            this.BtnOk.Location = new System.Drawing.Point(566, 8);
            this.BtnOk.Name = "BtnOk";
            this.BtnOk.Size = new System.Drawing.Size(90, 26);
            this.BtnOk.TabIndex = 0;
            this.BtnOk.Text = "OK (F7)";
            this.BtnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(660, 8);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 26);
            this.BtnCancel.TabIndex = 1;
            this.BtnCancel.Text = "Cancel (F8)";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // LblCount
            //
            this.LblCount.Location = new System.Drawing.Point(10, 15);
            this.LblCount.Name = "LblCount";
            this.LblCount.Size = new System.Drawing.Size(50, 13);
            this.LblCount.TabIndex = 2;
            this.LblCount.Text = "0 record(s)";
            //
            // AdvanceSearch_Form
            //
            this.ClientSize = new System.Drawing.Size(760, 492);
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.PnlBottom);
            this.KeyPreview = true;
            this.MinimizeBox = false;
            this.Name = "AdvanceSearch_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advance Search";
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PnlBottom)).EndInit();
            this.PnlBottom.ResumeLayout(false);
            this.PnlBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
