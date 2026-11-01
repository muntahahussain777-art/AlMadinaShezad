using Guna.UI2.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.ReportForm;
using AlMadinaShahzad.Services;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DigiKhataApp
{
    public partial class frmBankAccountView : SampleView
    {
        private const int ExcelSheetMinRows = 111;
        private const int ExcelSheetGrowBatch = 200;
        private readonly Timer _searchTimer = new Timer();
        private AutoCompleteStringCollection _dealerNames = new AutoCompleteStringCollection();
        private AutoCompleteStringCollection _customerNames = new AutoCompleteStringCollection();
        private AutoCompleteStringCollection _bankNames = new AutoCompleteStringCollection();
        private DataTable _dealerTable;
        private DataTable _customerTable;
        private Guna2Button _btnExcel;
        private Guna2Button _btnReport;
        private Guna2Button _btnAddBank;
        private Guna2Button _btnDeleteBank;
        private bool _loading;
        private bool _ignoreBankTextChange;
        private bool _batchGridUpdate;
        private bool _flushGridPending;
        private string _loadedBank = "";
        private int _saveRowInProgress = -1;
        private readonly Timer _bankTimer = new Timer();
        private ContextMenuStrip _bankDeleteMenu;
        private int? _dateFillStartRow;
        private string _dateFillValue;

        private static string LastBankAccountFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiselPetrolPump");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, "LastBankAccount.txt");
            }
        }

        private static string CustomBanksFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiselPetrolPump");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string file = Path.Combine(dir, "CustomBankNames.txt");
                string oldFile = Path.Combine(Application.StartupPath, "CustomBankNames.txt");
                if (!File.Exists(file) && File.Exists(oldFile))
                {
                    try { File.Copy(oldFile, file); }
                    catch { /* Purani install folder read-only ho sakti hai */ }
                }

                return file;
            }
        }

        private static readonly string[] DefaultBanks =
        {
            "Allied Bank","Askari Bank","Bank Al Habib","Bank Alfalah","Faysal Bank","HBL","MCB Bank","NBP","UBL",
            "Meezan Bank","HabibMetropolitanBank","JS Bank","Samba Bank","Soneri Bank","StandardCharteredBank",
            "Bank of Khyber","Bank of Punjab","Summit Bank","Silk Bank","Al Baraka Bank (Pakistan)",
            "(UPaisa)","FINCA Microfinance Bank","Apna Microfinance Bank","The First MicroFinance Bank",
            "JazzCash","EasyPaisa","SadaPay","NayaPay"
        };

        public frmBankAccountView()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyDown += FrmBankAccountView_KeyDown;
        }

        private void FrmBankAccountView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                LoadData();
                MessageBox.Show("Form reloaded successfully!", "Info");
            }
            else if (e.KeyCode == Keys.Delete && guna2DataGridView1.CurrentRow != null)
            {
                DeleteCurrentRow();
            }
        }

        private void frmBankAccountView_Load(object sender, EventArgs e)
        {
            label7.Text = "Total";
            txtSearch.PlaceholderText = "Find: name / given / taken amount";

            LoadLookups();
            SetupBankCombo();
            SetupBankDeleteMenu();
            SetupGrid();
            SetupExtraButtons();
            SetupAddBankButton();
            SetupResponsiveLayout();

            _searchTimer.Interval = 400;
            _searchTimer.Tick += (s, ev) => { _searchTimer.Stop(); ApplyGridSearch(); };

            _bankTimer.Interval = 400;
            _bankTimer.Tick += (s, ev) => { _bankTimer.Stop(); LoadData(); };

            RestoreLastActiveBank();
            VisibleChanged += FrmBankAccountView_VisibleChanged;
            Shown += (s, ev) => BeginInvoke(new Action(AfterBankGridShown));
        }

        private void FrmBankAccountView_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible || !IsHandleCreated || _loading) return;
            if (!HasSelectedBank()) return;
            BeginInvoke(new Action(() =>
            {
                if (!Visible || _loading) return;
                FlushGridDisplay(allRows: true, immediate: true);
            }));
        }

        private void AfterBankGridShown()
        {
            FlushGridDisplay(allRows: true);
            ScrollToLastDataEntry();
        }

        private void SetupResponsiveLayout()
        {
            panel1.Dock = DockStyle.Top;
            panel1.Height = 90;
            txtBank.Width = 185;

            var sumTitleFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            var sumValueFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label7.Font = sumTitleFont;
            lblBalance.Font = sumValueFont;
            lblBalance.AutoSize = false;
            label4.Visible = false;
            label5.Visible = false;
            lblIn.Visible = false;
            lblOut.Visible = false;

            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            if (_btnExcel != null)
                _btnExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (_btnReport != null)
                _btnReport.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            panel1.Resize += (s, e) => LayoutBankPanel();
            Resize += (s, e) =>
            {
                LayoutBankPanel();
                LayoutGridBounds();
            };
            LayoutBankPanel();
            LayoutGridBounds();
        }

        private void LayoutGridBounds()
        {
            int top = panel1.Height;
            guna2DataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            guna2DataGridView1.Location = new Point(0, top);
            guna2DataGridView1.Size = new Size(
                Math.Max(200, ClientSize.Width),
                Math.Max(100, ClientSize.Height - top));
            EnsureGridHeadersVisible();
        }

        private void EnsureGridHeadersVisible()
        {
            guna2DataGridView1.ColumnHeadersVisible = true;
            guna2DataGridView1.ColumnHeadersHeight = 40;
            guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            guna2DataGridView1.ThemeStyle.HeaderStyle.Height = 40;
            guna2DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(217, 225, 242);
            guna2DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.Black;
            guna2DataGridView1.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        private void LayoutBankPanel()
        {
            const int yLabel = 6;
            const int yInput = 34;
            const int left = 12;
            const int pad = 8;
            const int amountColW = 118;

            lblBank.Location = new Point(left, yLabel);
            txtBank.Location = new Point(left, yInput);

            if (_btnAddBank != null && _btnDeleteBank != null)
            {
                _btnAddBank.Location = new Point(txtBank.Right + 4, yInput);
                _btnDeleteBank.Location = new Point(_btnAddBank.Right + 4, yInput);
            }

            int rightEdge = Math.Max(400, panel1.ClientSize.Width - pad);
            btnAdd.Location = new Point(rightEdge - btnAdd.Width, yInput);

            if (_btnExcel != null && _btnReport != null)
            {
                _btnExcel.Location = new Point(rightEdge - btnAdd.Width - _btnExcel.Width - 6, yLabel);
                _btnReport.Location = new Point(_btnExcel.Left - _btnReport.Width - 6, yLabel);
            }

            int x = btnAdd.Left - pad;

            PlaceAmountValue(lblBalance, amountColW, ref x, yInput);
            PlaceAmountTitle(label7, ref x, yInput, pad);

            int searchLeft = (_btnDeleteBank?.Right ?? txtBank.Right) + 12;
            int searchWidth = Math.Max(90, x - pad - searchLeft);
            label1.Location = new Point(searchLeft, yLabel);
            txtSearch.Location = new Point(searchLeft, yInput);
            txtSearch.Width = searchWidth;
        }

        private static void PlaceAmountValue(Label valueLabel, int width, ref int rightX, int y)
        {
            rightX -= width;
            valueLabel.Width = width;
            valueLabel.Height = 30;
            valueLabel.Location = new Point(rightX, y);
            valueLabel.TextAlign = ContentAlignment.MiddleRight;
            FitAmountLabelFont(valueLabel, width);
        }

        private static void PlaceAmountTitle(Label titleLabel, ref int rightX, int y, int pad)
        {
            titleLabel.AutoSize = true;
            rightX -= pad;
            rightX -= titleLabel.PreferredWidth;
            titleLabel.Location = new Point(rightX, y);
        }

        private static void FitAmountLabelFont(Label lbl, int maxWidth)
        {
            string text = lbl.Text ?? "0.00";
            var font11 = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            var font9 = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lbl.Font = TextRenderer.MeasureText(text, font11).Width > maxWidth - 6 ? font9 : font11;
        }

        private void SetupExtraButtons()
        {
            _btnExcel = new Guna2Button
            {
                Text = "Excel",
                Size = new Size(90, 33),
                Location = new Point(915, 5),
                BorderRadius = 10,
                FillColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };
            _btnExcel.Click += (s, e) => ExportExcel();
            panel1.Controls.Add(_btnExcel);

            _btnReport = new Guna2Button
            {
                Text = "Report",
                Size = new Size(90, 33),
                Location = new Point(820, 5),
                BorderRadius = 10,
                FillColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };
            _btnReport.Click += (s, e) => ShowReport();
            panel1.Controls.Add(_btnReport);
        }

        private void LoadLookups()
        {
            _dealerTable = MainClass.ExecuteSelectQuery("SELECT Did, DealerName FROM AddDealer", null);
            _customerTable = MainClass.ExecuteSelectQuery("SELECT id, Name FROM AddCustomer", null);

            _dealerNames = new AutoCompleteStringCollection();
            _customerNames = new AutoCompleteStringCollection();
            if (_dealerTable != null)
                foreach (DataRow r in _dealerTable.Rows)
                    _dealerNames.Add(r["DealerName"].ToString());
            if (_customerTable != null)
                foreach (DataRow r in _customerTable.Rows)
                    _customerNames.Add(r["Name"].ToString());

            _bankNames = new AutoCompleteStringCollection();
            _bankNames.AddRange(DefaultBanks);
            var banks = MainClass.ExecuteSelectQuery(
                "SELECT DISTINCT BankName FROM BankTransactions WHERE IFNULL(BankName,'') <> '' ORDER BY BankName", null);
            if (banks != null)
                foreach (DataRow r in banks.Rows)
                {
                    string b = r["BankName"].ToString();
                    AddBankNameToList(b, persist: false);
                }

            LoadCustomBankNames();
        }

        private void LoadCustomBankNames()
        {
            if (!File.Exists(CustomBanksFile)) return;
            foreach (string line in File.ReadAllLines(CustomBanksFile))
                AddBankNameToList(line, persist: false);
        }

        private void AddBankNameToList(string bankName, bool persist)
        {
            bankName = (bankName ?? "").Trim();
            if (string.IsNullOrEmpty(bankName)) return;

            bool exists = false;
            foreach (string existing in _bankNames)
            {
                if (string.Equals(existing, bankName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                _bankNames.Add(bankName);
                if (txtBank != null)
                    txtBank.AutoCompleteCustomSource = _bankNames;

                if (persist)
                {
                    string dir = Path.GetDirectoryName(CustomBanksFile);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.AppendAllText(CustomBanksFile, bankName + Environment.NewLine);
                }
            }
        }

        private void SetupBankCombo()
        {
            txtBank.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtBank.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtBank.AutoCompleteCustomSource = _bankNames;
            txtBank.Leave += TxtBank_Leave;
        }

        private void TxtBank_Leave(object sender, EventArgs e)
        {
            _bankTimer.Stop();
            if (HasSelectedBank())
                LoadData();
        }

        private void SetupBankDeleteMenu()
        {
            _bankDeleteMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete Bank (Complete Record)");
            deleteItem.Click += (s, e) => DeleteBankCompletely();
            _bankDeleteMenu.Items.Add(deleteItem);
            txtBank.ContextMenuStrip = _bankDeleteMenu;
        }

        private string ResolveBankNameForDelete()
        {
            string text = (txtBank.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
                return "";

            string active = GetActiveBankName();
            if (!string.IsNullOrEmpty(active))
                return active;

            DataTable exact = MainClass.ExecuteSelectQuery(
                "SELECT DISTINCT BankName FROM BankTransactions WHERE TRIM(IFNULL(BankName,'')) = @Bank COLLATE NOCASE LIMIT 1",
                new Hashtable { { "@Bank", text } });
            if (exact != null && exact.Rows.Count > 0)
                return Convert.ToString(exact.Rows[0]["BankName"]);

            DataTable like = MainClass.ExecuteSelectQuery(
                "SELECT DISTINCT BankName FROM BankTransactions WHERE IFNULL(BankName,'') LIKE @Like COLLATE NOCASE",
                new Hashtable { { "@Like", "%" + text + "%" } });
            if (like != null && like.Rows.Count == 1)
                return Convert.ToString(like.Rows[0]["BankName"]);

            return text;
        }

        private void DeleteBankCompletely()
        {
            string bankName = ResolveBankNameForDelete();
            if (string.IsNullOrEmpty(bankName))
            {
                ShowPickBankToDeleteDialog();
                return;
            }

            DeleteBankByName(bankName);
        }

        private void DeleteBankByName(string bankName)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return;

            bankName = bankName.Trim();

            int rowCount = 0;
            DataTable cnt = MainClass.ExecuteSelectQuery(
                "SELECT COUNT(*) AS C FROM BankTransactions WHERE TRIM(IFNULL(BankName,'')) = @Bank COLLATE NOCASE",
                new Hashtable { { "@Bank", bankName } });
            if (cnt != null && cnt.Rows.Count > 0)
                int.TryParse(Convert.ToString(cnt.Rows[0]["C"]), out rowCount);

            string confirm = rowCount > 0
                ? $"Kya '{bankName}' bank ki saari {rowCount} entries permanently delete karein?\nYe wapas nahi aayengi."
                : $"Kya '{bankName}' bank ko list se hata dein?\n(Is naam ki koi transaction entry nahi mili.)";

            if (MessageBox.Show(confirm, "Confirm Delete Bank", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (rowCount > 0)
                {
                    int deleted = MainClass.DataInsertUpdateDelete(
                        "DELETE FROM BankTransactions WHERE TRIM(IFNULL(BankName,'')) = @Bank COLLATE NOCASE",
                        new Hashtable { { "@Bank", bankName } });
                    if (deleted < 0)
                    {
                        MessageBox.Show("Bank delete nahi hui. Dobara try karein.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                RemoveBankFromCustomFile(bankName);
                RemoveBankFromAutocomplete(bankName);
                txtBank.Text = "";
                LoadData();
                MessageBox.Show($"'{bankName}' bank delete ho gayi.", "Deleted",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bank delete karte waqt error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPickBankToDeleteDialog()
        {
            var bankList = new System.Collections.Generic.List<string>();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DataTable dbBanks = MainClass.ExecuteSelectQuery(
                "SELECT DISTINCT BankName FROM BankTransactions WHERE IFNULL(BankName,'') <> '' ORDER BY BankName", null);
            if (dbBanks != null)
            {
                foreach (DataRow row in dbBanks.Rows)
                {
                    string name = Convert.ToString(row["BankName"])?.Trim();
                    if (!string.IsNullOrEmpty(name) && seen.Add(name))
                        bankList.Add(name);
                }
            }

            foreach (string name in _bankNames)
            {
                string trimmed = (name ?? "").Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    bankList.Add(trimmed);
            }

            if (bankList.Count == 0)
            {
                MessageBox.Show("Delete karne ke liye koi bank nahi mili.\nPehle bank add karein.", "No Bank",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bankList.Sort(StringComparer.OrdinalIgnoreCase);

            using (var dlg = new Form())
            {
                dlg.Text = "Bank Delete Karein";
                dlg.ClientSize = new Size(420, 150);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(32, 36, 66);

                var lbl = new Label
                {
                    Text = "Kaunsi bank delete karni hai?",
                    ForeColor = Color.White,
                    Location = new Point(14, 14),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                };

                var cbo = new ComboBox
                {
                    Location = new Point(14, 42),
                    Size = new Size(392, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10F)
                };
                cbo.Items.AddRange(bankList.ToArray());
                if (cbo.Items.Count > 0)
                    cbo.SelectedIndex = 0;

                var btnDelete = new Guna2Button
                {
                    Text = "Delete",
                    Location = new Point(220, 88),
                    Size = new Size(90, 34),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(192, 57, 43),
                    ForeColor = Color.White
                };
                var btnCancel = new Guna2Button
                {
                    Text = "Cancel",
                    Location = new Point(316, 88),
                    Size = new Size(90, 34),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(100, 88, 255),
                    ForeColor = Color.White
                };

                btnDelete.Click += (s, ev) =>
                {
                    if (cbo.SelectedItem == null)
                        return;
                    dlg.DialogResult = DialogResult.OK;
                };
                btnCancel.Click += (s, ev) => dlg.DialogResult = DialogResult.Cancel;

                dlg.Controls.AddRange(new Control[] { lbl, cbo, btnDelete, btnCancel });

                if (dlg.ShowDialog(this) == DialogResult.OK && cbo.SelectedItem != null)
                    DeleteBankByName(cbo.SelectedItem.ToString());
            }
        }

        private void RemoveBankFromCustomFile(string bankName)
        {
            if (!File.Exists(CustomBanksFile))
                return;

            var keep = new StringBuilder();
            foreach (string line in File.ReadAllLines(CustomBanksFile))
            {
                if (!string.Equals(line.Trim(), bankName, StringComparison.OrdinalIgnoreCase))
                    keep.AppendLine(line);
            }
            File.WriteAllText(CustomBanksFile, keep.ToString());
        }

        private void RemoveBankFromAutocomplete(string bankName)
        {
            var updated = new AutoCompleteStringCollection();
            foreach (string name in _bankNames)
            {
                if (!string.Equals(name, bankName, StringComparison.OrdinalIgnoreCase))
                    updated.Add(name);
            }
            _bankNames = updated;
            txtBank.AutoCompleteCustomSource = _bankNames;
        }

        private string GetActiveBankName()
        {
            string text = (txtBank.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
                return "";

            foreach (string bank in _bankNames)
            {
                if (string.Equals(bank, text, StringComparison.OrdinalIgnoreCase))
                    return bank;
            }

            return text;
        }

        private bool HasSelectedBank()
        {
            return !string.IsNullOrWhiteSpace(GetActiveBankName());
        }

        private void RememberLastActiveBank(string bankName)
        {
            bankName = (bankName ?? "").Trim();
            if (string.IsNullOrEmpty(bankName)) return;
            try { File.WriteAllText(LastBankAccountFile, bankName); }
            catch { /* ignore */ }
        }

        private void RestoreLastActiveBank()
        {
            try
            {
                if (!File.Exists(LastBankAccountFile)) return;
                string bank = File.ReadAllText(LastBankAccountFile).Trim();
                if (string.IsNullOrEmpty(bank)) return;
                AddBankNameToList(bank, persist: false);
                _bankTimer.Stop();
                _ignoreBankTextChange = true;
                try { txtBank.Text = bank; }
                finally { _ignoreBankTextChange = false; }
                LoadData();
            }
            catch { /* ignore */ }
        }

        private void ScrollToLastDataEntry()
        {
            if (guna2DataGridView1.Rows.Count == 0) return;

            int lastDataRow = -1;
            for (int i = guna2DataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                var row = guna2DataGridView1.Rows[i];
                if (!row.Visible) continue;

                bool hasId = row.Cells["Id"].Value != null && row.Cells["Id"].Value != DBNull.Value
                    && int.TryParse(row.Cells["Id"].Value.ToString(), out int id) && id > 0;
                bool hasAmount = HasValue(row.Cells["Debit"].Value) || HasValue(row.Cells["Credit"].Value);
                if (hasId || hasAmount)
                {
                    lastDataRow = i;
                    break;
                }
            }

            if (lastDataRow < 0) return;

            try
            {
                int scrollTo = Math.Max(0, lastDataRow - 2);
                guna2DataGridView1.FirstDisplayedScrollingRowIndex = scrollTo;
                if (guna2DataGridView1.Columns.Contains("TransactionDate"))
                    guna2DataGridView1.CurrentCell = guna2DataGridView1.Rows[lastDataRow].Cells["TransactionDate"];
            }
            catch { /* grid not ready */ }
        }

        private void RememberBankName(string bankName)
        {
            AddBankNameToList(bankName, persist: true);
        }

        private void SetupAddBankButton()
        {
            _btnAddBank = new Guna2Button
            {
                Text = "+",
                Size = new Size(34, 34),
                BorderRadius = 10,
                FillColor = Color.FromArgb(44, 215, 207),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            };
            _btnAddBank.Click += BtnAddBank_Click;
            panel1.Controls.Add(_btnAddBank);

            _btnDeleteBank = new Guna2Button
            {
                Text = "Del",
                Size = new Size(42, 34),
                BorderRadius = 10,
                FillColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            _btnDeleteBank.Click += (s, e) => DeleteBankCompletely();
            panel1.Controls.Add(_btnDeleteBank);
        }

        private void BtnAddBank_Click(object sender, EventArgs e)
        {
            string currentBank = GetActiveBankName();

            using (var dlg = new Form())
            {
                dlg.Text = "Bank / Account Name";
                dlg.ClientSize = new Size(400, 155);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(32, 36, 66);

                var lbl = new Label
                {
                    Text = string.IsNullOrEmpty(currentBank)
                        ? "Naya bank / account naam:"
                        : "Naam (Add naya YA Update current '" + currentBank + "'):",
                    ForeColor = Color.White,
                    Location = new Point(14, 12),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
                };

                var txtNewBank = new Guna2TextBox
                {
                    Location = new Point(14, 38),
                    Size = new Size(372, 36),
                    BorderRadius = 17,
                    BorderColor = Color.FromArgb(112, 51, 255),
                    FillColor = Color.FromArgb(37, 41, 74),
                    ForeColor = Color.White,
                    PlaceholderText = "e.g. irtazameezan",
                    Text = currentBank ?? ""
                };

                var btnAdd = new Guna2Button
                {
                    Text = "Add New",
                    Location = new Point(90, 90),
                    Size = new Size(90, 32),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(44, 215, 207),
                    ForeColor = Color.White
                };

                var btnUpdate = new Guna2Button
                {
                    Text = "Update",
                    Location = new Point(190, 90),
                    Size = new Size(90, 32),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(241, 196, 15),
                    ForeColor = Color.Black,
                    Enabled = !string.IsNullOrEmpty(currentBank)
                };

                var btnCancel = new Guna2Button
                {
                    Text = "Cancel",
                    Location = new Point(290, 90),
                    Size = new Size(90, 32),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(100, 88, 255),
                    ForeColor = Color.White
                };

                string mode = "";
                btnAdd.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtNewBank.Text))
                    {
                        MessageBox.Show("Bank ka naam likhein.", "Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    mode = "add";
                    dlg.DialogResult = DialogResult.OK;
                };
                btnUpdate.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtNewBank.Text))
                    {
                        MessageBox.Show("Naya bank ka naam likhein.", "Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (string.IsNullOrEmpty(currentBank))
                    {
                        MessageBox.Show("Pehle upar se bank select karein, phir Update karein.", "Update",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    mode = "update";
                    dlg.DialogResult = DialogResult.OK;
                };
                btnCancel.Click += (s, ev) => dlg.DialogResult = DialogResult.Cancel;
                txtNewBank.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter)
                    {
                        ev.SuppressKeyPress = true;
                        if (btnUpdate.Enabled) btnUpdate.PerformClick();
                        else btnAdd.PerformClick();
                    }
                };

                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txtNewBank);
                dlg.Controls.Add(btnAdd);
                dlg.Controls.Add(btnUpdate);
                dlg.Controls.Add(btnCancel);

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                string name = txtNewBank.Text.Trim();
                if (mode == "update")
                    RenameBank(currentBank, name);
                else
                    AddNewBankSafe(name);
            }
        }

        private bool BankNameExists(string bankName, string exceptName = null)
        {
            bankName = (bankName ?? "").Trim();
            if (string.IsNullOrEmpty(bankName)) return false;

            foreach (string existing in _bankNames)
            {
                if (!string.IsNullOrEmpty(exceptName)
                    && string.Equals(existing, exceptName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(existing, bankName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT 1 FROM BankTransactions WHERE TRIM(IFNULL(BankName,'')) = @Bank COLLATE NOCASE LIMIT 1",
                new Hashtable { { "@Bank", bankName } });
            return dt != null && dt.Rows.Count > 0;
        }

        private void AddNewBankSafe(string name)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (BankNameExists(name))
            {
                MessageBox.Show("Ye bank naam pehle se maujood hai.\nDuplicate nahi banega.\n\nRename ke liye pehle bank select karke Update dabayein.",
                    "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                foreach (string existing in _bankNames)
                {
                    if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                    {
                        txtBank.Text = existing;
                        break;
                    }
                }
                LoadData();
                return;
            }

            AddBankNameToList(name, persist: true);
            txtBank.Text = name;
            RememberLastActiveBank(name);
            LoadData();
        }

        private void RenameBank(string oldName, string newName)
        {
            oldName = (oldName ?? "").Trim();
            newName = (newName ?? "").Trim();
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return;

            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Naam same hai — koi change nahi.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (BankNameExists(newName, exceptName: oldName))
            {
                MessageBox.Show("'" + newName + "' pehle se maujood hai.\nDuplicate naam nahi ho sakta.",
                    "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Kya '{oldName}' ko '{newName}' mein rename/update karein?\nSaari bank entries ka naam update ho jayega.",
                    "Confirm Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                MainClass.DataInsertUpdateDelete(
                    "UPDATE BankTransactions SET BankName = @New WHERE TRIM(IFNULL(BankName,'')) = @Old COLLATE NOCASE",
                    new Hashtable { { "@New", newName }, { "@Old", oldName } });

                ReplaceCustomBankName(oldName, newName);
                RemoveBankFromAutocomplete(oldName);
                AddBankNameToList(newName, persist: true);

                _ignoreBankTextChange = true;
                try { txtBank.Text = newName; }
                finally { _ignoreBankTextChange = false; }

                RememberLastActiveBank(newName);
                LoadData();
                MessageBox.Show("Bank naam update ho gaya.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReplaceCustomBankName(string oldName, string newName)
        {
            try
            {
                if (!File.Exists(CustomBanksFile))
                {
                    File.AppendAllText(CustomBanksFile, newName + Environment.NewLine);
                    return;
                }

                var lines = new List<string>();
                bool replaced = false;
                foreach (string line in File.ReadAllLines(CustomBanksFile))
                {
                    string t = (line ?? "").Trim();
                    if (string.IsNullOrEmpty(t)) continue;
                    if (string.Equals(t, oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!replaced)
                        {
                            lines.Add(newName);
                            replaced = true;
                        }
                        continue;
                    }
                    if (string.Equals(t, newName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    lines.Add(t);
                }
                if (!replaced)
                    lines.Add(newName);
                File.WriteAllLines(CustomBanksFile, lines);
            }
            catch { /* ignore */ }
        }

        private void txtBank_TextChanged(object sender, EventArgs e)
        {
            if (_ignoreBankTextChange) return;
            _bankTimer.Stop();
            _bankTimer.Start();
        }

        private void SetupGrid()
        {
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, guna2DataGridView1, new object[] { true });

            guna2DataGridView1.AllowUserToAddRows = false;
            guna2DataGridView1.ReadOnly = false;
            guna2DataGridView1.EditMode = DataGridViewEditMode.EditOnEnter;

            EnsureColumnsExist();
            SetColumnOrder();
            ApplyExcelGridStyle();
            ApplyGridColumnFillWeights();
            EnsureGridHeadersVisible();

            if (guna2DataGridView1.Columns.Contains("dgvid"))
                guna2DataGridView1.Columns["dgvid"].Visible = false;
            SetupSrColumn();

            foreach (DataGridViewColumn c in guna2DataGridView1.Columns)
            {
                if (c.Name == "Balance" || c.Name == "dgvSr" || c.Name == "Id" || c.Name == "dgvid" || c.Name == "BankName"
                    || c.Name == "DealerName" || c.Name == "CustomerName" || c.Name == "Note")
                    c.ReadOnly = true;
                else if (c.Name != "dgvSr")
                    c.ReadOnly = false;
            }

            guna2DataGridView1.EditingControlShowing += Grid_EditingControlShowing;
            guna2DataGridView1.CellEndEdit += Grid_CellEndEdit;
            guna2DataGridView1.DefaultValuesNeeded += Grid_DefaultValuesNeeded;
            guna2DataGridView1.KeyDown += Grid_KeyDown;
            guna2DataGridView1.RowsAdded += (s, ev) =>
            {
                if (!_loading && !_batchGridUpdate)
                    RefreshSrNo();
            };
            guna2DataGridView1.CellPainting += Grid_CellPainting;
            guna2DataGridView1.Scroll += Grid_Scroll;
            guna2DataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            guna2DataGridView1.CellMouseDown += Grid_CellMouseDown;
            guna2DataGridView1.CellMouseMove += Grid_CellMouseMove;
            guna2DataGridView1.CellMouseUp += Grid_CellMouseUp;
        }

        private void Grid_Scroll(object sender, ScrollEventArgs e)
        {
            if (_loading) return;
            int first = guna2DataGridView1.FirstDisplayedScrollingRowIndex;
            if (first < 0) return;
            int visible = guna2DataGridView1.DisplayedRowCount(false);
            if (first + visible >= guna2DataGridView1.Rows.Count - 10)
                GrowExcelSheetRows(ExcelSheetGrowBatch);
        }

        private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            _dateFillStartRow = null;
            _dateFillValue = null;
            if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "TransactionDate") return;

            // Sirf cell ke bottom-right corner (fill handle) se drag — warna normal click/edit date spoil nahi karega
            Rectangle cellRect = guna2DataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            const int handle = 10;
            bool onFillHandle = e.X >= cellRect.Width - handle && e.Y >= cellRect.Height - handle;
            if (!onFillHandle) return;

            string val = guna2DataGridView1.Rows[e.RowIndex].Cells["TransactionDate"].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(val)) return;

            _dateFillStartRow = e.RowIndex;
            _dateFillValue = val;
        }

        private void Grid_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !_dateFillStartRow.HasValue) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "TransactionDate") return;
            if (e.RowIndex <= _dateFillStartRow.Value) return;

            EnsureRowIndexExists(e.RowIndex);
            for (int r = _dateFillStartRow.Value + 1; r <= e.RowIndex; r++)
            {
                // Sirf khali date rows fill — pehle se likhi date overwrite mat karo
                object existing = guna2DataGridView1.Rows[r].Cells["TransactionDate"].Value;
                if (existing != null && !string.IsNullOrWhiteSpace(existing.ToString()))
                    continue;
                guna2DataGridView1.Rows[r].Cells["TransactionDate"].Value = _dateFillValue;
            }
            RefreshSrNo();
        }

        private void Grid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            bool wasDateFill = _dateFillStartRow.HasValue;
            _dateFillStartRow = null;
            _dateFillValue = null;
            if (wasDateFill && !_loading)
                CalculateRunningBalance();
        }

        private void SetupSrColumn()
        {
            if (!guna2DataGridView1.Columns.Contains("dgvSr")) return;
            var sr = guna2DataGridView1.Columns["dgvSr"];
            sr.Visible = true;
            sr.HeaderText = "Sr";
            sr.ReadOnly = true;
            sr.Width = 45;
            sr.MinimumWidth = 40;
            sr.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            sr.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            sr.DefaultCellStyle.ForeColor = Color.Black;
            sr.DefaultCellStyle.BackColor = Color.White;
            sr.DisplayIndex = 0;
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0) return;
            if (!guna2DataGridView1.Columns[e.ColumnIndex].Visible) return;

            using (var back = new SolidBrush(Color.FromArgb(217, 225, 242)))
                e.Graphics.FillRectangle(back, e.CellBounds);

            string text = guna2DataGridView1.Columns[e.ColumnIndex].HeaderText ?? "";
            var rect = e.CellBounds;
            rect.Inflate(-6, 0);
            TextRenderer.DrawText(e.Graphics, text, new Font("Calibri", 13F, FontStyle.Bold), rect, Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            using (var pen = new Pen(Color.FromArgb(180, 180, 180)))
                e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);

            e.Handled = true;
        }

        private void ApplyExcelGridStyle()
        {
            const float fontSize = 13F;
            var excelFont = new Font("Calibri", fontSize);
            var headerFont = new Font("Calibri", fontSize, FontStyle.Bold);
            Color headerBack = Color.FromArgb(217, 225, 242);
            Color gridLine = Color.FromArgb(180, 180, 180);
            Color cellBack = Color.White;
            Color cellFore = Color.Black;
            Color selectionBack = Color.FromArgb(51, 153, 255);
            Color selectionFore = Color.White;

            guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            guna2DataGridView1.BackgroundColor = cellBack;
            guna2DataGridView1.GridColor = gridLine;
            guna2DataGridView1.BorderStyle = BorderStyle.None;
            guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            guna2DataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            guna2DataGridView1.RowHeadersVisible = false;
            guna2DataGridView1.EnableHeadersVisualStyles = false;
            guna2DataGridView1.ColumnHeadersVisible = true;
            guna2DataGridView1.ColumnHeadersHeight = 36;
            guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            guna2DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = cellBack;
            guna2DataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = cellFore;
            guna2DataGridView1.RowTemplate.Height = 30;

            guna2DataGridView1.DefaultCellStyle.BackColor = cellBack;
            guna2DataGridView1.DefaultCellStyle.ForeColor = cellFore;
            guna2DataGridView1.DefaultCellStyle.SelectionBackColor = selectionBack;
            guna2DataGridView1.DefaultCellStyle.SelectionForeColor = selectionFore;
            guna2DataGridView1.DefaultCellStyle.Font = excelFont;
            guna2DataGridView1.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);

            guna2DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = headerBack;
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = cellFore;
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.Font = headerFont;
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBack;
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = cellFore;
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            guna2DataGridView1.ThemeStyle.BackColor = cellBack;
            guna2DataGridView1.ThemeStyle.GridColor = gridLine;
            guna2DataGridView1.ThemeStyle.RowsStyle.BackColor = cellBack;
            guna2DataGridView1.ThemeStyle.RowsStyle.ForeColor = cellFore;
            guna2DataGridView1.ThemeStyle.RowsStyle.Font = excelFont;
            guna2DataGridView1.ThemeStyle.RowsStyle.SelectionBackColor = selectionBack;
            guna2DataGridView1.ThemeStyle.RowsStyle.SelectionForeColor = selectionFore;
            guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.BackColor = cellBack;
            guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.ForeColor = cellFore;
            guna2DataGridView1.ThemeStyle.HeaderStyle.BackColor = headerBack;
            guna2DataGridView1.ThemeStyle.HeaderStyle.ForeColor = cellFore;
            guna2DataGridView1.ThemeStyle.HeaderStyle.Font = headerFont;
            guna2DataGridView1.ThemeStyle.HeaderStyle.Height = 36;
            guna2DataGridView1.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.Single;
            guna2DataGridView1.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            foreach (DataGridViewColumn column in guna2DataGridView1.Columns)
            {
                column.DefaultCellStyle.ForeColor = cellFore;
                column.DefaultCellStyle.Font = excelFont;
                column.DefaultCellStyle.BackColor = column.Name == "Balance"
                    ? Color.FromArgb(242, 242, 242)
                    : cellBack;
            }

            if (guna2DataGridView1.Columns.Contains("Debit") || guna2DataGridView1.Columns.Contains("Credit"))
            {
                var numStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    BackColor = cellBack,
                    ForeColor = cellFore,
                    Font = excelFont,
                    Format = "N2"
                };
                if (guna2DataGridView1.Columns.Contains("Debit"))
                {
                    var debitStyle = (DataGridViewCellStyle)numStyle.Clone();
                    debitStyle.ForeColor = Color.FromArgb(192, 0, 0);
                    guna2DataGridView1.Columns["Debit"].DefaultCellStyle = debitStyle;
                }
                if (guna2DataGridView1.Columns.Contains("Credit"))
                    guna2DataGridView1.Columns["Credit"].DefaultCellStyle = numStyle;
                if (guna2DataGridView1.Columns.Contains("Balance"))
                {
                    var balStyle = (DataGridViewCellStyle)numStyle.Clone();
                    balStyle.BackColor = Color.FromArgb(242, 242, 242);
                    balStyle.ForeColor = cellFore;
                    guna2DataGridView1.Columns["Balance"].DefaultCellStyle = balStyle;
                }
            }
        }

        private void ApplyGridColumnFillWeights()
        {
            SetColFillWeight("TransactionDate", 12F);
            SetColFillWeight("Name", 32F);
            SetColFillWeight("Debit", 18F);
            SetColFillWeight("Credit", 18F);
            SetColFillWeight("Balance", 20F);
            guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void SetColFillWeight(string name, float weight)
        {
            if (!guna2DataGridView1.Columns.Contains(name)) return;
            var col = guna2DataGridView1.Columns[name];
            if (!col.Visible) return;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col.FillWeight = weight;
        }

        private void RefreshSrNo()
        {
            int count = 0;
            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (!row.Visible) continue;
                count++;
                if (guna2DataGridView1.Columns.Contains("dgvSr"))
                    row.Cells["dgvSr"].Value = count;
            }
        }

        private void SetColumnOrder()
        {
            int i = 0;
            if (guna2DataGridView1.Columns.Contains("dgvSr")) guna2DataGridView1.Columns["dgvSr"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("TransactionDate")) guna2DataGridView1.Columns["TransactionDate"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Name")) guna2DataGridView1.Columns["Name"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Debit")) guna2DataGridView1.Columns["Debit"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Credit")) guna2DataGridView1.Columns["Credit"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Balance")) guna2DataGridView1.Columns["Balance"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("DealerName")) guna2DataGridView1.Columns["DealerName"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("CustomerName")) guna2DataGridView1.Columns["CustomerName"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("BankName")) guna2DataGridView1.Columns["BankName"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Note")) guna2DataGridView1.Columns["Note"].DisplayIndex = i++;
            if (guna2DataGridView1.Columns.Contains("Id")) guna2DataGridView1.Columns["Id"].DisplayIndex = i++;
        }

        private void FlushGridDisplay(int rowIndex = -1, bool allRows = false, bool immediate = false)
        {
            if (IsDisposed || guna2DataGridView1.IsDisposed) return;
            if (_batchGridUpdate && !immediate) return;
            if (_flushGridPending && !immediate) return;

            void DoFlush()
            {
                if (IsDisposed || guna2DataGridView1.IsDisposed) return;
                _flushGridPending = false;
                try { guna2DataGridView1.EndEdit(DataGridViewDataErrorContexts.Commit); } catch { }

                if (allRows || rowIndex < 0)
                    guna2DataGridView1.Invalidate();
                else if (rowIndex < guna2DataGridView1.Rows.Count)
                    guna2DataGridView1.InvalidateRow(rowIndex);
            }

            if (immediate)
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(DoFlush));
                else
                    DoFlush();
                return;
            }

            _flushGridPending = true;
            BeginInvoke(new Action(DoFlush));
        }

        private static void FormatAmountCell(DataGridViewCell cell)
        {
            if (cell?.Value == null || cell.Value == DBNull.Value) return;
            string raw = cell.Value.ToString().Replace(",", "");
            if (decimal.TryParse(raw, out decimal amt) && amt != 0)
                cell.Value = Math.Abs(amt).ToString("F2");
        }

        private void Grid_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["TransactionDate"].Value = "";
            e.Row.Cells["BankName"].Value = GetActiveBankName();
            if (guna2DataGridView1.Columns.Contains("Name"))
                e.Row.Cells["Name"].Value = "";
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                if (TryPasteIntoCurrentCell())
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                int col = guna2DataGridView1.CurrentCell?.ColumnIndex ?? -1;
                int row = guna2DataGridView1.CurrentCell?.RowIndex ?? -1;
                if (col < 0 || row < 0) return;
                int nextCol = col + 1;
                while (nextCol < guna2DataGridView1.Columns.Count)
                {
                    string n = guna2DataGridView1.Columns[nextCol].Name;
                    if (n == "TransactionDate" || n == "Name" || n == "Debit" || n == "Credit")
                    {
                        guna2DataGridView1.CurrentCell = guna2DataGridView1.Rows[row].Cells[nextCol];
                        guna2DataGridView1.BeginEdit(true);
                        return;
                    }
                    nextCol++;
                }
                if (row < guna2DataGridView1.Rows.Count - 1)
                {
                    guna2DataGridView1.CurrentCell = guna2DataGridView1.Rows[row + 1].Cells["TransactionDate"];
                    guna2DataGridView1.BeginEdit(true);
                }
            }
        }

        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (!(e.Control is TextBox tb)) return;
            tb.AutoCompleteMode = AutoCompleteMode.None;
            tb.AutoCompleteSource = AutoCompleteSource.None;
            tb.ForeColor = Color.Black;
            tb.BackColor = Color.White;
            tb.Font = new Font("Calibri", 13F);
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || e.RowIndex < 0) return;
            var row = guna2DataGridView1.Rows[e.RowIndex];

            string col = guna2DataGridView1.Columns[e.ColumnIndex].Name;
            if (col == "Debit" && HasValue(row.Cells["Debit"].Value))
                row.Cells["Credit"].Value = DBNull.Value;
            else if (col == "Credit" && HasValue(row.Cells["Credit"].Value))
                row.Cells["Debit"].Value = DBNull.Value;

            if (col == "Name")
                ApplyNameToHiddenFields(row);
            else if (col == "TransactionDate" && TryParseGridDate(row.Cells["TransactionDate"].Value, out DateTime parsedDate))
                row.Cells["TransactionDate"].Value = FormatExcelDate(parsedDate);
            else if (col == "Debit" || col == "Credit")
                FormatAmountCell(row.Cells[col]);

            if (IsRowReadyToSave(row))
                SaveRow(e.RowIndex);
            else
                CalculateRunningBalance();

            ApplyGridSearch();
            FlushGridDisplay(e.RowIndex, allRows: col == "Debit" || col == "Credit", immediate: true);
        }

        private static bool HasValue(object v)
        {
            if (v == null || v == DBNull.Value) return false;
            return decimal.TryParse(v.ToString(), out decimal d) && d != 0;
        }

        private bool IsRowReadyToSave(DataGridViewRow row)
        {
            bool hasMoney = HasValue(row.Cells["Debit"].Value) || HasValue(row.Cells["Credit"].Value);
            bool hasDate = row.Cells["TransactionDate"].Value != null &&
                           !string.IsNullOrWhiteSpace(row.Cells["TransactionDate"].Value.ToString());
            return hasMoney && hasDate;
        }

        private bool TryPasteIntoCurrentCell()
        {
            if (guna2DataGridView1.CurrentCell == null) return false;
            string text = Clipboard.GetText()?.Trim();
            if (string.IsNullOrEmpty(text)) return false;

            var cell = guna2DataGridView1.CurrentCell;
            var row = guna2DataGridView1.Rows[cell.RowIndex];
            string col = cell.OwningColumn.Name;

            if (col == "TransactionDate")
            {
                if (DateTime.TryParse(text, out DateTime dt))
                    cell.Value = FormatExcelDate(dt);
                else
                    cell.Value = text;
            }
            else if (col == "Debit" || col == "Credit")
            {
                string num = text.Replace(",", "");
                if (decimal.TryParse(num, out decimal amt))
                    cell.Value = Math.Abs(amt).ToString("F2");
                else
                    cell.Value = text;
            }
            else if (col == "Name")
            {
                cell.Value = text;
                ApplyNameToHiddenFields(row);
            }
            else
                return false;

            if (col == "Debit" && HasValue(cell.Value))
                row.Cells["Credit"].Value = DBNull.Value;
            else if (col == "Credit" && HasValue(cell.Value))
                row.Cells["Debit"].Value = DBNull.Value;

            if (col == "Debit" || col == "Credit")
                FormatAmountCell(cell);

            if (IsRowReadyToSave(row))
                SaveRow(cell.RowIndex);
            else
                CalculateRunningBalance();

            ApplyGridSearch();
            FlushGridDisplay(cell.RowIndex, allRows: col == "Debit" || col == "Credit", immediate: true);
            return true;
        }

        private void ApplyGridSearch()
        {
            if (_loading) return;
            string term = (txtSearch.Text ?? "").Trim();

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (string.IsNullOrEmpty(term))
                    row.Visible = true;
                else
                    row.Visible = RowMatchesSearch(row, term);
            }

            RefreshSrNo();
        }

        private static bool RowMatchesSearch(DataGridViewRow row, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;

            string name = row.Cells["Name"].Value?.ToString() ?? "";
            string debit = row.Cells["Debit"].Value?.ToString() ?? "";
            string credit = row.Cells["Credit"].Value?.ToString() ?? "";

            if (name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (debit.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (credit.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            string numTerm = term.Replace(",", "");
            if (decimal.TryParse(numTerm, out decimal searchAmt))
            {
                if (decimal.TryParse((debit ?? "").Replace(",", ""), out decimal d) && d == searchAmt) return true;
                if (decimal.TryParse((credit ?? "").Replace(",", ""), out decimal c) && c == searchAmt) return true;
            }

            return false;
        }

        private enum BankSearchScope { Both, CustomerOnly, DealerOnly }

        private static void ParseSearchFilter(string raw, out BankSearchScope scope, out string term)
        {
            term = (raw ?? "").Trim();
            scope = BankSearchScope.Both;
            if (string.IsNullOrEmpty(term)) return;

            string lower = term.ToLowerInvariant();
            int colon = lower.IndexOf(':');
            if (colon > 0)
            {
                string key = lower.Substring(0, colon).Trim();
                if (key == "customer" || key == "c")
                {
                    scope = BankSearchScope.CustomerOnly;
                    term = term.Substring(colon + 1).Trim();
                    return;
                }
                if (key == "dealer" || key == "d")
                {
                    scope = BankSearchScope.DealerOnly;
                    term = term.Substring(colon + 1).Trim();
                    return;
                }
            }

            int sp = term.IndexOf(' ');
            string first = (sp > 0 ? term.Substring(0, sp) : term).ToLowerInvariant();
            if (first == "customer" || first == "c")
            {
                scope = BankSearchScope.CustomerOnly;
                term = sp > 0 ? term.Substring(sp + 1).Trim() : "";
                return;
            }
            if (first == "dealer" || first == "d")
            {
                scope = BankSearchScope.DealerOnly;
                term = sp > 0 ? term.Substring(sp + 1).Trim() : "";
                return;
            }
        }

        private void LoadData()
        {
            _loading = true;
            _batchGridUpdate = true;
            try
            {
                string bankName = GetActiveBankName();
                if (string.IsNullOrEmpty(bankName))
                {
                    guna2DataGridView1.Rows.Clear();
                    lblIn.Text = "0.00";
                    lblOut.Text = "0.00";
                    lblBalance.Text = "0.00";
                    return;
                }

                ParseSearchFilter(txtSearch.Text, out BankSearchScope scope, out _);
                string searchClause = "";
                if (scope == BankSearchScope.CustomerOnly)
                    searchClause = " AND IFNULL(AC.Name,'') <> '' ";
                else if (scope == BankSearchScope.DealerOnly)
                    searchClause = " AND IFNULL(AD.DealerName,'') <> '' ";

                string qry = @"
SELECT BT.Id, AD.DealerName, AC.Name AS CustomerName, BT.TransactionType,
       BT.BankName, BT.Amount, BT.Note, BT.TransactionDate
FROM BankTransactions BT
LEFT JOIN AddCustomer AC ON BT.CustomerId = AC.id
LEFT JOIN AddDealer AD ON BT.DealerId = AD.Did
WHERE IFNULL(BT.BankName,'') LIKE @BankLike" + searchClause + @"
ORDER BY BT.Id ASC";

                var ht = new Hashtable { { "@BankLike", "%" + bankName + "%" } };
                DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);

                guna2DataGridView1.SuspendLayout();
                try
                {
                    guna2DataGridView1.Rows.Clear();
                    if (dt != null)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            decimal amt = 0;
                            decimal.TryParse(dr["Amount"]?.ToString(), out amt);
                            string type = dr["TransactionType"]?.ToString() ?? "";
                            object debit = DBNull.Value;
                            object credit = DBNull.Value;
                            if (type.Equals("Out", StringComparison.OrdinalIgnoreCase))
                                debit = Math.Abs(amt).ToString("F2");
                            else if (type.Equals("In", StringComparison.OrdinalIgnoreCase))
                                credit = Math.Abs(amt).ToString("F2");

                            string dealer = dr["DealerName"] == DBNull.Value ? "" : dr["DealerName"].ToString();
                            string customer = dr["CustomerName"] == DBNull.Value ? "" : dr["CustomerName"].ToString();
                            string note = dr["Note"] == DBNull.Value ? "" : dr["Note"].ToString();

                            int idx = guna2DataGridView1.Rows.Add();
                            var r = guna2DataGridView1.Rows[idx];
                            r.Cells["Id"].Value = dr["Id"];
                            r.Cells["DealerName"].Value = dealer;
                            r.Cells["CustomerName"].Value = customer;
                            r.Cells["Debit"].Value = debit;
                            r.Cells["Credit"].Value = credit;
                            r.Cells["BankName"].Value = dr["BankName"] == DBNull.Value ? "" : dr["BankName"];
                            r.Cells["Note"].Value = note;
                            r.Cells["Name"].Value = ResolveDisplayName(customer, dealer, note);
                            r.Cells["TransactionDate"].Value = dr["TransactionDate"] == DBNull.Value
                                ? FormatExcelDate(DateTime.Now)
                                : FormatExcelDate(Convert.ToDateTime(dr["TransactionDate"]));
                            r.Cells["Balance"].Value = "";
                        }
                    }

                    EnsureExcelSheetRows();
                }
                finally
                {
                    guna2DataGridView1.ResumeLayout(true);
                }

                _loadedBank = bankName;
                RefreshSrNo();
                CalculateRunningBalance();
                ApplyGridSearch();
                EnsureGridHeadersVisible();
                RememberLastActiveBank(bankName);
            }
            finally
            {
                _batchGridUpdate = false;
                _loading = false;
                FlushGridDisplay(allRows: true, immediate: true);
                BeginInvoke(new Action(ScrollToLastDataEntry));
            }
        }

        private void EnsureColumnsExist()
        {
            AddCol("Id", "Id", false);
            AddCol("TransactionDate", "Date", true);
            AddCol("Name", "Name", true);
            AddCol("Debit", "Amount Given (-)", true);
            AddCol("Credit", "Amount Taken (+)", true);
            AddCol("Balance", "Balance", true);
            AddCol("DealerName", "Dealer Name", false);
            AddCol("CustomerName", "Customer Name", false);
            AddCol("BankName", "Bank Name", false);
            AddCol("Note", "Note", false);

            if (guna2DataGridView1.Columns.Contains("TransactionDate"))
            {
                guna2DataGridView1.Columns["TransactionDate"].FillWeight = 12;
                guna2DataGridView1.Columns["TransactionDate"].MinimumWidth = 70;
            }
            if (guna2DataGridView1.Columns.Contains("Name"))
            {
                guna2DataGridView1.Columns["Name"].FillWeight = 32;
                guna2DataGridView1.Columns["Name"].MinimumWidth = 90;
            }
            if (guna2DataGridView1.Columns.Contains("Debit"))
            {
                guna2DataGridView1.Columns["Debit"].FillWeight = 18;
                guna2DataGridView1.Columns["Debit"].MinimumWidth = 75;
            }
            if (guna2DataGridView1.Columns.Contains("Credit"))
            {
                guna2DataGridView1.Columns["Credit"].FillWeight = 18;
                guna2DataGridView1.Columns["Credit"].MinimumWidth = 75;
            }
            if (guna2DataGridView1.Columns.Contains("Balance"))
            {
                guna2DataGridView1.Columns["Balance"].FillWeight = 20;
                guna2DataGridView1.Columns["Balance"].MinimumWidth = 75;
            }
        }

        private static string ResolveDisplayName(string customer, string dealer, string note)
        {
            if (!string.IsNullOrWhiteSpace(note)) return note.Trim();
            if (!string.IsNullOrWhiteSpace(customer)) return customer.Trim();
            if (!string.IsNullOrWhiteSpace(dealer)) return dealer.Trim();
            return "";
        }

        private void ApplyNameToHiddenFields(DataGridViewRow row)
        {
            string name = (row.Cells["Name"].Value?.ToString() ?? "").Trim();
            row.Cells["CustomerName"].Value = "";
            row.Cells["DealerName"].Value = "";
            row.Cells["Note"].Value = name;
        }

        private void EnsureExcelSheetRows()
        {
            int target = Math.Max(ExcelSheetMinRows, guna2DataGridView1.Rows.Count);
            GrowExcelSheetRows(target - guna2DataGridView1.Rows.Count);
        }

        private void GrowExcelSheetRows(int count)
        {
            if (count <= 0) return;
            string bankName = GetActiveBankName();
            bool ownBatch = !_batchGridUpdate;
            if (ownBatch)
            {
                _batchGridUpdate = true;
                guna2DataGridView1.SuspendLayout();
            }

            try
            {
                for (int i = 0; i < count; i++)
                {
                    int idx = guna2DataGridView1.Rows.Add();
                    var row = guna2DataGridView1.Rows[idx];
                    row.Cells["Id"].Value = DBNull.Value;
                    row.Cells["BankName"].Value = bankName;
                    row.Cells["Name"].Value = "";
                    row.Cells["TransactionDate"].Value = "";
                    row.Cells["Debit"].Value = DBNull.Value;
                    row.Cells["Credit"].Value = DBNull.Value;
                    row.Cells["Balance"].Value = "";
                }
            }
            finally
            {
                if (ownBatch)
                {
                    guna2DataGridView1.ResumeLayout(true);
                    _batchGridUpdate = false;
                    CalculateRunningBalance();
                }
            }
        }

        private void EnsureRowIndexExists(int rowIndex)
        {
            while (guna2DataGridView1.Rows.Count <= rowIndex)
                GrowExcelSheetRows(ExcelSheetGrowBatch);
        }

        private static string FormatExcelDate(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is DateTime dt) return dt.ToString("d.M.yy");
            if (DateTime.TryParse(value.ToString(), out DateTime parsed)) return parsed.ToString("d.M.yy");
            return value.ToString();
        }

        private static bool TryParseGridDate(object value, out DateTime result)
        {
            result = DateTime.Now;
            if (value == null || value == DBNull.Value) return false;
            return DateTime.TryParse(value.ToString(), out result);
        }

        private void AddCol(string name, string header, bool visible)
        {
            if (guna2DataGridView1.Columns.Contains(name))
            {
                guna2DataGridView1.Columns[name].HeaderText = header;
                guna2DataGridView1.Columns[name].Visible = visible;
                return;
            }
            var col = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Visible = visible };
            guna2DataGridView1.Columns.Add(col);
        }

        private void CalculateTotals()
        {
            decimal totalGiven = 0, totalTaken = 0;
            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (HasValue(row.Cells["Debit"].Value) && decimal.TryParse(row.Cells["Debit"].Value.ToString(), out decimal d))
                    totalGiven += d;
                if (HasValue(row.Cells["Credit"].Value) && decimal.TryParse(row.Cells["Credit"].Value.ToString(), out decimal c))
                    totalTaken += c;
            }
            lblIn.Text = totalGiven.ToString("N2");
            lblOut.Text = totalTaken.ToString("N2");
            lblBalance.Text = (totalTaken - totalGiven).ToString("N2");
            if (!_batchGridUpdate && !_loading)
                LayoutBankPanel();
        }

        private static void SetBalanceCell(DataGridViewRow row, string balanceText)
        {
            var cell = row.Cells["Balance"];
            string current = cell.Value == null || cell.Value == DBNull.Value ? "" : cell.Value.ToString();
            if (!string.Equals(current, balanceText, StringComparison.Ordinal))
                cell.Value = balanceText;
        }

        private void CalculateRunningBalance()
        {
            decimal running = 0;
            bool firstRow = true;
            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                decimal given = 0, taken = 0;
                if (HasValue(row.Cells["Debit"].Value)) decimal.TryParse(row.Cells["Debit"].Value.ToString(), out given);
                if (HasValue(row.Cells["Credit"].Value)) decimal.TryParse(row.Cells["Credit"].Value.ToString(), out taken);
                running = firstRow ? taken - given : running + taken - given;
                firstRow = false;
                SetBalanceCell(row, running.ToString("F2"));
            }
            CalculateTotals();
            if (!_batchGridUpdate)
                RefreshSrNo();
            if (!_batchGridUpdate && !_loading)
                FlushGridDisplay(allRows: true, immediate: true);
        }

        private object ResolveDealerId(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || _dealerTable == null) return DBNull.Value;
            foreach (DataRow r in _dealerTable.Rows)
                if (string.Equals(r["DealerName"].ToString().Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return r["Did"];
            return DBNull.Value;
        }

        private object ResolveCustomerId(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || _customerTable == null) return DBNull.Value;
            foreach (DataRow r in _customerTable.Rows)
                if (string.Equals(r["Name"].ToString().Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return r["id"];
            return DBNull.Value;
        }

        private void SaveRow(int rowIndex)
        {
            if (_saveRowInProgress == rowIndex) return;

            var row = guna2DataGridView1.Rows[rowIndex];
            if (!IsRowReadyToSave(row)) return;

            ApplyNameToHiddenFields(row);
            string transType = "In";
            decimal amount = 0;
            if (HasValue(row.Cells["Debit"].Value))
            {
                transType = "Out";
                decimal.TryParse(row.Cells["Debit"].Value.ToString(), out amount);
                amount = -Math.Abs(amount);
            }
            else
            {
                decimal.TryParse(row.Cells["Credit"].Value.ToString(), out amount);
                amount = Math.Abs(amount);
            }

            int id = 0;
            if (row.Cells["Id"].Value != null && row.Cells["Id"].Value != DBNull.Value)
                int.TryParse(row.Cells["Id"].Value.ToString(), out id);

            string dateStr = row.Cells["TransactionDate"].Value?.ToString() ?? "";
            DateTime transDate = DateTime.Now;
            if (!TryParseGridDate(dateStr, out transDate))
                transDate = DateTime.Now;

            var ht = new Hashtable
            {
                { "@TransactionDate", transDate.ToString("yyyy-MM-dd") },
                { "@TransactionType", transType },
                { "@CustomerId", DBNull.Value },
                { "@DealerId", DBNull.Value },
                { "@Amount", amount.ToString("F2") },
                { "@Note", (row.Cells["Name"].Value?.ToString() ?? "").Trim() },
                { "@BankName", GetActiveBankName() }
            };

            string qry;
            if (id <= 0)
            {
                qry = @"INSERT INTO BankTransactions (TransactionDate, TransactionType, CustomerId, DealerId, Amount, Note, BankName)
                        VALUES (@TransactionDate, @TransactionType, @CustomerId, @DealerId, @Amount, @Note, @BankName)";
            }
            else
            {
                qry = @"UPDATE BankTransactions SET TransactionDate=@TransactionDate, TransactionType=@TransactionType,
                        CustomerId=@CustomerId, DealerId=@DealerId, Amount=@Amount, Note=@Note, BankName=@BankName WHERE Id=@Id";
                ht.Add("@Id", id);
            }

            _saveRowInProgress = rowIndex;
            try
            {
                int r;
                if (id <= 0)
                {
                    using (var con = new SQLiteConnection(projectconnection.conReturn()))
                    {
                        con.Open();
                        using (var cmd = new SQLiteCommand(qry, con))
                        {
                            foreach (DictionaryEntry item in ht)
                                cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                            r = cmd.ExecuteNonQuery();
                        }

                        if (r > 0)
                        {
                            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", con))
                                row.Cells["Id"].Value = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
                else
                {
                    r = MainClass.DataInsertUpdateDelete(qry, ht);
                }

                if (r > 0)
                {
                    row.Cells["BankName"].Value = GetActiveBankName();
                    RememberBankName(GetActiveBankName());
                    CalculateRunningBalance();
                    ApplyGridSearch();
                    FlushGridDisplay(rowIndex, allRows: true, immediate: true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saveRowInProgress = -1;
            }
        }

        private void DeleteCurrentRow()
        {
            var row = guna2DataGridView1.CurrentRow;
            if (row == null) return;
            if (MessageBox.Show("Delete this row?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            if (row.Cells["Id"].Value != null && int.TryParse(row.Cells["Id"].Value.ToString(), out int id) && id > 0)
            {
                var ht = new Hashtable { { "@Id", id } };
                MainClass.DataInsertUpdateDelete("DELETE FROM BankTransactions WHERE Id=@Id", ht);
                LoadData();
                return;
            }

            ClearExcelRow(row);
            CalculateRunningBalance();
        }

        private void ClearExcelRow(DataGridViewRow row)
        {
            row.Cells["Id"].Value = DBNull.Value;
            row.Cells["Name"].Value = "";
            row.Cells["DealerName"].Value = "";
            row.Cells["CustomerName"].Value = "";
            row.Cells["Note"].Value = "";
            row.Cells["Debit"].Value = DBNull.Value;
            row.Cells["Credit"].Value = DBNull.Value;
            row.Cells["TransactionDate"].Value = "";
            row.Cells["BankName"].Value = GetActiveBankName();
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            if (!HasSelectedBank())
            {
                MessageBox.Show("Pehle upar se bank select karein.", "Bank Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtBank.Focus();
                return;
            }

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (HasValue(row.Cells["Debit"].Value) || HasValue(row.Cells["Credit"].Value))
                    continue;
                if (row.Cells["Id"].Value != null && row.Cells["Id"].Value != DBNull.Value)
                    continue;

                row.Cells["TransactionDate"].Value = "";
                row.Cells["BankName"].Value = GetActiveBankName();
                row.Cells["Name"].Value = "";
                guna2DataGridView1.CurrentCell = row.Cells["TransactionDate"];
                guna2DataGridView1.BeginEdit(true);
                return;
            }

            GrowExcelSheetRows(ExcelSheetGrowBatch);
            btnAdd_Click(sender, e);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                guna2DataGridView1.BeginEdit(true);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = guna2DataGridView1.Columns[e.ColumnIndex].Name;
            if (colName == "Balance")
                e.CellStyle.BackColor = Color.FromArgb(242, 242, 242);
            else if (colName == "Debit")
                e.CellStyle.ForeColor = Color.FromArgb(192, 0, 0);
            else
            {
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.BackColor = Color.White;
            }
        }

        private void ExportExcel()
        {
            string bank = GetActiveBankName();
            if (string.IsNullOrEmpty(bank))
            {
                MessageBox.Show("Pehle bank select karein.", "Bank Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel (*.xls)|*.xls";
                string safeBank = string.Join("_", bank.Split(Path.GetInvalidFileNameChars()));
                sfd.FileName = safeBank + "_" + DateTime.Now.ToString("yyyyMMdd") + ".xls";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\"?>");
                sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
                sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
                sb.AppendLine("<Worksheet ss:Name=\"" + System.Security.SecurityElement.Escape(bank) + "\"><Table>");
                sb.AppendLine("<Row>" +
                    XmlStringCell("Sr") +
                    XmlStringCell("Date") +
                    XmlStringCell("Name") +
                    XmlStringCell("Amount Given (-)") +
                    XmlStringCell("Amount Taken (+)") +
                    XmlStringCell("Balance") + "</Row>");

                foreach (DataGridViewRow row in guna2DataGridView1.Rows)
                {
                    if (!row.Visible) continue;
                    sb.AppendLine("<Row>" +
                        XmlCell(row.Cells["dgvSr"].Value) +
                        XmlCell(row.Cells["TransactionDate"].Value) +
                        XmlCell(row.Cells["Name"].Value) +
                        XmlCell(row.Cells["Debit"].Value) +
                        XmlCell(row.Cells["Credit"].Value) +
                        XmlCell(row.Cells["Balance"].Value) + "</Row>");
                }

                sb.AppendLine("</Table></Worksheet></Workbook>");
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
            }
        }

        private static string XmlStringCell(string text)
        {
            string s = System.Security.SecurityElement.Escape(text ?? "");
            return $"<Cell><Data ss:Type=\"String\">{s}</Data></Cell>";
        }

        private static string XmlCell(object v)
        {
            string s = v == null || v == DBNull.Value ? "" : System.Security.SecurityElement.Escape(v.ToString());
            if (decimal.TryParse(s, out decimal num))
                return $"<Cell><Data ss:Type=\"Number\">{num}</Data></Cell>";
            return $"<Cell><Data ss:Type=\"String\">{s}</Data></Cell>";
        }

        private static string XmlFormulaCell(object value, string formula)
        {
            string s = value == null || value == DBNull.Value ? "0" : value.ToString();
            if (!decimal.TryParse(s, out decimal num)) num = 0;
            string f = System.Security.SecurityElement.Escape(formula);
            return $"<Cell ss:Formula=\"{f}\"><Data ss:Type=\"Number\">{num}</Data></Cell>";
        }

        private void ShowReport()
        {
            string activeBank = GetActiveBankName();
            if (string.IsNullOrEmpty(activeBank))
            {
                MessageBox.Show("Pehle bank select karein.", "Bank Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("DealerName", typeof(string));
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("Debit", typeof(decimal));
            dt.Columns.Add("Credit", typeof(decimal));
            dt.Columns.Add("BankName", typeof(string));
            dt.Columns.Add("Note", typeof(string));
            dt.Columns.Add("TransactionDate", typeof(string));
            dt.Columns.Add("Balance", typeof(decimal));

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (!row.Visible || row.IsNewRow) continue;

                bool hasId = row.Cells["Id"].Value != null && row.Cells["Id"].Value != DBNull.Value
                    && int.TryParse(row.Cells["Id"].Value.ToString(), out int savedId) && savedId > 0;
                bool hasAmount = HasValue(row.Cells["Debit"].Value) || HasValue(row.Cells["Credit"].Value);
                string displayName = row.Cells["Name"].Value?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = ResolveDisplayName(
                        row.Cells["CustomerName"].Value?.ToString(),
                        row.Cells["DealerName"].Value?.ToString(),
                        row.Cells["Note"].Value?.ToString())?.Trim() ?? "";

                if (!hasId && !hasAmount && string.IsNullOrWhiteSpace(displayName))
                    continue;

                decimal debit = 0, credit = 0, balance = 0;
                if (HasValue(row.Cells["Debit"].Value)) decimal.TryParse(row.Cells["Debit"].Value.ToString(), out debit);
                if (HasValue(row.Cells["Credit"].Value)) decimal.TryParse(row.Cells["Credit"].Value.ToString(), out credit);
                if (row.Cells["Balance"].Value != null && decimal.TryParse(row.Cells["Balance"].Value.ToString(), out decimal b))
                    balance = b;

                int id = 0;
                if (row.Cells["Id"].Value != null) int.TryParse(row.Cells["Id"].Value.ToString(), out id);

                dt.Rows.Add(id,
                    "",
                    displayName,
                    debit, credit,
                    activeBank,
                    displayName,
                    row.Cells["TransactionDate"].Value?.ToString() ?? "",
                    balance);
            }

            using (var frm = new Form())
            {
                frm.Text = "Bank Report - " + activeBank;
                frm.Size = new Size(900, 600);
                frm.StartPosition = FormStartPosition.CenterParent;
                var rv = new ReportViewer { Dock = DockStyle.Fill };
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BankAccountViewReport.rdlc");
                rv.LocalReport.ReportPath = reportPath;
                rv.LocalReport.DataSources.Clear();
                rv.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                rv.LocalReport.SetParameters(new[]
                {
                    new ReportParameter("BankName", string.IsNullOrWhiteSpace(activeBank) ? "-" : activeBank),
                    new ReportParameter("ReportTitle", "Bank Account Report")
                });
                frm.Controls.Add(rv);
                ReportHeaderBrandHelper.Attach(frm);
                ReportHeaderBrandHelper.ApplyBrandToViewer(rv, ReportHeaderBrandHelper.CurrentCompany);
                try
                {
                    rv.LocalReport.SetParameters(new[]
                    {
                        new ReportParameter("BankName", string.IsNullOrWhiteSpace(activeBank) ? "-" : activeBank),
                        new ReportParameter("ReportTitle", "Bank Account Report")
                    });
                }
                catch { }
                ReportHeaderBrandHelper.SafeRefreshReport(rv);
                frm.ShowDialog();
            }
        }
    }
}
