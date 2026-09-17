using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Where the other company's account book is, and proof that it can be reached.
    ///
    /// <para>Inter-billing runs between two books of the same owner. One holds the machines and takes
    /// the readings; the other holds the end customer and issues the bill at its own prices. The
    /// billing book PULLS -- it is the only side that knows which contracts it wants and which
    /// customer to put them on -- so it is the billing book that keeps this connection, and every use
    /// of it is a read.</para>
    ///
    /// <para>Test is not a convenience button. Until a book has been reached it has no identity here,
    /// and nothing can be linked to a book with no identity: a link that cannot name its source is a
    /// link to nowhere. So the identity is learned by the test and never typed.</para>
    /// </summary>
    // Plain single-instance window, like Document Numbering Format next door.
    [AutoCount.PlugIn.MenuItem("Inter-Billing Setup",
    ParentMenuCaption = "General Setup", MenuOrder = 160, ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_INTERBILL,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_INTERBILL)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Normal, false)]
    public partial class InterBillSetup_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dt;
        private List<ScpInterBillBook> _books = new List<ScpInterBillBook>();
        private ScpInterBillBook _current;
        private bool _loading;
        private bool _dirty;

        public InterBillSetup_Form() { InitializeComponent(); ApplyAutoCountToolbarImages(); }

        public InterBillSetup_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public InterBillSetup_Form(DBSetting dbSetting) : this()
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
                this.BtnNew.ImageOptions.Image = img.GetLargeImage_New();
                this.BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                this.BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete();
                this.BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                this.BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            LblThisBook.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblPasswordNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblPasswordNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);
            LblMarginNote.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            LblMarginNote.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            if (_dbSetting == null) return;

            ShowThisBook();
            GridView.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(OnFocusedRowChanged);
            TxtAlias.EditValueChanged += new EventHandler(OnFieldEdited);
            TxtServer.EditValueChanged += new EventHandler(OnFieldEdited);
            TxtDatabase.EditValueChanged += new EventHandler(OnFieldEdited);
            TxtUser.EditValueChanged += new EventHandler(OnFieldEdited);
            TxtPassword.EditValueChanged += new EventHandler(OnFieldEdited);
            ChkWindowsAuth.CheckedChanged += new EventHandler(OnFieldEdited);
            SpnMargin.EditValueChanged += new EventHandler(OnFieldEdited);
            ChkInactive.CheckedChanged += new EventHandler(OnFieldEdited);
            ChkIsHq.CheckedChanged += new EventHandler(OnFieldEdited);
            LoadGrid(0);
        }

        /// <summary>Names this book to whoever is setting the other one up. Two books of one owner
        /// look alike from a login screen, and pointing the connection at the book you are already in
        /// is the easiest mistake available -- so what you are standing in is written down.</summary>
        private void ShowThisBook()
        {
            string company = "";
            try
            {
                object o = _dbSetting.ExecuteScalar("SELECT TOP 1 CompanyName FROM dbo.Profile");
                company = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            }
            catch { }

            Guid id = ScpBookIdentity.Local(_dbSetting);
            string db = "";
            try { db = new System.Data.SqlClient.SqlConnectionStringBuilder(_dbSetting.ConnectionString).InitialCatalog; }
            catch { }

            LblThisBook.Text =
                (company.Length > 0 ? company + "   -   " : "") + db + Environment.NewLine +
                (id == Guid.Empty
                    ? "This book has no identity yet. Close and reopen it once so the plug-in can issue one."
                    : "Book id " + id.ToString().ToUpperInvariant());
        }

        private void LoadGrid(int selectKey)
        {
            _loading = true;
            try
            {
                _books = ScpInterBillBooks.LoadAll(_dbSetting);

                _dt = new DataTable();
                _dt.Columns.Add("BookKey", typeof(int));
                _dt.Columns.Add("Alias", typeof(string));
                _dt.Columns.Add("ServerName", typeof(string));
                _dt.Columns.Add("DatabaseName", typeof(string));
                _dt.Columns.Add("RemoteCompany", typeof(string));
                // Not the raw date: what a person wants to know is whether it works, and a date on its
                // own does not say whether that test succeeded.
                _dt.Columns.Add("Reached", typeof(string));

                ScpInterBillBook hq = ScpInterBillBooks.HqBook(_dbSetting);
                foreach (ScpInterBillBook b in _books)
                {
                    DataRow r = _dt.NewRow();
                    r["BookKey"] = b.BookKey;
                    r["Alias"] = b.Alias + (b.Inactive ? "   (not in use)" : "") +
                                 (hq != null && hq.BookKey == b.BookKey ? "   (HQ)" : "");
                    r["ServerName"] = b.ServerName;
                    r["DatabaseName"] = b.DatabaseName;
                    r["RemoteCompany"] = b.RemoteCompany;
                    r["Reached"] = b.RemoteBookId == Guid.Empty
                        ? "never"
                        : (b.LastTestedAt.HasValue ? b.LastTestedAt.Value.ToString("dd/MM/yyyy HH:mm") : "yes");
                    _dt.Rows.Add(r);
                }

                Grid.DataSource = _dt;

                int want = 0;
                for (int i = 0; i < _books.Count; i++)
                    if (_books[i].BookKey == selectKey) want = i;
                if (_dt.Rows.Count > 0) GridView.FocusedRowHandle = want;
            }
            finally { _loading = false; }

            Bind(_dt.Rows.Count > 0 ? _books[Math.Max(0, GridView.FocusedRowHandle)] : null);
            _dirty = false;
        }

        private void OnFocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading) return;
            if (_dirty && !ConfirmDiscard()) { return; }
            int i = GridView.FocusedRowHandle;
            Bind(i >= 0 && i < _books.Count ? _books[i] : null);
        }

        private void Bind(ScpInterBillBook b)
        {
            _loading = true;
            try
            {
                _current = b;
                bool has = b != null;
                TxtAlias.Text = has ? b.Alias : "";
                TxtServer.Text = has ? b.ServerName : "";
                TxtDatabase.Text = has ? b.DatabaseName : "";
                ChkWindowsAuth.Checked = has && b.WindowsAuth;
                TxtUser.Text = has ? b.DbUser : "";
                TxtPassword.Text = "";
                SpnMargin.EditValue = has ? b.MarginPercent : 0m;
                ChkInactive.Checked = has && b.Inactive;
                ScpInterBillBook hqNow = has ? ScpInterBillBooks.HqBook(_dbSetting) : null;
                ChkIsHq.Checked = has && hqNow != null && hqNow.BookKey == b.BookKey;

                TxtPassword.Properties.NullValuePrompt =
                    has && b.HasPassword ? "(a password is stored)" : "";
                TxtPassword.Properties.ShowNullValuePromptWhenFocused = true;

                MemoResult.Text = !has ? "" :
                    (b.LastTestedAt.HasValue
                        ? b.LastTestedAt.Value.ToString("dd/MM/yyyy HH:mm") + Environment.NewLine + Environment.NewLine
                        : "") + b.LastTestResult;

                PanelDetail.Enabled = has;
                ApplyAuthMode();
            }
            finally { _loading = false; }
            _dirty = false;
        }

        private void ApplyAuthMode()
        {
            bool win = ChkWindowsAuth.Checked;
            LblUser.Enabled = !win;
            TxtUser.Enabled = !win;
            LblPassword.Enabled = !win;
            TxtPassword.Enabled = !win;
            LblPasswordNote.Enabled = !win;
        }

        /// <summary>The ... button beside the database name: every database on that server, with the
        /// company in it, so the one step nobody can check by eye is a choice rather than a guess.</summary>
        private void OnPickDatabase(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            string picked = PickDatabase_Form.Pick(
                this, TxtServer.Text, ChkWindowsAuth.Checked, TxtUser.Text,
                // The password just typed if there is one, else the stored one -- the list has to be
                // reachable before the row has ever been saved.
                TxtPassword.Text.Length > 0
                    ? TxtPassword.Text
                    : (_current == null ? "" : ScpInterBillBooks.PasswordOf(_dbSetting, _current.BookKey)));
            if (picked.Length == 0) return;
            TxtDatabase.Text = picked;
            _dirty = true;
        }

        private void OnWindowsAuthChanged(object sender, EventArgs e)
        {
            ApplyAuthMode();
            if (!_loading) _dirty = true;
        }

        private void OnFieldEdited(object sender, EventArgs e)
        {
            if (!_loading) _dirty = true;
        }

        /// <summary>Reads the screen back into the row. Everything that writes -- Save and Test
        /// both -- goes through here, so the two can never disagree about what was typed.</summary>
        private void Harvest()
        {
            if (_current == null) return;
            _current.Alias = TxtAlias.Text.Trim();
            _current.ServerName = TxtServer.Text.Trim();
            _current.DatabaseName = TxtDatabase.Text.Trim();
            _current.WindowsAuth = ChkWindowsAuth.Checked;
            _current.DbUser = TxtUser.Text.Trim();
            _current.Password = TxtPassword.Text;      // empty = keep the stored one
            try { _current.MarginPercent = Convert.ToDecimal(SpnMargin.EditValue); }
            catch { _current.MarginPercent = 0m; }
            _current.Inactive = ChkInactive.Checked;
        }

        private bool Validate(out string problem)
        {
            problem = "";
            if (_current == null) { problem = "Add a connection first."; return false; }
            if (_current.Alias.Length == 0)
            {
                problem = "Give this connection a name -- it is what every other screen calls the " +
                          "other book.";
                return false;
            }
            if (_current.ServerName.Length == 0 || _current.DatabaseName.Length == 0)
            {
                problem = "The SQL server and the account book are both needed.";
                return false;
            }
            if (!_current.WindowsAuth && _current.DbUser.Length == 0)
            {
                problem = "Enter the user name, or tick to sign in as the Windows user.";
                return false;
            }
            return true;
        }

        private void OnNew(object sender, EventArgs e)
        {
            if (_dirty && !ConfirmDiscard()) return;
            ScpInterBillBook b = new ScpInterBillBook();
            _books.Add(b);

            DataRow r = _dt.NewRow();
            r["BookKey"] = 0;
            r["Alias"] = "";
            r["ServerName"] = "";
            r["DatabaseName"] = "";
            r["RemoteCompany"] = "";
            r["Reached"] = "never";
            _loading = true;
            try
            {
                _dt.Rows.Add(r);
                GridView.FocusedRowHandle = _dt.Rows.Count - 1;
            }
            finally { _loading = false; }

            Bind(b);
            TxtAlias.Focus();
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_current == null) return;
            Harvest();
            string problem;
            if (!Validate(out problem))
            {
                XtraMessageBox.Show(problem, "Inter-Billing Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int key = ScpInterBillBooks.Save(_dbSetting, _current);
                // One HQ only: ticking this connection makes it HQ; unticking the HQ clears it.
                if (ChkIsHq.Checked && !_current.Inactive) ScpInterBillBooks.SetHqBook(_dbSetting, key);
                else if (ScpInterBillBooks.HqBookKey(_dbSetting) == key) ScpInterBillBooks.SetHqBook(_dbSetting, 0);
                _dirty = false;
                LoadGrid(key);
                XtraMessageBox.Show(
                    _current != null && _current.RemoteBookId == Guid.Empty
                        ? "Saved. Now press Test connection -- until the other book has been reached, " +
                          "nothing can be taken from it."
                        : "Saved.",
                    "Inter-Billing Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:" + Environment.NewLine + ex.Message, "Error");
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_current == null) return;
            if (_current.BookKey <= 0) { LoadGrid(0); return; }

            // What is already linked keeps working off the machines and readings ALREADY taken; what
            // stops is fetching anything new. Saying so is the difference between a delete somebody
            // can judge and one they have to find out about at month end.
            if (XtraMessageBox.Show(
                    "Remove the connection to " + _current.Display + "?" + Environment.NewLine +
                    Environment.NewLine +
                    "Contracts already taken from it stay exactly as they are -- they are this book's " +
                    "own contracts. What stops is taking anything new, and fetching this month's " +
                    "readings.",
                    "Inter-Billing Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            try
            {
                ScpInterBillBooks.Delete(_dbSetting, _current.BookKey);
                LoadGrid(0);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Delete failed:" + Environment.NewLine + ex.Message, "Error");
            }
        }

        private void OnTest(object sender, EventArgs e)
        {
            if (_current == null) return;
            Harvest();
            string problem;
            if (!Validate(out problem))
            {
                XtraMessageBox.Show(problem, "Inter-Billing Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string message;
            bool ok;
            try
            {
                ok = ScpInterBillBooks.Test(_dbSetting, _current, out message);
            }
            finally { this.Cursor = old; }

            MemoResult.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
                              Environment.NewLine + Environment.NewLine + message;

            if (ok)
            {
                // The identity only just arrived, and it is the thing that makes the row usable. An
                // unsaved one would have to be tested again tomorrow.
                if (_current.BookKey > 0 || !_dirty)
                {
                    try { ScpInterBillBooks.Save(_dbSetting, _current); _dirty = false; }
                    catch { }
                }
                LoadGrid(_current.BookKey);
                XtraMessageBox.Show(message, "Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                XtraMessageBox.Show(message, "Could not connect",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            if (_dirty && !ConfirmDiscard()) return;
            LoadGrid(_current == null ? 0 : _current.BookKey);
        }

        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private bool ConfirmDiscard()
        {
            return XtraMessageBox.Show(
                "You have unsaved changes. Discard them?", "Inter-Billing Setup",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty && !ConfirmDiscard()) { e.Cancel = true; return; }
            base.OnFormClosing(e);
        }
    }
}
