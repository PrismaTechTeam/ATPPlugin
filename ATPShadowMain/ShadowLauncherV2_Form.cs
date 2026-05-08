using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using AutoCount.Data;

using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraNavBar;
using DevExpress.XtraTab;
using DevExpress.XtraTab.ViewInfo;

namespace ATPShadowMain
{
    /// <summary>
    /// Tabbed dev shell — chrome (top bar / nav / tabs / status) lives in the
    /// Designer file so VS Designer can render and edit it. Dynamic content
    /// (KPI cards, quick-access tiles, NavBar items from the catalog) is added
    /// at runtime since it's data-driven and can't be expressed in the designer.
    /// </summary>
    public partial class ShadowLauncherV2_Form : XtraForm
    {
        private readonly DBSetting _db;

        // Tab tracking — entry-title -> page so we don't reopen duplicates
        private readonly Dictionary<string, XtraTabPage> _openTabs =
            new Dictionary<string, XtraTabPage>(StringComparer.OrdinalIgnoreCase);

        // Dynamic dashboard state (created at runtime, parented to PanelDashboard)
        private LabelControl[] _kpiValues = new LabelControl[5];
        private static readonly string[] KPI_TABLES =
            { "zSCP_ServiceContract", "zSCP_ServiceItem", "zSCP_ServiceNote", "zSCP_Appointment", "zSCP_MeterTrans" };
        private static readonly string[] KPI_LABELS =
            { "Service Contracts", "Service Items", "Service Notes", "Appointments", "Meter Readings" };
        private static readonly Color[] KPI_COLORS = {
            Color.FromArgb(33, 150, 243),   // blue
            Color.FromArgb(76, 175, 80),    // green
            Color.FromArgb(255, 152, 0),    // orange
            Color.FromArgb(156, 39, 176),   // purple
            Color.FromArgb(244, 67, 54)     // red
        };

        public ShadowLauncherV2_Form()
        {
            InitializeComponent();
            this.BtnRefresh.Click += new EventHandler(OnRefreshClicked);
            this.NavLeft.LinkClicked += new NavBarLinkEventHandler(OnNavLinkClicked);
            this.TabsMain.CloseButtonClick += new EventHandler(OnTabCloseClicked);
            this.TabsMain.SelectedPageChanged += new TabPageChangedEventHandler(OnTabChanged);
        }

        public ShadowLauncherV2_Form(DBSetting db) : this()
        {
            _db = db;
            BuildKpiCards();
            BuildQuickTiles();
            this.LblStatus.Text = BuildStatusText();
            RefreshDashboard();
        }

        // ============================================================
        // Status bar
        // ============================================================
        private string BuildStatusText()
        {
            string user = AutoCount.Authentication.UserSession.CurrentUserSession != null
                ? AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID
                : "(none)";
            string srv = _db != null ? _db.ServerName : "(none)";
            string dbName = _db != null ? _db.DBName : "(none)";
            int openCount = _openTabs != null ? _openTabs.Count : 0;
            return string.Format("User: {0}    DB: {1} @ {2}    Open tabs: {3}",
                user, dbName, srv, openCount);
        }

        // ============================================================
        // Nav — groups + items are statically declared in the Designer
        // (see ShadowLauncherV2_Form.Designer.cs). OnNavLinkClicked
        // dispatches by Caption via the FormCatalog.
        // ============================================================

        // ============================================================
        // Dashboard — KPI cards (dynamic, parented to PanelDashboard)
        // ============================================================
        private void BuildKpiCards()
        {
            int kpiY = 84;
            int kpiW = 180;
            int kpiH = 96;
            int kpiGap = 14;
            for (int i = 0; i < KPI_TABLES.Length; i++)
            {
                PanelControl card = new PanelControl();
                card.Location = new Point(24 + i * (kpiW + kpiGap), kpiY);
                card.Size = new Size(kpiW, kpiH);
                card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                card.Appearance.BackColor = KPI_COLORS[i];
                card.Appearance.Options.UseBackColor = true;

                LabelControl val = new LabelControl();
                val.Text = "—";
                val.Appearance.Font = new Font("Tahoma", 24F, FontStyle.Bold);
                val.Appearance.ForeColor = Color.White;
                val.Appearance.BackColor = Color.Transparent;
                val.Appearance.Options.UseFont = true;
                val.Appearance.Options.UseForeColor = true;
                val.Appearance.Options.UseBackColor = true;
                val.Appearance.Options.UseTextOptions = true;
                val.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
                val.Appearance.TextOptions.VAlignment = VertAlignment.Center;
                val.Location = new Point(0, 12);
                val.Size = new Size(kpiW, 42);
                val.AutoSizeMode = LabelAutoSizeMode.None;
                card.Controls.Add(val);
                this._kpiValues[i] = val;

                LabelControl cap = new LabelControl();
                cap.Text = KPI_LABELS[i];
                cap.Appearance.Font = new Font("Tahoma", 10F);
                cap.Appearance.ForeColor = Color.White;
                cap.Appearance.BackColor = Color.Transparent;
                cap.Appearance.Options.UseFont = true;
                cap.Appearance.Options.UseForeColor = true;
                cap.Appearance.Options.UseBackColor = true;
                cap.Appearance.Options.UseTextOptions = true;
                cap.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
                cap.Location = new Point(0, 60);
                cap.Size = new Size(kpiW, 24);
                cap.AutoSizeMode = LabelAutoSizeMode.None;
                card.Controls.Add(cap);

                this.PanelDashboard.Controls.Add(card);
            }
        }

