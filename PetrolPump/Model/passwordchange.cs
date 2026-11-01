using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace AlMadinaShahzad.Model
{
    public partial class passwordchange : SampleAdd
    {
        public passwordchange()
        {
            InitializeComponent();
        }
        public int id = 0;
        private void passwordchange_Load(object sender, EventArgs e)
        {
            LoadData();
            txtName.Focus();
            EnsureBackupSettingsButton();
        }

        private void EnsureBackupSettingsButton()
        {
            if (Controls.Find("btnAutoBackupSettings", true).Length > 0)
                return;

            // Form thora lamba — button Save panel ke upar clearly dikhe
            if (this.ClientSize.Height < 280)
                this.ClientSize = new Size(Math.Max(this.ClientSize.Width, 618), 280);

            var btn = new Button
            {
                Name = "btnAutoBackupSettings",
                Text = "Auto Backup Settings",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Size = new Size(200, 36),
                Location = new Point(19, 150),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(112, 51, 255),
                ForeColor = Color.White
            };
            btn.Click += (s, ev) =>
            {
                AlMadinaShahzad.Services.AutoBackupService.ShowSetupDialog(false);
            };
            Controls.Add(btn);
            btn.BringToFront();
        }

        private void LoadData()
        {
            if (id > 0)
            {
                DataTable dt = MainClass.GetData("Select * from  tblUser where sid = " + id);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtName.Text = row["uName"].ToString();
                    txtusername.Text = row["uUserName"].ToString();
                    txtpassword.Text = row["uPass"].ToString();
                  
                }
            }
        }
        public override void btnSave_Click(object sender, EventArgs e)
        {
       
            try
            {
                string qry;
                if (id == 0)
                {
                    qry = "INSERT INTO [tblUser] (uName, uUserName, uPass) VALUES (@Name,  @UserName, @Password)";
                }
                else
                {
                    qry = "UPDATE [tblUser] SET Name = @Name,  UserName = @UserName, " +
                                     "Password = @Password WHERE userID = @id";
                }

                Hashtable ht = new Hashtable
        {
            { "@id", id },
            { "@Name", txtName.Text },
            { "@UserName", txtusername.Text },
            { "@Password", txtpassword.Text },
        };

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    MessageBox.Show("Saved Successfully");
                    MainClass.Enable_reset(this);
                    id = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                string qry = "Delete from  [tblUser] where userID = " + id + " ";
                Hashtable ht = new Hashtable();
                MainClass.DataInsertUpdateDelete(qry, ht);
                MessageBox.Show("Deleted Successfully");
                MainClass.Enable_reset(this);
                id = 0;
            }
        }
    }
}
