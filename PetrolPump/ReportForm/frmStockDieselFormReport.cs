using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmStockDieselForm
    {
        private const string StockDetailReportFile = "StockDieselDetailRDLC.rdlc";

        private void BindStockDieselReport(DataTable dt, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                StockDieselReportHelper.Summary summary = StockDieselReportHelper.EnrichReportTable(dt);

                string reportPath = Path.Combine(Application.StartupPath, "Reports", StockDetailReportFile);
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found: " + reportPath, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string dateRange = fromDate.HasValue && toDate.HasValue
                    ? $"{fromDate.Value:dd-MMM-yyyy}   to   {toDate.Value:dd-MMM-yyyy}"
                    : string.Empty;

                DataTable summaryTable = StockDieselReportHelper.BuildSummaryTable(summary, dateRange);

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("SummarySet", summaryTable));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report load karte waqt error: " + ex.Message, "Report Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
