using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.CommonForms;
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
        private class Line
        {
            public string Side;

            public string GroupCode;

            public string Name;

            public List<int> Rows = new List<int>();
        }

        private const string SIDE_R = "R";

        private const string SIDE_M = "M";

        private readonly List<ItemEditData> _items;

        private readonly Dictionary<string, ScpLineTerms> _terms;   // "SIDE|GROUP" -> agreed figures

        private DataTable _dtMachines;

        private DataTable _dtLines;

        private DataTable _dtLineMachines;

        private bool _changed;

        private int _seq;

        public Dictionary<string, ScpLineTerms> Result = new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);

        public string ResultSplit = "ONE";

        public bool ResultShowModel = true;

        public bool ResultShowSerial = true;

        public bool ResultShowUnits = true;

        public DBSetting Db;

        public Action ShowSample;

        private bool _suppressSplit;

        private bool _filling;

        private bool WaiveFits
        {
            get
            {
                return ResultSplit == "ONE" || ResultSplit == "PM";
            }
        }

        public RentalGroupPrice_Form()
        {
            InitializeComponent();
            ApplyButtonIcons();
        }

        /// <summary>A 16 px icon beside each button's words, from the DevExpress image library AutoCount
        /// already ships. The skin's own colours are kept; a missing image leaves the button as it was.</summary>
        private void ApplyButtonIcons()
        {
            SetIcon(BtnSample, "svgimages/reports/preview.svg");
            SetIcon(BtnToggleOptions, "svgimages/outlook%20inspired/expandcollapse.svg");
            SetIcon(BtnMerge, "svgimages/reports/mergecells.svg");
            SetIcon(BtnMergeMeter, "svgimages/reports/mergecells.svg");
            SetIcon(BtnMergeBoth, "svgimages/dashboards/group.svg");
            SetIcon(BtnUngroup, "svgimages/spreadsheet/pivottableungroup.svg");
            SetIcon(BtnBkTiers, "svgimages/format/listmultilevel.svg");
            SetIcon(BtnClTiers, "svgimages/format/listmultilevel.svg");
            SetIcon(BtnByPrice, "svgimages/business%20objects/bo_price.svg");
            SetIcon(BtnByModel, "svgimages/business%20objects/bo_product_group.svg");
            SetIcon(BtnUngroupAll, "svgimages/spreadsheet/pivottableungroup.svg");
            SetIcon(BtnSameModel, "svgimages/icon%20builder/actions_checkcircled.svg");
        }

        private static void SetIcon(SimpleButton b, string key)
        {
            try
            {
                DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(key);
                if (b == null || img == null) return;
                b.ImageOptions.SvgImage = img;
                b.ImageOptions.SvgImageSize = new Size(16, 16);
                b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
                b.ImageOptions.ImageToTextIndent = 6;
            }
            catch { }
        }

        public RentalGroupPrice_Form(List<ItemEditData> items, Dictionary<string, ScpLineTerms> current, string split)
            : this()
        {
            _items = items ?? new List<ItemEditData>();
            _terms = current ?? new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);
            ResultSplit = (split ?? "ONE").Trim().ToUpperInvariant();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            RgSplit.EditValue = ResultSplit;
            ChkShowModel.Checked = ResultShowModel;
            ChkShowSerial.Checked = ResultShowSerial;
            ChkShowUnits.Checked = ResultShowUnits;
            RgSplit.EditValueChanged += RgSplit_Changed;
            BtnSample.Click += BtnSample_Click;
            BtnToggleOptions.Click += BtnToggleOptions_Click;
            SetOptionsShown(!OptionsHiddenSaved());
            GridViewLines.FocusedRowChanged += GridViewLines_FocusedRowChanged;
            GridViewMachines.RowCellStyle += GridViewMachines_RowCellStyle;
            BtnSameModel.Click += BtnSameModel_Click;
            GridViewMachines.CellValueChanged += GridViewMachines_CellValueChanged;
            BtnBkTiers.Click += BtnBkTiers_Click;
            BtnClTiers.Click += BtnClTiers_Click;
            BtnMerge.Click += BtnMerge_Click;
            BtnMergeMeter.Click += BtnMergeMeter_Click;
            BtnMergeBoth.Click += BtnMergeBoth_Click;
            BtnUngroup.Click += BtnUngroup_Click;
            BtnByPrice.Click += BtnByPrice_Click;
            BtnByModel.Click += BtnByModel_Click;
            BtnUngroupAll.Click += BtnUngroupAll_Click;
            RepoTerms.ButtonClick += RepoTerms_ButtonClick;
            RepoTiers.ButtonClick += RepoTiers_ButtonClick;
            GridViewLines.DoubleClick += BtnTerms_Click;
            BtnOK.Click += BtnOK_Click;
            GridViewLines.RowCellStyle += GridViewLines_RowCellStyle;
            GridViewLines.CustomColumnSort += GridViewLines_CustomColumnSort;
            GridViewLines.MasterRowEmpty += GridViewLines_MasterRowEmpty;
            GridViewLines.CustomDrawCell += GridViewLines_DrawPad;
            GridViewLines.ShowingEditor += GridViewLines_ShowingEditor;
            GridViewLines.CustomColumnDisplayText += GridViewLines_DisplayText;
            GridViewLines.CellValueChanged += GridViewLines_CellValueChanged;
            base.FormClosing += OnFormClosingGuard;
            BuildMachines();
            RebuildLines();
        }

        private void BuildMachines()
        {
            _dtMachines = new DataTable();
            _dtMachines.Columns.Add("Sel", typeof(bool));
            _dtMachines.Columns.Add("ServiceItemNo", typeof(string));
            _dtMachines.Columns.Add("SerialNumber", typeof(string));
            _dtMachines.Columns.Add("ItemCode", typeof(string));
            _dtMachines.Columns.Add("MergeGroup", typeof(string));
            _dtMachines.Columns.Add("MeterGroup", typeof(string));
            _dtMachines.Columns.Add("PrintsOn", typeof(string));
            _dtMachines.Columns.Add("OwnRate", typeof(decimal));
            _dtMachines.Columns.Add("OwnBk", typeof(decimal));
            _dtMachines.Columns.Add("OwnCl", typeof(decimal));
            _dtMachines.Columns.Add("BillGroup", typeof(string));
            _dtMachines.Columns.Add("LineLabel", typeof(string));
            _dtMachines.Columns.Add("HasRental", typeof(bool));
            _dtMachines.Columns.Add("HasBk", typeof(bool));
            _dtMachines.Columns.Add("HasCl", typeof(bool));
            _dtMachines.Columns.Add("BkTier", typeof(string));
            _dtMachines.Columns.Add("ClTier", typeof(string));
            _dtMachines.Columns.Add("Idx", typeof(int));
            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d != null && !d.IsGroupItem && !d.Inactive)
                {
                    DataRow r = _dtMachines.NewRow();
                    r["ServiceItemNo"] = d.ServiceItemNo ?? "";
                    r["SerialNumber"] = d.SerialNumber ?? "";
                    r["ItemCode"] = d.ItemCode ?? "";
                    r["MergeGroup"] = d.MergeGroupCode ?? "";
                    r["MeterGroup"] = d.MergeGroupCodeMeter ?? "";
                    r["PrintsOn"] = LineNameOf(d.MergeGroupCode, d.ServiceItemNo);
                    decimal rate;
                    r["OwnRate"] = (RateOf(d, "RENTAL", out rate) ? rate : 0m);
                    r["OwnBk"] = (RateOf(d, "BK", out rate) ? rate : 0m);
                    r["OwnCl"] = (RateOf(d, "CL", out rate) ? rate : 0m);
                    r["BillGroup"] = (d.BillGroupCode ?? "").Trim();
                    r["LineLabel"] = (d.LineGroupCode ?? "").Trim();
                    r["HasRental"] = MeterOf(d, "RENTAL") != null;
                    r["HasBk"] = MeterOf(d, "BK") != null;
                    r["HasCl"] = MeterOf(d, "CL") != null;
                    r["BkTier"] = TierWordsFor(d, "BK");
                    r["ClTier"] = TierWordsFor(d, "CL");
                    r["Idx"] = i;
                    _dtMachines.Rows.Add(r);
                }
            }
            GridMachines.DataSource = _dtMachines;
        }

        /// <summary>A machine's own rate for one role, from its meter rows. Rentals are the awkward
        /// one: the old book puts a rental's money in MinimumCharges as often as in ChargesRate, so
        /// both are read and the larger stands.</summary>
        private static bool RateOf(ItemEditData d, string role, out decimal rate)
        {
            rate = default(decimal);
            if (d == null || d.Meters == null)
            {
                return false;
            }
            bool found = false;
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                string r = Str(mr, "MeterRole").Trim().ToUpperInvariant();
                string t = Str(mr, "MeterTypeCode").Trim().ToUpperInvariant();
                bool num;
                if (!(role == "RENTAL"))
                {
                    if (r == role)
                    {
                        goto IL_00d6;
                    }
                    if (r.Length != 0)
                    {
                        continue;
                    }
                    num = t == role;
                }
                else
                {
                    num = ScpStrategy.IsRentalRole(r, t);
                }
                if (!num)
                {
                    continue;
                }
                goto IL_00d6;
                IL_00d6:
                decimal v = Dec(mr, "ChargesRate");
                if (role == "RENTAL")
                {
                    decimal m = Dec(mr, "MinimumCharges");
                    if (m > v)
                    {
                        v = m;
                    }
                }
                if (v > rate)
                {
                    rate = v;
                }
                found = true;
            }
            return found;
        }

        private static string Str(DataRow r, string col)
        {
            return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToString(r[col]) : "";
        }

        private static decimal Dec(DataRow r, string col)
        {
            return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToDecimal(r[col]) : 0m;
        }

        private static string LineNameOf(string group, string itemNo)
        {
            string g = (group ?? "").Trim();
            return (g.Length > 0) ? g : (itemNo ?? "");
        }

        /// <summary>Whether any machine on this line carries the meter the column stands for.</summary>
        private bool AnyOn(Line L, string flag)
        {
            foreach (int i in L.Rows)
                if (Flag(_dtMachines.Rows[i], flag)) return true;
            return false;
        }

        private List<Line> Collect(string side)
        {
            List<Line> outp = new List<Line>();
            Dictionary<string, Line> by = new Dictionary<string, Line>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _dtMachines.Rows.Count; i++)
            {
                DataRow r = _dtMachines.Rows[i];
                // A machine only joins a line for a charge it actually carries. KENSINGTON has
                // eighteen machines and not one rental meter, and this list was giving it
                // eighteen "Monthly rental" rows at 0.00 -- a rental the customer does not pay,
                // offered for pricing, and counted in the footer. The tick box above is where a
                // rental gets added; until it is ticked there is no line to price.
                if (side == "R" ? !Flag(r, "HasRental")
                                : (!Flag(r, "HasBk") && !Flag(r, "HasCl")))
                    continue;
                string g = Convert.ToString(r[(side == "R") ? "MergeGroup" : "MeterGroup"]).Trim();
                // Not merged = a line of its own. Keyed by the machine's place, not its item number: a
                // machine added today has no number yet, and three of them were landing on one line.
                string key = ((g.Length > 0) ? ("#" + g.ToUpperInvariant()) : ("=" + i));
                Line L;
                if (!by.TryGetValue(key, out L))
                {
                    L = new Line();
                    L.Side = side;
                    L.GroupCode = g;
                    string itemNo = Convert.ToString(r["ServiceItemNo"]).Trim();
                    L.Name = ((g.Length > 0) ? g : (itemNo.Length > 0 ? itemNo : Convert.ToString(r["SerialNumber"])));
                    by[key] = L;
                    outp.Add(L);
                }
                L.Rows.Add(i);
            }
            return outp;
        }

        private void RebuildLines()
        {
            _dtLines = new DataTable("Lines");
            _dtLines.Columns.Add("LineName", typeof(string));
            _dtLines.Columns.Add("Charge", typeof(string));
            _dtLines.Columns.Add("Units", typeof(int));
            _dtLines.Columns.Add("OnMachines", typeof(string));
            _dtLines.Columns.Add("UnitPrice", typeof(decimal));
            _dtLines.Columns.Add("Monthly", typeof(string));
            _dtLines.Columns.Add("Side", typeof(string));
            _dtLines.Columns.Add("GroupCode", typeof(string));
            _dtLines.Columns.Add("Field", typeof(string));
            _dtLines.Columns.Add("Invoice", typeof(string));
            _dtLines.Columns.Add("LineKey", typeof(string));
            _dtLines.Columns.Add("Terms", typeof(string));
            _dtLines.Columns.Add("Tiers", typeof(string));
            _dtLines.Columns.Add("LadderText", typeof(string));
            _dtLines.Columns.Add("LineLabel", typeof(string));
            _dtLines.Columns.Add("MachineIdx", typeof(string));
            _dtLines.Columns.Add("SoloIdx", typeof(int));
            _dtLines.Columns.Add("BillGroupN", typeof(int));
            _dtLines.Columns.Add("BillGroup1", typeof(string));
            _dtLines.Columns.Add("Pad", typeof(string));
            _dtLineMachines = new DataTable("LineMachines");
            _dtLineMachines.Columns.Add("LineKey", typeof(string));
            _dtLineMachines.Columns.Add("ServiceItemNo", typeof(string));
            _dtLineMachines.Columns.Add("SerialNumber", typeof(string));
            _dtLineMachines.Columns.Add("ItemCode", typeof(string));
            _dtLineMachines.Columns.Add("OwnRate", typeof(decimal));
            _dtLineMachines.Columns.Add("OwnBk", typeof(decimal));
            _dtLineMachines.Columns.Add("OwnCl", typeof(decimal));
            foreach (Line L in Collect("R"))
            {
                AddLineRow(L, "Monthly rental", "OwnRate", 2);
            }
            foreach (Line L2 in Collect("M"))
            {
                // Same rule one level down: a line of machines that have no colour counter
                // between them has no colour line to show.
                if (AnyOn(L2, "HasBk")) AddLineRow(L2, "Black copies", "OwnBk", 4);
                if (AnyOn(L2, "HasCl")) AddLineRow(L2, "Colour copies", "OwnCl", 4);
            }
            StampInvoices();
            DataSet ds = new DataSet("LinesAndMachines");
            ds.Tables.Add(_dtLines);
            ds.Tables.Add(_dtLineMachines);
            ds.Relations.Add("Machines on this line", _dtLines.Columns["LineKey"], _dtLineMachines.Columns["LineKey"], false);
            GridLines.DataSource = ds;
            GridLines.DataMember = "Lines";
            GridViewMachines.RefreshData();
            bool oneInvoice = ResultSplit == "ONE" && InvoiceCount() <= 1;
            ColInvoice.GroupIndex = (oneInvoice ? (-1) : 0);
            GridViewLines.ExpandAllGroups();
            RefreshSummary();
        }

        /// <summary>
        /// A line's rows, one per invoice it prints on.
        ///
        /// <para>A bill group is its own invoice, and the rules below it -- merged or not, one invoice or
        /// rental apart -- apply inside each group. So a line is listed under every bill group its
        /// machines are in, each time with only that group's machines: the way the invoices will read.
        /// The price and the terms stay the line's own; editing one row changes them on all of its rows.</para>
        /// </summary>
        private void AddLineRow(Line L, string charge, string field, int dp)
        {
            foreach (string bg in BillGroupsOf(L))
            {
                Line part = new Line();
                part.Side = L.Side;
                part.GroupCode = L.GroupCode;
                part.Name = L.Name;
                foreach (int i in L.Rows)
                    if (string.Equals(BillGroupOfMachine(i), bg, StringComparison.OrdinalIgnoreCase)) part.Rows.Add(i);
                if (part.Rows.Count > 0) AddLinePart(L, part, bg, charge, field, dp);
            }
        }

        private void AddLinePart(Line L, Line part, string bg, string charge, string field, int dp)
        {
            List<decimal> rates = new List<decimal>();
            foreach (int i in part.Rows)
            {
                decimal v = Convert.ToDecimal(_dtMachines.Rows[i][field]);
                if (!rates.Contains(v))
                {
                    rates.Add(v);
                }
            }
            ScpLineTerms t;
            _terms.TryGetValue(ScpLineTerms.Key(L.Side, L.GroupCode), out t);
            decimal agreed = ((t == null) ? 0m : ((field == "OwnRate") ? t.UnitPrice : ((field == "OwnBk") ? t.BkPrice : t.ClPrice)));
            DataRow r = _dtLines.NewRow();
            r["LineName"] = NameWithMachines(L);
            r["Charge"] = charge;
            r["Units"] = part.Rows.Count;
            r["OnMachines"] = ((rates.Count == 1) ? rates[0].ToString((dp == 4) ? "n4" : "n2") : (rates.Count + " prices -> " + rates.Count + " lines"));
            r["UnitPrice"] = agreed;
            r["Monthly"] = ((!(field == "OwnRate")) ? "by usage" : ((agreed > 0m) ? (agreed * (decimal)part.Rows.Count).ToString("n2") : (SumOf(part, field).ToString("n2") + "  own")));
            r["Terms"] = DescribeTerms(L);
            string lad = (string)(r["LadderText"] = ((t == null || field == "OwnRate") ? "" : ((field == "OwnBk") ? (t.LadderBk ?? "") : (t.LadderCl ?? ""))));
            r["Tiers"] = ((field == "OwnRate") ? "" : ((L.Rows.Count == 1) ? MachineLadderWords(L, field) : DescribeLadder(lad, L, field)));
            r["Pad"] = "";
            string key = LineKeyOf(L) + "|" + bg.ToUpperInvariant();
            r["LineKey"] = key;
            FillLineMachines(key, part.Rows);
            r["Side"] = L.Side;
            r["GroupCode"] = L.GroupCode;
            r["Field"] = field;
            r["SoloIdx"] = ((L.Rows.Count == 1) ? L.Rows[0] : (-1));
            r["LineLabel"] = LabelsOf(part);
            List<string> idx = new List<string>();
            for (int j = 0; j < part.Rows.Count; j++)
            {
                idx.Add(part.Rows[j].ToString());
            }
            idx.Sort(StringComparer.Ordinal);
            r["MachineIdx"] = string.Join(",", idx.ToArray());
            r["BillGroupN"] = 1;
            r["BillGroup1"] = bg;
            _dtLines.Rows.Add(r);
        }

        /// <summary>A machine's bill group as the billing engine reads it.</summary>
        private string BillGroupOfMachine(int machineRow)
        {
            return ScpStrategy.SanitizeBillGroup(Convert.ToString(_dtMachines.Rows[machineRow]["BillGroup"]));
        }

        /// <summary>Copies an edit to the line's rows under the other invoices: a merged line has one
        /// price, however many bill groups it prints in.</summary>
        private void SyncLineRows(DataRow src, string column)
        {
            if (src == null) return;
            string grp = Convert.ToString(src["GroupCode"]).Trim();
            if (grp.Length == 0) return;
            foreach (DataRow other in _dtLines.Rows)
            {
                if (other == src) continue;
                if (Convert.ToString(other["Side"]) != Convert.ToString(src["Side"])) continue;
                if (Convert.ToString(other["Field"]) != Convert.ToString(src["Field"])) continue;
                if (!string.Equals(Convert.ToString(other["GroupCode"]).Trim(), grp, StringComparison.OrdinalIgnoreCase)) continue;
                other[column] = src[column];
            }
        }

        /// <summary>The machine row indexes a lines-grid row stands for.</summary>
        private static List<int> MachinesOfRow(DataRow lineRow)
        {
            List<int> list = new List<int>();
            string csv = lineRow == null || lineRow["MachineIdx"] == DBNull.Value ? "" : Convert.ToString(lineRow["MachineIdx"]);
            foreach (string part in csv.Split(','))
            {
                int i;
                if (int.TryParse(part, out i)) list.Add(i);
            }
            return list;
        }

        private string LabelsOf(Line L)
        {
            List<string> seen = new List<string>();
            if (L == null)
            {
                return "";
            }
            for (int i = 0; i < L.Rows.Count; i++)
            {
                int idx = L.Rows[i];
                if (idx < 0 || idx >= _dtMachines.Rows.Count)
                {
                    continue;
                }
                string lb = Convert.ToString((_dtMachines.Rows[idx]["LineLabel"] == DBNull.Value) ? "" : _dtMachines.Rows[idx]["LineLabel"]).Trim();
                if (lb.Length == 0)
                {
                    continue;
                }
                bool had = false;
                for (int k = 0; k < seen.Count; k++)
                {
                    if (string.Equals(seen[k], lb, StringComparison.OrdinalIgnoreCase))
                    {
                        had = true;
                    }
                }
                if (!had)
                {
                    seen.Add(lb);
                }
            }
            return string.Join(", ", seen.ToArray());
        }

        private void SetLabelFor(string machineIdxCsv, string label)
        {
            if (string.IsNullOrEmpty(machineIdxCsv))
            {
                return;
            }
            string[] array = machineIdxCsv.Split(',');
            foreach (string part in array)
            {
                int idx;
                if (int.TryParse(part, out idx) && idx >= 0 && idx < _dtMachines.Rows.Count)
                {
                    _dtMachines.Rows[idx]["LineLabel"] = label ?? "";
                }
            }
            GridViewMachines.RefreshData();
        }

        private decimal RentalOfLine(Line L)
        {
            if (L == null || L.Side != "R")
            {
                return 0m;
            }
            string lineKey = LineKeyOf(L) + "|";
            foreach (DataRow r in _dtLines.Rows)
            {
                if (Convert.ToString(r["Field"]) != "OwnRate" || !Convert.ToString(r["LineKey"]).StartsWith(lineKey, StringComparison.Ordinal))
                {
                    continue;
                }
                decimal agreed = ((r["UnitPrice"] == DBNull.Value) ? 0m : Convert.ToDecimal(r["UnitPrice"]));
                if (agreed > 0m)
                {
                    return agreed * (decimal)L.Rows.Count;
                }
                break;
            }
            decimal sum = default(decimal);
            for (int i = 0; i < L.Rows.Count; i++)
            {
                int idx = L.Rows[i];
                if (idx >= 0 && idx < _dtMachines.Rows.Count)
                {
                    sum += Dec2(_dtMachines.Rows[idx]["OwnRate"]);
                }
            }
            return sum;
        }

        private List<string> BillGroupsOf(Line L)
        {
            List<string> g = new List<string>();
            foreach (int i in L.Rows)
            {
                string bg = BillGroupOfMachine(i);
                bool had = false;
                foreach (string x in g) if (string.Equals(x, bg, StringComparison.OrdinalIgnoreCase)) { had = true; break; }
                if (!had)
                {
                    g.Add(bg);
                }
            }
            return g;
        }

        private void StampInvoices()
        {
            bool perMachine = ResultSplit == "PM" || ResultSplit == "PMS";
            foreach (DataRow r in _dtLines.Rows)
            {
                bool rental = Convert.ToString(r["Side"]) == "R";
                int bgCount = ((r["BillGroupN"] != DBNull.Value) ? Convert.ToInt32(r["BillGroupN"]) : 0);
                string bgOne = ((r["BillGroup1"] == DBNull.Value) ? "" : Convert.ToString(r["BillGroup1"]));
                if (bgCount > 1)
                {
                    r["Invoice"] = "!  billed on " + bgCount + " separate invoices - prints once on each";
                    continue;
                }
                string bg1 = bgOne;
                string inv = ((bg1.Length > 0) ? ("Invoice " + bg1) : "");
                if (bg1.Length > 0 && !perMachine)
                {
                    // A bill group is the invoice for "one invoice" and "rental apart"; per machine
                    // outranks it (below), every machine its own paper whatever its group.
                    bool apart = ResultSplit == "RS" || ResultSplit == "PMS";
                    r["Invoice"] = inv + (apart ? (rental ? " - rental" : " - meters") : "");
                    continue;
                }
                if (!perMachine)
                {
                    r["Invoice"] = ((!(ResultSplit == "RS")) ? ((bg1.Length > 0) ? inv : "The invoice") : ((bg1.Length > 0) ? (inv + (rental ? " - rental" : " - meters")) : (rental ? "Rental invoice" : "Meter invoice")));
                    continue;
                }
                int units = Convert.ToInt32(r["Units"]);
                if (units > 1)
                {
                    r["Invoice"] = "!  covers " + units + " machines - cannot go on a per-machine invoice";
                    continue;
                }
                string who = Convert.ToString(r["LineName"]);
                r["Invoice"] = ((ResultSplit == "PMS") ? (who + (rental ? " - rental" : " - meters")) : who);
            }
        }

        /// <summary>
        /// Whether this waive can only be judged by the meter.
        ///
        /// <para>A usage target and a partial band both ask "how many copies did they
        /// print", and when the rental prints on an invoice of its own that question cannot
        /// be answered from it. Free months ask nothing of the sort.</para>
        /// </summary>
        private bool NeedsCopies(Line L, decimal at)
        {
            if (at > 0m) return true;
            DataRow wm = FindTermMeter(L, "WAIVE");
            return wm != null
                && Dec(wm, "WaivePartialThreshold") > 0m
                && Dec(wm, "WaivePartialAmount") > 0m;
        }

        /// <summary>
        /// The waive, said the way it was actually set.
        ///
        /// <para>It used to read "reach 0.00 -> waive 900.00" whatever the deal was, because it
        /// looked only at the usage target and the amount. A free window said nothing at all, and
        /// a waive with NO condition -- which the engine fires every single month -- came out as
        /// if the customer had to reach zero copies to earn it. Both are the opposite of the
        /// truth, and this column is the only place the deal shows without opening a dialog.</para>
        /// </summary>
        private string WaiveWords(Line L, decimal at, decimal amt)
        {
            DataRow wm = FindTermMeter(L, "WAIVE");
            int firstN = wm == null ? 0 : Int(wm, "WaiveFirstNMonths");
            decimal partAt = wm == null ? 0m : Dec(wm, "WaivePartialThreshold");
            decimal partOff = wm == null ? 0m : Dec(wm, "WaivePartialAmount");

            StringBuilder b = new StringBuilder();
            if (firstN > 0)
                b.Append("free for the first ").Append(firstN)
                 .Append(firstN == 1 ? " month" : " months");

            // FREE MONTHS ARE NOT A WAIVE, and the sentence must not call them one. In the free
            // window the rental is simply not charged -- one line, nothing to pay. A waive bills
            // the rent and credits it back, which is a different deal on the paper.
            if (firstN > 0 && at <= 0m)
            {
                b.Append(" (").Append(amt.ToString("n2")).Append(" a month)");
                if (partAt > 0m && partOff > 0m)
                    b.Append(", or reach ").Append(partAt.ToString("n2"))
                     .Append(" -> waive ").Append(partOff.ToString("n2"));
                return b.ToString();
            }

            // The window and the target run in that order, the way the engine reads them, so the
            // sentence says "then" instead of listing two rival conditions.
            if (at > 0m)
                b.Append(b.Length > 0 ? ", then reach " : "reach ").Append(at.ToString("n2"));
            else
                b.Append("every month");   // no window, no target: the engine waives it always

            b.Append(" -> waive ").Append(amt.ToString("n2"));
            if (partAt > 0m && partOff > 0m)
                b.Append(", or reach ").Append(partAt.ToString("n2"))
                 .Append(" -> waive ").Append(partOff.ToString("n2"));
            return b.ToString();
        }

        private string DescribeTerms(Line L)
        {
            decimal min = default(decimal);
            decimal at = default(decimal);
            decimal amt = default(decimal);
            ReadTerms(L, out min, out at, out amt);
            int nMin = CountTermMeters(L, "COMMIT");
            int nWaive = CountTermMeters(L, "WAIVE");
            if (L.Side == "R")
            {
                if (nWaive > 1)
                {
                    return "own waive each — " + nWaive + " of " + L.Rows.Count + " machines, up to " + SumTermMeters(L, "WAIVE").ToString("n2") + " off";
                }
                if (at <= 0m && amt <= 0m)
                {
                    return "";
                }
                string w = ((nWaive == 1 && L.Rows.Count > 1 && !IsGroupScoped(L, "WAIVE")) ? ("one machine's own waive " + amt.ToString("n2")) : WaiveWords(L, at, amt));
                // The warning is about COPIES, so it belongs only on a waive that counts them.
                // A free window reads a calendar and works whatever invoice the rental is on --
                // flagging that one told the customer their own deal was in doubt.
                return (WaiveFits || !NeedsCopies(L, at))
                    ? w
                    : "! rental is on its own invoice - " + w;
            }
            if (nMin > 1)
            {
                return "own minimum each — " + nMin + " of " + L.Rows.Count + " machines, " + SumTermMeters(L, "COMMIT").ToString("n2") + " in total";
            }
            if (min <= 0m)
            {
                return "";
            }
            return (nMin == 1 && L.Rows.Count > 1 && !IsGroupScoped(L, "COMMIT")) ? ("one machine's own minimum " + min.ToString("n2")) : ("bill at least " + min.ToString("n2"));
        }

        private int CountTermMeters(Line L, string role)
        {
            int n = 0;
            foreach (int i in L.Rows)
            {
                ItemEditData d = ItemAt(i);
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState != DataRowState.Deleted && !(Str(mr, "MeterRole").Trim().ToUpperInvariant() != role))
                    {
                        decimal v = Math.Abs(Dec(mr, "MinimumCharges"));
                        if (v <= 0m)
                        {
                            v = Math.Abs(Dec(mr, "ChargesRate"));
                        }
                        if (v > 0m || Dec(mr, "WaiveTargetAmount") > 0m)
                        {
                            n++;
                        }
                    }
                }
            }
            return n;
        }

        private decimal MinOfMachine(int machineIndex)
        {
            ItemEditData d = ItemAt(machineIndex);
            if (d == null || d.Meters == null)
            {
                return 0m;
            }
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted || Str(mr, "MeterRole").Trim().ToUpperInvariant() != "COMMIT")
                {
                    continue;
                }
                return Math.Abs(Dec(mr, "MinimumCharges"));
            }
            return 0m;
        }

        private void WaiveOfMachine(int machineIndex, out decimal at, out decimal amount, out int freeN)
        {
            at = default(decimal);
            amount = default(decimal);
            freeN = 0;
            ItemEditData d = ItemAt(machineIndex);
            if (d == null || d.Meters == null)
            {
                return;
            }
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted || Str(mr, "MeterRole").Trim().ToUpperInvariant() != "WAIVE")
                {
                    continue;
                }
                at = Dec(mr, "WaiveTargetAmount");
                amount = Math.Abs(Dec(mr, "MinimumCharges"));
                if (amount <= 0m)
                {
                    amount = Math.Abs(Dec(mr, "ChargesRate"));
                }
                freeN = Int(mr, "WaiveFirstNMonths");
                break;
            }
        }

        private static int Int(DataRow r, string col)
        {
            return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToInt32(r[col]) : 0;
        }

        private void ClearTermMeters(Line L, string role)
        {
            foreach (int i in L.Rows)
            {
                ItemEditData d = ItemAt(i);
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                for (int k = d.Meters.Rows.Count - 1; k >= 0; k--)
                {
                    DataRow mr = d.Meters.Rows[k];
                    if (mr.RowState != DataRowState.Deleted && Str(mr, "MeterRole").Trim().ToUpperInvariant() == role)
                    {
                        mr.Delete();
                    }
                }
            }
        }

        private void ClearAllMinimums()
        {
            foreach (ItemEditData d in _items)
            {
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                for (int k = d.Meters.Rows.Count - 1; k >= 0; k--)
                {
                    DataRow mr = d.Meters.Rows[k];
                    if (mr.RowState != DataRowState.Deleted && ScpStrategy.IsCommittedMinRole(Str(mr, "MeterRole"), Str(mr, "MeterTypeCode"), Math.Abs(Dec(mr, "MinimumCharges"))))
                    {
                        mr.Delete();
                    }
                }
            }
        }

        private void WriteMinPerMachine(Line L, DataTable machines, string countOnly)
        {
            ClearTermMeters(L, "COMMIT");
            for (int i = 0; i < L.Rows.Count && i < machines.Rows.Count; i++)
            {
                DataRow src = machines.Rows[i];
                if (src.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                decimal v = ((src["Min"] == DBNull.Value) ? 0m : Math.Abs(Convert.ToDecimal(src["Min"])));
                if (v <= 0m)
                {
                    continue;
                }
                ItemEditData d = ItemAt(L.Rows[i]);
                if (d != null && d.Meters != null)
                {
                    DataRow m = NewTermMeter(d, "COMMIT");
                    if (m != null)
                    {
                        SetIfCol(m, "CommitScope", "S");
                        SetIfCol(m, "WaiveScope", countOnly);
                        m["MinimumCharges"] = v;
                    }
                }
            }
            _changed = true;
        }

        private static void WriteWaiveTerms(DataRow m, LineTerms_Form f, int firstN)
        {
            if (m != null)
            {
                SetIfCol(m, "WaiveFirstNMonths", firstN);
                SetIfCol(m, "WaivePartialThreshold", f.WaivePartialAt);
                SetIfCol(m, "WaivePartialAmount", f.WaivePartialOff);
                SetIfCol(m, "WaiveScope", f.WaiveCount);
            }
        }

        private void WriteWaivePerMachine(Line L, DataTable machines, LineTerms_Form f)
        {
            ClearTermMeters(L, "WAIVE");
            for (int i = 0; i < L.Rows.Count && i < machines.Rows.Count; i++)
            {
                DataRow src = machines.Rows[i];
                if (src.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                decimal amt = ((src["Amount"] == DBNull.Value) ? 0m : Math.Abs(Convert.ToDecimal(src["Amount"])));
                decimal at = ((src["At"] == DBNull.Value) ? 0m : Math.Abs(Convert.ToDecimal(src["At"])));
                if (amt <= 0m)
                {
                    continue;
                }
                ItemEditData d = ItemAt(L.Rows[i]);
                if (d != null && d.Meters != null)
                {
                    DataRow m = NewTermMeter(d, "WAIVE");
                    if (m != null)
                    {
                        SetIfCol(m, "CommitScope", "S");
                        SetIfCol(m, "WaiveTargetAmount", at);
                        SetIfCol(m, "ChargesRate", 0m);
                        m["MinimumCharges"] = -amt;
                        int freeN = ((src["FreeMonths"] != DBNull.Value) ? Convert.ToInt32(src["FreeMonths"]) : 0);
                        WriteWaiveTerms(m, f, freeN);
                    }
                }
            }
            _changed = true;
        }

        private decimal SumTermMeters(Line L, string role)
        {
            decimal sum = default(decimal);
            foreach (int i in L.Rows)
            {
                ItemEditData d = ItemAt(i);
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState != DataRowState.Deleted && !(Str(mr, "MeterRole").Trim().ToUpperInvariant() != role))
                    {
                        sum += Math.Abs(Dec(mr, "MinimumCharges"));
                    }
                }
            }
            return sum;
        }

        private bool IsGroupScoped(Line L, string role)
        {
            DataRow m = FindTermMeter(L, role);
            return m != null && Str(m, "CommitScope").Trim().ToUpperInvariant() == "G";
        }

        private void ReadTerms(Line L, out decimal min, out decimal at, out decimal amt)
        {
            min = default(decimal);
            at = default(decimal);
            amt = default(decimal);
            DataRow c = FindTermMeter(L, "COMMIT");
            if (c != null)
            {
                min = Dec(c, "MinimumCharges");
            }
            DataRow w = FindTermMeter(L, "WAIVE");
            if (w != null)
            {
                at = Dec(w, "WaiveTargetAmount");
                amt = Math.Abs(Dec(w, "MinimumCharges"));
                if (amt <= 0m)
                {
                    amt = Math.Abs(Dec(w, "ChargesRate"));
                }
            }
        }

        private Line FocusedLine()
        {
            DataRow r = GridViewLines.GetDataRow(GridViewLines.FocusedRowHandle);
            if (r == null)
            {
                return null;
            }
            string side = Convert.ToString(r["Side"]);
            string grp = Convert.ToString(r["GroupCode"]).Trim();
            int solo = r["SoloIdx"] == DBNull.Value ? -1 : Convert.ToInt32(r["SoloIdx"]);
            foreach (Line L in Collect(side))
            {
                if ((grp.Length > 0) ? string.Equals(L.GroupCode, grp, StringComparison.OrdinalIgnoreCase) : (L.Rows.Count == 1 && L.Rows[0] == solo))
                {
                    return L;
                }
            }
            return null;
        }

        private string DescribeLadder(string stored, Line L, string field)
        {
            string t = (stored ?? "").Trim();
            if (t.Length == 0)
            {
                int mine = ((L != null) ? MachinesWithOwnLadder(L, field).Count : 0);
                return (mine == 0) ? "" : ("not priced together, " + mine + " machine" + ((mine == 1) ? "" : "s") + " priced on its own");
            }
            string words = ((t.IndexOf('|') < 0) ? t : ScpMultiPrice.Describe(ScpMultiPrice.ParseTiers(t)));
            if (L == null)
            {
                return words;
            }
            int own = MachinesWithOwnLadder(L, field).Count;
            if (own == 0)
            {
                return words;
            }
            return (own >= L.Rows.Count) ? (words + "  (not in use - every machine is priced on its own)") : (words + "  (" + own + " of " + L.Rows.Count + " machines priced on their own)");
        }

        private void RepoTiers_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            BtnTiers_Click(sender, EventArgs.Empty);
        }

        private void BtnTiers_Click(object sender, EventArgs e)
        {
            DataRow row = GridViewLines.GetDataRow(GridViewLines.FocusedRowHandle);
            if (row == null)
            {
                return;
            }
            string field = Convert.ToString(row["Field"]);
            if (field == "OwnRate" || field == "WAIVE" || field == "COMMIT")
            {
                XtraMessageBox.Show("Tier pricing is for copies. A rental is one figure a month — there is nothing to band." + Environment.NewLine + "Set it on the Black or Colour row of this group.", "Tier pricing", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            Line L = FocusedLine();
            if (L == null)
            {
                return;
            }
            if (L.Rows.Count == 1)
            {
                SetMachineLadder(L, field, row);
                return;
            }
            List<string> ownLadders = MachinesWithOwnLadder(L, field);
            if (ownLadders.Count > 0 && XtraMessageBox.Show("These machines carry tiers of their own, so they keep their own line and stay out of this group's total:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, ownLadders.ToArray()) + Environment.NewLine + Environment.NewLine + "Set the group's tiers anyway?", "Tier pricing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            string current = Convert.ToString(row["LadderText"]).Trim();
            string code = ((current.IndexOf('|') < 0) ? current : "");
            string csv = ((current.IndexOf('|') < 0) ? "" : current);
            using (MultiPricePicker_Form dlg = new MultiPricePicker_Form(Db, code, csv, 0m))
            {
                dlg.Text = "Tier pricing for " + L.Name + "  (" + L.Rows.Count + ((L.Rows.Count == 1) ? " machine)" : " machines)");
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string chosen = (dlg.CustomCsv ?? "").Trim();
                    if (chosen.Length == 0)
                    {
                        chosen = (dlg.SelectedCode ?? "").Trim();
                    }
                    row["LadderText"] = chosen;
                    row["Tiers"] = DescribeLadder(chosen, L, field);
                    if (chosen.Length > 0)
                    {
                        row["UnitPrice"] = 0m;
                    }
                    SyncLineRows(row, "LadderText");
                    SyncLineRows(row, "Tiers");
                    SyncLineRows(row, "UnitPrice");
                    _changed = true;
                    GridViewLines.RefreshData();
                }
            }
        }

        private string TierWordsFor(ItemEditData d, string role)
        {
            DataRow m = MeterOf(d, role);
            if (m == null)
            {
                return "";
            }
            string csv = Str(m, "CustomTiers").Trim();
            if (csv.Length > 0)
            {
                return ScpMultiPrice.Describe(ScpMultiPrice.ParseTiers(csv));
            }
            string code = Str(m, "MeterMultiPriceCode").Trim();
            if (code.Length > 0)
            {
                return code;
            }
            string grp = (d.MergeGroupCodeMeter ?? "").Trim();
            if (grp.Length == 0)
            {
                return "";
            }
            ScpLineTerms t;
            if (!_terms.TryGetValue(ScpLineTerms.Key("M", grp), out t) || t == null)
            {
                return "";
            }
            string stored = (((role == "CL") ? t.LadderCl : t.LadderBk) ?? "").Trim();
            if (stored.Length == 0)
            {
                return "";
            }
            return "same as the line";
        }

        private void SetMachineLadder(Line L, string field, DataRow row)
        {
            string role = ((field == "OwnBk") ? "BK" : "CL");
            ItemEditData d = ItemAt(L.Rows[0]);
            DataRow m = MeterOf(d, role);
            if (m == null)
            {
                XtraMessageBox.Show("This machine has no " + ((role == "BK") ? "black" : "colour") + " meter, so there are no copies to price by volume.", "Tier pricing", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string code = Str(m, "MeterMultiPriceCode").Trim();
            string csv = Str(m, "CustomTiers").Trim();
            using (MultiPricePicker_Form dlg = new MultiPricePicker_Form(Db, code, csv, 0m))
            {
                dlg.Text = "Tier pricing for " + d.ServiceItemNo;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetIfCol(m, "MeterMultiPriceCode", dlg.SelectedCode ?? "");
                    SetIfCol(m, "CustomTiers", dlg.CustomCsv ?? "");
                    if ((dlg.SelectedCode ?? "").Trim().Length > 0 || (dlg.CustomCsv ?? "").Trim().Length > 0)
                    {
                        SetIfCol(m, "ChargesRate", 0m);
                    }
                    _changed = true;
                    RebuildLines();
                }
            }
        }

        private static DataRow MeterOf(ItemEditData d, string role)
        {
            if (d == null || d.Meters == null)
            {
                return null;
            }
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                string r = Str(mr, "MeterRole").Trim().ToUpperInvariant();
                string t = Str(mr, "MeterTypeCode").Trim().ToUpperInvariant();
                bool num;
                if (!(role == "RENTAL"))
                {
                    if (r == role)
                    {
                        goto IL_00c1;
                    }
                    if (r.Length != 0)
                    {
                        continue;
                    }
                    num = t == role;
                }
                else
                {
                    num = ScpStrategy.IsRentalRole(r, t);
                }
                if (!num)
                {
                    continue;
                }
                goto IL_00c1;
                IL_00c1:
                return mr;
            }
            return null;
        }

        private string MachineLadderWords(Line L, string field)
        {
            DataRow m = MeterOf(ItemAt(L.Rows[0]), (field == "OwnBk") ? "BK" : "CL");
            if (m == null)
            {
                return "";
            }
            string csv = Str(m, "CustomTiers").Trim();
            if (csv.Length > 0)
            {
                return ScpMultiPrice.Describe(ScpMultiPrice.ParseTiers(csv)) + " (this machine)";
            }
            return Str(m, "MeterMultiPriceCode").Trim();
        }

        private List<string> MachinesWithOwnLadder(Line L, string field)
        {
            List<string> names = new List<string>();
            string role = ((field == "OwnBk") ? "BK" : "CL");
            foreach (int i in L.Rows)
            {
                ItemEditData d = ItemAt(i);
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState != DataRowState.Deleted && !(Str(mr, "MeterRole").Trim().ToUpperInvariant() != role) && (Str(mr, "MeterMultiPriceCode").Trim().Length > 0 || Str(mr, "CustomTiers").Trim().Length > 0))
                    {
                        names.Add("   " + d.ServiceItemNo + "  " + Str(mr, "MeterMultiPriceCode").Trim());
                    }
                }
            }
            return names;
        }

        private void RepoTerms_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            BtnTerms_Click(sender, EventArgs.Empty);
        }

        private void BtnTerms_Click(object sender, EventArgs e)
        {
            Line L = FocusedLine();
            if (L == null)
            {
                XtraMessageBox.Show("Click the row you want to set terms for first.", "Billing Setup", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            decimal min;
            decimal at;
            decimal amt;
            ReadTerms(L, out min, out at, out amt);
            int firstN = 0;
            decimal partAt = default(decimal);
            decimal partOff = default(decimal);
            string countOnly = "BKCL";
            DataRow wm = FindTermMeter(L, "WAIVE");
            if (wm != null)
            {
                firstN = Int(wm, "WaiveFirstNMonths");
                partAt = Dec(wm, "WaivePartialThreshold");
                partOff = Dec(wm, "WaivePartialAmount");
                string sc = Str(wm, "WaiveScope").Trim().ToUpperInvariant();
                countOnly = ((sc == "BK" || sc == "CL") ? sc : "BKCL");
            }
            // Which copies this line's minimum counts, off the COMMIT meter that carries it.
            string minCountOnly = "BKCL";
            DataRow cmm = FindTermMeter(L, "COMMIT");
            if (cmm != null)
            {
                string ms = Str(cmm, "WaiveScope").Trim().ToUpperInvariant();
                minCountOnly = ((ms == "BK" || ms == "CL") ? ms : "BKCL");
            }
            bool rentalSide = L.Side == "R";
            bool oneInvoice = WaiveFits;
            List<LineTerms_Form.MachineMin> onLine = new List<LineTerms_Form.MachineMin>();
            foreach (int i in L.Rows)
            {
                DataRow mr = _dtMachines.Rows[i];
                LineTerms_Form.MachineMin mm = new LineTerms_Form.MachineMin();
                mm.Name = Convert.ToString(mr["ServiceItemNo"]);
                mm.Model = Convert.ToString(mr["ItemCode"]);
                mm.Min = MinOfMachine(i);
                decimal wAt;
                decimal wAmt;
                int wFree;
                WaiveOfMachine(i, out wAt, out wAmt, out wFree);
                mm.WaiveAt = wAt;
                mm.WaiveAmount = wAmt;
                mm.WaiveFreeMonths = wFree;
                onLine.Add(mm);
            }
            bool perMachine = !rentalSide && L.Rows.Count > 1 && CountTermMeters(L, "COMMIT") > 0 && !IsGroupScoped(L, "COMMIT");
            bool waivePerMachine = rentalSide && L.Rows.Count > 1 && CountTermMeters(L, "WAIVE") > 0 && !IsGroupScoped(L, "WAIVE");
            using (LineTerms_Form f = new LineTerms_Form(L.Name, L.Rows.Count, min, at, amt, !rentalSide, rentalSide, (!rentalSide || oneInvoice) ? "" : "The rental is on an invoice of its own, so a waive cannot be judged by what the copies came to -- they are on the other paper. Free months still work: they read a calendar, not a meter.", perMachine, onLine, waivePerMachine, firstN, partAt, partOff, countOnly, rentalSide && oneInvoice, RentalOfLine(L), minCountOnly))
            {
                if (f.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                if (!rentalSide)
                {
                    if (f.MinPerMachine)
                    {
                        WriteMinPerMachine(L, f.Machines, f.MinCount);
                    }
                    else
                    {
                        if (f.MinScope == "C")
                        {
                            ClearAllMinimums();
                        }
                        else
                        {
                            ClearTermMeters(L, "COMMIT");
                        }
                        WriteTermFor(L, "COMMIT", f.MinCharge, 0m);
                        DataRow w = FindTermMeter(L, "COMMIT");
                        if (w != null)
                        {
                            SetIfCol(w, "CommitScope", f.MinScope);
                            SetIfCol(w, "WaiveScope", f.MinCount);
                        }
                    }
                }
                else if (f.WaiveScopeMode == "S")
                {
                    WriteWaivePerMachine(L, f.WaiveMachines, f);
                }
                else
                {
                    ClearTermMeters(L, "WAIVE");
                    WriteTermFor(L, "WAIVE", f.WaiveAmount, f.WaiveAt);
                    WriteWaiveTerms(FindTermMeter(L, "WAIVE"), f, f.WaiveFirstN);
                }
                _changed = true;
                RebuildLines();
            }
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
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState == DataRowState.Deleted || !(Str(mr, "MeterRole").Trim().ToUpperInvariant() == role))
                    {
                        continue;
                    }
                    return mr;
                }
            }
            return null;
        }

        private ItemEditData ItemAt(int machineRow)
        {
            int idx = Convert.ToInt32(_dtMachines.Rows[machineRow]["Idx"]);
            return (idx >= 0 && idx < _items.Count) ? _items[idx] : null;
        }

        private string NameWithMachines(Line L)
        {
            return L.Name;
        }

        private static string LineKeyOf(Line L)
        {
            return L.Side + "|" + ((L.GroupCode.Length > 0) ? ("#" + L.GroupCode.ToUpperInvariant()) : ("=" + (L.Rows.Count > 0 ? L.Rows[0] : -1)));
        }

        private void FillLineMachines(string key, List<int> rows)
        {
            if (_dtLineMachines.Select("LineKey = '" + key.Replace("'", "''") + "'").Length != 0)
            {
                return;
            }
            foreach (int i in rows)
            {
                DataRow m = _dtMachines.Rows[i];
                DataRow r = _dtLineMachines.NewRow();
                r["LineKey"] = key;
                r["ServiceItemNo"] = m["ServiceItemNo"];
                r["SerialNumber"] = m["SerialNumber"];
                r["ItemCode"] = m["ItemCode"];
                r["OwnRate"] = m["OwnRate"];
                r["OwnBk"] = m["OwnBk"];
                r["OwnCl"] = m["OwnCl"];
                _dtLineMachines.Rows.Add(r);
            }
        }

        private decimal SumOf(Line L, string field)
        {
            decimal sum = default(decimal);
            foreach (int i in L.Rows)
            {
                sum += Convert.ToDecimal(_dtMachines.Rows[i][field]);
            }
            return sum;
        }

        private List<DataRow> Ticked()
        {
            List<DataRow> picked = new List<DataRow>();
            int[] rows = GridViewMachines.GetSelectedRows();
            for (int i = 0; i < rows.Length; i++)
            {
                DataRow r = GridViewMachines.GetDataRow(rows[i]);
                if (r != null)
                {
                    picked.Add(r);
                }
            }
            return picked;
        }

        private void ClearTicks()
        {
            GridViewMachines.ClearSelection();
        }

        private void MergeTicked(bool rental, bool meter, string what)
        {
            GridViewMachines.PostEditor();
            List<DataRow> picked = Ticked();
            if (picked.Count < 2)
            {
                XtraMessageBox.Show("Tick at least two machines to put them on one line.", "Billing Setup", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            List<string> groups = new List<string>();
            foreach (DataRow r0 in picked)
            {
                string bg = Convert.ToString(r0["BillGroup"]).Trim();
                if (!groups.Contains(bg))
                {
                    groups.Add(bg);
                }
            }
            if (groups.Count > 1)
            {
                List<string> named = new List<string>();
                foreach (string g in groups)
                {
                    named.Add("   " + ((g.Length > 0) ? g : "(no bill group)"));
                }
                XtraMessageBox.Show("These machines are billed on different invoices:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, named.ToArray()) + Environment.NewLine + Environment.NewLine + "A printed line lives on one invoice, so they cannot share one. Merge the machines of each bill group separately, or change their bill group first.", "Machines", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            bool same = true;
            string model0 = Convert.ToString(picked[0]["ItemCode"]);
            foreach (DataRow r1 in picked)
            {
                if (!string.Equals(Convert.ToString(r1["ItemCode"]), model0, StringComparison.OrdinalIgnoreCase))
                {
                    same = false;
                }
            }
            string suggest = (same ? model0 : ("GROUP " + (_seq + 1)));
            string name = XtraInputBox.Show("Give these merged machines a name", "Billing Setup", suggest);
            if (name == null)
            {
                return;
            }
            name = ScpRentalGroupPrice.Sanitize(name);
            if (name.Length == 0)
            {
                return;
            }
            if (!same)
            {
                _seq++;
            }
            foreach (DataRow r2 in picked)
            {
                if (rental)
                {
                    r2["MergeGroup"] = name;
                    r2["PrintsOn"] = name;
                }
                if (meter)
                {
                    r2["MeterGroup"] = name;
                }
            }
            _changed = true;
            RebuildLines();
        }

        private void RgSplit_Changed(object sender, EventArgs e)
        {
            string want = ((RgSplit.EditValue == null) ? "ONE" : Convert.ToString(RgSplit.EditValue));
            // Only the splits that put the rental on an invoice of its own break a waive -- the same test
            // as WaiveFits. "One invoice per machine" keeps each machine's rental and copies together, so
            // it used to take the waive away for nothing.
            bool rentalApart = want == "RS" || want == "PMS";
            if (rentalApart && CountWaives() > 0)
            {
                if (XtraMessageBox.Show("A rental waive only works when the rental and its copies are on the same invoice — it takes money off the rental because of what the copies came to." + Environment.NewLine + Environment.NewLine + "Sending the rental on an invoice of its own removes the waive on this contract. Continue?", "Rental waive", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                {
                    _suppressSplit = true;
                    try
                    {
                        RgSplit.EditValue = ResultSplit;
                        return;
                    }
                    finally
                    {
                        _suppressSplit = false;
                    }
                }
                DropWaives();
            }
            if (!_suppressSplit)
            {
                ResultSplit = want;
                _changed = true;
                RebuildLines();
            }
        }

        private int CountWaives()
        {
            int n = 0;
            foreach (ItemEditData d in _items)
            {
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState != DataRowState.Deleted && Str(mr, "MeterRole").Trim().ToUpperInvariant() == "WAIVE")
                    {
                        n++;
                    }
                }
            }
            return n;
        }

        private void DropWaives()
        {
            foreach (ItemEditData d in _items)
            {
                if (d == null || d.Meters == null)
                {
                    continue;
                }
                for (int i = d.Meters.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow mr = d.Meters.Rows[i];
                    if (mr.RowState != DataRowState.Deleted && Str(mr, "MeterRole").Trim().ToUpperInvariant() == "WAIVE")
                    {
                        mr.Delete();
                    }
                }
            }
        }

        // ---------- the options band ----------

        private bool _optionsShown = true;

        /// <summary>Every button above the lists and the invoice options, shown or folded away. Folded, only
        /// the toggle stays (at the left) and the invoice list moves up into their place; the choice is
        /// remembered for this user.</summary>
        private void SetOptionsShown(bool shown)
        {
            if (shown == _optionsShown) return;
            int top = GrpTicked.Top;
            int gap = (GrpNames.Bottom > GrpAll.Bottom ? GrpNames.Bottom : GrpAll.Bottom) + 1 - top;
            GrpTicked.Visible = shown;
            GrpAll.Visible = shown;
            GrpSplit.Visible = shown;
            GrpNames.Visible = shown;
            BtnSample.Visible = shown;
            BtnToggleOptions.Left = shown ? BtnSample.Right + 8 : BtnSample.Left;
            if (shown)
            {
                GridLines.Top += gap;
                GridLines.Height -= gap;
            }
            else
            {
                GridLines.Top -= gap;
                GridLines.Height += gap;
            }
            _optionsShown = shown;
            BtnToggleOptions.Text = shown ? "Hide options" : "Show options";
        }

        private void BtnToggleOptions_Click(object sender, EventArgs e)
        {
            SetOptionsShown(!_optionsShown);
            try { ServiceContractPhotocopier.Data.PumsConfig.Set(Db, OptionsKey(), _optionsShown ? "N" : "Y"); } catch { }
        }

        private bool OptionsHiddenSaved()
        {
            try { return Db != null && ServiceContractPhotocopier.Data.PumsConfig.Get(Db, OptionsKey(), "N") == "Y"; }
            catch { return false; }
        }

        private static string OptionsKey()
        {
            string user = "";
            try { user = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? ""; } catch { }
            return "PRICING_OPTIONS_HIDDEN_" + (user.Trim().Length > 0 ? user.Trim().ToUpperInvariant() : "ADMIN");
        }

        private void BtnSample_Click(object sender, EventArgs e)
        {
            if (ShowSample != null)
            {
                Harvest();
                ResultShowModel = ChkShowModel.Checked;
                ResultShowSerial = ChkShowSerial.Checked;
                ResultShowUnits = ChkShowUnits.Checked;
                ShowSample();
            }
        }

        private void GridViewLines_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            GridViewMachines.RefreshData();
        }

        private void GridViewMachines_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            DataRow line = GridViewLines.GetDataRow(GridViewLines.FocusedRowHandle);
            if (line == null)
            {
                return;
            }
            DataRow mach = GridViewMachines.GetDataRow(e.RowHandle);
            if (mach == null)
            {
                return;
            }
            bool num = MachinesOfRow(line).Contains(_dtMachines.Rows.IndexOf(mach));
            if (num && e.RowHandle != GridViewMachines.FocusedRowHandle)
            {
                e.Appearance.BackColor = Color.FromArgb(232, 240, 251);
                e.Appearance.BackColor2 = Color.FromArgb(232, 240, 251);
                e.Appearance.ForeColor = Color.FromArgb(27, 31, 38);
            }
        }

        private void GridViewMachines_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (_filling || e.Column == null)
            {
                return;
            }
            string fld = e.Column.FieldName;
            if (fld != "OwnRate" && fld != "OwnBk" && fld != "OwnCl")
            {
                return;
            }
            _changed = true;
            DataRow edited = GridViewMachines.GetDataRow(e.RowHandle);
            if (edited == null)
            {
                RebuildLines();
                return;
            }
            if (edited["Sel"] == DBNull.Value || !Convert.ToBoolean(edited["Sel"]))
            {
                RebuildLines();
                return;
            }
            string hasField = ((fld == "OwnRate") ? "HasRental" : ((fld == "OwnBk") ? "HasBk" : "HasCl"));
            string word = ((fld == "OwnRate") ? "rent" : ((fld == "OwnBk") ? "charge for black" : "charge for colour"));
            decimal v = ((e.Value == null || e.Value == DBNull.Value) ? 0m : Convert.ToDecimal(e.Value));
            _filling = true;
            int done = 1;
            int skipped = 0;
            try
            {
                foreach (DataRow r in _dtMachines.Rows)
                {
                    if (r.RowState != DataRowState.Deleted && r != edited && r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                    {
                        if (r[hasField] == DBNull.Value || !Convert.ToBoolean(r[hasField]))
                        {
                            skipped++;
                            continue;
                        }
                        r[fld] = v;
                        done++;
                    }
                }
            }
            finally
            {
                _filling = false;
            }
            GridViewMachines.RefreshData();
            RebuildLines();
            if (done > 1 || skipped > 0)
            {
                LblSummary.Text = done + " ticked machine" + ((done == 1) ? "" : "s") + " now " + word + " " + v.ToString("n4").TrimEnd('0').TrimEnd('.') + ((skipped > 0) ? ("   ·   " + skipped + " skipped (no such counter)") : "");
            }
        }

        private void BtnSameModel_Click(object sender, EventArgs e)
        {
            DataRow f = GridViewMachines.GetDataRow(GridViewMachines.FocusedRowHandle);
            if (f == null)
            {
                return;
            }
            string model = Convert.ToString(f["ItemCode"]).Trim();
            int n = 0;
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState != DataRowState.Deleted && string.Equals(Convert.ToString(r["ItemCode"]).Trim(), model, StringComparison.OrdinalIgnoreCase))
                {
                    r["Sel"] = true;
                    n++;
                }
            }
            GridViewMachines.RefreshData();
            LblSummary.Text = n + " machine" + ((n == 1) ? "" : "s") + " of " + ((model.Length > 0) ? model : "this model") + " ticked";
        }

        private void BtnBkTiers_Click(object sender, EventArgs e)
        {
            TickedTiers("BK", "black");
        }

        private void BtnClTiers_Click(object sender, EventArgs e)
        {
            TickedTiers("CL", "colour");
        }

        private void TickedTiers(string role, string word)
        {
            GridViewMachines.PostEditor();
            List<DataRow> picked = Ticked();
            if (picked.Count == 0)
            {
                XtraMessageBox.Show("Tick the machines you want to price by volume first.", "Tier pricing", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string code = "";
            string csv = "";
            int withMeter = 0;
            int differ = 0;
            foreach (DataRow r in picked)
            {
                DataRow m = MeterOf(ItemAt(Convert.ToInt32(r["Idx"])), role);
                if (m != null)
                {
                    string c = Str(m, "MeterMultiPriceCode").Trim();
                    string v = Str(m, "CustomTiers").Trim();
                    if (withMeter == 0)
                    {
                        code = c;
                        csv = v;
                    }
                    else if (c != code || v != csv)
                    {
                        differ++;
                    }
                    withMeter++;
                }
            }
            if (withMeter == 0)
            {
                XtraMessageBox.Show("None of the ticked machines has a " + word + " meter, so there are no copies to price.", "Tier pricing", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            using (MultiPricePicker_Form dlg = new MultiPricePicker_Form(Db, code, csv, 0m))
            {
                dlg.Text = "Tier pricing for " + withMeter + " machine" + ((withMeter == 1) ? "" : "s") + "  (" + word + ")" + ((differ > 0) ? "  — they do not all agree today" : "");
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                string chosen = (dlg.CustomCsv ?? "").Trim();
                string chosenCode = (dlg.SelectedCode ?? "").Trim();
                foreach (DataRow r2 in picked)
                {
                    DataRow m2 = MeterOf(ItemAt(Convert.ToInt32(r2["Idx"])), role);
                    if (m2 != null)
                    {
                        SetIfCol(m2, "MeterMultiPriceCode", chosenCode);
                        SetIfCol(m2, "CustomTiers", chosen);
                        if (chosenCode.Length > 0 || chosen.Length > 0)
                        {
                            SetIfCol(m2, "ChargesRate", 0m);
                        }
                    }
                }
                _changed = true;
                BuildMachines();
                RebuildLines();
            }
        }

        private void BtnMerge_Click(object sender, EventArgs e)
        {
            MergeTicked(true, false, "rental");
        }

        private void BtnMergeMeter_Click(object sender, EventArgs e)
        {
            MergeTicked(false, true, "BK+CL");
        }

        private void BtnMergeBoth_Click(object sender, EventArgs e)
        {
            MergeTicked(true, true, "rental and BK+CL");
        }

        private void BtnUngroup_Click(object sender, EventArgs e)
        {
            GridViewMachines.PostEditor();
            List<DataRow> picked = Ticked();
            if (picked.Count == 0)
            {
                return;
            }
            foreach (DataRow r in picked)
            {
                r["MergeGroup"] = "";
                r["MeterGroup"] = "";
                r["PrintsOn"] = Convert.ToString(r["ServiceItemNo"]);
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
                r["MeterGroup"] = "COPIES " + Convert.ToDecimal(r["OwnBk"]).ToString("n4") + " / " + Convert.ToDecimal(r["OwnCl"]).ToString("n4");
                r["PrintsOn"] = Convert.ToString(r["MergeGroup"]);
            }
            _changed = true;
            RebuildLines();
        }

        private void BtnUngroupAll_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtMachines.Rows)
            {
                r["MergeGroup"] = "";
                r["MeterGroup"] = "";
                r["PrintsOn"] = Convert.ToString(r["ServiceItemNo"]);
            }
            _changed = true;
            RebuildLines();
        }

        private void BtnByModel_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in _dtMachines.Rows)
            {
                string m = (string)(r["MeterGroup"] = (r["MergeGroup"] = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["ItemCode"]))));
                r["PrintsOn"] = m;
            }
            _changed = true;
            RebuildLines();
        }

        private void GridViewLines_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column == ColUnitPrice && e.RowHandle != GridViewLines.FocusedRowHandle)
            {
                DataRow r = GridViewLines.GetDataRow(e.RowHandle);
                if (r != null && Convert.ToString(r["OnMachines"]).IndexOf("prices ->", StringComparison.Ordinal) >= 0 && (r["UnitPrice"] == DBNull.Value || !(Convert.ToDecimal(r["UnitPrice"]) > 0m)))
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 236, 179);
                    e.Appearance.ForeColor = Color.FromArgb(120, 70, 0);
                }
            }
        }

        private void GridViewLines_CustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            if (e.Column == ColInvoice)
            {
                string whoA;
                int rankA;
                SplitInvoiceName(Convert.ToString(e.Value1), out whoA, out rankA);
                string whoB;
                int rankB;
                SplitInvoiceName(Convert.ToString(e.Value2), out whoB, out rankB);
                int c = string.Compare(whoA, whoB, StringComparison.OrdinalIgnoreCase);
                e.Result = ((c != 0) ? c : rankA.CompareTo(rankB));
                e.Handled = true;
            }
        }

        private static void SplitInvoiceName(string name, out string who, out int rank)
        {
            string t = (who = (name ?? "").Trim());
            rank = 2;
            if (string.Equals(t, "Rental invoice", StringComparison.OrdinalIgnoreCase))
            {
                who = "";
                rank = 0;
            }
            else if (string.Equals(t, "Meter invoice", StringComparison.OrdinalIgnoreCase))
            {
                who = "";
                rank = 1;
            }
            else if (t.EndsWith(" - rental", StringComparison.OrdinalIgnoreCase))
            {
                who = t.Substring(0, t.Length - 9);
                rank = 0;
            }
            else if (t.EndsWith(" - meters", StringComparison.OrdinalIgnoreCase))
            {
                who = t.Substring(0, t.Length - 9);
                rank = 1;
            }
        }

        private bool FirstOfLine(int sourceIndex)
        {
            if (sourceIndex <= 0 || sourceIndex >= _dtLines.Rows.Count)
            {
                return true;
            }
            DataRow me = _dtLines.Rows[sourceIndex];
            DataRow above = _dtLines.Rows[sourceIndex - 1];
            return Convert.ToString(above["LineKey"]) != Convert.ToString(me["LineKey"]);
        }

        private bool ShowsMachines(int rowHandle)
        {
            DataRow r = GridViewLines.GetDataRow(rowHandle);
            if (r == null)
            {
                return false;
            }
            if (r["Units"] == DBNull.Value || Convert.ToInt32(r["Units"]) < 2)
            {
                return false;
            }
            int above = GridViewLines.GetVisibleRowHandle(GridViewLines.GetVisibleIndex(rowHandle) - 1);
            if (above < 0)
            {
                return true;
            }
            DataRow prev = GridViewLines.GetDataRow(above);
            return prev == null || Convert.ToString(prev["LineKey"]) != Convert.ToString(r["LineKey"]);
        }

        private void GridViewLines_MasterRowEmpty(object sender, MasterRowEmptyEventArgs e)
        {
            e.IsEmpty = !ShowsMachines(e.RowHandle);
        }

        private void GridViewLines_DrawPad(object sender, RowCellCustomDrawEventArgs e)
        {
            if (e.Column == ColPad && !ShowsMachines(e.RowHandle))
            {
                e.Appearance.FillRectangle(e.Cache, e.Bounds);
                e.Handled = true;
            }
        }

        private void GridViewLines_ShowingEditor(object sender, CancelEventArgs e)
        {
            DataRow r = GridViewLines.GetDataRow(GridViewLines.FocusedRowHandle);
            if (r == null)
            {
                e.Cancel = true;
            }
        }

        private void GridViewLines_DisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            decimal v;
            if (e.Column == ColTerms)
            {
                if (e.Value == null || Convert.ToString(e.Value).Trim().Length <= 0)
                {
                    e.DisplayText = (FirstOfLine(e.ListSourceRowIndex) ? "set a minimum or a waive…" : "");
                }
            }
            else if (e.Column == ColUnitPrice && (e.Value == null || e.Value == DBNull.Value || !decimal.TryParse(Convert.ToString(e.Value), out v) || v <= 0m))
            {
                DataRow lr = ((e.ListSourceRowIndex >= 0 && e.ListSourceRowIndex < _dtLines.Rows.Count) ? _dtLines.Rows[e.ListSourceRowIndex] : null);
                bool mixed = lr != null && Convert.ToString(lr["OnMachines"]).IndexOf("prices ->", StringComparison.Ordinal) >= 0;
                e.DisplayText = (mixed ? "needed to merge" : "same as now");
            }
        }

        private void GridViewLines_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column == ColLineLbl)
            {
                DataRow lr = GridViewLines.GetDataRow(e.RowHandle);
                if (lr == null)
                {
                    return;
                }
                _changed = true;
                string typed = Convert.ToString((lr["LineLabel"] == DBNull.Value) ? "" : lr["LineLabel"]).Trim();
                if (typed.Length > 60)
                {
                    typed = typed.Substring(0, 60);
                }
                string mine = Convert.ToString((lr["MachineIdx"] == DBNull.Value) ? "" : lr["MachineIdx"]);
                SetLabelFor(mine, typed);
                {
                    foreach (DataRow other in _dtLines.Rows)
                    {
                        if (other != lr && string.Equals(Convert.ToString(other["MachineIdx"]), mine, StringComparison.Ordinal))
                        {
                            other["LineLabel"] = typed;
                        }
                    }
                    return;
                }
            }
            if (e.Column == ColUnitPrice)
            {
                _changed = true;
                GridViewLines.PostEditor();
                SyncLineRows(GridViewLines.GetDataRow(e.RowHandle), "UnitPrice");
                RecomputeMonthly();
                RefreshSummary();
            }
        }

        private void WriteTermFor(Line L, string role, decimal amount, decimal target)
        {
            if (L == null || L.Rows.Count == 0)
            {
                return;
            }
            DataRow existing = FindTermMeter(L, role);
            if (amount <= 0m && target <= 0m)
            {
                if (existing != null)
                {
                    existing.Delete();
                }
                return;
            }
            if (existing == null)
            {
                existing = NewTermMeter(ItemAt(L.Rows[0]), role);
                if (existing == null)
                {
                    return;
                }
            }
            SetIfCol(existing, "CommitScope", (L.GroupCode.Length > 0) ? "G" : "S");
            if (role == "WAIVE")
            {
                SetIfCol(existing, "WaiveScope", "BKCL");
            }
            existing["MinimumCharges"] = ((role == "WAIVE") ? (-amount) : amount);
            if (role == "WAIVE")
            {
                SetIfCol(existing, "ChargesRate", 0m);
                SetIfCol(existing, "WaiveTargetAmount", target);
            }
        }

        private DataRow NewTermMeter(ItemEditData d, string role)
        {
            if (d == null || d.Meters == null)
            {
                return null;
            }
            DataRow m = d.Meters.NewRow();
            m["MeterRole"] = role;
            m["MeterTypeCode"] = ((role == "WAIVE") ? "WAIVE" : "COMMIT");
            m["Description"] = ((role == "WAIVE") ? "RENTAL WAIVE" : "MINIMUM COMMITTED PRINT CHARGES");
            SetIfCol(m, "MachineSerialNo", "");
            SetIfCol(m, "ChargesRate", 0m);
            SetIfCol(m, "MinimumCharges", 0m);
            SetIfCol(m, "MeterMultiPriceCode", "");
            SetIfCol(m, "RebateQtyInPercent", 0m);
            SetIfCol(m, "FOCQty", 0m);
            SetIfCol(m, "InitialReading", 0m);
            SetIfCol(m, "CustomTiers", "");
            SetIfCol(m, "WaiveFirstNMonths", 0);
            SetIfCol(m, "WaiveTargetAmount", 0m);
            SetIfCol(m, "WaivePartialThreshold", 0m);
            SetIfCol(m, "WaivePartialAmount", 0m);
            SetIfCol(m, "WaiveScope", "BKCL");
            d.Meters.Rows.Add(m);
            return m;
        }

        private static void SetIfCol(DataRow r, string col, object value)
        {
            if (r.Table.Columns.Contains(col))
            {
                r[col] = value;
            }
        }

        private void RecomputeMonthly()
        {
            foreach (DataRow r in _dtLines.Rows)
            {
                if (Convert.ToString(r["Field"]) != "OwnRate")
                {
                    continue;
                }
                decimal agreed = ((r["UnitPrice"] == DBNull.Value) ? 0m : Convert.ToDecimal(r["UnitPrice"]));
                int units = Convert.ToInt32(r["Units"]);
                if (agreed > 0m)
                {
                    r["Monthly"] = (agreed * (decimal)units).ToString("n2");
                    continue;
                }
                decimal sum = default(decimal);
                foreach (int mi in MachinesOfRow(r))
                {
                    if (mi >= 0 && mi < _dtMachines.Rows.Count) sum += Convert.ToDecimal(_dtMachines.Rows[mi]["OwnRate"]);
                }
                r["Monthly"] = sum.ToString("n2") + "  own";
            }
        }

        private int InvoiceCount()
        {
            List<string> groups = new List<string>();
            bool ungrouped = false;
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                string bg = ScpStrategy.SanitizeBillGroup(Convert.ToString(r["BillGroup"]));
                if (bg.Length == 0)
                {
                    ungrouped = true;
                    continue;
                }
                bool seen = false;
                foreach (string g in groups)
                {
                    if (string.Equals(g, bg, StringComparison.OrdinalIgnoreCase))
                    {
                        seen = true;
                        break;
                    }
                }
                if (!seen)
                {
                    groups.Add(bg);
                }
            }
            int n = groups.Count + (ungrouped ? 1 : 0);
            return (n < 1) ? 1 : n;
        }

        private void RefreshSummary()
        {
            int rentalLines = 0;
            int meterLines = 0;
            int priced = 0;
            int split = 0;
            foreach (DataRow r in _dtLines.Rows)
            {
                if (Convert.ToString(r["Field"]) == "OwnRate")
                {
                    rentalLines++;
                }
                decimal p = ((r["UnitPrice"] == DBNull.Value) ? 0m : Convert.ToDecimal(r["UnitPrice"]));
                if (p > 0m)
                {
                    priced++;
                }
                else if (Convert.ToString(r["OnMachines"]).IndexOf("prices ->", StringComparison.Ordinal) >= 0)
                {
                    split++;
                }
            }
            foreach (DataRow r2 in _dtLines.Rows)
                if (Convert.ToString(r2["Field"]) != "OwnRate")
                    meterLines++;
            int invoicesPerSplit = InvoiceCount();
            int machines = _dtMachines.Rows.Count;
            string splitWords = ((ResultSplit == "PM") ? (machines + ((machines == 1) ? " invoice" : " invoices") + " - one per machine") : ((ResultSplit == "PMS") ? (machines * 2 + " invoices - two per machine, rental apart") : ((!(ResultSplit == "RS")) ? (invoicesPerSplit + ((invoicesPerSplit == 1) ? " invoice" : " invoices") + ((invoicesPerSplit > 1) ? " - one per bill group" : "")) : (invoicesPerSplit * 2 + " invoices - rental and meters apart" + ((invoicesPerSplit > 1) ? (", on " + invoicesPerSplit + " bill groups") : "")))));
            LblSummary.Text = splitWords + "  ·  " + rentalLines + ((rentalLines == 1) ? " rental row" : " rental rows") + "  ·  " + meterLines + " BK+CL " + ((meterLines == 1) ? "row" : "rows") + "  ·  " + _dtMachines.Rows.Count + " machines  ·  " + priced + " priced here" + ((split > 0) ? ("   ·   " + split + ((split == 1) ? " row" : " rows") + " still print one per price until a figure is agreed") : "   (the rest bill at the machine's own rate)");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewLines.PostEditor();
            GridViewLines.UpdateCurrentRow();
            ResultShowModel = ChkShowModel.Checked;
            ResultShowSerial = ChkShowSerial.Checked;
            ResultShowUnits = ChkShowUnits.Checked;
            List<string> unpriced = new List<string>();
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState != DataRowState.Deleted && Convert.ToString(r["OnMachines"]).IndexOf("prices ->", StringComparison.Ordinal) >= 0)
                {
                    decimal p = ((r["UnitPrice"] == DBNull.Value) ? 0m : Convert.ToDecimal(r["UnitPrice"]));
                    if (!(p > 0m))
                    {
                        unpriced.Add("   " + Convert.ToString(r["LineName"]) + "   —   " + Convert.ToString(r["Charge"]) + "   (" + Convert.ToString(r["OnMachines"]) + ")");
                    }
                }
            }
            if (unpriced.Count > 0)
            {
                XtraMessageBox.Show("These merged lines still have machines charging different money, and no price has been agreed for them:\r\n\r\n" + string.Join("\r\n", unpriced.ToArray()) + "\r\n\r\nThey will NOT print as one row — the invoice will show one row per price, exactly as if they were never merged.\r\n\r\nType the agreed figure in \"Use this price instead\", or un-merge them.", "Merged, but not priced", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                GridViewLines.Focus();
            }
            else if (HarvestMeters())
            {
                Harvest();
                base.DialogResult = DialogResult.OK;
                Close();
            }
        }

        private bool HarvestMeters()
        {
            GridViewMachines.PostEditor();
            GridViewMachines.UpdateCurrentRow();
            List<string> borrowed = BorrowedCountersBeingUnticked();
            if (borrowed.Count > 0)
            {
                XtraMessageBox.Show("These counters came from another account book and cannot be taken off:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, borrowed.ToArray()) + Environment.NewLine + Environment.NewLine + ScpInterBillGuard.RefusalText("", "Each of them") + Environment.NewLine + Environment.NewLine + "Nothing was applied.", "Machines", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return false;
            }
            int losing = 0;
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState != DataRowState.Deleted)
                {
                    ItemEditData d = ItemAt(Convert.ToInt32(r["Idx"]));
                    if (d != null)
                    {
                        losing += WouldLoseHistory(d, "RENTAL", Flag(r, "HasRental"));
                        losing += WouldLoseHistory(d, "BK", Flag(r, "HasBk"));
                        losing += WouldLoseHistory(d, "CL", Flag(r, "HasCl"));
                    }
                }
            }
            if (losing > 0 && XtraMessageBox.Show("This removes " + losing + " meter" + ((losing == 1) ? "" : "s") + "." + Environment.NewLine + Environment.NewLine + "When you save the contract, each one AND its reading history (all readings + billing log for that counter) are permanently deleted." + Environment.NewLine + Environment.NewLine + "Continue?", "Machines", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
            {
                return false;
            }
            bool typeMissing = false;
            foreach (DataRow r2 in _dtMachines.Rows)
            {
                if (r2.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                ItemEditData d2 = ItemAt(Convert.ToInt32(r2["Idx"]));
                if (d2 != null)
                {
                    if (!PutMeter(d2, "RENTAL", Flag(r2, "HasRental"), Dec2(r2["OwnRate"])))
                    {
                        typeMissing = true;
                    }
                    if (!PutMeter(d2, "BK", Flag(r2, "HasBk"), Dec2(r2["OwnBk"])))
                    {
                        typeMissing = true;
                    }
                    if (!PutMeter(d2, "CL", Flag(r2, "HasCl"), Dec2(r2["OwnCl"])))
                    {
                        typeMissing = true;
                    }
                }
            }
            if (typeMissing)
            {
                XtraMessageBox.Show("Some counters could not be created because their standard meter type is missing from this book." + Environment.NewLine + "Everything else was applied. Add the meter types and try those machines again.", "Machines", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return true;
        }

        private static bool Flag(DataRow r, string field)
        {
            return r[field] != DBNull.Value && Convert.ToBoolean(r[field]);
        }

        private List<string> BorrowedCountersBeingUnticked()
        {
            List<string> hit = new List<string>();
            if (Db == null)
            {
                return hit;
            }
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState == DataRowState.Deleted)
                {
                    continue;
                }
                ItemEditData d = ItemAt(Convert.ToInt32(r["Idx"]));
                if (d != null && d.ItemKey > 0)
                {
                    HashSet<string> owned = ScpInterBillGuard.OwnedMeterTags(Db, d.ItemKey);
                    if (owned.Count != 0)
                    {
                        CheckUntick(hit, owned, d, r, "RENTAL", "HasRental");
                        CheckUntick(hit, owned, d, r, "BK", "HasBk");
                        CheckUntick(hit, owned, d, r, "CL", "HasCl");
                    }
                }
            }
            return hit;
        }

        private static void CheckUntick(List<string> hit, HashSet<string> owned, ItemEditData d, DataRow r, string role, string field)
        {
            if (Flag(r, field))
            {
                return;
            }
            DataRow m = MeterOf(d, role);
            if (m != null)
            {
                string tag = ScpInterBillGuard.Tag(Convert.ToString(m["MeterTypeCode"]), m.Table.Columns.Contains("MachineSerialNo") ? Convert.ToString(m["MachineSerialNo"]) : "");
                if (owned.Contains(tag))
                {
                    hit.Add("   " + (string.IsNullOrEmpty(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo) + "   " + Convert.ToString(r["SerialNumber"]).Trim() + "   " + role);
                }
            }
        }

        private static decimal Dec2(object v)
        {
            return (v == null || v == DBNull.Value) ? 0m : Convert.ToDecimal(v);
        }

        private static int WouldLoseHistory(ItemEditData d, string role, bool want)
        {
            if (want)
            {
                return 0;
            }
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            return (m != null && m.RowState != DataRowState.Added) ? 1 : 0;
        }

        private bool PutMeter(ItemEditData d, string role, bool want, decimal rate)
        {
            if (d.Meters == null)
            {
                d.Meters = zSCP2_Item_Form.CreateMetersTable();
            }
            DataRow m = MeterOf(d, role);
            if (!want)
            {
                if (m != null)
                {
                    m.Delete();
                }
                return true;
            }
            bool ok = true;
            if (m == null)
            {
                ok = zSCP2_Item_Form.AddStandardMeter(Db, d.Meters, role);
                m = MeterOf(d, role);
                if (m == null)
                {
                    return ok;
                }
            }
            if (Str(m, "MeterMultiPriceCode").Trim().Length <= 0 && Str(m, "CustomTiers").Trim().Length <= 0)
            {
                SetIfCol(m, "ChargesRate", rate);
                if (role == "RENTAL")
                {
                    SetIfCol(m, "MinimumCharges", 0m);
                }
            }
            return ok;
        }

        private void Harvest()
        {
            GridViewLines.PostEditor();
            GridViewMachines.PostEditor();
            foreach (DataRow r in _dtMachines.Rows)
            {
                int idx = Convert.ToInt32(r["Idx"]);
                if (idx >= 0 && idx < _items.Count)
                {
                    _items[idx].MergeGroupCode = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["MergeGroup"]));
                    _items[idx].MergeGroupCodeMeter = ScpRentalGroupPrice.Sanitize(Convert.ToString(r["MeterGroup"]));
                    string label = Convert.ToString((r["LineLabel"] == DBNull.Value) ? "" : r["LineLabel"]).Trim();
                    if (label.Length > 60)
                    {
                        label = label.Substring(0, 60);
                    }
                    _items[idx].LineGroupCode = label;
                }
            }
            Result = new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r2 in _dtLines.Rows)
            {
                string fld = Convert.ToString(r2["Field"]);
                if (fld == "WAIVE" || fld == "COMMIT")
                {
                    continue;
                }
                decimal p = ((r2["UnitPrice"] == DBNull.Value) ? 0m : Convert.ToDecimal(r2["UnitPrice"]));
                string lad = ((r2["LadderText"] == DBNull.Value) ? "" : Convert.ToString(r2["LadderText"]).Trim());
                int solo = ((r2["SoloIdx"] == DBNull.Value) ? (-1) : Convert.ToInt32(r2["SoloIdx"]));
                if (solo >= 0 && p > 0m)
                {
                    ItemEditData sd = ItemAt(solo);
                    string role = ((fld == "OwnRate") ? "RENTAL" : ((fld == "OwnBk") ? "BK" : "CL"));
                    DataRow sm = MeterOf(sd, role);
                    if (sm != null)
                    {
                        SetIfCol(sm, "ChargesRate", p);
                        if (role == "RENTAL")
                        {
                            SetIfCol(sm, "MinimumCharges", 0m);
                        }
                    }
                }
                else if (solo < 0 && (!(p <= 0m) || lad.Length != 0))
                {
                    string side = Convert.ToString(r2["Side"]);
                    string grp = ScpRentalGroupPrice.Sanitize(Convert.ToString(r2["GroupCode"]));
                    string key = ScpLineTerms.Key(side, grp);
                    ScpLineTerms t;
                    if (!Result.TryGetValue(key, out t))
                    {
                        t = new ScpLineTerms();
                        t.Side = side;
                        t.GroupCode = grp;
                        Result[key] = t;
                    }
                    string field = Convert.ToString(r2["Field"]);
                    if (field == "OwnRate")
                    {
                        t.UnitPrice = p;
                    }
                    else if (field == "OwnBk")
                    {
                        t.BkPrice = p;
                        t.LadderBk = lad;
                    }
                    else
                    {
                        t.ClPrice = p;
                        t.LadderCl = lad;
                    }
                }
            }
        }

        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (base.DialogResult != DialogResult.OK && _changed && XtraMessageBox.Show("You have unsaved changes. Discard them and close?", "Billing Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
