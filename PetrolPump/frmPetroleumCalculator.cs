using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad
{
    public partial class frmPetroleumCalculator : Form
    {
        private static readonly Color Gold = Color.FromArgb(212, 175, 55);
        private static readonly Color GoldLight = Color.FromArgb(255, 215, 100);
        private static readonly Color DarkBg = Color.FromArgb(13, 13, 26);
        private static readonly Color PanelBg = Color.FromArgb(26, 26, 46);

        public frmPetroleumCalculator()
        {
            InitializeComponent();
        }

        private void frmPetroleumCalculator_Load(object sender, EventArgs e)
        {
            WireNumericFields();
            RecalcSale();
            RecalcProfit();
            RecalcPercent();
            RecalcAverage();
            txtFastCalc.Focus();
        }

        private void WireNumericFields()
        {
            foreach (var tb in new[]
            {
                txtLiters, txtRate, txtAdvance, txtCredit,
                txtSaleRate, txtPurchaseRate, txtProfitLiters, txtExpense,
                txtDebit, txtCreditPct,
                txtAvgLiters, txtAvgRate
            })
            {
                tb.TextChanged += (s, ev) => OnFieldChanged(tb);
                tb.Enter += Numeric_Enter;
                tb.KeyDown += Numeric_KeyDown;
            }
        }

        private void OnFieldChanged(TextBox source)
        {
            if (source == txtLiters || source == txtRate || source == txtAdvance || source == txtCredit)
                RecalcSale();
            else if (source == txtSaleRate || source == txtPurchaseRate || source == txtProfitLiters || source == txtExpense)
                RecalcProfit();
            else if (source == txtDebit || source == txtCreditPct)
                RecalcPercent();
            else if (source == txtAvgLiters || source == txtAvgRate)
                RecalcAverage();
        }

        private void Numeric_Enter(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }

        private void Numeric_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        private void RecalcSale()
        {
            decimal liters = PetroleumCalculatorEngine.Parse(txtLiters.Text);
            decimal rate = PetroleumCalculatorEngine.Parse(txtRate.Text);
            decimal advance = PetroleumCalculatorEngine.Parse(txtAdvance.Text);
            decimal credit = PetroleumCalculatorEngine.Parse(txtCredit.Text);

            decimal amount = PetroleumCalculatorEngine.SaleAmount(liters, rate, advance);
            decimal balance = PetroleumCalculatorEngine.Balance(amount, credit);

            lblSaleAmount.Text = PetroleumCalculatorEngine.Format(amount);
            lblSaleBalance.Text = PetroleumCalculatorEngine.Format(balance);
            lblSaleHint.Text = liters > 0 && rate > 0
                ? $"{PetroleumCalculatorEngine.Format(liters)} L × {PetroleumCalculatorEngine.Format(rate)} = {PetroleumCalculatorEngine.Format(liters * rate)}"
                : "Liters × Rate + Advance − Credit";
        }

        private void RecalcProfit()
        {
            decimal sale = PetroleumCalculatorEngine.Parse(txtSaleRate.Text);
            decimal purchase = PetroleumCalculatorEngine.Parse(txtPurchaseRate.Text);
            decimal liters = PetroleumCalculatorEngine.Parse(txtProfitLiters.Text);
            decimal expense = PetroleumCalculatorEngine.Parse(txtExpense.Text);

            decimal rateDiff = PetroleumCalculatorEngine.RateDifference(sale, purchase);
            decimal gross = PetroleumCalculatorEngine.GrossProfit(sale, purchase, liters);
            decimal net = PetroleumCalculatorEngine.NetProfit(gross, expense);
            decimal revenue = sale * liters;
            decimal pct = PetroleumCalculatorEngine.ProfitPercent(net, revenue);
            decimal margin = PetroleumCalculatorEngine.MarginPercent(sale, purchase);

            lblRateDiff.Text = PetroleumCalculatorEngine.Format(rateDiff);
            lblGrossProfit.Text = PetroleumCalculatorEngine.Format(gross);
            lblNetProfit.Text = PetroleumCalculatorEngine.Format(net);
            lblProfitPct.Text = pct.ToString("N2", CultureInfo.InvariantCulture) + " %";
            lblMarginPct.Text = margin.ToString("N2", CultureInfo.InvariantCulture) + " %";
        }

        private void RecalcPercent()
        {
            decimal debit = PetroleumCalculatorEngine.Parse(txtDebit.Text);
            decimal credit = PetroleumCalculatorEngine.Parse(txtCreditPct.Text);
            decimal net = PetroleumCalculatorEngine.CustomerNet(debit, credit);
            decimal pct = PetroleumCalculatorEngine.CustomerNetPercent(net, debit);

            lblNetAmount.Text = PetroleumCalculatorEngine.Format(net);
            lblNetPct.Text = pct.ToString("N2", CultureInfo.InvariantCulture) + " %";
            lblPctHint.Text = net >= 0 ? "Receivable / Profit side" : "Payable / Loss side";
        }

        private void RecalcAverage()
        {
            decimal liters = PetroleumCalculatorEngine.Parse(txtAvgLiters.Text);
            decimal rate = PetroleumCalculatorEngine.Parse(txtAvgRate.Text);
            decimal amount = liters * rate;

            lblAvgAmount.Text = PetroleumCalculatorEngine.Format(amount);
            lblAvgRateResult.Text = PetroleumCalculatorEngine.Format(
                PetroleumCalculatorEngine.WeightedAverageRate(liters, amount));

            decimal totalL = _avgTotalLiters + liters;
            decimal totalA = _avgTotalAmount + amount;
            lblAvgCombined.Text = totalL > 0
                ? $"Combined: {PetroleumCalculatorEngine.Format(totalL)} L @ {PetroleumCalculatorEngine.Format(PetroleumCalculatorEngine.WeightedAverageRate(totalL, totalA))}"
                : "Add rows for combined weighted average";
        }

        private decimal _avgTotalLiters;
        private decimal _avgTotalAmount;

        private void btnAvgAdd_Click(object sender, EventArgs e)
        {
            decimal liters = PetroleumCalculatorEngine.Parse(txtAvgLiters.Text);
            decimal rate = PetroleumCalculatorEngine.Parse(txtAvgRate.Text);
            if (liters <= 0) return;

            decimal amount = liters * rate;
            _avgTotalLiters += liters;
            _avgTotalAmount += amount;

            dgvAvg.Rows.Add(
                PetroleumCalculatorEngine.Format(liters),
                PetroleumCalculatorEngine.Format(rate),
                PetroleumCalculatorEngine.Format(amount));

            txtAvgLiters.Clear();
            txtAvgRate.Clear();
            txtAvgLiters.Focus();
            RecalcAverage();
        }

        private void btnAvgClear_Click(object sender, EventArgs e)
        {
            _avgTotalLiters = 0m;
            _avgTotalAmount = 0m;
            dgvAvg.Rows.Clear();
            RecalcAverage();
        }

        private void btnFastEquals_Click(object sender, EventArgs e)
        {
            try
            {
                decimal result = PetroleumCalculatorEngine.EvaluateExpression(txtFastCalc.Text);
                lblFastResult.Text = PetroleumCalculatorEngine.Format(result);
                txtFastCalc.Text = result.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                lblFastResult.Text = "Error";
            }
        }

        private void btnFastClear_Click(object sender, EventArgs e)
        {
            txtFastCalc.Clear();
            lblFastResult.Text = "0.00";
            txtFastCalc.Focus();
        }

        private void btnFastLxR_Click(object sender, EventArgs e)
        {
            string[] parts = (txtFastCalc.Text ?? "").Split(new[] { '*', '×', 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                decimal a = PetroleumCalculatorEngine.Parse(parts[0]);
                decimal b = PetroleumCalculatorEngine.Parse(parts[1]);
                decimal r = a * b;
                lblFastResult.Text = PetroleumCalculatorEngine.Format(r);
                txtFastCalc.Text = r.ToString(CultureInfo.InvariantCulture);
                return;
            }
            txtFastCalc.Text = (txtFastCalc.Text ?? "").Trim() + "*";
            txtFastCalc.Focus();
            txtFastCalc.SelectionStart = txtFastCalc.Text.Length;
        }

        private void FastKeypad_Click(object sender, EventArgs e)
        {
            if (!(sender is Guna.UI2.WinForms.Guna2Button btn)) return;
            string token = btn.Tag as string ?? btn.Text;
            if (token == "C")
            {
                btnFastClear_Click(sender, e);
                return;
            }
            if (token == "=")
            {
                btnFastEquals_Click(sender, e);
                return;
            }
            if (token == "⌫")
            {
                if (txtFastCalc.Text.Length > 0)
                    txtFastCalc.Text = txtFastCalc.Text.Substring(0, txtFastCalc.Text.Length - 1);
                return;
            }
            txtFastCalc.Text += token;
            txtFastCalc.Focus();
            txtFastCalc.SelectionStart = txtFastCalc.Text.Length;
        }

        private void txtFastCalc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnFastEquals_Click(sender, e);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                btnFastClear_Click(sender, e);
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            txtLiters.Clear(); txtRate.Clear(); txtAdvance.Clear(); txtCredit.Clear();
            txtSaleRate.Clear(); txtPurchaseRate.Clear(); txtProfitLiters.Clear(); txtExpense.Clear();
            txtDebit.Clear(); txtCreditPct.Clear();
            txtAvgLiters.Clear(); txtAvgRate.Clear();
            btnAvgClear_Click(sender, e);
            btnFastClear_Click(sender, e);
            RecalcSale();
            RecalcProfit();
            RecalcPercent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
