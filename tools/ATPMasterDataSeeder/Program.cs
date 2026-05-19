using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.GeneralMaint.Project;
using AutoCount.Stock.Item;
using AutoCount.Stock.Location;
using Newtonsoft.Json.Linq;

namespace ATPMasterDataSeeder
{
    /// <summary>
    /// One-shot seeder that bulk-creates AutoCount master data needed by the
    /// PUMS → Stock Issue / Transfer flow:
    ///   • Stock Locations   — one per technician name, plus "HQ"
    ///   • Item Master       — one per unique ItemCode seen in either JSON
    ///   • Item UOM children — every (ItemCode, UOM) pair the data shows
    ///
    /// Source JSON files (3,821 stock-issue rows + 4,819 standby rows) live in
    /// ../../../StockReportClone next to this exe's repo tree.
    /// </summary>
    internal static class Program
    {
        private const string AUTOCOUNT_DIR = @"C:\Program Files (x86)\AutoCount\Accounting 2.2";

        // Cells indices in StockReportClone/data.json (stock issue)
        private const int IDX_ISSUE_DESC_HEADER = 5;   // "Stock Issue-{TECH}"
        private const int IDX_ISSUE_JOB         = 7;   // fallback technician
        private const int IDX_ISSUE_ITEM_CODE   = 11;
        private const int IDX_ISSUE_ITEM_DESC   = 13;
        private const int IDX_ISSUE_UOM         = 16;
        private const int IDX_ISSUE_STOCK_DESC  = 20;  // longer descriptive text

