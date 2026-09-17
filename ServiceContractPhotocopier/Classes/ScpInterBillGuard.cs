using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// "This counter comes from the other book" -- asked wherever something could remove one.
    ///
    /// <para>The rule it enforces is one line long: <b>a machine or a counter taken from another book
    /// may not be deleted here.</b> They are the other company's equipment. What this book may do is
    /// price them, group them, split them across invoices and add counters of its own; what it may
    /// not do is decide that a machine the other company still has on hire no longer exists.</para>
    ///
    /// <para>It is asked in three places, and that is deliberate. Greying a control does not stop a
    /// save from writing -- this product has proved that more than once -- so the tick refuses, the
    /// minus refuses, and the save refuses again as the last gate. Each says the same reason.</para>
    ///
    /// <para>An ordinary contract has no links, so every question here answers "no" off an index seek
    /// and nothing anywhere behaves differently. That is the point: the guard costs a book that does
    /// not use inter-billing nothing at all.</para>
    /// </summary>
    public static class ScpInterBillGuard
    {
        /// <summary>Is this contract billed off another book's machines?</summary>
        public static bool ContractIsSourced(DBSetting db, long contractKey)
        {
            return ScpInterBillLinks.ContractLink(db, contractKey) != null;
        }

        /// <summary>The name of the book a contract's machines came from, for a message a person can
        /// act on. Empty when the contract is this book's own from top to bottom.</summary>
        public static string SourceName(DBSetting db, long contractKey)
        {
            ScpInterBillLink l = ScpInterBillLinks.ContractLink(db, contractKey);
            if (l == null) return "";
            ScpInterBillBook b = ScpInterBillBooks.ByBookId(db, l.SourceBookId);
            if (b == null) return "the other account book";
            return b.Alias.Length > 0 ? b.Alias : b.DatabaseName;
        }

        /// <summary>Is this machine one taken from another book?</summary>
        public static bool ItemIsSourced(DBSetting db, long itemKey)
        {
            ScpInterBillLink l = ScpInterBillLinks.Of(db, ScpInterBillLink.ITEM, itemKey);
            return l != null && l.LocalKey > 0;
        }

        /// <summary>
        /// The counters of one machine that came from another book, named the way the screens name
        /// them: <c>TYPE|SERIAL</c>, upper case.
        ///
        /// <para>Not by key, because the Counters panel does not hold one. Its rows are matched on
        /// meter type plus machine serial -- that is how the contract save itself pairs a screen row
        /// with a stored counter -- so the guard has to speak the same language or it would guard
        /// rows the save was never going to touch.</para>
        /// </summary>
        public static HashSet<string> OwnedMeterTags(DBSetting db, long itemKey)
        {
            HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (db == null || itemKey <= 0) return tags;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, " +
                    "       ISNULL(m.MachineSerialNo,'') AS MachineSerialNo " +
                    "  FROM dbo.zSCP2_InterBillLink l " +
                    "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = l.LocalKey " +
                    " WHERE l.EntityType = 'METER' AND l.LocalKey > 0 AND l.Status = 'LIVE' " +
                    "   AND m.ItemKey = " + itemKey, false);
                foreach (DataRow r in t.Rows)
                    tags.Add(Tag(Convert.ToString(r["MeterTypeCode"]),
                                 Convert.ToString(r["MachineSerialNo"])));
            }
            catch { }   // no table, no links: nothing is owned elsewhere
            return tags;
        }

        /// <summary>The same, for every machine on a contract at once -- what a save needs.</summary>
        public static Dictionary<long, HashSet<string>> OwnedMeterTagsByItem(DBSetting db, long contractKey)
        {
            Dictionary<long, HashSet<string>> map = new Dictionary<long, HashSet<string>>();
            if (db == null || contractKey <= 0) return map;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT m.ItemKey, ISNULL(m.MeterTypeCode,'') AS MeterTypeCode, " +
                    "       ISNULL(m.MachineSerialNo,'') AS MachineSerialNo " +
                    "  FROM dbo.zSCP2_InterBillLink l " +
                    "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = l.LocalKey " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    " WHERE l.EntityType = 'METER' AND l.LocalKey > 0 AND l.Status = 'LIVE' " +
                    "   AND i.ContractKey = " + contractKey, false);
                foreach (DataRow r in t.Rows)
                {
                    long ik = Convert.ToInt64(r["ItemKey"]);
                    HashSet<string> set;
                    if (!map.TryGetValue(ik, out set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        map[ik] = set;
                    }
                    set.Add(Tag(Convert.ToString(r["MeterTypeCode"]),
                                Convert.ToString(r["MachineSerialNo"])));
                }
            }
            catch { }
            return map;
        }

        /// <summary>How a counter is named for this comparison. Type and serial together, because a
        /// multi-machine item carries the same meter type once per serial.</summary>
        public static string Tag(string meterTypeCode, string machineSerialNo)
        {
            return (meterTypeCode ?? "").Trim().ToUpperInvariant() + "|" +
                   (machineSerialNo ?? "").Trim().ToUpperInvariant();
        }

        /// <summary>
        /// The last gate: which of a machine's borrowed counters are about to disappear.
        ///
        /// <para>Given the rows as they now stand on screen, returns the counters that came from the
        /// other book and are no longer among them. An empty list means the save may proceed.</para>
        /// </summary>
        public static List<string> VanishedOwnedMeters(HashSet<string> owned, DataTable onScreen)
        {
            List<string> gone = new List<string>();
            if (owned == null || owned.Count == 0) return gone;

            HashSet<string> present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (onScreen != null)
            {
                foreach (DataRow r in onScreen.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string type = r.Table.Columns.Contains("MeterTypeCode")
                        ? Convert.ToString(r["MeterTypeCode"]) : "";
                    string serial = r.Table.Columns.Contains("MachineSerialNo")
                        ? Convert.ToString(r["MachineSerialNo"]) : "";
                    if (type.Trim().Length == 0) continue;
                    present.Add(Tag(type, serial));
                }
            }

            foreach (string tag in owned)
            {
                if (present.Contains(tag)) continue;
                int bar = tag.IndexOf('|');
                string type = bar > 0 ? tag.Substring(0, bar) : tag;
                string serial = bar >= 0 && bar + 1 < tag.Length ? tag.Substring(bar + 1) : "";
                gone.Add(serial.Length > 0 ? type + "  on  " + serial : type);
            }
            return gone;
        }

        /// <summary>The sentence every refusal says. One wording in one place, so the tick, the minus
        /// and the save cannot end up explaining the same rule three different ways.</summary>
        public static string RefusalText(string bookName, string what)
        {
            string who = (bookName ?? "").Trim().Length > 0 ? bookName.Trim() : "the other account book";
            return what + " comes from " + who + "." + Environment.NewLine + Environment.NewLine +
                   "Which machines are on the contract, and which counters are on them, is theirs to " +
                   "decide -- it is their equipment. What is yours is the money: prices, minimums, " +
                   "waives, bill groups and how the invoice is split." + Environment.NewLine +
                   Environment.NewLine +
                   "If they have taken it off hire, it will show up under Inter-Billing > What they " +
                   "changed, and you can stop billing it there.";
        }
    }
}
