using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite; // SQL Server کی جگہ SQLite استعمال کریں
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmDealerReport : Sample
    {
        public frmDealerReport()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search text from input

            // SQLite کے لیے کنکشن اسٹرنگ حاصل کریں
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // SQLite کے لیے SQL کمانڈ بنائیں
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
                       ((TRIM(DA.DealerName) = @SearchText COLLATE NOCASE OR @SearchText = '') OR DA.DealerName IS NULL)
                   AND 
                       date(DL.Date) >= date(@FromDate) AND date(DL.Date) <= date(@ToDate)
                   " + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                   ORDER BY 
                       DL.Date ASC", con);

                // پیرامیٹرز کو سیٹ کریں
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                // DataTable میں ڈیٹا لوڈ کریں
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // اگر کوئی ڈیٹا نہ ملے تو میسج دکھائیں
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("No data found for the selected criteria.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ReportDataSource کو سیٹ کریں
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);

                // Report file کا راستہ سیٹ کریں
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerReport1.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Report کے data sources کو clear کر کے نئے data کو set کریں
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                // Report refresh کریں
                reportViewer1.RefreshReport();
            }
        }

        private void frmDealerReport_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date set کریں
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }
        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                // SQL query to load all dealer records
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
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource banayen aur data source ko set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerReport1.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

    }
}
