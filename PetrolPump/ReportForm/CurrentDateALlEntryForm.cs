using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class CurrentDateALlEntryForm : Sample
    {
        public CurrentDateALlEntryForm()
        {
            InitializeComponent();
        }

        private void CurrentDateALlEntryForm_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default from date set to current date
            todate.Value = DateTime.Now;    // Default to date set to current date
            this.reportViewer1.RefreshReport();
            LoadAllRecords();               // Load current date records on form load
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // SQL query to load all customers with current date entries (if any)
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        c.id AS CustomerId,
                        c.Name AS CustomerName,
                        p.pid, 
                        p.Date, 
                        p.ReceiptNo, 
                        p.vehicle, 
                        IFNULL(p.Litter, 0) AS Litter, 
                        IFNULL(p.Rate, 0) AS Rate, 
                        IFNULL(p.Advance, 0) AS Advance, 
                        IFNULL(p.Amount, 0) AS Amount, 
                        IFNULL(p.Credit, 0) AS Credit, 
                        IFNULL(p.Balance, 0) AS Balance, 
                        p.Note
                    FROM AddCustomer c
                    LEFT JOIN PetrolAdd p ON p.CustomerId = c.id 
                        AND p.Date = DATE('now')" + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "c.OfficeName") + @"
                    ORDER BY c.Name", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Agar koi data na mile to message dikhayein
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Koi customer data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // ReportDataSource banayein aur data source ko set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "CurrentDateReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search text from input

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // SQL query date range aur search ke saath, saare customers dikhane ke liye
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        c.id AS CustomerId,
                        c.Name AS CustomerName,
                        p.pid, 
                        p.Date, 
                        p.ReceiptNo, 
                        p.vehicle, 
                        IFNULL(p.Litter, 0) AS Litter, 
                        IFNULL(p.Rate, 0) AS Rate, 
                        IFNULL(p.Advance, 0) AS Advance, 
                        IFNULL(p.Amount, 0) AS Amount, 
                        IFNULL(p.Credit, 0) AS Credit, 
                        IFNULL(p.Balance, 0) AS Balance, 
                        p.Note
                    FROM AddCustomer c
                    LEFT JOIN PetrolAdd p ON p.CustomerId = c.id 
                        AND date(p.Date) >= date(@FromDate) AND date(p.Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
                    WHERE (@SearchText = '' OR TRIM(c.Name) = @SearchText COLLATE NOCASE)
                    " + ReportOfficeHelper.Sql(this, "c.OfficeName") + @"
                    ORDER BY c.Name", con);

                // Parameters set karein
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Agar koi data na mile to message dikhayein
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range ya search ke liye koi customer data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ReportDataSource set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "CurrentDateReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}