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
    public partial class NewDealerAMountFormCreate : Sample
    {
        public NewDealerAMountFormCreate()
        {
            InitializeComponent();
        }

        private void NewDealerAMountFormCreate_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date set
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
    q.Did,
    q.DealerName,
    q.DDAmount_Master,
    q.DAmount_Master,
    q.Balance_Master,
    q.EntrySource,
    q.EntryDate,
    q.Vehicle,
    q.Litter,
    q.Rate,
    q.Amount,
    q.Debit,
    q.Credit,
    q.Note
FROM
(
    -- 1) SUMMARY ROW PER DEALER
    SELECT
        d.Did                AS Did,
        d.DealerName         AS DealerName,
        IFNULL(d.DDAmount, 0)  AS DDAmount_Master,
        IFNULL(d.DAmount, 0)   AS DAmount_Master,
        (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS Balance_Master,
        NULL    AS EntrySource,
        NULL    AS EntryDate,
        NULL    AS Vehicle,
        NULL    AS Litter,
        NULL    AS Rate,
        NULL    AS Amount,
        0       AS Debit,
        0       AS Credit,
        NULL    AS Note
    FROM AddDealer d WHERE 1=1" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

    UNION ALL

    -- 2) DETAIL ROWS (sari forms se aane wali entries)
    SELECT
        d.Did                AS Did,
        d.DealerName         AS DealerName,
        0       AS DDAmount_Master,
        0       AS DAmount_Master,
        0       AS Balance_Master,
        t.Source      AS EntrySource,
        t.TranDate    AS EntryDate,
        t.Vehicle,
        t.Litter,
        t.Rate,
        t.Amount,
        t.Debit,
        t.Credit,
        t.Note
    FROM AddDealer d
    LEFT JOIN
    (
        -- 🟢 1) STOCK ENTRIES  (frmStockView / AddStock)
        SELECT
            'DieselAddLitter'      AS Source,
            s.DealerId             AS Did,
            s.Date                 AS TranDate,
            s.Vehicle              AS Vehicle,
            IFNULL(s.AddDisel, 0)  AS Litter,
            IFNULL(s.Rate, 0)      AS Rate,
            (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Amount,
            (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Debit,
            0                      AS Credit,
            s.Note                 AS Note
        FROM AddStock s WHERE 1=1" + ReportOfficeHelper.Sql(this, "s.OfficeName") + @"

        UNION ALL

        -- 🟡 2) LEDGER CREDIT ENTRIES  (frmDieselLedgerView / DieselLedgerCredit)
        SELECT
            'LedgerCredit'         AS Source,
            c.Did                  AS Did,
            c.Date                 AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                      AS Amount,
            0                      AS Debit,
            IFNULL(c.AmounGiven, 0) AS Credit,
            c.Note                 AS Note
        FROM DieselLedgerCredit c WHERE 1=1" + ReportOfficeHelper.Sql(this, "c.OfficeName") + @"

        UNION ALL

        -- 🔴 3) DIRECT DEALER PAYMENT ENTRIES  (FrmDirectDealerPaymentAmountView / DieselLedgerDebit)
        SELECT
            'DirectDealerPayment'  AS Source,
            db.Did                 AS Did,
            db.Date                AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                         AS Amount,                 -- Amount 0
            IFNULL(db.AmounGiven, 0) AS Debit,                  -- Debit
            0                         AS Credit,                -- Credit 0
            db.Note                  AS Note
        FROM DieselLedgerDebit db WHERE 1=1" + ReportOfficeHelper.Sql(this, "db.OfficeName") + @"

        UNION ALL

        -- 🧷 4) DEALER-TO-DEALER PAYMENT SIDE (FirstDealer → DAmount side / Credit)
        SELECT
            'DealerToDealer_Pay'   AS Source,
            dtd.id                 AS Did,        -- FirstDealerId (jisse amount gaya)
            dtd.Date               AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                      AS Amount,     -- diesel amount nahi, isliye 0
            0                      AS Debit,
            IFNULL(dtd.AmounGiven, 0) AS Credit,  -- DAmount side
            dtd.Note               AS Note
        FROM DealertoDealer dtd WHERE 1=1" + ReportOfficeHelper.Sql(this, "dtd.OfficeName") + @"

        UNION ALL

        -- 🧷 5) DEALER-TO-DEALER RECEIVE SIDE (SecondDealer → DDAmount side / Debit)
        SELECT
            'DealerToDealer_Receive' AS Source,
            dtd.Did                  AS Did,       -- SecondDealerId (jisko amount mila)
            dtd.Date                 AS TranDate,
            NULL                     AS Vehicle,
            0                        AS Litter,
            0                        AS Rate,
            0                        AS Amount,    -- diesel amount nahi
            IFNULL(dtd.AmounGiven, 0) AS Debit,    -- DDAmount side
            0                        AS Credit,
            dtd.Note                 AS Note
        FROM DealertoDealer dtd WHERE 1=1" + ReportOfficeHelper.Sql(this, "dtd.OfficeName") + @"

    ) t ON t.Did = d.Did
    WHERE 1=1" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

) q

