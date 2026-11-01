using Guna.UI2.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AlMadinaShahzad.Model;
using AlMadinaShahzad.ReportForm;

namespace AlMadinaShahzad.View
{
    public partial class frmStockDieselView
    {
        private Guna2Button _btnStockPdf;

        private void SetupStockPdfButton()
        {
            if (_btnStockPdf != null) return;

            _btnStockPdf = new Guna2Button
            {
                Text = "PDF",
                AutoRoundedCorners = true,
                BorderRadius = 15,
                Size = new Size(95, 33),
                FillColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            _btnStockPdf.Click += (s, e) => ExportStockToPdf();
            panel1.Controls.Add(_btnStockPdf);
            _btnStockPdf.BringToFront();
            PositionTopActionButtons();
        }

        private void ExportStockToPdf()
        {
            try
            {
                DateTime startDate = dtpStart.Value.Date;
                DateTime endDate = dtpEnd.Value.Date;
                if (endDate < startDate)
                {
                    DateTime tmp = startDate;
                    startDate = endDate;
                    endDate = tmp;
                }

                DataTable dt = FetchStockPdfData(txtSearch.Text ?? "", startDate, endDate);
                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein koi stock entry nahi mili.", "PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PDF File (*.pdf)|*.pdf";
                    sfd.Title = "Stock PDF save karein";
                    sfd.FileName = "StockDetails_" + startDate.ToString("yyyyMMdd") + "_" +
                                   endDate.ToString("yyyyMMdd") + ".pdf";

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    string subtitle = $"{startDate:dd-MMM-yyyy}   to   {endDate:dd-MMM-yyyy}";
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                        subtitle += "   |   Filter: " + txtSearch.Text.Trim();

                    WriteStockPdf(sfd.FileName, BuildStockPdfRows(dt), subtitle);

                    MessageBox.Show("PDF file successfully create ho gayi.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF export karte waqt error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable FetchStockPdfData(string searchText, DateTime startDate, DateTime endDate)
        {
            searchText = (searchText ?? "").Trim();
            string qry = @"
SELECT StockDiesel.SID,
       StockDiesel.Date,
       IFNULL(AddDealer.DealerName, '') AS DealerName,
       IFNULL(StockDiesel.Vehicle, '') AS Vehicle,
       IFNULL(StockDiesel.Rate, 0) AS Rate,
       IFNULL(StockDiesel.Litter, 0) AS Litter,
       IFNULL(StockDiesel.Credit, 0) AS Credit,
       IFNULL(StockDiesel.Debit, 0) AS Debit,
       IFNULL(StockDiesel.Note, '') AS Note
FROM StockDiesel
LEFT JOIN AddDealer ON StockDiesel.SDid = AddDealer.Did
WHERE date(StockDiesel.Date) >= date(@StartDate)
  AND date(StockDiesel.Date) <= date(@EndDate)
  AND (
        @Search = ''
        OR IFNULL(AddDealer.DealerName, '') LIKE @SearchLike
        OR IFNULL(StockDiesel.Vehicle, '') LIKE @SearchLike
      )
ORDER BY StockDiesel.Date ASC, StockDiesel.SID ASC";

            var ht = new Hashtable
            {
                { "@StartDate", startDate.ToString("yyyy-MM-dd") },
                { "@EndDate", endDate.ToString("yyyy-MM-dd") },
                { "@Search", searchText },
                { "@SearchLike", "%" + searchText + "%" }
            };

            return MainClass.ExecuteSelectQuery(qry, ht);
        }

        private enum StockPdfRowKind { Section, Header, Normal, Bold, Empty }
        private enum StockPdfTone { None, Minus, Total }

        private sealed class StockPdfRow
        {
            public StockPdfRowKind Kind;
            public string[] Cells;
            public StockPdfTone Tone;
        }

        private List<StockPdfRow> BuildStockPdfRows(DataTable dt)
        {
            var rows = new List<StockPdfRow>();
            string N0(decimal v) => v.ToString("N0", CultureInfo.InvariantCulture);
            string N2(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);

            void Add(StockPdfRowKind kind, params string[] cells) =>
                rows.Add(new StockPdfRow { Kind = kind, Cells = cells, Tone = StockPdfTone.None });

            void AddToned(StockPdfRowKind kind, StockPdfTone tone, params string[] cells) =>
                rows.Add(new StockPdfRow { Kind = kind, Cells = cells, Tone = tone });

            decimal addLitter = 0, minusLitter = 0, addAmount = 0, minusAmount = 0;
            decimal totalCredit = 0, totalDebit = 0;
            int sr = 0;

            Add(StockPdfRowKind.Section, "OVERALL SUMMARY");
            Add(StockPdfRowKind.Header, "Description", "Value", "", "", "", "", "", "", "", "");

            foreach (DataRow row in dt.Rows)
            {
                decimal litter = Convert.ToDecimal(row["Litter"]);
                decimal rate = Convert.ToDecimal(row["Rate"]);
                string note = row["Note"]?.ToString() ?? "";
                bool isMinus = StockDieselLitterHelper.IsMinusEntry(note, litter);
                decimal displayLitter = StockDieselLitterHelper.GetDisplayLitter(litter, note);
                decimal amount = displayLitter * rate;

                if (isMinus) { minusLitter += displayLitter; minusAmount += amount; }
                else { addLitter += displayLitter; addAmount += amount; }

                totalCredit += Convert.ToDecimal(row["Credit"]);
                totalDebit += Convert.ToDecimal(row["Debit"]);
            }

            decimal baqaya = addLitter - minusLitter;
            decimal netAmount = addAmount - minusAmount;

            Add(StockPdfRowKind.Normal, "Add Litter (L)", N2(addLitter), "", "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Normal, "Minus Litter (L)", N2(minusLitter), "", "", "", "", "", "", "", "");
            AddToned(StockPdfRowKind.Bold, StockPdfTone.Total, "Baqaya Litter (L)", N2(baqaya), "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Empty);
            Add(StockPdfRowKind.Normal, "Add Amount (Rs)", N0(addAmount), "", "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Normal, "Minus Amount (Rs)", N0(minusAmount), "", "", "", "", "", "", "", "");
            AddToned(StockPdfRowKind.Bold, StockPdfTone.Total, "Total Amount (Rs)", N0(netAmount), "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Normal, "Total Credit (Rs)", N0(totalCredit), "", "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Normal, "Total Debit (Rs)", N0(totalDebit), "", "", "", "", "", "", "", "");
            Add(StockPdfRowKind.Normal, "Balance (Debit - Credit)", N0(totalDebit - totalCredit), "", "", "", "", "", "", "", "");

            Add(StockPdfRowKind.Empty);
            Add(StockPdfRowKind.Section, "STOCK DETAIL ENTRIES");
            Add(StockPdfRowKind.Header,
                "Sr", "Date", "Dealer", "Vehicle", "Type", "Ltr", "Rate", "Amount", "Credit", "Debit", "Note");

            foreach (DataRow row in dt.Rows)
            {
                sr++;
                decimal litter = Convert.ToDecimal(row["Litter"]);
                decimal rate = Convert.ToDecimal(row["Rate"]);
                string note = StockDieselLitterHelper.StripMinusTag(row["Note"]?.ToString() ?? "");
                bool isMinus = StockDieselLitterHelper.IsMinusEntry(row["Note"]?.ToString(), litter);
                decimal displayLitter = StockDieselLitterHelper.GetDisplayLitter(litter, row["Note"]?.ToString());
                decimal amount = displayLitter * rate;
                DateTime dtVal = Convert.ToDateTime(row["Date"]);

                AddToned(StockPdfRowKind.Normal, isMinus ? StockPdfTone.Minus : StockPdfTone.None,
                    sr.ToString(),
                    dtVal.ToString("dd-MMM-yy"),
                    row["DealerName"]?.ToString() ?? "",
                    row["Vehicle"]?.ToString() ?? "",
                    isMinus ? "Minus" : "Add",
                    N2(displayLitter),
                    N2(rate),
                    N0(amount),
                    N0(Convert.ToDecimal(row["Credit"])),
                    N0(Convert.ToDecimal(row["Debit"])),
                    note);
            }

            return rows;
        }

        private sealed class SpColDef
        {
            public float Left;
            public float Width;
            public bool RightAlign;
        }

        // Landscape A4: 842pt wide, 26pt margins => 790pt table
        private static readonly SpColDef[] SpCols =
        {
            new SpColDef { Left = 26f,  Width = 24f,  RightAlign = false }, // Sr
            new SpColDef { Left = 50f,  Width = 56f,  RightAlign = false }, // Date
            new SpColDef { Left = 106f, Width = 92f,  RightAlign = false }, // Dealer
            new SpColDef { Left = 198f, Width = 68f,  RightAlign = false }, // Vehicle
            new SpColDef { Left = 266f, Width = 34f,  RightAlign = true  }, // Type
            new SpColDef { Left = 300f, Width = 44f,  RightAlign = true  }, // Ltr
            new SpColDef { Left = 344f, Width = 44f,  RightAlign = true  }, // Rate
            new SpColDef { Left = 388f, Width = 54f,  RightAlign = true  }, // Amount
            new SpColDef { Left = 442f, Width = 50f,  RightAlign = true  }, // Credit
            new SpColDef { Left = 492f, Width = 50f,  RightAlign = true  }, // Debit
            new SpColDef { Left = 542f, Width = 248f, RightAlign = false }  // Note
        };

        // ── VIP PDF writer (Stock Details) ─────────────────────────────────────
        private const float SpPageW = 842f;
        private const float SpPageH = 595f;
        private const float SpMargin = 26f;
        private const float SpRowH = 13f;
        private const float SpBannerH = 58f;
        private const float SpFooterY = 28f;

        private const string SpNavy = "0.098 0.153 0.318";
        private const string SpNavyLt = "0.152 0.239 0.462";
        private const string SpBlue = "0.180 0.459 0.714";
        private const string SpGold = "0.831 0.686 0.216";
        private const string SpZebra = "0.945 0.957 0.972";
        private const string SpLine = "0.796 0.831 0.871";
        private const string SpText = "0.129 0.145 0.180";
        private const string SpWhite = "1 1 1";
        private const string SpRed = "0.698 0.114 0.153";
        private const string SpGreen = "0.106 0.541 0.286";
        private const string SpMuted = "0.451 0.482 0.529";

        private static void WriteStockPdf(string filePath, List<StockPdfRow> rows, string subtitle)
        {
            var pages = new List<StringBuilder>();
            StringBuilder page = StartStockPdfPage(pages, subtitle);
            float tableW = SpPageW - SpMargin * 2f;
            float y = SpPageH - SpBannerH - 20f;
            StockPdfRow lastHeader = null;
            bool zebra = false;

            foreach (StockPdfRow row in rows)
            {
                if (y < SpFooterY + 24f)
                {
                    page = StartStockPdfPage(pages, subtitle);
                    y = SpPageH - SpBannerH - 20f;
                    zebra = false;
                    if (lastHeader != null)
                    {
                        DrawStockPdfRow(page, lastHeader, y, tableW, false);
                        y -= SpRowH;
                    }
                }

                if (row.Kind == StockPdfRowKind.Header)
                {
                    lastHeader = row;
                    zebra = false;
                }

                if (row.Kind == StockPdfRowKind.Empty)
                {
                    y -= SpRowH * 0.65f;
                    continue;
                }

                if (row.Kind == StockPdfRowKind.Section)
                    y -= 5f;

                DrawStockPdfRow(page, row, y, tableW, row.Kind == StockPdfRowKind.Normal && zebra);
                if (row.Kind == StockPdfRowKind.Normal)
                    zebra = !zebra;
                y -= SpRowH;
            }

            for (int i = 0; i < pages.Count; i++)
                DrawStockPdfFooter(pages[i], i + 1, pages.Count);

            File.WriteAllBytes(filePath, BuildStockPdfBytes(pages));
        }

        private static StringBuilder StartStockPdfPage(List<StringBuilder> pages, string subtitle)
        {
            var page = new StringBuilder();
            pages.Add(page);
            float w = SpPageW;
            float top = SpPageH;

            FillSpRect(page, 0f, top - SpBannerH, w, SpBannerH, SpNavy);
            FillSpRect(page, 0f, top - SpBannerH, w, 4f, SpGold);
            AppendSpText(page, "/F2", 14f, SpMargin + 8f, top - 22f, ReportHeaderBrandHelper.CurrentCompany, SpWhite);
            AppendSpText(page, "/F1", 10f, SpMargin + 8f, top - 44f, "Stock Details Report", SpGold);

            string sub = FitSpText(subtitle, w - SpMargin * 2f - 16f, 9f);
            float sw = MeasureSpText(sub, 10f);
            AppendSpText(page, "/F2", 10f, w - SpMargin - 8f - sw, top - 28f, sub, SpWhite);

            string stamp = "Printed: " + DateTime.Now.ToString("dd-MMM-yyyy  hh:mm tt");
            float tw = MeasureSpText(stamp, 8f);
            AppendSpText(page, "/F1", 8f, w - SpMargin - 8f - tw, top - 44f, stamp, SpLine);

            return page;
        }

        private static void DrawStockPdfFooter(StringBuilder page, int pageNum, int pageCount)
        {
            float w = SpPageW;
            DrawSpLine(page, SpMargin, SpFooterY + 12f, w - SpMargin, SpFooterY + 12f, SpLine, 0.6f);
            FillSpRect(page, 0f, 0f, w, 5f, SpNavy);
            AppendSpText(page, "/F1", 8f, SpMargin, SpFooterY,
                ReportHeaderBrandHelper.CurrentCompany + "  -  Stock Details", SpMuted);
            string pt = $"Page {pageNum} of {pageCount}";
            AppendSpText(page, "/F2", 8f, w - SpMargin - MeasureSpText(pt, 8f), SpFooterY, pt, SpNavy);
        }

        private static void DrawStockPdfRow(StringBuilder page, StockPdfRow row, float y, float tableW, bool zebra)
        {
            if (row.Cells == null || row.Cells.Length == 0) return;
            float bottom = y - 3f;

            if (row.Kind == StockPdfRowKind.Section)
            {
                FillSpRect(page, SpMargin, bottom, tableW, SpRowH, SpNavyLt);
                FillSpRect(page, SpMargin, bottom, 3f, SpRowH, SpGold);
                AppendSpText(page, "/F2", 9.5f, SpMargin + 10f, y, row.Cells[0], SpWhite);
                return;
            }

            if (row.Kind == StockPdfRowKind.Header)
            {
                FillSpRect(page, SpMargin, bottom, tableW, SpRowH, SpBlue);
                DrawStockCells(page, row, y, "/F2", 7.2f, SpWhite);
                return;
            }

            if (row.Kind == StockPdfRowKind.Bold)
            {
                FillSpRect(page, SpMargin, bottom, tableW, SpRowH, SpNavy);
                string valColor = row.Tone == StockPdfTone.Total ? SpGold : SpWhite;
                DrawStockCells(page, row, y, "/F2", 7.5f, SpWhite, valColor);
                return;
            }

            if (zebra)
                FillSpRect(page, SpMargin, bottom, tableW, SpRowH, SpZebra);

            DrawSpLine(page, SpMargin, bottom, SpMargin + tableW, bottom, SpLine, 0.35f);

            string accent = row.Tone == StockPdfTone.Minus ? SpRed : SpText;
            DrawStockCells(page, row, y, "/F1", 6.8f, SpText, accent);
        }

        private static string FitSpText(string text, float maxWidth, float size)
        {
            text = (text ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (maxWidth <= 4f) return string.Empty;
            if (MeasureSpText(text, size) <= maxWidth) return text;

            const string suffix = "..";
            float suffixW = MeasureSpText(suffix, size);
            int lo = 0;
            int hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                string candidate = text.Substring(0, mid) + suffix;
                if (MeasureSpText(candidate, size) <= maxWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            return lo <= 0 ? suffix : text.Substring(0, lo) + suffix;
        }

        private static void DrawStockCells(StringBuilder page, StockPdfRow row, float y,
            string font, float size, string textColor, string valueColor = null)
        {
            const float pad = 2.5f;

            for (int i = 0; i < row.Cells.Length && i < SpCols.Length; i++)
            {
                string text = row.Cells[i];
                if (string.IsNullOrEmpty(text)) continue;

                SpColDef col = SpCols[i];
                float maxW = col.Width - pad * 2f;
                text = FitSpText(text, maxW, size);
                if (string.IsNullOrEmpty(text)) continue;

                float textW = MeasureSpText(text, size);
                float x = col.RightAlign
                    ? col.Left + col.Width - pad - textW
                    : col.Left + pad;

                bool isNum = col.RightAlign;
                string color = isNum && valueColor != null ? valueColor : textColor;
                if (row.Tone == StockPdfTone.Minus && (i == 4 || i == 5 || i == 7))
                    color = SpRed;

                AppendSpText(page, isNum ? "/F2" : font, size, x, y, text, color);
            }
        }

        private static void FillSpRect(StringBuilder p, float x, float y, float w, float h, string c) =>
            p.Append(c).Append(" rg ").Append(Fs(x)).Append(' ').Append(Fs(y)).Append(' ')
             .Append(Fs(w)).Append(' ').Append(Fs(h)).AppendLine(" re f");

        private static void DrawSpLine(StringBuilder p, float x1, float y1, float x2, float y2, string c, float w) =>
            p.Append(c).Append(" RG ").Append(Fs(w)).Append(" w ")
             .Append(Fs(x1)).Append(' ').Append(Fs(y1)).Append(" m ")
             .Append(Fs(x2)).Append(' ').Append(Fs(y2)).AppendLine(" l S");

        private static void AppendSpText(StringBuilder p, string font, float size,
            float x, float y, string text, string color) =>
            p.Append(color).Append(" rg BT ").Append(font).Append(' ')
             .Append(Fs(size)).Append(" Tf 1 0 0 1 ")
             .Append(Fs(x)).Append(' ').Append(Fs(y)).Append(" Tm (")
             .Append(EscapeSpText(text)).AppendLine(") Tj ET");

        private static string Fs(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static float MeasureSpText(string text, float size)
        {
            float units = 0f;
            foreach (char c in text ?? "")
            {
                if (char.IsDigit(c)) units += 556f;
                else if (c == ',' || c == '.' || c == ' ') units += 278f;
                else if (char.IsUpper(c)) units += 667f;
                else units += 520f;
            }
            return units / 1000f * size;
        }

        private static string EscapeSpText(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text ?? "")
            {
                if (c == '(' || c == ')' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 32 || c > 126) sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static byte[] BuildStockPdfBytes(List<StringBuilder> pages)
        {
            var enc = Encoding.ASCII;
            int n = pages.Count;
            int pageId = 3, contentId = 3 + n, fontR = 3 + 2 * n, fontB = fontR + 1;
            int total = fontB;
            var offsets = new int[total + 1];
            var output = new StringBuilder("%PDF-1.4\n");

            void AddObj(int id, string body)
            {
                offsets[id] = enc.GetByteCount(output.ToString());
                output.Append(id).Append(" 0 obj\n").Append(body).Append("\nendobj\n");
            }

            AddObj(1, "<< /Type /Catalog /Pages 2 0 R >>");
            var kids = new StringBuilder();
            for (int i = 0; i < n; i++) kids.Append(pageId + i).Append(" 0 R ");
            AddObj(2, $"<< /Type /Pages /Count {n} /Kids [ {kids.ToString().Trim()} ] >>");

            string box = $"[ 0 0 {Fs(SpPageW)} {Fs(SpPageH)} ]";
            for (int i = 0; i < n; i++)
                AddObj(pageId + i,
                    $"<< /Type /Page /Parent 2 0 R /MediaBox {box} " +
                    $"/Resources << /Font << /F1 {fontR} 0 R /F2 {fontB} 0 R >> >> " +
                    $"/Contents {contentId + i} 0 R >>");

            for (int i = 0; i < n; i++)
            {
                string stream = pages[i].ToString();
                AddObj(contentId + i, $"<< /Length {enc.GetByteCount(stream)} >>\nstream\n{stream}endstream");
            }

            AddObj(fontR, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            AddObj(fontB, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

            int xref = enc.GetByteCount(output.ToString());
            output.Append("xref\n0 ").Append(total + 1).Append('\n').Append("0000000000 65535 f \n");
            for (int id = 1; id <= total; id++)
                output.Append(offsets[id].ToString("D10")).Append(" 00000 n \n");
            output.Append("trailer\n<< /Size ").Append(total + 1)
                  .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
            return enc.GetBytes(output.ToString());
        }
    }
}
