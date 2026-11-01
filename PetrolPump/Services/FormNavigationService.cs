using AlMadinaShahzad.Model;
using AlMadinaShahzad.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// FormShortcuts.txt se keyboard shortcuts — form se form tezi se jump.
    /// </summary>
    public static class FormNavigationService
    {
        private sealed class ShortcutEntry
        {
            public Keys KeyData;
            public string FormName;
            public string Label;
        }

        private static readonly List<ShortcutEntry> _shortcuts = new List<ShortcutEntry>();
        private static readonly string[] _namespaces =
        {
            "ZaibPetroleumService.View.",
            "ZaibPetroleumService.Model.",
            "ZaibPetroleumService.",
            "ZaibPetroleumService.ReportForm.",
            "DigiKhataApp.",
            "HajiBalochSoftwere.ReportForm."
        };

        private static DateTime _lastFileWriteUtc = DateTime.MinValue;
        private static Dictionary<string, Type> _formTypeCache;

        public static void Initialize()
        {
            LoadShortcuts();
        }

        public static void Reload()
        {
            LoadShortcuts();
        }

        public static bool TryHandle(Keys keyData)
        {
            if (keyData == Keys.None)
                return false;

            // Add/dialog open ho to shortcuts skip — entry disturb na ho
            if (Form.ActiveForm != null && Form.ActiveForm.Modal)
                return false;

            ReloadIfFileChanged();

            if (keyData == Keys.F1)
            {
                ShowHelp();
                return true;
            }

            foreach (ShortcutEntry entry in _shortcuts)
            {
                if (entry.KeyData != keyData)
                    continue;

                NavigateToForm(entry.FormName);
                return true;
            }

            return false;
        }

        public static void ShowHelp()
        {
            ReloadIfFileChanged();

            var sb = new StringBuilder();
            sb.AppendLine("Ctrl + letter se form open hota hai.");
            sb.AppendLine();
            if (_shortcuts.Count == 0)
            {
                sb.AppendLine("Koi shortcut nahi mili.");
            }
            else
            {
                foreach (ShortcutEntry e in _shortcuts)
                {
                    string label = string.IsNullOrWhiteSpace(e.Label) ? e.FormName : e.Label;
                    sb.AppendLine(FormatShortcut(e.KeyData) + "  →  " + label);
                }
            }
            sb.AppendLine();
            sb.AppendLine("F1  →  Ye list");
            sb.AppendLine("Ctrl+N  →  Add (view screen)");
            sb.AppendLine("Ctrl+S  →  Save (add form)");
            sb.AppendLine("Esc  →  Add form band");

            MessageBox.Show(sb.ToString(), "Commands (F1)",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void NavigateToForm(string formName)
        {
            if (string.IsNullOrWhiteSpace(formName))
                return;

            Form form = CreateForm(formName.Trim());
            if (form == null)
            {
                MessageBox.Show("Form nahi mila: " + formName + "\nFormShortcuts.txt check karein.",
                    "Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            frmMain main = frmMain.instance;
            if (main == null || main.IsDisposed)
            {
                MessageBox.Show("Main form open nahi hai.", "Shortcut",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!main.Visible)
            {
                MessageBox.Show("Pehle login karke main screen open karein.", "Shortcut",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            main.AddControls(form);
            main.Activate();
        }

        private static Form CreateForm(string formName)
        {
            Type type = ResolveFormType(formName);
            if (type == null)
                return null;

            try
            {
                return (Form)Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private static Type ResolveFormType(string formName)
        {
            if (string.IsNullOrWhiteSpace(formName))
                return null;

            Assembly asm = typeof(frmMain).Assembly;
            foreach (string ns in _namespaces)
            {
                Type t = asm.GetType(ns + formName, false, true);
                if (t != null && typeof(Form).IsAssignableFrom(t))
                    return t;
            }

            EnsureFormTypeCache(asm);
            Type cached;
            if (_formTypeCache != null && _formTypeCache.TryGetValue(formName, out cached))
                return cached;

            return null;
        }

        private static void EnsureFormTypeCache(Assembly asm)
        {
            if (_formTypeCache != null)
                return;

            _formTypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t == null || !typeof(Form).IsAssignableFrom(t) || t.IsAbstract)
                        continue;
                    if (!_formTypeCache.ContainsKey(t.Name))
                        _formTypeCache[t.Name] = t;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Type t in ex.Types)
                {
                    if (t == null || !typeof(Form).IsAssignableFrom(t) || t.IsAbstract)
                        continue;
                    if (!_formTypeCache.ContainsKey(t.Name))
                        _formTypeCache[t.Name] = t;
                }
            }
        }

        private static void ReloadIfFileChanged()
        {
            try
            {
                string path = GetShortcutsPath();
                if (!File.Exists(path))
                {
                    if (_shortcuts.Count == 0)
                        LoadShortcuts();
                    return;
                }

                DateTime writeUtc = File.GetLastWriteTimeUtc(path);
                if (writeUtc != _lastFileWriteUtc)
                    LoadShortcuts();
            }
            catch
            {
                // ignore file race
            }
        }

        private static string GetShortcutsPath()
        {
            return Path.Combine(Application.StartupPath, "FormShortcuts.txt");
        }

        private static void LoadShortcuts()
        {
            _shortcuts.Clear();
            string path = GetShortcutsPath();
            if (!File.Exists(path))
            {
                TryWriteDefaultFile(path);
                if (!File.Exists(path))
                    return;
            }

            try
            {
                _lastFileWriteUtc = File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                _lastFileWriteUtc = DateTime.UtcNow;
            }

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string shortcutText = line.Substring(0, eq).Trim();
                string right = line.Substring(eq + 1).Trim();
                if (string.IsNullOrEmpty(shortcutText) || string.IsNullOrEmpty(right))
                    continue;

                string formName = right;
                string label = "";
                int pipe = right.IndexOf('|');
                if (pipe >= 0)
                {
                    formName = right.Substring(0, pipe).Trim();
                    label = right.Substring(pipe + 1).Trim();
                }

                if (!TryParseShortcut(shortcutText, out Keys keyData))
                    continue;

                _shortcuts.Add(new ShortcutEntry
                {
                    KeyData = keyData,
                    FormName = formName,
                    Label = label
                });
            }
        }

        private static void TryWriteDefaultFile(string path)
        {
            try
            {
                const string content = @"# Al Madina Shezad Chhutta Services — Form Shortcuts
# Sirf Ctrl + letter. Help: F1

Ctrl+D=frmDiselView|Daily Diesel Sales
Ctrl+C=frmCreditAdjust|Credit Customer
Ctrl+A=frmDealerNameView|Add Dealer
Ctrl+P=frmDieselLedgerView|Dealer Payout
Ctrl+M=frmStockView|DealerAmount
Ctrl+I=FrmDirectDealerPaymentAmountView|Direct Dealer Amount
Ctrl+U=frmCustomerView|Add Customer
Ctrl+T=frmCustomerToCustomerView|Customer To Customer
Ctrl+L=frmDieselLedger|Customer To Dealer
Ctrl+J=frmDealertoDealerView|Dealer To Dealer
Ctrl+K=frmStockDieselView|Stock Diesel
Ctrl+E=frmExpense|Expense
Ctrl+B=frmBankAccountView|Bank Account
Ctrl+H=frmDashBoard|Dashboard
Ctrl+O=frmClosingformEntry|Closing Entry
Ctrl+W=frmClosing2|Closing 2
Ctrl+Q=ReportAndBackup|Reports
Ctrl+G=frmGmailDriveBackup|Gmail Drive Backup
";
                File.WriteAllText(path, content);
            }
            catch
            {
                // ignore
            }
        }

        private static bool TryParseShortcut(string text, out Keys keyData)
        {
            keyData = Keys.None;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] parts = text.Split('+').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
                return false;

            Keys modifiers = Keys.None;
            string keyPart = parts[parts.Length - 1];

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string mod = parts[i];
                if (mod.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    mod.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    modifiers |= Keys.Control;
                else if (mod.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= Keys.Alt;
                else if (mod.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= Keys.Shift;
                else
                    return false;
            }

            Keys key = Keys.None;
            if (keyPart.Length >= 2 && keyPart[0] == 'F' &&
                int.TryParse(keyPart.Substring(1), out int fn) && fn >= 1 && fn <= 24)
            {
                key = Keys.F1 + (fn - 1);
            }
            else if (keyPart.Length == 1 && char.IsDigit(keyPart[0]))
            {
                int digit = keyPart[0] - '0';
                key = digit == 0 ? Keys.D0 : Keys.D0 + digit;
            }
            else if (keyPart.Length == 1)
            {
                try
                {
                    key = (Keys)Enum.Parse(typeof(Keys), keyPart, true);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    key = (Keys)Enum.Parse(typeof(Keys), keyPart, true);
                }
                catch
                {
                    return false;
                }
            }

            if (key == Keys.None)
                return false;

            keyData = modifiers | key;
            return true;
        }

        private static string FormatShortcut(Keys keyData)
        {
            var parts = new List<string>();
            if (keyData.HasFlag(Keys.Control)) parts.Add("Ctrl");
            if (keyData.HasFlag(Keys.Alt)) parts.Add("Alt");
            if (keyData.HasFlag(Keys.Shift)) parts.Add("Shift");

            Keys key = keyData & Keys.KeyCode;
            if (key >= Keys.F1 && key <= Keys.F24)
                parts.Add("F" + ((int)(key - Keys.F1) + 1));
            else if (key >= Keys.D0 && key <= Keys.D9)
                parts.Add(((char)('0' + (key - Keys.D0))).ToString());
            else
                parts.Add(key.ToString());

            return string.Join("+", parts);
        }
    }
}
