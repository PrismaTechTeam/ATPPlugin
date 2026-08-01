namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class BulkEmailTemplate_Form
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
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.GrpEdit = new DevExpress.XtraEditors.GroupControl();
            this.LblTpl = new DevExpress.XtraEditors.LabelControl();
            this.CmbTemplate = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnTplNew = new DevExpress.XtraEditors.SimpleButton();
            this.BtnTplDelete = new DevExpress.XtraEditors.SimpleButton();
            this.BtnTplDefault = new DevExpress.XtraEditors.SimpleButton();
            this.LblStyle = new DevExpress.XtraEditors.LabelControl();
            this.CmbStyle = new DevExpress.XtraEditors.ComboBoxEdit();
            this.WebPreview = new System.Windows.Forms.WebBrowser();
            this.LblSubject = new DevExpress.XtraEditors.LabelControl();
            this.TxtSubject = new DevExpress.XtraEditors.TextEdit();
            this.LblBody = new DevExpress.XtraEditors.LabelControl();
            this.TxtBody = new DevExpress.XtraEditors.MemoEdit();
            this.LblInsert = new DevExpress.XtraEditors.LabelControl();
            this.BtnTokAcc = new DevExpress.XtraEditors.SimpleButton();
            this.BtnTokName = new DevExpress.XtraEditors.SimpleButton();
            this.BtnTokDocs = new DevExpress.XtraEditors.SimpleButton();
            this.GrpPreview = new DevExpress.XtraEditors.GroupControl();
            this.LblPvSubjectCap = new DevExpress.XtraEditors.LabelControl();
            this.LblPvSubject = new DevExpress.XtraEditors.LabelControl();
            this.TxtPreview = new DevExpress.XtraEditors.MemoEdit();
            this.LblPvSample = new DevExpress.XtraEditors.LabelControl();
            this.BtnSave = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDefault = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GrpEdit)).BeginInit();
            this.GrpEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CmbTemplate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbStyle.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSubject.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtBody.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpPreview)).BeginInit();
            this.GrpPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtPreview.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Bulk Email Template";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(934, 34);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // GrpEdit
            //
            this.GrpEdit.Controls.Add(this.LblTpl);
            this.GrpEdit.Controls.Add(this.CmbTemplate);
            this.GrpEdit.Controls.Add(this.BtnTplNew);
            this.GrpEdit.Controls.Add(this.BtnTplDelete);
            this.GrpEdit.Controls.Add(this.BtnTplDefault);
            this.GrpEdit.Controls.Add(this.LblStyle);
            this.GrpEdit.Controls.Add(this.CmbStyle);
            this.GrpEdit.Controls.Add(this.LblSubject);
            this.GrpEdit.Controls.Add(this.TxtSubject);
            this.GrpEdit.Controls.Add(this.LblBody);
            this.GrpEdit.Controls.Add(this.TxtBody);
            this.GrpEdit.Controls.Add(this.LblInsert);
            this.GrpEdit.Controls.Add(this.BtnTokAcc);
            this.GrpEdit.Controls.Add(this.BtnTokName);
            this.GrpEdit.Controls.Add(this.BtnTokDocs);
            this.GrpEdit.Location = new System.Drawing.Point(10, 42);
            this.GrpEdit.Name = "GrpEdit";
            this.GrpEdit.Size = new System.Drawing.Size(474, 408);
            this.GrpEdit.TabIndex = 1;
            this.GrpEdit.Text = "Edit Template";
            //
            // LblTpl
            //
            this.LblTpl.Location = new System.Drawing.Point(14, 34);
            this.LblTpl.Name = "LblTpl";
            this.LblTpl.Size = new System.Drawing.Size(44, 13);
            this.LblTpl.Text = "Template";
            //
            // CmbTemplate
            //
            this.CmbTemplate.Location = new System.Drawing.Point(80, 31);
            this.CmbTemplate.Name = "CmbTemplate";
            this.CmbTemplate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbTemplate.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbTemplate.Size = new System.Drawing.Size(168, 20);
            this.CmbTemplate.TabIndex = 0;
            //
            // BtnTplNew
            //
            this.BtnTplNew.Location = new System.Drawing.Point(252, 29);
            this.BtnTplNew.Name = "BtnTplNew";
            this.BtnTplNew.Size = new System.Drawing.Size(52, 24);
            this.BtnTplNew.TabIndex = 9;
            this.BtnTplNew.Text = "New";
            this.BtnTplNew.ToolTip = "Create a new template version (starts as a copy of what is on screen).";
            this.BtnTplNew.Click += new System.EventHandler(this.BtnTplNew_Click);
            //
            // BtnTplDelete
            //
            this.BtnTplDelete.Location = new System.Drawing.Point(308, 29);
            this.BtnTplDelete.Name = "BtnTplDelete";
            this.BtnTplDelete.Size = new System.Drawing.Size(58, 24);
            this.BtnTplDelete.TabIndex = 10;
            this.BtnTplDelete.Text = "Delete";
            this.BtnTplDelete.Click += new System.EventHandler(this.BtnTplDelete_Click);
            //
            // BtnTplDefault
            //
            this.BtnTplDefault.Location = new System.Drawing.Point(370, 29);
            this.BtnTplDefault.Name = "BtnTplDefault";
            this.BtnTplDefault.Size = new System.Drawing.Size(90, 24);
            this.BtnTplDefault.TabIndex = 11;
            this.BtnTplDefault.Text = "Set Default";
            this.BtnTplDefault.ToolTip = "Bulk Email Invoice always sends with the DEFAULT template.";
            this.BtnTplDefault.Click += new System.EventHandler(this.BtnTplDefault_Click);
            //
            // LblStyle
            //
            this.LblStyle.Location = new System.Drawing.Point(14, 62);
            this.LblStyle.Name = "LblStyle";
            this.LblStyle.Size = new System.Drawing.Size(51, 13);
            this.LblStyle.Text = "Email style";
            //
            // CmbStyle
            //
            this.CmbStyle.Location = new System.Drawing.Point(80, 59);
            this.CmbStyle.Name = "CmbStyle";
            this.CmbStyle.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbStyle.Properties.Items.AddRange(new object[] {
            "Plain text",
            "Professional (styled)"});
            this.CmbStyle.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbStyle.Size = new System.Drawing.Size(200, 20);
            this.CmbStyle.TabIndex = 0;
            //
            // LblSubject
            //
            this.LblSubject.Location = new System.Drawing.Point(14, 90);
            this.LblSubject.Name = "LblSubject";
            this.LblSubject.Size = new System.Drawing.Size(38, 13);
            this.LblSubject.Text = "Subject";
            //
            // TxtSubject
            //
            this.TxtSubject.Location = new System.Drawing.Point(80, 87);
            this.TxtSubject.Name = "TxtSubject";
            this.TxtSubject.Size = new System.Drawing.Size(380, 20);
            this.TxtSubject.TabIndex = 1;
            //
            // LblBody
            //
            this.LblBody.Location = new System.Drawing.Point(14, 118);
            this.LblBody.Name = "LblBody";
            this.LblBody.Size = new System.Drawing.Size(45, 13);
            this.LblBody.Text = "Message";
            //
            // TxtBody
            //
            this.TxtBody.Location = new System.Drawing.Point(80, 115);
            this.TxtBody.Name = "TxtBody";
            this.TxtBody.Size = new System.Drawing.Size(380, 234);
            this.TxtBody.TabIndex = 2;
            //
            // LblInsert
            //
            this.LblInsert.Location = new System.Drawing.Point(80, 361);
            this.LblInsert.Name = "LblInsert";
            this.LblInsert.Size = new System.Drawing.Size(60, 13);
            this.LblInsert.Text = "Insert token:";
            //
            // BtnTokAcc
            //
            this.BtnTokAcc.Location = new System.Drawing.Point(80, 377);
            this.BtnTokAcc.Name = "BtnTokAcc";
            this.BtnTokAcc.Size = new System.Drawing.Size(122, 25);
            this.BtnTokAcc.TabIndex = 3;
            this.BtnTokAcc.Text = "Customer Code";
            this.BtnTokAcc.ToolTip = "Insert {AccNo} at the cursor — becomes each customer's account code when sending.";
            this.BtnTokAcc.Click += new System.EventHandler(this.BtnTokAcc_Click);
            //
            // BtnTokName
            //
            this.BtnTokName.Location = new System.Drawing.Point(208, 377);
            this.BtnTokName.Name = "BtnTokName";
            this.BtnTokName.Size = new System.Drawing.Size(126, 25);
            this.BtnTokName.TabIndex = 4;
            this.BtnTokName.Text = "Customer Name";
            this.BtnTokName.ToolTip = "Insert {CompanyName} at the cursor — becomes each customer's company name when sending.";
            this.BtnTokName.Click += new System.EventHandler(this.BtnTokName_Click);
            //
            // BtnTokDocs
            //
            this.BtnTokDocs.Location = new System.Drawing.Point(340, 377);
            this.BtnTokDocs.Name = "BtnTokDocs";
            this.BtnTokDocs.Size = new System.Drawing.Size(120, 25);
            this.BtnTokDocs.TabIndex = 5;
            this.BtnTokDocs.Text = "Invoice Nos";
            this.BtnTokDocs.ToolTip = "Insert {DocNos} at the cursor — becomes that customer's invoice numbers when sending.";
            this.BtnTokDocs.Click += new System.EventHandler(this.BtnTokDocs_Click);
            //
            // GrpPreview
            //
            this.GrpPreview.Controls.Add(this.LblPvSubjectCap);
            this.GrpPreview.Controls.Add(this.LblPvSubject);
            this.GrpPreview.Controls.Add(this.TxtPreview);
            this.GrpPreview.Controls.Add(this.WebPreview);
            this.GrpPreview.Controls.Add(this.LblPvSample);
            this.GrpPreview.Location = new System.Drawing.Point(492, 42);
            this.GrpPreview.Name = "GrpPreview";
            this.GrpPreview.Size = new System.Drawing.Size(432, 408);
            this.GrpPreview.TabIndex = 2;
            this.GrpPreview.Text = "Preview — what the customer receives";
            //
            // LblPvSubjectCap
            //
            this.LblPvSubjectCap.Location = new System.Drawing.Point(14, 34);
            this.LblPvSubjectCap.Name = "LblPvSubjectCap";
            this.LblPvSubjectCap.Size = new System.Drawing.Size(41, 13);
            this.LblPvSubjectCap.Text = "Subject:";
            //
            // LblPvSubject
            //
            this.LblPvSubject.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblPvSubject.Appearance.Options.UseFont = true;
            this.LblPvSubject.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
            this.LblPvSubject.Appearance.Options.UseTextOptions = true;
            this.LblPvSubject.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblPvSubject.Location = new System.Drawing.Point(64, 34);
            this.LblPvSubject.Name = "LblPvSubject";
            this.LblPvSubject.Size = new System.Drawing.Size(354, 13);
            this.LblPvSubject.Text = "";
            //
            // TxtPreview
            //
            this.TxtPreview.Location = new System.Drawing.Point(14, 55);
            this.TxtPreview.Name = "TxtPreview";
            this.TxtPreview.Properties.ReadOnly = true;
            this.TxtPreview.Size = new System.Drawing.Size(404, 320);
            this.TxtPreview.TabIndex = 0;
            this.TxtPreview.TabStop = false;
            //
            // WebPreview
            //
            this.WebPreview.AllowNavigation = false;
            this.WebPreview.AllowWebBrowserDrop = false;
            this.WebPreview.IsWebBrowserContextMenuEnabled = false;
            this.WebPreview.Location = new System.Drawing.Point(14, 55);
            this.WebPreview.Name = "WebPreview";
            this.WebPreview.ScriptErrorsSuppressed = true;
            this.WebPreview.Size = new System.Drawing.Size(404, 320);
            this.WebPreview.TabStop = false;
            this.WebPreview.Visible = false;
            this.WebPreview.WebBrowserShortcutsEnabled = false;
            //
            // LblPvSample
            //
            this.LblPvSample.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.LblPvSample.Appearance.Options.UseForeColor = true;
            this.LblPvSample.Location = new System.Drawing.Point(14, 383);
            this.LblPvSample.Name = "LblPvSample";
            this.LblPvSample.Size = new System.Drawing.Size(404, 13);
            this.LblPvSample.Text = "Sample customer: 3000-A0011 · ATRIA ARCHITECT SDN BHD · MR2607.0001-0003";
            //
            // BtnSave
            //
            this.BtnSave.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.BtnSave.Appearance.Options.UseFont = true;
            this.BtnSave.Location = new System.Drawing.Point(576, 460);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(120, 32);
            this.BtnSave.TabIndex = 6;
            this.BtnSave.Text = "Save";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            //
            // BtnDefault
            //
            this.BtnDefault.Location = new System.Drawing.Point(702, 460);
            this.BtnDefault.Name = "BtnDefault";
            this.BtnDefault.Size = new System.Drawing.Size(110, 32);
            this.BtnDefault.TabIndex = 7;
            this.BtnDefault.Text = "Reset Text";
            this.BtnDefault.ToolTip = "Reset subject and message to the standard wording (not saved until you click Save).";
            this.BtnDefault.Click += new System.EventHandler(this.BtnDefault_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(818, 460);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(106, 32);
            this.BtnCancel.TabIndex = 8;
            this.BtnCancel.Text = "Close";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // BulkEmailTemplate_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 502);
            this.Controls.Add(this.GrpEdit);
            this.Controls.Add(this.GrpPreview);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.BtnDefault);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.PanelHeaderTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BulkEmailTemplate_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bulk Email Template";
            ((System.ComponentModel.ISupportInitialize)(this.CmbTemplate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbStyle.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSubject.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtBody.Properties)).EndInit();
            this.GrpEdit.ResumeLayout(false);
            this.GrpEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtPreview.Properties)).EndInit();
            this.GrpPreview.ResumeLayout(false);
            this.GrpPreview.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpPreview)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.GroupControl GrpEdit;
        private DevExpress.XtraEditors.LabelControl LblTpl;
        private DevExpress.XtraEditors.ComboBoxEdit CmbTemplate;
        private DevExpress.XtraEditors.SimpleButton BtnTplNew;
        private DevExpress.XtraEditors.SimpleButton BtnTplDelete;
        private DevExpress.XtraEditors.SimpleButton BtnTplDefault;
        private DevExpress.XtraEditors.LabelControl LblStyle;
        private DevExpress.XtraEditors.ComboBoxEdit CmbStyle;
        private System.Windows.Forms.WebBrowser WebPreview;
        private DevExpress.XtraEditors.LabelControl LblSubject;
        private DevExpress.XtraEditors.TextEdit TxtSubject;
        private DevExpress.XtraEditors.LabelControl LblBody;
        private DevExpress.XtraEditors.MemoEdit TxtBody;
        private DevExpress.XtraEditors.LabelControl LblInsert;
        private DevExpress.XtraEditors.SimpleButton BtnTokAcc;
        private DevExpress.XtraEditors.SimpleButton BtnTokName;
        private DevExpress.XtraEditors.SimpleButton BtnTokDocs;
        private DevExpress.XtraEditors.GroupControl GrpPreview;
        private DevExpress.XtraEditors.LabelControl LblPvSubjectCap;
        private DevExpress.XtraEditors.LabelControl LblPvSubject;
        private DevExpress.XtraEditors.MemoEdit TxtPreview;
        private DevExpress.XtraEditors.LabelControl LblPvSample;
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnDefault;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
