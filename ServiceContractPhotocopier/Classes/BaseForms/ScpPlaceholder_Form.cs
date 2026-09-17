using System;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.BaseForms
{
    /// <summary>
    /// Generic placeholder form for submodules that are scaffolded but whose full
    /// implementation is deferred to Slice 2 polish via /ralph-loop. Shows the form
    /// caption + a "not yet implemented" notice. Satisfies the form-triple rule while
    /// making the menu navigable end-to-end.
    /// </summary>
    public class ScpPlaceholder_Form : XtraForm
    {
        protected LabelControl LblTitle;
        protected LabelControl LblNotice;
        protected SimpleButton BtnClose;

        protected DBSetting _dbSetting;

        protected virtual string FormCaption { get { return "Not Yet Implemented"; } }
        protected virtual string FormNotice  { get { return "This form is scaffolded. Full UI will land in Slice 2 (/ralph-loop polish)."; } }

        public ScpPlaceholder_Form()
        {
            InitializeBaseLayout();
            // A scaffold is never shown any more: the one Under Maintenance notice opens in its
            // place and this form closes itself. The scaffold stays so the menu entry still exists.
            ServiceContractPhotocopier.Classes.CommonForms.ScpMaintenance_Form.TakeOver(this, FormCaption);
        }

        public ScpPlaceholder_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
        }

        public ScpPlaceholder_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; }

        private void InitializeBaseLayout()
        {
            this.Text = FormCaption;
            this.Width = 800;
            this.Height = 540;
            this.StartPosition = FormStartPosition.CenterParent;

            LblTitle = new LabelControl();
            LblTitle.Text = FormCaption;
            LblTitle.Appearance.Font = new Font("Tahoma", 14F, FontStyle.Bold);
            LblTitle.Location = new Point(24, 20);
            LblTitle.AutoSizeMode = LabelAutoSizeMode.None;
            LblTitle.Width = 600;
            LblTitle.Height = 30;

            LblNotice = new LabelControl();
            LblNotice.Text = FormNotice;
            LblNotice.Appearance.Font = new Font("Tahoma", 10F);
            LblNotice.Location = new Point(24, 60);
            LblNotice.AutoSizeMode = LabelAutoSizeMode.None;
            LblNotice.Width = 720;
            LblNotice.Height = 60;

            BtnClose = new SimpleButton();
            BtnClose.Text = "&Close";
            BtnClose.Location = new Point(700, 475);
            BtnClose.Width = 75;
            BtnClose.Height = 28;
            BtnClose.Click += delegate { this.Close(); };

            this.Controls.Add(LblTitle);
            this.Controls.Add(LblNotice);
            this.Controls.Add(BtnClose);
            this.CancelButton = BtnClose;
        }
    }
}
