using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>What came of taking a contract from the other book.</summary>
    public class ScpTakeResult
    {
        public long ContractKey;
        public string ContractNo = "";
        public int Machines;
        public int Meters;
        public List<string> MeterTypesCreated = new List<string>();
        public string Error = "";
        public bool Ok { get { return Error.Length == 0 && ContractKey > 0; } }
    }

    /// <summary>
    /// Makes THIS book's contract from one in the other book.
    ///
    /// <para>The word for what this does is not "copy". What comes across is the shape of the deal --
    /// which machines, which counters, which serial numbers -- because those are facts about
    /// equipment and both companies have to agree on them.</para>
    ///
    /// <para>How they bill it comes across too -- one invoice per contract or per machine, how the
    /// lines group, which day of the month. Not because this company must bill the same way, but
    /// because their contract is the only information that exists, and the alternative is the module
    /// making a number up. All of it ordinary settings on an ordinary contract from the moment it
    /// lands.</para>
    ///
    /// <para>Prices come across THROUGH A MARGIN. The two companies charge different customers
    /// different prices, so the other book's rate is never simply this book's rate; but a contract
    /// that arrived with no prices at all needed fifty figures retyped before it could bill anything,
    /// and that is not a product, it is a chore. So the margin does the work: their 1.30 at 30% is
    /// 1.69, and every price that lands is one somebody agreed -- a percentage a person typed, on a
    /// base the other company set. All of it editable afterwards, like any other price.</para>
    ///
    /// <para>The debtor is not copied either, and cannot be: on the other book's contract the
    /// customer IS this company. Choosing the real end customer is the reason taking a contract is a
    /// decision rather than a synchronisation.</para>
    /// </summary>
    public static class ScpInterBillTake
    {
        /// <summary>
        /// The other book's price, plus this book's margin.
        ///
        /// <para>Their 1.30 at 30% is 1.69. The figure is not invented: the percentage was typed by a
        /// person and the base was decided by the other company, so every price that lands is one
        /// somebody agreed to. And it lands as an ORDINARY price on an ordinary contract -- editable
        /// from the moment it arrives, in Meters &amp; Pricing, like any other.</para>
        ///
        /// <para>Rounded to what the column can actually hold: two places for a flat monthly
        /// charge, six for a rate per copy, because that is exactly how wide
        /// <c>zSCP2_ItemMeter.MinimumCharges</c> and <c>ChargesRate</c> are. A rental of 380.00 at
        /// 30% is 494.00, not 494.0000; a black copy at 0.0285 is 0.03705, and rounding a rate to
        /// four places would store 0.0371 -- a sixteenth of a sen per copy, which over 32,852 copies
        /// is RM1.64 of money nobody agreed to.</para>
        ///
        /// <para>Six places also makes a margin of zero an exact copy, which is what a person who
        /// leaves the box at 0 has every right to expect.</para>
        ///
        /// <para>A price of zero over there stays zero here. They have not priced it, so there is
        /// nothing to add a margin to, and inventing one is the thing this module does not do.</para>
        /// </summary>
        public static decimal WithMargin(decimal theirPrice, decimal marginPercent, bool flatCharge)
        {
            if (theirPrice == 0m) return 0m;
            decimal raw = theirPrice * (1m + marginPercent / 100m);
            return Math.Round(raw, flatCharge ? 2 : 6, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// A ladder through the margin: every band keeps its boundary and its price goes up.
        ///
        /// <para>"3000|0.025;99999999|0.02" at 30% is "3000|0.0325;99999999|0.026". The boundaries are
        /// counts of copies, which no margin changes -- only the prices move.</para>
        /// </summary>
        public static string LadderWithMargin(string csv, decimal marginPercent)
        {
            if (string.IsNullOrEmpty(csv) || marginPercent == 0m) return csv ?? "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string part in csv.Split(';'))
            {
                string[] ab = part.Split('|');
                if (ab.Length != 2) continue;
                decimal boundary, price;
                if (!decimal.TryParse(ab[0], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out boundary) ||
                    !decimal.TryParse(ab[1], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out price)) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(boundary.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append('|');
                sb.Append(WithMargin(price, marginPercent, false)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// The prices the other book agreed for whole LINES, brought across through the same margin.
        ///
        /// <para>The grouping arrives with the machines, and a group without its price is worse than
        /// no group at all: seven machines fold onto one line and that line then bills whatever the
        /// first machine's own counter happens to say.</para>
        ///
        /// <para>Only groups that actually landed here are written. A price filed under a group no
        /// machine came in with prices nothing, and would sit in the contract waiting to surprise
        /// somebody who later types that name.</para>
        /// </summary>
        private static int InsertLinePrices(SqlConnection cn, SqlTransaction tx, long ck,
                                            List<ScpRemoteLinePrice> prices,
                                            List<ScpRemoteMachine> machines, decimal marginPercent)
        {
            if (prices == null || prices.Count == 0) return 0;

            Dictionary<string, bool> landed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (machines != null)
                foreach (ScpRemoteMachine m in machines)
                {
                    string a = (m.MergeGroupCode ?? "").Trim();
                    string b = (m.MergeGroupCodeMeter ?? "").Trim();
                    if (a.Length > 0) landed[a] = true;
                    if (b.Length > 0) landed[b] = true;
                }

            int n = 0;
            foreach (ScpRemoteLinePrice p in prices)
            {
                string grp = (p.GroupCode ?? "").Trim();
                if (grp.Length == 0 || !landed.ContainsKey(grp)) continue;
                using (SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ContractRentalPrice " +
                    "                WHERE ContractKey = @ck AND Side = @side AND GroupCode = @grp) " +
                    "INSERT INTO dbo.zSCP2_ContractRentalPrice " +
                    "(ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified) " +
                    "VALUES (@ck, @side, @grp, @up, @bk, @cl, @lbk, @lcl, GETDATE());", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@ck", ck);
                    cmd.Parameters.AddWithValue("@side", (p.Side ?? "R").Trim());
                    cmd.Parameters.AddWithValue("@grp", grp);
                    // A rental line is quoted in ringgit and sen; a copies line is a rate per copy.
                    cmd.Parameters.AddWithValue("@up", WithMargin(p.UnitPrice, marginPercent, true));
                    cmd.Parameters.AddWithValue("@bk", WithMargin(p.BkPrice, marginPercent, false));
                    cmd.Parameters.AddWithValue("@cl", WithMargin(p.ClPrice, marginPercent, false));
                    cmd.Parameters.AddWithValue("@lbk", LadderWithMargin(p.LadderBk, marginPercent));
                    cmd.Parameters.AddWithValue("@lcl", LadderWithMargin(p.LadderCl, marginPercent));
                    if (cmd.ExecuteNonQuery() > 0) n++;
                }
            }
            return n;
        }

        /// <summary>Is this counter a flat monthly amount rather than a rate per copy? Asked with the
        /// engine's own tolerant test, because a quarter of the older book carries no role at all and
        /// is recognised by its item code.</summary>
        private static bool IsFlat(ScpRemoteMeter t)
        {
            return ScpStrategy.IsRentalRole(t.MeterRole, t.MeterTypeCode);
        }

        /// <summary>Their service item number, kept as it is. Falls back to a number built from the
        /// contract only when they have not given the machine one -- a blank would break the unique
        /// key on the second machine, and an unnamed machine is not worth failing a take over.</summary>
        private static string ServiceItemNoFor(ScpRemoteMachine m, string contractNo, int pos)
        {
            string theirs = (m.ServiceItemNo ?? "").Trim();
            if (theirs.Length > 0) return theirs;
            return contractNo + "-" + pos.ToString("000",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Which of the numbers about to be used are already in this book. Empty means the
        /// take can go ahead.</summary>
        public static List<string> NumberClashes(DBSetting localDb, string contractNo,
                                                 List<ScpRemoteMachine> machines)
        {
            List<string> clashes = new List<string>();
            if (localDb == null) return clashes;
            try
            {
                object o = localDb.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_Contract WHERE ContractNo = N'" +
                    (contractNo ?? "").Replace("'", "''") + "'");
                if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0)
                    clashes.Add("   contract " + contractNo);
            }
            catch { }

            if (machines == null) return clashes;
            int pos = 0;
            foreach (ScpRemoteMachine m in machines)
            {
                pos++;
                string sino = ServiceItemNoFor(m, contractNo, pos);
                try
                {
                    object o = localDb.ExecuteScalar(
                        "SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ServiceItemNo = N'" +
                        sino.Replace("'", "''") + "'");
                    if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0)
                        clashes.Add("   service item " + sino);
                }
                catch { }
            }
            return clashes;
        }

        /// <summary>Meter types the other book uses that this book has never heard of. They have to
        /// exist here before a counter can name one -- and the answer is to create them, not to drop
        /// the counter: a counter this book cannot represent is a machine it cannot bill.</summary>
        public static List<string> MissingMeterTypes(DBSetting localDb, List<ScpRemoteMachine> machines)
        {
            List<string> missing = new List<string>();
            if (localDb == null || machines == null) return missing;

            List<string> wanted = new List<string>();
            foreach (ScpRemoteMachine m in machines)
                foreach (ScpRemoteMeter t in m.Meters)
                {
                    string code = (t.MeterTypeCode ?? "").Trim();
                    if (code.Length == 0) continue;
                    bool seen = false;
                    foreach (string w in wanted)
                        if (string.Equals(w, code, StringComparison.OrdinalIgnoreCase)) seen = true;
                    if (!seen) wanted.Add(code);
                }
            if (wanted.Count == 0) return missing;

            foreach (string code in wanted)
            {
                try
                {
                    object o = localDb.ExecuteScalar(
                        "SELECT COUNT(*) FROM dbo.zSCP_MeterType WHERE MeterTypeCode = N'" +
                        code.Replace("'", "''") + "'");
                    if (o == null || o == DBNull.Value || Convert.ToInt32(o) == 0) missing.Add(code);
                }
                catch { }
            }
            return missing;
        }

        /// <summary>
        /// Creates this book's contract from theirs, with its machines, its counters and the links
        /// that remember where they came from. All of it or none of it.
        /// </summary>
        /// <param name="debtorCode">THIS book's customer -- the real end customer, not the company on
        /// the other book's contract.</param>
        public static ScpTakeResult TakeContract(
            DBSetting localDb, ScpInterBillBook book, string remoteConnectionString,
            ScpRemoteContract src, List<ScpRemoteMachine> machines,
            string debtorCode, string contractNo, string description,
            DateTime? startDate, DateTime? expiryDate, string userId, decimal marginPercent)
        {
            ScpTakeResult res = new ScpTakeResult();
            if (localDb == null || book == null || src == null || machines == null)
            { res.Error = "Nothing to take."; return res; }
            if (book.RemoteBookId == Guid.Empty)
            { res.Error = "That book has never been reached. Test the connection first."; return res; }
            if ((debtorCode ?? "").Trim().Length == 0)
            { res.Error = "Choose the customer this contract is billed to."; return res; }

            // Meter types first, and on their own connection: they are reference data of this book,
            // they are worth keeping even if the take is abandoned, and a second attempt then has
            // nothing left to do.
            List<string> missing = MissingMeterTypes(localDb, machines);
            if (missing.Count > 0)
            {
                try { CreateMeterTypes(localDb, remoteConnectionString, missing); res.MeterTypesCreated = missing; }
                catch (Exception ex)
                {
                    res.Error = "Could not create the meter types this contract uses:" +
                                Environment.NewLine + ex.Message;
                    return res;
                }
            }

            // THEIR numbers, kept exactly.
            //
            // A contract taken from the other book is the same deal about the same machines, and the
            // two companies talk to each other about it. Give this book its own SC-000007 and nobody
            // can match a phone call to a machine without opening both systems side by side. The
            // contract number and every service item number come across unchanged.
            //
            // This book's own running number is deliberately NOT drawn: a taken contract is not a new
            // document of this book's, and burning a number for it would leave a hole in the sequence.
            string no = (contractNo ?? "").Trim();
            if (no.Length == 0) no = (src.ContractNo ?? "").Trim();
            if (no.Length == 0)
            {
                try { no = ScpDocNo.Next(localDb, "SC"); }
                catch { no = "IB-" + DateTime.Now.ToString("yyMMddHHmmss"); }
            }

            // Both numbers are unique per book, so a clash has to be found BEFORE anything is written
            // -- a half-made contract that failed on the fortieth machine is worse than a refusal.
            List<string> clashes = NumberClashes(localDb, no, machines);
            if (clashes.Count > 0)
            {
                res.Error = "These numbers are already used in this book:" + Environment.NewLine +
                            string.Join(Environment.NewLine, clashes.ToArray()) + Environment.NewLine +
                            Environment.NewLine +
                            "A contract taken from another book keeps their numbers, so the two " +
                            "companies can talk about the same machine. Remove or renumber what is " +
                            "already here, or take it under a number of your own.";
                return res;
            }
            string who = (userId ?? "").Trim().Length > 0 ? userId.Trim() : "ADMIN";

            // Read across BEFORE the transaction opens -- a network round trip does not belong
            // inside a write that is holding locks on this book.
            List<ScpRemoteLinePrice> linePrices;
            try { linePrices = ScpInterBillReader.LinePrices(remoteConnectionString, src.ContractKey); }
            catch { linePrices = new List<ScpRemoteLinePrice>(); }

            using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("ScpInterBillTake"))
                {
                    try
                    {
                        long ck = InsertContract(cn, tx, no, debtorCode, description,
                                                 startDate, expiryDate, who, src);

                        Link(cn, tx, book, ScpInterBillLink.CONTRACT, ck, ck,
                             src.ContractKey, src.ContractNo, ContractSnapshot(src));

                        int pos = 0;
                        foreach (ScpRemoteMachine m in machines)
                        {
                            pos++;
                            long ik = InsertMachine(cn, tx, ck, ServiceItemNoFor(m, no, pos), m, pos,
                                                    startDate, expiryDate);
                            res.Machines++;

                            Link(cn, tx, book, ScpInterBillLink.ITEM, ik, ck,
                                 m.ItemKey, m.ServiceItemNo, m.Snapshot());

                            foreach (ScpRemoteMeter t in m.Meters)
                            {
                                long imk = InsertMeter(cn, tx, ik, t, marginPercent);
                                res.Meters++;
                                Link(cn, tx, book, ScpInterBillLink.METER, imk, ck,
                                     t.ItemMeterKey, t.MeterTypeCode, t.Snapshot());
                            }
                        }

                        InsertLinePrices(cn, tx, ck, linePrices, machines, marginPercent);

                        // This book bills the contract from the month it was taken. Stored, because the
                        // link's TakenAt moves whenever a change from the other book is accepted.
                        using (SqlCommand bf = new SqlCommand(
                            "UPDATE dbo.zSCP2_Contract SET BillFromPeriod = @p WHERE ContractKey = @k", cn, tx))
                        {
                            bf.Parameters.AddWithValue("@p", DateTime.Today.Year * 100 + DateTime.Today.Month);
                            bf.Parameters.AddWithValue("@k", ck);
                            bf.ExecuteNonQuery();
                        }

                        // ATP-10: whether the rental is billed a month ahead. The other book keeps it on
                        // the contract AND on each machine (its contract screen keeps them in step); the
                        // machines are what were copied, so the contract follows them.
                        using (SqlCommand rb = new SqlCommand(
                            "UPDATE c SET c.RentalBasis = 'P' FROM dbo.zSCP2_Contract c WHERE c.ContractKey = @k " +
                            "AND EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                            "            WHERE i.ContractKey = c.ContractKey AND ISNULL(m.RentalBasis,'A') = 'P')", cn, tx))
                        {
                            rb.Parameters.AddWithValue("@k", ck);
                            rb.ExecuteNonQuery();
                        }

                        tx.Commit();
                        res.ContractKey = ck;
                        res.ContractNo = no;
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        res.ContractKey = 0;
                        res.Error = ex.Message;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Adds ONE more of the other book's machines to a contract already taken -- the answer to
        /// "they put another machine on it".
        ///
        /// <para>Same terms as the original take: the machine and its counters come across, no price
        /// does. It lands at the end of the machine list, and it is linked, so from the next look
        /// onwards it is not news any more.</para>
        /// </summary>
        public static ScpTakeResult AddMachine(DBSetting localDb, ScpInterBillBook book,
                                               string remoteConnectionString,
                                               long localContractKey, ScpRemoteMachine m,
                                               decimal marginPercent)
        {
            ScpTakeResult res = new ScpTakeResult();
            if (localDb == null || book == null || m == null || localContractKey <= 0)
            { res.Error = "Nothing to add."; return res; }

            List<ScpRemoteMachine> one = new List<ScpRemoteMachine>();
            one.Add(m);
            List<string> missing = MissingMeterTypes(localDb, one);
            if (missing.Count > 0)
            {
                try { CreateMeterTypes(localDb, remoteConnectionString, missing); res.MeterTypesCreated = missing; }
                catch (Exception ex) { res.Error = ex.Message; return res; }
            }

            string no = "";
            int pos = 1;
            DateTime? start = null;
            DateTime? expiry = null;
            try
            {
                System.Data.DataTable t = localDb.GetDataTable(
                    "SELECT c.ContractNo, c.ServiceStartDate, c.ServiceExpiryDate, " +
                    " (SELECT ISNULL(MAX(Pos),0) FROM dbo.zSCP2_Item WHERE ContractKey = c.ContractKey) AS MaxPos " +
                    "FROM dbo.zSCP2_Contract c WHERE c.ContractKey = " + localContractKey, false);
                if (t.Rows.Count > 0)
                {
                    no = Convert.ToString(t.Rows[0]["ContractNo"]).Trim();
                    pos = Convert.ToInt32(t.Rows[0]["MaxPos"]) + 1;
                    if (t.Rows[0]["ServiceStartDate"] != DBNull.Value)
                        start = Convert.ToDateTime(t.Rows[0]["ServiceStartDate"]);
                    if (t.Rows[0]["ServiceExpiryDate"] != DBNull.Value)
                        expiry = Convert.ToDateTime(t.Rows[0]["ServiceExpiryDate"]);
                }
            }
            catch { }

            // A machine can arrive naming a group this book has never seen -- they put it on a new
            // line. The group would land with no price behind it and bill off the machine's own
            // counter, so their price for that line comes too. Groups already here keep the figure
            // they have: InsertLinePrices only writes what is missing.
            List<ScpRemoteLinePrice> linePrices = new List<ScpRemoteLinePrice>();
            try
            {
                System.Data.DataTable sk = localDb.GetDataTable(
                    "SELECT TOP 1 SourceKey FROM dbo.zSCP2_InterBillLink " +
                    " WHERE EntityType = 'CONTRACT' AND LocalKey = " + localContractKey, false);
                if (sk.Rows.Count > 0)
                    linePrices = ScpInterBillReader.LinePrices(remoteConnectionString,
                                                              Convert.ToInt64(sk.Rows[0]["SourceKey"]));
            }
            catch { }

            using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("ScpInterBillAddMachine"))
                {
                    try
                    {
                        long ik = InsertMachine(cn, tx, localContractKey, ServiceItemNoFor(m, no, pos),
                                                m, pos, start, expiry);
                        Link(cn, tx, book, ScpInterBillLink.ITEM, ik, localContractKey,
                             m.ItemKey, m.ServiceItemNo, m.Snapshot());
                        res.Machines = 1;

                        foreach (ScpRemoteMeter t in m.Meters)
                        {
                            long imk = InsertMeter(cn, tx, ik, t, marginPercent);
                            res.Meters++;
                            Link(cn, tx, book, ScpInterBillLink.METER, imk, localContractKey,
                                 t.ItemMeterKey, t.MeterTypeCode, t.Snapshot());
                        }

                        InsertLinePrices(cn, tx, localContractKey, linePrices, one, marginPercent);

                        tx.Commit();
                        res.ContractKey = localContractKey;
                        res.ContractNo = no;
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        res.Error = ex.Message;
                    }
                }
            }
            return res;
        }

        /// <summary>Adds ONE more of the other book's counters to a machine already taken.</summary>
        public static ScpTakeResult AddMeter(DBSetting localDb, ScpInterBillBook book,
                                             string remoteConnectionString,
                                             long localContractKey, long localItemKey, ScpRemoteMeter t,
                                             decimal marginPercent)
        {
            ScpTakeResult res = new ScpTakeResult();
            if (localDb == null || book == null || t == null || localItemKey <= 0)
            { res.Error = "Nothing to add."; return res; }

            ScpRemoteMachine holder = new ScpRemoteMachine();
            holder.Meters.Add(t);
            List<ScpRemoteMachine> one = new List<ScpRemoteMachine>();
            one.Add(holder);
            List<string> missing = MissingMeterTypes(localDb, one);
            if (missing.Count > 0)
            {
                try { CreateMeterTypes(localDb, remoteConnectionString, missing); res.MeterTypesCreated = missing; }
                catch (Exception ex) { res.Error = ex.Message; return res; }
            }

            using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("ScpInterBillAddMeter"))
                {
                    try
                    {
                        long imk = InsertMeter(cn, tx, localItemKey, t, marginPercent);
                        Link(cn, tx, book, ScpInterBillLink.METER, imk, localContractKey,
                             t.ItemMeterKey, t.MeterTypeCode, t.Snapshot());
                        tx.Commit();
                        res.ContractKey = localContractKey;
                        res.Meters = 1;
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        res.Error = ex.Message;
                    }
                }
            }
            return res;
        }

        /// <summary>What a contract looked like over there when it was taken. Only what belongs to
        /// them: the machines and the dates. Their debtor is in it too, not because this book cares
        /// who it is, but because a changed debtor over there usually means the deal itself
        /// moved.</summary>
        public static string ContractSnapshot(ScpRemoteContract c)
        {
            List<KeyValuePair<string, string>> v = new List<KeyValuePair<string, string>>();
            v.Add(new KeyValuePair<string, string>("NO", c.ContractNo));
            v.Add(new KeyValuePair<string, string>("DEBTOR", c.DebtorCode));
            v.Add(new KeyValuePair<string, string>("MACHINES", c.MachineCount.ToString()));
            v.Add(new KeyValuePair<string, string>("EXPIRY",
                c.ExpiryDate.HasValue ? c.ExpiryDate.Value.ToString("yyyy-MM-dd") : ""));
            v.Add(new KeyValuePair<string, string>("ACTIVE", c.Inactive ? "N" : "Y"));
            return ScpInterBillSnapshot.Format(v);
        }

        private static void CreateMeterTypes(DBSetting localDb, string remoteConnectionString, List<string> codes)
        {
            // The type's NAME and what it means come across; its rates do not. A meter type carries a
            // default charge, and a default charge copied from the other company is exactly the kind
            // of price nobody in this book ever agreed.
            Dictionary<string, string[]> defs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection rc = new SqlConnection(remoteConnectionString))
            {
                rc.Open();
                foreach (string code in codes)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 ISNULL([Description],''), ISNULL(ACItemCode,''), " +
                        "       ISNULL(IsFlatCharge,'N'), ISNULL(IsRentalWaive,'N'), ISNULL(DefaultRole,'') " +
                        "  FROM dbo.zSCP_MeterType WHERE MeterTypeCode = @c", rc))
                    {
                        cmd.Parameters.AddWithValue("@c", code);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                defs[code] = new string[] { r.GetString(0), r.GetString(1),
                                                            r.GetString(2), r.GetString(3), r.GetString(4) };
                        }
                    }
                }
            }

            using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
            {
                cn.Open();
                foreach (string code in codes)
                {
                    string[] d = defs.ContainsKey(code)
                        ? defs[code]
                        : new string[] { code, "", "N", "N", "" };
                    using (SqlCommand cmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP_MeterType WHERE MeterTypeCode=@c) " +
                        "INSERT INTO dbo.zSCP_MeterType " +
                        " (MeterTypeCode, [Description], ACItemCode, IsFlatCharge, IsRentalWaive, " +
                        "  DefaultRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                        "  RebateQtyInPercent, FOCQty, Inactive, LastModified) " +
                        "VALUES (@c, @d, @ac, @flat, @waive, @role, 0, 0, '', 0, 0, 'N', GETDATE())", cn))
                    {
                        cmd.Parameters.AddWithValue("@c", code);
                        cmd.Parameters.AddWithValue("@d", d[0].Length > 0 ? d[0] : code);
                        cmd.Parameters.AddWithValue("@ac", d[1]);
                        cmd.Parameters.AddWithValue("@flat", d[2]);
                        cmd.Parameters.AddWithValue("@waive", d[3]);
                        cmd.Parameters.AddWithValue("@role", d[4]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static long InsertContract(SqlConnection cn, SqlTransaction tx, string no,
                                           string debtorCode, string description,
                                           DateTime? start, DateTime? expiry, string who,
                                           ScpRemoteContract src)
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_Contract " +
                "(ContractNo, DebtorCode, [Description], BillingFormatCode, UseNewLayout, " +
                " MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, " +
                " ContractDate, ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, " +
                " CreatedBy, ModifiedBy, Created, Modified, LastModified) " +
                // How they bill it, as the starting point.
                //
                // This was once hardcoded -- one invoice per contract, billing day 1 -- and that was
                // the module inventing two answers nobody had given it. Their contract is the only
                // information that exists about how this deal is billed, so it is what arrives, the
                // same as the machines and the opening readings. It is an ordinary setting on an
                // ordinary contract from that moment, and how many invoices this company sends its
                // own customer is nobody's business but its own.
                //
                // The named Billing Format is NOT taken: a format is a name in their book. What a
                // format SETS comes across instead, so the shape matches without borrowing the name.
                "VALUES (@no, @deb, @desc, '', 'Y', @shows, @rlm, @mlm, @bm, @bday, GETDATE(), @sd, @ed, @rsep, 'N', " +
                " @who, @who, GETDATE(), GETDATE(), GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@no", no);
                cmd.Parameters.AddWithValue("@deb", debtorCode.Trim());
                cmd.Parameters.AddWithValue("@desc", (description ?? "").Trim());
                cmd.Parameters.AddWithValue("@sd", start.HasValue ? (object)start.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ed", expiry.HasValue ? (object)expiry.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@who", who);
                cmd.Parameters.AddWithValue("@bm", src == null ? "G" : src.BillingMode);
                cmd.Parameters.AddWithValue("@rlm", src == null ? "S" : src.RentalLineMode);
                cmd.Parameters.AddWithValue("@mlm", src == null ? "S" : src.MeterLineMode);
                cmd.Parameters.AddWithValue("@rsep", src == null ? "N" : src.RentalSeparateInvoice);
                cmd.Parameters.AddWithValue("@shows", src == null ? "L" : src.MachineLineShows);
                cmd.Parameters.AddWithValue("@bday", src == null ? 1 : src.BillingDay);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private static long InsertMachine(SqlConnection cn, SqlTransaction tx, long ck, string sino,
                                          ScpRemoteMachine m, int pos, DateTime? start, DateTime? expiry)
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_Item " +
                "(ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, " +
                // The grouping comes across.
                //
                // It used to be left blank, on the argument that how an invoice is split and folded
                // is an arrangement with THIS book's own customer. True, and it is still this
                // book's to change -- but blank is not neutral, it is "every machine on its own
                // line". Seven machines the other book prints as three lines arrived as twenty-one,
                // and the only way back was to re-type a layout somebody had already typed once.
                //
                // So it lands the same way the prices do: their arrangement as the starting point,
                // ordinary and editable from the moment it arrives.
                " MergeGroupCode, MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, " +
                " IsGroupItem, MachineMode, ServiceStartDate, ServiceExpiryDate, LastModified) " +
                "VALUES (@ck, @sino, @code, @sn, @desc, @pos, @mg, @mgm, @bg, @lg, @inact, 'N', 'ONLINE', " +
                " @sd, @ed, GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", ck);
                cmd.Parameters.AddWithValue("@sino", sino);
                cmd.Parameters.AddWithValue("@code", m.ItemCode);
                cmd.Parameters.AddWithValue("@sn", m.SerialNumber);
                cmd.Parameters.AddWithValue("@desc", m.Description.Length > 0
                    ? m.Description
                    : (m.ItemCode + " / " + m.SerialNumber).Trim());
                cmd.Parameters.AddWithValue("@pos", pos);
                cmd.Parameters.AddWithValue("@inact", m.Inactive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@mg", (m.MergeGroupCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@mgm", (m.MergeGroupCodeMeter ?? "").Trim());
                cmd.Parameters.AddWithValue("@bg", (m.BillGroupCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@lg", (m.LineGroupCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@sd", start.HasValue ? (object)start.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ed", expiry.HasValue ? (object)expiry.Value : DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private static long InsertMeter(SqlConnection cn, SqlTransaction tx, long ik, ScpRemoteMeter t,
                                        decimal marginPercent)
        {
            bool flat = IsFlat(t);
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO dbo.zSCP2_ItemMeter " +
                "(ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, " +
                // Their price plus this book's margin -- see WithMargin. It arrives as an ORDINARY
                // price: nothing downstream knows or cares that it was derived, and Meters & Pricing
                // edits it like any other. What must never happen is the system choosing a figure of
                // its own; a percentage somebody typed, applied to a price the other company set, is
                // not that.
                " MinimumCharges, ChargesRate, MeterMultiPriceCode, RebateQtyInPercent, FOCQty, " +
                // The opening reading DOES come across: it is a fact about the machine, and without
                // it the first month bills the meter's whole life.
                " InitialReading, RentalMonths, RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, " +
                " WaivePartialPct, WaiveScope, WaivePartialThreshold, WaivePartialAmount, " +
                " CommitScope, LastModified) " +
                "VALUES (@ik, @type, @desc, @role, @msn, @min, @rate, '', 0, 0, @init, 0, @rb, 0, 0, 100, 'S', " +
                " 0, 0, 'S', GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@ik", ik);
                cmd.Parameters.AddWithValue("@rb", t.RentalBasis == "P" ? "P" : "A");   // ATP-10
                cmd.Parameters.AddWithValue("@type", t.MeterTypeCode);
                cmd.Parameters.AddWithValue("@desc", t.Description);
                cmd.Parameters.AddWithValue("@role", t.MeterRole);
                cmd.Parameters.AddWithValue("@msn", t.MachineSerialNo);
                cmd.Parameters.AddWithValue("@init", t.InitialReading);
                cmd.Parameters.AddWithValue("@rate", WithMargin(t.ChargesRate, marginPercent, flat));
                // A minimum is a flat sum of money whatever counter it sits on, so it rounds like one.
                cmd.Parameters.AddWithValue("@min", WithMargin(t.MinimumCharges, marginPercent, true));
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private static void Link(SqlConnection cn, SqlTransaction tx, ScpInterBillBook book,
                                 string entityType, long localKey, long rootContractKey,
                                 long sourceKey, string sourceRef, string snapshot)
        {
            ScpInterBillLink l = new ScpInterBillLink();
            l.EntityType = entityType;
            l.LocalKey = localKey;
            l.RootContractKey = rootContractKey;
            l.SourceBookId = book.RemoteBookId;
            l.SourceKey = sourceKey;
            l.SourceRef = sourceRef ?? "";
            l.TakenAt = DateTime.Now;
            l.TakenSnapshot = snapshot ?? "";
            l.Status = ScpInterBillLink.LIVE;
            ScpInterBillLinks.Save(cn, tx, l);
        }
    }
}
