using AlMadinaShahzad.Model;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace AlMadinaShahzad.View
{
    public partial class frmDiselView : SampleView
    {
        private readonly Timer _searchDebounceTimer = new Timer();
        private int _loadToken;
        private static bool _dbIndexEnsured;
        private Guna.UI2.WinForms.Guna2Button _btnAdd2;
        private Guna.UI2.WinForms.Guna2ComboBox _cmbEntryType;

        public frmDiselView()
        {
            InitializeComponent();
            this.KeyPreview = true;  // Enable form to capture key events

        }

     


        private void frmDiselView_Load(object sender, EventArgs e)
        {
            EnableGridDoubleBuffering();
            EnsureQueryIndex();
            try { DieselUnitReadingHelper.EnsureSchema(); } catch { }
            guna2DataGridView1.AutoGenerateColumns = false;
            OfficeHelper.AttachOfficeFilter(this, (s, ev) => LoadData1(), left: 700, top: 12);
            SetupAdd2AndEntryFilter();

            comboBox1.Visible = true;
            btnAverage.Visible = true;
            dtpStart.Value = DateTime.Now;
            dtpEnd.Value = DateTime.Now;

            comboBox1.Items.Clear();
            comboBox1.Items.Add("Rate");
            comboBox1.SelectedIndex = 0;

            _searchDebounceTimer.Interval = 400;
            _searchDebounceTimer.Tick += (s, ev) =>
            {
                _searchDebounceTimer.Stop();
                LoadData1();
            };

            dtpStart.ValueChanged += DatePickers_ValueChanged;
            dtpEnd.ValueChanged += DatePickers_ValueChanged;

            if (!guna2DataGridView1.Columns.Contains("dgvTotalBalance"))
            {
                DataGridViewTextBoxColumn totalBalanceColumn = new DataGridViewTextBoxColumn();
                totalBalanceColumn.Name = "dgvTotalBalance";
                totalBalanceColumn.HeaderText = "Total Balance";
                guna2DataGridView1.Columns.Add(totalBalanceColumn);
            }

            guna2DataGridView1.KeyDown += new KeyEventHandler(guna2DataGridView1_KeyDown);

            // Form turant dikhe — data background mein aaye (aaj ki date filter)
            LoadData1();
        }

        private static void EnsureQueryIndex()
        {
            if (_dbIndexEnsured) return;
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd1 = new SQLiteCommand(
                        "CREATE INDEX IF NOT EXISTS idx_PetrolAdd_Date_Initial ON PetrolAdd(Date, IsInitialEntry)", con))
                        cmd1.ExecuteNonQuery();
                    using (var cmd2 = new SQLiteCommand(
                        "CREATE INDEX IF NOT EXISTS idx_PetrolAdd_CustomerId ON PetrolAdd(CustomerId)", con))
                        cmd2.ExecuteNonQuery();
                }
                _dbIndexEnsured = true;
            }
            catch { /* index optional — load still works */ }
        }

        private void EnableGridDoubleBuffering()
        {
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, guna2DataGridView1, new object[] { true });
        }

        private void btnAverage_Click(object sender, EventArgs e)
        {
            // Calculate Average based on the selected ComboBox option (Rate)
            if (comboBox1.Visible && comboBox1.SelectedItem != null)
            {
                string selectedOption = comboBox1.SelectedItem.ToString();
                if (selectedOption == "Rate")
                {
                    CalculateAverage("Rate");
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid option!", "Info");
                    noDataMessage.ShowDialog();
                }
            }
        }

 
      
        private void frmCreditAdjust_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Ctrl key is pressed along with R
            if (e.Control && e.KeyCode == Keys.R)
            {
                // Call the method to reload the form
                LoadData1();
                CustomeMessage noDataMessage = new CustomeMessage("Form reloaded successfully!", "Info");
                noDataMessage.ShowDialog();
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmDiselAdd frm = new frmDiselAdd();
            frm.ShowDialog();
            LoadData1();
        }

        private void SetupAdd2AndEntryFilter()
        {
            if (_btnAdd2 != null) return;

            _btnAdd2 = new Guna.UI2.WinForms.Guna2Button
            {
                Name = "btnAdd2",
                Text = "Add2",
                Size = new Size(95, 33),
                BorderRadius = 15,
                FillColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnAdd2.Location = new Point(btnAdd.Left - _btnAdd2.Width - 8, btnAdd.Top);
            _btnAdd2.Click += (s, e) =>
            {
                using (var frm = new frmDiselAdd2())
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                        LoadData1();
                }
            };
            panel1.Controls.Add(_btnAdd2);
            _btnAdd2.BringToFront();

            // All / Normal / Unit — Add2 + Add ke bilkul neeche
            int entryWidth = Math.Max(110, btnAdd.Right - _btnAdd2.Left);
            _cmbEntryType = new Guna.UI2.WinForms.Guna2ComboBox
            {
                Name = "cmbEntryTypeFilter",
                Size = new Size(entryWidth, 32),
                BorderRadius = 8,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_btnAdd2.Left, _btnAdd2.Bottom + 8)
            };
            _cmbEntryType.Items.AddRange(new object[] { "All", "Normal", "Unit" });
            _cmbEntryType.SelectedIndex = 0;
            _cmbEntryType.SelectedIndexChanged += (s, e) => LoadData1();
            panel1.Controls.Add(_cmbEntryType);
            _cmbEntryType.BringToFront();
        }

        private string GetEntryTypeFilter()
        {
            if (_cmbEntryType == null || _cmbEntryType.SelectedItem == null)
                return "All";
            return Convert.ToString(_cmbEntryType.SelectedItem) ?? "All";
        }

        private void LoadData1()
        {
            int token = ++_loadToken;
            string searchText = txtSearch.Text ?? string.Empty;
            string office = OfficeHelper.GetSelectedOffice(this, allowAll: true);
            string entryType = GetEntryTypeFilter();
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;
            if (endDate < startDate)
            {
                DateTime tmp = startDate;
                startDate = endDate;
                endDate = tmp;
            }

            Task.Run(() => FetchPetrolData(searchText, startDate, endDate, office, entryType))
                .ContinueWith(t =>
                {
                    if (token != _loadToken || IsDisposed) return;
                    if (t.IsFaulted)
                    {
                        MessageBox.Show(t.Exception?.GetBaseException().Message ?? "Load failed.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    BindPetrolGrid(t.Result);
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static DataTable FetchPetrolData(string searchText, DateTime startDate, DateTime endDate, string office, string entryType = "All")
        {
            searchText = (searchText ?? string.Empty).Trim();
            string officeFilter = OfficeHelper.OfficeSqlFilter("PetrolAdd.OfficeName", office);
            // All = sales + credit customer; Normal = non-unit sales + credit; Unit = unit sales only
            string entryFilter = "";
            if (string.Equals(entryType, "Normal", StringComparison.OrdinalIgnoreCase))
                entryFilter = " AND (IFNULL(PetrolAdd.IsInitialEntry,1)=0 OR IFNULL(PetrolAdd.UnitReadingId,0)=0) ";
            else if (string.Equals(entryType, "Unit", StringComparison.OrdinalIgnoreCase))
                entryFilter = " AND IFNULL(PetrolAdd.IsInitialEntry,1)=1 AND IFNULL(PetrolAdd.UnitReadingId,0)<>0 ";

            // Sale (IsInitialEntry=1): Balance = Amount - Credit - Advance
            // Credit Customer (IsInitialEntry=0): Balance = -Credit (running se minus)
            string selectCore = @"
SELECT PetrolAdd.pid,
       PetrolAdd.Date,
       PetrolAdd.ReceiptNo,
       PetrolAdd.vehicle,
       IFNULL(PetrolAdd.Litter, 0) AS Litter,
       IFNULL(PetrolAdd.Rate, 0) AS Rate,
       IFNULL(PetrolAdd.Advance, 0) AS Advance,
       CASE
           WHEN IFNULL(PetrolAdd.IsInitialEntry, 1) = 0 THEN CAST(0 AS REAL)
           WHEN IFNULL(PetrolAdd.Litter, 0) <> 0 AND IFNULL(PetrolAdd.Rate, 0) <> 0
               THEN CAST(PetrolAdd.Litter * PetrolAdd.Rate AS REAL)
           ELSE CAST(IFNULL(PetrolAdd.Amount, 0) AS REAL)
       END AS Amount,
       IFNULL(PetrolAdd.Credit, 0) AS Credit,
       CAST((
           CASE
               WHEN IFNULL(PetrolAdd.IsInitialEntry, 1) = 0
                   THEN -IFNULL(PetrolAdd.Credit, 0)
               WHEN IFNULL(PetrolAdd.Litter, 0) <> 0 AND IFNULL(PetrolAdd.Rate, 0) <> 0
                   THEN PetrolAdd.Litter * PetrolAdd.Rate - IFNULL(PetrolAdd.Credit, 0) - IFNULL(PetrolAdd.Advance, 0)
               ELSE IFNULL(PetrolAdd.Amount, 0) - IFNULL(PetrolAdd.Credit, 0) - IFNULL(PetrolAdd.Advance, 0)
           END
       ) AS REAL) AS Balance,
       CAST(0 AS REAL) AS TotalBalance,
       CASE
           WHEN IFNULL(PetrolAdd.IsInitialEntry, 1) = 0
               THEN TRIM(IFNULL(PetrolAdd.Note,'') || CASE WHEN IFNULL(PetrolAdd.Note,'')='' THEN '' ELSE ' ' END || '[Credit]')
           ELSE PetrolAdd.Note
       END AS Note,
       AddCustomer.Name,
       IFNULL(PetrolAdd.UnitReadingId, 0) AS UnitReadingId,
       PetrolAdd.CustomerId,
       IFNULL(PetrolAdd.IsInitialEntry, 1) AS IsInitialEntry
FROM PetrolAdd
INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id
WHERE 1=1" + officeFilter + entryFilter;

            var dt = new DataTable();
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                string qry;
                var cmd = new SQLiteCommand { Connection = con };

                if (string.IsNullOrEmpty(searchText))
                {
                    // Bina search: sirf date range (pehle jaisa fast open)
                    qry = selectCore + @"
  AND date(PetrolAdd.Date) >= date(@StartDate)
  AND date(PetrolAdd.Date) <= date(@EndDate)
ORDER BY PetrolAdd.Date ASC";
                    cmd.CommandText = qry;
                    cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    // Search active: poori history — date filter nahi; naam sirf exact match (amount mix nahi)
                    string searchLike = "%" + searchText + "%";
                    int? exactCustomerId = TryGetExactCustomerId(con, searchText);

                    if (exactCustomerId.HasValue)
                    {
                        qry = selectCore + @"
  AND PetrolAdd.CustomerId = @CustomerId
ORDER BY PetrolAdd.Date ASC";
                        cmd.CommandText = qry;
                        cmd.Parameters.AddWithValue("@CustomerId", exactCustomerId.Value);
                    }
                    else
                    {
                        // Customer name exact nahi — vehicle / receipt / note
                        qry = selectCore + @"
  AND (
        IFNULL(PetrolAdd.vehicle, '') LIKE @SearchLike
        OR CAST(IFNULL(PetrolAdd.ReceiptNo, '') AS TEXT) LIKE @SearchLike
        OR IFNULL(PetrolAdd.Note, '') LIKE @SearchLike
      )
ORDER BY PetrolAdd.Date ASC";
                        cmd.CommandText = qry;
                        cmd.Parameters.AddWithValue("@SearchLike", searchLike);
                    }
                }

                using (cmd)
                using (var da = new SQLiteDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        private static int? TryGetExactCustomerId(SQLiteConnection con, string name)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT Id FROM AddCustomer WHERE TRIM(Name) = @Name COLLATE NOCASE LIMIT 1", con))
            {
                cmd.Parameters.AddWithValue("@Name", name.Trim());
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }

        private static List<int> GetCustomerIdsByNameLike(SQLiteConnection con, string searchLike)
        {
            // Deprecated path — exact name match only (similar names mix nahi honge)
            var ids = new List<int>();
            string exactName = (searchLike ?? "").Trim().Trim('%');
            if (string.IsNullOrEmpty(exactName))
                return ids;

            int? id = TryGetExactCustomerId(con, exactName);
            if (id.HasValue)
                ids.Add(id.Value);
            return ids;
        }

        private void BindPetrolGrid(DataTable dt)
        {
            ApplyRunningBalancesToTable(dt);

            guna2DataGridView1.SuspendLayout();
            try
            {
                guna2DataGridView1.AutoGenerateColumns = false;
                // Name-based binding — Credit/Balance mix nahi hoga
                MapCol("dgvid", "pid");
                MapCol("dgvDate", "Date");
                MapCol("dgvreciptno", "ReceiptNo");
                MapCol("dgvVechleNo", "vehicle");
                MapCol("dgvLitter", "Litter");
                MapCol("dgvrate", "Rate");
                MapCol("dgvAdvance", "Advance");
                MapCol("dgvAmount", "Amount");
                MapCol("dgvCredit", "Credit");
                MapCol("dgvBalance", "Balance");
                MapCol("dgvTotalBalance", "TotalBalance");
                MapCol("dgvNote", "Note");
                MapCol("dgvName", "Name");

                if (guna2DataGridView1.Columns.Contains("dgvCredit"))
                {
                    guna2DataGridView1.Columns["dgvCredit"].Visible = true;
                    guna2DataGridView1.Columns["dgvCredit"].MinimumWidth = 80;
                    guna2DataGridView1.Columns["dgvCredit"].Width = 90;
                }
                if (guna2DataGridView1.Columns.Contains("dgvTotalBalance"))
                    guna2DataGridView1.Columns["dgvTotalBalance"].HeaderText = "Running Balance";

                guna2DataGridView1.DataSource = dt;
                MainClass.SrNo(guna2DataGridView1);
                SetColumnOrder();
            }
            finally
            {
                guna2DataGridView1.ResumeLayout(true);
            }

            CalculateTotalLitter();
            guna2DataGridView1.Invalidate();
        }

        private void MapCol(string gridCol, string dataCol)
        {
            if (guna2DataGridView1.Columns.Contains(gridCol))
                guna2DataGridView1.Columns[gridCol].DataPropertyName = dataCol;
        }

        /// <summary>
        /// Har customer ka running balance: Amount - Credit - Advance accumulate (date order).
        /// </summary>
        private static void ApplyRunningBalancesToTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            if (!dt.Columns.Contains("TotalBalance"))
                dt.Columns.Add("TotalBalance", typeof(decimal));

            // Sort keys: CustomerId/Name + Date + pid
            var rows = dt.AsEnumerable().ToList();
            rows.Sort((a, b) =>
            {
                string na = Convert.ToString(a["Name"]) ?? "";
                string nb = Convert.ToString(b["Name"]) ?? "";
                int c = string.Compare(na, nb, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                DateTime da, db;
                DateTime.TryParse(Convert.ToString(a["Date"]), out da);
                DateTime.TryParse(Convert.ToString(b["Date"]), out db);
                c = da.CompareTo(db);
                if (c != 0) return c;
                int pa = a["pid"] != DBNull.Value ? Convert.ToInt32(a["pid"]) : 0;
                int pb = b["pid"] != DBNull.Value ? Convert.ToInt32(b["pid"]) : 0;
                return pa.CompareTo(pb);
            });

            var running = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in rows)
            {
                string name = Convert.ToString(row["Name"]) ?? "";
                // SQL already set Balance (sale net or -Credit for credit entries)
                decimal lineBalance = ToDec(row["Balance"]);
                if (!running.ContainsKey(name))
                    running[name] = 0;
                running[name] += lineBalance;
                row["TotalBalance"] = running[name];
            }
        }

        private static decimal ToDec(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            decimal d;
            return decimal.TryParse(Convert.ToString(v), NumberStyles.Any, CultureInfo.InvariantCulture, out d)
                || decimal.TryParse(Convert.ToString(v), NumberStyles.Any, CultureInfo.CurrentCulture, out d)
                ? d : 0;
        }


        private void SetColumnOrder()
        {
            guna2DataGridView1.Columns["dgvName"].DisplayIndex = 1;         // Name
            guna2DataGridView1.Columns["dgvDate"].DisplayIndex = 2;         // Date
            guna2DataGridView1.Columns["dgvreciptno"].DisplayIndex = 3;     // ReceiptNo
            guna2DataGridView1.Columns["dgvVechleNo"].DisplayIndex = 4;     // Vehicle
            guna2DataGridView1.Columns["dgvLitter"].DisplayIndex = 5;       // Litter
            guna2DataGridView1.Columns["dgvrate"].DisplayIndex = 6;         // Rate
            guna2DataGridView1.Columns["dgvAdvance"].DisplayIndex = 7;      // Advance
            guna2DataGridView1.Columns["dgvAmount"].DisplayIndex = 8;       // Amount
            guna2DataGridView1.Columns["dgvCredit"].DisplayIndex = 9;       // Credit
            guna2DataGridView1.Columns["dgvBalance"].DisplayIndex = 10;     // Balance
            guna2DataGridView1.Columns["dgvNote"].DisplayIndex = 11;        // Note
            guna2DataGridView1.Columns["dgvTotalBalance"].DisplayIndex = 12;// TotalBalance
        }


        // Method to update the TotalBalance in the database
        private void UpdateTotalBalanceInDatabase(int pid, decimal totalBalance)
        {
            string qry = "UPDATE PetrolAdd SET TotalBalance = @TotalBalance WHERE pid = @pid";

            Hashtable ht = new Hashtable();
            ht.Add("@TotalBalance", totalBalance);
            ht.Add("@pid", pid);

            MainClass.DataInsertUpdateDelete(qry, ht);  // Update the database
        }
        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string col = guna2DataGridView1.Columns[e.ColumnIndex].Name;
            if ((col == "dgvBalance" || col == "dgvTotalBalance") && e.Value != null && e.Value != DBNull.Value)
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal balanceValue))
                {
                    e.Value = balanceValue.ToString("N2");
                    e.FormattingApplied = true;
                    ApplyBalanceCellColor(e, balanceValue);
                }
            }
        }

        private static void ApplyBalanceCellColor(DataGridViewCellFormattingEventArgs e, decimal balanceValue)
        {
            decimal abs = Math.Abs(balanceValue);
            if (abs > 10000m)
            {
                e.CellStyle.BackColor = Color.FromArgb(220, 53, 69);
                e.CellStyle.ForeColor = Color.White;
            }
            else if (abs > 1000m)
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 255, 0);
                e.CellStyle.ForeColor = Color.Black;
            }
            else
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 140, 0);
                e.CellStyle.ForeColor = Color.White;
            }
        }

        // Method to set the order of the columns in DataGridView
        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow == null) return;
            int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["dgvid"].Value);

            // DB se UnitReadingId lo — grid binding miss hone par bhi sahi session khule
            int unitReadingId = LookupUnitReadingId(id);
            if (unitReadingId <= 0
                && guna2DataGridView1.CurrentRow.DataBoundItem is DataRowView drv
                && drv.Row.Table.Columns.Contains("UnitReadingId")
                && drv["UnitReadingId"] != null && drv["UnitReadingId"] != DBNull.Value)
            {
                unitReadingId = Convert.ToInt32(drv["UnitReadingId"]);
            }

            int isInitial = 1;
            if (guna2DataGridView1.CurrentRow.DataBoundItem is DataRowView drvInit
                && drvInit.Row.Table.Columns.Contains("IsInitialEntry")
                && drvInit["IsInitialEntry"] != null && drvInit["IsInitialEntry"] != DBNull.Value)
            {
                isInitial = Convert.ToInt32(drvInit["IsInitialEntry"]);
            }
            else
            {
                isInitial = LookupIsInitialEntry(id);
            }

            if (unitReadingId > 0)
            {
                using (var frm = new frmDiselAdd2(unitReadingId))
                    frm.ShowDialog(this);
            }
            else if (isInitial == 0)
            {
                // Credit Customer entry — update yahan se
                var frm = new frmCreditAdjustAdd();
                frm.id = id;
                frm.ShowDialog(this);
            }
            else
            {
                frmDiselAdd frm = new frmDiselAdd();
                frm.id = id;
                frm.ShowDialog();
            }
            LoadData1();
        }

        private static int LookupUnitReadingId(int pid)
        {
            try
            {
                DieselUnitReadingHelper.EnsureSchema();
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT IFNULL(UnitReadingId, 0) FROM PetrolAdd WHERE pid=@id LIMIT 1", con))
                    {
                        cmd.Parameters.AddWithValue("@id", pid);
                        object v = cmd.ExecuteScalar();
                        if (v == null || v == DBNull.Value) return 0;
                        return Convert.ToInt32(v);
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private static int LookupIsInitialEntry(int pid)
        {
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT IFNULL(IsInitialEntry, 1) FROM PetrolAdd WHERE pid=@id LIMIT 1", con))
                    {
                        cmd.Parameters.AddWithValue("@id", pid);
                        object v = cmd.ExecuteScalar();
                        if (v == null || v == DBNull.Value) return 1;
                        return Convert.ToInt32(v);
                    }
                }
            }
            catch
            {
                return 1;
            }
        }



        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void guna2DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (guna2DataGridView1.CurrentRow != null)
                {
                    // Confirm deletion
                    DialogResult confirmDelete = MessageBox.Show("Are you sure you want to delete this record?",
                                                                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (confirmDelete == DialogResult.Yes)
                    {
                        int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["dgvid"].Value);
                        string qry = "DELETE FROM PetrolAdd WHERE pid = " + id;
                        Hashtable ht = new Hashtable();
                        MainClass.DataInsertUpdateDelete(qry, ht);
                        MessageBox.Show("Record deleted successfully.");
                        LoadData1();
                    }
                }
            }
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedOption = comboBox1.SelectedItem.ToString();

                if (selectedOption == "Rate")
                {
                    CalculateAverage("Rate");
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid option!", "Info");
                    noDataMessage.ShowDialog();
                }
            }
        }

        private void CalculateAverage(string type)
        {
            DateTime startDate = dtpStart.Value.Date; // Start date
            DateTime endDate = dtpEnd.Value.Date; // End date
            decimal totalSum = 0;
            decimal totalLitterSum = 0;
            int rowCount = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                DateTime rowDate = Convert.ToDateTime(row.Cells["dgvDate"].Value).Date;

                // Check if row date is equal to or between start and end dates (inclusive)
                if (rowDate >= startDate && rowDate <= endDate)
                {
                    if (row.Cells["dgvAmount"].Value != DBNull.Value && row.Cells["dgvLitter"].Value != DBNull.Value)
                    {
                        decimal amount = Convert.ToDecimal(row.Cells["dgvAmount"].Value);
                        decimal litter = Convert.ToDecimal(row.Cells["dgvLitter"].Value);

                        totalSum += amount;
                        totalLitterSum += litter;
                        rowCount++;
                    }
                }
            }

            // Displaying the results
            if (rowCount > 0)
            {
                if (totalLitterSum != 0) // Ensure no division by zero
                {
                    decimal ratio = totalSum / totalLitterSum;
                    lblResult.Text = $"Sum Amount: {totalSum:F2}, Sum Litter: {totalLitterSum:F2}, Average: {ratio:F2}";
                }
                else
                {
                    lblResult.Text = "Total Litter is zero, cannot calculate ratio.";
                }
            }
            else
            {
                lblResult.Text = "No data found for the selected date range.";
            }
        }

        private void btnAverage_Click_1(object sender, EventArgs e)
        {
            // Calculate Average based on the selected ComboBox option (Rate)
            if (comboBox1.Visible && comboBox1.SelectedItem != null)
            {
                string selectedOption = comboBox1.SelectedItem.ToString();
                if (selectedOption == "Rate")
                {
                    CalculateAverage("Rate");
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid option!", "Info");
                    noDataMessage.ShowDialog();
                }
            }
        }


        // Method to calculate TotalBalance for each customer
        private void CalculateTotalBalance()
        {
            decimal runningTotalBalance = 0;
            string previousCustomer = "";
            int firstCustomerRowIndex = -1;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                string currentCustomer = row.Cells["dgvName"].Value?.ToString() ?? string.Empty;

                // If a new customer starts, show TotalBalance in the first row of the previous customer
                if (currentCustomer != previousCustomer && firstCustomerRowIndex != -1)
                {
                    guna2DataGridView1.Rows[firstCustomerRowIndex].Cells["dgvTotalBalance"].Value = runningTotalBalance.ToString("F2");
                    runningTotalBalance = 0; // Reset for new customer
                }

                // Calculate current customer's running total balance
                if (row.Cells["dgvBalance"].Value != DBNull.Value)
                {
                    decimal currentBalance = Convert.ToDecimal(row.Cells["dgvBalance"].Value);
                    runningTotalBalance += currentBalance;
                }

                // Track first row index for current customer
                if (currentCustomer != previousCustomer)
                {
                    firstCustomerRowIndex = row.Index; // Store first index for the customer
                }

                previousCustomer = currentCustomer; // Update previous customer
            }

            // Update TotalBalance for the last customer
            if (firstCustomerRowIndex != -1)
            {
                guna2DataGridView1.Rows[firstCustomerRowIndex].Cells["dgvTotalBalance"].Value = runningTotalBalance.ToString("F2");
            }
        }

        private void DatePickers_ValueChanged(object sender, EventArgs e)
        {
            LoadData1();
        }

        private void CalculateTotalLitter()
        {
            decimal totalLitterSum = 0;
            decimal totalAmountSum = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["dgvLitter"].Value != null && row.Cells["dgvLitter"].Value != DBNull.Value)
                    totalLitterSum += Convert.ToDecimal(row.Cells["dgvLitter"].Value);

                if (row.Cells["dgvAmount"].Value != null && row.Cells["dgvAmount"].Value != DBNull.Value)
                    totalAmountSum += Convert.ToDecimal(row.Cells["dgvAmount"].Value);
            }

            lblLitter.Text = $"Total Litter: {totalLitterSum:F2} L";
            lblAmount.Text = $"Total Amount: {totalAmountSum:F2} ";
        }

    }
}