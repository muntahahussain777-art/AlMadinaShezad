using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Model;

namespace AlMadinaShahzad.View
{
    public partial class frmStockDieselView
    {
        private void SetupStockGridFormatting()
        {
            SetupStockSummaryLayout();
            guna2DataGridView1.CellFormatting -= StockGrid_CellFormatting;
            guna2DataGridView1.CellFormatting += StockGrid_CellFormatting;
        }

        private void SetupStockSummaryLayout()
        {
            panel1.Resize -= Panel1_ResizeSummary;
            panel1.Resize += Panel1_ResizeSummary;

            lblTotalLitter.AutoSize = false;
            lblTotalAmount.AutoSize = false;
            lblTotalLitter.TextAlign = ContentAlignment.MiddleRight;
            lblTotalAmount.TextAlign = ContentAlignment.MiddleRight;
            lblTotalLitter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTotalAmount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTotalLitter.Font = new Font("Segoe UI Semibold", 10.25F, FontStyle.Bold);
            lblTotalAmount.Font = new Font("Segoe UI Semibold", 10.25F, FontStyle.Bold);

            PositionSummaryLabels();
        }

        private void Panel1_ResizeSummary(object sender, EventArgs e)
        {
            PositionSummaryLabels();
        }

        private void PositionSummaryLabels()
        {
            if (panel1 == null || lblTotalLitter == null || lblTotalAmount == null)
                return;

            const int rightGap = 118;
            const int top = 86;
            const int lineHeight = 22;
            const int sidePad = 8;

            int labelWidth = Math.Max(420, panel1.ClientSize.Width - rightGap - sidePad - 240);
            int x = panel1.ClientSize.Width - labelWidth - rightGap;

            lblTotalLitter.Size = new Size(labelWidth, lineHeight);
            lblTotalAmount.Size = new Size(labelWidth, lineHeight);
            lblTotalLitter.Location = new Point(Math.Max(sidePad, x), top);
            lblTotalAmount.Location = new Point(Math.Max(sidePad, x), top + lineHeight + 2);

            lblTotalLitter.BringToFront();
            lblTotalAmount.BringToFront();
        }

        private void StockGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = guna2DataGridView1.Columns[e.ColumnIndex].Name;
            var row = guna2DataGridView1.Rows[e.RowIndex];

            if (colName == "Amount" && e.Value != null && e.Value != DBNull.Value)
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal amount))
                {
                    e.Value = amount.ToString("N0", CultureInfo.InvariantCulture);
                    e.FormattingApplied = true;
                }
            }

            if (!guna2DataGridView1.Columns.Contains("EntryType")) return;
            object entryTypeObj = row.Cells["EntryType"].Value;
            if (entryTypeObj == null || entryTypeObj == DBNull.Value) return;

            bool isMinus = entryTypeObj.ToString().IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isMinus) return;

            Font baseFont = guna2DataGridView1.DefaultCellStyle.Font ?? guna2DataGridView1.Font;
            var minusFont = new Font(baseFont.FontFamily, baseFont.Size + 1.5f, FontStyle.Bold);

            if (colName == "EntryType" || colName == "Litter" || colName == "Amount")
            {
                e.CellStyle.ForeColor = Color.FromArgb(255, 120, 120);
                e.CellStyle.Font = minusFont;
            }
        }

        private void UpdateStockSummaryLabels(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (guna2DataGridView1.DataSource == null)
            {
                lblTotalLitter.Text = "Add Litter: 0.00 L | Minus Litter: 0.00 L | Baqaya: 0.00 L";
                lblTotalAmount.Text = "Add Amount: Rs 0 | Minus Amount: Rs 0 | Total: Rs 0";
                PositionSummaryLabels();
                return;
            }

            decimal addLitter = 0;
            decimal minusLitter = 0;
            decimal addAmount = 0;
            decimal minusAmount = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["SID"].Value == null || row.Cells["SID"].Value == DBNull.Value)
                    continue;

                if (startDate.HasValue && endDate.HasValue &&
                    guna2DataGridView1.Columns.Contains("Date") &&
                    row.Cells["Date"].Value != null && row.Cells["Date"].Value != DBNull.Value)
                {
                    if (DateTime.TryParse(row.Cells["Date"].Value.ToString(), out DateTime rowDate))
                    {
                        rowDate = rowDate.Date;
                        if (rowDate < startDate.Value.Date || rowDate > endDate.Value.Date)
                            continue;
                    }
                }

                if (row.Cells["Litter"].Value == null || row.Cells["Litter"].Value == DBNull.Value)
                    continue;

                decimal displayLitter = Convert.ToDecimal(row.Cells["Litter"].Value);
                string entryType = row.Cells["EntryType"].Value?.ToString() ?? "";
                bool isMinus = entryType.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0;

                decimal amount = 0;
                if (row.Cells["Amount"].Value != null && row.Cells["Amount"].Value != DBNull.Value)
                    amount = Math.Abs(Convert.ToDecimal(row.Cells["Amount"].Value));

                if (isMinus)
                {
                    minusLitter += displayLitter;
                    minusAmount += amount;
                }
                else
                {
                    addLitter += displayLitter;
                    addAmount += amount;
                }
            }

            decimal baqayaLitter = addLitter - minusLitter;
            decimal netAmount = addAmount - minusAmount;

            lblTotalLitter.Text =
                $"Add Litter: {FormatLit(addLitter)} L | Minus Litter: {FormatLit(minusLitter)} L | Baqaya: {FormatLit(baqayaLitter)} L";

            lblTotalAmount.Text =
                $"Add Amount: Rs {FormatRs(addAmount)} | Minus Amount: Rs {FormatRs(minusAmount)} | Total: Rs {FormatRs(netAmount)}";

            PositionSummaryLabels();
        }

        private static string FormatRs(decimal value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string FormatLit(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}
