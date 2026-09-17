using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Everything a contract's machines are metered and charged for, in one screen: rent, black,
    /// colour, a committed minimum, a rental waive, and a tiered price scheme — set for one machine
    /// or for the whole fleet.
    ///
    /// <para>The machine grid can tick one machine at a time, and for thirty machines that is thirty
    /// clicks and one of them missed. Worse, clicking a tick collapses a multi-row selection to the
    /// row clicked, so "select the fleet, then tick" cannot work in the grid at all. Here the ticking
    /// and the doing are separate: tick the machines, then press what they need.</para>
    ///
    /// <para>Rates work the same way, and for a reason beyond convenience: a merged line is only
    /// possible when its machines agree on price, so "give these six the same rate" is not a
    /// shortcut, it is how you decide the invoice prints one black line instead of six.</para>
    ///
    /// <para>Removing takes the meter and, once saved, its whole reading history — so it says how
    /// many machines that is before it does it. A machine already the way it is asked to be is not
    /// touched.</para>
    ///
    /// <para>Changes are written back into the contract editor on OK only, and the contract still has
    /// to be saved.</para>
    /// </summary>
    public partial class MachineMeters_Form : XtraForm
    {
        private const string SCOPE_MACHINE = "This machine";
        private const string SCOPE_GROUP = "Its group";
        private const string SCOPE_CONTRACT = "Whole contract";

        private readonly List<ItemEditData> _items;
        private readonly AutoCount.Data.DBSetting _db;
        private DataTable _dt;
        private bool _changed;

        public MachineMeters_Form()
        {
            InitializeComponent();
        }

        public MachineMeters_Form(AutoCount.Data.DBSetting db, List<ItemEditData> items) : this()
        {
            _db = db;
            _items = items ?? new List<ItemEditData>();
        }

        /// <summary>The contract's agreed line terms, so this screen can say when a machine's copies
        /// are already priced by a LINE's tier ladder.
        ///
        /// <para>Without it the tier column reads blank on a machine that is being priced by bands
        /// agreed for its whole group — and giving that machine a ladder of its own pulls it out of
        /// the group, which changes what the machines LEFT BEHIND pay. Nobody should have to know
        /// that to use this screen; it says so instead.</para></summary>
        public System.Collections.Generic.Dictionary<string,
            ServiceContractPhotocopier.Classes.ScpLineTerms> LineTerms;

        /// <summary>The bands the machine's line has agreed for this colour, described, or "" when
        /// the line has none.</summary>
        private string LineLadderFor(ItemEditData d, string role)
        {
            if (LineTerms == null || d == null) return "";
            string grp = (d.MergeGroupCodeMeter ?? "").Trim();
            if (grp.Length == 0) return "";
            ServiceContractPhotocopier.Classes.ScpLineTerms t;
            if (!LineTerms.TryGetValue(ServiceContractPhotocopier.Classes.ScpLineTerms.Key(
                    ServiceContractPhotocopier.Classes.ScpLineTerms.SIDE_METER, grp), out t) || t == null)
                return "";
            string stored = ((role == "CL" ? t.LadderCl : t.LadderBk) ?? "").Trim();
            if (stored.Length == 0) return "";
            return stored.IndexOf('|') < 0
                ? stored
                : ServiceContractPhotocopier.Classes.ScpMultiPrice.Describe(
                      ServiceContractPhotocopier.Classes.ScpMultiPrice.ParseTiers(stored));
        }

        // ---- scope: stored as one letter, shown as words ----
        private static string ScopeWords(string code)
        {
            string c = (code ?? "S").Trim().ToUpperInvariant();
            return c == "G" ? SCOPE_GROUP : (c == "C" ? SCOPE_CONTRACT : SCOPE_MACHINE);
        }

        private static string ScopeCode(string words)
        {
            string w = (words ?? "").Trim();
            if (string.Equals(w, SCOPE_GROUP, StringComparison.OrdinalIgnoreCase)) return "G";
            if (string.Equals(w, SCOPE_CONTRACT, StringComparison.OrdinalIgnoreCase)) return "C";
            return "S";
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _dt = new DataTable();
            _dt.Columns.Add("Idx", typeof(int));
            _dt.Columns.Add("Sel", typeof(bool));
            _dt.Columns.Add("ServiceItemNo", typeof(string));
            _dt.Columns.Add("SerialNumber", typeof(string));
            _dt.Columns.Add("ItemCode", typeof(string));
            _dt.Columns.Add("HasRental", typeof(bool));
            _dt.Columns.Add("RentalRate", typeof(decimal));
            _dt.Columns.Add("HasBK", typeof(bool));
            _dt.Columns.Add("BKRate", typeof(decimal));
            _dt.Columns.Add("BKLadder", typeof(string));
            // The LINE's ladder for this colour, held apart from what the machine carries. The two
            // are different facts and the screen needs both: a machine can be priced by its own
            // bands INSIDE a group that has agreed its own, and that is exactly the case where
            // clearing the machine's bands changes what the other machines pay.
            _dt.Columns.Add("BKLine", typeof(string));
            _dt.Columns.Add("HasCL", typeof(bool));
            _dt.Columns.Add("CLRate", typeof(decimal));
            _dt.Columns.Add("CLLadder", typeof(string));
            _dt.Columns.Add("CLLine", typeof(string));
            _dt.Columns.Add("HasMin", typeof(bool));
            _dt.Columns.Add("MinAmount", typeof(decimal));
            _dt.Columns.Add("MinScope", typeof(string));
            _dt.Columns.Add("HasWaive", typeof(bool));
            _dt.Columns.Add("WaiveDeal", typeof(string));
            // The waive's own numbers ride along hidden — the deal column is only their summary.
            _dt.Columns.Add("WFirstN", typeof(int));
            _dt.Columns.Add("WTarget", typeof(decimal));
            _dt.Columns.Add("WPThreshold", typeof(decimal));
            _dt.Columns.Add("WPAmount", typeof(decimal));
            _dt.Columns.Add("WScope", typeof(string));
            _dt.Columns.Add("WaiveAmount", typeof(decimal));

            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d == null || d.IsGroupItem) continue;
                if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
                DataRow r = _dt.NewRow();
                r["Idx"] = i;
                r["Sel"] = true;                 // the usual case is "all of them"
                r["ServiceItemNo"] = string.IsNullOrWhiteSpace(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber ?? "";
                r["ItemCode"] = d.ItemCode ?? "";
                LoadRole(r, d, "RENTAL", "HasRental", "RentalRate", null, null);
                LoadRole(r, d, "BK", "HasBK", "BKRate", "BKLadder", "BKLine");
                LoadRole(r, d, "CL", "HasCL", "CLRate", "CLLadder", "CLLine");
                LoadMin(r, d);
                LoadWaive(r, d);
                _dt.Rows.Add(r);
            }
            GridMachines.DataSource = _dt;
            GridViewMachines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMachines_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);

            RepoScope.Items.Clear();
            RepoScope.Items.Add(SCOPE_MACHINE);
            RepoScope.Items.Add(SCOPE_GROUP);
            RepoScope.Items.Add(SCOPE_CONTRACT);
            CmbMinScope.Properties.Items.Clear();
            CmbMinScope.Properties.Items.Add(SCOPE_MACHINE);
            CmbMinScope.Properties.Items.Add(SCOPE_GROUP);
            CmbMinScope.Properties.Items.Add(SCOPE_CONTRACT);
            CmbMinScope.SelectedIndex = 0;
            LoadLadderSchemes();
            RefreshSummary();
        }

        /// <summary>The tier schemes this book has, for the "everyone on this ladder" button.</summary>
        private void LoadLadderSchemes()
        {
            CmbLadder.Properties.Items.Clear();
            try
            {
                DataTable t = _db.GetDataTable(
                    "SELECT MeterMultiPriceCode, ISNULL([Description],'') AS [Description] " +
                    "FROM dbo.zSCP_MeterMultiPrice ORDER BY MeterMultiPriceCode", false);
                foreach (DataRow r in t.Rows)
                    CmbLadder.Properties.Items.Add(Convert.ToString(r["MeterMultiPriceCode"]));
            }
            catch { }
            if (CmbLadder.Properties.Items.Count > 0) CmbLadder.SelectedIndex = 0;
        }

        private void LoadRole(DataRow r, ItemEditData d, string role, string hasField,
            string rateField, string ladderField, string lineField)
        {
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            r[hasField] = m != null;
            r[rateField] = m == null ? 0m : RateOf(m);
            if (ladderField == null) return;
            string own = m == null ? "" : LadderOf(m);
            string line = LineLadderFor(d, role);
            r[lineField] = line;
            // Three states, and the screen has to tell them apart because the buttons do different
            // damage in each:
            //   nothing of its own, line has bands   -> the line prices it
            //   its own bands, line has bands too    -> it is being HELD OUT of a live group deal,
            //                                           and putting it back changes the others
            //   its own bands, line has none         -> nobody else is involved at all
            if (own.Length == 0)
                own = line.Length > 0 ? "the whole line: " + line : "";
            else if (line.Length > 0)
                own = own + "  (priced apart from the line)";
            r[ladderField] = own;
        }

        /// <summary>What the tier column says: the scheme, "(Custom)" for a machine priced on its own
        /// ladder, or blank for a plain per-copy rate.</summary>
        private static string LadderOf(DataRow m)
        {
            string custom = m.Table.Columns.Contains("CustomTiers") && m["CustomTiers"] != DBNull.Value
                ? Convert.ToString(m["CustomTiers"]).Trim() : "";
            string code = m.Table.Columns.Contains("MeterMultiPriceCode") && m["MeterMultiPriceCode"] != DBNull.Value
                ? Convert.ToString(m["MeterMultiPriceCode"]).Trim() : "";
            if (custom.Length > 0) return code.Length > 0 ? code + " (Custom)" : "(Custom)";
            return code;
        }

        private static void LoadMin(DataRow r, ItemEditData d)
        {
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, "COMMIT");
            r["HasMin"] = m != null;
            r["MinAmount"] = m == null ? 0m : Math.Abs(RateOf(m));
            string sc = m != null && m.Table.Columns.Contains("CommitScope") && m["CommitScope"] != DBNull.Value
                ? Convert.ToString(m["CommitScope"]) : "S";
            r["MinScope"] = ScopeWords(sc);
        }

        private static void LoadWaive(DataRow r, ItemEditData d)
        {
            DataRow m = zSCP2_Item_Form.FindWaiveMeter(d.Meters);
            r["HasWaive"] = m != null;
            int firstN = 0;
            decimal target = 0m, pt = 0m, pa = 0m, amt = 0m;
            string scope = "BKCL";
            if (m != null)
            {
                firstN = Col(m, "WaiveFirstNMonths") ? Convert.ToInt32(m["WaiveFirstNMonths"]) : 0;
                target = Col(m, "WaiveTargetAmount") ? Convert.ToDecimal(m["WaiveTargetAmount"]) : 0m;
                pt = Col(m, "WaivePartialThreshold") ? Convert.ToDecimal(m["WaivePartialThreshold"]) : 0m;
                pa = Col(m, "WaivePartialAmount") ? Convert.ToDecimal(m["WaivePartialAmount"]) : 0m;
                scope = Col(m, "WaiveScope") ? Convert.ToString(m["WaiveScope"]) : "BKCL";
                amt = Math.Abs(RateOf(m));
            }
            r["WFirstN"] = firstN; r["WTarget"] = target; r["WPThreshold"] = pt; r["WPAmount"] = pa;
            r["WScope"] = string.IsNullOrEmpty(scope) ? "BKCL" : scope;
            r["WaiveAmount"] = amt;
            r["WaiveDeal"] = m == null ? "" : Classes.CommonForms.WaiveConfig_Form.Summary(firstN, target, pt, pa);
        }

        private static bool Col(DataRow m, string name)
        {
            return m.Table.Columns.Contains(name) && m[name] != DBNull.Value;
        }

        /// <summary>A meter's price. A flat meter carries it in whichever of the two columns was
        /// filled in, which is why the old book has rentals in both.</summary>
        private static decimal RateOf(DataRow m)
        {
            decimal rate = Col(m, "ChargesRate") ? Convert.ToDecimal(m["ChargesRate"]) : 0m;
            decimal min = Col(m, "MinimumCharges") ? Convert.ToDecimal(m["MinimumCharges"]) : 0m;
            return rate != 0m ? rate : min;
        }

        private void GridViewMachines_CellValueChanged(object sender,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "Sel") { RefreshSummary(); return; }
            _changed = true;
            RefreshSummary();
        }

        private List<DataRow> Ticked()
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            List<DataRow> list = new List<DataRow>();
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) list.Add(r);
            }
            return list;
        }

        private void NeedTicks()
        {
            XtraMessageBox.Show("Tick the machines first.", "Meters",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- counters on and off ----
        private void Apply(string field, bool want)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }
            int changed = 0;
            foreach (DataRow r in ticked)
            {
                bool now = r[field] != DBNull.Value && Convert.ToBoolean(r[field]);
                if (now == want) continue;
                r[field] = want;
                changed++;
            }
            if (changed > 0) _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            if (changed == 0) LblSummary.Text = "Nothing to change — those machines are already like that.";
        }

        private void BtnAddRental_Click(object sender, EventArgs e) { Apply("HasRental", true); }
        private void BtnDelRental_Click(object sender, EventArgs e) { Apply("HasRental", false); }
        private void BtnAddBK_Click(object sender, EventArgs e) { Apply("HasBK", true); }
        private void BtnDelBK_Click(object sender, EventArgs e) { Apply("HasBK", false); }
        private void BtnAddCL_Click(object sender, EventArgs e) { Apply("HasCL", true); }
        private void BtnDelCL_Click(object sender, EventArgs e) { Apply("HasCL", false); }

        // ---- one rate, many machines ----
        // Same model, same deal: type the figure once. This is also what makes a merged line
        // possible -- machines that disagree on price cannot share a printed line.
        private void SetRate(string hasField, string rateField, string word)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }
            decimal rate = TxtRate.Value;
            if (rate < 0m) return;

            int done = 0, skipped = 0;
            foreach (DataRow r in ticked)
            {
                bool has = r[hasField] != DBNull.Value && Convert.ToBoolean(r[hasField]);
                if (!has) { skipped++; continue; }      // no such counter on this machine
                r[rateField] = rate;
                done++;
            }
            if (done > 0) _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = done + " machine" + (done == 1 ? "" : "s") + " now charge " +
                Trim(rate) + " for " + word +
                (skipped > 0 ? "   ·   " + skipped + " skipped (no " + word + " meter)" : "");
        }

        private static string Trim(decimal v)
        {
            string s = v.ToString("n4");
            if (s.IndexOf('.') < 0) return s;
            s = s.TrimEnd('0').TrimEnd('.');
            return s.Length == 0 ? "0" : s;
        }

        private void BtnRateRental_Click(object sender, EventArgs e) { SetRate("HasRental", "RentalRate", "rental"); }
        private void BtnRateBK_Click(object sender, EventArgs e) { SetRate("HasBK", "BKRate", "black"); }
        private void BtnRateCL_Click(object sender, EventArgs e) { SetRate("HasCL", "CLRate", "colour"); }

        // ---- committed minimum ----
        // A minimum is a floor under the print charges, not a charge. Scope says whose charges it is
        // measured against: this machine, its merge group, or the whole contract. A group of six
        // sharing RM 3,000 therefore takes ONE minimum on ONE machine, scoped to the group -- six
        // meters at 3,000 would top the shortfall up six times.
        private void BtnAddMin_Click(object sender, EventArgs e)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }
            decimal amount = TxtMinAmount.Value;
            if (amount <= 0m)
            {
                XtraMessageBox.Show("Type the committed amount first.", "Committed minimum",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string scope = Convert.ToString(CmbMinScope.EditValue);
            if (ScopeCode(scope) != "S" && ticked.Count > 1)
            {
                if (XtraMessageBox.Show(
                        "You are giving " + ticked.Count + " machines a minimum measured against " +
                        scope.ToLowerInvariant() + "." + Environment.NewLine + Environment.NewLine +
                        "Each of them would top the SAME shortfall up, so it would be billed " +
                        ticked.Count + " times. A shared minimum belongs on ONE machine." +
                        Environment.NewLine + Environment.NewLine + "Give it to all " + ticked.Count +
                        " anyway?",
                        "Committed minimum", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            foreach (DataRow r in ticked)
            {
                r["HasMin"] = true;
                r["MinAmount"] = amount;
                r["MinScope"] = scope;
            }
            _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = ticked.Count + " machine" + (ticked.Count == 1 ? "" : "s") +
                " committed to " + amount.ToString("n2") + " a month, measured against " +
                scope.ToLowerInvariant() + ".";
        }

        private void BtnDelMin_Click(object sender, EventArgs e) { Apply("HasMin", false); }

        // ---- rental waive ----
        // The contra line that gives the rent back. It needs a rental to give back, so machines
        // without one are refused rather than left carrying a waive that can never fire.
        private void BtnSetWaive_Click(object sender, EventArgs e)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }

            DataRow seed = ticked[0];
            using (Classes.CommonForms.WaiveConfig_Form f = new Classes.CommonForms.WaiveConfig_Form(
                ServiceContractPhotocopier.Classes.ScpStrategy.METER_TYPE_WAIVE,
                seed["WFirstN"] == DBNull.Value ? 0 : Convert.ToInt32(seed["WFirstN"]),
                seed["WTarget"] == DBNull.Value ? 0m : Convert.ToDecimal(seed["WTarget"]),
                seed["WPThreshold"] == DBNull.Value ? 0m : Convert.ToDecimal(seed["WPThreshold"]),
                seed["WPAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(seed["WPAmount"]),
                Convert.ToString(seed["WScope"])))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                int done = 0, noRent = 0;
                foreach (DataRow r in ticked)
                {
                    bool hasRent = r["HasRental"] != DBNull.Value && Convert.ToBoolean(r["HasRental"]);
                    if (!hasRent) { noRent++; continue; }
                    r["HasWaive"] = true;
                    r["WFirstN"] = f.FirstNMonths;
                    r["WTarget"] = f.TargetAmount;
                    r["WPThreshold"] = f.PartialThreshold;
                    r["WPAmount"] = f.PartialAmount;
                    r["WScope"] = f.Scope;
                    // The contra is worth the rent it gives back, unless it already has its own figure.
                    decimal amt = r["WaiveAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["WaiveAmount"]);
                    if (amt <= 0m) r["WaiveAmount"] = r["RentalRate"] == DBNull.Value ? 0m : Convert.ToDecimal(r["RentalRate"]);
                    r["WaiveDeal"] = Classes.CommonForms.WaiveConfig_Form.Summary(
                        f.FirstNMonths, f.TargetAmount, f.PartialThreshold, f.PartialAmount);
                    done++;
                }
                _changed = true;
                GridViewMachines.RefreshData();
                RefreshSummary();
                LblSummary.Text = done + " machine" + (done == 1 ? "" : "s") + " on this waive deal" +
                    (noRent > 0 ? "   ·   " + noRent + " skipped (no rental to waive)" : "");
            }
        }

        private void BtnDelWaive_Click(object sender, EventArgs e) { Apply("HasWaive", false); }

        // ---- tier pricing ----
        // Setting the scheme must also clear the machine's own tier override, because an override
        // BEATS the scheme at billing time -- leaving it would make this button look like it did
        // nothing at all.
        private void SetLadder(string hasField, string ladderField, string lineField,
            string word, string code)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }

            // Leaving a line's ladder, or rejoining it, changes the volume the WHOLE line is priced
            // on -- so it changes what the other machines pay, without anyone touching them. Say it
            // before it happens; the arithmetic is invisible afterwards.
            List<string> onLine = new List<string>();
            List<string> ownBands = new List<string>();
            foreach (DataRow tr in ticked)
            {
                bool hasMeter = tr[hasField] != DBNull.Value && Convert.ToBoolean(tr[hasField]);
                if (!hasMeter) continue;
                string now = Convert.ToString(tr[ladderField]);
                // Whether a group ladder is in play AT ALL. Asked of the line, not of the machine --
                // the machine's own ladder says nothing about whether a group deal exists, and the
                // first version of this guard confused the two. It warned that "the machines that
                // stay may move to a different band" on a contract whose line had no ladder and
                // where nothing could move, which is a warning that teaches people to click Yes.
                bool lineHasLadder = Convert.ToString(tr[lineField]).Length > 0;
                bool machineApart = now.IndexOf("(line:", StringComparison.OrdinalIgnoreCase) != 0
                                 && now.Length > 0;
                // Bands typed for THIS machine. A scheme set over them replaces them outright, and
                // they cannot be got back -- so this asks before it happens, the same as any other
                // irreversible thing on this screen.
                if (now.IndexOf("(Custom)", StringComparison.OrdinalIgnoreCase) >= 0)
                    ownBands.Add("   " + Convert.ToString(tr["ServiceItemNo"]));
                // Leaving the group's total, or rejoining it. Either way the line is priced on a
                // different number of copies afterwards.
                bool moves = lineHasLadder && (code.Length > 0 ? !machineApart : machineApart);
                if (moves) onLine.Add("   " + Convert.ToString(tr["ServiceItemNo"]));
            }

            if (ownBands.Count > 0)
            {
                if (XtraMessageBox.Show(
                        "These machines are priced on bands typed for them, not on a scheme:" +
                        Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, ownBands.ToArray()) + Environment.NewLine +
                        Environment.NewLine +
                        (code.Length > 0
                            ? "Setting scheme " + code + " REPLACES those bands. They are not kept " +
                              "anywhere and cannot be brought back."
                            : "Going back to a flat rate THROWS those bands away. They cannot be " +
                              "brought back.") + Environment.NewLine + Environment.NewLine +
                        "Continue?",
                        "Tier pricing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            if (onLine.Count > 0)
            {
                string what = code.Length > 0
                    ? "These machines are priced by their LINE's tier ladder. Giving them one of their " +
                      "own takes them out of that line's total:"
                    : "Taking these machines off their own tiers puts them back into their LINE's tier " +
                      "ladder:";
                if (XtraMessageBox.Show(
                        what + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, onLine.ToArray()) + Environment.NewLine +
                        Environment.NewLine +
                        "The line is priced on its machines' copies added up, so the machines that STAY " +
                        "may move to a different band." + Environment.NewLine + Environment.NewLine +
                        "Continue?",
                        "Tier pricing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            int done = 0, skipped = 0, cleared = 0;
            foreach (DataRow r in ticked)
            {
                bool has = r[hasField] != DBNull.Value && Convert.ToBoolean(r[hasField]);
                if (!has) { skipped++; continue; }
                string was = Convert.ToString(r[ladderField]);
                if (was.IndexOf("(Custom)", StringComparison.OrdinalIgnoreCase) >= 0) cleared++;
                r[ladderField] = code;
                done++;
            }
            if (done > 0) _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = (code.Length == 0
                    ? done + " machine" + (done == 1 ? "" : "s") + " back to a plain " + word + " rate"
                    : done + " machine" + (done == 1 ? "" : "s") + " on scheme " + code + " for " + word) +
                (cleared > 0 ? "   ·   " + cleared + " had their own tiers, now replaced" : "") +
                (skipped > 0 ? "   ·   " + skipped + " skipped (no " + word + " meter)" : "");
        }

        private string LadderCode()
        {
            return CmbLadder.EditValue == null ? "" : Convert.ToString(CmbLadder.EditValue).Trim();
        }

        private void BtnLadderBK_Click(object sender, EventArgs e) { SetLadder("HasBK", "BKLadder", "BKLine", "black", LadderCode()); }
        private void BtnLadderCL_Click(object sender, EventArgs e) { SetLadder("HasCL", "CLLadder", "CLLine", "colour", LadderCode()); }
        private void BtnLadderClear_Click(object sender, EventArgs e)
        {
            SetLadder("HasBK", "BKLadder", "BKLine", "black", "");
            SetLadder("HasCL", "CLLadder", "CLLine", "colour", "");
        }

        // ---- selecting ----
        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            bool anyOff = false;
            foreach (DataRow r in _dt.Rows)
                if (r.RowState != DataRowState.Deleted && (r["Sel"] == DBNull.Value || !Convert.ToBoolean(r["Sel"])))
                    anyOff = true;
            foreach (DataRow r in _dt.Rows)
                if (r.RowState != DataRowState.Deleted) r["Sel"] = anyOff;
            GridViewMachines.RefreshData();
            RefreshSummary();
        }

        /// <summary>Tick every machine of the same model as the focused row — the usual way a rate is
        /// decided, since one model is one deal.</summary>
        private void BtnSameModel_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            DataRowView drv = GridViewMachines.GetRow(GridViewMachines.FocusedRowHandle) as DataRowView;
            if (drv == null)
            {
                XtraMessageBox.Show("Click a machine of the model you want first.", "Meters",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string model = Convert.ToString(drv.Row["ItemCode"]).Trim();
            int n = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                bool same = string.Equals(Convert.ToString(r["ItemCode"]).Trim(), model,
                    StringComparison.OrdinalIgnoreCase);
                r["Sel"] = same;
                if (same) n++;
            }
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = n + " machine" + (n == 1 ? "" : "s") + " of " +
                (model.Length == 0 ? "(no model)" : model) + " ticked.";
        }

        private void RefreshSummary()
        {
            int machines = 0, sel = 0, rent = 0, bk = 0, cl = 0, min = 0, waive = 0, none = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                machines++;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) sel++;
                bool hr = Flag(r, "HasRental"), hb = Flag(r, "HasBK"), hc = Flag(r, "HasCL");
                if (hr) rent++;
                if (hb) bk++;
                if (hc) cl++;
                if (Flag(r, "HasMin")) min++;
                if (Flag(r, "HasWaive")) waive++;
                if (!hr && !hb && !hc) none++;
            }
            LblSummary.Text = machines + " machine" + (machines == 1 ? "" : "s") + ", " + sel + " ticked" +
                "   ·   " + rent + " rental, " + bk + " black, " + cl + " colour" +
                (min > 0 ? ", " + min + " committed" : "") +
                (waive > 0 ? ", " + waive + " waived" : "") +
                (none > 0 ? "   ·   " + none + " with no meter at all" : "");
        }

        private static bool Flag(DataRow r, string field)
        {
            return r[field] != DBNull.Value && Convert.ToBoolean(r[field]);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            if (!Harvest()) return;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Applies the grid to the contract's machines. False = the user backed out, so
        /// nothing at all is applied.</summary>
        private bool Harvest()
        {
            // Two minimums measured against the same set would each top it up, and the shortfall
            // would be billed twice. Say so before writing, not at Generate.
            List<string> clashes = MinimumClashes();
            if (clashes.Count > 0)
            {
                XtraMessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, clashes.ToArray()) +
                    Environment.NewLine + Environment.NewLine +
                    "A shared minimum belongs on ONE machine of the set.",
                    "Committed minimum", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int losing = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                ItemEditData d = _items[Convert.ToInt32(r["Idx"])];
                if (d.ItemKey <= 0) continue;
                losing += LosesHistory(d, "RENTAL", Flag(r, "HasRental"));
                losing += LosesHistory(d, "BK", Flag(r, "HasBK"));
                losing += LosesHistory(d, "CL", Flag(r, "HasCL"));
                DataRow w = zSCP2_Item_Form.FindWaiveMeter(d.Meters);
                if (!Flag(r, "HasWaive") && w != null && w.RowState != DataRowState.Added) losing++;
            }
            if (losing > 0 &&
                XtraMessageBox.Show(
                    "This removes " + losing + " meter" + (losing == 1 ? "" : "s") + "." +
                    Environment.NewLine + Environment.NewLine +
                    "When you save the contract, each one AND its reading history (all readings + " +
                    "billing log for that counter) are permanently deleted." + Environment.NewLine +
                    Environment.NewLine + "Continue?",
                    "Meters", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return false;

            bool typeMissing = false;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                ItemEditData d = _items[Convert.ToInt32(r["Idx"])];
                if (!Set(d, "RENTAL", Flag(r, "HasRental"), Dec(r["RentalRate"]), null)) typeMissing = true;
                if (!Set(d, "BK", Flag(r, "HasBK"), Dec(r["BKRate"]), Convert.ToString(r["BKLadder"]))) typeMissing = true;
                if (!Set(d, "CL", Flag(r, "HasCL"), Dec(r["CLRate"]), Convert.ToString(r["CLLadder"]))) typeMissing = true;
                // The committed minimum is NOT written from here any more -- see the note on
                // SetMin. The columns went off this grid when the minimum became a term agreed
                // for a LINE, but the write stayed behind, so every OK still pushed a value
                // read out of a column nobody can see back onto the meter.
                // The waive is NOT written from here any more -- see the note on SetWaive. Calling
                // it would delete the contract's waive the moment a tick nobody can see reads false.
            }
            if (typeMissing)
                XtraMessageBox.Show(
                    "This book is missing one of the standard RENTAL / BK / CL / COMMIT / WAIVE meter " +
                    "types, so those meters are waiting for one." + Environment.NewLine + Environment.NewLine +
                    "Open a machine and pick a meter type on its new meter row.",
                    "Meters", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        /// <summary>Sets of machines that more than one minimum is measured against.</summary>
        private List<string> MinimumClashes()
        {
            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted || !Flag(r, "HasMin")) continue;
                string code = ScopeCode(Convert.ToString(r["MinScope"]));
                if (code == "S") continue;                 // a per-machine minimum cannot collide
                ItemEditData d = _items[Convert.ToInt32(r["Idx"])];
                string grp = (d.MergeGroupCode ?? "").Trim().ToUpperInvariant();
                string key = code == "C" ? "C" : "G|" + (grp.Length > 0 ? grp : "@" + d.ItemKey);
                int n;
                seen.TryGetValue(key, out n);
                seen[key] = n + 1;
                words[key] = code == "C" ? "the whole contract"
                           : (grp.Length > 0 ? "group " + grp : "an ungrouped machine");
            }
            List<string> outp = new List<string>();
            foreach (KeyValuePair<string, int> kv in seen)
                if (kv.Value > 1)
                    outp.Add(kv.Value + " committed minimums are measured against " + words[kv.Key] +
                             " — each would top it up, so the shortfall would be billed " + kv.Value + " times.");
            return outp;
        }

        private static decimal Dec(object v)
        {
            return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
        }

        private static int LosesHistory(ItemEditData d, string role, bool want)
        {
            if (want) return 0;
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            return (m != null && m.RowState != DataRowState.Added) ? 1 : 0;
        }

        /// <summary>Puts one counter on or off a machine, prices it, and points it at a tier scheme.
        /// False = the standard type is missing.</summary>
        private bool Set(ItemEditData d, string role, bool want, decimal rate, string ladder)
        {
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            bool ok = true;
            if (!want)
            {
                if (m != null) m.Delete();
                return true;
            }
            if (m == null)
            {
                ok = zSCP2_Item_Form.AddStandardMeter(_db, d.Meters, role);
                m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
                if (m == null) return ok;
            }
            // Only write a price the user actually changed. A rental that carries its amount in
            // MinimumCharges keeps it there untouched unless a new figure is given, so an old
            // contract is not quietly re-shaped by opening this screen.
            if (rate != RateOf(m))
            {
                m["ChargesRate"] = rate;
                if (m.Table.Columns.Contains("MinimumCharges")) m["MinimumCharges"] = 0m;
            }
            if (ladder == null) return ok;
            // "(line: ...)" is this screen reporting the LINE's ladder, not something this machine
            // carries. Writing it back would invent a scheme code out of a sentence.
            if (ladder.StartsWith("(line:", StringComparison.OrdinalIgnoreCase)) return ok;
            string wasCode = m.Table.Columns.Contains("MeterMultiPriceCode") && m["MeterMultiPriceCode"] != DBNull.Value
                ? Convert.ToString(m["MeterMultiPriceCode"]).Trim() : "";
            string wantCode = (ladder ?? "").Replace("(Custom)", "").Trim();
            bool wasCustom = LadderOf(m).IndexOf("(Custom)", StringComparison.OrdinalIgnoreCase) >= 0;
            // "(Custom)" left alone means the user did not touch this row -- keep their own tiers.
            if (wasCustom && ladder.IndexOf("(Custom)", StringComparison.OrdinalIgnoreCase) >= 0) return ok;
            if (wantCode != wasCode || wasCustom)
            {
                m["MeterMultiPriceCode"] = wantCode;
                // The machine's own tiers BEAT the scheme, so a scheme set over them would do nothing
                // at all. Clearing them is what makes the button mean what it says.
                if (m.Table.Columns.Contains("CustomTiers")) m["CustomTiers"] = "";
            }
            return ok;
        }

        /// <summary>
        /// UNUSED, deliberately — the twin of <see cref="SetWaive"/>. A committed minimum is agreed
        /// for a LINE and set in Billing Setup, which is the only screen that can express the three
        /// scopes it has (this machine / this line's machines added up / the whole contract).
        ///
        /// <para>It kept running here long after its columns were taken off the grid: it rewrote
        /// MinimumCharges from whichever of two columns happened to hold a number, and stamped a
        /// CommitScope read out of a hidden cell. A screen must not write what it does not show.</para>
        /// </summary>
        private bool SetMin(ItemEditData d, DataRow r)
        {
            bool want = Flag(r, "HasMin");
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, "COMMIT");
            if (!want)
            {
                if (m != null) m.Delete();
                return true;
            }
            bool ok = true;
            if (m == null)
            {
                ok = zSCP2_Item_Form.AddStandardMeter(_db, d.Meters, "COMMIT");
                m = zSCP2_Item_Form.FindMeterByRole(d.Meters, "COMMIT");
                if (m == null) return ok;
            }
            // The committed amount is the meter's minimum, which is what the engine reads.
            m["MinimumCharges"] = Dec(r["MinAmount"]);
            m["ChargesRate"] = 0m;
            if (m.Table.Columns.Contains("CommitScope"))
                m["CommitScope"] = ScopeCode(Convert.ToString(r["MinScope"]));
            return ok;
        }

        /// <summary>
        /// UNUSED, deliberately. A rental waive is agreed for a LINE now, and Billing Setup is where
        /// it is set — the whole deal in one place: the target, the free window, the consolation
        /// band, which copies count, and whether it is one waive for the line or one per machine.
        ///
        /// <para>This screen could only ever see a meter on a machine, which is half the deal. A
        /// waive scoped to the group lives on ONE machine and speaks for all of them, so the grid
        /// showed one row ticked and the rest blank — and ticking the others to "fix" it credited the
        /// same rental three times. Unticking the one deleted the contract's waive outright.</para>
        ///
        /// <para>Kept as code rather than deleted so the shape of what it used to write is still
        /// readable next to what replaced it.</para>
        /// </summary>
        private bool SetWaive(ItemEditData d, DataRow r)
        {
            bool want = Flag(r, "HasWaive");
            DataRow m = zSCP2_Item_Form.FindWaiveMeter(d.Meters);
            if (!want)
            {
                if (m != null) m.Delete();
                return true;
            }
            bool ok = true;
            if (m == null)
            {
                ok = zSCP2_Item_Form.AddStandardMeter(_db, d.Meters, "WAIVE");
                m = zSCP2_Item_Form.FindWaiveMeter(d.Meters);
                if (m == null) return ok;
            }
            // The contra is stored NEGATIVE — it takes the rent away.
            decimal amt = Math.Abs(Dec(r["WaiveAmount"]));
            m["MinimumCharges"] = -amt;
            m["ChargesRate"] = 0m;
            m["WaiveFirstNMonths"] = r["WFirstN"] == DBNull.Value ? 0 : Convert.ToInt32(r["WFirstN"]);
            m["WaiveTargetAmount"] = Dec(r["WTarget"]);
            m["WaivePartialThreshold"] = Dec(r["WPThreshold"]);
            m["WaivePartialAmount"] = Dec(r["WPAmount"]);
            string ws = Convert.ToString(r["WScope"]).Trim().ToUpperInvariant();
            m["WaiveScope"] = ws == "BK" || ws == "CL" ? ws : "BKCL";
            return ok;
        }

        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            DialogResult r = XtraMessageBox.Show(
                "You changed the meters but did not press OK." + Environment.NewLine + Environment.NewLine +
                "Apply them to the contract?" + Environment.NewLine +
                "(Remember to SAVE the contract afterwards.)",
                "Meters", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)
            {
                GridViewMachines.CloseEditor();
                GridViewMachines.UpdateCurrentRow();
                if (!Harvest()) { e.Cancel = true; return; }
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
