using System;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Bulk Email Invoice message template (demo feedback 28/07 #9a). Saved once per account book
    /// (Z_PumsConfig) — the Bulk Email screen uses it on every run, so nobody retypes the wording.
    /// Left side edits, right side shows a LIVE PREVIEW rendered with sample customer data so the
    /// user sees exactly what the customer will receive; the token buttons insert placeholders at
    /// the cursor — no need to remember the {curly} names.
    /// </summary>
    public partial class BulkEmailTemplate_Form : XtraForm
    {
        private readonly DBSetting _dbSetting;
        private DevExpress.XtraEditors.TextEdit _lastFocused;   // where a token button inserts (MemoEdit derives TextEdit)

        private const string SAMPLE_ACC = "3000-A0011";
        private const string SAMPLE_NAME = "ATRIA ARCHITECT SDN BHD";
        private const string SAMPLE_DOCS = "MR2607.0001, MR2607.0002, MR2607.0003";

        public BulkEmailTemplate_Form()
        {
            InitializeComponent();
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            _lastFocused = TxtBody;
            TxtSubject.Enter += delegate { _lastFocused = TxtSubject; };
            TxtBody.Enter += delegate { _lastFocused = TxtBody; };
            TxtSubject.EditValueChanged += delegate { UpdatePreview(); };
            TxtBody.EditValueChanged += delegate { UpdatePreview(); };
        }

        public BulkEmailTemplate_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            if (_dbSetting != null)
            {
                TxtSubject.Text = PumsConfig.Get(_dbSetting,
                    PumsConfig.KEY_BULKMAIL_SUBJECT, PumsConfig.DEFAULT_BULKMAIL_SUBJECT);
                TxtBody.Text = PumsConfig.Get(_dbSetting,
                    PumsConfig.KEY_BULKMAIL_BODY, PumsConfig.DEFAULT_BULKMAIL_BODY);
            }
            UpdatePreview();
        }

        private void ApplyButtonIcons()
        {
            SetSvg(BtnSave, "svgimages/outlook inspired/saveas.svg", 20);
            SetSvg(BtnDefault, "svgimages/xaf/action_reload.svg", 20);
            SetSvg(BtnTokAcc, "svgimages/xaf/actiongroup_default.svg", 16);
            SetSvg(BtnTokName, "svgimages/business objects/bo_customer.svg", 16);
            SetSvg(BtnTokDocs, "svgimages/business objects/bo_order.svg", 16);
        }

        private static void SetSvg(SimpleButton btn, string svgName, int size)
        {
            try
            {
                DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(svgName);
                if (img == null) return;
                btn.ImageOptions.SvgImage = img;
                btn.ImageOptions.SvgImageSize = new System.Drawing.Size(size, size);
                btn.ImageOptions.ImageToTextIndent = 6;
                btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
                btn.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
            }
            catch { }   // icons are cosmetic — never block the dialog
        }

        // ── token buttons: insert at the cursor of whichever editor was last focused ──

        private void BtnTokAcc_Click(object sender, EventArgs e) { InsertToken("{AccNo}"); }
        private void BtnTokName_Click(object sender, EventArgs e) { InsertToken("{CompanyName}"); }
        private void BtnTokDocs_Click(object sender, EventArgs e) { InsertToken("{DocNos}"); }

        private void InsertToken(string token)
        {
            DevExpress.XtraEditors.TextEdit target = _lastFocused != null ? _lastFocused : TxtBody;
            int pos = target.SelectionStart;
            string text = target.Text ?? "";
            if (pos < 0 || pos > text.Length) pos = text.Length;
            target.Text = text.Substring(0, pos) + token + text.Substring(pos);
            target.Focus();
            target.SelectionStart = pos + token.Length;
            target.SelectionLength = 0;
        }

        // ── live preview: exactly what the sample customer would receive ──

        private static string Fill(string text)
        {
            return (text ?? "")
                .Replace("{AccNo}", SAMPLE_ACC)
                .Replace("{CompanyName}", SAMPLE_NAME)
                .Replace("{DocNos}", SAMPLE_DOCS);
        }

        private void UpdatePreview()
        {
            LblPvSubject.Text = Fill(TxtSubject.Text);
            TxtPreview.Text = Fill(TxtBody.Text);
        }

        // ── buttons ──

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) { this.DialogResult = DialogResult.Cancel; return; }
            string subject = (TxtSubject.Text ?? "").Trim();
            if (subject.Length == 0) subject = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            PumsConfig.Set(_dbSetting, PumsConfig.KEY_BULKMAIL_SUBJECT, subject);
            PumsConfig.Set(_dbSetting, PumsConfig.KEY_BULKMAIL_BODY, TxtBody.Text ?? "");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            TxtSubject.Text = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            TxtBody.Text = PumsConfig.DEFAULT_BULKMAIL_BODY;
            UpdatePreview();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
