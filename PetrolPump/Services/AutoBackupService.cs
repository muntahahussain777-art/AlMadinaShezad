using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Auto SQLite backup:
    /// - First start: ask partition + interval
    /// - Backup only after real data entries (not idle)
    /// - Writes to Documents\Al_Madina_Shezad_Chhutta Backup AND selected partition\Al_Madina_Shezad_Chhutta Backup
    /// - Intervals: 1 Day / 1 Hour / 3 Hours / 6 Hours
    /// </summary>
    public static class AutoBackupService
    {
        public const string FolderName = "Al_Madina_Shezad_Chhutta Backup";
        public const string ModeOneDay = "1Day";
        public const string ModeOneHour = "1Hour";
        public const string ModeThreeHours = "3Hours";
        public const string ModeSixHours = "6Hours";

        private static Timer _timer;
        private static readonly object Sync = new object();
        private static bool _running;
        private static bool _entriesPending;
        private static bool _started;

        private static string SettingsPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiselPetrolPump",
                "AutoBackup.settings");

        public static void Start()
        {
            if (_started) return;
            _started = true;

            EnsureConfiguredInteractive();

            lock (Sync)
            {
                if (_timer == null)
                {
                    _timer = new Timer();
                    _timer.Tick += (s, e) => TryRunBackupIfDue();
                }
                _timer.Interval = 60 * 1000; // 1 min check
                _timer.Start();
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (_timer != null)
                    _timer.Stop();
            }
        }

        /// <summary>Call after successful business INSERT/UPDATE/DELETE.</summary>
        public static void NotifyEntryMade()
        {
            lock (Sync) { _entriesPending = true; }
            TryRunBackupIfDue();
        }

        public static void NotifyEntryMadeIfBusinessQuery(string qry)
        {
            if (string.IsNullOrWhiteSpace(qry)) return;
            string q = qry.ToUpperInvariant();
            // Skip auth / license / permissions / sync noise
            if (q.Contains("TBLUSER") || q.Contains("LICENSE") ||
                q.Contains("ROLEPERMISSIONS") || q.Contains("AUDITLOG") ||
                q.Contains("SYNCTOMBSTONE") || q.Contains("SQLITE_SEQUENCE"))
                return;
            NotifyEntryMade();
        }

        public static bool IsConfigured()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return false;
                foreach (string line in File.ReadAllLines(SettingsPath))
                {
                    if (line.StartsWith("Configured=1", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        public static void EnsureConfiguredInteractive()
        {
            if (IsConfigured()) return;
            ShowSetupDialog(true);
        }

        public static void ShowSetupDialog(bool firstTime)
        {
            using (var dlg = new AutoBackupSetupForm(firstTime))
            {
                dlg.ShowDialog();
            }
            // Pehli dafa agar X se band kiya — default save taake bar bar na aaye
            if (firstTime && !IsConfigured())
                SaveSetup(ModeOneDay, "C:\\");
        }

        public static string GetIntervalMode()
        {
            string v = ReadSetting("Interval");
            if (string.Equals(v, ModeOneHour, StringComparison.OrdinalIgnoreCase)) return ModeOneHour;
            if (string.Equals(v, ModeThreeHours, StringComparison.OrdinalIgnoreCase)) return ModeThreeHours;
            if (string.Equals(v, ModeSixHours, StringComparison.OrdinalIgnoreCase)) return ModeSixHours;
            return ModeOneDay;
        }

        public static string GetPartitionRoot()
        {
            string p = ReadSetting("Partition");
            if (string.IsNullOrWhiteSpace(p)) return "";
            try
            {
                string root = Path.GetPathRoot(p.Trim());
                return string.IsNullOrEmpty(root) ? p.Trim() : root;
            }
            catch { return p.Trim(); }
        }

        public static void SetIntervalMode(string mode)
        {
            mode = NormalizeMode(mode);
            SaveAllSettings(mode, GetPartitionRoot(), GetLastBackupUtc(), true);
        }

        public static void SetPartitionRoot(string partitionRoot)
        {
            string root = "";
            try { root = Path.GetPathRoot(partitionRoot) ?? partitionRoot; } catch { root = partitionRoot; }
            SaveAllSettings(GetIntervalMode(), root, GetLastBackupUtc(), true);
        }

        public static void SaveSetup(string intervalMode, string partitionRoot)
        {
            SaveAllSettings(NormalizeMode(intervalMode), partitionRoot, GetLastBackupUtc(), true);
        }

        public static DateTime? GetLastBackupUtc()
        {
            string v = ReadSetting("LastBackupUtc");
            if (string.IsNullOrWhiteSpace(v)) return null;
            if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt))
                return dt.ToUniversalTime();
            return null;
        }

        public static string GetDocumentsBackupRoot()
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FolderName);
            Directory.CreateDirectory(root);
            return root;
        }

        public static string GetPartitionBackupRoot()
        {
            string part = GetPartitionRoot();
            if (string.IsNullOrWhiteSpace(part)) return null;
            try
            {
                if (!Directory.Exists(part)) return null;
                string root = Path.Combine(part, FolderName);
                Directory.CreateDirectory(root);
                return root;
            }
            catch { return null; }
        }

        public static IEnumerable<string> GetAllBackupRoots()
        {
            yield return GetDocumentsBackupRoot();
            string part = GetPartitionBackupRoot();
            if (!string.IsNullOrEmpty(part))
                yield return part;
        }

        public static string GetStatusText()
        {
            string mode = IntervalDisplayName(GetIntervalMode());
            DateTime? last = GetLastBackupUtc();
            string lastText = last.HasValue
                ? last.Value.ToLocalTime().ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
                : "never";
            string part = GetPartitionRoot();
            if (string.IsNullOrEmpty(part)) part = "(not set)";
            return "Auto Backup: " + mode + "  |  Partition: " + part +
                   "  |  Last: " + lastText + "  |  Also: Documents\\" + FolderName;
        }

        public static string IntervalDisplayName(string mode)
        {
            if (mode == ModeOneHour) return "Every 1 Hour";
            if (mode == ModeThreeHours) return "Every 3 Hours";
            if (mode == ModeSixHours) return "Every 6 Hours";
            return "Every 1 Day";
        }

        public static void TryRunBackupIfDue()
        {
            lock (Sync)
            {
                if (_running) return;
                if (!_entriesPending) return; // without entries → no backup
                _running = true;
            }

            try
            {
                DateTime? last = GetLastBackupUtc();
                TimeSpan due = GetDueSpan();
                bool need = !last.HasValue || (DateTime.UtcNow - last.Value) >= due;
                if (!need) return;

                string msg = RunBackupNow(false);
                if (!string.IsNullOrEmpty(msg))
                {
                    try
                    {
                        MessageBox.Show(
                            "Auto backup complete.\n\n" + msg,
                            "Al_Madina_Shezad_Chhutta Backup",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                lock (Sync) { _running = false; }
            }
        }

        /// <summary>Manual / Backup Now — always runs (ignores pending entries).</summary>
        public static string RunBackupNow(bool forceIgnorePending = true)
        {
            DateTime now = DateTime.Now;
            string day = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string sub = ResolveSubFolderName(now);
            string fileName = "DiselPetrolPumpAlMadina_" + now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".db";

            var saved = new List<string>();
            foreach (string root in GetAllBackupRoots())
            {
                try
                {
                    string folder = Path.Combine(root, day, sub);
                    Directory.CreateDirectory(folder);
                    string path = Path.Combine(folder, fileName);
                    DatabaseBackupHelper.CreateSqliteBackup(path);
                    saved.Add(path);
                }
                catch { }
            }

            if (saved.Count == 0)
                throw new IOException("Backup folder nahi ban saka (Documents / partition check karein).");

            SaveAllSettings(GetIntervalMode(), GetPartitionRoot(), DateTime.UtcNow, true);
            lock (Sync) { _entriesPending = false; }

            return string.Join("\n", saved);
        }

        private static string ResolveSubFolderName(DateTime now)
        {
            string mode = GetIntervalMode();
            if (mode == ModeOneHour)
                return now.Hour.ToString("00", CultureInfo.InvariantCulture) + "h";
            if (mode == ModeThreeHours)
                return ((now.Hour / 3) * 3).ToString("00", CultureInfo.InvariantCulture) + "h";
            if (mode == ModeSixHours)
                return ((now.Hour / 6) * 6).ToString("00", CultureInfo.InvariantCulture) + "h";
            return "Daily";
        }

        private static TimeSpan GetDueSpan()
        {
            string mode = GetIntervalMode();
            if (mode == ModeOneHour) return TimeSpan.FromHours(1);
            if (mode == ModeThreeHours) return TimeSpan.FromHours(3);
            if (mode == ModeSixHours) return TimeSpan.FromHours(6);
            return TimeSpan.FromDays(1);
        }

        private static string NormalizeMode(string mode)
        {
            if (string.Equals(mode, ModeOneHour, StringComparison.OrdinalIgnoreCase)) return ModeOneHour;
            if (string.Equals(mode, ModeThreeHours, StringComparison.OrdinalIgnoreCase)) return ModeThreeHours;
            if (string.Equals(mode, ModeSixHours, StringComparison.OrdinalIgnoreCase)) return ModeSixHours;
            return ModeOneDay;
        }

        private static string ReadSetting(string key)
        {
            try
            {
                if (!File.Exists(SettingsPath)) return "";
                string prefix = key + "=";
                foreach (string line in File.ReadAllLines(SettingsPath))
                {
                    if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(prefix.Length).Trim();
                }
            }
            catch { }
            return "";
        }

        private static void SaveAllSettings(string intervalMode, string partitionRoot, DateTime? lastUtc, bool configured)
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string last = lastUtc.HasValue
                    ? lastUtc.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                    : "";

                File.WriteAllText(SettingsPath,
                    "Configured=" + (configured ? "1" : "0") + Environment.NewLine +
                    "Interval=" + NormalizeMode(intervalMode) + Environment.NewLine +
                    "Partition=" + (partitionRoot ?? "") + Environment.NewLine +
                    "LastBackupUtc=" + last + Environment.NewLine);
            }
            catch { }
        }
    }

    /// <summary>First-run / settings dialog — partition + interval.</summary>
    internal sealed class AutoBackupSetupForm : Form
    {
        private readonly ComboBox _cmbDrive;
        private readonly ComboBox _cmbInterval;
        private readonly bool _firstTime;

        public AutoBackupSetupForm(bool firstTime)
        {
            _firstTime = firstTime;
            Text = firstTime ? "Al_Madina_Shezad_Chhutta Backup Setup (First Time)" : "Al_Madina_Shezad_Chhutta Backup Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = firstTime;
            BackColor = Color.FromArgb(32, 36, 61);
            ForeColor = Color.White;
            ClientSize = new Size(460, 230);
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            var lblInfo = new Label
            {
                AutoSize = false,
                Size = new Size(420, 40),
                Location = new Point(20, 12),
                Text = firstTime
                    ? "Pehli dafa setup: backup kis partition pe rakhna hai aur kitni der baad?"
                    : "Backup partition aur interval change karein (Documents mein bhi hamesha backup hoga)."
            };

            var lblDrive = new Label { Text = "Partition / Drive:", Location = new Point(20, 60), AutoSize = true };
            _cmbDrive = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 85),
                Size = new Size(200, 28),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                try
                {
                    if (d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                        _cmbDrive.Items.Add(d.Name);
                }
                catch { }
            }
            if (_cmbDrive.Items.Count == 0)
                _cmbDrive.Items.Add("C:\\");

            string current = AutoBackupService.GetPartitionRoot();
            int idx = 0;
            for (int i = 0; i < _cmbDrive.Items.Count; i++)
            {
                if (string.Equals(_cmbDrive.Items[i].ToString(), current, StringComparison.OrdinalIgnoreCase))
                { idx = i; break; }
            }
            _cmbDrive.SelectedIndex = idx;

            var lblInt = new Label { Text = "Auto Backup Interval:", Location = new Point(240, 60), AutoSize = true };
            _cmbInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(240, 85),
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbInterval.Items.AddRange(new object[]
            {
                "Every 1 Day",
                "Every 1 Hour",
                "Every 3 Hours",
                "Every 6 Hours"
            });
            string mode = AutoBackupService.GetIntervalMode();
            _cmbInterval.SelectedIndex =
                mode == AutoBackupService.ModeOneHour ? 1 :
                mode == AutoBackupService.ModeThreeHours ? 2 :
                mode == AutoBackupService.ModeSixHours ? 3 : 0;

            var btnOk = new Button
            {
                Text = "Save & Continue",
                Location = new Point(240, 150),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(112, 51, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) =>
            {
                string drive = _cmbDrive.SelectedItem != null ? _cmbDrive.SelectedItem.ToString() : "C:\\";
                string interval =
                    _cmbInterval.SelectedIndex == 1 ? AutoBackupService.ModeOneHour :
                    _cmbInterval.SelectedIndex == 2 ? AutoBackupService.ModeThreeHours :
                    _cmbInterval.SelectedIndex == 3 ? AutoBackupService.ModeSixHours :
                    AutoBackupService.ModeOneDay;
                AutoBackupService.SaveSetup(interval, drive);
                MessageBox.Show(
                    "Backup settings save ho gayi.\n\nDocuments + " + drive + AutoBackupService.FolderName +
                    "\nInterval: " + AutoBackupService.IntervalDisplayName(interval) +
                    "\n\nBackup tabhi hoga jab entries hon.",
                    "Al_Madina_Shezad_Chhutta Backup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            var btnCancel = new Button
            {
                Text = _firstTime ? "Use C:\\ (default)" : "Cancel",
                Location = new Point(20, 150),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) =>
            {
                if (_firstTime)
                {
                    AutoBackupService.SaveSetup(AutoBackupService.ModeOneDay, "C:\\");
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                }
                Close();
            };

            Controls.Add(lblInfo);
            Controls.Add(lblDrive);
            Controls.Add(_cmbDrive);
            Controls.Add(lblInt);
            Controls.Add(_cmbInterval);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;

            FormClosing += (s, e) =>
            {
                // First time close without save → defaults, dialog dubara na aaye
                if (_firstTime && !AutoBackupService.IsConfigured())
                    AutoBackupService.SaveSetup(AutoBackupService.ModeOneDay, "C:\\");
            };
        }
    }
}
