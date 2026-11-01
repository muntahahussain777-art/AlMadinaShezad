using Microsoft.Reporting.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.ReportForm;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HajiBalochSoftwere.ReportForm
{
    public partial class StockReportFormPurchaseSaleForm : Sample
    {
        private bool _hooksAttached;

        public StockReportFormPurchaseSaleForm()
        {
            InitializeComponent();
        }

        private void StockReportFormPurchaseSaleForm_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
            AttachDateHooks();
            LoadBySelectedDates(showEmptyMessage: false);
        }

        private void AttachDateHooks()
        {
            if (_hooksAttached) return;
            _hooksAttached = true;

            btnSearch.Click -= btnSearch_Click;
            btnSearch.Click += btnSearch_Click;
            fromdate.ValueChanged -= DatePickers_ValueChanged;
            todate.ValueChanged -= DatePickers_ValueChanged;
            fromdate.ValueChanged += DatePickers_ValueChanged;
            todate.ValueChanged += DatePickers_ValueChanged;
        }

        private void DatePickers_ValueChanged(object sender, EventArgs e)
        {
            LoadBySelectedDates(showEmptyMessage: false);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            LoadBySelectedDates(showEmptyMessage: true);
        }

        private void LoadBySelectedDates(bool showEmptyMessage)
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                SQLiteCommand cmd = new SQLiteCommand(@"
SELECT
    IFNULL(p.Total_Liter_Purchase, 0) AS Total_Liter_Purchase,
    IFNULL(s.Total_Liter_Sale, 0)     AS Total_Liter_Sale,

    (IFNULL(p.Total_Liter_Purchase, 0) - IFNULL(s.Total_Liter_Sale, 0)) AS Current_Stock_Liter,

    IFNULL(p.Purchase_LR_Amount, 0)   AS Purchase_LiterRate_Amount,
    IFNULL(s.Sale_LR_Amount, 0)       AS Sale_LiterRate_Amount,

    (IFNULL(p.Purchase_LR_Amount, 0) - IFNULL(s.Sale_LR_Amount, 0))     AS Net_LiterRate_Amount,

    IFNULL(d.Total_DDAmount, 0)       AS Purchase_Direct_Amount,

    IFNULL(s.Total_Amount_Advance, 0) AS Sale_Direct_Amount,

    (IFNULL(d.Total_DDAmount, 0) - IFNULL(s.Total_Amount_Advance, 0))   AS Net_Direct_Amount

FROM
(
    SELECT
        SUM(AddDisel)        AS Total_Liter_Purchase,
        SUM(AddDisel * Rate) AS Purchase_LR_Amount
    FROM AddStock WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
) AS p
CROSS JOIN
(
    SELECT
        SUM(Litter)          AS Total_Liter_Sale,
        SUM(Litter * Rate)   AS Sale_LR_Amount,
        SUM(IFNULL(Amount, 0) + IFNULL(Advance, 0)) AS Total_Amount_Advance
    FROM PetrolAdd
    WHERE date(Date) >= date(@FromDate) AND date(Date) <= date(@ToDate)" + ReportOfficeHelper.Sql(this, "OfficeName") + @"
) AS s
CROSS JOIN
( SELECT SUM(DDAmount) AS Total_DDAmount FROM AddDealer WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @") AS d;
", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                if (showEmptyMessage && (dt.Rows.Count == 0 || IsAllZero(dt)))
                {
                    MessageBox.Show("Is date range ke liye koi data nahi mila.",
                                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "StockReportForSaleLitterPurchase.rdlc");

                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private static bool IsAllZero(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return true;
            DataRow row = dt.Rows[0];
            foreach (DataColumn col in dt.Columns)
            {
                object v = row[col];
                if (v == null || v == DBNull.Value) continue;
                if (decimal.TryParse(v.ToString(), out decimal n) && n != 0m)
                    return false;
            }
            return true;
        }
    }
}
