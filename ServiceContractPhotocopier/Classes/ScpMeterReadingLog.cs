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
            Append(conn, tx, itemMeterKey, periodYear, periodMonth, reading, readingDate, source, docNo, null, null);
        }

        /// <summary>Baseline + usage form (pricing columns left NULL).</summary>
        public static void Append(SqlConnection conn, SqlTransaction tx, long itemMeterKey,
            int periodYear, int periodMonth, decimal reading, DateTime? readingDate, string source, string docNo,
            decimal? lastReading, decimal? usage)
        {
            Append(conn, tx, itemMeterKey, periodYear, periodMonth, reading, readingDate, source, docNo,
                lastReading, usage, null, null, null, null, null);
        }

        /// <summary>Full form: baseline, usage AND the meter's pricing AS OF the event (unit price /
        /// min charges / FOC / rebate) plus the resulting charge — the complete historical record
        /// behind an invoice line, immune to later price edits on the meter master.</summary>
        public static void Append(SqlConnection conn, SqlTransaction tx, long itemMeterKey,
            int periodYear, int periodMonth, decimal reading, DateTime? readingDate, string source, string docNo,
            decimal? lastReading, decimal? usage,
            decimal? unitPrice, decimal? minCharges, decimal? focQty, decimal? rebatePct, decimal? charge)
        {
            Append(conn, tx, itemMeterKey, periodYear, periodMonth, reading, readingDate, source, docNo,
                lastReading, usage, unitPrice, minCharges, focQty, rebatePct, charge, null, null);
        }

        /// <summary>v4 form: everything above PLUS the strategy in force for the event and its
        /// human-readable outcome (e.g. "WAIVED: meter charges 512.40 >= target 500.00").</summary>
        public static void Append(SqlConnection conn, SqlTransaction tx, long itemMeterKey,
            int periodYear, int periodMonth, decimal reading, DateTime? readingDate, string source, string docNo,
            decimal? lastReading, decimal? usage,
            decimal? unitPrice, decimal? minCharges, decimal? focQty, decimal? rebatePct, decimal? charge,
            string strategyCode, string strategyNote)
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_MeterReadingLog " +
                "(ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, Source, DocNo, CreatedBy, LastReading, [Usage], " +
                " UnitPrice, MinCharges, FOCQty, RebatePct, Charge, StrategyCode, StrategyNote) " +
                "VALUES (@imk, @yr, @mo, @rd, @dt, @src, @dn, @by, @last, @use, @up, @min, @foc, @reb, @chg, @sc, @sn)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                cmd.Parameters.AddWithValue("@yr", periodYear);
                cmd.Parameters.AddWithValue("@mo", periodMonth);
                cmd.Parameters.AddWithValue("@rd", reading);
                cmd.Parameters.AddWithValue("@dt", (object)readingDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@src", source ?? "");
                cmd.Parameters.AddWithValue("@dn", docNo ?? "");
                cmd.Parameters.AddWithValue("@by", CurrentUser());
                cmd.Parameters.AddWithValue("@last", lastReading.HasValue ? (object)lastReading.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@use", usage.HasValue ? (object)usage.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@up", unitPrice.HasValue ? (object)unitPrice.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@min", minCharges.HasValue ? (object)minCharges.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@foc", focQty.HasValue ? (object)focQty.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@reb", rebatePct.HasValue ? (object)rebatePct.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@chg", charge.HasValue ? (object)charge.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@sc", string.IsNullOrEmpty(strategyCode) ? (object)DBNull.Value : strategyCode);
                cmd.Parameters.AddWithValue("@sn", string.IsNullOrEmpty(strategyNote) ? (object)DBNull.Value : strategyNote);
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
