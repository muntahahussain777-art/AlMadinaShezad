using AlMadinaShahzad.Model;
using Guna.UI2.WinForms;
using System;
using System.Collections;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Hub Chowki / Band Murad / Askari — office tag + filter + cross-office Yes/No.
    /// </summary>
    public static class OfficeHelper
    {
        public const string AllOffices = "All";
        public const string HubChowki = "Hub Chowki";
        public const string BandMurad = "Band Murad";
        public const string Askari = "Askari";

        public static readonly string[] Offices = { HubChowki, BandMurad, Askari };

        public const string ComboName = "cmbOfficeHelper";
        public const string LabelName = "lblOfficeHelper";
        public const string FilterComboName = "cmbOfficeFilterHelper";
        public const string FilterLabelName = "lblOfficeFilterHelper";

        public static void EnsureOfficeSchema(SQLiteConnection connection)
        {
            string[] tables =
            {
                "AddCustomer", "AddDealer", "PetrolAdd", "AddStock", "StockDiesel",
                "DieselLedger", "DieselLedgerCredit", "DieselLedgerDebit",
                "DealerToDealer", "DealertoDealer", "CustomerToCustomer", "Expensetable", "BankTransactions"
            };

            foreach (string table in tables)
                DatabaseSchemaManager.EnsureColumn(connection, table, "OfficeName", "TEXT");

            SeedOffices(connection);
        }

        private static void SeedOffices(SQLiteConnection connection)
        {
            if (!TableExists(connection, "DiselPetrolPump"))
                return;

            foreach (string office in Offices)
            {
                using (var check = new SQLiteCommand(
                    "SELECT COUNT(1) FROM DiselPetrolPump WHERE TRIM(Name)=@n COLLATE NOCASE", connection))
                {
                    check.Parameters.AddWithValue("@n", office);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        continue;
                }

                using (var insert = new SQLiteCommand(
                    "INSERT INTO DiselPetrolPump (Name, Location) VALUES (@n, @n)", connection))
                {
                    insert.Parameters.AddWithValue("@n", office);
                    insert.ExecuteNonQuery();
                }
            }
        }

        private static bool TableExists(SQLiteConnection connection, string tableName)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@name LIMIT 1", connection))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                return cmd.ExecuteScalar() != null;
            }
        }

        public static Guna2ComboBox AttachOfficeCombo(Control host, bool includeAll = false, int left = 420, int top = 55)
        {
            Control found = FindControl(host, ComboName);
            if (found is Guna2ComboBox existing)
            {
                FillOffices(existing, includeAll);
                return existing;
            }

            var lbl = new Label
            {
                Name = LabelName,
                Text = "Office:",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Location = new Point(left, top + 6),
                BackColor = Color.Transparent
            };

            var cmb = new Guna2ComboBox
            {
                Name = ComboName,
                Location = new Point(left + 70, top),
                Size = new Size(170, 36),
                BorderRadius = 10,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = Color.FromArgb(37, 41, 74),
                BorderColor = Color.FromArgb(112, 51, 255),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            FillOffices(cmb, includeAll);
            host.Controls.Add(lbl);
            host.Controls.Add(cmb);
            lbl.BringToFront();
            cmb.BringToFront();
            return cmb;
        }

        public static Guna2ComboBox AttachOfficeFilter(Control host, EventHandler onChanged, int left = 520, int top = 12)
        {
            Control found = FindControl(host, FilterComboName);
            if (found is Guna2ComboBox existing)
            {
                FillOffices(existing, includeAll: true);
                return existing;
            }

            Control parent;
            int placeLeft;
            int placeTop;
            bool anchorRight;
            ResolveFilterPlacement(host, left, top, out parent, out placeLeft, out placeTop, out anchorRight);

            var lbl = new Label
            {
                Name = FilterLabelName,
                Text = "Office:",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(placeLeft, placeTop + 6),
                BackColor = Color.Transparent
            };

            var cmb = new Guna2ComboBox
            {
                Name = FilterComboName,
                Location = new Point(placeLeft + 62, placeTop),
                Size = new Size(150, 32),
                BorderRadius = 8,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = Color.FromArgb(37, 41, 74),
                BorderColor = Color.FromArgb(112, 51, 255),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            if (anchorRight)
            {
                lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                cmb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            FillOffices(cmb, includeAll: true);
            if (onChanged != null)
                cmb.SelectedIndexChanged += onChanged;

            parent.Controls.Add(lbl);
            parent.Controls.Add(cmb);
            lbl.BringToFront();
            cmb.BringToFront();
            return cmb;
        }

        /// <summary>
        /// Filter ko header/panel mein sahi jagah: Search ke baad ya Add/Load ke paas — form se bahar nahi.
        /// </summary>
        private static void ResolveFilterPlacement(
            Control host, int fallbackLeft, int fallbackTop,
            out Control parent, out int left, out int top, out bool anchorRight)
        {
            parent = host;
            left = fallbackLeft;
            top = fallbackTop;
            anchorRight = false;

            Control panel1 = FindControl(host, "panel1");
            Control btnAdd = FindControl(host, "btnAdd");
            Control txtSearch = FindControl(host, "txtSearch");
            Control btnExcel = FindControl(host, "btnExcel");
            Control btnLoad = FindControl(host, "btnLoad");

            // SampleView forms: panel1 + Add button → Office Add ke bilkul left (header row)
            if (panel1 != null && btnAdd != null && btnAdd.Parent == panel1)
            {
                parent = panel1;
                const int blockWidth = 220;
                left = btnAdd.Left - blockWidth - 8;
                top = btnAdd.Top;
                if (txtSearch != null && left < txtSearch.Right + 8)
                    left = txtSearch.Right + 12;
                if (left < 8) left = 8;
                anchorRight = true;
                return;
            }

            // Closing2: PDF button ke baad (Excel/PDF se overlap nahi)
            Control btnPdf = FindControl(host, "btnPdf");
            if (btnPdf != null)
            {
                parent = btnPdf.Parent ?? host;
                left = btnPdf.Right + 16;
                top = btnPdf.Top;
                return;
            }

            // Closing forms: Excel / Load ke baad same row
            if (btnExcel != null)
            {
                parent = btnExcel.Parent ?? host;
                left = btnExcel.Right + 16;
                top = btnExcel.Top;
                return;
            }
            if (btnLoad != null)
            {
                parent = btnLoad.Parent ?? host;
                left = btnLoad.Right + 16;
                top = btnLoad.Top;
                return;
            }

            // Report forms: after todate / fromdate / btnSearch on same row
            Control todate = FindControl(host, "todate");
            if (todate != null)
            {
                parent = todate.Parent ?? host;
                left = todate.Right + 16;
                top = todate.Top;
                return;
            }
            Control fromdate = FindControl(host, "fromdate");
            if (fromdate != null)
            {
                parent = fromdate.Parent ?? host;
                left = fromdate.Right + 16;
                top = fromdate.Top;
                return;
            }
            Control btnSearch = FindControl(host, "btnSearch");
            if (btnSearch != null)
            {
                parent = btnSearch.Parent ?? host;
                left = btnSearch.Right + 16;
                top = btnSearch.Top;
                return;
            }

            if (panel1 != null)
            {
                parent = panel1;
                if (txtSearch != null && txtSearch.Parent == panel1)
                {
                    left = txtSearch.Right + 16;
                    top = txtSearch.Top;
                }
            }
        }

        public static void FillOffices(ComboBox cmb, bool includeAll)
        {
            string prev = cmb.SelectedItem as string;
            cmb.Items.Clear();
            if (includeAll)
                cmb.Items.Add(AllOffices);
            foreach (string o in Offices)
                cmb.Items.Add(o);

            if (!string.IsNullOrEmpty(prev) && cmb.Items.Contains(prev))
                cmb.SelectedItem = prev;
            else
                cmb.SelectedIndex = 0;
        }

        public static string GetSelectedOffice(Control host, bool allowAll = false)
        {
            var cmb = FindControl(host, allowAll ? FilterComboName : ComboName) as ComboBox
                      ?? FindControl(host, ComboName) as ComboBox
                      ?? FindControl(host, FilterComboName) as ComboBox;
            if (cmb == null || cmb.SelectedItem == null)
                return allowAll ? AllOffices : "";
            return Convert.ToString(cmb.SelectedItem) ?? "";
        }

        public static void SetSelectedOffice(Control host, string office, bool isFilter = false)
        {
            var cmb = FindControl(host, isFilter ? FilterComboName : ComboName) as ComboBox;
            if (cmb == null) return;
            if (string.IsNullOrWhiteSpace(office))
            {
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
                return;
            }
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(cmb.Items[i]), office.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        public static bool RequireOfficeSelected(Control host)
        {
            string office = GetSelectedOffice(host, allowAll: false);
            if (string.IsNullOrWhiteSpace(office) || office == AllOffices)
            {
                new CustomeMessage("Office select karein (Hub Chowki / Band Murad / Askari)!", "Warning").ShowDialog();
                return false;
            }
            return true;
        }

        /// <summary>
        /// SQL AND clause. Empty / All = no filter. Also includes blank OfficeName rows when filtering
        /// so purana data dikhta rahe under All only — jab specific office ho to blank optional nahi.
        /// </summary>
        public static string OfficeSqlFilter(string columnAlias, string selectedOffice)
        {
            if (string.IsNullOrWhiteSpace(selectedOffice) || selectedOffice == AllOffices)
                return "";

            string col = string.IsNullOrWhiteSpace(columnAlias) ? "OfficeName" : columnAlias;
            string safe = selectedOffice.Replace("'", "''");
            return $" AND TRIM(IFNULL({col},'')) = '{safe}' ";
        }

        public static string GetDealerOffice(int did)
        {
            if (did <= 0) return "";
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT IFNULL(OfficeName,'') AS OfficeName FROM AddDealer WHERE Did=@id",
                new Hashtable { { "@id", did } });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToString(dt.Rows[0]["OfficeName"]) ?? "";
            return "";
        }

        public static string GetCustomerOffice(int customerId)
        {
            if (customerId <= 0) return "";
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT IFNULL(OfficeName,'') AS OfficeName FROM AddCustomer WHERE id=@id",
                new Hashtable { { "@id", customerId } });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToString(dt.Rows[0]["OfficeName"]) ?? "";
            return "";
        }

        public static string GetDealerOfficeByName(string dealerName)
        {
            if (string.IsNullOrWhiteSpace(dealerName)) return "";
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT IFNULL(OfficeName,'') AS OfficeName FROM AddDealer WHERE TRIM(DealerName)=@n COLLATE NOCASE LIMIT 1",
                new Hashtable { { "@n", dealerName.Trim() } });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToString(dt.Rows[0]["OfficeName"]) ?? "";
            return "";
        }

        public static string GetCustomerOfficeByName(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return "";
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT IFNULL(OfficeName,'') AS OfficeName FROM AddCustomer WHERE TRIM(Name)=@n COLLATE NOCASE LIMIT 1",
                new Hashtable { { "@n", customerName.Trim() } });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToString(dt.Rows[0]["OfficeName"]) ?? "";
            return "";
        }

        /// <summary>
        /// Agar entity pehle kisi aur office mein hai to Yes/No. Yes = continue, No = cancel.
        /// Blank home office = pehli assignment, confirm nahi.
        /// </summary>
        public static bool ConfirmCrossOfficeUse(string entityLabel, string entityName, string homeOffice, string targetOffice)
        {
            if (string.IsNullOrWhiteSpace(homeOffice) || string.IsNullOrWhiteSpace(targetOffice))
                return true;
            if (string.Equals(homeOffice.Trim(), targetOffice.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            string msg =
                $"Ye {entityLabel} pehle '{homeOffice.Trim()}' mein hai.\n" +
                $"Kya aap '{entityName}' ko '{targetOffice.Trim()}' mein use karna chahte hain?";

            using (var dlg = new YesOrNoMessage(msg, "Office Confirm"))
            {
                return dlg.ShowDialog() == DialogResult.Yes;
            }
        }

        public static bool ConfirmDealerForOffice(int did, string dealerName, string targetOffice)
        {
            string home = did > 0 ? GetDealerOffice(did) : GetDealerOfficeByName(dealerName);
            return ConfirmCrossOfficeUse("dealer", string.IsNullOrWhiteSpace(dealerName) ? ("#" + did) : dealerName, home, targetOffice);
        }

        public static bool ConfirmCustomerForOffice(int customerId, string customerName, string targetOffice)
        {
            string home = customerId > 0 ? GetCustomerOffice(customerId) : GetCustomerOfficeByName(customerName);
            return ConfirmCrossOfficeUse("customer", string.IsNullOrWhiteSpace(customerName) ? ("#" + customerId) : customerName, home, targetOffice);
        }

        private static Control FindControl(Control root, string name)
        {
            if (root == null) return null;
            Control[] hits = root.Controls.Find(name, true);
            return hits != null && hits.Length > 0 ? hits[0] : null;
        }
    }
}
