using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class AttendanceReport : Sample
    {
        public AttendanceReport()
        {
            InitializeComponent();
        }

        private void AttendanceReport_Load(object sender, EventArgs e)
        {
            PopulateCustomerComboBox();
            LoadAllRecords(); // Load all records on form load
        }

        private void PopulateCustomerComboBox()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to get distinct dealer names only present in DieselLedger
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT DISTINCT DA.DealerName
                    FROM AddDealer DA
                    INNER JOIN DieselLedger DL ON DA.Did = DL.Did
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Populate ComboBox with dealer names that have entries in DieselLedger
                comboBoxCustomer.DataSource = dt;
                comboBoxCustomer.DisplayMember = "DealerName";  // Correct display member
                comboBoxCustomer.ValueMember = "DealerName";    // Correct value member
                comboBoxCustomer.DropDownHeight = 200;
                comboBoxCustomer.IntegralHeight = false;
            }
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to load all records from DieselLedger
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        DL.LedgerID,
                        DL.Date,
                        AC.Name,
                        DA.DealerName,
                        DL.AmounGiven,
                        DL.Balance,
                        DL.Note
                    FROM 
                        DieselLedger DL
                    LEFT JOIN 
                        AddCustomer AC ON DL.id = AC.id
                    LEFT JOIN 
                        AddDealer DA ON DL.Did = DA.Did
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                    ORDER BY 
                        DL.LedgerID ASC", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Set up the report data source
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "ReportTransfer.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                reportViewer1.RefreshReport();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string selectedCustomer = comboBoxCustomer.SelectedValue?.ToString();

            if (string.IsNullOrEmpty(selectedCustomer))
            {
                MessageBox.Show("Please select a dealer.");
                return;
            }

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        DL.LedgerID,
                        DL.Date,
                        AC.Name,
                        DA.DealerName,
                        DL.AmounGiven,
                        DL.Balance,
                        DL.Note
                    FROM 
                        DieselLedger DL
                    LEFT JOIN 
                        AddCustomer AC ON DL.id = AC.id
                    LEFT JOIN 
                        AddDealer DA ON DL.Did = DA.Did
                    WHERE 
                        TRIM(DA.DealerName) = @SearchText COLLATE NOCASE
                    " + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                    ORDER BY 
                        DL.LedgerID ASC", con);

                ReportSearchHelper.BindSearch(cmd, selectedCustomer);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Set up the report data source
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "ReportTransfer.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                reportViewer1.RefreshReport();
            }
        }
    }
}
