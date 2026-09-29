using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One machine line on the board: what HQ's book has, what this contract has, and
    /// whether there is anything to answer.</summary>
    public sealed class IbMachineRow
    {
        public string Serial = "";
        public string Model = "";
        public string AtHq = "";
        public string Mine = "";
        public string StatusText = "";
        public IbChange Change;       // null when there is nothing to answer
    }

    /// <summary>Something HQ did to a contract this book has taken, with what is needed to act on it.</summary>
    public sealed class IbChange
    {
        public const string MACHINE_ADDED = "MACHINE_ADDED";
        public const string MACHINE_GONE = "MACHINE_GONE";
        public const string METER_ADDED = "METER_ADDED";
        public const string METER_GONE = "METER_GONE";
        public const string DETAIL = "DETAIL";
        public const string CONTRACT_GONE = "CONTRACT_GONE";
        /// <summary>A machine's counters whose terms at HQ are not this book's: changed at HQ since the
        /// take, or never brought over by a take from before 29/9. One change per machine; each
        /// counter is one of its <see cref="Parts"/>.</summary>
        public const string METER_TERMS = "METER_TERMS";

        public string Kind = "";
        public string Which = "";
        public long LinkKey;
        public long SourceKey;
        public long LocalParentKey;
        public string NewSnapshot = "";
        public ScpRemoteMachine Machine;
        public ScpRemoteMeter Meter;
        public string ApplyCaption = "";
        public string IgnoreCaption = "Ignore";
        /// <summary>METER_TERMS: bring HQ's rate, minimum and ladder too (a change since the take), or
        /// only what an older take left out.</summary>
        public bool PricesToo;
        /// <summary>METER_TERMS: one part per counter -- LinkKey, LocalParentKey (this book's counter),
        /// Meter (HQ's) and NewSnapshot.</summary>
        public List<IbChange> Parts = new List<IbChange>();
    }

    /// <summary>One counter's reading for the period, as the board shows it.</summary>
    public sealed class IbReadingRow
    {
        public long LocalMeterKey;
        public long SourceMeterKey;      // 0 = a counter of this book's own
        public string ServiceItemNo = "";
        public string Serial = "";
        public string Meter = "";
        public decimal Last;
        public decimal Current;
        public decimal Copies;
        public decimal Amount;
        public string AtHq = "";
        public string Saved = "";
        public DateTime? HqDate;
        public bool FromHq { get { return SourceMeterKey > 0; } }
        public bool HasReading;
        public bool Invoiced;
        public DataRow Row;              // the billing row it came from
        /// <summary>A charge with no counter -- the rent, a minimum, a waive -- listed with the readings
        /// so the board shows everything the invoice will carry. Never keyed, never a reading.</summary>
        public bool IsCharge;
        /// <summary>The Amount column means something: false for a minimum or a waive already on an
        /// invoice, whose billed top-up or credit is on that invoice, not on this row.</summary>
        public bool AmountShown = true;
    }

    /// <summary>One contract on the board. A contract of theirs that this book has not taken is a row
    /// too; one they have that was taken twice is two.</summary>
    public sealed class IbContractRow
    {
        public ScpRemoteContract Remote;
        public string HqContractNo = "";
        public long LocalKey;
        public string LocalNo = "";
        public string DebtorCode = "";
        public string Customer = "";
        public int Machines;
        public int MetersTotal;
        public int MetersRead;
        public decimal Amount;
        public int Status;
        public string StatusText = "";
        public int Unpriced;
        /// <summary>Meter types on this month's rows with no item of this book to post them under
        /// (user, 29/9). Blocks Generate like Unpriced.</summary>
        public int NoItemCode;
        public List<string> NoItemTypes = new List<string>();
        public int Backwards;            // HQ's reading is below the last one billed here
        public bool CorrectedAfterInvoice;
        public List<string> InvoicedDocs = new List<string>();
        public List<IbMachineRow> MachineRows = new List<IbMachineRow>();
        public List<IbReadingRow> Readings = new List<IbReadingRow>();
        public List<IbChange> Changes = new List<IbChange>();
        public List<ScpRemoteMachine> HqMachines = new List<ScpRemoteMachine>();
        public DataTable Rows;
        public Dictionary<string, List<decimal[]>> Ladders;
        public string Error = "";
        /// <summary>The month shown for this contract (YYYYMM): the one being billed, or the last one
        /// billed when nothing is due. 0 = none.</summary>
        public int Period;
        /// <summary>True when <see cref="Period"/> still has something to bill.</summary>
        public bool Open;
        /// <summary>Months to bill up to this month, <see cref="Period"/> included.</summary>
        public int Due;
        public ScpContractSequence Sequence;
        public bool Taken { get { return LocalKey > 0; } }
        public int Missing { get { return MetersTotal - MetersRead; } }
    }

    /// <summary>One invoice on the board's invoice list: an invoice a taken contract's open month will
    /// make, one already made this month, or a month skipped.</summary>
    public sealed class IbInvoiceRow
    {
        public IbContractRow Contract;
        public InvoiceRunItem Item;      // null for a skipped month
        public int Period;
        public string JobKey = "";
        public string Kind = "";
        public string Detail = "";
        public int Machines;
        public int MetersTotal;
        public int MetersRead;
        public decimal Amount;
        public int Status;
        public string StatusText = "";
        public string DocNo = "";
        public bool Skipped;
    }

    /// <summary>
    /// The Inter-Billing board: HQ's contracts, and for each one this book has taken, what it would
    /// bill this period using HQ's readings as they stand.
    ///
    /// <para>HQ reads the meters; this book only bills. Nothing is fetched or brought over by hand:
    /// the readings are read from HQ's book when the board loads, and written into this book at the
    /// moment an invoice is generated -- stamped INTERBILL with HQ's reading date -- so every invoice
    /// keeps its own meter record even if HQ changes the figure later.</para>
    ///
    /// <para>Money is never decided here. Rows come from <see cref="ScpBillingRows"/> and jobs from
    /// <see cref="ScpInvoiceJobs.Build"/>, the same as every other screen that bills.</para>
    /// </summary>
    public static class ScpInterBillBoard
    {
        public const int READY = 0;
        public const int WAITING = 1;
        public const int CHANGED = 2;
        public const int UNPRICED = 3;
        public const int NOT_TAKEN = 4;
        public const int INVOICED = 5;
        public const int ENDED = 6;
        public const int ERROR = 7;
        public const int NO_ITEM = 8;

        // ───────────────────────────── load ─────────────────────────────

        public static List<IbContractRow> Load(DBSetting db, ScpInterBillBook book, string cs,
                                               int year, int month, bool showEnded)
        {
            return Load(db, book, cs, year, month, showEnded, null);
        }

        /// <summary>The board, for HQ's contracts whose customer AT HQ is in <paramref name="customers"/>
        /// (HQ's debtor codes). Null = every HQ contract. A contract left out is never read -- not its
        /// machines, not HQ's readings for it, not the contract taken from it here.
        /// <para><paramref name="year"/>/<paramref name="month"/> is the month the board bills UP TO. Nobody
        /// picks the month a contract bills: each taken contract is shown at its own first month with
        /// no invoice (<see cref="ScpBillingSequence"/>), so a month cannot be skipped by accident.</para></summary>
        public static List<IbContractRow> Load(DBSetting db, ScpInterBillBook book, string cs,
                                               int year, int month, bool showEnded, HashSet<string> customers)
        {
            List<IbContractRow> list = new List<IbContractRow>();
            if (db == null || book == null || !book.IsUsable || string.IsNullOrEmpty(cs)) return list;

            List<ScpRemoteContract> remote = ScpInterBillReader.Contracts(cs, true, "");
            Dictionary<long, ScpRemoteContract> remoteByKey = new Dictionary<long, ScpRemoteContract>();
            foreach (ScpRemoteContract rc in remote) remoteByKey[rc.ContractKey] = rc;

            // Their contract -> the contracts this book made from it.
            Dictionary<long, List<ScpInterBillLink>> takenBySource = new Dictionary<long, List<ScpInterBillLink>>();
            foreach (ScpInterBillLink cl in ScpInterBillLinks.OfType(db, book.RemoteBookId, ScpInterBillLink.CONTRACT, false))
            {
                if (cl.LocalKey <= 0 || LocalContractNo(db, cl.LocalKey).Length == 0) continue;
                List<ScpInterBillLink> l;
                if (!takenBySource.TryGetValue(cl.SourceKey, out l)) { l = new List<ScpInterBillLink>(); takenBySource[cl.SourceKey] = l; }
                l.Add(cl);
            }

            // Where each taken contract stands in its months, read for all of them at once.
            int upTo = ScpBillingSequence.Period(year, month);
            List<long> takenKeys = new List<long>();
            foreach (List<ScpInterBillLink> taken in takenBySource.Values)
                foreach (ScpInterBillLink cl in taken) takenKeys.Add(cl.LocalKey);
            Dictionary<long, ScpContractSequence> sequences = ScpBillingSequence.Load(db, takenKeys);

            foreach (ScpRemoteContract rc in remote)
            {
                if (customers != null && !customers.Contains((rc.DebtorCode ?? "").Trim())) continue;
                List<ScpInterBillLink> links;
                if (takenBySource.TryGetValue(rc.ContractKey, out links))
                {
                    foreach (ScpInterBillLink cl in links)
                        list.Add(LoadTaken(db, book, cs, rc, cl, upTo, SequenceOf(sequences, cl.LocalKey, upTo)));
                    continue;
                }
                bool ended = rc.Inactive || (rc.ExpiryDate.HasValue && rc.ExpiryDate.Value.Date < new DateTime(year, month, 1));
                if (ended && !showEnded) continue;
                IbContractRow row = new IbContractRow();
                row.Remote = rc;
                row.HqContractNo = rc.ContractNo;
                row.Machines = rc.MachineCount;
                row.Status = ended ? ENDED : NOT_TAKEN;
                row.StatusText = ended ? "Ended at HQ" : "Not taken";
                try
                {
                    row.HqMachines = ScpInterBillReader.Machines(cs, rc.ContractKey);
                    foreach (ScpRemoteMachine m in row.HqMachines)
                    {
                        IbMachineRow mr = new IbMachineRow();
                        mr.Serial = m.SerialNumber; mr.Model = m.ItemCode;
                        mr.AtHq = CounterList(m);
                        mr.Mine = "";
                        mr.StatusText = OpeningReadings(m);
                        row.MachineRows.Add(mr);
                    }
                }
                catch (Exception ex) { row.Error = ex.Message; }
                list.Add(row);
            }

            // Contracts taken from them that are no longer in their book at all.
            foreach (KeyValuePair<long, List<ScpInterBillLink>> kv in takenBySource)
            {
                if (remoteByKey.ContainsKey(kv.Key)) continue;
                // HQ no longer has the contract, so there is no HQ customer to filter on: only an
                // unfiltered board shows these.
                if (customers != null) continue;
                foreach (ScpInterBillLink cl in kv.Value)
                {
                    IbContractRow row = new IbContractRow();
                    row.HqContractNo = cl.SourceRef;
                    row.DebtorCode = LocalDebtor(db, cl.LocalKey);
                    row.LocalKey = cl.LocalKey;
                    row.LocalNo = LocalContractNo(db, cl.LocalKey);
                    row.Customer = LocalCustomer(db, cl.LocalKey);
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.CONTRACT_GONE; ch.Which = cl.SourceRef; ch.LinkKey = cl.LinkKey;
                    ch.NewSnapshot = cl.TakenSnapshot; ch.ApplyCaption = "Noted"; ch.IgnoreCaption = "";
                    if (cl.Status == ScpInterBillLink.LIVE) row.Changes.Add(ch);
                    row.Status = row.Changes.Count > 0 ? CHANGED : ENDED;
                    row.StatusText = row.Changes.Count > 0 ? "Removed at HQ" : "Ended at HQ";
                    IbMachineRow mr = new IbMachineRow();
                    mr.Serial = cl.SourceRef; mr.AtHq = "—"; mr.Mine = row.LocalNo; mr.StatusText = "Removed at HQ";
                    mr.Change = row.Changes.Count > 0 ? ch : null;
                    row.MachineRows.Add(mr);
                    list.Add(row);
                }
            }
            return list;
        }

        private static IbContractRow LoadTaken(DBSetting db, ScpInterBillBook book, string cs,
                                               ScpRemoteContract rc, ScpInterBillLink cl, int upTo, ScpContractSequence seq)
        {
            IbContractRow row = new IbContractRow();
            row.Remote = rc;
            row.Sequence = seq;
            row.HqContractNo = rc.ContractNo;
            row.LocalKey = cl.LocalKey;
            row.LocalNo = LocalContractNo(db, cl.LocalKey);
            row.DebtorCode = LocalDebtor(db, cl.LocalKey);
            row.Customer = LocalCustomer(db, cl.LocalKey);
            try
            {
                row.HqMachines = ScpInterBillReader.Machines(cs, rc.ContractKey);
                List<ScpInterBillLink> mine = ScpInterBillLinks.ForContract(db, cl.LocalKey);

                // The contract's own facts first: withdrawn, end date, number.
                string nowSnap = ScpInterBillTake.ContractSnapshot(rc);
                List<string> diff = ScpInterBillSnapshot.Differences(cl.TakenSnapshot, nowSnap);
                diff.Remove("MACHINES");
                if (diff.Count > 0)
                {
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.DETAIL; ch.Which = rc.ContractNo; ch.LinkKey = cl.LinkKey;
                    ch.NewSnapshot = nowSnap; ch.ApplyCaption = "Accept"; ch.IgnoreCaption = "";
                    row.Changes.Add(ch);
                    IbMachineRow mr = new IbMachineRow();
                    mr.Serial = rc.ContractNo; mr.Model = "Contract"; mr.AtHq = DescribeNow(diff, nowSnap);
                    mr.Mine = DescribeNow(diff, cl.TakenSnapshot); mr.StatusText = "Changed at HQ"; mr.Change = ch;
                    row.MachineRows.Add(mr);
                }

                CompareMachines(db, row, cl, mine, book.MarginPercent);
                LoadSequenceMonth(db, cs, row, mine, upTo);
                if (row.Rows == null) row.Machines = row.HqMachines.Count;
                Decide(row, upTo);
            }
            catch (Exception ex)
            {
                row.Error = ex.Message;
                row.Status = ERROR;
                row.StatusText = "Error";
            }
            return row;
        }

        /// <summary>HQ's machines against this contract's, machine by machine and counter by counter.
        /// The same comparison the old "What they changed" tab made, now producing board lines.</summary>
        private static void CompareMachines(DBSetting db, IbContractRow row, ScpInterBillLink contractLink,
                                            List<ScpInterBillLink> mine, decimal marginPercent)
        {
            Dictionary<long, DataRow> localTerms = LocalMeterTerms(db, row.LocalKey);
            Dictionary<long, ScpInterBillLink> itemBySource = new Dictionary<long, ScpInterBillLink>();
            Dictionary<long, ScpInterBillLink> meterBySource = new Dictionary<long, ScpInterBillLink>();
            HashSet<long> linkedItems = new HashSet<long>();
            foreach (ScpInterBillLink l in mine)
            {
                if (l.EntityType == ScpInterBillLink.ITEM) { itemBySource[l.SourceKey] = l; if (l.LocalKey > 0) linkedItems.Add(l.LocalKey); }
                else if (l.EntityType == ScpInterBillLink.METER) meterBySource[l.SourceKey] = l;
            }
            Dictionary<long, string> localCounters = LocalCountersByItem(db, row.LocalKey);
            Dictionary<long, string[]> localItems = LocalItems(db, row.LocalKey);

            HashSet<long> seenItems = new HashSet<long>();
            HashSet<long> seenMeters = new HashSet<long>();
            string mineNo = row.LocalNo;

            foreach (ScpRemoteMachine m in row.HqMachines)
            {
                seenItems.Add(m.ItemKey);
                IbMachineRow mr = new IbMachineRow();
                mr.Serial = m.SerialNumber; mr.Model = m.ItemCode; mr.AtHq = CounterList(m);
                ScpInterBillLink link;
                if (!itemBySource.TryGetValue(m.ItemKey, out link))
                {
                    if (m.Inactive) continue;   // a machine off hire over there that was never here is not news
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.MACHINE_ADDED; ch.Which = m.SerialNumber; ch.SourceKey = m.ItemKey; ch.Machine = m;
                    ch.ApplyCaption = "Add " + m.SerialNumber + " to " + mineNo;
                    row.Changes.Add(ch);
                    mr.Mine = "—"; mr.StatusText = "Added at HQ"; mr.Change = ch;
                    row.MachineRows.Add(mr);
                    continue;
                }
                string localCounterText;
                mr.Mine = link.LocalKey > 0 && localCounters.TryGetValue(link.LocalKey, out localCounterText) ? localCounterText : "—";
                if (link.Status == ScpInterBillLink.DECLINED || link.Status == ScpInterBillLink.GONE)
                {
                    // Already answered. Its counters are still HQ's and still seen, or each would be
                    // reported as "removed at HQ" on the next load.
                    foreach (ScpRemoteMeter mt in m.Meters) seenMeters.Add(mt.ItemMeterKey);
                    if (link.Status == ScpInterBillLink.DECLINED) { mr.Mine = "—"; mr.StatusText = "Ignored"; }
                    else mr.StatusText = m.Inactive ? "Off hire at HQ" : "Kept";
                    row.MachineRows.Add(mr);
                    continue;
                }

                if (m.Inactive)
                {
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.MACHINE_GONE; ch.Which = m.SerialNumber; ch.LinkKey = link.LinkKey;
                    ch.NewSnapshot = m.Snapshot(); ch.ApplyCaption = "Stop billing " + m.SerialNumber;
                    row.Changes.Add(ch);
                    mr.StatusText = "Off hire at HQ"; mr.Change = ch;
                }
                else
                {
                    List<string> diff = ScpInterBillSnapshot.Differences(link.TakenSnapshot, m.Snapshot());
                    diff.Remove("METERS");
                    if (diff.Count > 0)
                    {
                        IbChange ch = new IbChange();
                        ch.Kind = IbChange.DETAIL; ch.Which = m.SerialNumber; ch.LinkKey = link.LinkKey;
                        ch.NewSnapshot = m.Snapshot(); ch.ApplyCaption = "Accept"; ch.IgnoreCaption = "";
                        row.Changes.Add(ch);
                        mr.StatusText = "Changed at HQ · " + DescribeFields(diff); mr.Change = ch;
                    }
                    else mr.StatusText = "Same";
                }

                IbChange terms = null;
                List<string> termWords = new List<string>();
                foreach (ScpRemoteMeter mt in m.Meters)
                {
                    seenMeters.Add(mt.ItemMeterKey);
                    ScpInterBillLink ml;
                    if (!meterBySource.TryGetValue(mt.ItemMeterKey, out ml))
                    {
                        IbChange ch = new IbChange();
                        ch.Kind = IbChange.METER_ADDED; ch.Which = m.SerialNumber + " " + mt.MeterTypeCode;
                        ch.SourceKey = mt.ItemMeterKey; ch.LocalParentKey = link.LocalKey; ch.Meter = mt;
                        ch.ApplyCaption = "Add " + mt.MeterTypeCode + " to " + m.SerialNumber;
                        row.Changes.Add(ch);
                        if (mr.Change == null) { mr.Change = ch; mr.StatusText = mt.MeterTypeCode + " added at HQ"; }
                        continue;
                    }
                    if (ml.Status != ScpInterBillLink.LIVE || ml.LocalKey <= 0) continue;

                    // Its terms (29/9, user: "必须带过来"). Taken since then: what HQ changed since the
                    // take, prices included. Taken before: the snapshot has no terms in it, so this
                    // book's counter is held against HQ's instead, for what that take left out.
                    bool olderTake = !ScpInterBillSnapshot.Parse(ml.TakenSnapshot).ContainsKey("FOC");
                    List<string> diff;
                    if (olderTake)
                    {
                        DataRow lt;
                        diff = localTerms.TryGetValue(ml.LocalKey, out lt) ? TermsNotBrought(lt, mt, marginPercent) : new List<string>();
                    }
                    else diff = ScpInterBillSnapshot.Differences(ml.TakenSnapshot, mt.Snapshot());
                    if (diff.Count == 0) continue;
                    if (terms == null)
                    {
                        terms = new IbChange();
                        terms.Kind = IbChange.METER_TERMS; terms.Which = m.SerialNumber; terms.LocalParentKey = link.LocalKey;
                        terms.Machine = m;
                    }
                    IbChange part = new IbChange();
                    part.Kind = IbChange.METER_TERMS; part.Which = m.SerialNumber + " " + mt.MeterTypeCode;
                    part.LinkKey = ml.LinkKey; part.LocalParentKey = ml.LocalKey; part.Meter = mt;
                    part.NewSnapshot = mt.Snapshot(); part.PricesToo = !olderTake;
                    terms.Parts.Add(part);
                    string words = DescribeFields(diff);
                    // On a rental the FOC is its free months, not copies.
                    if (ScpStrategy.IsRentalRole(mt.MeterRole, mt.MeterTypeCode)) words = words.Replace("free copies", "free months");
                    termWords.Add(mt.MeterTypeCode + " " + words);
                }
                if (terms != null)
                {
                    bool before = false;
                    foreach (IbChange p in terms.Parts) if (!p.PricesToo) before = true;
                    terms.ApplyCaption = before ? "Bring over from HQ" : "Take HQ's terms";
                    terms.IgnoreCaption = "Keep mine";
                    row.Changes.Add(terms);
                    if (mr.Change == null)
                    {
                        mr.Change = terms;
                        mr.StatusText = (before ? "Not brought over at the take · " : "Changed at HQ · ") +
                                        string.Join("; ", termWords.ToArray());
                    }
                }
                row.MachineRows.Add(mr);
            }

            // Linked here, no longer over there.
            foreach (ScpInterBillLink l in mine)
            {
                if (l.LocalKey <= 0 || l.Status != ScpInterBillLink.LIVE) continue;
                if (l.EntityType == ScpInterBillLink.ITEM && !seenItems.Contains(l.SourceKey))
                {
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.MACHINE_GONE; ch.Which = l.SourceRef; ch.LinkKey = l.LinkKey;
                    ch.NewSnapshot = l.TakenSnapshot; ch.ApplyCaption = "Stop billing " + l.SourceRef;
                    row.Changes.Add(ch);
                    IbMachineRow mr = new IbMachineRow();
                    string[] li;
                    mr.Serial = localItems.TryGetValue(l.LocalKey, out li) ? li[1] : l.SourceRef;
                    mr.Model = li != null ? li[2] : "";
                    mr.AtHq = "—";
                    string lc;
                    mr.Mine = localCounters.TryGetValue(l.LocalKey, out lc) ? lc : "";
                    mr.StatusText = "Removed at HQ"; mr.Change = ch;
                    row.MachineRows.Add(mr);
                }
                else if (l.EntityType == ScpInterBillLink.METER && !seenMeters.Contains(l.SourceKey))
                {
                    IbChange ch = new IbChange();
                    ch.Kind = IbChange.METER_GONE; ch.Which = l.SourceRef; ch.LinkKey = l.LinkKey;
                    ch.NewSnapshot = l.TakenSnapshot; ch.ApplyCaption = "Noted"; ch.IgnoreCaption = "";
                    row.Changes.Add(ch);
                    IbMachineRow mr = new IbMachineRow();
                    mr.Serial = l.SourceRef; mr.Model = "Counter"; mr.AtHq = "—"; mr.Mine = l.SourceRef;
                    mr.StatusText = "Counter removed at HQ"; mr.Change = ch;
                    row.MachineRows.Add(mr);
                }
            }

            // Machines this book put on the contract itself.
            foreach (KeyValuePair<long, string[]> kv in localItems)
            {
                if (linkedItems.Contains(kv.Key)) continue;
                IbMachineRow mr = new IbMachineRow();
                mr.Serial = kv.Value[1]; mr.Model = kv.Value[2]; mr.AtHq = "—";
                string lc;
                mr.Mine = localCounters.TryGetValue(kv.Key, out lc) ? lc : "";
                mr.StatusText = "Own machine";
                row.MachineRows.Add(mr);
            }
        }

        /// <summary>This contract's billing rows with HQ's readings laid over them, priced, and the
        /// reading lines the board shows. Nothing is written.</summary>
        private static void LoadRowsAndReadings(DBSetting db, string cs, IbContractRow row,
                                                List<ScpInterBillLink> mine, int year, int month)
        {
            Dictionary<string, List<decimal[]>> ladders;
            // The same filters as the Meter Invoice Run: an inactive item or contract, and a machine
            // past its expiry month (unless the setting shows expired items), does not bill.
            bool includeExpired = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
            string expiryFilter = includeExpired ? "" :
                " AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                "OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(" + year + "," + month + ",1)) ";
            DataTable rows = ScpBillingRows.Load(db, year, month,
                "ISNULL(i.Inactive,'N') = 'N' AND ISNULL(c.Inactive,'N') = 'N'", "",
                " AND c.ContractKey = " + row.LocalKey + " ", expiryFilter, out ladders);
            // Same order as the Meter Invoice Run: staged readings, the agreed line prices, then the
            // flat charges fill themselves in at those prices.
            ScpBillingRows.PrefillFromStaging(db, rows, month, year, ladders);
            ScpBillingRows.ApplyRentalGroupPrices(db, rows);
            ScpBillingRows.ApplyMeterLinePrices(db, rows);
            ScpBillingRows.AutoFillFlatMeters(db, rows, year, month, ladders);
            row.Rows = rows;
            row.Ladders = ladders;

            Dictionary<long, long> sourceByLocal = new Dictionary<long, long>();
            foreach (ScpInterBillLink l in mine)
                if (l.EntityType == ScpInterBillLink.METER && l.LocalKey > 0 && l.Status == ScpInterBillLink.LIVE)
                    sourceByLocal[l.LocalKey] = l.SourceKey;

            Dictionary<long, ScpRemoteReading> hq = new Dictionary<long, ScpRemoteReading>();
            Dictionary<long, decimal> hqPrev = new Dictionary<long, decimal>();
            if (sourceByLocal.Count > 0)
            {
                foreach (ScpRemoteReading x in ScpInterBillReader.Readings(cs, sourceByLocal.Values, year, month))
                    hq[x.ItemMeterKey] = x;
                hqPrev = ScpInterBillReader.PreviousReadings(cs, sourceByLocal.Values, year, month);
            }

            HashSet<long> machines = new HashSet<long>();
            foreach (DataRow r in rows.Rows)
            {
                machines.Add(D64(r["ItemKey"]));
                bool invoiced = S(r["InvoicedDocNo"]).Trim().Length > 0;
                if (invoiced)
                {
                    string d = S(r["InvoicedDocNo"]).Trim();
                    if (!row.InvoicedDocs.Contains(d)) row.InvoicedDocs.Add(d);
                }
                if (!ScpInvoiceRun.IsUsageMeter(r))
                {
                    if (!invoiced) row.Amount += Dec(r["TotalCharges"]);
                    // Listed with the readings (user, 28/9: "I cannot see the rental in the readings,
                    // so I do not know what will be invoiced"): the rent and which month it pays, a
                    // minimum, a waive. Never keyed, and not counted among the readings.
                    IbReadingRow cr = new IbReadingRow();
                    cr.Row = r;
                    cr.IsCharge = true;
                    cr.LocalMeterKey = D64(r["ItemMeterKey"]);
                    cr.ServiceItemNo = S(r["ServiceItemNo"]);
                    cr.Serial = S(r["SerialNo"]);
                    cr.Meter = S(r["MeterType"]);
                    cr.Invoiced = invoiced;
                    cr.Amount = Dec(r["TotalCharges"]);
                    cr.AtHq = ChargeWords(r, invoiced);
                    bool worksOut = ScpBillingRows.IsCommitRow(r) || (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]));
                    cr.AmountShown = !(invoiced && worksOut);
                    cr.Saved = invoiced ? S(r["InvoicedDocNo"]).Trim() : "";
                    row.Readings.Add(cr);
                    continue;
                }

                IbReadingRow rr = new IbReadingRow();
                rr.Row = r;
                rr.LocalMeterKey = D64(r["ItemMeterKey"]);
                rr.ServiceItemNo = S(r["ServiceItemNo"]);
                rr.Serial = S(r["SerialNo"]);
                rr.Meter = S(r["MeterType"]);
                rr.Invoiced = invoiced;
                long src;
                if (sourceByLocal.TryGetValue(rr.LocalMeterKey, out src)) rr.SourceMeterKey = src;
                ScpRemoteReading x = null;
                if (rr.FromHq) hq.TryGetValue(rr.SourceMeterKey, out x);

                // Never billed here (no reading history in this book): start from HQ's last reading
                // before this period, not from the install reading the take copied.
                decimal prev;
                if (rr.FromHq && !invoiced && r["LastReadDate"] == DBNull.Value &&
                    hqPrev.TryGetValue(rr.SourceMeterKey, out prev) && prev > Dec(r["LastReading"]))
                    r["LastReading"] = prev;

                if (invoiced)
                {
                    rr.HasReading = true;
                    rr.Saved = S(r["InvoicedDocNo"]).Trim();
                    rr.AtHq = rr.FromHq ? (x == null ? "" : SourceText(x.Source, x.ReadingDate)) : "Own counter";
                    if (x != null && x.Corrected && x.CurrentReading != Dec(r["CurrentReading"]))
                    {
                        rr.AtHq = "Corrected at HQ · " + x.CorrectionDocNo;
                        row.CorrectedAfterInvoice = true;
                    }
                }
                else if (rr.FromHq)
                {
                    if (x != null && x.CurrentReading > 0m)
                    {
                        r["CurrentReading"] = x.CurrentReading;
                        r["EntrySource"] = "INTERBILL";
                        ScpBillingRows.Recalc(r, ladders, year, month);
                        rr.HasReading = true;
                        rr.HqDate = x.ReadingDate;
                        rr.AtHq = SourceText(x.Source, x.ReadingDate) + (x.Corrected ? " · corrected " + x.CorrectionDocNo : "");
                        if (x.CurrentReading < Dec(r["LastReading"])) { row.Backwards++; rr.AtHq += " · below last"; }
                    }
                    else
                    {
                        r["CurrentReading"] = 0m;
                        r["MeterUsage"] = 0m;
                        r["TotalCharges"] = 0m;
                        rr.AtHq = "No reading at HQ";
                    }
                }
                else
                {
                    rr.HasReading = Dec(r["CurrentReading"]) > 0m;
                    if (rr.HasReading) ScpBillingRows.Recalc(r, ladders, year, month);
                    rr.AtHq = "Own counter";
                }
                rr.Last = Dec(r["LastReading"]);
                rr.Current = Dec(r["CurrentReading"]);
                rr.Copies = Dec(r["MeterUsage"]);
                rr.Amount = Dec(r["TotalCharges"]);
                if (!invoiced) row.Amount += rr.Amount;
                row.MetersTotal++;
                if (rr.HasReading) row.MetersRead++;
                row.Readings.Add(rr);
            }
            row.Machines = machines.Count;

            // A counter with no price bills nothing, silently. A minimum, a machine's own ladder, or a
            // ladder agreed for its merged line (Billing Setup) is a price. Only the charges a
            // contract is expected to price are checked: rental, black, colour.
            HashSet<string> groupLadders = GroupLadders(db, row.LocalKey);
            foreach (DataRow r in rows.Rows)
            {
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;
                if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) continue;
                if (ScpBillingRows.IsCommitRow(r)) continue;
                string role = S(r["Role"]).Trim().ToUpperInvariant();
                if (role != "RENTAL" && role != "BK" && role != "CL") continue;
                if (Dec(r["UnitPrice"]) != 0m || Dec(r["MinCharges"]) != 0m || S(r["MultiPriceCode"]).Trim().Length > 0) continue;
                string grp = S(r["MergeGroupCodeMeter"]).Trim().ToUpperInvariant();
                if ((role == "BK" || role == "CL") && grp.Length > 0 && groupLadders.Contains(role + "|" + grp)) continue;
                row.Unpriced++;
            }
            CountNoItemCode(db, row, rows);
        }

        /// <summary>
        /// Every machine on the contract has to be a stock item of THIS book, and every line this month
        /// bills has to be posted under an item of THIS book (user, 29/9: "Inter-
        /// Billing 画面加一个状态 No item code, 跟 Unpriced 一样, 没设好就挡住"). A code this book does not
        /// have fails the invoice when it is saved; no code at all posts the line to the default sales
        /// account, missing from every report by item. Counted per meter type; a counter that is never
        /// billed (role NA) needs none.
        /// </summary>
        private static void CountNoItemCode(DBSetting db, IbContractRow row, DataTable rows)
        {
            // The machines first: each model on this contract must be a stock item of this book, the
            // same rule the take applies (ScpInterBillTake.ItemCodeProblems).
            DataTable models = db.GetDataTable(
                "SELECT DISTINCT LTRIM(RTRIM(ISNULL(i.ItemCode,''))) AS Code, CASE WHEN it.ItemCode IS NULL THEN 0 ELSE 1 END AS Found " +
                "  FROM dbo.zSCP2_Item i LEFT JOIN dbo.Item it ON it.ItemCode = i.ItemCode " +
                " WHERE i.ContractKey = " + row.LocalKey + " AND ISNULL(i.Inactive,'N') = 'N' AND ISNULL(i.IsGroupItem,'N') = 'N'", false);
            foreach (DataRow m in models.Rows)
            {
                if (Convert.ToInt32(m["Found"]) == 1) continue;
                string code = S(m["Code"]);
                row.NoItemCode++;
                row.NoItemTypes.Add(code.Length > 0 ? "model " + code : "a machine with no model");
            }

            Dictionary<string, string> codeOfType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in rows.Rows)
            {
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;
                if (S(r["Role"]).Trim().ToUpperInvariant() == "NA") continue;
                string type = S(r["MeterType"]).Trim();
                if (type.Length == 0 || codeOfType.ContainsKey(type)) continue;
                codeOfType[type] = S(r["ACItemCode"]).Trim();
            }
            List<string> codes = new List<string>();
            foreach (string c in codeOfType.Values) if (c.Length > 0 && !codes.Contains(c)) codes.Add(c);
            HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (codes.Count > 0)
            {
                List<string> quoted = new List<string>();
                foreach (string c in codes) quoted.Add("N'" + c.Replace("'", "''") + "'");
                DataTable t = db.GetDataTable("SELECT ItemCode FROM dbo.Item WHERE ItemCode IN (" + string.Join(",", quoted.ToArray()) + ")", false);
                foreach (DataRow r in t.Rows) found.Add(S(r["ItemCode"]).Trim());
            }
            foreach (KeyValuePair<string, string> kv in codeOfType)
            {
                if (kv.Value.Length > 0 && found.Contains(kv.Value)) continue;
                row.NoItemCode++;
                row.NoItemTypes.Add(kv.Key);
            }
        }

        /// <summary>This book's counters on one contract, with the terms an older take did not bring:
        /// held against HQ's by <see cref="TermsNotBrought"/>.</summary>
        private static Dictionary<long, DataRow> LocalMeterTerms(DBSetting db, long contractKey)
        {
            Dictionary<long, DataRow> map = new Dictionary<long, DataRow>();
            if (contractKey <= 0) return map;
            DataTable t = db.GetDataTable(
                "SELECT m.ItemMeterKey, ISNULL(m.MeterRole,'') AS MeterRole, ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, ISNULL(m.FOCQty,0) AS FOCQty, " +
                "       ISNULL(m.RebateQtyInPercent,0) AS RebatePct, m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, " +
                "       ISNULL(m.WaiveFirstNMonths,0) AS WaiveFirstNMonths, ISNULL(m.WaiveTargetAmount,0) AS WaiveTargetAmount, " +
                "       ISNULL(m.WaivePartialPct,100) AS WaivePartialPct, ISNULL(NULLIF(m.WaiveScope,''),'BKCL') AS WaiveScope, " +
                "       ISNULL(m.WaivePartialThreshold,0) AS WaivePartialThreshold, ISNULL(m.WaivePartialAmount,0) AS WaivePartialAmount, " +
                "       ISNULL(NULLIF(m.CommitScope,''),'S') AS CommitScope, ISNULL(m.TierMode,'T') AS TierMode, " +
                "       CASE WHEN EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeterPrice p WHERE p.ItemMeterKey = m.ItemMeterKey) " +
                "              OR ISNULL(m.MeterMultiPriceCode,'') <> '' THEN 1 ELSE 0 END AS HasLadder, " +
                // This book's meter type's default tiers: what prices a counter with no ladder of its own.
                "       CASE WHEN ISNULL(mt.MeterMultiPriceCode,'') <> '' AND EXISTS (SELECT 1 FROM dbo.zSCP_MeterMultiPriceItem x " +
                "            WHERE x.MeterMultiPriceCode = mt.MeterMultiPriceCode) THEN 1 ELSE 0 END AS TypeLadder " +
                "  FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "  LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                " WHERE i.ContractKey = " + contractKey, false);
            foreach (DataRow r in t.Rows) map[Convert.ToInt64(r["ItemMeterKey"])] = r;
            return map;
        }

        /// <summary>What a take from before 29/9 left out of one counter, held against HQ's terms as they
        /// would have arrived (sums of money through the margin). The rate and the minimum did come
        /// across then and are not compared: they may have been re-agreed here since.</summary>
        private static List<string> TermsNotBrought(DataRow l, ScpRemoteMeter t, decimal marginPercent)
        {
            List<string> d = new List<string>();
            string role = S(l["MeterRole"]).Trim().ToUpperInvariant();
            bool rideOn = role == "WAIVE" || role == "COMMIT";
            // A rental's FOC is its free months left -- each book counts its own down -- so it is only
            // "not brought over" when HQ still has free months and this book has none at all.
            if (ScpStrategy.IsRentalRole(role, S(l["MeterTypeCode"])) ? Dec(l["FOCQty"]) == 0m && t.FocQty > 0m : Dec(l["FOCQty"]) != t.FocQty)
                d.Add("FOC");
            if (Dec(l["RebatePct"]) != t.RebatePct) d.Add("REBATE");
            if (Convert.ToInt32(l["WaiveFirstNMonths"]) != t.WaiveFirstNMonths
                || Dec(l["WaiveTargetAmount"]) != ScpInterBillTake.WithMargin(t.WaiveTargetAmount, marginPercent, true)
                || Dec(l["WaivePartialPct"]) != t.WaivePartialPct
                || Dec(l["WaivePartialThreshold"]) != ScpInterBillTake.WithMargin(t.WaivePartialThreshold, marginPercent, true)
                || Dec(l["WaivePartialAmount"]) != ScpInterBillTake.WithMargin(t.WaivePartialAmount, marginPercent, true))
                d.Add("WAIVE");
            // As the engine reads it: an older take's 'S' is both colours, the same as HQ's BKCL.
            if (rideOn && ScpCommittedMin.NormScope(S(l["WaiveScope"])) != ScpCommittedMin.NormScope(t.WaiveScope)) d.Add("COLOUR");
            if (rideOn && !string.Equals(S(l["CommitScope"]).Trim(), t.CommitScope, StringComparison.OrdinalIgnoreCase)) d.Add("COUNTS");
            bool own = Convert.ToInt32(l["HasLadder"]) == 1, typeLadder = Convert.ToInt32(l["TypeLadder"]) == 1;
            bool flat = ScpStrategy.IsRentalRole(role, S(l["MeterTypeCode"])) || rideOn;
            if (!string.Equals(S(l["TierMode"]).Trim(), t.TierMode, StringComparison.OrdinalIgnoreCase)
                || (t.Ladder.Count > 0 && !own)                                     // HQ's tiers never came
                || (!flat && t.Ladder.Count == 0 && t.ChargesRate > 0m && !own && typeLadder))   // this book's type tiers price it
                d.Add("TIER");
            DateTime? ls = l["RentalStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(l["RentalStartDate"]).Date;
            DateTime? hs = t.RentalStartDate.HasValue ? t.RentalStartDate.Value.Date : (DateTime?)null;
            if (ls != hs || Convert.ToInt32(l["RentalMonths"]) != t.RentalMonths) d.Add("RENTAL");
            return d;
        }

        /// <summary>Picks the month a contract is at, and loads it.
        /// <para>A month that has a stamp but may not be finished -- billed in part, or an invoice
        /// deleted since -- is put through the billing engine first, oldest first: if anything is still
        /// to bill, that month is the one to work, before anything later. Otherwise the next month is
        /// loaded when it is due. When nothing is due, the last month billed is named with its invoices,
        /// without loading it.</para></summary>
        private static void LoadSequenceMonth(DBSetting db, string cs, IbContractRow row, List<ScpInterBillLink> mine, int upTo)
        {
            ScpContractSequence seq = row.Sequence;
            int checks = 0;
            foreach (int p in seq.Unfinished)
            {
                if (p < seq.First || p >= seq.Next || seq.Skipped.ContainsKey(p)) continue;
                if (checks++ >= MAX_UNFINISHED_CHECKS) break;
                ResetMonth(row);
                LoadRowsAndReadings(db, cs, row, mine, ScpBillingSequence.YearOf(p), ScpBillingSequence.MonthOf(p));
                row.Period = p;
                if (HasPending(row))
                {
                    row.Open = true;
                    row.Due = 1 + seq.DueFrom(seq.Next, upTo);
                    return;
                }
            }
            ResetMonth(row);
            int next = seq.Next;
            if (next <= upTo && !seq.Complete)
            {
                LoadRowsAndReadings(db, cs, row, mine, ScpBillingSequence.YearOf(next), ScpBillingSequence.MonthOf(next));
                row.Period = next;
                row.Open = true;
                // A month with nothing on the contract to bill (every machine inactive or ended) is not
                // a month behind; it says "Nothing to bill" and does not count.
                row.Due = row.Rows != null && row.Rows.Rows.Count > 0 ? seq.DueFrom(next, upTo) : 0;
                return;
            }
            int prev = ScpBillingSequence.AddMonths(next, -1);
            if (prev >= seq.First && seq.Billed.Contains(prev))
            {
                // This month's invoices are loaded line by line, so the invoice list keeps each one under
                // the name it had while it waited, now Invoiced. An older month is only named.
                if (prev == upTo) LoadRowsAndReadings(db, cs, row, mine, ScpBillingSequence.YearOf(prev), ScpBillingSequence.MonthOf(prev));
                else row.InvoicedDocs.AddRange(ScpBillingSequence.InvoiceNumbers(db, row.LocalKey, prev));
                row.Period = prev;
            }
        }

        /// <summary>How many suspect months one contract may put through the engine per load. Each check
        /// is a full load of that month; a contract with more than this is looked at again next time.</summary>
        private const int MAX_UNFINISHED_CHECKS = 6;

        private static void ResetMonth(IbContractRow row)
        {
            row.Readings.Clear();
            row.InvoicedDocs.Clear();
            row.Amount = 0m;
            row.MetersTotal = 0;
            row.MetersRead = 0;
            row.Unpriced = 0;
            row.NoItemCode = 0;
            row.NoItemTypes.Clear();
            row.Backwards = 0;
            row.CorrectedAfterInvoice = false;
            row.Machines = 0;
            row.Rows = null;
            row.Ladders = null;
            row.Period = 0;
        }

        private static ScpContractSequence SequenceOf(Dictionary<long, ScpContractSequence> map, long contractKey, int upTo)
        {
            ScpContractSequence s;
            if (map.TryGetValue(contractKey, out s)) return s;
            s = new ScpContractSequence();
            s.ContractKey = contractKey;
            s.BillFrom = upTo;
            s.Next = upTo;
            return s;
        }

        /// <summary>The month a contract's progress is counted to: the month being billed, or the next
        /// one when nothing is open.</summary>
        public static int OpenPeriod(IbContractRow r)
        {
            if (r == null) return 0;
            if (r.Open) return r.Period;
            return r.Sequence != null ? r.Sequence.Next : 0;
        }

        private static bool HasPending(IbContractRow row)
        {
            if (row.Rows == null) return false;
            foreach (DataRow r in row.Rows.Rows)
                if (StillToBill(r)) return true;
            return false;
        }

        /// <summary>The one status a contract shows, in the order a month is worked.</summary>
        private static void Decide(IbContractRow row, int upTo)
        {
            // Still to bill = a row the engine stamps when it bills: a charge (not a waive or a
            // committed minimum, which ride on other lines and are never stamped themselves) or a
            // counter that is read. Counting the others would put an invoiced contract back to Ready.
            bool pending = HasPending(row);
            ScpContractSequence seq = row.Sequence;

            if (row.Changes.Count > 0) { row.Status = CHANGED; row.StatusText = "Changed at HQ"; return; }
            if (!row.Open)
            {
                row.Status = INVOICED;
                if (seq != null && seq.Complete) row.StatusText = "Complete";
                else if (seq != null && seq.First > upTo) { row.Status = WAITING; row.StatusText = "Starts " + ScpBillingSequence.Name(seq.First); }
                else row.StatusText = "Up to date";
                if (row.InvoicedDocs.Count > 0)
                    row.StatusText += " · " + string.Join(", ", row.InvoicedDocs.ToArray()) +
                                      (row.CorrectedAfterInvoice ? " · corrected at HQ" : "");
                return;
            }
            if (row.Backwards > 0) { row.Status = CHANGED; row.StatusText = "HQ reading below last (" + row.Backwards + ")"; }
            else if (row.Unpriced > 0) { row.Status = UNPRICED; row.StatusText = "Unpriced (" + row.Unpriced + ")"; }
            else if (row.NoItemCode > 0) { row.Status = NO_ITEM; row.StatusText = NoItemText(row); }
            else if (row.Missing > 0) { row.Status = WAITING; row.StatusText = "Waiting HQ reading (" + row.Missing + ")"; }
            else if (!pending) { row.Status = WAITING; row.StatusText = "Nothing to bill"; }
            else { row.Status = READY; row.StatusText = "Ready"; }
        }

        private static bool StillToBill(DataRow r)
        {
            if (S(r["InvoicedDocNo"]).Trim().Length > 0) return false;
            if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) return false;
            if (ScpBillingRows.IsCommitRow(r)) return false;
            bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            if (flat) return true;
            return ScpInvoiceRun.IsUsageMeter(r);
        }

        /// <summary>"BK|GROUP" / "CL|GROUP" for every merged meter line of a contract that has a
        /// ladder agreed in Billing Setup.</summary>
        private static HashSet<string> GroupLadders(DBSetting db, long contractKey)
        {
            HashSet<string> set = new HashSet<string>();
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT UPPER(ISNULL(GroupCode,'')) AS GroupCode, ISNULL(LadderBk,'') AS LadderBk, ISNULL(LadderCl,'') AS LadderCl " +
                    "  FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + contractKey + " AND Side = 'M'", false);
                foreach (DataRow r in t.Rows)
                {
                    string g = S(r["GroupCode"]).Trim();
                    if (S(r["LadderBk"]).Trim().Length > 0) set.Add("BK|" + g);
                    if (S(r["LadderCl"]).Trim().Length > 0) set.Add("CL|" + g);
                }
            }
            catch { }
            return set;
        }

        // ───────────────────────────── the invoice list ─────────────────────────────

        /// <summary>The board as invoices -- a checklist. Every invoice a taken contract's open month will
        /// make, folded exactly as the Meter Invoice Run folds them (<see cref="ScpInvoiceRun.BuildItems"/>),
        /// with this board's reasons to wait laid over each. This month's invoices already made stay in
        /// the list as Invoiced, and a month skipped says so.</summary>
        public static List<IbInvoiceRow> Invoices(DBSetting db, List<IbContractRow> contracts, int year, int month)
        {
            List<IbInvoiceRow> list = new List<IbInvoiceRow>();
            if (db == null || contracts == null) return list;
            int upTo = ScpBillingSequence.Period(year, month);
            string grpMode = ScpInvoiceRun.GroupingMode(db);
            foreach (IbContractRow c in contracts)
            {
                if (!c.Taken || c.Sequence == null) continue;
                if (c.Rows != null && c.Period > 0)
                {
                    if (!c.Rows.Columns.Contains(ScpInvoiceRun.COL_JOBKEY)) c.Rows.Columns.Add(ScpInvoiceRun.COL_JOBKEY, typeof(string));
                    if (!c.Rows.Columns.Contains(ScpInvoiceRun.COL_NEEDS)) c.Rows.Columns.Add(ScpInvoiceRun.COL_NEEDS, typeof(bool));
                    foreach (InvoiceRunItem it in ScpInvoiceRun.BuildItems(c.Rows, grpMode))
                    {
                        IbInvoiceRow v = new IbInvoiceRow();
                        v.Contract = c;
                        v.Item = it;
                        v.Period = c.Period;
                        v.JobKey = it.JobKey;
                        v.Kind = it.Kind;
                        v.Detail = it.Detail;
                        v.Machines = it.Machines.Count;
                        v.MetersTotal = it.MetersTotal;
                        v.MetersRead = it.MetersRead;
                        v.Amount = it.Amount;
                        if (it.Status == InvoiceRunItem.INVOICED)
                        {
                            v.Status = INVOICED; v.DocNo = it.InvoicedDocNo; v.StatusText = "Invoiced · " + it.InvoicedDocNo;
                        }
                        else if (c.Changes.Count > 0) { v.Status = CHANGED; v.StatusText = "Changed at HQ"; }
                        else if (c.Backwards > 0) { v.Status = CHANGED; v.StatusText = "HQ reading below last (" + c.Backwards + ")"; }
                        else if (c.Unpriced > 0) { v.Status = UNPRICED; v.StatusText = "Unpriced (" + c.Unpriced + ")"; }
                        else if (c.NoItemCode > 0) { v.Status = NO_ITEM; v.StatusText = NoItemText(c); }
                        else if (it.Missing > 0) { v.Status = WAITING; v.StatusText = "Waiting HQ reading (" + it.Missing + ")"; }
                        else { v.Status = READY; v.StatusText = "Ready"; }
                        list.Add(v);
                    }
                }
                else if (!c.Open && c.Sequence.Skipped.ContainsKey(upTo))
                {
                    IbInvoiceRow v = new IbInvoiceRow();
                    v.Contract = c;
                    v.Period = upTo;
                    v.Skipped = true;
                    v.Kind = "—";
                    v.Status = INVOICED;
                    v.StatusText = "Skipped · " + c.Sequence.Skipped[upTo];
                    list.Add(v);
                }
            }
            return list;
        }

        // ───────────────────────────── generate ─────────────────────────────

        /// <summary>Invoice jobs for the contracts given, from the rows the board already priced with
        /// HQ's readings. A contract the engine refuses is left out and named in <paramref name="skipped"/>.</summary>
        public static List<MeterInvoiceGenerator.InvoiceJob> BuildJobs(DBSetting db, List<IbContractRow> contracts,
            int year, int month, out Dictionary<long, string> snapshots, out List<string> skipped)
        {
            return BuildJobs(db, contracts, year, month, null, out snapshots, out skipped);
        }

        /// <summary>As above, for the invoices in <paramref name="onlyJobKeys"/> only (null = every invoice of
        /// a Ready contract). Picking invoices lets one go out while another of the same contract still
        /// waits -- a rental billed apart while HQ's copies are not read yet -- but nothing of a contract
        /// with an unanswered change, a reading below the last one, or a price missing.</summary>
        public static List<MeterInvoiceGenerator.InvoiceJob> BuildJobs(DBSetting db, List<IbContractRow> contracts,
            int year, int month, HashSet<string> onlyJobKeys, out Dictionary<long, string> snapshots, out List<string> skipped)
        {
            List<MeterInvoiceGenerator.InvoiceJob> all = new List<MeterInvoiceGenerator.InvoiceJob>();
            snapshots = new Dictionary<long, string>();
            skipped = new List<string>();
            string grpMode = ScpBillingRows.GroupingMode(db);
            foreach (IbContractRow c in contracts)
            {
                if (onlyJobKeys == null)
                {
                    if (c.Status != READY || c.Rows == null) { skipped.Add(c.LocalNo + ": " + c.StatusText); continue; }
                }
                else
                {
                    string block = c.Rows == null ? c.StatusText
                                 : c.Changes.Count > 0 ? "Changed at HQ"
                                 : c.Backwards > 0 ? "HQ reading below last"
                                 : c.Unpriced > 0 ? "Unpriced"
                                 : c.NoItemCode > 0 ? NoItemText(c) : "";
                    if (block.Length > 0) { skipped.Add(c.LocalNo + ": " + block); continue; }
                }
                if (c.Period != ScpBillingSequence.Period(year, month))
                {
                    skipped.Add(c.LocalNo + ": bills " + ScpBillingSequence.Name(c.Period));
                    continue;
                }
                List<DataRow> sel = new List<DataRow>();
                foreach (DataRow r in c.Rows.Rows)
                {
                    r["Sel"] = S(r["InvoicedDocNo"]).Trim().Length == 0 &&
                               (onlyJobKeys == null || onlyJobKeys.Contains(ScpInvoiceRun.JobKeyOf(r, grpMode)));
                    sel.Add(r);
                }
                int alreadyInvoiced;
                Dictionary<long, string> snaps;
                string blockTitle, blockMessage;
                Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs = ScpInvoiceJobs.Build(
                    db, c.Rows, sel, c.Ladders, year, month, grpMode,
                    out alreadyInvoiced, out snaps, out blockTitle, out blockMessage);
                if (jobs == null) { skipped.Add(c.LocalNo + ": " + blockTitle + " - " + blockMessage); continue; }
                if (jobs.Count == 0) { skipped.Add(c.LocalNo + ": nothing to bill"); continue; }
                all.AddRange(jobs.Values);
                if (snaps != null) foreach (KeyValuePair<long, string> kv in snaps) snapshots[kv.Key] = kv.Value;
            }
            return all;
        }

        /// <summary>Writes the HQ readings a contract is about to be billed on into this book, stamped
        /// INTERBILL with HQ's reading date, and appends them to the reading log. A period already
        /// invoiced here is never touched. Returns how many were written.</summary>
        public static int SaveHqReadings(DBSetting db, IbContractRow c, int year, int month)
        {
            return SaveHqReadings(db, c, year, month, null);
        }

        /// <summary>As above, for the counters in <paramref name="onlyMeters"/> (null = all). All or
        /// nothing: if any of them has been invoiced or locked in this book since the board loaded,
        /// nothing is written and the reason is thrown, so an invoice is never built on it.</summary>
        /// <summary>What a charge with no counter is, in the board's At HQ column: "Rental · 2/36 ·
        /// OCT 2026", "Minimum · worked out at Generate", "Waive · decided at Generate".</summary>
        private static string ChargeWords(DataRow r, bool invoiced)
        {
            if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]))
                return invoiced ? "Waive · see the invoice" : "Waive · decided at Generate";
            if (ScpBillingRows.IsCommitRow(r))
                return invoiced ? "Minimum · see the invoice" : "Minimum · worked out at Generate";
            string role = S(r["Role"]).Trim();
            if (ScpStrategy.IsRentalRole(role, S(r["MeterType"])))   // the billing rows call the type code MeterType
            {
                string rf = r.Table.Columns.Contains("RentalFor") ? S(r["RentalFor"]).Trim() : "";
                string free = S(r["EntrySource"]).Trim().ToUpperInvariant() == "RENTAL FREE" ? "Rental free" : "Rental";
                return rf.Length > 0 ? free + " · " + rf : free;
            }
            return "Charge";
        }

        public static int SaveHqReadings(DBSetting db, IbContractRow c, int year, int month, HashSet<long> onlyMeters)
        {
            int n = 0;
            if (db == null || c == null) return 0;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("IbSaveReadings"))
                {
                    try
                    {
                        foreach (IbReadingRow rr in c.Readings)
                        {
                            if (rr.IsCharge || !rr.FromHq || !rr.HasReading || rr.Invoiced || rr.Current <= 0m) continue;
                            if (onlyMeters != null && !onlyMeters.Contains(rr.LocalMeterKey)) continue;
                            DateTime on = rr.HqDate.HasValue ? rr.HqDate.Value : DateTime.Today;
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@v, ReadingDate=@d, Source='INTERBILL', TrackingId='', LastModified=GETDATE() " +
                                " WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m AND InvoicedDocKey IS NULL " +
                                "   AND ISNULL(InvoicedDocNo,'') = '' AND LockedAt IS NULL; " +
                                "IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m) " +
                                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified) " +
                                "VALUES (@k, @y, @m, @v, @d, 'INTERBILL', 'N', GETDATE());", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@k", rr.LocalMeterKey);
                                cmd.Parameters.AddWithValue("@y", year);
                                cmd.Parameters.AddWithValue("@m", month);
                                cmd.Parameters.AddWithValue("@v", rr.Current);
                                cmd.Parameters.AddWithValue("@d", on);
                                if (cmd.ExecuteNonQuery() <= 0)
                                    throw new InvalidOperationException(c.LocalNo + " " + rr.ServiceItemNo + " " + rr.Meter +
                                        ": this period is already invoiced or locked in this book. Refresh and check it.");
                                ScpMeterReadingLog.Append(cn, tx, rr.LocalMeterKey, year, month, rr.Current, on, "INTERBILL", "");
                                n++;
                            }
                        }
                        tx.Commit();
                    }
                    catch { try { tx.Rollback(); } catch { } throw; }
                }
            }
            return n;
        }

        // ───────────────────────────── answers ─────────────────────────────

        /// <summary>Carries out what the Apply button on a change says. Returns an error, or "".</summary>
        public static string Apply(DBSetting db, ScpInterBillBook book, string cs, IbContractRow row, IbChange ch)
        {
            try
            {
                if (ch.Kind == IbChange.MACHINE_ADDED)
                {
                    ScpTakeResult r = ScpInterBillTake.AddMachine(db, book, cs, row.LocalKey, ch.Machine, book.MarginPercent);
                    if (r.Error.Length == 0 && ch.Machine != null)
                    {
                        List<long> hqMeters = new List<long>();
                        foreach (ScpRemoteMeter mt in ch.Machine.Meters) hqMeters.Add(mt.ItemMeterKey);
                        MarkAddedLater(db, row.LocalKey, hqMeters);
                    }
                    return r.Error;
                }
                if (ch.Kind == IbChange.METER_ADDED)
                {
                    ScpTakeResult r = ScpInterBillTake.AddMeter(db, book, cs, row.LocalKey, ch.LocalParentKey, ch.Meter, book.MarginPercent);
                    if (r.Error.Length == 0) MarkAddedLater(db, row.LocalKey, new List<long>(new long[] { ch.SourceKey }));
                    return r.Error;
                }
                if (ch.Kind == IbChange.MACHINE_GONE)
                {
                    db.ExecuteNonQuery(
                        "UPDATE i SET i.Inactive = 'Y', i.LastModified = GETDATE() FROM dbo.zSCP2_Item i " +
                        "  JOIN dbo.zSCP2_InterBillLink l ON l.LocalKey = i.ItemKey AND l.EntityType = 'ITEM' " +
                        " WHERE l.LinkKey = " + ch.LinkKey);
                    ScpInterBillLinks.Notice(db, ch.LinkKey, ScpInterBillLink.GONE, "off hire in " + book.Alias);
                    return "";
                }
                if (ch.Kind == IbChange.METER_GONE || ch.Kind == IbChange.CONTRACT_GONE)
                {
                    ScpInterBillLinks.Notice(db, ch.LinkKey, ScpInterBillLink.GONE, "no longer in " + book.Alias);
                    return "";
                }
                if (ch.Kind == IbChange.METER_TERMS)
                {
                    // HQ's terms onto this book's counters, through this book's margin; the snapshot
                    // moves on so the same change is not offered again.
                    foreach (IbChange p in ch.Parts)
                    {
                        string err = ScpInterBillTake.ApplyMeterTerms(db, p.LocalParentKey, p.Meter, book.MarginPercent, p.PricesToo);
                        if (err.Length > 0) return p.Which + ": " + err;
                        ScpInterBillLinks.Accept(db, p.LinkKey, p.NewSnapshot);
                    }
                    return "";
                }
                ScpInterBillLinks.Accept(db, ch.LinkKey, ch.NewSnapshot);
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>A machine or counter added to a contract now was not on it in the months already billed
        /// before this one: its counters are stamped ADDED LATER for those months, so a finished month
        /// does not reopen for it. A month not billed yet, and this month, bill it as usual.</summary>
        private static void MarkAddedLater(DBSetting db, long contractKey, List<long> hqMeterKeys)
        {
            if (hqMeterKeys == null || hqMeterKeys.Count == 0) return;
            List<string> keys = new List<string>();
            foreach (long k in hqMeterKeys) keys.Add(k.ToString(CultureInfo.InvariantCulture));
            int thisMonth = ScpBillingSequence.Period(DateTime.Today);
            db.ExecuteNonQuery(
                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, InvoicedDocNo, InvoicedAt, LastModified) " +
                "SELECT l.LocalKey, p.PeriodYear, p.PeriodMonth, 0, GETDATE(), 'INTERBILL', 'N', 'ADDED LATER', GETDATE(), GETDATE() " +
                "  FROM dbo.zSCP2_InterBillLink l " +
                "  CROSS JOIN (SELECT DISTINCT e.PeriodYear, e.PeriodMonth FROM dbo.zSCP2_MeterEntry e " +
                "                JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "                JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "               WHERE i.ContractKey = " + contractKey + " AND ISNULL(e.InvoicedDocNo,'') <> '' " +
                "                 AND e.PeriodYear * 100 + e.PeriodMonth < " + thisMonth + ") p " +
                " WHERE l.EntityType = 'METER' AND l.LocalKey > 0 AND l.RootContractKey = " + contractKey +
                "   AND l.SourceKey IN (" + string.Join(",", keys.ToArray()) + ") " +
                "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry x WHERE x.ItemMeterKey = l.LocalKey " +
                "                      AND x.PeriodYear = p.PeriodYear AND x.PeriodMonth = p.PeriodMonth)");
        }

        /// <summary>Carries out Ignore: an offer is declined for good; a removal is noted and the
        /// machine keeps billing here.</summary>
        public static string Ignore(DBSetting db, ScpInterBillBook book, IbContractRow row, IbChange ch)
        {
            try
            {
                if (ch.Kind == IbChange.MACHINE_ADDED || ch.Kind == IbChange.METER_ADDED)
                {
                    ScpInterBillLinks.Decline(db, book,
                        ch.Kind == IbChange.MACHINE_ADDED ? ScpInterBillLink.ITEM : ScpInterBillLink.METER,
                        row.LocalKey, ch.SourceKey, ch.Which, "not wanted on " + row.LocalNo);
                    return "";
                }
                if (ch.Kind == IbChange.MACHINE_GONE)
                {
                    ScpInterBillLinks.Notice(db, ch.LinkKey, ScpInterBillLink.GONE, "kept on " + row.LocalNo);
                    return "";
                }
                if (ch.Kind == IbChange.METER_TERMS)
                {
                    // "Keep mine": this book's terms stay; HQ's as they are now are noted, so only a
                    // later change at HQ is offered again.
                    foreach (IbChange p in ch.Parts) ScpInterBillLinks.Accept(db, p.LinkKey, p.NewSnapshot);
                    return "";
                }
                ScpInterBillLinks.Accept(db, ch.LinkKey, ch.NewSnapshot);
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>Takes one of their contracts for a customer of this book.</summary>
        public static ScpTakeResult Take(DBSetting db, ScpInterBillBook book, string cs, IbContractRow row,
                                         string debtorCode, string userId)
        {
            List<ScpRemoteMachine> machines = row.HqMachines;
            if (machines == null || machines.Count == 0) machines = ScpInterBillReader.Machines(cs, row.Remote.ContractKey);
            List<ScpRemoteMachine> live = new List<ScpRemoteMachine>();
            foreach (ScpRemoteMachine m in machines) if (!m.Inactive) live.Add(m);
            return ScpInterBillTake.TakeContract(db, book, cs, row.Remote, live, debtorCode, "",
                row.Remote.Description, row.Remote.StartDate, row.Remote.ExpiryDate, userId, book.MarginPercent);
        }

        // ───────────────────────────── helpers ─────────────────────────────

        public static string SourceText(string source, DateTime? on)
        {
            string s = (source ?? "").Trim().ToUpperInvariant();
            string w = s == "MANUAL" ? "Keyed" : s == "ONLINE" ? "PUMS" : s == "OFFLINE" ? "PUMS (offline)"
                     : s == "INVOICE" ? "Invoice" : s == "INTERBILL" ? "Other book" : s.Length == 0 ? "HQ" : s;
            return on.HasValue ? w + " · " + on.Value.ToString("dd MMM", CultureInfo.InvariantCulture) : w;
        }

        private static string CounterList(ScpRemoteMachine m)
        {
            List<string> parts = new List<string>();
            foreach (ScpRemoteMeter t in m.Meters) parts.Add(RoleWord(t.MeterRole, t.MeterTypeCode));
            return string.Join(" · ", parts.ToArray());
        }

        private static string OpeningReadings(ScpRemoteMachine m)
        {
            List<string> parts = new List<string>();
            foreach (ScpRemoteMeter t in m.Meters)
                if (t.InitialReading > 0m) parts.Add(t.MeterTypeCode + " " + t.InitialReading.ToString("#,##0"));
            return parts.Count == 0 ? "" : string.Join(" · ", parts.ToArray());
        }

        private static string RoleWord(string role, string code)
        {
            string r = (role ?? "").Trim().ToUpperInvariant();
            if (r == "RENTAL") return "Rental";
            if (r == "BK" || r == "CL" || r == "WAIVE" || r == "COMMIT") return r == "WAIVE" ? "Waive" : r == "COMMIT" ? "Min" : r;
            return (code ?? "").Trim();
        }

        private static string DescribeFields(List<string> fields)
        {
            List<string> w = new List<string>();
            foreach (string f in fields)
                w.Add(f == "SERIAL" ? "serial" : f == "MODEL" ? "model" : f == "DESC" ? "description" : f == "ACTIVE" ? "active"
                    : f == "EXPIRY" ? "end date" : f == "NO" ? "number" : f == "DEBTOR" ? "customer" : f == "RATE" ? "HQ price"
                    : f == "MIN" ? "HQ minimum" : f == "FOC" ? "free copies" : f == "REBATE" ? "rebate"
                    : f == "WAIVE" ? "waive" : f == "COLOUR" ? "black/colour" : f == "COUNTS" ? "whose charges count" : f == "TIER" ? "tier price"
                    : f == "RENTAL" ? "rental months" : f.ToLowerInvariant());
            return string.Join(", ", w.ToArray());
        }

        /// <summary>"No item code (2) · COMMIT, WAIVE".</summary>
        private static string NoItemText(IbContractRow r)
        {
            return "No item code (" + r.NoItemCode + ")" +
                   (r.NoItemTypes.Count > 0 ? " · " + string.Join(", ", r.NoItemTypes.ToArray()) : "");
        }

        private static string DescribeNow(List<string> fields, string snapshot)
        {
            Dictionary<string, string> map = ScpInterBillSnapshot.Parse(snapshot);
            List<string> w = new List<string>();
            foreach (string f in fields) w.Add(f.ToLowerInvariant() + " " + ScpInterBillSnapshot.Get(map, f));
            return string.Join(" · ", w.ToArray());
        }

        private static Dictionary<long, string> LocalCountersByItem(DBSetting db, long contractKey)
        {
            Dictionary<long, List<string>> acc = new Dictionary<long, List<string>>();
            DataTable t = db.GetDataTable(
                "SELECT m.ItemKey, ISNULL(m.MeterRole,'') AS MeterRole, ISNULL(m.MeterTypeCode,'') AS MeterTypeCode " +
                "  FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                " WHERE i.ContractKey = " + contractKey + " ORDER BY m.ItemKey, m.ItemMeterKey", false);
            foreach (DataRow r in t.Rows)
            {
                long ik = Convert.ToInt64(r["ItemKey"]);
                List<string> l;
                if (!acc.TryGetValue(ik, out l)) { l = new List<string>(); acc[ik] = l; }
                l.Add(RoleWord(S(r["MeterRole"]), S(r["MeterTypeCode"])));
            }
            Dictionary<long, string> map = new Dictionary<long, string>();
            foreach (KeyValuePair<long, List<string>> kv in acc) map[kv.Key] = string.Join(" · ", kv.Value.ToArray());
            return map;
        }

        private static Dictionary<long, string[]> LocalItems(DBSetting db, long contractKey)
        {
            Dictionary<long, string[]> map = new Dictionary<long, string[]>();
            DataTable t = db.GetDataTable(
                "SELECT ItemKey, ISNULL(ServiceItemNo,'') AS ServiceItemNo, ISNULL(SerialNumber,'') AS SerialNumber, " +
                "       ISNULL(ItemCode,'') AS ItemCode FROM dbo.zSCP2_Item " +
                " WHERE ContractKey = " + contractKey + " AND ISNULL(Inactive,'N') = 'N' ORDER BY Pos, ItemKey", false);
            foreach (DataRow r in t.Rows)
                map[Convert.ToInt64(r["ItemKey"])] = new string[] { S(r["ServiceItemNo"]), S(r["SerialNumber"]), S(r["ItemCode"]) };
            return map;
        }

        private static string LocalContractNo(DBSetting db, long contractKey)
        {
            if (contractKey <= 0) return "";
            object o = db.ExecuteScalar("SELECT ContractNo FROM dbo.zSCP2_Contract WHERE ContractKey = " + contractKey);
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        private static string LocalDebtor(DBSetting db, long contractKey)
        {
            if (contractKey <= 0) return "";
            object o = db.ExecuteScalar("SELECT DebtorCode FROM dbo.zSCP2_Contract WHERE ContractKey = " + contractKey);
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        /// <summary>HQ's customers: the debtor (code, name) of every contract in HQ's book, for the
        /// board's customer filter. Read from HQ's book.</summary>
        public static List<string[]> Customers(string cs, bool includeEnded)
        {
            List<string[]> list = new List<string[]>();
            if (string.IsNullOrEmpty(cs)) return list;
            Dictionary<string, string> byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ScpRemoteContract rc in ScpInterBillReader.Contracts(cs, includeEnded, ""))
            {
                string code = (rc.DebtorCode ?? "").Trim();
                if (code.Length == 0 || byCode.ContainsKey(code)) continue;
                byCode[code] = (rc.DebtorName ?? "").Trim();
            }
            foreach (KeyValuePair<string, string> kv in byCode) list.Add(new string[] { kv.Key, kv.Value });
            list.Sort(delegate (string[] a, string[] b2)
            {
                int n = string.Compare(a[1], b2[1], StringComparison.OrdinalIgnoreCase);
                return n != 0 ? n : string.Compare(a[0], b2[0], StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>A user's saved customer filter: null = all customers.</summary>
        public static HashSet<string> LoadCustomerFilter(DBSetting db, string userId)
        {
            string s = "";
            try { s = ServiceContractPhotocopier.Data.PumsConfig.Get(db, CustomerFilterKey(userId), ""); } catch { }
            s = (s ?? "").Trim();
            if (s.Length == 0) return null;
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in s.Split('|')) if (part.Trim().Length > 0) set.Add(part.Trim());
            return set;
        }

        /// <summary>Saves a user's customer filter; null or empty = all customers.</summary>
        public static void SaveCustomerFilter(DBSetting db, string userId, HashSet<string> codes)
        {
            string v = codes == null || codes.Count == 0 ? "" : string.Join("|", new List<string>(codes).ToArray());
            ServiceContractPhotocopier.Data.PumsConfig.Set(db, CustomerFilterKey(userId), v);
        }

        private static string CustomerFilterKey(string userId)
        {
            return "INTERBILL_CUSTOMERS_" + ((userId ?? "").Trim().Length > 0 ? userId.Trim().ToUpperInvariant() : "ADMIN");
        }

        private static string LocalCustomer(DBSetting db, long contractKey)
        {
            object o = db.ExecuteScalar(
                "SELECT ISNULL(NULLIF(d.CompanyName,''), c.DebtorCode) FROM dbo.zSCP2_Contract c " +
                "  LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode WHERE c.ContractKey = " + contractKey);
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        private static string S(object o) { return o == null || o == DBNull.Value ? "" : o.ToString(); }
        private static decimal Dec(object o)
        {
            if (o == null || o == DBNull.Value) return 0m;
            decimal d; return decimal.TryParse(o.ToString(), out d) ? d : 0m;
        }
        private static long D64(object o)
        {
            if (o == null || o == DBNull.Value) return 0L;
            long v; return long.TryParse(o.ToString(), out v) ? v : 0L;
        }
    }
}
