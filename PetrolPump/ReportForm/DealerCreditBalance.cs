using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class DealerCreditBalance : Sample
    {
        public DealerCreditBalance()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text;
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                SQLiteCommand cmd = new SQLiteCommand(@"
            SELECT 
                Did,
                DealerName,
                SUM(DDAmount) AS TotalDDAmount,
                SUM(DAmount) AS TotalDAmount,
                (SUM(DDAmount) - SUM(DAmount)) AS DealerProfitOrLoss
            FROM 
                AddDealer
            WHERE 
                (TRIM(DealerName) = @SearchText COLLATE NOCASE OR @SearchText = '')
                AND date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)
            " + ReportOfficeHelper.Sql(this, "OfficeName") + @"
            GROUP BY 
                Did, DealerName", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Koi data nahi mila is date range ya search ke liye.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerBalanceCredit.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void DealerCreditBalance_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand(@"
               SELECT 
    Did,
    DealerName,
    SUM(DDAmount) AS TotalDDAmount,
    SUM(DAmount) AS TotalDAmount,
    (SUM(DDAmount) - SUM(DAmount)) AS DealerProfitOrLoss
FROM AddDealer
WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
GROUP BY Did, DealerName; ", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerBalanceCredit.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
