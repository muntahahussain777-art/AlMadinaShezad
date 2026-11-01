using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    /// <summary>
    /// Report header company names — Al Madina permanent + user custom names (AppData file).
    /// </summary>
    public static class ReportHeaderNamesStore
    {
        public static string StoreFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiselPetrolPump");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ReportHeaderNames.txt");
            }
        }

        public static string SelectedFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiselPetrolPump");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ReportHeaderSelected.txt");
            }
        }

        public static List<string> LoadAll()
        {
            var list = new List<string>();
            AddUnique(list, ReportHeaderBrandHelper.BrandName);

            try
            {
                if (File.Exists(StoreFile))
                {
                    foreach (string line in File.ReadAllLines(StoreFile))
                    {
                        string t = (line ?? "").Trim();
                        if (string.IsNullOrEmpty(t)) continue;
                        if (string.Equals(t, ReportHeaderBrandHelper.FourStar, StringComparison.OrdinalIgnoreCase))
                            continue;
                        AddUnique(list, t);
                    }
                }
            }
            catch { }

            return list;
        }

        public static void SaveAll(IEnumerable<string> names)
        {
            var list = new List<string>();
            AddUnique(list, ReportHeaderBrandHelper.BrandName);
            foreach (string n in names ?? Enumerable.Empty<string>())
            {
                string t = (n ?? "").Trim();
                if (string.IsNullOrEmpty(t)) continue;
                if (string.Equals(t, ReportHeaderBrandHelper.FourStar, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddUnique(list, t);
            }

            File.WriteAllLines(StoreFile, list.Where(x =>
                !string.Equals(x, ReportHeaderBrandHelper.BrandName, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool Add(string name, out string error)
        {
            error = null;
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                error = "Name likhein.";
                return false;
            }
            if (string.Equals(name, ReportHeaderBrandHelper.FourStar, StringComparison.OrdinalIgnoreCase))
            {
                error = "Four Star remove ho chuka hai.";
                return false;
            }

            var all = LoadAll();
            if (all.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
            {
                error = "Ye name pehle se maujood hai.";
                return false;
            }

            all.Add(name);
            SaveAll(all);
            return true;
        }

        public static bool Update(string oldName, string newName, out string error)
        {
            error = null;
            oldName = (oldName ?? "").Trim();
            newName = (newName ?? "").Trim();
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                error = "Name select/likhein.";
                return false;
            }
            if (string.Equals(oldName, ReportHeaderBrandHelper.BrandName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Al Madina Shezad Chhutta Services permanent hai — rename nahi hoga.";
                return false;
            }
            if (string.Equals(newName, ReportHeaderBrandHelper.BrandName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Ye permanent name pehle se maujood hai.";
                return false;
            }

            var all = LoadAll();
            if (all.Any(x => string.Equals(x, newName, StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(x, oldName, StringComparison.OrdinalIgnoreCase)))
            {
                error = "Duplicate name nahi ho sakta.";
                return false;
            }

            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i], oldName, StringComparison.OrdinalIgnoreCase))
                    all[i] = newName;
            }
            SaveAll(all);

            if (string.Equals(GetSelected(), oldName, StringComparison.OrdinalIgnoreCase))
                SetSelected(newName);

            return true;
        }

        public static bool Delete(string name, out string error)
        {
            error = null;
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                error = "Name select karein.";
                return false;
            }
            if (string.Equals(name, ReportHeaderBrandHelper.BrandName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Al Madina Shezad Chhutta Services permanent hai — delete nahi hoga.";
                return false;
            }

            var all = LoadAll().Where(x =>
                !string.Equals(x, name, StringComparison.OrdinalIgnoreCase)).ToList();
            SaveAll(all);

            if (string.Equals(GetSelected(), name, StringComparison.OrdinalIgnoreCase))
                SetSelected(ReportHeaderBrandHelper.BrandName);

            return true;
        }

        public static string GetSelected()
        {
            try
            {
                if (File.Exists(SelectedFile))
                {
                    string s = File.ReadAllText(SelectedFile).Trim();
                    if (!string.IsNullOrEmpty(s)
                        && !string.Equals(s, ReportHeaderBrandHelper.FourStar, StringComparison.OrdinalIgnoreCase))
                    {
                        var all = LoadAll();
                        foreach (string n in all)
                        {
                            if (string.Equals(n, s, StringComparison.OrdinalIgnoreCase))
                                return n;
                        }
                    }
                }
            }
            catch { }
            return ReportHeaderBrandHelper.BrandName;
        }

        public static void SetSelected(string name)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                name = ReportHeaderBrandHelper.BrandName;
            try { File.WriteAllText(SelectedFile, name); }
            catch { }
            ReportHeaderBrandHelper.SetCurrentCompany(name);
        }

        private static void AddUnique(List<string> list, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            if (list.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                return;
            list.Add(name);
        }
    }
}
