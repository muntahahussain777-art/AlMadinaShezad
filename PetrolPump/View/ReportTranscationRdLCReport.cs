using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.ReportForm;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class ReportTranscationRdLCReport : Sample
    {
        public ReportTranscationRdLCReport()
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
                                LedgerID,
                                DL.Date,
                                AC.Name AS CustomerName,
                                DA.DealerName,
                                DL.AmounGiven,
                                Balance,
                                Note
                             FROM
                                DieselLedger DL
                             LEFT JOIN
                                AddCustomer AC ON DL.id = AC.id
                             LEFT JOIN
                                AddDealer DA ON DL.Did = DA.Did
                             WHERE
                                (AC.Name LIKE @searchText OR DA.DealerName LIKE @searchText)
                AND date(DL.Date) >= date(@FromDate) AND date(DL.Date) <= date(@ToDate)
                " + ReportOfficeHelper.Sql(this, "DL.OfficeName") + @"
                 ORDER BY DL.LedgerID ASC", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                cmd.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

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

        private void ReportTranscationRdLCReport_Load(object sender, EventArgs e)
        {
            ReportOfficeHelper.Attach(this);
            this.reportViewer1.RefreshReport();
        }
    }
}
