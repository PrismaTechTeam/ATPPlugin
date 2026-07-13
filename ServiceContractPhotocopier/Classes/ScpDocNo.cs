using System;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Plugin document numbering (zSCP2_DocNoFormat) — the plugin's own "Document Numbering Format".
    /// FormatString convention follows AutoCount: literal text + ONE &lt;000...&gt; placeholder whose
    /// width = number of characters between the brackets ('SC-&lt;000000&gt;' → SC-000123).
    /// <see cref="Next"/> atomically takes and increments the running number (UPDLOCK), so two users
    /// can never draw the same document number.
    /// </summary>
    public static class ScpDocNo
    {
        public const string DOCTYPE_CONTRACT = "SC";
        public const string DOCTYPE_SERVICE_ITEM = "SI";

        /// <summary>Previews the next number WITHOUT consuming it (for the Auto button). Clicking Auto
        /// repeatedly always shows the same number; the number is only reserved when <see cref="Next"/>
        /// is called at save time. Returns "" if the format row does not exist yet.</summary>
        public static string Peek(DBSetting db, string docType)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(db.ConnectionString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT FormatString, NextNumber FROM [dbo].[zSCP2_DocNoFormat] WHERE DocType=@t", cn))
                    {
                        cmd.Parameters.AddWithValue("@t", docType);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read()) return Format(r.GetString(0), r.GetInt32(1));
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>Draws the next number for the doc type (consumes it). Self-heals a missing row.</summary>
        public static string Next(DBSetting db, string docType)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("ScpDocNo"))
                {
                    try
                    {
                        string fmt = null;
                        int next = 1;
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT FormatString, NextNumber FROM [dbo].[zSCP2_DocNoFormat] " +
                            "WITH (UPDLOCK, HOLDLOCK) WHERE DocType=@t", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@t", docType);
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                if (r.Read()) { fmt = r.GetString(0); next = r.GetInt32(1); }
                            }
                        }

                        if (fmt == null)
                        {
                            // Row missing (e.g. table added after the book was created) — create a default.
                            fmt = docType + "-<000000>";
                            using (SqlCommand ins = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP2_DocNoFormat] (DocType, Description, FormatString, NextNumber) " +
                                "VALUES (@t, @t, @f, 1)", cn, tx))
                            {
                                ins.Parameters.AddWithValue("@t", docType);
                                ins.Parameters.AddWithValue("@f", fmt);
                                ins.ExecuteNonQuery();
                            }
                            next = 1;
                        }

                        using (SqlCommand upd = new SqlCommand(
                            "UPDATE [dbo].[zSCP2_DocNoFormat] SET NextNumber=@n, LastModified=GETDATE() WHERE DocType=@t", cn, tx))
                        {
                            upd.Parameters.AddWithValue("@n", next + 1);
                            upd.Parameters.AddWithValue("@t", docType);
                            upd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return Format(fmt, next);
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>Renders a number into the format string ('SC-&lt;000000&gt;', 123 → "SC-000123").</summary>
        public static string Format(string fmt, int number)
        {
            if (string.IsNullOrEmpty(fmt)) return number.ToString();
            int open = fmt.IndexOf('<');
            int close = fmt.IndexOf('>');
            if (open < 0 || close <= open) return fmt + number;   // no placeholder → append
            int width = close - open - 1;
            if (width < 1) width = 1;
            return fmt.Substring(0, open) + number.ToString(new string('0', width)) + fmt.Substring(close + 1);
        }
    }
}
