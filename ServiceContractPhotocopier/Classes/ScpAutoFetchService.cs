using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;
using ServiceContractPhotocopier.Data;
using ServiceContractPhotocopier.MeterReading.Services;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Billing-day AUTO-FETCH background service (runs while AutoCount is open; started from
    /// PluginMain). On each billing day D it snapshots the API readings AS OF the configured
    /// cutoff (e.g. day D-1 23:59, or day D itself — Meter Reading &gt; Setting) for every machine
    /// whose effective billing day is D, stages them into zSCP2_MeterEntry and LOCKS them
    /// (LockedAt): later fetches and manual key-ins can no longer override the snapshot.
    /// One run per calendar day (marker in Z_PumsConfig); every reading is audit-logged.
    /// </summary>
    public static class ScpAutoFetchService
    {
        private static System.Threading.Timer _timer;
        private static DBSetting _db;
        private static bool _running;

        /// <summary>Start the background checker: first check after 1 minute, then every 10 minutes.</summary>
        public static void Start(DBSetting db)
        {
            _db = db;
            if (_timer == null)
                _timer = new System.Threading.Timer(Tick, null, 60 * 1000, 10 * 60 * 1000);
        }

        private static void Tick(object state)
        {
            if (_running) return;
            _running = true;
            try { CheckAndRun(); }
            catch { /* background best-effort — never crash the host */ }
            finally { _running = false; }
        }

        private static void CheckAndRun()
        {
            if (_db == null) return;
            if (!PumsConfig.GetBool(_db, PumsConfig.KEY_AUTO_FETCH_ENABLED, PumsConfig.DEFAULT_AUTO_FETCH_ENABLED)) return;

            DateTime today = DateTime.Today;
            int year = today.Year, month = today.Month, day = today.Day;
            int monthEnd = DateTime.DaysInMonth(year, month);

            // Is today a billing day? (billing days beyond the month's end clamp to month-end)
            DataTable days = _db.GetDataTable(
                "SELECT DISTINCT COALESCE(i.BillingDayOverride, c.BillingDay) AS d " +
                "FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "WHERE i.Inactive='N' AND c.Inactive='N' " +
                // Expired machines (expiry before this month) stopped billing — stop fetching them too.
                "AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                "  OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))", false);
            bool isBillingDay = false;
            foreach (DataRow r in days.Rows)
            {
                int d = Convert.ToInt32(r["d"]);
                if (d > monthEnd) d = monthEnd;
                if (d == day) { isBillingDay = true; break; }
            }
            if (!isBillingDay) return;

            // One snapshot per calendar day.
            string marker = "AUTO_FETCH_DONE_" + today.ToString("yyyyMMdd");
            if (PumsConfig.Get(_db, marker, null) != null) return;

            // Cutoff moment: "BEFORE" = day D-1 at HH:mm; "ON" = day D at HH:mm. When the cutoff sits
            // ON the billing day, wait until the cutoff time has passed before snapshotting.
            string cutTime = PumsConfig.Get(_db, PumsConfig.KEY_AUTO_FETCH_CUTOFF, PumsConfig.DEFAULT_AUTO_FETCH_CUTOFF);
            TimeSpan t;
            if (!TimeSpan.TryParse(cutTime, out t)) t = new TimeSpan(23, 59, 0);
            string cutDayMode = PumsConfig.Get(_db, PumsConfig.KEY_AUTO_FETCH_CUTOFF_DAY, PumsConfig.DEFAULT_AUTO_FETCH_CUTOFF_DAY);
            bool onBillingDay = string.Equals(cutDayMode, "ON", StringComparison.OrdinalIgnoreCase);
            DateTime cutoff = (onBillingDay ? today : today.AddDays(-1)).Add(t);
            if (DateTime.Now < cutoff) return;   // cutoff not reached yet — try again next tick

            // Fetch both endpoints and keep only readings audited on/before the cutoff (same month
            // for the day-before mode may cross a month boundary on day 1 — accept any <= cutoff).
            IMeterReadingApiClient client = MeterReadingApiClientFactory.Create(_db);
            List<MeterReadingDto> all = new List<MeterReadingDto>();
            try { List<MeterReadingDto> on = client.GetReadings(MachineStatus.Online, year, month); if (on != null) all.AddRange(on); } catch { }
            try { List<MeterReadingDto> off = client.GetReadings(MachineStatus.Offline, year, month); if (off != null) all.AddRange(off); } catch { }
            Dictionary<string, MeterReadingDto> bySerial = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, MeterReadingDto> byCode = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterReadingDto d in all)
            {
                if (!d.LastAuditDate.HasValue || d.LastAuditDate.Value > cutoff) continue;
                if (!string.IsNullOrWhiteSpace(d.SerialNumber) && !bySerial.ContainsKey(d.SerialNumber.Trim()))
                    bySerial[d.SerialNumber.Trim()] = d;
                if (!string.IsNullOrWhiteSpace(d.Code) && !byCode.ContainsKey(d.Code.Trim()))
                    byCode[d.Code.Trim()] = d;
            }

            // Machines whose EFFECTIVE billing day is today.
            DataTable meters = _db.GetDataTable(
                "SELECT m.ItemMeterKey, m.MeterRole, " +
                "COALESCE(NULLIF(m.MachineSerialNo,''), i.SerialNumber) AS SerialNo, i.ServiceItemNo " +
                "FROM dbo.zSCP2_ItemMeter m " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "WHERE m.MeterRole IN ('BK','CL') AND i.Inactive='N' AND c.Inactive='N' " +
                // Expired machines (expiry before this month) stopped billing — stop fetching them too.
                "AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                "  OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) " +
                "AND (CASE WHEN COALESCE(i.BillingDayOverride, c.BillingDay) > " + monthEnd +
                " THEN " + monthEnd + " ELSE COALESCE(i.BillingDayOverride, c.BillingDay) END) = " + day, false);

            int locked = 0;
            using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("AutoFetch"))
                {
                    try
                    {
                        foreach (DataRow m in meters.Rows)
                        {
                            string serial = (m["SerialNo"] as string ?? "").Trim();
                            string code = (m["ServiceItemNo"] as string ?? "").Trim();
                            MeterReadingDto dto = null;
                            if (serial.Length > 0) bySerial.TryGetValue(serial, out dto);
                            if (dto == null && code.Length > 0) byCode.TryGetValue(code, out dto);
                            if (dto == null) continue;

                            decimal reading = string.Equals(m["MeterRole"] as string, "CL", StringComparison.OrdinalIgnoreCase)
                                ? dto.TotalCL : dto.TotalBK;
                            string source = dto.Status == MachineStatus.Online ? "ONLINE" : "OFFLINE";
                            // Offline monthly-report id — persisted so the invoice RefDocNo can cite it.
                            string tid = dto.Status == MachineStatus.Online ? "" : ((dto.TrackingId ?? "").Trim());
                            long imk = Convert.ToInt64(m["ItemMeterKey"]);

                            // Snapshot + LOCK. Never touches invoiced or already-locked rows.
                            SqlCommand cmd = new SqlCommand(
                                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, ReadingDate=@dt, Source=@src, TrackingId=@tid, " +
                                "LockedAt=GETDATE(), LastModified=GETDATE() " +
                                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo " +
                                "AND InvoicedDocKey IS NULL AND LockedAt IS NULL; " +
                                "IF @@ROWCOUNT=0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry " +
                                "  WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo) " +
                                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source,TrackingId,LockedAt) " +
                                "VALUES (@imk,@yr,@mo,@rd,@dt,@src,@tid,GETDATE());", cn, tx);
                            cmd.Parameters.AddWithValue("@imk", imk);
                            cmd.Parameters.AddWithValue("@yr", year);
                            cmd.Parameters.AddWithValue("@mo", month);
                            cmd.Parameters.AddWithValue("@rd", reading);
                            cmd.Parameters.AddWithValue("@dt", (object)dto.LastAuditDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@src", source);
                            cmd.Parameters.AddWithValue("@tid", tid);
                            cmd.ExecuteNonQuery();

                            ScpMeterReadingLog.Append(cn, tx, imk, year, month, reading,
                                dto.LastAuditDate, source, "");
                            locked++;
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }

            PumsConfig.Set(_db, marker,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " — " + locked + " reading(s) snapshotted, cutoff " + cutoff.ToString("dd/MM/yyyy HH:mm"));
        }
    }
}
