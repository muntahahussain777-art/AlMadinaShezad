using Guna.UI2.WinForms;
using System;
using System.Data;
using System.Data.SQLite;

namespace AlMadinaShahzad.ReportForm
{
    public static class ReportDateRangeHelper
    {
        public static void Normalize(Guna2DateTimePicker from, Guna2DateTimePicker to, out string fromDate, out string toDate)
        {
            DateTime f = from.Value.Date;
            DateTime t = to.Value.Date;
            if (f > t)
            {
                DateTime swap = f;
                f = t;
                t = swap;
            }
            fromDate = f.ToString("yyyy-MM-dd");
            toDate = t.ToString("yyyy-MM-dd");
        }

        public static void AddToCommand(SQLiteCommand cmd, Guna2DateTimePicker from, Guna2DateTimePicker to)
        {
            Normalize(from, to, out string fromDate, out string toDate);
            cmd.Parameters.AddWithValue("@FromDate", fromDate);
            cmd.Parameters.AddWithValue("@ToDate", toDate);
        }

        public static void AddToCommand(IDbCommand cmd, Guna2DateTimePicker from, Guna2DateTimePicker to)
        {
            Normalize(from, to, out string fromDate, out string toDate);
            var pFrom = cmd.CreateParameter();
            pFrom.ParameterName = "@FromDate";
            pFrom.Value = fromDate;
            cmd.Parameters.Add(pFrom);
            var pTo = cmd.CreateParameter();
            pTo.ParameterName = "@ToDate";
            pTo.Value = toDate;
            cmd.Parameters.Add(pTo);
        }

        public static string InclusiveRange(string dateColumn)
        {
            return $"date({dateColumn}) >= date(@FromDate) AND date({dateColumn}) <= date(@ToDate)";
        }
    }
}
