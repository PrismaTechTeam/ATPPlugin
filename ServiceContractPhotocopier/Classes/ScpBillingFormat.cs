using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// A named invoice layout — the eleven the customer's own July-2026 invoices use — and the one
    /// place that knows what its three answers mean.
    ///
    /// <para>The layout of an invoice is three independent questions:
    /// how many invoices come out, how the rental lines collapse, how the BK/CL lines collapse.
    /// Twenty-one customers use eleven of the nineteen reachable combinations, so they are stored
    /// once as presets (<c>zSCP2_BillingFormat</c>) rather than re-described on every contract.</para>
    ///
    /// <para><b>The contract stays the source of truth.</b> Choosing a format COPIES its values into
    /// <c>zSCP2_Contract</c> (BillingMode, RentalSeparateInvoice, RentalLineMode, MeterLineMode).
    /// The billing engine reads only the contract and never loads this class, so a contract can be
    /// nudged off its format without dragging its siblings along, and nothing in the generate path
    /// depends on the preset table existing.</para>
    /// </summary>
    public class ScpBillingFormat
    {
        // ----- line modes (contract RentalLineMode / MeterLineMode) -----

        /// <summary>Merge regardless of model. How many lines come out is decided by the data, not
        /// by the mode: Pasir Gudang's MEDIUM DUTY row merges a C5550i, three 4545i and a C4535i
        /// into one 52,860 because they share a rate of 0.0285, while Pontian's six machines across
        /// two models come out as a single BK line of 74,722.</summary>
        public const char LINE_ACROSS_MODEL = 'A';

        /// <summary>One line per model. MBJB groups 52 machines into 7 meter lines this way, every
        /// BK at the same 0.020 — so the split is by model and could not have come from the rate.
        /// JPJ's three rental lines (1 / 5 / 6 units) match its three models exactly.</summary>
        public const char LINE_SAME_MODEL = 'M';

        /// <summary>No merge — one line per machine. MARA's five machines print ten meter lines,
        /// BK and CL paired per machine.</summary>
        public const char LINE_PER_MACHINE = 'S';

        // ----- invoice split (maps onto the two columns that already carry it) -----

        /// <summary>Everything on one invoice.</summary>
        public const string SPLIT_ONE = "ONE";
        /// <summary>Rental on its own invoice, meters on another.</summary>
        public const string SPLIT_RENTAL_SEPARATE = "RS";
        /// <summary>One invoice per machine, rental and meters together.</summary>
        public const string SPLIT_PER_MACHINE = "PM";
        /// <summary>One rental invoice and one meter invoice per machine. Tangkak: five machines
        /// produced nine invoices in July 2026 (one machine has no rental).</summary>
        public const string SPLIT_PER_MACHINE_SEPARATE = "PMS";

        public string FormatCode = "";
        public string FormatName = "";
        public string InvoiceSplit = SPLIT_ONE;
        public char RentalLineMode = LINE_ACROSS_MODEL;
        public char MeterLineMode = LINE_PER_MACHINE;
        public string ReadingText = "";     // "" = follow the company default
        public string RentalDescTemplate;   // null = follow the company default
        public string MeterDescTemplate;    // null = follow the company default
        public string Remark = "";

        /// <summary>'G' one invoice for the contract / 'S' one per machine — the existing column.</summary>
        public char BillingMode
        {
            get
            {
                return InvoiceSplit == SPLIT_PER_MACHINE || InvoiceSplit == SPLIT_PER_MACHINE_SEPARATE
                    ? 'S' : 'G';
            }
        }

        /// <summary>Whether rental gets its own invoice — the existing column.</summary>
        public bool RentalSeparateInvoice
        {
            get
            {
                return InvoiceSplit == SPLIT_RENTAL_SEPARATE || InvoiceSplit == SPLIT_PER_MACHINE_SEPARATE;
            }
        }

        /// <summary>The format in the words the contract screen shows, e.g.
        /// "2 invoices · rental per model · BK+CL 1 line each".</summary>
        public string Summary()
        {
            return DescribeSplit(InvoiceSplit) + " · rental " + DescribeLineMode(RentalLineMode) +
                   " · BK+CL " + DescribeLineMode(MeterLineMode);
        }

        public static string DescribeSplit(string split)
        {
            if (split == SPLIT_RENTAL_SEPARATE) return "2 invoices";
            if (split == SPLIT_PER_MACHINE) return "1 invoice per machine";
            if (split == SPLIT_PER_MACHINE_SEPARATE) return "2 invoices per machine";
            return "1 invoice";
        }

        public static string DescribeLineMode(char mode)
        {
            if (mode == LINE_SAME_MODEL) return "per model";
            if (mode == LINE_PER_MACHINE) return "per machine";
            return "1 line each";
        }

        /// <summary>Every active format, code order. Never throws — an older book without the table
        /// simply has no presets, and the contract's own columns still drive billing.</summary>
        public static List<ScpBillingFormat> LoadAll(DBSetting db)
        {
            List<ScpBillingFormat> list = new List<ScpBillingFormat>();
            if (db == null) return list;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, " +
                    "ISNULL(ReadingText,'') AS ReadingText, RentalDescTemplate, MeterDescTemplate, " +
                    "ISNULL(Remark,'') AS Remark " +
                    "FROM dbo.zSCP2_BillingFormat WHERE Inactive='N' ORDER BY FormatCode", false);
                foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            }
            catch { }
            return list;
        }

        /// <summary>One format by code, or null when it is missing or inactive.</summary>
        public static ScpBillingFormat Load(DBSetting db, string formatCode)
        {
            if (db == null || string.IsNullOrEmpty(formatCode)) return null;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, " +
                    "ISNULL(ReadingText,'') AS ReadingText, RentalDescTemplate, MeterDescTemplate, " +
                    "ISNULL(Remark,'') AS Remark " +
                    "FROM dbo.zSCP2_BillingFormat WHERE Inactive='N' AND FormatCode=N'" +
                    formatCode.Replace("'", "''") + "'", false);
                if (t.Rows.Count == 0) return null;
                return FromRow(t.Rows[0]);
            }
            catch { return null; }
        }

        private static ScpBillingFormat FromRow(DataRow r)
        {
            ScpBillingFormat f = new ScpBillingFormat();
            f.FormatCode = Str(r["FormatCode"]);
            f.FormatName = Str(r["FormatName"]);
            f.InvoiceSplit = Str(r["InvoiceSplit"]);
            f.RentalLineMode = Chr(r["RentalLineMode"], LINE_ACROSS_MODEL);
            f.MeterLineMode = Chr(r["MeterLineMode"], LINE_PER_MACHINE);
            f.ReadingText = Str(r["ReadingText"]);
            f.RentalDescTemplate = r["RentalDescTemplate"] == DBNull.Value ? null : Convert.ToString(r["RentalDescTemplate"]);
            f.MeterDescTemplate = r["MeterDescTemplate"] == DBNull.Value ? null : Convert.ToString(r["MeterDescTemplate"]);
            f.Remark = Str(r["Remark"]);
            return f;
        }

        /// <summary>
        /// Stamp this format's four answers onto a contract. Deliberately a copy rather than a live
        /// lookup: the engine reads the contract, so an invoice generated last month can still be
        /// explained by what the contract said at the time, and editing a preset does not silently
        /// re-shape invoices for every contract that shares it.
        /// </summary>
        public void ApplyTo(DBSetting db, long contractKey)
        {
            if (db == null || contractKey <= 0) return;
            db.ExecuteNonQuery(
                "UPDATE dbo.zSCP2_Contract SET " +
                "BillingFormatCode = N'" + FormatCode.Replace("'", "''") + "', " +
                "BillingMode = '" + BillingMode + "', " +
                "RentalSeparateInvoice = '" + (RentalSeparateInvoice ? 'Y' : 'N') + "', " +
                "RentalLineMode = '" + RentalLineMode + "', " +
                "MeterLineMode = '" + MeterLineMode + "' " +
                "WHERE ContractKey = " + contractKey);
        }

        /// <summary>How many contracts already use this format — shown in the picker so the effect of
        /// editing a preset is visible before it is edited.</summary>
        public static int UsedByContractCount(DBSetting db, string formatCode)
        {
            if (db == null || string.IsNullOrEmpty(formatCode)) return 0;
            try
            {
                object n = db.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_Contract WHERE BillingFormatCode = N'" +
                    formatCode.Replace("'", "''") + "'");
                return n == null || n == DBNull.Value ? 0 : Convert.ToInt32(n);
            }
            catch { return 0; }
        }

        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        private static char Chr(object o, char fallback)
        {
            string s = Str(o);
            return s.Length == 0 ? fallback : char.ToUpperInvariant(s[0]);
        }
    }
}
