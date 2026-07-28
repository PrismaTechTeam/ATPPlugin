using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// Per-meter Multi-Price picker + tier editor, opened from the meter grid's price button.
    /// Top: SearchLookUpEdit to pick a zSCP_MeterMultiPrice scheme. Below: its tier ladder
    /// (Meter Reading &lt;= / Unit Price), editable — add / delete / reorder / "∞ Unlimited".
    ///
    /// The master schemes are NEVER modified here. Edits become a PER-METER override:
    ///   * scheme picked, tiers untouched  -> meter uses the scheme          (grid shows "CODE")
    ///   * scheme picked, tiers edited     -> override rows + base code      (grid shows "CODE (Modified)")
    ///   * no scheme, tiers hand-built     -> override rows only             (grid shows "(Custom)")
    ///   * "No Multi-Price (clear)"        -> neither                        (grid empty; Unit Price active again)
    ///
    /// On OK it validates the billing traps:
    ///   * the LAST bracket must be unlimited (otherwise usage beyond it finds no bracket),
    ///   * a 0.00-price FREE band conflicts with the meter's own Free Qty (FOC) — only ONE of
    ///     the two free mechanisms is allowed, so the caller is told to clear Free Qty.
    /// </summary>
    public partial class MultiPricePicker_Form : XtraForm
    {
        // A boundary at/above this is treated as "unlimited". The master data's own convention
        // is 1,000,000,000 — anything a real copier can never reach.
        private const decimal INFINITY = 999999999999m;
        private const decimal INFINITY_MIN = 999999999m;

        private readonly DBSetting _db;
        private DataTable _schemes;
        private DataTable _tiers;
        private bool _loading;

        /// <summary>The scheme code for the meter row ("" when fully custom / cleared).</summary>
        public string SelectedCode { get; private set; } = "";
        /// <summary>The per-meter override tiers as "boundary|price;..." CSV ("" = scheme as-is / cleared).</summary>
        public string CustomCsv { get; private set; } = "";

        public MultiPricePicker_Form(DBSetting db, string currentCode, string currentCustomCsv, decimal meterFocQty)
        {
            InitializeComponent();
            _db = db;
            // meterFocQty is no longer needed for a prompt — a ladder always zeroes + locks Free Qty.

            _tiers = new DataTable();
            _tiers.Columns.Add("MeterReading", typeof(decimal));
            _tiers.Columns.Add("UnitPrice", typeof(decimal));
            GridTiers.DataSource = _tiers;
            ColReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            ColReading.DisplayFormat.FormatString = "#,##0.##";
            ColPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            ColPrice.DisplayFormat.FormatString = "0.000000";

            LoadSchemes();
            SluScheme.Properties.DataSource = _schemes;
            SluScheme.Properties.DisplayMember = "MeterMultiPriceCode";
            SluScheme.Properties.ValueMember = "MeterMultiPriceCode";

            SluScheme.EditValueChanged += new EventHandler(SluScheme_EditValueChanged);
            BtnAddRow.Click += new EventHandler(BtnAddRow_Click);
            BtnDelRow.Click += new EventHandler(BtnDelRow_Click);
            BtnRowUp.Click += new EventHandler(delegate { MoveRow(-1); });
            BtnRowDown.Click += new EventHandler(delegate { MoveRow(+1); });
            BtnInfinity.Click += new EventHandler(BtnInfinity_Click);
            BtnClear.Click += new EventHandler(BtnClear_Click);
            BtnOK.Click += new EventHandler(BtnOK_Click);

            // Existing override rows win; else the current scheme's master rows.
            _loading = true;
            if (!string.IsNullOrEmpty(currentCode)) SluScheme.EditValue = currentCode;
            _loading = false;
            if (!string.IsNullOrEmpty(currentCustomCsv))
                FillTiers(ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Item_Form.ParseTiersCsv(currentCustomCsv));
            else if (!string.IsNullOrEmpty(currentCode))
                FillTiers(LoadMasterTiers(currentCode));
        }

        private void LoadSchemes()
        {
            try
            {
                _schemes = _db.GetDataTable(
                    "SELECT MeterMultiPriceCode, [Description] FROM [dbo].[zSCP_MeterMultiPrice] ORDER BY MeterMultiPriceCode", false);
            }
            catch { _schemes = new DataTable(); _schemes.Columns.Add("MeterMultiPriceCode"); _schemes.Columns.Add("Description"); }
        }

        private void SluScheme_EditValueChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            string code = SluScheme.EditValue == null ? "" : SluScheme.EditValue.ToString().Trim();
            FillTiers(code.Length == 0 ? new System.Collections.Generic.List<decimal[]>() : LoadMasterTiers(code));
        }

        private System.Collections.Generic.List<decimal[]> LoadMasterTiers(string code)
        {
            System.Collections.Generic.List<decimal[]> rows = new System.Collections.Generic.List<decimal[]>();
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT MeterReading, UnitPrice FROM [dbo].[zSCP_MeterMultiPriceItem] " +
                    "WHERE MeterMultiPriceCode = N'" + SQLString(code) + "' ORDER BY MeterReading", false);
                foreach (DataRow r in dt.Rows)
                    rows.Add(new decimal[] { Convert.ToDecimal(r["MeterReading"]), Convert.ToDecimal(r["UnitPrice"]) });
            }
            catch (Exception ex) { XtraMessageBox.Show("Load tiers failed:\r\n" + ex.Message, "Multi-Price"); }
            return rows;
        }

        private void FillTiers(System.Collections.Generic.List<decimal[]> rows)
        {
            _tiers.Clear();
            foreach (decimal[] t in rows)
            {
                DataRow nr = _tiers.NewRow();
                nr["MeterReading"] = t[0];
                nr["UnitPrice"] = t[1];
                _tiers.Rows.Add(nr);
            }
        }

        // Canonical order-independent set string (billing sorts by boundary anyway).
        private static string Canonical(System.Collections.Generic.List<decimal[]> rows)
        {
            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
            foreach (decimal[] t in rows)
                parts.Add(t[0].ToString("0.00") + "|" + t[1].ToString("0.000000"));
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts.ToArray());
        }

        private System.Collections.Generic.List<decimal[]> GridRows()
        {
            System.Collections.Generic.List<decimal[]> rows = new System.Collections.Generic.List<decimal[]>();
            foreach (DataRow r in _tiers.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                decimal mr = r["MeterReading"] == DBNull.Value ? 0m : Convert.ToDecimal(r["MeterReading"]);
                decimal up = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (mr <= 0m) continue;   // a 0-boundary row covers nothing
                rows.Add(new decimal[] { mr, up });
            }
            return rows;
        }

        private int FocusedTierIndex()
        {
            int rh = ViewTiers.FocusedRowHandle;
            if (rh < 0) return -1;
            DataRow row = ViewTiers.GetDataRow(rh);
            if (row == null) return -1;
            return _tiers.Rows.IndexOf(row);
        }

        private void BtnAddRow_Click(object sender, EventArgs e)
        {
            ViewTiers.CloseEditor(); ViewTiers.UpdateCurrentRow();
            DataRow nr = _tiers.NewRow();
            nr["MeterReading"] = 0m; nr["UnitPrice"] = 0m;
            _tiers.Rows.Add(nr);
            ViewTiers.FocusedRowHandle = ViewTiers.GetRowHandle(_tiers.Rows.Count - 1);
            ViewTiers.FocusedColumn = ColReading;
            ViewTiers.ShowEditor();
        }

        private void BtnDelRow_Click(object sender, EventArgs e)
        {
            int idx = FocusedTierIndex();
            if (idx < 0) return;
            ViewTiers.CloseEditor();
            _tiers.Rows[idx].Delete();
            _tiers.AcceptChanges();
        }

        private void MoveRow(int delta)
        {
            int idx = FocusedTierIndex();
            if (idx < 0) return;
            int target = idx + delta;
            if (target < 0 || target >= _tiers.Rows.Count) return;
            ViewTiers.CloseEditor(); ViewTiers.UpdateCurrentRow();
            object[] a = _tiers.Rows[idx].ItemArray;
            object[] b = _tiers.Rows[target].ItemArray;
            _tiers.Rows[idx].ItemArray = b;
            _tiers.Rows[target].ItemArray = a;
            ViewTiers.FocusedRowHandle = ViewTiers.GetRowHandle(target);
        }

        // "∞ Unlimited": the focused row (or the highest row when none focused) becomes the
        // catch-all last bracket, so no usage can ever fall outside the ladder.
        private void BtnInfinity_Click(object sender, EventArgs e)
        {
            ViewTiers.CloseEditor(); ViewTiers.UpdateCurrentRow();
            int idx = FocusedTierIndex();
            if (idx < 0 && _tiers.Rows.Count > 0) idx = IndexOfMaxBoundary();
            if (idx < 0) return;
            _tiers.Rows[idx]["MeterReading"] = INFINITY;
        }

        private int IndexOfMaxBoundary()
        {
            int best = -1; decimal bestVal = decimal.MinValue;
            for (int i = 0; i < _tiers.Rows.Count; i++)
            {
                if (_tiers.Rows[i].RowState == DataRowState.Deleted) continue;
                decimal v = _tiers.Rows[i]["MeterReading"] == DBNull.Value ? 0m : Convert.ToDecimal(_tiers.Rows[i]["MeterReading"]);
                if (v > bestVal) { bestVal = v; best = i; }
            }
            return best;
        }

        // Clear: no scheme, no override — the meter bills by its flat Unit Price again
        // (and its own stored Free Qty comes back into force).
        private void BtnClear_Click(object sender, EventArgs e)
        {
            SelectedCode = "";
            CustomCsv = "";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ViewTiers.CloseEditor(); ViewTiers.UpdateCurrentRow();
            string code = SluScheme.EditValue == null ? "" : SluScheme.EditValue.ToString().Trim();

            System.Collections.Generic.List<decimal[]> rows = GridRows();
            if (rows.Count == 0)
            {
                XtraMessageBox.Show(code.Length == 0
                        ? "Add at least one tier row (Meter Reading > 0), or use \"No Multi-Price (clear)\"."
                        : "The scheme has no tier rows. Add at least one row (Meter Reading > 0).",
                    "Multi-Price");
                return;
            }

            decimal maxBoundary = 0m;
            foreach (decimal[] t in rows)
                if (t[0] > maxBoundary) maxBoundary = t[0];

            // 1) The LAST bracket must be unlimited — otherwise usage above it finds NO bracket.
            if (maxBoundary < INFINITY_MIN)
            {
                if (XtraMessageBox.Show(
                        "The last bracket ends at " + maxBoundary.ToString("#,##0.##") + ". Usage above that would find NO " +
                        "bracket (billing error).\r\n\r\nExtend the last bracket to unlimited (∞) now?",
                        "Multi-Price", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                int idx = IndexOfMaxBoundary();
                if (idx >= 0) _tiers.Rows[idx]["MeterReading"] = INFINITY;
                rows = GridRows();
            }

            // 2) A ladder in effect means the engine IGNORES the meter's own Free Qty entirely (the
            //    ladder's 0.00 band is the free mechanism). The meter grid locks the Free Qty cell
            //    and DISPLAYS the ladder's own free quantity instead; the stored value is untouched
            //    (it comes back into force if the ladder is later cleared).

            // 3) Scheme untouched -> just the code. Edited (or schemeless) -> per-meter override rows.
            //    Master schemes are NEVER written from here (that is Meter Multi Pricing maintenance).
            SelectedCode = code;
            if (code.Length > 0 && Canonical(rows) == Canonical(LoadMasterTiers(code)))
                CustomCsv = "";
            else
            {
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                foreach (decimal[] t in rows)
                    parts.Add(t[0].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        + "|" + t[1].ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
                CustomCsv = string.Join(";", parts.ToArray());
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
