using AlMadinaShahzad.Model;
using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmDieselLedger : SampleView
    {
        public frmDieselLedger()
        {
            InitializeComponent();
        }

        private void frmDieselLedger_Load(object sender, EventArgs e)
        {
            LoadLedgerEntries();  // form load pe ledger entries
        }

        public void LoadLedgerEntries()
        {
            try
            {
                string searchText = txtSearch.Text.Trim();

                string query = @"
                    SELECT 
                        DL.LedgerID, 
                        DL.Date,
                        AC.Name AS CustomerName,
                        DA.DealerName,
                        DL.AmounGiven,
                        DL.Note
                    FROM 
                        DieselLedger DL
                    LEFT JOIN 
                        AddCustomer AC ON DL.id = AC.id
                    LEFT JOIN 
                        AddDealer DA ON DL.Did = DA.Did
                    WHERE 
                        (@searchText = '' 
                         OR AC.Name LIKE @searchText 
                         OR DA.DealerName LIKE @searchText)
                    ORDER BY 
                        DL.LedgerID ASC;
                ";

                Hashtable ht = new Hashtable
                {
                    { "@searchText", "%" + searchText + "%" }
                };

                DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    guna2DataGridView1.DataSource = dt;
                    guna2DataGridView1.AutoGenerateColumns = true;
                }
                else
                {
                    guna2DataGridView1.DataSource = null;

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        CustomeMessage noDataMessage = new CustomeMessage("Koi ledger entries nahi mili!", "Info");
                        noDataMessage.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("LoadLedgerEntries error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // Add a new record
        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                frmDieselLedgerAdd frmAdd = new frmDieselLedgerAdd();  // nayi entry form
                if (frmAdd.ShowDialog() == DialogResult.OK)
                {
                    LoadLedgerEntries();  // reload grid
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Add form error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // Edit an existing record on double-click
        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || guna2DataGridView1.CurrentRow == null)
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid record!", "Info");
                    noDataMessage.ShowDialog();
                    return;
                }

                DataGridViewRow row = guna2DataGridView1.CurrentRow;
                if (row == null)
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid record!", "Info");
                    noDataMessage.ShowDialog();
                    return;
                }

                int id = Convert.ToInt32(row.Cells["LedgerID"].Value);
                string customerName = row.Cells["CustomerName"].Value?.ToString();
                string dealerName = row.Cells["DealerName"].Value?.ToString();

                frmDieselLedgerAdd frmEdit = new frmDieselLedgerAdd(id, customerName, dealerName);
                if (frmEdit.ShowDialog() == DialogResult.OK)
                {
                    LoadLedgerEntries();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Edit form open error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadLedgerEntries();
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                MainClass.SrNo(guna2DataGridView1);
            }
            catch
            {
                // numbering na bhi ho to crash na ho
            }
        }
    }
}
