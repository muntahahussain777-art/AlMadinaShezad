using System;
using System.Data.SQLite;

namespace AlMadinaShahzad.ReportForm
{
    public static class ReportSearchHelper
    {
        public static string Trim(string text) => (text ?? "").Trim();

        public static void BindSearch(SQLiteCommand cmd, string searchText)
        {
            string t = Trim(searchText);
            cmd.Parameters.AddWithValue("@SearchText", t);
            cmd.Parameters.AddWithValue("@SearchLike", "%" + t + "%");
        }

        /// <summary>Customer/dealer name filter — exact match only (irtaza vs irtazaj mix nahi hoga).</summary>
        public static string CustomerNameExact(string column) =>
            $"(@SearchText = '' OR TRIM({column}) = @SearchText COLLATE NOCASE)";

        public static string DealerNameExact(string column) =>
            $"(@SearchText = '' OR TRIM({column}) = @SearchText COLLATE NOCASE)";

        public static int? TryGetCustomerId(SQLiteConnection con, string name)
        {
            string t = Trim(name);
            if (string.IsNullOrEmpty(t)) return null;
            using (var cmd = new SQLiteCommand(
                "SELECT Id FROM AddCustomer WHERE TRIM(Name) = @Name COLLATE NOCASE LIMIT 1", con))
            {
                cmd.Parameters.AddWithValue("@Name", t);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }

        public static int? TryGetDealerId(SQLiteConnection con, string name)
        {
            string t = Trim(name);
            if (string.IsNullOrEmpty(t)) return null;
            using (var cmd = new SQLiteCommand(
                "SELECT Did FROM AddDealer WHERE TRIM(DealerName) = @Name COLLATE NOCASE LIMIT 1", con))
            {
                cmd.Parameters.AddWithValue("@Name", t);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }
    }
}
