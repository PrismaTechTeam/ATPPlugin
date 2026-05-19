using System;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Data
{
    /// <summary>
    /// Append-only audit log for the PUMS flow. Written by the SiStGenerator on each
    /// SI/ST attempt and read by the View Log form. The webhook side (ATPApi) writes
    /// to the same Z_PumsLog table.
    /// </summary>
    public static class PumsLog
    {
        public const string TYPE_INFO  = "Information";
        public const string TYPE_WARN  = "Warning";
        public const string TYPE_ERROR = "Error";

        public static void Write(DBSetting db, string logType, string source,
            string referenceId, string message, string payload, string response, string loggedBy)
        {
            if (db == null) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO Z_PumsLog (LogType, Source, ReferenceId, Message, Payload, Response, LoggedBy)
                    VALUES (@type, @source, @ref, @msg, @payload, @response, @user)", conn))
                {
                    cmd.Parameters.AddWithValue("@type",     (object)(logType    ?? TYPE_INFO));
                    cmd.Parameters.AddWithValue("@source",   (object)(source     ?? "?"));
                    cmd.Parameters.AddWithValue("@ref",      (object)(referenceId ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msg",      (object)(message    ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@payload",  (object)(payload    ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@response", (object)(response   ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@user",     (object)(loggedBy   ?? "") ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* logging must never throw — swallow */ }
        }

        public static DataTable Query(DBSetting db, string logType, DateTime fromUtc, DateTime toUtc)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(BuildSql(logType), conn))
            {
                cmd.Parameters.AddWithValue("@from", fromUtc);
                cmd.Parameters.AddWithValue("@to",   toUtc);
                if (!string.IsNullOrWhiteSpace(logType) && logType != "All")
                    cmd.Parameters.AddWithValue("@type", logType);
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        private static string BuildSql(string logType)
        {
            string sql = @"SELECT LogKey, LogType, Source, ReferenceId, Message, Payload, Response,
                                  LoggedAt, LoggedBy
                           FROM Z_PumsLog
                           WHERE LoggedAt >= @from AND LoggedAt < @to";
            if (!string.IsNullOrWhiteSpace(logType) && logType != "All")
                sql += " AND LogType = @type";
            sql += " ORDER BY LoggedAt DESC";
            return sql;
        }
    }
}
