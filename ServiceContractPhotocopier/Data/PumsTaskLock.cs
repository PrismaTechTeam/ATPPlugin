using System;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Data
{
    /// <summary>
    /// Single-row table-based distributed lock for the "Generate SI/ST" job.
    /// One AutoCount user across the network may run the generator at a time —
    /// concurrent attempts see who is running and back off.
    ///
    /// Schema:
    ///   Z_PumsTaskLock(LockKey NVARCHAR(50) PK, LockedBy NVARCHAR(200) NOT NULL,
    ///                  Machine NVARCHAR(100) NULL, LockedAt DATETIME2 NOT NULL)
    /// </summary>
    public static class PumsTaskLock
    {
        public const string LOCK_KEY_GENERATE = "GENERATE_SIST";
        private const int StaleMinutes = 30;

        public static void EnsureTable(DBSetting db)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Z_PumsTaskLock')
                    BEGIN
                        CREATE TABLE Z_PumsTaskLock (
                            LockKey   NVARCHAR(50)  NOT NULL PRIMARY KEY,
                            LockedBy  NVARCHAR(200) NOT NULL,
                            Machine   NVARCHAR(100) NULL,
                            LockedAt  DATETIME2     NOT NULL
                        )
                    END", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Tries to claim the named lock for <paramref name="user"/>. Returns null on success;
        /// returns the current holder description (e.g. "ADMIN @ DESKTOP-XYZ since 14:32") if already held.
        /// </summary>
        public static string TryAcquire(DBSetting db, string lockKey, string user, string machine)
        {
            EnsureTable(db);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    // Look for an existing holder
                    string existingUser = null;
                    string existingMachine = null;
                    DateTime? lockedAt = null;
                    using (SqlCommand sel = new SqlCommand(
                        "SELECT LockedBy, Machine, LockedAt FROM Z_PumsTaskLock WITH (UPDLOCK,HOLDLOCK) WHERE LockKey = @k",
                        conn, tx))
                    {
                        sel.Parameters.AddWithValue("@k", lockKey);
                        using (SqlDataReader r = sel.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                existingUser = r.GetString(0);
                                existingMachine = r.IsDBNull(1) ? null : r.GetString(1);
                                lockedAt = r.GetDateTime(2);
                            }
                        }
                    }

                    if (existingUser != null)
                    {
                        // Stale lock? Steal it.
                        if (lockedAt.HasValue && (DateTime.UtcNow - lockedAt.Value).TotalMinutes > StaleMinutes)
                        {
                            using (SqlCommand del = new SqlCommand(
                                "DELETE FROM Z_PumsTaskLock WHERE LockKey = @k", conn, tx))
                            {
                                del.Parameters.AddWithValue("@k", lockKey);
                                del.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            tx.Commit();
                            return existingUser + " @ " + (existingMachine ?? "?") +
                                   " since " + (lockedAt.HasValue ? lockedAt.Value.ToLocalTime().ToString("HH:mm:ss") : "?");
                        }
                    }

                    using (SqlCommand ins = new SqlCommand(@"
                        INSERT INTO Z_PumsTaskLock (LockKey, LockedBy, Machine, LockedAt)
                        VALUES (@k, @u, @m, SYSUTCDATETIME())", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@k", lockKey);
                        ins.Parameters.AddWithValue("@u", user ?? "?");
                        ins.Parameters.AddWithValue("@m", (object)(machine ?? string.Empty));
                        ins.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return null;
                }
            }
        }

        public static void Release(DBSetting db, string lockKey, string user)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Z_PumsTaskLock WHERE LockKey = @k AND LockedBy = @u", conn))
                {
                    cmd.Parameters.AddWithValue("@k", lockKey);
                    cmd.Parameters.AddWithValue("@u", user ?? "?");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Returns a human-readable description of the current holder, or null if free.</summary>
        public static string Inspect(DBSetting db, string lockKey)
        {
            EnsureTable(db);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT LockedBy, Machine, LockedAt FROM Z_PumsTaskLock WHERE LockKey = @k", conn))
                {
                    cmd.Parameters.AddWithValue("@k", lockKey);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        string u = r.GetString(0);
                        string m = r.IsDBNull(1) ? "" : r.GetString(1);
                        DateTime t = r.GetDateTime(2);
                        return u + (m.Length > 0 ? " @ " + m : "") +
                               " since " + t.ToLocalTime().ToString("HH:mm:ss");
                    }
                }
            }
        }
    }
}
