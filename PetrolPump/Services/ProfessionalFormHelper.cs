using Guna.UI2.WinForms;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.Services
{
    public static class ProfessionalFormHelper
    {
        public static Guna2Button AddCloseButton(Control parent, int left = 750, int top = 12)
        {
            var btnClose = new Guna2Button
            {
                Text = "Close",
                Location = new Point(left, top),
                Size = new Size(90, 32),
                BorderRadius = 10,
                FillColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnClose.Click += (s, e) =>
            {
                if (parent is Form form)
                    form.Close();
            };

            parent.Controls.Add(btnClose);
            btnClose.BringToFront();
            return btnClose;
        }
    }
}
