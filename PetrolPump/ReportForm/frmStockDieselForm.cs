using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmStockDieselForm : Sample
    {
        public frmStockDieselForm()
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
                        StockDiesel.SID AS SID, 
                        StockDiesel.SDid,
                        StockDiesel.Date,
                        AddDealer.DealerName, 
                        StockDiesel.Vehicle,
                        StockDiesel.Rate,
                        StockDiesel.Litter,
                        StockDiesel.Credit,
                        StockDiesel.Debit,
                        StockDiesel.Note
                    FROM StockDiesel
                    LEFT JOIN AddDealer ON StockDiesel.SDid = AddDealer.Did 
                    WHERE (@SearchText = '' OR IFNULL(AddDealer.DealerName,'') LIKE @SearchLike COLLATE NOCASE OR IFNULL(StockDiesel.Vehicle,'') LIKE @SearchLike COLLATE NOCASE)
                    AND date(StockDiesel.Date) >= date(@FromDate) AND date(StockDiesel.Date) <= date(@ToDate)
                    " + ReportOfficeHelper.Sql(this, "StockDiesel.OfficeName") + @"
                    ORDER BY AddDealer.DealerName ASC
                ", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                BindStockDieselReport(dt, fromdate.Value.Date, todate.Value.Date);
            }
        }

        private void frmStockDieselForm_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand(@"
                                       SELECT 
                        StockDiesel.SID AS SID, 
                        StockDiesel.SDid,
                        StockDiesel.Date,
                        AddDealer.DealerName, 
                        StockDiesel.Vehicle,
                        StockDiesel.Rate,
                        StockDiesel.Litter,
                        StockDiesel.Credit,
                        StockDiesel.Debit,
                        StockDiesel.Note
                    FROM StockDiesel
                    LEFT JOIN AddDealer ON StockDiesel.SDid = AddDealer.Did
                    WHERE 1=1" + ReportOfficeHelper.Sql(this, "StockDiesel.OfficeName") + @"
", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                BindStockDieselReport(dt);
            }
        }
    }
}
