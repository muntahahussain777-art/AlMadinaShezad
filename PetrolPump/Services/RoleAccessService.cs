using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using AlMadinaShahzad.ProjectConnection;

namespace AlMadinaShahzad.Services
{
    public static class RoleAccessService
    {
        private static readonly Dictionary<string, bool> PermissionCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static int CurrentUserId { get; private set; }
        public static string CurrentUserName { get; private set; } = "";
        public static string CurrentRole { get; private set; } = "Admin";

        public static bool IsAdmin => string.Equals(CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        public static void SetSession(int userId, string userName, string role)
        {
            CurrentUserId = userId;
            CurrentUserName = userName ?? "";
            CurrentRole = string.IsNullOrWhiteSpace(role) ? "Admin" : role.Trim();
            ReloadPermissions();
        }

        public static void ReloadPermissions()
        {
            PermissionCache.Clear();
            if (IsAdmin)
                return;

            try
            {
                using (SQLiteConnection con = new SQLiteConnection(projectconnection.ConnectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(
                    "SELECT FormKey, CanAccess FROM RolePermissions WHERE RoleName = @role", con))
                {
                    cmd.Parameters.AddWithValue("@role", CurrentRole);
                    con.Open();
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string key = r["FormKey"].ToString();
                            bool access = Convert.ToInt32(r["CanAccess"]) == 1;
                            PermissionCache[key] = access;
                        }
                    }
                }
            }
            catch
            {
                // Purane DB par agar table na ho to Admin jaisa treat karo
                CurrentRole = "Admin";
            }
        }

        public static bool CanAccess(string formKey)
        {
            if (string.IsNullOrWhiteSpace(formKey))
                return true;
            if (IsAdmin)
                return true;

            if (PermissionCache.TryGetValue(formKey, out bool allowed))
                return allowed;

            return false;
        }

        public static DataTable GetAllPermissions()
        {
            return MainClass.GetData("SELECT Id, RoleName, FormKey, CanAccess FROM RolePermissions ORDER BY RoleName, FormKey");
        }

        public static void SavePermission(int id, int canAccess)
        {
            var ht = new System.Collections.Hashtable
            {
                { "@access", canAccess },
                { "@id", id }
            };
            MainClass.DataInsertUpdateDelete("UPDATE RolePermissions SET CanAccess=@access WHERE Id=@id", ht);
            ReloadPermissions();
        }

        public static void SetUserRole(int userId, string role)
        {
            var ht = new System.Collections.Hashtable
            {
                { "@role", role },
                { "@id", userId }
            };
            MainClass.DataInsertUpdateDelete("UPDATE tblUser SET uRole=@role WHERE UserID=@id", ht);
        }
    }
}
