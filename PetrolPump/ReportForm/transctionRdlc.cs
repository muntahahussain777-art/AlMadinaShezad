using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite; // SQL Server کی جگہ SQLite استعمال کریں
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class transctionRdlc : Sample
    {
        public transctionRdlc()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search text from input

            // کنکشن اسٹرنگ حاصل کریں
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();
                // SQL query جو customer name اور dealer name کو filter کرے گی اور date کے حساب سے بھی filter کرے گی
                SQLiteCommand cmd = new SQLiteCommand(@"
                  SELECT 
                      DL.LedgerID,
                      DL.Date,
                      AC.Name,
                      DA.DealerName,
                      DL.AmounGiven,
                      DL.Balance,
                      DL.Note
                  FROM 
                      DieselLedger DL
                  LEFT JOIN 
                      AddCustomer AC ON DL.id = AC.id
                  LEFT JOIN 
                      AddDealer DA ON DL.Did = DA.Did
                  WHERE 
                      ((TRIM(AC.Name) = @SearchText COLLATE NOCASE OR TRIM(DA.DealerName) = @SearchText COLLATE NOCASE) OR @SearchText = '')
                  AND 
                      date(DL.Date) >= date(@FromDate) AND date(DL.Date) <= date(@ToDate)
                  " + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                  ORDER BY 
                      DL.LedgerID ASC", con);

                // پیرامیٹرز کو set کریں
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                // DataTable میں ڈیٹا load کریں
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource کو set کریں
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);

                // Report file کا راستہ set کریں
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "ReportTransfer.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Purane data sources کو clear کریں اور نئے data کو add کریں
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                // Report refresh کریں
                reportViewer1.RefreshReport();
            }
        }

        private void transctionRdlc_Load(object sender, EventArgs e)
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
                      DL.LedgerID,
                      DL.Date,
                      AC.Name,
                      DA.DealerName,
                      DL.AmounGiven,
                      DL.Balance,
                      DL.Note
                  FROM 
                      DieselLedger DL
                  LEFT JOIN 
                      AddCustomer AC ON DL.id = AC.id
                  LEFT JOIN 
                      AddDealer DA ON DL.Did = DA.Did
                  WHERE 1=1" + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // ReportDataSource banayen aur data source ko set karein
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
