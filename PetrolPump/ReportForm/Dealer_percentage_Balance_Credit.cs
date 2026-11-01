using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class Dealer_percentage_Balance_Credit : Sample
    {
        public Dealer_percentage_Balance_Credit()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search text from input

            // SQLite ke liye connection string hasil karen
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // SQLite ke liye SQL command banayein
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        Did,
                        DealerName,
                        SUM(DDAmount) AS TotalDDAmount,
                        SUM(DAmount) AS TotalDAmount,
                        (SUM(DDAmount) - SUM(DAmount)) AS DealerProfitOrLoss,
                        CASE 
                            WHEN SUM(DDAmount) = 0 THEN 0
                            ELSE ROUND(((SUM(DDAmount) - SUM(DAmount)) / SUM(DDAmount)) * 100, 2)
                        END AS ProfitOrLossPercentage
                    FROM 
                        AddDealer
                    WHERE 
                        (TRIM(DealerName) = @SearchText COLLATE NOCASE OR @SearchText = '')
                        AND date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)
                    " + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                    GROUP BY 
                        Did", con);

                // Parameters ko set karen
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                // DataTable mein data load karen
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Agar koi data na mile to message dikhayein
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Koi data nahi mila is date range ya search ke liye.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ReportDataSource ko set karen
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);

                // Report file ka rasta set karen
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerProfit_LossReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Report ke data sources ko clear kar ke naya data set karen
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                // Report refresh karen
                reportViewer1.RefreshReport();
            }
        }

        private void Dealer_percentage_Balance_Credit_Load(object sender, EventArgs e)
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
                        Did,
                        DealerName,
                        SUM(DDAmount) AS TotalDDAmount,
                        SUM(DAmount) AS TotalDAmount,
                        (SUM(DDAmount) - SUM(DAmount)) AS DealerProfitOrLoss,
                        CASE 
                            WHEN SUM(DDAmount) = 0 THEN 0
                            ELSE ROUND(((SUM(DDAmount) - SUM(DAmount)) / SUM(DDAmount)) * 100, 2)
                        END AS ProfitOrLossPercentage
                    FROM 
                        AddDealer
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
                    GROUP BY 
                        Did", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource banayen aur data source ko set karein
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerProfit_LossReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}