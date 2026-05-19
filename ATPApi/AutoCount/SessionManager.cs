using System;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ATPApi.AutoCount
{
    public static class SessionManager
    {
        private static DBSetting _dbSetting;
        private static UserSession _userSession;

        public static DBSetting DbSetting => _dbSetting;
        public static UserSession UserSession => _userSession;
        public static string CurrentProfile { get; private set; }
        public static string SqlConnectionString { get; private set; }
        public static string DatabaseName { get; private set; }

        public static void Initialize(string server, string sqlUser, string sqlPassword,
            string database, string loginUser, string loginPassword, string profileName = "default")
        {
            CurrentProfile = profileName;
            DatabaseName = database;
            SqlConnectionString = $"Server={server};Database={database};User Id={sqlUser};Password={sqlPassword};TrustServerCertificate=True;";
            _dbSetting = new DBSetting(DBServerType.SQL2000, server, sqlUser, sqlPassword, database);
            _userSession = new UserSession(_dbSetting);

            if (!_userSession.Login(loginUser, loginPassword))
                throw new InvalidOperationException(
                    $"Failed to login to AutoCount as '{loginUser}'. Check credentials.");

            Console.WriteLine($"AutoCount session initialized: [{profileName}] {server}/{database} as {loginUser}");
        }
    }
}
