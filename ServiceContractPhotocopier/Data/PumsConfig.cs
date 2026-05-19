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
