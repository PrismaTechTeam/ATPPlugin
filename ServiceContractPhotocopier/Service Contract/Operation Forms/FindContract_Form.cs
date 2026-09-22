using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes.CommonForms;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Find Service Contract -- AutoCount's own Find Stock Item, for contracts.
    ///
    /// <para>A contract is found by what people actually have in hand: a service item number off a
    /// machine's sticker, a serial number, a model, as often as a contract number or a customer. So
    /// the keyword is matched against the contract's own fields AND every machine on it -- its
    /// service item no, its serial and the serials of the units it provides, and their models -- and
    /// "Found In" says which of them it was.</para>
    ///
    /// <para>Opened from Maintain Service Contract, and it stays open beside it: Edit and Delete are
    /// that list's own, so a contract opens and deletes exactly as it does from the list.</para>
    /// </summary>
    public partial class FindContract_Form : XtraForm
    {
        private const string ALL = "All";
        private const string MATCH_OR = "OR";
        private const string MATCH_AND = "AND";

        private readonly DBSetting _db;
        private readonly Action<long> _openContract;
        private readonly Func<List<long>, List<string>, bool> _deleteContracts;
        private readonly DataTable _result = NewResult();

        public FindContract_Form()
        {
            InitializeComponent();
        }

        /// <param name="openContract">Opens one contract the way the list does.</param>
        /// <param name="deleteContracts">Asks, deletes, and says whether it did -- the list's own delete.</param>
        public FindContract_Form(DBSetting db, Action<long> openContract,
            Func<List<long>, List<string>, bool> deleteContracts) : this()
        {
            _db = db;
            _openContract = openContract;
            _deleteContracts = deleteContracts;
            GridResult.DataSource = _result;
            Fill(CmbMatching, new string[] { MATCH_OR, MATCH_AND });
            Fill(CmbStatus, new string[] { ALL, "Active", "Inactive" });
            Fill(CmbExpiry, new string[] { ALL, "Running", "Expiring in 30 days", "Expired" });
            Fill(CmbBilling, new string[] { ALL, "One invoice", "One per machine" });
            Fill(CmbRental, new string[] { ALL, "With the copies", "On its own" });
            CmbMatching.Enabled = false;
            SetIcon();
            Wire();
            ShowCount();
        }

        private static void Fill(ComboBoxEdit c, string[] items)
        {
            c.Properties.Items.AddRange(items);
            c.SelectedIndex = 0;
        }

        private void SetIcon()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                System.Drawing.Image find = img.GetLargeImage_Find();
                if (find != null) this.IconOptions.Image = find;
            }
            catch { }   // an icon is cosmetic
        }

        private void Wire()
        {
            BtnSearch.Click += new EventHandler(BtnSearch_Click);
            BtnClearSearch.Click += new EventHandler(BtnClearSearch_Click);
            BtnAdvanced.Click += new EventHandler(BtnAdvanced_Click);
            BtnCheckAll.Click += new EventHandler(BtnCheckAll_Click);
            BtnUncheckAll.Click += new EventHandler(BtnUncheckAll_Click);
            BtnUncheckSelection.Click += new EventHandler(BtnUncheckSelection_Click);
            BtnClearUnchecked.Click += new EventHandler(BtnClearUnchecked_Click);
            BtnEdit.Click += new EventHandler(BtnEdit_Click);
            BtnDelete.Click += new EventHandler(BtnDelete_Click);
            BtnCancel.Click += new EventHandler(BtnCancel_Click);
            ChkGoogle.CheckedChanged += new EventHandler(ChkGoogle_CheckedChanged);
            TxtKeyword.KeyDown += new KeyEventHandler(TxtKeyword_KeyDown);
            GridViewResult.DoubleClick += new EventHandler(GridViewResult_DoubleClick);
            this.KeyDown += new KeyEventHandler(Form_KeyDown);
            this.Shown += new EventHandler(Form_Shown);
        }

        private void Form_Shown(object sender, EventArgs e)
        {
            TxtKeyword.Focus();
        }

        // ------------------------------------------------------------------ tables

        private static DataTable NewResult()
        {
            DataTable t = new DataTable("Result");
            t.Columns.Add("Check", typeof(bool));
            t.Columns.Add("ContractKey", typeof(long));
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("DebtorCode", typeof(string));
            t.Columns.Add("DebtorName", typeof(string));
            t.Columns.Add("ContractTypeCode", typeof(string));
            t.Columns.Add("ContractDate", typeof(DateTime));
            t.Columns.Add("ServiceStartDate", typeof(DateTime));
            t.Columns.Add("ServiceExpiryDate", typeof(DateTime));
            t.Columns.Add("ItemCount", typeof(int));
            t.Columns.Add("FoundIn", typeof(string));
            t.Columns.Add("Status", typeof(string));
            t.Columns.Add("Agent", typeof(string));
            t.Columns.Add("ReferenceNo", typeof(string));
            t.Columns.Add("Description", typeof(string));
            return t;
        }

        /// <summary>Every contract with the fields it can be found by. Read fresh on every search, so
        /// a contract saved a minute ago is found.</summary>
        private DataTable LoadContracts()
        {
            return _db.GetDataTable(
                "SELECT v.ContractKey, v.ContractNo, ISNULL(v.ContractTypeCode,'') AS ContractTypeCode, " +
                "ISNULL(v.DebtorCode,'') AS DebtorCode, ISNULL(v.DebtorName,'') AS DebtorName, " +
                "v.ContractDate, v.ServiceStartDate, v.ServiceExpiryDate, ISNULL(v.ItemCount,0) AS ItemCount, " +
                "ISNULL(v.Inactive,'N') AS Inactive, ISNULL(v.BillingMode,'G') AS BillingMode, " +
                "ISNULL(v.Agent,'') AS Agent, ISNULL(v.Area,'') AS Area, ISNULL(v.ReferenceNo,'') AS ReferenceNo, " +
                "ISNULL(v.Description,'') AS Description, " +
                "LTRIM(ISNULL(c.Address1,'') + ' ' + ISNULL(c.Address2,'') + ' ' + ISNULL(c.Address3,'') + ' ' + ISNULL(c.Address4,'')) AS Address, " +
                "ISNULL(c.Attention,'') AS Attention, ISNULL(c.Phone,'') AS Phone, " +
                "LTRIM(ISNULL(c.Remark1,'') + ' ' + ISNULL(c.Remark2,'') + ' ' + ISNULL(CAST(c.Note AS nvarchar(max)),'')) AS Remark, " +
                "ISNULL(c.RentalSeparateInvoice,'N') AS RentalSeparateInvoice " +
                "FROM dbo.zvSCP2_ContractList v JOIN dbo.zSCP2_Contract c ON c.ContractKey = v.ContractKey " +
                "ORDER BY v.ContractNo", false);
        }

        /// <summary>One machine, and everything on it a person might search by.</summary>
        private class Machine
        {
            public string ItemNo = "";
            public readonly List<string> Serials = new List<string>();
            public readonly List<string> Models = new List<string>();
        }

        /// <summary>Every machine of every contract, keyed by contract. The contract's invisible
        /// fleet machine is left out -- nobody has its number on a sticker.</summary>
        private Dictionary<long, List<Machine>> LoadMachines()
        {
            Dictionary<long, List<Machine>> byContract = new Dictionary<long, List<Machine>>();
            Dictionary<long, Machine> byItem = new Dictionary<long, Machine>();
            DataTable items = _db.GetDataTable(
                "SELECT ItemKey, ContractKey, ISNULL(ServiceItemNo,'') AS ServiceItemNo, " +
                "ISNULL(SerialNumber,'') AS SerialNumber, ISNULL(ItemCode,'') AS ItemCode " +
                "FROM dbo.zSCP2_Item WHERE ISNULL(IsGroupItem,'N') <> 'Y'", false);
            foreach (DataRow r in items.Rows)
            {
                Machine m = new Machine();
                m.ItemNo = S(r["ServiceItemNo"]).Trim();
                AddDistinct(m.Serials, S(r["SerialNumber"]));
                AddDistinct(m.Models, S(r["ItemCode"]));
                long ck = D64(r["ContractKey"]);
                List<Machine> list;
                if (!byContract.TryGetValue(ck, out list)) { list = new List<Machine>(); byContract[ck] = list; }
                list.Add(m);
                byItem[D64(r["ItemKey"])] = m;
            }
            // The units a machine provides carry serials and models of their own.
            DataTable units = _db.GetDataTable(
                "SELECT ItemKey, ISNULL(ItemCode,'') AS ItemCode, ISNULL(SerialNumber,'') AS SerialNumber " +
                "FROM dbo.zSCP2_ItemCode", false);
            foreach (DataRow u in units.Rows)
            {
                Machine m;
                if (!byItem.TryGetValue(D64(u["ItemKey"]), out m)) continue;
                AddDistinct(m.Serials, S(u["SerialNumber"]));
                AddDistinct(m.Models, S(u["ItemCode"]));
            }
            return byContract;
        }

        private static void AddDistinct(List<string> list, string value)
        {
            string v = (value ?? "").Trim();
            if (v.Length == 0) return;
            foreach (string x in list) if (string.Equals(x, v, StringComparison.OrdinalIgnoreCase)) return;
            list.Add(v);
        }

        // ------------------------------------------------------------------ search

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void TxtKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Search();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void ChkGoogle_CheckedChanged(object sender, EventArgs e)
        {
            // OR / AND is a question about several words, and only a Google-like search has them.
            CmbMatching.Enabled = ChkGoogle.Checked;
        }

        private void Search()
        {
            string keyword = (TxtKeyword.Text ?? "").Trim();
            List<string> words = new List<string>();
            if (keyword.Length > 0)
            {
                if (ChkGoogle.Checked)
                {
                    foreach (string w in keyword.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        words.Add(w);
                }
                else words.Add(keyword);
            }
            if (words.Count > 0 && !AnyFieldTicked())
            {
                XtraMessageBox.Show(this, "Tick at least one field under Search Criteria to look for the keyword in.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool everyWord = !ChkGoogle.Checked || CmbMatching.Text == MATCH_AND;

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                DataTable contracts = LoadContracts();
                Dictionary<long, List<Machine>> machines = LoadMachines();

                GridViewResult.BeginDataUpdate();
                try
                {
                    if (!ChkKeepResult.Checked) _result.Rows.Clear();
                    Dictionary<long, DataRow> have = new Dictionary<long, DataRow>();
                    foreach (DataRow r in _result.Rows) have[D64(r["ContractKey"])] = r;

                    foreach (DataRow c in contracts.Rows)
                    {
                        if (!PassesFilters(c)) continue;
                        long key = D64(c["ContractKey"]);
                        List<Machine> ms;
                        if (!machines.TryGetValue(key, out ms)) ms = new List<Machine>();

                        List<string> found = new List<string>();
                        if (words.Count > 0)
                        {
                            int hit = 0;
                            foreach (string w in words)
                            {
                                bool inContract = MatchContract(c, w, found);
                                bool inMachines = MatchMachines(ms, w, found);
                                if (inContract || inMachines) hit++;
                            }
                            bool ok = everyWord ? hit == words.Count : hit > 0;
                            if (!ok) continue;
                        }

                        DataRow row;
                        if (have.TryGetValue(key, out row))
                        {
                            row["FoundIn"] = Join(found);   // kept from an earlier search, found again
                            continue;
                        }
                        row = _result.NewRow();
                        row["Check"] = false;
                        row["ContractKey"] = key;
                        row["ContractNo"] = S(c["ContractNo"]);
                        row["DebtorCode"] = S(c["DebtorCode"]);
                        row["DebtorName"] = S(c["DebtorName"]);
                        row["ContractTypeCode"] = S(c["ContractTypeCode"]);
                        row["ContractDate"] = c["ContractDate"];
                        row["ServiceStartDate"] = c["ServiceStartDate"];
                        row["ServiceExpiryDate"] = c["ServiceExpiryDate"];
                        row["ItemCount"] = Convert.ToInt32(c["ItemCount"]);
                        row["FoundIn"] = Join(found);
                        row["Status"] = S(c["Inactive"]) == "Y" ? "Inactive" : "Active";
                        row["Agent"] = S(c["Agent"]);
                        row["ReferenceNo"] = S(c["ReferenceNo"]);
                        row["Description"] = S(c["Description"]);
                        _result.Rows.Add(row);
                        have[key] = row;
                    }
                }
                finally { GridViewResult.EndDataUpdate(); }
                if (GridViewResult.RowCount > 0)
                {
                    GridViewResult.FocusedRowHandle = 0;
                    GridViewResult.TopRowIndex = 0;   // a new result is read from the top
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "The search failed:" + Environment.NewLine + ex.Message, this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { this.Cursor = old; }
            ShowCount();
        }

        private bool AnyFieldTicked()
        {
            return ChkContractNo.Checked || ChkDebtorCode.Checked || ChkDebtorName.Checked ||
                   ChkServiceItemNo.Checked || ChkSerialNo.Checked || ChkItemCode.Checked ||
                   ChkContractType.Checked || ChkRefNo.Checked || ChkDescription.Checked ||
                   ChkRemark.Checked || ChkAgent.Checked || ChkArea.Checked || ChkAddress.Checked;
        }

        /// <summary>The word in any ticked field of the contract itself; each field it was in is noted.</summary>
        private bool MatchContract(DataRow c, string word, List<string> found)
        {
            bool any = false;
            any |= Try(ChkContractNo.Checked, S(c["ContractNo"]), word, "Contract No", found);
            any |= Try(ChkDebtorCode.Checked, S(c["DebtorCode"]), word, "Customer Code", found);
            any |= Try(ChkDebtorName.Checked, S(c["DebtorName"]), word, "Customer Name", found);
            any |= Try(ChkContractType.Checked, S(c["ContractTypeCode"]), word, "Contract Type", found);
            any |= Try(ChkRefNo.Checked, S(c["ReferenceNo"]), word, "Reference No", found);
            any |= Try(ChkDescription.Checked, S(c["Description"]), word, "Description", found);
            any |= Try(ChkRemark.Checked, S(c["Remark"]), word, "Remark / Note", found);
            any |= Try(ChkAgent.Checked, S(c["Agent"]), word, "Agent", found);
            any |= Try(ChkArea.Checked, S(c["Area"]), word, "Area", found);
            any |= Try(ChkAddress.Checked, S(c["Address"]), word, "Address", found);
            any |= Try(ChkAddress.Checked, S(c["Attention"]), word, "Attention", found);
            any |= Try(ChkAddress.Checked, S(c["Phone"]), word, "Phone", found);
            return any;
        }

        /// <summary>The word on any machine of the contract; noted as the machine it was on, because
        /// that is the answer the person searching by a serial wants.</summary>
        private bool MatchMachines(List<Machine> machines, string word, List<string> found)
        {
            bool any = false;
            foreach (Machine m in machines)
            {
                if (ChkServiceItemNo.Checked && Has(m.ItemNo, word))
                {
                    AddDistinct(found, m.ItemNo);
                    any = true;
                }
                if (ChkSerialNo.Checked)
                    foreach (string s in m.Serials)
                        if (Has(s, word)) { AddDistinct(found, "Serial " + s + " (" + m.ItemNo + ")"); any = true; }
                if (ChkItemCode.Checked)
                    foreach (string md in m.Models)
                        if (Has(md, word)) { AddDistinct(found, "Model " + md + " (" + m.ItemNo + ")"); any = true; }
            }
            return any;
        }

        private static bool Try(bool ticked, string value, string word, string label, List<string> found)
        {
            if (!ticked || !Has(value, word)) return false;
            AddDistinct(found, label);
            return true;
        }

        private static bool Has(string value, string word)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Join(List<string> found)
        {
            string s = string.Join(", ", found.ToArray());
            return s.Length > 250 ? s.Substring(0, 247) + "..." : s;
        }

        private bool PassesFilters(DataRow c)
        {
            string status = CmbStatus.Text;
            bool inactive = S(c["Inactive"]) == "Y";
            if (status == "Active" && inactive) return false;
            if (status == "Inactive" && !inactive) return false;

            string expiry = CmbExpiry.Text;
            if (expiry != ALL)
            {
                DateTime? xp = c["ServiceExpiryDate"] == DBNull.Value ? (DateTime?)null
                    : Convert.ToDateTime(c["ServiceExpiryDate"]).Date;
                DateTime today = DateTime.Today;
                if (expiry == "Running" && xp.HasValue && xp.Value < today) return false;
                if (expiry == "Expiring in 30 days" && (!xp.HasValue || xp.Value < today || xp.Value > today.AddDays(30))) return false;
                if (expiry == "Expired" && (!xp.HasValue || xp.Value >= today)) return false;
            }

            string billing = CmbBilling.Text;
            bool perMachine = S(c["BillingMode"]).Trim().ToUpperInvariant() == "S";
            if (billing == "One invoice" && perMachine) return false;
            if (billing == "One per machine" && !perMachine) return false;

            string rental = CmbRental.Text;
            bool apart = S(c["RentalSeparateInvoice"]).Trim().ToUpperInvariant() == "Y";
            if (rental == "With the copies" && apart) return false;
            if (rental == "On its own" && !apart) return false;
            return true;
        }

        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            TxtKeyword.Text = "";
            _result.Rows.Clear();
            ShowCount();
            TxtKeyword.Focus();
        }

        /// <summary>The older all-columns search, for when the ticks are not enough. What is picked
        /// there joins the result here, ticked.</summary>
        private void BtnAdvanced_Click(object sender, EventArgs e)
        {
            DataTable data;
            try { data = LoadContracts(); }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "The contracts could not be read:" + Environment.NewLine + ex.Message, this.Text);
                return;
            }
            object picked = AdvanceSearch_Form.Pick(this, "Advanced Search - Service Contract", data, "ContractKey",
                new string[] { "ContractNo", "DebtorCode", "DebtorName", "ContractTypeCode", "ReferenceNo",
                               "Description", "Agent", "Area", "Address", "Attention", "Phone", "Remark" },
                new string[] { "Contract No.", "Customer Code", "Customer Name", "Contract Type", "Reference No",
                               "Description", "Agent", "Area", "Address", "Attention", "Phone", "Remark / Note" },
                new int[] { 110, 95, 220, 90, 110, 180, 80, 80, 260, 110, 100, 200 });
            if (picked == null) return;
            long key = D64(picked);
            DataRow row = null;
            foreach (DataRow r in _result.Rows) if (D64(r["ContractKey"]) == key) { row = r; break; }
            if (row == null)
            {
                foreach (DataRow c in data.Rows)
                {
                    if (D64(c["ContractKey"]) != key) continue;
                    row = _result.NewRow();
                    row["ContractKey"] = key;
                    row["ContractNo"] = S(c["ContractNo"]);
                    row["DebtorCode"] = S(c["DebtorCode"]);
                    row["DebtorName"] = S(c["DebtorName"]);
                    row["ContractTypeCode"] = S(c["ContractTypeCode"]);
                    row["ContractDate"] = c["ContractDate"];
                    row["ServiceStartDate"] = c["ServiceStartDate"];
                    row["ServiceExpiryDate"] = c["ServiceExpiryDate"];
                    row["ItemCount"] = Convert.ToInt32(c["ItemCount"]);
                    row["FoundIn"] = "Advanced Search";
                    row["Status"] = S(c["Inactive"]) == "Y" ? "Inactive" : "Active";
                    row["Agent"] = S(c["Agent"]);
                    row["ReferenceNo"] = S(c["ReferenceNo"]);
                    row["Description"] = S(c["Description"]);
                    _result.Rows.Add(row);
                    break;
                }
            }
            if (row == null) return;
            row["Check"] = true;
            int handle = GridViewResult.LocateByValue("ContractKey", key);
            if (handle >= 0) GridViewResult.FocusedRowHandle = handle;
            ShowCount();
        }

        // ------------------------------------------------------------------ result grid

        private void BtnCheckAll_Click(object sender, EventArgs e) { SetCheckAll(true); }

        private void BtnUncheckAll_Click(object sender, EventArgs e) { SetCheckAll(false); }

        private void SetCheckAll(bool value)
        {
            GridViewResult.CloseEditor();
            // What the grid shows: a filter or a group narrows "all" the way the eye expects.
            for (int i = 0; i < GridViewResult.DataRowCount; i++)
            {
                DataRow r = GridViewResult.GetDataRow(i);
                if (r != null) r["Check"] = value;
            }
            ShowCount();
        }

        private void BtnUncheckSelection_Click(object sender, EventArgs e)
        {
            GridViewResult.CloseEditor();
            foreach (int h in GridViewResult.GetSelectedRows())
            {
                DataRow r = GridViewResult.GetDataRow(h);
                if (r != null) r["Check"] = false;
            }
            ShowCount();
        }

        private void BtnClearUnchecked_Click(object sender, EventArgs e)
        {
            GridViewResult.CloseEditor();
            List<DataRow> drop = new List<DataRow>();
            foreach (DataRow r in _result.Rows) if (!Bool(r["Check"])) drop.Add(r);
            foreach (DataRow r in drop) _result.Rows.Remove(r);
            ShowCount();
        }

        /// <summary>The ticked contracts, or the one the cursor is on when none are ticked.</summary>
        private List<DataRow> Chosen()
        {
            GridViewResult.CloseEditor();
            List<DataRow> list = new List<DataRow>();
            foreach (DataRow r in _result.Rows) if (Bool(r["Check"])) list.Add(r);
            if (list.Count == 0 && GridViewResult.FocusedRowHandle >= 0)
            {
                DataRow f = GridViewResult.GetDataRow(GridViewResult.FocusedRowHandle);
                if (f != null) list.Add(f);
            }
            return list;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            List<DataRow> rows = Chosen();
            if (rows.Count == 0)
            {
                XtraMessageBox.Show(this, "Search, then pick the contract to open.", this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (rows.Count > 5 && XtraMessageBox.Show(this, "Open all " + rows.Count + " ticked contracts?", this.Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (DataRow r in rows) _openContract(D64(r["ContractKey"]));
        }

        private void GridViewResult_DoubleClick(object sender, EventArgs e)
        {
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit =
                GridViewResult.CalcHitInfo(GridResult.PointToClient(Control.MousePosition));
            if (!hit.InRow || hit.Column == ColCheck) return;
            DataRow r = GridViewResult.GetDataRow(hit.RowHandle);
            if (r != null) _openContract(D64(r["ContractKey"]));
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            List<DataRow> rows = Chosen();
            if (rows.Count == 0) return;
            List<long> keys = new List<long>();
            List<string> nos = new List<string>();
            foreach (DataRow r in rows) { keys.Add(D64(r["ContractKey"])); nos.Add(S(r["ContractNo"])); }
            if (!_deleteContracts(keys, nos)) return;
            foreach (DataRow r in rows) _result.Rows.Remove(r);
            ShowCount();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ShowCount()
        {
            int ticked = 0;
            foreach (DataRow r in _result.Rows) if (Bool(r["Check"])) ticked++;
            LblCount.Text = _result.Rows.Count + " contract" + (_result.Rows.Count == 1 ? "" : "s") + " found" +
                            (ticked > 0 ? ", " + ticked + " ticked" : "");
        }

        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
        private static bool Bool(object o) { return o != null && o != DBNull.Value && Convert.ToBoolean(o); }
    }
}
