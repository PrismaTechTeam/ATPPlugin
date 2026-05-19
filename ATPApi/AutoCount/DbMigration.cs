using System.Data.SqlClient;
using AutoCount.Data;
using Microsoft.Extensions.Logging;

namespace ATPApi.AutoCount
{
    /// <summary>
    /// Auto-creates/updates ATP API tables on startup.
    /// Uses INFORMATION_SCHEMA checks so it is safe to run repeatedly.
    /// </summary>
    public static class DbMigration
    {
        public static void Run(DBSetting db, ILogger log)
        {
            log.LogInformation("Running DB migration checks...");

            CreatePumsStockIssue(db, log);
            CreatePumsStockTransfer(db, log);
            AlterAddCompletionColumns(db, log);
            CreatePumsLog(db, log);

            log.LogInformation("DB migration complete");
        }

        private static void CreatePumsLog(DBSetting db, ILogger log)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand check = new SqlCommand(
                    "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Z_PumsLog'", conn))
                {
                    if (check.ExecuteScalar() != null) return;
                }
                log.LogInformation("Creating table Z_PumsLog");
                using (SqlCommand cmd = new SqlCommand(@"
                    CREATE TABLE Z_PumsLog (
                        LogKey       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        LogType      VARCHAR(20)   NOT NULL,
                        Source       VARCHAR(50)   NOT NULL,
                        ReferenceId  NVARCHAR(100) NULL,
                        Message      NVARCHAR(MAX) NULL,
                        Payload      NVARCHAR(MAX) NULL,
                        Response     NVARCHAR(MAX) NULL,
                        LoggedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
                        LoggedBy     NVARCHAR(50)  NULL
                    )", conn))
                    cmd.ExecuteNonQuery();
                using (SqlCommand cmd = new SqlCommand(
                    "CREATE INDEX IX_PumsLog_LoggedAt ON Z_PumsLog (LoggedAt DESC)", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static void AlterAddCompletionColumns(DBSetting db, ILogger log)
        {
            // GeneratedDocNo + CompletedBy + CompletedAt are added after the initial
            // ship — keep this idempotent so existing books pick them up on next start.
            AddColumnIfMissing(db, log, "Z_PumsStockIssue",   "GeneratedDocNo", "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockIssue",   "CompletedBy",    "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockIssue",   "CompletedAt",    "DATETIME2 NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockTransfer","GeneratedDocNo", "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockTransfer","CompletedBy",    "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockTransfer","CompletedAt",    "DATETIME2 NULL");

            // Operator-set Location overrides — let the user pick a Location from a
            // dropdown on the Stock Request Task grids before clicking Generate SI/ST.
            AddColumnIfMissing(db, log, "Z_PumsStockIssue",   "LocationOverride",     "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockTransfer","FromLocationOverride", "NVARCHAR(50) NULL");
            AddColumnIfMissing(db, log, "Z_PumsStockTransfer","ToLocationOverride",   "NVARCHAR(50) NULL");
        }

        private static void AddColumnIfMissing(DBSetting db, ILogger log, string table, string col, string def)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand check = new SqlCommand(
                    "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME=@c", conn))
                {
                    check.Parameters.AddWithValue("@t", table);
                    check.Parameters.AddWithValue("@c", col);
                    if (check.ExecuteScalar() != null) return;
                }
                log.LogInformation("Adding column {Col} to {Table}", col, table);
                using (SqlCommand alter = new SqlCommand(
                    string.Format("ALTER TABLE [{0}] ADD [{1}] {2}", table, col, def), conn))
                    alter.ExecuteNonQuery();
            }
        }

        private static void CreatePumsStockIssue(DBSetting db, ILogger log)
        {
            if (TableExists(db, "Z_PumsStockIssue")) return;
            log.LogInformation("Creating table Z_PumsStockIssue");
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    CREATE TABLE Z_PumsStockIssue (
                        AutoKey         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        StockIssueId    NVARCHAR(50)   NOT NULL,
                        IssueDateTime   DATETIME2      NOT NULL,
                        StockIssueNo    NVARCHAR(50)   NULL,
                        ReferenceNo     NVARCHAR(50)   NULL,
                        Description     NVARCHAR(255)  NULL,
                        Department      NVARCHAR(50)   NULL,
                        Job             NVARCHAR(50)   NULL,
                        Technician      NVARCHAR(100)  NULL,
                        Location        NVARCHAR(50)   NULL,
                        ItemCode        NVARCHAR(50)   NULL,
                        Quantity        DECIMAL(18,4)  NULL,
                        UOM             NVARCHAR(20)   NULL,
                        Status          VARCHAR(10)    NOT NULL,
                        ReceivedAt      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
                        RawJson         NVARCHAR(MAX)  NULL
                    )", conn))
                    cmd.ExecuteNonQuery();
                using (SqlCommand cmd = new SqlCommand(
                    "CREATE INDEX IX_PumsStockIssue_Id ON Z_PumsStockIssue (StockIssueId)", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static void CreatePumsStockTransfer(DBSetting db, ILogger log)
        {
            if (TableExists(db, "Z_PumsStockTransfer")) return;
            log.LogInformation("Creating table Z_PumsStockTransfer");
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    CREATE TABLE Z_PumsStockTransfer (
                        AutoKey           BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        RequestId         NVARCHAR(50)   NOT NULL,
                        DocumentDateTime  DATETIME2      NOT NULL,
                        Technician        NVARCHAR(100)  NULL,
                        Part              NVARCHAR(255)  NULL,
                        Qty               DECIMAL(18,4)  NULL,
                        TransferType      NVARCHAR(20)   NULL,
                        Unit              NVARCHAR(20)   NULL,
                        Approval          NVARCHAR(50)   NULL,
                        Status            VARCHAR(10)    NOT NULL,
                        ReceivedAt        DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
                        RawJson           NVARCHAR(MAX)  NULL
                    )", conn))
                    cmd.ExecuteNonQuery();
                using (SqlCommand cmd = new SqlCommand(
                    "CREATE INDEX IX_PumsStockTransfer_Id ON Z_PumsStockTransfer (RequestId)", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static bool TableExists(DBSetting db, string tableName)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", conn))
            {
                cmd.Parameters.AddWithValue("@t", tableName);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null;
            }
        }
    }
}
