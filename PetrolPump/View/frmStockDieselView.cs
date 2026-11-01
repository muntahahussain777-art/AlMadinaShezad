using AlMadinaShahzad.Model;
using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public partial class frmStockDieselView : SampleView
    {
        public frmStockDieselView()
        {
            InitializeComponent();
        }

        private void frmStockDieselView_Load(object sender, EventArgs e)
        {
            try
            {
                OfficeHelper.AttachOfficeFilter(this, (s, ev) => LoadData1(), left: 520, top: 12);
                LoadData1();

                comboBox1.Items.Add("TotalAmount/Divide");  // New option for Litter * Rate divided by Litter
                comboBox1.SelectedIndex = 0;  // Default selection

                dtpStart.Value = DateTime.Now;
                dtpEnd.Value = DateTime.Now;

                dtpStart.ValueChanged += DatePickers_ValueChanged;
                dtpEnd.ValueChanged += DatePickers_ValueChanged;
                SetupStockGridFormatting();
                SetupStockPdfButton();
                SetupStockReportButton();
                SetupStockActionLayout();
                Shown += (s, ev) =>
                {
                    PositionTopActionButtons();
                    PositionSummaryLabels();
                };
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (Load): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                frmStockDieselAdd frm = new frmStockDieselAdd();
                frm.ShowDialog();
                LoadData1();
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (Add): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void LoadData1()
        {
            try
            {
                string searchText = txtSearch.Text.Trim();
                string office = OfficeHelper.GetSelectedOffice(this, allowAll: true);
                string officeFilter = OfficeHelper.OfficeSqlFilter("StockDiesel.OfficeName", office);

                string qry = @"
SELECT 
    StockDiesel.SID AS SID,
    StockDiesel.SDid,
    StockDiesel.Date,
    AddDealer.DealerName, 
    StockDiesel.Vehicle,
    StockDiesel.Rate,
    StockDiesel.Litter,
    StockDiesel.Credit,
    StockDiesel.Debit,
    StockDiesel.Note
FROM StockDiesel
LEFT JOIN AddDealer ON StockDiesel.SDid = AddDealer.Did 
WHERE (AddDealer.DealerName LIKE @searchText OR StockDiesel.Vehicle LIKE @searchText)" + officeFilter + @"
ORDER BY AddDealer.DealerName ASC
;";

                Hashtable parameters = new Hashtable();
                parameters.Add("@searchText", "%" + searchText + "%");

                DataTable dt = MainClass.ExecuteSelectQuery(qry, parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataTable displayTable = new DataTable();
                    displayTable.Columns.Add("Sr", typeof(int));
                    displayTable.Columns.Add("SID", typeof(int));
                    displayTable.Columns.Add("SDid", typeof(int));
                    displayTable.Columns.Add("Date", typeof(DateTime));
                    displayTable.Columns.Add("DealerName", typeof(string));
                    displayTable.Columns.Add("Vehicle", typeof(string));
                    displayTable.Columns.Add("Rate", typeof(decimal));
                    displayTable.Columns.Add("EntryType", typeof(string));
                    displayTable.Columns.Add("Litter", typeof(decimal));
                    displayTable.Columns.Add("Amount", typeof(decimal));    // 🔹 NAYA COLUMN
                    displayTable.Columns.Add("Credit", typeof(decimal));
                    displayTable.Columns.Add("Debit", typeof(decimal));
                    displayTable.Columns.Add("TotalLitter", typeof(decimal));
                    displayTable.Columns.Add("Balance", typeof(decimal));
                    displayTable.Columns.Add("Note", typeof(string));

                    var dealerTotals = new Dictionary<int, (decimal totalLitter, decimal totalCredit, decimal totalDebit)>();
                    var dealerNames = new Dictionary<int, string>();

                    foreach (DataRow row in dt.Rows)
                    {
                        int dealerId = row["SDid"] != DBNull.Value ? Convert.ToInt32(row["SDid"]) : 0;
                        int sid = row["SID"] != DBNull.Value ? Convert.ToInt32(row["SID"]) : 0;
                        string dealerName = row["DealerName"] != DBNull.Value ? row["DealerName"].ToString() : "Unknown";

                        decimal litterValue = row["Litter"] != DBNull.Value ? Convert.ToDecimal(row["Litter"]) : 0;
                        string noteValue = row["Note"] != DBNull.Value ? row["Note"].ToString() : "";
                        decimal signedLitter = StockDieselLitterHelper.GetSignedLitter(litterValue, noteValue);
                        decimal displayLitter = StockDieselLitterHelper.GetDisplayLitter(litterValue, noteValue);
                        string entryType = StockDieselLitterHelper.GetEntryType(litterValue, noteValue);
                        string displayNote = StockDieselLitterHelper.StripMinusTag(noteValue);

                        decimal creditValue = row["Credit"] != DBNull.Value ? Convert.ToDecimal(row["Credit"]) : 0;
                        decimal debitValue = row["Debit"] != DBNull.Value ? Convert.ToDecimal(row["Debit"]) : 0;
                        decimal rateValue = row["Rate"] != DBNull.Value ? Convert.ToDecimal(row["Rate"]) : 0;   // 🔹 rate alag

                        if (!dealerTotals.ContainsKey(dealerId))
                        {
                            dealerTotals[dealerId] = (0, 0, 0);
                            dealerNames[dealerId] = dealerName;
                        }

                        dealerTotals[dealerId] = (
                            dealerTotals[dealerId].totalLitter + signedLitter,
                            dealerTotals[dealerId].totalCredit + creditValue,
                            dealerTotals[dealerId].totalDebit + debitValue
                        );

                        DataRow newRow = displayTable.NewRow();
                        newRow["Sr"] = displayTable.Rows.Count + 1;
                        newRow["SID"] = sid;
                        newRow["SDid"] = dealerId;
                        newRow["Date"] = row["Date"] != DBNull.Value
                                            ? Convert.ToDateTime(row["Date"])
                                            : DateTime.MinValue;
                        newRow["DealerName"] = dealerName;
                        newRow["Vehicle"] = row["Vehicle"] != DBNull.Value ? row["Vehicle"].ToString() : "";
                        newRow["Rate"] = rateValue;
                        newRow["EntryType"] = entryType;
                        newRow["Litter"] = displayLitter;
                        newRow["Amount"] = displayLitter * rateValue;
                        newRow["Credit"] = creditValue;
                        newRow["Debit"] = debitValue;
                        newRow["Note"] = displayNote;
                        displayTable.Rows.Add(newRow);
                    }

                    // Dealer-wise total row (TotalLitter / Balance) – Amount ko yahan 0 hi rehne do
                    foreach (var dealerId in dealerTotals.Keys.ToList())
                    {
                        DataRow totalRow = displayTable.NewRow();
                        totalRow["Sr"] = displayTable.Rows.Count + 1;
                        totalRow["SDid"] = dealerId;
                        totalRow["DealerName"] = dealerNames[dealerId];
                        totalRow["TotalLitter"] = dealerTotals[dealerId].totalLitter;
                        totalRow["Balance"] = dealerTotals[dealerId].totalDebit - dealerTotals[dealerId].totalCredit;
                        // totalRow["Amount"] by default 0 / null
                        displayTable.Rows.Add(totalRow);
                    }

                    guna2DataGridView1.DataSource = displayTable;

                    if (guna2DataGridView1.Columns.Contains("Sr"))
                        guna2DataGridView1.Columns["Sr"].HeaderText = "S/N";

                    if (guna2DataGridView1.Columns.Contains("DealerName"))
                        guna2DataGridView1.Columns["DealerName"].HeaderText = "Dealer Name";

                    if (guna2DataGridView1.Columns.Contains("EntryType"))
                    {
                        guna2DataGridView1.Columns["EntryType"].HeaderText = "Type";
                        guna2DataGridView1.Columns["EntryType"].DisplayIndex = 6;
                    }

                    if (guna2DataGridView1.Columns.Contains("TotalLitter"))
                    {
                        guna2DataGridView1.Columns["TotalLitter"].HeaderText = "Total Litter";
                        guna2DataGridView1.Columns["TotalLitter"].DefaultCellStyle.Format = "F2";
                    }

                    if (guna2DataGridView1.Columns.Contains("Balance"))
                    {
                        guna2DataGridView1.Columns["Balance"].HeaderText = "Balance";
                        guna2DataGridView1.Columns["Balance"].DefaultCellStyle.Format = "F2";
                    }

                    // 🔹 Naya Amount column header + format
                    if (guna2DataGridView1.Columns.Contains("Amount"))
                    {
                        guna2DataGridView1.Columns["Amount"].HeaderText = "Amount (Ltr x Rate)";
                        guna2DataGridView1.Columns["Amount"].DefaultCellStyle.Format = "F2";
                    }

                    if (guna2DataGridView1.Columns.Contains("SID"))
                        guna2DataGridView1.Columns["SID"].Visible = false;

                    if (guna2DataGridView1.Columns.Contains("SDid"))
                        guna2DataGridView1.Columns["SDid"].Visible = false;
                }
                else
                {
                    guna2DataGridView1.DataSource = null;
                    UpdateStockSummaryLabels();
                }

                foreach (DataGridViewColumn column in guna2DataGridView1.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                UpdateStockSummaryLabels();
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (LoadData1): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || guna2DataGridView1.CurrentRow == null)
                    return;

                if (!guna2DataGridView1.Columns.Contains("SID"))
                {
                    CustomeMessage msg = new CustomeMessage("SID column nahi mila!", "Warning");
                    msg.ShowDialog();
                    return;
                }

                var cellValue = guna2DataGridView1.CurrentRow.Cells["SID"].Value;
                if (cellValue == null || cellValue == DBNull.Value)
                {
                    CustomeMessage msg = new CustomeMessage("SID empty hai, record edit nahi ho sakta!", "Warning");
                    msg.ShowDialog();
                    return;
                }

                int id = Convert.ToInt32(cellValue);

                frmStockDieselAdd frm = new frmStockDieselAdd();
                frm.id = id; // Set the ID for editing
                frm.ShowDialog(); // Open the add form in edit mode
                LoadData1(); // Reload data after closing the add form

                try
                {
                    frm.txtcredit.Focus();
                    frm.txtcredit.SelectAll();
                    frm.txtdebit.Focus();
                    frm.txtdebit.SelectAll();
                }
                catch
                {
                    // agar access na mile to ignore
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (CellDoubleClick): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                LoadData1();
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (Search): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void btnAverage_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    string selectedOption = comboBox1.SelectedItem.ToString();

                    if (selectedOption == "TotalAmount/Divide")
                    {
                        CalculateAverageDiselRate();
                    }
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
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (Average): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // Method to calculate the sum of Litter * Rate and divide by Litter
        private void CalculateAverageDiselRate()
        {
            try
            {
                if (guna2DataGridView1.DataSource == null)
                {
                    lblResult.Text = "Koi data nahi hai.";
                    return;
                }

                DateTime startDate = dtpStart.Value.Date;
                DateTime endDate = dtpEnd.Value.Date;

                decimal totalAmountSum = 0;
                decimal totalLitterSum = 0;

                foreach (DataGridViewRow row in guna2DataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    if (!guna2DataGridView1.Columns.Contains("Date") ||
                        !guna2DataGridView1.Columns.Contains("Litter") ||
                        !guna2DataGridView1.Columns.Contains("Rate"))
                    {
                        continue;
                    }

                    if (row.Cells["Date"].Value == null || row.Cells["Date"].Value == DBNull.Value)
                        continue;

                    DateTime rowDate;
                    if (!DateTime.TryParse(row.Cells["Date"].Value.ToString(), out rowDate))
                        continue;

                    rowDate = rowDate.Date;

                    if (rowDate >= startDate && rowDate <= endDate)
                    {
                        if (row.Cells["SID"].Value == null || row.Cells["SID"].Value == DBNull.Value)
                            continue;

                        if (row.Cells["Litter"].Value != DBNull.Value &&
                            row.Cells["Rate"].Value != DBNull.Value &&
                            row.Cells["Litter"].Value != null &&
                            row.Cells["Rate"].Value != null)
                        {
                            decimal litter;
                            decimal rate;

                            if (!decimal.TryParse(row.Cells["Litter"].Value.ToString(), out litter))
                                litter = 0;

                            if (!decimal.TryParse(row.Cells["Rate"].Value.ToString(), out rate))
                                rate = 0;

                            string entryType = row.Cells["EntryType"].Value?.ToString() ?? "";
                            bool isMinus = entryType.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0;
                            decimal signedLitter = isMinus ? -Math.Abs(litter) : Math.Abs(litter);

                            decimal amount = Math.Abs(litter) * rate;
                            totalAmountSum += isMinus ? -amount : amount;
                            totalLitterSum += signedLitter;
                        }
                    }
                }

                UpdateStockSummaryLabels(startDate, endDate);

                if (totalLitterSum != 0)
                {
                    decimal average = totalAmountSum / totalLitterSum;
                    lblResult.Text = $"Sum Amount: {totalAmountSum:N0}, Sum Litter: {totalLitterSum:N2}, Average: {average:N2}";
                }
                else
                {
                    lblResult.Text = "Total Litter is zero, cannot calculate average.";
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (AverageCalc): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void DatePickers_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                CalculateAverageDiselRate();
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Error (DateChange): " + ex.Message, "Error");
                err.ShowDialog();
            }
        }
    }
}
