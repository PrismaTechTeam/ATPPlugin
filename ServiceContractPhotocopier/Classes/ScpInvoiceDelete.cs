using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>What a delete did.</summary>
    public sealed class ScpDeleteResult
    {
        public int Deleted;        // invoices removed from AutoCount
        public int CreditNotes;    // correction credit notes removed first
        public int Blockers;       // payments / CNs / refunds knocked off them, removed first
        public int Failed;         // invoices AutoCount refused
        public string Errors = "";
        /// <summary>Later months already invoiced, which is why nothing was deleted.</summary>
        public List<string> Blocked = new List<string>();
        /// <summary>The book's own refusals, in its own words — for an invoice the guard did not
        /// catch before asking, because somebody else changed the book meanwhile.</summary>
        public List<string> Refused = new List<string>();
        public bool WasBlocked { get { return Blocked.Count > 0; } }

        public string Summary
        {
            get
            {
                if (WasBlocked) return ScpInvoiceDelete.BlockedMessage(Blocked);
                StringBuilder sb = new StringBuilder();
                // Refused, and nothing else happened: say only that, in the book's own words.
                if (Deleted == 0 && Failed == 0 && CreditNotes == 0 && Blockers == 0 && Refused.Count > 0)
                {
                    sb.Append(Refused.Count == 1 ? "This invoice was not deleted." : "These invoices were not deleted.");
                    sb.Append("\r\n\r\n");
                    foreach (string s in Refused) sb.Append(s).Append("\r\n");
                    return sb.ToString().TrimEnd();
                }
                sb.Append("Deleted ").Append(Deleted).Append(Deleted == 1 ? " invoice." : " invoices.");
                if (CreditNotes > 0) sb.Append("\r\nRemoved ").Append(CreditNotes).Append(" correction credit note(s) first.");
                if (Blockers > 0) sb.Append("\r\nCleared ").Append(Blockers).Append(" blocking payment/CN/refund first.");
                foreach (string s in Refused) sb.Append("\r\n\r\n").Append(s);
                if (Failed > 0) sb.Append("\r\nFailed: ").Append(Failed).Append("\r\n").Append(Errors);
                // Only ever true of invoices that actually went.
                if (Deleted > 0)
                    sb.Append("\r\nThe meter readings are released — those months can be billed again.");
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Deleting invoices this module generated, and putting the meters back the way they were.
    ///
    /// <para>AutoCount refuses to delete an invoice that carries a knock-off, so every payment,
    /// credit note and refund settled against it goes first -- the whole document, even when it also
    /// settles other invoices, because this is used to undo a billing run rather than to correct
    /// accounts. Afterwards <see cref="ScpBillingRows.ReconcileDeletedInvoices"/> takes the billed
    /// readings and the period stamps back off, so the month reads as unbilled and can be run again.</para>
    /// </summary>
    public static class ScpInvoiceDelete
    {
        /// <summary>Every invoice this module has generated in the book: DocKey and DocNo.</summary>
        public static DataTable Generated(DBSetting db)
        {
            return db.GetDataTable(
                "SELECT DISTINCT iv.DocKey, iv.DocNo, " + BILLED_PERIOD + " AS P FROM dbo.IV iv WHERE iv.DocKey IN (" +
                "SELECT InvoicedDocKey FROM dbo.zSCP2_MeterEntry WHERE InvoicedDocKey IS NOT NULL " +
                "UNION SELECT SalesInvoiceDocKey FROM dbo.zSCP_MeterTrans WHERE SalesInvoiceDocKey IS NOT NULL) " +
                "ORDER BY P DESC, iv.DocNo DESC", false);
        }

        /// <summary>The month an invoice billed, as YYYYMM -- 0 when it billed no meter period.</summary>
        private const string BILLED_PERIOD =
            "ISNULL((SELECT MAX(e.PeriodYear * 100 + e.PeriodMonth) FROM dbo.zSCP2_MeterEntry e " +
            "         WHERE e.InvoicedDocKey = iv.DocKey), 0)";

        /// <summary>The invoices with these numbers: DocKey and DocNo, NEWEST MONTH FIRST -- a batch
        /// deleted in this order never takes an earlier month out from under a later one.</summary>
        public static DataTable ByDocNo(DBSetting db, ICollection<string> docNos)
        {
            DataTable empty = new DataTable();
            empty.Columns.Add("DocKey", typeof(long));
            empty.Columns.Add("DocNo", typeof(string));
            empty.Columns.Add("P", typeof(long));
            if (docNos == null || docNos.Count == 0) return empty;
            List<string> quoted = new List<string>();
            foreach (string no in docNos)
            {
                string s = (no ?? "").Trim();
                if (s.Length > 0) quoted.Add("N'" + s.Replace("'", "''") + "'");
            }
            if (quoted.Count == 0) return empty;
            return db.GetDataTable(
                "SELECT iv.DocKey, iv.DocNo, " + BILLED_PERIOD + " AS P FROM dbo.IV iv " +
                " WHERE iv.DocNo IN (" + string.Join(",", quoted.ToArray()) + ") " +
                " ORDER BY P DESC, iv.DocNo DESC", false);
        }

        /// <summary>
        /// The months already invoiced AFTER the ones being deleted, on the same contracts, and not
        /// being deleted with them -- each as "contract · month · invoice".
        ///
        /// <para>A month is billed from where the month before it stopped: September's copies are the
        /// difference between September's reading and the one August billed. Take August's invoice away
        /// and September is left measuring from a reading nobody was charged for, so its copies are
        /// already on paper twice. The later invoice has to go first -- which is why this refuses
        /// rather than warns.</para>
        /// </summary>
        public static List<string> LaterInvoices(DBSetting db, DataTable invoices)
        {
            List<string> later = new List<string>();
            if (db == null || invoices == null || invoices.Rows.Count == 0) return later;
            StringBuilder ids = new StringBuilder();
            foreach (DataRow r in invoices.Rows)
            {
                if (ids.Length > 0) ids.Append(",");
                ids.Append(Convert.ToInt64(r["DocKey"]));
            }
            string going = ids.ToString();
            DataTable t = db.GetDataTable(
                "SELECT DISTINCT c.ContractNo, later.PeriodYear, later.PeriodMonth, later.InvoicedDocNo " +
                "  FROM (SELECT DISTINCT i.ContractKey, e.PeriodYear * 100 + e.PeriodMonth AS P " +
                "          FROM dbo.zSCP2_MeterEntry e " +
                "          JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "          JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "         WHERE e.InvoicedDocKey IN (" + going + ")) going " +
                "  JOIN dbo.zSCP2_Item i2 ON i2.ContractKey = going.ContractKey " +
                "  JOIN dbo.zSCP2_ItemMeter m2 ON m2.ItemKey = i2.ItemKey " +
                "  JOIN dbo.zSCP2_MeterEntry later ON later.ItemMeterKey = m2.ItemMeterKey " +
                "   AND later.PeriodYear * 100 + later.PeriodMonth > going.P " +
                "   AND later.InvoicedDocKey IS NOT NULL AND later.InvoicedDocKey NOT IN (" + going + ") " +
                "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = going.ContractKey " +
                "  JOIN dbo.IV iv ON iv.DocKey = later.InvoicedDocKey " +
                " ORDER BY c.ContractNo, later.PeriodYear, later.PeriodMonth, later.InvoicedDocNo", false);
            foreach (DataRow r in t.Rows)
            {
                string when = new DateTime(Convert.ToInt32(r["PeriodYear"]), Convert.ToInt32(r["PeriodMonth"]), 1)
                    .ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                string line = Convert.ToString(r["ContractNo"]).Trim() + "  ·  " + when + "  ·  " +
                              Convert.ToString(r["InvoicedDocNo"]).Trim();
                if (!later.Contains(line)) later.Add(line);
            }
            return later;
        }

        /// <summary>What to tell somebody who tried to delete an earlier month.</summary>
        public static string BlockedMessage(List<string> later)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("This invoice cannot be deleted: a later month is already invoiced.");
            sb.Append(Environment.NewLine).Append(Environment.NewLine);
            int shown = 0;
            foreach (string s in later)
            {
                if (shown++ >= 10) { sb.Append("   … and ").Append(later.Count - 10).Append(" more").Append(Environment.NewLine); break; }
                sb.Append("   ").Append(s).Append(Environment.NewLine);
            }
            sb.Append(Environment.NewLine);
            sb.Append("The later invoice was billed from the reading this one charged for. Delete the " +
                      "later month first, then this one.");
            return sb.ToString();
        }

        /// <summary>Deletes the invoices given. <paramref name="alsoCorrectionCreditNotes"/> clears the
        /// module's reading-correction credit notes too -- a whole-book reset; leave it off when the
        /// user is deleting invoices they picked.
        /// <para>Nothing is deleted while a LATER month of the same contract is still invoiced
        /// (<see cref="LaterInvoices"/>): the months come off newest first or not at all.</para></summary>
        public static ScpDeleteResult Delete(DBSetting db, DataTable invoices, bool alsoCorrectionCreditNotes)
        {
            ScpDeleteResult res = new ScpDeleteResult();
            if (db == null || invoices == null || invoices.Rows.Count == 0) return res;
            res.Blocked = LaterInvoices(db, invoices);
            if (res.WasBlocked) return res;      // the guard, wherever the delete is asked for
            StringBuilder errs = new StringBuilder();

            // A correction credit note raised from a reading knocks off against the invoice it
            // corrects, so it has to die first -- and its override row with it, or it would keep
            // winning the next baseline.
            if (alsoCorrectionCreditNotes)
            {
                try
                {
                    DataTable cnKeys = db.GetDataTable(
                        "SELECT DISTINCT CNDocKey, ISNULL(CNDocNo,'') AS CNDocNo FROM dbo.zSCP_MeterTrans WHERE CNDocKey IS NOT NULL", false);
                    if (cnKeys.Rows.Count > 0)
                    {
                        AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand cnCmd =
                            AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                                AutoCount.Authentication.UserSession.CurrentUserSession, db);
                        ScpCnWatcher.Suppress = true;
                        try
                        {
                            foreach (DataRow r in cnKeys.Rows)
                            {
                                try { cnCmd.Delete(Convert.ToInt64(r["CNDocKey"])); res.CreditNotes++; }
                                catch (Exception ex)
                                {
                                    res.Failed++;
                                    if (errs.Length < 600) errs.AppendLine(Convert.ToString(r["CNDocNo"]) + ": " + ex.Message);
                                }
                            }
                        }
                        finally { ScpCnWatcher.Suppress = false; }
                    }
                    db.ExecuteNonQuery("DELETE FROM dbo.zSCP_MeterTrans WHERE CNDocKey IS NOT NULL");
                }
                catch { }
            }

            res.Blockers = ClearBlockingArDocuments(db, invoices, errs);

            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                    AutoCount.Authentication.UserSession.CurrentUserSession, db);
            foreach (DataRow r in invoices.Rows)
            {
                try { cmd.Delete(Convert.ToInt64(r["DocKey"])); res.Deleted++; }
                catch (Exception ex)
                {
                    // The book's own guard talking (the trigger on dbo.IV). It already says what is
                    // wrong and what to do about it, so it is passed on as it stands rather than
                    // wrapped in "Unknown Sql Exception (Number=50000, Message=...)".
                    string refusal = TriggerRefusal(ex);
                    if (refusal.Length > 0)
                    {
                        if (!res.Refused.Contains(refusal)) res.Refused.Add(refusal);
                        continue;
                    }
                    res.Failed++;
                    if (errs.Length < 600) errs.AppendLine(Convert.ToString(r["DocNo"]) + ": " + ex.Message);
                }
            }

            // The readings the deleted invoices billed, and their period stamps, come back off.
            ScpBillingRows.ReconcileDeletedInvoices(db);
            res.Errors = errs.ToString();
            return res;
        }

        /// <summary>What the trigger's refusal always says — how one is recognised.</summary>
        private const string REFUSAL_MARK = "cannot be deleted:";

        /// <summary>The sentence the delete trigger raised, on its own — "" when the failure was
        /// something else. AutoCount hands a trigger's message back wrapped in its own wording, with
        /// "The transaction ended in the trigger" after it; neither belongs in front of a clerk.</summary>
        private static string TriggerRefusal(Exception ex)
        {
            string m = ex == null ? "" : (ex.Message ?? "");
            if (m.IndexOf(REFUSAL_MARK, StringComparison.Ordinal) < 0) return "";
            int start = m.IndexOf("Invoice ", StringComparison.Ordinal);
            if (start < 0) start = 0;
            string s = m.Substring(start);
            int cut = s.IndexOf("The transaction ended", StringComparison.Ordinal);
            if (cut > 0) s = s.Substring(0, cut);
            s = s.Trim();
            while (s.EndsWith(")")) s = s.Substring(0, s.Length - 1).Trim();
            return s;
        }

        /// <summary>
        /// Deletes every AR document knocked off against these sales invoices. AutoCount treats any
        /// knock-off (customer payment, credit note, refund) as payment on the invoice and refuses to
        /// delete it while one exists. The whole blocking document goes, even when it also settles
        /// other invoices. Returns how many were removed; failures are appended to errs.
        /// </summary>
        private static int ClearBlockingArDocuments(DBSetting db, DataTable ivKeys, StringBuilder errs)
        {
            if (db == null || ivKeys == null || ivKeys.Rows.Count == 0) return 0;
            StringBuilder ids = new StringBuilder();
            foreach (DataRow r in ivKeys.Rows)
            {
                if (ids.Length > 0) ids.Append(",");
                ids.Append(Convert.ToInt64(r["DocKey"]));
            }
            // dbo.IV and dbo.ARInvoice are separate keyspaces — the AR side points back with
            // SourceType='IV' + SourceKey = the sales invoice DocKey.
            string arInv = "SELECT DocKey FROM dbo.ARInvoice WHERE SourceType='IV' AND SourceKey IN (" + ids + ")";
            DataTable blockers;
            try
            {
                blockers = db.GetDataTable(
                    "SELECT DISTINCT 'CN' AS Kind, k.DocKey FROM dbo.ARCNKnockOff k WHERE k.KnockOffDocKey IN (" + arInv + ") " +
                    "UNION ALL SELECT DISTINCT 'PAYMENT', k.DocKey FROM dbo.ARPaymentKnockOff k WHERE k.KnockOffDocKey IN (" + arInv + ") " +
                    "UNION ALL SELECT DISTINCT 'REFUND', k.DocKey FROM dbo.ARRefundKnockOff k WHERE k.KnockOffDocKey IN (" + arInv + ") " +
                    "UNION ALL SELECT DISTINCT 'CONTRA', k.DocKey FROM dbo.ARContraKnockOff k WHERE k.KnockOffDocKey IN (" + arInv + ")", false);
            }
            catch (Exception ex)
            {
                if (errs.Length < 600) errs.AppendLine("Blocker lookup failed: " + ex.Message);
                return 0;
            }
            if (blockers.Rows.Count == 0) return 0;

            AutoCount.Authentication.UserSession us = AutoCount.Authentication.UserSession.CurrentUserSession;
            string userId = us.LoginUserID;
            int cleared = 0;
            ScpCnWatcher.Suppress = true;
            try
            {
                foreach (DataRow b in blockers.Rows)
                {
                    string kind = Convert.ToString(b["Kind"]);
                    long key = Convert.ToInt64(b["DocKey"]);
                    try
                    {
                        if (kind == "CN")
                        {
                            // An AR CN raised from the Invoicing module must die through the sales
                            // side (SourceType='CN' -> SourceKey = dbo.CN.DocKey); a pure AR CN
                            // through the AR side.
                            DataTable src = db.GetDataTable(
                                "SELECT ISNULL(SourceType,'') AS SourceType, ISNULL(SourceKey,0) AS SourceKey " +
                                "FROM dbo.ARCN WHERE DocKey=" + key, false);
                            long salesKey = 0;
                            if (src.Rows.Count > 0 && Convert.ToString(src.Rows[0]["SourceType"]).Trim().ToUpperInvariant() == "CN")
                                salesKey = Convert.ToInt64(src.Rows[0]["SourceKey"]);
                            if (salesKey > 0)
                                AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(us, db).Delete(salesKey);
                            else
                                AutoCount.ARAP.ARCN.ARCNDataAccess.Create(us, db).DeleteARCN(key, userId);
                        }
                        else if (kind == "PAYMENT")
                            AutoCount.ARAP.ARPayment.ARPaymentDataAccess.Create(us, db).DeleteARPayment(key, userId);
                        else if (kind == "REFUND")
                            AutoCount.ARAP.ARRefund.ARRefundDataAccess.Create(us, db).DeleteARRefund(key, userId);
                        else
                        {
                            // AR Contra has no public delete API in AutoCount.Accounting — say so
                            // instead of failing the invoice delete with a mystery message.
                            if (errs.Length < 600)
                                errs.AppendLine("AR Contra (DocKey " + key + ") blocks the invoice — delete it in AutoCount first.");
                            continue;
                        }
                        cleared++;
                    }
                    catch (Exception ex)
                    {
                        if (errs.Length < 600) errs.AppendLine(kind + " " + key + ": " + ex.Message);
                    }
                }
            }
            finally { ScpCnWatcher.Suppress = false; }
            return cleared;
        }
    }
}
