using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

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

        /// <summary>The rest of how their contract bills, carried the same way (29/9, user: "必须带过来
        /// 不要懒惰"): when the free copies start again, billing at month end, the rental's own billing
        /// day, the period following the contract's start, and the strategy its rules came from.</summary>
        public string FocResetUnit = "M";
        public int FocResetN;
        public string BillOnMonthEnd = "N";
        public int RentalBillingDay;
        public string PeriodFollowContract = "N";
        public string StrategyCode = "";

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

        /// <summary>The machine's own billing facts (29/9): its own billing day (0 = none, the contract's), its
        /// own start and end when they differ from the contract's, how its readings arrive, and whether
        /// it is the contract's group machine.</summary>
        public int BillingDayOverride;
        public DateTime? StartDate;
        public DateTime? ExpiryDate;
        /// <summary>'ONLINE' / 'OFFLINE' as set over there; '' = follows the fetch status.</summary>
        public string MachineMode = "";
        public bool IsGroupItem;

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
        /// <summary>The minimum agreed for the line, and its waive: charges reach WaiveAt, WaiveAmt comes off.</summary>
        public decimal MinCharge;
        public decimal WaiveAt;
        public decimal WaiveAmt;
    }

    /// <summary>One of the other book's own strategy rules on a contract (its Strategy tab), with the
    /// machines it is bound to named by THEIR item keys.</summary>
    public class ScpRemoteRule
    {
        public int Seq;
        public string RuleKind = "";
        public string Scope = "";
        public string ServiceItemKeys = "";
        public decimal TargetAmount;
        public decimal PartialPct;
        public int FreeMonths;
        public decimal CommitAmount;
        public decimal FocCopies;
        public decimal RebatePct;
        public string NetBilling = "N";
        public string LimitScope = "S";
        public decimal LimitQty;
        public string Remark = "";
        public string SeededFromCode = "";
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

        /// <summary>The rest of the counter's deal (29/9, user: "必须带过来不要懒惰"). The take once
        /// brought the rate and the minimum only, so free copies were billed, a waive with a target
        /// waived every month, and a tiered counter billed one flat rate. All of it comes now: free
        /// copies and rebate, the waive's conditions, whose charges a minimum counts, the rental's own
        /// start and length, and the tier ladder with its mode.</summary>
        public decimal FocQty;
        public decimal RebatePct;
        public DateTime? RentalStartDate;
        public int RentalMonths;
        public int WaiveFirstNMonths;
        public decimal WaiveTargetAmount;
        public decimal WaivePartialPct = 100m;
        public string WaiveScope = "BKCL";
        public decimal WaivePartialThreshold;
        public decimal WaivePartialAmount;
        public string CommitScope = "S";
        /// <summary>'T' one price for all copies, 'I' split by tier (ATP-3). A book from before ATP-3
        /// has no such column and prices every ladder the one way it knew: 'T'.</summary>
        public string TierMode = "T";
        /// <summary>The ladder that prices this counter over there, bands of (copies, price) in order:
        /// its own ladder, else its price scheme's, else its meter type's -- the same order their
        /// billing uses. Empty = one flat rate.</summary>
        public List<decimal[]> Ladder = new List<decimal[]>();
        /// <summary>Where the ladder came from over there: "own", a scheme code, or "".</summary>
        public string LadderFrom = "";
        /// <summary>Its meter type bills flat or is a rental waive over there: its Free Qty is free
        /// MONTHS, never copies. Not part of the snapshot.</summary>
        public bool TypeFlatOrWaive;

        /// <summary>The ladder as the snapshot writes it: "3000|0.025,99999999|0.02".</summary>
        public string LadderText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (decimal[] b in Ladder)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(b[0].ToString("0.##", CultureInfo.InvariantCulture)).Append("|")
                  .Append(b[1].ToString("0.######", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>The waive's conditions in one value: first N months / target / partial % / partial
        /// threshold / partial amount. Which charges count (black / colour) is its own value, COLOUR --
        /// on a minimum it is the minimum's colour, not a waive.</summary>
        public string WaiveText()
        {
            return WaiveFirstNMonths.ToString(CultureInfo.InvariantCulture) + "/" +
                   WaiveTargetAmount.ToString("0.##", CultureInfo.InvariantCulture) + "/" +
                   WaivePartialPct.ToString("0.##", CultureInfo.InvariantCulture) + "/" +
                   WaivePartialThreshold.ToString("0.##", CultureInfo.InvariantCulture) + "/" +
                   WaivePartialAmount.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>The rental's own start and length in one value. Its basis (with the copies or a
        /// month ahead) is the contract's, carried at the take and locked once a rental is invoiced.</summary>
        public string RentalText()
        {
            return (RentalStartDate.HasValue ? RentalStartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "") +
                   "/" + RentalMonths.ToString(CultureInfo.InvariantCulture);
        }

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
            // The rest of the deal (29/9). A contract taken before these were in the snapshot is not
            // reported as changed for them -- see ScpInterBillSnapshot.Differences.
            // A rental's FOC is its free months still to come: a countdown each book runs down as it
            // bills a free month, not a term. In the snapshot it would report "changed at HQ" every
            // month HQ bills one. It is carried at the take (the months left then) and not compared.
            v.Add(new KeyValuePair<string, string>("FOC", ScpStrategy.IsRentalRole(MeterRole, MeterTypeCode)
                ? "-" : FocQty.ToString("0.##", CultureInfo.InvariantCulture)));
            v.Add(new KeyValuePair<string, string>("REBATE", RebatePct.ToString("0.##", CultureInfo.InvariantCulture)));
            v.Add(new KeyValuePair<string, string>("WAIVE", WaiveText()));
            // As the engine reads it: BK, CL, else both ('S' and '' from older rows are both).
            v.Add(new KeyValuePair<string, string>("COLOUR", ScpCommittedMin.NormScope(WaiveScope)));
            v.Add(new KeyValuePair<string, string>("COUNTS", CommitScope));
            v.Add(new KeyValuePair<string, string>("TIER", TierMode + (Ladder.Count > 0 ? ":" + LadderText() : "")));
            v.Add(new KeyValuePair<string, string>("RENTAL", RentalText()));
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
                    "       ISNULL(c.FOCResetUnit,'M') AS FOCResetUnit, ISNULL(c.FOCResetN,0) AS FOCResetN, " +
                    "       ISNULL(c.BillOnMonthEnd,'N') AS BillOnMonthEnd, ISNULL(c.RentalBillingDay,0) AS RentalBillingDay, " +
                    "       ISNULL(c.PeriodFollowContract,'N') AS PeriodFollowContract, ISNULL(c.StrategyCode,'') AS StrategyCode, " +
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
                    "       ISNULL(c.FOCResetUnit,'M') AS FOCResetUnit, ISNULL(c.FOCResetN,0) AS FOCResetN, " +
                    "       ISNULL(c.BillOnMonthEnd,'N') AS BillOnMonthEnd, ISNULL(c.RentalBillingDay,0) AS RentalBillingDay, " +
                    "       ISNULL(c.PeriodFollowContract,'N') AS PeriodFollowContract, ISNULL(c.StrategyCode,'') AS StrategyCode, " +
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
                    "       ISNULL(LineGroupCode,'') AS LineGroupCode, " +
                    "       ISNULL(BillingDayOverride,0) AS BillingDayOverride, ServiceStartDate, ServiceExpiryDate, " +
                    "       ISNULL(MachineMode,'') AS MachineMode, ISNULL(IsGroupItem,'N') AS IsGroupItem " +
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
                            int bd = 0;
                            try { bd = Convert.ToInt32(r["BillingDayOverride"]); } catch { }
                            m.BillingDayOverride = bd > 28 ? 28 : bd > 0 ? bd : 0;
                            m.StartDate = Dt(r["ServiceStartDate"]);
                            m.ExpiryDate = Dt(r["ServiceExpiryDate"]);
                            m.MachineMode = Str(r["MachineMode"]).ToUpperInvariant();
                            m.IsGroupItem = Str(r["IsGroupItem"]).ToUpperInvariant() == "Y";
                            byKey[m.ItemKey] = m;
                            list.Add(m);
                        }
                    }
                }

                if (list.Count > 0)
                {
                    // A book from before ATP-3 has no TierMode column; naming it would fail the whole read.
                    bool hasTierMode;
                    using (SqlCommand probe = new SqlCommand(
                        "SELECT CASE WHEN COL_LENGTH('dbo.zSCP2_ItemMeter','TierMode') IS NULL THEN 0 ELSE 1 END", cn))
                        hasTierMode = Convert.ToInt32(probe.ExecuteScalar()) == 1;
                    Dictionary<long, string> schemeOf = new Dictionary<long, string>();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT m.ItemMeterKey, m.ItemKey, ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, " +
                        "       ISNULL(m.MeterRole,'') AS MeterRole, ISNULL(m.MachineSerialNo,'') AS MachineSerialNo, " +
                        "       ISNULL(m.Description,'') AS Description, ISNULL(m.InitialReading,0) AS InitialReading, " +
                        "       ISNULL(m.ChargesRate,0) AS ChargesRate, ISNULL(m.MinimumCharges,0) AS MinimumCharges, " +
                        "       ISNULL(m.RentalBasis,'A') AS RentalBasis, " +
                        "       ISNULL(m.FOCQty,0) AS FOCQty, ISNULL(m.RebateQtyInPercent,0) AS RebatePct, " +
                        "       m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, " +
                        "       ISNULL(m.WaiveFirstNMonths,0) AS WaiveFirstNMonths, ISNULL(m.WaiveTargetAmount,0) AS WaiveTargetAmount, " +
                        "       ISNULL(m.WaivePartialPct,100) AS WaivePartialPct, ISNULL(NULLIF(m.WaiveScope,''),'BKCL') AS WaiveScope, " +
                        "       ISNULL(m.WaivePartialThreshold,0) AS WaivePartialThreshold, ISNULL(m.WaivePartialAmount,0) AS WaivePartialAmount, " +
                        "       ISNULL(NULLIF(m.CommitScope,''),'S') AS CommitScope, " +
                        (hasTierMode ? "ISNULL(m.TierMode,'T')" : "'T'") + " AS TierMode, " +
                        // The scheme their billing prices it by: the counter's own, else its meter type's.
                        "       ISNULL(NULLIF(m.MeterMultiPriceCode,''), ISNULL(mt.MeterMultiPriceCode,'')) AS SchemeCode, " +
                        "       CASE WHEN ISNULL(mt.IsFlatCharge,'N') = 'Y' OR ISNULL(mt.IsRentalWaive,'N') = 'Y' THEN 1 ELSE 0 END AS TypeFlatOrWaive " +
                        "  FROM dbo.zSCP2_ItemMeter m " +
                        "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                        "  LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
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
                                t.FocQty = Convert.ToDecimal(r["FOCQty"]);
                                t.RebatePct = Convert.ToDecimal(r["RebatePct"]);
                                t.RentalStartDate = Dt(r["RentalStartDate"]);
                                t.RentalMonths = Convert.ToInt32(r["RentalMonths"]);
                                t.WaiveFirstNMonths = Convert.ToInt32(r["WaiveFirstNMonths"]);
                                t.WaiveTargetAmount = Convert.ToDecimal(r["WaiveTargetAmount"]);
                                t.WaivePartialPct = Convert.ToDecimal(r["WaivePartialPct"]);
                                t.WaiveScope = Str(r["WaiveScope"]).ToUpperInvariant();
                                t.WaivePartialThreshold = Convert.ToDecimal(r["WaivePartialThreshold"]);
                                t.WaivePartialAmount = Convert.ToDecimal(r["WaivePartialAmount"]);
                                t.CommitScope = Str(r["CommitScope"]).ToUpperInvariant();
                                t.TierMode = Str(r["TierMode"]).ToUpperInvariant() == "I" ? "I" : "T";
                                string scheme = Str(r["SchemeCode"]);
                                if (scheme.Length > 0) schemeOf[t.ItemMeterKey] = scheme;
                                t.TypeFlatOrWaive = Convert.ToInt32(r["TypeFlatOrWaive"]) == 1;
                                ScpRemoteMachine owner;
                                if (byKey.TryGetValue(t.ItemKey, out owner)) owner.Meters.Add(t);
                            }
                        }
                    }
                    ReadLadders(cn, list, schemeOf);
                    // A book from before ATP-3 still writes "first 2,500 free" as a 0.00 first band and
                    // ignores a laddered counter's own Free Qty; this book counts both. Its terms are
                    // read as its own upgrade will leave them (v19 + FreeBandToFreeQty), so free copies
                    // are never given twice, and HQ upgrading later changes nothing on the board.
                    using (SqlCommand probe = new SqlCommand(
                        "SELECT CASE WHEN COL_LENGTH('dbo.zSCP2_Contract','TierMode') IS NULL THEN 1 ELSE 0 END", cn))
                        if (Convert.ToInt32(probe.ExecuteScalar()) == 1) AsUpgraded(cn, contractKey, list, schemeOf);
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
                    "       ISNULL(LadderCl,'') AS LadderCl, ISNULL(MinCharge,0) AS MinCharge, " +
                    "       ISNULL(WaiveAt,0) AS WaiveAt, ISNULL(WaiveAmt,0) AS WaiveAmt " +
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
                            p.MinCharge = Convert.ToDecimal(r["MinCharge"]);
                            p.WaiveAt = Convert.ToDecimal(r["WaiveAt"]);
                            p.WaiveAmt = Convert.ToDecimal(r["WaiveAmt"]);
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
            // Billing days are 1-28 (month-end billing retired, v8 -- a day above 28 becomes 28 on
            // every load). One above 28 from an older book is therefore 28 here, not the 1st.
            c.BillingDay = d > 28 ? 28 : d >= 1 ? d : 1;
            c.FocResetUnit = Pick(Str(r["FOCResetUnit"]), "M", "W", "D");
            int n = 0;
            try { n = Convert.ToInt32(r["FOCResetN"]); } catch { }
            c.FocResetN = n > 0 ? n : 0;
            c.BillOnMonthEnd = "N";   // retired (v8): always 'N'
            int rd = 0;
            try { rd = Convert.ToInt32(r["RentalBillingDay"]); } catch { }
            c.RentalBillingDay = rd > 28 ? 28 : rd > 0 ? rd : 0;
            c.PeriodFollowContract = Str(r["PeriodFollowContract"]).ToUpperInvariant() == "Y" ? "Y" : "N";
            c.StrategyCode = Str(r["StrategyCode"]);
        }

        /// <summary>The ladder each counter is priced by over there, in the order their billing looks:
        /// the counter's own ladder (zSCP2_ItemMeterPrice), else the scheme's bands. Read in two
        /// queries for the whole contract. A book without the per-counter table simply has none.</summary>
        private static void ReadLadders(SqlConnection cn, List<ScpRemoteMachine> machines, Dictionary<long, string> schemeOf)
        {
            Dictionary<long, ScpRemoteMeter> byKey = new Dictionary<long, ScpRemoteMeter>();
            foreach (ScpRemoteMachine m in machines)
                foreach (ScpRemoteMeter t in m.Meters) byKey[t.ItemMeterKey] = t;
            if (byKey.Count == 0) return;

            bool hasOwn;
            using (SqlCommand probe = new SqlCommand(
                "SELECT CASE WHEN OBJECT_ID('dbo.zSCP2_ItemMeterPrice') IS NULL THEN 0 ELSE 1 END", cn))
                hasOwn = Convert.ToInt32(probe.ExecuteScalar()) == 1;
            if (hasOwn)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ItemMeterKey, MeterReading, UnitPrice FROM dbo.zSCP2_ItemMeterPrice " +
                    " WHERE ItemMeterKey IN (" + InList(byKey.Keys) + ") ORDER BY ItemMeterKey, MeterReading", cn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        ScpRemoteMeter t;
                        if (!byKey.TryGetValue(Convert.ToInt64(r["ItemMeterKey"]), out t)) continue;
                        t.Ladder.Add(new decimal[] { Convert.ToDecimal(r["MeterReading"]), Convert.ToDecimal(r["UnitPrice"]) });
                        t.LadderFrom = "own";
                    }
                }
            }

            List<string> codes = new List<string>();
            foreach (KeyValuePair<long, string> kv in schemeOf)
            {
                ScpRemoteMeter t;
                if (byKey.TryGetValue(kv.Key, out t) && t.Ladder.Count == 0 && !codes.Contains(kv.Value)) codes.Add(kv.Value);
            }
            if (codes.Count == 0) return;
            Dictionary<string, List<decimal[]>> bands = new Dictionary<string, List<decimal[]>>(StringComparer.OrdinalIgnoreCase);
            List<string> names = new List<string>();
            for (int i = 0; i < codes.Count; i++) names.Add("@c" + i.ToString(CultureInfo.InvariantCulture));
            using (SqlCommand cmd = new SqlCommand(
                "SELECT MeterMultiPriceCode, MeterReading, UnitPrice FROM dbo.zSCP_MeterMultiPriceItem " +
                " WHERE MeterMultiPriceCode IN (" + string.Join(",", names.ToArray()) + ") " +
                " ORDER BY MeterMultiPriceCode, MeterReading", cn))
            {
                for (int i = 0; i < codes.Count; i++) cmd.Parameters.AddWithValue(names[i], codes[i]);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string code = Str(r["MeterMultiPriceCode"]);
                        List<decimal[]> l;
                        if (!bands.TryGetValue(code, out l)) { l = new List<decimal[]>(); bands[code] = l; }
                        l.Add(new decimal[] { Convert.ToDecimal(r["MeterReading"]), Convert.ToDecimal(r["UnitPrice"]) });
                    }
                }
            }
            foreach (KeyValuePair<long, string> kv in schemeOf)
            {
                ScpRemoteMeter t;
                List<decimal[]> l;
                if (!byKey.TryGetValue(kv.Key, out t) || t.Ladder.Count > 0 || !bands.TryGetValue(kv.Value, out l)) continue;
                foreach (decimal[] b in l) t.Ladder.Add(new decimal[] { b[0], b[1] });
                t.LadderFrom = kv.Value;
            }
        }

        /// <summary>
        /// An older HQ's free copies as its own upgrade will set them -- the same three rules, in the
        /// same order, as 02_Update_zSCP2_Contract_v19_TierMode.sql and
        /// 02_Update_zSCP_MeterMultiPriceItem_v2_FreeBandToFreeQty.sql. Rental and waive counters are
        /// never touched: their Free Qty is free months.
        /// <list type="number">
        /// <item>A ladder starting at a PAID rate: its Free Qty was ignored there -> 0.</item>
        /// <item>A black / colour counter priced only by its copy group's ladder on a new-layout
        /// contract: the group's 0.00 band was the allowance -> 0.</item>
        /// <item>A ladder starting at 0.00 with a paid band after it (not a scheme a copy group names):
        /// the band's width becomes the Free Qty and the band goes.</item>
        /// </list>
        /// </summary>
        private static void AsUpgraded(SqlConnection cn, long contractKey, List<ScpRemoteMachine> machines,
                                       Dictionary<long, string> schemeOf)
        {
            bool newLayout;
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CASE WHEN ISNULL(UseNewLayout,'N') = 'Y' OR ISNULL(BillingFormatCode,'') <> '' THEN 1 ELSE 0 END " +
                "  FROM dbo.zSCP2_Contract WHERE ContractKey = @ck", cn))
            {
                cmd.Parameters.AddWithValue("@ck", contractKey);
                object o = cmd.ExecuteScalar();
                newLayout = o != null && o != DBNull.Value && Convert.ToInt32(o) == 1;
            }
            // Copy groups of this contract whose ladder resolves (text, or a scheme with bands), per colour.
            HashSet<string> groupBk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> groupCl = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlCommand cmd = new SqlCommand(
                "SELECT LTRIM(RTRIM(g.GroupCode)) AS GroupCode, " +
                "       CASE WHEN ISNULL(g.LadderBk,'') <> '' AND (CHARINDEX('|', g.LadderBk) > 0 OR EXISTS (SELECT 1 FROM dbo.zSCP_MeterMultiPriceItem x " +
                "            WHERE x.MeterMultiPriceCode = LTRIM(RTRIM(g.LadderBk)))) THEN 1 ELSE 0 END AS Bk, " +
                "       CASE WHEN ISNULL(g.LadderCl,'') <> '' AND (CHARINDEX('|', g.LadderCl) > 0 OR EXISTS (SELECT 1 FROM dbo.zSCP_MeterMultiPriceItem x " +
                "            WHERE x.MeterMultiPriceCode = LTRIM(RTRIM(g.LadderCl)))) THEN 1 ELSE 0 END AS Cl " +
                "  FROM dbo.zSCP2_ContractRentalPrice g WHERE g.ContractKey = @ck AND ISNULL(g.Side,'R') = 'M'", cn))
            {
                cmd.Parameters.AddWithValue("@ck", contractKey);
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        if (Convert.ToInt32(r["Bk"]) == 1) groupBk.Add(Str(r["GroupCode"]));
                        if (Convert.ToInt32(r["Cl"]) == 1) groupCl.Add(Str(r["GroupCode"]));
                    }
            }
            // Schemes a copy group names as its tiers, anywhere in that book: never converted.
            HashSet<string> groupNamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqlCommand cmd = new SqlCommand(
                "SELECT DISTINCT LTRIM(RTRIM(LadderBk)) AS Code FROM dbo.zSCP2_ContractRentalPrice WHERE ISNULL(LadderBk,'') <> '' " +
                "UNION SELECT DISTINCT LTRIM(RTRIM(LadderCl)) FROM dbo.zSCP2_ContractRentalPrice WHERE ISNULL(LadderCl,'') <> ''", cn))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read()) groupNamed.Add(Str(r["Code"]));

            foreach (ScpRemoteMachine m in machines)
                foreach (ScpRemoteMeter t in m.Meters)
                {
                    if (t.TypeFlatOrWaive) continue;
                    if (t.Ladder.Count > 0)
                    {
                        bool paid = false;
                        foreach (decimal[] b in t.Ladder) if (b[1] != 0m) paid = true;
                        if (t.Ladder[0][1] != 0m) t.FocQty = 0m;                                   // (1)
                        else if (paid && (t.LadderFrom == "own" || !groupNamed.Contains(t.LadderFrom)))
                        {
                            t.FocQty = t.Ladder[0][0];                                             // (3)
                            t.Ladder.RemoveAt(0);
                        }
                        continue;
                    }
                    if (schemeOf.ContainsKey(t.ItemMeterKey) || !newLayout) continue;
                    string role = (t.MeterRole ?? "").Trim().ToUpperInvariant();
                    string grp = (m.MergeGroupCodeMeter ?? "").Trim();
                    if (grp.Length > 0 && ((role == "BK" && groupBk.Contains(grp)) || (role == "CL" && groupCl.Contains(grp))))
                        t.FocQty = 0m;                                                             // (2)
                }
        }

        /// <summary>The other book's own strategy rules on one contract, in order.</summary>
        public static List<ScpRemoteRule> StrategyRules(string connectionString, long contractKey)
        {
            List<ScpRemoteRule> list = new List<ScpRemoteRule>();
            if (string.IsNullOrEmpty(connectionString) || contractKey <= 0) return list;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand probe = new SqlCommand(
                    "SELECT CASE WHEN OBJECT_ID('dbo.zSCP2_ContractStrategyRule') IS NULL THEN 0 ELSE 1 END", cn))
                    if (Convert.ToInt32(probe.ExecuteScalar()) == 0) return list;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Seq, ISNULL(RuleKind,'') AS RuleKind, ISNULL(Scope,'') AS Scope, " +
                    "       ISNULL(ServiceItemKeys,'') AS ServiceItemKeys, ISNULL(TargetAmount,0) AS TargetAmount, " +
                    "       ISNULL(PartialPct,0) AS PartialPct, ISNULL(FreeMonths,0) AS FreeMonths, " +
                    "       ISNULL(CommitAmount,0) AS CommitAmount, ISNULL(FocCopies,0) AS FocCopies, " +
                    "       ISNULL(RebatePct,0) AS RebatePct, ISNULL(NetBilling,'N') AS NetBilling, " +
                    "       ISNULL(LimitScope,'S') AS LimitScope, ISNULL(LimitQty,0) AS LimitQty, " +
                    "       ISNULL(Remark,'') AS Remark, ISNULL(SeededFromCode,'') AS SeededFromCode " +
                    "  FROM dbo.zSCP2_ContractStrategyRule WHERE ContractKey = @ck ORDER BY Seq", cn))
                {
                    cmd.Parameters.AddWithValue("@ck", contractKey);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ScpRemoteRule x = new ScpRemoteRule();
                            x.Seq = Convert.ToInt32(r["Seq"]);
                            x.RuleKind = Str(r["RuleKind"]);
                            x.Scope = Str(r["Scope"]);
                            x.ServiceItemKeys = Str(r["ServiceItemKeys"]);
                            x.TargetAmount = Convert.ToDecimal(r["TargetAmount"]);
                            x.PartialPct = Convert.ToDecimal(r["PartialPct"]);
                            x.FreeMonths = Convert.ToInt32(r["FreeMonths"]);
                            x.CommitAmount = Convert.ToDecimal(r["CommitAmount"]);
                            x.FocCopies = Convert.ToDecimal(r["FocCopies"]);
                            x.RebatePct = Convert.ToDecimal(r["RebatePct"]);
                            x.NetBilling = Str(r["NetBilling"]).ToUpperInvariant() == "Y" ? "Y" : "N";
                            x.LimitScope = Str(r["LimitScope"]).ToUpperInvariant() == "G" ? "G" : "S";
                            x.LimitQty = Convert.ToDecimal(r["LimitQty"]);
                            x.Remark = Str(r["Remark"]);
                            x.SeededFromCode = Str(r["SeededFromCode"]);
                            list.Add(x);
                        }
                    }
                }
            }
            return list;
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
