using System;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// "Summary sales invoice meter listing" (the customer's Appendix A): one row per CSSI for a
    /// billing month, showing the readings, free copies, rates and charges that produced that
    /// month's invoice — plus one ".C COMBINE" row per contract carrying the rental and the invoice
    /// total, exactly as their own spreadsheet lays it out.
    ///
    /// Everything comes from zSCP2_MeterReadingLog rows stamped Source='INVOICE'. That log is
    /// written AT GENERATION TIME with the reading, last reading, usage, FOC, unit price and charge
    /// actually used, so the listing reproduces what was billed even after a rate or a FOC quantity
    /// is edited on the master later. Reading it back from today's master data is exactly the
    /// mistake that made the FOC numbers look wrong on 07/08.
    /// </summary>
    public static class ScpMeterListing
    {
        /// <summary>The report type these designs are saved under. AutoCount needs no registration
        /// for a custom type — NewReport falls back to BaseReport when no handler is found.</summary>
        public const string REPORT_TYPE = "Summary Sales Invoice Meter Listing";

        public static DataTable NewTable()
        {
            DataTable dt = new DataTable("MeterListing");
            dt.Columns.Add("CSSI", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("Branch", typeof(string));
            dt.Columns.Add("Model", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("ServiceVoucher", typeof(string));
            dt.Columns.Add("CurrentServiceDate", typeof(DateTime));
            dt.Columns.Add("CurrentTotalBK", typeof(decimal));
            dt.Columns.Add("CurrentTotalCL", typeof(decimal));
            dt.Columns.Add("PreviousServiceDate", typeof(DateTime));
            dt.Columns.Add("PreviousTotalBK", typeof(decimal));
            dt.Columns.Add("PreviousTotalCL", typeof(decimal));
            dt.Columns.Add("FOCBK", typeof(decimal));
            dt.Columns.Add("FOCCL", typeof(decimal));
            dt.Columns.Add("RateBK", typeof(decimal));
            dt.Columns.Add("RateCL", typeof(decimal));
            dt.Columns.Add("FixedRebateBKQty", typeof(decimal));
            dt.Columns.Add("FixedRebateCLQty", typeof(decimal));
            dt.Columns.Add("NetBK", typeof(decimal));
            dt.Columns.Add("NetCL", typeof(decimal));
            dt.Columns.Add("BKCharges", typeof(decimal));
            dt.Columns.Add("CLCharges", typeof(decimal));
            dt.Columns.Add("RentalAmt", typeof(decimal));
            dt.Columns.Add("GrandTotal", typeof(decimal));
            dt.Columns.Add("InvNo", typeof(string));
            // Grouping / header context — the report design binds its title block to these.
            dt.Columns.Add("DebtorCode", typeof(string));
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("ContractNo", typeof(string));
            dt.Columns.Add("PeriodYear", typeof(int));
            dt.Columns.Add("PeriodMonth", typeof(int));
            dt.Columns.Add("MonthEnd", typeof(DateTime));   // "Month: 31-Jul-26"
            dt.Columns.Add("IsCombineRow", typeof(bool));   // the ".C" contract total row
            return dt;
        }

        /// <summary>
        /// Build the listing for one billing month. <paramref name="debtorFrom"/>/<paramref name="debtorTo"/>
        /// are inclusive and may be empty for "all"; <paramref name="contractNo"/> narrows to one contract.
        /// </summary>
        public static DataTable Build(DBSetting db, int year, int month,
            string debtorFrom, string debtorTo, string contractNo)
        {
            DataTable outT = NewTable();
            if (db == null || year <= 0 || month < 1 || month > 12) return outT;

            string where =
                "g.Source = 'INVOICE' AND g.PeriodYear = " + year + " AND g.PeriodMonth = " + month;
            if (!string.IsNullOrEmpty(debtorFrom))
                where += " AND c.DebtorCode >= '" + Esc(debtorFrom) + "'";
            if (!string.IsNullOrEmpty(debtorTo))
                where += " AND c.DebtorCode <= '" + Esc(debtorTo) + "'";
            if (!string.IsNullOrEmpty(contractNo))
                where += " AND c.ContractNo = '" + Esc(contractNo) + "'";

            // BK / CL live on separate log rows; pivot them onto one row per machine. Anything that
            // is not a usage meter (rental, waive, committed minimum) is money without a reading —
            // it aggregates into Rental Amt.
            string sql =
                "SELECT i.ItemKey, i.ServiceItemNo AS CSSI, " +
                "  CASE WHEN ISNULL(i.Inactive,'N') = 'Y' THEN 'INACTIVE' ELSE 'ACTIVE' END AS [Status], " +
                "  LTRIM(RTRIM(ISNULL(NULLIF(i.DelBranchName,''), ISNULL(i.DelBranchCode,'')))) AS Branch, " +
                "  ISNULL(NULLIF(i.ItemCode,''), ISNULL(i.Description,'')) AS Model, " +
                "  ISNULL(i.SerialNumber,'') AS SerialNo, " +
                "  c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CustomerName, " +
                "  MAX(CASE WHEN m.MeterRole = 'BK' THEN g.Reading END)      AS CurrentTotalBK, " +
                "  MAX(CASE WHEN m.MeterRole = 'CL' THEN g.Reading END)      AS CurrentTotalCL, " +
                "  MAX(CASE WHEN m.MeterRole = 'BK' THEN g.LastReading END)  AS PreviousTotalBK, " +
                "  MAX(CASE WHEN m.MeterRole = 'CL' THEN g.LastReading END)  AS PreviousTotalCL, " +
                "  MAX(CASE WHEN m.MeterRole = 'BK' THEN g.FOCQty END)       AS FOCBK, " +
                "  MAX(CASE WHEN m.MeterRole = 'CL' THEN g.FOCQty END)       AS FOCCL, " +
                "  MAX(CASE WHEN m.MeterRole = 'BK' THEN g.UnitPrice END)    AS RateBK, " +
                "  MAX(CASE WHEN m.MeterRole = 'CL' THEN g.UnitPrice END)    AS RateCL, " +
                "  SUM(CASE WHEN m.MeterRole = 'BK' THEN ISNULL(g.Charge,0) ELSE 0 END) AS BKCharges, " +
                "  SUM(CASE WHEN m.MeterRole = 'CL' THEN ISNULL(g.Charge,0) ELSE 0 END) AS CLCharges, " +
                "  SUM(CASE WHEN ISNULL(m.MeterRole,'') NOT IN ('BK','CL') THEN ISNULL(g.Charge,0) ELSE 0 END) AS RentalAmt, " +
                "  MAX(g.ReadingDate) AS CurrentServiceDate, " +
                "  MAX(g.DocNo) AS InvNo, " +
                "  MAX(ISNULL(e.TrackingId,'')) AS ServiceVoucher " +
                "FROM dbo.zSCP2_MeterReadingLog g " +
                "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = g.ItemMeterKey " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                "OUTER APPLY (SELECT TOP 1 e2.TrackingId FROM dbo.zSCP2_MeterEntry e2 " +
                "   WHERE e2.ItemMeterKey = g.ItemMeterKey AND e2.PeriodYear = g.PeriodYear " +
                "     AND e2.PeriodMonth = g.PeriodMonth AND ISNULL(e2.TrackingId,'') <> '') e " +
                "WHERE " + where + " " +
                "GROUP BY i.ItemKey, i.ServiceItemNo, i.Inactive, i.DelBranchName, i.DelBranchCode, " +
                "  i.ItemCode, i.Description, i.SerialNumber, c.ContractNo, c.DebtorCode, d.CompanyName " +
                "ORDER BY c.DebtorCode, c.ContractNo, i.ServiceItemNo";

            DataTable src;
            try { src = db.GetDataTable(sql, false); }
            catch { return outT; }

            DateTime monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            // Previous service date = the reading date of the SAME meters one period earlier. Read
            // once per machine rather than per row: an OUTER APPLY inside the pivot above would fire
            // for every meter and still need collapsing.
            System.Collections.Generic.Dictionary<long, DateTime> prevDates = LoadPreviousDates(db, year, month);

            string curContract = null;
            decimal cRental = 0m, cGrand = 0m;
            string cInv = "", cDebtor = "", cCust = "", cBranchless = "";
            DataRow combineAnchor = null;

            foreach (DataRow s in src.Rows)
            {
                string contract = Str(s["ContractNo"]);
                if (curContract != null && contract != curContract)
                {
                    AppendCombine(outT, curContract, cDebtor, cCust, cBranchless, cRental, cGrand, cInv,
                        year, month, monthEnd);
                    cRental = 0m; cGrand = 0m; cInv = "";
                }
                curContract = contract;

                DataRow r = outT.NewRow();
                r["CSSI"] = Str(s["CSSI"]);
                r["Status"] = Str(s["Status"]);
                r["Branch"] = Str(s["Branch"]);
                r["Model"] = Str(s["Model"]);
                r["SerialNo"] = Str(s["SerialNo"]);
                r["ServiceVoucher"] = Str(s["ServiceVoucher"]);
                if (s["CurrentServiceDate"] != DBNull.Value) r["CurrentServiceDate"] = s["CurrentServiceDate"];
                long itemKey = Convert.ToInt64(s["ItemKey"]);
                DateTime pd;
                if (prevDates.TryGetValue(itemKey, out pd)) r["PreviousServiceDate"] = pd;

                decimal curBK = Dec(s["CurrentTotalBK"]), curCL = Dec(s["CurrentTotalCL"]);
                decimal prvBK = Dec(s["PreviousTotalBK"]), prvCL = Dec(s["PreviousTotalCL"]);
                decimal focBK = Dec(s["FOCBK"]), focCL = Dec(s["FOCCL"]);
                r["CurrentTotalBK"] = curBK; r["CurrentTotalCL"] = curCL;
                r["PreviousTotalBK"] = prvBK; r["PreviousTotalCL"] = prvCL;
                r["FOCBK"] = focBK; r["FOCCL"] = focCL;
                r["RateBK"] = Dec(s["RateBK"]); r["RateCL"] = Dec(s["RateCL"]);
                // Their Appendix A has a Fixed Rebate QUANTITY. We only hold a rebate PERCENT today,
                // which is a different field — carried as 0 rather than silently passing off a
                // percentage as a quantity. Flagged for Jean.
                r["FixedRebateBKQty"] = 0m;
                r["FixedRebateCLQty"] = 0m;
                r["NetBK"] = Net(curBK, prvBK, focBK);
                r["NetCL"] = Net(curCL, prvCL, focCL);
                decimal bkChg = Dec(s["BKCharges"]), clChg = Dec(s["CLCharges"]), rental = Dec(s["RentalAmt"]);
                r["BKCharges"] = bkChg; r["CLCharges"] = clChg;
                r["RentalAmt"] = 0m;      // rental rolls up to the contract's COMBINE row
                r["GrandTotal"] = 0m;     // ditto — the machine rows carry the meter detail only
                r["InvNo"] = "";
                r["DebtorCode"] = Str(s["DebtorCode"]);
                r["CustomerName"] = Str(s["CustomerName"]);
                r["ContractNo"] = contract;
                r["PeriodYear"] = year; r["PeriodMonth"] = month; r["MonthEnd"] = monthEnd;
                r["IsCombineRow"] = false;
                outT.Rows.Add(r);

                cRental += rental;
                cGrand += bkChg + clChg + rental;
                if (cInv.Length == 0) cInv = Str(s["InvNo"]);
                cDebtor = Str(s["DebtorCode"]);
                cCust = Str(s["CustomerName"]);
                cBranchless = Str(s["Branch"]);
                combineAnchor = r;
            }
            if (curContract != null && combineAnchor != null)
                AppendCombine(outT, curContract, cDebtor, cCust, cBranchless, cRental, cGrand, cInv,
                    year, month, monthEnd);
            return outT;
        }

        /// <summary>The ".C" row: the contract's rental and the money actually invoiced, exactly as
        /// the customer's own sheet closes each contract.</summary>
        private static void AppendCombine(DataTable outT, string contractNo, string debtor, string customer,
            string branch, decimal rental, decimal grand, string invNo, int year, int month, DateTime monthEnd)
        {
            DataRow r = outT.NewRow();
            r["CSSI"] = contractNo + ".C";
            r["Status"] = "ACTIVE";
            r["Branch"] = branch;
            r["Model"] = ""; r["SerialNo"] = ""; r["ServiceVoucher"] = "COMBINE";
            r["CurrentTotalBK"] = 0m; r["CurrentTotalCL"] = 0m;
            r["PreviousTotalBK"] = 0m; r["PreviousTotalCL"] = 0m;
            r["FOCBK"] = 0m; r["FOCCL"] = 0m; r["RateBK"] = 0m; r["RateCL"] = 0m;
            r["FixedRebateBKQty"] = 0m; r["FixedRebateCLQty"] = 0m;
            r["NetBK"] = 0m; r["NetCL"] = 0m; r["BKCharges"] = 0m; r["CLCharges"] = 0m;
            r["RentalAmt"] = rental;
            r["GrandTotal"] = grand;
            r["InvNo"] = invNo;
            r["DebtorCode"] = debtor; r["CustomerName"] = customer; r["ContractNo"] = contractNo;
            r["PeriodYear"] = year; r["PeriodMonth"] = month; r["MonthEnd"] = monthEnd;
            r["IsCombineRow"] = true;
            outT.Rows.Add(r);
        }

        /// <summary>Reading date of each machine's PREVIOUS invoiced period (the "Previous Service
        /// Date" column). Keyed by ItemKey; machines billed for the first time are simply absent.</summary>
        private static System.Collections.Generic.Dictionary<long, DateTime> LoadPreviousDates(
            DBSetting db, int year, int month)
        {
            System.Collections.Generic.Dictionary<long, DateTime> map =
                new System.Collections.Generic.Dictionary<long, DateTime>();
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT m.ItemKey, MAX(g.ReadingDate) AS PrevDate " +
                    "FROM dbo.zSCP2_MeterReadingLog g " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = g.ItemMeterKey " +
                    "WHERE g.Source = 'INVOICE' " +
                    "AND (g.PeriodYear * 100 + g.PeriodMonth) < " + (year * 100 + month) + " " +
                    "GROUP BY m.ItemKey", false);
                foreach (DataRow r in t.Rows)
                    if (r["PrevDate"] != DBNull.Value)
                        map[Convert.ToInt64(r["ItemKey"])] = Convert.ToDateTime(r["PrevDate"]);
            }
            catch { }
            return map;
        }

        private static decimal Net(decimal current, decimal previous, decimal foc)
        {
            decimal n = current - previous - foc;
            return n < 0m ? 0m : n;
        }

        private static decimal Dec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }
        private static string Str(object v) { return v == null || v == DBNull.Value ? "" : Convert.ToString(v); }
        private static string Esc(string s) { return (s ?? "").Replace("'", "''"); }
    }
}
