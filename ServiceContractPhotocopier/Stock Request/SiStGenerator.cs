using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Stock.StockIssue;
using AutoCount.Stock.StockTransfer;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.StockRequest
{
    /// <summary>
    /// Creates AutoCount Stock Issue / Stock Transfer documents from selected
    /// rows of Z_PumsStockIssue / Z_PumsStockTransfer. Per-row failures are
    /// captured in <see cref="ErrorLog"/> and do not stop the run.
    /// On success, the source row's <c>Status</c> flips to <c>'Complete'</c>.
    /// </summary>
    public class SiStGenerator
    {
        public class Job
        {
            public bool IsTransfer;                  // false => Stock Issue, true => Stock Transfer
            public long AutoKey;                     // PK of the source row
            public string Label;                     // shown in progress dialog
            public string Status;                    // "New" or "Update" — drives create-vs-edit
            public string ExistingDocNo;             // for Update, the AutoCount DocNo to edit
            public Dictionary<string, object> Row;
        }

        public class Progress
        {
            public int Total;
            public int Done;
            public int Failed;
            public string CurrentLabel;
        }

        public delegate void ProgressHandler(Progress p);

        public List<string> ErrorLog { get; } = new List<string>();
        public int Total { get; private set; }
        public int Done { get; private set; }
        public int Failed { get; private set; }

        private readonly DBSetting _db;
        private readonly UserSession _userSession;
        private readonly string _defaultFromLocation;

        public SiStGenerator(DBSetting db, UserSession userSession, string defaultFromLocation)
        {
            _db = db;
            _userSession = userSession;
            _defaultFromLocation = string.IsNullOrWhiteSpace(defaultFromLocation)
                ? PumsConfig.DEFAULT_FROM_LOCATION_VALUE
                : defaultFromLocation.Trim();
        }

        public void Run(IList<Job> jobs, ProgressHandler onProgress, Func<bool> isCancelled)
        {
            Total = jobs?.Count ?? 0;
            Done = 0;
            Failed = 0;

            for (int i = 0; i < Total; i++)
            {
                if (isCancelled != null && isCancelled()) break;
                Job j = jobs[i];

                Progress p = new Progress { Total = Total, Done = Done, Failed = Failed, CurrentLabel = j.Label };
                onProgress?.Invoke(p);

                string source = j.IsTransfer ? "GenerateStockTransfer" : "GenerateStockIssue";
                string user   = _userSession != null ? _userSession.LoginUserID : null;
                string payload = SafeJson(j.Row);
                try
                {
                    string docNo;
                    if (j.IsTransfer) docNo = CreateOrEditStockTransfer(j);
                    else              docNo = CreateOrEditStockIssue(j);
                    MarkComplete(j, docNo);
                    Done++;
                    Data.PumsLog.Write(_db, Data.PumsLog.TYPE_INFO, source, j.Label,
                        "Generated DocNo " + docNo, payload, docNo, user);
                }
                catch (Exception ex)
                {
                    Failed++;
                    string err = ShortError(ex);
                    ErrorLog.Add("[" + (j.IsTransfer ? "ST " : "SI ") + j.Label + "]  " + err);
                    Data.PumsLog.Write(_db, Data.PumsLog.TYPE_ERROR, source, j.Label,
                        err, payload, ex.ToString(), user);
                }
            }

            onProgress?.Invoke(new Progress { Total = Total, Done = Done, Failed = Failed, CurrentLabel = "" });
        }

        // ---------------- Stock Issue ----------------

        private string CreateOrEditStockIssue(Job j)
        {
            string docNo       = AsString(j.Row, "StockIssueNo");
            string refDocNo    = AsString(j.Row, "ReferenceNo");
            string technician  = AsString(j.Row, "Technician");
            string itemCode    = AsString(j.Row, "ItemCode");
            string uom         = AsString(j.Row, "UOM");
            decimal qty        = AsDecimal(j.Row, "Quantity");
            string department  = AsString(j.Row, "Department");
            string serialNo    = AsString(j.Row, "SerialNumber");
            DateTime docDate   = AsDateTime(j.Row, "IssueDateTime");

            if (string.IsNullOrWhiteSpace(itemCode))
                throw new InvalidOperationException("ItemCode missing on source row.");
            if (string.IsNullOrWhiteSpace(technician))
                throw new InvalidOperationException("Technician missing — line Location can't be resolved.");
            if (!ItemExists(itemCode))
                throw new InvalidOperationException("ItemCode '" + itemCode +
                    "' does not exist in the AutoCount Item master. Create or map the item, then re-generate.");

            StockIssueCommand cmd = StockIssueCommand.Create(_userSession, _db);
            StockIssue doc;
            bool isUpdate = string.Equals(j.Status, "Update", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(j.ExistingDocNo);
            if (isUpdate)
            {
                doc = cmd.Edit(j.ExistingDocNo);
                // Clear existing detail rows so the new payload is the only line
                while (doc.DetailCount > 0) doc.DeleteDetail(0);
            }
            else
            {
                doc = cmd.AddNew();
                if (!string.IsNullOrWhiteSpace(docNo)) doc.DocNo = docNo;
            }
            doc.DocDate     = docDate;
            doc.RefDocNo    = refDocNo;
            doc.Description = "STOCK ISSUE - " + technician;

            // Respect operator-chosen Location override from the grid; default to Technician.
            string lineLocation = AsString(j.Row, "Location");
            if (string.IsNullOrWhiteSpace(lineLocation)) lineLocation = technician;

            StockIssueDetail line = doc.AddDetail();
            line.ItemCode    = itemCode;
            line.Description = LookupItemDescription(itemCode); // pull from AutoCount Item master
            // Surface the PUMS machine serial on the line so it appears on the printed document.
            if (!string.IsNullOrWhiteSpace(serialNo))
                line.Description = ((line.Description ?? string.Empty).TrimEnd() + "  [S/N: " + serialNo + "]").Trim();
            line.UOM         = string.IsNullOrWhiteSpace(uom) ? "UNIT" : uom;
            line.Qty         = qty;
            line.Location    = NormalizeLocation(lineLocation);
            line.ProjNo      = SplitDepartmentForProject(department);

            doc.Save();
            return doc.DocNo;
        }

        // ---------------- Stock Transfer ----------------

        private string CreateOrEditStockTransfer(Job j)
        {
            string requestId    = AsString(j.Row, "RequestId");
            string technician   = AsString(j.Row, "Technician");
            string part         = AsString(j.Row, "Part");
            string unit         = AsString(j.Row, "Unit");
            string transferType = AsString(j.Row, "TransferType");
            decimal qty         = AsDecimal(j.Row, "Qty");
            DateTime docDate    = AsDateTime(j.Row, "DocumentDateTime");

            if (string.IsNullOrWhiteSpace(technician))
                throw new InvalidOperationException("Technician missing — Location can't be resolved.");
            string itemCode = StripBracketSuffix(part);
            if (string.IsNullOrWhiteSpace(itemCode))
                throw new InvalidOperationException("ItemCode missing after stripping bracket suffix from Part.");
            if (!ItemExists(itemCode))
                throw new InvalidOperationException("ItemCode '" + itemCode +
                    "' (from Part) does not exist in the AutoCount Item master. Create or map the item, then re-generate.");

            // From/To rule (matches the grid display) with operator overrides:
            //   IN  → From = default, To = technician
            //   OUT → From = technician, To = default
            // The grid's FromLocation/ToLocation columns already COALESCE the override
            // in front of the computed default, so prefer them when populated.
            string gridFrom = AsString(j.Row, "FromLocation");
            string gridTo   = AsString(j.Row, "ToLocation");

            string fromLoc, toLoc;
            if (!string.IsNullOrWhiteSpace(gridFrom) || !string.IsNullOrWhiteSpace(gridTo))
            {
                fromLoc = NormalizeLocation(string.IsNullOrWhiteSpace(gridFrom) ? technician : gridFrom);
                toLoc   = NormalizeLocation(string.IsNullOrWhiteSpace(gridTo)   ? _defaultFromLocation : gridTo);
            }
            else
            {
                string t = (transferType ?? "").Trim().ToUpperInvariant();
                if (t == "OUT")
                {
                    fromLoc = NormalizeLocation(technician);
                    toLoc   = NormalizeLocation(_defaultFromLocation);
                }
                else // IN (default fallback)
                {
                    fromLoc = NormalizeLocation(_defaultFromLocation);
                    toLoc   = NormalizeLocation(technician);
                }
            }

            StockTransferCommand cmd = StockTransferCommand.Create(_userSession, _db);
            StockTransfer doc;
            bool isUpdate = string.Equals(j.Status, "Update", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(j.ExistingDocNo);
            if (isUpdate)
            {
                doc = cmd.Edit(j.ExistingDocNo);
                while (doc.DetailCount > 0) doc.DeleteDetail(0);
            }
            else
            {
                doc = cmd.AddNew();
            }
            doc.DocDate      = docDate;
            doc.FromLocation = fromLoc;
            doc.ToLocation   = toLoc;
            doc.Description  = "STOCK TRANSFER - " + technician;

            decimal absQty = qty < 0 ? -qty : qty;
            if (absQty == 0m)
                throw new InvalidOperationException("Source qty is zero — AutoCount rejects a Stock Transfer line with zero/negative quantity.");

            StockTransferDetail line = doc.AddDetail();
            line.ItemCode    = itemCode;
            line.Description = LookupItemDescription(itemCode); // pull from AutoCount Item master
            line.UOM         = string.IsNullOrWhiteSpace(unit) ? "UNIT" : unit;
            line.Qty         = absQty;

            doc.Save();
            return doc.DocNo;
        }

        // ---------- Item-master checks ----------

        // True when the code exists in the AutoCount Item master. Used to fail unknown items
        // with a clear message BEFORE handing them to AutoCount (which throws a cryptic error).
        private readonly Dictionary<string, bool> _existsCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool ItemExists(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return false;
            if (_existsCache.TryGetValue(itemCode, out bool cached)) return cached;
            bool exists;
            using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 1 FROM Item WHERE ItemCode = @c", conn))
            {
                cmd.Parameters.AddWithValue("@c", itemCode);
                conn.Open();
                object o = cmd.ExecuteScalar();
                exists = (o != null && o != DBNull.Value);
            }
            _existsCache[itemCode] = exists;
            return exists;
        }

        // ---------- Item-master description lookup ----------

        private readonly Dictionary<string, string> _descCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string LookupItemDescription(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return string.Empty;
            if (_descCache.TryGetValue(itemCode, out string cached)) return cached;
            string desc = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 Description FROM Item WHERE ItemCode = @c", conn))
                {
                    cmd.Parameters.AddWithValue("@c", itemCode);
                    conn.Open();
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) desc = o.ToString();
                }
            }
            catch { /* swallow — line.Description stays empty */ }
            _descCache[itemCode] = desc;
            return desc;
        }

        // ---------------- Mapping helpers (testable) ----------------

        /// <summary>
        /// Department → AutoCount ProjNo. Take the part after the last '/' ("A/4PE20147"
        /// → "4PE20147"); then truncate to AutoCount's nvarchar(10) limit. MUST stay
        /// byte-for-byte identical to the seeder's <c>SplitDepartmentForProject</c>.
        /// </summary>
        public static string SplitDepartmentForProject(string department)
        {
            if (string.IsNullOrWhiteSpace(department)) return string.Empty;
            int i = department.LastIndexOf('/');
            string tail = i < 0 ? department.Trim() : department.Substring(i + 1).Trim();
            if (tail.Length > 10) tail = tail.Substring(0, 10);
            return tail;
        }

        /// <summary>
        /// Strips a trailing "[…]" annotation from an item code. "TN-123 [toner cartridge]" → "TN-123".
        /// </summary>
        public static string StripBracketSuffix(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            int open = raw.IndexOf('[');
            return (open < 0 ? raw : raw.Substring(0, open)).Trim();
        }

        /// <summary>
        /// AutoCount's Location code column is nvarchar(8). Trim, uppercase, take the
        /// first 8 chars. MUST stay byte-for-byte identical to the seeder so generated
        /// documents can resolve the location row.
        /// </summary>
        public static string NormalizeLocation(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string s = raw.Trim().ToUpperInvariant();
            if (s.Length > 8) s = s.Substring(0, 8);
            return s.TrimEnd();
        }

        // ---------------- Source row → Complete ----------------

        private void MarkComplete(Job j, string generatedDocNo)
        {
            string table = j.IsTransfer ? "Z_PumsStockTransfer" : "Z_PumsStockIssue";
            string user = _userSession != null ? _userSession.LoginUserID : null;
            using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE " + table + " SET Status = 'Complete', GeneratedDocNo = @doc, " +
                "CompletedBy = @user, CompletedAt = SYSUTCDATETIME() WHERE AutoKey = @k", conn))
            {
                cmd.Parameters.AddWithValue("@k", j.AutoKey);
                cmd.Parameters.AddWithValue("@doc",  (object)generatedDocNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@user", (object)user ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------- Plumbing ----------------

        private static string SafeJson(Dictionary<string, object> row)
        {
            if (row == null) return "";
            // Minimal JSON-like dump — no external dependency. Skips DBNull, escapes quotes.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (KeyValuePair<string, object> kv in row)
            {
                if (kv.Value == null || kv.Value == DBNull.Value) continue;
                if (!first) sb.Append(", ");
                first = false;
                string val = Convert.ToString(kv.Value).Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.Append('"').Append(kv.Key).Append("\":\"").Append(val).Append('"');
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string ShortError(Exception ex)
        {
            // Walk the inner-exception chain so SDK-wrapped DB errors are surfaced.
            StringBuilder sb = new StringBuilder();
            Exception cur = ex;
            while (cur != null)
            {
                if (sb.Length > 0) sb.Append("  ‹ ");
                sb.Append(cur.GetType().Name).Append(": ").Append(cur.Message);
                cur = cur.InnerException;
            }
            return sb.ToString();
        }

        private static string AsString(Dictionary<string, object> row, string key)
        {
            if (row == null || !row.TryGetValue(key, out object v) || v == null || v == DBNull.Value) return string.Empty;
            return v.ToString();
        }

        private static decimal AsDecimal(Dictionary<string, object> row, string key)
        {
            if (row == null || !row.TryGetValue(key, out object v) || v == null || v == DBNull.Value) return 0m;
            return Convert.ToDecimal(v);
        }

        private static DateTime AsDateTime(Dictionary<string, object> row, string key)
        {
            if (row == null || !row.TryGetValue(key, out object v) || v == null || v == DBNull.Value) return DateTime.Now;
            return Convert.ToDateTime(v);
        }

        // ---------------- Build jobs from grid rows ----------------

        public static List<Job> BuildJobsFromGrids(DataTable issueTable, DataTable transferTable)
        {
            List<Job> list = new List<Job>();
            AppendTicked(issueTable, false, "StockIssueNo", list);
            AppendTicked(transferTable, true, "RequestId", list);
            return list;
        }

        private static void AppendTicked(DataTable t, bool isTransfer, string labelCol, List<Job> list)
        {
            if (t == null || !t.Columns.Contains("Selected")) return;
            foreach (DataRow r in t.Rows)
            {
                if (!(r["Selected"] is bool b) || !b) continue;
                // Only Complete rows are skipped — Ignored rows are explicitly allowed
                // through so the operator can re-process them.
                string status = t.Columns.Contains("Status") ? Convert.ToString(r["Status"]) : "";
                if (status == "Complete") continue;
                list.Add(BuildJob(r, isTransfer, labelCol));
            }
        }

        private static Job BuildJob(DataRow r, bool isTransfer, string labelCol)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (DataColumn c in r.Table.Columns) dict[c.ColumnName] = r[c];
            return new Job
            {
                IsTransfer     = isTransfer,
                AutoKey        = Convert.ToInt64(r["AutoKey"]),
                Label          = r.Table.Columns.Contains(labelCol) ? Convert.ToString(r[labelCol]) : "?",
                Status         = r.Table.Columns.Contains("Status") ? Convert.ToString(r["Status"]) : "",
                ExistingDocNo  = r.Table.Columns.Contains("GeneratedDocNo") && r["GeneratedDocNo"] != DBNull.Value
                                 ? Convert.ToString(r["GeneratedDocNo"]) : null,
                Row            = dict
            };
        }
    }
}
