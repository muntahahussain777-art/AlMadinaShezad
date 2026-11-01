using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data.SQLite;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Daily Diesel Sales Add2 — meter Opening/Closing session + linked PetrolAdd lines.
    /// </summary>
    public static class DieselUnitReadingHelper
    {
        public const string TableName = "DieselUnitReading";

        public static void EnsureSchema(SQLiteConnection connection)
        {
            if (connection == null) return;

            using (var create = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS DieselUnitReading (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date TEXT NOT NULL,
    OfficeName TEXT NOT NULL,
    OpeningReading REAL NOT NULL DEFAULT 0,
    ClosingReading REAL NOT NULL DEFAULT 0,
    SoldLitter REAL NOT NULL DEFAULT 0,
    OpeningText TEXT,
    ClosingText TEXT,
    Note TEXT
);", connection))
            {
                create.ExecuteNonQuery();
            }

            DatabaseSchemaManager.EnsureColumn(connection, "DieselUnitReading", "OpeningText", "TEXT");
            DatabaseSchemaManager.EnsureColumn(connection, "DieselUnitReading", "ClosingText", "TEXT");
            DatabaseSchemaManager.EnsureColumn(connection, "PetrolAdd", "UnitReadingId", "INTEGER");

            // Purani rows: TEXT empty ho to REAL se fill
            using (var backfill = new SQLiteCommand(@"
UPDATE DieselUnitReading
SET OpeningText = CAST(OpeningReading AS TEXT)
WHERE IFNULL(TRIM(OpeningText),'') = '';
UPDATE DieselUnitReading
SET ClosingText = CAST(ClosingReading AS TEXT)
WHERE IFNULL(TRIM(ClosingText),'') = '';", connection))
            {
                backfill.ExecuteNonQuery();
            }
        }

        public static void EnsureSchema()
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                EnsureSchema(con);
            }
        }
    }
}
