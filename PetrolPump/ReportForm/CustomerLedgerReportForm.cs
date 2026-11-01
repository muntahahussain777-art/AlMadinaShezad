using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class CustomerLedgerReportForm : Sample
    {
        public CustomerLedgerReportForm()
        {
            InitializeComponent();
        }

        private void CustomerLedgerReportForm_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
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
                                CL.CustomerId, 
                                C.Name, 
                                CL.Debit, 
                                CL.Credit, 
                                CL.Notes, 
                                CL.Date 
                            FROM 
                                CustomerLedger CL
                            INNER JOIN 
                                AddCustomer C ON CL.CustomerId = C.id
                            WHERE 
                                (@SearchText = '' OR TRIM(C.Name) = @SearchText COLLATE NOCASE)
                            AND 
                                date(CL.Date) >= date(@FromDate) AND date(CL.Date) <= date(@ToDate)
                            ORDER BY 
                                CL.CustomerId ASC
                            ", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerLedger.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
