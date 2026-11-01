using Microsoft.Reporting.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.ReportForm;
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

namespace HajiBalochSoftwere.ReportForm
{
    public partial class DealerCompletePackageForm : Sample
    {
        public DealerCompletePackageForm()
        {
            InitializeComponent();
        }

        private void DealerCompletePackageForm_Load(object sender, EventArgs e)
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
                SQLiteCommand cmd = new SQLiteCommand(@"
               SELECT
                d.Did,
                d.DealerName,

                -- 🔹 Master totals (AddDealer se)
                IFNULL(d.DDAmount, 0)  AS DDAmount_Master,
                IFNULL(d.DAmount, 0)   AS DAmount_Master,
                (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS Balance_Master,

                -- 🔹 Stock detail
                s.Date                 AS StockDate,
                s.Vehicle              AS VehicleNo,
                IFNULL(s.AddDisel, 0)  AS Litter,
                IFNULL(s.Rate, 0)      AS Rate,

                -- 🔹 Litter * Rate = Amount
                (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Amount

            FROM AddDealer d
            LEFT JOIN AddStock s 
                ON s.DealerId = d.Did
            WHERE 1=1" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

            ORDER BY 
                d.DealerName,
                s.Date,
                s.Sid;", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerCompleteReportForm.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = txtSearch.Text;

                // 🔹 Prevent wrong date range
                if (fromdate.Value.Date > todate.Value.Date)
                {
                    MessageBox.Show("To Date, From Date se chhoti nahi ho sakti.",
                                    "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    SQLiteCommand cmd = new SQLiteCommand(@"
        SELECT
    d.Did,
    d.DealerName,

    IFNULL(d.DDAmount, 0)  AS DDAmount_Master,
    IFNULL(d.DAmount, 0)   AS DAmount_Master,
    (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS Balance_Master,

    s.Date                 AS StockDate,
    s.Vehicle              AS VehicleNo,
    IFNULL(s.AddDisel, 0)  AS Litter,
    IFNULL(s.Rate, 0)      AS Rate,

    (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Amount

FROM AddDealer d
LEFT JOIN AddStock s 
    ON s.DealerId = d.Did

WHERE 
    (
        @SearchText = ''
        OR TRIM(d.DealerName) = @SearchText COLLATE NOCASE
        OR IFNULL(s.Vehicle, '') LIKE @SearchLike COLLATE NOCASE
    )
    AND date(s.Date) >= date(@FromDate) AND date(s.Date) <= date(@ToDate)
" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

ORDER BY 
    d.DealerName,
    s.Date,
    s.Sid;", con);

                    ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                    ReportSearchHelper.BindSearch(cmd, searchText);

                    SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    sd.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Koi data nahi mila.",
                                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                    string reportPath = Path.Combine(Application.StartupPath, "Reports", "DealerCompleteReportForm.rdlc");
                    reportViewer1.LocalReport.ReportPath = reportPath;

                    reportViewer1.LocalReport.DataSources.Clear();
                    reportViewer1.LocalReport.DataSources.Add(rds);
                    reportViewer1.RefreshReport();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report load nahi hui: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}