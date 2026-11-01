using Guna.UI2.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.ReportForm;
using AlMadinaShahzad.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public class frmProfessionalHub : Sample
    {
        public frmProfessionalHub()
        {
            Text = "Professional Accounting";
            Size = new Size(700, 420);
            StartPosition = FormStartPosition.CenterScreen;

            AddBtn("Profit & Loss (P&L)", 30, Color.FromArgb(39, 174, 96),
                () => ReportWindowHelper.ShowOnce(() => new ProfitLossReportForm()));
            AddBtn("Balance Sheet", 100, Color.FromArgb(66, 133, 244),
                () => ReportWindowHelper.ShowOnce(() => new BalanceSheetReportForm()));
            AddBtn("Audit Trail", 170, Color.FromArgb(142, 68, 173),
                () => ReportWindowHelper.ShowOnce(() => new AuditTrailView()));
            AddBtn("Role Access", 240, Color.FromArgb(230, 126, 34),
                () => ReportWindowHelper.ShowOnce(() => new RoleAccessView()));

            ProfessionalFormHelper.AddCloseButton(this, 580, 15);
        }

        private void AddBtn(string text, int top, Color color, Action onClick)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Location = new Point(40, top),
                Size = new Size(600, 48),
                BorderRadius = 12,
                FillColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            btn.Click += (s, e) => onClick();
            Controls.Add(btn);
        }
    }
}
