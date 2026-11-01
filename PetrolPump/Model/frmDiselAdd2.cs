using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using Guna.UI2.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    /// <summary>
    /// Daily Diesel Sales Add2: Opening/Closing (unit select) then multiple customer litter/rate lines.
    /// Office only on header — not on each customer line.
    /// </summary>
    public class frmDiselAdd2 : Form
    {
        public int UnitReadingId { get; set; }

        private Guna2DateTimePicker dtpDate;
        private Guna2ComboBox cmbOffice;
        // Plain TextBox — Guna placeholder/Text kabhi Opening-Closing ko miss kar deta tha
        private TextBox txtOpening;
        private TextBox txtClosing;
        private TextBox txtSold;
        private Guna2TextBox txtHeaderNote;
        private Label lblSold;
        private Label lblAllocated;
        private Label lblRemaining;
        private DataGridView grid;
        private DataTable _customers;
        private bool _updating;

        public frmDiselAdd2()
        {
            BuildUi();
            Load += FrmDiselAdd2_Load;
        }

        public frmDiselAdd2(int unitReadingId) : this()
        {
            UnitReadingId = unitReadingId;
        }

        private void FrmDiselAdd2_Load(object sender, EventArgs e)
        {
            try
            {
                DieselUnitReadingHelper.EnsureSchema();
                LoadCustomers();
                FillOfficeCombo();
                if (UnitReadingId > 0)
                    LoadSession(UnitReadingId);
                else
                {
                    dtpDate.Value = DateTime.Now;
                    AddEmptyLine();
                }
                RecalcTotals();
                UpdateTitle();

                // Update open pe readings dobara apply (UI miss na ho)
                Shown -= FrmDiselAdd2_ShownReload;
                Shown += FrmDiselAdd2_ShownReload;
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Add2 load error: " + ex.Message, "Error").ShowDialog();
            }
        }

        private void FrmDiselAdd2_ShownReload(object sender, EventArgs e)
        {
            Shown -= FrmDiselAdd2_ShownReload;
            if (UnitReadingId <= 0) return;
            try
            {
                // Sirf header readings refresh — lines pehle se loaded
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT OpeningReading, ClosingReading, IFNULL(OpeningText,'') AS OpeningText, IFNULL(ClosingText,'') AS ClosingText
FROM DieselUnitReading WHERE Id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", UnitReadingId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return;
                            _updating = true;
                            try
                            {
                                SetReadingBox(txtOpening, r["OpeningReading"], Convert.ToString(r["OpeningText"]));
                                SetReadingBox(txtClosing, r["ClosingReading"], Convert.ToString(r["ClosingText"]));
                            }
                            finally { _updating = false; }
                        }
                    }
                }
                RecalcTotals();
                UpdateTitle();
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                AddEmptyLine();
                RecalcTotals();
                return true;
            }
            if (keyData == (Keys.Control | Keys.R))
            {
                RemoveSelectedLine();
                return true;
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveSession();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateTitle()
        {
            string open = (txtOpening != null ? txtOpening.Text : "") ?? "";
            string close = (txtClosing != null ? txtClosing.Text : "") ?? "";
            if (string.IsNullOrWhiteSpace(open)) open = FormatReading(GetOpening());
            if (string.IsNullOrWhiteSpace(close)) close = FormatReading(GetClosing());
            string date = dtpDate.Value.ToString("yyyy-MM-dd");
            string office = cmbOffice.SelectedItem != null ? Convert.ToString(cmbOffice.SelectedItem) : "";
            Text = "Add2 Unit Reading | " + office + " | " + date + " | Open " + open.Trim() + " → Close " + close.Trim();
        }

        private static string FormatReading(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            try
            {
                // SQLite REAL / string dono se exact display
                if (value is string s)
                {
                    s = s.Trim();
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ds)
                        || decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out ds))
                        return ds.ToString("0.##########", CultureInfo.InvariantCulture);
                    return s;
                }
                decimal d = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return d.ToString("0.##########", CultureInfo.InvariantCulture);
            }
            catch
            {
                return Convert.ToString(value) ?? "";
            }
        }

        private void BuildUi()
        {
            Text = "Daily Diesel Sales — Add2 (Unit Reading)";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 680);
            MinimumSize = new Size(900, 560);
            BackColor = Color.FromArgb(26, 29, 48);
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            KeyPreview = true;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 168,
                BackColor = Color.FromArgb(37, 41, 74),
                Padding = new Padding(12)
            };

            var lblDate = MakeLabel("Date", 12, 14);
            dtpDate = new Guna2DateTimePicker
            {
                Location = new Point(12, 36),
                Size = new Size(150, 34),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd",
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderRadius = 8
            };

            var lblOffice = MakeLabel("Unit (Pump)", 175, 14);
            cmbOffice = new Guna2ComboBox
            {
                Location = new Point(175, 36),
                Size = new Size(170, 34),
                BorderRadius = 8,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblOpen = MakeLabel("Opening Reading", 360, 14);
            txtOpening = MakeReadingBox(360, 36, 130);
            txtOpening.TextChanged += (s, e) => { if (!_updating) RecalcTotals(); };

            var lblClose = MakeLabel("Closing Reading", 505, 14);
            txtClosing = MakeReadingBox(505, 36, 130);
            txtClosing.TextChanged += (s, e) => { if (!_updating) RecalcTotals(); };

            lblSold = MakeLabel("Sold Litter: 0", 650, 42);
            lblSold.ForeColor = Color.FromArgb(80, 220, 160);
            lblSold.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            txtSold = MakeReadingBox(650, 36, 120);
            txtSold.ReadOnly = true;
            txtSold.TabStop = false;
            txtSold.BackColor = Color.FromArgb(30, 34, 60);

            var lblNote = MakeLabel("Header Note", 12, 82);
            txtHeaderNote = new Guna2TextBox
            {
                Location = new Point(12, 104),
                Size = new Size(620, 36),
                BorderRadius = 8,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                PlaceholderText = "Optional note for this unit reading"
            };

            lblAllocated = MakeLabel("Allocated: 0", 650, 100);
            lblAllocated.ForeColor = Color.White;
            lblRemaining = MakeLabel("Baqaya: 0", 650, 124);
            lblRemaining.ForeColor = Color.FromArgb(255, 180, 100);
            lblRemaining.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);

            header.Controls.Add(lblDate);
            header.Controls.Add(dtpDate);
            header.Controls.Add(lblOffice);
            header.Controls.Add(cmbOffice);
            header.Controls.Add(lblOpen);
            header.Controls.Add(txtOpening);
            header.Controls.Add(lblClose);
            header.Controls.Add(txtClosing);
            header.Controls.Add(lblSold);
            header.Controls.Add(txtSold);
            header.Controls.Add(lblNote);
            header.Controls.Add(txtHeaderNote);
            header.Controls.Add(lblAllocated);
            header.Controls.Add(lblRemaining);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Color.FromArgb(37, 41, 74)
            };

            var btnAddLine = MakeButton("Add Line (Ctrl+N)", 12, 12, 150, Color.FromArgb(52, 152, 219));
            btnAddLine.Click += (s, e) => { AddEmptyLine(); RecalcTotals(); };

            var btnRemoveLine = MakeButton("Remove (Ctrl+R)", 172, 12, 150, Color.FromArgb(192, 57, 43));
            btnRemoveLine.Click += (s, e) => RemoveSelectedLine();

            var btnSave = MakeButton("Save", 780, 12, 120, Color.FromArgb(39, 174, 96));
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Click += (s, e) => SaveSession();

            var btnCancel = MakeButton("Close", 910, 12, 120, Color.FromArgb(100, 100, 120));
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.Click += (s, e) => Close();

            footer.Controls.Add(btnAddLine);
            footer.Controls.Add(btnRemoveLine);
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);
            footer.Resize += (s, e) =>
            {
                btnCancel.Left = footer.Width - 132;
                btnSave.Left = footer.Width - 262;
            };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(26, 29, 48),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 48, 84);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(37, 41, 74);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 80, 140);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(32, 36, 64);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            grid.RowTemplate.Height = 32;
            grid.CellEndEdit += Grid_CellEndEdit;
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.EditingControlShowing += Grid_EditingControlShowing;
            grid.DataError += Grid_DataError;

            Controls.Add(grid);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
        }

        private static TextBox MakeReadingBox(int x, int y, int w)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 28),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Left
            };
        }

        private void SetReadingBox(TextBox box, object dbValue, string textFallback = null)
        {
            if (box == null) return;
            string text = !string.IsNullOrWhiteSpace(textFallback)
                ? textFallback.Trim()
                : FormatReading(dbValue);
            box.Text = text ?? "";
        }

        private decimal GetOpening() => ParseDec(txtOpening != null ? txtOpening.Text : null);
        private decimal GetClosing() => ParseDec(txtClosing != null ? txtClosing.Text : null);

        private static Guna2Button MakeButton(string text, int x, int y, int w, Color fill)
        {
            return new Guna2Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 36),
                BorderRadius = 10,
                FillColor = fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };
        }

        private void FillOfficeCombo()
        {
            cmbOffice.Items.Clear();
            foreach (string o in OfficeHelper.Offices)
                cmbOffice.Items.Add(o);
            if (cmbOffice.Items.Count > 0)
                cmbOffice.SelectedIndex = 0;
        }

        private void LoadCustomers()
        {
            DataTable raw = MainClass.ExecuteSelectQuery(
                "SELECT Id, Name FROM AddCustomer ORDER BY Name COLLATE NOCASE", null) ?? new DataTable();

            // Combo ke liye Id hamesha Int32 — warna edit/load pe FormatException aata hai
            _customers = new DataTable();
            _customers.Columns.Add("Id", typeof(int));
            _customers.Columns.Add("Name", typeof(string));
            foreach (DataRow r in raw.Rows)
            {
                if (r["Id"] == null || r["Id"] == DBNull.Value) continue;
                _customers.Rows.Add(Convert.ToInt32(r["Id"]), Convert.ToString(r["Name"]) ?? "");
            }
            SetupGridColumns();
        }

        private void SetupGridColumns()
        {
            grid.Columns.Clear();

            var colCust = new DataGridViewComboBoxColumn
            {
                Name = "CustomerId",
                HeaderText = "Customer",
                DataSource = _customers.Copy(),
                DisplayMember = "Name",
                ValueMember = "Id",
                ValueType = typeof(int),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FillWeight = 160,
                DisplayStyleForCurrentCellOnly = true
            };

            grid.Columns.Add(colCust);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vehicle", HeaderText = "Vehicle No", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Litter", HeaderText = "Litter", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rate", HeaderText = "Rate", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = "Amount",
                ReadOnly = true,
                FillWeight = 80
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Note", FillWeight = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Pid",
                HeaderText = "Pid",
                Visible = false,
                ValueType = typeof(int)
            });
        }

        private void AddEmptyLine()
        {
            int idx = grid.Rows.Add();
            // Combo empty = DBNull (null int invalid hota hai)
            grid.Rows[idx].Cells["CustomerId"].Value = DBNull.Value;
            grid.Rows[idx].Cells["Litter"].Value = "";
            grid.Rows[idx].Cells["Rate"].Value = "";
            grid.Rows[idx].Cells["Amount"].Value = "";
            grid.Rows[idx].Cells["Vehicle"].Value = "";
            grid.Rows[idx].Cells["Note"].Value = "";
            grid.Rows[idx].Cells["Pid"].Value = 0;
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Combo invalid value dialog suppress — value safe set karte hain
            e.ThrowException = false;
            e.Cancel = true;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "CustomerId")
            {
                try
                {
                    object v = grid.Rows[e.RowIndex].Cells["CustomerId"].Value;
                    int id = NormalizeCustomerId(v);
                    if (id > 0 && CustomerExists(id))
                        grid.Rows[e.RowIndex].Cells["CustomerId"].Value = id;
                    else
                        grid.Rows[e.RowIndex].Cells["CustomerId"].Value = DBNull.Value;
                }
                catch
                {
                    grid.Rows[e.RowIndex].Cells["CustomerId"].Value = DBNull.Value;
                }
            }
        }

        private static int NormalizeCustomerId(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            if (value is int i) return i;
            if (value is long l) return (int)l;
            return int.TryParse(Convert.ToString(value), out int parsed) ? parsed : 0;
        }

        private bool CustomerExists(int id)
        {
            if (_customers == null || id <= 0) return false;
            foreach (DataRow r in _customers.Rows)
            {
                if (Convert.ToInt32(r["Id"]) == id)
                    return true;
            }
            return false;
        }

        private void RemoveSelectedLine()
        {
            if (grid.CurrentRow == null || grid.Rows.Count == 0) return;
            if (grid.Rows.Count <= 1)
            {
                new CustomeMessage("Kam az kam 1 line rakhein.", "Warning").ShowDialog();
                return;
            }
            grid.Rows.RemoveAt(grid.CurrentRow.Index);
            RecalcTotals();
        }

        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is ComboBox cb)
            {
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _updating) return;
            RecalcTotals();
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            if (grid.Columns[e.ColumnIndex].Name == "CustomerId")
            {
                int id = NormalizeCustomerId(row.Cells["CustomerId"].Value);
                row.Cells["CustomerId"].Value = id > 0 && CustomerExists(id) ? (object)id : DBNull.Value;
            }
            decimal litter = ParseDec(row.Cells["Litter"].Value);
            decimal rate = ParseDec(row.Cells["Rate"].Value);
            row.Cells["Amount"].Value = (litter * rate).ToString("0.##", CultureInfo.InvariantCulture);
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            if (_updating) return;
            _updating = true;
            try
            {
                decimal opening = GetOpening();
                decimal closing = GetClosing();
                decimal sold = closing - opening;
                if (sold < 0) sold = 0;
                if (txtSold != null)
                    txtSold.Text = sold.ToString("0.##########", CultureInfo.InvariantCulture);
                lblSold.Text = "Sold Litter: " + sold.ToString("0.##", CultureInfo.InvariantCulture);

                decimal allocated = 0;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    allocated += ParseDec(row.Cells["Litter"].Value);
                    decimal litter = ParseDec(row.Cells["Litter"].Value);
                    decimal rate = ParseDec(row.Cells["Rate"].Value);
                    row.Cells["Amount"].Value = (litter * rate).ToString("0.##", CultureInfo.InvariantCulture);
                }

                decimal remaining = sold - allocated;
                lblAllocated.Text = "Allocated: " + allocated.ToString("0.##", CultureInfo.InvariantCulture);
                if (remaining > 0.009m)
                {
                    lblRemaining.Text = "Baqaya: " + remaining.ToString("0.##", CultureInfo.InvariantCulture) + " (kam hai)";
                    lblRemaining.ForeColor = Color.FromArgb(255, 180, 100);
                }
                else if (remaining < -0.009m)
                {
                    lblRemaining.Text = "Zyada: " + Math.Abs(remaining).ToString("0.##", CultureInfo.InvariantCulture) + " (reading se upar)";
                    lblRemaining.ForeColor = Color.FromArgb(255, 100, 100);
                }
                else
                {
                    lblRemaining.Text = "Baqaya: 0 (theek — save OK)";
                    lblRemaining.ForeColor = Color.FromArgb(80, 220, 160);
                }
            }
            finally
            {
                _updating = false;
            }
            try { UpdateTitle(); } catch { }
        }

        private static decimal ParseDec(object value)
        {
            string t = Convert.ToString(value) ?? "";
            t = t.Replace(",", "").Trim();
            if (string.IsNullOrEmpty(t)) return 0;
            return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)
                || decimal.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out d)
                ? d : 0;
        }

        private void LoadSession(int id)
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                DieselUnitReadingHelper.EnsureSchema(con);

                _updating = true;
                try
                {
                    using (var cmd = new SQLiteCommand(@"
SELECT Date, OfficeName, OpeningReading, ClosingReading, SoldLitter,
       IFNULL(OpeningText,'') AS OpeningText, IFNULL(ClosingText,'') AS ClosingText, Note
FROM DieselUnitReading WHERE Id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read())
                            {
                                new CustomeMessage("Unit reading session nahi mili.", "Warning").ShowDialog();
                                return;
                            }
                            if (DateTime.TryParse(Convert.ToString(r["Date"]), out DateTime dt))
                                dtpDate.Value = dt;
                            string office = Convert.ToString(r["OfficeName"]) ?? "";
                            for (int i = 0; i < cmbOffice.Items.Count; i++)
                            {
                                if (string.Equals(Convert.ToString(cmbOffice.Items[i]), office, StringComparison.OrdinalIgnoreCase))
                                {
                                    cmbOffice.SelectedIndex = i;
                                    break;
                                }
                            }

                            // Exact saved text pehle, warna REAL value
                            string openTxt = Convert.ToString(r["OpeningText"]);
                            string closeTxt = Convert.ToString(r["ClosingText"]);
                            SetReadingBox(txtOpening, r["OpeningReading"], openTxt);
                            SetReadingBox(txtClosing, r["ClosingReading"], closeTxt);
                            txtHeaderNote.Text = Convert.ToString(r["Note"]) ?? "";
                            UnitReadingId = id;
                        }
                    }

                    grid.Rows.Clear();
                    using (var cmd = new SQLiteCommand(@"
SELECT pid, CustomerId, IFNULL(vehicle,'') AS vehicle, IFNULL(Litter,0) AS Litter,
       IFNULL(Rate,0) AS Rate, IFNULL(Note,'') AS Note
FROM PetrolAdd
WHERE UnitReadingId=@id AND IFNULL(IsInitialEntry,1)=1
ORDER BY pid", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int idx = grid.Rows.Add();
                                int custId = NormalizeCustomerId(r["CustomerId"]);
                                grid.Rows[idx].Cells["CustomerId"].Value =
                                    custId > 0 && CustomerExists(custId) ? (object)custId : DBNull.Value;
                                grid.Rows[idx].Cells["Vehicle"].Value = Convert.ToString(r["vehicle"]);
                                decimal litter = Convert.ToDecimal(r["Litter"]);
                                decimal rate = Convert.ToDecimal(r["Rate"]);
                                grid.Rows[idx].Cells["Litter"].Value = litter.ToString("0.##########", CultureInfo.InvariantCulture);
                                grid.Rows[idx].Cells["Rate"].Value = rate.ToString("0.##########", CultureInfo.InvariantCulture);
                                grid.Rows[idx].Cells["Amount"].Value = (litter * rate).ToString("0.##", CultureInfo.InvariantCulture);
                                // Purana "[Unit] ..." append hata ke clean note dikhao
                                grid.Rows[idx].Cells["Note"].Value = StripUnitNoteTag(Convert.ToString(r["Note"]));
                                grid.Rows[idx].Cells["Pid"].Value = Convert.ToInt32(r["pid"]);
                            }
                        }
                    }

                    if (grid.Rows.Count == 0)
                        AddEmptyLine();
                }
                finally
                {
                    _updating = false;
                }
            }
            RecalcTotals();
            UpdateTitle();
        }

        private void SaveSession()
        {
            try
            {
                RecalcTotals();

                if (cmbOffice.SelectedItem == null || string.IsNullOrWhiteSpace(Convert.ToString(cmbOffice.SelectedItem)))
                {
                    new CustomeMessage("Unit select karein (Hub Chowki / Band Murad / Askari)!", "Warning").ShowDialog();
                    return;
                }
                string office = Convert.ToString(cmbOffice.SelectedItem).Trim();

                decimal opening = GetOpening();
                decimal closing = GetClosing();
                string openingText = (txtOpening.Text ?? "").Trim();
                string closingText = (txtClosing.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(openingText) || string.IsNullOrWhiteSpace(closingText))
                {
                    new CustomeMessage("Opening aur Closing reading dono daalein!", "Warning").ShowDialog();
                    return;
                }
                if (closing < opening)
                {
                    new CustomeMessage("Closing reading Opening se chhoti nahi ho sakti!", "Warning").ShowDialog();
                    return;
                }
                decimal sold = closing - opening;
                if (sold <= 0)
                {
                    new CustomeMessage("Sold Litter 0 se zyada hona chahiye (Closing - Opening).", "Warning").ShowDialog();
                    return;
                }

                var lines = new List<LineRow>();
                decimal allocated = 0;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    int customerId = NormalizeCustomerId(row.Cells["CustomerId"].Value);
                    if (customerId <= 0 || !CustomerExists(customerId))
                    {
                        new CustomeMessage("Har line par Customer select karein!", "Warning").ShowDialog();
                        return;
                    }
                    string customerName = "";
                    foreach (DataRow cr in _customers.Rows)
                    {
                        if (Convert.ToInt32(cr["Id"]) == customerId)
                        {
                            customerName = Convert.ToString(cr["Name"]);
                            break;
                        }
                    }

                    decimal litter = ParseDec(row.Cells["Litter"].Value);
                    decimal rate = ParseDec(row.Cells["Rate"].Value);
                    if (litter <= 0)
                    {
                        new CustomeMessage("Har line par Litter 0 se zyada daalein!", "Warning").ShowDialog();
                        return;
                    }
                    if (rate < 0)
                    {
                        new CustomeMessage("Rate negative nahi ho sakta!", "Warning").ShowDialog();
                        return;
                    }

                    if (!OfficeHelper.ConfirmCustomerForOffice(customerId, customerName, office))
                        return;

                    decimal amount = litter * rate;
                    allocated += litter;
                    lines.Add(new LineRow
                    {
                        CustomerId = customerId,
                        CustomerName = customerName,
                        Vehicle = Convert.ToString(row.Cells["Vehicle"].Value) ?? "",
                        Litter = litter,
                        Rate = rate,
                        Amount = amount,
                        Note = StripUnitNoteTag(Convert.ToString(row.Cells["Note"].Value)),
                        Pid = 0
                    });
                }

                if (lines.Count == 0)
                {
                    new CustomeMessage("Kam az kam 1 customer line daalein!", "Warning").ShowDialog();
                    return;
                }

                // Sirf jab Baqaya bilkul 0 ho — zyada/kam dono block
                if (allocated > sold + 0.009m)
                {
                    new CustomeMessage(
                        "Reading mein sirf " + sold.ToString("0.##") + " litter hain.\n" +
                        "Aap " + allocated.ToString("0.##") + " litter daal rahe ho (zyada).\n" +
                        "Zyada: " + (allocated - sold).ToString("0.##") + " — kam karke Baqaya 0 karo, tab save hoga.",
                        "Warning").ShowDialog();
                    return;
                }
                if (allocated < sold - 0.009m)
                {
                    new CustomeMessage(
                        "Reading mein " + sold.ToString("0.##") + " litter hain.\n" +
                        "Aap ne sirf " + allocated.ToString("0.##") + " litter diya (kam).\n" +
                        "Baqaya: " + (sold - allocated).ToString("0.##") + " — poora allocate karo (Baqaya 0), tab save hoga.",
                        "Warning").ShowDialog();
                    return;
                }

                string dateStr = dtpDate.Value.Date.ToString("yyyy-MM-dd");
                string headerNote = (txtHeaderNote.Text ?? "").Trim();

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    DieselUnitReadingHelper.EnsureSchema(con);
                    using (var tx = con.BeginTransaction())
                    {
                        int sessionId = UnitReadingId;
                        if (sessionId <= 0)
                        {
                            using (var cmd = new SQLiteCommand(@"
INSERT INTO DieselUnitReading (Date, OfficeName, OpeningReading, ClosingReading, SoldLitter, OpeningText, ClosingText, Note)
VALUES (@d, @o, @op, @cl, @sold, @opt, @clt, @n)", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@d", dateStr);
                                cmd.Parameters.AddWithValue("@o", office);
                                cmd.Parameters.AddWithValue("@op", opening);
                                cmd.Parameters.AddWithValue("@cl", closing);
                                cmd.Parameters.AddWithValue("@sold", sold);
                                cmd.Parameters.AddWithValue("@opt", openingText);
                                cmd.Parameters.AddWithValue("@clt", closingText);
                                cmd.Parameters.AddWithValue("@n", string.IsNullOrEmpty(headerNote) ? (object)DBNull.Value : headerNote);
                                cmd.ExecuteNonQuery();
                            }
                            using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid()", con, tx))
                                sessionId = Convert.ToInt32(idCmd.ExecuteScalar());
                        }
                        else
                        {
                            using (var cmd = new SQLiteCommand(@"
UPDATE DieselUnitReading
SET Date=@d, OfficeName=@o, OpeningReading=@op, ClosingReading=@cl, SoldLitter=@sold,
    OpeningText=@opt, ClosingText=@clt, Note=@n
WHERE Id=@id", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@d", dateStr);
                                cmd.Parameters.AddWithValue("@o", office);
                                cmd.Parameters.AddWithValue("@op", opening);
                                cmd.Parameters.AddWithValue("@cl", closing);
                                cmd.Parameters.AddWithValue("@sold", sold);
                                cmd.Parameters.AddWithValue("@opt", openingText);
                                cmd.Parameters.AddWithValue("@clt", closingText);
                                cmd.Parameters.AddWithValue("@n", string.IsNullOrEmpty(headerNote) ? (object)DBNull.Value : headerNote);
                                cmd.Parameters.AddWithValue("@id", sessionId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var del = new SQLiteCommand(
                                "DELETE FROM PetrolAdd WHERE UnitReadingId=@id AND IFNULL(IsInitialEntry,1)=1", con, tx))
                            {
                                del.Parameters.AddWithValue("@id", sessionId);
                                del.ExecuteNonQuery();
                            }
                        }

                        foreach (LineRow line in lines)
                        {
                            using (var cmd = new SQLiteCommand(@"
INSERT INTO PetrolAdd
    (CustomerId, Date, ReceiptNo, vehicle, Litter, Rate, Advance, Amount, Credit, Balance, Note, IsInitialEntry, OfficeName, UnitReadingId)
VALUES
    (@cid, @d, @rec, @veh, @lit, @rate, 0, @amt, 0, @bal, @note, 1, @office, @uid)", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@cid", line.CustomerId);
                                cmd.Parameters.AddWithValue("@d", dateStr);
                                cmd.Parameters.AddWithValue("@rec", "U" + sessionId);
                                cmd.Parameters.AddWithValue("@veh", string.IsNullOrWhiteSpace(line.Vehicle) ? (object)DBNull.Value : line.Vehicle.Trim());
                                cmd.Parameters.AddWithValue("@lit", line.Litter.ToString("F2", CultureInfo.InvariantCulture));
                                cmd.Parameters.AddWithValue("@rate", line.Rate.ToString("0.##", CultureInfo.InvariantCulture));
                                cmd.Parameters.AddWithValue("@amt", line.Amount.ToString("F2", CultureInfo.InvariantCulture));
                                cmd.Parameters.AddWithValue("@bal", line.Amount.ToString("F2", CultureInfo.InvariantCulture));
                                // Header note alag table mein — line note pe dobara chipkao mat
                                string note = StripUnitNoteTag(line.Note);
                                cmd.Parameters.AddWithValue("@note",
                                    string.IsNullOrEmpty(note) ? (object)DBNull.Value : note);
                                cmd.Parameters.AddWithValue("@office", office);
                                cmd.Parameters.AddWithValue("@uid", sessionId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        UnitReadingId = sessionId;
                    }
                }

                new CustomeMessage("Unit reading + sales save ho gaye!", "Success").ShowDialog();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Save error: " + ex.Message, "Error").ShowDialog();
            }
        }

        /// <summary>
        /// "ASD | [Unit] ASA | [Unit] ASA" → "ASD" (header note line pe accumulate nahi hona chahiye)
        /// </summary>
        private static string StripUnitNoteTag(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return "";
            string n = note.Trim();
            // Har "| [Unit] ..." / leading "[Unit] ..." hatao
            while (true)
            {
                int idx = n.IndexOf("| [Unit]", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    idx = n.IndexOf("|[Unit]", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    break;
                n = n.Substring(0, idx).TrimEnd();
            }
            if (n.StartsWith("[Unit]", StringComparison.OrdinalIgnoreCase))
            {
                n = n.Substring(6).TrimStart();
                // agar sirf header text reh gaya ho to empty (header alag field mein hai)
                n = "";
            }
            return n.Trim();
        }

        private sealed class LineRow
        {
            public int CustomerId;
            public string CustomerName;
            public string Vehicle;
            public decimal Litter;
            public decimal Rate;
            public decimal Amount;
            public string Note;
            public int Pid;
        }
    }
}

