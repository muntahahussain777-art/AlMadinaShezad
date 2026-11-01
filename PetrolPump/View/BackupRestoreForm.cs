using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class BackupRestoreForm : Sample
    {
        private ComboBox _cmbAutoBackupInterval;
        private Label _lblAutoBackupStatus;

        public BackupRestoreForm()
        {
            InitializeComponent();
            SetupAutoBackupControls();
            Load += (s, e) => RefreshAutoBackupStatus();
        }

        private void SetupAutoBackupControls()
        {
            // Existing form height thori bari — naye controls niche
            this.ClientSize = new Size(Math.Max(this.ClientSize.Width, 410), 360);

            var lbl = new Label
            {
                AutoSize = true,
                Text = "Auto Backup Interval:",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(19, 265)
            };

            _cmbAutoBackupInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(19, 290),
                Size = new Size(200, 28),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbAutoBackupInterval.Items.Add("Every 1 Day");
            _cmbAutoBackupInterval.Items.Add("Every 1 Hour");
            _cmbAutoBackupInterval.Items.Add("Every 3 Hours");
            _cmbAutoBackupInterval.Items.Add("Every 6 Hours");
            string mode = AutoBackupService.GetIntervalMode();
            _cmbAutoBackupInterval.SelectedIndex =
                mode == AutoBackupService.ModeOneHour ? 1 :
                mode == AutoBackupService.ModeThreeHours ? 2 :
                mode == AutoBackupService.ModeSixHours ? 3 : 0;
            _cmbAutoBackupInterval.SelectedIndexChanged += (s, e) =>
            {
                string m =
                    _cmbAutoBackupInterval.SelectedIndex == 1 ? AutoBackupService.ModeOneHour :
                    _cmbAutoBackupInterval.SelectedIndex == 2 ? AutoBackupService.ModeThreeHours :
                    _cmbAutoBackupInterval.SelectedIndex == 3 ? AutoBackupService.ModeSixHours :
                    AutoBackupService.ModeOneDay;
                AutoBackupService.SetIntervalMode(m);
                RefreshAutoBackupStatus();
            };

            var btnSettings = new Button
            {
                Text = "Partition...",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(230, 288),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White
            };
            btnSettings.Click += (s, e) =>
            {
                AutoBackupService.ShowSetupDialog(false);
                // refresh combo from saved settings
                string m2 = AutoBackupService.GetIntervalMode();
                _cmbAutoBackupInterval.SelectedIndex =
                    m2 == AutoBackupService.ModeOneHour ? 1 :
                    m2 == AutoBackupService.ModeThreeHours ? 2 :
                    m2 == AutoBackupService.ModeSixHours ? 3 : 0;
                RefreshAutoBackupStatus();
            };

            var btnNow = new Button
            {
                Text = "Backup Now",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(325, 288),
                Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(112, 51, 255),
                ForeColor = Color.White
            };
            btnNow.Click += (s, e) =>
            {
                try
                {
                    string path = AutoBackupService.RunBackupNow(true);
                    RefreshAutoBackupStatus();
                    MessageBox.Show("Backup saved:\n" + path, "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Auto backup error: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnOpen = new Button
            {
                Text = "Open",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(19, 255),
                Size = new Size(60, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White
            };
            btnOpen.Click += (s, e) =>
            {
                try
                {
                    string root = AutoBackupService.GetDocumentsBackupRoot();
                    System.Diagnostics.Process.Start("explorer.exe", root);
                }
                catch { }
            };

            _lblAutoBackupStatus = new Label
            {
                AutoSize = false,
                Size = new Size(376, 36),
                Location = new Point(19, 322),
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Color.FromArgb(200, 200, 210)
            };

            Controls.Add(lbl);
            Controls.Add(_cmbAutoBackupInterval);
            Controls.Add(btnSettings);
            Controls.Add(btnNow);
            Controls.Add(btnOpen);
            Controls.Add(_lblAutoBackupStatus);
            lbl.BringToFront();
            _cmbAutoBackupInterval.BringToFront();
            btnSettings.BringToFront();
            btnNow.BringToFront();
            btnOpen.BringToFront();
            _lblAutoBackupStatus.BringToFront();
        }

        private void RefreshAutoBackupStatus()
        {
            if (_lblAutoBackupStatus != null)
                _lblAutoBackupStatus.Text = AutoBackupService.GetStatusText();
        }

        public static string ConnectionString => conReturn();

        // Method to return the connection string for SQLite
        public static string conReturn()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolderPathAppData = Path.Combine(appDataFolder, "DiselPetrolPump");
            string dbFilePath = Path.Combine(dbFolderPathAppData, "DiselPetrolPumpAlMadina.db");

            // Ensure directory exists and the database file exists
            if (!File.Exists(dbFilePath))
            {
                if (!Directory.Exists(dbFolderPathAppData))
                    Directory.CreateDirectory(dbFolderPathAppData);

                using (FileStream fs = File.Create(dbFilePath))
                {
                    fs.Close();
                }
            }

            return $@"Data Source={dbFilePath};Version=3;";
        }

        // Backup Button: Backs up the database
        private void btnBackup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Please select a valid backup path.");
                return;
            }

            string backupFilePath = Path.Combine(txtBackupPath.Text, "DiselPetrolPumpAlMadina_backup.db");

            try
            {
                BackupDatabase(backupFilePath);
                MessageBox.Show("Backup completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Restore Button: Restores the database from a backup file
        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRestorePath.Text))
            {
                MessageBox.Show("Please select a valid restore file.");
                return;
            }

            try
            {
                RestoreDatabase(txtRestorePath.Text);
                MessageBox.Show("Restore completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during restore: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to backup the SQLite database file to a new location using SQLite Backup API
        public static void BackupDatabase(string backupFilePath)
        {
            string databaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiselPetrolPump", "DiselPetrolPumpAlMadina.db");

            try
            {
                string dir = Path.GetDirectoryName(backupFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Create a backup connection
                using (SQLiteConnection sourceConnection = new SQLiteConnection($@"Data Source={databaseFilePath};Version=3;"))
                using (SQLiteConnection backupConnection = new SQLiteConnection($@"Data Source={backupFilePath};Version=3;"))
                {
                    sourceConnection.Open();
                    backupConnection.Open();

                    // Start the backup process
                    sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);

                    MessageBox.Show("Backup completed successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to restore the SQLite database file from a backup
        public static void RestoreDatabase(string restoreFilePath)
        {
            string databaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiselPetrolPump", "DiselPetrolPumpAlMadina.db");

            try
            {
                // Ensure database is not in use (close connection if it's open)
                using (SQLiteConnection con = new SQLiteConnection($@"Data Source={databaseFilePath};Version=3;"))
                {
                    con.Open();

                    // Perform restore operation by overwriting the database with the backup
                    File.Copy(restoreFilePath, databaseFilePath, true);
                    projectconnection.EnsureSchemaUpdates(databaseFilePath);

                    MessageBox.Show("Restore completed successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during restore: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Close the form (optional)
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }



        private void btnBrowseBackup_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = folderBrowser.SelectedPath;
                }
            }
        }

        // Browse Button for restore file
        private void btnBrowseRestore_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "SQLite Database Files (*.db)|*.db";
                openFileDialog.Title = "Select the SQLite Database File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtRestorePath.Text = openFileDialog.FileName;
                }
            }
        }
    }
}
