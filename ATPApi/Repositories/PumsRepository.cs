using System;
using System.Data;
using System.Data.SqlClient;
using ATPApi.AutoCount;
using ATPApi.Models;

namespace ATPApi.Repositories
{
    /// <summary>
    /// Persists incoming PUMS payloads. Behaviour for duplicate IDs is governed by
    /// the <c>FLAG_CONTROL</c> row in <c>Z_PumsConfig</c>:
    /// • flag = false (default) → re-push is silently ignored, no row written
    /// • flag = true            → oldest open row for this ID is deleted, the new
    ///                             payload is inserted as <c>Update</c>
    /// Completed/Ignored rows are always preserved as history.
    /// </summary>
    public static class PumsRepository
    {
        public const string FLAG_CONTROL_KEY = "FLAG_CONTROL";

        public enum UpsertOutcome { New, Update, Ignored }

        public static UpsertOutcome UpsertStockIssue(StockIssueRequest req, string rawJson)
        {
            using (SqlConnection conn = new SqlConnection(SessionManager.SqlConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    bool flag = ReadFlagControl(conn, tx);
                    UpsertOutcome outcome = DecideAndPrepareStockIssue(conn, tx, req.StockIssueId, flag);
                    if (outcome == UpsertOutcome.Ignored)
                    {
                        tx.Commit();
                        return outcome;
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Z_PumsStockIssue
                            (StockIssueId, IssueDateTime, StockIssueNo, ReferenceNo, Description, SerialNumber, Department, Job,
                             Technician, Location, ItemCode, Quantity, UOM, Status, RawJson)
                        VALUES
                            (@StockIssueId, @IssueDateTime, @StockIssueNo, @ReferenceNo, @Description, @SerialNumber, @Department, @Job,
                             @Technician, @Location, @ItemCode, @Quantity, @UOM, @Status, @RawJson)",
                        conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@StockIssueId", (object)req.StockIssueId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IssueDateTime", (object)(req.IssueDateTime ?? DateTime.UtcNow));
                        cmd.Parameters.AddWithValue("@StockIssueNo", (object)req.StockIssueNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ReferenceNo",  (object)req.ReferenceNo  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description",  (object)req.Description  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SerialNumber", (object)req.SerialNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Department",   (object)req.Department   ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Job",          (object)req.Job          ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Technician",   (object)req.Technician   ?? DBNull.Value);
                        // Location is intentionally left NULL — the plugin derives it from Technician
                        // (and, after Generate, from the AutoCount document). The webhook ignores any
                        // value the caller sends in the Location field.
                        cmd.Parameters.AddWithValue("@Location",     DBNull.Value);
                        cmd.Parameters.AddWithValue("@ItemCode",     (object)req.ItemCode     ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Quantity",     (object)(req.Quantity ?? 0));
                        cmd.Parameters.AddWithValue("@UOM",          (object)req.UOM          ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status",       outcome.ToString());
                        cmd.Parameters.AddWithValue("@RawJson",      (object)rawJson ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return outcome;
                }
            }
        }

        public static UpsertOutcome UpsertStockTransfer(StockTransferRequest req, string rawJson)
        {
            using (SqlConnection conn = new SqlConnection(SessionManager.SqlConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    bool flag = ReadFlagControl(conn, tx);
                    UpsertOutcome outcome = DecideAndPrepareStockTransfer(conn, tx, req.RequestId, flag);
                    if (outcome == UpsertOutcome.Ignored)
                    {
                        tx.Commit();
                        return outcome;
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Z_PumsStockTransfer
                            (RequestId, DocumentDateTime, Technician, Part, Qty, TransferType, Unit, Approval, Status, RawJson)
                        VALUES
                            (@RequestId, @DocumentDateTime, @Technician, @Part, @Qty, @TransferType, @Unit, @Approval, @Status, @RawJson)",
                        conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@RequestId",        (object)req.RequestId   ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocumentDateTime", (object)(req.DocumentDateTime ?? DateTime.UtcNow));
                        cmd.Parameters.AddWithValue("@Technician",       (object)req.Technician  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Part",             (object)req.Part        ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Qty",              (object)(req.Qty ?? 0));
                        cmd.Parameters.AddWithValue("@TransferType",     (object)req.Type        ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Unit",             (object)req.Unit        ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Approval",         (object)req.Approval    ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status",           outcome.ToString());
                        cmd.Parameters.AddWithValue("@RawJson",          (object)rawJson ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return outcome;
                }
            }
        }

        // -----------------------------------------------------------------------
        //   Duplicate-handling decision logic
        // -----------------------------------------------------------------------

        /// <summary>
        /// Reads the FLAG_CONTROL toggle from Z_PumsConfig (creates the table if missing).
        /// Default = false (ignore duplicates).
        /// </summary>
        private static bool ReadFlagControl(SqlConnection conn, SqlTransaction tx)
        {
            using (SqlCommand cmd = new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Z_PumsConfig') " +
                "BEGIN CREATE TABLE Z_PumsConfig (ConfigKey NVARCHAR(50) NOT NULL PRIMARY KEY, ConfigValue NVARCHAR(MAX) NULL) END",
                conn, tx)) cmd.ExecuteNonQuery();

            using (SqlCommand cmd = new SqlCommand(
                "SELECT ConfigValue FROM Z_PumsConfig WHERE ConfigKey = @k", conn, tx))
            {
                cmd.Parameters.AddWithValue("@k", FLAG_CONTROL_KEY);
                object v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return false;
                string s = v.ToString().Trim();
                return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static UpsertOutcome DecideAndPrepareStockIssue(SqlConnection conn, SqlTransaction tx, string id, bool flag)
        {
            return DecideAndPrepare(conn, tx, id, flag, "Z_PumsStockIssue", "StockIssueId");
        }

        private static UpsertOutcome DecideAndPrepareStockTransfer(SqlConnection conn, SqlTransaction tx, string id, bool flag)
        {
            return DecideAndPrepare(conn, tx, id, flag, "Z_PumsStockTransfer", "RequestId");
        }

        /// <summary>
        /// Inspects the table for prior rows with this idempotency id:
        /// • no prior row                → New
        /// • prior row(s) all closed     → New (closed rows are history; treat as fresh)
        /// • flag=false, open row exists → Ignored (no DB change)
        /// • flag=true,  open row exists → delete that open row, return Update so the
        ///                                  caller can INSERT the latest payload
        /// "Closed" = Status IN ('Complete', 'Ignore').
        /// </summary>
        private static UpsertOutcome DecideAndPrepare(SqlConnection conn, SqlTransaction tx,
            string id, bool flag, string table, string idCol)
        {
            long? openAutoKey = null;
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TOP 1 AutoKey FROM " + table +
                " WHERE " + idCol + " = @id AND Status NOT IN ('Complete','Ignore') " +
                " ORDER BY AutoKey DESC", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", (object)id ?? DBNull.Value);
                object r = cmd.ExecuteScalar();
                if (r != null && r != DBNull.Value) openAutoKey = Convert.ToInt64(r);
            }

            if (openAutoKey == null) return UpsertOutcome.New;
            if (!flag) return UpsertOutcome.Ignored;

            // flag = true → delete the existing open row so the upsert collapses to one row
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM " + table + " WHERE AutoKey = @k", conn, tx))
            {
                cmd.Parameters.AddWithValue("@k", openAutoKey.Value);
                cmd.ExecuteNonQuery();
            }
            return UpsertOutcome.Update;
        }
    }
}
