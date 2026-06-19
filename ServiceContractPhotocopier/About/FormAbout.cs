using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using Newtonsoft.Json.Linq;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.About
{
    /// <summary>
    /// About dialog. Shows the plugin version + the ATPApi service version, and a
    /// [Check for Updates] button that reads the S3 release manifest and launches
    /// ATPApiUpdater.exe (elevated) when a newer API version is available.
    ///
    /// The auto-update targets the ATPApi SERVICE (which the updater can stop/swap/start).
    /// The plugin itself is updated separately through the AutoCount Plug-in Manager.
    /// </summary>
    [AutoCount.PlugIn.MenuItem("About / Check for Updates", MenuOrder = 9000)]
    public partial class FormAbout : XtraForm
    {
        private const string UpdateManifestUrl =
            "https://prisma-atp-updates.s3.ap-southeast-5.amazonaws.com/atp/latest.json";
        private const string DefaultUpdaterPath = @"C:\Program Files\ATPApi\ATPApiUpdater.exe";
        private const string ServiceName = "ATPApi";
        private const string InstallDir  = @"C:\Program Files\ATPApi";

        private string _apiBaseUrl = PumsConfig.DEFAULT_ATP_API_BASE_URL;

        private Label _lblPluginVersion;
        private Label _lblApiVersion;
        private Label _lblLatestVersion;
        private Label _lblStatus;
        private Button _btnCheck;
        private Button _btnInstall;

        private JObject _latestManifest;

        public FormAbout()
        {
            InitializeComponent();
            BuildUi();
            Load += async (_, __) =>
            {
                PopulateVersions();
                await CheckForUpdateAsync();
            };
        }

        public FormAbout(DBSetting db) : this() { ApplyDb(db); }
        public FormAbout(UserSession session) : this() { ApplyDb(session != null ? session.DBSetting : null); }

        private void ApplyDb(DBSetting db)
        {
            if (db == null) return;
            try
            {
                string v = PumsConfig.Get(db, PumsConfig.KEY_ATP_API_BASE_URL, PumsConfig.DEFAULT_ATP_API_BASE_URL);
                if (!string.IsNullOrWhiteSpace(v)) _apiBaseUrl = v.TrimEnd('/');
            }
            catch { /* fall back to default */ }
        }

        private void BuildUi()
        {
            Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label
            {
                Text = "Service Contract Photocopier",
                Font = new Font("Segoe UI Semibold", 16F),
                Location = new Point(20, 18),
                AutoSize = true
            };
            Label lblSubtitle = new Label
            {
                Text = "Service & Contract, Meter Reading and Stock Request\r\nintegration for AutoCount.",
                Location = new Point(20, 52),
                Size = new Size(420, 36),
                ForeColor = Color.DimGray
            };
            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            AddSeparator(95);

            AddFieldLabel("Plugin version:",   105);
            AddFieldLabel("API version:",      128);
            AddFieldLabel("Latest available:", 151);

            _lblPluginVersion = new Label { Location = new Point(160, 105), AutoSize = true };
            _lblApiVersion    = new Label { Location = new Point(160, 128), AutoSize = true };
            _lblLatestVersion = new Label { Location = new Point(160, 151), AutoSize = true, ForeColor = Color.DimGray, Text = "checking…" };
            Controls.Add(_lblPluginVersion);
            Controls.Add(_lblApiVersion);
            Controls.Add(_lblLatestVersion);

            AddSeparator(185);
            _btnCheck = new Button { Text = "Check for Updates", Location = new Point(20, 200), Size = new Size(160, 30) };
            _btnCheck.Click += async (_, __) => { _lblLatestVersion.Text = "checking…"; _lblLatestVersion.ForeColor = Color.DimGray; await CheckForUpdateAsync(); };

            _btnInstall = new Button { Text = "Install Update", Location = new Point(190, 200), Size = new Size(160, 30), Enabled = false };
            _btnInstall.Click += (_, __) => LaunchUpdater();

            _lblStatus = new Label { Location = new Point(20, 240), Size = new Size(420, 36), ForeColor = Color.DimGray };
            Controls.Add(_btnCheck);
            Controls.Add(_btnInstall);
            Controls.Add(_lblStatus);

            AddSeparator(285);
            Label lblCompany = new Label
            {
                Text = "Prisma Technology Solution Sdn Bhd",
                Font = new Font("Segoe UI Semibold", 9.5F),
                Location = new Point(20, 300),
                AutoSize = true
            };
            Label lblContact = new Label
            {
                Text = "For support, contact your Prisma Technology representative.",
                Location = new Point(20, 322),
                Size = new Size(420, 36),
                ForeColor = Color.DimGray
            };
            Controls.Add(lblCompany);
            Controls.Add(lblContact);

            Button btnClose = new Button { Text = "Close", DialogResult = DialogResult.OK, Location = new Point(360, 385), Size = new Size(80, 28) };
            btnClose.Click += (_, __) => Close();
            Controls.Add(btnClose);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private void AddSeparator(int y)
        {
            Controls.Add(new Panel { Location = new Point(20, y), Size = new Size(420, 1), BackColor = Color.LightGray });
        }

        private void AddFieldLabel(string text, int y)
        {
            Controls.Add(new Label { Text = text, Location = new Point(20, y), Size = new Size(140, 18), ForeColor = Color.Gray });
        }

        // ---- version discovery -------------------------------------------------

        private void PopulateVersions()
        {
            _lblPluginVersion.Text = GetPluginVersion();
            _lblApiVersion.Text = "querying…";
            _lblApiVersion.ForeColor = Color.DimGray;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                string v = FetchApiVersion();
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        _lblApiVersion.Text = string.IsNullOrEmpty(v) ? "unreachable" : v;
                        _lblApiVersion.ForeColor = string.IsNullOrEmpty(v) ? Color.Firebrick : Color.Black;
                    }));
                }
                catch { /* form closed */ }
            });
        }

        private static string GetPluginVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? v.ToString(3) : "unknown";
        }

        private string FetchApiVersion()
        {
            if (string.IsNullOrEmpty(_apiBaseUrl)) return null;
            try
            {
                using (HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                {
                    HttpResponseMessage resp = http.GetAsync(_apiBaseUrl + "/api/ping").Result;
                    if (!resp.IsSuccessStatusCode) return null;
                    string body = resp.Content.ReadAsStringAsync().Result;
                    JObject j = JObject.Parse(body);
                    return (string)j["apiVersion"];
                }
            }
            catch { return null; }
        }

        // ---- update check ------------------------------------------------------

        private async Task CheckForUpdateAsync()
        {
            _btnCheck.Enabled = false;
            _btnInstall.Enabled = false;
            _lblStatus.Text = "";
            _lblStatus.ForeColor = Color.DimGray;

            try
            {
                using (HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    string body = await http.GetStringAsync(UpdateManifestUrl);
                    _latestManifest = JObject.Parse(body);
                }
            }
            catch (Exception ex)
            {
                _lblLatestVersion.Text = "unreachable";
                _lblLatestVersion.ForeColor = Color.Firebrick;
                _lblStatus.Text = "Unable to reach update server: " + ex.Message;
                _lblStatus.ForeColor = Color.Firebrick;
                _btnCheck.Enabled = true;
                return;
            }

            string latest = (string)_latestManifest["version"];
            string releaseDate = (string)_latestManifest["releaseDate"];
            long sizeBytes = (long?)_latestManifest["sizeBytes"] ?? 0;

            _lblLatestVersion.Text = string.Format("v{0}  ·  {1}  ·  {2:F1} MB", latest, releaseDate, sizeBytes / 1024.0 / 1024.0);

            string current = _lblApiVersion.Text;
            if (current == "querying…" || current == "unreachable" || string.IsNullOrEmpty(current))
            {
                _lblStatus.Text = "API not reachable — can't compare versions. Start the ATPApi service and try again.";
                _lblStatus.ForeColor = Color.DarkOrange;
                _lblLatestVersion.ForeColor = Color.DimGray;
                _btnCheck.Enabled = true;
                return;
            }

            if (CompareVersions(latest, current) > 0)
            {
                _lblLatestVersion.ForeColor = Color.DarkOrange;
                _lblStatus.Text = string.Format("Update available: v{0} → v{1}", current, latest);
                _lblStatus.ForeColor = Color.DarkOrange;
                _btnInstall.Enabled = true;
                _btnInstall.Text = "Install v" + latest;
            }
            else
            {
                _lblLatestVersion.ForeColor = Color.ForestGreen;
                _lblStatus.Text = "You're running the latest version.";
                _lblStatus.ForeColor = Color.ForestGreen;
            }
            _btnCheck.Enabled = true;
        }

        /// <summary>Strip -suffix, then compare. Returns &gt;0 if a &gt; b.</summary>
        private static int CompareVersions(string a, string b)
        {
            Version va, vb;
            if (!Version.TryParse(StripSuffix(a), out va)) return 0;
            if (!Version.TryParse(StripSuffix(b), out vb)) return 0;
            return va.CompareTo(vb);
        }

        private static string StripSuffix(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0.0.0";
            int dash = v.IndexOf('-');
            return dash >= 0 ? v.Substring(0, dash) : v;
        }

        // ---- launch updater ----------------------------------------------------

        private void LaunchUpdater()
        {
            if (_latestManifest == null) return;

            if (!File.Exists(DefaultUpdaterPath))
            {
                XtraMessageBox.Show(this,
                    "Updater not found at:\r\n" + DefaultUpdaterPath + "\r\n\r\n" +
                    "This feature requires the ATPApi service to be installed (with ATPApiUpdater.exe " +
                    "in its folder). It's not available in development mode.",
                    "Update unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string url = (string)_latestManifest["downloadUrl"];
            string sha = (string)_latestManifest["sha256"];
            string ver = (string)_latestManifest["version"];
            string health = _apiBaseUrl + "/api/ping";

            DialogResult confirm = XtraMessageBox.Show(this,
                "Install ATPApi v" + ver + "?\r\n\r\n" +
                "• The ATPApi service will stop during the update (usually < 30 seconds).\r\n" +
                "• Windows will ask for administrator permission (UAC prompt).\r\n" +
                "• On failure the previous version is automatically restored.\r\n\r\n" +
                "Continue?",
                "Confirm update", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (confirm != DialogResult.OK) return;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = DefaultUpdaterPath,
                    Arguments = string.Format(
                        "--url \"{0}\" --sha256 \"{1}\" --version \"{2}\" --service \"{3}\" --health \"{4}\" --install-dir \"{5}\"",
                        url, sha, ver, ServiceName, health, InstallDir),
                    UseShellExecute = true,   // required for UAC manifest elevation
                    WorkingDirectory = Path.GetDirectoryName(DefaultUpdaterPath) ?? ""
                };
                Process.Start(psi);

                _lblStatus.Text = "Updater launched — watch the UAC prompt. This dialog will close.";
                _lblStatus.ForeColor = Color.DarkBlue;
                _btnInstall.Enabled = false;

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 2500 };
                timer.Tick += (_, __) => { timer.Stop(); Close(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Failed to launch updater: " + ex.Message,
                    "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
