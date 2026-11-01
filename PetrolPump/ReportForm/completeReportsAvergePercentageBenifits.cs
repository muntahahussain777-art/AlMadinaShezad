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
    public partial class completeReportsAvergePercentageBenifits: Sample
    {
        public completeReportsAvergePercentageBenifits()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // Best report query
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        -- PetrolAdd ka data
                        COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolRate,
                        COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolLitter,
                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolIncome,
                        COALESCE((SELECT AVG(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS AvgPetrolRate,

                        -- StockDiesel ka data
                        COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselRate,
                        COALESCE((SELECT SUM(Litter) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselLitter,
                        COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselCost,
                        COALESCE((SELECT AVG(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS AvgDieselRate,

                        -- Expenses ka data
                        COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE date(EDate) >= date(@FromDate) AND date(EDate) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalExpenseAmount,

                        -- Calculations
                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                        COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS GrossProfit,
                        (
                            COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                            COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                            COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE date(EDate) >= date(@FromDate) AND date(EDate) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS NetProfit,

                        -- Extra insights
                        COALESCE((SELECT COUNT(DISTINCT Date) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS PetrolDays,
                        COALESCE((SELECT COUNT(DISTINCT Date) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS DieselDays,
                        (
                            CASE 
                                WHEN COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) = 0 
                                THEN 0 
                                ELSE 
                                    (
                                        (
                                            COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                                            COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                                            COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE date(EDate) >= date(@FromDate) AND date(EDate) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                        ) * 100.0 / 
                                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                    )
                            END
                        ) AS ProfitMarginPercentage
                    ", con);

                // Parameters set karo
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein koi data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Report setup
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BenifitsCompleteAverageBenfits.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void completeReportsAvergePercentageBenifits_Load(object sender, EventArgs e)
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
                con.Open();

                // Saara data load karne ke liye query
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        -- PetrolAdd ka data
                        COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolRate,
                        COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolLitter,
                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolIncome,
                        COALESCE((SELECT AVG(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS AvgPetrolRate,

                        -- StockDiesel ka data
                        COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselRate,
                        COALESCE((SELECT SUM(Litter) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselLitter,
                        COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselCost,
                        COALESCE((SELECT AVG(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS AvgDieselRate,

                        -- Expenses ka data
                        COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalExpenseAmount,

                        -- Calculations
                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                        COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS GrossProfit,
                        (
                            COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                            COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                            COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS NetProfit,

                        -- Extra insights
                        COALESCE((SELECT COUNT(DISTINCT Date) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS PetrolDays,
                        COALESCE((SELECT COUNT(DISTINCT Date) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS DieselDays,
                        (
                            CASE 
                                WHEN COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) = 0 
                                THEN 0 
                                ELSE 
                                    (
                                        (
                                            COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                                            COALESCE((SELECT SUM(Rate * Litter) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) -
                                            COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                        ) * 100.0 / 
                                        COALESCE((SELECT SUM(Rate * Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                    )
                            END
                        ) AS ProfitMarginPercentage
                    ", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BenifitsCompleteAverageBenfits.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
