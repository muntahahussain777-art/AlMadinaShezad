using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.Model;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmLitterCompareReport : Sample
    {
        private bool _hooksAttached;
        private int _loadTicket;
        private const string ReportFile = "LitterCompareDetailRDLC.rdlc";
        private const string LoadButtonText = "Load";

        public frmLitterCompareReport()
        {
            InitializeComponent();
            FormClosed += frmLitterCompareReport_FormClosed;
        }

        private void frmLitterCompareReport_FormClosed(object sender, FormClosedEventArgs e)
        {
            _loadTicket++;
        }

        private void frmLitterCompareReport_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;
            todate.Value = DateTime.Now;
            AttachHooks();
            BeginLoadReport(showEmptyMessage: false);
        }

        private void AttachHooks()
        {
            if (_hooksAttached) return;
            _hooksAttached = true;
            btnSearch.Click -= btnSearch_Click;
            btnSearch.Click += btnSearch_Click;
            fromdate.CloseUp -= DatePickers_CloseUp;
            todate.CloseUp -= DatePickers_CloseUp;
            fromdate.CloseUp += DatePickers_CloseUp;
            todate.CloseUp += DatePickers_CloseUp;
        }

        private void DatePickers_CloseUp(object sender, EventArgs e)
        {
            BeginLoadReport(showEmptyMessage: false);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            BeginLoadReport(showEmptyMessage: true);
        }

        private void BeginLoadReport(bool showEmptyMessage)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            int ticket = ++_loadTicket;
            _ = LoadReportAsync(ticket, showEmptyMessage);
        }

        private async Task LoadReportAsync(int ticket, bool showEmptyMessage)
        {
            SetLoadingUi(true);

            try
            {
                ReportDateRangeHelper.Normalize(fromdate, todate, out string fromDate, out string toDate);
                string search = (txtSearch.Text ?? "").Trim();
                string dateRange =
                    DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd-MMM-yyyy")
                    + "   to   "
                    + DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd-MMM-yyyy");

                string reportPath = Path.Combine(Application.StartupPath, "Reports", ReportFile);
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found: " + reportPath, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string officeAddStock = ReportOfficeHelper.Sql(this, "AddStock.OfficeName");
                string officeStockDiesel = ReportOfficeHelper.Sql(this, "StockDiesel.OfficeName");
                string officePetrolAdd = ReportOfficeHelper.Sql(this, "PetrolAdd.OfficeName");

                LitterCompareReportHelper.ReportPayload payload = await Task.Run(() =>
                    LitterCompareReportHelper.BuildReportPayload(fromDate, toDate, search, dateRange, officeAddStock, officeStockDiesel, officePetrolAdd))
                    .ConfigureAwait(true);

                if (ticket != _loadTicket || IsDisposed)
                    return;

                if (showEmptyMessage && payload.DetailRowCount == 0)
                {
                    MessageBox.Show("Is date range ke liye koi litter entry nahi mili.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                reportViewer1.ProcessingMode = ProcessingMode.Local;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", payload.Detail));
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("SummarySet", payload.SummaryTable));
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DailySummarySet", payload.DailySummary));
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.PageWidth;
                await Task.Yield();
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (ticket == _loadTicket && !IsDisposed)
                {
                    MessageBox.Show("Report error: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (ticket == _loadTicket)
                    SetLoadingUi(false);
            }
        }

        private void SetLoadingUi(bool loading)
        {
            btnSearch.Enabled = !loading;
            btnSearch.Text = loading ? "Loading..." : LoadButtonText;
            fromdate.Enabled = !loading;
            todate.Enabled = !loading;
            txtSearch.Enabled = !loading;
            UseWaitCursor = loading;
            Cursor.Current = loading ? Cursors.WaitCursor : Cursors.Default;
        }
    }

    internal static class LitterCompareReportHelper
    {
        internal sealed class ReportPayload
        {
            public DataTable Detail;
            public DataTable SummaryTable;
            public DataTable DailySummary;
            public int DetailRowCount;
        }

        internal const int MaxFullDailyRows = 93;

        internal static ReportPayload BuildReportPayload(string fromDate, string toDate, string search, string dateRange,
            string officeAddStock = "", string officeStockDiesel = "", string officePetrolAdd = "")
        {
            DataTable detail = FetchDetailRows(fromDate, toDate, search, officeAddStock, officeStockDiesel, officePetrolAdd);
            Summary summary = BuildSummary(detail);
            return new ReportPayload
            {
                Detail = detail,
                DailySummary = BuildDailySummaryTable(detail, fromDate, toDate),
                SummaryTable = BuildSummaryTable(summary, dateRange),
                DetailRowCount = detail.Rows.Count
            };
        }

        internal sealed class Summary
        {
            public decimal DealerAmountLitter;
            public decimal DailyDieselSaleLitter;
            public decimal StockAddLitter;
            public decimal StockMinusLitter;
            public decimal StockBaqayaLitter;
            public decimal DailyMinusStockBaqaya;
            public decimal TotalPurchaseLitter;
            public decimal TotalSaleLitter;
            public decimal CompareDifference;
        }

        internal static DataTable FetchDetailRows(string fromDate, string toDate, string searchText,
            string officeAddStock = "", string officeStockDiesel = "", string officePetrolAdd = "")
        {
            string sql = @"
SELECT * FROM (
    SELECT
        'DealerAmount' AS Source,
        AddStock.Date AS Date,
        IFNULL(AddDealer.DealerName, '') AS PartyName,
        IFNULL(AddStock.Vehicle, '') AS Vehicle,
        'Purchase' AS EntryType,
        ABS(IFNULL(AddStock.AddDisel, 0)) AS Litter,
        IFNULL(AddStock.Rate, 0) AS Rate,
        ABS(IFNULL(AddStock.AddDisel, 0)) * IFNULL(AddStock.Rate, 0) AS Amount,
        IFNULL(AddStock.Note, '') AS Note,
        1 AS SortSource
    FROM AddStock
    LEFT JOIN AddDealer ON AddStock.DealerId = AddDealer.Did
    WHERE date(AddStock.Date) >= date(@FromDate)
      AND date(AddStock.Date) <= date(@ToDate)
      AND IFNULL(AddStock.AddDisel, 0) <> 0" + officeAddStock + @"

    UNION ALL

    SELECT
        'Stock' AS Source,
        StockDiesel.Date AS Date,
        IFNULL(AddDealer.DealerName, '') AS PartyName,
        IFNULL(StockDiesel.Vehicle, '') AS Vehicle,
        '' AS EntryType,
        IFNULL(StockDiesel.Litter, 0) AS Litter,
        IFNULL(StockDiesel.Rate, 0) AS Rate,
        ABS(IFNULL(StockDiesel.Litter, 0)) * IFNULL(StockDiesel.Rate, 0) AS Amount,
        IFNULL(StockDiesel.Note, '') AS Note,
        2 AS SortSource
    FROM StockDiesel
    LEFT JOIN AddDealer ON StockDiesel.SDid = AddDealer.Did
    WHERE date(StockDiesel.Date) >= date(@FromDate)
      AND date(StockDiesel.Date) <= date(@ToDate)
      AND IFNULL(StockDiesel.Litter, 0) <> 0" + officeStockDiesel + @"

    UNION ALL

    SELECT
        'Daily Diesel Sales' AS Source,
        PetrolAdd.Date AS Date,
        IFNULL(AddCustomer.Name, '') AS PartyName,
        IFNULL(PetrolAdd.vehicle, '') AS Vehicle,
        'Sale' AS EntryType,
        ABS(IFNULL(PetrolAdd.Litter, 0)) AS Litter,
        IFNULL(PetrolAdd.Rate, 0) AS Rate,
        ABS(IFNULL(PetrolAdd.Litter, 0)) * IFNULL(PetrolAdd.Rate, 0) AS Amount,
        IFNULL(PetrolAdd.Note, '') AS Note,
        3 AS SortSource
    FROM PetrolAdd
    LEFT JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
    WHERE date(PetrolAdd.Date) >= date(@FromDate)
      AND date(PetrolAdd.Date) <= date(@ToDate)
      AND IFNULL(PetrolAdd.Litter, 0) <> 0" + officePetrolAdd + @"
) AS u
WHERE (@SearchText = ''
    OR IFNULL(u.Source, '') LIKE @SearchLike COLLATE NOCASE
    OR IFNULL(u.PartyName, '') LIKE @SearchLike COLLATE NOCASE
    OR IFNULL(u.Vehicle, '') LIKE @SearchLike COLLATE NOCASE
    OR IFNULL(u.EntryType, '') LIKE @SearchLike COLLATE NOCASE
    OR IFNULL(u.Note, '') LIKE @SearchLike COLLATE NOCASE)
ORDER BY date(u.Date) ASC, u.SortSource ASC, u.PartyName ASC;
";

            var dt = new DataTable();
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@SearchText", searchText ?? "");
                    cmd.Parameters.AddWithValue("@SearchLike", "%" + (searchText ?? "") + "%");
                    using (var da = new SQLiteDataAdapter(cmd))
                        da.Fill(dt);
                }
            }

            NormalizeDetailTable(dt);
            return dt;
        }

        /// <summary>
        /// RDLC ke liye Date/numbers sahi types + Stock note clean.
        /// SortSource: 1 DealerAmount, 2 Stock, 3 Daily Diesel Sales
        /// </summary>
        private static void NormalizeDetailTable(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Date")) return;

            if (dt.Columns["Date"].DataType != typeof(DateTime))
            {
                var dateCol = dt.Columns.Add("_DateFixed", typeof(DateTime));
                foreach (DataRow row in dt.Rows)
                    row[dateCol] = ParseRowDate(row["Date"]);
                int ord = dt.Columns["Date"].Ordinal;
                dt.Columns.Remove("Date");
                dateCol.ColumnName = "Date";
                dateCol.SetOrdinal(ord);
            }

            foreach (DataRow row in dt.Rows)
            {
                string source = Convert.ToString(row["Source"]) ?? "";
                decimal litter = ToDec(row["Litter"]);
                string note = row["Note"]?.ToString();

                if (source.Equals("Stock", StringComparison.OrdinalIgnoreCase))
                {
                    decimal displayLitter = StockDieselLitterHelper.GetDisplayLitter(litter, note);
                    row["EntryType"] = StockDieselLitterHelper.GetEntryType(litter, note);
                    row["Litter"] = displayLitter;
                    row["Note"] = StockDieselLitterHelper.StripMinusTag(note);
                    if (dt.Columns.Contains("Amount"))
                        row["Amount"] = displayLitter * ToDec(row["Rate"]);
                }
                else
                {
                    if (dt.Columns.Contains("Note"))
                        row["Note"] = StockDieselLitterHelper.StripMinusTag(note);
                }

                if (dt.Columns.Contains("Litter"))
                    row["Litter"] = ToDec(row["Litter"]);
                if (dt.Columns.Contains("Rate"))
                    row["Rate"] = ToDec(row["Rate"]);
                if (dt.Columns.Contains("Amount"))
                    row["Amount"] = ToDec(row["Amount"]);
            }
        }

        private static DateTime ParseRowDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;
            if (value is DateTime dt)
                return dt.Date;
            string s = Convert.ToString(value)?.Trim() ?? "";
            if (DateTime.TryParseExact(s, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed.Date;
            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return parsed.Date;
            return DateTime.MinValue;
        }

        internal static Summary BuildSummary(DataTable detail)
        {
            var s = new Summary();
            if (detail == null) return s;

            foreach (DataRow row in detail.Rows)
            {
                string source = Convert.ToString(row["Source"]) ?? "";
                string type = Convert.ToString(row["EntryType"]) ?? "";
                decimal litter = ToDec(row["Litter"]);

                if (source.Equals("DealerAmount", StringComparison.OrdinalIgnoreCase))
                    s.DealerAmountLitter += litter;
                else if (source.Equals("Daily Diesel Sales", StringComparison.OrdinalIgnoreCase))
                    s.DailyDieselSaleLitter += litter;
                else if (source.Equals("Stock", StringComparison.OrdinalIgnoreCase))
                {
                    if (type.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0)
                        s.StockMinusLitter += litter;
                    else
                        s.StockAddLitter += litter;
                }
            }

            s.StockBaqayaLitter = s.StockAddLitter - s.StockMinusLitter;
            s.DailyMinusStockBaqaya = s.DailyDieselSaleLitter - s.StockBaqayaLitter;
            s.TotalPurchaseLitter = s.DealerAmountLitter + s.StockAddLitter;
            s.TotalSaleLitter = s.DailyDieselSaleLitter + s.StockMinusLitter;
            s.CompareDifference = s.TotalPurchaseLitter - s.TotalSaleLitter;
            return s;
        }

        internal static DataTable BuildSummaryTable(Summary s, string dateRange)
        {
            var dt = new DataTable("SummaryTable");
            dt.Columns.Add("DateRange", typeof(string));
            dt.Columns.Add("DealerAmountLitter", typeof(string));
            dt.Columns.Add("DailyDieselSaleLitter", typeof(string));
            dt.Columns.Add("StockAddLitter", typeof(string));
            dt.Columns.Add("StockMinusLitter", typeof(string));
            dt.Columns.Add("StockBaqayaLitter", typeof(string));
            dt.Columns.Add("DailyMinusStockBaqaya", typeof(string));
            dt.Columns.Add("TotalPurchaseLitter", typeof(string));
            dt.Columns.Add("TotalSaleLitter", typeof(string));
            dt.Columns.Add("CompareDifference", typeof(string));

            dt.Rows.Add(
                dateRange ?? "",
                FormatLitter(s.DealerAmountLitter),
                FormatLitter(s.DailyDieselSaleLitter),
                FormatLitter(s.StockAddLitter),
                FormatLitter(s.StockMinusLitter),
                FormatLitter(s.StockBaqayaLitter),
                FormatLitter(s.DailyMinusStockBaqaya),
                FormatLitter(s.TotalPurchaseLitter),
                FormatLitter(s.TotalSaleLitter),
                FormatLitter(s.CompareDifference));

            return dt;
        }

        internal static DataTable BuildDailySummaryTable(DataTable detail, string fromDate, string toDate)
        {
            var dt = new DataTable("DailySummaryTable");
            dt.Columns.Add("Date", typeof(DateTime));
            dt.Columns.Add("DealerAmountLitter", typeof(decimal));
            dt.Columns.Add("StockAddLitter", typeof(decimal));
            dt.Columns.Add("StockMinusLitter", typeof(decimal));
            dt.Columns.Add("StockBaqayaLitter", typeof(decimal));
            dt.Columns.Add("DailyDieselSaleLitter", typeof(decimal));
            dt.Columns.Add("DailyMinusStockBaqaya", typeof(decimal));

            DateTime start = ParseFilterDate(fromDate);
            DateTime end = ParseFilterDate(toDate);
            if (start == DateTime.MinValue || end == DateTime.MinValue || end < start)
                return dt;

            var byDate = new System.Collections.Generic.Dictionary<DateTime, DailyRow>();

            if (detail != null)
            {
                foreach (DataRow row in detail.Rows)
                {
                    DateTime day = ParseRowDate(row["Date"]);
                    if (day == DateTime.MinValue) continue;

                    if (!byDate.TryGetValue(day, out DailyRow dr))
                    {
                        dr = new DailyRow();
                        byDate[day] = dr;
                    }

                    string source = Convert.ToString(row["Source"]) ?? "";
                    string type = Convert.ToString(row["EntryType"]) ?? "";
                    decimal litter = ToDec(row["Litter"]);

                    if (source.Equals("DealerAmount", StringComparison.OrdinalIgnoreCase))
                        dr.DealerAmount += litter;
                    else if (source.Equals("Daily Diesel Sales", StringComparison.OrdinalIgnoreCase))
                        dr.DailySale += litter;
                    else if (source.Equals("Stock", StringComparison.OrdinalIgnoreCase))
                    {
                        if (type.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0)
                            dr.StockMinus += litter;
                        else
                            dr.StockAdd += litter;
                    }
                }
            }

            int dayCount = (end.Date - start.Date).Days + 1;
            if (dayCount <= MaxFullDailyRows)
            {
                for (DateTime day = start.Date; day <= end.Date; day = day.AddDays(1))
                    AddDailySummaryRow(dt, day, byDate);
            }
            else
            {
                var sortedDays = new System.Collections.Generic.List<DateTime>(byDate.Keys);
                sortedDays.Sort();
                foreach (DateTime day in sortedDays)
                {
                    if (day < start.Date || day > end.Date)
                        continue;
                    AddDailySummaryRow(dt, day, byDate);
                }
            }

            return dt;
        }

        private static void AddDailySummaryRow(
            DataTable dt,
            DateTime day,
            System.Collections.Generic.Dictionary<DateTime, DailyRow> byDate)
        {
            if (!byDate.TryGetValue(day, out DailyRow dr))
                dr = new DailyRow();

            decimal dayBaqaya = dr.StockAdd - dr.StockMinus;
            dt.Rows.Add(
                day,
                dr.DealerAmount,
                dr.StockAdd,
                dr.StockMinus,
                dayBaqaya,
                dr.DailySale,
                dr.DailySale - dayBaqaya);
        }

        private static DateTime ParseFilterDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;
            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed.Date;
            return DateTime.MinValue;
        }

        private sealed class DailyRow
        {
            public decimal DealerAmount;
            public decimal StockAdd;
            public decimal StockMinus;
            public decimal DailySale;
        }

        private static string FormatLitter(decimal value) =>
            value.ToString("N2", CultureInfo.InvariantCulture);

        private static decimal ToDec(object v)
        {
            if (v == null || v == DBNull.Value) return 0m;
            return Convert.ToDecimal(v);
        }
    }
}
