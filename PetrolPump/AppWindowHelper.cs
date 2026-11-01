using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public static class AppWindowHelper
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        public static void EnableTopLevelWindow(Form form)
        {
            if (form == null || form.IsDisposed || !form.TopLevel) return;

            form.MinimumSize = new Size(800, 450);
            form.ShowInTaskbar = true;

            var panel = FindControl(form, "panel1");
            if (panel != null)
            {
                panel.MouseDown += (s, e) => BeginDrag(form, panel, e);
                foreach (Control c in panel.Controls)
                {
                    if (c is Label)
                        c.MouseDown += (s, e) => BeginDrag(form, c, e);
                }
            }
        }

        public static void BeginDrag(Form form, Control source, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (form.WindowState == FormWindowState.Maximized)
            {
                var screen = source.PointToScreen(e.Location);
                form.WindowState = FormWindowState.Normal;
                form.Location = new Point(screen.X - form.Width / 2, Math.Max(0, screen.Y - 12));
            }

            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private static Control FindControl(Control parent, string name)
        {
            var found = parent.Controls[name];
            if (found != null) return found;
            foreach (Control c in parent.Controls)
            {
                var inner = FindControl(c, name);
                if (inner != null) return inner;
            }
            return null;
        }
    }
}
