using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Bulk Email template MAINTENANCE (demo #9a + user request 01/08): named template versions
    /// live in zSCP2_EmailTemplate — create as many as needed, edit each, and mark ONE as the
    /// default that Bulk Email Invoice sends with. Left side edits (token buttons insert the
    /// placeholders at the cursor), right side live-renders what the customer receives; the
    /// "Professional" style wraps the plain text in the styled HTML frame at send time.
    /// </summary>
    public partial class BulkEmailTemplate_Form : XtraForm
    {
        private readonly DBSetting _dbSetting;
        private DevExpress.XtraEditors.TextEdit _lastFocused;   // where a token button inserts (MemoEdit derives TextEdit)
        private readonly List<ScpEmailTemplates.Template> _templates = new List<ScpEmailTemplates.Template>();
        private ScpEmailTemplates.Template _current;
        private bool _loadingTpl;
        private string _senderCompany = "Your Company";

        private const string SAMPLE_ACC = "3000-A0011";
        private const string SAMPLE_NAME = "ATRIA ARCHITECT SDN BHD";
        private const string SAMPLE_DOCS = "MR2607.0001, MR2607.0002, MR2607.0003";

        public BulkEmailTemplate_Form()
        {
            InitializeComponent();
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            CmbStyle.SelectedIndex = 0;
            CmbStyle.ToolTip = "Plain text sends exactly what you type. Professional wraps your text " +
                "in a styled layout (company header bar, clean typography, footer) — the customer " +
                "sees a polished HTML email; you still just type plain text.";
            _lastFocused = TxtBody;
            TxtSubject.Enter += delegate { _lastFocused = TxtSubject; };
            TxtBody.Enter += delegate { _lastFocused = TxtBody; };
            TxtSubject.EditValueChanged += delegate { UpdatePreview(); };
            TxtBody.EditValueChanged += delegate { UpdatePreview(); };
            CmbStyle.SelectedIndexChanged += delegate { UpdatePreview(); };
            CmbTemplate.SelectedIndexChanged += new EventHandler(CmbTemplate_SelectedIndexChanged);
            this.FormClosing += new FormClosingEventHandler(OnClosingConfirm);
        }

        public BulkEmailTemplate_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            if (_dbSetting != null)
            {
                try
                {
                    System.Data.DataTable p = _dbSetting.GetDataTable(
                        "SELECT CompanyName FROM dbo.Profile", false);
                    if (p.Rows.Count > 0 && Convert.ToString(p.Rows[0]["CompanyName"]).Trim().Length > 0)
                        _senderCompany = Convert.ToString(p.Rows[0]["CompanyName"]).Trim();
                }
                catch { }
                LoadTemplates(0);
            }
            UpdatePreview();
        }

        // ── template list ──

        private void LoadTemplates(long selectKey)
        {
            _loadingTpl = true;
            try
            {
                _templates.Clear();
                CmbTemplate.Properties.Items.Clear();
                try
                {
                    System.Data.DataTable dt = ScpEmailTemplates.List(_dbSetting);
                    foreach (System.Data.DataRow r in dt.Rows)
                        _templates.Add(ScpEmailTemplates.FromRow(r));
                }
                catch { }
                int pick = -1;
                for (int i = 0; i < _templates.Count; i++)
                {
                    ScpEmailTemplates.Template t = _templates[i];
                    CmbTemplate.Properties.Items.Add(t.Name + (t.IsDefault ? "   ★ default" : ""));
                    if (selectKey > 0 ? t.Key == selectKey : (pick < 0 && t.IsDefault)) pick = i;
                }
                if (pick < 0 && _templates.Count > 0) pick = 0;
                if (pick >= 0) { CmbTemplate.SelectedIndex = pick; ApplyTemplate(_templates[pick]); }
            }
            finally { _loadingTpl = false; }
        }

        private void ApplyTemplate(ScpEmailTemplates.Template t)
        {
            _current = t;
            TxtSubject.Text = t.Subject;
            TxtBody.Text = t.Body;
            CmbStyle.SelectedIndex = t.Styled ? 1 : 0;
            UpdatePreview();
        }

        private bool HasUnsavedChanges()
        {
            if (_current == null) return false;
            return (TxtSubject.Text ?? "") != _current.Subject
                || (TxtBody.Text ?? "") != _current.Body
                || (CmbStyle.SelectedIndex == 1) != _current.Styled;
        }

        private void CmbTemplate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingTpl) return;
            int i = CmbTemplate.SelectedIndex;
            if (i < 0 || i >= _templates.Count) return;
            if (_current != null && _templates[i].Key == _current.Key) return;
            if (HasUnsavedChanges() &&
                XtraMessageBox.Show("Discard the unsaved changes on '" + _current.Name + "'?",
                    "Bulk Email Template", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                _loadingTpl = true;
                try { for (int j = 0; j < _templates.Count; j++) if (_templates[j].Key == _current.Key) CmbTemplate.SelectedIndex = j; }
                finally { _loadingTpl = false; }
                return;
            }
            ApplyTemplate(_templates[i]);
        }

        private void BtnTplNew_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            string name = XtraInputBox.Show("Name for the new template version:", "New Template", "");
            if (string.IsNullOrEmpty(name) || name.Trim().Length == 0) return;
            name = name.Trim();
            try
            {
                long key = ScpEmailTemplates.Insert(_dbSetting, name,
                    TxtSubject.Text, TxtBody.Text, CmbStyle.SelectedIndex == 1);
                LoadTemplates(key);
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create failed (name already used?):\r\n" + ex.Message, "New Template"); }
        }

        private void BtnTplDelete_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _current == null) return;
            if (_templates.Count <= 1)
            { XtraMessageBox.Show("At least one template must remain.", "Delete Template"); return; }
            if (XtraMessageBox.Show("Delete template '" + _current.Name + "'?", "Delete Template",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { ScpEmailTemplates.Delete(_dbSetting, _current.Key); _current = null; LoadTemplates(0); }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Delete Template"); }
        }

        private void BtnTplDefault_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _current == null) return;
            try { ScpEmailTemplates.SetDefault(_dbSetting, _current.Key); LoadTemplates(_current.Key); }
            catch (Exception ex) { XtraMessageBox.Show("Set default failed:\r\n" + ex.Message, "Set Default"); }
        }

        private void ApplyButtonIcons()
        {
            SetSvg(BtnSave, "svgimages/outlook inspired/saveas.svg", 20);
            SetSvg(BtnDefault, "svgimages/xaf/action_reload.svg", 20);
            SetSvg(BtnTplNew, "svgimages/icon builder/actions_add.svg", 16);
            SetSvg(BtnTplDelete, "svgimages/icon builder/actions_delete.svg", 16);
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
            bool styled = CmbStyle.SelectedIndex == 1;
            TxtPreview.Visible = !styled;
            WebPreview.Visible = styled;
            if (styled)
            {
                try { WebPreview.DocumentText = ScpMailHtml.BuildStyled(Fill(TxtBody.Text), _senderCompany); }
                catch { TxtPreview.Visible = true; WebPreview.Visible = false; TxtPreview.Text = Fill(TxtBody.Text); }
            }
            else
            {
                TxtPreview.Text = Fill(TxtBody.Text);
            }
        }

        // ── buttons ──

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _current == null) { this.DialogResult = DialogResult.Cancel; return; }
            string subject = (TxtSubject.Text ?? "").Trim();
            if (subject.Length == 0) subject = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            try
            {
                ScpEmailTemplates.Update(_dbSetting, _current.Key, subject, TxtBody.Text ?? "",
                    CmbStyle.SelectedIndex == 1);
                LoadTemplates(_current.Key);   // refresh so HasUnsavedChanges is clean
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Bulk Email Template"); }
        }

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            TxtSubject.Text = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            TxtBody.Text = PumsConfig.DEFAULT_BULKMAIL_BODY;
            UpdatePreview();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnClosingConfirm(object sender, FormClosingEventArgs e)
        {
            if (!HasUnsavedChanges()) return;
            if (XtraMessageBox.Show("You have unsaved changes on '" + _current.Name + "'. Discard them and close?",
                    "Bulk Email Template", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                e.Cancel = true;
        }
    }
}