        // ============================================================
        // Dashboard — Quick Access tiles (dynamic)
        // ============================================================
        private void BuildQuickTiles()
        {
            QuickTile[] tiles = new QuickTile[]
            {
                new QuickTile("Key Meter Reading",     "Record meter + generate invoice", Color.FromArgb(244, 67, 54),  "Meter Type Trans Entry"),
                new QuickTile("New Service Note",      "Open a service ticket",           Color.FromArgb(255, 152, 0),  "New Service Note"),
                new QuickTile("Maintain Service Item", "Browse / edit machines",          Color.FromArgb(33, 150, 243), "Maintain Service Item"),
                new QuickTile("New Service Contract",  "Set up a billing contract",       Color.FromArgb(76, 175, 80),  "New Service Contract"),
                new QuickTile("Service Quick View",    "Snapshot inquiry",                Color.FromArgb(96, 125, 139), "Service Quick View"),
                new QuickTile("Appointment Calendar",  "Today's & upcoming visits",       Color.FromArgb(156, 39, 176), "Appointment Calendar")
            };
            int tileY = this.LblQuickAccess.Bottom + 10;
            int tileW = 300;
            int tileH = 88;
            int tileGap = 14;
            int cols = 3;
            for (int i = 0; i < tiles.Length; i++)
            {
                int row = i / cols;
                int col = i % cols;
                this.PanelDashboard.Controls.Add(BuildTile(
                    tiles[i],
                    new Point(24 + col * (tileW + tileGap), tileY + row * (tileH + tileGap)),
                    new Size(tileW, tileH)));
            }
        }

        private sealed class QuickTile
        {
            public string Caption;
            public string Subtitle;
            public Color BackColor;
            public string TargetTitle;
            public QuickTile(string c, string s, Color bg, string t)
            { Caption = c; Subtitle = s; BackColor = bg; TargetTitle = t; }
        }

        private PanelControl BuildTile(QuickTile t, Point loc, Size sz)
        {
            PanelControl p = new PanelControl();
            p.Location = loc;
            p.Size = sz;
            p.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            p.Appearance.BackColor = t.BackColor;
            p.Appearance.Options.UseBackColor = true;
            p.Cursor = Cursors.Hand;

            LabelControl cap = new LabelControl();
            cap.Text = t.Caption;
            cap.Appearance.Font = new Font("Tahoma", 12F, FontStyle.Bold);
            cap.Appearance.ForeColor = Color.White;
            cap.Appearance.BackColor = Color.Transparent;
            cap.Appearance.Options.UseFont = true;
            cap.Appearance.Options.UseForeColor = true;
            cap.Appearance.Options.UseBackColor = true;
            cap.Location = new Point(14, 14);
            cap.AutoSizeMode = LabelAutoSizeMode.None;
            cap.Size = new Size(sz.Width - 28, 22);
            cap.Cursor = Cursors.Hand;
            p.Controls.Add(cap);

            LabelControl sub = new LabelControl();
            sub.Text = t.Subtitle;
            sub.Appearance.Font = new Font("Tahoma", 9F);
            sub.Appearance.ForeColor = Color.FromArgb(240, 240, 240);
            sub.Appearance.BackColor = Color.Transparent;
            sub.Appearance.Options.UseFont = true;
            sub.Appearance.Options.UseForeColor = true;
            sub.Appearance.Options.UseBackColor = true;
            sub.Location = new Point(14, 42);
            sub.AutoSizeMode = LabelAutoSizeMode.None;
            sub.Size = new Size(sz.Width - 28, 36);
            sub.Cursor = Cursors.Hand;
            p.Controls.Add(sub);

            // Click anywhere on the tile opens the form
            string target = t.TargetTitle;
            EventHandler open = new EventHandler((s, e) => OpenByTitle(target));
            p.Click += open;
            cap.Click += open;
            sub.Click += open;

            return p;
        }

        private void RefreshDashboard()
        {
            for (int i = 0; i < KPI_TABLES.Length; i++)
            {
                if (this._kpiValues[i] != null)
                    this._kpiValues[i].Text = SafeCount(KPI_TABLES[i]);
            }
        }

