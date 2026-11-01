using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;   // ReportDataSource ke liye
using System.IO;                     // Path.Combine ke liye
using System.Data.SQLite;
using AlMadinaShahzad.ProjectConnection;            // SQLite classes

namespace AlMadinaShahzad.ReportForm
{
    public partial class OtherStockLitter : Sample
    {
        public OtherStockLitter()
        {
            InitializeComponent();
        }

        private void OtherStockLitter_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date set
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // -----------------
                // Litter Difference Query (Bina Date Filter)
                // -----------------
                string litterQuery = @"
                    SELECT
                        (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @") AS StockDieselTotal,
                        (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")   AS PetrolAddTotal,
                        (
                            (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")
                            - (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")
                        ) AS LitterDifference
                ";

                SQLiteCommand litterCmd = new SQLiteCommand(litterQuery, con);
                SQLiteDataAdapter litterAdapter = new SQLiteDataAdapter(litterCmd);
                DataTable dtLiters = new DataTable();
                litterAdapter.Fill(dtLiters);

                // ReportDataSource for Liters
                ReportDataSource rdsLiters = new ReportDataSource("DataSet1", dtLiters);

                // RDLC ka path set karna
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "StockDifferenceReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Pehle purane DataSources ko clear karein
                reportViewer1.LocalReport.DataSources.Clear();

                // Naya DataSource add karein
                reportViewer1.LocalReport.DataSources.Add(rdsLiters);

                // Final report refresh
                reportViewer1.RefreshReport();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // 2) Litter difference query with date filter (StockDiesel and PetrolAdd)
                string litterQuery = @"
                    SELECT
                        (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel 
                         WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                        ) AS StockDieselTotal,
                        
                        (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd
                         WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                        ) AS PetrolAddTotal,

                        (
                            (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel
                             WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                            )
                            -
                            (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd
                             WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                            )
                        ) AS LitterDifference
                ";

                SQLiteCommand litterCmd = new SQLiteCommand(litterQuery, con);
                ReportDateRangeHelper.AddToCommand(litterCmd, fromdate, todate);

                SQLiteDataAdapter litterAdapter = new SQLiteDataAdapter(litterCmd);
                DataTable dtLiters = new DataTable();
                litterAdapter.Fill(dtLiters);

                if (dtLiters.Rows.Count == 0)
                {
                    MessageBox.Show("Koi data nahi mila.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ReportDataSource
                ReportDataSource rdsLiters = new ReportDataSource("DataSet1", dtLiters);

                // Set the path
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "StockDifferenceReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Clear old datasources and set new
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rdsLiters);

                // Refresh the report
                reportViewer1.RefreshReport();
            }
        }
    }
}
