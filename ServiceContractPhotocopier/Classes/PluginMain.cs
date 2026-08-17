using System;
using AutoCount.Authentication;
using AutoCount.PlugIn;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Plugin entry point. AutoCount discovers this class via the BasePlugIn subclass + matching
    /// AssemblyFile name in the .appp. BeforeLoad runs once when the account book opens:
    ///   1) run embedded SQL migrations (create tables, views, seed lookups)
    ///   2) register access rights
    ///   3) set the main menu caption
    /// </summary>
    public class PluginMain : BasePlugIn
    {
        private const string PLUGIN_GUID = "6A996121-169E-4D35-AEED-58CFBB1386B7";
        private const string PLUGIN_NAME = "Service Contract Photocopier";
        private const string PLUGIN_VERSION = "1.0.0.0";
        private const string PLUGIN_CONTACT = "support@ruisin.local";

        public PluginMain()
            : base(new Guid(PLUGIN_GUID), PLUGIN_NAME, PLUGIN_VERSION, PLUGIN_CONTACT)
        {
            SetMinimumAccountingVersionRequired("2.0.2");
            SetDevExpressComponentVersionRequired("22.2.7");
        }

        public override bool BeforeLoad(BeforeLoadArgs e)
        {
            e.MainMenuCaption = "Service && Contract";

            try
            {
                ScpMigrations_Cls.RunEmbeddedSQLScripts(e.DBSetting);
            }
            catch (Exception ex)
            {
                // Migration failure is fatal — surface to user and abort plugin load.
                AutoCount.AppMessage.ShowErrorMessage(null,
                    "Service & Contract Photocopier plugin failed to initialize database schema:\r\n\r\n" +
                    ex.Message);
                return false;
            }

            // AutoCount User Defined Fields the plugin depends on. Separate from the SQL migrations
            // because a UDF must be registered through AutoCount's SDK (metadata + ALTER in one
            // transaction) or its screens and report designer will not see it. Idempotent and never
            // fatal — a missing UDF costs a feature, not the module.
            try { ScpUdf_Cls.EnsureUdfs(e.DBSetting); } catch { }

            RegisterAccessRights();

            // Billing-day auto-fetch snapshot service (background while AutoCount is open; it
            // no-ops unless enabled in Meter Reading > Setting).
            try { ScpAutoFetchService.Start(e.DBSetting); } catch { }

            // #10: real-time watch on Sales CN saves/deletes — a correction CN edited/deleted
            // anywhere in AutoCount alerts (and rolls back) IMMEDIATELY, not on next screen load.
            try { ScpCnWatcher.Start(e.DBSetting); } catch { }

            return true;
        }

        public override void AfterUnload(BaseArgs e)
        {
            // AccessRightMap entries are process-wide; nothing to undo here.
            try { ScpCnWatcher.Stop(); } catch { }
        }

        private static void RegisterAccessRights()
        {
            // Group
            AccessRightMap.AddAccessRightRecord(new AccessRightRecord(
                AccessRightsConsts.GROUP_SCP, null, "SERVICE & CONTRACT PHOTOCOPIER"));

            // --- Service Contract ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_CONTRACT, "SHOW MAINTAIN SERVICE CONTRACT");
            Add(AccessRightsConsts.CMD_OPEN_SCP_CONTRACT, "OPEN MAINTAIN SERVICE CONTRACT");
            Add(AccessRightsConsts.CMD_SHOW_SCP_CONTRACT_INQUIRY, "SHOW SERVICE CONTRACT INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_CONTRACT_INQUIRY, "OPEN SERVICE CONTRACT INQUIRY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_OUTSTANDING_CONTRACT_ITEM, "SHOW OUTSTANDING SERVICE CONTRACT ITEM INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_OUTSTANDING_CONTRACT_ITEM, "OPEN OUTSTANDING SERVICE CONTRACT ITEM INQUIRY");

            // --- Service Item ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_ITEM, "SHOW MAINTAIN SERVICE ITEM");
            Add(AccessRightsConsts.CMD_OPEN_SCP_ITEM, "OPEN MAINTAIN SERVICE ITEM");
            Add(AccessRightsConsts.CMD_SHOW_SCP_ITEM_INQUIRY, "SHOW SERVICE ITEM INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_ITEM_INQUIRY, "OPEN SERVICE ITEM INQUIRY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_ITEM_TAG_SEARCH, "SHOW SERVICE ITEM TAG SEARCH");
            Add(AccessRightsConsts.CMD_OPEN_SCP_ITEM_TAG_SEARCH, "OPEN SERVICE ITEM TAG SEARCH");
            Add(AccessRightsConsts.CMD_SHOW_SCP_GENERATE_ITEM_FROM_SERIAL, "SHOW GENERATE SERVICE ITEM FROM SERIAL");
            Add(AccessRightsConsts.CMD_OPEN_SCP_GENERATE_ITEM_FROM_SERIAL, "OPEN GENERATE SERVICE ITEM FROM SERIAL");
            Add(AccessRightsConsts.CMD_SHOW_SCP_RESET_ITEM_DEBTOR, "SHOW RESET SERVICE ITEM DEBTOR OWNERSHIP");
            Add(AccessRightsConsts.CMD_OPEN_SCP_RESET_ITEM_DEBTOR, "OPEN RESET SERVICE ITEM DEBTOR OWNERSHIP");

            // --- Service Note ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_NOTE, "SHOW SERVICE NOTE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_NOTE, "OPEN SERVICE NOTE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_NOTE_QUICK_ENTRY, "SHOW SERVICE NOTE QUICK ENTRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_NOTE_QUICK_ENTRY, "OPEN SERVICE NOTE QUICK ENTRY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_NOTE_CLOSING, "SHOW SERVICE NOTE CLOSING");
            Add(AccessRightsConsts.CMD_OPEN_SCP_NOTE_CLOSING, "OPEN SERVICE NOTE CLOSING");
            Add(AccessRightsConsts.CMD_SHOW_SCP_NOTE_ASSIGNMENT, "SHOW SERVICE NOTE ASSIGNMENT");
            Add(AccessRightsConsts.CMD_OPEN_SCP_NOTE_ASSIGNMENT, "OPEN SERVICE NOTE ASSIGNMENT");
            Add(AccessRightsConsts.CMD_SHOW_SCP_NOTE_INQUIRY, "SHOW SERVICE NOTE INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_NOTE_INQUIRY, "OPEN SERVICE NOTE INQUIRY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_OUTSTANDING_NOTE_ASSIGNMENT, "SHOW OUTSTANDING SERVICE NOTE ASSIGNMENT INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_OUTSTANDING_NOTE_ASSIGNMENT, "OPEN OUTSTANDING SERVICE NOTE ASSIGNMENT INQUIRY");

            // --- Service Appointment ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_APPOINTMENT, "SHOW SERVICE APPOINTMENT");
            Add(AccessRightsConsts.CMD_OPEN_SCP_APPOINTMENT, "OPEN SERVICE APPOINTMENT");
            Add(AccessRightsConsts.CMD_SHOW_SCP_APPOINTMENT_INQUIRY, "SHOW SERVICE APPOINTMENT INQUIRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_APPOINTMENT_INQUIRY, "OPEN SERVICE APPOINTMENT INQUIRY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_PREVENTIVE_MAINT, "SHOW PREVENTIVE MAINTENANCE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_PREVENTIVE_MAINT, "OPEN PREVENTIVE MAINTENANCE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_METER_TYPE_TRANS, "SHOW METER TYPE TRANSACTION ENTRY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_METER_TYPE_TRANS, "OPEN METER TYPE TRANSACTION ENTRY");

            // --- Quick View ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_QUICK_VIEW, "SHOW SERVICE QUICK VIEW");
            Add(AccessRightsConsts.CMD_OPEN_SCP_QUICK_VIEW, "OPEN SERVICE QUICK VIEW");

            // --- General Setup: people ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_PERSON, "SHOW SETUP SERVICE PERSON");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_PERSON, "OPEN SETUP SERVICE PERSON");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_ADVISOR, "SHOW SETUP SERVICE ADVISOR");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_ADVISOR, "OPEN SETUP SERVICE ADVISOR");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_MECHANIC, "SHOW SETUP MECHANIC");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_MECHANIC, "OPEN SETUP MECHANIC");

            // --- General Setup: lookups ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_STATUS, "SHOW SETUP SERVICE STATUS");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_STATUS, "OPEN SETUP SERVICE STATUS");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_SEVERITY, "SHOW SETUP SERVICE SEVERITY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_SEVERITY, "OPEN SETUP SERVICE SEVERITY");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_SOLUTION, "SHOW SETUP SERVICE SOLUTION");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_SOLUTION, "OPEN SETUP SERVICE SOLUTION");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_PROBLEM, "SHOW SETUP SERVICE PROBLEM");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_PROBLEM, "OPEN SETUP SERVICE PROBLEM");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_TYPE, "SHOW SETUP SERVICE TYPE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_TYPE, "OPEN SETUP SERVICE TYPE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_CONTRACT_TYPE, "SHOW SETUP SERVICE CONTRACT TYPE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_CONTRACT_TYPE, "OPEN SETUP SERVICE CONTRACT TYPE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_ITEM_GRADE, "SHOW SETUP SERVICE ITEM GRADE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_ITEM_GRADE, "OPEN SETUP SERVICE ITEM GRADE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_APPOINTMENT_TYPE, "SHOW SETUP APPOINTMENT TYPE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_APPOINTMENT_TYPE, "OPEN SETUP APPOINTMENT TYPE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_APPOINTMENT_PRIORITY, "SHOW SETUP APPOINTMENT PRIORITY");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_APPOINTMENT_PRIORITY, "OPEN SETUP APPOINTMENT PRIORITY");

            // --- General Setup: meter ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_METER_TYPE, "SHOW SETUP METER TYPE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_METER_TYPE, "OPEN SETUP METER TYPE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_METER_MULTI_PRICE, "SHOW SETUP METER MULTI PRICING");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_METER_MULTI_PRICE, "OPEN SETUP METER MULTI PRICING");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_BILLING_FORMAT, "SHOW SETUP BILLING FORMAT");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_BILLING_FORMAT, "OPEN SETUP BILLING FORMAT");
            Add(AccessRightsConsts.CMD_SHOW_SCP_SETUP_STRATEGY, "SHOW SETUP STRATEGY MAINTENANCE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_SETUP_STRATEGY, "OPEN SETUP STRATEGY MAINTENANCE");
            Add(AccessRightsConsts.CMD_SHOW_SCP_RENTAL_MAINT, "SHOW RENTAL MAINTENANCE");
            Add(AccessRightsConsts.CMD_OPEN_SCP_RENTAL_MAINT, "OPEN RENTAL MAINTENANCE");

            // --- Service Option ---
            Add(AccessRightsConsts.CMD_SHOW_SCP_OPTION, "SHOW SERVICE OPTION");
            Add(AccessRightsConsts.CMD_OPEN_SCP_OPTION, "OPEN SERVICE OPTION");
        }

        private static void Add(string cmd, string display)
        {
            AccessRightMap.AddAccessRightRecord(new AccessRightRecord(cmd, AccessRightsConsts.GROUP_SCP, display));
        }
    }
}
