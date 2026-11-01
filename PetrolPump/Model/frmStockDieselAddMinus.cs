using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmStockDieselAdd
    {
        private Label lblMinusLitter;
        private Guna.UI2.WinForms.Guna2TextBox txtMinusLitter;

        private void SetupMinusLitterControls()
        {
            if (txtMinusLitter != null)
                return;

            label7.Text = "Add Litter";
            txtlitter.Size = new Size(95, txtlitter.Height);
            txtlitter.PlaceholderText = "Plus";
            txtlitter.TextAlign = HorizontalAlignment.Left;
            txtRate.TextAlign = HorizontalAlignment.Left;

            lblMinusLitter = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 140),
                Location = new Point(565, label7.Top),
                Name = "lblMinusLitter",
                Text = "Minus Litter"
            };

            txtMinusLitter = new Guna.UI2.WinForms.Guna2TextBox
            {
                AutoRoundedCorners = true,
                BorderColor = Color.FromArgb(220, 53, 69),
                BorderRadius = 17,
                Cursor = Cursors.IBeam,
                DefaultText = "",
                FillColor = Color.FromArgb(37, 41, 74),
                Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(553, txtlitter.Top),
                Name = "txtMinusLitter",
                PlaceholderText = "Sale / Minus",
                Size = new Size(95, txtlitter.Height),
                TabIndex = 109,
                Visible = true,
                TextAlign = HorizontalAlignment.Left
            };
            txtMinusLitter.DisabledState.Parent = txtMinusLitter;
            txtMinusLitter.FocusedState.Parent = txtMinusLitter;
            txtMinusLitter.HoverState.Parent = txtMinusLitter;
            txtMinusLitter.ShadowDecoration.Parent = txtMinusLitter;
            txtMinusLitter.FocusedState.BorderColor = Color.FromArgb(255, 99, 99);
            txtMinusLitter.HoverState.BorderColor = Color.FromArgb(255, 120, 120);

            Controls.Add(lblMinusLitter);
            Controls.Add(txtMinusLitter);

            txtMinusLitter.TextChanged += MinusLitter_TextChanged;
            txtMinusLitter.Leave += MinusLitter_Leave;
            txtMinusLitter.KeyPress += PositiveLitter_KeyPress;

            BringMinusControlsToFront();
        }

        private void BringMinusControlsToFront()
        {
            if (lblMinusLitter == null || txtMinusLitter == null)
                return;

            lblMinusLitter.Visible = true;
            txtMinusLitter.Visible = true;
            lblMinusLitter.BringToFront();
            txtMinusLitter.BringToFront();
            txtlitter.BringToFront();
            label7.BringToFront();
        }

        private bool TryGetLitterSaveDetails(out decimal litterToSave, out bool isMinusEntry, out string error)
        {
            litterToSave = 0;
            isMinusEntry = false;
            error = null;

            decimal addLitter = ParseLitterOrZero(txtlitter.Text);
            decimal minusLitter = ParseLitterOrZero(txtMinusLitter?.Text);

            if (addLitter > 0 && minusLitter > 0)
            {
                error = "Ek time par sirf Add YA Minus litter daalein.";
                return false;
            }

            if (addLitter > 0)
            {
                litterToSave = addLitter;
                isMinusEntry = false;
                return true;
            }

            if (minusLitter > 0)
            {
                litterToSave = minusLitter;
                isMinusEntry = true;
                return true;
            }

            error = "Add Litter ya Minus Litter mein se koi ek value zaroor daalein.";
            return false;
        }

        private void LoadLitterBoxesFromRecord(decimal storedLitter, string note)
        {
            bool isMinus = StockDieselLitterHelper.IsMinusEntry(note, storedLitter);
            decimal display = StockDieselLitterHelper.GetDisplayLitter(storedLitter, note);
            string cleanNote = StockDieselLitterHelper.StripMinusTag(note);

            if (isMinus)
            {
                txtlitter.Text = "";
                if (txtMinusLitter != null)
                    txtMinusLitter.Text = display == 0 ? "" : StockDieselLitterHelper.FormatLitter(display);
            }
            else
            {
                txtlitter.Text = display == 0 ? "" : StockDieselLitterHelper.FormatLitter(display);
                if (txtMinusLitter != null) txtMinusLitter.Text = "";
            }

            txtNote.Text = cleanNote;
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

        /// <summary>Add type ho to Minus clear; Minus type ho to Add clear.</summary>
        private void EnforceExclusiveLitterBoxes(Guna.UI2.WinForms.Guna2TextBox active)
        {
            if (active == null) return;
            if (ParseLitterOrZero(active.Text) <= 0) return;

            if (ReferenceEquals(active, txtlitter) && txtMinusLitter != null
                && !string.IsNullOrWhiteSpace(txtMinusLitter.Text))
            {
                txtMinusLitter.Text = "";
            }
            else if (ReferenceEquals(active, txtMinusLitter)
                && !string.IsNullOrWhiteSpace(txtlitter.Text))
            {
                txtlitter.Text = "";
            }
        }

        private void MinusLitter_TextChanged(object sender, EventArgs e)
        {
            KeepLitterCaretAtEndWhileTyping(txtMinusLitter);
            EnforceExclusiveLitterBoxes(txtMinusLitter);
        }

        private void MinusLitter_Leave(object sender, EventArgs e)
        {
            FormatPositiveLitterBox(txtMinusLitter);
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
                e.Handled = true;
        }
    }
}
