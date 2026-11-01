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
    public partial class BankFormCustomerDealer: Sample
    {
        public BankFormCustomerDealer()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text; // Search ka text

            // SQLite connection string
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // Date filter ke saath query
                SQLiteCommand cmd = new SQLiteCommand(@"
                   SELECT
                BT.Id,
                CASE 
                    WHEN BT.DealerId IS NOT NULL THEN 'Dealer: ' || AD.DealerName
                    WHEN BT.CustomerId IS NOT NULL THEN 'Customer: ' || AC.Name
                    ELSE ''
                END AS PartyName,
                BT.TransactionType,
                BT.BankName,
                BT.Amount,
                BT.Note,
                BT.TransactionDate
            FROM BankTransactions BT
            LEFT JOIN AddCustomer AC ON BT.CustomerId = AC.Id
            LEFT JOIN AddDealer   AD ON BT.DealerId   = AD.Did
            WHERE 
                date(BT.TransactionDate) >= date(@FromDate) AND date(BT.TransactionDate) <= date(@ToDate)
                AND (
                    @SearchText = '' OR TRIM(AD.DealerName) = @SearchText COLLATE NOCASE OR TRIM(AC.Name) = @SearchText COLLATE NOCASE
                )
            " + ReportOfficeHelper.Sql(this, "BT.OfficeName") + @"
            ORDER BY 
                BT.TransactionDate ASC", con);

                // Parameters set karo
                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                // Agar searchText ka use karna chahte ho kisi column mein (jaise Note), to yahan add kar sakte ho, warna hata do
                ReportSearchHelper.BindSearch(cmd, searchText);

                // DataTable mein data load karo
                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Agar data nahi mila to message
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein koi data nahi mila.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Report ke liye data source set karo
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BankReportsTransaction.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }

        private void BankFormCustomerDealer_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now; // Default date aaj ki
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords(); // Form load hone par saara data dikhao
        }

        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open(); // Connection kholo

                // Saara data load karne ke liye query (bina date filter ke)
                SQLiteCommand cmd = new SQLiteCommand(@"
                     SELECT
        BT.Id,
        CASE 
            WHEN BT.DealerId IS NOT NULL THEN 'Dealer: ' || AD.DealerName
            WHEN BT.CustomerId IS NOT NULL THEN 'Customer: ' || AC.Name
            ELSE ''
        END AS PartyName,
        BT.TransactionType,
        BT.BankName,
        BT.Amount,
        BT.Note,
        BT.TransactionDate
    FROM BankTransactions BT
    LEFT JOIN AddCustomer AC ON BT.CustomerId = AC.Id
    LEFT JOIN AddDealer   AD ON BT.DealerId   = AD.Did
    WHERE 1=1" + ReportOfficeHelper.Sql(this, "BT.OfficeName") + @"
    ORDER BY 
        BT.TransactionDate ASC", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                // Report ke liye data source
                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BankReportsTransaction.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
