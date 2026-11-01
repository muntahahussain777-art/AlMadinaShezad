using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection; // Yeh assume karta hai ke aap ka connection class SQLite support karta hai
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite; // SQLite ke liye package
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class TwoDateReport : Sample
    {
        public TwoDateReport()
        {
            InitializeComponent();
        }

        private void TwoDateReport_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date
            todate.Value = DateTime.Now;    // Default date
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
                PetrolAdd.pid, 
                PetrolAdd.Date, 
                PetrolAdd.ReceiptNo, 
                PetrolAdd.vehicle, 
                PetrolAdd.Litter, 
                PetrolAdd.Rate, 
                PetrolAdd.Advance, 

                -- 🔹 Amount: direct entry ho to PetrolAdd.Amount use karo
                CAST(
                    CASE 
                        WHEN IFNULL(PetrolAdd.Litter, 0) = 0 
                             AND IFNULL(PetrolAdd.Rate, 0) = 0
                        THEN IFNULL(PetrolAdd.Amount, 0)
                        ELSE (PetrolAdd.Litter * PetrolAdd.Rate + IFNULL(PetrolAdd.Advance, 0))
                    END 
                AS REAL) AS Amount, 

                PetrolAdd.Credit, 

                -- 🔹 Balance: direct entry ho to PetrolAdd.Balance use karo
                CAST(
                    CASE 
                        WHEN IFNULL(PetrolAdd.Litter, 0) = 0 
                             AND IFNULL(PetrolAdd.Rate, 0) = 0
                        THEN IFNULL(PetrolAdd.Balance, 0)
                        ELSE ((PetrolAdd.Litter * PetrolAdd.Rate + IFNULL(PetrolAdd.Advance, 0)) 
                              - IFNULL(PetrolAdd.Credit, 0))
                    END
                AS REAL) AS Balance, 

                PetrolAdd.Note, 
                AddCustomer.Name 
            FROM 
                PetrolAdd 
            INNER JOIN 
                AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
            WHERE 1=1" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
            ", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "AttendanceViewReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text;

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand(@"
            SELECT 
                PetrolAdd.pid, 
                PetrolAdd.Date, 
                PetrolAdd.ReceiptNo, 
                PetrolAdd.vehicle, 
                PetrolAdd.Litter, 
                PetrolAdd.Rate, 
                PetrolAdd.Advance, 

                CAST(
                    CASE 
                        WHEN IFNULL(PetrolAdd.Litter, 0) = 0 
                             AND IFNULL(PetrolAdd.Rate, 0) = 0
                        THEN IFNULL(PetrolAdd.Amount, 0)
                        ELSE (PetrolAdd.Litter * PetrolAdd.Rate + IFNULL(PetrolAdd.Advance, 0))
                    END 
                AS REAL) AS Amount, 

                PetrolAdd.Credit, 

                CAST(
                    CASE 
                        WHEN IFNULL(PetrolAdd.Litter, 0) = 0 
                             AND IFNULL(PetrolAdd.Rate, 0) = 0
                        THEN IFNULL(PetrolAdd.Balance, 0)
                        ELSE ((PetrolAdd.Litter * PetrolAdd.Rate + IFNULL(PetrolAdd.Advance, 0)) 
                              - IFNULL(PetrolAdd.Credit, 0))
                    END
                AS REAL) AS Balance, 

                PetrolAdd.Note, 
                AddCustomer.Name 
            FROM 
                PetrolAdd 
            INNER JOIN 
                AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id 
            WHERE 
          (@SearchText = '' OR TRIM(AddCustomer.Name) = @SearchText COLLATE NOCASE OR IFNULL(PetrolAdd.vehicle,'') LIKE @SearchLike COLLATE NOCASE)
            AND 
                date(PetrolAdd.Date) >= date(@FromDate) AND date(PetrolAdd.Date) <= date(@ToDate)
            " + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
            ORDER BY 
                PetrolAdd.pid ASC", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "AttendanceViewReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}