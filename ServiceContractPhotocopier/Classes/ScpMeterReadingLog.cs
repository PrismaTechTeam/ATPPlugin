using System;
using System.Data.SqlClient;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Append-only writer for zSCP2_MeterReadingLog — the immutable meter reading audit trail.
    /// Every reading event adds a row; nothing here (or anywhere else in the app) updates or deletes
    /// log rows, so the full previous/previous-previous reading history is always available.
    /// </summary>
    public static class ScpMeterReadingLog
    {
        public const string SOURCE_INVOICE = "INVOICE";
        public const string SOURCE_INVOICE_DELETED = "INVOICE-DELETED";
        public const string SOURCE_CLEARED = "CLEARED";

        public static void Append(SqlConnection conn, SqlTransaction tx, long itemMeterKey,
            int periodYear, int periodMonth, decimal reading, DateTime? readingDate, string source, string docNo)
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_MeterReadingLog " +
                "(ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, Source, DocNo, CreatedBy) " +
                "VALUES (@imk, @yr, @mo, @rd, @dt, @src, @dn, @by)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                cmd.Parameters.AddWithValue("@yr", periodYear);
                cmd.Parameters.AddWithValue("@mo", periodMonth);
                cmd.Parameters.AddWithValue("@rd", reading);
                cmd.Parameters.AddWithValue("@dt", (object)readingDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@src", source ?? "");
                cmd.Parameters.AddWithValue("@dn", docNo ?? "");
                cmd.Parameters.AddWithValue("@by", CurrentUser());
                cmd.ExecuteNonQuery();
            }
        }

        private static string CurrentUser()
        {
            try
            {
                AutoCount.Authentication.UserSession s = AutoCount.Authentication.UserSession.CurrentUserSession;
                return s != null ? (s.LoginUserID ?? "") : "";
            }
            catch { return ""; }
        }
    }
}
