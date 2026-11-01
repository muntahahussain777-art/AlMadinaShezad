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
    public partial class DealerFormReport : Sample
    {
        public DealerFormReport()
        {
            InitializeComponent();
        }

        private void DealerFormReport_Load(object sender, EventArgs e)
        {
            PopulateCustomerComboBox();
            LoadAllRecords(); // Load all records on form load
        }

        private void PopulateCustomerComboBox()
        {
            // Connection string ko retrieve karein
            string connectionString = projectconnection.conReturn();

            // SQLiteConnection object create karein
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to get distinct dealer names
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT DISTINCT DealerName
                    FROM AddDealer WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
", con);

                // DataTable mein data fill karein
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ComboBox ko dealer names se populate karein
                comboBoxCustomer.DataSource = dt;
                comboBoxCustomer.DisplayMember = "DealerName";
                comboBoxCustomer.ValueMember = "DealerName"; // Use DealerName for filtering
                comboBoxCustomer.DropDownHeight = 200;
                comboBoxCustomer.IntegralHeight = false;
            }
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to load all dealer records
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        Did,
                        DealerName,
                        DDAmount,
                        DAmount,
                        Date
                    FROM 
                        AddDealer
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource banayen aur data source ko set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerRDLC.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

  

        private void btnSearch_Click_1(object sender, EventArgs e)
        {
            // Get the selected dealer name from ComboBox
            string selectedCustomer = comboBoxCustomer.SelectedValue?.ToString();

            // If no dealer selected, return
            if (string.IsNullOrEmpty(selectedCustomer))
            {
                MessageBox.Show("Please select a dealer.");
                return;
            }

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to filter by selected dealer name
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        Did,
                        DealerName,
                        DDAmount,
                        DAmount,
                        Date
                    FROM 
                        AddDealer
                    WHERE 
                        TRIM(DealerName) = @SearchText COLLATE NOCASE" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
", con);

                // Add parameter for dealer name filtering
                ReportSearchHelper.BindSearch(cmd, selectedCustomer);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource banayen aur data source ko set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerRDLC.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
