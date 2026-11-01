using AlMadinaShahzad.ProjectConnection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AlMadinaShahzad.View
{
    public partial class frmDashBoard : Sample
    {
        private Series lastClickedSeries = null; // Last clicked column ko track karne ke liye
        private int lastClickedIndex = -1; // Last clicked index
        private bool isProcessingClick = false; // Double click ko rokne ke liye

        public frmDashBoard()
        {
            InitializeComponent();
            LoadYearsInComboBox();
            GetDataForSelectedYear();
            SubscribeMouseDownEvent(); // Event ko subscribe karo
            supplierChart.MouseWheel += new MouseEventHandler(SupplierChart_MouseWheel); // Zoom ke liye mouse wheel
        }

        private void SubscribeMouseDownEvent()
        {
            supplierChart.MouseDown -= SupplierChart_MouseClick; // Pehle unsubscribe karo
            supplierChart.MouseDown += new MouseEventHandler(SupplierChart_MouseClick); // Phir subscribe
        }

        private void frmDashBoard_Load(object sender, EventArgs e)
        {
            LoadChartForSelectedYear();
            SetupStockBoard();
        }

        private void LoadYearsInComboBox()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = @"
            SELECT DISTINCT strftime('%Y', Date) AS Year
            FROM PetrolAdd
            WHERE Date IS NOT NULL AND Date != ''
            UNION
            SELECT DISTINCT strftime('%Y', Date) AS Year
            FROM AddStock
            WHERE Date IS NOT NULL AND Date != ''
            UNION
            SELECT DISTINCT strftime('%Y', Date) AS Year
            FROM AddDealer
            WHERE Date IS NOT NULL AND Date != ''
            UNION
            SELECT DISTINCT strftime('%Y', EDate) AS Year
            FROM Expensetable
            WHERE EDate IS NOT NULL AND EDate != ''
            ORDER BY Year DESC";

                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    try
                    {
                        con.Open();
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        comboYear.Items.Clear();
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row["Year"] != DBNull.Value && !string.IsNullOrEmpty(row["Year"].ToString()))
                            {
                                comboYear.Items.Add(row["Year"].ToString());
                            }
                        }

                        int currentYear = DateTime.Now.Year;
                        if (comboYear.Items.Contains(currentYear.ToString()))
                        {
                            comboYear.SelectedItem = currentYear.ToString();
                        }
                        else if (comboYear.Items.Count > 0)
                        {
                            comboYear.SelectedIndex = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading years: " + ex.Message);
                    }
                }
            }
        }

        private void comboYear_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboYear.SelectedItem != null)
            {
                GetDataForSelectedYear();
                LoadChartForSelectedYear();
            }
        }

        private void GetDataForSelectedYear()
        {
            string selectedYear = comboYear.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString();
            GetDebit(selectedYear);
            GetCredit(selectedYear);
            GetBalance1(selectedYear);
            CalculateNetBalance(selectedYear);
            GetTotalDieselAdded(selectedYear);
            GetTotalPetrolAdded(selectedYear);
            GetAdvance(selectedYear);
            GetLitter(selectedYear);
            GetAmount(selectedYear);
            GetDealerCredit(selectedYear);
            GetDealerAmount(selectedYear);
            GetDealerBalance(selectedYear);
            GetTotalExpenses(selectedYear); // Naya method call kiya
        }

        // Naya method total expenses ke liye
        private void GetTotalExpenses(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(Amount AS DECIMAL(18, 2))), 0) FROM Expensetable WHERE strftime('%Y', EDate) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal totalExpenses = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        lblExpenseTotal.Text = "Rs " + totalExpenses.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error fetching total expenses: " + ex.Message);
                        lblExpenseTotal.Text = "Rs 0.00"; // Error case mein default value
                    }
                }
            }
        }

        private void LoadChartForSelectedYear()
        {
            string selectedYear = comboYear.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString();
            var supplierMonthMapping = new Dictionary<int, string>
            {
                { 1, "January" }, { 2, "February" }, { 3, "March" },
                { 4, "April" }, { 5, "May" }, { 6, "June" },
                { 7, "July" }, { 8, "August" }, { 9, "September" },
                { 10, "October" }, { 11, "November" }, { 12, "December" }
            };

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = @"
            SELECT 
                CAST(strftime('%m', Date) AS INTEGER) AS MonthNumber, 
                IFNULL(SUM(Litter), 0) AS TotalLiters,
                IFNULL(SUM(Rate), 0) AS TotalRate
            FROM PetrolAdd
            WHERE strftime('%Y', Date) = @Year
            GROUP BY strftime('%m', Date)";

                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", selectedYear);
                    try
                    {
                        con.Open();
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        supplierChart.Series.Clear();
                        supplierChart.Legends.Clear();
                        supplierChart.ChartAreas[0].AxisX.CustomLabels.Clear();

                        Legend legend = new Legend("Legend")
                        {
                            Docking = Docking.Right,
                            Alignment = StringAlignment.Center,
                            Font = new Font("Arial", 12f, FontStyle.Bold),
                            ForeColor = Color.White,
                            BackColor = Color.FromArgb(32, 36, 61),
                            Title = "Data Type",
                            TitleFont = new Font("Arial", 14f, FontStyle.Bold),
                            TitleForeColor = Color.White
                        };
                        supplierChart.Legends.Add(legend);

                        Series litterSeries = new Series("Litter")
                        {
                            ChartType = SeriesChartType.Column,
                            XValueType = ChartValueType.String,
                            IsValueShownAsLabel = true,
                            LabelForeColor = Color.White,
                            Color = Color.FromArgb(100, 149, 237),
                            Legend = "Legend",
                            LegendText = "Liters (L)"
                        };

                        Series rateSeries = new Series("Rate")
                        {
                            ChartType = SeriesChartType.Column,
                            XValueType = ChartValueType.String,
                            IsValueShownAsLabel = true,
                            LabelForeColor = Color.White,
                            Color = Color.FromArgb(255, 99, 71),
                            Legend = "Legend",
                            LegendText = "Rate (Rs)"
                        };

                        supplierChart.Series.Add(litterSeries);
                        supplierChart.Series.Add(rateSeries);

                        foreach (DataRow row in dt.Rows)
                        {
                            int monthNumber = Convert.ToInt32(row["MonthNumber"]);
                            string monthName = supplierMonthMapping.ContainsKey(monthNumber)
                                ? supplierMonthMapping[monthNumber]
                                : "Unknown";

                            double totalLiters = Convert.ToDouble(row["TotalLiters"]);
                            double totalRate = Convert.ToDouble(row["TotalRate"]);

                            litterSeries.Points.AddXY(monthName, totalLiters);
                            rateSeries.Points.AddXY(monthName, totalRate);
                        }

                        supplierChart.ChartAreas[0].AxisX.Title = "Months";
                        supplierChart.ChartAreas[0].AxisY.Title = "Values";
                        supplierChart.ChartAreas[0].AxisX.Interval = 1;
                        supplierChart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
                        supplierChart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
                        supplierChart.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
                        supplierChart.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
                        supplierChart.ChartAreas[0].AxisX.TitleForeColor = Color.White;
                        supplierChart.ChartAreas[0].AxisY.TitleForeColor = Color.White;
                        supplierChart.BackColor = Color.FromArgb(32, 36, 61);
                        supplierChart.ChartAreas[0].BackColor = Color.FromArgb(32, 36, 61);

                        litterSeries["PointWidth"] = "0.4";
                        rateSeries["PointWidth"] = "0.4";
                        litterSeries["BarLabelAlignment"] = "Center";
                        rateSeries["BarLabelAlignment"] = "Center";

                        // Zoom enable karo
                        supplierChart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                        supplierChart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
                        supplierChart.ChartAreas[0].CursorX.IsUserEnabled = true;
                        supplierChart.ChartAreas[0].CursorY.IsUserEnabled = true;
                        supplierChart.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
                        supplierChart.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;

                        if (litterSeries.Points.Count == 0 && rateSeries.Points.Count == 0)
                        {
                            MessageBox.Show($"Koi data nahi mila {selectedYear} ke liye chart banane ke liye.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        supplierChart.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Chart load karte waqt error: " + ex.Message);
                    }
                }
            }
        }

        private void SupplierChart_MouseClick(object sender, MouseEventArgs e)
        {
            if (isProcessingClick) return; // Double click rokne ke liye
            isProcessingClick = true;

            HitTestResult result = supplierChart.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.DataPoint && result.Series != null)
            {
                DataPoint clickedPoint = result.Series.Points[result.PointIndex];
                string month = clickedPoint.AxisLabel;
                int monthIndex = result.PointIndex;

                // Litter aur Rate dono values nikalo
                double litterValue = supplierChart.Series["Litter"].Points[monthIndex].YValues[0];
                double rateValue = supplierChart.Series["Rate"].Points[monthIndex].YValues[0];
                double multiplication = litterValue * rateValue;

                // Pehle purane clicked column ko normal karo
                if (lastClickedSeries != null && lastClickedIndex != -1)
                {
                    lastClickedSeries.Points[lastClickedIndex].Color = lastClickedSeries.Name == "Litter" ? Color.FromArgb(100, 149, 237) : Color.FromArgb(255, 99, 71);
                    lastClickedSeries["PointWidth"] = "0.4";
                }

                // Naya clicked column ko highlight aur bara karo
                Color originalColor = result.Series.Name == "Litter" ? Color.FromArgb(100, 149, 237) : Color.FromArgb(255, 99, 71);
                result.Series.Points[result.PointIndex].Color = Color.Gold;
                result.Series["PointWidth"] = "0.6";

                // Animation effect ke liye bounce
                for (int i = 0; i < 3; i++)
                {
                    System.Threading.Tasks.Task.Delay(100 * i).ContinueWith(t =>
                    {
                        if (supplierChart.InvokeRequired)
                        {
                            supplierChart.Invoke(new Action(() =>
                            {
                                result.Series["PointWidth"] = i % 2 == 0 ? "0.65" : "0.55";
                                supplierChart.Invalidate();
                            }));
                        }
                    });
                }

                // Details show karo with Litter * Rate
                string details = $"Month: {month}\nLitter: {litterValue:N2} L\nRate: {rateValue:N2} Rs\nLitter * Rate: {multiplication:N2}";
                MessageBox.Show(details, "Column Details", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Last clicked ko save karo
                lastClickedSeries = result.Series;
                lastClickedIndex = result.PointIndex;

                supplierChart.Invalidate();
            }

            isProcessingClick = false; // Click processing khatam
            SubscribeMouseDownEvent(); // Event ko refresh karo taake repeat na ho
        }

        // Zoom ke liye MouseWheel event
        private void SupplierChart_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Delta > 0) // Zoom in
                {
                    double xMin = supplierChart.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    double xMax = supplierChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                    double yMin = supplierChart.ChartAreas[0].AxisY.ScaleView.ViewMinimum;
                    double yMax = supplierChart.ChartAreas[0].AxisY.ScaleView.ViewMaximum;

                    double posX = supplierChart.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
                    double posY = supplierChart.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);

                    double xRange = (xMax - xMin) / 2;
                    double yRange = (yMax - yMin) / 2;

                    supplierChart.ChartAreas[0].AxisX.ScaleView.Zoom(posX - xRange / 2, posX + xRange / 2);
                    supplierChart.ChartAreas[0].AxisY.ScaleView.Zoom(posY - yRange / 2, posY + yRange / 2);
                }
                else if (e.Delta < 0) // Zoom out
                {
                    supplierChart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                    supplierChart.ChartAreas[0].AxisY.ScaleView.ZoomReset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Zoom karte waqt error: " + ex.Message);
            }
        }

        // Baqi methods same hain, sirf int → decimal fix kiya
        private void GetDebit(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(Balance), 0) AS TotalBalance FROM PetrolAdd WHERE IsInitialEntry = 1 AND Balance <> 0 AND strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal Inc = result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
                        txtdebit.Text = "Rs " + Inc.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetAdvance(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(Advance AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal advanceSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        txtadvance.Text = "Rs " + advanceSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetAmount(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(Amount AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal amountSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        txtAmount.Text = "Rs " + amountSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetLitter(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(Litter AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal litterSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        // yahan "L" likhna logically sahi hai, lekin tumne "Rs" rakha hua tha, maine waise hi rehne diya:
                        txtlitter.Text = "Rs " + litterSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetCredit(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(Credit AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal creditSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        txtcredit.Text = "Rs " + creditSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetTotalDieselAdded(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(AddDisel), 0) FROM AddStock WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal totalDiesel = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        lblTotalDiesel.Text = totalDiesel.ToString("N2") + " L";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error fetching diesel total: " + ex.Message);
                    }
                }
            }
        }

        private void GetTotalPetrolAdded(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal totalPetrol = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        lblTotalPetrol.Text = totalPetrol.ToString("N2") + " L";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error fetching petrol total: " + ex.Message);
                    }
                }
            }
        }

        private void CalculateNetBalance(string year)
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                try
                {
                    con.Open();

                    string expensesQuery = "SELECT IFNULL(SUM(CAST(Amount AS DECIMAL(18, 2))), 0) FROM Expensetable WHERE strftime('%Y', EDate) = @Year";
                    SQLiteCommand expensesCmd = new SQLiteCommand(expensesQuery, con);
                    expensesCmd.Parameters.AddWithValue("@Year", year);
                    object expResult = expensesCmd.ExecuteScalar();
                    decimal totalExpenses = expResult != DBNull.Value ? Convert.ToDecimal(expResult) : 0m;

                    string creditQuery = "SELECT IFNULL(SUM(CAST(Amount AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                    SQLiteCommand creditCmd = new SQLiteCommand(creditQuery, con);
                    creditCmd.Parameters.AddWithValue("@Year", year);
                    object creditResult = creditCmd.ExecuteScalar();
                    decimal totalCredit = creditResult != DBNull.Value ? Convert.ToDecimal(creditResult) : 0m;

                    decimal netBalance = totalCredit - totalExpenses;
                    BalanceLbl1.Text = "Rs " + netBalance.ToString("N2");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
                finally
                {
                    con.Close();
                }
            }
        }

        private void GetBalance1(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                try
                {
                    con.Open();

                    string debitQuery = "SELECT IFNULL(SUM(Balance), 0) AS TotalBalance FROM PetrolAdd WHERE IsInitialEntry = 1 AND Balance <> 0 AND strftime('%Y', Date) = @Year";
                    SQLiteCommand debitCmd = new SQLiteCommand(debitQuery, con);
                    debitCmd.Parameters.AddWithValue("@Year", year);
                    object debitResult = debitCmd.ExecuteScalar();
                    decimal debitAmount = debitResult != DBNull.Value ? Convert.ToDecimal(debitResult) : 0;

                    string creditQuery = "SELECT IFNULL(SUM(CAST(Credit AS DECIMAL(18, 2))), 0) FROM PetrolAdd WHERE strftime('%Y', Date) = @Year";
                    SQLiteCommand creditCmd = new SQLiteCommand(creditQuery, con);
                    creditCmd.Parameters.AddWithValue("@Year", year);
                    object creditResult = creditCmd.ExecuteScalar();
                    decimal creditAmount = creditResult != DBNull.Value ? Convert.ToDecimal(creditResult) : 0;

                    decimal balance = debitAmount - creditAmount;
                    txtbalance.Text = "Rs " + balance.ToString("N2");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void GetDealerAmount(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(DDAmount AS DECIMAL(18, 2))), 0) FROM AddDealer WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal advanceSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        txtdealeramount.Text = "Rs " + advanceSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetDealerCredit(string year)
        {
            string connectionString = projectconnection.conReturn();
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                string query = "SELECT IFNULL(SUM(CAST(DAmount AS DECIMAL(18, 2))), 0) FROM AddDealer WHERE strftime('%Y', Date) = @Year";
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        decimal amountSum = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        txtdealerCredit.Text = "Rs " + amountSum.ToString("N2");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void GetDealerBalance(string year)
        {
            GetDealerAmount(year);
            GetDealerCredit(year);

            decimal dealerAmount = decimal.TryParse(txtdealeramount.Text.Replace("Rs ", "").Replace(",", ""), out decimal amount) ? amount : 0;
            decimal dealerCredit = decimal.TryParse(txtdealerCredit.Text.Replace("Rs ", "").Replace(",", ""), out decimal credit) ? credit : 0;

            decimal dealerBalance = dealerAmount - dealerCredit;
            txtDealerBalance.Text = "Rs " + dealerBalance.ToString("N2");
        }

        private void comboYear_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboYear.SelectedItem != null)
            {
                GetDataForSelectedYear();
                LoadChartForSelectedYear();
            }
        }
    }
}
