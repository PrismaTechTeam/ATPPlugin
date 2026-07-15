using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Combined Service Contract editor (module v2). One customer per contract; its machines
    /// (service items) are managed inline via a grid + child dialog. Billing Day (default) and
    /// Billing Mode (grouped one invoice vs separate per item) drive the Meter Reading billing run.
    /// </summary>
    public partial class zSCP2_Contract_Form : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        private readonly DBSetting _db;
        private long _contractKey;
        private bool _isNew;
        private bool _modeGuard;
        private bool _loading;
        private bool _dirty;       // unsaved-changes flag; drives the close confirmation (see CLAUDE.md rule 8)
        private bool _savedOk;     // set true after a successful save so closing skips the confirmation
        private readonly List<ItemEditData> _items = new List<ItemEditData>();
        // Items detached from this contract THIS session (still ContractKey=this in DB until save) —
        // made available again in the attach picker / inline lookup so "Remove then re-pick" works now.
        private readonly List<long> _detachedThisSession = new List<long>();
        private DataTable _orphanLookup;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _orphanRepo;
        private DataTable _debtorLookup;
        private DataTable _dtItemsView;

        public zSCP2_Contract_Form()
        {
            InitializeComponent();
        }

        public zSCP2_Contract_Form(DBSetting db) : this()
        {
            _db = db;
            _isNew = true;
            this.Load += new EventHandler(OnFormLoad);
        }

        public zSCP2_Contract_Form(DBSetting db, long contractKey) : this()
        {
            _db = db;
            _contractKey = contractKey;
            _isNew = false;
            this.Load += new EventHandler(OnFormLoad);
        }

        // Clone-as-new ("Copy to a new Service Contract"): a NEW contract pre-filled from sourceKey.
        private long _cloneFromKey;
        public zSCP2_Contract_Form(DBSetting db, long sourceKey, bool cloneAsNew) : this()
        {
            _db = db;
            _isNew = true;
            _cloneFromKey = cloneAsNew ? sourceKey : 0;
            this.Load += new EventHandler(OnFormLoad);
        }

        private long PickContract(DataTable src, string title)
        {
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, title, src, "ContractKey",
                new string[] { "ContractNo", "DebtorCode", "CompanyName" },
                new string[] { "Contract No", "Customer", "Company Name" },
                new int[] { 110, 90, 260 });
            long v; return (k != null && k != DBNull.Value && long.TryParse(k.ToString(), out v)) ? v : 0;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null) return;
            ApplyToolbarIcons();
            LoadCurrencyLabel();
            LoadDebtorLookup();
            LoadContractTypeLookup();
            LoadAgentLookup();
            LoadDeptProjectLookups();
            WireBillingModeRadios();
            LkDebtorCode.EditValueChanged += new EventHandler(OnDebtorChanged);

            if (_isNew)
            {
                AutoPickContractNo();
                DtContractDate.EditValue = DateTime.Today;
                SpnBillingDay.Value = PumsConfig.GetInt(_db, PumsConfig.KEY_DEFAULT_BILLING_DAY, PumsConfig.DEFAULT_BILLING_DAY_VALUE);
                string mode = PumsConfig.Get(_db, PumsConfig.KEY_DEFAULT_BILLING_MODE, PumsConfig.DEFAULT_BILLING_MODE_VALUE);
                SetBillingMode(mode);
                // Copy-to-a-new: pre-fill from the source contract (fresh copies of its items).
                if (_cloneFromKey > 0) LoadContractAsTemplate(_cloneFromKey);
            }
            else
            {
                LoadContract();
            }

            SetupInlineOrphanLookup();
            RebuildItemsView();
            SetupSparePartsGrid();
            LoadSpareParts();
            BuildMoreHeaderTab();
            LoadMoreHeader();

            // Dirty tracking for the close confirmation: any header edit marks the form dirty. Wired
            // AFTER the initial load so loading an existing contract does not itself set the flag.
            WireDirtyTracking();
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnFormClosing);

            UpdateServiceItemButtons();
        }

        // Service items need a saved ContractKey to belong to, so Add / Edit / Attach / Remove are only
        // enabled in EDIT mode (an existing contract). In NEW mode the user must Save the header first.
        private void UpdateServiceItemButtons()
        {
            bool canEditItems = !_isNew;
            barAddItem.Enabled = canEditItems;
            barEditItem.Enabled = canEditItems;
            barDelItem.Enabled = canEditItems;
            if (BtnItemAttach != null) BtnItemAttach.Enabled = canEditItems;
            if (BtnItemDetach != null) BtnItemDetach.Enabled = canEditItems;
            DevExpress.XtraGrid.Columns.GridColumn col = GridViewItems.Columns.ColumnByFieldName("ServiceItemNo");
            if (col != null) col.OptionsColumn.AllowEdit = canEditItems;   // inline attach only in edit mode
            barDemoFill.Enabled = _isNew;   // demo random-fill only for a NEW contract
            LblItemsHint.Text = canEditItems
                ? ""
                : "Save the contract first — service items can be added after the contract is saved.";
            LblItemsHint.Visible = !canEditItems;
        }

        private void WireDirtyTracking()
        {
            EventHandler h = delegate { if (!_loading) _dirty = true; };
            TxtContractNo.EditValueChanged += h;
            SluContractType.EditValueChanged += h;
            LkDebtorCode.EditValueChanged += h;
            DtContractDate.EditValueChanged += h;
            DtStartDate.EditValueChanged += h;
            DtExpiryDate.EditValueChanged += h;
            SpnContractValue.EditValueChanged += h;
            SpnBillingDay.EditValueChanged += h;
            SluAgent.EditValueChanged += h;
            SluDept.EditValueChanged += h;
            SluProject.EditValueChanged += h;
            TxtAddress.EditValueChanged += h;
            TxtAttention.EditValueChanged += h;
            TxtPhone.EditValueChanged += h;
            TxtTerm.EditValueChanged += h;
            TxtArea.EditValueChanged += h;
            TxtDescription.EditValueChanged += h;
            TxtRemark1.EditValueChanged += h;
            TxtRemark2.EditValueChanged += h;
            TxtNote.EditValueChanged += h;
            ChkInactive.EditValueChanged += h;
        }

        // CLAUDE.md rule 8: mirror AutoCount's create/edit behaviour — closing with unsaved changes
        // prompts for confirmation. A successful Save clears the flag so it closes silently.
        private void OnFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (_savedOk || !_dirty) return;
            DialogResult r = XtraMessageBox.Show(
                "You have unsaved changes. Discard them and close?", "Unsaved Changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) e.Cancel = true;
        }

        private void LoadDebtorLookup()
        {
            try
            {
                _debtorLookup = _db.GetDataTable(
                    "SELECT AccNo, CompanyName, ISNULL(Address1,'') AS Address1, ISNULL(Address2,'') AS Address2, " +
                    "ISNULL(Address3,'') AS Address3, ISNULL(Address4,'') AS Address4, ISNULL(Attention,'') AS Attention, " +
                    "ISNULL(Phone1,'') AS Phone1, ISNULL(AreaCode,'') AS AreaCode, ISNULL(SalesAgent,'') AS SalesAgent, " +
                    "ISNULL(DisplayTerm,'') AS DisplayTerm, ISNULL(PostCode,'') AS PostCode, ISNULL(Fax1,'') AS Fax1, " +
                    "ISNULL(EmailAddress,'') AS EmailAddress, ISNULL(DeliverAddr1,'') AS DeliverAddr1, " +
                    "ISNULL(DeliverAddr2,'') AS DeliverAddr2, ISNULL(DeliverAddr3,'') AS DeliverAddr3, " +
                    "ISNULL(DeliverAddr4,'') AS DeliverAddr4, ISNULL(DeliverPostCode,'') AS DeliverPostCode " +
                    "FROM dbo.Debtor ORDER BY AccNo", false);
            }
            catch { _debtorLookup = new DataTable(); }
            LkDebtorCode.Properties.DataSource = _debtorLookup;
            LkDebtorCode.Properties.DisplayMember = "AccNo";
            LkDebtorCode.Properties.ValueMember = "AccNo";
            // SearchLookUpEdit shows its columns through the popup GridView — show only Code + Name,
            // and turn on the auto-filter row so the user can search (CLAUDE.md rule 9).
            LkDebtorView.Columns.Clear();
            LkDebtorView.PopulateColumns();
            foreach (DevExpress.XtraGrid.Columns.GridColumn c in LkDebtorView.Columns) c.Visible = false;
            ShowLkCol(LkDebtorView, "AccNo", "Code", 90, 0);
            ShowLkCol(LkDebtorView, "CompanyName", "Company Name", 240, 1);
            LkDebtorView.OptionsView.ShowAutoFilterRow = true;
        }

        private static void ShowLkCol(DevExpress.XtraGrid.Views.Grid.GridView v, string field, string caption, int width, int visibleIndex)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
        }

        // Contract Type dropdown: values come from Service Contract Type Maintenance
        // (zSCP_LK_ServiceContractType). Stores the CODE by value, so contracts already referencing a
        // type keep working even if the type's description changes (no hard FK; matched by code).
        private void LoadContractTypeLookup()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT ServiceContractTypeCode, Description FROM [dbo].[zSCP_LK_ServiceContractType] " +
                    "WHERE Inactive = 'N' ORDER BY ServiceContractTypeCode", false);
            }
            catch { dt = new DataTable(); }
            SluContractType.Properties.DataSource = dt;
            SluContractType.Properties.ValueMember = "ServiceContractTypeCode";
            SluContractType.Properties.DisplayMember = "ServiceContractTypeCode";
            // Popup shows the 2 datasource columns (Code + Description) auto-populated by the view.
        }

        // Agent dropdown: values come from AutoCount's Sales Agent Maintenance (dbo.SalesAgent).
        private void LoadAgentLookup()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT SalesAgent, ISNULL(Description,'') AS Description FROM [dbo].[SalesAgent] " +
                    "WHERE IsActive = 'T' ORDER BY SalesAgent", false);
            }
            catch { dt = new DataTable(); }
            SluAgent.Properties.DataSource = dt;
            SluAgent.Properties.ValueMember = "SalesAgent";
            SluAgent.Properties.DisplayMember = "SalesAgent";
            // Popup shows the 2 datasource columns (Agent + Description) auto-populated by the view.
        }

        // Department + Project dropdowns list AutoCount's own masters (Dept / Project tables).
        private void LoadDeptProjectLookups()
        {
            try
            {
                SluDept.Properties.DataSource = _db.GetDataTable("SELECT DeptNo, Description FROM [dbo].[Dept] ORDER BY DeptNo", false);
                SluDept.Properties.ValueMember = "DeptNo";
                SluDept.Properties.DisplayMember = "DeptNo";
            }
            catch { }
            try
            {
                SluProject.Properties.DataSource = _db.GetDataTable("SELECT ProjNo, Description FROM [dbo].[Project] ORDER BY ProjNo", false);
                SluProject.Properties.ValueMember = "ProjNo";
                SluProject.Properties.DisplayMember = "ProjNo";
            }
            catch { }
        }

        // "+" on the Department dropdown: open AutoCount's OWN new-Department form.
        private void SluDept_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(AutoCount.GeneralMaint.Project.ProjectType.Department, session);
                AutoCount.GeneralMaint.Project.DepartmentEntity entity = cmd.NewDepartment(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Department))
                    form.ShowDialog(this);
                LoadDeptProjectLookups();
                string code = entity.Row["DeptNo"] == null ? "" : entity.Row["DeptNo"].ToString().Trim();
                if (code.Length > 0) SluDept.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Department failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // "+" on the Project dropdown: open AutoCount's OWN new-Project form.
        private void SluProject_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(AutoCount.GeneralMaint.Project.ProjectType.Project, session);
                AutoCount.GeneralMaint.Project.ProjectEntity entity = cmd.NewProject(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Project))
                    form.ShowDialog(this);
                LoadDeptProjectLookups();
                string code = entity.Row["ProjNo"] == null ? "" : entity.Row["ProjNo"].ToString().Trim();
                if (code.Length > 0) SluProject.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Project failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // "+" on the Contract Type dropdown: open the Service Contract Type edit dialog to add a new
        // one, then reload the list and select it. (Same edit form as the Type Maintenance menu.)
        private void SluContractType_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form(_db, null))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadContractTypeLookup();
                        if (!string.IsNullOrEmpty(f.SavedCode)) SluContractType.EditValue = f.SavedCode;
                    }
                }
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Contract Type failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // When the customer changes interactively, auto-fill address/contact/term/area/agent from the
        // Debtor master — the same behaviour as AutoCount's Quotation/Invoice entry. Guarded by _loading
        // so loading an existing contract does not overwrite its saved values.
        private void OnDebtorChanged(object sender, EventArgs e)
        {
            if (_loading || _debtorLookup == null) return;
            string code = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (code.Length == 0) return;
            DataRow[] found = _debtorLookup.Select("AccNo='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow d = found[0];

            string addr = string.Join("\r\n", new string[] {
                AsStr(d["Address1"]), AsStr(d["Address2"]), AsStr(d["Address3"]), AsStr(d["Address4"]) })
                .Trim('\r', '\n');
            TxtAddress.Text = addr;
            TxtAttention.Text = AsStr(d["Attention"]);
            TxtPhone.Text = AsStr(d["Phone1"]);
            TxtTerm.Text = AsStr(d["DisplayTerm"]);
            TxtArea.Text = AsStr(d["AreaCode"]);
            SluAgent.EditValue = SetOrNull(AsStr(d["SalesAgent"]));

            // Map the debtor's info into the More Header tab too (only fields the Debtor master has —
            // AutoCount stores address as freeform Address1-4, so City/State/Country aren't derived).
            if (_mh != null && _mh.Count > 0)
            {
                MhSet("PostalCode", AsStr(d["PostCode"]));
                MhSet("Fax", AsStr(d["Fax1"]));
                // Delivery Address block from the debtor's delivery address.
                if (_mhDelAddress != null)
                    _mhDelAddress.Text = string.Join("\r\n", new string[] {
                        AsStr(d["DeliverAddr1"]), AsStr(d["DeliverAddr2"]), AsStr(d["DeliverAddr3"]), AsStr(d["DeliverAddr4"]) })
                        .Trim('\r', '\n');
                MhSet("DelPostalCode", AsStr(d["DeliverPostCode"]));
                MhSet("DelPhone", AsStr(d["Phone1"]));
                MhSet("DelFax", AsStr(d["Fax1"]));
                MhSet("DelEmail", AsStr(d["EmailAddress"]));
                MhSet("DelContactPerson", AsStr(d["Attention"]));
            }
        }

        // SearchLookUpEdit shows NullText when its value isn't in the list; empty string -> null so
        // the placeholder shows instead of a blank selected row.
        private static object SetOrNull(string code)
        {
            return string.IsNullOrEmpty(code) ? null : (object)code;
        }

        private void WireBillingModeRadios()
        {
            ChkBillGroup.CheckedChanged += new EventHandler(OnBillGroupChanged);
            ChkBillSeparate.CheckedChanged += new EventHandler(OnBillSeparateChanged);
        }

        private void OnBillGroupChanged(object sender, EventArgs e)
        {
            if (_modeGuard) return;
            _modeGuard = true;
            if (ChkBillGroup.Checked) ChkBillSeparate.Checked = false;
            else if (!ChkBillSeparate.Checked) ChkBillGroup.Checked = true; // keep one selected
            _modeGuard = false;
        }

        private void OnBillSeparateChanged(object sender, EventArgs e)
        {
            if (_modeGuard) return;
            _modeGuard = true;
            if (ChkBillSeparate.Checked) ChkBillGroup.Checked = false;
            else if (!ChkBillGroup.Checked) ChkBillSeparate.Checked = true;
            _modeGuard = false;
        }

        private void SetBillingMode(string mode)
        {
            _modeGuard = true;
            bool separate = string.Equals(mode, "S", StringComparison.OrdinalIgnoreCase);
            ChkBillSeparate.Checked = separate;
            ChkBillGroup.Checked = !separate;
            _modeGuard = false;
        }

        private string CurrentBillingMode()
        {
            return ChkBillSeparate.Checked ? "S" : "G";
        }

        private string _autoPeekNo;   // previewed auto number; if the box still shows it at save, a real one is drawn

        private void AutoPickContractNo()
        {
            // PREVIEW ONLY (peek, no consume) — clicking Auto never increments the counter. The real
            // number is reserved by ScpDocNo.Next() at save time (see BtnSave_Click).
            string peek = ServiceContractPhotocopier.Classes.ScpDocNo.Peek(
                _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_CONTRACT);
            if (string.IsNullOrEmpty(peek))
            {
                try
                {
                    object o = _db.ExecuteScalar(
                        "SELECT ISNULL(MAX(CONVERT(int, SUBSTRING(ContractNo,4,20))),0)+1 " +
                        "FROM [dbo].[zSCP2_Contract] WHERE ContractNo LIKE 'SC-%' AND ISNUMERIC(SUBSTRING(ContractNo,4,20))=1");
                    int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
                    peek = "SC-" + next.ToString("000000");
                }
                catch { peek = "SC-000001"; }
            }
            _autoPeekNo = peek;
            TxtContractNo.Text = peek;
        }

        private void BtnAutoNo_Click(object sender, EventArgs e) { AutoPickContractNo(); }

        // Real AutoCount toolbar icons (the exact ones AutoCount uses in its own detail grids), set in
        // code because the DevExpress ImageUri names were unreliable (some rendered empty). Icon-only
        // buttons are centered (MiddleCenter); icon+text buttons keep the icon on the left.
        private void ApplyToolbarIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));

                IconOnly(BtnSpInsert, img.GetSmallImage_Insert());
                IconOnly(BtnSpRemove, img.GetSmallImage_Delete());
                IconOnly(BtnSpUp, img.GetSmallImage_MoveUp());
                IconOnly(BtnSpDown, img.GetSmallImage_MoveDown());
                IconLeft(BtnItemAttach, img.GetSmallImage_Insert());
                IconLeft(BtnItemDetach, img.GetSmallImage_Delete());

                barCopyFrom.ImageOptions.Image = img.GetLargeImage_CopyFrom();
                barCopyToNew.ImageOptions.Image = img.GetLargeImage_New();
                barCopyWhole.ImageOptions.Image = img.GetSmallImage_CopyToClipboard();
                barCopySelected.ImageOptions.Image = img.GetSmallImage_CopySelectedToClipboard();
                barCopySpreadsheet.ImageOptions.Image = img.GetSmallImage_CopyAsSpreadsheet();   // #7: the missing icon
                barPasteWhole.ImageOptions.Image = img.GetSmallImage_PasteToClipboard();
                barPasteItems.ImageOptions.Image = img.GetSmallImage_PasteSelectedToClipoard();
                barDemoFill.ImageOptions.Image = img.GetLargeImage_Refresh();
            }
            catch { }   // icons are cosmetic — never block the form
        }

        private static void IconOnly(DevExpress.XtraEditors.SimpleButton b, System.Drawing.Image im)
        {
            b.Text = "";
            b.ImageOptions.Image = im;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
        }

        private static void IconLeft(DevExpress.XtraEditors.SimpleButton b, System.Drawing.Image im)
        {
            b.ImageOptions.Image = im;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        // Show the AutoCount home/default currency symbol (e.g. "RM") behind the Contract Value box.
        // Authoritative source: AutoCount stores it in dbo.Settings (Name='General') as JSON
        // (LocalCurrencySymbol / LocalCurrencyCode). Falls back to the rate=1.0 currency if unavailable.
        private void LoadCurrencyLabel()
        {
            try
            {
                object o = _db.ExecuteScalar(
                    "SELECT ISNULL(NULLIF(JSON_VALUE(Value,'$.LocalCurrencySymbol'),''), " +
                    "JSON_VALUE(Value,'$.LocalCurrencyCode')) FROM [dbo].[Settings] WHERE Name = 'General'");
                if (o == null || o == DBNull.Value || o.ToString().Trim().Length == 0)
                    o = _db.ExecuteScalar(
                        "SELECT TOP 1 ISNULL(NULLIF(LTRIM(RTRIM(CurrencySymbol)),''), CurrencyCode) " +
                        "FROM [dbo].[Currency] WHERE BankBuyRate = 1 ORDER BY CurrencyCode");
                LblCurrency.Text = (o == null || o == DBNull.Value) ? "" : o.ToString();
            }
            catch { LblCurrency.Text = ""; }
        }

        // "Last day of month": disable the day spin and use month-end (stored as day 31 -> the existing
        // month-end clamp yields the true last day for every month).
        private void ChkMonthEnd_CheckedChanged(object sender, EventArgs e)
        {
            SpnBillingDay.Enabled = !ChkMonthEnd.Checked;
            if (ChkMonthEnd.Checked) SpnBillingDay.Value = 31;
            if (!_loading) _dirty = true;
        }

        private void LoadContract()
        {
            _loading = true;
            try
            {
            DataTable dt = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + _contractKey, false);
            if (dt.Rows.Count == 0) { _isNew = true; AutoPickContractNo(); return; }
            DataRow r = dt.Rows[0];
            TxtContractNo.Text = AsStr(r["ContractNo"]);
            SluContractType.EditValue = SetOrNull(AsStr(r["ContractTypeCode"]));
            LkDebtorCode.EditValue = AsStr(r["DebtorCode"]);
            DtContractDate.EditValue = AsDate(r["ContractDate"]);
            DtStartDate.EditValue = AsDate(r["ServiceStartDate"]);
            DtExpiryDate.EditValue = AsDate(r["ServiceExpiryDate"]);
            SpnContractValue.Value = AsDec(r["ContractValue"]);
            SpnBillingDay.Value = AsInt(r["BillingDay"], 1);
            ChkMonthEnd.Checked = r.Table.Columns.Contains("BillOnMonthEnd") && AsStr(r["BillOnMonthEnd"]) == "Y";
            SpnBillingDay.Enabled = !ChkMonthEnd.Checked;
            SetBillingMode(AsStr(r["BillingMode"]));
            TxtAddress.Text = AsStr(r["Address1"]);
            TxtAttention.Text = AsStr(r["Attention"]);
            TxtPhone.Text = AsStr(r["Phone"]);
            TxtTerm.Text = AsStr(r["TermCode"]);
            TxtArea.Text = AsStr(r["AreaCode"]);
            if (r.Table.Columns.Contains("ReferenceNo")) TxtRefNo.Text = AsStr(r["ReferenceNo"]);
            SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
            SluDept.EditValue = r.Table.Columns.Contains("DeptNo") ? SetOrNull(AsStr(r["DeptNo"])) : null;
            SluProject.EditValue = r.Table.Columns.Contains("ProjNo") ? SetOrNull(AsStr(r["ProjNo"])) : null;
            TxtDescription.Text = AsStr(r["Description"]);
            TxtRemark1.Text = AsStr(r["Remark1"]);
            TxtRemark2.Text = AsStr(r["Remark2"]);
            TxtNote.Text = AsStr(r["Note"]);
            ChkInactive.Checked = AsStr(r["Inactive"]) == "Y";

            LoadItems();
            }
            finally { _loading = false; }
        }

        private void LoadItems()
        {
            _items.Clear();
            DataTable it = _db.GetDataTable(
                "SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=" + _contractKey + " ORDER BY Pos, ItemKey", false);
            foreach (DataRow r in it.Rows)
                _items.Add(LoadOneItem(Convert.ToInt64(r["ItemKey"])));
        }

        // Load a single service item (+ its meters + item codes) into an ItemEditData. Reused by the
        // "+" attach picker so an existing contract-less item can be pulled into this contract.
        private ItemEditData LoadOneItem(long itemKey)
        {
            ItemEditData d = new ItemEditData();
            DataTable it = _db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + itemKey, false);
            if (it.Rows.Count == 0) return d;
            DataRow r = it.Rows[0];
            d.ItemKey = itemKey;
            d.ServiceItemNo = AsStr(r["ServiceItemNo"]);
            d.SerialNumber = AsStr(r["SerialNumber"]);
            d.Description = AsStr(r["Description"]);
            d.BillingDayOverride = (r["BillingDayOverride"] == null || r["BillingDayOverride"] == DBNull.Value)
                ? (int?)null : Convert.ToInt32(r["BillingDayOverride"]);
            d.DepartmentCode = AsStr(r["DepartmentCode"]);
            d.JobCode = AsStr(r["JobCode"]);
            d.StockLocationCode = AsStr(r["StockLocationCode"]);
            d.Inactive = AsStr(r["Inactive"]) == "Y";
            d.ServiceExpiryDate = (r["ServiceExpiryDate"] == null || r["ServiceExpiryDate"] == DBNull.Value)
                ? (DateTime?)null : Convert.ToDateTime(r["ServiceExpiryDate"]);
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();

            DataTable m = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + itemKey + " ORDER BY ItemMeterKey", false);
            foreach (DataRow mr in m.Rows)
            {
                DataRow nr = d.Meters.NewRow();
                nr["MeterTypeCode"] = AsStr(mr["MeterTypeCode"]);
                nr["MeterRole"] = AsStr(mr["MeterRole"]);
                nr["MinimumCharges"] = AsDec(mr["MinimumCharges"]);
                nr["ChargesRate"] = AsDec(mr["ChargesRate"]);
                nr["MeterMultiPriceCode"] = AsStr(mr["MeterMultiPriceCode"]);
                nr["RebateQtyInPercent"] = AsDec(mr["RebateQtyInPercent"]);
                nr["FOCQty"] = AsDec(mr["FOCQty"]);
                nr["InitialReading"] = AsDec(mr["InitialReading"]);
                d.Meters.Rows.Add(nr);
            }
            d.Meters.AcceptChanges();

            DataTable ics = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=" + itemKey + " ORDER BY Pos, ItemCodeKey", false);
            foreach (DataRow ir in ics.Rows)
            {
                DataRow nr = d.ItemCodes.NewRow();
                nr["ItemCode"] = AsStr(ir["ItemCode"]);
                nr["Description"] = AsStr(ir["Description"]);
                nr["Qty"] = AsDec(ir["Qty"]);
                nr["SerialNumber"] = AsStr(ir["SerialNumber"]);
                d.ItemCodes.Rows.Add(nr);
            }
            d.ItemCodes.AcceptChanges();

            d.SpareParts = CreateSparePartsTable();
            DataTable sp = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=" + itemKey + " ORDER BY Pos", false);
            int spPos = 0;
            foreach (DataRow s in sp.Rows)
            {
                DataRow nr = d.SpareParts.NewRow();
                nr["SparePartKey"] = s["SparePartKey"]; nr["ItemKey"] = itemKey; nr["Bound"] = false;
                nr["No"] = ++spPos;
                nr["ItemCode"] = AsStr(s["ItemCode"]); nr["Description"] = AsStr(s["Description"]);
                nr["Unlimited"] = AsStr(s["Unlimited"]) == "Y"; nr["UOM"] = AsStr(s["UOM"]);
                nr["Quantity"] = AsDec(s["Quantity"]); nr["Discount"] = AsStr(s["Discount"]);
                nr["UnitPrice"] = AsDec(s["UnitPrice"]); nr["TaxType"] = AsStr(s["TaxType"]);
                nr["TaxInclusive"] = AsStr(s["TaxInclusive"]) == "Y"; nr["TaxRate"] = AsDec(s["TaxRate"]);
                nr["Pos"] = AsInt(s["Pos"], spPos);
                ComputeSpareRow(nr);
                d.SpareParts.Rows.Add(nr);
            }
            d.SpareParts.AcceptChanges();
            return d;
        }

        // "+" Attach: pick a service item that has NO contract (ContractKey IS NULL) and pull it into
        // this contract. On save it is re-parented (its ItemKey + meter history are preserved).
        // Orphan (contract-less) service items available to attach: DB orphans (ContractKey IS NULL)
        // plus any detached in this session, minus the ones already in this contract.
        private DataTable LoadOrphanItems()
        {
            string extra = _detachedThisSession.Count > 0
                ? " OR ItemKey IN (" + string.Join(",", _detachedThisSession) + ")" : "";
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT ItemKey, ServiceItemNo, SerialNumber, ISNULL(Description,'') AS Description " +
                    "FROM [dbo].[zSCP2_Item] WHERE ContractKey IS NULL" + extra + " ORDER BY ServiceItemNo", false);
            }
            catch { dt = new DataTable(); }
            // drop ones already attached in this contract
            System.Collections.Generic.HashSet<long> here = new System.Collections.Generic.HashSet<long>();
            foreach (ItemEditData d in _items) if (d.ItemKey > 0) here.Add(d.ItemKey);
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
                if (here.Contains(Convert.ToInt64(dt.Rows[i]["ItemKey"]))) dt.Rows.RemoveAt(i);
            return dt;
        }

        private void BtnItemAttach_Click(object sender, EventArgs e)
        {
            DataTable loose = LoadOrphanItems();
            if (loose.Rows.Count == 0)
            { XtraMessageBox.Show("There are no unattached service items (items with no contract) to attach.", "Nothing to attach"); return; }

            long key = PickLooseItem(loose);
            if (key == 0) return;
            AttachOrphan(key);
        }

        private void AttachOrphan(long itemKey)
        {
            foreach (ItemEditData ex2 in _items)
                if (ex2.ItemKey == itemKey) { XtraMessageBox.Show("That item is already in this contract.", "Already added"); return; }
            _items.Add(LoadOneItem(itemKey));
            _detachedThisSession.Remove(itemKey);
            _dirty = true;
            RebuildItemsView();
        }

        private long PickLooseItem(DataTable loose)
        {
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Attach Service Item (no contract)", loose, "ItemKey",
                new string[] { "ServiceItemNo", "SerialNumber", "Description" },
                new string[] { "Service Item No", "Serial Number", "Description" },
                new int[] { 130, 110, 220 });
            long v; return (k != null && k != DBNull.Value && long.TryParse(k.ToString(), out v)) ? v : 0;
        }

        // "-" Remove selected service item from the contract (detach — the item survives as
        // contract-less; existing items get ContractKey=NULL on save, new ones are just dropped).
        private void BtnItemDetach_Click(object sender, EventArgs e)
        {
            BtnDelItem_Click(sender, e);
        }

        private void RebuildItemsView()
        {
            _dtItemsView = new DataTable();
            _dtItemsView.Columns.Add("No", typeof(int));
            _dtItemsView.Columns.Add("ServiceItemNo", typeof(string));
            _dtItemsView.Columns.Add("SerialNumber", typeof(string));
            _dtItemsView.Columns.Add("Items", typeof(string));
            _dtItemsView.Columns.Add("BillingDay", typeof(string));
            _dtItemsView.Columns.Add("BKMeter", typeof(string));
            _dtItemsView.Columns.Add("CLMeter", typeof(string));
            _dtItemsView.Columns.Add("Inactive", typeof(string));
            _dtItemsView.Columns.Add("Expiry", typeof(DateTime));

            int n = 1;
            foreach (ItemEditData d in _items)
            {
                DataRow r = _dtItemsView.NewRow();
                r["No"] = n++;
                r["ServiceItemNo"] = d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber;
                r["Items"] = ItemCodesSummary(d);
                r["BillingDay"] = d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value.ToString() : "(contract)";
                r["BKMeter"] = MeterForRole(d, "BK");
                r["CLMeter"] = MeterForRole(d, "CL");
                r["Inactive"] = d.Inactive ? "Y" : "N";
                r["Expiry"] = (object)d.ServiceExpiryDate ?? DBNull.Value;
                _dtItemsView.Rows.Add(r);
            }
            GridItems.DataSource = _dtItemsView;
            GridViewItems.BestFitColumns();
            RefreshOrphanLookup();   // keep the inline attach list current after add/detach/swap
        }

        private static string ItemCodesSummary(ItemEditData d)
        {
            if (d.ItemCodes == null || d.ItemCodes.Rows.Count == 0) return "";
            System.Collections.Generic.List<string> codes = new System.Collections.Generic.List<string>();
            foreach (DataRow r in d.ItemCodes.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string c = r["ItemCode"] == null ? "" : r["ItemCode"].ToString().Trim();
                if (c.Length > 0) codes.Add(c);
            }
            return string.Join(", ", codes.ToArray());
        }

        private static string MeterForRole(ItemEditData d, string role)
        {
            if (d.Meters == null) return "";
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string rr = (r["MeterRole"] == null ? "" : r["MeterRole"].ToString()).Trim().ToUpperInvariant();
                if (rr == role) return r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString();
            }
            return "";
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            // Add a New Service Item under THIS contract: contract shown read-only, billing day mapped.
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, d, (int)SpnBillingDay.Value, TxtContractNo.Text.Trim()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _items.Add(d);
                    _dirty = true;
                    RebuildItemsView();
                }
            }
        }

        private void BtnEditItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, _items[idx], (int)SpnBillingDay.Value))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) { _dirty = true; RebuildItemsView(); }
            }
        }

        private void BtnDelItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            if (XtraMessageBox.Show("Remove the selected service item from this contract?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            long removedKey = _items[idx].ItemKey;
            _items.RemoveAt(idx);
            // Existing item -> it becomes an orphan (contract-less) on save; make it re-pickable now.
            if (removedKey > 0 && !_detachedThisSession.Contains(removedKey)) _detachedThisSession.Add(removedKey);
            _dirty = true;
            RebuildItemsView();
        }

        private void GridViewItems_DoubleClick(object sender, EventArgs e) { BtnEditItem_Click(null, null); }

        // #5: the Service Item No column is an inline SearchLookUpEdit of ORPHAN items (contract-less +
        // detached-this-session). Picking one on a row attaches/swaps it in place. Other columns stay
        // read-only. Combined with the "+" attach button this lets a removed (orphaned) item be
        // re-chosen or a different one attached without leaving the grid.
        private void SetupInlineOrphanLookup()
        {
            _orphanLookup = LoadOrphanItems();
            _orphanRepo = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            _orphanRepo.DataSource = _orphanLookup;
            _orphanRepo.ValueMember = "ServiceItemNo";
            _orphanRepo.DisplayMember = "ServiceItemNo";
            _orphanRepo.NullText = "(pick an unattached item)";
            GridItems.RepositoryItems.Add(_orphanRepo);

            GridViewItems.OptionsBehavior.Editable = true;
            foreach (DevExpress.XtraGrid.Columns.GridColumn c in GridViewItems.Columns)
                c.OptionsColumn.AllowEdit = false;
            DevExpress.XtraGrid.Columns.GridColumn col = GridViewItems.Columns.ColumnByFieldName("ServiceItemNo");
            if (col != null) { col.OptionsColumn.AllowEdit = true; col.ColumnEdit = _orphanRepo; }
            GridViewItems.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewItems_CellValueChanged);
        }

        private void RefreshOrphanLookup()
        {
            if (_orphanRepo == null) return;
            _orphanLookup = LoadOrphanItems();
            _orphanRepo.DataSource = _orphanLookup;
        }

        private void GridViewItems_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName != "ServiceItemNo") return;
            string picked = e.Value == null ? "" : e.Value.ToString().Trim();
            if (picked.Length == 0) return;
            object noVal = GridViewItems.GetRowCellValue(e.RowHandle, "No");
            if (noVal == null || noVal == DBNull.Value) return;
            int idx = Convert.ToInt32(noVal) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            if (_items[idx].ServiceItemNo == picked) return;   // unchanged
            DataRow[] f = _orphanLookup.Select("ServiceItemNo='" + picked.Replace("'", "''") + "'");
            if (f.Length == 0) return;
            long newKey = Convert.ToInt64(f[0]["ItemKey"]);
            long oldKey = _items[idx].ItemKey;
            _items[idx] = LoadOneItem(newKey);                 // swap the row's item
            _detachedThisSession.Remove(newKey);
            if (oldKey > 0 && !_detachedThisSession.Contains(oldKey)) _detachedThisSession.Add(oldKey);
            _dirty = true;
            BeginInvoke(new MethodInvoker(RebuildItemsView));  // rebuild after the edit commits
        }

        // Expiry column: RED bold if expired (before today), GREEN bold if still active.
        // RowCellStyle fires per cell per repaint — cache the bold font instead of allocating one each time.
        private System.Drawing.Font _boldFont;
        private static readonly System.Drawing.Color _expiredRed = System.Drawing.Color.FromArgb(198, 40, 40);
        private static readonly System.Drawing.Color _activeGreen = System.Drawing.Color.FromArgb(46, 125, 50);

        private void GridViewItems_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column != ColExpiry) return;
            object v = GridViewItems.GetRowCellValue(e.RowHandle, ColExpiry);
            if (v == null || v == DBNull.Value) return;
            DateTime exp = Convert.ToDateTime(v);
            e.Appearance.ForeColor = exp.Date < DateTime.Today ? _expiredRed : _activeGreen;
            if (_boldFont == null)
                _boldFont = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.Font = _boldFont;
        }

        // ===================== Spare Parts / Services Provided =====================

        private DataTable _spareParts;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _spCheck;
        private DataTable _spItemLookup;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _spItemRepo;

        internal static DataTable CreateSparePartsTable()
        {
            DataTable dt = new DataTable("SpareParts");
            dt.Columns.Add("SparePartKey", typeof(long));
            dt.Columns.Add("ItemKey", typeof(long));       // set => item-bound (read-only on the contract)
            dt.Columns.Add("Bound", typeof(bool));         // ItemKey present -> true
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemCode", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Unlimited", typeof(bool));
            dt.Columns.Add("UOM", typeof(string));
            dt.Columns.Add("Quantity", typeof(decimal));
            dt.Columns.Add("Discount", typeof(string));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("Amount", typeof(decimal));
            dt.Columns.Add("TaxType", typeof(string));
            dt.Columns.Add("TaxInclusive", typeof(bool));
            dt.Columns.Add("TaxRate", typeof(decimal));
            dt.Columns.Add("TaxAmount", typeof(decimal));
            dt.Columns.Add("AmountAfterTax", typeof(decimal));
            dt.Columns.Add("Pos", typeof(int));
            return dt;
        }

        private void SetupSparePartsGrid()
        {
            _spCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            GridSpareParts.RepositoryItems.Add(_spCheck);
            _spItemLookup = LoadItemLookup(_db);
            _spItemRepo = MakeItemCodeRepo(_spItemLookup);
            GridSpareParts.RepositoryItems.Add(_spItemRepo);

            GridViewSpareParts.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            GridViewSpareParts.OptionsBehavior.Editable = true;
            GridViewSpareParts.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(SpareParts_CellValueChanged);
            GridViewSpareParts.ShowingEditor += new System.ComponentModel.CancelEventHandler(SpareParts_ShowingEditor);

            _spareParts = CreateSparePartsTable();
            GridSpareParts.DataSource = _spareParts.DefaultView;
            _spareParts.DefaultView.Sort = "Pos";
            ConfigureSpareView(GridViewSpareParts, _spCheck, _spItemRepo);
            GridViewSpareParts.RowHeight = 26;   // ~20% taller rows for easier editing
        }

        // Item master lookup for the "Item Provided" grid's Item Code column (like invoice / stock
        // issue): pick an item code and its Description + UOM + Tax auto-fill.
        internal static DataTable LoadItemLookup(DBSetting db)
        {
            try
            {
                return db.GetDataTable(
                    "SELECT ItemCode, ISNULL(Description,'') AS Description, ISNULL(SalesUOM,'') AS UOM, " +
                    "ISNULL(TaxCode,'') AS TaxType FROM [dbo].[Item] ORDER BY ItemCode", false);
            }
            catch { return new DataTable(); }
        }

        internal static DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit MakeItemCodeRepo(DataTable itemLookup)
        {
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repo =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            repo.DataSource = itemLookup;
            repo.ValueMember = "ItemCode";
            repo.DisplayMember = "ItemCode";
            repo.NullText = "";
            return repo;   // popup grid auto-populates the 4 datasource columns
        }

        // After an Item Code is picked, fill Description / UOM / Tax from the item master.
        internal static void FillFromItem(DataRow row, DataTable itemLookup)
        {
            if (row == null || itemLookup == null) return;
            string c = row["ItemCode"] == DBNull.Value ? "" : Convert.ToString(row["ItemCode"]).Trim();
            if (c.Length == 0) return;
            DataRow[] f = itemLookup.Select("ItemCode='" + c.Replace("'", "''") + "'");
            if (f.Length == 0) return;
            row["Description"] = f[0]["Description"];
            row["UOM"] = f[0]["UOM"];
            row["TaxType"] = f[0]["TaxType"];
        }

        // Shared spare-parts column layout — used by BOTH the contract editor and the service item
        // editor so the two grids match exactly.
        internal static void ConfigureSpareView(DevExpress.XtraGrid.Views.Grid.GridView v,
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit check,
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit itemRepo)
        {
            v.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            v.OptionsBehavior.Editable = true;
            v.OptionsView.ShowGroupPanel = false;
            v.Columns.Clear();
            v.PopulateColumns();
            SpHide(v, "SparePartKey"); SpHide(v, "ItemKey"); SpHide(v, "Bound"); SpHide(v, "Pos");
            SpCol2(v, "No", "No", 40, 0, true);
            SpCol2(v, "ItemCode", "Item Code", 130, 1);
            if (itemRepo != null && v.Columns["ItemCode"] != null) v.Columns["ItemCode"].ColumnEdit = itemRepo;
            SpCol2(v, "Description", "Description", 240, 2);
            SpBool2(v, check, "Unlimited", "Unlimited", 70, 3);
            SpCol2(v, "UOM", "UOM", 70, 4);
            SpCol2(v, "Quantity", "Quantity", 80, 5);
            SpCol2(v, "Discount", "Discount", 80, 6);
            SpCol2(v, "UnitPrice", "Unit Price", 90, 7);
            SpCol2(v, "Amount", "Amount", 90, 8, true);
            SpCol2(v, "TaxType", "Tax Type", 80, 9);
            SpBool2(v, check, "TaxInclusive", "Tax Inclusive", 90, 10);
            SpCol2(v, "TaxRate", "Tax (%)", 70, 11);
            SpCol2(v, "TaxAmount", "Tax Amount", 90, 12, true);
            SpCol2(v, "AmountAfterTax", "Amount After Tax", 110, 13, true);
        }

        private static void SpHide(DevExpress.XtraGrid.Views.Grid.GridView v, string field)
        { DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field]; if (c != null) c.Visible = false; }

        private static void SpCol2(DevExpress.XtraGrid.Views.Grid.GridView v, string field, string caption, int width, int visibleIndex, bool readOnly = false)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            if (caption != null) c.Caption = caption;
            if (width > 0) c.Width = width;
            if (visibleIndex >= 0) { c.Visible = true; c.VisibleIndex = visibleIndex; }
            if (readOnly) { c.OptionsColumn.AllowEdit = false; c.OptionsColumn.ReadOnly = true; }
        }

        private static void SpBool2(DevExpress.XtraGrid.Views.Grid.GridView v, DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit check, string field, string caption, int width, int visibleIndex)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
            c.ColumnEdit = check;
        }

        // Item-bound lines are read-only on the contract (they belong to a service item); block editing.
        private void SpareParts_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            object bound = GridViewSpareParts.GetRowCellValue(rh, "Bound");
            if (bound != null && bound != DBNull.Value && Convert.ToBoolean(bound)) e.Cancel = true;
        }

        private void SpareParts_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column.FieldName == "Amount" || e.Column.FieldName == "TaxAmount" ||
                e.Column.FieldName == "AmountAfterTax" || e.Column.FieldName == "No") return;
            GridViewSpareParts.PostEditor();
            DataRowView drv = GridViewSpareParts.GetRow(e.RowHandle) as DataRowView;
            if (drv != null)
            {
                if (e.Column.FieldName == "ItemCode") FillFromItem(drv.Row, _spItemLookup);   // auto-fill Description/UOM/Tax
                ComputeSpareRow(drv.Row);
            }
            GridViewSpareParts.RefreshData();
        }

        internal static void ComputeSpareRow(DataRow r)
        {
            decimal qty = r["Quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Quantity"]);
            decimal price = r["UnitPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(r["UnitPrice"]);
            decimal gross = qty * price;
            // Discount: "10%" -> percent of gross; a plain number -> absolute amount off.
            string disc = r["Discount"] == DBNull.Value ? "" : Convert.ToString(r["Discount"]).Trim();
            decimal discAmt = 0m;
            if (disc.EndsWith("%"))
            {
                decimal p; if (decimal.TryParse(disc.TrimEnd('%').Trim(), out p)) discAmt = gross * p / 100m;
            }
            else { decimal a; if (decimal.TryParse(disc, out a)) discAmt = a; }
            decimal amount = gross - discAmt; if (amount < 0) amount = 0;
            decimal rate = r["TaxRate"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TaxRate"]);
            bool inclusive = r["TaxInclusive"] != DBNull.Value && Convert.ToBoolean(r["TaxInclusive"]);
            decimal taxAmt, afterTax;
            if (inclusive) { afterTax = amount; taxAmt = rate == 0 ? 0 : amount - (amount / (1 + rate / 100m)); }
            else { taxAmt = amount * rate / 100m; afterTax = amount + taxAmt; }
            r["Amount"] = decimal.Round(amount, 2);
            r["TaxAmount"] = decimal.Round(taxAmt, 2);
            r["AmountAfterTax"] = decimal.Round(afterTax, 2);
        }

        private void LoadSpareParts()
        {
            if (_spareParts == null) SetupSparePartsGrid();
            _spareParts.Rows.Clear();
            if (_isNew || _contractKey == 0) { RenumberSpareParts(); return; }
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT SparePartKey, ItemKey, ItemCode, Description, Unlimited, UOM, Quantity, Discount, " +
                    "UnitPrice, TaxType, TaxInclusive, TaxRate, Pos FROM [dbo].[zSCP2_ContractSparePart] " +
                    "WHERE ContractKey=" + _contractKey + " ORDER BY Pos", false);
                foreach (DataRow s in dt.Rows)
                {
                    DataRow r = _spareParts.NewRow();
                    r["SparePartKey"] = s["SparePartKey"];
                    r["ItemKey"] = s["ItemKey"];
                    r["Bound"] = s["ItemKey"] != DBNull.Value;
                    r["ItemCode"] = s["ItemCode"]; r["Description"] = s["Description"];
                    r["Unlimited"] = Convert.ToString(s["Unlimited"]) == "Y";
                    r["UOM"] = s["UOM"]; r["Quantity"] = s["Quantity"]; r["Discount"] = s["Discount"];
                    r["UnitPrice"] = s["UnitPrice"]; r["TaxType"] = s["TaxType"];
                    r["TaxInclusive"] = Convert.ToString(s["TaxInclusive"]) == "Y";
                    r["TaxRate"] = s["TaxRate"]; r["Pos"] = s["Pos"];
                    ComputeSpareRow(r);
                    _spareParts.Rows.Add(r);
                }
            }
            catch { }
            RenumberSpareParts();
        }

        private void RenumberSpareParts()
        {
            if (_spareParts == null) return;
            System.Data.DataView v = _spareParts.DefaultView;
            for (int i = 0; i < v.Count; i++) { v[i].Row["No"] = i + 1; v[i].Row["Pos"] = i; }
        }

        private void BtnSpInsert_Click(object sender, EventArgs e)
        {
            if (_spareParts == null) return;
            GridViewSpareParts.PostEditor();
            DataRow r = _spareParts.NewRow();
            r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
            r["ItemCode"] = ""; r["Description"] = ""; r["Unlimited"] = false; r["UOM"] = "";
            r["Quantity"] = 0m; r["Discount"] = ""; r["UnitPrice"] = 0m; r["Amount"] = 0m;
            r["TaxType"] = ""; r["TaxInclusive"] = false; r["TaxRate"] = 0m; r["TaxAmount"] = 0m;
            r["AmountAfterTax"] = 0m; r["Pos"] = _spareParts.Rows.Count;
            _spareParts.Rows.Add(r);
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
            GridViewSpareParts.FocusedRowHandle = GridViewSpareParts.RowCount - 1;
        }

        private void BtnSpRemove_Click(object sender, EventArgs e)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            DataRowView drv = GridViewSpareParts.GetRow(rh) as DataRowView;
            if (drv == null) return;
            if (drv.Row["ItemKey"] != DBNull.Value)
            { XtraMessageBox.Show("This spare part belongs to a service item and cannot be removed here. Edit it on the service item.", "Read-only"); return; }
            drv.Row.Delete();
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
        }

        private void BtnSpUp_Click(object sender, EventArgs e) { MoveSparePart(-1); }
        private void BtnSpDown_Click(object sender, EventArgs e) { MoveSparePart(1); }

        // Move Up/Down mirrors AutoCount's detail-grid reorder: swap the Pos of the two adjacent rows,
        // the Pos-sorted view then re-orders, and focus follows the moved row.
        private void MoveSparePart(int dir)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            int target = rh + dir;
            if (target < 0 || target >= GridViewSpareParts.RowCount) return;
            DataRowView a = GridViewSpareParts.GetRow(rh) as DataRowView;
            DataRowView b = GridViewSpareParts.GetRow(target) as DataRowView;
            if (a == null || b == null) return;
            int pa = Convert.ToInt32(a.Row["Pos"]), pb = Convert.ToInt32(b.Row["Pos"]);
            a.Row["Pos"] = pb; b.Row["Pos"] = pa;
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
            GridViewSpareParts.FocusedRowHandle = target;
        }

        // Persist a service item's own (item-bound) spare parts. These show read-only on the contract.
        private void SaveItemSpareParts(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=@ik", P("@ik", itemKey));
            if (d.SpareParts == null) return;
            int pos = 0;
            foreach (DataRow r in d.SpareParts.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["ItemCode"] == DBNull.Value ? "" : Convert.ToString(r["ItemCode"]).Trim();
                if (code.Length == 0 && Convert.ToString(r["Description"]).Trim().Length == 0) continue;
                ExecNonQuery(conn, tx,
                    "INSERT INTO [dbo].[zSCP2_ContractSparePart] " +
                    "(ContractKey, ItemKey, ItemCode, Description, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                    " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                    "VALUES (@ck, @ik, @code, @desc, @unl, @uom, @qty, @disc, @price, @ttype, @tinc, @trate, @pos, GETDATE())",
                    P("@ck", _contractKey), P("@ik", itemKey), P("@code", code),
                    P("@desc", AsStr(r["Description"])),
                    P("@unl", Convert.ToBoolean(r["Unlimited"]) ? "Y" : "N"),
                    P("@uom", AsStr(r["UOM"])), P("@qty", AsDec(r["Quantity"])),
                    P("@disc", AsStr(r["Discount"])), P("@price", AsDec(r["UnitPrice"])),
                    P("@ttype", AsStr(r["TaxType"])),
                    P("@tinc", Convert.ToBoolean(r["TaxInclusive"]) ? "Y" : "N"),
                    P("@trate", AsDec(r["TaxRate"])), P("@pos", pos++));
            }
        }

        private void SaveSpareParts(SqlConnection conn, SqlTransaction tx)
        {
            if (_spareParts == null) return;
            GridViewSpareParts.PostEditor();
            // Replace only contract-level lines; item-bound lines are owned by the service item and
            // are left untouched (they were shown read-only).
            ExecNonQuery(conn, tx,
                "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ContractKey=@ck AND ItemKey IS NULL",
                P("@ck", _contractKey));
            foreach (DataRowView drv in _spareParts.DefaultView)
            {
                DataRow r = drv.Row;
                if (r["ItemKey"] != DBNull.Value) continue;   // item-bound: not owned here
                string code = r["ItemCode"] == DBNull.Value ? "" : Convert.ToString(r["ItemCode"]).Trim();
                if (code.Length == 0 && Convert.ToString(r["Description"]).Trim().Length == 0) continue;
                ExecNonQuery(conn, tx,
                    "INSERT INTO [dbo].[zSCP2_ContractSparePart] " +
                    "(ContractKey, ItemKey, ItemCode, Description, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                    " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                    "VALUES (@ck, NULL, @code, @desc, @unl, @uom, @qty, @disc, @price, @ttype, @tinc, @trate, @pos, GETDATE())",
                    P("@ck", _contractKey), P("@code", code),
                    P("@desc", AsStr(r["Description"])),
                    P("@unl", Convert.ToBoolean(r["Unlimited"]) ? "Y" : "N"),
                    P("@uom", AsStr(r["UOM"])), P("@qty", AsDec(r["Quantity"])),
                    P("@disc", AsStr(r["Discount"])), P("@price", AsDec(r["UnitPrice"])),
                    P("@ttype", AsStr(r["TaxType"])),
                    P("@tinc", Convert.ToBoolean(r["TaxInclusive"]) ? "Y" : "N"),
                    P("@trate", AsDec(r["TaxRate"])), P("@pos", Convert.ToInt32(r["Pos"])));
            }
        }

        // ===================== More Header tab =====================
        // Built in code (many simple fields) into the designer's empty PageMoreHeader shell. Each edit
        // is keyed by its zSCP2_Contract column name so load/save is a simple loop.

        private readonly System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit> _mh
            = new System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit>();
        private DevExpress.XtraEditors.MemoEdit _mhDelAddress;

        private void BuildMoreHeaderTab()
        {
            // Top block: two columns of contact fields.
            MhField("City", "City", 12, 14, 200);
            MhField("PostalCode", "Postal Code", 430, 14, 200);
            MhField("State", "State", 12, 40, 200);
            MhField("Country", "Country", 430, 40, 200);
            MhField("Fax", "Fax", 12, 66, 200);
            MhField("Ref1", "Ref 1", 430, 66, 200);
            MhField("Ref2", "Ref 2", 12, 92, 200);
            MhField("Ref3", "Ref 3", 430, 92, 200);
            MhField("Ref4", "Ref 4", 12, 118, 200);

            // Delivery Address group.
            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            grp.Text = "Delivery Address";
            grp.Location = new System.Drawing.Point(12, 150);
            grp.Size = new System.Drawing.Size(820, 210);
            PageMoreHeader.Controls.Add(grp);

            // Search = pick one of the customer's branches; Copy = copy the main contract address here.
            DevExpress.XtraEditors.SimpleButton btnSearch = new DevExpress.XtraEditors.SimpleButton();
            btnSearch.Text = "Search"; btnSearch.Location = new System.Drawing.Point(300, 27); btnSearch.Size = new System.Drawing.Size(60, 22);
            btnSearch.Click += new EventHandler(DelSearch_Click);
            grp.Controls.Add(btnSearch);
            DevExpress.XtraEditors.SimpleButton btnCopy = new DevExpress.XtraEditors.SimpleButton();
            btnCopy.Text = "Copy"; btnCopy.Location = new System.Drawing.Point(364, 27); btnCopy.Size = new System.Drawing.Size(55, 22);
            btnCopy.Click += new EventHandler(DelCopy_Click);
            grp.Controls.Add(btnCopy);

            MhFieldIn(grp, "DelBranchCode", "Branch Code", 10, 28, 180);
            MhFieldIn(grp, "DelState", "State", 430, 28, 180);
            MhFieldIn(grp, "DelBranchName", "Branch Name", 10, 54, 180);
            MhFieldIn(grp, "DelCountry", "Country", 430, 54, 180);

            DevExpress.XtraEditors.LabelControl lblAddr = new DevExpress.XtraEditors.LabelControl();
            lblAddr.Text = "Address"; lblAddr.Location = new System.Drawing.Point(10, 83);
            grp.Controls.Add(lblAddr);
            _mhDelAddress = new DevExpress.XtraEditors.MemoEdit();
            _mhDelAddress.Location = new System.Drawing.Point(110, 80);
            _mhDelAddress.Size = new System.Drawing.Size(200, 60);
            _mhDelAddress.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            grp.Controls.Add(_mhDelAddress);

            MhFieldIn(grp, "DelPhone", "Phone", 430, 83, 180);
            MhFieldIn(grp, "DelFax", "Fax", 430, 109, 180);
            MhFieldIn(grp, "DelEmail", "Email", 430, 135, 180);
            MhFieldIn(grp, "DelContactPerson", "Contact Person", 430, 161, 180);
            MhFieldIn(grp, "DelCity", "City", 10, 150, 180);
            MhFieldIn(grp, "DelPostalCode", "Postal Code", 10, 176, 180);
        }

        private void MhField(string col, string caption, int x, int y, int width)
        {
            MhFieldOn(PageMoreHeader, col, caption, x, y, width);
        }
        private void MhFieldIn(DevExpress.XtraEditors.GroupControl grp, string col, string caption, int x, int y, int width)
        {
            MhFieldOn(grp, col, caption, x, y, width);
        }
        private void MhFieldOn(System.Windows.Forms.Control parent, string col, string caption, int x, int y, int width)
        {
            DevExpress.XtraEditors.LabelControl lbl = new DevExpress.XtraEditors.LabelControl();
            lbl.Text = caption; lbl.Location = new System.Drawing.Point(x, y + 3);
            parent.Controls.Add(lbl);
            DevExpress.XtraEditors.TextEdit ed = new DevExpress.XtraEditors.TextEdit();
            ed.Location = new System.Drawing.Point(x + 98, y);
            ed.Size = new System.Drawing.Size(width, 20);
            ed.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            parent.Controls.Add(ed);
            _mh[col] = ed;
        }

        private void LoadMoreHeader()
        {
            if (_isNew || _contractKey == 0) return;
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT City, PostalCode, State, Country, Fax, Ref1, Ref2, Ref3, Ref4, " +
                    "DelBranchCode, DelBranchName, DelAddress, DelCity, DelPostalCode, DelState, " +
                    "DelCountry, DelPhone, DelFax, DelEmail, DelContactPerson " +
                    "FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + _contractKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _mh)
                    if (dt.Columns.Contains(kv.Key)) kv.Value.Text = AsStr(r[kv.Key]);
                if (_mhDelAddress != null) _mhDelAddress.Text = AsStr(r["DelAddress"]);
            }
            catch { }
        }

        private void SaveMoreHeader(SqlConnection conn, SqlTransaction tx)
        {
            ExecNonQuery(conn, tx,
                "UPDATE [dbo].[zSCP2_Contract] SET City=@City, PostalCode=@PostalCode, State=@State, " +
                "Country=@Country, Fax=@Fax, Ref1=@Ref1, Ref2=@Ref2, Ref3=@Ref3, Ref4=@Ref4, " +
                "DelBranchCode=@DelBranchCode, DelBranchName=@DelBranchName, DelAddress=@DelAddress, " +
                "DelCity=@DelCity, DelPostalCode=@DelPostalCode, DelState=@DelState, DelCountry=@DelCountry, " +
                "DelPhone=@DelPhone, DelFax=@DelFax, DelEmail=@DelEmail, DelContactPerson=@DelContactPerson " +
                "WHERE ContractKey=@ck",
                P("@City", MhVal("City")), P("@PostalCode", MhVal("PostalCode")), P("@State", MhVal("State")),
                P("@Country", MhVal("Country")), P("@Fax", MhVal("Fax")), P("@Ref1", MhVal("Ref1")),
                P("@Ref2", MhVal("Ref2")), P("@Ref3", MhVal("Ref3")), P("@Ref4", MhVal("Ref4")),
                P("@DelBranchCode", MhVal("DelBranchCode")), P("@DelBranchName", MhVal("DelBranchName")),
                P("@DelAddress", _mhDelAddress == null ? "" : _mhDelAddress.Text.Trim()),
                P("@DelCity", MhVal("DelCity")), P("@DelPostalCode", MhVal("DelPostalCode")),
                P("@DelState", MhVal("DelState")), P("@DelCountry", MhVal("DelCountry")),
                P("@DelPhone", MhVal("DelPhone")), P("@DelFax", MhVal("DelFax")),
                P("@DelEmail", MhVal("DelEmail")), P("@DelContactPerson", MhVal("DelContactPerson")),
                P("@ck", _contractKey));
        }

        private string MhVal(string col)
        {
            DevExpress.XtraEditors.TextEdit ed;
            return _mh.TryGetValue(col, out ed) ? (ed.Text ?? "").Trim() : "";
        }

        private void MhSet(string col, string val)
        {
            DevExpress.XtraEditors.TextEdit ed;
            if (_mh.TryGetValue(col, out ed)) ed.Text = val ?? "";
        }

        // "Copy": copy the main contract contact/address into the Delivery Address block.
        private void DelCopy_Click(object sender, EventArgs e)
        {
            if (_mhDelAddress != null) _mhDelAddress.Text = TxtAddress.Text;
            MhSet("DelCity", MhVal("City"));
            MhSet("DelPostalCode", MhVal("PostalCode"));
            MhSet("DelState", MhVal("State"));
            MhSet("DelCountry", MhVal("Country"));
            MhSet("DelPhone", TxtPhone.Text);
            MhSet("DelFax", MhVal("Fax"));
            _dirty = true;
        }

        // "Search": pick one of the current customer's branches (dbo.Branch) and fill the delivery block.
        private void DelSearch_Click(object sender, EventArgs e)
        {
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString().Trim();
            if (debtor.Length == 0) { XtraMessageBox.Show("Pick a Customer first.", "Search"); return; }
            DataTable br;
            try
            {
                br = _db.GetDataTable(
                    "SELECT BranchCode, ISNULL(BranchName,'') AS BranchName, ISNULL(Address1,'') AS Address1, " +
                    "ISNULL(Address2,'') AS Address2, ISNULL(Address3,'') AS Address3, ISNULL(Address4,'') AS Address4, " +
                    "ISNULL(PostCode,'') AS PostCode, ISNULL(Phone1,'') AS Phone1, ISNULL(Fax1,'') AS Fax1, " +
                    "ISNULL(EmailAddress,'') AS EmailAddress, ISNULL(Contact,'') AS Contact " +
                    "FROM [dbo].[Branch] WHERE AccNo=N'" + debtor.Replace("'", "''") + "' ORDER BY BranchCode", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); return; }
            if (br.Rows.Count == 0) { XtraMessageBox.Show("This customer has no branches.", "Search"); return; }

            object sel = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Select Branch", br, "BranchCode",
                new string[] { "BranchCode", "BranchName", "Address1", "PostCode", "Phone1" },
                new string[] { "Branch Code", "Branch Name", "Address", "Post Code", "Phone" },
                new int[] { 90, 200, 200, 80, 100 });
            if (sel == null || sel == DBNull.Value) return;
            {
                DataRow[] f = br.Select("BranchCode='" + sel.ToString().Replace("'", "''") + "'");
                if (f.Length == 0) return;
                DataRow b = f[0];
                MhSet("DelBranchCode", AsStr(b["BranchCode"]));
                MhSet("DelBranchName", AsStr(b["BranchName"]));
                if (_mhDelAddress != null)
                    _mhDelAddress.Text = string.Join("\r\n", new string[] { AsStr(b["Address1"]), AsStr(b["Address2"]), AsStr(b["Address3"]), AsStr(b["Address4"]) }).Trim('\r', '\n');
                MhSet("DelPostalCode", AsStr(b["PostCode"]));
                MhSet("DelPhone", AsStr(b["Phone1"]));
                MhSet("DelFax", AsStr(b["Fax1"]));
                MhSet("DelEmail", AsStr(b["EmailAddress"]));
                MhSet("DelContactPerson", AsStr(b["Contact"]));
                _dirty = true;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            bool wasNew = _isNew;   // InsertContract flips _isNew during save; remember the entry state
            if (string.IsNullOrWhiteSpace(TxtContractNo.Text))
            { XtraMessageBox.Show("Contract No is required.", "Validation"); return; }
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (string.IsNullOrWhiteSpace(debtor))
            { XtraMessageBox.Show("Customer (Debtor) is required.", "Validation"); return; }

            // Service Expiry (end) date cannot be earlier than the Service Start date.
            if (DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value &&
                DtExpiryDate.EditValue != null && DtExpiryDate.EditValue != DBNull.Value &&
                Convert.ToDateTime(DtExpiryDate.EditValue).Date < Convert.ToDateTime(DtStartDate.EditValue).Date)
            { XtraMessageBox.Show("Service Expiry (To) date cannot be earlier than the Service Start date.", "Validation"); return; }

            // Reserve the REAL contract number now (only if new & still showing the auto-preview) so
            // the counter is consumed on save, not on every Auto click.
            if (_isNew && !string.IsNullOrEmpty(_autoPeekNo) && TxtContractNo.Text.Trim() == _autoPeekNo)
                TxtContractNo.Text = ServiceContractPhotocopier.Classes.ScpDocNo.Next(
                    _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_CONTRACT);

            // Embedded items showing an auto-preview number: reserve a real one each at save.
            foreach (ItemEditData d in _items)
            {
                if (d.ServiceItemNoIsAuto)
                {
                    d.ServiceItemNo = ServiceContractPhotocopier.Classes.ScpDocNo.Next(
                        _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_SERVICE_ITEM);
                    d.ServiceItemNoIsAuto = false;
                }
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        if (_isNew) InsertContract(conn, tx, debtor);
                        else UpdateContract(conn, tx, debtor);

                        // Upsert items, preserving ItemKeys (attach/detach model). Existing items no
                        // longer in the contract are DETACHED (ContractKey=NULL), not deleted, so they
                        // survive as contract-less and keep their meter history.
                        System.Collections.Generic.HashSet<long> kept = new System.Collections.Generic.HashSet<long>();
                        foreach (ItemEditData d in _items) if (d.ItemKey > 0) kept.Add(d.ItemKey);
                        using (SqlCommand ex = new SqlCommand("SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=@ck", conn, tx))
                        {
                            ex.Parameters.AddWithValue("@ck", _contractKey);
                            System.Collections.Generic.List<long> present = new System.Collections.Generic.List<long>();
                            using (SqlDataReader rd = ex.ExecuteReader()) { while (rd.Read()) present.Add(rd.GetInt64(0)); }
                            foreach (long k in present)
                                if (!kept.Contains(k))
                                    ExecNonQuery(conn, tx, "UPDATE [dbo].[zSCP2_Item] SET ContractKey=NULL, LastModified=GETDATE() WHERE ItemKey=@ik", P("@ik", k));
                        }

                        int pos = 0;
                        foreach (ItemEditData d in _items)
                        {
                            long itemKey;
                            if (d.ItemKey > 0)
                            {
                                UpdateItem(conn, tx, d, pos);
                                itemKey = d.ItemKey;
                                // children are rebuilt each save; wipe this item's rows first
                                ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=@ik", P("@ik", itemKey));
                                ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=@ik", P("@ik", itemKey));
                            }
                            else
                            {
                                itemKey = InsertItem(conn, tx, d, pos);
                            }
                            InsertMeters(conn, tx, d, itemKey);
                            InsertItemCodes(conn, tx, d, itemKey);
                            SaveItemSpareParts(conn, tx, d, itemKey);
                            zSCP2_Item_Form.PersistItemExtras(conn, tx, d, itemKey);
                            pos++;
                        }
                        SaveSpareParts(conn, tx);
                        SaveMoreHeader(conn, tx);
                        tx.Commit();
                    }
                }
                _dirty = false;
                if (wasNew)
                {
                    // The contract now exists (has a ContractKey) -> switch to EDIT mode and STAY OPEN so
                    // the user can immediately add service items (Add/Attach are enabled only in edit mode).
                    _detachedThisSession.Clear();
                    LoadContract();          // reload header + (empty) items from the just-saved row
                    RebuildItemsView();
                    LoadSpareParts();
                    LoadMoreHeader();
                    _dirty = false;
                    UpdateServiceItemButtons();
                    XtraMessageBox.Show("Contract saved. You can now add service items.", "Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    XtraMessageBox.Show("Contract saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _savedOk = true;   // skip the unsaved-changes prompt on the close that follows
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertContract(SqlConnection conn, SqlTransaction tx, string debtor)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Contract] " +
                "(ContractNo, ContractTypeCode, DebtorCode, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
                " ContractValue, BillingDay, BillOnMonthEnd, BillingMode, Address1, Attention, Phone, TermCode, AreaCode, StaffCode, " +
                " ReferenceNo, Description, Remark1, Remark2, Note, DeptNo, ProjNo, Inactive, Created, LastModified) " +
                "VALUES (@no,@type,@debtor,@cdate,@sdate,@edate,@val,@bday,@monthend,@bmode,@addr,@attn,@phone,@term,@area,@staff," +
                "@refno,@desc,@r1,@r2,@note,@dept,@proj,@inact,GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                AddContractParams(cmd, debtor);
                _contractKey = Convert.ToInt64(cmd.ExecuteScalar());
            }
            _isNew = false;
        }

        private void UpdateContract(SqlConnection conn, SqlTransaction tx, string debtor)
        {
            string sql =
                "UPDATE [dbo].[zSCP2_Contract] SET ContractNo=@no, ContractTypeCode=@type, DebtorCode=@debtor, " +
                "ContractDate=@cdate, ServiceStartDate=@sdate, ServiceExpiryDate=@edate, ContractValue=@val, " +
                "BillingDay=@bday, BillOnMonthEnd=@monthend, BillingMode=@bmode, Address1=@addr, Attention=@attn, Phone=@phone, TermCode=@term, " +
                "AreaCode=@area, StaffCode=@staff, ReferenceNo=@refno, Description=@desc, Remark1=@r1, Remark2=@r2, Note=@note, " +
                "DeptNo=@dept, ProjNo=@proj, Inactive=@inact, Modified=GETDATE(), LastModified=GETDATE() WHERE ContractKey=@ck";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                AddContractParams(cmd, debtor);
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.ExecuteNonQuery();
            }
        }

        private void AddContractParams(SqlCommand cmd, string debtor)
        {
            cmd.Parameters.AddWithValue("@no", TxtContractNo.Text.Trim());
            cmd.Parameters.AddWithValue("@type", SluContractType.EditValue == null ? "" : SluContractType.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@debtor", debtor);
            cmd.Parameters.AddWithValue("@cdate", DateParam(DtContractDate.EditValue));
            cmd.Parameters.AddWithValue("@sdate", DateParam(DtStartDate.EditValue));
            cmd.Parameters.AddWithValue("@edate", DateParam(DtExpiryDate.EditValue));
            cmd.Parameters.AddWithValue("@val", SpnContractValue.Value);
            // Bill-on-month-end: store day 31 so the existing month-end clamp bills the true last day
            // of every month (31 -> 30/29/28 on short months, 31 on long ones).
            cmd.Parameters.AddWithValue("@bday", (byte)(ChkMonthEnd.Checked ? 31 : (int)SpnBillingDay.Value));
            cmd.Parameters.AddWithValue("@monthend", ChkMonthEnd.Checked ? "Y" : "N");
            cmd.Parameters.AddWithValue("@bmode", CurrentBillingMode());
            cmd.Parameters.AddWithValue("@addr", TxtAddress.Text.Trim());
            cmd.Parameters.AddWithValue("@attn", TxtAttention.Text.Trim());
            cmd.Parameters.AddWithValue("@phone", TxtPhone.Text.Trim());
            cmd.Parameters.AddWithValue("@term", TxtTerm.Text.Trim());
            cmd.Parameters.AddWithValue("@area", TxtArea.Text.Trim());
            cmd.Parameters.AddWithValue("@refno", TxtRefNo.Text.Trim());
            cmd.Parameters.AddWithValue("@staff", SluAgent.EditValue == null ? "" : SluAgent.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@dept", SluDept.EditValue == null ? "" : SluDept.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@proj", SluProject.EditValue == null ? "" : SluProject.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@desc", TxtDescription.Text.Trim());
            cmd.Parameters.AddWithValue("@r1", TxtRemark1.Text.Trim());
            cmd.Parameters.AddWithValue("@r2", TxtRemark2.Text.Trim());
            cmd.Parameters.AddWithValue("@note", (object)(TxtNote.Text ?? ""));
            cmd.Parameters.AddWithValue("@inact", ChkInactive.Checked ? "Y" : "N");
        }

        private long InsertItem(SqlConnection conn, SqlTransaction tx, ItemEditData d, int pos)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Item] " +
                "(ContractKey, ServiceItemNo, SerialNumber, Description, BillingDayOverride, " +
                " DepartmentCode, JobCode, StockLocationCode, Pos, Inactive, LastModified) " +
                "VALUES (@ck,@no,@serial,@desc,@bday,@dept,@job,@loc,@pos,@inact,GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                cmd.Parameters.AddWithValue("@loc", d.StockLocationCode ?? "");
                cmd.Parameters.AddWithValue("@pos", pos);
                cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        // Re-parent + update an EXISTING service item (used for attached items and edited items) so
        // its ItemKey (and meter history) is preserved instead of delete+reinsert.
        private void UpdateItem(SqlConnection conn, SqlTransaction tx, ItemEditData d, int pos)
        {
            string sql =
                "UPDATE [dbo].[zSCP2_Item] SET ContractKey=@ck, ServiceItemNo=@no, SerialNumber=@serial, " +
                "Description=@desc, BillingDayOverride=@bday, DepartmentCode=@dept, JobCode=@job, " +
                "StockLocationCode=@loc, Pos=@pos, Inactive=@inact, LastModified=GETDATE() WHERE ItemKey=@ik";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                cmd.Parameters.AddWithValue("@loc", d.StockLocationCode ?? "");
                cmd.Parameters.AddWithValue("@pos", pos);
                cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@ik", d.ItemKey);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertItemCodes(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            if (d.ItemCodes == null) return;
            int pos = 0;
            foreach (DataRow r in d.ItemCodes.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["ItemCode"] == null ? "" : r["ItemCode"].ToString().Trim();
                if (string.IsNullOrEmpty(code)) continue;
                string sql =
                    "INSERT INTO [dbo].[zSCP2_ItemCode] (ItemKey, ItemCode, Description, Qty, SerialNumber, Pos, LastModified) " +
                    "VALUES (@ik,@code,@desc,@qty,@serial,@pos,GETDATE());";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@desc", r["Description"] == null ? "" : r["Description"].ToString());
                    cmd.Parameters.AddWithValue("@qty", AsDec(r["Qty"]));
                    cmd.Parameters.AddWithValue("@serial", r["SerialNumber"] == null ? "" : r["SerialNumber"].ToString());
                    cmd.Parameters.AddWithValue("@pos", pos++);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertMeters(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            if (d.Meters == null) return;
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString().Trim();
                if (string.IsNullOrEmpty(code)) continue;
                string sql =
                    "INSERT INTO [dbo].[zSCP2_ItemMeter] " +
                    "(ItemKey, MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                    " RebateQtyInPercent, FOCQty, InitialReading, LastModified) " +
                    "VALUES (@ik,@code,@role,@min,@rate,@multi,@rebate,@foc,@init,GETDATE());";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    string role = r["MeterRole"] == null ? "NA" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    if (role != "BK" && role != "CL") role = "NA";
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@min", AsDec(r["MinimumCharges"]));
                    cmd.Parameters.AddWithValue("@rate", AsDec(r["ChargesRate"]));
                    cmd.Parameters.AddWithValue("@multi", r["MeterMultiPriceCode"] == null ? "" : r["MeterMultiPriceCode"].ToString());
                    cmd.Parameters.AddWithValue("@rebate", AsDec(r["RebateQtyInPercent"]));
                    cmd.Parameters.AddWithValue("@foc", AsDec(r["FOCQty"]));
                    cmd.Parameters.AddWithValue("@init", AsDec(r["InitialReading"]));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ExecNonQuery(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                if (ps != null) cmd.Parameters.AddRange(ps);
                cmd.ExecuteNonQuery();
            }
        }

        private static SqlParameter P(string name, object val) { return new SqlParameter(name, val ?? DBNull.Value); }

        private void BtnClose_Click(object sender, EventArgs e) { this.Close(); }

        // ---- Ribbon button click wrappers (forward to the existing handlers) ----
        private void barSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnSave_Click(sender, System.EventArgs.Empty); }
        private void barClose_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnClose_Click(sender, System.EventArgs.Empty); }
        private void barAddItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnAddItem_Click(sender, System.EventArgs.Empty); }
        private void barEditItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnEditItem_Click(sender, System.EventArgs.Empty); }
        private void barDelItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnDelItem_Click(sender, System.EventArgs.Empty); }

        // ===================== Demo Fill (random test data) =====================

        private static readonly Random _rng = new Random();
        private static readonly string[] _demoWords = {
            "Alpha","Beta","Gamma","Delta","Prime","Nova","Metro","Summit","Vertex","Pioneer",
            "Copier","Printer","MFP","Toner","Drum","Fuser","Roller","Panel","Sensor","Unit" };
        private static readonly string[] _demoCities = { "Johor Bahru","Kuala Lumpur","Penang","Ipoh","Melaka","Shah Alam","Klang" };
        private static readonly string[] _demoStates = { "Johor","Selangor","Penang","Perak","Melaka","Kedah","Pahang" };

        private string DW() { return _demoWords[_rng.Next(_demoWords.Length)]; }
        private string RandRef() { return DW() + "-" + _rng.Next(1000, 9999); }

        // Fills EVERY field with random values (different each click) and adds random rows to both grids
        // — a demo/testing helper.
        private void barDemoFill_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try { DemoFill(); }
            catch (Exception ex) { XtraMessageBox.Show("Demo fill failed:\r\n" + ex.Message, "Demo"); }
        }

        private void DemoFill()
        {
            // --- header ---
            SluContractType.EditValue = RandomLookupValue(SluContractType);
            if (_debtorLookup != null && _debtorLookup.Rows.Count > 0)
                LkDebtorCode.EditValue = _debtorLookup.Rows[_rng.Next(_debtorLookup.Rows.Count)]["AccNo"];   // triggers auto-fill
            SluAgent.EditValue = RandomLookupValue(SluAgent);

            DateTime cd = DateTime.Today.AddDays(-_rng.Next(0, 60));
            DtContractDate.EditValue = cd;
            DtStartDate.EditValue = cd;
            DtExpiryDate.EditValue = cd.AddMonths(_rng.Next(6, 36));
            SpnContractValue.Value = _rng.Next(500, 50000);
            ChkMonthEnd.Checked = false;
            SpnBillingDay.Value = _rng.Next(1, 28);
            SetBillingMode(_rng.Next(2) == 0 ? "G" : "S");
            TxtDescription.Text = "Demo contract " + DW() + " " + _rng.Next(100, 999);
            TxtRemark1.Text = "Remark " + DW(); TxtRemark2.Text = "Remark " + DW();
            TxtNote.Text = "Auto-generated demo note " + DW() + " " + _rng.Next(1000, 9999);

            // --- More Header (over the debtor-mapped values) ---
            MhSet("City", _demoCities[_rng.Next(_demoCities.Length)]);
            MhSet("State", _demoStates[_rng.Next(_demoStates.Length)]);
            MhSet("PostalCode", _rng.Next(10000, 99999).ToString());
            MhSet("Country", "Malaysia");
            MhSet("Ref1", RandRef()); MhSet("Ref2", RandRef()); MhSet("Ref3", RandRef()); MhSet("Ref4", RandRef());

            // NOTE: service items are NOT added here — they can only be added in edit mode (after the
            // contract is saved), so the demo just fills the header + more-header + item-provided.

            // --- Item Provided: add 1-3 random rows ---
            int nSp = _rng.Next(1, 4);
            for (int i = 0; i < nSp; i++)
            {
                DataRow r = _spareParts.NewRow();
                r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
                string code = ""; string desc = DW() + " part";
                if (_spItemLookup != null && _spItemLookup.Rows.Count > 0)
                {
                    DataRow it = _spItemLookup.Rows[_rng.Next(_spItemLookup.Rows.Count)];
                    code = Convert.ToString(it["ItemCode"]); desc = Convert.ToString(it["Description"]);
                    r["UOM"] = it["UOM"]; r["TaxType"] = it["TaxType"];
                }
                else { r["UOM"] = "UNIT"; r["TaxType"] = ""; }
                r["ItemCode"] = code; r["Description"] = desc; r["Unlimited"] = false;
                r["Quantity"] = _rng.Next(1, 20); r["Discount"] = ""; r["UnitPrice"] = _rng.Next(5, 500);
                r["TaxInclusive"] = false; r["TaxRate"] = 0m; r["Pos"] = _spareParts.Rows.Count;
                ComputeSpareRow(r);
                _spareParts.Rows.Add(r);
            }
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();

            _dirty = true;
            RebuildItemsView();
            XtraMessageBox.Show("Random demo data filled (header + more header + " + nSp + " item-provided row(s)).\r\n" +
                "Save the contract, then add service items in edit mode.", "Demo Fill");
        }

        private object RandomLookupValue(DevExpress.XtraEditors.SearchLookUpEdit slu)
        {
            DataTable dt = slu.Properties.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0) return null;
            string vm = slu.Properties.ValueMember;
            if (string.IsNullOrEmpty(vm) || !dt.Columns.Contains(vm)) return null;
            return dt.Rows[_rng.Next(dt.Rows.Count)][vm];
        }

        private DataTable SafeGet(string sql)
        {
            try { return _db.GetDataTable(sql, false); } catch { return null; }
        }

        // ===================== Copy / Clipboard ribbon =====================

        private const string CLIP_HEADER = "ATP-SCP-DOC-V1";

        // "Copy from other Service Contract": pick a saved contract and load its content into THIS
        // (usually new) contract as a template — header + items (as fresh copies) + spare parts.
        private void barCopyFrom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DataTable src;
            try
            {
                src = _db.GetDataTable(
                    "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM [dbo].[zSCP2_Contract] c LEFT JOIN [dbo].[Debtor] d ON d.AccNo=c.DebtorCode " +
                    "ORDER BY c.ContractNo", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); return; }
            long key = PickContract(src, "Copy from Service Contract");
            if (key == 0) return;
            LoadContractAsTemplate(key);
            _dirty = true;
            XtraMessageBox.Show("Copied. Review the details and Save to create this contract.", "Copied",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // "Copy to a new Service Contract": open a NEW contract editor pre-filled from the current one.
        private void barCopyToNew_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_isNew && _contractKey == 0)
            { XtraMessageBox.Show("Save this contract first before copying it to a new one.", "Copy to new"); return; }
            using (zSCP2_Contract_Form f = new zSCP2_Contract_Form(_db, _contractKey, true))
            { f.ShowDialog(this); }
        }

        // Serialize header + items to the clipboard (custom tagged text).
        private void barCopyWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(CLIP_HEADER);
            sb.Append("H\t")
              .Append(Cv(SluContractType)).Append('\t').Append(Cv(LkDebtorCode)).Append('\t')
              .Append(TxtAddress.Text.Replace("\r", " ").Replace("\n", " ")).Append('\t')
              .Append(TxtAttention.Text).Append('\t').Append(TxtPhone.Text).Append('\t')
              .Append(TxtTerm.Text).Append('\t').Append(TxtArea.Text).Append('\t')
              .Append(Cv(SluAgent)).Append('\t').Append(TxtDescription.Text).Append('\t')
              .Append(((int)SpnBillingDay.Value)).Append('\t').Append(CurrentBillingMode()).AppendLine();
            foreach (ItemEditData d in _items)
                sb.Append("I\t").Append(Tsv(d.ServiceItemNo)).Append('\t').Append(Tsv(d.SerialNumber)).Append('\t')
                  .Append(Tsv(d.Description)).Append('\t')
                  .Append(d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value.ToString() : "").Append('\t')
                  .Append(Tsv(d.DepartmentCode)).Append('\t').Append(Tsv(d.JobCode)).Append('\t')
                  .Append(Tsv(d.StockLocationCode)).Append('\t').Append(d.Inactive ? "Y" : "N").AppendLine();
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show("Whole document copied to clipboard.", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
        }

        // Copy the SELECTED service item rows as tab-separated (Excel-pasteable).
        private void barCopySelected_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] sel = GridViewItems.GetSelectedRows();
            if (sel == null || sel.Length == 0) { XtraMessageBox.Show("Select one or more service item rows first.", "Copy Selected"); return; }
            System.Collections.Generic.List<int> handles = new System.Collections.Generic.List<int>(sel);
            CopyItemsAsTsv(handles);
        }

        // Copy the WHOLE service item grid as a spreadsheet (TSV with a header row).
        private void barCopySpreadsheet_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Collections.Generic.List<int> handles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < GridViewItems.RowCount; i++) handles.Add(GridViewItems.GetVisibleRowHandle(i));
            CopyItemsAsTsv(handles);
        }

        private void CopyItemsAsTsv(System.Collections.Generic.List<int> handles)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("No\tService Item No\tSerial Number\tProvided Items\tBilling Day\tBlack Meter\tColour Meter\tInactive\tExpiry");
            foreach (int rh in handles)
            {
                if (rh < 0) continue;
                sb.Append(Gv(rh, "No")).Append('\t').Append(Gv(rh, "ServiceItemNo")).Append('\t')
                  .Append(Gv(rh, "SerialNumber")).Append('\t').Append(Gv(rh, "Items")).Append('\t')
                  .Append(Gv(rh, "BillingDay")).Append('\t').Append(Gv(rh, "BKMeter")).Append('\t')
                  .Append(Gv(rh, "CLMeter")).Append('\t').Append(Gv(rh, "Inactive")).Append('\t')
                  .Append(Gv(rh, "Expiry")).AppendLine();
            }
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show(handles.Count + " row(s) copied.", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
        }

        private string Gv(int rh, string field)
        {
            object v = GridViewItems.GetRowCellValue(rh, field);
            return v == null || v == DBNull.Value ? "" : v.ToString().Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        // Paste a whole document (from Copy Whole Document): replaces header + items in this form.
        private void barPasteWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (text == null || !text.StartsWith(CLIP_HEADER))
            { XtraMessageBox.Show("The clipboard does not contain a copied Service Contract document.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            _items.Clear();
            foreach (string line in lines)
            {
                string[] p = line.Split('\t');
                if (p.Length == 0) continue;
                if (p[0] == "H" && p.Length >= 13)
                {
                    SluContractType.EditValue = SetOrNull(p[1]);
                    LkDebtorCode.EditValue = SetOrNull(p[2]);
                    TxtAddress.Text = p[3]; TxtAttention.Text = p[4]; TxtPhone.Text = p[5];
                    TxtTerm.Text = p[6]; TxtArea.Text = p[7]; SluAgent.EditValue = SetOrNull(p[8]);
                    TxtDescription.Text = p[9];
                    int bd; if (int.TryParse(p[10], out bd)) SpnBillingDay.Value = bd;
                    SetBillingMode(p[11]);
                }
                else if (p[0] == "I") _items.Add(ItemFromTsv(p, 1));
            }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show("Document pasted. Review and Save.", "Pasted");
        }

        // Paste ONLY item detail rows (accepts our I-lines or plain TSV): appends to the item list.
        private void barPasteItems_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (string.IsNullOrEmpty(text)) { XtraMessageBox.Show("Clipboard is empty.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int added = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0) continue;
                string[] p = line.Split('\t');
                if (p[0] == CLIP_HEADER || p[0] == "H") continue;
                if (p[0] == "I") { _items.Add(ItemFromTsv(p, 1)); added++; }
                else if (p.Length >= 2 && p[0] != "No")   // plain TSV: No, ServiceItemNo, Serial, ...
                { _items.Add(ItemFromTsv(p, 1)); added++; }
            }
            if (added == 0) { XtraMessageBox.Show("No item rows found on the clipboard.", "Paste"); return; }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show(added + " item(s) pasted.", "Pasted");
        }

        // Build a fresh ItemEditData (ItemKey=0 => inserted as new) from a tab-split line at offset.
        private ItemEditData ItemFromTsv(string[] p, int off)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            d.ServiceItemNo = At(p, off); d.SerialNumber = At(p, off + 1); d.Description = At(p, off + 2);
            int bd; d.BillingDayOverride = int.TryParse(At(p, off + 3), out bd) ? (int?)bd : null;
            d.DepartmentCode = At(p, off + 4); d.JobCode = At(p, off + 5); d.StockLocationCode = At(p, off + 6);
            d.Inactive = At(p, off + 7) == "Y";
            return d;
        }

        private static string At(string[] a, int i) { return (i >= 0 && i < a.Length) ? a[i] : ""; }
        private static string Tsv(string s) { return (s ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " "); }
        private static string Cv(DevExpress.XtraEditors.BaseEdit ed) { return ed.EditValue == null ? "" : ed.EditValue.ToString(); }
        private static string SafeClipboardText()
        { try { return System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null; } catch { return null; } }

        // Load another contract's content into THIS form as a template (fresh copies; keeps this form new).
        private void LoadContractAsTemplate(long sourceKey)
        {
            _loading = true;
            try
            {
                DataTable dt = _db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + sourceKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                SluContractType.EditValue = SetOrNull(AsStr(r["ContractTypeCode"]));
                LkDebtorCode.EditValue = AsStr(r["DebtorCode"]);
                DtStartDate.EditValue = AsDate(r["ServiceStartDate"]);
                DtExpiryDate.EditValue = AsDate(r["ServiceExpiryDate"]);
                SpnContractValue.Value = AsDec(r["ContractValue"]);
                SpnBillingDay.Value = AsInt(r["BillingDay"], 1);
                SetBillingMode(AsStr(r["BillingMode"]));
                TxtAddress.Text = AsStr(r["Address1"]); TxtAttention.Text = AsStr(r["Attention"]);
                TxtPhone.Text = AsStr(r["Phone"]); TxtTerm.Text = AsStr(r["TermCode"]);
                TxtArea.Text = AsStr(r["AreaCode"]); SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
                TxtDescription.Text = AsStr(r["Description"]);
                TxtRemark1.Text = AsStr(r["Remark1"]); TxtRemark2.Text = AsStr(r["Remark2"]); TxtNote.Text = AsStr(r["Note"]);

                _items.Clear();
                DataTable it = _db.GetDataTable("SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=" + sourceKey + " ORDER BY Pos, ItemKey", false);
                foreach (DataRow ir in it.Rows)
                {
                    ItemEditData d = LoadOneItem(Convert.ToInt64(ir["ItemKey"]));
                    d.ItemKey = 0;                 // fresh copy -> inserted as a new item
                    d.ServiceItemNoIsAuto = true;  // draw a new number on save
                    _items.Add(d);
                }
            }
            finally { _loading = false; }
            RebuildItemsView();
        }

        // ---- value helpers ----
        private static string AsStr(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal AsDec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static int AsInt(object o, int def) { int n; return (o != null && o != DBNull.Value && int.TryParse(o.ToString(), out n)) ? n : def; }
        private static object AsDate(object o) { return (o == null || o == DBNull.Value) ? null : (object)Convert.ToDateTime(o); }
        private static object DateParam(object editValue)
        {
            if (editValue == null || editValue == DBNull.Value) return DBNull.Value;
            DateTime dt;
            if (DateTime.TryParse(editValue.ToString(), out dt)) return dt;
            return DBNull.Value;
        }
    }
}
