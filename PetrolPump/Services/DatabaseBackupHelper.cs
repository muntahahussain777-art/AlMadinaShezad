using System;
using System.Data.SQLite;
using System.IO;

namespace AlMadinaShahzad.Services
{
    public static class DatabaseBackupHelper
    {
        public static string GetDatabaseFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiselPetrolPump",
                "DiselPetrolPumpAlMadina.db");
        }

        public static void CreateSqliteBackup(string backupFilePath)
        {
            string databaseFilePath = GetDatabaseFilePath();
            if (!File.Exists(databaseFilePath))
                throw new FileNotFoundException("Database file nahi mili.", databaseFilePath);

            string dir = Path.GetDirectoryName(backupFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            using (var sourceConnection = new SQLiteConnection($@"Data Source={databaseFilePath};Version=3;"))
            using (var backupConnection = new SQLiteConnection($@"Data Source={backupFilePath};Version=3;"))
            {
                sourceConnection.Open();
                backupConnection.Open();
                sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);
            }
        }
    }
}
