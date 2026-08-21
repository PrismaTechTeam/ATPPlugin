using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Every line this contract will print, and what each one charges.
    ///
    /// <para>A machine on its own line is a line too — merging is only one way to make one. Tick
    /// machines and put them on one line: for the rental, for the copies, or for both. That is the
    /// whole vocabulary, and it reaches every arrangement — "all on one line" is grouping them all,
    /// "one line per model" is grouping by model, "one line per machine" is grouping none of them,
    /// and everything in between (a C5335 and a C5665 together while four C1234 stay apart) is
    /// simply which machines were ticked.</para>
    ///
    /// <para><b>Rental and copies group separately.</b> The commonest deal here is one agreed rental
    /// across the fleet while every machine still bills its own copies, and a single grouping column
    /// could not say it. So a machine carries two: the line its rental prints on, and the line its
    /// black and colour print on.</para>
    ///
    /// <para><b>A printed line carries ONE unit price.</b> That is why a line can be given an agreed
    /// figure at all: machines whose own rates differ cannot share a row — the row has one price
    /// cell and there is no honest way to put two numbers in it — so until a figure is agreed here,
    /// such a group still comes out as one line per rate. The grid says so where it happens.</para>
    ///
    /// <para>Everything is written back into the contract editor on OK only, and the contract still
    /// has to be saved.</para>
    /// </summary>
    public partial class RentalGroupPrice_Form : XtraForm
    {
        private const string SIDE_R = "R";
        private const string SIDE_M = "M";

        private readonly List<ItemEditData> _items;
        private readonly Dictionary<string, ScpLineTerms> _terms;   // "SIDE|GROUP" -> agreed figures
        private DataTable _dtMachines;
        private DataTable _dtLines;
        private bool _changed;
        private int _seq;

        /// <summary>The agreed figures as edited, keyed "SIDE|GROUPCODE". A line left at 0 is absent.
        /// </summary>
        public Dictionary<string, ScpLineTerms> Result =
            new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);

        public RentalGroupPrice_Form()
        {
            InitializeComponent();
        }

        public RentalGroupPrice_Form(List<ItemEditData> items, Dictionary<string, ScpLineTerms> current) : this()
        {
            _items = items ?? new List<ItemEditData>();
            _terms = current ?? new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);
        }

        // ---------- load ----------

        private void OnFormLoad(object sender, EventArgs e)
        {
            LblHint.Text =
                "Tick the machines that belong on ONE line and say which line: the rental, the copies, " +
                "or both. A machine in no group prints on its own." + Environment.NewLine +
                "Then type what that line charges — a rental a month for ONE machine, a rate per copy " +
                "for black and colour. Left at 0, every machine keeps its own rate; where those rates " +
                "differ the line still comes out one per rate, because a printed line has one price cell.";

            BuildMachines();
            RebuildLines();

            BtnMerge.Click += new EventHandler(BtnMerge_Click);
            BtnMergeMeter.Click += new EventHandler(BtnMergeMeter_Click);
            BtnMergeBoth.Click += new EventHandler(BtnMergeBoth_Click);
            BtnUngroup.Click += new EventHandler(BtnUngroup_Click);
            BtnByPrice.Click += new EventHandler(BtnByPrice_Click);
            BtnByModel.Click += new EventHandler(BtnByModel_Click);
            BtnFromMachines.Click += new EventHandler(BtnFromMachines_Click);
            BtnClear.Click += new EventHandler(BtnClear_Click);
            BtnOK.Click += new EventHandler(BtnOK_Click);
            GridViewLines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewLines_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);
        }

        private void BuildMachines()
        {
            _dtMachines = new DataTable();
            _dtMachines.Columns.Add("Sel", typeof(bool));
            _dtMachines.Columns.Add("ItemNo", typeof(string));
            _dtMachines.Columns.Add("Serial", typeof(string));
            _dtMachines.Columns.Add("Model", typeof(string));
            _dtMachines.Columns.Add("MergeGroup", typeof(string));
            _dtMachines.Columns.Add("MeterGroup", typeof(string));
            _dtMachines.Columns.Add("PrintsOn", typeof(string));
            _dtMachines.Columns.Add("OwnRate", typeof(decimal));
            _dtMachines.Columns.Add("OwnBk", typeof(decimal));
            _dtMachines.Columns.Add("OwnCl", typeof(decimal));
            _dtMachines.Columns.Add("Idx", typeof(int));

            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d == null || d.IsGroupItem || d.Inactive) continue;
                DataRow r = _dtMachines.NewRow();
                r["Sel"] = false;
                r["ItemNo"] = d.ServiceItemNo ?? "";
                r["Serial"] = d.SerialNumber ?? "";
                r["Model"] = d.ItemCode ?? "";
                r["MergeGroup"] = d.MergeGroupCode ?? "";
                r["MeterGroup"] = d.MergeGroupCodeMeter ?? "";
                r["PrintsOn"] = LineNameOf(d.MergeGroupCode, d.ServiceItemNo);
                decimal rate;
                r["OwnRate"] = RateOf(d, "RENTAL", out rate) ? rate : 0m;
                r["OwnBk"] = RateOf(d, "BK", out rate) ? rate : 0m;
                r["OwnCl"] = RateOf(d, "CL", out rate) ? rate : 0m;
                r["Idx"] = i;
                _dtMachines.Rows.Add(r);
            }
            GridMachines.DataSource = _dtMachines;
            ColPrintsOn.Caption = "Rental line";
            ColMergeGroup.Caption = "Rental group";
            ColMergeGroup.Visible = false;      // the "prints on" column already says it in words
        }

        /// <summary>A machine's own rate for one role, from its meter rows. Rentals are the awkward
        /// one: the old book puts a rental's money in MinimumCharges as often as in ChargesRate, so
        /// both are read and the larger stands.</summary>
        private static bool RateOf(ItemEditData d, string role, out decimal rate)
        {
            rate = 0m;
            if (d == null || d.Meters == null) return false;
            bool found = false;
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted) continue;
                string r = Str(mr, "MeterRole").Trim().ToUpperInvariant();
                string t = Str(mr, "MeterTypeCode").Trim().ToUpperInvariant();
                bool isRole = role == "RENTAL"
                    ? ScpStrategy.IsRentalRole(r, t)
                    : (r == role || (r.Length == 0 && t == role));
                if (!isRole) continue;
                decimal v = Dec(mr, "ChargesRate");
                if (role == "RENTAL")
                {
                    decimal m = Dec(mr, "MinimumCharges");
                    if (m > v) v = m;
                }
                if (v > rate) rate = v;
                found = true;
            }
            return found;
        }

        private static string Str(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToString(r[col]) : "";
        }
        private static decimal Dec(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDecimal(r[col]) : 0m;
        }

        private static string LineNameOf(string group, string itemNo)
        {
            string g = (group ?? "").Trim();
            return g.Length > 0 ? g : (itemNo ?? "");
        }

        // ---------- the lines ----------

        private class Line
        {
            public string Side;
            public string GroupCode;      // "" = the machine's own line
            public string Name;
            public List<int> Rows = new List<int>();
        }

        private List<Line> Collect(string side)
        {
            List<Line> outp = new List<Line>();
            Dictionary<string, Line> by = new Dictionary<string, Line>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _dtMachines.Rows.Count; i++)
            {
                DataRow r = _dtMachines.Rows[i];
                string g = Convert.ToString(r[side == SIDE_R ? "MergeGroup" : "MeterGroup"]).Trim();
                string key = g.Length > 0 ? "#" + g.ToUpperInvariant() : "=" + Convert.ToString(r["ItemNo"]);
                Line L;
                if (!by.TryGetValue(key, out L))
                {
                    L = new Line();
                    L.Side = side;
                    L.GroupCode = g;
                    L.Name = g.Length > 0 ? g : Convert.ToString(r["ItemNo"]);
                    by[key] = L;
                    outp.Add(L);
                }
                L.Rows.Add(i);
            }
            return outp;
        }

        private void RebuildLines()
        {
            _dtLines = new DataTable();
            _dtLines.Columns.Add("LineName", typeof(string));
            _dtLines.Columns.Add("Charge", typeof(string));
            _dtLines.Columns.Add("Units", typeof(int));
            _dtLines.Columns.Add("OnMachines", typeof(string));
            _dtLines.Columns.Add("UnitPrice", typeof(decimal));
            _dtLines.Columns.Add("Monthly", typeof(string));
            _dtLines.Columns.Add("Side", typeof(string));
            _dtLines.Columns.Add("GroupCode", typeof(string));
            _dtLines.Columns.Add("Field", typeof(string));

            foreach (Line L in Collect(SIDE_R))
            {
                AddLineRow(L, "Monthly rental", "OwnRate", 2);
                AddTermRow(L, "Rental waive", "WAIVE");
            }
            foreach (Line L in Collect(SIDE_M))
            {
                AddLineRow(L, "Black copies", "OwnBk", 4);
                AddLineRow(L, "Colour copies", "OwnCl", 4);
                AddTermRow(L, "Minimum charge", "COMMIT");
            }

            GridLines.DataSource = _dtLines;
            ColUnitPrice.Caption = "Agreed for this line";
            ColOnMachines.Caption = "Unit price now";
            ColMonthly.Caption = "A month";
            RefreshSummary();
        }

        private void AddLineRow(Line L, string charge, string field, int dp)
        {
            List<decimal> rates = new List<decimal>();
            foreach (int i in L.Rows)
            {
                decimal v = Convert.ToDecimal(_dtMachines.Rows[i][field]);
                if (!rates.Contains(v)) rates.Add(v);
            }

            ScpLineTerms t;
            _terms.TryGetValue(ScpLineTerms.Key(L.Side, L.GroupCode), out t);
            decimal agreed = t == null ? 0m
                : (field == "OwnRate" ? t.UnitPrice : (field == "OwnBk" ? t.BkPrice : t.ClPrice));

            DataRow r = _dtLines.NewRow();
            r["LineName"] = L.Name;
            r["Charge"] = charge;
            r["Units"] = L.Rows.Count;
            // One printed line is one Qty x Unit Price. Where the machines disagree and nothing has
            // been agreed, this line does not exist -- it comes out as one line per price, and saying
            // so here is the only way the screen matches the paper.
            r["OnMachines"] = rates.Count == 1
                ? rates[0].ToString(dp == 4 ? "n4" : "n2")
                : rates.Count + " prices -> " + rates.Count + " lines";
            r["UnitPrice"] = agreed;
            r["Monthly"] = field == "OwnRate"
                ? (agreed > 0m ? (agreed * L.Rows.Count).ToString("n2")
                               : SumOf(L, field).ToString("n2") + "  own")
                : "by usage";
            r["Side"] = L.Side;
            r["GroupCode"] = L.GroupCode;
            r["Field"] = field;
            _dtLines.Rows.Add(r);
        }

        /// <summary>The minimum and the waive are terms measured in ringgit over the line's black and
        /// colour, not prices on a charge — so they get a row each rather than a column nothing else
        /// would use, and they carry the two figures the deal actually has.
        ///
        /// <para>They are stored where the engine already looks: a COMMIT or WAIVE meter on the first
        /// machine of the line, scoped 'G' so it is measured over the machines that share the line.
        /// That is the same mechanism the committed minimum has always used; nothing new is invented,
        /// so nothing can be written here and quietly never read.</para></summary>
        private void AddTermRow(Line L, string charge, string role)
        {
            decimal at = 0m, amt = 0m;
            DataRow meter = FindTermMeter(L, role);
            if (meter != null)
            {
                if (role == "WAIVE")
                {
                    at = Dec(meter, "WaiveTargetAmount");
                    amt = Math.Abs(Dec(meter, "MinimumCharges"));
                    if (amt <= 0m) amt = Math.Abs(Dec(meter, "ChargesRate"));
                }
                else
                {
                    amt = Dec(meter, "MinimumCharges");
                }
            }

            DataRow r = _dtLines.NewRow();
            r["LineName"] = L.Name;
            r["Charge"] = charge;
            r["Units"] = L.Rows.Count;
            r["OnMachines"] = role == "WAIVE"
                ? (at > 0m ? "if BK+CL reaches " + at.ToString("n2") : "not set")
                : (amt > 0m ? "over this line's BK+CL" : "not set");
            r["UnitPrice"] = amt;
            r["Monthly"] = role == "WAIVE"
                ? (at > 0m ? "waive " + amt.ToString("n2") : "")
                : (amt > 0m ? "billed if the copies come to less" : "");
            r["Side"] = L.Side;
            r["GroupCode"] = L.GroupCode;
            r["Field"] = role;
            _dtLines.Rows.Add(r);
        }

        /// <summary>The COMMIT or WAIVE meter that carries this line's terms, if one has been set.
        /// It lives on the FIRST machine of the line: one meter states the deal for the line, the way
        /// one row states it on paper — spreading it over every machine would bill it that many times.
        /// </summary>
        private DataRow FindTermMeter(Line L, string role)
        {
            foreach (int i in L.Rows)
            {
                ItemEditData d = ItemAt(i);
                if (d == null || d.Meters == null) continue;
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState == DataRowState.Deleted) continue;
                    if (Str(mr, "MeterRole").Trim().ToUpperInvariant() == role) return mr;
                }
            }
            return null;
        }

        private ItemEditData ItemAt(int machineRow)
        {
            int idx = Convert.ToInt32(_dtMachines.Rows[machineRow]["Idx"]);
            return idx >= 0 && idx < _items.Count ? _items[idx] : null;
        }

        private decimal SumOf(Line L, string field)
        {
            decimal sum = 0m;
            foreach (int i in L.Rows) sum += Convert.ToDecimal(_dtMachines.Rows[i][field]);
            return sum;
        }

        // ---------- grouping ----------

        private List<DataRow> Ticked()
        {
            List<DataRow> picked = new List<DataRow>();
            foreach (DataRow r in _dtMachines.Rows)
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) picked.Add(r);
            return picked;
        }

        private void MergeTicked(bool rental, bool meter, string what)
        {
            GridViewMachines.PostEditor();
            List<DataRow> picked = Ticked();
            if (picked.Count < 2)
            {
                XtraMessageBox.Show("Tick at least two machines to put them on one line.",
                    "Lines & Price", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool same = true;
            string model0 = Convert.ToString(picked[0]["Model"]);
            foreach (DataRow r in picked)
                if (!string.Equals(Convert.ToString(r["Model"]), model0, StringComparison.OrdinalIgnoreCase))
                    same = false;

            string suggest = same ? model0 : "GROUP " + (_seq + 1);
            string name = XtraInputBox.Show("What is this " + what + " line called?", "Lines & Price", suggest);
            if (name == null) return;
            name = ScpRentalGroupPrice.Sanitize(name);
            if (name.Length == 0) return;
            if (!same) _seq++;

            foreach (DataRow r in picked)
            {
                if (rental) { r["MergeGroup"] = name; r["PrintsOn"] = name; }
                if (meter) r["MeterGroup"] = name;
                r["Sel"] = false;
            }
            _changed = true;
            RebuildLines();
        }

        private void BtnMerge_Click(object sender, EventArgs e) { MergeTicked(true, false, "rental"); }
        private void BtnMergeMeter_Click(object sender, EventArgs e) { MergeTicked(false, true, "BK+CL"); }
        private void BtnMergeBoth_Click(object sender, EventArgs e) { MergeTicked(true, true, "rental and BK+CL"); }

        private void BtnUngroup_Click(object sender, EventArgs e)
        {
            GridViewMachines.PostEditor();
            List<DataRow> picked = Ticked();
            if (picked.Count == 0) return;
            foreach (DataRow r in picked)
            {
                r["MergeGroup"] = "";
                r["MeterGroup"] = "";
                r["PrintsOn"] = Convert.ToString(r["ItemNo"]);
                r["Sel"] = false;
            }
            _changed = true;
            RebuildLines();
        }

        /// <summary>Group by what a line can actually carry. Machines on the same price merge into one
        /// line and stay there; machines on different prices were never going to share a line, so this
        /// is the largest merge that does not come apart the moment it prints.</summary>
        private void BtnByPrice_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtMachines.Rows)
            {
                r["MergeGroup"] = "RENTAL " + Convert.ToDecimal(r["OwnRate"]).ToString("n2");
                r["MeterGroup"] = "COPIES " + Convert.ToDecimal(r["OwnBk"]).ToString("n4") +
                                  " / " + Convert.ToDecimal(r["OwnCl"]).ToString("n4");
                r["PrintsOn"] = Convert.ToString(r["MergeGroup"]);
                r["Sel"] = false;
            }
            _changed = true;
            RebuildLines();
        }

        private void BtnByModel_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtMachines.Rows)
            {
                string m = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["Model"]));
                r["MergeGroup"] = m;
                r["MeterGroup"] = m;
                r["PrintsOn"] = m;
                r["Sel"] = false;
            }
            _changed = true;
            RebuildLines();
        }

        // ---------- prices ----------

        private void GridViewLines_CellValueChanged(object sender,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != ColUnitPrice) return;
            _changed = true;
            GridViewLines.PostEditor();

            DataRow r = GridViewLines.GetDataRow(e.RowHandle);
            if (r != null)
            {
                string field = Convert.ToString(r["Field"]);
                if (field == "WAIVE" || field == "COMMIT")
                {
                    decimal v = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                    WriteTerm(r, v);
                    if (field == "WAIVE" && v > 0m) AskWaiveTarget(r);
                    RebuildLines();
                    return;
                }
            }
            RecomputeMonthly();
            RefreshSummary();
        }

        /// <summary>Write a line's minimum or waive onto the meter the engine reads. Zero clears it:
        /// the row is removed rather than left at nothing, so a term that was taken off the deal does
        /// not sit in the data waiting to surprise somebody.</summary>
        private void WriteTerm(DataRow lineRow, decimal value)
        {
            string role = Convert.ToString(lineRow["Field"]);
            string grp = Convert.ToString(lineRow["GroupCode"]).Trim();
            Line target = null;
            foreach (Line L in Collect(Convert.ToString(lineRow["Side"])))
            {
                bool mine = grp.Length > 0
                    ? string.Equals(L.GroupCode, grp, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(L.Name, Convert.ToString(lineRow["LineName"]), StringComparison.OrdinalIgnoreCase);
                if (mine) { target = L; break; }
            }
            if (target == null || target.Rows.Count == 0) return;

            DataRow existing = FindTermMeter(target, role);
            if (value <= 0m)
            {
                if (existing != null) existing.Delete();
                return;
            }

            if (existing == null)
            {
                ItemEditData first = ItemAt(target.Rows[0]);
                if (first == null || first.Meters == null) return;
                existing = first.Meters.NewRow();
                existing["MeterTypeCode"] = role;
                existing["MeterRole"] = role;
                existing["Description"] = role == "WAIVE"
                    ? "RENTAL WAIVE" : "MINIMUM COMMITTED PRINT CHARGES";
                first.Meters.Rows.Add(existing);
            }

            // Scoped over the LINE, not the machine it happens to sit on. Same column the committed
            // minimum has always used to say the same thing.
            if (existing.Table.Columns.Contains("CommitScope"))
                existing["CommitScope"] = target.GroupCode.Length > 0 ? "G" : "S";
            if (existing.Table.Columns.Contains("WaiveScope")) existing["WaiveScope"] = "BKCL";

            if (role == "WAIVE")
            {
                existing["MinimumCharges"] = value;      // what comes off the rental
            }
            else
            {
                existing["MinimumCharges"] = value;      // the floor the copies are topped up to
            }
        }

        /// <summary>The waive's other half: how much the line's black and colour have to reach.</summary>
        private void WriteWaiveTarget(DataRow lineRow, decimal target)
        {
            string grp = Convert.ToString(lineRow["GroupCode"]).Trim();
            Line line = null;
            foreach (Line L in Collect(SIDE_R))
            {
                bool mine = grp.Length > 0
                    ? string.Equals(L.GroupCode, grp, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(L.Name, Convert.ToString(lineRow["LineName"]), StringComparison.OrdinalIgnoreCase);
                if (mine) { line = L; break; }
            }
            if (line == null) return;
            DataRow m = FindTermMeter(line, "WAIVE");
            if (m == null) return;
            if (m.Table.Columns.Contains("WaiveTargetAmount")) m["WaiveTargetAmount"] = target;
        }

        private void RecomputeMonthly()
        {
            foreach (DataRow r in _dtLines.Rows)
            {
                if (Convert.ToString(r["Field"]) != "OwnRate") continue;
                decimal agreed = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                int units = Convert.ToInt32(r["Units"]);
                if (agreed > 0m) { r["Monthly"] = (agreed * units).ToString("n2"); continue; }
                decimal sum = 0m;
                string grp = Convert.ToString(r["GroupCode"]).Trim();
                foreach (DataRow m in _dtMachines.Rows)
                {
                    string g = Convert.ToString(m["MergeGroup"]).Trim();
                    bool mine = grp.Length > 0
                        ? string.Equals(g, grp, StringComparison.OrdinalIgnoreCase)
                        : string.Equals(Convert.ToString(m["ItemNo"]), Convert.ToString(r["LineName"]),
                                        StringComparison.OrdinalIgnoreCase);
                    if (mine) sum += Convert.ToDecimal(m["OwnRate"]);
                }
                r["Monthly"] = sum.ToString("n2") + "  own";
            }
        }

        /// <summary>A waive has two figures — what the copies have to reach, and what comes off the
        /// rental when they do. The grid cell holds the second; this asks for the first, because a
        /// waive with no target fires every month and gives the rental away.</summary>
        private void AskWaiveTarget(DataRow lineRow)
        {
            string current = "0.00";
            string grp = Convert.ToString(lineRow["GroupCode"]).Trim();
            foreach (Line L in Collect(SIDE_R))
            {
                bool mine = grp.Length > 0
                    ? string.Equals(L.GroupCode, grp, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(L.Name, Convert.ToString(lineRow["LineName"]), StringComparison.OrdinalIgnoreCase);
                if (!mine) continue;
                DataRow m = FindTermMeter(L, "WAIVE");
                if (m != null) current = Dec(m, "WaiveTargetAmount").ToString("n2");
                break;
            }

            string answer = XtraInputBox.Show(
                "When this line's black and colour reach how much in a month?" + Environment.NewLine +
                "0 = waive every month, whatever they print.",
                "Rental waive", current);
            if (answer == null) return;
            decimal t;
            if (!decimal.TryParse(answer.Trim(), out t) || t < 0m) t = 0m;
            WriteWaiveTarget(lineRow, t);
        }

        /// <summary>One agreed figure per line, taken from the machines — the highest where they
        /// disagree, because dropping to the lowest quietly gives money away.</summary>
        private void BtnFromMachines_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtLines.Rows)
            {
                string field = Convert.ToString(r["Field"]);
                if (field == "WAIVE" || field == "COMMIT") continue;   // terms, not a unit price
                string side = Convert.ToString(r["Side"]);
                string grp = Convert.ToString(r["GroupCode"]).Trim();
                decimal top = 0m;
                foreach (DataRow m in _dtMachines.Rows)
                {
                    string g = Convert.ToString(m[side == SIDE_R ? "MergeGroup" : "MeterGroup"]).Trim();
                    bool mine = grp.Length > 0
                        ? string.Equals(g, grp, StringComparison.OrdinalIgnoreCase)
                        : (g.Length == 0 && string.Equals(Convert.ToString(m["ItemNo"]),
                                                          Convert.ToString(r["LineName"]),
                                                          StringComparison.OrdinalIgnoreCase));
                    if (!mine) continue;
                    decimal v = Convert.ToDecimal(m[field]);
                    if (v > top) top = v;
                }
                r["UnitPrice"] = top;
            }
            _changed = true;
            RecomputeMonthly();
            RefreshSummary();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtLines.Rows)
            {
                string f = Convert.ToString(r["Field"]);
                if (f == "WAIVE" || f == "COMMIT") continue;   // clearing prices is not tearing up terms
                r["UnitPrice"] = 0m;
            }
            _changed = true;
            RecomputeMonthly();
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            int rentalLines = 0, meterLines = 0, priced = 0, split = 0;
            foreach (DataRow r in _dtLines.Rows)
            {
                bool isRental = Convert.ToString(r["Field"]) == "OwnRate";
                if (isRental) rentalLines++;
                decimal p = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (p > 0m) priced++;
                else if (Convert.ToString(r["OnMachines"]).IndexOf("prices ->", StringComparison.Ordinal) >= 0)
                    split++;
            }
            foreach (Line L in Collect(SIDE_M)) meterLines += 2;

            LblSummary.Text =
                rentalLines + (rentalLines == 1 ? " rental line" : " rental lines") + "  ·  " +
                meterLines + " BK+CL " + (meterLines == 1 ? "line" : "lines") + "  ·  " +
                _dtMachines.Rows.Count + " machines  ·  " + priced + " priced here" +
                (split > 0
                    ? "   ·   " + split + (split == 1 ? " line" : " lines") +
                      " still prints one per price until a figure is agreed"
                    : "   (the rest bill at the machine's own rate)");
        }

        // ---------- out ----------

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Harvest();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Harvest()
        {
            GridViewLines.PostEditor();
            GridViewMachines.PostEditor();

            // the grouping goes back onto the machines
            foreach (DataRow r in _dtMachines.Rows)
            {
                int idx = Convert.ToInt32(r["Idx"]);
                if (idx < 0 || idx >= _items.Count) continue;
                _items[idx].MergeGroupCode = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["MergeGroup"]));
                _items[idx].MergeGroupCodeMeter = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["MeterGroup"]));
            }

            // and the agreed figures go back as one row per line per side
            Result = new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _dtLines.Rows)
            {
                string fld = Convert.ToString(r["Field"]);
                if (fld == "WAIVE" || fld == "COMMIT") continue;   // those live on the meter, not here
                decimal p = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (p <= 0m) continue;
                string side = Convert.ToString(r["Side"]);
                string grp = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["GroupCode"]));
                string key = ScpLineTerms.Key(side, grp);
                ScpLineTerms t;
                if (!Result.TryGetValue(key, out t))
                {
                    t = new ScpLineTerms();
                    t.Side = side;
                    t.GroupCode = grp;
                    Result[key] = t;
                }
                string field = Convert.ToString(r["Field"]);
                if (field == "OwnRate") t.UnitPrice = p;
                else if (field == "OwnBk") t.BkPrice = p;
                else t.ClPrice = p;
            }
        }

        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            if (XtraMessageBox.Show("You have unsaved changes. Discard them and close?",
                    "Lines & Price", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                e.Cancel = true;
        }
    }
}
