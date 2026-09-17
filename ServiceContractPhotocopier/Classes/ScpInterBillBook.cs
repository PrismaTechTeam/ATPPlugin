using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// A connection to the OTHER account book, as this book holds it.
    ///
    /// <para>Inter-billing has one direction: the book that bills the end customer PULLS from the
    /// book that owns the machines. So it is the billing book that keeps this row, and every use of
    /// it is a read. Nothing in the module writes across.</para>
    ///
    /// <para><see cref="Password"/> is the plain text, in memory only, and is empty when a row is
    /// loaded from the table -- what comes back is <see cref="HasPassword"/>. A password that is
    /// never read back cannot be shown back by accident, and there is no screen that needs to.</para>
    /// </summary>
    public class ScpInterBillBook
    {
        public int BookKey;
        public string Alias = "";
        public string ServerName = "";
        public string DatabaseName = "";
        public bool WindowsAuth;
        public string DbUser = "";

        /// <summary>Typed now, in memory only. Empty on a loaded row -- see the class note.</summary>
        public string Password = "";

        /// <summary>Is a password stored for this row? The screen shows this instead of the
        /// password, and a save that leaves <see cref="Password"/> empty keeps what is stored.</summary>
        public bool HasPassword;

        /// <summary>The other book's own permanent id, learned by a successful test and never typed.
        /// Empty means it has never been reached, and until it is, nothing may be linked -- a link
        /// naming no source book is a link to nowhere.</summary>
        public Guid RemoteBookId = Guid.Empty;

        public string RemoteCompany = "";

        /// <summary>What this book adds to the other book's prices when it takes a contract, as a
        /// percentage. Their 1.30 at 30 becomes 1.69.
        ///
        /// <para>A standing arrangement between the two companies, so it lives on the connection;
        /// the taking screen offers it as the starting figure and lets it be changed for the contract
        /// in hand. Zero is meaningful and is the default: bill at exactly what they charge.</para></summary>
        public decimal MarginPercent;

        public bool Inactive;
        public DateTime? LastTestedAt;
        public string LastTestResult = "";

        public bool IsUsable
        {
            get
            {
                return !Inactive && RemoteBookId != Guid.Empty
                       && ServerName.Trim().Length > 0 && DatabaseName.Trim().Length > 0;
            }
        }

        /// <summary>How the other book is named to a person: what they called it, and what the book
        /// calls itself. Both, because the alias is theirs and the company name is the proof they
        /// pointed it at what they meant to.</summary>
        public string Display
        {
            get
            {
                string a = (Alias ?? "").Trim();
                string c = (RemoteCompany ?? "").Trim();
                if (a.Length == 0) return c.Length == 0 ? DatabaseName : c;
                return c.Length == 0 || string.Equals(a, c, StringComparison.OrdinalIgnoreCase)
                    ? a : a + "  -  " + c;
            }
        }

        /// <summary>The connection string, with the password given. Read-only in intent: this module
        /// issues nothing but SELECTs across it, and says so wherever it opens one.</summary>
        public string BuildConnectionString(string plainPassword)
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = (ServerName ?? "").Trim();
            b.InitialCatalog = (DatabaseName ?? "").Trim();
            if (WindowsAuth)
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.IntegratedSecurity = false;
                b.UserID = (DbUser ?? "").Trim();
                b.Password = plainPassword ?? "";
            }
            // A billing screen must not hang on a server somebody has switched off. Fifteen seconds is
            // long enough for a slow VPN and short enough that the person knows it failed.
            b.ConnectTimeout = 15;
            b.ApplicationName = "ATP Service Contract - inter-billing";
            return b.ConnectionString;
        }
    }

    /// <summary>Storage and testing for <see cref="ScpInterBillBook"/>.</summary>
    public static class ScpInterBillBooks
    {
        private const string SelectCols =
            "SELECT BookKey, Alias, ServerName, DatabaseName, WindowsAuth, DbUser, " +
            "       ISNULL(DbPasswordEnc,'') AS DbPasswordEnc, RemoteBookId, " +
            "       ISNULL(RemoteCompany,'') AS RemoteCompany, Inactive, LastTestedAt, " +
            "       ISNULL(LastTestResult,'') AS LastTestResult, " +
            "       ISNULL(MarginPercent,0) AS MarginPercent FROM dbo.zSCP2_InterBillBook ";

        /// <summary>Every configured book, active first. Never throws: a book whose migrations have
        /// not run simply has none.</summary>
        public static List<ScpInterBillBook> LoadAll(DBSetting db)
        {
            List<ScpInterBillBook> list = new List<ScpInterBillBook>();
            if (db == null) return list;
            try
            {
                DataTable t = db.GetDataTable(SelectCols + "ORDER BY Inactive, Alias", false);
                foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            }
            catch { }
            return list;
        }

        /// <summary>The book set as HQ in Inter-Billing Setup. When none is set and exactly one
        /// connection is in use, that one is HQ -- there is nothing to choose between. Null otherwise.</summary>
        public static ScpInterBillBook HqBook(DBSetting db)
        {
            if (db == null) return null;
            List<ScpInterBillBook> active = new List<ScpInterBillBook>();
            foreach (ScpInterBillBook b in LoadAll(db)) if (!b.Inactive) active.Add(b);
            int key = HqBookKey(db);
            foreach (ScpInterBillBook b in active) if (b.BookKey == key) return b;
            return key <= 0 && active.Count == 1 ? active[0] : null;
        }

        public static int HqBookKey(DBSetting db)
        {
            int k;
            try
            {
                string s = ServiceContractPhotocopier.Data.PumsConfig.Get(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INTERBILL_HQ_BOOK, "");
                return int.TryParse((s ?? "").Trim(), out k) ? k : 0;
            }
            catch { return 0; }
        }

        public static void SetHqBook(DBSetting db, int bookKey)
        {
            ServiceContractPhotocopier.Data.PumsConfig.Set(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INTERBILL_HQ_BOOK,
                bookKey > 0 ? bookKey.ToString() : "");
        }

        public static ScpInterBillBook Load(DBSetting db, int bookKey)
        {
            if (db == null || bookKey <= 0) return null;
            try
            {
                DataTable t = db.GetDataTable(SelectCols + "WHERE BookKey = " + bookKey, false);
                if (t.Rows.Count > 0) return FromRow(t.Rows[0]);
            }
            catch { }
            return null;
        }

        /// <summary>The book a link row names. Links carry the source book's GUID rather than its
        /// key, so that repointing a configuration row at a different database cannot silently
        /// re-parent everything already taken.</summary>
        public static ScpInterBillBook ByBookId(DBSetting db, Guid remoteBookId)
        {
            if (remoteBookId == Guid.Empty) return null;
            foreach (ScpInterBillBook b in LoadAll(db))
                if (b.RemoteBookId == remoteBookId) return b;
            return null;
        }

        /// <summary>Inserts or updates, and returns the key. An empty
        /// <see cref="ScpInterBillBook.Password"/> keeps the stored one -- the screen never has the
        /// real password to send back, so "unchanged" is the only meaning it can have.</summary>
        public static int Save(DBSetting db, ScpInterBillBook b)
        {
            if (db == null || b == null) return 0;
            string enc = ScpSecret.Protect(b.Password ?? "");

            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                if (b.BookKey <= 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO dbo.zSCP2_InterBillBook " +
                        "(Alias, ServerName, DatabaseName, WindowsAuth, DbUser, DbPasswordEnc, " +
                        " RemoteBookId, RemoteCompany, Inactive, LastTestedAt, LastTestResult, MarginPercent) " +
                        "VALUES (@a, @s, @d, @w, @u, @p, @g, @c, @i, @t, @r, @m); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);", cn))
                    {
                        Bind(cmd, b, enc);
                        object o = cmd.ExecuteScalar();
                        b.BookKey = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                    }
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE dbo.zSCP2_InterBillBook SET Alias=@a, ServerName=@s, DatabaseName=@d, " +
                        " WindowsAuth=@w, DbUser=@u, " +
                        // An empty new password means "leave it alone", not "clear it".
                        " DbPasswordEnc = CASE WHEN @p = N'' THEN DbPasswordEnc ELSE @p END, " +
                        " RemoteBookId=@g, RemoteCompany=@c, Inactive=@i, LastTestedAt=@t, " +
                        " LastTestResult=@r, MarginPercent=@m, LastModified=GETDATE() WHERE BookKey=@k", cn))
                    {
                        Bind(cmd, b, enc);
                        cmd.Parameters.AddWithValue("@k", b.BookKey);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            if (enc.Length > 0) b.HasPassword = true;
            return b.BookKey;
        }

        public static void Delete(DBSetting db, int bookKey)
        {
            if (db == null || bookKey <= 0) return;
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_InterBillBook WHERE BookKey = " + bookKey);
        }

        /// <summary>The stored password, decrypted, for one row.</summary>
        public static string PasswordOf(DBSetting db, int bookKey)
        {
            if (db == null || bookKey <= 0) return "";
            try
            {
                object o = db.ExecuteScalar(
                    "SELECT ISNULL(DbPasswordEnc,'') FROM dbo.zSCP2_InterBillBook WHERE BookKey = " + bookKey);
                return o == null || o == DBNull.Value ? "" : ScpSecret.Unprotect(Convert.ToString(o));
            }
            catch { return ""; }
        }

        /// <summary>The connection string for a saved row, using the stored password -- or the one
        /// just typed, if there is one, so Test works before Save does.</summary>
        public static string ConnectionStringOf(DBSetting db, ScpInterBillBook b)
        {
            if (b == null) return "";
            string pw = (b.Password ?? "").Length > 0 ? b.Password : PasswordOf(db, b.BookKey);
            return b.BuildConnectionString(pw);
        }

        /// <summary>
        /// Opens the other book and asks it who it is. On success <paramref name="b"/> comes back
        /// carrying the remote book's id and company name -- the id is learned here and nowhere else.
        /// </summary>
        /// <returns>true if the book was reached AND may be linked to.</returns>
        public static bool Test(DBSetting localDb, ScpInterBillBook b, out string message)
        {
            message = "";
            if (b == null) { message = "Nothing to test."; return false; }
            if ((b.ServerName ?? "").Trim().Length == 0 || (b.DatabaseName ?? "").Trim().Length == 0)
            {
                message = "Enter the server and the database first.";
                return false;
            }

            string cs = ConnectionStringOf(localDb, b);
            string company = "";
            try
            {
                using (SqlConnection cn = new SqlConnection(cs))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 CompanyName FROM dbo.Profile", cn))
                    {
                        object o = cmd.ExecuteScalar();
                        company = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                // Everything from a wrong password to a switched-off server arrives here. The server's
                // own words are the most useful thing anybody can be told.
                message = "Could not open that book:" + Environment.NewLine + Environment.NewLine + ex.Message;
                Stamp(localDb, b, message);
                return false;
            }

            Guid remote = ScpBookIdentity.Read(cs);
            if (remote == Guid.Empty)
            {
                message = "Connected to " + (company.Length > 0 ? company : b.DatabaseName) +
                          ", but that book does not have the Service Contract plug-in installed." +
                          Environment.NewLine + Environment.NewLine +
                          "Install the plug-in there and open the book once, then test again.";
                Stamp(localDb, b, message);
                return false;
            }

            Guid mine = ScpBookIdentity.Local(localDb);
            if (mine != Guid.Empty && remote == mine)
            {
                // Not pedantry: a book linked to itself would show its own contracts as incoming and
                // let somebody bill a machine to its own customer twice.
                message = "That is this book. Inter-billing connects TWO books -- point this at the " +
                          "other company's account book.";
                Stamp(localDb, b, message);
                return false;
            }

            b.RemoteBookId = remote;
            b.RemoteCompany = company;
            message = "Connected to " + (company.Length > 0 ? company : b.DatabaseName) + ".";
            Stamp(localDb, b, message);
            return true;
        }

        private static void Stamp(DBSetting db, ScpInterBillBook b, string result)
        {
            b.LastTestedAt = DateTime.Now;
            b.LastTestResult = result == null ? "" : (result.Length > 400 ? result.Substring(0, 400) : result);
            if (db == null || b.BookKey <= 0) return;
            try
            {
                using (SqlConnection cn = new SqlConnection(db.ConnectionString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE dbo.zSCP2_InterBillBook SET LastTestedAt=@t, LastTestResult=@r, " +
                        " RemoteBookId=@g, RemoteCompany=@c WHERE BookKey=@k", cn))
                    {
                        cmd.Parameters.AddWithValue("@t", b.LastTestedAt.Value);
                        cmd.Parameters.AddWithValue("@r", b.LastTestResult);
                        cmd.Parameters.AddWithValue("@g",
                            b.RemoteBookId == Guid.Empty ? (object)DBNull.Value : b.RemoteBookId);
                        cmd.Parameters.AddWithValue("@c", b.RemoteCompany ?? "");
                        cmd.Parameters.AddWithValue("@k", b.BookKey);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static void Bind(SqlCommand cmd, ScpInterBillBook b, string enc)
        {
            cmd.Parameters.AddWithValue("@a", (b.Alias ?? "").Trim());
            cmd.Parameters.AddWithValue("@s", (b.ServerName ?? "").Trim());
            cmd.Parameters.AddWithValue("@d", (b.DatabaseName ?? "").Trim());
            cmd.Parameters.AddWithValue("@w", b.WindowsAuth ? "Y" : "N");
            cmd.Parameters.AddWithValue("@u", (b.DbUser ?? "").Trim());
            cmd.Parameters.AddWithValue("@p", enc ?? "");
            cmd.Parameters.AddWithValue("@g",
                b.RemoteBookId == Guid.Empty ? (object)DBNull.Value : b.RemoteBookId);
            cmd.Parameters.AddWithValue("@c", b.RemoteCompany ?? "");
            cmd.Parameters.AddWithValue("@i", b.Inactive ? "Y" : "N");
            cmd.Parameters.AddWithValue("@t",
                b.LastTestedAt.HasValue ? (object)b.LastTestedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@r", b.LastTestResult ?? "");
            cmd.Parameters.AddWithValue("@m", b.MarginPercent);
        }

        private static ScpInterBillBook FromRow(DataRow r)
        {
            ScpInterBillBook b = new ScpInterBillBook();
            b.BookKey = Convert.ToInt32(r["BookKey"]);
            b.Alias = Convert.ToString(r["Alias"]).Trim();
            b.ServerName = Convert.ToString(r["ServerName"]).Trim();
            b.DatabaseName = Convert.ToString(r["DatabaseName"]).Trim();
            b.WindowsAuth = Convert.ToString(r["WindowsAuth"]).Trim().ToUpperInvariant() == "Y";
            b.DbUser = Convert.ToString(r["DbUser"]).Trim();
            b.HasPassword = Convert.ToString(r["DbPasswordEnc"]).Length > 0;
            b.Password = "";                       // never read back -- see the class note
            b.RemoteBookId = r["RemoteBookId"] == DBNull.Value ? Guid.Empty : (Guid)r["RemoteBookId"];
            b.RemoteCompany = Convert.ToString(r["RemoteCompany"]).Trim();
            b.Inactive = Convert.ToString(r["Inactive"]).Trim().ToUpperInvariant() == "Y";
            b.LastTestedAt = r["LastTestedAt"] == DBNull.Value
                ? (DateTime?)null : Convert.ToDateTime(r["LastTestedAt"]);
            b.LastTestResult = Convert.ToString(r["LastTestResult"]);
            b.MarginPercent = r.Table.Columns.Contains("MarginPercent") && r["MarginPercent"] != DBNull.Value
                ? Convert.ToDecimal(r["MarginPercent"]) : 0m;
            return b;
        }
    }
}
