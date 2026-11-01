using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmStockAdd
    {
        private Label lblMinusDisel;
        private Guna.UI2.WinForms.Guna2TextBox txtMinusDisel;

        private void SetupMinusLitterControls()
        {
            if (txtMinusDisel != null)
                return;

            label7.Text = "Add Litter";
            txtAddDisel.Size = new Size(115, txtAddDisel.Height);
            txtAddDisel.PlaceholderText = "Plus";
            txtAddDisel.TextAlign = HorizontalAlignment.Left;
            txtRate.TextAlign = HorizontalAlignment.Left;

            lblMinusDisel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 140),
                Location = new Point(595, label7.Top),
                Name = "lblMinusDisel",
                Text = "Minus Litter"
            };

            txtMinusDisel = new Guna.UI2.WinForms.Guna2TextBox
            {
                AutoRoundedCorners = true,
                BorderColor = Color.FromArgb(220, 53, 69),
                BorderRadius = 17,
                Cursor = Cursors.IBeam,
                DefaultText = "",
                FillColor = Color.FromArgb(37, 41, 74),
                Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(580, txtAddDisel.Top),
                Name = "txtMinusDisel",
                PlaceholderText = "Sale / Minus",
                Size = new Size(115, txtAddDisel.Height),
                TabIndex = 6,
                Visible = true,
                TextAlign = HorizontalAlignment.Left
            };
            txtMinusDisel.DisabledState.Parent = txtMinusDisel;
            txtMinusDisel.FocusedState.Parent = txtMinusDisel;
            txtMinusDisel.HoverState.Parent = txtMinusDisel;
            txtMinusDisel.ShadowDecoration.Parent = txtMinusDisel;
            txtMinusDisel.FocusedState.BorderColor = Color.FromArgb(255, 99, 99);
            txtMinusDisel.HoverState.BorderColor = Color.FromArgb(255, 120, 120);

            Controls.Add(lblMinusDisel);
            Controls.Add(txtMinusDisel);

            txtMinusDisel.TextChanged += MinusDisel_TextChanged;
            txtMinusDisel.Leave += MinusDisel_Leave;
            txtMinusDisel.KeyPress += PositiveLitter_KeyPress;

            BringMinusControlsToFront();
        }

        private void BringMinusControlsToFront()
        {
            if (lblMinusDisel == null || txtMinusDisel == null)
                return;

            txtMinusDisel.Visible = true;
            lblMinusDisel.Visible = true;
            lblMinusDisel.BringToFront();
            txtMinusDisel.BringToFront();
            txtAddDisel.BringToFront();
            label7.BringToFront();
        }

        private decimal GetNetLitterForSave(out string error)
        {
            error = null;
            decimal addLitter = ParseLitterOrZero(txtAddDisel.Text);
            decimal minusLitter = ParseLitterOrZero(txtMinusDisel?.Text);

            if (addLitter < 0 || minusLitter < 0)
            {
                error = "Litter minus sign ke sath nahi — Add box mein plus, Minus box mein sale likhein.";
                return 0;
            }

            // Ek time par sirf Add YA Minus — dono saath amount/litter mix nahi
            if (addLitter > 0 && minusLitter > 0)
            {
                error = "Ek time par sirf Add YA Minus litter daalein.";
                return 0;
            }

            if (addLitter > 0)
                return addLitter;

            if (minusLitter > 0)
                return -minusLitter;

            error = "Add Litter ya Minus Litter mein se koi ek value zaroor daalein.";
            return 0;
        }

        /// <summary>Add type ho to Minus clear; Minus type ho to Add clear.</summary>
        private void EnforceExclusiveLitterBoxes(Guna.UI2.WinForms.Guna2TextBox active)
        {
            if (active == null) return;
            if (ParseLitterOrZero(active.Text) <= 0) return;

            if (ReferenceEquals(active, txtAddDisel) && txtMinusDisel != null
                && !string.IsNullOrWhiteSpace(txtMinusDisel.Text))
            {
                txtMinusDisel.Text = "";
            }
            else if (ReferenceEquals(active, txtMinusDisel)
                && !string.IsNullOrWhiteSpace(txtAddDisel.Text))
            {
                txtAddDisel.Text = "";
            }
        }

        private void LoadLitterBoxesFromStored(decimal stored)
        {
            if (stored >= 0)
            {
                txtAddDisel.Text = stored == 0 ? "" : FormatLitter(stored);
                if (txtMinusDisel != null) txtMinusDisel.Text = "";
            }
            else
            {
                txtAddDisel.Text = "";
                if (txtMinusDisel != null)
                    txtMinusDisel.Text = FormatLitter(Math.Abs(stored));
            }
        }

        private static decimal ParseLitterOrZero(string text)
        {
            text = (text ?? "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(text)) return 0;
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value)
                ? value : 0;
        }

        private static string FormatLitter(decimal value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
        }

        private void MinusDisel_TextChanged(object sender, EventArgs e)
        {
            KeepLitterCaretAtEndWhileTyping(txtMinusDisel);
            EnforceExclusiveLitterBoxes(txtMinusDisel);
            UpdateLitterRateAmount();
        }

        private void MinusDisel_Leave(object sender, EventArgs e)
        {
            FormatPositiveLitterBox(txtMinusDisel);
            UpdateLitterRateAmount();
        }

        private void FormatPositiveLitterBox(Guna.UI2.WinForms.Guna2TextBox box)
        {
            if (box == null) return;

            string currentText = (box.Text ?? "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(currentText))
                return;

            // Typing ke dauran format mat karo — warna cursor beech mein jump hota hai
            if (box.Focused)
                return;

            if (decimal.TryParse(currentText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
            {
                string formatted = FormatLitter(Math.Abs(value));
                if (formatted != box.Text)
                    box.Text = formatted;
            }
        }

        /// <summary>
        /// Digit type karte waqt cursor end / sahi jagah rahe — comma format sirf Leave pe.
        /// </summary>
        private void KeepLitterCaretAtEndWhileTyping(Guna.UI2.WinForms.Guna2TextBox box)
        {
            if (box == null || !box.Focused) return;

            string raw = (box.Text ?? "").Replace(",", "");
            if (raw != (box.Text ?? ""))
            {
                int digitsBefore = 0;
                int sel = Math.Min(box.SelectionStart, box.Text.Length);
                for (int i = 0; i < sel; i++)
                {
                    if (char.IsDigit(box.Text[i]) || box.Text[i] == '.')
                        digitsBefore++;
                }

                box.Text = raw;
                int newSel = Math.Min(digitsBefore, box.Text.Length);
                box.SelectionStart = newSel;
                box.SelectionLength = 0;
            }
        }

        private void PositiveLitter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            var gtb = sender as Guna.UI2.WinForms.Guna2TextBox;
            string text = gtb?.Text ?? "";
            int selStart = gtb?.SelectionStart ?? 0;
            int selLen = gtb?.SelectionLength ?? 0;

            if (e.KeyChar == '.')
            {
                string newText = text.Substring(0, selStart) + "." + text.Substring(selStart + selLen);
                if (text.Contains(".") || newText.IndexOf('.') != newText.LastIndexOf('.'))
                    e.Handled = true;
                return;
            }

            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                CustomeMessage customMessageBox = new CustomeMessage("Yahan sirf number aur decimal (.) likh sakte ho!", "Ghalti");
                customMessageBox.ShowDialog();
            }
        }
    }
}
