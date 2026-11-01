using Guna.UI2.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AlMadinaShahzad;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmClosing2 : Sample
    {
        private DataTable _customerCredits;
        private DataTable _dealerList;

        // Temporary in-memory entries (not saved to DB, only for Excel/display)
        private DataTable _tempCustomerEntries;
        private DataTable _tempDealerEntries;

        // Permanent DB rows hidden from grid until next Load (not deleted from DB)
        private readonly HashSet<int> _hiddenCustomerIds = new HashSet<int>();
        private readonly HashSet<int> _hiddenDealerIds = new HashSet<int>();
        private readonly HashSet<string> _hiddenBankCustomerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _hiddenBankDealerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _hideProfitDealerRow;
        private bool _hideProfitCustomerRow;

        private decimal _customerAvgRate;
        private decimal _dealerAvgRate;
        private decimal _rateDiff;
        private decimal _totalCustomerLitter;
        private decimal _totalCustomerAmount;
        private decimal _totalValuePurchase;
        private decimal _totalLitterPurchase;
        private decimal _grossProfit;
        private decimal _totalExpense;
        private decimal _netProfit;
        private bool _dataLoaded;
        private bool _initializingDates;
        private int _customerDbRowCount;
        private int _dealerDbRowCount;
        private int _customerBankRowCount;
        private int _dealerBankRowCount;
        private decimal _totalBankAmount;
        private Guna2Button _btnRemoveTemp;
        private Guna2Button _btnCreditCustomer;
        private Guna2Button _btnDealerPayout;
        private TextBox _txtGridSearch;
        private Label _lblGridSearch;
        private Guna2Button _btnZoomIn;
        private Guna2Button _btnZoomOut;
        private static readonly string[] _rowZoomColumns = { "dgvRecived", "dgvPayable", "dgvNetProfit" };
        private int _lastGridRowIndex = -1;
        private Font _gridDefaultCellFont;
        private readonly Dictionary<int, float> _rowZoomLevels = new Dictionary<int, float>();
        private const float RowMinZoom = 1f;
        private const float RowMaxZoom = 3.5f;
        private const float RowZoomStep = 1.12f;
        private Closing2RowZoomFilter _rowZoomFilter;

        private sealed class GridRowMeta
        {
            public Closing2SideMeta Customer;
            public Closing2SideMeta Dealer;
            public bool IsProfitRow;
            public bool IsProfitOnDealerSide;
        }

        private struct Closing2SideMeta
        {
            public bool IsTemp;
            public bool IsBank;
            public string Name;
            public decimal Amount;
            public int EntityId;

            public bool HasValue => Amount > 0 && !string.IsNullOrWhiteSpace(Name);
        }

        // Form dobara khulne par temp/hidden/date wapas lane ke liye (sirf Closing2 session)
        private static class Closing2SessionStore
        {
            public static DataTable TempCustomer;
            public static DataTable TempDealer;
            public static readonly HashSet<int> HiddenCustomers = new HashSet<int>();
            public static readonly HashSet<int> HiddenDealers = new HashSet<int>();
            public static readonly HashSet<string> HiddenBankCustomers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public static readonly HashSet<string> HiddenBankDealers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public static bool HideProfitDealerRow;
            public static bool HideProfitCustomerRow;
            public static DateTime StartDate = DateTime.Today;
            public static DateTime EndDate = DateTime.Today;
            public static string OfficeFilter = OfficeHelper.AllOffices;
            public static bool HasSavedSession;
        }

        private string GetClosingOfficeFilter()
        {
            return OfficeHelper.GetSelectedOffice(this, allowAll: true);
        }

        private string GetClosingOfficeForSave()
        {
            string office = GetClosingOfficeFilter();
            if (string.IsNullOrWhiteSpace(office) || office == OfficeHelper.AllOffices)
                return "";
            return office;
        }

        private static DataTable CloneSessionTable(DataTable source)
        {
            return source == null ? null : source.Copy();
        }

        private void SaveSessionState()
        {
            Closing2SessionStore.TempCustomer = CloneSessionTable(_tempCustomerEntries);
            Closing2SessionStore.TempDealer = CloneSessionTable(_tempDealerEntries);
            Closing2SessionStore.HiddenCustomers.Clear();
            foreach (int id in _hiddenCustomerIds)
                Closing2SessionStore.HiddenCustomers.Add(id);
            Closing2SessionStore.HiddenDealers.Clear();
            foreach (int id in _hiddenDealerIds)
                Closing2SessionStore.HiddenDealers.Add(id);
            Closing2SessionStore.HiddenBankCustomers.Clear();
            foreach (string name in _hiddenBankCustomerNames)
                Closing2SessionStore.HiddenBankCustomers.Add(name);
            Closing2SessionStore.HiddenBankDealers.Clear();
            foreach (string name in _hiddenBankDealerNames)
                Closing2SessionStore.HiddenBankDealers.Add(name);
            Closing2SessionStore.HideProfitDealerRow = _hideProfitDealerRow;
            Closing2SessionStore.HideProfitCustomerRow = _hideProfitCustomerRow;
            if (dtpStart != null && dtpEnd != null)
            {
                Closing2SessionStore.StartDate = dtpStart.Value.Date;
                Closing2SessionStore.EndDate = dtpEnd.Value.Date;
            }
            Closing2SessionStore.OfficeFilter = GetClosingOfficeFilter();
            Closing2SessionStore.HasSavedSession = true;
        }

        private void RestoreSessionState()
        {
            _tempCustomerEntries = CloneSessionTable(Closing2SessionStore.TempCustomer);
            _tempDealerEntries = CloneSessionTable(Closing2SessionStore.TempDealer);
            _hiddenCustomerIds.Clear();
            foreach (int id in Closing2SessionStore.HiddenCustomers)
                _hiddenCustomerIds.Add(id);
            _hiddenDealerIds.Clear();
            foreach (int id in Closing2SessionStore.HiddenDealers)
                _hiddenDealerIds.Add(id);
            _hiddenBankCustomerNames.Clear();
            foreach (string name in Closing2SessionStore.HiddenBankCustomers)
                _hiddenBankCustomerNames.Add(name);
            _hiddenBankDealerNames.Clear();
            foreach (string name in Closing2SessionStore.HiddenBankDealers)
                _hiddenBankDealerNames.Add(name);
            _hideProfitDealerRow = Closing2SessionStore.HideProfitDealerRow;
            _hideProfitCustomerRow = Closing2SessionStore.HideProfitCustomerRow;
            if (Closing2SessionStore.HasSavedSession && dtpStart != null && dtpEnd != null)
            {
                dtpStart.Value = Closing2SessionStore.StartDate;
                dtpEnd.Value = Closing2SessionStore.EndDate;
            }
        }

        private sealed class Closing2RowZoomFilter : IMessageFilter
        {
            private const int WM_MOUSEWHEEL = 0x020A;
            private readonly frmClosing2 _form;

            public Closing2RowZoomFilter(frmClosing2 form)
            {
                _form = form;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL)
                    return false;
                if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
                    return false;
                if (_form == null || _form.IsDisposed || !_form.Visible || _form.guna2DataGridView1 == null)
                    return false;

                try
                {
                    Point gridPt = _form.guna2DataGridView1.PointToClient(Cursor.Position);
                    if (!_form.guna2DataGridView1.ClientRectangle.Contains(gridPt))
                        return false;
                }
                catch
                {
                    return false;
                }

                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                if (_form.ZoomSelectedGridRow(delta > 0))
                    return true;
                return false;
            }
        }

        public frmClosing2()
        {
            InitializeComponent();
            KeyPreview = true;
            _gridDefaultCellFont = guna2DataGridView1.DefaultCellStyle.Font
                ?? new Font("Segoe UI", 9F, FontStyle.Regular);
            guna2DataGridView1.CellFormatting += guna2DataGridView1_CellFormatting;
            guna2DataGridView1.SelectionChanged += guna2DataGridView1_SelectionChanged;
            _rowZoomFilter = new Closing2RowZoomFilter(this);
            FormClosed += frmClosing2_FormClosed;
        }

        private void frmClosing2_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_rowZoomFilter != null)
            {
                Application.RemoveMessageFilter(_rowZoomFilter);
                _rowZoomFilter = null;
            }
        }

        private bool ZoomSelectedGridRow(bool zoomIn)
        {
            DataGridViewRow row = GetSelectedGridRow();
            if (row == null)
                return false;

            int idx = row.Index;
            float current = _rowZoomLevels.TryGetValue(idx, out float z) ? z : 1f;
            float newZoom = zoomIn ? current * RowZoomStep : current / RowZoomStep;
            if (newZoom < RowMinZoom || newZoom > RowMaxZoom)
                return false;

            if (Math.Abs(newZoom - 1f) < 0.01f)
                _rowZoomLevels.Remove(idx);
            else
                _rowZoomLevels[idx] = newZoom;

            ApplyRowZoom(idx, newZoom);
            return true;
        }

        private void ApplyRowZoom(int rowIndex, float scale)
        {
            if (rowIndex < 0 || rowIndex >= guna2DataGridView1.Rows.Count)
                return;

            DataGridViewRow row = guna2DataGridView1.Rows[rowIndex];
            if (row.IsNewRow)
                return;

            float fontSize = Math.Max(8f, _gridDefaultCellFont.Size * scale);
            Font rowFont = new Font(_gridDefaultCellFont.FontFamily, fontSize, _gridDefaultCellFont.Style);

            foreach (DataGridViewCell cell in row.Cells)
            {
                string colName = guna2DataGridView1.Columns[cell.ColumnIndex]?.Name;
                if (colName == null || Array.IndexOf(_rowZoomColumns, colName) < 0)
                    continue;

                cell.Style.Font = rowFont;
            }

            row.Height = Math.Max(22, (int)(fontSize * 2.4f));
            guna2DataGridView1.InvalidateRow(rowIndex);
        }

        private void guna2DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null && !guna2DataGridView1.CurrentRow.IsNewRow)
                _lastGridRowIndex = guna2DataGridView1.CurrentRow.Index;
        }

        private void frmClosing2_Load(object sender, EventArgs e)
        {
            _initializingDates = true;
            RestoreSessionState();
            if (!Closing2SessionStore.HasSavedSession)
            {
                dtpStart.Value = DateTime.Today;
                dtpEnd.Value = DateTime.Today;
            }
            ResetCalculationLabels();

            cbTempMode.Items.Clear();
            cbTempMode.Items.Add("Customer");
            cbTempMode.Items.Add("Dealer");
            cbTempMode.Items.Add("Expense");
            SetupTempTransferButtons();
            SetupPdfButton();
            OfficeHelper.AttachOfficeFilter(this, (s, ev) =>
            {
                if (_initializingDates) return;
                ClearHiddenPermanentEntries();
                CalculateProfitAndLoadSummary();
            }, left: 520, top: 8);
            if (Closing2SessionStore.HasSavedSession)
                OfficeHelper.SetSelectedOffice(this, Closing2SessionStore.OfficeFilter, isFilter: true);
            Application.AddMessageFilter(_rowZoomFilter);

            dtpStart.ValueChanged += DateRangePicker_ValueChanged;
            dtpEnd.ValueChanged += DateRangePicker_ValueChanged;

            _initializingDates = false;
            CalculateProfitAndLoadSummary();
        }

        private void DateRangePicker_ValueChanged(object sender, EventArgs e)
        {
            if (_initializingDates)
                return;

            if (dtpStart.Value.Date > dtpEnd.Value.Date)
                return;

            ClearHiddenPermanentEntries();
            CalculateProfitAndLoadSummary();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            ClearHiddenPermanentEntries();
            CalculateProfitAndLoadSummary();
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            ExportToExcel();
        }

        private void btnAddTemp_Click(object sender, EventArgs e)
        {
            AddTempEntry();
        }

        private void btnClearTemp_Click(object sender, EventArgs e)
        {
            string name = (txtTempName.Text ?? "").Trim();
            string mode = cbTempMode.SelectedItem?.ToString() ?? "";

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(mode))
            {
                string msg = mode.Equals("Customer", StringComparison.OrdinalIgnoreCase)
                    ? $"Kya '{name}' ki customer temp entry hata dein?"
                    : $"Kya '{name}' ki dealer/expense temp entry hata dein?";
                using (var yn = new YesOrNoMessage(msg, "Confirm Remove"))
                {
                    if (yn.ShowDialog() != DialogResult.Yes)
                        return;
                }

                if (RemoveTempEntry(name, mode))
                {
                    txtTempName.Text = "";
                    txtTempAmount.Text = "";
                    RefreshGridIfLoaded("Temp entry hata di.");
                    SaveSessionState();
                }
                else
                    MessageBox.Show("Is naam ki temp entry nahi mili.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!string.IsNullOrEmpty(mode))
            {
                string side = mode.Equals("Customer", StringComparison.OrdinalIgnoreCase)
                    ? "customer" : "dealer/expense";
                using (var yn = new YesOrNoMessage($"Saari {side} temp entries hata dein?", "Confirm Clear"))
                {
                    if (yn.ShowDialog() != DialogResult.Yes)
                        return;
                }

                ClearTempSide(mode);
                txtTempName.Text = "";
                txtTempAmount.Text = "";
                cbTempMode.SelectedIndex = -1;
                RefreshGridIfLoaded("Temp entries clear ho gayin.");
                SaveSessionState();
                return;
            }

            using (var ynCust = new YesOrNoMessage("Customer side ki saari temp entries hata dein?", "Clear Customer Temp"))
            {
                if (ynCust.ShowDialog() == DialogResult.Yes)
                    _tempCustomerEntries = null;
            }

            using (var ynDeal = new YesOrNoMessage("Dealer side ki saari temp entries hata dein?", "Clear Dealer Temp"))
            {
                if (ynDeal.ShowDialog() == DialogResult.Yes)
                    _tempDealerEntries = null;
            }

            txtTempName.Text = "";
            txtTempAmount.Text = "";
            cbTempMode.SelectedIndex = -1;
            RefreshGridIfLoaded();
            SaveSessionState();
        }

        private void SetupTempTransferButtons()
        {
            _btnRemoveTemp = new Guna2Button
            {
                Text = "✕",
                Size = new System.Drawing.Size(34, 30),
                Location = new System.Drawing.Point(720, 212),
                BorderRadius = 12,
                FillColor = System.Drawing.Color.FromArgb(192, 57, 43),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            _btnRemoveTemp.Click += (s, e) => RemoveSelectedGridEntry();
            Controls.Add(_btnRemoveTemp);
            _btnRemoveTemp.BringToFront();

            _btnCreditCustomer = new Guna2Button
            {
                Text = "Credit",
                Size = new System.Drawing.Size(72, 30),
                Location = new System.Drawing.Point(760, 212),
                BorderRadius = 12,
                FillColor = System.Drawing.Color.FromArgb(66, 133, 244),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold)
            };
            _btnCreditCustomer.Click += (s, e) => PostSelectedToCreditCustomer();
            Controls.Add(_btnCreditCustomer);
            _btnCreditCustomer.BringToFront();

            _btnDealerPayout = new Guna2Button
            {
                Text = "Payout",
                Size = new System.Drawing.Size(72, 30),
                Location = new System.Drawing.Point(840, 212),
                BorderRadius = 12,
                FillColor = System.Drawing.Color.FromArgb(230, 126, 34),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold)
            };
            _btnDealerPayout.Click += (s, e) => PostSelectedToDealerPayout();
            Controls.Add(_btnDealerPayout);
            _btnDealerPayout.BringToFront();

            SetupGridSearch();
        }

        private void SetupGridSearch()
        {
            _lblGridSearch = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.Silver,
                Location = new Point(920, 218),
                Text = "Search:"
            };

            _txtGridSearch = new TextBox
            {
                BackColor = Color.FromArgb(37, 41, 74),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                Location = new Point(975, 214),
                Size = new Size(280, 26)
            };
            _txtGridSearch.TextChanged += (s, e) => ApplyGridSearchFilter();

            Controls.Add(_lblGridSearch);
            Controls.Add(_txtGridSearch);
            _lblGridSearch.BringToFront();
            _txtGridSearch.BringToFront();

            SetupZoomButtons();
        }

        private void SetupZoomButtons()
        {
            _btnZoomIn = new Guna2Button
            {
                Text = "Zoom +",
                Size = new Size(90, 30),
                Location = new Point(18, 752),
                BorderRadius = 12,
                FillColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            _btnZoomIn.Click += (s, e) =>
            {
                if (!ZoomSelectedGridRow(true))
                    MessageBox.Show("Pehle grid se row select karein.", "Zoom",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _btnZoomOut = new Guna2Button
            {
                Text = "Zoom -",
                Size = new Size(90, 30),
                Location = new Point(116, 752),
                BorderRadius = 12,
                FillColor = Color.FromArgb(127, 140, 141),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            _btnZoomOut.Click += (s, e) =>
            {
                if (!ZoomSelectedGridRow(false))
                    MessageBox.Show("Pehle grid se row select karein.", "Zoom",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Controls.Add(_btnZoomIn);
            Controls.Add(_btnZoomOut);
            _btnZoomIn.BringToFront();
            _btnZoomOut.BringToFront();
        }

        private bool RowMatchesGridSearch(DataGridViewRow row, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return true;

            string receivable = Convert.ToString(row.Cells["dgvRecived"]?.Value) ?? "";
            string payable = Convert.ToString(row.Cells["dgvPayable"]?.Value) ?? "";

            if (receivable.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (payable.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (row.Tag is GridRowMeta meta)
            {
                if (meta.Customer.HasValue &&
                    meta.Customer.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (meta.Dealer.HasValue &&
                    meta.Dealer.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void ApplyGridSearchFilter()
        {
            if (guna2DataGridView1 == null || guna2DataGridView1.IsDisposed)
                return;

            string term = _txtGridSearch?.Text?.Trim() ?? "";
            bool filterActive = term.Length > 0;
            int visibleSr = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (row.IsNewRow)
                    continue;

                bool match = RowMatchesGridSearch(row, term);
                row.Visible = match;

                if (match && filterActive && row.Cells.Count > 0)
                {
                    visibleSr++;
                    row.Cells[0].Value = visibleSr;
                }
            }

            if (!filterActive)
                guna2DataGridView1.Invalidate();
        }

        private void RefreshGridIfLoaded(string infoMessage = null)
        {
            if (_dataLoaded)
                LoadReceivablePayableGrid();
            else if (!string.IsNullOrEmpty(infoMessage))
                MessageBox.Show(infoMessage, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool RemoveTempEntry(string name, string mode)
        {
            if (mode.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                if (_tempCustomerEntries == null) return false;
                for (int i = _tempCustomerEntries.Rows.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(Convert.ToString(_tempCustomerEntries.Rows[i]["CustomerName"]),
                        name, StringComparison.OrdinalIgnoreCase))
                    {
                        _tempCustomerEntries.Rows.RemoveAt(i);
                        if (_tempCustomerEntries.Rows.Count == 0)
                            _tempCustomerEntries = null;
                        return true;
                    }
                }
                return false;
            }

            string displayName = mode.Equals("Expense", StringComparison.OrdinalIgnoreCase)
                ? $"[Exp] {name}" : name;
            if (_tempDealerEntries == null) return false;
            for (int i = _tempDealerEntries.Rows.Count - 1; i >= 0; i--)
            {
                string dn = Convert.ToString(_tempDealerEntries.Rows[i]["DealerName"]);
                if (string.Equals(dn, displayName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dn, name, StringComparison.OrdinalIgnoreCase))
                {
                    _tempDealerEntries.Rows.RemoveAt(i);
                    if (_tempDealerEntries.Rows.Count == 0)
                        _tempDealerEntries = null;
                    return true;
                }
            }
            return false;
        }

        private void ClearTempSide(string mode)
        {
            if (mode.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                _tempCustomerEntries = null;
            else
                _tempDealerEntries = null;
        }

        private DataGridViewRow GetSelectedGridRow()
        {
            if (guna2DataGridView1.SelectedRows.Count > 0 &&
                !guna2DataGridView1.SelectedRows[0].IsNewRow)
                return guna2DataGridView1.SelectedRows[0];

            if (guna2DataGridView1.CurrentRow != null && !guna2DataGridView1.CurrentRow.IsNewRow)
                return guna2DataGridView1.CurrentRow;

            if (_lastGridRowIndex >= 0 && _lastGridRowIndex < guna2DataGridView1.Rows.Count &&
                !guna2DataGridView1.Rows[_lastGridRowIndex].IsNewRow)
                return guna2DataGridView1.Rows[_lastGridRowIndex];

            return null;
        }

        private GridRowMeta GetSelectedGridMeta()
        {
            DataGridViewRow row = GetSelectedGridRow();
            return row?.Tag as GridRowMeta;
        }

        private decimal GetLiveCustomerReceivable(int customerId)
        {
            var ht = new Hashtable { { "@customerId", customerId } };
            string officeFilter = OfficeHelper.OfficeSqlFilter("p.OfficeName", GetClosingOfficeFilter());
            DataTable dt = MainClass.ExecuteSelectQuery(@"
                SELECT IFNULL(SUM(
                    IFNULL(p.Amount, 0) + IFNULL(p.Advance, 0) - IFNULL(p.Credit, 0)
                ), 0) AS Receivable
                FROM PetrolAdd p
                WHERE p.CustomerId = @customerId" + officeFilter, ht);
            if (dt != null && dt.Rows.Count > 0)
                return SafeToDecimal(dt.Rows[0]["Receivable"]);
            return 0m;
        }

        private decimal GetLiveDealerPayable(int dealerId)
        {
            var ht = new Hashtable { { "@dealerId", dealerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(@"
                SELECT (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS Payable
                FROM AddDealer d
                WHERE d.Did = @dealerId", ht);
            if (dt != null && dt.Rows.Count > 0)
                return SafeToDecimal(dt.Rows[0]["Payable"]);
            return 0m;
        }

        private void ClearHiddenPermanentEntries()
        {
            _hiddenCustomerIds.Clear();
            _hiddenDealerIds.Clear();
            _hiddenBankCustomerNames.Clear();
            _hiddenBankDealerNames.Clear();
            _hideProfitDealerRow = false;
            _hideProfitCustomerRow = false;
        }

        private DataTable FilterHiddenBankCustomerRows(DataTable dt)
        {
            if (dt == null || _hiddenBankCustomerNames.Count == 0)
                return dt;

            DataTable filtered = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                string name = Convert.ToString(row["CustomerName"]);
                if (_hiddenBankCustomerNames.Contains(name))
                    continue;
                filtered.ImportRow(row);
            }
            return filtered;
        }

        private DataTable FilterHiddenBankDealerRows(DataTable dt)
        {
            if (dt == null || _hiddenBankDealerNames.Count == 0)
                return dt;

            DataTable filtered = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                string name = Convert.ToString(row["DealerName"]);
                if (_hiddenBankDealerNames.Contains(name))
                    continue;
                filtered.ImportRow(row);
            }
            return filtered;
        }

        private DataTable FilterHiddenCustomerRows(DataTable dt)
        {
            if (dt == null || _hiddenCustomerIds.Count == 0)
                return dt;

            DataTable filtered = dt.Clone();
            foreach (DataRow dr in dt.Rows)
            {
                int id = 0;
                if (dt.Columns.Contains("CustomerId") && dr["CustomerId"] != DBNull.Value)
                    id = Convert.ToInt32(dr["CustomerId"]);
                if (id > 0 && _hiddenCustomerIds.Contains(id))
                    continue;
                filtered.ImportRow(dr);
            }
            return filtered.Rows.Count > 0 ? filtered : dt.Clone();
        }

        private DataTable FilterHiddenDealerRows(DataTable dt)
        {
            if (dt == null || _hiddenDealerIds.Count == 0)
                return dt;

            DataTable filtered = dt.Clone();
            foreach (DataRow dr in dt.Rows)
            {
                int id = 0;
                if (dt.Columns.Contains("Did") && dr["Did"] != DBNull.Value)
                    id = Convert.ToInt32(dr["Did"]);
                if (id > 0 && _hiddenDealerIds.Contains(id))
                    continue;
                filtered.ImportRow(dr);
            }
            return filtered.Rows.Count > 0 ? filtered : dt.Clone();
        }

        private void RemoveSelectedGridEntry()
        {
            GridRowMeta meta = GetSelectedGridMeta();
            if (meta == null)
            {
                MessageBox.Show("Pehle grid se row select karein.", "Select Row",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool removed = false;

            if (meta.IsProfitRow)
            {
                string label = meta.IsProfitOnDealerSide
                    ? $"Profit Dealer - {_netProfit:N2}"
                    : $"Loss Customer Payable - {Math.Abs(_netProfit):N2}";
                using (var yn = new YesOrNoMessage(
                    $"Kya '{label}' ki entry temporarily hata dein?",
                    "Remove Temp"))
                {
                    if (yn.ShowDialog() == DialogResult.Yes)
                    {
                        if (meta.IsProfitOnDealerSide)
                            _hideProfitDealerRow = true;
                        else
                            _hideProfitCustomerRow = true;
                        removed = true;
                    }
                }
            }

            if (meta.Customer.HasValue && !removed)
            {
                if (meta.Customer.IsTemp)
                {
                    using (var yn = new YesOrNoMessage(
                        $"Kya '{meta.Customer.Name}' ki temp customer entry hata dein?",
                        "Remove Temp"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes &&
                            RemoveTempEntry(meta.Customer.Name, "Customer"))
                            removed = true;
                    }
                }
                else if (meta.Customer.IsBank)
                {
                    using (var yn = new YesOrNoMessage(
                        $"Kya '{meta.Customer.Name}' ki bank entry temporarily hata dein?",
                        "Remove Temp"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes)
                        {
                            _hiddenBankCustomerNames.Add(meta.Customer.Name);
                            removed = true;
                        }
                    }
                }
                else
                {
                    using (var yn = new YesOrNoMessage(
                        $"Kya '{meta.Customer.Name}' ki permanent customer entry grid se hata dein?\n(Load karne par wapas aayegi.)",
                        "Remove Entry"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes)
                        {
                            int id = meta.Customer.EntityId > 0
                                ? meta.Customer.EntityId
                                : ResolveCustomerId(meta.Customer.Name);
                            if (id > 0)
                            {
                                _hiddenCustomerIds.Add(id);
                                removed = true;
                            }
                        }
                    }
                }
            }

            if (meta.Dealer.HasValue && !removed)
            {
                if (meta.Dealer.IsTemp)
                {
                    string dealerName = meta.Dealer.Name;
                    bool isExpense = dealerName.StartsWith("[Exp]", StringComparison.OrdinalIgnoreCase);
                    string rawName = isExpense && dealerName.StartsWith("[Exp] ", StringComparison.OrdinalIgnoreCase)
                        ? dealerName.Substring(6).Trim() : dealerName;
                    string mode = isExpense ? "Expense" : "Dealer";

                    using (var yn = new YesOrNoMessage(
                        $"Kya '{dealerName}' ki temp dealer entry hata dein?",
                        "Remove Temp"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes &&
                            RemoveTempEntry(rawName, mode))
                            removed = true;
                    }
                }
                else if (meta.Dealer.IsBank)
                {
                    using (var yn = new YesOrNoMessage(
                        $"Kya '{meta.Dealer.Name}' ki bank entry temporarily hata dein?",
                        "Remove Temp"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes)
                        {
                            _hiddenBankDealerNames.Add(meta.Dealer.Name);
                            removed = true;
                        }
                    }
                }
                else
                {
                    using (var yn = new YesOrNoMessage(
                        $"Kya '{meta.Dealer.Name}' ki permanent dealer entry grid se hata dein?\n(Load karne par wapas aayegi.)",
                        "Remove Entry"))
                    {
                        if (yn.ShowDialog() == DialogResult.Yes)
                        {
                            int id = meta.Dealer.EntityId > 0
                                ? meta.Dealer.EntityId
                                : ResolveDealerId(meta.Dealer.Name);
                            if (id > 0)
                            {
                                _hiddenDealerIds.Add(id);
                                removed = true;
                            }
                        }
                    }
                }
            }

            if (!removed)
                MessageBox.Show("Is row par koi entry nahi mili.\nCustomer ya dealer wali row select karein.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                RefreshGridIfLoaded("Entry grid se hata di.");

            SaveSessionState();
        }

        private int ResolveCustomerId(string name)
        {
            var ht = new Hashtable { { "@name", name } };
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT id FROM AddCustomer WHERE TRIM(Name) = @name COLLATE NOCASE LIMIT 1", ht);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["id"]);
            return 0;
        }

        private int ResolveDealerId(string name)
        {
            var ht = new Hashtable { { "@name", name } };
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT Did FROM AddDealer WHERE TRIM(DealerName) = @name COLLATE NOCASE LIMIT 1", ht);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["Did"]);
            return 0;
        }

        private decimal CalculateCustomerBalanceAfterCredit(int customerId, decimal newCredit)
        {
            var ht = new Hashtable { { "@customerId", customerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(@"
                SELECT IFNULL(SUM(IFNULL(Amount, 0) + IFNULL(Advance, 0)), 0) AS TotalAmount,
                       IFNULL(SUM(IFNULL(Credit, 0)), 0) AS TotalCredit
                FROM PetrolAdd WHERE CustomerId = @customerId", ht);

            decimal totalAmount = 0m, totalCredit = 0m;
            if (dt != null && dt.Rows.Count > 0)
            {
                totalAmount = SafeToDecimal(dt.Rows[0]["TotalAmount"]);
                totalCredit = SafeToDecimal(dt.Rows[0]["TotalCredit"]);
            }
            return totalAmount - (totalCredit + newCredit);
        }

        private void PostSelectedToCreditCustomer()
        {
            GridRowMeta meta = GetSelectedGridMeta();
            if (meta == null || !meta.Customer.HasValue)
            {
                MessageBox.Show("Pehle grid se customer wali row select karein.", "Select Row",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (meta.Customer.IsTemp)
            {
                MessageBox.Show("Temp entry ko Credit nahi kiya ja sakta.\n✕ button se sirf remove karein.",
                    "Temp Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (meta.Customer.IsBank)
            {
                MessageBox.Show("Bank entry ko Credit Customer mein post nahi kiya ja sakta.\nYe sirf Closing2 display ke liye hai.",
                    "Bank Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = meta.Customer.Name;
            int customerId = meta.Customer.EntityId > 0
                ? meta.Customer.EntityId
                : ResolveCustomerId(name);

            if (customerId <= 0)
            {
                MessageBox.Show($"Customer '{name}' database mein nahi mila.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal amount = GetLiveCustomerReceivable(customerId);
            if (amount <= 0)
            {
                MessageBox.Show($"'{name}' ka receivable balance 0 ya negative hai.\nGrid refresh karein (Load).",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGridIfLoaded();
                return;
            }

            using (var yn = new YesOrNoMessage(
                $"Kya {name} ki poori {amount:N2} Credit Customer mein post karein?\n(Ye permanent entry hai — database mein save hogi.)",
                "Confirm Credit Customer"))
            {
                if (yn.ShowDialog() != DialogResult.Yes)
                    return;
            }

            try
            {
                string office = GetClosingOfficeForSave();
                if (string.IsNullOrWhiteSpace(office))
                {
                    MessageBox.Show("Credit post se pehle Office select karein (Hub Chowki / Band Murad / Askari).",
                        "Office", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!OfficeHelper.ConfirmCustomerForOffice(customerId, name, office))
                    return;

                decimal balance = CalculateCustomerBalanceAfterCredit(customerId, amount);
                string receipt = "CL2-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var ht = new Hashtable
                {
                    { "@customerId", customerId },
                    { "@date", DateTime.Today.ToString("yyyy-MM-dd") },
                    { "@receiptNo", receipt },
                    { "@credit", amount.ToString("F2") },
                    { "@balance", balance.ToString("F2") },
                    { "@note", "Closing2 → Credit Customer" },
                    { "@office", office }
                };

                string qry = @"INSERT INTO PetrolAdd (CustomerId, Date, ReceiptNo, Credit, Balance, Note, IsInitialEntry, OfficeName)
                               VALUES (@customerId, @date, @receiptNo, @credit, @balance, @note, 0, @office)";
                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r <= 0)
                {
                    MessageBox.Show("Credit Customer mein save nahi hua.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RefreshGridIfLoaded($"{name} ki amount Credit Customer mein post ho gayi.");
            }
            catch (Exception ex)
            {
                ShowError("Credit Customer post karte waqt error aaya.", ex);
            }
        }

        private void PostSelectedToDealerPayout()
        {
            GridRowMeta meta = GetSelectedGridMeta();
            if (meta == null || !meta.Dealer.HasValue)
            {
                MessageBox.Show("Pehle grid se dealer wali row select karein.", "Select Row",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (meta.Dealer.IsTemp)
            {
                MessageBox.Show("Temp entry ko Payout nahi kiya ja sakta.\n✕ button se sirf remove karein.",
                    "Temp Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (meta.Dealer.IsBank)
            {
                MessageBox.Show("Bank entry ko Dealer Payout mein post nahi kiya ja sakta.\nYe sirf Closing2 display ke liye hai.",
                    "Bank Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = meta.Dealer.Name;
            int dealerId = meta.Dealer.EntityId > 0
                ? meta.Dealer.EntityId
                : ResolveDealerId(name);

            if (dealerId <= 0)
            {
                MessageBox.Show($"Dealer '{name}' database mein nahi mila.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal amount = GetLiveDealerPayable(dealerId);
            if (amount <= 0)
            {
                MessageBox.Show($"'{name}' ka payable balance 0 ya negative hai.\nGrid refresh karein (Load).",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGridIfLoaded();
                return;
            }

            using (var yn = new YesOrNoMessage(
                $"Kya {name} ki poori {amount:N2} Dealer Payout mein post karein?\n(Ye permanent entry hai — database mein save hogi.)",
                "Confirm Dealer Payout"))
            {
                if (yn.ShowDialog() != DialogResult.Yes)
                    return;
            }

            try
            {
                string office = GetClosingOfficeForSave();
                if (string.IsNullOrWhiteSpace(office))
                {
                    MessageBox.Show("Dealer Payout se pehle Office select karein (Hub Chowki / Band Murad / Askari).",
                        "Office", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!OfficeHelper.ConfirmDealerForOffice(dealerId, name, office))
                    return;

                var ht = new Hashtable
                {
                    { "@dealerId", dealerId },
                    { "@amountPaid", amount.ToString("F2") },
                    { "@paymentDate", DateTime.Today.ToString("yyyy-MM-dd") },
                    { "@note", "Closing2 → Dealer Payout" },
                    { "@office", office }
                };

                string insertQry = @"INSERT INTO DieselLedgerDebit (Did, AmounGiven, Date, Note, OfficeName)
                                     VALUES (@dealerId, @amountPaid, @paymentDate, @note, @office)";
                int r = MainClass.DataInsertUpdateDelete(insertQry, ht);
                if (r <= 0)
                {
                    MessageBox.Show("Dealer Payout mein save nahi hua.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var htUpd = new Hashtable
                {
                    { "@dealerId", dealerId },
                    { "@amount", amount.ToString("F2") }
                };
                MainClass.DataInsertUpdateDelete(
                    "UPDATE AddDealer SET DDAmount = IFNULL(DDAmount, 0) + @amount WHERE Did = @dealerId", htUpd);

                RefreshGridIfLoaded($"{name} ki amount Dealer Payout mein post ho gayi.");
            }
            catch (Exception ex)
            {
                ShowError("Dealer Payout post karte waqt error aaya.", ex);
            }
        }

        private void AddTempEntry()
        {
            try
            {
                string name = (txtTempName.Text ?? "").Trim();
                string mode = cbTempMode.SelectedItem?.ToString() ?? "";
                string amtText = (txtTempAmount.Text ?? "").Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Name enter karo.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTempName.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(mode))
                {
                    MessageBox.Show("Mode select karo (Customer / Dealer / Expense).", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbTempMode.Focus();
                    return;
                }
                if (!decimal.TryParse(amtText, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal amount))
                    decimal.TryParse(amtText, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
                if (amount <= 0)
                {
                    MessageBox.Show("Sahi amount enter karo (0 se zyada).", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTempAmount.Focus();
                    return;
                }

                if (mode.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    if (_tempCustomerEntries == null)
                    {
                        _tempCustomerEntries = new DataTable();
                        _tempCustomerEntries.Columns.Add("CustomerName", typeof(string));
                        _tempCustomerEntries.Columns.Add("CustomerReceivable", typeof(decimal));
                    }
                    DataRow ex = null;
                    foreach (DataRow dr in _tempCustomerEntries.Rows)
                        if (string.Equals(Convert.ToString(dr["CustomerName"]), name, StringComparison.OrdinalIgnoreCase))
                        { ex = dr; break; }
                    if (ex == null)
                    {
                        var nr = _tempCustomerEntries.NewRow();
                        nr["CustomerName"] = name;
                        nr["CustomerReceivable"] = amount;
                        _tempCustomerEntries.Rows.Add(nr);
                    }
                    else
                        ex["CustomerReceivable"] = SafeToDecimal(ex["CustomerReceivable"]) + amount;
                }
                else
                {
                    // Dealer or Expense → both go to Dealer (right/payable) side
                    string displayName = mode.Equals("Expense", StringComparison.OrdinalIgnoreCase)
                        ? $"[Exp] {name}" : name;

                    if (_tempDealerEntries == null)
                    {
                        _tempDealerEntries = new DataTable();
                        _tempDealerEntries.Columns.Add("DealerName", typeof(string));
                        _tempDealerEntries.Columns.Add("DealerAmount", typeof(decimal));
                    }
                    DataRow ex = null;
                    foreach (DataRow dr in _tempDealerEntries.Rows)
                        if (string.Equals(Convert.ToString(dr["DealerName"]), displayName, StringComparison.OrdinalIgnoreCase))
                        { ex = dr; break; }
                    if (ex == null)
                    {
                        var nr = _tempDealerEntries.NewRow();
                        nr["DealerName"] = displayName;
                        nr["DealerAmount"] = amount;
                        _tempDealerEntries.Rows.Add(nr);
                    }
                    else
                        ex["DealerAmount"] = SafeToDecimal(ex["DealerAmount"]) + amount;
                }

                txtTempName.Text = "";
                txtTempAmount.Text = "";
                cbTempMode.SelectedIndex = -1;
                txtTempName.Focus();

                if (_dataLoaded)
                    LoadReceivablePayableGrid();
                else
                    MessageBox.Show("Entry add ho gayi. Pehle Load karein to grid mein dikhe.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                SaveSessionState();
            }
            catch (Exception ex)
            {
                ShowError("Temp entry add karte waqt error aaya.", ex);
            }
        }

        // Merges tempTable rows into a copy of dbTable; returns merged DataTable
        private DataTable AppendTempToTable(DataTable dbTable, DataTable tempTable,
            string nameCol, string amtCol)
        {
            if (tempTable == null || tempTable.Rows.Count == 0)
                return dbTable;

            DataTable result;
            if (dbTable != null)
                result = dbTable.Copy();
            else
            {
                result = new DataTable();
                result.Columns.Add(nameCol, typeof(string));
                result.Columns.Add(amtCol, typeof(decimal));
            }

            foreach (DataRow dr in tempTable.Rows)
            {
                DataRow nr = result.NewRow();
                if (result.Columns.Contains(nameCol) && tempTable.Columns.Contains(nameCol))
                    nr[nameCol] = dr[nameCol];
                if (result.Columns.Contains(amtCol) && tempTable.Columns.Contains(amtCol))
                    nr[amtCol] = dr[amtCol];
                result.Rows.Add(nr);
            }
            return result;
        }

        private void CalculateProfitAndLoadSummary()
        {
            try
            {
                ResetCalculationLabels();
                _dataLoaded = false;

                if (dtpStart.Value.Date > dtpEnd.Value.Date)
                {
                    MessageBox.Show("Start date end date se aage nahi ho sakti.", "Invalid Date Range",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string startDate = dtpStart.Value.Date.ToString("yyyy-MM-dd");
                string endDate = dtpEnd.Value.Date.ToString("yyyy-MM-dd");
                string office = GetClosingOfficeFilter();
                string petrolOffice = OfficeHelper.OfficeSqlFilter("OfficeName", office);
                string stockOffice = OfficeHelper.OfficeSqlFilter("OfficeName", office);
                string expenseOffice = OfficeHelper.OfficeSqlFilter("OfficeName", office);

                Hashtable htRange = new Hashtable
                {
                    { "@StartDate", startDate },
                    { "@EndDate", endDate }
                };

                _totalCustomerAmount = 0m;
                _totalCustomerLitter = 0m;

                string qSaleRange = @"
            SELECT 
                IFNULL(
                    SUM(
                        CASE 
                            WHEN IFNULL(Litter, 0) = 0 
                              OR IFNULL(Rate,   0) = 0
                            THEN IFNULL(Balance, 0)
                            ELSE (Litter * Rate)
                        END
                    ),
                    0
                ) AS TotalCustomerAmount,
                IFNULL(SUM(IFNULL(Litter, 0)), 0) AS TotalCustomerLitter
            FROM PetrolAdd
            WHERE date(Date) >= date(@StartDate) 
              AND date(Date) <= date(@EndDate)
              AND IFNULL(IsInitialEntry, 0) = 1" + petrolOffice + ";";

                DataTable dtSale = MainClass.ExecuteSelectQuery(qSaleRange, htRange);
                if (dtSale != null && dtSale.Rows.Count > 0)
                {
                    _totalCustomerAmount = SafeToDecimal(dtSale.Rows[0]["TotalCustomerAmount"]);
                    _totalCustomerLitter = SafeToDecimal(dtSale.Rows[0]["TotalCustomerLitter"]);
                }

                _totalLitterPurchase = 0m;
                _totalValuePurchase = 0m;

                string qPurchaseRange = @"
    SELECT
        IFNULL(SUM(AddDisel), 0) AS TotalLitterPurchase,
        IFNULL(SUM(AddDisel * Rate), 0) AS TotalValuePurchase
    FROM AddStock
    WHERE date(Date) >= date(@StartDate) AND date(Date) <= date(@EndDate)" + stockOffice + ";";

                DataTable dtPurchase = MainClass.ExecuteSelectQuery(qPurchaseRange, htRange);
                if (dtPurchase != null && dtPurchase.Rows.Count > 0)
                {
                    _totalLitterPurchase = SafeToDecimal(dtPurchase.Rows[0]["TotalLitterPurchase"]);
                    _totalValuePurchase = SafeToDecimal(dtPurchase.Rows[0]["TotalValuePurchase"]);
                }

                _customerAvgRate = _totalCustomerLitter > 0
                    ? _totalCustomerAmount / _totalCustomerLitter
                    : 0m;
                _dealerAvgRate = _totalLitterPurchase > 0
                    ? _totalValuePurchase / _totalLitterPurchase
                    : 0m;

                _rateDiff = _customerAvgRate - _dealerAvgRate;
                _grossProfit = _rateDiff * _totalCustomerLitter;

                string qExpense = @"
                    SELECT IFNULL(SUM(Amount), 0) AS TotalExpense
                    FROM Expensetable
                    WHERE date(EDate) >= date(@StartDate) AND date(EDate) <= date(@EndDate)" + expenseOffice + ";";

                DataTable dtExpense = MainClass.ExecuteSelectQuery(qExpense, htRange);
                _totalExpense = 0m;
                if (dtExpense != null && dtExpense.Rows.Count > 0)
                    _totalExpense = SafeToDecimal(dtExpense.Rows[0]["TotalExpense"]);

                _netProfit = _grossProfit - _totalExpense;

                lblCustomerSale.Text = _customerAvgRate.ToString("N3");
                lblDealerPurchase.Text = _dealerAvgRate.ToString("N3");
                lblMarginPerLiter.Text = _rateDiff.ToString("N3");
                lblTotalLiters.Text = _totalCustomerLitter.ToString("N3");
                lblGrossProfit.Text = _grossProfit.ToString("N2");
                lblExpenseTotal.Text = _totalExpense.ToString("N2");
                lblNetProfit.Text = _netProfit.ToString("N2");

                lblDealerResult.Text =
                    $"Sum Amount: {_totalValuePurchase:F2}, Sum AddDisel: {_totalLitterPurchase:F2}, Average: {_dealerAvgRate:F2}";

                LoadReceivablePayableGrid();
                _dataLoaded = true;
                SaveSessionState();
            }
            catch (Exception ex)
            {
                ShowError("Closing2 calculate karte waqt error aaya.", ex);
            }
        }

        private void UpdateSumAmountLabelWithBank()
        {
            lblCustomerResult.Text =
                $"Sum Amount: {_totalCustomerAmount:F2}, Sum Litter: {_totalCustomerLitter:F2}, Average: {_customerAvgRate:F2}  |  Bank Total: {_totalBankAmount:N2}";
        }

        private void LoadReceivablePayableGrid()
        {
            string office = GetClosingOfficeFilter();
            string petrolOffice = OfficeHelper.OfficeSqlFilter("p.OfficeName", office);
            string dealerOffice = OfficeHelper.OfficeSqlFilter("d.OfficeName", office);

            // Customer / Dealer — overall balance (Closing form jaisa), date se filter nahi
            string qCustomerCredits = @"
                    SELECT 
                        c.id AS CustomerId,
                        c.Name AS CustomerName,
                        IFNULL(
                            SUM(
                                IFNULL(p.Amount, 0) 
                                + IFNULL(p.Advance, 0)
                                - IFNULL(p.Credit, 0)
                            ),
                            0
                        ) AS CustomerReceivable
                    FROM PetrolAdd p
                    INNER JOIN AddCustomer c ON c.id = p.CustomerId
                    WHERE 1=1" + petrolOffice + @"
                    GROUP BY c.id
                    HAVING CustomerReceivable > 0
                    ORDER BY c.Name;";

            string qDealerList = @"
                    SELECT 
                        d.Did,
                        d.DealerName,
                        (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) AS DealerAmount
                    FROM AddDealer d
                    WHERE (IFNULL(d.DDAmount, 0) - IFNULL(d.DAmount, 0)) > 0" + dealerOffice + @"
                    ORDER BY d.DealerName;";

            _customerCredits = FilterPositiveAmountRows(SafeGetData(qCustomerCredits), "CustomerReceivable");
            _dealerList = FilterPositiveAmountRows(SafeGetData(qDealerList), "DealerAmount");

            _customerCredits = FilterHiddenCustomerRows(_customerCredits);
            _dealerList = FilterHiddenDealerRows(_dealerList);

            _customerDbRowCount = _customerCredits?.Rows.Count ?? 0;
            _dealerDbRowCount = _dealerList?.Rows.Count ?? 0;

            // Bank Account — har bank ka total amount (naam ke bina)
            DataTable bankCustomerSide;
            DataTable bankDealerSide;
            LoadBankBalancesForClosing2(out bankCustomerSide, out bankDealerSide);
            bankCustomerSide = FilterHiddenBankCustomerRows(bankCustomerSide);
            bankDealerSide = FilterHiddenBankDealerRows(bankDealerSide);

            _customerCredits = AppendTempToTable(_customerCredits, bankCustomerSide, "CustomerName", "CustomerReceivable");
            _dealerList = AppendTempToTable(_dealerList, bankDealerSide, "DealerName", "DealerAmount");

            _customerBankRowCount = bankCustomerSide?.Rows.Count ?? 0;
            _dealerBankRowCount = bankDealerSide?.Rows.Count ?? 0;

            // Temp entries display ke liye end par — inhe Credit/Payout nahi hota
            _customerCredits = AppendTempToTable(_customerCredits, _tempCustomerEntries, "CustomerName", "CustomerReceivable");
            _dealerList = AppendTempToTable(_dealerList, _tempDealerEntries, "DealerName", "DealerAmount");

            string profitCustomerText = "";
            string profitDealerText = "";

            bool showProfitDealer = _netProfit > 0 && !_hideProfitDealerRow;
            bool showProfitCustomer = _netProfit < 0 && !_hideProfitCustomerRow;
            if (showProfitDealer)
                profitDealerText = $"Profit Dealer - {_netProfit:N2}";
            else if (showProfitCustomer)
                profitCustomerText = $"Loss Customer Payable - {Math.Abs(_netProfit):N2}";

            guna2DataGridView1.DataSource = null;
            guna2DataGridView1.Rows.Clear();
            _rowZoomLevels.Clear();
            guna2DataGridView1.AutoGenerateColumns = false;
            guna2DataGridView1.AllowUserToAddRows = false;
            guna2DataGridView1.ReadOnly = true;

            int customerCount = _customerCredits?.Rows.Count ?? 0;
            int dealerCount = _dealerList?.Rows.Count ?? 0;
            int extraRow = (showProfitDealer || showProfitCustomer) ? 1 : 0;
            int maxRows = Math.Max(customerCount, dealerCount) + extraRow;

            decimal totalReceivable = 0m;
            decimal totalPayable = 0m;

            for (int i = 0; i < maxRows; i++)
            {
                int rowIndex = guna2DataGridView1.Rows.Add();
                DataGridViewRow row = guna2DataGridView1.Rows[rowIndex];

                string receivableText = "";
                string payableText = "";
                string balanceText = "";

                if (i == 0 && extraRow == 1)
                {
                    receivableText = profitCustomerText;
                    payableText = profitDealerText;
                    if (showProfitDealer)
                    {
                        totalPayable += _netProfit;
                        balanceText = _netProfit.ToString("N2");
                    }
                    else if (showProfitCustomer)
                    {
                        totalReceivable += Math.Abs(_netProfit);
                        balanceText = _netProfit.ToString("N2");
                    }

                    row.Tag = new GridRowMeta
                    {
                        IsProfitRow = true,
                        IsProfitOnDealerSide = showProfitDealer
                    };
                }
                else
                {
                    int dataIndex = i - extraRow;
                    var gridMeta = new GridRowMeta();

                    if (dataIndex >= 0 && dataIndex < customerCount && _customerCredits != null)
                    {
                        bool isTempCustomer = dataIndex >= _customerDbRowCount + _customerBankRowCount;
                        bool isBankCustomer = dataIndex >= _customerDbRowCount &&
                                              dataIndex < _customerDbRowCount + _customerBankRowCount;
                        string custName = Convert.ToString(_customerCredits.Rows[dataIndex]["CustomerName"]);
                        decimal custReceive = SafeToDecimal(_customerCredits.Rows[dataIndex]["CustomerReceivable"]);
                        if (custReceive > 0)
                        {
                            receivableText = isTempCustomer
                                ? $"✕ [Temp] {custName} - {custReceive:N2}"
                                : isBankCustomer
                                    ? $"{custName} - {custReceive:N2}"
                                    : $"{custName} - {custReceive:N2}";
                            totalReceivable += custReceive;

                            int custId = 0;
                            if (!isTempCustomer && !isBankCustomer && _customerCredits.Columns.Contains("CustomerId") &&
                                _customerCredits.Rows[dataIndex]["CustomerId"] != DBNull.Value)
                                custId = Convert.ToInt32(_customerCredits.Rows[dataIndex]["CustomerId"]);

                            gridMeta.Customer = new Closing2SideMeta
                            {
                                IsTemp = isTempCustomer,
                                IsBank = isBankCustomer,
                                Name = custName,
                                Amount = custReceive,
                                EntityId = custId
                            };
                        }
                    }

                    if (dataIndex >= 0 && dataIndex < dealerCount && _dealerList != null)
                    {
                        bool isTempDealer = dataIndex >= _dealerDbRowCount + _dealerBankRowCount;
                        bool isBankDealer = dataIndex >= _dealerDbRowCount &&
                                            dataIndex < _dealerDbRowCount + _dealerBankRowCount;
                        string dealerName = Convert.ToString(_dealerList.Rows[dataIndex]["DealerName"]);
                        decimal dealerAmt = SafeToDecimal(_dealerList.Rows[dataIndex]["DealerAmount"]);
                        if (dealerAmt > 0)
                        {
                            payableText = isTempDealer
                                ? $"✕ [Temp] {dealerName} - {dealerAmt:N2}"
                                : $"{dealerName} - {dealerAmt:N2}";
                            totalPayable += dealerAmt;

                            int did = 0;
                            if (!isTempDealer && !isBankDealer && _dealerList.Columns.Contains("Did") &&
                                _dealerList.Rows[dataIndex]["Did"] != DBNull.Value)
                                did = Convert.ToInt32(_dealerList.Rows[dataIndex]["Did"]);

                            gridMeta.Dealer = new Closing2SideMeta
                            {
                                IsTemp = isTempDealer,
                                IsBank = isBankDealer,
                                Name = dealerName,
                                Amount = dealerAmt,
                                EntityId = did
                            };
                        }
                    }

                    row.Tag = gridMeta;
                }

                SafeSetCell(row, "dgvRecived", receivableText);
                SafeSetCell(row, "dgvPayable", payableText);
                SafeSetCell(row, "dgvNetProfit", balanceText);
            }

            lblCustomerTotal.Text = totalReceivable.ToString("N2");
            lblDealerTotal.Text = totalPayable.ToString("N2");
            lblNetBalance.Text = (totalReceivable - totalPayable).ToString("N2");
            UpdateSumAmountLabelWithBank();
            ApplyGridSearchFilter();
        }

        private void LoadBankBalancesForClosing2(out DataTable customerSide, out DataTable dealerSide)
        {
            _totalBankAmount = 0m;
            customerSide = new DataTable();
            customerSide.Columns.Add("CustomerName", typeof(string));
            customerSide.Columns.Add("CustomerReceivable", typeof(decimal));

            dealerSide = new DataTable();
            dealerSide.Columns.Add("DealerName", typeof(string));
            dealerSide.Columns.Add("DealerAmount", typeof(decimal));

            // Bank Account — har bank ka total (customer/dealer names ke bina)
            string qBank = @"
                SELECT
                    TRIM(IFNULL(BT.BankName, '')) AS BankName,
                    SUM(IFNULL(BT.Amount, 0)) AS NetBalance
                FROM BankTransactions BT
                WHERE TRIM(IFNULL(BT.BankName, '')) <> ''
                GROUP BY TRIM(IFNULL(BT.BankName, ''))
                HAVING ABS(SUM(IFNULL(BT.Amount, 0))) > 0.001
                ORDER BY BankName;";

            DataTable dt = SafeGetData(qBank);
            if (dt == null || dt.Rows.Count == 0)
                return;

            foreach (DataRow dr in dt.Rows)
            {
                string bankName = Convert.ToString(dr["BankName"])?.Trim() ?? "";
                decimal netBalance = SafeToDecimal(dr["NetBalance"]);
                if (string.IsNullOrEmpty(bankName))
                    continue;

                string displayName = $"Bank {bankName}";

                if (netBalance > 0)
                {
                    DataRow nr = customerSide.NewRow();
                    nr["CustomerName"] = displayName;
                    nr["CustomerReceivable"] = netBalance;
                    customerSide.Rows.Add(nr);
                    _totalBankAmount += netBalance;
                }
                else if (netBalance < 0)
                {
                    DataRow nr = dealerSide.NewRow();
                    nr["DealerName"] = displayName;
                    nr["DealerAmount"] = Math.Abs(netBalance);
                    dealerSide.Rows.Add(nr);
                    _totalBankAmount += Math.Abs(netBalance);
                }
            }
        }

        private void ResetCalculationLabels()
        {
            _dataLoaded = false;
            lblCustomerSale.Text = "0.000";
            lblDealerPurchase.Text = "0.000";
            lblMarginPerLiter.Text = "0.000";
            lblTotalLiters.Text = "0.000";
            lblGrossProfit.Text = "0.00";
            lblExpenseTotal.Text = "0.00";
            lblNetProfit.Text = "0.00";
            lblCustomerResult.Text = "Customer Average";
            lblDealerResult.Text = "Dealer Average";
            lblCustomerTotal.Text = "0.00";
            lblDealerTotal.Text = "0.00";
            lblNetBalance.Text = "0.00";
        }

        private void ExportToExcel()
        {
            try
            {
                if (!_dataLoaded)
                    CalculateProfitAndLoadSummary();

                if (!_dataLoaded)
                {
                    MessageBox.Show("Pehle date select karke Load karein.", "Excel",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel File (*.xls)|*.xls";
                    sfd.Title = "Excel file save karein";
                    sfd.FileName = "Closing2_" + dtpStart.Value.ToString("yyyyMMdd") + "_" +
                                   dtpEnd.Value.ToString("yyyyMMdd") + ".xls";

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    WriteExcelWithFormulas(sfd.FileName);

                    MessageBox.Show("Excel file successfully create ho gayi (formulas included).", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError("Excel export karte waqt error aaya.", ex);
            }
        }

        private void WriteExcelWithFormulas(string filePath)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine("<Styles>");
            sb.AppendLine("<Style ss:ID=\"Default\" ss:Name=\"Normal\"><Alignment ss:Vertical=\"Bottom\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Title\"><Font ss:Bold=\"1\" ss:Size=\"13\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Section\"><Font ss:Bold=\"1\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Header\"><Font ss:Bold=\"1\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Bold\"><Font ss:Bold=\"1\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Number\"><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"Number3\"><NumberFormat ss:Format=\"#,##0.000\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"CalcVal\"><Font ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"CalcVal3\"><Font ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0.000\"/></Style>");
            // Print-friendly: koi background color nahi, sirf bold + number format
            sb.AppendLine("<Style ss:ID=\"NumRed\"><Font ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"NumOrange\"><Font ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"NumYellow\"><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"TotalNum\"><Font ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.AppendLine("</Styles>");
            sb.AppendLine("<Worksheet ss:Name=\"Closing2\">");
            sb.AppendLine("<Table>");

            // ── Title ────────────────────────────────────────────────────────────
            XmlRowString(sb, $"Closing Statement  |  {dtpStart.Value:dd-MMM-yyyy}  to  {dtpEnd.Value:dd-MMM-yyyy}", 5, "Title");
            XmlRowEmpty(sb, 6);

            // ── Profit Calculation ────────────────────────────────────────────────
            XmlRowString(sb, "PROFIT CALCULATION", 2, "Section");
            sb.AppendLine("<Row ss:StyleID=\"Header\">");
            XmlCellString(sb, "Description");
            XmlCellString(sb, "Value");
            XmlCellString(sb, "");
            sb.AppendLine("</Row>");

            XmlCalcRow(sb, "Customer Sale — Total Amount (Rs)", _totalCustomerAmount, inv, "CalcVal");
            XmlCalcRow(sb, "Customer Sale — Total Liters (L)", _totalCustomerLitter, inv, "CalcVal");
            XmlCalcRow(sb, "Customer Average Sale Rate (Rs/L)", _customerAvgRate, inv, "CalcVal3");
            XmlCalcRow(sb, "Dealer Purchase — Total Amount (Rs)", _totalValuePurchase, inv, "CalcVal");
            XmlCalcRow(sb, "Dealer Purchase — Total Liters (L)", _totalLitterPurchase, inv, "CalcVal");
            XmlCalcRow(sb, "Dealer Average Purchase Rate (Rs/L)", _dealerAvgRate, inv, "CalcVal3");
            XmlCalcRow(sb, "Margin per Liter  (Sale Avg - Dealer Avg)", _rateDiff, inv, "CalcVal3");
            XmlCalcRow(sb, "Total Liters Sold (Profit Base)", _totalCustomerLitter, inv, "CalcVal");
            XmlCalcRow(sb, "Gross Profit  (Margin x Liters)", _grossProfit, inv, "CalcVal");
            XmlCalcRow(sb, "Total Expense", _totalExpense, inv, "CalcVal");

            // Net profit row — color coded
            sb.AppendLine("<Row ss:StyleID=\"Bold\">");
            XmlCellString(sb, _netProfit >= 0 ? "NET PROFIT" : "NET LOSS");
            sb.AppendLine($"<Cell ss:StyleID=\"{AmountColorStyle(Math.Abs(_netProfit))}\"><Data ss:Type=\"Number\">{_netProfit.ToString("G15", inv)}</Data></Cell>");
            XmlCellString(sb, _netProfit >= 0 ? "Profit" : "Loss");
            sb.AppendLine("</Row>");

            XmlRowEmpty(sb, 6);

            // ── Receivable / Payable ─────────────────────────────────────────────
            XmlRowString(sb, "CUSTOMER RECEIVABLE  /  DEALER PAYABLE  (Positive balances only)", 5, "Section");
            sb.AppendLine("<Row ss:StyleID=\"Header\">");
            XmlCellString(sb, "Customer Name");
            XmlCellString(sb, "Receivable (Rs)");
            XmlCellString(sb, "");
            XmlCellString(sb, "Dealer Name");
            XmlCellString(sb, "Payable (Rs)");
            XmlCellString(sb, "Notes");
            sb.AppendLine("</Row>");

            decimal totalReceivable = 0m;
            decimal totalPayable = 0m;

            if (_netProfit > 0)
            {
                totalPayable += _netProfit;
                XmlRowColorGrid(sb, "", null, "Profit Dealer", _netProfit, inv, "Net profit to dealer");
            }
            else if (_netProfit < 0)
            {
                decimal lossAmt = Math.Abs(_netProfit);
                totalReceivable += lossAmt;
                XmlRowColorGrid(sb, "Loss Customer Payable", lossAmt, "", null, inv, "Net loss to customer");
            }

            int customerCount = _customerCredits?.Rows.Count ?? 0;
            int dealerCount = _dealerList?.Rows.Count ?? 0;
            int pairRows = Math.Max(customerCount, dealerCount);

            for (int i = 0; i < pairRows; i++)
            {
                string custName = "";
                decimal? custAmt = null;
                string dealName = "";
                decimal? dealAmt = null;

                if (i < customerCount && _customerCredits != null)
                {
                    custName = Convert.ToString(_customerCredits.Rows[i]["CustomerName"]);
                    custAmt = SafeToDecimal(_customerCredits.Rows[i]["CustomerReceivable"]);
                    if (custAmt.HasValue) totalReceivable += custAmt.Value;
                }
                if (i < dealerCount && _dealerList != null)
                {
                    dealName = Convert.ToString(_dealerList.Rows[i]["DealerName"]);
                    dealAmt = SafeToDecimal(_dealerList.Rows[i]["DealerAmount"]);
                    if (dealAmt.HasValue) totalPayable += dealAmt.Value;
                }

                XmlRowColorGrid(sb, custName, custAmt, dealName, dealAmt, inv, "");
            }

            // TOTAL row — plain calculated values
            decimal balance = totalReceivable - totalPayable;
            sb.AppendLine("<Row ss:StyleID=\"Bold\">");
            XmlCellString(sb, "TOTAL RECEIVABLE");
            sb.AppendLine($"<Cell ss:StyleID=\"TotalNum\"><Data ss:Type=\"Number\">{totalReceivable.ToString("G15", inv)}</Data></Cell>");
            XmlCellString(sb, "");
            XmlCellString(sb, "TOTAL PAYABLE");
            sb.AppendLine($"<Cell ss:StyleID=\"TotalNum\"><Data ss:Type=\"Number\">{totalPayable.ToString("G15", inv)}</Data></Cell>");
            sb.AppendLine($"<Cell ss:StyleID=\"{AmountColorStyle(Math.Abs(balance))}\"><Data ss:Type=\"Number\">{balance.ToString("G15", inv)}</Data></Cell>");
            sb.AppendLine("</Row>");

            sb.AppendLine("</Table>");
            sb.AppendLine("</Worksheet>");
            sb.AppendLine("</Workbook>");

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        // Returns style ID based on amount value for color coding
        private static string AmountColorStyle(decimal amount)
        {
            if (amount >= 10000000m) return "NumRed";    // >= 1 crore → Red
            if (amount > 10000m)     return "NumOrange"; // > 10,000   → Orange
            return "NumYellow";                           // <= 10,000  → Yellow
        }

        // Calculation section row: label | colored value | empty
        private static void XmlCalcRow(StringBuilder sb, string label, decimal value, CultureInfo inv, string styleId)
        {
            sb.AppendLine("<Row>");
            XmlCellString(sb, label);
            sb.AppendLine($"<Cell ss:StyleID=\"{styleId}\"><Data ss:Type=\"Number\">{value.ToString("G15", inv)}</Data></Cell>");
            XmlCellString(sb, "");
            sb.AppendLine("</Row>");
        }

        // Grid row with color-coded customer and dealer amounts
        private static void XmlRowColorGrid(StringBuilder sb, string custName, decimal? custAmt,
            string dealName, decimal? dealAmt, CultureInfo inv, string note)
        {
            sb.AppendLine("<Row>");
            XmlCellString(sb, custName ?? "");
            if (custAmt.HasValue)
                sb.AppendLine($"<Cell ss:StyleID=\"{AmountColorStyle(custAmt.Value)}\"><Data ss:Type=\"Number\">{custAmt.Value.ToString("G15", inv)}</Data></Cell>");
            else
                XmlCellString(sb, "");
            XmlCellString(sb, "");
            XmlCellString(sb, dealName ?? "");
            if (dealAmt.HasValue)
                sb.AppendLine($"<Cell ss:StyleID=\"{AmountColorStyle(dealAmt.Value)}\"><Data ss:Type=\"Number\">{dealAmt.Value.ToString("G15", inv)}</Data></Cell>");
            else
                XmlCellString(sb, "");
            XmlCellString(sb, note);
            sb.AppendLine("</Row>");
        }

        private static void XmlRowString(StringBuilder sb, string text, int mergeAcross, string styleId)
        {
            sb.AppendLine("<Row>");
            sb.AppendLine($"<Cell ss:StyleID=\"{styleId}\" ss:MergeAcross=\"{mergeAcross}\"><Data ss:Type=\"String\">{XmlEscape(text)}</Data></Cell>");
            sb.AppendLine("</Row>");
        }

        private static void XmlRowEmpty(StringBuilder sb, int cells)
        {
            sb.AppendLine("<Row>");
            for (int i = 0; i < cells; i++)
                XmlCellString(sb, "");
            sb.AppendLine("</Row>");
        }

        private static void XmlCellString(StringBuilder sb, string text)
        {
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{XmlEscape(text)}</Data></Cell>");
        }

        private static string XmlEscape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);
        }

        private DataTable FilterPositiveAmountRows(DataTable source, string amountColumn)
        {
            if (source == null)
                return null;

            DataTable filtered = source.Clone();
            foreach (DataRow row in source.Rows)
            {
                if (SafeToDecimal(row[amountColumn]) > 0)
                    filtered.ImportRow(row);
            }
            return filtered;
        }

        private DataTable SafeGetData(string query, Hashtable parameters = null)
        {
            try
            {
                if (parameters != null)
                    return MainClass.ExecuteSelectQuery(query, parameters);
                return MainClass.GetData(query);
            }
            catch (Exception ex)
            {
                ShowError("Query run karte waqt error aaya.", ex);
                return null;
            }
        }

        private decimal SafeToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0m;
            if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;
            return 0m;
        }

        private void SafeSetCell(DataGridViewRow row, string columnName, object value)
        {
            if (row?.DataGridView == null || !row.DataGridView.Columns.Contains(columnName))
                return;
            row.Cells[columnName].Value = value ?? "";
        }

        private void ShowError(string context, Exception ex)
        {
            MessageBox.Show(context + Environment.NewLine + Environment.NewLine + "Detail: " + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
