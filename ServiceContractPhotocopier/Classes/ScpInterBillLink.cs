using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One thing in this book that came from another book, and what it looked like when it was taken.
    ///
    /// <para>The link is the module. Three of its rules are nothing but questions asked of this
    /// table: which machines the other book has added since, which counter may not be deleted,
    /// which invoice was cancelled over there. See
    /// <c>SQL/02_CreateTable_zSCP2_InterBillLink.sql</c> for why the source is stored as a KEY and
    /// never as a document number.</para>
    /// </summary>
    public class ScpInterBillLink
    {
        public const string CONTRACT = "CONTRACT";
        public const string ITEM = "ITEM";
        public const string METER = "METER";
        public const string INVOICE = "INVOICE";

        /// <summary>Still as it was taken.</summary>
        public const string LIVE = "LIVE";
        /// <summary>The other book has withdrawn it -- the machine came off hire.</summary>
        public const string GONE = "GONE";
        /// <summary>The other book cancelled the document this one came from.</summary>
        public const string CANCELLED = "CANCELLED";
        /// <summary>Offered and turned down. Goes with <see cref="LocalKey"/> zero: nothing was made
        /// here, and the row exists only so the same offer is not made again every month.</summary>
        public const string DECLINED = "DECLINED";

        public long LinkKey;
        public string EntityType = ITEM;
        public long LocalKey;
        public long RootContractKey;
        public Guid SourceBookId = Guid.Empty;
        public long SourceKey;

        /// <summary>The other book's number, for a human reading the table. Nothing matches on
        /// it -- numbers are edited and reused, keys are not.</summary>
        public string SourceRef = "";

        public DateTime TakenAt = DateTime.Now;

        /// <summary>What the other book said at the moment it was taken, as
        /// <c>NAME=value;NAME=value</c>. See <see cref="ScpInterBillSnapshot"/>.</summary>
        public string TakenSnapshot = "";

        public string Status = LIVE;
        public DateTime? NoticeAt;
        public string NoticeText = "";

        public bool IsLive { get { return Status == LIVE; } }
    }

    /// <summary>
    /// A snapshot is a flat list of named values -- <c>RENT=450.00;BK=0.0250;ACTIVE=Y</c>.
    ///
    /// <para>Deliberately not JSON. It is written once and read to be compared, it is looked at in
    /// SSMS by whoever is asking why a machine showed up as changed, and every value in it is a
    /// number or a word. A format a person can read at a glance and a parser can handle in twenty
    /// lines is worth more here than one that can nest.</para>
    /// </summary>
    public static class ScpInterBillSnapshot
    {
        public static string Format(IEnumerable<KeyValuePair<string, string>> values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> kv in values)
            {
                if (kv.Key == null || kv.Key.Trim().Length == 0) continue;
                if (sb.Length > 0) sb.Append(";");
                sb.Append(kv.Key.Trim().ToUpperInvariant());
                sb.Append("=");
                // A ';' or '=' inside a value would split the wrong way on the way back. Nothing that
                // goes in here contains one -- rates, serials, Y/N -- so they are dropped rather than
                // escaped, and the format stays readable.
                sb.Append((kv.Value ?? "").Replace(";", " ").Replace("=", " ").Trim());
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> Parse(string snapshot)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(snapshot)) return map;
            string[] parts = snapshot.Split(';');
            foreach (string p in parts)
            {
                int eq = p.IndexOf('=');
                if (eq <= 0) continue;
                map[p.Substring(0, eq).Trim()] = p.Substring(eq + 1).Trim();
            }
            return map;
        }

        public static string Get(Dictionary<string, string> map, string name)
        {
            string v;
            return map != null && map.TryGetValue(name, out v) ? v : "";
        }

        /// <summary>The names present in either snapshot whose values differ, in the order they
        /// appear. This IS the "changed since you took it" list, for one row.
        /// <para>A name the taken snapshot never had is not a difference: it was added to the snapshot
        /// by a later version (a counter's free copies, waive and ladder, 29/9), and a contract taken
        /// before that would otherwise show as changed at HQ the day the new version is installed.</para></summary>
        public static List<string> Differences(string taken, string now)
        {
            List<string> diff = new List<string>();
            Dictionary<string, string> a = Parse(taken);
            Dictionary<string, string> b = Parse(now);
            foreach (KeyValuePair<string, string> kv in b)
            {
                if (a.Count > 0 && !a.ContainsKey(kv.Key)) continue;
                string was = Get(a, kv.Key);
                if (!string.Equals(was, kv.Value, StringComparison.OrdinalIgnoreCase)) diff.Add(kv.Key);
            }
            foreach (KeyValuePair<string, string> kv in a)
            {
                if (!b.ContainsKey(kv.Key) && !diff.Contains(kv.Key)) diff.Add(kv.Key);
            }
            return diff;
        }
    }

    /// <summary>Storage for <see cref="ScpInterBillLink"/>.</summary>
    public static class ScpInterBillLinks
    {
        private const string Cols =
            "SELECT LinkKey, EntityType, LocalKey, RootContractKey, SourceBookId, SourceKey, " +
            "       ISNULL(SourceRef,'') AS SourceRef, TakenAt, ISNULL(TakenSnapshot,'') AS TakenSnapshot, " +
            "       Status, NoticeAt, ISNULL(NoticeText,'') AS NoticeText FROM dbo.zSCP2_InterBillLink ";

        /// <summary>The link on one local row, or null. This is the question the "may not be
        /// deleted" rules ask.</summary>
        public static ScpInterBillLink Of(DBSetting db, string entityType, long localKey)
        {
            if (db == null || localKey <= 0) return null;
            try
            {
                DataTable t = db.GetDataTable(
                    Cols + "WHERE EntityType = " + Q(entityType) + " AND LocalKey = " + localKey, false);
                if (t.Rows.Count > 0) return FromRow(t.Rows[0]);
            }
            catch { }   // a book without the table has no links, which is the right answer
            return null;
        }

        /// <summary>Everything taken for one contract in this book, in one query -- the form loads it
        /// once and asks it per row, instead of a round trip for every counter on the screen.</summary>
        public static List<ScpInterBillLink> ForContract(DBSetting db, long rootContractKey)
        {
            List<ScpInterBillLink> list = new List<ScpInterBillLink>();
            if (db == null || rootContractKey <= 0) return list;
            try
            {
                DataTable t = db.GetDataTable(
                    Cols + "WHERE RootContractKey = " + rootContractKey + " ORDER BY EntityType, LinkKey", false);
                foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            }
            catch { }
            return list;
        }

        /// <summary>The local keys of one kind that came from another book, for one contract. Empty
        /// for an ordinary contract -- which is every contract until somebody sets inter-billing up,
        /// so every caller of this gets "nothing is owned elsewhere" for free.</summary>
        public static HashSet<long> SourcedKeys(DBSetting db, long rootContractKey, string entityType)
        {
            HashSet<long> keys = new HashSet<long>();
            foreach (ScpInterBillLink l in ForContract(db, rootContractKey))
                if (l.EntityType == entityType) keys.Add(l.LocalKey);
            return keys;
        }

        /// <summary>Is this contract billed from another book's machines? What makes the counters
        /// untouchable, and what a screen has to say out loud so that somebody a year later knows
        /// why.</summary>
        public static ScpInterBillLink ContractLink(DBSetting db, long contractKey)
        {
            return Of(db, ScpInterBillLink.CONTRACT, contractKey);
        }

        /// <summary>Has anything in this book already been taken from that source row? Answers "is
        /// this contract of theirs already on my list", which is what the incoming list greys
        /// out.</summary>
        public static List<ScpInterBillLink> FromSource(
            DBSetting db, Guid sourceBookId, string entityType, long sourceKey)
        {
            List<ScpInterBillLink> list = new List<ScpInterBillLink>();
            if (db == null || sourceBookId == Guid.Empty || sourceKey <= 0) return list;
            try
            {
                DataTable t = db.GetDataTable(
                    Cols + "WHERE SourceBookId = " + Q(sourceBookId.ToString()) +
                    " AND EntityType = " + Q(entityType) + " AND SourceKey = " + sourceKey, false);
                foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            }
            catch { }
            return list;
        }

        /// <summary>Every link of one kind from one book. <paramref name="includeDeclined"/> decides
        /// whether the "offered and turned down" markers come with them -- they are wanted when
        /// working out what is still news, and never when working out what exists here.</summary>
        public static List<ScpInterBillLink> OfType(
            DBSetting db, Guid sourceBookId, string entityType, bool includeDeclined)
        {
            List<ScpInterBillLink> list = new List<ScpInterBillLink>();
            if (db == null || sourceBookId == Guid.Empty) return list;
            try
            {
                DataTable t = db.GetDataTable(
                    Cols + "WHERE SourceBookId = " + Q(sourceBookId.ToString()) +
                    " AND EntityType = " + Q(entityType) +
                    (includeDeclined ? "" : " AND LocalKey > 0") +
                    " ORDER BY RootContractKey, LinkKey", false);
                foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            }
            catch { }
            return list;
        }

        /// <summary>Every source key of one kind that this book has already taken from that book, so
        /// a whole incoming list can be marked in one query instead of one per row.</summary>
        public static HashSet<long> TakenSourceKeys(DBSetting db, Guid sourceBookId, string entityType)
        {
            HashSet<long> keys = new HashSet<long>();
            if (db == null || sourceBookId == Guid.Empty) return keys;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT SourceKey FROM dbo.zSCP2_InterBillLink WHERE SourceBookId = " +
                    Q(sourceBookId.ToString()) + " AND EntityType = " + Q(entityType), false);
                foreach (DataRow r in t.Rows) keys.Add(Convert.ToInt64(r["SourceKey"]));
            }
            catch { }
            return keys;
        }

        /// <summary>Inserts, or updates the one already on that local row. A local row has at most one
        /// source, so taking the same thing twice corrects the link rather than growing a second.</summary>
        public static long Save(DBSetting db, ScpInterBillLink l)
        {
            if (db == null || l == null || l.LocalKey <= 0 || l.SourceBookId == Guid.Empty) return 0;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                return Save(cn, null, l);
            }
        }

        /// <summary>The same, inside a transaction the caller owns -- taking a contract writes the
        /// contract, its machines and its counters, and half of that is worse than none.</summary>
        public static long Save(SqlConnection cn, SqlTransaction tx, ScpInterBillLink l)
        {
            if (cn == null || l == null || l.LocalKey <= 0 || l.SourceBookId == Guid.Empty) return 0;

            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_InterBillLink SET RootContractKey=@root, SourceBookId=@book, " +
                " SourceKey=@src, SourceRef=@ref, TakenAt=@taken, TakenSnapshot=@snap, Status=@st, " +
                " NoticeAt=@nat, NoticeText=@ntx " +
                "WHERE EntityType=@type AND LocalKey=@local; " +
                "IF @@ROWCOUNT = 0 " +
                "INSERT INTO dbo.zSCP2_InterBillLink " +
                " (EntityType, LocalKey, RootContractKey, SourceBookId, SourceKey, SourceRef, " +
                "  TakenAt, TakenSnapshot, Status, NoticeAt, NoticeText) " +
                "VALUES (@type, @local, @root, @book, @src, @ref, @taken, @snap, @st, @nat, @ntx); " +
                "SELECT LinkKey FROM dbo.zSCP2_InterBillLink WHERE EntityType=@type AND LocalKey=@local;",
                cn, tx))
            {
                cmd.Parameters.AddWithValue("@type", (l.EntityType ?? "").Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@local", l.LocalKey);
                cmd.Parameters.AddWithValue("@root", l.RootContractKey);
                cmd.Parameters.AddWithValue("@book", l.SourceBookId);
                cmd.Parameters.AddWithValue("@src", l.SourceKey);
                cmd.Parameters.AddWithValue("@ref", l.SourceRef ?? "");
                cmd.Parameters.AddWithValue("@taken", l.TakenAt == DateTime.MinValue ? DateTime.Now : l.TakenAt);
                cmd.Parameters.AddWithValue("@snap", l.TakenSnapshot ?? "");
                cmd.Parameters.AddWithValue("@st", (l.Status ?? ScpInterBillLink.LIVE).Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@nat", l.NoticeAt.HasValue ? (object)l.NoticeAt.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ntx", l.NoticeText ?? "");
                object o = cmd.ExecuteScalar();
                l.LinkKey = o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
                return l.LinkKey;
            }
        }

        /// <summary>
        /// Writes down that something the other book has was offered here and turned down.
        ///
        /// <para>Without this there is no way to say no. A machine of theirs that this book does not
        /// want would show up as news every month for ever, and the list that is supposed to be read
        /// would become the list nobody reads.</para>
        /// </summary>
        public static void Decline(DBSetting db, ScpInterBillBook book, string entityType,
                                   long rootContractKey, long sourceKey, string sourceRef, string why)
        {
            if (db == null || book == null || sourceKey <= 0 || book.RemoteBookId == Guid.Empty) return;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_InterBillLink " +
                    "                WHERE SourceBookId=@book AND EntityType=@type AND SourceKey=@src " +
                    "                  AND LocalKey = 0) " +
                    "INSERT INTO dbo.zSCP2_InterBillLink " +
                    " (EntityType, LocalKey, RootContractKey, SourceBookId, SourceKey, SourceRef, " +
                    "  TakenAt, TakenSnapshot, Status, NoticeAt, NoticeText) " +
                    "VALUES (@type, 0, @root, @book, @src, @ref, GETDATE(), '', 'DECLINED', GETDATE(), @why)", cn))
                {
                    cmd.Parameters.AddWithValue("@type", (entityType ?? "").Trim().ToUpperInvariant());
                    cmd.Parameters.AddWithValue("@root", rootContractKey);
                    cmd.Parameters.AddWithValue("@book", book.RemoteBookId);
                    cmd.Parameters.AddWithValue("@src", sourceKey);
                    cmd.Parameters.AddWithValue("@ref", sourceRef ?? "");
                    cmd.Parameters.AddWithValue("@why", why ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Records what this book has been TOLD. Being told is as far as it goes: the
        /// decision is this book's, because this book's document went to a real customer.</summary>
        public static void Notice(DBSetting db, long linkKey, string status, string text)
        {
            if (db == null || linkKey <= 0) return;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_InterBillLink SET Status=@s, NoticeAt=GETDATE(), NoticeText=@t " +
                    "WHERE LinkKey=@k", cn))
                {
                    cmd.Parameters.AddWithValue("@s", (status ?? ScpInterBillLink.LIVE).Trim().ToUpperInvariant());
                    cmd.Parameters.AddWithValue("@t", text ?? "");
                    cmd.Parameters.AddWithValue("@k", linkKey);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Updates what a link says it took, after this book has accepted a change from the
        /// other one -- otherwise the same change is reported for ever.</summary>
        public static void Accept(DBSetting db, long linkKey, string newSnapshot)
        {
            if (db == null || linkKey <= 0) return;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_InterBillLink SET TakenSnapshot=@s, TakenAt=GETDATE(), " +
                    " Status='LIVE', NoticeAt=NULL, NoticeText='' WHERE LinkKey=@k", cn))
                {
                    cmd.Parameters.AddWithValue("@s", newSnapshot ?? "");
                    cmd.Parameters.AddWithValue("@k", linkKey);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Cuts a contract loose from its source. What is left is an ordinary contract of
        /// this book's own -- the machines, the prices and the invoices all stay, because they were
        /// always this book's. Only the link goes.</summary>
        public static void Unlink(DBSetting db, long rootContractKey)
        {
            if (db == null || rootContractKey <= 0) return;
            db.ExecuteNonQuery(
                "DELETE FROM dbo.zSCP2_InterBillLink WHERE RootContractKey = " + rootContractKey);
        }

        private static string Q(string s)
        {
            return "N'" + (s ?? "").Replace("'", "''") + "'";
        }

        private static ScpInterBillLink FromRow(DataRow r)
        {
            ScpInterBillLink l = new ScpInterBillLink();
            l.LinkKey = Convert.ToInt64(r["LinkKey"]);
            l.EntityType = Convert.ToString(r["EntityType"]).Trim().ToUpperInvariant();
            l.LocalKey = Convert.ToInt64(r["LocalKey"]);
            l.RootContractKey = Convert.ToInt64(r["RootContractKey"]);
            l.SourceBookId = (Guid)r["SourceBookId"];
            l.SourceKey = Convert.ToInt64(r["SourceKey"]);
            l.SourceRef = Convert.ToString(r["SourceRef"]);
            l.TakenAt = Convert.ToDateTime(r["TakenAt"]);
            l.TakenSnapshot = Convert.ToString(r["TakenSnapshot"]);
            l.Status = Convert.ToString(r["Status"]).Trim().ToUpperInvariant();
            l.NoticeAt = r["NoticeAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["NoticeAt"]);
            l.NoticeText = Convert.ToString(r["NoticeText"]);
            return l;
        }
    }
}
