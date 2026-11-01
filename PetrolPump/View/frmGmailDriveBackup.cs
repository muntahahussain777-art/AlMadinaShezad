using Guna.UI2.WinForms;
using AlMadinaShahzad.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    /// <summary>
    /// Naya form — purane backup/forms ko change kiye bina Gmail / Google Drive backup.
    /// </summary>
    public class frmGmailDriveBackup : Sample
    {
        private Guna2TextBox _txtGmail;
        private Guna2TextBox _txtAppPassword;
        private Guna2Button _btnBackup;
        private Label _lblInfo;
        private Label _lblStatus;

        public frmGmailDriveBackup()
        {
            BackColor = Color.FromArgb(32, 36, 66);
            BuildUi();
            Load += FrmGmailDriveBackup_Load;
        }

        private void FrmGmailDriveBackup_Load(object sender, EventArgs e)
        {
            var (gmail, appPassword) = GoogleDriveBackupService.LoadSettings();
            _txtGmail.Text = gmail;
            _txtAppPassword.Text = appPassword;

            if (GoogleDriveApiUploader.IsConfigured())
                _lblStatus.Text = "Direct Drive upload: Ready (pehli dafa browser sign-in)";
            else if (!string.IsNullOrWhiteSpace(appPassword))
                _lblStatus.Text = "Gmail App Password set — backup email se ho sakta hai";
            else
                _lblStatus.Text = "App.config mein GoogleDriveClientId set karein YA App Password likhein";
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "☁ Gmail / Google Drive Backup (SQLite)",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            _lblInfo = new Label
            {
                Text =
                    "1) Apni Gmail likhein.\n" +
                    "2) Backup dabayein — pehli dafa browser khulega, Gmail se sign-in karein.\n" +
                    "3) File seedha Google Drive par upload hogi (Desktop install ki zaroorat nahi).\n" +
                    "4) Agar ClientId na ho to Gmail App Password se email backup.\n\n" +
                    "Sirf SQLite backup · Bina Drive Desktop",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Silver,
                Location = new Point(24, 58),
                Size = new Size(900, 110)
            };

            var lblGmail = MakeCaption("Gmail Address", 190);
            _txtGmail = MakeTextBox(220);

            var lblPass = MakeCaption("Gmail App Password (agar Drive ClientId na ho)", 280);
            _txtAppPassword = MakeTextBox(310);
            _txtAppPassword.UseSystemPasswordChar = true;
            _txtAppPassword.PlaceholderText = "Sirf tab jab Google Drive Desktop na ho";

            _btnBackup = new Guna2Button
            {
                Text = "☁ Backup to Google Drive",
                Size = new Size(260, 42),
                Location = new Point(24, 370),
                BorderRadius = 14,
                FillColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
            };
            _btnBackup.Click += BtnBackup_Click;

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(144, 238, 144),
                Location = new Point(24, 430),
                Size = new Size(900, 80)
            };

            Controls.Add(lblTitle);
            Controls.Add(_lblInfo);
            Controls.Add(lblGmail);
            Controls.Add(_txtGmail);
            Controls.Add(lblPass);
            Controls.Add(_txtAppPassword);
            Controls.Add(_btnBackup);
            Controls.Add(_lblStatus);
        }

        private static Label MakeCaption(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.Silver,
                AutoSize = true,
                Location = new Point(24, y)
            };
        }

        private Guna2TextBox MakeTextBox(int y)
        {
            return new Guna2TextBox
            {
                Location = new Point(24, y),
                Size = new Size(420, 36),
                BorderRadius = 8,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F)
            };
        }

        private async void BtnBackup_Click(object sender, EventArgs e)
        {
            _btnBackup.Enabled = false;
            _lblStatus.ForeColor = Color.Gold;
            _lblStatus.Text = "Backup ho raha hai...";

            string gmail = _txtGmail.Text;
            string appPassword = _txtAppPassword.Text;

            try
            {
                GoogleDriveBackupResult result = await Task.Run(() =>
                    GoogleDriveBackupService.BackupToGoogleDrive(gmail, appPassword));

                _lblStatus.ForeColor = Color.FromArgb(144, 238, 144);
                _lblStatus.Text = result.Message;
                MessageBox.Show(result.Message, "Backup OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
                _lblStatus.Text = ex.Message;
                MessageBox.Show(ex.Message, "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnBackup.Enabled = true;
            }
        }
    }
}
