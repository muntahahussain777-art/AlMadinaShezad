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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HajiBalochSoftwere.ReportForm
{
    public partial class New2DelearFormComplete : Sample
    {
        public New2DelearFormComplete()
        {
            InitializeComponent();
        }

        private void New2DelearFormComplete_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date set
            todate.Value = DateTime.Now;
            SetupDealerSearchSuggest();
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }

        private string ResolveDealerSearchText(string text)
        {
            text = (text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
                return text;

            AutoCompleteStringCollection src = txtSearch.AutoCompleteCustomSource;
            if (src == null || src.Count == 0)
                return text;

            foreach (string name in src)
            {
                if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            string startsWithMatch = null;
            foreach (string name in src)
            {
                if (name != null && name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                {
                    if (startsWithMatch != null)
                        return text; // ambiguous
                    startsWithMatch = name;
                }
            }

            return startsWithMatch ?? text;
        }

        private void SetupDealerSearchSuggest()
        {
            try
            {
                var names = new AutoCompleteStringCollection();
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "SELECT DISTINCT TRIM(DealerName) AS DealerName FROM AddDealer WHERE 1=1" + ReportOfficeHelper.Sql(this, "OfficeName") + @" AND IFNULL(TRIM(DealerName),'') <> '' ORDER BY DealerName COLLATE NOCASE", con))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = Convert.ToString(reader["DealerName"])?.Trim();
                            if (!string.IsNullOrEmpty(name))
                                names.Add(name);
                        }
                    }
                }

                txtSearch.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtSearch.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtSearch.AutoCompleteCustomSource = names;
            }
            catch
            {
                // suggest fail hone pe report phir bhi chalega
            }
        }

        // 🔹 1) FULL RECORDS – bina date / search filter
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
    IFNULL(q.Note, '') AS Bank,
    q.Note,
    CAST(0 AS REAL) AS Balance
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
                PrepareDealerGroupWiseTable(dt);
                BindDealerGroupWiseReport(dt);
            }
        }

        // 🔹 2) SEARCH + DATE FILTER WALA PART
        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = ResolveDealerSearchText(txtSearch.Text);

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
    IFNULL(q.Note, '') AS Bank,
    q.Note,
    CAST(0 AS REAL) AS Balance
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
WHERE
    (
        @SearchText = ''
        OR TRIM(q.DealerName) = @SearchText COLLATE NOCASE
        OR IFNULL(q.Vehicle, '') LIKE @SearchLike COLLATE NOCASE
    )
    AND 
    (
        q.EntryDate IS NULL 
        OR date(q.EntryDate) >= date(@FromDate) AND date(q.EntryDate) <= date(@ToDate)
    )
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

                    PrepareDealerGroupWiseTable(dt);
                    BindDealerGroupWiseReport(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report load nahi hui: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BindDealerGroupWiseReport(DataTable dt)
        {
            string templatePath = Path.Combine(Application.StartupPath, "Reports", "New2DealerReport.rdlc");
            string reportPath = DealerGroupWiseReportHelper.BuildAutoSizedReport(templatePath, dt);

            ReportDataSource rds = new ReportDataSource("DataSet1", dt);
            reportViewer1.LocalReport.ReportPath = reportPath;
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(rds);
            reportViewer1.RefreshReport();
        }

        private static void PrepareDealerGroupWiseTable(DataTable dt)
        {
            if (dt == null) return;

            if (!dt.Columns.Contains("Bank"))
                dt.Columns.Add("Bank", typeof(string));
            if (!dt.Columns.Contains("Balance"))
                dt.Columns.Add("Balance", typeof(double));

            // EntryDate ko string column mein format karo (DateTime column pe string store fail hota hai)
            EnsureEntryDateAsStringColumn(dt);

            string currentDealer = null;
            decimal running = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("Note") && (row["Bank"] == null || row["Bank"] == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(row["Bank"]))))
                    row["Bank"] = row["Note"] == DBNull.Value ? "" : Convert.ToString(row["Note"]) ?? "";

                string dealer = Convert.ToString(row["DealerName"]) ?? "";
                if (!string.Equals(dealer, currentDealer, StringComparison.OrdinalIgnoreCase))
                {
                    currentDealer = dealer;
                    running = 0;
                }

                FormatEntryDateDdMmYyyy(row);

                bool isSummary = row["EntryDate"] == null || row["EntryDate"] == DBNull.Value
                    || string.IsNullOrWhiteSpace(Convert.ToString(row["EntryDate"]));

                if (isSummary)
                {
                    row["Balance"] = 0d;
                    continue;
                }

                decimal debit = ToDecimal(row["Debit"]);
                decimal credit = ToDecimal(row["Credit"]);
                running += debit - credit;
                row["Balance"] = Convert.ToDouble(running);
            }
        }

        private static void EnsureEntryDateAsStringColumn(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("EntryDate")) return;
            if (dt.Columns["EntryDate"].DataType == typeof(string)) return;

            var formatted = new string[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                object value = dt.Rows[i]["EntryDate"];
                if (value == null || value == DBNull.Value)
                {
                    formatted[i] = null;
                    continue;
                }

                if (value is DateTime dtVal)
                {
                    formatted[i] = dtVal.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                    continue;
                }

                string text = Convert.ToString(value)?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    formatted[i] = null;
                    continue;
                }

                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed)
                    || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsed))
                    formatted[i] = parsed.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                else
                    formatted[i] = text;
            }

            dt.Columns.Remove("EntryDate");
            dt.Columns.Add("EntryDate", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
                dt.Rows[i]["EntryDate"] = (object)formatted[i] ?? DBNull.Value;
        }

        private static void FormatEntryDateDdMmYyyy(DataRow row)
        {
            if (row == null || !row.Table.Columns.Contains("EntryDate")) return;
            if (row.Table.Columns["EntryDate"].DataType != typeof(string)) return;

            object value = row["EntryDate"];
            if (value == null || value == DBNull.Value) return;

            string text = Convert.ToString(value)?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Already dd-MM-yyyy
            if (DateTime.TryParseExact(text, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed)
                || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsed))
            {
                row["EntryDate"] = parsed.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            }
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            if (value is decimal d) return d;
            if (value is double dbl) return Convert.ToDecimal(dbl);
            if (value is float f) return Convert.ToDecimal(f);
            if (value is long l) return l;
            if (value is int i) return i;
            decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed);
            return parsed;
        }
    }
}
