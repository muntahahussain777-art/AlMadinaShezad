using AlMadinaShahzad.Model;
using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmCustomerToCustomerView : SampleView
    {
        public frmCustomerToCustomerView()
        {
            InitializeComponent();
        }

        private void frmCustomerToCustomerView_Load(object sender, EventArgs e)
        {
            LoadLedgerEntries();
        }

        public void LoadLedgerEntries()
        {
            string searchText = txtSearch.Text.Trim();

            string query = @"SELECT 
                                DL.LedgerID, 
                                DL.Date,
                                AC.Name AS CustomerName,
                                AC2.Name AS DealerName,
                                DL.AmounGiven,
                                DL.Note
                            FROM 
                                CustomerToCustomer DL
                            LEFT JOIN 
                                AddCustomer AC ON DL.id = AC.id  
                            LEFT JOIN 
                                AddCustomer AC2 ON DL.Did = AC2.id 
                            WHERE 
                                (AC.Name LIKE @searchText OR AC2.Name LIKE @searchText OR DL.Note LIKE @searchText)
                            ORDER BY 
                                DL.LedgerID ASC";

            Hashtable ht = new Hashtable();
            ht.Add("@searchText", "%" + searchText + "%");

            try
            {
                DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    guna2DataGridView1.DataSource = dt;
                    guna2DataGridView1.AutoGenerateColumns = true;
                    guna2DataGridView1.Columns["LedgerID"].HeaderText = "Ledger ID";
                    guna2DataGridView1.Columns["Date"].HeaderText = "Date";
                    guna2DataGridView1.Columns["CustomerName"].HeaderText = "First Customer";
                    guna2DataGridView1.Columns["DealerName"].HeaderText = "Second Customer";
                    guna2DataGridView1.Columns["AmounGiven"].HeaderText = "Amount Given";
                    guna2DataGridView1.Columns["Note"].HeaderText = "Note";
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
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmCustomerToCustomerAdd frmAdd = new frmCustomerToCustomerAdd();
            if (frmAdd.ShowDialog() == DialogResult.OK)
            {
                LoadLedgerEntries();
            }
        }

        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null)
            {
                try
                {
                    int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["LedgerID"].Value);
                    string customerName = guna2DataGridView1.CurrentRow.Cells["CustomerName"].Value?.ToString();
                    string dealerName = guna2DataGridView1.CurrentRow.Cells["DealerName"].Value?.ToString();

                    frmCustomerToCustomerAdd frmEdit = new frmCustomerToCustomerAdd(id, customerName, dealerName);
                    if (frmEdit.ShowDialog() == DialogResult.OK)
                    {
                        LoadLedgerEntries();
                    }
                }
                catch (Exception ex)
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                    errorMessage.ShowDialog();
                }
            }
            else
            {
                CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek valid record select karein!", "Warning");
                noSelectionMessage.ShowDialog();
            }
        }
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