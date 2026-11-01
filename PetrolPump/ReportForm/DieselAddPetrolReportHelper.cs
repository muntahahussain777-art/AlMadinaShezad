using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AlMadinaShahzad.ReportForm
{
    /// <summary>
    /// Shared diesel (AddPetrol) report helpers: running balance + A4 portrait column autosize.
    /// Columns: S.No, Vehicle No, Date, (Receipt hidden), Bank, Advance, Litter, Rate, Amount, Credit, Balance, (Note hidden). Name shown as top label.
    /// </summary>
    internal static class DieselAddPetrolReportHelper
    {
        public const float A4ContentWidthCm = 19.0f;

        public static string ResolveCustomerDisplayName(DataTable dt, string customerFilter)
        {
            if (!string.IsNullOrWhiteSpace(customerFilter))
                return customerFilter.Trim();

            if (dt == null || dt.Rows.Count == 0 || !dt.Columns.Contains("Name"))
                return "";

            string found = null;
            foreach (DataRow row in dt.Rows)
            {
                string name = (Convert.ToString(row["Name"]) ?? "").Trim();
                if (string.IsNullOrEmpty(name)) continue;
                if (found == null)
                {
                    found = name;
                    continue;
                }
                if (!string.Equals(found, name, StringComparison.OrdinalIgnoreCase))
                    return "";
            }
            return found ?? "";
        }


        public const string SelectSql = @"
SELECT 
    PetrolAdd.pid, 
    PetrolAdd.Date, 
    PetrolAdd.ReceiptNo, 
    PetrolAdd.vehicle AS Vehicle, 
    PetrolAdd.Litter, 
    PetrolAdd.Rate, 
    PetrolAdd.Advance, 
    CASE 
        WHEN IFNULL(PetrolAdd.Litter, 0) <> 0 AND IFNULL(PetrolAdd.Rate, 0) <> 0
        THEN (PetrolAdd.Litter * PetrolAdd.Rate)
        ELSE IFNULL(PetrolAdd.Amount, 0)
    END AS Amount,
    IFNULL(PetrolAdd.Credit, 0) AS Credit,
    CAST(0 AS REAL) AS Balance,
    IFNULL(PetrolAdd.Note, '') AS Bank,
    PetrolAdd.Note, 
    AddCustomer.Name 
FROM PetrolAdd 
INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id";

        public static void ApplyRunningBalance(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            if (!dt.Columns.Contains("Balance")) return;

            string currentCustomer = null;
            decimal running = 0;

            foreach (DataRow row in dt.Rows)
            {
                string name = Convert.ToString(row["Name"]) ?? "";
                if (!string.Equals(name, currentCustomer, StringComparison.OrdinalIgnoreCase))
                {
                    currentCustomer = name;
                    running = 0;
                }

                decimal amount = ToDecimal(row["Amount"]);
                decimal credit = ToDecimal(row["Credit"]);
                decimal advance = dt.Columns.Contains("Advance") ? ToDecimal(row["Advance"]) : 0;
                // Same as Daily Diesel entry: Amount - Credit - Advance
                running += amount - credit - advance;
                row["Balance"] = running.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        public static string BuildAutoSizedReport(string templatePath, DataTable dt)
        {
            if (!File.Exists(templatePath))
                return templatePath;

            float[] widths = MeasureColumnWidthsCm(dt);
            float total = 0;
            for (int i = 0; i < widths.Length; i++)
                total += widths[i];

            if (total > A4ContentWidthCm && total > 0)
            {
                float scale = A4ContentWidthCm / total;
                for (int i = 0; i < widths.Length; i++)
                {
                    // Keep Receipt (3) and unused Note (11) hidden at 0
                    if (i == 3 || i == 11) continue;
                    widths[i] = (float)Math.Round(widths[i] * scale, 2);
                }
                total = A4ContentWidthCm;
            }

            string xml = File.ReadAllText(templatePath, Encoding.UTF8);

            // Selected brand AutoSize temp mein likho —
            // warna load/search par header wapas default brand ho jata hai.
            string company = ReportHeaderBrandHelper.CurrentCompany;
            if (string.IsNullOrWhiteSpace(company))
                company = ReportHeaderBrandHelper.BrandName;
            xml = xml.Replace(ReportHeaderBrandHelper.AlNoor, company);
            xml = xml.Replace("Al Noor Petroleum Service", company);
            xml = xml.Replace("Al Madina Shezad Chhutta Service", company);
            xml = xml.Replace(ReportHeaderBrandHelper.BrandName, company);
            xml = xml.Replace(ReportHeaderBrandHelper.FourStar, company);
            try
            {
                foreach (string n in ReportHeaderNamesStore.LoadAll())
                {
                    if (!string.IsNullOrWhiteSpace(n)
                        && !string.Equals(n, company, StringComparison.OrdinalIgnoreCase))
                        xml = xml.Replace(n, company);
                }
            }
            catch { }

            var sb = new StringBuilder();
            sb.AppendLine("          <TablixColumns>");
            for (int i = 0; i < widths.Length; i++)
            {
                sb.AppendLine("            <TablixColumn>");
                sb.AppendLine("              <Width>" + widths[i].ToString("0.00", CultureInfo.InvariantCulture) + "cm</Width>");
                sb.AppendLine("            </TablixColumn>");
            }
            sb.Append("          </TablixColumns>");

            xml = Regex.Replace(xml,
                @"<TablixColumns>[\s\S]*?</TablixColumns>",
                sb.ToString(),
                RegexOptions.IgnoreCase);

            string totalCm = total.ToString("0.00", CultureInfo.InvariantCulture);
            xml = Regex.Replace(xml,
                @"(<DataSetName>DataSet1</DataSetName>\s*<Top>)[^<]+(</Top>\s*<Left>)[^<]+(</Left>\s*<Height>)[^<]+(</Height>\s*<Width>)[^<]+(</Width>)",
                m => m.Groups[1].Value + "0.25cm" + m.Groups[2].Value + "0.30cm" + m.Groups[3].Value + "1.55cm" +
                     m.Groups[4].Value + totalCm + "cm" + m.Groups[5].Value,
                RegexOptions.IgnoreCase);

            xml = Regex.Replace(xml, @"<PageHeight>[^<]+</PageHeight>", "<PageHeight>29.7cm</PageHeight>");
            xml = Regex.Replace(xml, @"<PageWidth>[^<]+</PageWidth>", "<PageWidth>21cm</PageWidth>");

            string tempPath = Path.Combine(Path.GetTempPath(), "AddPetrol_AutoSize_" + Guid.NewGuid().ToString("N") + ".rdlc");
            File.WriteAllText(tempPath, xml, Encoding.UTF8);
            return tempPath;
        }

        private static float[] MeasureColumnWidthsCm(DataTable dt)
        {
            // Order matches RDLC: S.No, Vehicle No, Date, Receipt(hidden), Bank, Advance, Litter, Rate, Amount, Credit, Balance, Note(hidden)
            string[] headers = { "S.No", "Vehicle No", "Date", "", "Bank", "Advance", "Litter", "Rate", "Amount", "Credit", "Balance", "" };
            string[] fields = { null, "Vehicle", "Date", null, "Bank", "Advance", "Litter", "Rate", "Amount", "Credit", "Balance", null };
            float[] mins = { 0.55f, 1.20f, 1.35f, 0f, 1.20f, 1.00f, 1.00f, 1.00f, 1.25f, 1.15f, 1.25f, 0f };
            float[] maxs = { 0.90f, 3.50f, 2.20f, 0f, 5.00f, 2.40f, 2.40f, 2.40f, 3.00f, 2.60f, 3.00f, 0f };
            float[] needed = new float[12];

            using (var bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var font = new Font("Segoe UI", 10f, FontStyle.Regular))
            using (var headerFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                for (int i = 0; i < 12; i++)
                {
                    if (i == 3 || i == 11)
                    {
                        needed[i] = 0f;
                        continue;
                    }

                    float w = MeasureTextCm(g, headerFont, headers[i]);

                    if (i == 0)
                    {
                        int rows = dt == null ? 1 : Math.Max(1, dt.Rows.Count);
                        w = Math.Max(w, MeasureTextCm(g, font, rows.ToString(CultureInfo.InvariantCulture)));
                    }
                    else if (fields[i] != null && dt != null)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            string text = FormatCellText(row, fields[i]);
                            if (string.IsNullOrEmpty(text)) continue;
                            w = Math.Max(w, MeasureTextCm(g, font, text));
                        }
                    }

                    w += 0.35f;
                    needed[i] = Math.Max(mins[i], Math.Min(maxs[i], w));
                }
            }

            return needed;
        }

        private static float MeasureTextCm(Graphics g, Font font, string text)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            SizeF size = g.MeasureString(text, font);
            return size.Width * 2.54f / 96f;
        }

        private static string FormatCellText(DataRow row, string field)
        {
            if (row == null || !row.Table.Columns.Contains(field)) return "";
            object value = row[field];
            if (value == null || value == DBNull.Value) return "";

            if (field == "Date")
            {
                if (value is DateTime dt) return dt.ToString("dd-MM-yyyy");
                if (DateTime.TryParse(Convert.ToString(value), out DateTime parsed))
                    return parsed.ToString("dd-MM-yyyy");
                return Convert.ToString(value) ?? "";
            }

            if (field == "Litter" || field == "Rate" || field == "Advance" ||
                field == "Amount" || field == "Credit" || field == "Balance")
            {
                if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal n) ||
                    decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out n))
                    return n.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return (Convert.ToString(value) ?? "").Trim();
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
