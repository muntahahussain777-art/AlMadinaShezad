using Guna.UI2.WinForms;
using AlMadinaShahzad.ReportForm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    /// <summary>
    /// Closing 2 ka PDF export — Excel wala hi format, magar professional colored (VIP) design.
    /// Excel bilkul waise hi rehta hai (bina color).
    /// </summary>
    public partial class frmClosing2
    {
        private Guna2Button _btnPdf;

        private void SetupPdfButton()
        {
            if (_btnPdf != null)
                return;

            _btnPdf = new Guna2Button
            {
                Name = "btnPdf",
                Text = "PDF",
                AutoRoundedCorners = true,
                BorderRadius = 15,
                Size = new Size(95, 33),
                Location = new Point(btnExcel.Left + btnExcel.Width + 8, btnExcel.Top),
                FillColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            _btnPdf.Click += (s, e) => ExportToPdf();
            Controls.Add(_btnPdf);
            _btnPdf.BringToFront();
        }

        private void ExportToPdf()
        {
            try
            {
                if (!_dataLoaded)
                    CalculateProfitAndLoadSummary();

                if (!_dataLoaded)
                {
                    MessageBox.Show("Pehle date select karke Load karein.", "PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PDF File (*.pdf)|*.pdf";
                    sfd.Title = "PDF file save karein";
                    sfd.FileName = "Closing2_" + dtpStart.Value.ToString("yyyyMMdd") + "_" +
                                   dtpEnd.Value.ToString("yyyyMMdd") + ".pdf";

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    string subtitle = $"{dtpStart.Value:dd-MMM-yyyy}   to   {dtpEnd.Value:dd-MMM-yyyy}";
                    WritePdf(sfd.FileName, BuildClosingReportRows(), subtitle);

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
                ShowError("PDF export karte waqt error aaya.", ex);
            }
        }

        // ── Report rows — Excel wali tarteeb hi ──────────────────────────────────
        private enum PdfRowKind { Title, Section, Header, Normal, Bold, Empty }
        private enum PdfTone { None, Profit, Loss }

        private sealed class PdfReportRow
        {
            public PdfRowKind Kind;
            public string[] Cells;
            public PdfTone Tone;
        }

        private List<PdfReportRow> BuildClosingReportRows()
        {
            var rows = new List<PdfReportRow>();

            void Add(PdfRowKind kind, params string[] cells) =>
                rows.Add(new PdfReportRow { Kind = kind, Cells = cells });

            void AddToned(PdfRowKind kind, PdfTone tone, params string[] cells) =>
                rows.Add(new PdfReportRow { Kind = kind, Cells = cells, Tone = tone });

            string N0(decimal v) => v.ToString("#,##0", CultureInfo.InvariantCulture);
            string N3(decimal v) => v.ToString("#,##0.000", CultureInfo.InvariantCulture);

            Add(PdfRowKind.Section, "PROFIT CALCULATION");
            Add(PdfRowKind.Header, "Description", "Value", "");

            Add(PdfRowKind.Normal, "Customer Sale - Total Amount (Rs)", N0(_totalCustomerAmount), "");
            Add(PdfRowKind.Normal, "Customer Sale - Total Liters (L)", N0(_totalCustomerLitter), "");
            Add(PdfRowKind.Normal, "Customer Average Sale Rate (Rs/L)", N3(_customerAvgRate), "");
            Add(PdfRowKind.Normal, "Dealer Purchase - Total Amount (Rs)", N0(_totalValuePurchase), "");
            Add(PdfRowKind.Normal, "Dealer Purchase - Total Liters (L)", N0(_totalLitterPurchase), "");
            Add(PdfRowKind.Normal, "Dealer Average Purchase Rate (Rs/L)", N3(_dealerAvgRate), "");
            Add(PdfRowKind.Normal, "Margin per Liter  (Sale Avg - Dealer Avg)", N3(_rateDiff), "");
            Add(PdfRowKind.Normal, "Total Liters Sold (Profit Base)", N0(_totalCustomerLitter), "");
            Add(PdfRowKind.Normal, "Gross Profit  (Margin x Liters)", N0(_grossProfit), "");
            Add(PdfRowKind.Normal, "Total Expense", N0(_totalExpense), "");

            AddToned(PdfRowKind.Bold,
                _netProfit >= 0 ? PdfTone.Profit : PdfTone.Loss,
                _netProfit >= 0 ? "NET PROFIT" : "NET LOSS",
                N0(_netProfit),
                _netProfit >= 0 ? "Profit" : "Loss");

            Add(PdfRowKind.Empty);

            Add(PdfRowKind.Section, "CUSTOMER RECEIVABLE  /  DEALER PAYABLE  (Positive balances only)");
            Add(PdfRowKind.Header, "Customer Name", "Receivable (Rs)", "",
                "Dealer Name", "Payable (Rs)", "Notes");

            decimal totalReceivable = 0m;
            decimal totalPayable = 0m;

            if (_netProfit > 0)
            {
                totalPayable += _netProfit;
                Add(PdfRowKind.Normal, "", "", "", "Profit Dealer", N0(_netProfit), "Net profit to dealer");
            }
            else if (_netProfit < 0)
            {
                decimal lossAmt = Math.Abs(_netProfit);
                totalReceivable += lossAmt;
                Add(PdfRowKind.Normal, "Loss Customer Payable", N0(lossAmt), "", "", "", "Net loss to customer");
            }

            int customerCount = _customerCredits?.Rows.Count ?? 0;
            int dealerCount = _dealerList?.Rows.Count ?? 0;
            int pairRows = Math.Max(customerCount, dealerCount);

            for (int i = 0; i < pairRows; i++)
            {
                string custName = "";
                string custAmtText = "";
                string dealName = "";
                string dealAmtText = "";

                if (i < customerCount && _customerCredits != null)
                {
                    custName = Convert.ToString(_customerCredits.Rows[i]["CustomerName"]);
                    decimal amt = SafeToDecimal(_customerCredits.Rows[i]["CustomerReceivable"]);
                    custAmtText = N0(amt);
                    totalReceivable += amt;
                }
                if (i < dealerCount && _dealerList != null)
                {
                    dealName = Convert.ToString(_dealerList.Rows[i]["DealerName"]);
                    decimal amt = SafeToDecimal(_dealerList.Rows[i]["DealerAmount"]);
                    dealAmtText = N0(amt);
                    totalPayable += amt;
                }

                Add(PdfRowKind.Normal, custName, custAmtText, "", dealName, dealAmtText, "");
            }

            decimal balance = totalReceivable - totalPayable;
            AddToned(PdfRowKind.Bold, balance >= 0 ? PdfTone.Profit : PdfTone.Loss,
                "TOTAL RECEIVABLE", N0(totalReceivable), "",
                "TOTAL PAYABLE", N0(totalPayable), N0(balance));

            return rows;
        }

        // ── Minimal PDF writer (koi external library nahi) ───────────────────────
        private const float PdfPageWidth = 842f;   // A4 landscape
        private const float PdfPageHeight = 595f;
        private const float PdfMarginLeft = 32f;
        private const float PdfMarginRight = 32f;
        private const float PdfRowHeight = 16f;
        private const float PdfBannerHeight = 62f;
        private const float PdfFooterY = 30f;

        private const string ClrNavy = "0.098 0.153 0.318";
        private const string ClrNavyLight = "0.152 0.239 0.462";
        private const string ClrBlue = "0.180 0.459 0.714";
        private const string ClrGold = "0.831 0.686 0.216";
        private const string ClrZebra = "0.945 0.957 0.972";
        private const string ClrLine = "0.796 0.831 0.871";
        private const string ClrText = "0.129 0.145 0.180";
        private const string ClrWhite = "1 1 1";
        private const string ClrGreen = "0.106 0.541 0.286";
        private const string ClrRed = "0.698 0.114 0.153";
        private const string ClrMuted = "0.451 0.482 0.529";

        private static readonly float[] PdfColumnX = { 42f, 322f, 330f, 348f, 636f, 648f };
        private static readonly bool[] PdfColumnRightAligned = { false, true, false, false, true, false };

        private static void WritePdf(string filePath, List<PdfReportRow> rows, string subtitle)
        {
            var pages = new List<StringBuilder>();
            StringBuilder page = StartPdfPage(pages, subtitle);

            float tableWidth = PdfPageWidth - PdfMarginLeft - PdfMarginRight;
            float y = PdfPageHeight - PdfBannerHeight - 24f;
            PdfReportRow lastHeader = null;
            bool zebra = false;

            foreach (PdfReportRow row in rows)
            {
                if (y < PdfFooterY + 28f)
                {
                    page = StartPdfPage(pages, subtitle);
                    y = PdfPageHeight - PdfBannerHeight - 24f;
                    zebra = false;

                    if (lastHeader != null)
                    {
                        DrawPdfRow(page, lastHeader, y, tableWidth, false);
                        y -= PdfRowHeight;
                    }
                }

                if (row.Kind == PdfRowKind.Header)
                {
                    lastHeader = row;
                    zebra = false;
                }

                if (row.Kind == PdfRowKind.Empty)
                {
                    y -= PdfRowHeight * 0.7f;
                    continue;
                }

                if (row.Kind == PdfRowKind.Section)
                    y -= 6f;

                DrawPdfRow(page, row, y, tableWidth, row.Kind == PdfRowKind.Normal && zebra);

                if (row.Kind == PdfRowKind.Normal)
                    zebra = !zebra;

                y -= PdfRowHeight;
            }

            for (int i = 0; i < pages.Count; i++)
                DrawPdfFooter(pages[i], i + 1, pages.Count);

            File.WriteAllBytes(filePath, BuildPdfBytes(pages));
        }

        private static StringBuilder StartPdfPage(List<StringBuilder> pages, string subtitle)
        {
            var page = new StringBuilder();
            pages.Add(page);

            float w = PdfPageWidth;
            float top = PdfPageHeight;

            FillRect(page, 0f, top - PdfBannerHeight, w, PdfBannerHeight, ClrNavy);
            FillRect(page, 0f, top - PdfBannerHeight, w, 4f, ClrGold);

            AppendPdfText(page, "/F2", 15f, PdfMarginLeft + 10f, top - 24f,
                ReportHeaderBrandHelper.CurrentCompany, ClrWhite);
            AppendPdfText(page, "/F1", 10f, PdfMarginLeft + 10f, top - 46f,
                "Closing Statement", ClrGold);

            float subWidth = MeasurePdfText(subtitle, 11f);
            AppendPdfText(page, "/F2", 11f, w - PdfMarginRight - 10f - subWidth, top - 30f,
                subtitle, ClrWhite);

            string stamp = "Printed: " + DateTime.Now.ToString("dd-MMM-yyyy  hh:mm tt");
            float stampWidth = MeasurePdfText(stamp, 8.5f);
            AppendPdfText(page, "/F1", 8.5f, w - PdfMarginRight - 10f - stampWidth, top - 46f,
                stamp, ClrLine);

            return page;
        }

        private static void DrawPdfFooter(StringBuilder page, int pageNumber, int pageCount)
        {
            float w = PdfPageWidth;
            DrawLine(page, PdfMarginLeft, PdfFooterY + 14f, w - PdfMarginRight, PdfFooterY + 14f, ClrLine, 0.7f);
            FillRect(page, 0f, 0f, w, 6f, ClrNavy);

            AppendPdfText(page, "/F1", 8.5f, PdfMarginLeft, PdfFooterY,
                ReportHeaderBrandHelper.CurrentCompany + "  -  Closing 2 Statement", ClrMuted);

            string pageText = $"Page {pageNumber} of {pageCount}";
            float pw = MeasurePdfText(pageText, 8.5f);
            AppendPdfText(page, "/F2", 8.5f, w - PdfMarginRight - pw, PdfFooterY, pageText, ClrNavy);
        }

        private static void DrawPdfRow(StringBuilder page, PdfReportRow row, float y,
            float tableWidth, bool zebra)
        {
            if (row.Cells == null || row.Cells.Length == 0)
                return;

            float rowBottom = y - 4f;

            if (row.Kind == PdfRowKind.Section)
            {
                FillRect(page, PdfMarginLeft, rowBottom, tableWidth, PdfRowHeight, ClrNavyLight);
                FillRect(page, PdfMarginLeft, rowBottom, 4f, PdfRowHeight, ClrGold);
                AppendPdfText(page, "/F2", 10f, PdfMarginLeft + 12f, y, row.Cells[0], ClrWhite);
                return;
            }

            if (row.Kind == PdfRowKind.Header)
            {
                FillRect(page, PdfMarginLeft, rowBottom, tableWidth, PdfRowHeight, ClrBlue);
                DrawRowCells(page, row, y, "/F2", 9f, ClrWhite);
                return;
            }

            if (row.Kind == PdfRowKind.Bold)
            {
                FillRect(page, PdfMarginLeft, rowBottom, tableWidth, PdfRowHeight, ClrNavy);
                DrawRowCells(page, row, y, "/F2", 9.5f, ClrWhite,
                    row.Tone == PdfTone.Loss ? ClrGold : ClrWhite);
                return;
            }

            if (zebra)
                FillRect(page, PdfMarginLeft, rowBottom, tableWidth, PdfRowHeight, ClrZebra);

            DrawLine(page, PdfMarginLeft, rowBottom, PdfMarginLeft + tableWidth, rowBottom, ClrLine, 0.4f);

            string valueColor = row.Tone == PdfTone.Profit ? ClrGreen
                              : row.Tone == PdfTone.Loss ? ClrRed
                              : ClrText;

            DrawRowCells(page, row, y, "/F1", 9f, ClrText, valueColor);
        }

        private static void DrawRowCells(StringBuilder page, PdfReportRow row, float y,
            string font, float fontSize, string textColor, string valueColor = null)
        {
            for (int i = 0; i < row.Cells.Length && i < PdfColumnX.Length; i++)
            {
                string text = row.Cells[i];
                if (string.IsNullOrEmpty(text))
                    continue;

                bool isValueColumn = PdfColumnRightAligned[i];
                float x = PdfColumnX[i];
                if (isValueColumn)
                    x -= MeasurePdfText(text, fontSize);

                string color = isValueColumn && valueColor != null ? valueColor : textColor;
                string cellFont = isValueColumn ? "/F2" : font;

                AppendPdfText(page, cellFont, fontSize, x, y, text, color);
            }
        }

        private static void FillRect(StringBuilder page, float x, float y, float w, float h, string color)
        {
            page.Append(color).Append(" rg ")
                .Append(F(x)).Append(' ').Append(F(y)).Append(' ')
                .Append(F(w)).Append(' ').Append(F(h)).AppendLine(" re f");
        }

        private static void DrawLine(StringBuilder page, float x1, float y1, float x2, float y2,
            string color, float width)
        {
            page.Append(color).Append(" RG ").Append(F(width)).Append(" w ")
                .Append(F(x1)).Append(' ').Append(F(y1)).Append(" m ")
                .Append(F(x2)).Append(' ').Append(F(y2)).AppendLine(" l S");
        }

        private static void AppendPdfText(StringBuilder page, string font, float size,
            float x, float y, string text, string color)
        {
            page.Append(color).Append(" rg BT ").Append(font).Append(' ')
                .Append(F(size)).Append(" Tf 1 0 0 1 ")
                .Append(F(x)).Append(' ').Append(F(y)).Append(" Tm (")
                .Append(EscapePdfText(text)).AppendLine(") Tj ET");
        }

        private static string F(float value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        private static float MeasurePdfText(string text, float fontSize)
        {
            float units = 0f;
            foreach (char c in text ?? "")
            {
                if (char.IsDigit(c)) units += 556f;
                else if (c == ',' || c == '.' || c == ' ' || c == '\'') units += 278f;
                else if (c == '-') units += 333f;
                else if (char.IsUpper(c)) units += 667f;
                else units += 520f;
            }
            return units / 1000f * fontSize;
        }

        private static string EscapePdfText(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text ?? "")
            {
                if (c == '(' || c == ')' || c == '\\')
                    sb.Append('\\').Append(c);
                else if (c < 32 || c > 126)
                    sb.Append(' ');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static byte[] BuildPdfBytes(List<StringBuilder> pages)
        {
            var enc = Encoding.ASCII;
            int pageCount = pages.Count;

            int firstPageId = 3;
            int firstContentId = 3 + pageCount;
            int fontRegularId = 3 + 2 * pageCount;
            int fontBoldId = fontRegularId + 1;
            int totalObjects = fontBoldId;

            var offsets = new int[totalObjects + 1];
            var output = new StringBuilder();
            output.Append("%PDF-1.4\n");

            void AddObject(int id, string content)
            {
                offsets[id] = enc.GetByteCount(output.ToString());
                output.Append(id).Append(" 0 obj\n").Append(content).Append("\nendobj\n");
            }

            AddObject(1, "<< /Type /Catalog /Pages 2 0 R >>");

            var kids = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
                kids.Append(firstPageId + i).Append(" 0 R ");

            AddObject(2, $"<< /Type /Pages /Count {pageCount} /Kids [ {kids.ToString().Trim()} ] >>");

            string mediaBox = $"[ 0 0 {F(PdfPageWidth)} {F(PdfPageHeight)} ]";

            for (int i = 0; i < pageCount; i++)
            {
                AddObject(firstPageId + i,
                    $"<< /Type /Page /Parent 2 0 R /MediaBox {mediaBox} " +
                    $"/Resources << /Font << /F1 {fontRegularId} 0 R /F2 {fontBoldId} 0 R >> >> " +
                    $"/Contents {firstContentId + i} 0 R >>");
            }

            for (int i = 0; i < pageCount; i++)
            {
                string stream = pages[i].ToString();
                AddObject(firstContentId + i,
                    $"<< /Length {enc.GetByteCount(stream)} >>\nstream\n{stream}endstream");
            }

            AddObject(fontRegularId,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            AddObject(fontBoldId,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

            int xrefOffset = enc.GetByteCount(output.ToString());
            output.Append("xref\n0 ").Append(totalObjects + 1).Append('\n');
            output.Append("0000000000 65535 f \n");
            for (int id = 1; id <= totalObjects; id++)
                output.Append(offsets[id].ToString("D10")).Append(" 00000 n \n");

            output.Append("trailer\n<< /Size ").Append(totalObjects + 1)
                  .Append(" /Root 1 0 R >>\nstartxref\n")
                  .Append(xrefOffset).Append("\n%%EOF\n");

            return enc.GetBytes(output.ToString());
        }
    }
}
