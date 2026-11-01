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
    public partial class stocklittertotaldiffernceform: Sample
    {
        public stocklittertotaldiffernceform()
        {
            InitializeComponent();
        }

        private void stocklittertotaldiffernceform_Load(object sender, EventArgs e)
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
    (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @") AS StockDieselTotal,
    (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")   AS PetrolAddTotal,
    (
        (SELECT IFNULL(SUM(Litter), 0) FROM StockDiesel WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")
        - 
        (SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @")
    ) AS LitterDifference;

                    ", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "litterdiffernce.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // Query: StockDiesel aur PetrolAdd ke litter sum + difference (DATE filter)
                string qry = @"
SELECT
    (SELECT IFNULL(SUM(Litter), 0) 
       FROM StockDiesel 
       WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
    ) AS StockDieselTotal,

    (SELECT IFNULL(SUM(Litter), 0) 
       FROM PetrolAdd 
       WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
    ) AS PetrolAddTotal,

    (
      (SELECT IFNULL(SUM(Litter), 0) 
         FROM StockDiesel 
         WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
      )
      - 
      (SELECT IFNULL(SUM(Litter), 0) 
         FROM PetrolAdd 
         WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
      )
    ) AS LitterDifference
";

                SQLiteCommand cmd = new SQLiteCommand(qry, con);

                // Parameters
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);

                // DataTable fill
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Theoretically subqueries -> dt.Rows.Count hamesha 1 hogi
                // lekin hum safe side pe check kar lete hain:
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein koi data nahi mila.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Report DataSource
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "litterdiffernce.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Clear old datasources, add new
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);

                // Refresh
                reportViewer1.RefreshReport();
            }
        }

    }
}
