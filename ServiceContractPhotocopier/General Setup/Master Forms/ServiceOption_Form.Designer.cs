using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class ServiceOption_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private XtraTabControl TabMain;
        private XtraTabPage PageServiceOption, PageNoteControl, PageApi;
        private GroupControl GrpGeneral, GrpDefaults, GrpApi;
        private CheckEdit ChkShowStockPicture, ChkUseAlternativeItem, ChkNegativeStockChecking;
        private CheckEdit ChkAutoGenSalesInvoice, ChkAutoCloseNote, ChkAllowEditClosed;
        private LabelControl LblDefaultStatus, LblDefaultPriority, LblMeterInvFormat, LblCnFormat;
        private ComboBoxEdit CmbCnFormat;
        private TextEdit TxtDefaultServiceStatus, TxtDefaultAppointmentPriority;
        private ComboBoxEdit CmbMeterInvFormat;
        private CheckEdit ChkInvDescFromItem;
        private CheckEdit ChkInvFmtAdvanced;
        private LabelControl LblInvFmtOnline, LblInvFmtOffline;
        private ComboBoxEdit CmbInvFmtOnline, CmbInvFmtOffline;
        private LabelControl LblApiProfile, LblApiUrl, LblApiToken, LblApiMode, LblApiTimeout, LblApiHint;
        private ComboBoxEdit CmbApiProfile, CmbApiMode;
        private TextEdit TxtApiBaseUrl, TxtApiToken;
        private SpinEdit SpnApiTimeout;
        private SimpleButton BtnApiNewProfile, BtnApiSaveProfile, BtnApiDeleteProfile;
        private SimpleButton BtnSave, BtnCancel;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.TabMain = new XtraTabControl();
            this.PageServiceOption = new XtraTabPage();
            this.PageNoteControl = new XtraTabPage();
            this.PageApi = new XtraTabPage();
            this.GrpGeneral = new GroupControl();
            this.GrpDefaults = new GroupControl();
            this.GrpApi = new GroupControl();
            this.LblApiProfile = new LabelControl(); this.CmbApiProfile = new ComboBoxEdit();
            this.LblApiUrl = new LabelControl(); this.TxtApiBaseUrl = new TextEdit();
            this.LblApiToken = new LabelControl(); this.TxtApiToken = new TextEdit();
            this.LblApiMode = new LabelControl(); this.CmbApiMode = new ComboBoxEdit();
            this.LblApiTimeout = new LabelControl(); this.SpnApiTimeout = new SpinEdit();
            this.LblApiHint = new LabelControl();
            this.BtnApiNewProfile = new SimpleButton();
            this.BtnApiSaveProfile = new SimpleButton();
            this.BtnApiDeleteProfile = new SimpleButton();
            this.ChkShowStockPicture = new CheckEdit();
            this.ChkUseAlternativeItem = new CheckEdit();
            this.ChkNegativeStockChecking = new CheckEdit();
            this.ChkAutoGenSalesInvoice = new CheckEdit();
            this.ChkAutoCloseNote = new CheckEdit();
            this.ChkAllowEditClosed = new CheckEdit();
            this.LblDefaultStatus = new LabelControl(); this.TxtDefaultServiceStatus = new TextEdit();
            this.LblDefaultPriority = new LabelControl(); this.TxtDefaultAppointmentPriority = new TextEdit();
            this.LblMeterInvFormat = new LabelControl(); this.CmbMeterInvFormat = new ComboBoxEdit();
            this.LblCnFormat = new LabelControl(); this.CmbCnFormat = new ComboBoxEdit();
            this.ChkInvDescFromItem = new CheckEdit();
            this.ChkInvFmtAdvanced = new CheckEdit();
            this.LblInvFmtOnline = new LabelControl(); this.CmbInvFmtOnline = new ComboBoxEdit();
            this.LblInvFmtOffline = new LabelControl(); this.CmbInvFmtOffline = new ComboBoxEdit();
            this.BtnSave = new SimpleButton(); this.BtnCancel = new SimpleButton();

            this.SuspendLayout();
            this.Text = "Plugin Option";
            this.ClientSize = new Size(720, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;

            // Native AutoCount green header (same family as Maintain Service Contract / Item; hint hidden).
            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Plugin Option";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new Point(0, 0);
            this.PanelHeaderTop.Size = new Size(720, 56);

            // Toolbar row — 86x50 icon buttons, identical to the other maintenance modules.
            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Location = new Point(0, 56);
            this.PanelToolbar.Size = new Size(720, 62);
            this.BtnSave.Text = "Save"; this.BtnSave.Location = new Point(8, 6);
            this.BtnSave.Width = 86; this.BtnSave.Height = 50;
            this.BtnSave.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnSave.Click += new System.EventHandler(this.OnSave);
            this.BtnCancel.Text = "Cancel"; this.BtnCancel.Location = new Point(98, 6);
            this.BtnCancel.Width = 86; this.BtnCancel.Height = 50;
            this.BtnCancel.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnCancel.Click += new System.EventHandler(this.OnCancel);
            this.PanelToolbar.Controls.Add(this.BtnSave);
            this.PanelToolbar.Controls.Add(this.BtnCancel);

            this.TabMain.Location = new Point(12, 130);
            this.TabMain.Size = new Size(696, 396);
            this.PageServiceOption.Text = "1. Service Option";
            this.PageNoteControl.Text = "2. Service Note Control";
            this.PageApi.Text = "3. API";
            this.TabMain.TabPages.AddRange(new XtraTabPage[] { this.PageServiceOption, this.PageNoteControl, this.PageApi });

            // Service Option tab — General checkboxes + Defaults (incl. Meter Invoice No. Format).
            this.GrpGeneral.Text = "General";
            this.GrpGeneral.Location = new Point(14, 12);
            this.GrpGeneral.Size = new Size(660, 130);
            this.ChkShowStockPicture.Properties.Caption = "Show Stock Picture";
            this.ChkShowStockPicture.Location = new Point(16, 30); this.ChkShowStockPicture.Width = 400;
            this.ChkUseAlternativeItem.Properties.Caption = "Use Customer / Alternative Item";
            this.ChkUseAlternativeItem.Location = new Point(16, 60); this.ChkUseAlternativeItem.Width = 400;
            this.ChkNegativeStockChecking.Properties.Caption = "Negative Stock Balance Checking";
            this.ChkNegativeStockChecking.Location = new Point(16, 90); this.ChkNegativeStockChecking.Width = 400;
            this.GrpGeneral.Controls.Add(this.ChkShowStockPicture);
            this.GrpGeneral.Controls.Add(this.ChkUseAlternativeItem);
            this.GrpGeneral.Controls.Add(this.ChkNegativeStockChecking);

            this.GrpDefaults.Text = "Defaults";
            this.GrpDefaults.Location = new Point(14, 154);
            this.GrpDefaults.Size = new Size(660, 220);
            Lbl(this.LblDefaultStatus, "Default Service Status", 16, 32);
            this.TxtDefaultServiceStatus.Location = new Point(190, 30); this.TxtDefaultServiceStatus.Width = 220;
            Lbl(this.LblDefaultPriority, "Default Appt. Priority", 16, 62);
            this.TxtDefaultAppointmentPriority.Location = new Point(190, 60); this.TxtDefaultAppointmentPriority.Width = 220;
            Lbl(this.LblMeterInvFormat, "Meter Invoice No. Format", 16, 92);
            this.CmbMeterInvFormat.Location = new Point(190, 90); this.CmbMeterInvFormat.Width = 220;
            this.ChkInvDescFromItem.Properties.Caption = "Meter invoice line description from Stock Item (untick = meter type name)";
            this.ChkInvDescFromItem.Location = new Point(16, 120); this.ChkInvDescFromItem.Width = 620;
            this.ChkInvFmtAdvanced.Properties.Caption = "Advanced No. Format by machine status:";
            this.ChkInvFmtAdvanced.Location = new Point(16, 150); this.ChkInvFmtAdvanced.Width = 230;
            Lbl(this.LblInvFmtOnline, "Online", 252, 154);
            this.CmbInvFmtOnline.Location = new Point(292, 151); this.CmbInvFmtOnline.Width = 150;
            Lbl(this.LblInvFmtOffline, "Offline", 452, 154);
            this.CmbInvFmtOffline.Location = new Point(492, 151); this.CmbInvFmtOffline.Width = 150;
            Lbl(this.LblCnFormat, "Meter CN No. Format", 16, 182);
            this.CmbCnFormat.Location = new Point(190, 180); this.CmbCnFormat.Width = 220;
            this.GrpDefaults.Controls.Add(this.LblDefaultStatus); this.GrpDefaults.Controls.Add(this.TxtDefaultServiceStatus);
            this.GrpDefaults.Controls.Add(this.LblDefaultPriority); this.GrpDefaults.Controls.Add(this.TxtDefaultAppointmentPriority);
            this.GrpDefaults.Controls.Add(this.LblMeterInvFormat); this.GrpDefaults.Controls.Add(this.CmbMeterInvFormat);
            this.GrpDefaults.Controls.Add(this.ChkInvDescFromItem);
            this.GrpDefaults.Controls.Add(this.ChkInvFmtAdvanced);
            this.GrpDefaults.Controls.Add(this.LblInvFmtOnline); this.GrpDefaults.Controls.Add(this.CmbInvFmtOnline);
            this.GrpDefaults.Controls.Add(this.LblInvFmtOffline); this.GrpDefaults.Controls.Add(this.CmbInvFmtOffline);
            this.GrpDefaults.Controls.Add(this.LblCnFormat); this.GrpDefaults.Controls.Add(this.CmbCnFormat);

            this.PageServiceOption.Controls.Add(this.GrpGeneral);
            this.PageServiceOption.Controls.Add(this.GrpDefaults);

            // API tab — Meter API connection profiles (maintained in dbo.zSCP2_ApiProfile).
            this.GrpApi.Text = "Meter Reading API";
            this.GrpApi.Location = new Point(14, 12);
            this.GrpApi.Size = new Size(660, 260);
            Lbl(this.LblApiProfile, "API Profile", 16, 34);
            this.CmbApiProfile.Location = new Point(150, 32); this.CmbApiProfile.Width = 200;
            this.BtnApiNewProfile.Text = "New";
            this.BtnApiNewProfile.Location = new Point(356, 30);
            this.BtnApiNewProfile.Width = 60; this.BtnApiNewProfile.Height = 26;
            this.BtnApiNewProfile.Click += new System.EventHandler(this.OnApiNewProfile);
            this.BtnApiSaveProfile.Text = "Save Profile";
            this.BtnApiSaveProfile.Location = new Point(420, 30);
            this.BtnApiSaveProfile.Width = 100; this.BtnApiSaveProfile.Height = 26;
            this.BtnApiSaveProfile.Click += new System.EventHandler(this.OnApiSaveProfile);
            this.BtnApiDeleteProfile.Text = "Delete Profile";
            this.BtnApiDeleteProfile.Location = new Point(524, 30);
            this.BtnApiDeleteProfile.Width = 110; this.BtnApiDeleteProfile.Height = 26;
            this.BtnApiDeleteProfile.Click += new System.EventHandler(this.OnApiDeleteProfile);
            Lbl(this.LblApiUrl, "Base URL", 16, 68);
            this.TxtApiBaseUrl.Location = new Point(150, 66); this.TxtApiBaseUrl.Width = 480;
            Lbl(this.LblApiToken, "Token", 16, 98);
            this.TxtApiToken.Location = new Point(150, 96); this.TxtApiToken.Width = 480;
            Lbl(this.LblApiMode, "Mode", 16, 128);
            this.CmbApiMode.Location = new Point(150, 126); this.CmbApiMode.Width = 120;
            this.CmbApiMode.Properties.Items.AddRange(new object[] { "LIVE", "MOCK" });
            this.CmbApiMode.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            Lbl(this.LblApiTimeout, "Timeout (ms)", 16, 158);
            this.SpnApiTimeout.Location = new Point(150, 156); this.SpnApiTimeout.Width = 120;
            this.SpnApiTimeout.Properties.MaxValue = 600000; this.SpnApiTimeout.Properties.MinValue = 1000;
            this.SpnApiTimeout.Properties.IsFloatValue = false;
            this.LblApiHint.Text = "New = create a profile.   Save Profile = store the fields under the selected profile.\r\n" +
                "The main Save button (toolbar) also makes the selected profile the ACTIVE connection used\r\n" +
                "by Meter Reading Integration. Profiles live in the database (zSCP2_ApiProfile).";
            this.LblApiHint.Location = new Point(16, 192);
            this.LblApiHint.Appearance.ForeColor = Color.FromArgb(96, 96, 96);
            this.GrpApi.Controls.Add(this.LblApiProfile); this.GrpApi.Controls.Add(this.CmbApiProfile);
            this.GrpApi.Controls.Add(this.BtnApiNewProfile); this.GrpApi.Controls.Add(this.BtnApiSaveProfile);
            this.GrpApi.Controls.Add(this.BtnApiDeleteProfile);
            this.GrpApi.Controls.Add(this.LblApiUrl); this.GrpApi.Controls.Add(this.TxtApiBaseUrl);
            this.GrpApi.Controls.Add(this.LblApiToken); this.GrpApi.Controls.Add(this.TxtApiToken);
            this.GrpApi.Controls.Add(this.LblApiMode); this.GrpApi.Controls.Add(this.CmbApiMode);
            this.GrpApi.Controls.Add(this.LblApiTimeout); this.GrpApi.Controls.Add(this.SpnApiTimeout);
            this.GrpApi.Controls.Add(this.LblApiHint);
            this.PageApi.Controls.Add(this.GrpApi);

            // Service Note Control tab
            this.ChkAutoGenSalesInvoice.Properties.Caption = "Auto Generate Sales Invoice on Close";
            this.ChkAutoGenSalesInvoice.Location = new Point(20, 20); this.ChkAutoGenSalesInvoice.Width = 400;
            this.ChkAutoCloseNote.Properties.Caption = "Auto Close Service Note on Solution Save";
            this.ChkAutoCloseNote.Location = new Point(20, 50); this.ChkAutoCloseNote.Width = 400;
            this.ChkAllowEditClosed.Properties.Caption = "Allow Modify Closed Service Note";
            this.ChkAllowEditClosed.Location = new Point(20, 80); this.ChkAllowEditClosed.Width = 400;
            this.PageNoteControl.Controls.Add(this.ChkAutoGenSalesInvoice);
            this.PageNoteControl.Controls.Add(this.ChkAutoCloseNote);
            this.PageNoteControl.Controls.Add(this.ChkAllowEditClosed);

            this.Controls.Add(this.TabMain);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.AcceptButton = this.BtnSave;
            this.CancelButton = this.BtnCancel;
            this.ResumeLayout(false);
        }

        private static void Lbl(LabelControl l, string t, int x, int y) { l.Text = t; l.Location = new Point(x, y); }
    }
}
