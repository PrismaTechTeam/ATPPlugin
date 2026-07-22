using System;
using System.Collections.Generic;
using System.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// Reusable strategy-rules BUILDER: a rules grid (Add/Delete/Up/Down) + a per-rule parameter editor.
    /// Shared by Strategy Maintenance (master template, no service-item binding) and the contract's
    /// Strategy tab (contract copy, with a per-rule "Applied for all Service Items" checkbox + Service
    /// Item picker). Works on a List&lt;StrategyRule&gt; via an internal DataTable; the host handles
    /// persistence. Call <see cref="SetServiceItems"/> once (null = master mode), then LoadRules / GetRules.
    /// </summary>
    public partial class StrategyRulesEditorControl : DevExpress.XtraEditors.XtraUserControl
    {
        private DataTable _rules;
        private bool _editable = true;
        private bool _loadingRule = false;
        private bool _hasServiceItems = false;   // true = contract mode (service-item binding shown)
        private DataTable _serviceItems;          // ItemKey, ServiceItemNo (contract's items)

        private static readonly string[] TypeCodes = new string[]
        {
            "", ScpStrategy.TYPE_WAIVE_TARGET, ScpStrategy.TYPE_RENTAL_FREE_N, ScpStrategy.TYPE_COMMIT_MIN,
            ScpStrategy.TYPE_FOC_REBATE, ScpStrategy.TYPE_INITIAL_METER, ScpStrategy.TYPE_LIMIT
        };
        public static readonly string[] TypeNames = new string[]
        {
            "(none)", "Waive Rental - by Target (RM)", "Rental Free - first N months",
            "Committed Print Charges", "FOC + Rebate", "Initial Meter", "FOC Limit (single/group)"
        };
        // Scope only applies to FOC-REBATE (which usage meters get the FOC + rebate). Every other rule
        // kind has an implicit target (WAIVE-TARGET waives the rental, RENTAL-FREE-N -> rental, etc.), so
        // the Scope picker is hidden for them.
        private static readonly string[] ScopeCodes = new string[] { "BK", "CL", "BKCL" };
        public static readonly string[] ScopeNames = new string[]
        { "Black (BK) usage", "Colour (CL) usage", "BK + CL usage" };

        /// <summary>Raised when the user edits/adds/deletes/reorders a rule (not on programmatic LoadRules).
        /// Hosts use it to set their dirty flag.</summary>
        public event EventHandler RulesChanged;
        private void RaiseChanged() { if (RulesChanged != null) RulesChanged(this, EventArgs.Empty); }

        public StrategyRulesEditorControl()
        {
            InitializeComponent();
            BuildTable();
            CmbRuleKind.Properties.Items.AddRange(TypeNames);
            CmbScope.Properties.Items.AddRange(ScopeNames);
            CmbLimitScope.Properties.Items.AddRange(new object[] { "Single machine", "Group (pooled via .C combine item)" });
            GridViewRules.FocusedRowChanged += delegate { PopulateRuleEditor(); };
            CmbRuleKind.SelectedIndexChanged += new EventHandler(RuleEditor_KindChanged);
            CmbScope.SelectedIndexChanged += new EventHandler(RuleEditor_Changed);
            ChkAllItems.CheckedChanged += new EventHandler(ChkAllItems_Changed);
            CmbServiceItems.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnTarget.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnPartialPct.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnFreeMonths.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnCommit.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnFocCopies.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SpnRebatePct.EditValueChanged += new EventHandler(RuleEditor_Changed);
            ChkNetBilling.CheckedChanged += new EventHandler(RuleEditor_Changed);
            CmbLimitScope.SelectedIndexChanged += new EventHandler(RuleEditor_Changed);
            SpnLimitQty.EditValueChanged += new EventHandler(RuleEditor_Changed);
            SetServiceItems(null);
            PopulateRuleEditor();
        }

        private void BuildTable()
        {
            _rules = new DataTable();
            _rules.Columns.Add("Seq", typeof(int));
            _rules.Columns.Add("RuleKind", typeof(string));
            _rules.Columns.Add("Scope", typeof(string));
            _rules.Columns.Add("ServiceItemKeys", typeof(string));
            _rules.Columns.Add("TargetAmount", typeof(decimal));
            _rules.Columns.Add("PartialPct", typeof(decimal));
            _rules.Columns.Add("FreeMonths", typeof(int));
            _rules.Columns.Add("CommitAmount", typeof(decimal));
            _rules.Columns.Add("FocCopies", typeof(decimal));
            _rules.Columns.Add("RebatePct", typeof(decimal));
            _rules.Columns.Add("NetBilling", typeof(string));
            _rules.Columns.Add("LimitScope", typeof(string));
            _rules.Columns.Add("LimitQty", typeof(decimal));
            _rules.Columns.Add("KindName", typeof(string));
            _rules.Columns.Add("ScopeName", typeof(string));
            _rules.Columns.Add("ItemName", typeof(string));
            _rules.Columns.Add("Params", typeof(string));
            GridRules.DataSource = _rules;
            ConfigureRuleColumns();
        }

        private void ConfigureRuleColumns()
        {
            foreach (string hide in new string[] { "RuleKind", "Scope", "ServiceItemKeys", "TargetAmount", "PartialPct",
                "FreeMonths", "CommitAmount", "FocCopies", "RebatePct", "NetBilling", "LimitScope", "LimitQty" })
                if (GridViewRules.Columns[hide] != null) GridViewRules.Columns[hide].Visible = false;
            SetRuleCap("Seq", "#", 40, 0);
            SetRuleCap("KindName", "Rule Kind", 180, 1);
            SetRuleCap("ScopeName", "Applies to", 120, 2);
            SetRuleCap("ItemName", "Service Item", 150, 3);
            SetRuleCap("Params", "Parameters", 240, 4);
        }

        private void SetRuleCap(string field, string caption, int width, int idx)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewRules.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = idx;
        }

        /// <summary>Feed the contract's service items (columns ItemKey, ServiceItemNo). Null/empty = master
        /// mode: the per-rule service-item binding controls are hidden. Non-null = contract mode.</summary>
        public void SetServiceItems(DataTable items)
        {
            _serviceItems = items;
            _hasServiceItems = items != null && items.Columns.Contains("ItemKey");
            ChkAllItems.Visible = _hasServiceItems;
            LblServiceItem.Visible = _hasServiceItems;
            CmbServiceItems.Visible = _hasServiceItems;
            if (_hasServiceItems)
            {
                CmbServiceItems.Properties.DataSource = items;
                CmbServiceItems.Properties.DisplayMember = "ServiceItemNo";
                CmbServiceItems.Properties.ValueMember = "ItemKey";
            }
            // Rebind ItemName display for any loaded rows.
            if (_rules != null) foreach (DataRow r in _rules.Rows) RefreshRuleDisplay(r);
            PopulateRuleEditor();
        }

        /// <summary>Replace the grid's rules from a list (e.g. a master template or the contract's saved rules).</summary>
        public void LoadRules(List<StrategyRule> rules)
        {
            _rules.Rows.Clear();
            if (rules != null)
            {
                foreach (StrategyRule s in rules)
                {
                    DataRow n = _rules.NewRow();
                    n["Seq"] = s.Seq;
                    n["RuleKind"] = s.Kind ?? "";
                    n["Scope"] = s.Scope ?? "";
                    n["ServiceItemKeys"] = ScpStrategy.JoinItemKeys(s.ServiceItemKeys);
                    n["TargetAmount"] = s.TargetAmount;
                    n["PartialPct"] = s.PartialPct;
                    n["FreeMonths"] = s.FreeMonths;
                    n["CommitAmount"] = s.CommitAmount;
                    n["FocCopies"] = s.FocCopies;
                    n["RebatePct"] = s.RebatePct;
                    n["NetBilling"] = s.NetBilling ? "Y" : "N";
                    n["LimitScope"] = s.LimitScope == 'G' ? "G" : "S";
                    n["LimitQty"] = s.LimitQty;
                    RefreshRuleDisplay(n);
                    _rules.Rows.Add(n);
                }
            }
            RenumberRules();
            if (_rules.Rows.Count > 0) GridViewRules.FocusedRowHandle = 0;
            PopulateRuleEditor();
        }

        /// <summary>Read the current rules back (skips blank-kind rows). Seq is renumbered 1..n.</summary>
        public List<StrategyRule> GetRules()
        {
            List<StrategyRule> list = new List<StrategyRule>();
            int seq = 0;
            foreach (DataRow r in _rules.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (SV(r, "RuleKind").Length == 0) continue;
                seq++;
                StrategyRule x = new StrategyRule();
                x.Seq = seq;
                x.Kind = SV(r, "RuleKind");
                x.Scope = SV(r, "Scope");
                ScpStrategy.ParseItemKeys(SV(r, "ServiceItemKeys"), x.ServiceItemKeys);
                x.TargetAmount = Dec(r, "TargetAmount");
                x.PartialPct = Dec(r, "PartialPct");
                x.FreeMonths = r["FreeMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["FreeMonths"]);
                x.CommitAmount = Dec(r, "CommitAmount");
                x.FocCopies = Dec(r, "FocCopies");
                x.RebatePct = Dec(r, "RebatePct");
                x.NetBilling = SV(r, "NetBilling") == "Y";
                x.LimitScope = SV(r, "LimitScope") == "G" ? 'G' : 'S';
                x.LimitQty = Dec(r, "LimitQty");
                list.Add(x);
            }
            return list;
        }

        /// <summary>True if every non-blank rule is valid. Currently only requires a Rule Kind.</summary>
        public bool ValidateRules(out string error)
        {
            foreach (DataRow r in _rules.Rows)
                if (r.RowState != DataRowState.Deleted && SV(r, "RuleKind").Length == 0)
                { error = "One of the rules has no Rule Kind. Pick a kind or delete that rule."; return false; }
            error = "";
            return true;
        }

        public void SetEditable(bool editable)
        {
            _editable = editable;
            BtnRuleAdd.Enabled = editable;
            BtnRuleDel.Enabled = editable;
            BtnRuleUp.Enabled = editable;
            BtnRuleDown.Enabled = editable;
            bool ro = !editable;
            CmbRuleKind.Properties.ReadOnly = ro;
            CmbScope.Properties.ReadOnly = ro;
            ChkAllItems.Properties.ReadOnly = ro;
            CmbServiceItems.Properties.ReadOnly = ro;
            SpnTarget.Properties.ReadOnly = ro; SpnPartialPct.Properties.ReadOnly = ro;
            SpnFreeMonths.Properties.ReadOnly = ro; SpnCommit.Properties.ReadOnly = ro;
            SpnFocCopies.Properties.ReadOnly = ro; SpnRebatePct.Properties.ReadOnly = ro;
            ChkNetBilling.Properties.ReadOnly = ro; CmbLimitScope.Properties.ReadOnly = ro;
            SpnLimitQty.Properties.ReadOnly = ro;
            UpdateRuleEditorState();
        }

        private static string TypeDisplay(string code)
        { for (int i = 0; i < TypeCodes.Length; i++) if (TypeCodes[i] == code) return TypeNames[i]; return code; }
        private static string ScopeDisplay(string code)
        { for (int i = 0; i < ScopeCodes.Length; i++) if (ScopeCodes[i] == code) return ScopeNames[i]; return code; }

        // Grid "Applies to" text: FOC-REBATE shows its usage scope; other kinds show their implicit target.
        private static string AppliesToDisplay(string kind, string scope)
        {
            if (kind == ScpStrategy.TYPE_FOC_REBATE) return ScopeDisplay(scope);
            if (kind == ScpStrategy.TYPE_RENTAL_FREE_N) return "Rental";
            if (kind == ScpStrategy.TYPE_WAIVE_TARGET) return "Waive rental (count " + ScopeDisplay(scope) + ")";
            if (kind == ScpStrategy.TYPE_COMMIT_MIN) return "Min chg · " + ScopeDisplay(scope);
            if (kind == ScpStrategy.TYPE_INITIAL_METER) return "Initial · " + ScopeDisplay(scope);
            if (kind == ScpStrategy.TYPE_LIMIT) return "Group FOC cap · " + ScopeDisplay(scope);
            return "—";
        }
        private static int KindIndex(string code)
        { for (int i = 0; i < TypeCodes.Length; i++) if (TypeCodes[i] == code) return i; return 0; }
        // Scope index for the FOC combo; default BK+CL (index 2) when unset/unknown.
        private static int ScopeIndex(string code)
        { for (int i = 0; i < ScopeCodes.Length; i++) if (ScopeCodes[i] == code) return i; return 2; }

        // Every kind that targets usage (BK/CL) meters carries a Scope; only RENTAL-FREE-N (rental) doesn't.
        private static bool KindUsesScope(string code)
        {
            return code == ScpStrategy.TYPE_WAIVE_TARGET || code == ScpStrategy.TYPE_FOC_REBATE
                || code == ScpStrategy.TYPE_COMMIT_MIN || code == ScpStrategy.TYPE_INITIAL_METER
                || code == ScpStrategy.TYPE_LIMIT;
        }

        private string ItemDisplay(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return "All items";
            List<long> keys = new List<long>();
            ScpStrategy.ParseItemKeys(csv, keys);
            if (keys.Count == 0) return "All items";
            List<string> names = new List<string>();
            foreach (long k in keys) names.Add(OneItemName(k));
            if (names.Count <= 2) return string.Join(", ", names.ToArray());
            return names.Count + " items";
        }

        private string OneItemName(long key)
        {
            if (_serviceItems != null && _serviceItems.Columns.Contains("ItemKey"))
                foreach (DataRow r in _serviceItems.Rows)
                    if (r["ItemKey"] != DBNull.Value && Convert.ToInt64(r["ItemKey"]) == key)
                        return r.Table.Columns.Contains("ServiceItemNo") ? Convert.ToString(r["ServiceItemNo"]) : ("#" + key);
            return "#" + key;
        }

        private static string RuleParamsSummary(DataRow r)
        {
            string t = SV(r, "RuleKind");
            if (t == ScpStrategy.TYPE_WAIVE_TARGET)
            {
                string s = "Target RM " + D(r, "TargetAmount", "0.00");
                decimal pp = Dec(r, "PartialPct");
                if (pp > 0m) s += "  /  reach " + pp.ToString("0.##") + "% waive " + pp.ToString("0.##") + "%";
                return s;
            }
            if (t == ScpStrategy.TYPE_RENTAL_FREE_N) return "First " + SV(r, "FreeMonths") + " month(s) free";
            if (t == ScpStrategy.TYPE_COMMIT_MIN) return "Committed RM " + D(r, "CommitAmount", "0.00");
            if (t == ScpStrategy.TYPE_FOC_REBATE)
                return "FOC " + D(r, "FocCopies", "0") + " / rebate " + D(r, "RebatePct", "0.##") + "%" +
                       (SV(r, "NetBilling") == "Y" ? "  (NET)" : "  (raw)");
            if (t == ScpStrategy.TYPE_INITIAL_METER) return "Estimate readings keyed manually";
            if (t == ScpStrategy.TYPE_LIMIT)
                return (SV(r, "LimitScope") == "G" ? "GROUP pooled" : "Single machine") + " FOC limit " + D(r, "LimitQty", "0");
            return "(pick a rule kind)";
        }

        private void RefreshRuleDisplay(DataRow n)
        {
            n["KindName"] = TypeDisplay(SV(n, "RuleKind"));
            n["ScopeName"] = AppliesToDisplay(SV(n, "RuleKind"), SV(n, "Scope"));
            n["ItemName"] = ItemDisplay(SV(n, "ServiceItemKeys"));
            n["Params"] = RuleParamsSummary(n);
        }

        private void RenumberRules()
        { int i = 1; foreach (DataRow r in _rules.Rows) r["Seq"] = i++; }

        private DataRow FocusedRule()
        {
            int rh = GridViewRules.FocusedRowHandle;
            if (rh < 0) return null;
            return GridViewRules.GetDataRow(rh);
        }

        private void PopulateRuleEditor()
        {
            _loadingRule = true;
            try
            {
                DataRow r = FocusedRule();
                if (r == null)
                {
                    CmbRuleKind.SelectedIndex = 0; CmbScope.SelectedIndex = 0;
                    ChkAllItems.Checked = true; CmbServiceItems.EditValue = null;
                    SpnTarget.Value = 0m; SpnPartialPct.Value = 0m; SpnFreeMonths.Value = 0m;
                    SpnCommit.Value = 0m; SpnFocCopies.Value = 0m; SpnRebatePct.Value = 0m;
                    ChkNetBilling.Checked = false; CmbLimitScope.SelectedIndex = 0; SpnLimitQty.Value = 0m;
                    ShowParamFields("");
                    return;
                }
                CmbRuleKind.SelectedIndex = KindIndex(SV(r, "RuleKind"));
                CmbScope.SelectedIndex = ScopeIndex(SV(r, "Scope"));
                string csv = SV(r, "ServiceItemKeys");
                ChkAllItems.Checked = csv.Length == 0;
                CmbServiceItems.EditValue = csv.Length == 0 ? (object)null : csv;
                SpnTarget.Value = Dec(r, "TargetAmount");
                SpnPartialPct.Value = Dec(r, "PartialPct");
                SpnFreeMonths.Value = Dec(r, "FreeMonths");
                SpnCommit.Value = Dec(r, "CommitAmount");
                SpnFocCopies.Value = Dec(r, "FocCopies");
                SpnRebatePct.Value = Dec(r, "RebatePct");
                ChkNetBilling.Checked = SV(r, "NetBilling") == "Y";
                CmbLimitScope.SelectedIndex = SV(r, "LimitScope") == "G" ? 1 : 0;
                SpnLimitQty.Value = Dec(r, "LimitQty");
                ShowParamFields(SV(r, "RuleKind"));
            }
            finally { _loadingRule = false; UpdateRuleEditorState(); }
        }

        private void UpdateRuleEditorState()
        {
            bool hasRule = FocusedRule() != null;
            bool on = hasRule && _editable;
            CmbRuleKind.Enabled = hasRule; CmbScope.Enabled = hasRule;
            SpnTarget.Enabled = hasRule; SpnPartialPct.Enabled = hasRule;
            SpnFreeMonths.Enabled = hasRule; SpnCommit.Enabled = hasRule;
            SpnFocCopies.Enabled = hasRule; SpnRebatePct.Enabled = hasRule;
            ChkNetBilling.Enabled = hasRule; CmbLimitScope.Enabled = hasRule; SpnLimitQty.Enabled = hasRule;
            LblRuleKind.Enabled = hasRule; LblScope.Enabled = hasRule;
            ChkAllItems.Enabled = hasRule && _hasServiceItems;
            LblServiceItem.Enabled = hasRule && _hasServiceItems;
            CmbServiceItems.Enabled = hasRule && _hasServiceItems && !ChkAllItems.Checked;
            if (!hasRule)
            {
                LblTypeHint.Visible = true;
                LblTypeHint.Text = _editable ? "Click \"+ Add Rule\" to add a rule." : "No rules.";
            }
        }

        private void RuleEditor_Changed(object sender, EventArgs e) { WriteEditorToRule(); }
        private void RuleEditor_KindChanged(object sender, EventArgs e)
        {
            int i = CmbRuleKind.SelectedIndex;
            ShowParamFields(i >= 0 && i < TypeCodes.Length ? TypeCodes[i] : "");
            WriteEditorToRule();
        }
        private void ChkAllItems_Changed(object sender, EventArgs e)
        {
            CmbServiceItems.Enabled = _editable && _hasServiceItems && !ChkAllItems.Checked && FocusedRule() != null;
            if (ChkAllItems.Checked) { if (!_loadingRule) CmbServiceItems.EditValue = null; }
            WriteEditorToRule();
        }

        private void WriteEditorToRule()
        {
            if (_loadingRule || !_editable) return;
            DataRow r = FocusedRule();
            if (r == null) return;
            int ki = CmbRuleKind.SelectedIndex; if (ki < 0) ki = 0;
            int si = CmbScope.SelectedIndex; if (si < 0) si = 2;
            r["RuleKind"] = TypeCodes[ki];
            // Scope applies to every usage-touching kind (all but RENTAL-FREE-N).
            r["Scope"] = KindUsesScope(TypeCodes[ki]) ? ScopeCodes[si] : "";
            string csv = "";
            if (_hasServiceItems && !ChkAllItems.Checked && CmbServiceItems.EditValue != null && CmbServiceItems.EditValue != DBNull.Value)
                csv = CmbServiceItems.EditValue.ToString();
            r["ServiceItemKeys"] = csv;
            r["TargetAmount"] = SpnTarget.Value;
            r["PartialPct"] = SpnPartialPct.Value;
            r["FreeMonths"] = (int)SpnFreeMonths.Value;
            r["CommitAmount"] = SpnCommit.Value;
            r["FocCopies"] = SpnFocCopies.Value;
            r["RebatePct"] = SpnRebatePct.Value;
            r["NetBilling"] = ChkNetBilling.Checked ? "Y" : "N";
            r["LimitScope"] = "G";   // FOC Limit is always group-pooled
            r["LimitQty"] = SpnLimitQty.Value;
            RefreshRuleDisplay(r);
            GridViewRules.RefreshRow(GridViewRules.FocusedRowHandle);
            RaiseChanged();
        }

        private void ShowParamFields(string type)
        {
            bool waive = type == ScpStrategy.TYPE_WAIVE_TARGET;
            bool freeN = type == ScpStrategy.TYPE_RENTAL_FREE_N;
            bool commit = type == ScpStrategy.TYPE_COMMIT_MIN;
            bool foc = type == ScpStrategy.TYPE_FOC_REBATE;
            bool init = type == ScpStrategy.TYPE_INITIAL_METER;
            bool limit = type == ScpStrategy.TYPE_LIMIT;

            // Scope (BK / CL / BK+CL usage meters) applies to every kind that touches usage meters —
            // WAIVE-TARGET (which usage COUNTS toward target), FOC-REBATE / COMMIT-MIN / INITIAL-METER /
            // FOC-LIMIT (which usage meters get filled). Only RENTAL-FREE-N (rental) has no BK/CL scope.
            bool usesScope = KindUsesScope(type);
            LblScope.Visible = CmbScope.Visible = usesScope;
            LblScope.Text = waive ? "Count usage from" : (foc ? "Apply FOC/Rebate to" : "Apply to (BK/CL)");
            LblTarget.Visible = SpnTarget.Visible = waive;
            LblPartialPct.Visible = SpnPartialPct.Visible = waive;
            LblFreeMonths.Visible = SpnFreeMonths.Visible = freeN;
            LblCommit.Visible = SpnCommit.Visible = commit;
            LblFocCopies.Visible = SpnFocCopies.Visible = foc;
            LblRebatePct.Visible = SpnRebatePct.Visible = foc;
            ChkNetBilling.Visible = foc;
            LblLimitScope.Visible = CmbLimitScope.Visible = false;   // FOC Limit is ALWAYS group-pooled now
            LblLimitQty.Visible = SpnLimitQty.Visible = limit;
            LblTypeHint.Visible = true;
            if (waive)
                LblTypeHint.Text = "Waive rental when the month's metered charges reach Target. Partial waive %: reach X% of Target → waive X% of rental (0 = all-or-nothing).";
            else if (freeN)
                LblTypeHint.Text = "First N rental periods are free (counts down the rental meter's free months).";
            else if (commit)
                LblTypeHint.Text = "Committed minimum charge — enforced as the meter's minimum floor.";
            else if (foc)
                LblTypeHint.Text = "FOC free copies + Rebate %. NET billing deducts them from billed copies (else bills raw usage).";
            else if (init)
                LblTypeHint.Text = "Initial Meter: estimate readings are keyed manually in Meter Reading; this rule tags the contract.";
            else if (limit)
                LblTypeHint.Text = "GROUP FOC cap — the whole group shares ONE free-copy pool, accumulated across the contract's machines (via the '.C' combine item). Excess is charged.";
            else
                LblTypeHint.Text = "Pick a rule kind to configure its parameters.";
        }

        private void OnRuleAdd(object sender, EventArgs e)
        {
            if (!_editable) return;
            DataRow n = _rules.NewRow();
            n["Seq"] = _rules.Rows.Count + 1;
            n["RuleKind"] = ""; n["Scope"] = ""; n["ServiceItemKeys"] = "";
            n["TargetAmount"] = 0m; n["PartialPct"] = 0m; n["FreeMonths"] = 0;
            n["CommitAmount"] = 0m; n["FocCopies"] = 0m; n["RebatePct"] = 0m;
            n["NetBilling"] = "N"; n["LimitScope"] = "S"; n["LimitQty"] = 0m;
            RefreshRuleDisplay(n);
            _rules.Rows.Add(n);
            GridViewRules.FocusedRowHandle = _rules.Rows.Count - 1;
            PopulateRuleEditor();
            CmbRuleKind.Focus();
            RaiseChanged();
        }

        private void OnRuleDel(object sender, EventArgs e)
        {
            if (!_editable) return;
            DataRow r = FocusedRule();
            if (r == null) return;
            r.Delete();
            _rules.AcceptChanges();
            RenumberRules();
            PopulateRuleEditor();
            RaiseChanged();
        }

        private void OnRuleUp(object sender, EventArgs e) { MoveRule(-1); }
        private void OnRuleDown(object sender, EventArgs e) { MoveRule(+1); }

        private void MoveRule(int dir)
        {
            if (!_editable) return;
            int rh = GridViewRules.FocusedRowHandle;
            if (rh < 0) return;
            int target = rh + dir;
            if (target < 0 || target >= _rules.Rows.Count) return;
            DataRow a = GridViewRules.GetDataRow(rh);
            DataRow b = GridViewRules.GetDataRow(target);
            if (a == null || b == null) return;
            object[] tmp = a.ItemArray;
            a.ItemArray = b.ItemArray;
            b.ItemArray = tmp;
            RenumberRules();
            GridViewRules.FocusedRowHandle = target;
            PopulateRuleEditor();
            RaiseChanged();
        }

        private static string SV(DataRow r, string c) { return r[c] == DBNull.Value ? "" : r[c].ToString(); }
        private static decimal Dec(DataRow r, string c) { return r[c] == DBNull.Value ? 0m : Convert.ToDecimal(r[c]); }
        private static string D(DataRow r, string c, string fmt) { return r[c] == DBNull.Value ? 0m.ToString(fmt) : Convert.ToDecimal(r[c]).ToString(fmt); }
    }
}
