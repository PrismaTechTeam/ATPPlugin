using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class StrategyRulesEditorControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private GroupControl GrpRules;
        private SimpleButton BtnRuleAdd, BtnRuleDel, BtnRuleUp, BtnRuleDown;
        private GridControl GridRules; private GridView GridViewRules;

        private GroupControl GrpParams;
        private LabelControl LblRuleKind, LblScope;
        private ComboBoxEdit CmbRuleKind, CmbScope;
        private CheckEdit ChkAllItems;
        private LabelControl LblServiceItem;
        private CheckedComboBoxEdit CmbServiceItems;
        private LabelControl LblTarget, LblPartialPct, LblFreeMonths, LblCommit, LblFocCopies, LblRebatePct, LblLimitScope, LblLimitQty, LblTypeHint;
        private SpinEdit SpnTarget, SpnPartialPct, SpnFreeMonths, SpnCommit, SpnFocCopies, SpnRebatePct, SpnLimitQty;
        private CheckEdit ChkNetBilling;
        private ComboBoxEdit CmbLimitScope;

        private void InitializeComponent()
        {
            this.GrpRules = new GroupControl();
            this.BtnRuleAdd = new SimpleButton(); this.BtnRuleDel = new SimpleButton();
            this.BtnRuleUp = new SimpleButton(); this.BtnRuleDown = new SimpleButton();
            this.GridRules = new GridControl(); this.GridViewRules = new GridView();
            this.GrpParams = new GroupControl();
            this.LblRuleKind = new LabelControl(); this.CmbRuleKind = new ComboBoxEdit();
            this.LblScope = new LabelControl(); this.CmbScope = new ComboBoxEdit();
            this.ChkAllItems = new CheckEdit();
            this.LblServiceItem = new LabelControl();
            this.CmbServiceItems = new CheckedComboBoxEdit();
            this.LblTarget = new LabelControl(); this.SpnTarget = new SpinEdit();
            this.LblPartialPct = new LabelControl(); this.SpnPartialPct = new SpinEdit();
            this.LblFreeMonths = new LabelControl(); this.SpnFreeMonths = new SpinEdit();
            this.LblCommit = new LabelControl(); this.SpnCommit = new SpinEdit();
            this.LblFocCopies = new LabelControl(); this.SpnFocCopies = new SpinEdit();
            this.LblRebatePct = new LabelControl(); this.SpnRebatePct = new SpinEdit();
            this.ChkNetBilling = new CheckEdit();
            this.LblLimitScope = new LabelControl(); this.CmbLimitScope = new ComboBoxEdit();
            this.LblLimitQty = new LabelControl(); this.SpnLimitQty = new SpinEdit();
            this.LblTypeHint = new LabelControl();

            ((System.ComponentModel.ISupportInitialize)(this.GridRules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewRules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbServiceItems.Properties)).BeginInit();
            this.GrpRules.SuspendLayout();
            this.GrpParams.SuspendLayout();
            this.SuspendLayout();

            // ---- Rule editor (right, fixed width) ----
            this.GrpParams.Text = "Rule Parameters  (for the selected rule)";
            this.GrpParams.Dock = DockStyle.Right;
            this.GrpParams.Width = 510;

            int pLx = 14, pEx = 190;
            Lbl(this.LblRuleKind, "Rule Kind", pLx, 30); this.CmbRuleKind.Location = new Point(pEx, 30); this.CmbRuleKind.Width = 300;
            this.CmbRuleKind.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            Lbl(this.LblScope, "Apply FOC/Rebate to", pLx, 62); this.CmbScope.Location = new Point(pEx, 62); this.CmbScope.Width = 300;
            this.CmbScope.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            this.ChkAllItems.Properties.Caption = "Applied for all Service Items";
            this.ChkAllItems.Location = new Point(pLx, 96); this.ChkAllItems.Width = 300;
            Lbl(this.LblServiceItem, "Service Item(s)", pLx, 126);
            this.CmbServiceItems.Location = new Point(pEx, 126); this.CmbServiceItems.Width = 300;
            this.CmbServiceItems.Properties.NullText = "(tick one or more service items)";

            int qY1 = 168, qY2 = 200, qY3 = 232;
            Lbl(this.LblTarget, "Target Amount (RM)", pLx, qY1); Spn(this.SpnTarget, pEx, qY1, 130, 2);
            Lbl(this.LblPartialPct, "Partial waive %", pLx, qY2); Spn(this.SpnPartialPct, pEx, qY2, 130, 2);
            this.SpnPartialPct.Properties.MinValue = 1m;
            this.SpnPartialPct.Properties.MaxValue = 100m;
            Lbl(this.LblFreeMonths, "Free Months (first N)", pLx, qY1); Spn(this.SpnFreeMonths, pEx, qY1, 130, 0);
            Lbl(this.LblCommit, "Committed Amount (RM)", pLx, qY1); Spn(this.SpnCommit, pEx, qY1, 130, 2);
            Lbl(this.LblFocCopies, "FOC Copies", pLx, qY1); Spn(this.SpnFocCopies, pEx, qY1, 130, 0);
            Lbl(this.LblRebatePct, "Rebate %", pLx, qY2); Spn(this.SpnRebatePct, pEx, qY2, 130, 2);
            this.ChkNetBilling.Properties.Caption = "NET billing (deduct FOC + rebate from billed copies)";
            this.ChkNetBilling.Location = new Point(pLx, qY3); this.ChkNetBilling.Width = 470;
            Lbl(this.LblLimitScope, "Limit Scope", pLx, qY1); this.CmbLimitScope.Location = new Point(pEx, qY1); this.CmbLimitScope.Width = 300;
            this.CmbLimitScope.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            Lbl(this.LblLimitQty, "FOC Limit Qty", pLx, qY2); Spn(this.SpnLimitQty, pEx, qY2, 130, 0);
            Lbl(this.LblTypeHint, "Pick a rule kind to configure its parameters.", pLx, 280);
            // Hint wraps inside the panel instead of running past its right edge (clipped text).
            this.LblTypeHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.LblTypeHint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblTypeHint.Appearance.Options.UseTextOptions = true;
            this.LblTypeHint.Width = 470;

            this.GrpParams.Controls.Add(this.LblRuleKind); this.GrpParams.Controls.Add(this.CmbRuleKind);
            this.GrpParams.Controls.Add(this.LblScope); this.GrpParams.Controls.Add(this.CmbScope);
            this.GrpParams.Controls.Add(this.ChkAllItems);
            this.GrpParams.Controls.Add(this.LblServiceItem); this.GrpParams.Controls.Add(this.CmbServiceItems);
            this.GrpParams.Controls.Add(this.LblTarget); this.GrpParams.Controls.Add(this.SpnTarget);
            this.GrpParams.Controls.Add(this.LblPartialPct); this.GrpParams.Controls.Add(this.SpnPartialPct);
            this.GrpParams.Controls.Add(this.LblFreeMonths); this.GrpParams.Controls.Add(this.SpnFreeMonths);
            this.GrpParams.Controls.Add(this.LblCommit); this.GrpParams.Controls.Add(this.SpnCommit);
            this.GrpParams.Controls.Add(this.LblFocCopies); this.GrpParams.Controls.Add(this.SpnFocCopies);
            this.GrpParams.Controls.Add(this.LblRebatePct); this.GrpParams.Controls.Add(this.SpnRebatePct);
            this.GrpParams.Controls.Add(this.ChkNetBilling);
            this.GrpParams.Controls.Add(this.LblLimitScope); this.GrpParams.Controls.Add(this.CmbLimitScope);
            this.GrpParams.Controls.Add(this.LblLimitQty); this.GrpParams.Controls.Add(this.SpnLimitQty);
            this.GrpParams.Controls.Add(this.LblTypeHint);

            // ---- Rules list (fills the rest) ----
            this.GrpRules.Text = "Rules  (one strategy = many rules; mix any kinds)";
            this.GrpRules.Dock = DockStyle.Fill;
            Tb2(this.BtnRuleAdd, "+ Add Rule", 12, 26, 100); this.BtnRuleAdd.Click += new System.EventHandler(this.OnRuleAdd);
            Tb2(this.BtnRuleDel, "Delete Rule", 118, 26, 100); this.BtnRuleDel.Click += new System.EventHandler(this.OnRuleDel);
            Tb2(this.BtnRuleUp, "Up", 224, 26, 56); this.BtnRuleUp.Click += new System.EventHandler(this.OnRuleUp);
            Tb2(this.BtnRuleDown, "Down", 284, 26, 60); this.BtnRuleDown.Click += new System.EventHandler(this.OnRuleDown);
            this.GridRules.Location = new Point(12, 62); this.GridRules.Size = new Size(560, 300);
            this.GridRules.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.GridViewRules.GridControl = this.GridRules; this.GridViewRules.OptionsView.ShowGroupPanel = false;
            this.GridViewRules.OptionsBehavior.Editable = false; this.GridViewRules.OptionsView.ShowIndicator = false;
            this.GridRules.MainView = this.GridViewRules; this.GridRules.ViewCollection.Add(this.GridViewRules);
            this.GrpRules.Controls.Add(this.BtnRuleAdd); this.GrpRules.Controls.Add(this.BtnRuleDel);
            this.GrpRules.Controls.Add(this.BtnRuleUp); this.GrpRules.Controls.Add(this.BtnRuleDown);
            this.GrpRules.Controls.Add(this.GridRules);

            this.Controls.Add(this.GrpRules);
            this.Controls.Add(this.GrpParams);
            this.Name = "StrategyRulesEditorControl";
            this.Size = new Size(1120, 380);

            ((System.ComponentModel.ISupportInitialize)(this.CmbServiceItems.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewRules)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridRules)).EndInit();
            this.GrpParams.ResumeLayout(false);
            this.GrpRules.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static void Tb2(SimpleButton b, string t, int x, int y, int w)
        { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 28; }
        private static void Lbl(LabelControl l, string t, int x, int y) { l.Text = t; l.Location = new Point(x, y + 3); }
        private static void Spn(SpinEdit s, int x, int y, int w, int decimals)
        {
            s.Location = new Point(x, y); s.Width = w;
            s.Properties.IsFloatValue = decimals > 0;
            s.Properties.MaxValue = 999999999m;
            s.Properties.MinValue = 0m;
            s.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            s.Properties.DisplayFormat.FormatString = decimals > 0 ? "n2" : "n0";
        }
    }
}
