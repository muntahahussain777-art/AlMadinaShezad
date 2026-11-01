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
    /// Dealer Complete Group Wise (New2DealerReport): A4 portrait column autosize.
    /// Columns: S.N, Entry Date, Bank, Vehicle, Litter, Rate, Debit, Credit, Balance.
    /// </summary>
    internal static class DealerGroupWiseReportHelper
    {
        public const float A4ContentWidthCm = 19.0f;

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
                    widths[i] = (float)Math.Round(widths[i] * scale, 2);
                total = A4ContentWidthCm;
            }

            string xml = File.ReadAllText(templatePath, Encoding.UTF8);
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
                m => m.Groups[1].Value + "2.10cm" + m.Groups[2].Value + "0.30cm" + m.Groups[3].Value + "1.30cm" +
                     m.Groups[4].Value + totalCm + "cm" + m.Groups[5].Value,
                RegexOptions.IgnoreCase);

            // Keep totals aligned to right side under table
            string totalsLeft = Math.Max(0.30f, total - 8.5f).ToString("0.00", CultureInfo.InvariantCulture) + "cm";
            xml = Regex.Replace(xml,
                @"(<Textbox Name=""Textbox18"">[\s\S]*?<Top>)[^<]+(</Top>\s*<Left>)[^<]+(</Left>)",
                m => m.Groups[1].Value + "4.00cm" + m.Groups[2].Value + totalsLeft + m.Groups[3].Value,
                RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml,
                @"(<Textbox Name=""Textbox22"">[\s\S]*?<Top>)[^<]+(</Top>\s*<Left>)[^<]+(</Left>)",
                m => m.Groups[1].Value + "4.80cm" + m.Groups[2].Value + totalsLeft + m.Groups[3].Value,
                RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml,
                @"(<Textbox Name=""TextboxTotalBalance"">[\s\S]*?<Top>)[^<]+(</Top>\s*<Left>)[^<]+(</Left>)",
                m => m.Groups[1].Value + "5.60cm" + m.Groups[2].Value + totalsLeft + m.Groups[3].Value,
                RegexOptions.IgnoreCase);

            xml = Regex.Replace(xml, @"<PageHeight>[^<]+</PageHeight>", "<PageHeight>29.7cm</PageHeight>");
            xml = Regex.Replace(xml, @"<PageWidth>[^<]+</PageWidth>", "<PageWidth>21cm</PageWidth>");
            // Remove duplicate margin tags, then insert one clean set before </Page>
            xml = Regex.Replace(xml, @"\s*<LeftMargin>[^<]*</LeftMargin>", "", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, @"\s*<RightMargin>[^<]*</RightMargin>", "", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, @"\s*<TopMargin>[^<]*</TopMargin>", "", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, @"\s*<BottomMargin>[^<]*</BottomMargin>", "", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml,
                @"(<PageWidth>[^<]+</PageWidth>)(\s*)(<Style\s*/>\s*</Page>|<Style>\s*</Style>\s*</Page>|</Page>)",
                m => m.Groups[1].Value +
                     "\r\n    <LeftMargin>0.5cm</LeftMargin>" +
                     "\r\n    <RightMargin>0.5cm</RightMargin>" +
                     "\r\n    <TopMargin>0.5cm</TopMargin>" +
                     "\r\n    <BottomMargin>0.8cm</BottomMargin>" +
                     m.Groups[2].Value + m.Groups[3].Value,
                RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, @"(</ReportItems>\s*<Height>)[^<]+(</Height>\s*<Style />\s*</Body>)",
                m => m.Groups[1].Value + "7.20cm" + m.Groups[2].Value,
                RegexOptions.IgnoreCase);

            string tempPath = Path.Combine(Path.GetTempPath(), "New2Dealer_AutoSize_" + Guid.NewGuid().ToString("N") + ".rdlc");
            File.WriteAllText(tempPath, xml, Encoding.UTF8);
            return tempPath;
        }

        private static float[] MeasureColumnWidthsCm(DataTable dt)
        {
            string[] headers = { "S.N", "Entry Date", "Bank", "Vehicle", "Litter", "Rate", "Debit", "Credit", "Balance" };
            string[] fields = { null, "EntryDate", "Bank", "Vehicle", "Litter", "Rate", "Debit", "Credit", "Balance" };
            float[] mins = { 0.55f, 1.70f, 1.20f, 1.10f, 1.00f, 1.00f, 1.20f, 1.20f, 1.30f };
            float[] maxs = { 0.90f, 2.60f, 5.00f, 3.50f, 2.40f, 2.40f, 2.80f, 2.80f, 3.00f };
            float[] needed = new float[9];

            using (var bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var font = new Font("Segoe UI", 10f, FontStyle.Regular))
            using (var headerFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                for (int i = 0; i < 9; i++)
                {
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

            if (field == "Litter" || field == "Rate" || field == "Debit" || field == "Credit" || field == "Balance")
            {
                if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal n) ||
                    decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out n))
                    return n.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return (Convert.ToString(value) ?? "").Trim();
        }
    }
}
