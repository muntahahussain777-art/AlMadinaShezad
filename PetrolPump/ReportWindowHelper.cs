using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AlMadinaShahzad.ReportForm;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad
{
    public static class ReportWindowHelper
    {
        private static readonly Dictionary<string, Form> OpenReports = new Dictionary<string, Form>();

        public static void ShowOnce<T>(Func<T> createForm) where T : Form
        {
            string key = typeof(T).FullName;

            if (!RoleAccessService.CanAccess(typeof(T).Name))
            {
                MessageBox.Show("Aap ke role ki ijazat nahi hai is report par.", "Access Denied",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (OpenReports.TryGetValue(key, out Form existing) && existing != null && !existing.IsDisposed)
            {
                if (existing.WindowState == FormWindowState.Minimized)
                    existing.WindowState = FormWindowState.Normal;
                existing.BringToFront();
                existing.Activate();
                return;
            }

            OpenReports.Remove(key);

            T frm = createForm();
            ReportHeaderBrandHelper.Attach(frm);
            ReportOfficeHelper.Attach(frm);
            OpenReports[key] = frm;
            AppWindowHelper.EnableTopLevelWindow(frm);

            frm.FormClosed += (s, e) =>
            {
                if (OpenReports.TryGetValue(key, out Form f) && ReferenceEquals(f, frm))
                    OpenReports.Remove(key);
            };

            Form owner = frmMain.instance;
            if (owner != null && !owner.IsDisposed)
                frm.Show(owner);
            else
                frm.Show();
        }
    }
}
