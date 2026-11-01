using AlMadinaShahzad.Model;
using AlMadinaShahzad.Services;
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

namespace AlMadinaShahzad.View
{
    public partial class frmDealertoDealerView : SampleView
    {
        public frmDealertoDealerView()
        {
            InitializeComponent();
        }

        private void frmDealertoDealerView_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeFilter(this, (s, ev) => LoadLedgerEntries(), left: 520, top: 12);
            LoadLedgerEntries();  // Load ledger entries into DataGridView on form load
        }

        public void LoadLedgerEntries()
        {
            string searchText = txtSearch.Text.Trim();  // Get the search text
            string office = OfficeHelper.GetSelectedOffice(this, allowAll: true);
            string officeFilter = OfficeHelper.OfficeSqlFilter("DL.OfficeName", office);

            // SQL query to load data from DieselLedger with search functionality
            string query = @"SELECT 
        DL.LedgerID, 
        DL.Date,
        DL.FirstDealer,    -- Changed to FirstDealer
        DL.SecondDealer,   -- Changed to SecondDealer
        DL.AmounGiven,
        DL.AmountTaken,     -- Include AmountTaken if needed
        DL.Balance,         -- Include Balance if needed
        DL.Note,
        DL.Did,
        DL.id
     FROM 
        DealertoDealer DL
     WHERE 
        (DL.FirstDealer LIKE @searchText OR DL.SecondDealer LIKE @searchText)" + officeFilter + @"
     ORDER BY 
        DL.LedgerID ASC";

            // Prepare parameters for the search
            Hashtable ht = new Hashtable();
            ht.Add("@searchText", "%" + searchText + "%"); // Add wildcard for LIKE search

            // Execute the query and get the result
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

            if (dt != null && dt.Rows.Count > 0)
            {
                guna2DataGridView1.DataSource = dt;  // Bind data to DataGridView
                guna2DataGridView1.AutoGenerateColumns = true;  // Automatically generate columns based on the data
                guna2DataGridView1.Columns["LedgerID"].Visible = false;
                guna2DataGridView1.Columns["AmountTaken"].Visible = false;
                guna2DataGridView1.Columns["Did"].Visible = false;
                guna2DataGridView1.Columns["id"].Visible = false;
                guna2DataGridView1.Columns["Balance"].Visible = false;

            }
            else
            {
                // Only show the message if the search text is not empty
                if (!string.IsNullOrEmpty(searchText))
                {
                    guna2DataGridView1.DataSource = null;
                    CustomeMessage noDataMessage = new CustomeMessage("Koi ledger entries nahi mili!", "Info");
                    noDataMessage.ShowDialog();
                }
                else
                {
                    guna2DataGridView1.DataSource = null; // Clear the DataGridView if search text is empty
                }
            }
        }

        // Add a new record
        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmDealertoDealerAdd frmAdd = new frmDealertoDealerAdd();  // Open the add form
            if (frmAdd.ShowDialog() == DialogResult.OK)
            {
                LoadLedgerEntries();  // Reload the entries when the add form is closed with success
            }
        }

        // Edit an existing record on double-click
        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null)
            {
                int ledgerID = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["LedgerID"].Value);
                frmDealertoDealerAdd frmEdit = new frmDealertoDealerAdd(ledgerID);  // Pass the LedgerID for editing

                // Show the edit form
                if (frmEdit.ShowDialog() == DialogResult.OK)
                {
                    LoadLedgerEntries();  // Reload the entries after editing
                }
            }
            else
            {
                CustomeMessage noDataMessage = new CustomeMessage("Please select a valid record!", "Info");
                noDataMessage.ShowDialog();
            }
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadLedgerEntries();  // Reload entries on search text change
        }
        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);

        }
    }
}
