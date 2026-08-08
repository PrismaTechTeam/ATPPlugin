using System;
using AutoCount.Data;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Ensures the AutoCount **User Defined Fields** this plugin depends on exist in whatever
    /// account book it is loaded into — the UDF twin of <see cref="ScpMigrations_Cls"/>.
    ///
    /// Why this is code and not a .sql migration file (the one documented exception to the
    /// "every schema change gets a versioned .sql" rule): a UDF is NOT just a column. AutoCount
    /// registers it in dbo.UDF and alters the table in ONE transaction, and only fields created
    /// that way appear in AutoCount's screens, grids and report designer. A raw ALTER TABLE
    /// produces a column AutoCount will not recognise. So we go through the SDK's UDFTable —
    /// the same code path as the "Manage User Defined Field" screen.
    ///
    /// Idempotent: an already-registered field is skipped, so this runs safely on every load, in
    /// every book, forever. Failures NEVER block the plugin — a missing UDF costs a feature, not
    /// the module.
    /// </summary>
    public static class ScpUdf_Cls
    {
        /// <summary>Feedback 07/08 "A2": stock issue cost has to map to the machine it was issued
        /// for, so cost and revenue can be reported per CSSI. ISSDTL already carries SerialNoList,
        /// UnitCost and SubTotal per LINE — the service item number is the missing half, and it
        /// belongs on the line so one issue document can serve several machines.</summary>
        public const string TABLE_STOCK_ISSUE_DTL = "ISSDTL";
        public const string FIELD_SERVICE_ITEM_NO = "ServiceItemNo";
        public const string CAPTION_SERVICE_ITEM_NO = "Service Item No";
        public const int SIZE_SERVICE_ITEM_NO = 50;

        /// <summary>Physical column name AutoCount gives the field above.</summary>
        public const string COL_SERVICE_ITEM_NO = "UDF_" + FIELD_SERVICE_ITEM_NO;

        /// <summary>Create every UDF the plugin needs that is not already registered. Called from
        /// PluginMain.BeforeLoad, right after the SQL migrations.</summary>
        public static void EnsureUdfs(DBSetting db)
        {
            if (db == null) return;
            EnsureTextUdf(db, TABLE_STOCK_ISSUE_DTL, FIELD_SERVICE_ITEM_NO,
                CAPTION_SERVICE_ITEM_NO, SIZE_SERVICE_ITEM_NO);
        }

        /// <summary>Register one nvarchar UDF if it is missing. Returns true when it was created.</summary>
        public static bool EnsureTextUdf(DBSetting db, string table, string name, string caption, int size)
        {
            if (db == null || string.IsNullOrEmpty(table) || string.IsNullOrEmpty(name)) return false;
            try
            {
                if (IsRegistered(db, table, name)) return false;

                // A raw column of the same name (hand-made, or left behind by a half-finished
                // change) would make the SDK's ALTER fail. Say so in the log rather than throwing
                // the same opaque error at every single book open.
                if (RawColumnExists(db, table, "UDF_" + name))
                {
                    Log("UDF_" + name + " exists on " + table + " but is NOT a registered AutoCount UDF - " +
                        "skipping. Drop the raw column, then reopen the book.");
                    return false;
                }

                AutoCount.UDF.UDFTable t = new AutoCount.UDF.UDFTable(table, db);
                AutoCount.UDF.Field f = t.Add(name, AutoCount.UDF.UDFType.Text, caption);
                if (f != null && f.TextProperties != null) f.TextProperties.Size = size;
                t.Save(true);
                Log("Created " + table + ".UDF_" + name + " (Text " + size + ") \"" + caption + "\"");
                return true;
            }
            catch (Exception ex)
            {
                // Never fatal: the plugin's own tables are what it cannot live without.
                Log("Could not create " + table + ".UDF_" + name + ": " + ex.Message);
                return false;
            }
        }

        private static bool IsRegistered(DBSetting db, string table, string name)
        {
            AutoCount.UDF.UDFColumn[] list = new AutoCount.UDF.UDFUtil(
                AutoCount.Authentication.UserSession.CurrentUserSession, db).GetUDF(table);
            if (list == null) return false;
            for (int i = 0; i < list.Length; i++)
                if (string.Equals(list[i].ActualFieldName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool RawColumnExists(DBSetting db, string table, string column)
        {
            System.Data.DataTable t = db.GetDataTable(
                "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" +
                table.Replace("'", "''") + "' AND COLUMN_NAME = '" + column.Replace("'", "''") + "'", false);
            return t != null && t.Rows.Count > 0;
        }

        private static void Log(string msg)
        {
            try { System.Diagnostics.Debug.WriteLine("[SCP UDF] " + msg); }
            catch { }
        }
    }
}
