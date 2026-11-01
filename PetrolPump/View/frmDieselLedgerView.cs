using AlMadinaShahzad.Model;
using AlMadinaShahzad.Services;
using System;
using System.Data;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmDieselLedgerView : SampleView
    {
        public frmDieselLedgerView()
        {
            InitializeComponent();
        }

        private void frmDieselLedgerView_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeFilter(this, (s, ev) => LoadLedgerEntries(), left: 520, top: 12);
            LoadLedgerEntries();
        }

        public void LoadLedgerEntries()
        {
            string searchText = (txtSearch.Text ?? "").Trim().Replace("'", "''");
            string office = OfficeHelper.GetSelectedOffice(this, allowAll: true);
            string officeFilter = OfficeHelper.OfficeSqlFilter("DL.OfficeName", office);

            string query = @"SELECT 
                        DL.LedgerID,
                        DL.Did, 
                        DL.Date,
                        DA.DealerName,
                        DL.AmounGiven,
                        DL.Note
                     FROM DieselLedgerCredit DL
                     LEFT JOIN AddDealer DA ON DL.Did = DA.Did
                     WHERE (IFNULL(DA.DealerName, '') LIKE '%" + searchText + @"%'
                        OR IFNULL(DL.Note, '') LIKE '%" + searchText + @"%')" + officeFilter + @"
                     ORDER BY DL.LedgerID ASC";

            DataTable dt = MainClass.GetData(query);

            if (dt != null && dt.Rows.Count > 0)
            {
                guna2DataGridView1.DataSource = dt;
                guna2DataGridView1.AutoGenerateColumns = true;
            }
            else
                guna2DataGridView1.DataSource = null;
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmDieselLedgerDealerAdd frmAdd = new frmDieselLedgerDealerAdd();
            if (frmAdd.ShowDialog() == DialogResult.OK)
                LoadLedgerEntries();
        }

        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null)
            {
                int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["LedgerID"].Value);
                frmDieselLedgerDealerAdd frmEdit = new frmDieselLedgerDealerAdd(id);
                if (frmEdit.ShowDialog() == DialogResult.OK)
                    LoadLedgerEntries();
            }
            else
                MessageBox.Show("Please select a valid record.");
        }

        public override void txtSearch_TextChanged_1(object sender, EventArgs e) { }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadLedgerEntries();
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);
        }
    }
}
