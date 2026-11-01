using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmDiselAdd
    {
        private Label _lblCustomerCreditBalance;
        private bool _customerCreditHooksAttached;

        private void SetupCustomerCreditDisplay()
        {
            if (_lblCustomerCreditBalance != null)
                return;

            _lblCustomerCreditBalance = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(22, 148),
                Name = "lblCustomerCreditBalance",
                Text = "Credit Customer Balance: —"
            };

            Controls.Add(_lblCustomerCreditBalance);
            _lblCustomerCreditBalance.BringToFront();

            if (!_customerCreditHooksAttached)
            {
                _customerCreditHooksAttached = true;
                cbName.SelectedIndexChanged += CustomerCredit_OnCustomerChanged;
                txtName.TextChanged += CustomerCredit_OnCustomerChanged;
            }
        }

        private void CustomerCredit_OnCustomerChanged(object sender, EventArgs e)
        {
            RefreshCustomerCreditDisplay();
        }

        private void RefreshCustomerCreditDisplay()
        {
            if (_lblCustomerCreditBalance == null)
                return;

            int? customerId = TryGetCustomerIdForCreditDisplay();
            if (!customerId.HasValue)
            {
                _lblCustomerCreditBalance.Text = "Credit Customer Balance: —";
                _lblCustomerCreditBalance.ForeColor = Color.FromArgb(180, 180, 180);
                return;
            }

            decimal balance = BalanceConfirmationService.GetCustomerLedgerRemaining(customerId.Value);
            _lblCustomerCreditBalance.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Credit Customer Balance: {0:N0}",
                balance);
            _lblCustomerCreditBalance.ForeColor = balance > 0
                ? Color.FromArgb(255, 215, 0)
                : Color.FromArgb(144, 238, 144);
        }

        private int? TryGetCustomerIdForCreditDisplay()
        {
            if (cbName.SelectedValue != null && cbName.SelectedValue != DBNull.Value &&
                int.TryParse(cbName.SelectedValue.ToString(), out int id) && id > 0)
                return id;

            string name = txtName.Text?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                int resolved = ResolveCustomerIdForSave(name);
                if (resolved > 0)
                    return resolved;
            }

            return null;
        }
    }
}
