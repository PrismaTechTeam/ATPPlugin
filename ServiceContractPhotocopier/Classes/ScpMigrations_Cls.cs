using System;
using System.Reflection;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Runs the embedded SQL migration scripts on plugin load. Mirrors the BookHub
    /// RunEmbeddedSQLScripts pattern: check sysobjects for each target, create if missing.
    /// Every schema change must have a corresponding .sql file under ServiceContractPhotocopier\SQL
    /// registered as EmbeddedResource in the csproj.
    /// </summary>
    public static class ScpMigrations_Cls
    {
        private const string NameSpace = nameof(ServiceContractPhotocopier);

        public static bool RunEmbeddedSQLScripts(DBSetting dbsetting)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // === Tier 0: shared infrastructure ===
            RunIfTableMissing(dbsetting, "z_SysConfig",                 "01_CreateTable_z_SysConfig.sql", asm);
            // (z_SysRef registered just below)

            // === Tier 0b: PUMS stock integration tables ===
            // These were previously created only by the ATPApi webhook service or lazily in code,
            // so a fresh plugin install (no ATPApi run) was missing them. Provision on load here.
            RunIfTableMissing(dbsetting, "Z_PumsConfig",               "02_CreateTable_Z_PumsConfig.sql", asm);
            RunIfTableMissing(dbsetting, "Z_PumsLog",                  "02_CreateTable_Z_PumsLog.sql", asm);
            RunIfTableMissing(dbsetting, "Z_PumsStockIssue",           "02_CreateTable_Z_PumsStockIssue.sql", asm);
            RunDDL(dbsetting, "02_UpdateTable_Z_PumsStockIssue_v1.2.0.sql", asm);  // adds SerialNumber if missing (idempotent)
            RunIfTableMissing(dbsetting, "Z_PumsStockTransfer",        "02_CreateTable_Z_PumsStockTransfer.sql", asm);
            RunIfTableMissing(dbsetting, "Z_PumsTaskLock",             "02_CreateTable_Z_PumsTaskLock.sql", asm);
            RunIfTableMissing(dbsetting, "z_SysRef",                    "01_CreateTable_z_SysRef.sql", asm);

            // === Tier 1: lookups ===
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceStatus",       "02_CreateTable_zSCP_LK_ServiceStatus.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceSeverity",     "02_CreateTable_zSCP_LK_ServiceSeverity.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceSolution",     "02_CreateTable_zSCP_LK_ServiceSolution.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceProblem",      "02_CreateTable_zSCP_LK_ServiceProblem.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceType",         "02_CreateTable_zSCP_LK_ServiceType.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceContractType", "02_CreateTable_zSCP_LK_ServiceContractType.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_ServiceItemGrade",    "02_CreateTable_zSCP_LK_ServiceItemGrade.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_AppointmentType",     "02_CreateTable_zSCP_LK_AppointmentType.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_LK_AppointmentPriority", "02_CreateTable_zSCP_LK_AppointmentPriority.sql", asm);

            // === Tier 2: people ===
            RunIfTableMissing(dbsetting, "zSCP_ServicePerson",          "02_CreateTable_zSCP_ServicePerson.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceAdvisor",         "02_CreateTable_zSCP_ServiceAdvisor.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_Mechanic",               "02_CreateTable_zSCP_Mechanic.sql", asm);

            // === Tier 3: meter chain ===
            RunIfTableMissing(dbsetting, "zSCP_MeterMultiPrice",        "02_CreateTable_zSCP_MeterMultiPrice.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_MeterMultiPriceItem",    "02_CreateTable_zSCP_MeterMultiPriceItem.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_MeterType",              "02_CreateTable_zSCP_MeterType.sql", asm);
            RunDDL(dbsetting, "02_UpdateTable_zSCP_MeterType_v1.3.0.sql", asm);  // adds ACItemCode if missing (idempotent)
            RunDDL(dbsetting, "02_UpdateTable_zSCP_MeterType_v1.4.0.sql", asm);  // adds IsFlatCharge (rental) + auto-marks RA*
            RunDDL(dbsetting, "02_UpdateTable_zSCP_MeterType_v1.5.0.sql", asm);  // broadens rental flat-mark to "01.RA*"/"...RENTAL" codes

            // === Tier 4: service item ===
            RunIfTableMissing(dbsetting, "zSCP_ServiceItem",            "02_CreateTable_zSCP_ServiceItem.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceItemMeterType",   "02_CreateTable_zSCP_ServiceItemMeterType.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceItemDebtorHistory","02_CreateTable_zSCP_ServiceItemDebtorHistory.sql", asm);

            // === Tier 5: meter transactions ===
            RunIfTableMissing(dbsetting, "zSCP_MeterTrans",             "02_CreateTable_zSCP_MeterTrans.sql", asm);

            // === Tier 6: contract (parent BEFORE ID/SVI/DTL so FKs can link back) ===
            RunIfTableMissing(dbsetting, "zSCP_ServiceContract",        "02_CreateTable_zSCP_ServiceContract.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceContractID",      "02_CreateTable_zSCP_ServiceContractID.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceContractSVI",     "02_CreateTable_zSCP_ServiceContractSVI.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceContractDTL",     "02_CreateTable_zSCP_ServiceContractDTL.sql", asm);

            // === Tier 7: service note (parent BEFORE ID/DTL) ===
            RunIfTableMissing(dbsetting, "zSCP_ServiceNote",            "02_CreateTable_zSCP_ServiceNote.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceNoteID",          "02_CreateTable_zSCP_ServiceNoteID.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP_ServiceNoteDTL",         "02_CreateTable_zSCP_ServiceNoteDTL.sql", asm);

            // === Tier 8: appointment ===
            RunIfTableMissing(dbsetting, "zSCP_Appointment",            "02_CreateTable_zSCP_Appointment.sql", asm);

            // === Tier 8b: combined Service Contract module v2 (zSCP2_*) ===
            RunIfTableMissing(dbsetting, "zSCP2_Contract",              "02_CreateTable_zSCP2_Contract.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_Item",                  "02_CreateTable_zSCP2_Item.sql", asm);
            // Evolve an older zSCP2_Item (ItemNo->ServiceItemNo, drop MachineName/StockCode). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v3.sql", asm);
            // Add ServiceExpiryDate (per-item expiry, mirrors master serviceitem). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v4.sql", asm);
            // Service Item overhaul: Item Code + Grade, More Header, Note/Remarks, Preventive. Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v6_Overhaul.sql", asm);
            // v2 debtor ownership history (keyed to zSCP2_Item; legacy table FKs to v1 and can't hold v2 keys).
            RunIfTableMissing(dbsetting, "zSCP2_ItemDebtorHistory",     "02_CreateTable_zSCP2_ItemDebtorHistory.sql", asm);
            // Reference No (contract + item) + contract-context fields on the item. Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_ItemContract_v7_RefAndContext.sql", asm);
            // Seed opening debtor-ownership rows so the history tab isn't blank for existing items.
            RunDDL(dbsetting, "05_Backfill_zSCP2_ItemDebtorHistory.sql", asm);
            // Serial No column on provided items. Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_ContractSparePart_v2_SerialNo.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_ItemMeter",             "02_CreateTable_zSCP2_ItemMeter.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_ItemMeterPrice",        "02_CreateTable_zSCP2_ItemMeterPrice.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_ContractSnapshot",      "02_CreateTable_zSCP2_ContractSnapshot.sql", asm);
            // Multi-machine CSSI: per-unit meters (MachineSerialNo) + MultiMachine flag + widened unique
            // keys. MUST run AFTER the ItemMeter create — a fresh book has no table to ALTER yet.
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v2_MachineSerial.sql", asm);
            // Per-meter Description (defaults from the Meter Type, editable per machine).
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v3_Description.sql", asm);
            // Rental period tracking (start date / total months / accrual-prepayment basis -> n/N).
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v4_Rental.sql", asm);
            // Strategy Maintenance master (marketing/billing strategies attachable to contracts).
            RunIfTableMissing(dbsetting, "zSCP2_Strategy",              "02_CreateTable_zSCP2_Strategy.sql", asm);
            // Composable rule lines for a strategy (the builder detail table; FK+cascade to the header).
            RunIfTableMissing(dbsetting, "zSCP2_StrategyRule",          "02_CreateTable_zSCP2_StrategyRule.sql", asm);
            // Per-contract COPY of strategy rules (template->instance; edited on the contract, FK to contract).
            RunIfTableMissing(dbsetting, "zSCP2_ContractStrategyRule",  "02_CreateTable_zSCP2_ContractStrategyRule.sql", asm);
            // Field-level contract/item change audit (append-only, no FKs).
            RunIfTableMissing(dbsetting, "zSCP2_ContractAudit",         "02_CreateTable_zSCP2_ContractAudit.sql", asm);
            // Legacy usage meters tagged NA get BK/CL inferred from their type names (guards inside).
            RunDDL(dbsetting, "05_Backfill_zSCP2_ItemMeter_Roles.sql", asm);
            // Ownership: contract-less items are owned by a debtor directly (OwnerDebtorCode).
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v8_OwnerDebtor.sql", asm);
            // Per-item Purchase Date + Service Type (its own lookup list).
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v9_PurchaseServiceType.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_ItemCode",              "02_CreateTable_zSCP2_ItemCode.sql", asm);
            // Staging of current readings (manual key-ins + accepted API values) per billing period.
            RunIfTableMissing(dbsetting, "zSCP2_MeterEntry",            "02_CreateTable_zSCP2_MeterEntry.sql", asm);
            // Append-only meter reading audit log (immutable history of every reading event).
            RunIfTableMissing(dbsetting, "zSCP2_MeterReadingLog",       "02_CreateTable_zSCP2_MeterReadingLog.sql", asm);
            // Reading log v2: baseline + usage on every event (AFTER the create above).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterReadingLog_v2_LastUsage.sql", asm);
            // Reading log v3: pricing-as-of-event (unit price / min / FOC / rebate / charge).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterReadingLog_v3_Pricing.sql", asm);
            // Reading log v4: strategy code + human-readable strategy outcome per event.
            RunDDL(dbsetting, "02_Update_zSCP2_MeterReadingLog_v4_Strategy.sql", asm);
            // API connection profiles (Plugin Option > API tab), seeded Production + Local Mock.
            RunIfTableMissing(dbsetting, "zSCP2_ApiProfile",            "02_CreateTable_zSCP2_ApiProfile.sql", asm);
            // Add Invoiced stamp columns (billing-period duplicate guard). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v2.sql", asm);
            // Auto-fetch snapshot lock (LockedAt).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v3_Lock.sql", asm);
            // Strategy in force when the period was stamped (traceability).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v4_Strategy.sql", asm);
            // v5: allow Source='INVOICE' — rentals/flat meters are never staged, so the stamp path INSERTs
            // a fresh entry with Source='INVOICE' which the original CHECK rejected (rolled back the stamp).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v5_SourceInvoice.sql", asm);
            // v6: offline TrackingId persisted with the staged reading (stamped into invoice RefDocNo).
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v6_TrackingId.sql", asm);
            // Plugin document numbering (SC / SI formats + running numbers), seeded past legacy max.
            RunIfTableMissing(dbsetting, "zSCP2_DocNoFormat",           "02_CreateTable_zSCP2_DocNoFormat.sql", asm);
            // Spare parts / services provided lines under a contract (contract- or item-bound).
            RunIfTableMissing(dbsetting, "zSCP2_ContractSparePart",     "02_CreateTable_zSCP2_ContractSparePart.sql", asm);
            // More Header tab fields (extra contact + delivery address block). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v2_MoreHeader.sql", asm);
            // Allow contract-less service items (ContractKey NULL) for attach/detach. Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v5_NullableContract.sql", asm);
            // "Bill on last day of month" contract option. Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v3_MonthEnd.sql", asm);
            // Contract-level Department + Project (AutoCount native masters). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v4_DeptProj.sql", asm);
            // v5: strategy attach (soft code) + rental-separate-invoice flag.
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v5_Strategy.sql", asm);
            // v6: per-contract FOC/Rebate reset period (Monthly default / Weekly / every N days -> accrual).
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v6_FOCReset.sql", asm);
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v7_Inactive.sql", asm);
            // v8: month-end billing retired — legacy day-31/month-end rows become plain day 28.
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v8_RetireMonthEnd.sql", asm);
            // v9: rental-separate invoice's OWN billing day (0 = follow the meter invoice date).
            RunDDL(dbsetting, "02_Update_zSCP2_Contract_v9_RentalBillingDay.sql", asm);
            // Bulk Email send history (Emailed column / not-yet-emailed checklist / contract reminder).
            RunIfTableMissing(dbsetting, "zSCP2_EmailLog",              "02_CreateTable_zSCP2_EmailLog.sql", asm);
            // Rental Waive meter types ("(W)" family auto-tagged) + per-meter waive configuration.
            RunDDL(dbsetting, "02_Update_zSCP_MeterType_v2_RentalWaive.sql", asm);
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v5_WaiveConfig.sql", asm);
            // Per-type default Role (RENTAL/WAIVE/COMMIT/BK/CL/NA) auto-filled on type pick.
            RunDDL(dbsetting, "02_Update_zSCP_MeterType_v3_DefaultRole.sql", asm);
            // Role column widened CHAR(2) -> VARCHAR(10) for the new role values.
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v6_WideRole.sql", asm);
            // v7: partial waive in RM (threshold reached -> RM amount off) + one-time % backfill.
            RunDDL(dbsetting, "02_Update_zSCP2_ItemMeter_v7_WaivePartialRM.sql", asm);
            // Group "machine" per contract (engine-driven .C concept): fleet-total MIN/WAIVE/RENTAL.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v8_GroupItem.sql", asm);
            // Per-machine ONLINE/OFFLINE definition -> advanced invoice number format.
            RunDDL(dbsetting, "02_Update_zSCP2_Item_v9_MachineMode.sql", asm);
            // Repoint zSCP_MeterTrans -> zSCP2_ItemMeter (idempotent; self-guarded on FK existence).
            RunDDL(dbsetting, "02_Update_zSCP_MeterTrans_v2.sql", asm);
            // Performance indexes for the contract/service-item lists + meter load. Idempotent
            // (guarded by sys.indexes) — first install creates them; existing books get any missing.
            RunDDL(dbsetting, "02_CreateIndex_zSCP2_Performance.sql", asm);

            // === Tier 9: views (always drop-and-recreate) ===
            RecreateViews(dbsetting, asm);

            // === Tier 10: seed defaults (safe re-run; insert-if-empty) ===
            try
            {
                string seedDDL = ReadEmbeddedSql("04_Seed_zSCP_LK_Defaults.sql", asm);
                var dbu = DBUtils.Create(dbsetting);
                dbu.ExecuteDDLText(seedDDL);
            }
            catch (Exception ex)
            {
                // Non-fatal — seed is best-effort.
                System.Diagnostics.Debug.WriteLine("ScpMigrations seed failed: " + ex.Message);
            }

            // === Expiry auto-retire (runs on every plugin load) ===
            // A contract whose expiry MONTH is completely over flips to Inactive automatically,
            // stamped "Expired (auto)". The cutoff is the FIRST day of the CURRENT month — never
            // earlier — so the final month stays billable right up to its billing day. Leftover
            // un-invoiced readings of an auto-retired contract remain reachable via the Meter
            // Reading Setting "Include INACTIVE contracts / items".
            try
            {
                dbsetting.ExecuteNonQuery(
                    "UPDATE dbo.zSCP2_Contract SET Inactive='Y', InactiveDate=ServiceExpiryDate, " +
                    "InactiveReason='Expired (auto)' " +
                    "WHERE Inactive='N' AND ServiceExpiryDate IS NOT NULL " +
                    "AND ServiceExpiryDate < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ScpMigrations expiry auto-retire failed: " + ex.Message);
            }

            return true;
        }

        private static void RunDDL(DBSetting dbsetting, string sqlFile, Assembly asm)
        {
            // Runs an embedded DDL script unconditionally. The script itself must be idempotent
            // (guarded with IF EXISTS / IF NOT EXISTS) so it is safe on every plugin load.
            string ddl = ReadEmbeddedSql(sqlFile, asm);
            var dbu = DBUtils.Create(dbsetting);
            dbu.ExecuteDDLText(ddl);
        }

        private static void RunIfTableMissing(DBSetting dbsetting, string tableName, string sqlFile, Assembly asm)
        {
            string query = "SELECT COUNT(*) FROM dbo.sysobjects WHERE id = object_id(N'[dbo].["
                           + tableName + "]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1";
            object obj = dbsetting.ExecuteScalar(query);
            if (obj == null || obj == DBNull.Value || Convert.ToInt32(obj) == 0)
            {
                string ddl = ReadEmbeddedSql(sqlFile, asm);
                var dbu = DBUtils.Create(dbsetting);
                dbu.ExecuteDDLText(ddl);
            }
        }

        private static void RecreateViews(DBSetting dbsetting, Assembly asm)
        {
            // Drop existing views then create fresh so schema changes propagate.
            string[] views = new[]
            {
                "zvSCP_ServiceContractList",
                "zvSCP_ServiceNoteList",
                "zvSCP_ServiceItemList",
                "zvSCP_AppointmentCalendar",
                "zvSCP_OutstandingServiceContractItem",
                "zvSCP_OutstandingServiceNoteAssignment",
                "zvSCP2_ContractList",
            };

            var dbu = DBUtils.Create(dbsetting);
            foreach (var v in views)
            {
                try
                {
                    dbu.ExecuteDDLText("IF OBJECT_ID('dbo." + v + "', 'V') IS NOT NULL DROP VIEW [dbo].[" + v + "]");
                }
                catch { /* best-effort drop */ }
            }

            string viewDDL = ReadEmbeddedSql("03_CreateView_zvSCP_Views.sql", asm);
            dbu.ExecuteDDLText(viewDDL);

            // v2 combined-module list view (separate file).
            string viewDDL2 = ReadEmbeddedSql("03_CreateView_zvSCP2_ContractList.sql", asm);
            dbu.ExecuteDDLText(viewDDL2);
        }

        private static string ReadEmbeddedSql(string fileName, Assembly asm)
        {
            // EmbeddedResource names are <RootNamespace>.<FolderWithDots>.<FileName>
            string resourceName = NameSpace + ".SQL." + fileName;
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new Exception("Embedded SQL resource not found: " + resourceName
                                        + ". Check that the .sql file is marked as EmbeddedResource in the .csproj.");
                }
                using (var reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
