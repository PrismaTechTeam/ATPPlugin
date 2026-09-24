using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One of the other book's contracts, as this book sees it on the incoming list.</summary>
    public class ScpRemoteContract
    {
        public long ContractKey;
        public string ContractNo = "";
        public string DebtorCode = "";
        public string DebtorName = "";
        public string Description = "";
        public DateTime? StartDate;
        public DateTime? ExpiryDate;
        public bool Inactive;
        public int MachineCount;

        /// <summary>How THEY bill it: one invoice per contract or per machine, how the lines group,
        /// whether the rental goes on its own paper, which day of the month.
        ///
        /// <para>Carried across as the STARTING point, for the same reason the machines and the
        /// opening readings are. It is the only information that exists; anything else would be a
        /// number this module invented. It is an ordinary setting on an ordinary contract the moment
        /// it lands, and changing it is nobody's business but the billing company's.</para>
        ///
        /// <para>The named Billing Format is deliberately NOT carried: a format is a name in THEIR
        /// book, and the same name here could mean something else. What a format sets -- these
        /// values -- comes across instead, so the shape matches without borrowing the name.</para></summary>
        public string BillingMode = "G";
        public string RentalLineMode = "S";
        public string MeterLineMode = "S";
        public string RentalSeparateInvoice = "N";
        public string MachineLineShows = "L";
        public int BillingDay = 1;

        /// <summary>Has this book already made a contract from it? Filled by the screen from its own
        /// links, not read across -- the other book knows nothing about who took what.</summary>
        public bool AlreadyTaken;
    }

    /// <summary>One machine on one of the other book's contracts.</summary>
    public class ScpRemoteMachine
    {
        public long ItemKey;
        public string ServiceItemNo = "";
        public string SerialNumber = "";
        public string ItemCode = "";
        public string Description = "";
        public bool Inactive;

        /// <summary>How the other book has this machine folded onto its printed lines: which
        /// machines share a rental line, which share a copies line, which invoice it goes on, and
        /// what that line is called. Carried across as the STARTING POINT for this book's own
        /// arrangement -- see ScpInterBillTake.InsertMachine.</summary>
        public string MergeGroupCode = "";
        public string MergeGroupCodeMeter = "";
        public string BillGroupCode = "";
        public string LineGroupCode = "";

        public List<ScpRemoteMeter> Meters = new List<ScpRemoteMeter>();

        /// <summary>What is compared later to decide "they changed this". Only the facts that are
        /// THEIRS: the machine, its serial, its model, whether it is still on hire. Not one price --
        /// prices on this side of the line belong to whoever is billing the end customer.</summary>
        public string Snapshot()
        {
            List<KeyValuePair<string, string>> v = new List<KeyValuePair<string, string>>();
            v.Add(new KeyValuePair<string, string>("SERIAL", SerialNumber));
            v.Add(new KeyValuePair<string, string>("MODEL", ItemCode));
            v.Add(new KeyValuePair<string, string>("DESC", Description));
            v.Add(new KeyValuePair<string, string>("ACTIVE", Inactive ? "N" : "Y"));
            v.Add(new KeyValuePair<string, string>("METERS", Meters.Count.ToString()));
            return ScpInterBillSnapshot.Format(v);
        }
    }

    /// <summary>One price the other book agreed for a whole printed line.</summary>
    public class ScpRemoteLinePrice
    {
        /// <summary>"R" the rental side, "M" the copies side.</summary>
        public string Side = "R";
        public string GroupCode = "";
        public decimal UnitPrice;
        public decimal BkPrice;
        public decimal ClPrice;
        public string LadderBk = "";
        public string LadderCl = "";
    }

    /// <summary>One counter on one of the other book's machines.</summary>
    public class ScpRemoteMeter
    {
        public long ItemMeterKey;
        public long ItemKey;
        public string MeterTypeCode = "";
        public string MeterRole = "";
        public string MachineSerialNo = "";
        public string Description = "";
        public decimal InitialReading;

        /// <summary>The other book's own rate. Taken as a STARTING POINT for the price the billing
        /// book agrees with its own customer, and never as the price itself -- the whole reason two
        /// books bill the same machine is that the two prices are different.</summary>
        public decimal ChargesRate;
        public decimal MinimumCharges;
        /// <summary>'A' / 'P': the rental billed with the month's copies or a month ahead (ATP-10). The
        /// other book keeps its machines in step with its contract.</summary>
        public string RentalBasis = "A";

        /// <summary>What is compared later to decide "they changed this".
        ///
        /// <para>Their RATE is in it, and has to be: this book's price is derived from theirs by a
        /// margin, so their price going up is the one change that silently makes this book's margin
        /// wrong. Without it in the snapshot nobody would ever be told.</para></summary>
        public string Snapshot()
        {
            List<KeyValuePair<string, string>> v = new List<KeyValuePair<string, string>>();
            v.Add(new KeyValuePair<string, string>("TYPE", MeterTypeCode));
            v.Add(new KeyValuePair<string, string>("ROLE", MeterRole));
            v.Add(new KeyValuePair<string, string>("SERIAL", MachineSerialNo));
            v.Add(new KeyValuePair<string, string>("RATE", ChargesRate.ToString("0.######")));
            v.Add(new KeyValuePair<string, string>("MIN", MinimumCharges.ToString("0.##")));
            return ScpInterBillSnapshot.Format(v);
        }
    }

    /// <summary>A reading the other book took. These always follow: they were never this book's.</summary>
    public class ScpRemoteReading
    {
        public long ItemMeterKey;
        public int PeriodYear;
        public int PeriodMonth;
        /// <summary>The reading they stand by today: their corrected figure when a credit note
        /// has revised it, otherwise the one they billed.</summary>
        public decimal CurrentReading;

        /// <summary>What they originally billed. Same as CurrentReading unless they have
        /// corrected it -- kept so this book can be told the figure MOVED, not just handed a
        /// different number.</summary>
        public decimal BilledReading;

        /// <summary>Their credit note, when the reading has been corrected. Empty otherwise.</summary>
        public string CorrectionDocNo = "";

        /// <summary>Has their reading been revised since they billed it?</summary>
        public bool Corrected { get { return CorrectionDocNo.Trim().Length > 0; } }

        public DateTime? ReadingDate;

        /// <summary>How they got it: ONLINE / OFFLINE (the meter API), MANUAL (keyed), INVOICE, ...
        /// Shown beside the figure so a reading's origin can be answered from this book.</summary>
        public string Source = "";
    }

    /// <summary>
    /// Everything this book reads out of the other one. One class, so there is one list of what
    /// crosses the line, and so that the SELECT-only rule can be seen to hold by reading a single
    /// file.
    ///
    /// <para>Nothing here writes. The billing book pulls; the machine book is never touched by it,
    /// and a user of the machine book has no way of telling that anybody is billing off it.</para>
    /// </summary>
    public static class ScpInterBillReader
    {
        /// <summary>The other book's contracts. Inactive ones are left out unless asked for: a
        /// contract that has expired over there is not something to start billing here.</summary>
        public static List<ScpRemoteContract> Contracts(string connectionString, bool includeInactive, string search)
        {
            List<ScpRemoteContract> list = new List<ScpRemoteContract>();
            if (string.IsNullOrEmpty(connectionString)) return list;

            string where = includeInactive ? "" : " WHERE ISNULL(c.Inactive,'N') = 'N' ";
            string like = (search ?? "").Trim();
            if (like.Length > 0)
            {
                where += (where.Length == 0 ? " WHERE " : " AND ") +
                         " (c.ContractNo LIKE @q OR c.DebtorCode LIKE @q OR c.Description LIKE @q " +
                         "  OR d.CompanyName LIKE @q) ";
            }

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, " +
                    "       ISNULL(d.CompanyName,'') AS DebtorName, ISNULL(c.Description,'') AS Description, " +
                    "       c.ServiceStartDate, c.ServiceExpiryDate, ISNULL(c.Inactive,'N') AS Inactive, " +
                    "       ISNULL(c.BillingMode,'G') AS BillingMode, ISNULL(c.RentalLineMode,'S') AS RentalLineMode, " +
                    "       ISNULL(c.MeterLineMode,'S') AS MeterLineMode, " +
                    "       ISNULL(c.RentalSeparateInvoice,'N') AS RentalSeparateInvoice, " +
                    "       ISNULL(c.MachineLineShows,'L') AS MachineLineShows, " +
                    "       ISNULL(c.BillingDay,1) AS BillingDay, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item i " +
                    "         WHERE i.ContractKey = c.ContractKey AND ISNULL(i.Inactive,'N') = 'N') AS Machines " +
                    "  FROM dbo.zSCP2_Contract c " +
                    "  LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    where +
                    " ORDER BY c.ContractNo", cn))
                {
                    if (like.Length > 0) cmd.Parameters.AddWithValue("@q", "%" + like + "%");
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ScpRemoteContract c = new ScpRemoteContract();
                            c.ContractKey = Convert.ToInt64(r["ContractKey"]);
                            c.ContractNo = Str(r["ContractNo"]);
                            c.DebtorCode = Str(r["DebtorCode"]);
                            c.DebtorName = Str(r["DebtorName"]);
                            c.Description = Str(r["Description"]);
                            c.StartDate = Dt(r["ServiceStartDate"]);
                            c.ExpiryDate = Dt(r["ServiceExpiryDate"]);
                            c.Inactive = Str(r["Inactive"]).ToUpperInvariant() == "Y";
                            c.MachineCount = Convert.ToInt32(r["Machines"]);
                            HowTheyBill(c, r);
                            list.Add(c);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>One named contract of theirs, or null if it is not there any more. Asked about a
        /// contract this book has already taken, so the answer being null is itself news.</summary>
        public static ScpRemoteContract Contract(string connectionString, long contractKey)
        {
            if (string.IsNullOrEmpty(connectionString) || contractKey <= 0) return null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, " +
                    "       ISNULL(d.CompanyName,'') AS DebtorName, ISNULL(c.Description,'') AS Description, " +
                    "       c.ServiceStartDate, c.ServiceExpiryDate, ISNULL(c.Inactive,'N') AS Inactive, " +
                    "       ISNULL(c.BillingMode,'G') AS BillingMode, ISNULL(c.RentalLineMode,'S') AS RentalLineMode, " +
                    "       ISNULL(c.MeterLineMode,'S') AS MeterLineMode, " +
                    "       ISNULL(c.RentalSeparateInvoice,'N') AS RentalSeparateInvoice, " +
                    "       ISNULL(c.MachineLineShows,'L') AS MachineLineShows, " +
                    "       ISNULL(c.BillingDay,1) AS BillingDay, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item i " +
                    "         WHERE i.ContractKey = c.ContractKey AND ISNULL(i.Inactive,'N') = 'N') AS Machines " +
                    "  FROM dbo.zSCP2_Contract c " +
                    "  LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    " WHERE c.ContractKey = @ck", cn))
                {
                    cmd.Parameters.AddWithValue("@ck", contractKey);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        ScpRemoteContract c = new ScpRemoteContract();
                        c.ContractKey = Convert.ToInt64(r["ContractKey"]);
                        c.ContractNo = Str(r["ContractNo"]);
                        c.DebtorCode = Str(r["DebtorCode"]);
                        c.DebtorName = Str(r["DebtorName"]);
                        c.Description = Str(r["Description"]);
                        c.StartDate = Dt(r["ServiceStartDate"]);
                        c.ExpiryDate = Dt(r["ServiceExpiryDate"]);
                        c.Inactive = Str(r["Inactive"]).ToUpperInvariant() == "Y";
                        c.MachineCount = Convert.ToInt32(r["Machines"]);
                        HowTheyBill(c, r);
                        return c;
                    }
                }
            }
        }

        /// <summary>The machines on one of the other book's contracts, with their counters attached.
        /// Two queries, not one per machine: a fleet contract has fifty machines and this runs across
        /// a network.</summary>
        public static List<ScpRemoteMachine> Machines(string connectionString, long contractKey)
        {
            List<ScpRemoteMachine> list = new List<ScpRemoteMachine>();
            if (string.IsNullOrEmpty(connectionString) || contractKey <= 0) return list;

            Dictionary<long, ScpRemoteMachine> byKey = new Dictionary<long, ScpRemoteMachine>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ItemKey, ISNULL(ServiceItemNo,'') AS ServiceItemNo, " +
                    "       ISNULL(SerialNumber,'') AS SerialNumber, ISNULL(ItemCode,'') AS ItemCode, " +
                    "       ISNULL(Description,'') AS Description, ISNULL(Inactive,'N') AS Inactive, " +
                    "       ISNULL(MergeGroupCode,'') AS MergeGroupCode, " +
                    "       ISNULL(MergeGroupCodeMeter,'') AS MergeGroupCodeMeter, " +
                    "       ISNULL(BillGroupCode,'') AS BillGroupCode, " +
                    "       ISNULL(LineGroupCode,'') AS LineGroupCode " +
                    "  FROM dbo.zSCP2_Item WHERE ContractKey = @ck ORDER BY Pos, ItemKey", cn))
                {
                    cmd.Parameters.AddWithValue("@ck", contractKey);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ScpRemoteMachine m = new ScpRemoteMachine();
                            m.ItemKey = Convert.ToInt64(r["ItemKey"]);
                            m.ServiceItemNo = Str(r["ServiceItemNo"]);
                            m.SerialNumber = Str(r["SerialNumber"]);
                            m.ItemCode = Str(r["ItemCode"]);
                            m.Description = Str(r["Description"]);
                            m.Inactive = Str(r["Inactive"]).ToUpperInvariant() == "Y";
                            m.MergeGroupCode = Str(r["MergeGroupCode"]);
                            m.MergeGroupCodeMeter = Str(r["MergeGroupCodeMeter"]);
                            m.BillGroupCode = Str(r["BillGroupCode"]);
                            m.LineGroupCode = Str(r["LineGroupCode"]);
                            byKey[m.ItemKey] = m;
                            list.Add(m);
                        }
                    }
                }

                if (list.Count > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT m.ItemMeterKey, m.ItemKey, ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, " +
                        "       ISNULL(m.MeterRole,'') AS MeterRole, ISNULL(m.MachineSerialNo,'') AS MachineSerialNo, " +
                        "       ISNULL(m.Description,'') AS Description, ISNULL(m.InitialReading,0) AS InitialReading, " +
                        "       ISNULL(m.ChargesRate,0) AS ChargesRate, ISNULL(m.MinimumCharges,0) AS MinimumCharges, " +
                        "       ISNULL(m.RentalBasis,'A') AS RentalBasis " +
                        "  FROM dbo.zSCP2_ItemMeter m " +
                        "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                        " WHERE i.ContractKey = @ck ORDER BY m.ItemKey, m.ItemMeterKey", cn))
                    {
                        cmd.Parameters.AddWithValue("@ck", contractKey);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                ScpRemoteMeter t = new ScpRemoteMeter();
                                t.ItemMeterKey = Convert.ToInt64(r["ItemMeterKey"]);
                                t.ItemKey = Convert.ToInt64(r["ItemKey"]);
                                t.MeterTypeCode = Str(r["MeterTypeCode"]);
                                t.MeterRole = Str(r["MeterRole"]);
                                t.MachineSerialNo = Str(r["MachineSerialNo"]);
                                t.Description = Str(r["Description"]);
                                t.InitialReading = Convert.ToDecimal(r["InitialReading"]);
                                t.ChargesRate = Convert.ToDecimal(r["ChargesRate"]);
                                t.MinimumCharges = Convert.ToDecimal(r["MinimumCharges"]);
                                t.RentalBasis = Str(r["RentalBasis"]).Trim().ToUpperInvariant() == "P" ? "P" : "A";
                                ScpRemoteMachine owner;
                                if (byKey.TryGetValue(t.ItemKey, out owner)) owner.Meters.Add(t);
                            }
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// One price the other book agreed for a printed LINE rather than for a counter.
        ///
        /// <para>A fleet of seven renting at one figure has that figure here, once, not seven
        /// times on seven machines. Without it a taken contract keeps the grouping and loses the
        /// price the grouping was for, and every machine falls back to its own rate.</para>
        /// </summary>
        public static List<ScpRemoteLinePrice> LinePrices(string connectionString, long contractKey)
        {
            List<ScpRemoteLinePrice> list = new List<ScpRemoteLinePrice>();
            if (string.IsNullOrEmpty(connectionString) || contractKey <= 0) return list;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(Side,'R') AS Side, ISNULL(GroupCode,'') AS GroupCode, " +
                    "       ISNULL(UnitPrice,0) AS UnitPrice, ISNULL(BkPrice,0) AS BkPrice, " +
                    "       ISNULL(ClPrice,0) AS ClPrice, ISNULL(LadderBk,'') AS LadderBk, " +
                    "       ISNULL(LadderCl,'') AS LadderCl " +
                    "  FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = @ck", cn))
                {
                    cmd.Parameters.AddWithValue("@ck", contractKey);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            ScpRemoteLinePrice p = new ScpRemoteLinePrice();
                            p.Side = Str(r["Side"]);
                            p.GroupCode = Str(r["GroupCode"]);
                            p.UnitPrice = Convert.ToDecimal(r["UnitPrice"]);
                            p.BkPrice = Convert.ToDecimal(r["BkPrice"]);
                            p.ClPrice = Convert.ToDecimal(r["ClPrice"]);
                            p.LadderBk = Str(r["LadderBk"]);
                            p.LadderCl = Str(r["LadderCl"]);
                            list.Add(p);
                        }
                }
            }
            return list;
        }

        /// <summary>The other book's readings for a period, for the counters named. Readings are the
        /// one thing that always follows: they were never this book's to disagree with.</summary>
        public static List<ScpRemoteReading> Readings(
            string connectionString, IEnumerable<long> sourceMeterKeys, int year, int month)
        {
            List<ScpRemoteReading> list = new List<ScpRemoteReading>();
            if (string.IsNullOrEmpty(connectionString) || sourceMeterKeys == null) return list;

            string inList = InList(sourceMeterKeys);
            if (inList.Length == 0) return list;

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    // A reading they have CORRECTED is the reading they now stand by, and it is the
                    // one that must travel. Their correction does not live in the staging row --
                    // a credit note writes an override into zSCP_MeterTrans and leaves the staged
                    // figure exactly as it was billed. Reading only the staging row meant this book
                    // was still shown the number they had already given their customer back.
                    //
                    // The override says which INVOICE it corrects, and the staging row says which
                    // invoice it was billed on, so the two meet on that key and the correction lands
                    // on the right period. Newest correction wins, the same rule their own book
                    // follows.
                    "SELECT e.ItemMeterKey, e.PeriodYear, e.PeriodMonth, " +
                    "       ISNULL(cor.MeterTransReading, ISNULL(e.CurrentReading,0)) AS CurrentReading, " +
                    "       ISNULL(e.CurrentReading,0) AS BilledReading, " +
                    "       ISNULL(cor.CNDocNo,'') AS CorrectionDocNo, e.ReadingDate, ISNULL(e.Source,'') AS Source " +
                    "  FROM dbo.zSCP2_MeterEntry e " +
                    "  OUTER APPLY (SELECT TOP 1 p.MeterTransReading, ISNULL(p.CNDocNo,'') AS CNDocNo " +
                    "                 FROM dbo.zSCP_MeterTrans p " +
                    "                WHERE p.ServiceItemMeterTypeKey = e.ItemMeterKey " +
                    "                  AND p.CNDocKey IS NOT NULL " +
                    "                  AND p.CorrectedInvoiceDocKey = e.InvoicedDocKey " +
                    "                ORDER BY p.MeterTransKey DESC) cor " +
                    " WHERE e.PeriodYear = @y AND e.PeriodMonth = @m " +
                    "   AND e.ItemMeterKey IN (" + inList + ")", cn))
                {
                    cmd.Parameters.AddWithValue("@y", year);
                    cmd.Parameters.AddWithValue("@m", month);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ScpRemoteReading x = new ScpRemoteReading();
                            x.ItemMeterKey = Convert.ToInt64(r["ItemMeterKey"]);
                            x.PeriodYear = Convert.ToInt32(r["PeriodYear"]);
                            x.PeriodMonth = Convert.ToInt32(r["PeriodMonth"]);
                            x.CurrentReading = Convert.ToDecimal(r["CurrentReading"]);
                            x.BilledReading = Convert.ToDecimal(r["BilledReading"]);
                            x.CorrectionDocNo = Str(r["CorrectionDocNo"]);
                            x.ReadingDate = Dt(r["ReadingDate"]);
                            x.Source = Str(r["Source"]);
                            list.Add(x);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>For each counter named, the latest reading the other book has for a period BEFORE
        /// the one given. It is the baseline a contract taken mid-life must start from: without it
        /// this book's first invoice would bill from the install reading, the meter's whole life.</summary>
        public static Dictionary<long, decimal> PreviousReadings(
            string connectionString, IEnumerable<long> sourceMeterKeys, int year, int month)
        {
            Dictionary<long, decimal> map = new Dictionary<long, decimal>();
            if (string.IsNullOrEmpty(connectionString) || sourceMeterKeys == null) return map;
            string inList = InList(sourceMeterKeys);
            if (inList.Length == 0) return map;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT z.ItemMeterKey, z.CurrentReading FROM (" +
                    "  SELECT e.ItemMeterKey, ISNULL(e.CurrentReading,0) AS CurrentReading, " +
                    "         ROW_NUMBER() OVER (PARTITION BY e.ItemMeterKey ORDER BY e.PeriodYear DESC, e.PeriodMonth DESC) AS rn " +
                    "    FROM dbo.zSCP2_MeterEntry e " +
                    "   WHERE e.ItemMeterKey IN (" + inList + ") AND ISNULL(e.CurrentReading,0) > 0 " +
                    "     AND (e.PeriodYear * 12 + e.PeriodMonth) < (@y * 12 + @m)) z WHERE z.rn = 1", cn))
                {
                    cmd.Parameters.AddWithValue("@y", year);
                    cmd.Parameters.AddWithValue("@m", month);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        while (r.Read()) map[Convert.ToInt64(r["ItemMeterKey"])] = Convert.ToDecimal(r["CurrentReading"]);
                }
            }
            return map;
        }

        /// <summary>Which of the other book's invoices have been cancelled. Asked about the documents
        /// this book has already billed against, so the answer is small however big their book
        /// is.</summary>
        public static HashSet<long> CancelledInvoices(string connectionString, IEnumerable<long> sourceDocKeys)
        {
            HashSet<long> gone = new HashSet<long>();
            string inList = InList(sourceDocKeys);
            if (string.IsNullOrEmpty(connectionString) || inList.Length == 0) return gone;

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                // A document that is no longer there and one marked cancelled mean the same thing to
                // the book that billed off it, so both come back in one answer.
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT v.k FROM (SELECT CAST(value AS BIGINT) AS k FROM STRING_SPLIT(@keys, ',')) v " +
                    " LEFT JOIN dbo.ARInvoice i ON i.DocKey = v.k " +
                    " WHERE i.DocKey IS NULL OR ISNULL(i.Cancelled,'F') = 'T'", cn))
                {
                    cmd.Parameters.AddWithValue("@keys", inList);
                    try
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read()) gone.Add(Convert.ToInt64(r[0]));
                        }
                    }
                    catch
                    {
                        // STRING_SPLIT needs SQL 2016. On anything older this question simply goes
                        // unanswered -- which shows as "no news", never as a false cancellation.
                    }
                }
            }
            return gone;
        }

        private static string InList(IEnumerable<long> keys)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<long> seen = new List<long>();
            foreach (long k in keys)
            {
                if (k <= 0 || seen.Contains(k)) continue;
                seen.Add(k);
                if (sb.Length > 0) sb.Append(",");
                sb.Append(k);
            }
            return sb.ToString();
        }

        /// <summary>Their invoice shape, off the row.</summary>
        private static void HowTheyBill(ScpRemoteContract c, SqlDataReader r)
        {
            c.BillingMode = Pick(Str(r["BillingMode"]), "G", "S");
            c.RentalLineMode = Pick(Str(r["RentalLineMode"]), "S", "M", "A");
            c.MeterLineMode = Pick(Str(r["MeterLineMode"]), "S", "M", "A");
            c.RentalSeparateInvoice = Str(r["RentalSeparateInvoice"]).ToUpperInvariant() == "Y" ? "Y" : "N";
            c.MachineLineShows = Pick(Str(r["MachineLineShows"]), "L", "M", "B");
            int d = 1;
            try { d = Convert.ToInt32(r["BillingDay"]); } catch { }
            c.BillingDay = d >= 1 && d <= 28 ? d : 1;
        }

        /// <summary>The value if this book will accept it, else the first of the allowed ones. A mode
        /// their book has and this one does not must not travel: the column has a CHECK on it, and a
        /// value it refuses would fail the whole take on the contract insert.</summary>
        private static string Pick(string value, params string[] allowed)
        {
            string v = (value ?? "").Trim().ToUpperInvariant();
            foreach (string a in allowed) if (v == a) return a;
            return allowed[0];
        }

        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        private static DateTime? Dt(object o)
        {
            return o == null || o == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(o);
        }
    }
}
