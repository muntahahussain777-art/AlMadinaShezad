using System;
using System.Data;

namespace AlMadinaShahzad.ReportForm
{
    /// <summary>
    /// Sirf report ke liye: negative receivable ko payable mein shift + S.N theek karna.
    /// Database / forms par koi asar nahi.
    /// </summary>
    internal static partial class TwoTableReceivablePayableReportHelper
    {
        internal const string MovedPayableNote = "Receivable (negative) → Payable";

        internal static DataTable PrepareCombinedReportData(DataTable raw)
        {
            var report = CreateCombinedSchema();
            if (raw == null)
                return report;

            int receivableSr = 0;
            int payableSr = 0;

            foreach (DataRow row in raw.Rows)
            {
                string customerName = NullIfEmpty(row["CustomerName"]);
                string dealerName = NullIfEmpty(row["DealerName"]);
                decimal customerRate = ToDecimal(row["CustomerRate"]);
                decimal dealerRate = ToDecimal(row["DealerRate"]);
                bool isTemporary = IsTemporaryRow(row);

                if (!string.IsNullOrEmpty(customerName))
                {
                    if (customerRate < 0m)
                    {
                        string note = isTemporary ? TemporaryEntryNote : MovedPayableNote;
                        AddPayableRow(report, ref payableSr, customerName, Math.Abs(customerRate), note);
                    }
                    else if (customerRate > 0m)
                    {
                        AddReceivableRow(
                            report,
                            ref receivableSr,
                            FormatReceivableName(customerName, isTemporary),
                            customerRate);
                    }
                }

                if (!string.IsNullOrEmpty(dealerName) && dealerRate > 0m)
                {
                    string note = isTemporary ? TemporaryEntryNote : string.Empty;
                    AddPayableRow(report, ref payableSr, dealerName, dealerRate, note);
                }
            }

            return report;
        }

        internal static DataTable PrepareReceivableSide(DataTable customerRaw)
        {
            var dt = CreateCustomerSideSchema();
            if (customerRaw == null)
                return dt;

            int sr = 0;
            foreach (DataRow row in customerRaw.Rows)
            {
                decimal amount = ToDecimal(row["CustomerRate"]);
                if (amount <= 0m)
                    continue;

                bool isTemporary = IsTemporaryRow(row);
                string name = Convert.ToString(row["CustomerName"]) ?? string.Empty;
                dt.Rows.Add(
                    FormatReceivableName(name, isTemporary),
                    amount,
                    ++sr);
            }

            return dt;
        }

        internal static DataTable PreparePayableSide(DataTable dealerRaw, DataTable customerRaw)
        {
            var dt = CreateDealerSideSchema();
            int sr = 0;

            if (customerRaw != null)
            {
                foreach (DataRow row in customerRaw.Rows)
                {
                    decimal amount = ToDecimal(row["CustomerRate"]);
                    if (amount >= 0m)
                        continue;

                    dt.Rows.Add(
                        Convert.ToString(row["CustomerName"]) ?? string.Empty,
                        Math.Abs(amount),
                        MovedPayableNote,
                        ++sr);
                }
            }

            if (dealerRaw != null)
            {
                foreach (DataRow row in dealerRaw.Rows)
                {
                    decimal amount = ToDecimal(row["DealerRate"]);
                    if (amount <= 0m)
                        continue;

                    bool isTemporary = IsTemporaryRow(row);
                    string note = isTemporary ? TemporaryEntryNote : string.Empty;
                    dt.Rows.Add(
                        Convert.ToString(row["DealerName"]) ?? string.Empty,
                        amount,
                        note,
                        ++sr);
                }
            }

            return dt;
        }

        private static DataTable CreateCombinedSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("CustomerRate", typeof(decimal));
            dt.Columns.Add("DealerName", typeof(string));
            dt.Columns.Add("DealerRate", typeof(decimal));
            dt.Columns.Add("ReceivableSr", typeof(int));
            dt.Columns.Add("PayableSr", typeof(int));
            dt.Columns.Add("PayableNote", typeof(string));
            return dt;
        }

        private static DataTable CreateCustomerSideSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("CustomerRate", typeof(decimal));
            dt.Columns.Add("SerialNo", typeof(int));
            return dt;
        }

        private static DataTable CreateDealerSideSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("DealerName", typeof(string));
            dt.Columns.Add("DealerRate", typeof(decimal));
            dt.Columns.Add("PayableNote", typeof(string));
            dt.Columns.Add("SerialNo", typeof(int));
            return dt;
        }

        private static void AddReceivableRow(DataTable dt, ref int sr, string name, decimal amount)
        {
            sr++;
            dt.Rows.Add(name, amount, null, DBNull.Value, sr, 0, string.Empty);
        }

        private static void AddPayableRow(DataTable dt, ref int sr, string name, decimal amount, string note)
        {
            sr++;
            dt.Rows.Add(null, DBNull.Value, name, amount, 0, sr, note ?? string.Empty);
        }

        private static string NullIfEmpty(object value)
        {
            string s = Convert.ToString(value)?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(value);
        }
    }
}
