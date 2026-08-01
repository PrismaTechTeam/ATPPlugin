using System;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Data access for the named bulk-email templates (zSCP2_EmailTemplate). One row is the
    /// DEFAULT — the Bulk Email run uses it; the template dialog maintains the versions.
    /// </summary>
    public static class ScpEmailTemplates
    {
        public class Template
        {
            public long Key;
            public string Name = "";
            public string Subject = "";
            public string Body = "";
            public string Style = "PLAIN";   // PLAIN / STYLED / HTML
            public bool Styled { get { return Style == "STYLED"; } }
            public bool CustomHtml { get { return Style == "HTML"; } }
            public bool IsDefault;
        }

        public static DataTable List(DBSetting db)
        {
            return db.GetDataTable(
                "SELECT TemplateKey, [Name], [Subject], Body, Style, IsDefault " +
                "FROM dbo.zSCP2_EmailTemplate ORDER BY [Name]", false);
        }

        public static Template FromRow(DataRow r)
        {
            Template t = new Template();
            t.Key = Convert.ToInt64(r["TemplateKey"]);
            t.Name = Convert.ToString(r["Name"]);
            t.Subject = Convert.ToString(r["Subject"]);
            t.Body = Convert.ToString(r["Body"]);
            t.Style = Convert.ToString(r["Style"]).Trim().ToUpperInvariant();
            if (t.Style != "STYLED" && t.Style != "HTML") t.Style = "PLAIN";
            t.IsDefault = Convert.ToString(r["IsDefault"]).Trim() == "Y";
            return t;
        }

        /// <summary>The default template (falls back to the first row, then stock wording) —
        /// what the Bulk Email run sends with.</summary>
        public static Template LoadDefault(DBSetting db)
        {
            try
            {
                DataTable dt = List(db);
                DataRow pick = null;
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToString(r["IsDefault"]).Trim() == "Y") { pick = r; break; }
                    if (pick == null) pick = r;
                }
                if (pick != null) return FromRow(pick);
            }
            catch { }
            Template t = new Template();
            t.Name = "Standard";
            t.Subject = ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            t.Body = ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_BULKMAIL_BODY;
            t.IsDefault = true;
            return t;
        }

        public static long Insert(DBSetting db, string name, string subject, string body, string style)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_EmailTemplate ([Name], [Subject], Body, Style, IsDefault) " +
                "VALUES (@n, @s, @b, @st, 'N'); SELECT CAST(SCOPE_IDENTITY() AS bigint);", conn))
            {
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@s", subject ?? "");
                cmd.Parameters.AddWithValue("@b", body ?? "");
                cmd.Parameters.AddWithValue("@st", style == "STYLED" || style == "HTML" ? style : "PLAIN");
                conn.Open();
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        public static void Update(DBSetting db, long key, string subject, string body, string style)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_EmailTemplate SET [Subject]=@s, Body=@b, Style=@st, " +
                "LastModified=GETDATE() WHERE TemplateKey=@k", conn))
            {
                cmd.Parameters.AddWithValue("@s", subject ?? "");
                cmd.Parameters.AddWithValue("@b", body ?? "");
                cmd.Parameters.AddWithValue("@st", style == "STYLED" || style == "HTML" ? style : "PLAIN");
                cmd.Parameters.AddWithValue("@k", key);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Deletes a template; if it was the default, the first remaining one becomes
        /// default so a default always exists.</summary>
        public static void Delete(DBSetting db, long key)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM dbo.zSCP2_EmailTemplate WHERE TemplateKey=@k; " +
                    "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_EmailTemplate WHERE IsDefault='Y') " +
                    "UPDATE dbo.zSCP2_EmailTemplate SET IsDefault='Y' " +
                    "WHERE TemplateKey = (SELECT MIN(TemplateKey) FROM dbo.zSCP2_EmailTemplate)", conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SetDefault(DBSetting db, long key)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_EmailTemplate SET IsDefault = CASE WHEN TemplateKey=@k THEN 'Y' ELSE 'N' END", conn))
            {
                cmd.Parameters.AddWithValue("@k", key);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