        private string SafeCount(string table)
        {
            if (_db == null) return "—";
            try
            {
                System.Data.DataTable dt = _db.GetDataTable(
                    "SELECT COUNT(*) FROM [dbo].[" + table + "]", false);
                if (dt == null || dt.Rows.Count == 0) return "—";
                return Convert.ToInt64(dt.Rows[0][0]).ToString("n0");
            }
            catch { return "—"; }
        }

        public void OpenByTitle(string title)
        {
            XtraTabPage existing;
            if (this._openTabs.TryGetValue(title, out existing))
            {
                this.TabsMain.SelectedTabPage = existing;
                return;
            }
            foreach (CatalogEntry entry in FormCatalog.All())
            {
                if (string.Equals(entry.Title, title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Form frm = entry.Create(_db);
                        if (frm != null) EmbedFormInTab(entry, frm);
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(
                            "Failed to open '" + title + "':\r\n\r\n" + ex.Message,
                            "Open Form", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }
            }
            XtraMessageBox.Show("No form titled '" + title + "' in catalog.",
                "Quick Access", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ============================================================
        // Open / close tabs
        // ============================================================
        private void OnNavLinkClicked(object sender, NavBarLinkEventArgs e)
        {
            string title = e.Link.Item.Caption;
            OpenByTitle(title);
        }

        private void EmbedFormInTab(CatalogEntry entry, Form frm)
        {
            // The 3 magic flags that turn a Form into a Control hostable in a panel.
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;

            // Maximized makes no sense embedded — fall back to Normal.
            if (frm.WindowState == FormWindowState.Maximized)
                frm.WindowState = FormWindowState.Normal;

            // If the form closes itself (e.g. its own Exit button) we want to also
            // remove its tab page rather than leave a dead tab around.
            frm.FormClosed += new FormClosedEventHandler(OnEmbeddedFormClosed);

            XtraTabPage page = new XtraTabPage();
            page.Text = entry.Title;
            page.ShowCloseButton = DefaultBoolean.True;
            page.Tag = frm;
            page.Controls.Add(frm);
            frm.Show();   // makes it visible inside the panel

            this.TabsMain.TabPages.Add(page);
            this._openTabs[entry.Title] = page;
            this.TabsMain.SelectedTabPage = page;

            this.LblStatus.Text = BuildStatusText();
        }

        private void OnTabCloseClicked(object sender, EventArgs e)
        {
            ClosePageButtonEventArgs args = e as ClosePageButtonEventArgs;
            if (args == null) return;
            XtraTabPage page = args.Page as XtraTabPage;
            if (page == null || page == this.TabPageMaster) return;

            CloseTab(page);
        }

        private void CloseTab(XtraTabPage page)
        {
            Form frm = page.Tag as Form;

            // Remove from registry first so OnEmbeddedFormClosed doesn't loop.
            string key = null;
            foreach (KeyValuePair<string, XtraTabPage> kv in this._openTabs)
            {
                if (kv.Value == page) { key = kv.Key; break; }
            }
            if (key != null) this._openTabs.Remove(key);

            this.TabsMain.TabPages.Remove(page);

            if (frm != null && !frm.IsDisposed)
            {
                try { frm.FormClosed -= new FormClosedEventHandler(OnEmbeddedFormClosed); } catch { }
                try { frm.Close(); } catch { }
                try { frm.Dispose(); } catch { }
            }
            page.Dispose();

            this.LblStatus.Text = BuildStatusText();
        }

        private void OnEmbeddedFormClosed(object sender, FormClosedEventArgs e)
        {
            Form frm = sender as Form;
            if (frm == null) return;

            // Find the matching tab and remove it
            XtraTabPage owning = null;
            foreach (XtraTabPage p in this.TabsMain.TabPages)
            {
                if (ReferenceEquals(p.Tag, frm)) { owning = p; break; }
            }
            if (owning != null && owning != this.TabPageMaster) CloseTab(owning);
        }

        private void OnTabChanged(object sender, TabPageChangedEventArgs e)
        {
            if (e.Page != null)
                this.LblBreadcrumb.Text = "ATP  /  " + e.Page.Text;
            // When user comes back to the Master tab, refresh the KPIs so counts stay live.
            if (e.Page == this.TabPageMaster) RefreshDashboard();
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            // On Master tab → refresh dashboard counts. On any other tab → nudge the
            // embedded form to repaint (most grids re-query on their own Activated event).
            if (this.TabsMain.SelectedTabPage == this.TabPageMaster)
            {
                RefreshDashboard();
            }
            else if (this.TabsMain.SelectedTabPage != null)
            {
                this.TabsMain.SelectedTabPage.Refresh();
                Form frm = this.TabsMain.SelectedTabPage.Tag as Form;
                if (frm != null) try { frm.Refresh(); } catch { }
            }
            this.LblStatus.Text = BuildStatusText();
        }
    }
}
