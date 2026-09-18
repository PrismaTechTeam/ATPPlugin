using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Copies the machine serial numbers recorded on Delivery Orders / Invoices in one account book onto
// the SAME documents in another -- matched by document number and line -- so the contract screen's
// "Generate From Serial No" has something to list.
//
// Only dbo.SerialNoTrans is written: that is the one table the picker reads, and it is all the
// source book holds for these documents too (their DODTL.SerialNoList is empty there as well).
// Nothing else in the target is touched.
//
// TransKey comes from AutoCount's own global key generator (DBRegistry.NewGlobalUniqueKey), the
// same one AutoCount uses when it saves a serial, so these rows can never collide with a key
// AutoCount hands out later.
//
// Without --commit everything runs inside a transaction that is rolled back at the end: the report
// says exactly what WOULD be written. Re-running after a commit adds nothing -- a serial already on
// its line is skipped.
//
//   serialnocopy.exe <srcServer> <srcDb> <dstServer> <dstDb> <saPassword> [--commit]
internal static class SerialNoCopy
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";

    [STAThread]
    private static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs a)
        {
            string p = Path.Combine(AC, new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        if (args.Length < 5)
        {
            Console.WriteLine("usage: serialnocopy <srcServer> <srcDb> <dstServer> <dstDb> <saPassword> [--commit]");
            return 2;
        }
        try { return Run(args[0], args[1], args[2], args[3], args[4], args.Length > 5 && args[5] == "--commit"); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    private sealed class Row
    {
        public string DocType, DocNo, ItemCode, FromSerialNo, ToSerialNo, TransferedSN, Remarks, Cancelled, Note;
        public int Seq;
        public object Count, ManufacturedDate, ExpiryDate, LastSalesDate;
        public long DstDocKey, DstDtlKey;
        public DateTime DstDocDate;
        public string Problem = "";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string srcServer, string srcDb, string dstServer, string dstDb, string pwd, bool commit)
    {
        string srcCs = "Server=" + srcServer + ";Database=" + srcDb + ";User Id=sa;Password=" + pwd + ";";
        string dstCs = "Server=" + dstServer + ";Database=" + dstDb + ";User Id=sa;Password=" + pwd + ";";
        Console.WriteLine((commit ? "COMMIT" : "DRY RUN (rolled back)") + ":  " + srcDb + " @ " + srcServer +
                          "  ->  " + dstDb + " @ " + dstServer);

        // ---- 1. what the source book has --------------------------------------------------
        List<Row> rows = new List<Row>();
        using (SqlConnection src = new SqlConnection(srcCs))
        {
            src.Open();
            SqlCommand q = new SqlCommand(
                "SELECT st.DocType, RTRIM(h.DocNo) AS DocNo, d.Seq, RTRIM(st.ItemCode) AS ItemCode, " +
                "       st.FromSerialNo, st.ToSerialNo, st.[Count], st.TransferedSN, st.ManufacturedDate, " +
                "       st.ExpiryDate, st.LastSalesDate, st.Remarks, st.Cancelled, st.Note " +
                "  FROM dbo.SerialNoTrans st " +
                "  JOIN (SELECT 'DO' AS T, DocKey, DocNo FROM dbo.DO UNION ALL SELECT 'IV', DocKey, DocNo FROM dbo.IV) h " +
                "    ON h.T = st.DocType AND h.DocKey = st.DocKey " +
                "  JOIN (SELECT 'DO' AS T, DtlKey, Seq FROM dbo.DODTL UNION ALL SELECT 'IV', DtlKey, Seq FROM dbo.IVDTL) d " +
                "    ON d.T = st.DocType AND d.DtlKey = st.DtlKey " +
                " WHERE st.DocType IN ('DO','IV') " +
                " ORDER BY h.DocNo, d.Seq, st.FromSerialNo", src);
            using (SqlDataReader r = q.ExecuteReader())
                while (r.Read())
                {
                    Row x = new Row();
                    x.DocType = S(r["DocType"]); x.DocNo = S(r["DocNo"]); x.Seq = Convert.ToInt32(r["Seq"]);
                    x.ItemCode = S(r["ItemCode"]); x.FromSerialNo = S(r["FromSerialNo"]);
                    x.ToSerialNo = r["ToSerialNo"] == DBNull.Value ? null : S(r["ToSerialNo"]);
                    x.Count = r["Count"]; x.TransferedSN = r["TransferedSN"] == DBNull.Value ? null : S(r["TransferedSN"]);
                    x.ManufacturedDate = r["ManufacturedDate"]; x.ExpiryDate = r["ExpiryDate"];
                    x.LastSalesDate = r["LastSalesDate"];
                    x.Remarks = r["Remarks"] == DBNull.Value ? null : S(r["Remarks"]);
                    x.Cancelled = r["Cancelled"] == DBNull.Value ? null : S(r["Cancelled"]);
                    x.Note = r["Note"] == DBNull.Value ? null : S(r["Note"]);
                    rows.Add(x);
                }
        }
        Console.WriteLine("   source serial rows: " + rows.Count);

        // ---- 2. find each one's document and line in the target ------------------------------
        using (SqlConnection dst = new SqlConnection(dstCs))
        {
            dst.Open();
            foreach (Row x in rows)
            {
                string hdr = x.DocType == "DO" ? "dbo.DO" : "dbo.IV";
                string dtl = x.DocType == "DO" ? "dbo.DODTL" : "dbo.IVDTL";
                SqlCommand f = new SqlCommand(
                    "SELECT h.DocKey, h.DocDate, d.DtlKey, RTRIM(ISNULL(d.ItemCode,'')) AS ItemCode " +
                    "  FROM " + hdr + " h JOIN " + dtl + " d ON d.DocKey = h.DocKey " +
                    " WHERE h.DocNo = @no AND d.Seq = @seq", dst);
                f.Parameters.AddWithValue("@no", x.DocNo);
                f.Parameters.AddWithValue("@seq", x.Seq);
                bool matched = false;
                string lineProblem = "";
                using (SqlDataReader r = f.ExecuteReader())
                {
                    if (r.Read())
                    {
                        string tItem = S(r["ItemCode"]);
                        if (string.Equals(tItem, x.ItemCode, StringComparison.OrdinalIgnoreCase))
                        {
                            x.DstDocKey = Convert.ToInt64(r["DocKey"]);
                            x.DstDocDate = Convert.ToDateTime(r["DocDate"]);
                            x.DstDtlKey = Convert.ToInt64(r["DtlKey"]);
                            matched = true;
                        }
                        else lineProblem = "line " + x.Seq + " is " + tItem + " in target, not " + x.ItemCode;
                    }
                    else lineProblem = "document or line not in target";
                }
                // The same document can carry its lines in a different order in the other book
                // (DO 00001883 has the machine on line 16 in one and line 32 in the other). Then the
                // item decides -- but only when exactly ONE line of that document carries it, so a
                // serial is never guessed onto one of two identical machines.
                if (!matched)
                {
                    SqlCommand byItem = new SqlCommand(
                        "SELECT h.DocKey, h.DocDate, d.DtlKey " +
                        "  FROM " + hdr + " h JOIN " + dtl + " d ON d.DocKey = h.DocKey " +
                        " WHERE h.DocNo = @no AND RTRIM(ISNULL(d.ItemCode,'')) = @item", dst);
                    byItem.Parameters.AddWithValue("@no", x.DocNo);
                    byItem.Parameters.AddWithValue("@item", x.ItemCode);
                    DataTable hits = new DataTable();
                    using (SqlDataAdapter a = new SqlDataAdapter(byItem)) a.Fill(hits);
                    if (hits.Rows.Count == 1)
                    {
                        x.DstDocKey = Convert.ToInt64(hits.Rows[0]["DocKey"]);
                        x.DstDocDate = Convert.ToDateTime(hits.Rows[0]["DocDate"]);
                        x.DstDtlKey = Convert.ToInt64(hits.Rows[0]["DtlKey"]);
                        Console.WriteLine("   note  " + x.DocType + " " + x.DocNo + ": " + x.ItemCode +
                                          " is on a different line in the target -- matched by item");
                    }
                    else
                    {
                        x.Problem = lineProblem + (hits.Rows.Count > 1 ? "; " + hits.Rows.Count + " lines carry that item" : "");
                        continue;
                    }
                }
                SqlCommand dup = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.SerialNoTrans WHERE DocType = @t AND DocKey = @k AND DtlKey = @d AND FromSerialNo = @sn", dst);
                dup.Parameters.AddWithValue("@t", x.DocType);
                dup.Parameters.AddWithValue("@k", x.DstDocKey);
                dup.Parameters.AddWithValue("@d", x.DstDtlKey);
                dup.Parameters.AddWithValue("@sn", x.FromSerialNo);
                if (Convert.ToInt32(dup.ExecuteScalar()) > 0) x.Problem = "already on this line (skipped)";
            }
        }

        List<Row> go = rows.FindAll(delegate (Row x) { return x.Problem.Length == 0; });
        foreach (Row x in rows)
            if (x.Problem.Length > 0)
                Console.WriteLine("   skip  " + x.DocType + " " + x.DocNo + " line " + x.Seq + "  " + x.FromSerialNo + "  -- " + x.Problem);
        Console.WriteLine("   to write: " + go.Count + " of " + rows.Count);
        if (go.Count == 0) { Console.WriteLine("   nothing to do"); return 0; }

        // ---- 3. keys from AutoCount's own generator (only for a real commit) -------------------
        List<long> keys = new List<long>();
        if (commit)
        {
            AutoCount.Data.DBSetting acdb = new AutoCount.Data.DBSetting(
                AutoCount.Data.DBServerType.SQL2000, dstServer, "sa", pwd, dstDb, false);
            for (int i = 0; i < go.Count; i++) keys.Add(AutoCount.Data.DBRegistry.NewGlobalUniqueKey(acdb));
            Console.WriteLine("   keys from AutoCount: " + keys[0] + " .. " + keys[keys.Count - 1]);
        }
        else for (int i = 0; i < go.Count; i++) keys.Add(-1 - i);   // never persisted

        // ---- 4. write, in one transaction ------------------------------------------------------
        using (SqlConnection dst = new SqlConnection(dstCs))
        {
            dst.Open();
            SqlTransaction tx = dst.BeginTransaction();
            try
            {
                int before = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM dbo.SerialNoTrans", dst, tx).ExecuteScalar());
                for (int i = 0; i < go.Count; i++)
                {
                    Row x = go[i];
                    SqlCommand ins = new SqlCommand(
                        "INSERT INTO dbo.SerialNoTrans (TransKey, DocType, DocKey, DtlKey, FromSerialNo, ToSerialNo, [Count], " +
                        "  ItemCode, TransferedSN, FromDocDtlKey, ManufacturedDate, ExpiryDate, LastSalesDate, Remarks, " +
                        "  Cancelled, Note, DocDate, Guid) " +
                        "VALUES (@tk, @t, @k, @d, @from, @to, @cnt, @item, @tsn, NULL, @mfg, @exp, @last, @rem, @can, @note, @date, @guid)",
                        dst, tx);
                    ins.Parameters.AddWithValue("@tk", keys[i]);
                    ins.Parameters.AddWithValue("@t", x.DocType);
                    ins.Parameters.AddWithValue("@k", x.DstDocKey);
                    ins.Parameters.AddWithValue("@d", x.DstDtlKey);
                    ins.Parameters.AddWithValue("@from", x.FromSerialNo);
                    ins.Parameters.AddWithValue("@to", (object)x.ToSerialNo ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@cnt", x.Count ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@item", x.ItemCode);
                    ins.Parameters.AddWithValue("@tsn", (object)x.TransferedSN ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@mfg", x.ManufacturedDate ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@exp", x.ExpiryDate ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@last", x.LastSalesDate ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@rem", (object)x.Remarks ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@can", (object)x.Cancelled ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@note", (object)x.Note ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@date", x.DstDocDate);
                    ins.Parameters.AddWithValue("@guid", Guid.NewGuid());
                    ins.ExecuteNonQuery();
                }

                // What the contract screen's picker will now list -- its own filter, run in here.
                int picker = Convert.ToInt32(new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.SerialNoTrans st " +
                    "  JOIN (SELECT 'DO' AS DocType, DocKey FROM dbo.DO UNION ALL SELECT 'IV', DocKey FROM dbo.IV) h " +
                    "    ON h.DocType = st.DocType AND h.DocKey = st.DocKey " +
                    " WHERE st.DocType IN ('DO','IV') AND ISNULL(st.FromSerialNo,'') <> '' AND ISNULL(st.ToSerialNo,'') = ''",
                    dst, tx).ExecuteScalar());
                int after = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM dbo.SerialNoTrans", dst, tx).ExecuteScalar());
                Console.WriteLine("   SerialNoTrans: " + before + " -> " + after);
                Console.WriteLine("   Generate From Serial No would list: " + picker);

                if (commit) { tx.Commit(); Console.WriteLine("   COMMITTED"); }
                else { tx.Rollback(); Console.WriteLine("   rolled back -- nothing was written"); }
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }
        return 0;
    }

    private static string S(object v) { return v == null || v == DBNull.Value ? "" : Convert.ToString(v).Trim(); }
}
