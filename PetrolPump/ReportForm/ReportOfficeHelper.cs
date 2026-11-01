using AlMadinaShahzad.Services;
using Guna.UI2.WinForms;
using System;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public static class ReportOfficeHelper
    {
        public static void Attach(Form form, Action reload = null)
        {
            if (form == null) return;
            OfficeHelper.AttachOfficeFilter(form, (s, e) =>
            {
                if (reload != null) { reload(); return; }
                TryClickSearch(form);
            }, left: 700, top: 20);
        }

        public static string Sql(Control host, string columnAlias = "OfficeName")
        {
            return OfficeHelper.OfficeSqlFilter(columnAlias, OfficeHelper.GetSelectedOffice(host, allowAll: true));
        }

        public static string Selected(Control host)
        {
            return OfficeHelper.GetSelectedOffice(host, allowAll: true);
        }

        private static void TryClickSearch(Form form)
        {
            Control btn = Find(form, "btnSearch");
            if (btn is Guna2Button g) { g.PerformClick(); return; }
            if (btn is Button b) { b.PerformClick(); return; }
        }

        private static Control Find(Control root, string name)
        {
            if (root == null) return null;
            if (root.Name == name) return root;
            foreach (Control c in root.Controls)
            {
                var f = Find(c, name);
                if (f != null) return f;
            }
            return null;
        }
    }
}
