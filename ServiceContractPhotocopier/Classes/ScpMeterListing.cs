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

        /// <summary>
        /// The log row describes an invoice that is STILL in the book.
        ///
        /// <para>The log is an audit trail and nothing is ever taken out of it: generate a month,
        /// delete the invoice, generate it again, and the meter keeps every attempt, with a separate
        /// INVOICE-DELETED row recording each undo. Summing all of them is summing invoices that no
        /// longer exist -- one machine here carried SIX INVOICE rows for September 2026 and the
        /// listing charged 106.25 where the invoice that stands says 18.75. The listing is a picture
        /// of what was billed, so it counts only the rows whose document is still there.</para>
        /// </summary>
        private const string STILL_BILLED =
            "EXISTS (SELECT 1 FROM dbo.IV iv2 WHERE RTRIM(iv2.DocNo) = RTRIM(g.DocNo) " +
            "          AND ISNULL(iv2.Cancelled,'F') <> 'T')";

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
                "g.Source = 'INVOICE' AND g.PeriodYear = " + year + " AND g.PeriodMonth = " + month +
                " AND " + STILL_BILLED;
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
                "  MAX(CASE WHEN m.MeterRole = 'BK' THEN g.RebatePct END)    AS RebateBK, " +
                "  MAX(CASE WHEN m.MeterRole = 'CL' THEN g.RebatePct END)    AS RebateCL, " +
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
            decimal cFocBK = 0m, cFocCL = 0m, cRebBK = 0m, cRebCL = 0m;
            decimal cNetBK = 0m, cNetCL = 0m, cBK = 0m, cCL = 0m;
            decimal cCurBK = 0m, cCurCL = 0m, cPrvBK = 0m, cPrvCL = 0m;
            string cInv = "", cDebtor = "", cCust = "", cBranchless = "";
            DataRow combineAnchor = null;

            foreach (DataRow s in src.Rows)
            {
                string contract = Str(s["ContractNo"]);
                if (curContract != null && contract != curContract)
                {
                    AppendCombine(outT, curContract, cDebtor, cCust, cBranchless, cRental, cGrand, cInv,
                        year, month, monthEnd, cCurBK, cCurCL, cPrvBK, cPrvCL,
                        cFocBK, cFocCL, cRebBK, cRebCL, cNetBK, cNetCL, cBK, cCL);
                    cRental = 0m; cGrand = 0m; cInv = "";
                    cFocBK = 0m; cFocCL = 0m; cRebBK = 0m; cRebCL = 0m;
                    cNetBK = 0m; cNetCL = 0m; cBK = 0m; cCL = 0m;
                    cCurBK = 0m; cCurCL = 0m; cPrvBK = 0m; cPrvCL = 0m;
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
                // Their Appendix A has a Fixed Rebate QUANTITY; the deal is written as a PERCENT.
                // The two say the same thing and convert exactly -- 2% of 2,900 copies IS 58 copies --
                // so the percent is printed as the copies it comes to, and taken off NET. Leaving the
                // column at 0 was making the customer's own sheet not add up: 2,900 x 0.0243 = 70.47
                // against 69.06 charged, with nothing on the page to explain the difference.
                decimal rebBK = RebateQty(Net(curBK, prvBK, focBK), Dec(s["RebateBK"]));
                decimal rebCL = RebateQty(Net(curCL, prvCL, focCL), Dec(s["RebateCL"]));
                r["FixedRebateBKQty"] = rebBK;
                r["FixedRebateCLQty"] = rebCL;
                r["NetBK"] = Net(curBK, prvBK, focBK + rebBK);
                r["NetCL"] = Net(curCL, prvCL, focCL + rebCL);
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
                cCurBK += curBK; cCurCL += curCL;
                cPrvBK += prvBK; cPrvCL += prvCL;
                cFocBK += focBK; cFocCL += focCL;
                cRebBK += rebBK; cRebCL += rebCL;
                cNetBK += Dec(r["NetBK"]); cNetCL += Dec(r["NetCL"]);
                cBK += bkChg; cCL += clChg;
                if (cInv.Length == 0) cInv = Str(s["InvNo"]);
                cDebtor = Str(s["DebtorCode"]);
                cCust = Str(s["CustomerName"]);
                cBranchless = Str(s["Branch"]);
                combineAnchor = r;
            }
            if (curContract != null && combineAnchor != null)
                AppendCombine(outT, curContract, cDebtor, cCust, cBranchless, cRental, cGrand, cInv,
                    year, month, monthEnd, cCurBK, cCurCL, cPrvBK, cPrvCL,
                    cFocBK, cFocCL, cRebBK, cRebCL, cNetBK, cNetCL, cBK, cCL);
            return outT;
        }

        /// <summary>
        /// The ".C" row that closes each contract: its rental, the money actually invoiced, and the
        /// TOTAL of the machines above it.
        ///
        /// <para>Every counted column adds up, the reading columns included. A column of readings
        /// totalled is not a reading of anything, but the SUBTRACTION still holds and that is what the
        /// row is for: 471,000 − 460,500 − 100 free − 58 rebate = 10,342, the same NET the machines
        /// come to. Only the two rate columns stay blank — three machines on three rates have no
        /// single rate, and 0.0243 + 0.0243 + 0.0243 is not one.</para>
        /// </summary>
        private static void AppendCombine(DataTable outT, string contractNo, string debtor, string customer,
            string branch, decimal rental, decimal grand, string invNo, int year, int month, DateTime monthEnd,
            decimal curBK, decimal curCL, decimal prvBK, decimal prvCL,
            decimal focBK, decimal focCL, decimal rebBK, decimal rebCL,
            decimal netBK, decimal netCL, decimal bkCharges, decimal clCharges)
        {
            DataRow r = outT.NewRow();
            r["CSSI"] = contractNo + ".C";
            r["Status"] = "ACTIVE";
            r["Branch"] = branch;
            r["Model"] = ""; r["SerialNo"] = ""; r["ServiceVoucher"] = "COMBINE";
            r["CurrentTotalBK"] = curBK; r["CurrentTotalCL"] = curCL;
            r["PreviousTotalBK"] = prvBK; r["PreviousTotalCL"] = prvCL;
            r["FOCBK"] = focBK; r["FOCCL"] = focCL;
            r["RateBK"] = 0m; r["RateCL"] = 0m;          // no single rate covers the machines
            r["FixedRebateBKQty"] = rebBK; r["FixedRebateCLQty"] = rebCL;
            r["NetBK"] = netBK; r["NetCL"] = netCL;
            r["BKCharges"] = bkCharges; r["CLCharges"] = clCharges;
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
                    "WHERE g.Source = 'INVOICE' AND " + STILL_BILLED + " " +
                    "AND (g.PeriodYear * 100 + g.PeriodMonth) < " + (year * 100 + month) + " " +
                    "GROUP BY m.ItemKey", false);
                foreach (DataRow r in t.Rows)
                    if (r["PrevDate"] != DBNull.Value)
                        map[Convert.ToInt64(r["ItemKey"])] = Convert.ToDateTime(r["PrevDate"]);
            }
            catch { }
            return map;
        }

        /// <summary>A rebate percent said as the copies it takes off. Not rounded: the sheet has to
        /// multiply out to the money that was actually charged.</summary>
        private static decimal RebateQty(decimal net, decimal rebatePct)
        {
            if (rebatePct <= 0m || net <= 0m) return 0m;
            return net * rebatePct / 100m;
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
