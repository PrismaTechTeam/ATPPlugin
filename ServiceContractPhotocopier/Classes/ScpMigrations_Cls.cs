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
            RunIfTableMissing(dbsetting, "zSCP2_ItemMeter",             "02_CreateTable_zSCP2_ItemMeter.sql", asm);
            RunIfTableMissing(dbsetting, "zSCP2_ItemCode",              "02_CreateTable_zSCP2_ItemCode.sql", asm);
            // Staging of current readings (manual key-ins + accepted API values) per billing period.
            RunIfTableMissing(dbsetting, "zSCP2_MeterEntry",            "02_CreateTable_zSCP2_MeterEntry.sql", asm);
            // Add Invoiced stamp columns (billing-period duplicate guard). Idempotent.
            RunDDL(dbsetting, "02_Update_zSCP2_MeterEntry_v2.sql", asm);
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
