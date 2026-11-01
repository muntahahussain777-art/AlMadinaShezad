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
    public partial class BenitsSellBuyFormReport : Sample
    {
        public BenitsSellBuyFormReport()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search ka text

            // SQLite connection string
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // Date filter ke saath query
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolRate,
                        COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselRate,
                        COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolLitter,
                        COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE date(EDate) >= date(@FromDate) AND date(EDate) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalExpenseAmount,
                        (
                            COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                            COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS RateDifference,
                        (
                            (
                                COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                                COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                            ) * COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS ProfitBeforeExpense,
                        (
                            (
                                (
                                    COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                                    COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                ) * COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                            ) - COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE date(EDate) >= date(@FromDate) AND date(EDate) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS FinalProfit;", con);

                // Parameters set karo
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                // Agar searchText ka use karna chahte ho kisi column mein (jaise Note), to yahan add kar sakte ho, warna hata do
                cmd.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                // DataTable mein data load karo
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Agar data nahi mila to message
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein koi data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Report ke liye data source set karo
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BenifitsDiesel.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void BenitsSellBuyFormReport_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now; // Default date aaj ki
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords(); // Form load hone par saara data dikhao
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open(); // Connection kholo

                // Saara data load karne ke liye query (bina date filter ke)
                SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolRate,
                        COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalDieselRate,
                        COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalPetrolLitter,
                        COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) AS TotalExpenseAmount,
                        (
                            COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                            COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS RateDifference,
                        (
                            (
                                COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                                COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                            ) * COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS ProfitBeforeExpense,
                        (
                            (
                                (
                                    COALESCE((SELECT SUM(Rate) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0) - 
                                    COALESCE((SELECT SUM(Rate) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                                ) * COALESCE((SELECT SUM(Litter) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                            ) - COALESCE((SELECT SUM(Amount) FROM Expensetable WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @"), 0)
                        ) AS FinalProfit;", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Report ke liye data source
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BenifitsDiesel.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}