ORDER BY
    q.DealerName,
    CASE WHEN q.EntrySource IS NULL THEN 0 ELSE 1 END,
    q.EntryDate,
    q.EntrySource;", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "NewDealerAmountReport.rdlc");
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

                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    SQLiteCommand cmd = new SQLiteCommand(@"
SELECT
    q.Did,
    q.DealerName,
    q.DDAmount_Master,
    q.DAmount_Master,
    q.Balance_Master,
    q.EntrySource,
    q.EntryDate,
    q.Vehicle,
    q.Litter,
    q.Rate,
    q.Amount,
    q.Debit,
    q.Credit,
    q.Note
FROM
(
    -- 1) SUMMARY ROW PER DEALER
    SELECT
        d.Did                AS Did,
        d.DealerName         AS DealerName,
        IFNULL(d.DDAmount, 0)  AS DDAmount_Master,
        IFNULL(d.DAmount, 0)   AS DAmount_Master,
        (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS Balance_Master,
        NULL    AS EntrySource,
        NULL    AS EntryDate,
        NULL    AS Vehicle,
        NULL    AS Litter,
        NULL    AS Rate,
        NULL    AS Amount,
        0       AS Debit,
        0       AS Credit,
        NULL    AS Note
    FROM AddDealer d WHERE 1=1" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

    UNION ALL

    -- 2) DETAIL ROWS (sari forms se aane wali entries)
    SELECT
        d.Did                AS Did,
        d.DealerName         AS DealerName,
        0       AS DDAmount_Master,
        0       AS DAmount_Master,
        0       AS Balance_Master,
        t.Source      AS EntrySource,
        t.TranDate    AS EntryDate,
        t.Vehicle,
        t.Litter,
        t.Rate,
        t.Amount,
        t.Debit,
        t.Credit,
        t.Note
    FROM AddDealer d
    LEFT JOIN
    (
        -- 1) STOCK ENTRIES
        SELECT
            'DieselAddLitter'      AS Source,
            s.DealerId             AS Did,
            s.Date                 AS TranDate,
            s.Vehicle              AS Vehicle,
            IFNULL(s.AddDisel, 0)  AS Litter,
            IFNULL(s.Rate, 0)      AS Rate,
            (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Amount,
            (IFNULL(s.AddDisel, 0) * IFNULL(s.Rate, 0)) AS Debit,
            0                      AS Credit,
            s.Note                 AS Note
        FROM AddStock s WHERE 1=1" + ReportOfficeHelper.Sql(this, "s.OfficeName") + @"

        UNION ALL

        -- 2) LEDGER CREDIT ENTRIES
        SELECT
            'LedgerCredit'         AS Source,
            c.Did                  AS Did,
            c.Date                 AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                      AS Amount,
            0                      AS Debit,
            IFNULL(c.AmounGiven, 0) AS Credit,
            c.Note                 AS Note
        FROM DieselLedgerCredit c WHERE 1=1" + ReportOfficeHelper.Sql(this, "c.OfficeName") + @"

        UNION ALL

        -- 3) DIRECT DEALER PAYMENT ENTRIES
        SELECT
            'DirectDealerPayment'  AS Source,
            db.Did                 AS Did,
            db.Date                AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                         AS Amount,
            IFNULL(db.AmounGiven, 0) AS Debit,
            0                         AS Credit,
            db.Note                  AS Note
        FROM DieselLedgerDebit db WHERE 1=1" + ReportOfficeHelper.Sql(this, "db.OfficeName") + @"

        UNION ALL

        -- 4) DEALER-TO-DEALER PAYMENT SIDE (FirstDealer → DAmount / Credit)
        SELECT
            'DealerToDealer_Pay'   AS Source,
            dtd.id                 AS Did,
            dtd.Date               AS TranDate,
            NULL                   AS Vehicle,
            0                      AS Litter,
            0                      AS Rate,
            0                      AS Amount,
            0                      AS Debit,
            IFNULL(dtd.AmounGiven, 0) AS Credit,
            dtd.Note               AS Note
        FROM DealertoDealer dtd WHERE 1=1" + ReportOfficeHelper.Sql(this, "dtd.OfficeName") + @"

        UNION ALL

        -- 5) DEALER-TO-DEALER RECEIVE SIDE (SecondDealer → DDAmount / Debit)
        SELECT
            'DealerToDealer_Receive' AS Source,
            dtd.Did                  AS Did,
            dtd.Date                 AS TranDate,
            NULL                     AS Vehicle,
            0                        AS Litter,
            0                        AS Rate,
            0                        AS Amount,
            IFNULL(dtd.AmounGiven, 0) AS Debit,
            0                        AS Credit,
            dtd.Note                 AS Note
        FROM DealertoDealer dtd WHERE 1=1" + ReportOfficeHelper.Sql(this, "dtd.OfficeName") + @"

    ) t ON t.Did = d.Did
    WHERE 1=1" + ReportOfficeHelper.Sql(this, "d.OfficeName") + @"

) q
WHERE
    (
        @SearchText = ''
        OR TRIM(q.DealerName) = @SearchText COLLATE NOCASE
        OR IFNULL(q.Vehicle, '') LIKE @SearchLike COLLATE NOCASE
    )
    AND
(q.EntryDate IS NULL OR date(q.EntryDate) >= date(@FromDate) AND date(q.EntryDate) <= date(@ToDate))
ORDER BY
    q.DealerName,
    CASE WHEN q.EntrySource IS NULL THEN 0 ELSE 1 END,
    q.EntryDate,
    q.EntrySource;", con);

                    ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                    ReportSearchHelper.BindSearch(cmd, searchText);

                    SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    sd.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Koi data nahi mila is date range ya search ke liye.",
                                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                    string reportPath = Path.Combine(Application.StartupPath, "Reports", "NewDealerAmountReport.rdlc");
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
