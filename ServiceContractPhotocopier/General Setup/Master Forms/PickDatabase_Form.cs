using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Every database on a SQL server, so the other book can be picked instead of typed.
    ///
    /// <para>Typing a database name is the one step of setting up inter-billing that a person cannot
    /// check for themselves: get it wrong and the message says the server refused, which is true and
    /// useless. Listing them removes the guess, and showing the company name beside each one removes
    /// the second guess -- which of forty <c>AED_*</c> databases is the company you meant.</para>
    ///
    /// <para>The <b>Service Contract</b> column is the one that decides whether a row is usable at
    /// all: it says whether that book has this plug-in installed. A book without it holds no
    /// contracts, no machines and no readings, and the connection test would refuse it anyway --
    /// better to see that before choosing.</para>
    /// </summary>
    public partial class PickDatabase_Form : XtraForm
    {
        private readonly string _connectionString;

        /// <summary>The database the user chose, or "" if they backed out.</summary>
        public string SelectedDatabase = "";

        public PickDatabase_Form() { InitializeComponent(); }

        public PickDatabase_Form(string masterConnectionString) : this()
        {
            _connectionString = masterConnectionString;
            this.Load += new EventHandler(OnFormLoad);
        }

        /// <summary>
        /// Opens the picker against a server and returns the chosen database, or "".
        /// </summary>
        /// <param name="server">The SQL server as typed on the setup screen.</param>
        /// <param name="windowsAuth">Sign in as the Windows user instead of a SQL login.</param>
        public static string Pick(IWin32Window owner, string server, bool windowsAuth,
                                  string user, string password)
        {
            if ((server ?? "").Trim().Length == 0)
            {
                XtraMessageBox.Show("Enter the SQL server first -- the list comes from it.",
                    "Choose a database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = server.Trim();
            // master, because the question is "what databases are there" -- asking it of a database
            // you have not chosen yet is the chicken and the egg.
            b.InitialCatalog = "master";
            if (windowsAuth)
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.IntegratedSecurity = false;
                b.UserID = (user ?? "").Trim();
                b.Password = password ?? "";
            }
            b.ConnectTimeout = 15;
            b.ApplicationName = "ATP Service Contract - inter-billing";

            using (PickDatabase_Form f = new PickDatabase_Form(b.ConnectionString))
            {
                return f.ShowDialog(owner) == DialogResult.OK ? f.SelectedDatabase : "";
            }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                Grid.DataSource = ReadDatabases();
                GridView.BestFitColumns();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    "Could not read the list from that server:" + Environment.NewLine +
                    Environment.NewLine + ex.Message,
                    "Choose a database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.Cancel;
            }
            finally { this.Cursor = old; }
        }

        /// <summary>
        /// The databases on the server, with each one's company name and whether it carries this
        /// plug-in.
        ///
        /// <para>Two steps on one open connection: ask which databases hold an account book, then ask
        /// each of those its company name. Forty small queries down a connection that is already open
        /// costs nothing measurable.</para>
        ///
        /// <para>It was first written as a single dynamic-SQL statement that assembled forty
        /// <c>UNION ALL</c>s. That version had to nest a quoted database name inside a quoted SQL
        /// string inside a C# string -- eight apostrophes in a row, and wrong. This one can be read.</para>
        ///
        /// <para>A database the login cannot open never appears: <c>OBJECT_ID</c> returns NULL for
        /// anything out of reach, which is the right answer -- it could not have been used either.</para>
        /// </summary>
        private DataTable ReadDatabases()
        {
            DataTable t = new DataTable();
            t.Columns.Add("DatabaseName", typeof(string));
            t.Columns.Add("CompanyName", typeof(string));
            t.Columns.Add("PlugIn", typeof(string));

            using (SqlConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();

                List<string[]> books = new List<string[]>();
                using (SqlCommand cmd = new SqlCommand(
                    // QUOTENAME server-side, so a database name with a bracket in it cannot break the
                    // query that asks about it next.
                    "SELECT d.name, QUOTENAME(d.name) AS Safe, " +
                    "       CASE WHEN OBJECT_ID(QUOTENAME(d.name) + '.dbo.zSCP2_BookIdentity') IS NULL " +
                    "            THEN 'not installed' ELSE 'installed' END AS PlugIn " +
                    "  FROM sys.databases d " +
                    " WHERE d.state = 0 AND d.database_id > 4 " +
                    "   AND OBJECT_ID(QUOTENAME(d.name) + '.dbo.Profile') IS NOT NULL " +
                    " ORDER BY d.name", cn))
                {
                    cmd.CommandTimeout = 60;
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            books.Add(new string[] { r.GetString(0), r.GetString(1), r.GetString(2) });
                    }
                }

                foreach (string[] b in books)
                {
                    string company = "";
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT TOP 1 ISNULL(CompanyName, '') FROM " + b[1] + ".dbo.Profile", cn))
                        {
                            cmd.CommandTimeout = 15;
                            object o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) company = Convert.ToString(o).Trim();
                        }
                    }
                    catch { }   // one unreadable book must not empty the whole list

                    DataRow row = t.NewRow();
                    row["DatabaseName"] = b[0];
                    row["CompanyName"] = company;
                    row["PlugIn"] = b[2];
                    t.Rows.Add(row);
                }
            }
            return t;
        }

        private void OnGridDoubleClick(object sender, EventArgs e) { Take(); }

        private void OnSelect(object sender, EventArgs e) { Take(); }

        private void Take()
        {
            int row = GridView.FocusedRowHandle;
            if (row < 0) return;
            object v = GridView.GetRowCellValue(row, "DatabaseName");
            if (v == null || v == DBNull.Value) return;

            SelectedDatabase = Convert.ToString(v).Trim();
            if (SelectedDatabase.Length == 0) return;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
