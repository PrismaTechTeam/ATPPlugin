using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// The permanent identity of an account book: one GUID, issued once, never reissued.
    ///
    /// <para>Inter-billing links rows across two books, and every link has to name the book it came
    /// from in a way that still means the same thing in ten years. Neither of the obvious names can
    /// do that. A database name is whatever the person restoring the backup typed; a company name
    /// changes when the company is renamed, and two books on one server can carry the same one. A
    /// GUID written into the book on first load is the book itself -- copy it, move it, rename it,
    /// and the links still point where they pointed.</para>
    ///
    /// <para>Read constantly (every counter the "may not be deleted" rule looks at), written once,
    /// so it is cached per book for the life of the process.</para>
    /// </summary>
    public static class ScpBookIdentity
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Guid> Cache =
            new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        /// <summary>This book's own id. <see cref="Guid.Empty"/> only if the table is not there yet,
        /// which means the migrations have not run -- never a real identity.</summary>
        public static Guid Local(DBSetting db)
        {
            if (db == null) return Guid.Empty;
            return Of(db.ConnectionString);
        }

        /// <summary>The id of whichever book that connection string opens -- this one, or the other
        /// one across the network.</summary>
        public static Guid Of(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return Guid.Empty;
            lock (Gate)
            {
                Guid hit;
                if (Cache.TryGetValue(connectionString, out hit)) return hit;
            }

            Guid id = Read(connectionString);

            // Only a real answer is remembered. Caching Guid.Empty would mean a book whose migration
            // ran a second later stayed nameless until the program was restarted.
            if (id != Guid.Empty)
            {
                lock (Gate) { Cache[connectionString] = id; }
            }
            return id;
        }

        /// <summary>Reads the identity, creating it if the table is there and empty. Returns
        /// <see cref="Guid.Empty"/> if the book has no such table -- which is exactly how the module
        /// recognises a book that does not have this plugin installed.</summary>
        public static Guid Read(string connectionString)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 BookId FROM dbo.zSCP2_BookIdentity", cn))
                    {
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) return (Guid)o;
                    }

                    // Table exists but is empty: an interrupted first load. Issue one now rather than
                    // leaving the book unable to be linked until somebody notices.
                    using (SqlCommand ins = new SqlCommand(
                        "INSERT INTO dbo.zSCP2_BookIdentity (OnlyRow, BookId) " +
                        "SELECT 1, NEWID() WHERE NOT EXISTS (SELECT 1 FROM dbo.zSCP2_BookIdentity); " +
                        "SELECT TOP 1 BookId FROM dbo.zSCP2_BookIdentity", cn))
                    {
                        object o = ins.ExecuteScalar();
                        if (o != null && o != DBNull.Value) return (Guid)o;
                    }
                }
            }
            catch
            {
                // No table, no permission, no server. All of them mean the same thing to the caller:
                // this book cannot be named, so nothing may be linked to it.
            }
            return Guid.Empty;
        }
    }
}
