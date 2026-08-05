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

                    // AUTO-SYNC (user rule: qty IS the correction): for a single-meter correction CN,
                    // credited copies = base reading - corrected reading, so a qty edit re-derives the
                    // override: new reading = base - new qty. Multi-meter CNs can't tell which machine
                    // the edit was meant for -> warn only.
                    DataTable ov = dbSetting.GetDataTable(
                        "SELECT ServiceItemMeterTypeKey, MeterTransReading FROM dbo.zSCP_MeterTrans " +
                        "WHERE CNDocKey = " + docKey, false);
                    if (ov.Rows.Count == 1)
                    {
                        long imk = Convert.ToInt64(ov.Rows[0]["ServiceItemMeterTypeKey"]);
                        decimal oldReading = Convert.ToDecimal(ov.Rows[0]["MeterTransReading"]);
                        DataTable b = dbSetting.GetDataTable(
                            "SELECT TOP 1 LastReading, PeriodYear, PeriodMonth FROM dbo.zSCP2_MeterReadingLog " +
                            "WHERE Source = '" + ScpMeterReadingLog.SOURCE_CN + "' AND DocNo = '" + docNo.Replace("'", "''") + "' " +
                            "AND ItemMeterKey = " + imk + " AND LastReading IS NOT NULL ORDER BY LogKey", false);
                        if (b.Rows.Count == 1 && b.Rows[0]["LastReading"] != DBNull.Value)
                        {
                            decimal baseRead = Convert.ToDecimal(b.Rows[0]["LastReading"]);
                            decimal newReading = baseRead - cnQty;
                            if (newReading >= 0m)
                            {
                                dbSetting.ExecuteNonQuery(
                                    "UPDATE dbo.zSCP_MeterTrans SET MeterTransReading = " +
                                    newReading.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                    ", Remark = 'CN " + docNo.Replace("'", "''") + ": " +
                                    baseRead.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " -> " +
                                    newReading.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                                    " (qty edited in AutoCount)', LastModified = GETDATE() WHERE CNDocKey = " + docKey);
                                // Delta log row: totals re-balance (SUM(-Usage) becomes the new qty)
                                // and the latest Reading is the new override — full audit trail.
                                try
                                {
                                    using (System.Data.SqlClient.SqlConnection cn2 =
                                        new System.Data.SqlClient.SqlConnection(dbSetting.ConnectionString))
                                    {
                                        cn2.Open();
                                        ScpMeterReadingLog.Append(cn2, null, imk,
                                            b.Rows[0]["PeriodYear"] == DBNull.Value ? 0 : Convert.ToInt32(b.Rows[0]["PeriodYear"]),
                                            b.Rows[0]["PeriodMonth"] == DBNull.Value ? 0 : Convert.ToInt32(b.Rows[0]["PeriodMonth"]),
                                            newReading, DateTime.Now, ScpMeterReadingLog.SOURCE_CN, docNo,
                                            baseRead, -(cnQty - ourQty), null, null, null, null, null,
                                            null, "CN qty edited in AutoCount: " + ourQty.ToString("0.##") +
                                            " -> " + cnQty.ToString("0.##") + " copies");
                                    }
                                }
                                catch { }
                                DevExpress.XtraEditors.XtraMessageBox.Show(
                                    "Credit note " + docNo + " quantity changed " + ourQty.ToString("0.##") +
                                    " -> " + cnQty.ToString("0.##") + " copies.\r\n\r\n" +
                                    "The meter reading override was re-derived to stay consistent:\r\n" +
                                    "   last reading " + oldReading.ToString("#,##0.##") + "  ->  " +
                                    newReading.ToString("#,##0.##") + "   (base " + baseRead.ToString("#,##0.##") +
                                    " - " + cnQty.ToString("0.##") + " credited copies)\r\n\r\n" +
                                    "Future billing starts from " + newReading.ToString("#,##0.##") + ".",
                                    "Meter correction synced",
                                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                    DevExpress.XtraEditors.XtraMessageBox.Show(
                        "Credit note " + docNo + " is linked to a METER READING correction, but its quantity " +
                        "was changed:\r\n\r\n" +
                        "   correction issued for " + ourQty.ToString("0.##") + " copies — the CN now carries " +
                        cnQty.ToString("0.##") + "\r\n\r\n" +
                        (ov.Rows.Count > 1
                            ? "This CN corrects SEVERAL meters, so the edit cannot be mapped to one machine — the " +
                              "reading override is UNCHANGED. DELETE this CN (the override rolls back automatically) " +
                              "and re-issue it from Contract > Billing History > \"Correct with CN...\"."
                            : "The new quantity is larger than the base reading allows — the reading override is " +
                              "UNCHANGED. Fix the CN quantity, or delete the CN and re-issue the correction."),
                        "Meter correction mismatch",
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                // A CN we did NOT create that points at a METER invoice: it credits money but
                // carries no reading correction — every meter keeps its CURRENT effective last
                // reading (which may already come from an earlier correction CN). List them so the
                // message states facts, not implications.
                string ourInv = "";
                try { ourInv = Convert.ToString(m["OurInvoiceNo"]).Trim(); } catch { }
                if (ourInv.Length == 0) return;
                DataTable mi = dbSetting.GetDataTable(
                    "SELECT TOP 6 i.ServiceItemNo, " +
                    "COALESCE(NULLIF(im.MachineSerialNo,''), it.SerialNumber) AS Serial, cur.Reading " +
                    "FROM dbo.IV iv " +
                    "JOIN dbo.zSCP_MeterTrans t ON t.SalesInvoiceDocKey = iv.DocKey " +
                    "JOIN dbo.zSCP2_ItemMeter im ON im.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                    "JOIN dbo.zSCP2_Item it ON it.ItemKey = im.ItemKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = t.ServiceItemKey " +
                    "OUTER APPLY (SELECT TOP 1 x.MeterTransReading AS Reading FROM dbo.zSCP_MeterTrans x " +
                    "  WHERE x.ServiceItemMeterTypeKey = t.ServiceItemMeterTypeKey " +
                    "  ORDER BY x.MeterTransDate DESC, x.MeterTransKey DESC) cur " +
                    "WHERE iv.DocNo = '" + ourInv.Replace("'", "''") + "' " +
                    "GROUP BY i.ServiceItemNo, COALESCE(NULLIF(im.MachineSerialNo,''), it.SerialNumber), cur.Reading", false);
                if (mi.Rows.Count == 0) return;
                System.Text.StringBuilder cur2 = new System.Text.StringBuilder();
                foreach (DataRow r2 in mi.Rows)
                    cur2.AppendLine("   " + Convert.ToString(r2["ServiceItemNo"]) +
                        (Convert.ToString(r2["Serial"]).Length > 0 ? " (S/N " + Convert.ToString(r2["Serial"]) + ")" : "") +
                        "  last reading stays " + (r2["Reading"] == DBNull.Value ? "-" : Convert.ToDecimal(r2["Reading"]).ToString("#,##0.##")));
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "Credit note " + docNo + " references METER invoice " + ourInv + " but carries NO meter " +
                    "reading correction — this CN only credits money:\r\n\r\n" + cur2 +
                    "\r\n• Money-only credit intended (discount / goodwill)?  This CN is fine as it is.\r\n" +
                    "• Reading correction intended?  Delete this CN and use Contract > Billing History > " +
                    "\"Correct with CN...\" — there the reading AND the money change together.",
                    "Meter invoice CN — readings unchanged",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
            catch { }   // the dispatcher swallows exceptions anyway — never break a CN save
        }
    }
}
