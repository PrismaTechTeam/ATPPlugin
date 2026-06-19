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

        /// <summary>Value sent in the X-API-Key header on every meter-reading API call.</summary>
        public const string KEY_METER_API_KEY = "METER_API_KEY";

        /// <summary>"MOCK" (default) = deterministic local data; "LIVE" = call the real HTTP endpoints.</summary>
        public const string KEY_METER_API_MODE = "METER_API_MODE";
        public const string METER_API_MODE_MOCK = "MOCK";
        public const string METER_API_MODE_LIVE = "LIVE";

        /// <summary>HTTP timeout (milliseconds) for meter-reading API calls.</summary>
        public const string KEY_METER_API_TIMEOUT_MS = "METER_API_TIMEOUT_MS";
        public const int DEFAULT_METER_API_TIMEOUT_MS = 15000;

        /// <summary>Default day-of-month (1..31) used when creating a new contract.</summary>
        public const string KEY_DEFAULT_BILLING_DAY = "DEFAULT_BILLING_DAY";
        public const int DEFAULT_BILLING_DAY_VALUE = 1;

        /// <summary>Default billing mode for a new contract: 'G' grouped one invoice, 'S' separate per item.</summary>
        public const string KEY_DEFAULT_BILLING_MODE = "DEFAULT_BILLING_MODE";
        public const string DEFAULT_BILLING_MODE_VALUE = "G";

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
