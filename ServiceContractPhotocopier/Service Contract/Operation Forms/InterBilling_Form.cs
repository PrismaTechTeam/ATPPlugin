using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.MeterReading.OperationForms;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Billing off another company's machines.
    ///
    /// <para>Two account books of one owner. One holds the contract with the machines on it and takes
    /// the readings; the other has the real end customer and bills at its own prices. This screen is
    /// the second book's side of that: it looks into the first, takes a contract, and from then on
    /// keeps the two in step -- without either of them typing the same machine or the same reading
    /// twice.</para>
    ///
    /// <para>The rule the whole thing hangs on, and the reason the tabs are in this order:
    /// <b>they own the meters, this book owns the money</b>. Which machines and which readings are
    /// theirs and always follow; prices, minimums, waives, bill groups and how many invoices come out
    /// are this book's and never do.</para>
    /// </summary>
    // Off the menu (2026-09-14): replaced by InterBillBoard_Form, which reads HQ's readings live
    // and bills from one board. This screen and its four tabs are kept; put the attribute back to
    // list it again.
    // [AutoCount.PlugIn.MenuItem("Inter-Billing", MenuOrder = 240, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class InterBilling_Form : XtraForm
    {
        /// <summary>One line of "what they changed", with everything needed to act on it.</summary>
        private class ChangeRow
        {
            public long RootContractKey;
            public string ContractNo = "";
            public string Kind = "";          // MACHINE_ADDED / MACHINE_GONE / METER_ADDED / METER_GONE / DETAIL
            public string What = "";
            public string Which = "";
            public string Change = "";
            public DateTime TakenAt;
            public long LinkKey;              // when the thing is already linked
            public long SourceKey;            // when it is not
            public long LocalParentKey;       // a new counter hangs on this machine
            public string NewSnapshot = "";
            public ScpRemoteMachine Machine;
            public ScpRemoteMeter Meter;
        }

        /// <summary>One line of "their readings", against what this book has for the same period.</summary>
        private class ReadRow
        {
            public long LocalMeterKey;
            public string ContractNo = "";
            public string SerialNumber = "";
            public string Counter = "";
            public decimal TheirReading;
            public DateTime? ReadingDate;
            public string Status = "";
            public bool CanBring;
            /// <summary>Their credit note, when they have revised this reading since billing it.</summary>
            public string CorrectionDocNo = "";
            /// <summary>What they billed before revising it.</summary>
            public decimal TheirBilledReading;
            /// <summary>They revised it AFTER this book had already invoiced the customer --
            /// the one case nothing here can put right on its own.</summary>
            public bool CorrectedTooLate;
        }

        private DBSetting _dbSetting;
        private string _userId = "ADMIN";
        private List<ScpInterBillBook> _books = new List<ScpInterBillBook>();
        private ScpInterBillBook _book;
        private string _remoteCs = "";

        private List<ScpRemoteContract> _contracts = new List<ScpRemoteContract>();
        private List<ScpRemoteMachine> _machines = new List<ScpRemoteMachine>();
        private List<ChangeRow> _changes = new List<ChangeRow>();
        private List<ReadRow> _reads = new List<ReadRow>();

        private DataTable _dtContracts;
        private DataTable _dtMachines;
        private DataTable _dtChanges;
        private DataTable _dtReadings;
        private DataTable _dtBillable;
        private List<long> _billableKeys = new List<long>();
        private DataTable _dtDebtors;
        private bool _loading;

        public InterBilling_Form() { InitializeComponent(); ApplyAutoCountToolbarImages(); }

        public InterBilling_Form(UserSession userSession) : this()
        {
            if (userSession != null)
            {
                _dbSetting = userSession.DBSetting;
                try { _userId = userSession.LoginUserID; } catch { }
            }
            this.Load += new EventHandler(OnFormLoad);
        }

        public InterBilling_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void ApplyAutoCountToolbarImages()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                this.BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                this.BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            LblBookStatus.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblTakeNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblChangeNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblReadNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblTakeNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);
            LblChangeNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);
            LblReadNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            if (_dbSetting == null) return;

            SpnYear.EditValue = DateTime.Today.Year;
            SpnMonth.EditValue = DateTime.Today.Month;
            SpnBillYear.EditValue = DateTime.Today.Year;
            SpnBillMonth.EditValue = DateTime.Today.Month;
            LblBillNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblBillNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            LoadDebtors();
            GridViewContracts.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(OnContractRowChanged);
            TxtSearch.KeyDown += new KeyEventHandler(OnSearchKeyDown);
            ChkIncludeExpired.CheckedChanged += new EventHandler(OnRefresh);
            TabMain.SelectedPageChanged +=
                new DevExpress.XtraTab.TabPageChangedEventHandler(OnTabChanged);

            LoadBooks();
        }

        // ---------------------------------------------------------------- the other book

        private void LoadBooks()
        {
            _loading = true;
            try
            {
                _books = new List<ScpInterBillBook>();
                CboBook.Properties.Items.Clear();
                foreach (ScpInterBillBook b in ScpInterBillBooks.LoadAll(_dbSetting))
                {
                    if (b.Inactive) continue;
                    _books.Add(b);
                    CboBook.Properties.Items.Add(b.Display);
                }
                if (_books.Count > 0) CboBook.SelectedIndex = 0;
            }
            finally { _loading = false; }

            _book = _books.Count > 0 ? _books[0] : null;
            ShowBookStatus();
            if (_book != null && _book.IsUsable) LoadContracts();
        }

        private void ShowBookStatus()
        {
            if (_book == null)
            {
                LblBookStatus.Text =
                    "No other book is set up yet." + Environment.NewLine +
                    "General Setup > Inter-Billing Setup -- put in the server and account book, then Test connection.";
                _remoteCs = "";
                return;
            }
            if (!_book.IsUsable)
            {
                LblBookStatus.Text =
                    _book.Alias + " has never been reached." + Environment.NewLine +
                    "General Setup > Inter-Billing Setup -- press Test connection there. Nothing can be " +
                    "taken from a book this one has not opened.";
                _remoteCs = "";
                return;
            }
            _remoteCs = ScpInterBillBooks.ConnectionStringOf(_dbSetting, _book);
            // The standing arrangement with that company is the starting figure; this screen lets it
            // be changed for the contract in hand without changing the arrangement.
            SpnMargin.EditValue = _book.MarginPercent;
            LblBookStatus.Text = _book.RemoteCompany.Length > 0
                ? "Reading from " + _book.RemoteCompany + "  (" + _book.DatabaseName + " on " + _book.ServerName + ")"
                : "Reading from " + _book.DatabaseName + " on " + _book.ServerName;
        }

        private void OnBookChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            int i = CboBook.SelectedIndex;
            _book = i >= 0 && i < _books.Count ? _books[i] : null;
            ShowBookStatus();
            RefreshCurrentTab();
        }

        private void OnTabChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {
            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            if (_book == null || !_book.IsUsable)
            {
                Bind(ref _dtContracts, GridContracts, MakeContractTable());
                Bind(ref _dtMachines, GridMachines, MakeMachineTable());
                Bind(ref _dtChanges, GridChanges, MakeChangeTable());
                Bind(ref _dtReadings, GridReadings, MakeReadingTable());
                Bind(ref _dtBillable, GridBill, MakeBillableTable());
                return;
            }
            if (TabMain.SelectedTabPage == TabIncoming) LoadContracts();
            else if (TabMain.SelectedTabPage == TabChanges) LoadChanges();
            else if (TabMain.SelectedTabPage == TabReadings) LoadReadings(false);
            else if (TabMain.SelectedTabPage == TabBill) LoadBillable();
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            LoadBooks();
            RefreshCurrentTab();
        }

        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { LoadContracts(); e.Handled = true; }
        }

        // ---------------------------------------------------------------- 1. their contracts

        private static DataTable MakeContractTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("DebtorName", typeof(string));
            t.Columns.Add("Description", typeof(string));
            t.Columns.Add("MachineCount", typeof(int));
            t.Columns.Add("ExpiryDate", typeof(DateTime));
            t.Columns.Add("Taken", typeof(string));
            return t;
        }

        private void LoadContracts()
        {
            DataTable t = MakeContractTable();
            _contracts = new List<ScpRemoteContract>();
            if (_book == null || !_book.IsUsable) { Bind(ref _dtContracts, GridContracts, t); return; }

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _contracts = ScpInterBillReader.Contracts(
                    _remoteCs, ChkIncludeExpired.Checked, TxtSearch.Text);

                // Who has already been taken is answered from THIS book's links. The other book is
                // never told that anybody is billing off it, and does not need to be.
                HashSet<long> taken = ScpInterBillLinks.TakenSourceKeys(
                    _dbSetting, _book.RemoteBookId, ScpInterBillLink.CONTRACT);
                Dictionary<long, string> mine = MyContractNumbersBySource();

                foreach (ScpRemoteContract c in _contracts)
                {
                    c.AlreadyTaken = taken.Contains(c.ContractKey);
                    DataRow r = t.NewRow();
                    r["ContractNo"] = c.ContractNo;
                    r["DebtorName"] = c.DebtorName.Length > 0 ? c.DebtorName : c.DebtorCode;
                    r["Description"] = c.Description;
                    r["MachineCount"] = c.MachineCount;
                    if (c.ExpiryDate.HasValue) r["ExpiryDate"] = c.ExpiryDate.Value;
                    string ours;
                    r["Taken"] = c.AlreadyTaken
                        ? (mine.TryGetValue(c.ContractKey, out ours) ? ours : "already taken")
                        : "";
                    t.Rows.Add(r);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    "Could not read from " + _book.Display + ":" + Environment.NewLine +
                    Environment.NewLine + ex.Message,
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { this.Cursor = old; }

            Bind(ref _dtContracts, GridContracts, t);
            LoadMachinesForFocused();
        }

        /// <summary>Their contract key -> the number this book gave the contract it made from it. So
        /// the list can say "you already have this, and it is SC-000123" rather than just "taken".</summary>
        private Dictionary<long, string> MyContractNumbersBySource()
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            if (_book == null) return map;
            foreach (ScpInterBillLink l in ScpInterBillLinks.OfType(
                         _dbSetting, _book.RemoteBookId, ScpInterBillLink.CONTRACT, false))
            {
                string no = ContractNoOf(l.LocalKey);
                if (no.Length == 0) continue;
                map[l.SourceKey] = map.ContainsKey(l.SourceKey) ? map[l.SourceKey] + ", " + no : no;
            }
            return map;
        }

        private string ContractNoOf(long contractKey)
        {
            if (contractKey <= 0) return "";
            try
            {
                object o = _dbSetting.ExecuteScalar(
                    "SELECT ContractNo FROM dbo.zSCP2_Contract WHERE ContractKey = " + contractKey);
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            }
            catch { return ""; }
        }

        private static DataTable MakeMachineTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("SerialNumber", typeof(string));
            t.Columns.Add("ItemCode", typeof(string));
            t.Columns.Add("Description", typeof(string));
            t.Columns.Add("Counters", typeof(string));
            t.Columns.Add("Status", typeof(string));
            return t;
        }

        private ScpRemoteContract FocusedContract()
        {
            int i = GridViewContracts.GetDataSourceRowIndex(GridViewContracts.FocusedRowHandle);
            return i >= 0 && i < _contracts.Count ? _contracts[i] : null;
        }

        private void OnContractRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading) return;
            LoadMachinesForFocused();
        }

        private void LoadMachinesForFocused()
        {
            DataTable t = MakeMachineTable();
            _machines = new List<ScpRemoteMachine>();
            ScpRemoteContract c = FocusedContract();
            if (c != null && _book != null && _book.IsUsable)
            {
                Cursor old = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    _machines = ScpInterBillReader.Machines(_remoteCs, c.ContractKey);
                    foreach (ScpRemoteMachine m in _machines)
                    {
                        DataRow r = t.NewRow();
                        r["SerialNumber"] = m.SerialNumber;
                        r["ItemCode"] = m.ItemCode;
                        r["Description"] = m.Description;
                        r["Counters"] = CounterList(m);
                        r["Status"] = m.Inactive ? "off hire" : "";
                        t.Rows.Add(r);
                    }
                }
                catch { }
                finally { this.Cursor = old; }
            }
            Bind(ref _dtMachines, GridMachines, t);
        }

        private static string CounterList(ScpRemoteMachine m)
        {
            List<string> names = new List<string>();
            foreach (ScpRemoteMeter t in m.Meters)
            {
                string n = t.MeterTypeCode;
                if (!names.Contains(n)) names.Add(n);
            }
            return string.Join(", ", names.ToArray());
        }

        private void LoadDebtors()
        {
            try
            {
                _dtDebtors = _dbSetting.GetDataTable(
                    "SELECT AccNo, ISNULL(CompanyName,'') AS CompanyName FROM dbo.Debtor ORDER BY AccNo", false);
            }
            catch { _dtDebtors = new DataTable(); }

            LkDebtor.Properties.DataSource = _dtDebtors;
            LkDebtor.Properties.DisplayMember = "AccNo";
            LkDebtor.Properties.ValueMember = "AccNo";
            LkDebtorView.OptionsBehavior.AutoPopulateColumns = false;
            LkDebtorView.Columns.Clear();
            DevExpress.XtraGrid.Columns.GridColumn c1 = LkDebtorView.Columns.AddVisible("AccNo");
            c1.Caption = "Code"; c1.Width = 100;
            DevExpress.XtraGrid.Columns.GridColumn c2 = LkDebtorView.Columns.AddVisible("CompanyName");
            c2.Caption = "Company Name"; c2.Width = 320;
            LkDebtorView.OptionsView.ShowAutoFilterRow = true;
        }

        private void OnTake(object sender, EventArgs e)
        {
            if (_book == null || !_book.IsUsable)
            {
                XtraMessageBox.Show(LblBookStatus.Text, "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ScpRemoteContract c = FocusedContract();
            if (c == null)
            {
                XtraMessageBox.Show("Pick one of their contracts first.", "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string debtor = LkDebtor.EditValue == null ? "" : LkDebtor.EditValue.ToString().Trim();
            if (debtor.Length == 0)
            {
                XtraMessageBox.Show(
                    "Choose the customer this contract is billed to." + Environment.NewLine +
                    Environment.NewLine +
                    "On their contract the customer is THIS company, because you are who they bill. " +
                    "The contract made here names the customer YOU bill -- and choosing them is the " +
                    "whole point of taking it rather than copying it.",
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LkDebtor.Focus();
                return;
            }
            if (_machines.Count == 0)
            {
                XtraMessageBox.Show("That contract has no machines on it.", "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int counters = 0;
            foreach (ScpRemoteMachine m in _machines) counters += m.Meters.Count;

            // What the margin will actually do, in this contract's own numbers. A percentage is
            // abstract; "their 0.0180 becomes your 0.0234" is a figure somebody can agree or refuse
            // BEFORE it is written onto twenty machines.
            string priceLine = MarginExample(Margin);

            string already = c.AlreadyTaken
                ? Environment.NewLine + Environment.NewLine +
                  "You have taken this one before. Doing it again makes a SECOND contract here off " +
                  "the same machines -- right if you are splitting them between two customers, wrong " +
                  "if you meant to update the first."
                : "";

            // Their numbers are kept, so a clash is a real obstacle and belongs in front of the
            // person BEFORE the confirmation, not in an error after they have said yes.
            List<string> clashes = ScpInterBillTake.NumberClashes(_dbSetting, c.ContractNo, _machines);
            if (clashes.Count > 0)
            {
                XtraMessageBox.Show(
                    "These numbers are already used in this book:" + Environment.NewLine +
                    Environment.NewLine + string.Join(Environment.NewLine, clashes.ToArray()) +
                    Environment.NewLine + Environment.NewLine +
                    "A contract taken from them keeps their contract number and their service item " +
                    "numbers, so both companies can talk about the same machine. Remove or renumber " +
                    "what is already here first.",
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (XtraMessageBox.Show(
                    "Make a contract here from " + c.ContractNo + "?" + Environment.NewLine +
                    Environment.NewLine +
                    "   " + _machines.Count + " machines, " + counters + " counters" + Environment.NewLine +
                    "   billed to " + debtor + Environment.NewLine +
                    "   kept as " + c.ContractNo + " -- their numbers, so both books match" +
                    Environment.NewLine +
                    "   " + (c.BillingMode == "S" ? "one invoice per machine" : "one invoice") +
                    ", billed on day " + c.BillingDay + " -- the way they bill it, and yours to change" +
                    Environment.NewLine +
                    Environment.NewLine +
                    priceLine + Environment.NewLine + Environment.NewLine +
                    "Every price stays editable afterwards, in Meters & Pricing." + already,
                    "Inter-Billing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            ScpTakeResult res;
            try
            {
                res = ScpInterBillTake.TakeContract(
                    _dbSetting, _book, _remoteCs, c, _machines,
                    debtor, "", c.Description, c.StartDate, c.ExpiryDate, _userId, Margin);
            }
            finally { this.Cursor = old; }

            if (!res.Ok)
            {
                XtraMessageBox.Show("Nothing was made:" + Environment.NewLine + Environment.NewLine +
                    res.Error, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string extra = res.MeterTypesCreated.Count == 0 ? "" :
                Environment.NewLine + Environment.NewLine +
                "Meter types this book had never seen were created: " +
                string.Join(", ", res.MeterTypesCreated.ToArray()) + ".";

            XtraMessageBox.Show(
                "Contract " + res.ContractNo + " made -- " + res.Machines + " machines, " +
                res.Meters + " counters." + extra + Environment.NewLine + Environment.NewLine +
                (Margin == 0m
                    ? "It bills at exactly what they charge."
                    : "It bills at their prices plus " + Margin.ToString("0.##") + "%.") +
                " Check them in Meters & Pricing before the first run -- they are ordinary prices " +
                "now, and yours to change.",
                "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadContracts();
        }

        /// <summary>One line saying what the margin does to this contract, using a real counter off
        /// it. Falls back to plain words when there is nothing priced to show.</summary>
        private string MarginExample(decimal margin)
        {
            if (margin == 0m)
                return "Their prices come across unchanged - margin is 0, so you bill exactly what " +
                       "they charge.";

            ScpRemoteMeter shown = null;
            foreach (ScpRemoteMachine m in _machines)
                foreach (ScpRemoteMeter t in m.Meters)
                    if (shown == null && t.ChargesRate > 0m) shown = t;

            if (shown == null)
                return "Their prices come across with " + margin.ToString("0.##") + "% added. " +
                       "(Nothing on this contract is priced over there, so nothing will be priced here.)";

            bool flat = ScpStrategy.IsRentalRole(shown.MeterRole, shown.MeterTypeCode);
            string fmt = flat ? "#,##0.00" : "0.0000";
            return "Their prices come across with " + margin.ToString("0.##") + "% added - " +
                   shown.MeterTypeCode + " " + shown.ChargesRate.ToString(fmt) + " becomes " +
                   ScpInterBillTake.WithMargin(shown.ChargesRate, margin, flat).ToString(fmt) + ".";
        }

        // ---------------------------------------------------------------- 2. what they changed

        private static DataTable MakeChangeTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("What", typeof(string));
            t.Columns.Add("Which", typeof(string));
            t.Columns.Add("Change", typeof(string));
            t.Columns.Add("TakenAt", typeof(DateTime));
            return t;
        }

        /// <summary>
        /// Everything the other book has done since this one took its contracts.
        ///
        /// <para>It is a set comparison and nothing else: their machines and counters against the
        /// links held here, plus the snapshot each link kept of what it took. That is why the
        /// snapshot exists -- without it there is no telling "they changed this" from "it was always
        /// like that".</para>
        /// </summary>
        private void LoadChanges()
        {
            DataTable t = MakeChangeTable();
            _changes = new List<ChangeRow>();
            if (_book == null || !_book.IsUsable) { Bind(ref _dtChanges, GridChanges, t); return; }

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                List<ScpInterBillLink> contractLinks = ScpInterBillLinks.OfType(
                    _dbSetting, _book.RemoteBookId, ScpInterBillLink.CONTRACT, false);

                foreach (ScpInterBillLink cl in contractLinks)
                {
                    string myNo = ContractNoOf(cl.LocalKey);
                    if (myNo.Length == 0) continue;      // gone from this book -- nothing to keep in step

                    ScpRemoteContract rc = ScpInterBillReader.Contract(_remoteCs, cl.SourceKey);
                    if (rc == null)
                    {
                        Add(t, new ChangeRow
                        {
                            RootContractKey = cl.LocalKey,
                            ContractNo = myNo,
                            Kind = "CONTRACT_GONE",
                            What = "Contract",
                            Which = cl.SourceRef,
                            Change = "their contract is no longer in that book",
                            TakenAt = cl.TakenAt,
                            LinkKey = cl.LinkKey,
                            NewSnapshot = cl.TakenSnapshot
                        });
                        continue;
                    }

                    // The contract's own facts -- did it expire, was it withdrawn.
                    string nowSnap = ScpInterBillTake.ContractSnapshot(rc);
                    List<string> diff = ScpInterBillSnapshot.Differences(cl.TakenSnapshot, nowSnap);
                    // MACHINES is a count, and the machines themselves are listed row by row below;
                    // reporting it as well would say the same news twice in different words.
                    diff.Remove("MACHINES");
                    if (diff.Count > 0)
                    {
                        Add(t, new ChangeRow
                        {
                            RootContractKey = cl.LocalKey,
                            ContractNo = myNo,
                            Kind = "DETAIL",
                            What = "Contract",
                            Which = rc.ContractNo,
                            Change = Describe(diff, cl.TakenSnapshot, nowSnap),
                            TakenAt = cl.TakenAt,
                            LinkKey = cl.LinkKey,
                            NewSnapshot = nowSnap
                        });
                    }

                    CompareMachines(t, cl, myNo, rc);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    "Could not read from " + _book.Display + ":" + Environment.NewLine +
                    Environment.NewLine + ex.Message,
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { this.Cursor = old; }

            Bind(ref _dtChanges, GridChanges, t);
        }

        private void CompareMachines(DataTable t, ScpInterBillLink contractLink, string myNo, ScpRemoteContract rc)
        {
            List<ScpRemoteMachine> theirs = ScpInterBillReader.Machines(_remoteCs, rc.ContractKey);
            List<ScpInterBillLink> mine = ScpInterBillLinks.ForContract(_dbSetting, contractLink.LocalKey);

            Dictionary<long, ScpInterBillLink> itemBySource = new Dictionary<long, ScpInterBillLink>();
            Dictionary<long, ScpInterBillLink> meterBySource = new Dictionary<long, ScpInterBillLink>();
            foreach (ScpInterBillLink l in mine)
            {
                if (l.EntityType == ScpInterBillLink.ITEM) itemBySource[l.SourceKey] = l;
                else if (l.EntityType == ScpInterBillLink.METER) meterBySource[l.SourceKey] = l;
            }

            HashSet<long> seenItems = new HashSet<long>();
            HashSet<long> seenMeters = new HashSet<long>();

            foreach (ScpRemoteMachine m in theirs)
            {
                seenItems.Add(m.ItemKey);
                ScpInterBillLink link;
                if (!itemBySource.TryGetValue(m.ItemKey, out link))
                {
                    Add(t, new ChangeRow
                    {
                        RootContractKey = contractLink.LocalKey,
                        ContractNo = myNo,
                        Kind = "MACHINE_ADDED",
                        What = "Machine",
                        Which = (m.SerialNumber + "  " + m.ItemCode).Trim(),
                        Change = "they put this machine on the contract -- it is not on yours",
                        TakenAt = contractLink.TakenAt,
                        SourceKey = m.ItemKey,
                        Machine = m
                    });
                    continue;
                }
                if (link.Status == ScpInterBillLink.DECLINED) continue;

                if (m.Inactive && link.Status != ScpInterBillLink.GONE)
                {
                    Add(t, new ChangeRow
                    {
                        RootContractKey = contractLink.LocalKey,
                        ContractNo = myNo,
                        Kind = "MACHINE_GONE",
                        What = "Machine",
                        Which = (m.SerialNumber + "  " + m.ItemCode).Trim(),
                        Change = "they took this machine off hire -- yours is still billing it",
                        TakenAt = link.TakenAt,
                        LinkKey = link.LinkKey,
                        NewSnapshot = m.Snapshot()
                    });
                }
                else
                {
                    string nowSnap = m.Snapshot();
                    List<string> diff = ScpInterBillSnapshot.Differences(link.TakenSnapshot, nowSnap);
                    diff.Remove("METERS");        // the counters are reported one by one below
                    if (diff.Count > 0)
                    {
                        Add(t, new ChangeRow
                        {
                            RootContractKey = contractLink.LocalKey,
                            ContractNo = myNo,
                            Kind = "DETAIL",
                            What = "Machine",
                            Which = (m.SerialNumber + "  " + m.ItemCode).Trim(),
                            Change = Describe(diff, link.TakenSnapshot, nowSnap),
                            TakenAt = link.TakenAt,
                            LinkKey = link.LinkKey,
                            NewSnapshot = nowSnap
                        });
                    }
                }

                foreach (ScpRemoteMeter mt in m.Meters)
                {
                    seenMeters.Add(mt.ItemMeterKey);
                    ScpInterBillLink ml;
                    if (!meterBySource.TryGetValue(mt.ItemMeterKey, out ml))
                    {
                        Add(t, new ChangeRow
                        {
                            RootContractKey = contractLink.LocalKey,
                            ContractNo = myNo,
                            Kind = "METER_ADDED",
                            What = "Counter",
                            Which = m.SerialNumber + "  /  " + mt.MeterTypeCode,
                            Change = "they added this counter to the machine",
                            TakenAt = contractLink.TakenAt,
                            SourceKey = mt.ItemMeterKey,
                            LocalParentKey = link.LocalKey,
                            Meter = mt
                        });
                    }
                }
            }

            // Anything linked here that is no longer over there at all.
            foreach (ScpInterBillLink l in mine)
            {
                if (l.LocalKey <= 0 || l.Status != ScpInterBillLink.LIVE) continue;
                if (l.EntityType == ScpInterBillLink.ITEM && !seenItems.Contains(l.SourceKey))
                {
                    Add(t, new ChangeRow
                    {
                        RootContractKey = contractLink.LocalKey,
                        ContractNo = myNo,
                        Kind = "MACHINE_GONE",
                        What = "Machine",
                        Which = l.SourceRef,
                        Change = "this machine is no longer on their contract",
                        TakenAt = l.TakenAt,
                        LinkKey = l.LinkKey,
                        NewSnapshot = l.TakenSnapshot
                    });
                }
                else if (l.EntityType == ScpInterBillLink.METER && !seenMeters.Contains(l.SourceKey))
                {
                    Add(t, new ChangeRow
                    {
                        RootContractKey = contractLink.LocalKey,
                        ContractNo = myNo,
                        Kind = "METER_GONE",
                        What = "Counter",
                        Which = l.SourceRef,
                        Change = "this counter is no longer on their machine -- no more readings will come for it",
                        TakenAt = l.TakenAt,
                        LinkKey = l.LinkKey,
                        NewSnapshot = l.TakenSnapshot
                    });
                }
            }
        }

        private void Add(DataTable t, ChangeRow c)
        {
            _changes.Add(c);
            DataRow r = t.NewRow();
            r["ContractNo"] = c.ContractNo;
            r["What"] = c.What;
            r["Which"] = c.Which;
            r["Change"] = c.Change;
            r["TakenAt"] = c.TakenAt;
            t.Rows.Add(r);
        }

        /// <summary>Turns "these named values differ" into a sentence. The names are internal; what
        /// goes on screen is what a person would say.</summary>
        private static string Describe(List<string> fields, string was, string now)
        {
            Dictionary<string, string> a = ScpInterBillSnapshot.Parse(was);
            Dictionary<string, string> b = ScpInterBillSnapshot.Parse(now);
            List<string> said = new List<string>();
            foreach (string f in fields)
            {
                string before = ScpInterBillSnapshot.Get(a, f);
                string after = ScpInterBillSnapshot.Get(b, f);
                string word = f;
                if (f == "SERIAL") word = "serial no";
                else if (f == "MODEL") word = "model";
                else if (f == "DESC") word = "description";
                else if (f == "ACTIVE") word = after == "N" ? "withdrawn over there" : "back in use over there";
                else if (f == "EXPIRY") word = "end date";
                else if (f == "NO") word = "their contract number";
                else if (f == "DEBTOR") word = "who they bill";
                else if (f == "TYPE") word = "counter type";
                else if (f == "ROLE") word = "what the counter is for";

                said.Add(f == "ACTIVE"
                    ? word
                    : word + ": " + (before.Length > 0 ? before : "(blank)") + "  ->  " +
                      (after.Length > 0 ? after : "(blank)"));
            }
            return string.Join(";   ", said.ToArray());
        }

        private ChangeRow FocusedChange()
        {
            int i = GridViewChanges.GetDataSourceRowIndex(GridViewChanges.FocusedRowHandle);
            return i >= 0 && i < _changes.Count ? _changes[i] : null;
        }

        private void OnAcceptChange(object sender, EventArgs e)
        {
            ChangeRow c = FocusedChange();
            if (c == null) return;

            if (c.Kind == "MACHINE_ADDED")
            {
                if (XtraMessageBox.Show(
                        "Put " + c.Which + " on " + c.ContractNo + " too?" + Environment.NewLine +
                        Environment.NewLine +
                        "It arrives at their price plus this book's margin, like every machine taken " +
                        "from them -- check it in Meters & Pricing before the next run.",
                        "Inter-Billing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                ScpTakeResult r = ScpInterBillTake.AddMachine(
                    _dbSetting, _book, _remoteCs, c.RootContractKey, c.Machine, _book.MarginPercent);
                Report(r, c.Which + " added to " + c.ContractNo + ".");
            }
            else if (c.Kind == "METER_ADDED")
            {
                ScpTakeResult r = ScpInterBillTake.AddMeter(
                    _dbSetting, _book, _remoteCs, c.RootContractKey, c.LocalParentKey, c.Meter,
                    _book.MarginPercent);
                Report(r, "Counter added. Check its price in Meters & Pricing.");
            }
            else if (c.Kind == "MACHINE_GONE")
            {
                if (XtraMessageBox.Show(
                        "Stop billing " + c.Which + " on " + c.ContractNo + "?" + Environment.NewLine +
                        Environment.NewLine +
                        "The machine stays on your contract with everything it has billed so far. It " +
                        "is marked inactive, so this month's run leaves it out.",
                        "Inter-Billing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                try
                {
                    RetireLocalItem(c.LinkKey);
                    ScpInterBillLinks.Notice(_dbSetting, c.LinkKey, ScpInterBillLink.GONE,
                        "off hire in " + _book.Alias);
                    XtraMessageBox.Show("Marked inactive on " + c.ContractNo + ".", "Inter-Billing",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Could not do it:" + Environment.NewLine + ex.Message, "Error");
                }
            }
            else if (c.Kind == "METER_GONE" || c.Kind == "CONTRACT_GONE")
            {
                // Nothing is destroyed here. What this book already billed is real, and a counter
                // removed over there only means no more readings will arrive for it.
                ScpInterBillLinks.Notice(_dbSetting, c.LinkKey, ScpInterBillLink.GONE,
                    "no longer in " + _book.Alias);
                XtraMessageBox.Show(
                    "Noted. Nothing on your contract was changed -- what it has billed is yours, and " +
                    "no more readings will come for it.",
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ScpInterBillLinks.Accept(_dbSetting, c.LinkKey, c.NewSnapshot);
                XtraMessageBox.Show("Taken. It will not be reported again unless they change it once more.",
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            LoadChanges();
        }

        private void RetireLocalItem(long linkKey)
        {
            if (linkKey <= 0) return;
            _dbSetting.ExecuteNonQuery(
                "UPDATE i SET i.Inactive = 'Y', i.LastModified = GETDATE() " +
                "  FROM dbo.zSCP2_Item i " +
                "  JOIN dbo.zSCP2_InterBillLink l ON l.LocalKey = i.ItemKey AND l.EntityType = 'ITEM' " +
                " WHERE l.LinkKey = " + linkKey);
        }

        private void Report(ScpTakeResult r, string good)
        {
            if (r != null && r.Error.Length == 0)
                XtraMessageBox.Show(good, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                XtraMessageBox.Show("Nothing was changed:" + Environment.NewLine + Environment.NewLine +
                    (r == null ? "" : r.Error), "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnDismissChange(object sender, EventArgs e)
        {
            ChangeRow c = FocusedChange();
            if (c == null) return;

            if (c.Kind == "MACHINE_ADDED" || c.Kind == "METER_ADDED")
            {
                ScpInterBillLinks.Decline(_dbSetting, _book,
                    c.Kind == "MACHINE_ADDED" ? ScpInterBillLink.ITEM : ScpInterBillLink.METER,
                    c.RootContractKey, c.SourceKey, c.Which, "not wanted on " + c.ContractNo);
            }
            else if (c.LinkKey > 0)
            {
                // Accepting the snapshot without touching the contract IS "leave mine as it is": the
                // difference stops being news, and nothing here moved.
                ScpInterBillLinks.Accept(_dbSetting, c.LinkKey, c.NewSnapshot);
            }
            LoadChanges();
        }

        // ---------------------------------------------------------------- 3. their readings

        private static DataTable MakeReadingTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("SerialNumber", typeof(string));
            t.Columns.Add("Counter", typeof(string));
            t.Columns.Add("TheirReading", typeof(decimal));
            t.Columns.Add("ReadingDate", typeof(DateTime));
            t.Columns.Add("Status", typeof(string));
            return t;
        }

        /// <summary>The margin to add to their prices for the take about to happen.</summary>
        private decimal Margin
        {
            get { try { return Convert.ToDecimal(SpnMargin.EditValue); } catch { return 0m; } }
        }

        private int PeriodYear
        {
            get { try { return Convert.ToInt32(SpnYear.EditValue); } catch { return DateTime.Today.Year; } }
        }

        private int PeriodMonth
        {
            get { try { return Convert.ToInt32(SpnMonth.EditValue); } catch { return DateTime.Today.Month; } }
        }

        private void OnLookReadings(object sender, EventArgs e) { LoadReadings(false); }

        private void OnBringReadings(object sender, EventArgs e) { LoadReadings(true); }

        /// <summary>
        /// Their readings for the period, beside what this book has.
        ///
        /// <para>Readings are the one thing that always follows. They were never this book's: the
        /// counter is on their machine, they read it, and a billing book that argued with the number
        /// would be inventing usage. The only thing it will not overwrite is a reading it has
        /// already invoiced -- that one has been sent to a customer.</para>
        /// </summary>
        private void LoadReadings(bool bring)
        {
            DataTable t = MakeReadingTable();
            _reads = new List<ReadRow>();
            if (_book == null || !_book.IsUsable) { Bind(ref _dtReadings, GridReadings, t); return; }

            int year = PeriodYear;
            int month = PeriodMonth;

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            int brought = 0, tooLate = 0;
            int held = 0;
            try
            {
                List<ScpInterBillLink> meterLinks = ScpInterBillLinks.OfType(
                    _dbSetting, _book.RemoteBookId, ScpInterBillLink.METER, false);
                if (meterLinks.Count == 0)
                {
                    Bind(ref _dtReadings, GridReadings, t);
                    return;
                }

                Dictionary<long, long> localBySource = new Dictionary<long, long>();
                List<long> sourceKeys = new List<long>();
                List<long> localKeys = new List<long>();
                foreach (ScpInterBillLink l in meterLinks)
                {
                    if (l.Status != ScpInterBillLink.LIVE) continue;
                    localBySource[l.SourceKey] = l.LocalKey;
                    sourceKeys.Add(l.SourceKey);
                    localKeys.Add(l.LocalKey);
                }

                Dictionary<long, string[]> where = LocalMeterInfo(localKeys);
                Dictionary<long, object[]> hereNow = LocalEntries(localKeys, year, month);

                List<ScpRemoteReading> theirs = ScpInterBillReader.Readings(_remoteCs, sourceKeys, year, month);

                foreach (ScpRemoteReading x in theirs)
                {
                    long localKey;
                    if (!localBySource.TryGetValue(x.ItemMeterKey, out localKey)) continue;

                    ReadRow row = new ReadRow();
                    row.LocalMeterKey = localKey;
                    string[] info;
                    if (where.TryGetValue(localKey, out info))
                    {
                        row.ContractNo = info[0];
                        row.SerialNumber = info[1];
                        row.Counter = info[2];
                    }
                    row.TheirReading = x.CurrentReading;
                    row.TheirBilledReading = x.BilledReading;
                    row.CorrectionDocNo = x.CorrectionDocNo;
                    row.ReadingDate = x.ReadingDate;

                    object[] mine;
                    bool haveMine = hereNow.TryGetValue(localKey, out mine);
                    decimal myReading = haveMine ? Convert.ToDecimal(mine[0]) : 0m;
                    bool invoiced = haveMine && Convert.ToString(mine[1]).ToUpperInvariant() == "Y";
                    string myInvNo = haveMine && mine.Length > 2 ? Convert.ToString(mine[2]).Trim() : "";

                    // "They corrected it" is worth saying in its own words. A reading that has been
                    // revised by a credit note over there is not a reading that merely differs --
                    // somebody looked at it, decided it was wrong, and gave money back on it.
                    string cn = x.Corrected
                        ? "they corrected this (" + x.CorrectionDocNo + "): billed " +
                          x.BilledReading.ToString("#,##0") + ", now " + x.CurrentReading.ToString("#,##0")
                        : "";

                    if (invoiced)
                    {
                        if (x.Corrected && myReading != x.CurrentReading)
                        {
                            // The only combination nothing here can put right. This book has already
                            // billed the end customer on a figure the other book has since withdrawn,
                            // and an invoice that has gone out is not something a reading can reach
                            // back and change. It takes a credit note of this book's own -- so say
                            // exactly that, instead of "left alone".
                            row.Status = "THEY CORRECTED IT AFTER YOU BILLED - " + cn +
                                         "; you invoiced at " + myReading.ToString("#,##0") +
                                         (myInvNo.Length > 0 ? " on " + myInvNo : "") +
                                         ". Correct your own invoice with a credit note.";
                            row.CorrectedTooLate = true;
                            tooLate++;
                        }
                        else
                            row.Status = "already invoiced here at " + myReading.ToString("#,##0") +
                                         (myInvNo.Length > 0 ? " on " + myInvNo : "") + " - left alone";
                        row.CanBring = false;
                        held++;
                    }
                    else if (haveMine && myReading == x.CurrentReading)
                    {
                        row.Status = x.Corrected ? "same here already (" + cn + ")" : "same here already";
                        row.CanBring = false;
                    }
                    else
                    {
                        row.Status = haveMine
                            ? "here it says " + myReading.ToString("#,##0") + " - theirs replaces it"
                            : "not here yet";
                        if (x.Corrected) row.Status += "  [" + cn + "]";
                        row.CanBring = true;
                    }

                    _reads.Add(row);
                }

                if (bring)
                {
                    foreach (ReadRow row in _reads)
                    {
                        if (!row.CanBring) continue;
                        // Count what was actually written, not what was offered: the row was
                        // judged when the screen loaded, and the period can have been invoiced
                        // in between.
                        bool wrote = WriteEntry(row.LocalMeterKey, year, month, row.TheirReading, row.ReadingDate);
                        row.Status = wrote ? "brought over" : "invoiced here just now - left alone";
                        row.CanBring = false;
                        if (wrote) brought++; else held++;
                    }
                }

                foreach (ReadRow row in _reads)
                {
                    DataRow r = t.NewRow();
                    r["ContractNo"] = row.ContractNo;
                    r["SerialNumber"] = row.SerialNumber;
                    r["Counter"] = row.Counter;
                    r["TheirReading"] = row.TheirReading;
                    if (row.ReadingDate.HasValue) r["ReadingDate"] = row.ReadingDate.Value;
                    r["Status"] = row.Status;
                    t.Rows.Add(r);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    "Could not read from " + _book.Display + ":" + Environment.NewLine +
                    Environment.NewLine + ex.Message,
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { this.Cursor = old; }

            Bind(ref _dtReadings, GridReadings, t);

            if (bring)
            {
                XtraMessageBox.Show(
                    brought + " readings brought over for " + month + "/" + year + "." +
                    (held > 0
                        ? Environment.NewLine + Environment.NewLine + held +
                          " were left alone because this book has already invoiced them."
                        : "") +
                    (tooLate > 0
                        ? Environment.NewLine + Environment.NewLine + tooLate +
                          " of those they have since CORRECTED with a credit note of their own." +
                          Environment.NewLine +
                          "Your invoice went out on the old figure. Correct it with a credit note " +
                          "here -- see the rows marked \"THEY CORRECTED IT AFTER YOU BILLED\"."
                        : ""),
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>Contract number, serial and counter name for local counters -- what a person needs
        /// to recognise the row. One query for all of them.</summary>
        private Dictionary<long, string[]> LocalMeterInfo(List<long> localMeterKeys)
        {
            Dictionary<long, string[]> map = new Dictionary<long, string[]>();
            string inList = KeyList(localMeterKeys);
            if (inList.Length == 0) return map;
            try
            {
                DataTable t = _dbSetting.GetDataTable(
                    "SELECT m.ItemMeterKey, c.ContractNo, ISNULL(i.SerialNumber,'') AS SerialNumber, " +
                    "       ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, ISNULL(m.[Description],'') AS Descr " +
                    "  FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE m.ItemMeterKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                {
                    string name = Convert.ToString(r["Descr"]).Trim();
                    if (name.Length == 0) name = Convert.ToString(r["MeterTypeCode"]).Trim();
                    map[Convert.ToInt64(r["ItemMeterKey"])] = new string[]
                    {
                        Convert.ToString(r["ContractNo"]).Trim(),
                        Convert.ToString(r["SerialNumber"]).Trim(),
                        name
                    };
                }
            }
            catch { }
            return map;
        }

        private Dictionary<long, object[]> LocalEntries(List<long> localMeterKeys, int year, int month)
        {
            Dictionary<long, object[]> map = new Dictionary<long, object[]>();
            string inList = KeyList(localMeterKeys);
            if (inList.Length == 0) return map;
            try
            {
                DataTable t = _dbSetting.GetDataTable(
                    // "Has this period been invoiced here?" is answered by InvoicedDocKey, not by
                    // the Invoiced flag beside it. Nothing in the product ever sets that flag to
                    // 'Y' -- the generator stamps the DOCUMENT (InvoicedDocKey / DocNo / At) and
                    // leaves the flag at 'N' -- so every test against it came out false and this
                    // screen happily wrote a new reading over a month it had already billed.
                    // The key is also self-correcting: deleting or cancelling the invoice clears
                    // it (ReconcileDeletedInvoices), which is exactly when the period becomes
                    // open again.
                    "SELECT ItemMeterKey, ISNULL(CurrentReading,0) AS CurrentReading, " +
                    "       CASE WHEN InvoicedDocKey IS NULL THEN 'N' ELSE 'Y' END AS Invoiced, " +
                    "       ISNULL(InvoicedDocNo,'') AS InvoicedDocNo FROM dbo.zSCP2_MeterEntry " +
                    " WHERE PeriodYear = " + year + " AND PeriodMonth = " + month +
                    "   AND ItemMeterKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                    map[Convert.ToInt64(r["ItemMeterKey"])] =
                        new object[] { r["CurrentReading"], r["Invoiced"], r["InvoicedDocNo"] };
            }
            catch { }
            return map;
        }

        /// <summary>Stages one of their readings against this book's counter. Returns false when
        /// the period has been invoiced here in the meantime -- the row is left exactly as it was
        /// billed, and the caller says so rather than counting a write that did not happen.</summary>
        private bool WriteEntry(long localMeterKey, int year, int month, decimal reading, DateTime? on)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@v, ReadingDate=@d, Source='INTERBILL', " +
                    " LastModified=GETDATE() " +
                    // Same fact as the screen asks: the DOCUMENT, not the unmaintained flag.
                    " WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m " +
                    "   AND InvoicedDocKey IS NULL; " +
                    "IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry " +
                    "     WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m) " +
                    "INSERT INTO dbo.zSCP2_MeterEntry " +
                    " (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, " +
                    "  Invoiced, LastModified) " +
                    "VALUES (@k, @y, @m, @v, @d, 'INTERBILL', 'N', GETDATE())", cn))
                {
                    cmd.Parameters.AddWithValue("@k", localMeterKey);
                    cmd.Parameters.AddWithValue("@y", year);
                    cmd.Parameters.AddWithValue("@m", month);
                    cmd.Parameters.AddWithValue("@v", reading);
                    cmd.Parameters.AddWithValue("@d", on.HasValue ? (object)on.Value : DateTime.Today);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private static string KeyList(List<long> keys)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (long k in keys)
            {
                if (k <= 0) continue;
                if (sb.Length > 0) sb.Append(",");
                sb.Append(k);
            }
            return sb.ToString();
        }


        // ---------------------------------------------------------------- 4. bill one contract

        private static DataTable MakeBillableTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("Machines", typeof(int));
            t.Columns.Add("Readings", typeof(string));
            t.Columns.Add("Invoiced", typeof(string));
            return t;
        }

        private int BillYear
        {
            get { try { return Convert.ToInt32(SpnBillYear.EditValue); } catch { return DateTime.Today.Year; } }
        }

        private int BillMonth
        {
            get { try { return Convert.ToInt32(SpnBillMonth.EditValue); } catch { return DateTime.Today.Month; } }
        }

        /// <summary>
        /// The contracts this book has taken, with enough about the chosen period to see whether one
        /// is ready to bill: how many of its counters have a reading, and what has already been
        /// invoiced.
        /// </summary>
        private void LoadBillable()
        {
            DataTable t = MakeBillableTable();
            _billableKeys = new List<long>();
            if (_book == null) { Bind(ref _dtBillable, GridBill, t); return; }

            int year = BillYear, month = BillMonth;
            foreach (ScpInterBillLink cl in ScpInterBillLinks.OfType(
                         _dbSetting, _book.RemoteBookId, ScpInterBillLink.CONTRACT, false))
            {
                string no = ContractNoOf(cl.LocalKey);
                if (no.Length == 0) continue;

                DataRow r = t.NewRow();
                r["ContractNo"] = no;
                r["Customer"] = CustomerOf(cl.LocalKey);
                r["Machines"] = (int)Scalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = " + cl.LocalKey +
                    " AND ISNULL(Inactive,'N') = 'N'");

                decimal usage = Scalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    " WHERE i.ContractKey = " + cl.LocalKey + " AND m.MeterRole IN ('BK','CL')");
                decimal got = Scalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
                    "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    " WHERE i.ContractKey = " + cl.LocalKey +
                    "   AND e.PeriodYear = " + year + " AND e.PeriodMonth = " + month +
                    "   AND ISNULL(e.CurrentReading,0) > 0");
                // The readings are the thing that stops a run, so the count says both halves: what
                // arrived and what was expected. "0 of 6" is a sentence; "0" is a puzzle.
                r["Readings"] = got.ToString("#,##0") + " of " + usage.ToString("#,##0") +
                                (got == 0m && usage > 0m ? "   -  bring them over first" : "");

                DataTable inv = null;
                try
                {
                    inv = _dbSetting.GetDataTable(
                        "SELECT DISTINCT ISNULL(e.InvoicedDocNo,'') AS DocNo FROM dbo.zSCP2_MeterEntry e " +
                        "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                        "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                        " WHERE i.ContractKey = " + cl.LocalKey +
                        "   AND e.PeriodYear = " + year + " AND e.PeriodMonth = " + month +
                        "   AND ISNULL(e.InvoicedDocNo,'') <> ''", false);
                }
                catch { }
                List<string> docs = new List<string>();
                if (inv != null)
                    foreach (DataRow ir in inv.Rows)
                    {
                        string d = Convert.ToString(ir["DocNo"]).Trim();
                        if (d.Length > 0 && !docs.Contains(d)) docs.Add(d);
                    }
                r["Invoiced"] = docs.Count == 0 ? "" : string.Join(", ", docs.ToArray());

                t.Rows.Add(r);
                _billableKeys.Add(cl.LocalKey);
            }
            Bind(ref _dtBillable, GridBill, t);
        }

        private string CustomerOf(long contractKey)
        {
            try
            {
                object o = _dbSetting.ExecuteScalar(
                    "SELECT c.DebtorCode + ISNULL('  ' + d.CompanyName, '') FROM dbo.zSCP2_Contract c " +
                    "  LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode WHERE c.ContractKey = " + contractKey);
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            }
            catch { return ""; }
        }

        private decimal Scalar(string sql)
        {
            try
            {
                object o = _dbSetting.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
            }
            catch { return 0m; }
        }

        private long FocusedBillable()
        {
            int i = GridViewBill.GetDataSourceRowIndex(GridViewBill.FocusedRowHandle);
            return i >= 0 && i < _billableKeys.Count ? _billableKeys[i] : 0L;
        }

        /// <summary>
        /// The jobs for ONE contract, built by the same code the Meter Reading Integration screen
        /// uses -- same rows, same rules, same money. This screen supplies only two things that
        /// screen supplies differently: WHICH contract, and that every row of it counts.
        /// </summary>
        private List<MeterInvoiceGenerator.InvoiceJob> JobsForFocused(out string periodName)
        {
            periodName = new System.Globalization.CultureInfo("en-US")
                             .DateTimeFormat.GetMonthName(BillMonth) + " " + BillYear;

            long ck = FocusedBillable();
            if (ck <= 0)
            {
                XtraMessageBox.Show("Pick the contract to bill.", "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // A usage counter with no reading for the period is not "zero copies" -- it is a reading
            // that has not been fetched yet, and the two bill completely differently. With no reading
            // every usage line is nothing, so a committed minimum bills its whole shortfall and the
            // invoice comes out looking finished when it is empty.
            //
            // The readings tab already said so in a column and this screen let the button be pressed
            // anyway. It does not any more.
            List<string> missing = CountersWithoutReadings(ck, BillYear, BillMonth);
            if (missing.Count > 0)
            {
                XtraMessageBox.Show(
                    missing.Count + " counter" + (missing.Count == 1 ? " has" : "s have") +
                    " no reading for " + periodName + ":" + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, missing.ToArray()) + Environment.NewLine +
                    Environment.NewLine +
                    "Bring them over on \"3. Their readings\" first." + Environment.NewLine +
                    Environment.NewLine +
                    "Billing now would charge every one of them as nothing printed -- which bills any " +
                    "committed minimum in full, and looks like a finished invoice.",
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                Dictionary<string, List<decimal[]>> ladders;
                DataTable rows = ScpBillingRows.ForContract(_dbSetting, ck, BillYear, BillMonth, out ladders);

                // Everything on the contract is billed. There is no ticking here and there should not
                // be: this screen bills a whole contract or nothing, which is why it cannot produce
                // the half-billed customer the other screen has guards against.
                List<DataRow> all = new List<DataRow>();
                foreach (DataRow r in rows.Rows) { r["Sel"] = true; all.Add(r); }

                string grpMode = ScpBillingRows.GroupingMode(_dbSetting);
                int alreadyInvoiced;
                Dictionary<long, string> snapshots;
                string blockTitle, blockMessage;
                Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs = ScpInvoiceJobs.Build(
                    _dbSetting, rows, all, ladders, BillYear, BillMonth, grpMode,
                    out alreadyInvoiced, out snapshots, out blockTitle, out blockMessage);

                if (jobs == null)
                {
                    XtraMessageBox.Show(blockMessage, blockTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                _lastSnapshots = snapshots;

                if (jobs.Count == 0)
                {
                    XtraMessageBox.Show(
                        alreadyInvoiced > 0
                            ? "Everything on this contract is already invoiced for " + periodName + "."
                            : "Nothing to bill for " + periodName + "." + Environment.NewLine +
                              Environment.NewLine +
                              "If the readings have not been brought over yet, do that on the " +
                              "readings tab first.",
                        "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
                return new List<MeterInvoiceGenerator.InvoiceJob>(jobs.Values);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not work out what to bill:" + Environment.NewLine +
                    Environment.NewLine + ex.Message, "Inter-Billing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            finally { this.Cursor = old; }
        }


        /// <summary>The contract's usage counters that have no reading for the period, named the way
        /// somebody would look for them. Empty means it can be billed.</summary>
        private List<string> CountersWithoutReadings(long contractKey, int year, int month)
        {
            List<string> missing = new List<string>();
            try
            {
                DataTable t = _dbSetting.GetDataTable(
                    "SELECT i.ServiceItemNo, ISNULL(i.SerialNumber,'') AS SerialNumber, " +
                    "       ISNULL(m.MeterTypeCode,'') AS MeterTypeCode " +
                    "  FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    " WHERE i.ContractKey = " + contractKey +
                    "   AND ISNULL(i.Inactive,'N') = 'N' AND m.MeterRole IN ('BK','CL') " +
                    "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e " +
                    "                    WHERE e.ItemMeterKey = m.ItemMeterKey " +
                    "                      AND e.PeriodYear = " + year + " AND e.PeriodMonth = " + month +
                    "                      AND ISNULL(e.CurrentReading,0) > 0) " +
                    " ORDER BY i.Pos, m.ItemMeterKey", false);
                foreach (DataRow r in t.Rows)
                {
                    if (missing.Count >= 12) { missing.Add("   ..."); break; }
                    missing.Add("   " + Convert.ToString(r["ServiceItemNo"]).Trim() + "   " +
                                Convert.ToString(r["SerialNumber"]).Trim() + "   " +
                                Convert.ToString(r["MeterTypeCode"]).Trim());
                }
            }
            catch { }
            return missing;
        }

        private Dictionary<long, string> _lastSnapshots;

        private void OnPreviewInvoice(object sender, EventArgs e)
        {
            string periodName;
            List<MeterInvoiceGenerator.InvoiceJob> jobs = JobsForFocused(out periodName);
            if (jobs == null) return;

            // The same preview the other screen shows, computed by the same fold. Nothing is written.
            using (MeterInvoicePreview_Form pv = new MeterInvoicePreview_Form(
                       _dbSetting, jobs, periodName,
                       "Preview only -- nothing is saved until you press Generate invoice."))
            {
                pv.ShowDialog(this);
            }
        }

        private void OnGenerateInvoice(object sender, EventArgs e)
        {
            string periodName;
            List<MeterInvoiceGenerator.InvoiceJob> jobs = JobsForFocused(out periodName);
            if (jobs == null) return;

            using (MeterInvoicePreview_Form pv = new MeterInvoicePreview_Form(
                       _dbSetting, jobs, periodName,
                       "They are saved automatically -- no clicking through each one."))
            {
                if (pv.ShowDialog(this) != DialogResult.OK) return;
                jobs = pv.Approved;      // a blocked customer is skipped, the rest still generate
            }
            if (jobs.Count == 0) return;

            using (MeterInvoiceGenerateProgress_Form dlg = new MeterInvoiceGenerateProgress_Form(
                       _dbSetting, jobs, DateTime.Today, DateTime.Now,
                       BillYear, BillMonth, _lastSnapshots))
            {
                dlg.ShowDialog(this);
            }
            LoadBillable();
        }

        private void Bind(ref DataTable slot, DevExpress.XtraGrid.GridControl grid, DataTable t)
        {
            slot = t;
            _loading = true;
            try { grid.DataSource = t; }
            finally { _loading = false; }
        }
    }
}
