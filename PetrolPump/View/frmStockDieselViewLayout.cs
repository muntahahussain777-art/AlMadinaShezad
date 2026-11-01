using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmStockDieselView
    {
        private void SetupStockActionLayout()
        {
            if (btnAdd != null)
                btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            panel1.Resize -= Panel1_ResizeActions;
            panel1.Resize += Panel1_ResizeActions;
            PositionTopActionButtons();
        }

        private void Panel1_ResizeActions(object sender, EventArgs e)
        {
            PositionTopActionButtons();
            PositionSummaryLabels();
        }

        private void PositionAddButton(int rightPad)
        {
            if (btnAdd == null || panel1 == null)
                return;

            const int addTop = 37;
            int x = panel1.ClientSize.Width - rightPad - btnAdd.Width;
            btnAdd.Location = new Point(Math.Max(12, x), addTop);
        }

        private void PositionTopActionButtons()
        {
            if (panel1 == null || btnAverage == null || dtpStart == null || dtpEnd == null)
                return;

            const int top = 12;
            const int gap = 8;
            const int rightPad = 12;
            const int minDateWidth = 130;

            PositionAddButton(rightPad);

            // Row-1 buttons must stay left of Add button column
            int rowRight = btnAdd != null
                ? btnAdd.Left - gap
                : panel1.ClientSize.Width - rightPad;

            int right = rowRight;

            if (_btnStockReport != null)
            {
                right -= _btnStockReport.Width;
                _btnStockReport.Location = new Point(Math.Max(gap, right), top);
                right -= gap;
            }

            if (_btnStockPdf != null)
            {
                right -= _btnStockPdf.Width;
                _btnStockPdf.Location = new Point(Math.Max(gap, right), top);
                right -= gap;
            }

            right -= btnAverage.Width;
            btnAverage.Location = new Point(Math.Max(gap, right), top);
            right -= gap;

            int comboRight = comboBox1 != null ? comboBox1.Right + gap : 260;
            int dateRight = btnAverage.Left - gap;
            int availableForDates = dateRight - comboRight;
            int dateWidth = Math.Max(minDateWidth, (availableForDates - gap) / 2);

            if (dateWidth * 2 + gap > availableForDates)
                dateWidth = Math.Max(minDateWidth, (availableForDates - gap) / 2);

            dtpStart.Width = dateWidth;
            dtpEnd.Width = dateWidth;
            dtpStart.Location = new Point(comboRight, top);
            dtpEnd.Location = new Point(dtpStart.Right + gap, top);

            if (dtpEnd.Right > dateRight)
            {
                int overflow = dtpEnd.Right - dateRight;
                dtpEnd.Width = Math.Max(minDateWidth, dtpEnd.Width - overflow);
                dtpStart.Width = Math.Max(minDateWidth, dtpStart.Width - overflow / 2);
                dtpStart.Location = new Point(comboRight, top);
                dtpEnd.Location = new Point(dtpStart.Right + gap, top);
            }

            btnAdd?.BringToFront();
            if (_btnStockPdf != null) _btnStockPdf.BringToFront();
            if (_btnStockReport != null) _btnStockReport.BringToFront();
            btnAverage.BringToFront();
        }
    }
}
