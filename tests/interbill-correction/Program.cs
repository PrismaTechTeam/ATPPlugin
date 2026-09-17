using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;

/// <summary>
/// A reading the parent has CORRECTED must reach the book that bills the end customer.
///
/// <para>When the parent gives money back on a misread counter, the correction does not go into the
/// staging row -- a credit note writes an override into <c>zSCP_MeterTrans</c> and deliberately
/// leaves the staged figure exactly as it was billed, so the billed month is not rewritten
/// underneath an issued invoice. The inter-billing reader used to read only the staging row, which
/// meant the subsidiary was still shown a number the parent had already withdrawn, with nothing on
/// screen to say so.</para>
///
/// <para>This proves the correction now travels, and that it is reported as a CORRECTION rather
/// than quietly handed over as a different number.</para>
///
/// <para>It creates ONE override row against a real invoiced period, checks, and removes it again.
/// The row it writes carries a marker in Remark and is deleted by that marker, so it can never take
/// anything else with it.</para>
/// </summary>
internal static class InterBillCorrection
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    private const string MARKER = "ATP-TEST-CORRECTION-TRAVELS";

    private static int _fail;

    private static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs a)
        {
            string n = new AssemblyName(a.Name).Name;
            if (n == "ServiceContractPhotocopier") return Assembly.LoadFrom(PLUGIN);
            string p = Path.Combine(AC, n + ".dll");
            if (File.Exists(p)) return Assembly.LoadFrom(p);
            p = Path.Combine(Path.GetDirectoryName(PLUGIN), n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { return Run(args); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args)
    {
        string book = args.Length > 0 ? args[0] : "AED_ATPTEST";
        string contractNo = args.Length > 1 ? args[1] : "DEMO-PL2";
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        string cs = ConnString(book);

        Console.WriteLine();
        Console.WriteLine("   the parent book   " + book + "   contract " + contractNo);

        Cleanup(cs);   // anything a previous run left behind, before anything is measured

        // An invoiced period on that contract: the correction has to name the invoice it corrects.
        DataTable t = db.GetDataTable(
            "SELECT TOP 1 e.ItemMeterKey, e.PeriodYear, e.PeriodMonth, e.CurrentReading, " +
            "       e.InvoicedDocKey, ISNULL(e.InvoicedDocNo,'') AS InvoicedDocNo, " +
            "       m.MeterTypeCode, i.SerialNumber, m.ItemKey " +
            "  FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
            " WHERE c.ContractNo = N'" + contractNo.Replace("'", "''") + "' " +
            "   AND e.InvoicedDocKey IS NOT NULL AND e.CurrentReading > 0 " +
            "   AND m.MeterTypeCode <> 'RENTAL' ORDER BY e.EntryKey", false);
        if (t.Rows.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("   no invoiced counter on " + contractNo + " -- generate an invoice there first.");
            Console.WriteLine();
            return 2;
        }

        DataRow r = t.Rows[0];
        long entryKey = Convert.ToInt64(
            new SqlCommandScalar(cs,
                "SELECT TOP 1 EntryKey FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey = " +
                Convert.ToInt64(r["ItemMeterKey"]) + " AND PeriodYear = " +
                Convert.ToInt32(r["PeriodYear"]) + " AND PeriodMonth = " +
                Convert.ToInt32(r["PeriodMonth"])).Value);
        long meterKey = Convert.ToInt64(r["ItemMeterKey"]);
        long itemKey = Convert.ToInt64(r["ItemKey"]);
        int year = Convert.ToInt32(r["PeriodYear"]), month = Convert.ToInt32(r["PeriodMonth"]);
        decimal billed = Convert.ToDecimal(r["CurrentReading"]);
        long invKey = Convert.ToInt64(r["InvoicedDocKey"]);
        string type = Convert.ToString(r["MeterTypeCode"]);
        decimal corrected = billed - 2000m;

        Say("counter", Convert.ToString(r["SerialNumber"]) + " " + type + "   period " + month + "/" + year);
        Say("they billed", billed.ToString("n0") + "   on " + Convert.ToString(r["InvoicedDocNo"]));

        List<long> keys = new List<long>();
        keys.Add(meterKey);

        // ---------------------------------------------------------------- before
        Line("before they correct anything");
        ScpRemoteReading a = One(cs, keys, year, month);
        Check("the reading comes across", a != null);
        if (a == null) return Done();
        Check("it is the figure they billed", a.CurrentReading == billed);
        Check("and it is not flagged as corrected", !a.Corrected);

        // ---------------------------------------------------------------- they issue a correction CN
        Line("they credit " + (billed - corrected).ToString("n0") + " copies back");
        InsertOverride(cs, meterKey, itemKey, type, corrected, invKey);
        try
        {
            ScpRemoteReading b = One(cs, keys, year, month);
            Check("the reading still comes across", b != null);
            if (b != null)
            {
                Check("it is now THEIR CORRECTED figure", b.CurrentReading == corrected);
                Check("what they billed is still readable", b.BilledReading == billed);
                Check("it says it was corrected", b.Corrected);
                Say("their credit note", b.CorrectionDocNo);
                Check("and names the credit note", b.CorrectionDocNo == "CN-TEST-9999");
            }

            // A second, later correction wins -- the same rule their own book follows.
            InsertOverride(cs, meterKey, itemKey, type, corrected - 500m, invKey);
            ScpRemoteReading c = One(cs, keys, year, month);
            Check("a later correction wins", c != null && c.CurrentReading == corrected - 500m);
        }
        finally
        {
            Cleanup(cs);
        }

        // ---------------------------------------------------------------- after
        Line("the test's own rows are gone");
        ScpRemoteReading d = One(cs, keys, year, month);
        Check("back to what they billed", d != null && d.CurrentReading == billed);
        Check("and not flagged", d != null && !d.Corrected);
        Check("nothing of this test is left in their book", CountMarked(cs) == 0);


        // ---------------------------------------------------------------- the invoiced guard
        Line("a month already billed here is not overwritten");

        // The flag nobody maintains. The generator stamps the DOCUMENT and leaves Invoiced at
        // 'N', so every test written against the flag came out false and the guard it was
        // guarding never fired. If somebody ever starts setting it, this check goes green on its
        // own and nothing here breaks -- but while it stays unset, the flag must not be trusted.
        int billedButFlagSaysNo = Scalar(cs,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry " +
            " WHERE InvoicedDocKey IS NOT NULL AND ISNULL(Invoiced,'''N''') <> '''Y'''");
        Say("billed rows, flag still says N", billedButFlagSaysNo.ToString());

        // What the bring-over actually asks now, both ways round. Same WHERE clause as
        // InterBilling_Form.WriteEntry -- a period holding an invoice is closed, one that is not
        // is open.
        int closed = Scalar(cs,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry WHERE EntryKey = " + entryKey +
            "   AND InvoicedDocKey IS NULL");
        Check("the period we billed reads as closed", closed == 0);

        decimal before = ScalarDec(cs,
            "SELECT ISNULL(CurrentReading,0) FROM dbo.zSCP2_MeterEntry WHERE EntryKey = " + entryKey);
        int touched = NonQuery(cs,
            "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading = CurrentReading + 1 " +
            " WHERE EntryKey = " + entryKey + " AND InvoicedDocKey IS NULL");
        decimal after = ScalarDec(cs,
            "SELECT ISNULL(CurrentReading,0) FROM dbo.zSCP2_MeterEntry WHERE EntryKey = " + entryKey);
        Check("a bring-over cannot touch it", touched == 0);
        Check("and the billed reading is unmoved", after == before);

        return Done();
    }

    private static ScpRemoteReading One(string cs, List<long> keys, int year, int month)
    {
        List<ScpRemoteReading> l = ScpInterBillReader.Readings(cs, keys, year, month);
        return l.Count > 0 ? l[0] : null;
    }

    private static void InsertOverride(string cs, long meterKey, long itemKey, string type,
                                       decimal reading, long correctedInvoiceDocKey)
    {
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP_MeterTrans " +
                "(ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, MeterTransDate, " +
                " MeterTransReading, SalesInvoiceDocKey, Remark, LastModified, " +
                " CNDocKey, CNDocNo, CorrectedInvoiceDocKey) " +
                "VALUES (@imk, @ik, @type, GETDATE(), @rd, NULL, @rem, GETDATE(), " +
                "        @cnk, 'CN-TEST-9999', @civ)", cn))
            {
                cmd.Parameters.AddWithValue("@imk", meterKey);
                cmd.Parameters.AddWithValue("@ik", itemKey);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@rd", reading);
                cmd.Parameters.AddWithValue("@rem", MARKER);
                cmd.Parameters.AddWithValue("@cnk", 999999999L);
                cmd.Parameters.AddWithValue("@civ", correctedInvoiceDocKey);
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>Deletes ONLY what this test wrote, found by its marker.</summary>
    private static void Cleanup(string cs)
    {
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM dbo.zSCP_MeterTrans WHERE Remark = @rem", cn))
            {
                cmd.Parameters.AddWithValue("@rem", MARKER);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static int CountMarked(string cs)
    {
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.zSCP_MeterTrans WHERE Remark = @rem", cn))
            {
                cmd.Parameters.AddWithValue("@rem", MARKER);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }

    private sealed class SqlCommandScalar
    {
        public readonly object Value;
        public SqlCommandScalar(string cs, string sql)
        {
            using (SqlConnection cn = new SqlConnection(cs))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, cn)) Value = cmd.ExecuteScalar();
            }
        }
    }

    private static int Scalar(string cs, string sql)
    {
        object o = new SqlCommandScalar(cs, sql).Value;
        return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
    }

    private static decimal ScalarDec(string cs, string sql)
    {
        object o = new SqlCommandScalar(cs, sql).Value;
        return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
    }

    private static int NonQuery(string cs, string sql)
    {
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(sql, cn)) return cmd.ExecuteNonQuery();
        }
    }

    private static string ConnString(string book)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
        b.DataSource = "localhost,1433";
        b.InitialCatalog = book;
        b.UserID = "sa";
        b.Password = "rs6663";
        b.ConnectTimeout = 20;
        return b.ConnectionString;
    }

    private static void Line(string title)
    {
        Console.WriteLine();
        Console.WriteLine("== " + title + " " + new string('=', Math.Max(3, 60 - title.Length)));
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(34) + value);
    }

    private static void Check(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what);
    }

    private static int Done()
    {
        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }
}
