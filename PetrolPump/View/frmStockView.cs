using AlMadinaShahzad.Model;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmStockView : SampleView
    {
        private readonly Timer _searchDebounceTimer = new Timer();
        private int _loadToken;
        private static bool _dbIndexEnsured;

        public frmStockView()
        {
            InitializeComponent();
        }

        private void frmStockView_Load(object sender, EventArgs e)
        {
            EnableGridDoubleBuffering();
            EnsureQueryIndex();

            comboBox1.Items.Add("TotalAmount/Divide");
            comboBox1.SelectedIndex = 0;
            dtpStart.Value = DateTime.Now;
            dtpEnd.Value = DateTime.Now;

            _searchDebounceTimer.Interval = 400;
            _searchDebounceTimer.Tick += (s, ev) =>
            {
                _searchDebounceTimer.Stop();
                LoadData1();
            };

            dtpStart.ValueChanged += DatePickers_ValueChanged;
            dtpEnd.ValueChanged += DatePickers_ValueChanged;
            guna2DataGridView1.CellFormatting += guna2DataGridView1_CellFormatting;

            OfficeHelper.AttachOfficeFilter(this, (s, ev) => LoadData1(), left: 520, top: 12);
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
                        "CREATE INDEX IF NOT EXISTS idx_AddStock_Date ON AddStock(Date)", con))
                        cmd1.ExecuteNonQuery();
                    using (var cmd2 = new SQLiteCommand(
                        "CREATE INDEX IF NOT EXISTS idx_AddStock_DealerId ON AddStock(DealerId)", con))
                        cmd2.ExecuteNonQuery();
                }
                _dbIndexEnsured = true;
            }
            catch { }
        }

        private void EnableGridDoubleBuffering()
        {
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, guna2DataGridView1, new object[] { true });
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            frmStockAdd frm = new frmStockAdd();
            frm.ShowDialog();
            LoadData1();
        }

        private void LoadData1()
        {
            int token = ++_loadToken;
            string searchText = txtSearch.Text ?? string.Empty;
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;
            if (endDate < startDate)
            {
                DateTime tmp = startDate;
                startDate = endDate;
                endDate = tmp;
            }

            string office = OfficeHelper.GetSelectedOffice(this, allowAll: true);
            string officeFilter = OfficeHelper.OfficeSqlFilter("AddStock.OfficeName", office);

            Task.Run(() => FetchStockData(searchText, startDate, endDate, officeFilter))
                .ContinueWith(t =>
                {
                    if (token != _loadToken || IsDisposed) return;
                    if (t.IsFaulted)
                    {
                        MessageBox.Show(t.Exception?.GetBaseException().Message ?? "Load failed.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    BindStockGrid(t.Result);
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static DataTable FetchStockData(string searchText, DateTime startDate, DateTime endDate, string officeFilter)
        {
            searchText = (searchText ?? string.Empty).Trim();
            officeFilter = officeFilter ?? string.Empty;
            const string selectCore = @"
SELECT AddStock.Sid,
       AddStock.Date,
       AddDealer.DealerName,
       AddStock.Vehicle,
       IFNULL(AddStock.Rate, 0) AS Rate,
       IFNULL(AddStock.AddDisel, 0) AS AddDisel,
       CAST(IFNULL(AddStock.Rate, 0) * IFNULL(AddStock.AddDisel, 0) AS DECIMAL(18, 3)) AS Amount,
       AddStock.Note
FROM AddStock
LEFT JOIN AddDealer ON AddStock.DealerId = AddDealer.Did";

            var dt = new DataTable();
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                string qry;
                var cmd = new SQLiteCommand { Connection = con };

                if (string.IsNullOrEmpty(searchText))
                {
                    qry = selectCore + @"
WHERE date(AddStock.Date) >= date(@StartDate)
  AND date(AddStock.Date) <= date(@EndDate)" + officeFilter + @"
ORDER BY AddStock.Date ASC";
                    cmd.CommandText = qry;
                    cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    string searchLike = "%" + searchText + "%";
                    int? exactDealerId = TryGetExactDealerId(con, searchText);

                    if (exactDealerId.HasValue)
                    {
                        qry = selectCore + @"
WHERE AddStock.DealerId = @DealerId" + officeFilter + @"
ORDER BY AddStock.Date ASC";
                        cmd.CommandText = qry;
                        cmd.Parameters.AddWithValue("@DealerId", exactDealerId.Value);
                    }
                    else
                    {
                        qry = selectCore + @"
WHERE (
        IFNULL(AddStock.Vehicle, '') LIKE @SearchLike
        OR IFNULL(AddStock.Note, '') LIKE @SearchLike
      )" + officeFilter + @"
ORDER BY AddStock.Date ASC";
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

        private static int? TryGetExactDealerId(SQLiteConnection con, string name)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT Did FROM AddDealer WHERE TRIM(DealerName) = @Name COLLATE NOCASE LIMIT 1", con))
            {
                cmd.Parameters.AddWithValue("@Name", name.Trim());
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }

        private static List<int> GetDealerIdsByNameLike(SQLiteConnection con, string searchLike)
        {
            var ids = new List<int>();
            string exactName = (searchLike ?? "").Trim().Trim('%');
            if (string.IsNullOrEmpty(exactName))
                return ids;

            int? id = TryGetExactDealerId(con, exactName);
            if (id.HasValue)
                ids.Add(id.Value);
            return ids;
        }

        private void BindStockGrid(DataTable dt)
        {
            guna2DataGridView1.SuspendLayout();
            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    if (!dt.Columns.Contains("Sr"))
                        dt.Columns.Add("Sr", typeof(int));
                    for (int i = 0; i < dt.Rows.Count; i++)
                        dt.Rows[i]["Sr"] = i + 1;

                    guna2DataGridView1.DataSource = dt;
                    if (guna2DataGridView1.Columns.Contains("Sid"))
                        guna2DataGridView1.Columns["Sid"].Visible = false;
                    if (guna2DataGridView1.Columns.Contains("Sr"))
                    {
                        guna2DataGridView1.Columns["Sr"].HeaderText = "S/N";
                        guna2DataGridView1.Columns["Sr"].DisplayIndex = 0;
                        guna2DataGridView1.Columns["Sr"].Width = 50;
                    }
                }
                else
                {
                    guna2DataGridView1.DataSource = null;
                }

                foreach (DataGridViewColumn column in guna2DataGridView1.Columns)
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            finally
            {
                guna2DataGridView1.ResumeLayout(true);
            }

            CalculateTotals();
            guna2DataGridView1.Invalidate();
        }

        private void CalculateTotals()
        {
            decimal totalDisel = 0;
            decimal totalAmount = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["AddDisel"].Value != null && row.Cells["AddDisel"].Value != DBNull.Value)
                    totalDisel += Convert.ToDecimal(row.Cells["AddDisel"].Value);

                if (guna2DataGridView1.Columns.Contains("Amount") &&
                    row.Cells["Amount"].Value != null && row.Cells["Amount"].Value != DBNull.Value)
                {
                    totalAmount += Convert.ToDecimal(row.Cells["Amount"].Value);
                }
                else if (row.Cells["Rate"].Value != null && row.Cells["Rate"].Value != DBNull.Value &&
                         row.Cells["AddDisel"].Value != null && row.Cells["AddDisel"].Value != DBNull.Value)
                {
                    totalAmount += Convert.ToDecimal(row.Cells["Rate"].Value) * Convert.ToDecimal(row.Cells["AddDisel"].Value);
                }
            }

            lblTotalDisel.Text = $"Total Disel: {totalDisel:F2} L";
            lblTotalAmount.Text = $"Total Amount: {totalAmount:F2} Rs";
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!guna2DataGridView1.Columns.Contains("Amount")) return;
            if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "Amount" || e.Value == null || e.Value == DBNull.Value)
                return;

            if (decimal.TryParse(e.Value.ToString(), out decimal amount))
            {
                e.Value = amount.ToString("N2");
                e.FormattingApplied = true;
                ApplyAmountCellColor(e, amount);
            }
        }

        private static void ApplyAmountCellColor(DataGridViewCellFormattingEventArgs e, decimal amount)
        {
            decimal abs = Math.Abs(amount);
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

        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (guna2DataGridView1.CurrentRow != null)
            {
                int id = Convert.ToInt32(guna2DataGridView1.CurrentRow.Cells["Sid"].Value);
                frmStockAdd frm = new frmStockAdd();
                frm.id = id;
                frm.ShowDialog();
                LoadData1();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void btnAverage_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedOption = comboBox1.SelectedItem.ToString();
                if (selectedOption == "TotalAmount/Divide")
                    CalculateAverageDiselRate();
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Please select a valid option!", "Info");
                    noDataMessage.ShowDialog();
                }
            }
            else
            {
                CustomeMessage noDataMessage = new CustomeMessage("Please select an option from the ComboBox.", "Info");
                noDataMessage.ShowDialog();
            }
        }

        private void CalculateAverageDiselRate()
        {
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;
            decimal totalAmountSum = 0;
            decimal totalDiselSum = 0;

            foreach (DataGridViewRow row in guna2DataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["Date"].Value == null || row.Cells["Date"].Value == DBNull.Value) continue;

                DateTime rowDate = Convert.ToDateTime(row.Cells["Date"].Value).Date;
                if (rowDate < startDate || rowDate > endDate) continue;

                if (row.Cells["AddDisel"].Value != null && row.Cells["AddDisel"].Value != DBNull.Value &&
                    row.Cells["Rate"].Value != null && row.Cells["Rate"].Value != DBNull.Value)
                {
                    decimal disel = Convert.ToDecimal(row.Cells["AddDisel"].Value);
                    decimal rate = Convert.ToDecimal(row.Cells["Rate"].Value);
                    totalAmountSum += disel * rate;
                    totalDiselSum += disel;
                }
            }

            lblTotalDisel.Text = $"Total Disel: {totalDiselSum:F2} L";
            lblTotalAmount.Text = $"Total Amount: {totalAmountSum:F2} Rs";

            if (totalDiselSum > 0)
            {
                decimal average = totalAmountSum / totalDiselSum;
                lblResult.Text = $"Sum Amount: {totalAmountSum:F2}, Sum AddDisel: {totalDiselSum:F2}, Average: {average:F2}";
            }
            else
                lblResult.Text = "Total AddDisel is zero, cannot calculate average.";
        }

        private void DatePickers_ValueChanged(object sender, EventArgs e)
        {
            LoadData1();
        }
    }
}
