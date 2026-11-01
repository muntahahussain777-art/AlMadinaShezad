using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace AlMadinaShahzad.Services
{
    public static class GoogleDriveBackupService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZaibPetroleum");

        private static readonly string SettingsFile = Path.Combine(SettingsDir, "GmailDriveBackup.settings");

        public static void SaveSettings(string gmail, string appPassword)
        {
            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            var lines = new List<string>
            {
                "Gmail=" + (gmail ?? "").Trim()
            };

            if (!string.IsNullOrWhiteSpace(appPassword))
                lines.Add("AppPassword=" + Protect(appPassword.Trim()));

            File.WriteAllLines(SettingsFile, lines);
        }

        public static (string Gmail, string AppPassword) LoadSettings()
        {
            if (!File.Exists(SettingsFile))
                return ("", "");

            string gmail = "";
            string appPassword = "";
            foreach (string raw in File.ReadAllLines(SettingsFile))
            {
                string line = raw.Trim();
                if (line.StartsWith("Gmail=", StringComparison.OrdinalIgnoreCase))
                    gmail = line.Substring(6).Trim();
                else if (line.StartsWith("AppPassword=", StringComparison.OrdinalIgnoreCase))
                {
                    try { appPassword = Unprotect(line.Substring(12).Trim()); }
                    catch { appPassword = ""; }
                }
            }

            return (gmail, appPassword);
        }

        public static GoogleDriveBackupResult BackupToGoogleDrive(string gmail, string appPassword)
        {
            gmail = (gmail ?? "").Trim();
            if (string.IsNullOrWhiteSpace(gmail) || !gmail.Contains("@"))
                throw new InvalidOperationException("Sahi Gmail address likhein.");

            SaveSettings(gmail, appPassword);

            string tempFile = Path.Combine(Path.GetTempPath(),
                "DiselPetrolPumpAlMadina_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db");

            try
            {
                DatabaseBackupHelper.CreateSqliteBackup(tempFile);

                // 1) Seedha Google Drive cloud (browser sign-in, bina Drive Desktop)
                if (GoogleDriveApiUploader.IsConfigured())
                {
                    try
                    {
                        string link = GoogleDriveApiUploader.UploadBackupFile(tempFile);
                        return new GoogleDriveBackupResult
                        {
                            Success = true,
                            Mode = "DriveApi",
                            Message = "Backup seedha aap ke Google Drive par upload ho gaya.\n" +
                                      "(Pehli dafa browser mein Gmail se sign-in ho sakta hai.)\n\n" +
                                      link,
                            TargetPath = link
                        };
                    }
                    catch (Exception apiEx)
                    {
                        if (string.IsNullOrWhiteSpace(appPassword) && string.IsNullOrEmpty(FindGoogleDriveRoot()))
                            throw new InvalidOperationException(
                                "Google Drive upload fail: " + apiEx.Message +
                                "\n\nApp.config mein GoogleDriveClientId check karein ya Gmail App Password likhein.",
                                apiEx);
                    }
                }

                // 2) Gmail email attachment (bina Drive Desktop)
                if (!string.IsNullOrWhiteSpace(appPassword))
                {
                    EmailBackupToGmail(gmail, appPassword, tempFile);
                    return new GoogleDriveBackupResult
                    {
                        Success = true,
                        Mode = "Gmail",
                        Message = "Backup aap ke Gmail par email attachment ke tor par bhej diya.\n" +
                                  "Gmail khol kar Drive par save kar sakte hain.",
                        TargetPath = gmail
                    };
                }

                // 3) Local Google Drive sync folder (agar Desktop installed ho)
                string driveRoot = FindGoogleDriveRoot();
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    string safeMail = gmail.Replace("@", "_at_").Replace(".", "_");
                    string targetDir = Path.Combine(driveRoot, "Al Madina Shezad Chhutta Backup", safeMail);
                    Directory.CreateDirectory(targetDir);

                    string targetFile = Path.Combine(targetDir,
                        "DiselPetrolPumpAlMadina_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db");

                    File.Copy(tempFile, targetFile, true);

                    return new GoogleDriveBackupResult
                    {
                        Success = true,
                        Mode = "DriveSync",
                        Message = "Backup Google Drive folder mein save ho gaya (Desktop sync).\n\n" + targetFile,
                        TargetPath = targetFile
                    };
                }

                throw new InvalidOperationException(
                    "Bina Google Drive Desktop ke backup ke liye:\n\n" +
                    "Option A: App.config mein GoogleDriveClientId set karein → seedha Drive upload + browser sign-in\n\n" +
                    "Option B: Gmail App Password likhein → backup email ho jayegi\n\n" +
                    "Option C: Google Drive for Desktop install karein");
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch { }
            }
        }

        private static void EmailBackupToGmail(string gmail, string appPassword, string backupFile)
        {
            using (var client = new SmtpClient("smtp.gmail.com", 587))
            using (var message = new MailMessage())
            using (var attachment = new Attachment(backupFile))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(gmail, appPassword);

                message.From = new MailAddress(gmail, "Al Madina Shezad Chhutta Backup");
                message.To.Add(gmail);
                message.Subject = "Al Madina Shezad Chhutta Services DB Backup " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                message.Body = "SQLite database backup attached.\nAl Madina Shezad Chhutta Services";
                message.Attachments.Add(attachment);

                client.Send(message);
            }
        }

        public static string FindGoogleDriveRoot()
        {
            var candidates = new List<string>();

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Google\Drive"))
                {
                    if (key != null)
                    {
                        foreach (string name in new[] { "Path", "MountPoint", "RootPath", "UserFolder" })
                        {
                            string val = Convert.ToString(key.GetValue(name));
                            if (!string.IsNullOrWhiteSpace(val))
                                candidates.Add(val.Trim());
                        }
                    }
                }
            }
            catch { }

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidates.Add(Path.Combine(profile, "Google Drive"));
            candidates.Add(Path.Combine(profile, "My Drive"));
            candidates.Add(Path.Combine(profile, "Google Drive", "My Drive"));

            foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                candidates.Add(Path.Combine(drive.Name, "My Drive"));
                candidates.Add(Path.Combine(drive.Name, "Google Drive"));
            }

            foreach (string path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    if (Directory.Exists(path))
                        return path;
                }
                catch { }
            }

            return null;
        }

        private static string Protect(string plain)
        {
            byte[] data = Encoding.UTF8.GetBytes(plain);
            byte[] protectedBytes = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        private static string Unprotect(string protectedBase64)
        {
            byte[] protectedBytes = Convert.FromBase64String(protectedBase64);
            byte[] data = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
    }

    public sealed class GoogleDriveBackupResult
    {
        public bool Success;
        public string Mode;
        public string Message;
        public string TargetPath;
    }
}
