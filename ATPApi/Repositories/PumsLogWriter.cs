using System;
using System.Data.SqlClient;
using ATPApi.AutoCount;

namespace ATPApi.Repositories
{
    /// <summary>
    /// Writes a row to Z_PumsLog using the AutoCount-shared connection. Mirrors the
    /// plugin-side <c>ServiceContractPhotocopier.Data.PumsLog</c> so the View Log form
    /// can show both webhook and SI/ST generation events in one table.
    /// </summary>
    internal static class PumsLogWriter
    {
        public static void Write(string logType, string source, string referenceId,
            string message, string payload, string response)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(SessionManager.SqlConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO Z_PumsLog (LogType, Source, ReferenceId, Message, Payload, Response, LoggedBy)
                    VALUES (@type, @source, @ref, @msg, @payload, @response, @user)", conn))
                {
                    cmd.Parameters.AddWithValue("@type",     logType ?? "Information");
                    cmd.Parameters.AddWithValue("@source",   source ?? "?");
                    cmd.Parameters.AddWithValue("@ref",      (object)(referenceId ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msg",      (object)(message    ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@payload",  (object)(payload    ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@response", (object)(response   ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@user",     SessionManager.CurrentProfile ?? "API");
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* logging must never throw */ }
        }
    }
}
