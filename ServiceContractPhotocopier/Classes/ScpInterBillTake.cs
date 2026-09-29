using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
        /// <summary>What came across only in part, in words -- a strategy rule whose machines were not
        /// taken. Empty when everything came.</summary>
        public List<string> Notes = new List<string>();
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
                    "(ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, " +
                    " MinCharge, WaiveAt, WaiveAmt, LastModified) " +
                    "VALUES (@ck, @side, @grp, @up, @bk, @cl, @lbk, @lcl, @min, @wat, @wamt, GETDATE());", cn, tx))
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
                    // The line's minimum and its waive are sums of money like the rent, so they go
                    // through the margin the same way -- the same copies reach the same waive here.
                    cmd.Parameters.AddWithValue("@min", WithMargin(p.MinCharge, marginPercent, true));
                    cmd.Parameters.AddWithValue("@wat", WithMargin(p.WaiveAt, marginPercent, true));
                    cmd.Parameters.AddWithValue("@wamt", WithMargin(p.WaiveAmt, marginPercent, true));
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
            // A waive gives a rent back, so it rounds like the rent: at 6 places a 100.15 waive at 10%
            // left 110.165, the engine rounded it to 110.16 against a 110.17 rent, and a fully waived
            // month billed 0.01.
            return ScpStrategy.IsRentalRole(t.MeterRole, t.MeterTypeCode) || t.TypeFlatOrWaive
                || (t.MeterRole ?? "").Trim().ToUpperInvariant() == "WAIVE";
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
            res.Error = ItemCodeRefusal(localDb, machines, missing);
            if (res.Error.Length > 0) return res;

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
            //
            // A failed read refuses the take. It used to be swallowed, and the contract then arrived
            // without its line prices -- billing, but not the deal HQ agreed.
            List<ScpRemoteLinePrice> linePrices;
            List<ScpRemoteRule> rules;
            try
            {
                linePrices = ScpInterBillReader.LinePrices(remoteConnectionString, src.ContractKey);
                rules = ScpInterBillReader.StrategyRules(remoteConnectionString, src.ContractKey);
            }
            catch (Exception ex)
            {
                res.Error = "Could not read the contract's line prices and rules from " + book.Alias + ":" +
                            Environment.NewLine + ex.Message;
                return res;
            }

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
                        Dictionary<long, long> itemMap = new Dictionary<long, long>();
                        foreach (ScpRemoteMachine m in machines)
                        {
                            pos++;
                            long ik = InsertMachine(cn, tx, ck, ServiceItemNoFor(m, no, pos), m, pos,
                                                    startDate, expiryDate);
                            itemMap[m.ItemKey] = ik;
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
                        InsertRules(cn, tx, ck, rules, itemMap, marginPercent, res.Notes);

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
            res.Error = ItemCodeRefusal(localDb, one, missing);
            if (res.Error.Length > 0) return res;

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
            res.Error = ItemCodeRefusal(localDb, one, missing, false);
            if (res.Error.Length > 0) return res;

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
            //
            // Its item code comes across only when it is an item of THIS book too (their AC item code,
            // else their stock code -- the order their invoice line uses). A code this book does not
            // have would fail the invoice; left empty, the take refuses until somebody sets one here
            // (ItemCodeRefusal).
            Dictionary<string, string[]> defs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection rc = new SqlConnection(remoteConnectionString))
            {
                rc.Open();
                foreach (string code in codes)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 ISNULL([Description],''), ISNULL(NULLIF(ACItemCode,''), ISNULL(StockCode,'')), " +
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
                    if (d[1].Length > 0)
                    {
                        using (SqlCommand ic = new SqlCommand("SELECT COUNT(*) FROM dbo.Item WHERE ItemCode = @i", cn))
                        {
                            ic.Parameters.AddWithValue("@i", d[1]);
                            if (Convert.ToInt32(ic.ExecuteScalar()) == 0) d[1] = "";
                        }
                    }
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
                " FOCResetUnit, FOCResetN, BillOnMonthEnd, RentalBillingDay, PeriodFollowContract, StrategyCode, " +
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
                " @focu, @focn, @eom, @rday, @pfc, @strat, " +
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
                // When their free copies start again, month-end billing, the rental's own day, and the
                // period following the start (29/9: everything of the deal comes across).
                cmd.Parameters.AddWithValue("@focu", src == null ? "M" : src.FocResetUnit);
                cmd.Parameters.AddWithValue("@focn", src == null ? 0 : src.FocResetN);
                cmd.Parameters.AddWithValue("@eom", src == null ? "N" : src.BillOnMonthEnd);
                cmd.Parameters.AddWithValue("@rday", src == null ? 0 : src.RentalBillingDay);
                cmd.Parameters.AddWithValue("@pfc", src == null ? "N" : src.PeriodFollowContract);
                cmd.Parameters.AddWithValue("@strat", src == null ? "" : src.StrategyCode);
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
                " IsGroupItem, MachineMode, ServiceStartDate, ServiceExpiryDate, BillingDayOverride, LastModified) " +
                // Its own billing day, its own dates, how its readings arrive and whether it is the
                // group machine come as they are over there (29/9) -- they were written as the
                // contract's dates, 'ONLINE' and not a group machine, whatever HQ had.
                "VALUES (@ck, @sino, @code, @sn, @desc, @pos, @mg, @mgm, @bg, @lg, @inact, @grp, @mode, " +
                " @sd, @ed, @bday, GETDATE()); " +
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
                DateTime? sd = m.StartDate.HasValue ? m.StartDate : start;
                DateTime? ed = m.ExpiryDate.HasValue ? m.ExpiryDate : expiry;
                cmd.Parameters.AddWithValue("@sd", sd.HasValue ? (object)sd.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ed", ed.HasValue ? (object)ed.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@grp", m.IsGroupItem ? "Y" : "N");
                // An explicit ONLINE / OFFLINE comes as it is. Blank (follow the fetch status) has no
                // fetch status here -- the readings come from HQ -- so it stays ONLINE, what every take
                // has written, and the invoice numbering of taken contracts does not change.
                cmd.Parameters.AddWithValue("@mode", m.MachineMode.Length > 0 ? m.MachineMode : "ONLINE");
                // No day of its own is NULL, not 0: billing reads COALESCE(own day, contract's day).
                cmd.Parameters.AddWithValue("@bday", m.BillingDayOverride > 0 ? (object)m.BillingDayOverride : DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private static long InsertMeter(SqlConnection cn, SqlTransaction tx, long ik, ScpRemoteMeter t,
                                        decimal marginPercent)
        {
            long imk;
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
                " InitialReading, RentalStartDate, RentalMonths, RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, " +
                " WaivePartialPct, WaiveScope, WaivePartialThreshold, WaivePartialAmount, " +
                " CommitScope, TierMode, LastModified) " +
                // The rest of the deal comes too (29/9, user: "必须带过来不要懒惰"). It used to be written
                // as zeros -- free copies 0, the waive's conditions 0 & 0, which the engine reads as
                // "waive every month", and no ladder -- so the same machine billed differently here.
                // The ladder travels as this counter's own (zSCP2_ItemMeterPrice), not by scheme name:
                // a scheme is a name in their book and the same name here could price something else.
                "VALUES (@ik, @type, @desc, @role, @msn, @min, @rate, '', @rebate, @foc, @init, @rstart, @rmonths, @rb, " +
                " @wn, @wt, @wpct, @wscope, @wthr, @wamt, @cscope, @tier, GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@ik", ik);
                cmd.Parameters.AddWithValue("@rb", t.RentalBasis == "P" ? "P" : "A");   // ATP-10
                cmd.Parameters.AddWithValue("@type", t.MeterTypeCode);
                cmd.Parameters.AddWithValue("@desc", t.Description);
                cmd.Parameters.AddWithValue("@role", t.MeterRole);
                cmd.Parameters.AddWithValue("@msn", t.MachineSerialNo);
                cmd.Parameters.AddWithValue("@init", t.InitialReading);
                BindTerms(cmd, t, marginPercent, true);
                imk = Convert.ToInt64(cmd.ExecuteScalar());
            }
            WriteLadder(cn, tx, imk, t, marginPercent);
            return imk;
        }

        /// <summary>A counter's terms as this book keeps them: counts and percentages as they are, sums
        /// of money through the margin -- a waive's target and its partial band are measured against
        /// this book's charges, which carry the margin, so the same copies reach the same waive.
        /// <paramref name="prices"/> adds the rate and the minimum.</summary>
        private static void BindTerms(SqlCommand cmd, ScpRemoteMeter t, decimal marginPercent, bool prices)
        {
            if (prices)
            {
                cmd.Parameters.AddWithValue("@rate", WithMargin(t.ChargesRate, marginPercent, IsFlat(t)));
                // A minimum is a flat sum of money whatever counter it sits on, so it rounds like one.
                cmd.Parameters.AddWithValue("@min", WithMargin(t.MinimumCharges, marginPercent, true));
            }
            cmd.Parameters.AddWithValue("@rebate", t.RebatePct);
            cmd.Parameters.AddWithValue("@foc", t.FocQty);
            cmd.Parameters.AddWithValue("@rstart", t.RentalStartDate.HasValue ? (object)t.RentalStartDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@rmonths", t.RentalMonths);
            cmd.Parameters.AddWithValue("@wn", t.WaiveFirstNMonths);
            cmd.Parameters.AddWithValue("@wt", WithMargin(t.WaiveTargetAmount, marginPercent, true));
            cmd.Parameters.AddWithValue("@wpct", t.WaivePartialPct);
            cmd.Parameters.AddWithValue("@wscope", t.WaiveScope.Length > 0 ? t.WaiveScope : "BKCL");
            cmd.Parameters.AddWithValue("@wthr", WithMargin(t.WaivePartialThreshold, marginPercent, true));
            cmd.Parameters.AddWithValue("@wamt", WithMargin(t.WaivePartialAmount, marginPercent, true));
            cmd.Parameters.AddWithValue("@cscope", t.CommitScope.Length > 0 ? t.CommitScope : "S");
            cmd.Parameters.AddWithValue("@tier", t.TierMode == "I" ? "I" : "T");
        }

        /// <summary>Their ladder as this counter's own, every band's price through the margin; the
        /// boundaries are counts of copies, which no margin changes.</summary>
        private static void WriteLadder(SqlConnection cn, SqlTransaction tx, long imk, ScpRemoteMeter t, decimal marginPercent)
        {
            if (t.Ladder.Count == 0)
            {
                // HQ prices it at one rate. A counter with no ladder of its own falls back to its
                // meter type's default tiers -- this book's -- which would price it by a deal HQ never
                // made. One band at HQ's rate keeps it at that rate, and only when this book's type
                // has such tiers.
                if (IsFlat(t) || t.ChargesRate <= 0m) return;
                using (SqlCommand q = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.zSCP_MeterType mt JOIN dbo.zSCP_MeterMultiPriceItem i " +
                    "  ON i.MeterMultiPriceCode = mt.MeterMultiPriceCode WHERE mt.MeterTypeCode = @c " +
                    "   AND ISNULL(mt.MeterMultiPriceCode,'') <> ''", cn, tx))
                {
                    q.Parameters.AddWithValue("@c", t.MeterTypeCode);
                    if (Convert.ToInt32(q.ExecuteScalar()) == 0) return;
                }
                using (SqlCommand one = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_ItemMeterPrice (ItemMeterKey, MeterReading, UnitPrice) VALUES (@k, 99999999, @p)", cn, tx))
                {
                    one.Parameters.AddWithValue("@k", imk);
                    one.Parameters.AddWithValue("@p", WithMargin(t.ChargesRate, marginPercent, false));
                    one.ExecuteNonQuery();
                }
                return;
            }
            foreach (decimal[] b in t.Ladder)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_ItemMeterPrice (ItemMeterKey, MeterReading, UnitPrice) VALUES (@k, @q, @p)", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@k", imk);
                    cmd.Parameters.AddWithValue("@q", b[0]);
                    cmd.Parameters.AddWithValue("@p", WithMargin(b[1], marginPercent, false));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Brings HQ's terms for one counter over a counter already taken -- the answer to "they
        /// changed this" on the board, or to a contract taken before the terms came across.
        /// <para>With <paramref name="prices"/> the rate, the minimum and the ladder are replaced too
        /// (HQ's, through the margin). Without it only what an older take left out is brought: the
        /// terms, and the ladder when the counter has none of its own.</para>
        /// Returns an error, or "".
        /// </summary>
        public static string ApplyMeterTerms(DBSetting localDb, long localMeterKey, ScpRemoteMeter t,
                                             decimal marginPercent, bool prices)
        {
            if (localDb == null || localMeterKey <= 0 || t == null) return "Nothing to bring over.";
            using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("ScpInterBillTerms"))
                {
                    try
                    {
                        bool hasLadder;
                        using (SqlCommand q = new SqlCommand(
                            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeterPrice WHERE ItemMeterKey = @k) " +
                            "  OR EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter WHERE ItemMeterKey = @k AND ISNULL(MeterMultiPriceCode,'') <> '') " +
                            "THEN 1 ELSE 0 END", cn, tx))
                        {
                            q.Parameters.AddWithValue("@k", localMeterKey);
                            hasLadder = Convert.ToInt32(q.ExecuteScalar()) == 1;
                        }
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.zSCP2_ItemMeter SET MachineSerialNo=@msn, RebateQtyInPercent=@rebate, FOCQty=@foc, RentalStartDate=@rstart, " +
                            " RentalMonths=@rmonths, WaiveFirstNMonths=@wn, WaiveTargetAmount=@wt, WaivePartialPct=@wpct, " +
                            " WaiveScope=@wscope, WaivePartialThreshold=@wthr, WaivePartialAmount=@wamt, CommitScope=@cscope, " +
                            " TierMode=@tier, " +
                            (prices ? "ChargesRate=@rate, MinimumCharges=@min, MeterMultiPriceCode='', " : "") +
                            " LastModified=GETDATE() WHERE ItemMeterKey=@k", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@k", localMeterKey);
                            cmd.Parameters.AddWithValue("@msn", t.MachineSerialNo ?? "");
                            BindTerms(cmd, t, marginPercent, prices);
                            cmd.ExecuteNonQuery();
                        }
                        if (prices || !hasLadder)
                        {
                            using (SqlCommand del = new SqlCommand("DELETE FROM dbo.zSCP2_ItemMeterPrice WHERE ItemMeterKey = @k", cn, tx))
                            {
                                del.Parameters.AddWithValue("@k", localMeterKey);
                                del.ExecuteNonQuery();
                            }
                            WriteLadder(cn, tx, localMeterKey, t, marginPercent);
                        }
                        tx.Commit();
                        return "";
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        return ex.Message;
                    }
                }
            }
        }

        /// <summary>
        /// Their strategy rules (the contract's Strategy tab) as this contract's own. A rule bound to
        /// certain machines is bound to the same machines here; one whose machines were not taken is
        /// left out and named in <paramref name="notes"/> -- written with no machines it would apply to
        /// the whole contract. Money through the margin; copies, months and percentages as they are.
        /// </summary>
        private static void InsertRules(SqlConnection cn, SqlTransaction tx, long ck, List<ScpRemoteRule> rules,
                                        Dictionary<long, long> itemMap, decimal marginPercent, List<string> notes)
        {
            if (rules == null) return;
            foreach (ScpRemoteRule x in rules)
            {
                List<long> theirs = new List<long>();
                ScpStrategy.ParseItemKeys(x.ServiceItemKeys, theirs);
                List<long> mine = new List<long>();
                foreach (long k in theirs)
                {
                    long lk;
                    if (itemMap.TryGetValue(k, out lk)) mine.Add(lk);
                }
                if (theirs.Count > 0 && mine.Count == 0)
                {
                    notes.Add("Strategy rule " + x.Seq.ToString(CultureInfo.InvariantCulture) + " (" + x.RuleKind +
                              ") was left out: none of its machines were taken.");
                    continue;
                }
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_ContractStrategyRule (ContractKey, ServiceItemKeys, Seq, RuleKind, Scope, " +
                    " TargetAmount, PartialPct, FreeMonths, CommitAmount, FocCopies, RebatePct, NetBilling, LimitScope, " +
                    " LimitQty, Remark, SeededFromCode, LastModified) " +
                    "VALUES (@ck, @keys, @seq, @kind, @scope, @target, @pct, @free, @commit, @foc, @rebate, @net, @ls, " +
                    " @lq, @remark, @seed, GETDATE())", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@ck", ck);
                    cmd.Parameters.AddWithValue("@keys", ScpStrategy.JoinItemKeys(mine));
                    cmd.Parameters.AddWithValue("@seq", x.Seq);
                    cmd.Parameters.AddWithValue("@kind", x.RuleKind);
                    cmd.Parameters.AddWithValue("@scope", x.Scope);
                    cmd.Parameters.AddWithValue("@target", WithMargin(x.TargetAmount, marginPercent, true));
                    cmd.Parameters.AddWithValue("@pct", x.PartialPct);
                    cmd.Parameters.AddWithValue("@free", x.FreeMonths);
                    cmd.Parameters.AddWithValue("@commit", WithMargin(x.CommitAmount, marginPercent, true));
                    cmd.Parameters.AddWithValue("@foc", x.FocCopies);
                    cmd.Parameters.AddWithValue("@rebate", x.RebatePct);
                    cmd.Parameters.AddWithValue("@net", x.NetBilling);
                    cmd.Parameters.AddWithValue("@ls", x.LimitScope);
                    cmd.Parameters.AddWithValue("@lq", x.LimitQty);
                    cmd.Parameters.AddWithValue("@remark", x.Remark);
                    cmd.Parameters.AddWithValue("@seed", x.SeededFromCode);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Every machine and every counter has to be an item of THIS book (user, 29/9: "没有 itemcode 不
        /// allow take contract ... 我讲的是 machine, meter 也是可以 check"):
        /// <list type="bullet">
        /// <item>the machine's model (its item code) must be a stock item here -- the machine is on this
        /// book's contract, its delivery order and its reports by item;</item>
        /// <item>each counter's meter type must name an item here -- an invoice line with an item this
        /// book does not have fails when the invoice is saved, and one with no item posts to the default
        /// sales account, missing from every report by item. A counter never billed (role NA) needs none.</item>
        /// </list>
        /// The take is refused, naming each, before anything is written. <paramref name="checkMachines"/>
        /// is false for a counter added on its own (its machine is already here). Returns the refusal, or "".
        /// </summary>
        public static string ItemCodeRefusal(DBSetting localDb, List<ScpRemoteMachine> machines, List<string> justCreated,
                                             bool checkMachines = true)
        {
            List<string> problems = ItemCodeProblems(localDb, machines, checkMachines);
            if (problems.Count == 0) return "";
            bool models = false, types = false;
            foreach (string p in problems) { if (p.StartsWith("   machine")) models = true; else types = true; }
            string made = types && justCreated != null && justCreated.Count > 0
                ? "(" + string.Join(", ", justCreated.ToArray()) +
                  (justCreated.Count == 1 ? " was" : " were") + " not in this book and " +
                  (justCreated.Count == 1 ? "has" : "have") + " just been added to Meter Type Maintenance.)"
                : "";
            List<string> todo = new List<string>();
            if (models) todo.Add("add each machine model as a stock item of this book");
            if (types) todo.Add("give each meter type an item code of this book in Meter Type Maintenance");
            return "This contract cannot be taken yet. These have no item code in this book:" +
                   Environment.NewLine + Environment.NewLine +
                   string.Join(Environment.NewLine, problems.ToArray()) + Environment.NewLine +
                   (made.Length > 0 ? made + Environment.NewLine : "") + Environment.NewLine +
                   "First " + string.Join(", and ", todo.ToArray()) + ", then take it again.";
        }

        /// <summary>One line per machine model and per meter type that has no usable item code in this
        /// book. Empty = all can be put on this book's documents.</summary>
        public static List<string> ItemCodeProblems(DBSetting localDb, List<ScpRemoteMachine> machines, bool checkMachines = true)
        {
            List<string> problems = new List<string>();
            if (localDb == null || machines == null) return problems;

            if (checkMachines)
            {
                // By model, so ten machines of one model are one line.
                List<string> order = new List<string>();
                Dictionary<string, List<string>> serials = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (ScpRemoteMachine m in machines)
                {
                    if (m.IsGroupItem) continue;   // the contract's group line, not a machine
                    string code = (m.ItemCode ?? "").Trim();
                    List<string> l;
                    if (!serials.TryGetValue(code, out l)) { l = new List<string>(); serials[code] = l; order.Add(code); }
                    l.Add((m.SerialNumber ?? "").Trim());
                }
                foreach (string code in order)
                {
                    string who = string.Join(", ", serials[code].ToArray());
                    if (code.Length == 0) { problems.Add("   machine " + who + "  --  no item code"); continue; }
                    object n = localDb.ExecuteScalar("SELECT COUNT(*) FROM dbo.Item WHERE ItemCode = N'" + code.Replace("'", "''") + "'");
                    if (n == null || n == DBNull.Value || Convert.ToInt32(n) == 0)
                        problems.Add("   machine model " + code + "  (" + who + ")  --  not an item of this book");
                }
            }

            List<string> wanted = new List<string>();
            foreach (ScpRemoteMachine m in machines)
                foreach (ScpRemoteMeter t in m.Meters)
                {
                    string code = (t.MeterTypeCode ?? "").Trim();
                    if (code.Length == 0 || (t.MeterRole ?? "").Trim().ToUpperInvariant() == "NA") continue;
                    bool seen = false;
                    foreach (string w in wanted) if (string.Equals(w, code, StringComparison.OrdinalIgnoreCase)) seen = true;
                    if (!seen) wanted.Add(code);
                }
            foreach (string code in wanted)
            {
                DataTable d = localDb.GetDataTable(
                    "SELECT ISNULL(mt.[Description],'') AS Descr, " +
                    "       ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS Code, " +
                    "       CASE WHEN i.ItemCode IS NULL THEN 0 ELSE 1 END AS Found " +
                    "  FROM dbo.zSCP_MeterType mt " +
                    "  LEFT JOIN dbo.Item i ON i.ItemCode = ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) " +
                    " WHERE mt.MeterTypeCode = N'" + code.Replace("'", "''") + "'", false);
                if (d.Rows.Count == 0) { problems.Add("   " + code + "  --  not a meter type of this book"); continue; }
                string descr = Convert.ToString(d.Rows[0]["Descr"]).Trim();
                string item = Convert.ToString(d.Rows[0]["Code"]).Trim();
                string name = "   " + code + (descr.Length > 0 && !string.Equals(descr, code, StringComparison.OrdinalIgnoreCase) ? "  (" + descr + ")" : "");
                if (item.Length == 0) problems.Add(name + "  --  no item code");
                else if (Convert.ToInt32(d.Rows[0]["Found"]) == 0) problems.Add(name + "  --  item " + item + " is not an item of this book");
            }
            return problems;
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
