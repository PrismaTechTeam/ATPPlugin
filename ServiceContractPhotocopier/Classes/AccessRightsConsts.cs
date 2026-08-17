namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Access right keys for the Service & Contract Photocopier plugin.
    /// Registered in PluginMain.BeforeLoad via AccessRightMap.AddAccessRightRecord.
    /// Convention: GROUP_SCP + CMD_SHOW_SCP_xxx + CMD_OPEN_SCP_xxx per form.
    /// </summary>
    public static class AccessRightsConsts
    {
        public const string GROUP_SCP = nameof(GROUP_SCP);

        // ---- Service Contract ----
        public const string CMD_SHOW_SCP_CONTRACT = nameof(CMD_SHOW_SCP_CONTRACT);
        public const string CMD_OPEN_SCP_CONTRACT = nameof(CMD_OPEN_SCP_CONTRACT);
        public const string CMD_SHOW_SCP_CONTRACT_INQUIRY = nameof(CMD_SHOW_SCP_CONTRACT_INQUIRY);
        public const string CMD_OPEN_SCP_CONTRACT_INQUIRY = nameof(CMD_OPEN_SCP_CONTRACT_INQUIRY);
        public const string CMD_SHOW_SCP_OUTSTANDING_CONTRACT_ITEM = nameof(CMD_SHOW_SCP_OUTSTANDING_CONTRACT_ITEM);
        public const string CMD_OPEN_SCP_OUTSTANDING_CONTRACT_ITEM = nameof(CMD_OPEN_SCP_OUTSTANDING_CONTRACT_ITEM);

        // ---- Service Item ----
        public const string CMD_SHOW_SCP_ITEM = nameof(CMD_SHOW_SCP_ITEM);
        public const string CMD_OPEN_SCP_ITEM = nameof(CMD_OPEN_SCP_ITEM);
        public const string CMD_SHOW_SCP_ITEM_INQUIRY = nameof(CMD_SHOW_SCP_ITEM_INQUIRY);
        public const string CMD_OPEN_SCP_ITEM_INQUIRY = nameof(CMD_OPEN_SCP_ITEM_INQUIRY);
        public const string CMD_SHOW_SCP_ITEM_TAG_SEARCH = nameof(CMD_SHOW_SCP_ITEM_TAG_SEARCH);
        public const string CMD_OPEN_SCP_ITEM_TAG_SEARCH = nameof(CMD_OPEN_SCP_ITEM_TAG_SEARCH);
        public const string CMD_SHOW_SCP_GENERATE_ITEM_FROM_SERIAL = nameof(CMD_SHOW_SCP_GENERATE_ITEM_FROM_SERIAL);
        public const string CMD_OPEN_SCP_GENERATE_ITEM_FROM_SERIAL = nameof(CMD_OPEN_SCP_GENERATE_ITEM_FROM_SERIAL);
        public const string CMD_SHOW_SCP_RESET_ITEM_DEBTOR = nameof(CMD_SHOW_SCP_RESET_ITEM_DEBTOR);
        public const string CMD_OPEN_SCP_RESET_ITEM_DEBTOR = nameof(CMD_OPEN_SCP_RESET_ITEM_DEBTOR);

        // ---- Service Note ----
        public const string CMD_SHOW_SCP_NOTE = nameof(CMD_SHOW_SCP_NOTE);
        public const string CMD_OPEN_SCP_NOTE = nameof(CMD_OPEN_SCP_NOTE);
        public const string CMD_SHOW_SCP_NOTE_QUICK_ENTRY = nameof(CMD_SHOW_SCP_NOTE_QUICK_ENTRY);
        public const string CMD_OPEN_SCP_NOTE_QUICK_ENTRY = nameof(CMD_OPEN_SCP_NOTE_QUICK_ENTRY);
        public const string CMD_SHOW_SCP_NOTE_CLOSING = nameof(CMD_SHOW_SCP_NOTE_CLOSING);
        public const string CMD_OPEN_SCP_NOTE_CLOSING = nameof(CMD_OPEN_SCP_NOTE_CLOSING);
        public const string CMD_SHOW_SCP_NOTE_ASSIGNMENT = nameof(CMD_SHOW_SCP_NOTE_ASSIGNMENT);
        public const string CMD_OPEN_SCP_NOTE_ASSIGNMENT = nameof(CMD_OPEN_SCP_NOTE_ASSIGNMENT);
        public const string CMD_SHOW_SCP_NOTE_INQUIRY = nameof(CMD_SHOW_SCP_NOTE_INQUIRY);
        public const string CMD_OPEN_SCP_NOTE_INQUIRY = nameof(CMD_OPEN_SCP_NOTE_INQUIRY);
        public const string CMD_SHOW_SCP_OUTSTANDING_NOTE_ASSIGNMENT = nameof(CMD_SHOW_SCP_OUTSTANDING_NOTE_ASSIGNMENT);
        public const string CMD_OPEN_SCP_OUTSTANDING_NOTE_ASSIGNMENT = nameof(CMD_OPEN_SCP_OUTSTANDING_NOTE_ASSIGNMENT);

        // ---- Service Appointment ----
        public const string CMD_SHOW_SCP_APPOINTMENT = nameof(CMD_SHOW_SCP_APPOINTMENT);
        public const string CMD_OPEN_SCP_APPOINTMENT = nameof(CMD_OPEN_SCP_APPOINTMENT);
        public const string CMD_SHOW_SCP_APPOINTMENT_INQUIRY = nameof(CMD_SHOW_SCP_APPOINTMENT_INQUIRY);
        public const string CMD_OPEN_SCP_APPOINTMENT_INQUIRY = nameof(CMD_OPEN_SCP_APPOINTMENT_INQUIRY);
        public const string CMD_SHOW_SCP_PREVENTIVE_MAINT = nameof(CMD_SHOW_SCP_PREVENTIVE_MAINT);
        public const string CMD_OPEN_SCP_PREVENTIVE_MAINT = nameof(CMD_OPEN_SCP_PREVENTIVE_MAINT);
        public const string CMD_SHOW_SCP_METER_TYPE_TRANS = nameof(CMD_SHOW_SCP_METER_TYPE_TRANS);
        public const string CMD_OPEN_SCP_METER_TYPE_TRANS = nameof(CMD_OPEN_SCP_METER_TYPE_TRANS);

        // ---- Quick View ----
        public const string CMD_SHOW_SCP_QUICK_VIEW = nameof(CMD_SHOW_SCP_QUICK_VIEW);
        public const string CMD_OPEN_SCP_QUICK_VIEW = nameof(CMD_OPEN_SCP_QUICK_VIEW);

        // ---- General Setup — people ----
        public const string CMD_SHOW_SCP_SETUP_PERSON = nameof(CMD_SHOW_SCP_SETUP_PERSON);
        public const string CMD_OPEN_SCP_SETUP_PERSON = nameof(CMD_OPEN_SCP_SETUP_PERSON);
        public const string CMD_SHOW_SCP_SETUP_ADVISOR = nameof(CMD_SHOW_SCP_SETUP_ADVISOR);
        public const string CMD_OPEN_SCP_SETUP_ADVISOR = nameof(CMD_OPEN_SCP_SETUP_ADVISOR);
        public const string CMD_SHOW_SCP_SETUP_MECHANIC = nameof(CMD_SHOW_SCP_SETUP_MECHANIC);
        public const string CMD_OPEN_SCP_SETUP_MECHANIC = nameof(CMD_OPEN_SCP_SETUP_MECHANIC);

        // ---- General Setup — lookups ----
        public const string CMD_SHOW_SCP_SETUP_STATUS = nameof(CMD_SHOW_SCP_SETUP_STATUS);
        public const string CMD_OPEN_SCP_SETUP_STATUS = nameof(CMD_OPEN_SCP_SETUP_STATUS);
        public const string CMD_SHOW_SCP_SETUP_SEVERITY = nameof(CMD_SHOW_SCP_SETUP_SEVERITY);
        public const string CMD_OPEN_SCP_SETUP_SEVERITY = nameof(CMD_OPEN_SCP_SETUP_SEVERITY);
        public const string CMD_SHOW_SCP_SETUP_SOLUTION = nameof(CMD_SHOW_SCP_SETUP_SOLUTION);
        public const string CMD_OPEN_SCP_SETUP_SOLUTION = nameof(CMD_OPEN_SCP_SETUP_SOLUTION);
        public const string CMD_SHOW_SCP_SETUP_PROBLEM = nameof(CMD_SHOW_SCP_SETUP_PROBLEM);
        public const string CMD_OPEN_SCP_SETUP_PROBLEM = nameof(CMD_OPEN_SCP_SETUP_PROBLEM);
        public const string CMD_SHOW_SCP_SETUP_TYPE = nameof(CMD_SHOW_SCP_SETUP_TYPE);
        public const string CMD_OPEN_SCP_SETUP_TYPE = nameof(CMD_OPEN_SCP_SETUP_TYPE);
        public const string CMD_SHOW_SCP_SETUP_CONTRACT_TYPE = nameof(CMD_SHOW_SCP_SETUP_CONTRACT_TYPE);
        public const string CMD_OPEN_SCP_SETUP_CONTRACT_TYPE = nameof(CMD_OPEN_SCP_SETUP_CONTRACT_TYPE);
        public const string CMD_SHOW_SCP_SETUP_ITEM_GRADE = nameof(CMD_SHOW_SCP_SETUP_ITEM_GRADE);
        public const string CMD_OPEN_SCP_SETUP_ITEM_GRADE = nameof(CMD_OPEN_SCP_SETUP_ITEM_GRADE);
        public const string CMD_SHOW_SCP_SETUP_APPOINTMENT_TYPE = nameof(CMD_SHOW_SCP_SETUP_APPOINTMENT_TYPE);
        public const string CMD_OPEN_SCP_SETUP_APPOINTMENT_TYPE = nameof(CMD_OPEN_SCP_SETUP_APPOINTMENT_TYPE);
        public const string CMD_SHOW_SCP_SETUP_APPOINTMENT_PRIORITY = nameof(CMD_SHOW_SCP_SETUP_APPOINTMENT_PRIORITY);
        public const string CMD_OPEN_SCP_SETUP_APPOINTMENT_PRIORITY = nameof(CMD_OPEN_SCP_SETUP_APPOINTMENT_PRIORITY);

        // ---- General Setup — meter ----
        public const string CMD_SHOW_SCP_SETUP_METER_TYPE = nameof(CMD_SHOW_SCP_SETUP_METER_TYPE);
        public const string CMD_OPEN_SCP_SETUP_METER_TYPE = nameof(CMD_OPEN_SCP_SETUP_METER_TYPE);
        public const string CMD_SHOW_SCP_SETUP_METER_MULTI_PRICE = nameof(CMD_SHOW_SCP_SETUP_METER_MULTI_PRICE);
        public const string CMD_OPEN_SCP_SETUP_METER_MULTI_PRICE = nameof(CMD_OPEN_SCP_SETUP_METER_MULTI_PRICE);
        // Named invoice layouts (how many invoices a contract produces, how its lines group).
        public const string CMD_SHOW_SCP_SETUP_BILLING_FORMAT = nameof(CMD_SHOW_SCP_SETUP_BILLING_FORMAT);
        public const string CMD_OPEN_SCP_SETUP_BILLING_FORMAT = nameof(CMD_OPEN_SCP_SETUP_BILLING_FORMAT);

        // ---- General Setup — strategy ----
        public const string CMD_SHOW_SCP_SETUP_STRATEGY = nameof(CMD_SHOW_SCP_SETUP_STRATEGY);
        public const string CMD_OPEN_SCP_SETUP_STRATEGY = nameof(CMD_OPEN_SCP_SETUP_STRATEGY);

        // ---- Rental Maintenance ----
        public const string CMD_SHOW_SCP_RENTAL_MAINT = nameof(CMD_SHOW_SCP_RENTAL_MAINT);
        public const string CMD_OPEN_SCP_RENTAL_MAINT = nameof(CMD_OPEN_SCP_RENTAL_MAINT);

        // ---- Service Option ----
        public const string CMD_SHOW_SCP_OPTION = nameof(CMD_SHOW_SCP_OPTION);
        public const string CMD_OPEN_SCP_OPTION = nameof(CMD_OPEN_SCP_OPTION);
    }
}
