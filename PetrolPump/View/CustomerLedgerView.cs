using PetrolPump.Model;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace PetrolPump.View
{
    public partial class CustomerLedgerView : SampleView
    {
        public CustomerLedgerView()
        {
            InitializeComponent();
        }

        private void CustomerLedgerView_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            string qry = string.IsNullOrWhiteSpace(txtSearch.Text)
                ? @"SELECT CL.CustomerId, C.Name AS CustomerName, D.DealerName, CL.Debit, CL.Credit, CL.Notes, CL.Date 
                    FROM CustomerLedger CL 
                    LEFT JOIN AddCustomer C ON CL.CustomerId = C.id 
                    LEFT JOIN AddDealer D ON CL.CustomerId = D.Did"
                : @"SELECT CL.sid, C.Name AS CustomerName, D.DealerName, CL.Debit, CL.Credit, CL.Notes, CL.Date 
                    FROM CustomerLedger CL 
                    LEFT JOIN AddCustomer C ON CL.sid = C.id 
                    LEFT JOIN AddDealer D ON CL.sid = D.Did 
                    WHERE C.Name LIKE @searchText OR D.DealerName LIKE @searchText";

            Hashtable parameters = new Hashtable { { "@searchText", $"%{txtSearch.Text}%" } };
            DataTable dt = ExecuteSelectQuery(qry, parameters);
            guna2DataGridView1.DataSource = dt;

            decimal totalDebit = dt.AsEnumerable().Sum(row => row["Debit"] != DBNull.Value ? Convert.ToDecimal(row["Debit"]) : 0);
            decimal totalCredit = dt.AsEnumerable().Sum(row => row["Credit"] != DBNull.Value ? Convert.ToDecimal(row["Credit"]) : 0);
            decimal balance = totalDebit - totalCredit;

            totaldebitamountlabel.Text = $"Total Debit: {totalDebit:N2}";
            totalcreditamountlabel.Text = $"Total Credit: {totalCredit:N2}";
            totalbalancelabel.Text = $"Balance: {balance:N2}";
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            CustomerLedgerAdd frm = new CustomerLedgerAdd();
            frm.ShowDialog();
            LoadData();
        }

        private void guna2DataGridView1_CellDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = guna2DataGridView1.Rows[e.RowIndex];
                CustomerLedgerAdd frm = new CustomerLedgerAdd
                {
                    id = Convert.ToInt32(selectedRow.Cells["dgvid"].Value),
                    txtdebit = { Text = selectedRow.Cells["dgvDebit"].Value.ToString() },
                    txtcredit = { Text = selectedRow.Cells["dgvCredit"].Value.ToString() },
                    txtnote = { Text = selectedRow.Cells["dgvNote"].Value.ToString() }
                };

                string selectedCustomerName = selectedRow.Cells["dgvname"].Value.ToString();
                frm.SetCustomerAndDealerNames(selectedCustomerName);

                frm.date = DateTime.TryParse(selectedRow.Cells["dgvdate"].Value.ToString(), out DateTime parsedDate)
                    ? parsedDate.ToString("MM-dd-yyyy")
                    : string.Empty;

                frm.ShowDialog();
                LoadData();
            }
        }

        public override void txtSearch_TextChanged_1(object sender, EventArgs e)
        {
            LoadData();
        }

        private DataTable ExecuteSelectQuery(string query, Hashtable parameters)
        {
            // Database select query helper method
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection("your_connection_string"))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                foreach (DictionaryEntry param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key.ToString(), param.Value);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }
    }
}
