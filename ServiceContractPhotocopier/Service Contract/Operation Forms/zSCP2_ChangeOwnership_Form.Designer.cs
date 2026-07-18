namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class zSCP2_ChangeOwnership_Form
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
            this.LblCurrentCaption = new DevExpress.XtraEditors.LabelControl();
            this.LblCurrent = new DevExpress.XtraEditors.LabelControl();
            this.ChkToContract = new DevExpress.XtraEditors.CheckEdit();
            this.SluContract = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluContractView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ChkToCustomer = new DevExpress.XtraEditors.CheckEdit();
            this.SluCustomer = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluCustomerView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.ChkToContract.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContract.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContractView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkToCustomer.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluCustomer.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluCustomerView)).BeginInit();
            this.SuspendLayout();
            //
            // LblCurrentCaption
            //
            this.LblCurrentCaption.Location = new System.Drawing.Point(14, 14);
            this.LblCurrentCaption.Name = "LblCurrentCaption";
            this.LblCurrentCaption.Size = new System.Drawing.Size(93, 13);
            this.LblCurrentCaption.TabIndex = 0;
            this.LblCurrentCaption.Text = "Current ownership:";
            //
            // LblCurrent
            //
            this.LblCurrent.Location = new System.Drawing.Point(120, 14);
            this.LblCurrent.Name = "LblCurrent";
            this.LblCurrent.Size = new System.Drawing.Size(31, 13);
            this.LblCurrent.TabIndex = 1;
            this.LblCurrent.Text = "(none)";
            //
            // ChkToContract
            //
            this.ChkToContract.EditValue = true;
            this.ChkToContract.Location = new System.Drawing.Point(12, 44);
            this.ChkToContract.Name = "ChkToContract";
            this.ChkToContract.Properties.Caption = "Transfer to a contract (ownership = that contract / its customer)";
            this.ChkToContract.Properties.CheckStyle = DevExpress.XtraEditors.Controls.CheckStyles.Radio;
            this.ChkToContract.Properties.RadioGroupIndex = 1;
            this.ChkToContract.Size = new System.Drawing.Size(430, 20);
            this.ChkToContract.TabIndex = 2;
            //
            // SluContract
            //
            this.SluContract.Location = new System.Drawing.Point(34, 70);
            this.SluContract.Name = "SluContract";
            this.SluContract.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluContract.Properties.NullText = "";
            this.SluContract.Properties.PopupView = this.SluContractView;
            this.SluContract.Size = new System.Drawing.Size(440, 20);
            this.SluContract.TabIndex = 3;
            //
            // SluContractView
            //
            this.SluContractView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluContractView.Name = "SluContractView";
            this.SluContractView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluContractView.OptionsView.ShowGroupPanel = false;
            //
            // ChkToCustomer
            //
            this.ChkToCustomer.Location = new System.Drawing.Point(12, 104);
            this.ChkToCustomer.Name = "ChkToCustomer";
            this.ChkToCustomer.Properties.Caption = "Transfer to a customer directly (no contract)";
            this.ChkToCustomer.Properties.CheckStyle = DevExpress.XtraEditors.Controls.CheckStyles.Radio;
            this.ChkToCustomer.Properties.RadioGroupIndex = 1;
            this.ChkToCustomer.Size = new System.Drawing.Size(430, 20);
            this.ChkToCustomer.TabIndex = 4;
            //
            // SluCustomer
            //
            this.SluCustomer.Location = new System.Drawing.Point(34, 130);
            this.SluCustomer.Name = "SluCustomer";
            this.SluCustomer.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluCustomer.Properties.NullText = "";
            this.SluCustomer.Properties.PopupView = this.SluCustomerView;
            this.SluCustomer.Size = new System.Drawing.Size(440, 20);
            this.SluCustomer.TabIndex = 5;
            //
            // SluCustomerView
            //
            this.SluCustomerView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluCustomerView.Name = "SluCustomerView";
            this.SluCustomerView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluCustomerView.OptionsView.ShowGroupPanel = false;
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Location = new System.Drawing.Point(14, 164);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(433, 13);
            this.LblHint.TabIndex = 6;
            this.LblHint.Text = "A transfer to a contract of the SAME customer re-attaches without an ownership change.";
            //
            // BtnOK
            //
            this.BtnOK.Location = new System.Drawing.Point(310, 196);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(80, 28);
            this.BtnOK.TabIndex = 7;
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(396, 196);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(80, 28);
            this.BtnCancel.TabIndex = 8;
            this.BtnCancel.Text = "Cancel";
            //
            // zSCP2_ChangeOwnership_Form
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(490, 238);
            this.Controls.Add(this.LblCurrentCaption);
            this.Controls.Add(this.LblCurrent);
            this.Controls.Add(this.ChkToContract);
            this.Controls.Add(this.SluContract);
            this.Controls.Add(this.ChkToCustomer);
            this.Controls.Add(this.SluCustomer);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "zSCP2_ChangeOwnership_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Change Ownership";
            ((System.ComponentModel.ISupportInitialize)(this.SluCustomerView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluCustomer.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkToCustomer.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContractView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContract.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkToContract.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblCurrentCaption;
        private DevExpress.XtraEditors.LabelControl LblCurrent;
        private DevExpress.XtraEditors.CheckEdit ChkToContract;
        private DevExpress.XtraEditors.SearchLookUpEdit SluContract;
        private DevExpress.XtraGrid.Views.Grid.GridView SluContractView;
        private DevExpress.XtraEditors.CheckEdit ChkToCustomer;
        private DevExpress.XtraEditors.SearchLookUpEdit SluCustomer;
        private DevExpress.XtraGrid.Views.Grid.GridView SluCustomerView;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
