using System;
using System.Data.SQLite;

namespace AlMadinaShahzad.Services
{
    public static class DatabaseSchemaManager
    {
        public static void EnsureProfessionalSchema(SQLiteConnection connection)
        {
            EnsureTable(connection, "AuditLog", @"
                CREATE TABLE IF NOT EXISTS AuditLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ActionTime TEXT NOT NULL,
                    UserName TEXT,
                    UserRole TEXT,
                    ActionType TEXT,
                    TableName TEXT,
                    RecordId TEXT,
                    QuerySummary TEXT,
                    Details TEXT
                );");

            EnsureTable(connection, "RolePermissions", @"
                CREATE TABLE IF NOT EXISTS RolePermissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoleName TEXT NOT NULL,
                    FormKey TEXT NOT NULL,
                    CanAccess INTEGER NOT NULL DEFAULT 1,
                    UNIQUE(RoleName, FormKey)
                );");

            EnsureColumn(connection, "BankTransactions", "BankName", "TEXT");
            EnsureColumn(connection, "tblUser", "uRole", "TEXT DEFAULT 'Admin'");

            OfficeHelper.EnsureOfficeSchema(connection);
            DieselUnitReadingHelper.EnsureSchema(connection);

            SeedDefaultPermissions(connection);
        }

        private static void EnsureTable(SQLiteConnection connection, string tableName, string createSql)
        {
            using (SQLiteCommand check = new SQLiteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name=@name", connection))
            {
                check.Parameters.AddWithValue("@name", tableName);
                object found = check.ExecuteScalar();
                if (found != null && found != DBNull.Value)
                    return;
            }

            using (SQLiteCommand create = new SQLiteCommand(createSql, connection))
                create.ExecuteNonQuery();
        }

        public static void EnsureColumn(SQLiteConnection connection, string tableName, string columnName, string columnType)
        {
            if (!TableExists(connection, tableName))
                return;

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

        private static bool TableExists(SQLiteConnection connection, string tableName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@name LIMIT 1", connection))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void SeedDefaultPermissions(SQLiteConnection connection)
        {
            using (SQLiteCommand countCmd = new SQLiteCommand("SELECT COUNT(*) FROM RolePermissions", connection))
            {
                if (Convert.ToInt32(countCmd.ExecuteScalar()) > 0)
                    return;
            }

            string[] allForms =
            {
                "frmDashBoard","frmCustomerView","frmDiselView","frmCreditAdjust",
                "frmCustomerToCustomerView","frmDieselLedger","frmDealerNameView",
                "frmStockView","frmDealertoDealerView","frmDieselLedgerView",
                "FrmDirectDealerPaymentAmountView","frmBankAccountView","frmExpense",
                "frmStockDieselView","frmClosingformEntry","frmClosing2",
                "ReportAndBackup","changepassword","ProfitLossReportForm",
                "BalanceSheetReportForm","AuditTrailView","RoleAccessView"
            };

            string[] managerSkip = { "RoleAccessView", "changepassword" };
            string[] cashierAllow =
            {
                "frmDashBoard","frmCustomerView","frmDiselView","frmCreditAdjust","ReportAndBackup"
            };

            foreach (string form in allForms)
            {
                InsertPerm(connection, "Admin", form, 1);
                InsertPerm(connection, "Manager", form, Array.IndexOf(managerSkip, form) >= 0 ? 0 : 1);
                InsertPerm(connection, "Cashier", form, Array.IndexOf(cashierAllow, form) >= 0 ? 1 : 0);
            }
        }

        private static void InsertPerm(SQLiteConnection connection, string role, string formKey, int canAccess)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(
                "INSERT OR IGNORE INTO RolePermissions (RoleName, FormKey, CanAccess) VALUES (@role, @form, @access)", connection))
            {
                cmd.Parameters.AddWithValue("@role", role);
                cmd.Parameters.AddWithValue("@form", formKey);
                cmd.Parameters.AddWithValue("@access", canAccess);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
