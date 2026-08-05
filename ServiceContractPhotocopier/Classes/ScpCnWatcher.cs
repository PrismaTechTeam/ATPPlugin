using System;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// REAL-TIME watch on Sales Credit Note saves/deletes/cancels (demo 28/07 #10 hardening).
    /// Subscribes to AutoCount's own AsyncDataSetUpdate bus on CreditNoteCommand — the same
    /// mechanism AutoCount's CN list form uses — which fires AFTER COMMIT for every save and
    /// delete, no matter where it happened (CN entry form, batch actions, other plugins).
    /// Reactions:
    ///   • correction CN deleted/cancelled  -> roll the reading override back NOW + tell the user
    ///   • correction CN quantity edited    -> mismatch warning NOW (override unchanged)
    ///   • manual CN referencing a METER invoice -> "this does NOT update the last reading" warning
    /// Our own CN operations set <see cref="Suppress"/> so they never self-alert.
    /// </summary>
    public static class ScpCnWatcher
    {
        private static AutoCount.Application.AsyncDataSetUpdateDelegate _del;
        private static DBSetting _db;

        [ThreadStatic] public static bool Suppress;

        public static void Start(DBSetting db)
        {
            try
            {
                Stop();
                _db = db;
                _del = new AutoCount.Application.AsyncDataSetUpdateDelegate(OnCnChanged);
                AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.DataSetUpdate.Add(db, _del);
            }
            catch { }   // the watcher is a safety net — never block plugin load
        }

        public static void Stop()
        {
            try
            {
                if (_del != null && _db != null)
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.DataSetUpdate.Remove(_db, _del);
            }
            catch { }
            _del = null;
        }

        private static void OnCnChanged(DBSetting dbSetting, DataSet ds, AutoCount.Application.AsyncDataSetUpdateAction action)
        {
            if (Suppress) return;
            try
            {
                if (ds == null || !ds.Tables.Contains("Master") || ds.Tables["Master"].Rows.Count == 0) return;
                DataRow m = ds.Tables["Master"].Rows[0];
                long docKey = Convert.ToInt64(m["DocKey"]);
                string docNo = Convert.ToString(m["DocNo"]);
                bool cancelled = false;
                try { cancelled = Convert.ToString(m["Cancelled"]) == "T"; } catch { }

                bool isCorrectionCn = false;
                try
                {
                    DataTable c = dbSetting.GetDataTable(
                        "SELECT TOP 1 1 AS X FROM dbo.zSCP_MeterTrans WHERE CNDocKey = " + docKey, false);
                    isCorrectionCn = c.Rows.Count > 0;
                }
                catch { }

                // Cancel is just a save with Cancelled='T' — treat both like a delete.
                if (action == AutoCount.Application.AsyncDataSetUpdateAction.Delete || cancelled)
                {
                    if (!isCorrectionCn) return;
                    ScpCreditNoteBuilder.ReconcileDeletedCreditNotes(dbSetting);
                    DevExpress.XtraEditors.XtraMessageBox.Show(
                        "Credit note " + docNo + " was " + (cancelled ? "CANCELLED" : "DELETED") + ".\r\n\r\n" +
                        "It carried a METER READING correction — the reading override has been rolled back:\r\n" +
                        "the machine's Last Reading reverts to the invoiced value (audit trail: CN-DELETED).",
                        "Meter correction rolled back",
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                if (isCorrectionCn)
                {
                    // Correction CN re-saved: compare its qty against the correction it was issued for.
                    DataTable d = dbSetting.GetDataTable(
                        "SELECT ISNULL(cn.Q,0) AS CnQty, ISNULL(lg.Q,0) AS OurQty FROM " +
                        "(SELECT SUM(ISNULL(Qty,0)) AS Q FROM dbo.CNDTL WHERE DocKey = " + docKey + ") cn, " +
                        "(SELECT SUM(ISNULL(-[Usage],0)) AS Q FROM dbo.zSCP2_MeterReadingLog " +
                        " WHERE Source = '" + ScpMeterReadingLog.SOURCE_CN + "' AND DocNo = '" + docNo.Replace("'", "''") + "') lg", false);
                    if (d.Rows.Count == 0) return;
                    decimal cnQty = Convert.ToDecimal(d.Rows[0]["CnQty"]);
                    decimal ourQty = Convert.ToDecimal(d.Rows[0]["OurQty"]);
                    if (Math.Abs(cnQty - ourQty) <= 0.01m) return;
                    DevExpress.XtraEditors.XtraMessageBox.Show(
                        "Credit note " + docNo + " is linked to a METER READING correction, but its quantity " +
                        "was changed:\r\n\r\n" +
                        "   correction issued for " + ourQty.ToString("0.##") + " copies — the CN now carries " +
                        cnQty.ToString("0.##") + "\r\n\r\n" +
                        "The reading override is UNCHANGED. If the figures should change, DELETE this CN " +
                        "(the override rolls back automatically) and re-issue it from Contract > Billing History " +
                        "> \"Correct with CN...\".",
                        "Meter correction mismatch",
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                // A CN we did NOT create that points at a METER invoice: it credits money but can
                // never update the machine's last reading — say so immediately.
                string ourInv = "";
                try { ourInv = Convert.ToString(m["OurInvoiceNo"]).Trim(); } catch { }
                if (ourInv.Length == 0) return;
                DataTable mi = dbSetting.GetDataTable(
                    "SELECT TOP 1 1 AS X FROM dbo.IV iv " +
                    "JOIN dbo.zSCP2_MeterEntry me ON me.InvoicedDocKey = iv.DocKey " +
                    "WHERE iv.DocNo = '" + ourInv.Replace("'", "''") + "'", false);
                if (mi.Rows.Count == 0) return;
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "Credit note " + docNo + " references METER invoice " + ourInv + " but was created " +
                    "manually — the machine's LAST READING will NOT be updated by this CN.\r\n\r\n" +
                    "If this is a reading correction, delete it and use Contract > Billing History > " +
                    "\"Correct with CN...\" instead (reading + money stay consistent there).",
                    "Meter invoice CN — no reading link",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
            catch { }   // the dispatcher swallows exceptions anyway — never break a CN save
        }
    }
}
