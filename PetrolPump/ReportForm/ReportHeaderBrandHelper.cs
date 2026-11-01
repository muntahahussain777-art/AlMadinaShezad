using Guna.UI2.WinForms;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    /// <summary>
    /// Top combobox: Al Madina Shezad Chhutta Services / FOUR STAR.
    /// Original RDLC copy karke header name replace — koi parameter/BrandInfo nahi.
    /// </summary>
    public static class ReportHeaderBrandHelper
    {
        public const string BrandName = "Al Madina Shezad Chhutta Services";
        /// <summary>Legacy header still in some installed/bin RDLC copies — replaced at load time.</summary>
        public const string AlNoor = "AL NOOR PETROLEUM SERVICE";
        public const string FourStar = "FOUR STAR FILLING STATION";

        public static string CurrentCompany { get; private set; } = BrandName;

        private static readonly Dictionary<ReportViewer, string> OriginalPaths =
            new Dictionary<ReportViewer, string>();

        public static void SetCurrentCompany(string company)
        {
            company = (company ?? "").Trim();
            if (string.IsNullOrEmpty(company))
                company = BrandName;
            CurrentCompany = company;
        }

        public static void Attach(Form form)
        {
            if (form == null || form.IsDisposed) return;
            if (FindBrandCombo(form) != null) return;

            List<ReportViewer> viewers = FindReportViewers(form);
            if (viewers.Count == 0) return;

            Control combo = CreateCombo();
            PlaceComboAtTop(form, combo);
            PlaceManageButton(form, combo);
            CurrentCompany = ReportHeaderNamesStore.GetSelected();
            SetSelectedInCombo(combo, CurrentCompany);

            foreach (ReportViewer rv in viewers)
            {
                rv.ShowParameterPrompts = false;
                rv.ShowCredentialPrompts = false;
                // Form Load se pehle blank parameters set — warna pehli RefreshReport error deti hai
                try
                {
                    RememberOriginalPath(rv);
                    EnsureReportParameters(rv, null);
                }
                catch { }
            }

            bool comboReady = false;
            EventHandler onChanged = (s, e) =>
            {
                if (!comboReady) return;
                CurrentCompany = GetSelected(combo);
                ReportHeaderNamesStore.SetSelected(CurrentCompany);
                foreach (ReportViewer rv in FindReportViewers(form))
                {
                    try
                    {
                        // Data load ke baad hi brand + refresh — warna parameters/credentials error
                        if (rv.LocalReport.DataSources == null || rv.LocalReport.DataSources.Count == 0)
                            continue;
                        ApplyBrandToViewer(rv, CurrentCompany);
                        SafeRefreshReport(rv);
                    }
                    catch
                    {
                    }
                }
            };
            WireChanged(combo, onChanged);

            form.Shown += (s, e) =>
            {
                PlaceComboAtTop(form, combo);
                PlaceManageButton(form, combo);
                ReloadComboItems(combo);
                CurrentCompany = ReportHeaderNamesStore.GetSelected();
                SetSelectedInCombo(combo, CurrentCompany);
                foreach (ReportViewer rv in FindReportViewers(form))
                {
                    rv.ShowParameterPrompts = false;
                    rv.ShowCredentialPrompts = false;
                    RememberOriginalPath(rv);
                    // Shown par ApplyBrand/Refresh MAT karo — report Load pe data+params set hote hain;
                    // yahan path change se "parameters or credentials" error aata tha.
                }
                comboReady = true;
            };

            form.FormClosed += (s, e) =>
            {
                foreach (ReportViewer rv in viewers)
                {
                    OriginalPaths.Remove(rv);
                    LastParameters.Remove(rv);
                }
            };
        }

        public static void ApplyBrandToViewer(ReportViewer rv, string companyName = null)
        {
            if (rv == null || rv.IsDisposed) return;

            string company = string.IsNullOrWhiteSpace(companyName) ? CurrentCompany : companyName.Trim();
            if (string.IsNullOrEmpty(company)) company = BrandName;
            CurrentCompany = company;

            string currentPath = rv.LocalReport.ReportPath;
            // AutoSize diesel report: isi file ko brand karo (stock AddPetrol se replace mat karo)
            bool isAutoSize = !string.IsNullOrEmpty(currentPath)
                && currentPath.IndexOf("_AutoSize_", StringComparison.OrdinalIgnoreCase) >= 0
                && File.Exists(currentPath);

            string originalPath = isAutoSize ? currentPath : RememberOriginalPath(rv);
            if (string.IsNullOrEmpty(originalPath) || !File.Exists(originalPath))
                return;

            var savedSources = new List<ReportDataSource>();
            foreach (ReportDataSource ds in rv.LocalReport.DataSources)
            {
                if (string.Equals(ds.Name, "BrandInfo", StringComparison.OrdinalIgnoreCase))
                    continue;
                savedSources.Add(new ReportDataSource(ds.Name, ds.Value));
            }

            var savedParams = new List<ReportParameter>();
            try
            {
                foreach (var p in rv.LocalReport.GetParameters())
                {
                    if (p == null || string.IsNullOrEmpty(p.Name)) continue;
                    string val = (p.Values != null && p.Values.Count > 0) ? (p.Values[0] ?? "") : "";
                    savedParams.Add(new ReportParameter(p.Name, val));
                }
            }
            catch
            {
            }
            // Pehle se yaad params merge (Refresh ke baad GetParameters empty ho sakta hai)
            List<ReportParameter> remembered;
            if (LastParameters.TryGetValue(rv, out remembered) && remembered != null)
            {
                foreach (ReportParameter rp in remembered)
                {
                    if (rp == null || string.IsNullOrEmpty(rp.Name)) continue;
                    bool exists = savedParams.Exists(x =>
                        x != null && string.Equals(x.Name, rp.Name, StringComparison.OrdinalIgnoreCase));
                    if (!exists)
                        savedParams.Add(rp);
                    else
                    {
                        // Agar current empty hai to remembered value use karo
                        var cur = savedParams.Find(x =>
                            x != null && string.Equals(x.Name, rp.Name, StringComparison.OrdinalIgnoreCase));
                        if (cur != null && (cur.Values == null || cur.Values.Count == 0
                            || string.IsNullOrEmpty(cur.Values[0]))
                            && rp.Values != null && rp.Values.Count > 0)
                        {
                            savedParams.Remove(cur);
                            savedParams.Add(rp);
                        }
                    }
                }
            }

            string xml = File.ReadAllText(originalPath, Encoding.UTF8);
            xml = ReplaceAllKnownHeaders(xml, company);

            // Installed EXE (Program Files) read-only hota hai — branded file AppData mein likho
            string brandedPath = WriteBrandedReport(originalPath, company, xml);
            if (!isAutoSize)
                OriginalPaths[rv] = originalPath;

            if (!string.IsNullOrEmpty(brandedPath) && File.Exists(brandedPath))
            {
                rv.LocalReport.ReportPath = brandedPath;
            }
            else
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                    rv.LocalReport.LoadReportDefinition(ms);
            }

            rv.LocalReport.DataSources.Clear();
            foreach (ReportDataSource ds in savedSources)
                rv.LocalReport.DataSources.Add(ds);

            EnsureReportParameters(rv, savedParams);
            RememberParameters(rv, savedParams);
        }

        private static readonly Dictionary<ReportViewer, List<ReportParameter>> LastParameters =
            new Dictionary<ReportViewer, List<ReportParameter>>();

        private static void RememberParameters(ReportViewer rv, List<ReportParameter> savedParams)
        {
            if (rv == null) return;
            if (savedParams == null || savedParams.Count == 0) return;
            LastParameters[rv] = new List<ReportParameter>(savedParams);
        }

        public static void SafeRefreshReport(ReportViewer rv)
        {
            if (rv == null || rv.IsDisposed) return;
            try
            {
                if (rv.LocalReport.DataSources == null || rv.LocalReport.DataSources.Count == 0)
                    return;

                // Refresh se pehle parameters ensure
                List<ReportParameter> remembered = null;
                LastParameters.TryGetValue(rv, out remembered);
                EnsureReportParameters(rv, remembered);

                rv.ShowParameterPrompts = false;
                rv.ShowCredentialPrompts = false;
                rv.RefreshReport();
            }
            catch
            {
                // Swallow — caller ko crash na ho
            }
        }

        /// <summary>
        /// Har known / custom header ko target company se replace — Al Madina wapas aate waqt bhi sahi.
        /// </summary>
        private static string ReplaceAllKnownHeaders(string xml, string company)
        {
            if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(company))
                return xml;

            var known = new List<string>();
            Action<string> add = s =>
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                s = s.Trim();
                if (known.Exists(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase))) return;
                // Longer names pehle replace hon (partial replace avoid)
                known.Add(s);
            };

            add(AlNoor);
            add("Al Noor Petroleum Service");
            add("Al Noor Petroleum");
            add(FourStar);
            add(BrandName);
            add("Al Madina Shezad Chhutta Service");
            add("AlMadina Shahzad DIESEL & CEMENT");
            add("AlMadina Shahzad");
            try
            {
                foreach (string n in ReportHeaderNamesStore.LoadAll())
                    add(n);
            }
            catch { }

            known.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (string old in known)
            {
                if (string.Equals(old, company, StringComparison.OrdinalIgnoreCase))
                    continue;
                xml = xml.Replace(old, company);
            }
            return xml;
        }

        private static void EnsureReportParameters(ReportViewer rv, List<ReportParameter> savedParams)
        {
            if (rv == null || rv.IsDisposed) return;
            try
            {
                if (string.IsNullOrEmpty(rv.LocalReport.ReportPath)
                    || !File.Exists(rv.LocalReport.ReportPath))
                    return;

                var saved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (savedParams != null)
                {
                    foreach (ReportParameter p in savedParams)
                    {
                        if (p == null || string.IsNullOrEmpty(p.Name)) continue;
                        string val = (p.Values != null && p.Values.Count > 0) ? (p.Values[0] ?? "") : "";
                        saved[p.Name] = val;
                    }
                }

                ReportParameterInfoCollection infos;
                try
                {
                    infos = rv.LocalReport.GetParameters();
                }
                catch
                {
                    return;
                }

                var toSet = new List<ReportParameter>();
                foreach (ReportParameterInfo info in infos)
                {
                    if (info == null || string.IsNullOrEmpty(info.Name)) continue;
                    string val;
                    if (!saved.TryGetValue(info.Name, out val) || val == null)
                    {
                        // DefaultValue from RDLC if present
                        val = "";
                        try
                        {
                            if (info.Values != null && info.Values.Count > 0 && info.Values[0] != null)
                                val = info.Values[0];
                        }
                        catch { }
                    }
                    toSet.Add(new ReportParameter(info.Name, val));
                }

                if (toSet.Count > 0)
                {
                    rv.LocalReport.SetParameters(toSet);
                    RememberParameters(rv, toSet);
                }
            }
            catch
            {
                // Parameters optional / already set by caller
            }
        }

        public static void ReloadReportWithBrand(ReportViewer rv, string companyName = null)
        {
            ApplyBrandToViewer(rv, companyName);
        }

        public static void EnsureBrandData(ReportViewer rv, string companyName = null)
        {
            ApplyBrandToViewer(rv, companyName);
        }

        public static void ApplyCompanyParameter(ReportViewer rv, string companyName = null)
        {
            ApplyBrandToViewer(rv, companyName);
        }

        private static string RememberOriginalPath(ReportViewer rv)
        {
            if (rv == null) return null;

            string path = rv.LocalReport.ReportPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && IsOriginalReportPath(path))
            {
                OriginalPaths[rv] = path;
                return path;
            }

            string cached;
            if (OriginalPaths.TryGetValue(rv, out cached) && File.Exists(cached) && IsOriginalReportPath(cached))
                return cached;

            string resolved = ResolveOriginalReport(path);
            if (!string.IsNullOrEmpty(resolved))
            {
                OriginalPaths[rv] = resolved;
                return resolved;
            }

            return null;
        }

        private static bool IsOriginalReportPath(string path)
        {
            string name = Path.GetFileName(path) ?? string.Empty;
            if (name.IndexOf(".brand.", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            // AutoSize temp = working copy; stock RDLC se overwrite mat karo
            if (name.IndexOf("_AutoSize_", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return name.EndsWith(".rdlc", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveOriginalReport(string currentPath)
        {
            string fileName = null;
            if (!string.IsNullOrEmpty(currentPath))
            {
                fileName = Path.GetFileName(currentPath);
                int brandAt = fileName.IndexOf(".brand.", StringComparison.OrdinalIgnoreCase);
                if (brandAt >= 0)
                    fileName = fileName.Substring(0, brandAt) + ".rdlc";
            }

            if (string.IsNullOrEmpty(fileName))
                return null;

            foreach (string folder in ReportFolders())
            {
                string candidate = Path.Combine(folder, fileName);
                if (File.Exists(candidate) && IsOriginalReportPath(candidate))
                    return candidate;
            }

            return null;
        }

        private static IEnumerable<string> ReportFolders()
        {
            var folders = new List<string>();
            Action<string> add = folder =>
            {
                if (string.IsNullOrEmpty(folder) || folders.Contains(folder)) return;
                if (Directory.Exists(folder))
                    folders.Add(folder);
            };

            add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? "", "Reports"));
            add(Path.Combine(Application.StartupPath ?? "", "Reports"));
            add(Path.Combine(Directory.GetCurrentDirectory(), "Reports"));
            add(WritableReportsFolder());

            return folders;
        }

        private static string WritableReportsFolder()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiselPetrolPump",
                "Reports");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string CompanyFileTag(string company)
        {
            company = (company ?? BrandName).Trim().ToLowerInvariant();
            if (string.Equals(company, BrandName, StringComparison.OrdinalIgnoreCase)
                || company.Contains("al madina") || company.Contains("almadina"))
                return "almadina";
            // Unique tag so Al Madina aur custom names ek hi file overwrite na karein
            uint hash = 2166136261;
            foreach (char c in company)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return "c" + hash.ToString("x8");
        }

        private static string WriteBrandedReport(string originalPath, string company, string xml)
        {
            string name = Path.GetFileNameWithoutExtension(originalPath);
            string tag = CompanyFileTag(company);
            string fileName = name + ".brand." + tag + ".rdlc";

            // Installed EXE Program Files mein write nahi ho sakti — AppData/Temp use karo
            string[] targets =
            {
                Path.Combine(WritableReportsFolder(), fileName),
                Path.Combine(Path.GetTempPath(), "AlMadinaReports", fileName)
            };

            foreach (string target in targets)
            {
                try
                {
                    string dir = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllText(target, xml, new UTF8Encoding(false));
                    if (File.Exists(target))
                        return target;
                }
                catch
                {
                }
            }

            return null;
        }

        private static void PlaceComboAtTop(Form form, Control combo)
        {
            if (combo == null || form == null) return;
            try
            {
                Control panel = null;
                Control[] panels = form.Controls.Find("panel1", false);
                if (panels != null && panels.Length > 0)
                    panel = panels[0];

                if (panel != null)
                {
                    if (combo.Parent != panel)
                    {
                        if (combo.Parent != null)
                            combo.Parent.Controls.Remove(combo);
                        panel.Controls.Add(combo);
                    }
                    int h = Math.Max(22, panel.Height - 4);
                    combo.Size = new Size(270, h);
                    combo.Location = new Point(6, Math.Max(1, (panel.Height - combo.Height) / 2));
                    combo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    combo.BringToFront();
                    return;
                }

                if (combo.Parent != form)
                {
                    if (combo.Parent != null)
                        combo.Parent.Controls.Remove(combo);
                    form.Controls.Add(combo);
                }
                combo.Location = new Point(8, 2);
                combo.Size = new Size(270, 26);
                combo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                combo.BringToFront();
            }
            catch
            {
            }
        }

        private static Control CreateCombo()
        {
            try
            {
                var combo = new Guna2ComboBox
                {
                    Name = "cmbReportCompanyBrand",
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    FillColor = Color.FromArgb(37, 41, 74),
                    BorderColor = Color.FromArgb(112, 51, 255),
                    BorderRadius = 8,
                    BorderThickness = 1,
                    Size = new Size(310, 26),
                    TabIndex = 0,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    ItemHeight = 20
                };
                ReloadComboItems(combo);
                return combo;
            }
            catch
            {
                var combo = new ComboBox
                {
                    Name = "cmbReportCompanyBrand",
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 41, 74),
                    ForeColor = Color.White,
                    Size = new Size(310, 26),
                    TabIndex = 0
                };
                ReloadComboItems(combo);
                return combo;
            }
        }

        public static void ReloadComboItems(Control combo)
        {
            if (combo == null) return;
            string prev = GetSelected(combo);
            ComboBox itemsHost = combo as ComboBox;
            if (combo is Guna2ComboBox g) itemsHost = g;
            if (itemsHost == null) return;

            itemsHost.Items.Clear();
            foreach (string name in ReportHeaderNamesStore.LoadAll())
                itemsHost.Items.Add(name);

            if (itemsHost.Items.Count == 0)
                itemsHost.Items.Add(BrandName);

            SetSelectedInCombo(combo, string.IsNullOrEmpty(prev) ? ReportHeaderNamesStore.GetSelected() : prev);
        }

        private static void SetSelectedInCombo(Control combo, string company)
        {
            ComboBox itemsHost = combo as ComboBox;
            if (combo is Guna2ComboBox g) itemsHost = g;
            if (itemsHost == null || itemsHost.Items.Count == 0) return;

            company = (company ?? BrandName).Trim();
            for (int i = 0; i < itemsHost.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(itemsHost.Items[i]), company, StringComparison.OrdinalIgnoreCase))
                {
                    itemsHost.SelectedIndex = i;
                    return;
                }
            }
            itemsHost.SelectedIndex = 0;
        }

        private static void PlaceManageButton(Form form, Control combo)
        {
            if (form == null || combo == null) return;
            Control existing = null;
            Control[] found = form.Controls.Find("btnReportHeaderManage", true);
            if (found != null && found.Length > 0)
                existing = found[0];

            Control parent = combo.Parent ?? form;
            Guna2Button btn;
            if (existing is Guna2Button gBtn)
            {
                btn = gBtn;
            }
            else
            {
                btn = new Guna2Button
                {
                    Name = "btnReportHeaderManage",
                    Text = "Header Names",
                    Size = new Size(110, Math.Max(26, combo.Height)),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(52, 73, 94),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
                };
                btn.Click += (s, e) =>
                {
                    using (var frm = new frmReportHeaderNames())
                    {
                        frm.ShowDialog(form);
                    }
                    ReloadComboItems(combo);
                    CurrentCompany = ReportHeaderNamesStore.GetSelected();
                    SetSelectedInCombo(combo, CurrentCompany);
                    foreach (ReportViewer rv in FindReportViewers(form))
                    {
                        try
                        {
                            if (rv.LocalReport.DataSources == null || rv.LocalReport.DataSources.Count == 0)
                                continue;
                            ApplyBrandToViewer(rv, CurrentCompany);
                            SafeRefreshReport(rv);
                        }
                        catch { }
                    }
                };
                parent.Controls.Add(btn);
            }

            btn.Location = new Point(combo.Right + 6, combo.Top);
            btn.Height = Math.Max(26, combo.Height);
            btn.BringToFront();
        }

        private static Control FindBrandCombo(Form form)
        {
            Control[] found = form.Controls.Find("cmbReportCompanyBrand", true);
            return (found != null && found.Length > 0) ? found[0] : null;
        }

        private static void WireChanged(Control combo, EventHandler handler)
        {
            if (combo is Guna2ComboBox g)
            {
                g.SelectedIndexChanged += handler;
                g.SelectionChangeCommitted += handler;
            }
            else if (combo is ComboBox c)
            {
                c.SelectedIndexChanged += handler;
                c.SelectionChangeCommitted += handler;
            }
        }

        private static string GetSelected(Control combo)
        {
            int index = -1;
            ComboBox itemsHost = combo as ComboBox;
            if (combo is Guna2ComboBox g)
            {
                index = g.SelectedIndex;
                itemsHost = g;
            }
            else if (combo is ComboBox c)
            {
                index = c.SelectedIndex;
                itemsHost = c;
            }

            if (itemsHost != null && index >= 0 && index < itemsHost.Items.Count)
            {
                object item = itemsHost.Items[index];
                if (item != null)
                {
                    string text = item.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();
                }
            }

            return BrandName;
        }

        private static List<ReportViewer> FindReportViewers(Control root)
        {
            var list = new List<ReportViewer>();
            Collect(root, list);
            return list;
        }

        private static void Collect(Control parent, List<ReportViewer> list)
        {
            if (parent == null) return;
            if (parent is ReportViewer rv)
                list.Add(rv);
            foreach (Control child in parent.Controls)
                Collect(child, list);
        }
    }
}
