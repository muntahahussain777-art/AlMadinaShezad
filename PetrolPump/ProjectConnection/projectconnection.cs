using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.ProjectConnection
{
    class projectconnection
    {
        private static bool _schemaEnsured;

        // Initialize the ConnectionString property dynamically
        public static string ConnectionString { get; internal set; } = conReturn();

        // Method to return the connection string for SQLite
        public static string conReturn()
        {
            // Get the path for the SQLite database in the bin folder
            string binFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiselPetrolPumpAlMadina.db");

            // Get the path for storing the SQLite database in AppData folder
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolderPathAppData = Path.Combine(appDataFolder, "DiselPetrolPump");

            // Ensure the directory exists in AppData
            if (!Directory.Exists(dbFolderPathAppData))
            {
                Directory.CreateDirectory(dbFolderPathAppData);  // Create directory if it doesn't exist
            }

            string dbFilePath = Path.Combine(dbFolderPathAppData, "DiselPetrolPumpAlMadina.db");

            // Ensure the database file exists in AppData folder
            if (!File.Exists(dbFilePath))
            {
                try
                {
                    // If the database does not exist in AppData, copy it from the bin folder
                    if (File.Exists(binFolderPath))
                    {
                        File.Copy(binFolderPath, dbFilePath);
                        Console.WriteLine("Database copied from bin to AppData.");
                    }
                    else
                    {
                        // If the database does not exist in bin folder, throw an exception or create a new one
                        throw new FileNotFoundException("Database file not found in bin folder.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error copying database file: " + ex.Message);
                    throw;
                }
            }

            if (!_schemaEnsured)
            {
                _schemaEnsured = true;
                EnsureSchemaUpdates(dbFilePath);
            }

            // Return the SQLite connection string for the AppData database file
            return $@"Data Source={dbFilePath};Version=3;";
        }

        public static void EnsureSchemaUpdates(string dbFilePath = null)
        {
            if (string.IsNullOrEmpty(dbFilePath))
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                dbFilePath = Path.Combine(appDataFolder, "DiselPetrolPump", "DiselPetrolPumpAlMadina.db");
            }

            if (!File.Exists(dbFilePath))
                return;

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($@"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();
                    DatabaseSchemaManager.EnsureProfessionalSchema(connection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema update error: " + ex.Message);
            }
        }

        private static void EnsureColumn(SQLiteConnection connection, string tableName, string columnName, string columnType)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", connection))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader["name"].ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }

            using (SQLiteCommand alter = new SQLiteCommand(
                $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}", connection))
            {
                alter.ExecuteNonQuery();
            }
        }

        // Method to initialize the database (if needed)
        public static void InitializeDatabase()
        {
            int retryCount = 3;
            while (retryCount > 0)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();


                        // Enable foreign key support
                        string enableForeignKeyQuery = "PRAGMA foreign_keys = ON;";
                        using (SQLiteCommand cmd = new SQLiteCommand(enableForeignKeyQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }



                        // Enable WAL (Write-Ahead Logging) mode for better concurrency
                        string enableWALModeQuery = "PRAGMA journal_mode=WAL;";
                        using (SQLiteCommand cmd = new SQLiteCommand(enableWALModeQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Create tables if they don't already exist
                        string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS DiselPetrolPump (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                Location TEXT NOT NULL
                            );";

                        using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        Console.WriteLine("SQLite database initialized successfully in AppData.");
                        break;  // Exit loop if successful
                    }
                }
                catch (SQLiteException ex)
                {
                    // Retry on lock error
                    Console.WriteLine("SQLite locked, retrying... " + ex.Message);
                    retryCount--;
                    if (retryCount == 0)
                    {
                        Console.WriteLine("Failed after multiple attempts.");
                        throw;
                    }
                    Thread.Sleep(500);  // Wait 500ms before retrying
                }
                catch (Exception ex)
                {
                    Console.WriteLine("General error: " + ex.Message);
                    throw;
                }
            }
        }
    }
}
