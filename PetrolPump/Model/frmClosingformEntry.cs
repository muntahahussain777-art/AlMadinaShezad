using System;
using System.Data;
using System.Windows.Forms;
using System.Collections;              // Hashtable ke liye
using System.Diagnostics;              // Process.Start ke liye
using AlMadinaShahzad;            // MainClass ke liye
using AlMadinaShahzad.Services;
using System.IO;
using System.Globalization;

namespace AlMadinaShahzad.Model
{
    public partial class frmClosingformEntry : Sample
    {
        // ================== FIELDS (Summary Data Store) ==================
        private DataTable _customerCredits;
        private DataTable _dealerList;
        private decimal _faidaAmount;   // Date-range faida (btnLoad se set hoga)

        public frmClosingformEntry()
        {
            InitializeComponent();

            // Grid events
            guna2DataGridView1.CellContentClick += guna2DataGridView1_CellContentClick;

            // Clear button
            btnClear.Click += btnClear_Click;

            // 🔑 Ctrl + R shortcut ke liye
            this.KeyPreview = true;  // Form sab se pehle keys pakdega
            this.KeyDown += frmClosingformEntry_KeyDown;
        }

        #region Form Events

        private void frmClosingformEntry_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Ctrl + R dabaya gaya?
                if (e.Control && e.KeyCode == Keys.R)
                {
                    e.SuppressKeyPress = true;   // default behavior ko roko
                    btnClear.PerformClick();     // 🔁 Clear button ka action chalao
                }
            }
            catch (Exception ex)
            {
                ShowError("Ctrl + R shortcut par error aaya.", ex);
            }
        }

        private void frmClosingformEntry_Load(object sender, EventArgs e)
        {
            try
            {
                OfficeHelper.AttachOfficeFilter(this, (s, ev) =>
                {
                    try { LoadSummary(); }
                    catch { }
                }, left: 520, top: 8);
                dtpStart.Value = DateTime.Today;
                dtpEnd.Value = DateTime.Today;

                // 👉 Form load par sirf summary (grid + totals)
                LoadSummary();
            }
            catch (Exception ex)
            {
                ShowError("Form load par error aaya.", ex);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            // 👉 Sirf averages 4 label ke liye (date range se)
            LoadAveragesByDateRange();
        }

        #endregion

        #region Date Range Averages (sirf 4 labels)

        // ======================= DATE RANGE AVERAGE ONLY =======================
        private void LoadAveragesByDateRange()
        {
            try
            {
                // 👉 Sirf average / faida / liter walay labels reset karein
                if (lblCustomerAverage != null) lblCustomerAverage.Text = "0.000";
                if (lblDealerAverage != null) lblDealerAverage.Text = "0.000";
                if (lblAvgBalance != null) lblAvgBalance.Text = "0.000";
                if (lblMultiplyAverge != null) lblMultiplyAverge.Text = "0.00";
                if (lblLiter != null) lblLiter.Text = "0.000";

                // Date range strings
                string startDate = dtpStart.Value.Date.ToString("yyyy-MM-dd");
                string endDate = dtpEnd.Value.Date.ToString("yyyy-MM-dd");

                // ✅ agar kisi ne galti se start > end select kar diya
                if (dtpStart.Value.Date > dtpEnd.Value.Date)
                {
                    MessageBox.Show("Start date end date se aage nahi ho sakti.", "Invalid Date Range",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ========== 1) SALE SIDE (CUSTOMER) — BILKUL frmDiselView JAISE ==========

                decimal totalCustomerLitter = 0m;
                decimal totalCustomerAmount = 0m;

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
                IFNULL(
                    SUM(
                        IFNULL(Litter, 0)
                    ),
                    0
                ) AS TotalCustomerLitter
            FROM PetrolAdd
            WHERE Date >= @StartDate 
              AND Date <= @EndDate
              AND IFNULL(IsInitialEntry, 0) = 1" + OfficeHelper.OfficeSqlFilter("OfficeName", OfficeHelper.GetSelectedOffice(this, true)) + @";
        ";

                Hashtable htSale = new Hashtable
                {
                    { "@StartDate", startDate },
                    { "@EndDate",   endDate   }
                };

                DataTable dtSale = MainClass.ExecuteSelectQuery(qSaleRange, htSale);
                if (dtSale != null && dtSale.Rows.Count > 0)
                {
                    totalCustomerAmount = SafeToDecimal(dtSale.Rows[0]["TotalCustomerAmount"]);
                    totalCustomerLitter = SafeToDecimal(dtSale.Rows[0]["TotalCustomerLitter"]);
                }

                // ========== 2) PURCHASE SIDE (DEALER) — AddStock se (frmStockView jaisa) ==========

                decimal totalLitterPurchase = 0m;   // SUM(AddDisel)
                decimal totalValuePurchase = 0m;    // SUM(AddDisel * Rate)

                string qPurchaseRange = @"
    SELECT
        IFNULL(SUM(AddDisel), 0)                AS TotalLitterPurchase,
        IFNULL(SUM(AddDisel * Rate), 0)        AS TotalValuePurchase
    FROM AddStock
    WHERE Date >= @StartDate AND Date <= @EndDate" + OfficeHelper.OfficeSqlFilter("OfficeName", OfficeHelper.GetSelectedOffice(this, true)) + @";
";

                Hashtable htPurchase = new Hashtable
{
    { "@StartDate", startDate },
    { "@EndDate",   endDate   }
};

                DataTable dtPurchase = MainClass.ExecuteSelectQuery(qPurchaseRange, htPurchase);
                if (dtPurchase != null && dtPurchase.Rows.Count > 0)
                {
                    totalLitterPurchase = SafeToDecimal(dtPurchase.Rows[0]["TotalLitterPurchase"]);
                    totalValuePurchase = SafeToDecimal(dtPurchase.Rows[0]["TotalValuePurchase"]);
                }


                // ========== 3) AVERAGE CALCULATION ==========

                decimal customerAvgRate = 0m;   // Customer Avg (Rs per Litter)
                decimal dealerAvgRate = 0m;     // Dealer Avg (Rs per Litter)
                decimal rateDiff = 0m;          // CustomerAvg - DealerAvg
                decimal faidaAmount = 0m;       // rateDiff * totalCustomerLitter

                // Customer avg : EXACT frmDiselView → SUM(Amount) / SUM(Litter)
                if (totalCustomerLitter > 0)
                    customerAvgRate = totalCustomerAmount / totalCustomerLitter;

                // Dealer avg : SUM(Litter*Rate) / SUM(Litter)
                if (totalLitterPurchase > 0)
                    dealerAvgRate = totalValuePurchase / totalLitterPurchase;

                rateDiff = customerAvgRate - dealerAvgRate;
                faidaAmount = rateDiff * totalCustomerLitter;

                // ========== 4) LABELS FILL ==========

                if (lblCustomerAverage != null)
                    lblCustomerAverage.Text = customerAvgRate.ToString("N3");

                if (lblDealerAverage != null)
                    lblDealerAverage.Text = dealerAvgRate.ToString("N3");

                if (lblAvgBalance != null)
                    lblAvgBalance.Text = rateDiff.ToString("N3");

                // yahi Litter jisse multiply kar rahe hain
                if (lblLiter != null)
                    lblLiter.Text = totalCustomerLitter.ToString("N3");

                if (lblMultiplyAverge != null)
                    lblMultiplyAverge.Text = faidaAmount.ToString("N2");

                _faidaAmount = faidaAmount;   // Excel export ke liye
            }
            catch (Exception ex)
            {
                ShowError("Date range average nikalte waqt error aaya.", ex);
            }
        }

        #endregion

        #region Summary (Grid + Totals only)

        // ======================= SUMMARY LOAD =======================
        private void LoadSummary()
        {
            try
            {
                // Labels ko pehle reset kar do (totals)
                if (lblCustomerAmount != null) lblCustomerAmount.Text = "0.00";
                if (lblDealerTotal != null) lblDealerTotal.Text = "0.00";
                if (lblBalance != null) lblBalance.Text = "0.00";

                // 👉 Average walay labels ko yahan touch NAHI kar rahe
                // (sirf btnLoad se change honge)

                // ================= 1) CUSTOMER-WISE RECEIVABLE LIST =================
                string qCustomerCredits = @"
                    SELECT 
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
                    WHERE 1=1" + OfficeHelper.OfficeSqlFilter("p.OfficeName", OfficeHelper.GetSelectedOffice(this, true)) + @"
                    GROUP BY c.id, c.Name
                    HAVING CustomerReceivable <> 0
                    ORDER BY c.Name;
                ";

                DataTable dtCustomerCredits = SafeGetData(qCustomerCredits);
                _customerCredits = dtCustomerCredits;     // 🔹 Excel/export ke liye store

                decimal totalReceivable = 0m;
                if (dtCustomerCredits != null)
                {
                    foreach (DataRow dr in dtCustomerCredits.Rows)
                    {
                        totalReceivable += SafeToDecimal(dr["CustomerReceivable"]);
                    }
                }

                // Total customer remaining amount label me
                if (lblCustomerAmount != null)
                    lblCustomerAmount.Text = totalReceivable.ToString("N2");

                // ================= 2) DEALER-WISE PAYABLE LIST =================
                string qDealerList = @"
                    SELECT 
                        d.DealerName,
                        IFNULL(
                            SUM(
                                IFNULL(d.DDAmount, 0) 
                                - IFNULL(d.DAmount, 0)
                            ),
                            0
                        ) AS DealerAmount
                    FROM AddDealer d
                    WHERE 1=1" + OfficeHelper.OfficeSqlFilter("d.OfficeName", OfficeHelper.GetSelectedOffice(this, true)) + @"
                    GROUP BY d.Did, d.DealerName
                    HAVING DealerAmount <> 0
                    ORDER BY d.DealerName;
                ";

                DataTable dtDealerList = SafeGetData(qDealerList);
                _dealerList = dtDealerList;               // 🔹 Excel/export ke liye store

                decimal totalPayable = 0m;
                if (dtDealerList != null)
                {
                    foreach (DataRow dr in dtDealerList.Rows)
                    {
                        totalPayable += SafeToDecimal(dr["DealerAmount"]);
                    }
                }

                // Total dealer remaining amount label me
                if (lblDealerTotal != null)
                    lblDealerTotal.Text = totalPayable.ToString("N2");

                // ===== NET BALANCE (Customer - Dealer) =====
                decimal netBalance = totalReceivable - totalPayable;
                if (lblBalance != null)
                    lblBalance.Text = netBalance.ToString("N2");

                // ================= 3) DATAGRIDVIEW FILL (2 COLUMNS + optional Faida) =================
                guna2DataGridView1.DataSource = null;
                guna2DataGridView1.Rows.Clear();
                guna2DataGridView1.AutoGenerateColumns = false;
                guna2DataGridView1.AllowUserToAddRows = false;
                guna2DataGridView1.ReadOnly = true;

                int customerCount = dtCustomerCredits?.Rows.Count ?? 0;
                int dealerCount = dtDealerList?.Rows.Count ?? 0;
                int maxRows = Math.Max(customerCount, dealerCount);

                for (int i = 0; i < maxRows; i++)
                {
                    int rowIndex = guna2DataGridView1.Rows.Add();
                    DataGridViewRow row = guna2DataGridView1.Rows[rowIndex];

                    // ----- Left side: Receivable (Customer) -----
                    string receivableText = "";
                    if (i < customerCount && dtCustomerCredits != null)
                    {
                        string custName = Convert.ToString(dtCustomerCredits.Rows[i]["CustomerName"]);
                        decimal custReceive = SafeToDecimal(dtCustomerCredits.Rows[i]["CustomerReceivable"]);
                        receivableText = $"{custName} - {custReceive:N2}";
                    }
                    SafeSetCell(row, "dgvRecived", receivableText);

                    // ----- Right side: Payable (Dealer) -----
                    string payableText = "";
                    if (i < dealerCount && dtDealerList != null)
                    {
                        string dealerName = Convert.ToString(dtDealerList.Rows[i]["DealerName"]);
                        decimal dealerAmt = SafeToDecimal(dtDealerList.Rows[i]["DealerAmount"]);
                        payableText = $"{dealerName} - {dealerAmt:N2}";
                    }
                    SafeSetCell(row, "dgvPayable", payableText);

                    // Faida column ko yahan blank rakhte hain
                    SafeSetCell(row, "dgvFaida", "");
                }

                // ---- Agar chaho to sirf first row me _faidaAmount show kar sakte ho ----
                if (guna2DataGridView1.Rows.Count > 0)
                {
                    SafeSetCell(guna2DataGridView1.Rows[0], "dgvFaida", _faidaAmount.ToString("N2"));
                }
            }
            catch (Exception ex)
            {
                ShowError("Summary load karte waqt error aaya.", ex);
            }
        }

        #endregion

        #region EXCEL/CSV EXPORT

        // ======================= EXCEL/CSV EXPORT =======================
        private void ExportToExcelCsv()
        {
            try
            {
                // agar summary abhi tak load nahi hui to load kar lo
                if (_customerCredits == null && _dealerList == null)
                {
                    LoadSummary();
                }

                if ((_customerCredits == null || _customerCredits.Rows.Count == 0) &&
                    (_dealerList == null || _dealerList.Rows.Count == 0))
                {
                    MessageBox.Show("Excel ke liye koi data nahi mila.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel File (*.xls)|*.xls";
                    sfd.Title = "Excel file save karein";
                    sfd.FileName = "ClosingSummary.xls";

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    using (var writer = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("<html>");
                        writer.WriteLine("<head>");
                        writer.WriteLine("<meta http-equiv='Content-Type' content='text/html; charset=utf-8' />");
                        writer.WriteLine("<style>");
                        writer.WriteLine("table { border-collapse: collapse; }");
                        writer.WriteLine("th, td { border: 1px solid #000000; padding: 3px; font-family: Arial; font-size: 10pt; }");
                        writer.WriteLine("th { background-color: #D9D9D9; font-weight: bold; text-align: center; }");
                        writer.WriteLine(".title { font-weight:bold; font-size:12pt; }");
                        writer.WriteLine("</style>");
                        writer.WriteLine("</head>");
                        writer.WriteLine("<body>");

                        // MAIN TABLE
                        writer.WriteLine("<table>");

                        writer.WriteLine("<tr>");
                        writer.WriteLine("<th>Customer Name</th>");
                        writer.WriteLine("<th>Receivable</th>");
                        writer.WriteLine("<th></th>");
                        writer.WriteLine("<th>Dealer Name</th>");
                        writer.WriteLine("<th>Payable</th>");
                        writer.WriteLine("<th></th>");
                        writer.WriteLine("<th>Faida (System)</th>");
                        writer.WriteLine("</tr>");

                        int customerCount = _customerCredits?.Rows.Count ?? 0;
                        int dealerCount = _dealerList?.Rows.Count ?? 0;
                        int maxRows = Math.Max(customerCount, dealerCount);

                        decimal totalReceivable = 0m;
                        decimal totalPayable = 0m;

                        for (int i = 0; i < maxRows; i++)
                        {
                            string custName = "";
                            decimal custAmount = 0m;
                            string dealerName = "";
                            decimal dealerAmount = 0m;

                            if (i < customerCount && _customerCredits != null)
                            {
                                custName = Convert.ToString(_customerCredits.Rows[i]["CustomerName"]);
                                custAmount = SafeToDecimal(_customerCredits.Rows[i]["CustomerReceivable"]);
                                totalReceivable += custAmount;
                            }

                            if (i < dealerCount && _dealerList != null)
                            {
                                dealerName = Convert.ToString(_dealerList.Rows[i]["DealerName"]);
                                dealerAmount = SafeToDecimal(_dealerList.Rows[i]["DealerAmount"]);
                                totalPayable += dealerAmount;
                            }

                            writer.WriteLine("<tr>");

                            writer.WriteLine($"<td>{System.Security.SecurityElement.Escape(custName)}</td>");
                            writer.WriteLine($"<td style='mso-number-format:\"#,##0.00\";'>{custAmount}</td>");

                            writer.WriteLine("<td></td>");

                            writer.WriteLine($"<td>{System.Security.SecurityElement.Escape(dealerName)}</td>");
                            writer.WriteLine($"<td style='mso-number-format:\"#,##0.00\";'>{dealerAmount}</td>");

                            writer.WriteLine("<td></td>");

                            writer.WriteLine("<td></td>");

                            writer.WriteLine("</tr>");
                        }

                        writer.WriteLine("<tr style='font-weight:bold; background-color:#F2F2F2;'>");
                        writer.WriteLine("<td>TOTAL RECEIVABLE</td>");
                        writer.WriteLine($"<td style='mso-number-format:\"#,##0.00\";'>{totalReceivable}</td>");
                        writer.WriteLine("<td></td>");
                        writer.WriteLine("<td>TOTAL PAYABLE</td>");
                        writer.WriteLine($"<td style='mso-number-format:\"#,##0.00\";'>{totalPayable}</td>");
                        writer.WriteLine("<td></td>");
                        writer.WriteLine($"<td style='mso-number-format:\"#,##0.00\";'>{_faidaAmount}</td>");
                        writer.WriteLine("</tr>");

                        writer.WriteLine("</table>");

                        writer.WriteLine("<br/><span class='title'>Summary Values</span><br/><br/>");

                        string customerTotalText = lblCustomerAmount?.Text ?? "0.00";
                        string dealerTotalText = lblDealerTotal?.Text ?? "0.00";
                        string balanceText = lblBalance?.Text ?? "0.00";
                        string custAvgText = lblCustomerAverage?.Text ?? "0.000";
                        string dealerAvgText = lblDealerAverage?.Text ?? "0.000";
                        string avgDiffText = lblAvgBalance?.Text ?? "0.000";
                        string literText = lblLiter?.Text ?? "0.000";
                        string faidaAvgMultiplyTxt = lblMultiplyAverge?.Text ?? "0.00";

                        writer.WriteLine("<table>");

                        writer.WriteLine("<tr><td>Total Customer</td>" +
                                         $"<td style='mso-number-format:\"#,##0.00\";'>{customerTotalText}</td></tr>");

                        writer.WriteLine("<tr><td>Total Dealer</td>" +
                                         $"<td style='mso-number-format:\"#,##0.00\";'>{dealerTotalText}</td></tr>");

                        writer.WriteLine("<tr><td>Net Balance (Customer - Dealer)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.00\";'>{balanceText}</td></tr>");

                        writer.WriteLine("<tr><td colspan='2'></td></tr>");

                        writer.WriteLine("<tr>" +
                                         "<td>Customer Average (Rs/L)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.000\";'>{custAvgText}</td>" +
                                         "</tr>");

                        writer.WriteLine("<tr>" +
                                         "<td>Dealer Average (Rs/L)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.000\";'>{dealerAvgText}</td>" +
                                         "</tr>");

                        writer.WriteLine("<tr>" +
                                         "<td>Average Difference (Cust - Dealer)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.000\";'>{avgDiffText}</td>" +
                                         "</tr>");

                        writer.WriteLine("<tr>" +
                                         "<td>Total Litter (Avg Range)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.000\";'>{literText}</td>" +
                                         "</tr>");

                        writer.WriteLine("<tr>" +
                                         "<td>Faida (Avg Difference * Litter)</td>" +
                                         $"<td style='mso-number-format:\"#,##0.00\";'>{faidaAvgMultiplyTxt}</td>" +
                                         "</tr>");

                        writer.WriteLine("</table>");

                        writer.WriteLine("</body>");
                        writer.WriteLine("</html>");
                    }

                    MessageBox.Show("Excel file successfully create ho gayi.", "Success",
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

        #endregion

        #region Manual Entry

        private void SaveManualEntry()
        {
            try
            {
                string name = (txtName.Text ?? "").Trim();
                string mode = cbMode.SelectedItem == null ? "" : cbMode.SelectedItem.ToString();
                string amountText = (txtAmount.Text ?? "").Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Name enter karo.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(mode))
                {
                    MessageBox.Show("Mode select karo (Customer / Dealer).", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbMode.Focus();
                    return;
                }

                if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Sahi amount enter karo.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAmount.Focus();
                    return;
                }

                // ================== AB SIRF MEMORY ME KAAM HOGA ==================

                if (mode.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    // DataTable ensure karo
                    if (_customerCredits == null)
                    {
                        _customerCredits = new DataTable();
                        _customerCredits.Columns.Add("CustomerName", typeof(string));
                        _customerCredits.Columns.Add("CustomerReceivable", typeof(decimal));
                    }

                    // Existing customer dhoondo (name se)
                    DataRow existingRow = null;
                    foreach (DataRow dr in _customerCredits.Rows)
                    {
                        if (string.Equals(Convert.ToString(dr["CustomerName"]), name, StringComparison.OrdinalIgnoreCase))
                        {
                            existingRow = dr;
                            break;
                        }
                    }

                    if (existingRow == null)
                    {
                        // naya row
                        DataRow newRow = _customerCredits.NewRow();
                        newRow["CustomerName"] = name;
                        newRow["CustomerReceivable"] = amount;
                        _customerCredits.Rows.Add(newRow);
                    }
                    else
                    {
                        // purane amount me add
                        decimal oldVal = SafeToDecimal(existingRow["CustomerReceivable"]);
                        existingRow["CustomerReceivable"] = oldVal + amount;
                    }
                }
                else if (mode.Equals("Dealer", StringComparison.OrdinalIgnoreCase))
                {
                    if (_dealerList == null)
                    {
                        _dealerList = new DataTable();
                        _dealerList.Columns.Add("DealerName", typeof(string));
                        _dealerList.Columns.Add("DealerAmount", typeof(decimal));
                    }

                    DataRow existingRow = null;
                    foreach (DataRow dr in _dealerList.Rows)
                    {
                        if (string.Equals(Convert.ToString(dr["DealerName"]), name, StringComparison.OrdinalIgnoreCase))
                        {
                            existingRow = dr;
                            break;
                        }
                    }

                    if (existingRow == null)
                    {
                        DataRow newRow = _dealerList.NewRow();
                        newRow["DealerName"] = name;
                        newRow["DealerAmount"] = amount;
                        _dealerList.Rows.Add(newRow);
                    }
                    else
                    {
                        decimal oldVal = SafeToDecimal(existingRow["DealerAmount"]);
                        existingRow["DealerAmount"] = oldVal + amount;
                    }
                }
                else
                {
                    MessageBox.Show("Mode sirf 'Customer' ya 'Dealer' hona chahiye.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Ab memory tables se grid + totals redraw
                RebuildGridAndTotalsFromTables();

                MessageBox.Show("Temporary entry add ho gayi (sirf is form ke liye).", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearEntryControls();
            }
            catch (Exception ex)
            {
                ShowError("Manual entry (temporary) save karte waqt error aaya.", ex);
            }
        }

        private void ClearEntryControls()
        {
            try
            {
                txtName.Clear();
                txtAmount.Clear();
                cbMode.SelectedIndex = -1;
                txtName.Focus();
            }
            catch (Exception ex)
            {
                ShowError("Controls clear karte waqt error aaya.", ex);
            }
        }

        private int GetOrCreateCustomerId(string name)
        {
            try
            {
                string qCheck = @"
                    SELECT id 
                    FROM AddCustomer
                    WHERE Name = @Name
                    LIMIT 1;
                ";

                Hashtable ht = new Hashtable { { "@Name", name } };

                DataTable dt = MainClass.ExecuteSelectQuery(qCheck, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0]["id"]);
                }

                string today = DateTime.Today.ToString("yyyy-MM-dd");

                string insertCustomer = @"
                    INSERT INTO AddCustomer (Name, Mobile, Date)
                    VALUES (@Name, '', @Date);
                ";

                ht.Clear();
                ht.Add("@Name", name);
                ht.Add("@Date", today);

                int res = MainClass.SQL(insertCustomer, ht);
                if (res <= 0)
                {
                    throw new Exception("Naya customer insert nahi ho saka.");
                }

                string qGetId = "SELECT last_insert_rowid() AS NewId;";
                DataTable dtId = MainClass.GetData(qGetId);

                if (dtId == null || dtId.Rows.Count == 0)
                    throw new Exception("last_insert_rowid() se ID nahi mili.");

                return Convert.ToInt32(dtId.Rows[0]["NewId"]);
            }
            catch (Exception ex)
            {
                ShowError("Customer ID hasil karte waqt error aaya.", ex);
                return 0;
            }
        }

        #endregion

        #region Helper Methods

        private DataTable SafeGetData(string query)
        {
            try
            {
                return MainClass.GetData(query);
            }
            catch (Exception ex)
            {
                ShowError("Query run karte waqt error aaya.\n\nQuery:\n" + query, ex);
                return null;
            }
        }

        private decimal SafeToDecimal(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                    return 0m;

                if (decimal.TryParse(Convert.ToString(value), out decimal result))
                    return result;

                return 0m;
            }
            catch
            {
                return 0m;
            }
        }

        private void SafeSetCell(DataGridViewRow row, string columnName, object value)
        {
            try
            {
                if (row == null || row.DataGridView == null)
                    return;

                if (!row.DataGridView.Columns.Contains(columnName))
                    return;

                row.Cells[columnName].Value = value ?? "";
            }
            catch
            {
            }
        }

        private void ShowError(string context, Exception ex)
        {
            try
            {
                MessageBox.Show(
                    context + Environment.NewLine + Environment.NewLine +
                    "Detail: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch
            {
            }
        }

        #endregion

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveManualEntry();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ExportToExcelCsv();
        }

        private void guna2DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvdel")
                    return;

                DataGridViewRow row = guna2DataGridView1.Rows[e.RowIndex];

                string recText = Convert.ToString(row.Cells["dgvRecived"].Value);  // Customer side
                string payText = Convert.ToString(row.Cells["dgvPayable"].Value);  // Dealer side

                if (string.IsNullOrWhiteSpace(recText) && string.IsNullOrWhiteSpace(payText))
                    return;

                // Dono side filled → CustomMessage
                if (!string.IsNullOrWhiteSpace(recText) && !string.IsNullOrWhiteSpace(payText))
                {
                    using (var dlg = new CustomMessageForCLosing(
                        "Kis side ko hide karna hai?",
                        "Row Filter"))
                    {
                        var dr = dlg.ShowDialog();

                        if (dr != DialogResult.OK)
                        {
                            // Cancel ya form band → kuch na karo
                            return;
                        }

                        if (dlg.Result == CustomMessageForCLosing.CustomMessageResult.Customer)
                        {
                            row.Cells["dgvRecived"].Value = "";
                        }
                        else if (dlg.Result == CustomMessageForCLosing.CustomMessageResult.Dealer)
                        {
                            row.Cells["dgvPayable"].Value = "";
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                // Sirf customer side filled hai
                else if (!string.IsNullOrWhiteSpace(recText))
                {
                    using (var dlg = new YesOrNoMessage(
                        "Sirf customer side hai.\nIs row ko customer total se hataun?",
                        "Confirm"))
                    {
                        var dr = dlg.ShowDialog();

                        if (dr == DialogResult.Yes)
                        {
                            row.Cells["dgvRecived"].Value = "";
                        }
                        else
                        {
                            // No ya close → kuch na karo
                            return;
                        }
                    }
                }
                // Sirf dealer side filled hai
                else if (!string.IsNullOrWhiteSpace(payText))
                {
                    using (var dlg = new YesOrNoMessage(
                        "Sirf dealer side hai.\nIs row ko dealer total se hataun?",
                        "Confirm"))
                    {
                        var dr = dlg.ShowDialog();

                        if (dr == DialogResult.Yes)
                        {
                            row.Cells["dgvPayable"].Value = "";
                        }
                        else
                        {
                            // No ya close → kuch na karo
                            return;
                        }
                    }
                }

                RecalculateTotalsFromGrid();
            }
            catch (Exception ex)
            {
                ShowError("Row filter (dgvdel) pe click karte waqt error aaya.", ex);
            }
        }

        private void RecalculateTotalsFromGrid()
        {
            try
            {
                decimal totalReceivable = 0m; // Customer side
                decimal totalPayable = 0m;    // Dealer side

                var culture = CultureInfo.CurrentCulture;

                foreach (DataGridViewRow row in guna2DataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    string recText = Convert.ToString(row.Cells["dgvRecived"].Value);
                    if (!string.IsNullOrWhiteSpace(recText))
                    {
                        int lastDash = recText.LastIndexOf('-');
                        if (lastDash >= 0 && lastDash < recText.Length - 1)
                        {
                            string amtPart = recText.Substring(lastDash + 1).Trim();

                            if (decimal.TryParse(amtPart, NumberStyles.Any, culture, out decimal val))
                            {
                                totalReceivable += val;
                            }
                        }
                    }

                    string payText = Convert.ToString(row.Cells["dgvPayable"].Value);
                    if (!string.IsNullOrWhiteSpace(payText))
                    {
                        int lastDash = payText.LastIndexOf('-');
                        if (lastDash >= 0 && lastDash < payText.Length - 1)
                        {
                            string amtPart = payText.Substring(lastDash + 1).Trim();

                            if (decimal.TryParse(amtPart, NumberStyles.Any, culture, out decimal val))
                            {
                                totalPayable += val;
                            }
                        }
                    }
                }

                if (lblCustomerAmount != null)
                    lblCustomerAmount.Text = totalReceivable.ToString("N2");

                if (lblDealerTotal != null)
                    lblDealerTotal.Text = totalPayable.ToString("N2");

                if (lblBalance != null)
                    lblBalance.Text = (totalReceivable - totalPayable).ToString("N2");
            }
            catch (Exception ex)
            {
                ShowError("Grid se totals dobara calculate karte waqt error aaya.", ex);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                LoadSummary();          // DB se original summary phir se load
                ClearEntryControls();   // txtName, txtAmount, cbMode clear
            }
            catch (Exception ex)
            {
                ShowError("Clear button par error aaya.", ex);
            }
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(guna2DataGridView1);
        }
        // ======================= GRID & TOTALS FROM MEMORY TABLES =======================
        private void RebuildGridAndTotalsFromTables()
        {
            try
            {
                // Labels reset
                decimal totalReceivable = 0m;
                decimal totalPayable = 0m;

                // --- Customer side (Receivable) ---
                if (_customerCredits != null)
                {
                    foreach (DataRow dr in _customerCredits.Rows)
                    {
                        totalReceivable += SafeToDecimal(dr["CustomerReceivable"]);
                    }
                }

                if (lblCustomerAmount != null)
                    lblCustomerAmount.Text = totalReceivable.ToString("N2");

                // --- Dealer side (Payable) ---
                if (_dealerList != null)
                {
                    foreach (DataRow dr in _dealerList.Rows)
                    {
                        totalPayable += SafeToDecimal(dr["DealerAmount"]);
                    }
                }

                if (lblDealerTotal != null)
                    lblDealerTotal.Text = totalPayable.ToString("N2");

                // Net Balance
                if (lblBalance != null)
                    lblBalance.Text = (totalReceivable - totalPayable).ToString("N2");

                // --- Grid fill from _customerCredits + _dealerList ---
                guna2DataGridView1.DataSource = null;
                guna2DataGridView1.Rows.Clear();
                guna2DataGridView1.AutoGenerateColumns = false;
                guna2DataGridView1.AllowUserToAddRows = false;
                guna2DataGridView1.ReadOnly = true;

                int customerCount = _customerCredits?.Rows.Count ?? 0;
                int dealerCount = _dealerList?.Rows.Count ?? 0;
                int maxRows = Math.Max(customerCount, dealerCount);

                for (int i = 0; i < maxRows; i++)
                {
                    int rowIndex = guna2DataGridView1.Rows.Add();
                    DataGridViewRow row = guna2DataGridView1.Rows[rowIndex];

                    // Left side: Customer (Receivable)
                    string receivableText = "";
                    if (i < customerCount && _customerCredits != null)
                    {
                        string custName = Convert.ToString(_customerCredits.Rows[i]["CustomerName"]);
                        decimal custReceive = SafeToDecimal(_customerCredits.Rows[i]["CustomerReceivable"]);
                        receivableText = $"{custName} - {custReceive:N2}";
                    }
                    SafeSetCell(row, "dgvRecived", receivableText);

                    // Right side: Dealer (Payable)
                    string payableText = "";
                    if (i < dealerCount && _dealerList != null)
                    {
                        string dealerName = Convert.ToString(_dealerList.Rows[i]["DealerName"]);
                        decimal dealerAmt = SafeToDecimal(_dealerList.Rows[i]["DealerAmount"]);
                        payableText = $"{dealerName} - {dealerAmt:N2}";
                    }
                    SafeSetCell(row, "dgvPayable", payableText);

                    // Faida column: optional, pehle blank
                    SafeSetCell(row, "dgvFaida", "");
                }

                // Agar date-range se _faidaAmount set hua hai to first row me dikha do
                if (guna2DataGridView1.Rows.Count > 0)
                {
                    SafeSetCell(guna2DataGridView1.Rows[0], "dgvFaida", _faidaAmount.ToString("N2"));
                }
            }
            catch (Exception ex)
            {
                ShowError("Memory tables se grid/totals rebuild karte waqt error aaya.", ex);
            }
        }

    }
}
