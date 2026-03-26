using System;
using System.IO;

namespace Chronocode.Data
{
    internal static class DatabaseUtils
    {
        internal static string ExtractDatabasePath(string connectionString)
        {
            // Parse SQLite connection string to extract the database file path
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2 &&
                    (keyValue[0].Trim().Equals("DataSource", StringComparison.OrdinalIgnoreCase) ||
                     keyValue[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase)))
                {
                    var path = keyValue[1].Trim();
                    // Normalize both backslashes and forward slashes to platform-appropriate separator
                    return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                }
            }
            // Default fallback
            return Path.Combine("Data", "app.db");
        }
    }
}
