using System;
using System.Collections;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;
using AlMadinaShahzad.ProjectConnection;

namespace AlMadinaShahzad.Services
{
    public static class AuditTrailService
    {
        public static void TryLog(string query, Hashtable parameters, int rowsAffected)
        {
            if (rowsAffected <= 0 || string.IsNullOrWhiteSpace(query))
                return;

            string action = DetectAction(query);
            if (action == null)
                return;

            try
            {
                string tableName = DetectTable(query) ?? "";
                string recordId = DetectRecordId(query, parameters);
                string details = BuildDetails(parameters);

                using (SQLiteConnection con = new SQLiteConnection(projectconnection.ConnectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO AuditLog (ActionTime, UserName, UserRole, ActionType, TableName, RecordId, QuerySummary, Details)
                    VALUES (@time, @user, @role, @action, @table, @record, @summary, @details)", con))
                {
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@user", RoleAccessService.CurrentUserName ?? "");
                    cmd.Parameters.AddWithValue("@role", RoleAccessService.CurrentRole ?? "Admin");
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@table", tableName);
                    cmd.Parameters.AddWithValue("@record", recordId);
                    cmd.Parameters.AddWithValue("@summary", Truncate(query.Replace(Environment.NewLine, " "), 500));
                    cmd.Parameters.AddWithValue("@details", Truncate(details, 1000));
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Audit fail hone par main business flow break nahi hona chahiye
            }
        }

        private static string DetectAction(string query)
        {
            string q = query.TrimStart();
            if (q.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)) return "INSERT";
            if (q.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)) return "UPDATE";
            if (q.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) return "DELETE";
            return null;
        }

        private static string DetectTable(string query)
        {
            Match m = Regex.Match(query, @"(?:INTO|UPDATE|FROM)\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string DetectRecordId(string query, Hashtable parameters)
        {
            if (parameters != null)
            {
                foreach (DictionaryEntry item in parameters)
                {
                    string key = item.Key?.ToString() ?? "";
                    if (key.Equals("@Id", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("@id", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("@UserID", StringComparison.OrdinalIgnoreCase))
                        return item.Value?.ToString() ?? "";
                }
            }

            Match m = Regex.Match(query, @"\bWHERE\s+\w+\s*=\s*'?(\d+)'?", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string BuildDetails(Hashtable parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";

            var sb = new StringBuilder();
            foreach (DictionaryEntry item in parameters)
                sb.Append(item.Key).Append('=').Append(item.Value).Append("; ");
            return sb.ToString();
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text ?? "";
            return text.Substring(0, max);
        }
    }
}
