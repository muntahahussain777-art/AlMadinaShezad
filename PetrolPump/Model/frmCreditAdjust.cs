using AlMadinaShahzad.ReportForm;
using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmCreditAdjust : SampleView
    {
        private const string SoftwareName = "Haji Baloch Petrolleum";
        // 👆 yahan apne software ka asli naam likh lena

        public frmCreditAdjust()
        {
            InitializeComponent();
            this.KeyPreview = true;  // Enable form to capture key events
            this.KeyDown += new KeyEventHandler(frmCreditAdjust_KeyDown);  // Add KeyDown event handler
            this.guna2DataGridView1.CellFormatting += new DataGridViewCellFormattingEventHandler(guna2DataGridView1_CellFormatting);
        }

        private void frmCreditAdjust_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                LoadData1();
                MessageBox.Show("Form reloaded successfully!", "Reload", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void frmCreditAdjust_Load(object sender, EventArgs e)
        {
            LoadData1();
            Frm_DataSaved();
            guna2DataGridView1.KeyDown += new KeyEventHandler(guna2DataGridView1_KeyDown);
            guna2DataGridView1.Refresh();
            this.guna2DataGridView1.CellFormatting += new DataGridViewCellFormattingEventHandler(guna2DataGridView1_CellFormatting);

            // 🔹 VIP WhatsApp button event
            btnVipWhatsapp.Click += btnVipWhatsapp_Click;
        }

        private void guna2DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && guna2DataGridView1.CurrentRow != null)
            {
                DialogResult confirmDelete = MessageBox.Show("Are you sure you want to delete this record?",
                                                            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmDelete == DialogResult.Yes)
                {
                    int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["dgvid"].Value);
                    string qry = "DELETE FROM PetrolAdd WHERE pid = " + id;
                    Hashtable ht = new Hashtable();
                    MainClass.DataInsertUpdateDelete(qry, ht);
                    MessageBox.Show("Record deleted successfully.");
                    LoadData1();
                }
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmCreditAdjustAdd frm = new frmCreditAdjustAdd();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadData1();
            }
        }

        private void Frm_DataSaved()
        {
            LoadData1();
        }

        public void LoadData1()
        {
            string searchText = txtSearch.Text;

            // Query to show credit entries only where IsInitialEntry = 0
            string qryCredits = @"
                SELECT 
                    PetrolAdd.pid, 
                    PetrolAdd.Date, 
                    PetrolAdd.ReceiptNo,  
                    PetrolAdd.vehicle,  
                    0 AS Amount,  
                    PetrolAdd.Credit,  
                    0 AS Balance,  
                    PetrolAdd.Note,  
                    AddCustomer.Name
                FROM PetrolAdd
                INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
                WHERE AddCustomer.Name LIKE '%" + searchText + @"%' AND PetrolAdd.IsInitialEntry = 0";

            // Load data into the DataGridView (only credit entries)
            ListBox lb = new ListBox();
            lb.Items.Add(dgvid);
            lb.Items.Add(dgvDate);
            lb.Items.Add(dgvreciptno);
            lb.Items.Add(dgvVechleNo);
            lb.Items.Add(dgvAmount);
            lb.Items.Add(dgvCredit);
            lb.Items.Add(dgvBalance);
            lb.Items.Add(dgvNote);
            lb.Items.Add(dgvName);

            MainClass.loadData(qryCredits, guna2DataGridView1, lb);
            DataTable dt = MainClass.ExecuteSelectQuery(qryCredits, null);

            if (dt != null)
            {
                // Add IsInitialEntry column to DataTable
                dt.Columns.Add("IsInitialEntry", typeof(int));

                // Set IsInitialEntry to 0 for all credit entries
                foreach (DataRow row in dt.Rows)
                {
                    row["IsInitialEntry"] = 0; // Only credit entries are loaded
                }

                // Calculate total summarized balance for IsInitialEntry = 1 (not shown in grid)
                string balanceQuery = @"
                    SELECT SUM(Balance) AS TotalBalance 
                    FROM PetrolAdd 
                    INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id 
                    WHERE AddCustomer.Name LIKE '%" + searchText + @"%' AND PetrolAdd.IsInitialEntry = 1";
                Hashtable htBalance = new Hashtable();
                DataTable dtBalance = MainClass.ExecuteSelectQuery(balanceQuery, htBalance);

                decimal totalSummarizedBalance = 0;
                if (dtBalance != null && dtBalance.Rows.Count > 0 && dtBalance.Rows[0]["TotalBalance"] != DBNull.Value)
                {
                    totalSummarizedBalance = Convert.ToDecimal(dtBalance.Rows[0]["TotalBalance"]);
                }

                // Calculate total credits for IsInitialEntry = 0 (shown in grid)
                decimal totalCredits = dt.AsEnumerable()
                    .Sum(r =>
                    {
                        object creditValue = r["Credit"];
                        if (creditValue != DBNull.Value && creditValue != null)
                        {
                            if (decimal.TryParse(creditValue.ToString(), out decimal result))
                            {
                                return result;
                            }
                        }
                        return 0m; // Return 0 if null or invalid
                    });

                // Calculate remaining total balance
                decimal remainingTotalBalance = totalSummarizedBalance - totalCredits;

                // Add a summary row to display the remaining total balance
                DataRow summaryRow = dt.NewRow();
                summaryRow["pid"] = DBNull.Value;
                summaryRow["Date"] = DBNull.Value;
                summaryRow["ReceiptNo"] = DBNull.Value;
                summaryRow["Vehicle"] = DBNull.Value;
                summaryRow["Amount"] = 0;
                summaryRow["Credit"] = 0;
                summaryRow["Balance"] = remainingTotalBalance; // Final remaining balance after credits
                summaryRow["Note"] = "Remaining Balance after Credits";
                summaryRow["Name"] = "All Customers";
                summaryRow["IsInitialEntry"] = 1; // Mark as summarized entry
                dt.Rows.Add(summaryRow);

                // Bind the DataTable to the DataGridView
                guna2DataGridView1.DataSource = dt;

                // Disable sorting on columns
                foreach (DataGridViewColumn column in guna2DataGridView1.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                // Credit Add hamesha open — balance 0 ho to bhi credit entry ho sake
                btnAdd.Enabled = true;

                // Display the remaining balance in a label (assuming you have a label named 'lblTotalRemaining')
                if (this.Controls.ContainsKey("lblTotalRemaining"))
                {
                    ((Label)this.Controls["lblTotalRemaining"]).Text = $"Total Remaining: {remainingTotalBalance:F2}";
                }
            }
            else
            {
                guna2DataGridView1.DataSource = null; // Clear grid if no data
            }
        }

        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.SelectedRows.Count > 0)
            {
                var idCellValue = guna2DataGridView1.SelectedRows[0].Cells["dgvid"].Value;
                var nameCellValue = guna2DataGridView1.SelectedRows[0].Cells["dgvName"].Value;

                if (idCellValue != DBNull.Value)
                {
                    frmCreditAdjustAdd frm = new frmCreditAdjustAdd();
                    frm.id = Convert.ToInt32(idCellValue);

                    // Customer name ko form mein bhejna
                    if (nameCellValue != null && !string.IsNullOrEmpty(nameCellValue.ToString()))
                    {
                        frm.SetCustomerName(nameCellValue.ToString()); // Naya method use karo
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        LoadData1();
                    }
                }
                else
                {
                    MessageBox.Show("Is entry ka valid ID nahi hai.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (guna2DataGridView1.Columns[e.ColumnIndex].Name == "IsInitialEntry")
            {
                if (e.Value != null && e.Value is int)
                {
                    int isInitialEntry = Convert.ToInt32(e.Value);
                    if (isInitialEntry == 1)
                    {
                        e.CellStyle.BackColor = Color.LightGreen;
                    }
                    else if (isInitialEntry == 0)
                    {
                        e.CellStyle.BackColor = Color.LightCoral;
                    }
                }
            }
            if (guna2DataGridView1.Columns[e.ColumnIndex].Name == "dgvBalance" && e.Value != null)
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal balanceValue))
                {
                    e.Value = balanceValue.ToString("N2");
                    e.FormattingApplied = true;
                }
            }
            MainClass.SrNo(guna2DataGridView1);
        }
        private void btnVipWhatsapp_Click(object sender, EventArgs e)
        {
            try
            {
                // 1) Pehle check: txtSearch khali na ho
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    MessageBox.Show("Please pehle txtSearch mein kisi customer ka naam likhein, phir WhatsApp button press karein.",
                                    "No Customer Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2) Doosra check: grid mein koi row select ho
                if (guna2DataGridView1.CurrentRow == null || guna2DataGridView1.CurrentRow.Index < 0)
                {
                    MessageBox.Show("Please grid se kisi aik customer ki row select karein, phir WhatsApp button press karein.",
                                    "No Row Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var row = guna2DataGridView1.CurrentRow;

                // 3) Customer name grid se
                string customerName = Convert.ToString(row.Cells["dgvName"].Value);
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    MessageBox.Show("Is row mein customer ka naam nahi mila.",
                                    "Invalid Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // SQL ke liye safe name (single quote escape)
                string safeName = customerName.Replace("'", "''");

                // 4) Total summarized balance (IsInitialEntry = 1) sirf is customer ka
                string balanceQuery = @"
            SELECT SUM(Balance) AS TotalBalance 
            FROM PetrolAdd 
            INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id 
            WHERE AddCustomer.Name = '" + safeName + @"'
              AND PetrolAdd.IsInitialEntry = 1";

                // 5) Total credits (IsInitialEntry = 0) sirf is customer ke
                string creditQuery = @"
            SELECT SUM(Credit) AS TotalCredit 
            FROM PetrolAdd 
            INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id 
            WHERE AddCustomer.Name = '" + safeName + @"'
              AND PetrolAdd.IsInitialEntry = 0";

                DataTable dtBalance = MainClass.ExecuteSelectQuery(balanceQuery, null);
                DataTable dtCredit = MainClass.ExecuteSelectQuery(creditQuery, null);

                decimal totalSummarizedBalance = 0;
                if (dtBalance != null && dtBalance.Rows.Count > 0 && dtBalance.Rows[0]["TotalBalance"] != DBNull.Value)
                {
                    totalSummarizedBalance = Convert.ToDecimal(dtBalance.Rows[0]["TotalBalance"]);
                }

                decimal totalCredits = 0;
                if (dtCredit != null && dtCredit.Rows.Count > 0 && dtCredit.Rows[0]["TotalCredit"] != DBNull.Value)
                {
                    totalCredits = Convert.ToDecimal(dtCredit.Rows[0]["TotalCredit"]);
                }

                // 6) Is customer ka actual remaining balance
                decimal remainingBalance = totalSummarizedBalance - totalCredits;

                // 🔹 VIP style message
                string message =
                    $"{customerName}, Assalam o Alaikum 🌟\n\n" +
                    $"Ye message hamare \"{SoftwareName}\" software se aap ke record ke mutaliq hai.\n\n" +
                    $"Customer Name: {customerName}\n" +
                    $"Total Remaining Balance: {remainingBalance:N2}\n" +
                    $"Total Credit (Jama): {totalCredits:N2}\n\n" +
                    $"Meherbani farma kar is record ko aik dafa check kar lain.\n" +
                    $"Agar kisi cheez mein farq ya confusion ho to please WhatsApp par hi reply kar dein.\n\n" +
                    $"JazakAllah khair ✨";

                // 🔹 Message clipboard mein copy karo
                try
                {
                    Clipboard.SetText(message);
                }
                catch
                {
                    // agar clipboard access na mile to ignore, app crash na ho
                }

                // 🔹 Pehle WhatsApp Desktop app kholne ki koshish karo
                bool opened = false;
                try
                {
                    var psiDesktop = new ProcessStartInfo
                    {
                        FileName = "whatsapp://",   // PC app ke liye protocol
                        UseShellExecute = true
                    };
                    Process.Start(psiDesktop);
                    opened = true;
                }
                catch
                {
                    opened = false;
                }

                // Agar desktop app na khule to fallback WhatsApp Web
                if (!opened)
                {
                    try
                    {
                        var psiWeb = new ProcessStartInfo
                        {
                            FileName = "https://web.whatsapp.com",
                            UseShellExecute = true
                        };
                        Process.Start(psiWeb);
                    }
                    catch
                    {
                        // ignore, agar browser bhi na khule to neeche wala message help karega
                    }
                }

                // User ko guide karo
                MessageBox.Show(
                    "WhatsApp open ho gaya hai (PC app ya Web).\n" +
                    "Message clipboard mein copy ho chuka hai.\n" +
                    "Kisi chat mein ja kar Ctrl+V se paste karein, phir khud Send dabayein.",
                    "VIP WhatsApp Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Haji Baloch WhatsApp message bhejte waqt error aaya:\n" + ex.Message,
                                "WhatsApp Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadData1();
        }
    }
}