using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public partial class LoginForm : Sample
    {
        public LoginForm()
        {
            InitializeComponent();
            TryLoadLoginImage();
            SetupVipLogin();
        }

        private void TryLoadLoginImage()
        {
            try
            {
                var resources = new ComponentResourceManager(typeof(LoginForm));
                if (resources.GetObject("pictureBox1.Image") is Image img)
                    pictureBox1.Image = img;
            }
            catch
            {
                // Purani/corrupt Release exe par bhi login chale
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
         
                if (Isvalid())
                {
                string connectionString = projectconnection.conReturn();

                // SqlConnection object banayein
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                        // Iske baad jo bhi code aapka tha usko rakh sakte hain
                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT UserID, uName, IFNULL(uRole,'Admin') AS uRole FROM [tblUser] WHERE uUsername = @Username AND uPass = @Password", con))
                        {
                            // Parameters ko add karein
                            cmd.Parameters.AddWithValue("@Username", txtname.Text.Trim());
                            cmd.Parameters.AddWithValue("@Password", txtpassword.Text.Trim());

                            // Connection open karein
                            con.Open();

                            // ExecuteReader ka use karein
                            SQLiteDataReader sdr = cmd.ExecuteReader();

                            // Check karein agar koi row mil gayi
                            if (sdr.HasRows)
                            {
                                sdr.Read();
                                int userId = Convert.ToInt32(sdr["UserID"]);
                                string userName = sdr["uName"]?.ToString() ?? txtname.Text.Trim();
                                string role = sdr["uRole"]?.ToString() ?? "Admin";
                                RoleAccessService.SetSession(userId, userName, role);
                                MainClass.USER = userName;

                                // Agar user mil gaya, form ko hide karein aur mainform ko show karein
                                this.Hide();
                                frmMain df = new frmMain();
                                df.Show();
                            }
                            else
                            {
                                // Agar user nahi mila, error message show karein
                                MessageBox.Show("User Name or Password is incorrect, Please enter correct Login details", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
        }
        private bool Isvalid()
        {
            if (txtname.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Username is Required", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtname.Focus();
                return false;
            }
            if (txtpassword.Text.Trim() == string.Empty)
            {
                MessageBox.Show("password is Required", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtpassword.Focus();
                return false;
            }
            return true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