        // Cells indices in StockReportClone/standby.json (stock transfer)
        private const int IDX_STBY_TECH       = 3;
        private const int IDX_STBY_PART       = 4;
        private const int IDX_STBY_UNIT       = 9;

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAutoCount;
            try { return Run(args); }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FATAL: " + ex);
                return 1;
            }
        }

        private static Assembly ResolveAutoCount(object sender, ResolveEventArgs args)
        {
            string dll = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(AUTOCOUNT_DIR, dll);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            // 1. Locate the JSON folder
            string dataFolder = ConfigurationManager.AppSettings["DataFolder"];
            if (string.IsNullOrWhiteSpace(dataFolder))
            {
                // exe lives at  ...\Tools\ATPMasterDataSeeder\bin\Debug\net48\
                // JSON lives at ...\StockReportClone\
                dataFolder = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\StockReportClone"));
            }
            string issueJson    = Path.Combine(dataFolder, "data.json");
            string standbyJson  = Path.Combine(dataFolder, "standby.json");
            Info("Data folder: " + dataFolder);

            // 2. Parse JSON files
            HashSet<string> locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HQ" };
            HashSet<string> projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemInfo> items = new Dictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase);
            HashSet<Tuple<string, string>> itemUoms = new HashSet<Tuple<string, string>>();

            if (File.Exists(issueJson))
            {
                ParseStockIssue(issueJson, items, itemUoms, locations, projects);
                Info(string.Format("Parsed {0}", Path.GetFileName(issueJson)));
            }
            else Warn("data.json not found at " + issueJson);

            if (File.Exists(standbyJson))
            {
                ParseStandby(standbyJson, items, itemUoms, locations);
                Info(string.Format("Parsed {0}", Path.GetFileName(standbyJson)));
            }
            else Warn("standby.json not found at " + standbyJson);

            Info(string.Format("Distinct: {0} items   {1} item-UOM pairs   {2} locations   {3} projects",
                items.Count, itemUoms.Count, locations.Count, projects.Count));

            if (args.Contains("--dry-run"))
            {
                Info("Dry-run mode — exiting before any AutoCount calls.");
                return 0;
            }

            // 3. Connect to AutoCount
            Info("Connecting to AutoCount...");
            string server   = ConfigurationManager.AppSettings["DBSetting.ServerName"];
            string database = ConfigurationManager.AppSettings["DBSetting.DBName"];
            string sqlUser  = ConfigurationManager.AppSettings["DBSetting.User"];
            string sqlPass  = ConfigurationManager.AppSettings["DBSetting.Password"];
            string acUser   = ConfigurationManager.AppSettings["AutocountLogin.UserID"];
            string acPass   = ConfigurationManager.AppSettings["AutocountLogin.Password"];

            DBSetting db = new DBSetting(DBServerType.SQL2000, server, sqlUser, sqlPass, database);
            UserSession session = new UserSession(db);
            if (!session.Login(acUser, acPass))
                throw new Exception("AutoCount login failed for user " + acUser);
            Info(string.Format("Logged in: {0}@{1}/{2}", acUser, server, database));

            // 4. Seed locations
            SeedLocations(db, session, locations);

            // 5. Seed projects (used as ProjNo on Stock Issue detail rows)
            SeedProjects(db, session, projects);

            // 6. Seed items + their UOMs
            SeedItems(db, session, items, itemUoms);

            Info("Done.");
            return 0;
        }

        // --------------------- Parsing ---------------------

        private class ItemInfo
        {
            public string ItemCode;
            public string Description;
            public string BaseUOM; // first UOM seen
        }

        // Cells index in data.json for the Department column (used as ProjNo source).
        private const int IDX_ISSUE_DEPARTMENT = 6;

        private static void ParseStockIssue(string path,
            Dictionary<string, ItemInfo> items,
            HashSet<Tuple<string, string>> itemUoms,
            HashSet<string> locations,
            HashSet<string> projects)
        {
            JObject root = JObject.Parse(File.ReadAllText(path));
            JArray rows = (JArray)root["rows"];
            Regex techRx = new Regex(@"Stock Issue-(.+?)\s*$", RegexOptions.Compiled);

            foreach (JToken row in rows)
            {
                JArray cells = (JArray)row["cells"];
                if (cells == null || cells.Count < IDX_ISSUE_STOCK_DESC + 1) continue;

                string itemCode   = SafeStr(cells[IDX_ISSUE_ITEM_CODE]);
                string itemDesc   = SafeStr(cells[IDX_ISSUE_ITEM_DESC]);
                string uom        = SafeStr(cells[IDX_ISSUE_UOM]);
                string stockDesc  = SafeStr(cells[IDX_ISSUE_STOCK_DESC]);
                string descHeader = SafeStr(cells[IDX_ISSUE_DESC_HEADER]);
                string job        = SafeStr(cells[IDX_ISSUE_JOB]);
                string department = SafeStr(cells[IDX_ISSUE_DEPARTMENT]);

                Match m = techRx.Match(descHeader);
                string tech = m.Success ? m.Groups[1].Value.Trim() : (job.Length > 0 ? job : "");
                if (!string.IsNullOrEmpty(tech)) locations.Add(NormalizeLocation(tech));

                string proj = SplitDepartmentForProject(department);
                if (!string.IsNullOrEmpty(proj)) projects.Add(proj);

                if (string.IsNullOrEmpty(itemCode)) continue;
                if (!items.TryGetValue(itemCode, out ItemInfo it))
                {
                    it = new ItemInfo { ItemCode = itemCode };
                    items[itemCode] = it;
                }
                if (string.IsNullOrEmpty(it.Description))
                    it.Description = !string.IsNullOrWhiteSpace(stockDesc) ? stockDesc
                                  : !string.IsNullOrWhiteSpace(itemDesc)  ? itemDesc
                                  : itemCode;
                if (string.IsNullOrEmpty(it.BaseUOM) && !string.IsNullOrWhiteSpace(uom))
                    it.BaseUOM = uom;

                if (!string.IsNullOrWhiteSpace(uom))
                    itemUoms.Add(Tuple.Create(itemCode, uom));
            }
        }

        private static void ParseStandby(string path,
            Dictionary<string, ItemInfo> items,
            HashSet<Tuple<string, string>> itemUoms,
            HashSet<string> locations)
        {
            JObject root = JObject.Parse(File.ReadAllText(path));
            JArray rows = (JArray)root["rows"];

            foreach (JToken row in rows)
            {
                JArray cells = (JArray)row["cells"];
                if (cells == null || cells.Count < IDX_STBY_UNIT + 1) continue;

                string tech = SafeStr(cells[IDX_STBY_TECH]);
                string part = SafeStr(cells[IDX_STBY_PART]);
                string unit = SafeStr(cells[IDX_STBY_UNIT]);

                if (!string.IsNullOrEmpty(tech)) locations.Add(NormalizeLocation(tech));

                string itemCode = StripBracketSuffix(part);
                if (string.IsNullOrEmpty(itemCode)) continue;
                if (!items.TryGetValue(itemCode, out ItemInfo it))
                {
                    it = new ItemInfo { ItemCode = itemCode };
                    items[itemCode] = it;
                }
                if (string.IsNullOrEmpty(it.Description))
                    it.Description = part;
                if (string.IsNullOrEmpty(it.BaseUOM) && !string.IsNullOrWhiteSpace(unit))
                    it.BaseUOM = unit;

                if (!string.IsNullOrWhiteSpace(unit))
                    itemUoms.Add(Tuple.Create(itemCode, unit));
            }
        }

        // --------------------- Seeding ---------------------

        private static void SeedLocations(DBSetting db, UserSession session, IEnumerable<string> wanted)
        {
            // Existing locations
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT Location FROM Location", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) existing.Add(r.GetString(0));
            }

            LocationMaintenance lm = LocationMaintenance.CreateLocationMaint(session, db);
            int created = 0, skipped = 0, failed = 0;
            foreach (string loc in wanted)
            {
                string locCode = NormalizeLocation(loc);
                if (locCode.Length == 0) continue;
                if (existing.Contains(locCode)) { skipped++; continue; }

                try
                {
                    LocationEntity ent = lm.NewLocation();
                    ent.Location    = locCode;
                    ent.Description = locCode;
                    ent.IsActive    = "Y";
                    ent.Save();
                    existing.Add(locCode);
                    created++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Err(string.Format("location '{0}': {1}", locCode, ShortErr(ex)));
                }
            }
            Info(string.Format("Locations: {0} created, {1} skipped, {2} failed", created, skipped, failed));
        }

        private static void SeedProjects(DBSetting db, UserSession session, IEnumerable<string> wanted)
        {
            // Existing project codes
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT ProjNo FROM Project", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) existing.Add(r.GetString(0));
            }

            ProjectDeptCommand pc = ProjectDeptCommand.Create(ProjectType.Project, session);
            int created = 0, skipped = 0, failed = 0;
            foreach (string proj in wanted)
            {
                string code = (proj ?? "").Trim();
                if (code.Length == 0) continue;
                if (existing.Contains(code)) { skipped++; continue; }

                try
                {
                    ProjectEntity ent = pc.NewProject(ProjectLevel.Top, "");
                    ent.ProjNo      = code;
                    ent.Description = code;
                    ent.IsActive    = true;
                    ent.Save();
                    existing.Add(code);
                    created++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Err(string.Format("project '{0}': {1}", code, ShortErr(ex)));
                }
            }
            Info(string.Format("Projects: {0} created, {1} skipped, {2} failed", created, skipped, failed));
        }

        private static void SeedItems(DBSetting db, UserSession session,
            Dictionary<string, ItemInfo> items,
            HashSet<Tuple<string, string>> itemUoms)
        {
            // Existing item codes
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT ItemCode FROM Item", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) existing.Add(r.GetString(0));
            }

            ItemDataAccess ida = ItemDataAccess.Create(session, db);
            int created = 0, skipped = 0, failed = 0;
            Dictionary<string, List<string>> uomsByItem = itemUoms
                .GroupBy(t => t.Item1, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key,
                              g => g.Select(t => t.Item2).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                              StringComparer.OrdinalIgnoreCase);

            int n = 0;
            foreach (KeyValuePair<string, ItemInfo> kv in items)
            {
                n++;
                if (n % 50 == 0) Info(string.Format("  …{0}/{1} items processed", n, items.Count));

                string code = kv.Key;
                ItemInfo it = kv.Value;
                if (existing.Contains(code)) { skipped++; continue; }

                string baseUom = string.IsNullOrWhiteSpace(it.BaseUOM) ? "UNIT" : it.BaseUOM;
                try
                {
                    ItemEntity ent = ida.NewItem();
                    ent.ItemCode    = code;
                    ent.Description = string.IsNullOrWhiteSpace(it.Description) ? code : Trim(it.Description, 100);
                    ent.IsActive    = true;
                    // ItemType left empty/default

                    // BaseUOM must be a row in the item's UOM child table.
                    // Add the Rate=1 UOM first, then assign BaseUom to point at it.
                    ent.NewUom(baseUom, 1m);
                    ent.BaseUom = baseUom;

                    // Add any additional UOMs (rate=1; no conversion data)
                    if (uomsByItem.TryGetValue(code, out List<string> uoms))
                    {
                        foreach (string uom in uoms)
                        {
                            if (string.Equals(uom, baseUom, StringComparison.OrdinalIgnoreCase)) continue;
                            ent.NewUom(uom, 1m);
                        }
                    }

                    ida.SaveData(ent, session.LoginUserID);
                    existing.Add(code);
                    created++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Err(string.Format("item '{0}': {1}", code, ShortErr(ex)));
                }
            }
            Info(string.Format("Items: {0} created, {1} skipped, {2} failed", created, skipped, failed));
        }

        // --------------------- Helpers ---------------------

        public static string StripBracketSuffix(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            int open = raw.IndexOf('[');
            return (open < 0 ? raw : raw.Substring(0, open)).Trim();
        }

        /// <summary>
        /// Mirror of <c>SiStGenerator.SplitDepartmentForProject</c>: take the part after the
        /// last '/' so "A/4PE20147" → "4PE20147"; no slash → return trimmed input.
        /// </summary>
        public static string SplitDepartmentForProject(string department)
        {
            // AutoCount's Project.ProjNo column is nvarchar(10). Take the part after the
            // last '/' so "A/4PE20147" → "4PE20147"; then truncate to 10 chars so the
            // plugin's SiStGenerator and this seeder produce identical codes.
            if (string.IsNullOrWhiteSpace(department)) return string.Empty;
            int i = department.LastIndexOf('/');
            string tail = i < 0 ? department.Trim() : department.Substring(i + 1).Trim();
            if (tail.Length > 10) tail = tail.Substring(0, 10);
            return tail;
        }

        public static string NormalizeLocation(string raw)
        {
            // AutoCount's Location PK column is nvarchar(8). Trim, uppercase, take the
            // first 8 chars. The plugin's SiStGenerator MUST use the identical function
            // when setting line.Location / ToLocation so codes match.
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string s = raw.Trim().ToUpperInvariant();
            if (s.Length > 8) s = s.Substring(0, 8);
            return s.TrimEnd();
        }

        private static string SafeStr(JToken t) => (t == null || t.Type == JTokenType.Null) ? "" : (t.ToString() ?? "").Trim();

        private static string Trim(string s, int max) => s == null ? "" : (s.Length > max ? s.Substring(0, max) : s);

        private static string ShortErr(Exception ex)
        {
            Exception cur = ex; string s = "";
            while (cur != null) { s += (s.Length > 0 ? "  ‹ " : "") + cur.GetType().Name + ": " + cur.Message; cur = cur.InnerException; }
            return s;
        }

        private static void Info(string s)  { Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + s); }
        private static void Warn(string s)  { Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  [WARN] " + s); }
        private static void Err (string s)  { Console.Error.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  [ERR] " + s); }
    }
}
