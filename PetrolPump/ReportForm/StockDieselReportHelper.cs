using System;
using System.Data;
using System.Globalization;
using AlMadinaShahzad.Model;

namespace AlMadinaShahzad.ReportForm
{
    internal static class StockDieselReportHelper
    {
        internal sealed class Summary
        {
            public decimal AddLitter;
            public decimal MinusLitter;
            public decimal BaqayaLitter;
            public decimal AddAmount;
            public decimal MinusAmount;
            public decimal NetAmount;
        }

        internal static Summary EnrichReportTable(DataTable dt)
        {
            if (dt == null) return new Summary();

            EnsureColumn(dt, "Amount", typeof(decimal));
            EnsureColumn(dt, "EntryType", typeof(string));

            var summary = new Summary();

            foreach (DataRow row in dt.Rows)
            {
                decimal litter = ToDecimal(row["Litter"]);
                decimal rate = ToDecimal(row["Rate"]);
                string note = row["Note"]?.ToString();
                bool isMinus = StockDieselLitterHelper.IsMinusEntry(note, litter);
                decimal displayLitter = StockDieselLitterHelper.GetDisplayLitter(litter, note);
                decimal amount = displayLitter * rate;

                row["Litter"] = displayLitter;
                row["Amount"] = amount;
                row["EntryType"] = StockDieselLitterHelper.GetEntryType(litter, note);
                row["Note"] = StockDieselLitterHelper.StripMinusTag(note);

                if (isMinus)
                {
                    summary.MinusLitter += displayLitter;
                    summary.MinusAmount += amount;
                }
                else
                {
                    summary.AddLitter += displayLitter;
                    summary.AddAmount += amount;
                }
            }

            summary.BaqayaLitter = summary.AddLitter - summary.MinusLitter;
            summary.NetAmount = summary.AddAmount - summary.MinusAmount;
            return summary;
        }

        internal static DataTable BuildSummaryTable(Summary summary, string dateRange)
        {
            var dt = new DataTable("SummaryTable");
            dt.Columns.Add("AddLitter", typeof(string));
            dt.Columns.Add("MinusLitter", typeof(string));
            dt.Columns.Add("BaqayaLitter", typeof(string));
            dt.Columns.Add("AddAmount", typeof(string));
            dt.Columns.Add("MinusAmount", typeof(string));
            dt.Columns.Add("NetAmount", typeof(string));
            dt.Columns.Add("DateRange", typeof(string));

            dt.Rows.Add(
                FormatLitter(summary.AddLitter),
                FormatLitter(summary.MinusLitter),
                FormatLitter(summary.BaqayaLitter),
                FormatAmount(summary.AddAmount),
                FormatAmount(summary.MinusAmount),
                FormatAmount(summary.NetAmount),
                dateRange ?? string.Empty);

            return dt;
        }

        internal static string FormatLitter(decimal value) =>
            value.ToString("N2", CultureInfo.InvariantCulture);

        internal static string FormatAmount(decimal value) =>
            value.ToString("N0", CultureInfo.InvariantCulture);

        private static void EnsureColumn(DataTable dt, string name, Type type)
        {
            if (!dt.Columns.Contains(name))
                dt.Columns.Add(name, type);
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }
    }
}
