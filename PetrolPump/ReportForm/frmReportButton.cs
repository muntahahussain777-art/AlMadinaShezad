using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmReportButton : Sample
    {
        public frmReportButton()
        {
            InitializeComponent();
        }

        public string ReportName { get; set; }
        public DataTable ReportData { get; set; }

        private void frmReportButton_Load(object sender, EventArgs e)
        {
            PopulateCustomerComboBox();
            LoadAllRecords();
        }

        private void PopulateCustomerComboBox()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT DISTINCT AddCustomer.Name
                    FROM AddCustomer
                    INNER JOIN PetrolAdd ON AddCustomer.Id = PetrolAdd.CustomerId
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "AddCustomer.OfficeName") + @"
                    AND 1=1" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                comboBoxCustomer.DataSource = dt;
                comboBoxCustomer.DisplayMember = "Name";
                comboBoxCustomer.ValueMember = "Name";
                comboBoxCustomer.DropDownHeight = 200;
                comboBoxCustomer.IntegralHeight = false;
            }
        }

        private void LoadAllRecords()
        {
            BindDieselReport(DieselAddPetrolReportHelper.SelectSql + @"
WHERE 1=1" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
ORDER BY 
    AddCustomer.Name COLLATE NOCASE ASC,
    PetrolAdd.Date ASC,
    PetrolAdd.pid ASC", null);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string selectedCustomer = comboBoxCustomer.SelectedValue?.ToString();

            if (string.IsNullOrEmpty(selectedCustomer))
            {
                MessageBox.Show("Please select a customer.");
                return;
            }

            string sql = DieselAddPetrolReportHelper.SelectSql + @"
WHERE 
    TRIM(AddCustomer.Name) = @SearchText COLLATE NOCASE
" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
ORDER BY 
    PetrolAdd.Date ASC,
    PetrolAdd.pid ASC";

            BindDieselReport(sql, selectedCustomer);
        }

        private void BindDieselReport(string sql, string customerFilter)
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(sql, con);
                if (!string.IsNullOrEmpty(customerFilter))
                    ReportSearchHelper.BindSearch(cmd, customerFilter);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);
                DieselAddPetrolReportHelper.ApplyRunningBalance(dt);

                string templatePath = Path.Combine(Application.StartupPath, "Reports", "AddPetrol.rdlc");
                string reportPath = DieselAddPetrolReportHelper.BuildAutoSizedReport(templatePath, dt);
                string customerName = DieselAddPetrolReportHelper.ResolveCustomerDisplayName(dt, customerFilter);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.LocalReport.SetParameters(new ReportParameter("CustomerName", customerName ?? ""));
                ReportHeaderBrandHelper.ApplyBrandToViewer(reportViewer1, ReportHeaderBrandHelper.CurrentCompany);
                try
                {
                    reportViewer1.LocalReport.SetParameters(new ReportParameter("CustomerName", customerName ?? ""));
                }
                catch { }
                ReportHeaderBrandHelper.SafeRefreshReport(reportViewer1);
            }
        }
    }
}
