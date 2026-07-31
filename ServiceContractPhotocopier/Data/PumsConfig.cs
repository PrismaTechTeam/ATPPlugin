using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Data
{
    /// <summary>
    /// Tiny key/value config table for PUMS-related plugin settings.
    /// Schema: Z_PumsConfig(ConfigKey NVARCHAR(50) PK, ConfigValue NVARCHAR(MAX) NULL)
    /// </summary>
    public static class PumsConfig
    {
        public const string KEY_DEFAULT_FROM_LOCATION = "DEFAULT_FROM_LOCATION";
        public const string DEFAULT_FROM_LOCATION_VALUE = "HQ";

        /// <summary>
        /// FLAG_CONTROL — governs how the webhook handles a re-push of an already-open ID.
        /// false (default): drop the duplicate silently.
        /// true: delete the open prior row and insert the new payload as Update.
        /// </summary>
        public const string KEY_FLAG_CONTROL = "FLAG_CONTROL";

        // === ATPApi webhook/update service (used by the About dialog) ===

        /// <summary>Base URL of the local ATPApi service (used by the About dialog to read /api/ping
        /// for the API version and to point the updater's health-check). No trailing path.</summary>
        public const string KEY_ATP_API_BASE_URL = "ATP_API_BASE_URL";
        public const string DEFAULT_ATP_API_BASE_URL = "http://localhost:5007";

        // === Meter Reading API integration (combined Service Contract module v2) ===

        /// <summary>Base URL of the PUMS meter-reading API (no trailing path). Default points to a local mock.</summary>
        public const string KEY_METER_API_BASE_URL = "METER_API_BASE_URL";
        public const string DEFAULT_METER_API_BASE_URL = "http://localhost:8090";

        /// <summary>Auth token appended as the ?token= query parameter on every meter-reading API call.</summary>
        public const string KEY_METER_API_KEY = "METER_API_KEY";

        /// <summary>"MOCK" (default) = deterministic local data; "LIVE" = call the real HTTP endpoints.</summary>
        public const string KEY_METER_API_MODE = "METER_API_MODE";
        public const string METER_API_MODE_MOCK = "MOCK";
        public const string METER_API_MODE_LIVE = "LIVE";

        /// <summary>HTTP timeout (milliseconds) for meter-reading API calls.</summary>
        public const string KEY_METER_API_TIMEOUT_MS = "METER_API_TIMEOUT_MS";
        public const int DEFAULT_METER_API_TIMEOUT_MS = 15000;

        /// <summary>Name of the active zSCP2_ApiProfile row (Plugin Option &gt; API tab). Applying a
        /// profile copies its BaseUrl/Token/Mode/Timeout into the four METER_API_* keys above.</summary>
        public const string KEY_METER_API_ACTIVE_PROFILE = "METER_API_ACTIVE_PROFILE";

        /// <summary>Default day-of-month (1..31) used when creating a new contract.</summary>
        public const string KEY_DEFAULT_BILLING_DAY = "DEFAULT_BILLING_DAY";
        public const int DEFAULT_BILLING_DAY_VALUE = 1;

        /// <summary>Default billing mode for a new contract: 'G' grouped one invoice, 'S' separate per item.</summary>
        public const string KEY_DEFAULT_BILLING_MODE = "DEFAULT_BILLING_MODE";
        public const string DEFAULT_BILLING_MODE_VALUE = "G";

        /// <summary>Whether the Meter Reading list includes expired service items. Default true = show
        /// them (billing unchanged); set false (Meter Reading &gt; Setting) to hide expired machines.</summary>
        public const string KEY_INCLUDE_EXPIRED_ITEMS = "INCLUDE_EXPIRED_ITEMS";
        public const bool DEFAULT_INCLUDE_EXPIRED_ITEMS = true;
        // Show meters of INACTIVE contracts/items on the Meter Reading list (off by default) — used
        // to review, and if needed bill, the leftover un-invoiced readings of a stopped contract.
        public const string KEY_INCLUDE_INACTIVE = "INCLUDE_INACTIVE";
        public const bool DEFAULT_INCLUDE_INACTIVE = false;

        /// <summary>Whether Fetch accepts readings audited AFTER the selected billing day (they show
        /// a RED Last Audit Date = invoice not created on time). Default true; set false (Meter
        /// Reading &gt; Setting) to only match readings on/before the billing day.</summary>
        public const string KEY_INCLUDE_LATE_READINGS = "INCLUDE_LATE_READINGS";
        public const bool DEFAULT_INCLUDE_LATE_READINGS = true;

        // === Billing-day AUTO-FETCH snapshot (background, Meter Reading > Setting) ===

        /// <summary>Enable the background auto-fetch: on each billing day the readings are snapshotted
        /// as of the cutoff and LOCKED (no later override).</summary>
        public const string KEY_AUTO_FETCH_ENABLED = "AUTO_FETCH_ENABLED";
        public const bool DEFAULT_AUTO_FETCH_ENABLED = false;

        /// <summary>Cutoff TIME (HH:mm) for the auto-fetch snapshot.</summary>
        public const string KEY_AUTO_FETCH_CUTOFF = "AUTO_FETCH_CUTOFF";
        public const string DEFAULT_AUTO_FETCH_CUTOFF = "23:59";

        /// <summary>Which day the cutoff falls on: "BEFORE" = day before the billing day (billing day
        /// 7 → readings up to the 6th 23:59), "ON" = the billing day itself (up to the 7th 23:59).</summary>
        public const string KEY_AUTO_FETCH_CUTOFF_DAY = "AUTO_FETCH_CUTOFF_DAY";
        public const string DEFAULT_AUTO_FETCH_CUTOFF_DAY = "BEFORE";

        /// <summary>Native Document Numbering Format (DocType IV) used by meter-billing invoices.
        /// Configurable in Service Option; the builder falls back to the IV default when the named
        /// format doesn't exist in dbo.DocNoFormat.</summary>
        public const string KEY_METER_INVOICE_DOCNO_FORMAT = "METER_INVOICE_DOCNO_FORMAT";
        public const string DEFAULT_METER_INVOICE_DOCNO_FORMAT = "MR FORMAT";

        // Invoice line description source (user decision 2026-07-27): DEFAULT = the AutoCount stock
        // item's description (matches the customer's master invoices, e.g. "BK COPY + PRINT A4&A3");
        // OFF = the meter type name (the old plugin behavior).
        public const string KEY_INVOICE_DESC_FROM_ITEM = "INVOICE_DESC_FROM_ITEM";
        public const bool DEFAULT_INVOICE_DESC_FROM_ITEM = true;

        // Bulk Email Invoice message template (demo 28/07 #9a) - set once, used on every run.
        public const string KEY_BULKMAIL_SUBJECT = "BULKMAIL_SUBJECT";
        public const string KEY_BULKMAIL_BODY = "BULKMAIL_BODY";
        public const string DEFAULT_BULKMAIL_SUBJECT = "Invoice {DocNos}";
        public const string DEFAULT_BULKMAIL_BODY = "Dear {CompanyName},\r\n\r\nPlease find attached your invoice(s): {DocNos}.\r\n\r\nThank you.";

        // ADVANCED invoice numbering (user request 2026-07-27): pick a DIFFERENT IV Document
        // Numbering Format depending on the billed machines' API status — ONLINE machines get one
        // format, OFFLINE another; rows with no API status fall back to the default format above.
        public const string KEY_INV_FORMAT_ADVANCED = "INV_FORMAT_ADVANCED";
        public const string KEY_INV_FORMAT_ONLINE = "INV_FORMAT_ONLINE";
        public const string KEY_INV_FORMAT_OFFLINE = "INV_FORMAT_OFFLINE";

        /// <summary>Reads an int config value, falling back to <paramref name="defaultValue"/> on missing/invalid.</summary>
        public static int GetInt(DBSetting db, string key, int defaultValue)
        {
            string v = Get(db, key, null);
            int n;
            if (v != null && int.TryParse(v.Trim(), out n)) return n;
            return defaultValue;
        }

        public static bool GetBool(DBSetting db, string key, bool defaultValue)
        {
            string v = Get(db, key, defaultValue ? "1" : "0");
            if (v == null) return defaultValue;
            v = v.Trim();
            return v == "1" || string.Equals(v, "true", System.StringComparison.OrdinalIgnoreCase);
        }

        public static void SetBool(DBSetting db, string key, bool value)
        {
            Set(db, key, value ? "1" : "0");
        }

        public static void EnsureTable(DBSetting db)
        {
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Z_PumsConfig')
                    BEGIN
                        CREATE TABLE Z_PumsConfig (
                            ConfigKey   NVARCHAR(50)  NOT NULL PRIMARY KEY,
                            ConfigValue NVARCHAR(MAX) NULL
                        )
                    END", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public static string Get(DBSetting db, string key, string defaultValue)
        {
            EnsureTable(db);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ConfigValue FROM Z_PumsConfig WHERE ConfigKey = @k", conn))
            {
                cmd.Parameters.AddWithValue("@k", key);
                conn.Open();
                object o = cmd.ExecuteScalar();
                if (o == null || o == System.DBNull.Value) return defaultValue;
                string s = o.ToString();
                return string.IsNullOrWhiteSpace(s) ? defaultValue : s;
            }
        }

        public static void Set(DBSetting db, string key, string value)
        {
            EnsureTable(db);
            using (SqlConnection conn = new SqlConnection(db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(@"
                MERGE Z_PumsConfig AS t
                USING (SELECT @k AS ConfigKey) AS s
                  ON t.ConfigKey = s.ConfigKey
                WHEN MATCHED THEN UPDATE SET ConfigValue = @v
                WHEN NOT MATCHED THEN INSERT (ConfigKey, ConfigValue) VALUES (@k, @v);", conn))
            {
                cmd.Parameters.AddWithValue("@k", key);
                cmd.Parameters.AddWithValue("@v", (object)(value ?? string.Empty));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
