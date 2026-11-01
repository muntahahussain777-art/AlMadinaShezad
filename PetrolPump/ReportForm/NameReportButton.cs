using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite; // SQL Server کی جگہ SQLite استعمال کریں
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class NameReportButton : Sample
    {
        public NameReportButton()
        {
            InitializeComponent();
        }

        private void NameReportButton_Load(object sender, EventArgs e)
        {
            PopulateCustomerComboBox();
            LoadAllRecords(); // فارم لوڈ ہوتے ہی تمام records لوڈ کریں
        }

        private void PopulateCustomerComboBox()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT DISTINCT Name FROM AddCustomer WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                comboBoxCustomer.DataSource = dt;
                comboBoxCustomer.DisplayMember = "Name";
                comboBoxCustomer.ValueMember = "Name"; // filtering کے لیے Name استعمال کریں
                comboBoxCustomer.DropDownHeight = 200;
                comboBoxCustomer.IntegralHeight = false;
            }
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        AC.id,
                        AC.Name,
                        AC.Mobile,
                        AC.Date
                    FROM 
                        AddCustomer AC
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "AC.OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerRDLC.rdlc");
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
                MessageBox.Show("Please select a customer.");
                return;
            }

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        AC.id,
                        AC.Name,
                        AC.Mobile,
                        AC.Date
                    FROM 
                        AddCustomer AC
                    WHERE 
                        TRIM(AC.Name) = @SearchText COLLATE NOCASE" + ReportOfficeHelper.Sql(this, "AC.OfficeName") + @"
", con);

                ReportSearchHelper.BindSearch(cmd, selectedCustomer);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerRDLC.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
