using AlMadinaShahzad.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace AlMadinaShahzad
{
    public partial class changepassword : SampleView
    {
        public changepassword()
        {
            InitializeComponent();
        }

        private void changepassword_Load(object sender, EventArgs e)
        {
            LoadData();
            EnsureAutoBackupSettingsButton();
        }

        private void EnsureAutoBackupSettingsButton()
        {
            if (Controls.Find("btnAutoBackupSettings", true).Length > 0)
                return;

            var btn = new Button
            {
                Name = "btnAutoBackupSettings",
                Text = "Auto Backup Settings",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Size = new Size(190, 33),
                Location = new Point(640, 37),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(112, 51, 255),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.Click += (s, ev) => AlMadinaShahzad.Services.AutoBackupService.ShowSetupDialog(false);

            // SampleView top panel pe lagao agar mile, warna form pe
            Control[] panels = Controls.Find("panel1", true);
            if (panels != null && panels.Length > 0)
            {
                panels[0].Controls.Add(btn);
                btn.BringToFront();
            }
            else
            {
                Controls.Add(btn);
                btn.BringToFront();
            }
        }
      
        private void LoadData()
        {
            string qry;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
               
                qry = " SELECT UserID, uName, uUserName, uPass FROM  [tblUser]";
            }
            else
            {
                qry = "  SELECT UserID, uName, uUserName, uPass FROM  [tblUser] WHERE uName LIKE '%" + txtSearch.Text + "%'";
            }

            ListBox lb = new ListBox();
            lb.Items.Add(dgvid);
            lb.Items.Add(dgvname);
            lb.Items.Add(dgvusername);
            lb.Items.Add(dgvpassword);
   
            MainClass.loadData(qry, guna2DataGridView1, lb);
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            passwordchange frm = new passwordchange();
            frm.ShowDialog();
            LoadData();
        }

        private void guna2DataGridView1_CellDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["dgvid"].Value);
            passwordchange frm = new passwordchange();
            frm.id = id;
            frm.ShowDialog();
            LoadData();
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);
        }
        public override void txtSearch_TextChanged_1(object sender, EventArgs e)
        {
            LoadData();
        }

    }
}
