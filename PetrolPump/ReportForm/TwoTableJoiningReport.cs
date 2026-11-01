using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class TwoTableJoiningReport : Sample
    {
        private DataTable _lastCustomerData;
        private DataTable _lastDealerData;
        private TwoTableReportTempEntryController _tempEntries;

        public TwoTableJoiningReport()
        {
            InitializeComponent();
            _tempEntries = new TwoTableReportTempEntryController(this, reportViewer1, RefreshReportFromCache);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = ReportSearchHelper.Trim(txtSearch.Text);
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                string dealerQuery = @"
            SELECT 
                DealerName,
                (DDAmount - DAmount) AS DealerRate
            FROM AddDealer
            WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)
              AND (@SearchText = '' OR TRIM(DealerName) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "OfficeName") + @";
        ";

                using (SQLiteCommand cmdDealer = new SQLiteCommand(dealerQuery, con))
                {
                    ReportDateRangeHelper.AddToCommand(cmdDealer, fromdate, todate);
                    ReportSearchHelper.BindSearch(cmdDealer, searchText);

                    SQLiteDataAdapter daDealer = new SQLiteDataAdapter(cmdDealer);
                    DataTable dtDealer = new DataTable();
                    daDealer.Fill(dtDealer);

                    // ---------- 2) CUSTOMER SIDE DATA (RECEIVABLE) ----------
                    string customerQuery = @"
                SELECT 
                    c.Name AS CustomerName,
                    (
                        SUM(CASE WHEN p.IsInitialEntry = 1 THEN p.Balance ELSE 0 END)
                        - 
                        SUM(CASE WHEN p.IsInitialEntry = 0 THEN p.Credit  ELSE 0 END)
                    ) AS CustomerRate
                FROM PetrolAdd p
                INNER JOIN AddCustomer c ON p.CustomerId = c.Id
                WHERE date(p.Date) >= date(@FromDate) AND date(p.Date) <= date(@ToDate)
                  AND (@SearchText = '' OR TRIM(c.Name) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
                GROUP BY c.Id;
            ";

                    using (SQLiteCommand cmdCustomer = new SQLiteCommand(customerQuery, con))
                    {
                        ReportDateRangeHelper.AddToCommand(cmdCustomer, fromdate, todate);
                        ReportSearchHelper.BindSearch(cmdCustomer, searchText);

                        SQLiteDataAdapter daCustomer = new SQLiteDataAdapter(cmdCustomer);
                        DataTable dtCustomer = new DataTable();
                        daCustomer.Fill(dtCustomer);

                        BindSeparateReport(dtCustomer, dtDealer);
                    }
                }

                con.Close();
            }
        }

        private void TwoTableJoiningReport_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date
            todate.Value = DateTime.Now;    // Default date
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            string searchText = ReportSearchHelper.Trim(txtSearch.Text);
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                string dealerQuery = @"
            SELECT 
                DealerName,
                (DDAmount - DAmount) AS DealerRate
            FROM AddDealer
            WHERE (@SearchText = '' OR TRIM(DealerName) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "OfficeName") + @";
        ";

                SQLiteCommand cmdDealer = new SQLiteCommand(dealerQuery, con);
                ReportSearchHelper.BindSearch(cmdDealer, searchText);
                SQLiteDataAdapter daDealer = new SQLiteDataAdapter(cmdDealer);
                DataTable dtDealer = new DataTable();
                daDealer.Fill(dtDealer);

                // ---------- 2) CUSTOMER SIDE (ALL) ----------
                string customerQuery = @"
            SELECT 
                c.Name AS CustomerName,
                (
                    SUM(CASE WHEN p.IsInitialEntry = 1 THEN p.Balance ELSE 0 END)
                    - 
                    SUM(CASE WHEN p.IsInitialEntry = 0 THEN p.Credit  ELSE 0 END)
                ) AS CustomerRate
            FROM PetrolAdd p
            INNER JOIN AddCustomer c ON p.CustomerId = c.Id
            WHERE (@SearchText = '' OR TRIM(c.Name) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
            GROUP BY c.Id;
        ";

                SQLiteCommand cmdCustomer = new SQLiteCommand(customerQuery, con);
                ReportSearchHelper.BindSearch(cmdCustomer, searchText);
                SQLiteDataAdapter daCustomer = new SQLiteDataAdapter(cmdCustomer);
                DataTable dtCustomer = new DataTable();
                daCustomer.Fill(dtCustomer);

                BindSeparateReport(dtCustomer, dtDealer);

                con.Close();
            }
        }

        private void BindSeparateReport(DataTable dtCustomer, DataTable dtDealer)
        {
            _lastCustomerData = dtCustomer == null ? null : dtCustomer.Copy();
            _lastDealerData = dtDealer == null ? null : dtDealer.Copy();
            RenderSeparateReport();
        }

        private void RefreshReportFromCache()
        {
            RenderSeparateReport();
        }

        private void RenderSeparateReport()
        {
            DataTable dtCustomer = _lastCustomerData == null ? new DataTable() : _lastCustomerData.Copy();
            DataTable dtDealer = _lastDealerData == null ? new DataTable() : _lastDealerData.Copy();

            if (dtCustomer.Columns.Count == 0)
            {
                dtCustomer.Columns.Add("CustomerName", typeof(string));
                dtCustomer.Columns.Add("CustomerRate", typeof(decimal));
            }
            if (dtDealer.Columns.Count == 0)
            {
                dtDealer.Columns.Add("DealerName", typeof(string));
                dtDealer.Columns.Add("DealerRate", typeof(decimal));
            }

            TwoTableReceivablePayableReportHelper.MergeTempEntriesIntoSeparate(
                dtCustomer,
                dtDealer,
                _tempEntries.Entries);

            DataTable receivable = TwoTableReceivablePayableReportHelper.PrepareReceivableSide(dtCustomer);
            DataTable payable = TwoTableReceivablePayableReportHelper.PreparePayableSide(dtDealer, dtCustomer);

            string reportPath = Path.Combine(Application.StartupPath, "Reports", "2tablejoin.rdlc");
            reportViewer1.LocalReport.ReportPath = reportPath;
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", receivable));
            reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet2", payable));
            reportViewer1.RefreshReport();
        }
    }
}