using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Data
{
    /// <summary>
    /// Reads PUMS-sourced rows from <c>Z_PumsStockIssue</c> and <c>Z_PumsStockTransfer</c>
    /// for the Stock Request Task form. The two grids each get a <see cref="DataTable"/>
    /// already filtered by the operator's choices (date range, search box, pending toggle).
    /// </summary>
    public static class StockRequestRepository
    {
        public static DataTable LoadStockIssue(DBSetting db, DateTime fromDate, DateTime toDate,
            string search, bool pendingOnly, bool hideIgnore = false)
        {
            // Location column rule (#19, with operator override):
            //   • Complete  → look up the line's Location from the AutoCount StockIssueDTL
            //   • otherwise → operator-picked LocationOverride if set, else Technician
            string sql = @"
                SELECT p.AutoKey, p.StockIssueId, p.IssueDateTime, p.StockIssueNo, p.ReferenceNo, p.Description,
                       p.SerialNumber, p.Department, p.Job, p.Technician,
                       CASE
                         WHEN p.Status = 'Complete' AND p.GeneratedDocNo IS NOT NULL THEN
                           ISNULL((
                             SELECT TOP 1 d.Location
                             FROM vStockIssueDetail d
                             INNER JOIN vStockIssue si ON si.DocKey = d.DocKey
                             WHERE si.DocNo = p.GeneratedDocNo
                           ), COALESCE(p.LocationOverride, p.Technician))
                         ELSE COALESCE(p.LocationOverride, p.Technician)
                       END AS Location,
                       p.ItemCode, p.Quantity, p.UOM,
                       CASE WHEN EXISTS (SELECT 1 FROM Item it WHERE it.ItemCode = p.ItemCode)
                            THEN 'Yes' ELSE 'No' END AS ItemExists,
                       -- The existing generated doc for this id (if any, not cancelled).
                       (SELECT TOP 1 o.GeneratedDocNo FROM Z_PumsStockIssue o
                          WHERE o.StockIssueId = p.StockIssueId AND o.GeneratedDocNo IS NOT NULL
                            AND LTRIM(RTRIM(o.GeneratedDocNo)) <> '' AND o.Status <> 'Cancelled'
                          ORDER BY o.AutoKey DESC) AS OriginalDocNo,
                       -- Change request: this (not-yet-processed) row re-sends an id that already has a
                       -- generated doc. Qty 0 => Cancel; different qty => Update; else nothing.
                       CASE WHEN p.GeneratedDocNo IS NULL AND EXISTS (
                              SELECT 1 FROM Z_PumsStockIssue o
                              WHERE o.StockIssueId = p.StockIssueId AND o.AutoKey <> p.AutoKey
                                AND o.GeneratedDocNo IS NOT NULL AND LTRIM(RTRIM(o.GeneratedDocNo)) <> ''
                                AND o.Status <> 'Cancelled')
                            THEN CASE
                                   WHEN ISNULL(p.Quantity,0) = 0 THEN 'Cancel'
                                   WHEN ISNULL(p.Quantity,0) <> ISNULL((SELECT TOP 1 o.Quantity FROM Z_PumsStockIssue o
                                          WHERE o.StockIssueId = p.StockIssueId AND o.GeneratedDocNo IS NOT NULL
                                            AND o.Status <> 'Cancelled' ORDER BY o.AutoKey DESC), -1) THEN 'Update'
                                   ELSE 'No' END
                            ELSE 'No' END AS IssueChange,
                       p.Status, p.ReceivedAt, p.GeneratedDocNo, p.CompletedBy, p.CompletedAt
                FROM Z_PumsStockIssue p
                WHERE p.IssueDateTime >= @from AND p.IssueDateTime < @to";

            if (!string.IsNullOrWhiteSpace(search))
                sql += @" AND (
                              p.StockIssueId LIKE @q
                           OR p.StockIssueNo LIKE @q
                           OR p.ReferenceNo  LIKE @q
                           OR p.Description  LIKE @q
                           OR p.SerialNumber LIKE @q
                           OR p.Department   LIKE @q
                           OR p.Job          LIKE @q
                           OR p.Technician   LIKE @q
                           OR p.ItemCode     LIKE @q
                           OR p.UOM          LIKE @q
                           OR p.Status       LIKE @q
                          )";

            // "Show Only New Task" → hide Complete, Ignore and Cancelled
            if (pendingOnly)
                sql += " AND p.Status NOT IN ('Complete','Ignore','Cancelled')";
            else if (hideIgnore)
                sql += " AND p.Status <> 'Ignore'";

            sql += " ORDER BY p.ReceivedAt DESC";

            DataTable dt = Query(db, sql, fromDate, toDate.AddDays(1), search);
            AddSelectColumn(dt);
            // Cancel intent for a Stock Issue = quantity 0.
            CollapsePendingRows(dt, "StockIssueId", "IssueChange",
                r => r["Quantity"] == DBNull.Value || Convert.ToDecimal(r["Quantity"]) == 0m);
            AddActionColumn(dt, "IssueChange");
            FillGeneratedDocFromOriginal(dt);
            return dt;
        }

        public static DataTable LoadStockTransfer(DBSetting db, DateTime fromDate, DateTime toDate,
            string search, bool pendingOnly, string defaultFromLocation, bool hideIgnore = false)
        {
            // From/To Location rule (#21, corrected):
            //   • Complete            → look up Auto Count StockTransfer header FromLocation / ToLocation
            //   • TransferType = IN   → From = defaultFromLocation, To = Technician
            //   • TransferType = OUT  → From = Technician,          To = defaultFromLocation
            //   • otherwise           → null (no opinion)
            string sql = @"
                SELECT p.AutoKey, p.RequestId, p.DocumentDateTime, p.Technician, p.Part, p.Qty,
                       p.TransferType, p.Unit, p.Approval, p.Status, p.ReceivedAt,
                       p.GeneratedDocNo, p.CompletedBy, p.CompletedAt,
                       CASE
                         WHEN p.Status = 'Complete' AND p.GeneratedDocNo IS NOT NULL THEN
                           COALESCE(p.FromLocationOverride,
                             (SELECT TOP 1 st.FromLocation FROM vStockTransfer st WHERE st.DocNo = p.GeneratedDocNo))
                         WHEN UPPER(LTRIM(RTRIM(p.TransferType))) = 'IN'  THEN COALESCE(p.FromLocationOverride, @defLoc)
                         WHEN UPPER(LTRIM(RTRIM(p.TransferType))) = 'OUT' THEN COALESCE(p.FromLocationOverride, p.Technician)
                         ELSE p.FromLocationOverride
                       END AS FromLocation,
                       CASE
                         WHEN p.Status = 'Complete' AND p.GeneratedDocNo IS NOT NULL THEN
                           COALESCE(p.ToLocationOverride,
                             (SELECT TOP 1 st.ToLocation FROM vStockTransfer st WHERE st.DocNo = p.GeneratedDocNo))
                         WHEN UPPER(LTRIM(RTRIM(p.TransferType))) = 'IN'  THEN COALESCE(p.ToLocationOverride, p.Technician)
                         WHEN UPPER(LTRIM(RTRIM(p.TransferType))) = 'OUT' THEN COALESCE(p.ToLocationOverride, @defLoc)
                         ELSE p.ToLocationOverride
                       END AS ToLocation,
                       -- Mirror SiStGenerator.StripBracketSuffix: take Part up to the first '[' and
                       -- check it exists in the AutoCount Item master.
                       CASE WHEN EXISTS (
                              SELECT 1 FROM Item it
                              WHERE it.ItemCode = LTRIM(RTRIM(
                                  CASE WHEN CHARINDEX('[', p.Part) > 0
                                       THEN LEFT(p.Part, CHARINDEX('[', p.Part) - 1)
                                       ELSE p.Part END))
                            ) THEN 'Yes' ELSE 'No' END AS ItemExists,
                       -- Change request on a re-sent id that already has a generated (not-cancelled) doc:
                       --   approval=No        -> 'Cancel'
                       --   approval=Yes, qty differs from the generated doc -> 'Update'
                       CASE WHEN p.GeneratedDocNo IS NULL AND EXISTS (
                              SELECT 1 FROM Z_PumsStockTransfer o
                              WHERE o.RequestId = p.RequestId AND o.AutoKey <> p.AutoKey
                                AND o.GeneratedDocNo IS NOT NULL AND LTRIM(RTRIM(o.GeneratedDocNo)) <> ''
                                AND o.Status <> 'Cancelled')
                            THEN CASE
                                   WHEN UPPER(LTRIM(RTRIM(ISNULL(p.Approval,'')))) IN ('NO','N','FALSE','0') THEN 'Cancel'
                                   WHEN ISNULL(p.Qty,0) <> ISNULL((SELECT TOP 1 o.Qty FROM Z_PumsStockTransfer o
                                          WHERE o.RequestId = p.RequestId AND o.GeneratedDocNo IS NOT NULL
                                            AND o.Status <> 'Cancelled' ORDER BY o.AutoKey DESC), -1) THEN 'Update'
                                   ELSE 'No' END
                            ELSE 'No' END AS TransferChange,
                       (SELECT TOP 1 o.GeneratedDocNo FROM Z_PumsStockTransfer o
                          WHERE o.RequestId = p.RequestId AND o.GeneratedDocNo IS NOT NULL
                            AND LTRIM(RTRIM(o.GeneratedDocNo)) <> ''
                          ORDER BY o.AutoKey DESC) AS OriginalDocNo
                FROM Z_PumsStockTransfer p
                WHERE p.DocumentDateTime >= @from AND p.DocumentDateTime < @to";

            if (!string.IsNullOrWhiteSpace(search))
                sql += @" AND (
                              p.RequestId    LIKE @q
                           OR p.Technician   LIKE @q
                           OR p.Part         LIKE @q
                           OR p.TransferType LIKE @q
                           OR p.Unit         LIKE @q
                           OR p.Approval     LIKE @q
                           OR p.Status       LIKE @q
                          )";

            if (pendingOnly)
                sql += " AND p.Status NOT IN ('Complete','Ignore','Cancelled')";
            else if (hideIgnore)
                sql += " AND p.Status <> 'Ignore'";

            sql += " ORDER BY p.ReceivedAt DESC";

            DataTable dt = QueryTransfer(db, sql, fromDate, toDate.AddDays(1), search, defaultFromLocation);
            AddSelectColumn(dt);
            // Cancel intent for a Stock Transfer = approval No.
            CollapsePendingRows(dt, "RequestId", "TransferChange", IsTransferCancel);
            AddActionColumn(dt, "TransferChange");
            FillGeneratedDocFromOriginal(dt);
            return dt;
        }

        /// <summary>
        /// Adds an "Action" column describing the OPERATION the record represents — distinct from
        /// Status, which is the lifecycle RESULT. Update/Cancel come from the change column; an
        /// already-cancelled record reads "Cancel"; everything else is a "Generate" (new SI/ST).
        /// </summary>
        private static void AddActionColumn(DataTable dt, string changeCol)
        {
            if (dt == null || dt.Columns.Contains("Action")) return;
            dt.Columns.Add("Action", typeof(string));
            foreach (DataRow r in dt.Rows)
            {
                string ch = dt.Columns.Contains(changeCol) ? Convert.ToString(r[changeCol]) : "";
                string st = dt.Columns.Contains("Status")  ? Convert.ToString(r["Status"])  : "";
                string action;
                if      (string.Equals(ch, "Update", StringComparison.OrdinalIgnoreCase)) action = "Update";
                else if (string.Equals(ch, "Cancel", StringComparison.OrdinalIgnoreCase)) action = "Cancel";
                else if (string.Equals(st, "Cancelled", StringComparison.OrdinalIgnoreCase)) action = "Cancel";
                else if (string.Equals(st, "Ignore", StringComparison.OrdinalIgnoreCase)) action = "Ignore";
                else action = "Generate";
                r["Action"] = action;
            }
            dt.AcceptChanges();
        }

        private static bool IsTransferCancel(DataRow r)
        {
            string a = Convert.ToString(r["Approval"]).Trim().ToUpperInvariant();
            return a == "NO" || a == "N" || a == "FALSE" || a == "0";
        }

        /// <summary>
        /// Collapses re-sent change requests per id and applies the "before vs after generation" rules:
        ///   • Already generated (OriginalDocNo present): keep the latest pending row as the Update/Cancel
        ///     change row and surface the target document in GeneratedDocNo; drop no-op re-sends (same qty).
        ///   • Never generated (no OriginalDocNo): override in place — keep only the latest pending row;
        ///     a cancel (isCancel) is hidden entirely, otherwise it stays a plain New request (Status=New).
        /// "Pending" = a row that has not generated a document yet and is still active (not Complete/Ignore/Cancelled).
        /// Generated/Complete/history rows are left untouched.
        /// </summary>
        private static void CollapsePendingRows(DataTable dt, string idCol, string changeCol, Func<DataRow, bool> isCancel)
        {
            if (dt == null || !dt.Columns.Contains(idCol)) return;

            Dictionary<string, List<DataRow>> pendingById =
                new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow r in dt.Rows)
            {
                string gen = dt.Columns.Contains("GeneratedDocNo") ? Convert.ToString(r["GeneratedDocNo"]) : "";
                string st  = dt.Columns.Contains("Status") ? Convert.ToString(r["Status"]) : "";
                bool isPending = string.IsNullOrWhiteSpace(gen)
                                 && st != "Complete" && st != "Ignore" && st != "Cancelled";
                if (!isPending) continue;

                string id = Convert.ToString(r[idCol]);
                if (!pendingById.ContainsKey(id)) pendingById[id] = new List<DataRow>();
                pendingById[id].Add(r);
            }

            List<DataRow> toRemove = new List<DataRow>();
            foreach (KeyValuePair<string, List<DataRow>> kv in pendingById)
            {
                List<DataRow> rows = kv.Value;
                rows.Sort((a, b) => Convert.ToInt64(a["AutoKey"]).CompareTo(Convert.ToInt64(b["AutoKey"])));
                DataRow latest = rows[rows.Count - 1];
                for (int i = 0; i < rows.Count - 1; i++) toRemove.Add(rows[i]);  // superseded re-sends

                string origDoc = dt.Columns.Contains("OriginalDocNo") ? Convert.ToString(latest["OriginalDocNo"]) : "";
                bool hasGenDoc = !string.IsNullOrWhiteSpace(origDoc);

                if (hasGenDoc)
                {
                    string ch = Convert.ToString(latest[changeCol]);
                    if (string.Equals(ch, "No", StringComparison.OrdinalIgnoreCase))
                        toRemove.Add(latest);                       // re-send with no real change → drop
                    // GeneratedDocNo for the kept row is surfaced by FillGeneratedDocFromOriginal.
                }
                else
                {
                    if (isCancel(latest))
                        toRemove.Add(latest);                        // cancel before generation → hide entirely
                    else
                    {
                        if (dt.Columns.Contains("Status")) latest["Status"] = "New";
                        latest[changeCol] = "No";                    // override → plain New request, new qty
                    }
                }
            }

            foreach (DataRow r in toRemove) dt.Rows.Remove(r);
            dt.AcceptChanges();
        }

        /// <summary>
        /// For any row that links to an AutoCount document but has no GeneratedDocNo of its own
        /// (Update/Cancel change requests, and cancel-request rows now marked Cancelled), show the
        /// target document in GeneratedDocNo. This makes the doc visible AND double-clickable —
        /// e.g. a cancelled row opens the cancelled AutoCount document.
        /// </summary>
        private static void FillGeneratedDocFromOriginal(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("GeneratedDocNo") || !dt.Columns.Contains("OriginalDocNo")) return;
            foreach (DataRow r in dt.Rows)
            {
                string gen = Convert.ToString(r["GeneratedDocNo"]);
                string orig = Convert.ToString(r["OriginalDocNo"]);
                if (string.IsNullOrWhiteSpace(gen) && !string.IsNullOrWhiteSpace(orig))
                    r["GeneratedDocNo"] = orig;
            }
            dt.AcceptChanges();
        }

        private static DataTable QueryTransfer(DBSetting db, string sql, DateTime from, DateTime toExclusive,
            string search, string defaultLoc)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@from",   from);
                cmd.Parameters.AddWithValue("@to",     toExclusive);
                cmd.Parameters.AddWithValue("@defLoc", (object)(defaultLoc ?? "") ?? DBNull.Value);
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@q", "%" + search + "%");
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        private static void AddSelectColumn(DataTable dt)
        {
            DataColumn col = new DataColumn("Selected", typeof(bool));
            col.DefaultValue = false;
            dt.Columns.Add(col);
            dt.Columns["Selected"].SetOrdinal(0);
            foreach (DataRow r in dt.Rows) r["Selected"] = false;
        }

        /// <summary>
        /// Loads the AutoCount Location table for dropdown editors. Same source as the
        /// Settings dialog so the operator picks from the same values everywhere.
        /// </summary>
        public static DataTable LoadLocations(DBSetting db)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT Location, ISNULL(Description,'') AS Description FROM Location " +
                "WHERE ISNULL(IsActive,'Y') IN ('Y','T','1','True','true') ORDER BY Location", conn))
            {
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// Returns the subset of <paramref name="codes"/> that do NOT exist in the AutoCount
        /// Location master (case-insensitive). Used to warn/auto-create technician-derived
        /// Stock Locations before generating documents.
        /// </summary>
        public static System.Collections.Generic.List<string> FilterMissingLocations(
            DBSetting db, System.Collections.Generic.IEnumerable<string> codes)
        {
            System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
            System.Collections.Generic.HashSet<string> wanted =
                new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string c in codes)
                if (!string.IsNullOrWhiteSpace(c)) wanted.Add(c.Trim());
            if (wanted.Count == 0) return missing;

            System.Collections.Generic.HashSet<string> existing =
                new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT Location FROM Location", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        if (!r.IsDBNull(0)) existing.Add(r.GetString(0).Trim());
            }
            foreach (string w in wanted)
                if (!existing.Contains(w)) missing.Add(w);
            return missing;
        }

        /// <summary>
        /// After an AutoCount Stock Transfer document has been cancelled, mark the revoking
        /// (approval=No) PUMS row AND every prior row that generated that DocNo as 'Cancelled'.
        /// </summary>
        public static void MarkTransferCancelled(DBSetting db, long revokeAutoKey, string requestId, string docNo)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Z_PumsStockTransfer SET Status='Cancelled' WHERE AutoKey=@k; " +
                    "UPDATE Z_PumsStockTransfer SET Status='Cancelled' WHERE RequestId=@r AND GeneratedDocNo=@d;", conn))
                {
                    cmd.Parameters.AddWithValue("@k", revokeAutoKey);
                    cmd.Parameters.AddWithValue("@r", (object)requestId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@d", (object)docNo ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Mark a Stock Transfer update as applied: the re-sent row becomes Complete and
        /// points at the (now updated) AutoCount document.</summary>
        public static void MarkTransferChangeApplied(DBSetting db, long autoKey, string docNo)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Z_PumsStockTransfer SET Status='Complete', GeneratedDocNo=@d, CompletedAt=SYSUTCDATETIME() WHERE AutoKey=@k", conn))
            {
                cmd.Parameters.AddWithValue("@d", (object)docNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@k", autoKey);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Mark a Stock Issue change as applied: the revoking row becomes Complete and
        /// points at the (now updated) AutoCount document.</summary>
        public static void MarkIssueChangeApplied(DBSetting db, long autoKey, string docNo)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Z_PumsStockIssue SET Status='Complete', GeneratedDocNo=@d, CompletedAt=SYSUTCDATETIME() WHERE AutoKey=@k", conn))
            {
                cmd.Parameters.AddWithValue("@d", (object)docNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@k", autoKey);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>After an AutoCount Stock Issue document is cancelled (qty 0 approved), mark the
        /// revoking row AND every prior row that generated that DocNo as 'Cancelled'.</summary>
        public static void MarkIssueCancelled(DBSetting db, long revokeAutoKey, string stockIssueId, string docNo)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Z_PumsStockIssue SET Status='Cancelled' WHERE AutoKey=@k; " +
                "UPDATE Z_PumsStockIssue SET Status='Cancelled' WHERE StockIssueId=@id AND GeneratedDocNo=@d;", conn))
            {
                cmd.Parameters.AddWithValue("@k", revokeAutoKey);
                cmd.Parameters.AddWithValue("@id", (object)stockIssueId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)docNo ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Persists the operator-chosen Location for a Stock Issue row.</summary>
        public static void SetIssueLocationOverride(DBSetting db, long autoKey, string location)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Z_PumsStockIssue SET LocationOverride = @v WHERE AutoKey = @k", conn))
            {
                cmd.Parameters.AddWithValue("@v", string.IsNullOrWhiteSpace(location) ? (object)DBNull.Value : location);
                cmd.Parameters.AddWithValue("@k", autoKey);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Persists the operator-chosen From-Location for a Stock Transfer row.</summary>
        public static void SetTransferFromOverride(DBSetting db, long autoKey, string location)
        {
            SetTransferOverride(db, autoKey, "FromLocationOverride", location);
        }

        /// <summary>Persists the operator-chosen To-Location for a Stock Transfer row.</summary>
        public static void SetTransferToOverride(DBSetting db, long autoKey, string location)
        {
            SetTransferOverride(db, autoKey, "ToLocationOverride", location);
        }

        private static void SetTransferOverride(DBSetting db, long autoKey, string column, string location)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Z_PumsStockTransfer SET [" + column + "] = @v WHERE AutoKey = @k", conn))
            {
                cmd.Parameters.AddWithValue("@v", string.IsNullOrWhiteSpace(location) ? (object)DBNull.Value : location);
                cmd.Parameters.AddWithValue("@k", autoKey);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static DataTable Query(DBSetting db, string sql, DateTime from, DateTime toExclusive, string search)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", toExclusive);
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@q", "%" + search + "%");
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }
    }
}
