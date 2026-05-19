using System;
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
            string search, bool pendingOnly)
        {
            // Location column rule (#19, with operator override):
            //   • Complete  → look up the line's Location from the AutoCount StockIssueDTL
            //   • otherwise → operator-picked LocationOverride if set, else Technician
            string sql = @"
                SELECT p.AutoKey, p.StockIssueId, p.IssueDateTime, p.StockIssueNo, p.ReferenceNo, p.Description,
                       p.Department, p.Job, p.Technician,
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
                       p.Status, p.ReceivedAt, p.GeneratedDocNo, p.CompletedBy, p.CompletedAt
                FROM Z_PumsStockIssue p
                WHERE p.IssueDateTime >= @from AND p.IssueDateTime < @to";

            if (!string.IsNullOrWhiteSpace(search))
                sql += @" AND (
                              p.StockIssueId LIKE @q
                           OR p.StockIssueNo LIKE @q
                           OR p.ReferenceNo  LIKE @q
                           OR p.Description  LIKE @q
                           OR p.Department   LIKE @q
                           OR p.Job          LIKE @q
                           OR p.Technician   LIKE @q
                           OR p.ItemCode     LIKE @q
                           OR p.UOM          LIKE @q
                           OR p.Status       LIKE @q
                          )";

            // "Show Only New Task" → hide Complete and Ignore
            if (pendingOnly)
                sql += " AND p.Status NOT IN ('Complete','Ignore')";

            sql += " ORDER BY p.ReceivedAt DESC";

            DataTable dt = Query(db, sql, fromDate, toDate.AddDays(1), search);
            AddSelectColumn(dt);
            return dt;
        }

        public static DataTable LoadStockTransfer(DBSetting db, DateTime fromDate, DateTime toDate,
            string search, bool pendingOnly, string defaultFromLocation)
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
                       END AS ToLocation
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
                sql += " AND p.Status NOT IN ('Complete','Ignore')";

            sql += " ORDER BY p.ReceivedAt DESC";

            DataTable dt = QueryTransfer(db, sql, fromDate, toDate.AddDays(1), search, defaultFromLocation);
            AddSelectColumn(dt);
            return dt;
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
