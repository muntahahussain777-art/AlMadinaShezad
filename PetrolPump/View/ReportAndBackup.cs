using HajiBalochSoftwere.ReportForm;
using AlMadinaShahzad.ReportForm;
using AlMadinaShahzad.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class ReportAndBackup : Sample
    {
        private const int PanelPadding = 10;
        private const int LitterButtonTop = 560;

        public ReportAndBackup()
        {
            InitializeComponent();
            Load += ReportAndBackup_Load;
            Shown += ReportAndBackup_Shown;
        }

        private void ReportAndBackup_Load(object sender, EventArgs e)
        {
            var btnPro = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Professional Accounting (P&L / Balance / Audit / Roles)",
                Size = new System.Drawing.Size(420, 43),
                Location = new System.Drawing.Point(15, 70),
                BorderRadius = 20,
                FillColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold)
            };
            btnPro.Click += (s, ev) => ReportWindowHelper.ShowOnce(() => new frmProfessionalHub());
            groupBox2.Controls.Add(btnPro);
            btnCustomer.Location = new System.Drawing.Point(450, 70);

            var btnLitterCompare = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Litter Compare Report",
                Size = new System.Drawing.Size(243, 43),
                Location = new System.Drawing.Point(377, LitterButtonTop),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BorderRadius = 20,
                FillColor = System.Drawing.Color.FromArgb(26, 39, 68),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold)
            };
            btnLitterCompare.Click += (s, ev) => ReportWindowHelper.ShowOnce(() => new frmLitterCompareReport());
            groupBox1.Controls.Add(btnLitterCompare);
            btnLitterCompare.BringToFront();

            var btnHeaderNames = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Report Header Names",
                Size = new System.Drawing.Size(220, 43),
                Location = new System.Drawing.Point(15, LitterButtonTop),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BorderRadius = 20,
                FillColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold)
            };
            btnHeaderNames.Click += (s, ev) =>
            {
                using (var frm = new frmReportHeaderNames())
                    frm.ShowDialog(this);
            };
            groupBox1.Controls.Add(btnHeaderNames);
            btnHeaderNames.BringToFront();
        }

        private void ReportAndBackup_Shown(object sender, EventArgs e)
        {
            ApplyReportPanelLayout();
            SizeChanged += ReportAndBackup_SizeChanged;
        }

        private void ReportAndBackup_SizeChanged(object sender, EventArgs e)
        {
            ApplyReportPanelLayout();
        }

        /// <summary>
        /// Dono group panels ko content area ki full width par rakhta hai (sidebar se right tak).
        /// </summary>
        private void ApplyReportPanelLayout()
        {
            if (groupBox1 == null || groupBox2 == null)
                return;

            int width = ClientSize.Width - (PanelPadding * 2);
            if (width < 200 || ClientSize.Height < 120)
                return;

            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            groupBox1.Left = PanelPadding;
            groupBox1.Width = width;

            groupBox2.Left = PanelPadding;
            groupBox2.Width = width;

            int minReportingHeight = LitterButtonTop + 55;
            if (groupBox1.Height < minReportingHeight)
            {
                int extra = minReportingHeight - groupBox1.Height;
                groupBox1.Height = minReportingHeight;
                groupBox2.Top += extra;
            }

            if (groupBox2.Top < groupBox1.Bottom + 6)
                groupBox2.Top = groupBox1.Bottom + 6;

            // Vertical scroll when buttons/backup section screen se neeche cut ho
            AutoScroll = true;
            int contentBottom = Math.Max(groupBox2.Bottom, groupBox1.Bottom) + PanelPadding;
            AutoScrollMinSize = new Size(0, contentBottom);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new transctionRdlc());
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new TwoDateReport());
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new frmReportButton());
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            BackupRestoreForm frm = new BackupRestoreForm();
            frm.ShowDialog();
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new AttendanceReport());
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new NameReportButton());
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new DealerFormReport());
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button6_Click_1(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new frmDealerReport());
        }

        private void guna2Button7_Click_1(object sender, EventArgs e)
        {

        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new frmStockDieselForm());
        }

        private void guna2Button7_Click_2(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new TwoTableJoiningReport());
        }

        private void CustomerBalanceCredit_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new DealerCreditBalance());
        }

        private void btndealerBalanceCredit_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new Customer_Balance_Credit());
        }

        private void CustomerProfitLost_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new CustomerProfitOrLostPercentange());
        }

        private void DealerProfitLost_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new Dealer_percentage_Balance_Credit());
        }

        private void BenifitsSell_Buy_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new BenitsSellBuyFormReport());
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new completeReportsAvergePercentageBenifits());
        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new CurrentDateALlEntryForm());
        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new OtherStockLitter());
        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new twotabledifference());
        }

        private void guna2Button13_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new BankFormCustomerDealer());
        }

        private void guna2Button14_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new stocklittertotaldiffernceform());
        }

        private void guna2Button15_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new seprate2table());
        }

        private void guna2Button16_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new DealerCompletePackageForm());
        }

        private void guna2Button17_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new DealerCompleteGroupWiseForm());
        }

        private void completeDealerAmount_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new NewDealerAMountFormCreate());
        }

        private void guna2Button18_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new New2DelearFormComplete());
        }

        private void StockTrack_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new StockReportFormPurchaseSaleForm());
        }

        private void guna2Button19_Click(object sender, EventArgs e)
        {
            ReportWindowHelper.ShowOnce(() => new firnFormReportStock());
        }
    }
}
