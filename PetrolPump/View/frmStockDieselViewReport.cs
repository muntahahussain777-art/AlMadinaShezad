using Guna.UI2.WinForms;
using System.Drawing;
using System.Windows.Forms;
using AlMadinaShahzad.ReportForm;

namespace AlMadinaShahzad.View
{
    public partial class frmStockDieselView
    {
        private Guna2Button _btnStockReport;

        private void SetupStockReportButton()
        {
            if (_btnStockReport != null) return;

            _btnStockReport = new Guna2Button
            {
                Text = "Report",
                AutoRoundedCorners = true,
                BorderRadius = 15,
                Size = new Size(95, 33),
                FillColor = Color.FromArgb(112, 51, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            _btnStockReport.Click += (s, e) => OpenStockRdlcReport();
            panel1.Controls.Add(_btnStockReport);
            _btnStockReport.BringToFront();
            PositionTopActionButtons();
        }

        private void OpenStockRdlcReport()
        {
            ReportWindowHelper.ShowOnce(() =>
            {
                var frm = new frmStockDieselForm();
                frm.Text = "Stock Details Report";
                frm.SetStockReportFilters(dtpStart.Value.Date, dtpEnd.Value.Date, txtSearch.Text ?? "");
                return frm;
            });
        }
    }
}
