using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class withoutReportDateDealer : Sample
    {
        public withoutReportDateDealer()
        {
            InitializeComponent();
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
                DA.DealerName,
                DL.Date,
                DL.Vehicle,     
                DL.Rate,        
                DL.AddDisel     
            FROM 
                AddStock DL
            LEFT JOIN 
                AddDealer DA ON DL.DealerId = DA.Did  
            WHERE 
                TRIM(DA.DealerName) = @SearchText COLLATE NOCASE
            " + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
            ORDER BY 
                DL.Date ASC", con); // Removed date condition

                // Set search parameter
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

        private void withoutReportDateDealer_Load(object sender, EventArgs e)
        {
            PopulateCustomerComboBox();
            LoadAllRecords(); // Load all records on form load
        }

        private void PopulateCustomerComboBox()
        {
            string connectionString = projectconnection.conReturn();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // SQL query to get distinct dealer names only present in DieselLedger
                SqlCommand cmd = new SqlCommand(@"
                    SELECT DISTINCT DA.DealerName
                    FROM AddDealer DA
                    INNER JOIN DieselLedger DL ON DA.Did = DL.Did", con);

                SqlDataAdapter sd = new SqlDataAdapter(cmd);
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
                       DA.DealerName,
                      DL.Date,
                      DL.Vehicle,     
                      DL.Rate,        
                      DL.AddDisel     
                  FROM 
                      AddStock DL
                  LEFT JOIN 
                      AddDealer DA ON DL.DealerId = DA.Did
                  WHERE 1=1" + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                  ORDER BY 
                      DL.DealerId ASC", con);

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
