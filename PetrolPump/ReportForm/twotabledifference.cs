using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class twotabledifference : Sample
    {
        private DataTable _lastRawData;
        private TwoTableReportTempEntryController _tempEntries;

        public twotabledifference()
        {
            InitializeComponent();
            _tempEntries = new TwoTableReportTempEntryController(this, reportViewer1, RefreshReportFromCache);
        }

        private void twotabledifference_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
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

                string query = @"
SELECT 
    NULL AS CustomerName, 
    NULL AS CustomerRate, 
    DealerName AS DealerName, 
    DDAmount AS DealerRate
FROM 
    AddDealer
WHERE (@SearchText = '' OR TRIM(DealerName) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"

UNION ALL

SELECT 
    AddCustomer.Name AS CustomerName, 
    SUM(
        CASE 
            WHEN PetrolAdd.IsInitialEntry = 1 THEN PetrolAdd.Balance 
            ELSE 0 
        END
    ) AS CustomerRate,
    NULL AS DealerName, 
    NULL AS DealerRate
FROM 
    PetrolAdd
    INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
WHERE (@SearchText = '' OR TRIM(AddCustomer.Name) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
GROUP BY 
    AddCustomer.Id
";

                SQLiteCommand cmd = new SQLiteCommand(query, con);
                ReportSearchHelper.BindSearch(cmd, searchText);
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dtCombined = new DataTable();
                sd.Fill(dtCombined);

                BindCombinedReport(dtCombined);
                con.Close();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = ReportSearchHelper.Trim(txtSearch.Text);
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                string filteredQuery = @"
SELECT 
    NULL AS CustomerName, 
    NULL AS CustomerRate, 
    DealerName AS DealerName, 
    DDAmount AS DealerRate
FROM 
    AddDealer
WHERE 
    date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)
    AND (@SearchText = '' OR TRIM(DealerName) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"

UNION ALL

SELECT 
    AddCustomer.Name AS CustomerName, 
    SUM(
        CASE 
            WHEN PetrolAdd.IsInitialEntry = 1 THEN PetrolAdd.Balance 
            ELSE 0 
        END
    ) AS CustomerRate,
    NULL AS DealerName, 
    NULL AS DealerRate
FROM 
    PetrolAdd
    INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
WHERE 
    date(PetrolAdd.Date) >= date(@FromDate) AND date(PetrolAdd.Date) <= date(@ToDate)
    AND (@SearchText = '' OR TRIM(AddCustomer.Name) = @SearchText COLLATE NOCASE)" + ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName") + @"
GROUP BY 
    AddCustomer.Id
";

                SQLiteCommand cmd = new SQLiteCommand(filteredQuery, con);
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dtCombined = new DataTable();
                sd.Fill(dtCombined);

                if (dtCombined.Rows.Count == 0 && _tempEntries.Count == 0)
                {
                    MessageBox.Show("Koi data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                BindCombinedReport(dtCombined);
                con.Close();
            }
        }

        private void BindCombinedReport(DataTable dtCombined)
        {
            _lastRawData = dtCombined == null ? null : dtCombined.Copy();
            RenderCombinedReport();
        }

        private void RefreshReportFromCache()
        {
            RenderCombinedReport();
        }

        private void RenderCombinedReport()
        {
            DataTable merged = TwoTableReceivablePayableReportHelper.MergeTempEntriesIntoCombined(
                _lastRawData,
                _tempEntries.Entries);
            DataTable reportData = TwoTableReceivablePayableReportHelper.PrepareCombinedReportData(merged);

            ReportDataSource rds = new ReportDataSource("DataSet1", reportData);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "2tabledifference.rdlc");
            reportViewer1.LocalReport.ReportPath = reportPath;
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(rds);
            reportViewer1.RefreshReport();
        }
    }
}
