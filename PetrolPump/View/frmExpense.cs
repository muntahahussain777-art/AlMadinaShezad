using AlMadinaShahzad.Model;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.ReportForm;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmExpense : SampleView
    {
        public frmExpense()
        {
            InitializeComponent();
            this.KeyPreview = true;  // Enable form to capture key events
            this.KeyDown += new KeyEventHandler(frmCreditAdjust_KeyDown);
        }
        private void frmExpense_Load(object sender, EventArgs e)
        {
            LoadData();
        }
        private void frmCreditAdjust_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Ctrl key is pressed along with R
            if (e.Control && e.KeyCode == Keys.R)
            {
                // Call the method to reload the form
                LoadData();
                CustomeMessage noDataMessage = new CustomeMessage("Form reloaded successfully!", "Info");
                noDataMessage.ShowDialog();
            }
        }
        private void guna2DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (guna2DataGridView1.CurrentRow != null)
                {
                    // Confirm deletion
                    DialogResult confirmDelete = MessageBox.Show("Are you sure you want to delete this record?",
                                                                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (confirmDelete == DialogResult.Yes)
                    {
                        int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["dgvid"].Value);
                        string qry = "DELETE FROM Expensetable WHERE sid = " + id;
                        Hashtable ht = new Hashtable();
                        MainClass.DataInsertUpdateDelete(qry, ht);
                        CustomeMessage noDataMessage = new CustomeMessage("Record deleted successfully.", "Info");
                        noDataMessage.ShowDialog();
                    }
                }
            }
            LoadData();
        }

      
        private void LoadData()
        {
            string qry = "SELECT sid AS dgvid, Name, Category, Amount, EDate, Note FROM Expensetable WHERE Name LIKE '%" + txtSearch.Text + "%' ORDER BY sid";

            ListBox lb = new ListBox();
            lb.Items.Add(dgvid);
            lb.Items.Add(dgvname);
            lb.Items.Add(dgvCategory);
            lb.Items.Add(dgvamount);
            lb.Items.Add(dgvdate);
            lb.Items.Add(dgvNote);

            MainClass.loadData(qry, guna2DataGridView1, lb);
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmExpenseAdd frm = new frmExpenseAdd();
            frm.ShowDialog();
            LoadData();
        }

        private void guna2DataGridView1_CellDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null)
            {
                var cellValue = guna2DataGridView1.CurrentRow.Cells["dgvid"].Value;

                if (cellValue != DBNull.Value)
                {
                    int id = Convert.ToInt32(cellValue);
                    frmExpenseAdd frm = new frmExpenseAdd();
                    frm.id = id;
                    frm.ShowDialog();
                    LoadData();
                }
                else
                {
                    MessageBox.Show("The selected record does not have a valid ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);
        }
        public override void txtSearch_TextChanged_1(object sender, EventArgs e)
        {
            LoadData();
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            if (txtSearch.Text.Trim() != string.Empty)
            {
                searchrecord();
            }
            else
            {
                showallrecord();
            }
        }
        private void showallrecord()
        {
            string querystring = @"SELECT sid, Name, Category, Amount, EDate, Note 
                           FROM Expensetable";

            string connectionString = projectconnection.conReturn();

            using (var con = new SQLiteConnection(connectionString))
            using (var cmd = new SQLiteCommand(querystring, con))
            {
                cmd.CommandType = CommandType.Text;

                if (con.State != ConnectionState.Open)
                    con.Open();

                using (var sdr = cmd.ExecuteReader())
                {
                    if (sdr.HasRows)
                    {
                        DataTable dtRecord = new DataTable();
                        dtRecord.Load(sdr);

                        ExpenseReportForm rs = new ExpenseReportForm();
                        rs.ReportData = dtRecord;
                        rs.ReportPath = @"Reports\Expense.rdlc";
                        rs.ShowDialog();
                    }
                    else
                    {
                        ErrorFormMessage noDataMessage = new ErrorFormMessage(
                            "No records found.", "Info");
                        noDataMessage.ShowDialog();
                    }
                }
            }
        }


        private void searchrecord()
        {
            string querystring = @"
        SELECT sid, Name, Category, Amount, EDate, Note
        FROM Expensetable
        WHERE Name   LIKE @q
           OR CAST(Amount AS TEXT) LIKE @q;
    ";

            string connectionString = projectconnection.conReturn();

            using (var con = new SQLiteConnection(connectionString))
            using (var cmd = new SQLiteCommand(querystring, con))
            {
                cmd.CommandType = CommandType.Text;

                string q = (txtSearch.Text ?? string.Empty).Trim();
                cmd.Parameters.AddWithValue("@q", "%" + q + "%");

                if (con.State != ConnectionState.Open)
                    con.Open();

                using (var sdr = cmd.ExecuteReader())
                {
                    if (sdr.HasRows)
                    {
                        DataTable dtRecord = new DataTable();
                        dtRecord.Load(sdr);

                        ExpenseReportForm rs = new ExpenseReportForm();
                        rs.ReportData = dtRecord;
                        rs.ReportPath = @"Reports\Expense.rdlc";
                        rs.ShowDialog();
                    }
                    else
                    {
                        ErrorFormMessage noDataMessage = new ErrorFormMessage(
                            "No records found for your search.", "Info");
                        noDataMessage.ShowDialog();
                    }
                }
            }
        }

    }
}