using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmDiselAdd : SampleAdd
    {
        private List<string> existingCustomerNames = new List<string>();
        public int id = 0;
        private decimal TotalBalance = 0;
        private bool _isLoadingData;

        public frmDiselAdd()
        {
            try
            {
                InitializeComponent();
                txtlitter.TextChanged += TriggerRecalculation;
                txtrate.TextChanged += TriggerRecalculation;
                txtlitter.Leave += txtlitter_Leave;
                txtrate.Leave += txtrate_Leave;
                txtadvance.TextChanged += TriggerRecalculation;
                txtcredit.TextChanged += TriggerRecalculation;
                this.KeyPreview = true;
                this.KeyDown += new KeyEventHandler(frmDiselAdd_KeyDown);
                PopulateCustomerNames();
                SetupVehicleAutoComplete();

                cbName.SelectionChangeCommitted += cbName_SelectionChangeCommitted;
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (Constructor): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }


        private void frmDiselAdd_Load(object sender, EventArgs e)
        {
            try
            {
                OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 520, top: 12);
                LoadCustomers();
                SetupVehicleAutoComplete();

                if (id > 0)
                {
                    LoadData();
                }
                else
                {
                    txtdate.Value = DateTime.Now;
                }
                txtbalance.TextChanged += new EventHandler(txtbalance_TextChanged);
                txtamount.TextChanged += new EventHandler(txtamount_TextChanged);
                txtadvance.TextChanged += new EventHandler(txtadvance_TextChanged);
                txtcredit.TextChanged += new EventHandler(txtcredit_TextChanged);

                SetupCustomerCreditDisplay();
                RefreshCustomerCreditDisplay();

                BeginInvoke(new Action(() =>
                {
                    txtVehicle.Focus();
                    txtVehicle.Select();
                }));
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (Form Load): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        /// <summary>
        /// PetrolAdd history se saare vehicle numbers — type karte hi Excel-style suggest (SuggestAppend).
        /// Vehicle likhne se customer auto-select NAHI hota.
        /// </summary>
        private void SetupVehicleAutoComplete()
        {
            try
            {
                string qry = @"
            SELECT DISTINCT TRIM(vehicle) AS VehicleNo
            FROM PetrolAdd
            WHERE TRIM(IFNULL(vehicle, '')) <> ''
            ORDER BY VehicleNo COLLATE NOCASE;
        ";
                DataTable dt = MainClass.ExecuteSelectQuery(qry, null);
                var vehicles = new AutoCompleteStringCollection();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string v = (Convert.ToString(row["VehicleNo"]) ?? "").Trim();
                        if (!string.IsNullOrEmpty(v))
                            vehicles.Add(v);
                    }
                }

                txtVehicle.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtVehicle.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtVehicle.AutoCompleteCustomSource = vehicles;
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (SetupVehicleAutoComplete): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        /// <summary>
        /// Selected customer ke liye PetrolAdd history se sab se zyada use hone wala
        /// vehicle number fill karta hai. User baad mein vehicle change / type-suggest kar sakta hai.
        /// </summary>
        private void SuggestVehicleByCustomer()
        {
            try
            {
                if (_isLoadingData)
                    return;

                int customerId = 0;
                if (cbName.SelectedValue != null && cbName.SelectedValue != DBNull.Value)
                    int.TryParse(cbName.SelectedValue.ToString(), out customerId);

                if (customerId <= 0)
                {
                    string name = (txtName.Visible ? txtName.Text : cbName.Text)?.Trim();
                    if (!string.IsNullOrEmpty(name))
                        customerId = ResolveCustomerIdForSave(name);
                }

                if (customerId <= 0)
                    return;

                string qry = @"
            SELECT TRIM(p.vehicle) AS VehicleNo, COUNT(*) AS UseCount
            FROM PetrolAdd p
            WHERE p.CustomerId = @cid
              AND TRIM(IFNULL(p.vehicle, '')) <> ''
            GROUP BY TRIM(p.vehicle)
            ORDER BY UseCount DESC
            LIMIT 1;
        ";

                Hashtable ht = new Hashtable { { "@cid", customerId } };
                DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    string vehicleNo = Convert.ToString(dt.Rows[0]["VehicleNo"]) ?? "";
                    if (!string.IsNullOrWhiteSpace(vehicleNo))
                        txtVehicle.Text = vehicleNo.Trim();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (SuggestVehicleByCustomer): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void cbName_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                SuggestVehicleByCustomer();
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (cbName_SelectionChangeCommitted): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void LoadCustomers()
        {
            try
            {
                string customerQry = "SELECT Id, Name FROM AddCustomer";
                DataTable customerData = MainClass.ExecuteSelectQuery(customerQry, null);

                if (customerData != null)
                {
                    cbName.DisplayMember = "Name";
                    cbName.ValueMember = "Id";
                    cbName.DataSource = customerData;
                }
                else
                {
                    cbName.DataSource = null;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (LoadCustomers): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void frmDiselAdd_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Control)
                {
                    if (e.KeyCode == Keys.D1)
                    {
                        txtName.Visible = true;
                        cbName.Visible = false;
                        txtName.Focus();
                    }
                    else if (e.KeyCode == Keys.D2)
                    {
                        cbName.Visible = true;
                        txtName.Visible = false;
                        cbName.Focus();
                    }
                    else if (e.KeyCode == Keys.S)
                    {
                        btnSave_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (KeyDown): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void PopulateCustomerNames()
        {
            try
            {
                string query = "SELECT Id, Name FROM AddCustomer";
                DataTable dt = MainClass.ExecuteSelectQuery(query, null);

                if (dt != null && dt.Rows.Count > 0)
                {
                    cbName.DataSource = dt;
                    cbName.DisplayMember = "Name";
                    cbName.ValueMember = "Id";
                    cbName.DropDownHeight = 200;
                    cbName.IntegralHeight = false;

                    AutoCompleteStringCollection customerNames = new AutoCompleteStringCollection();
                    foreach (DataRow row in dt.Rows)
                    {
                        customerNames.Add(row["Name"].ToString());
                    }

                    txtName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtName.AutoCompleteCustomSource = customerNames;

                    cbName.SelectedIndexChanged += new EventHandler(cbName_SelectedIndexChanged);
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Koi customer data nahi mila!", "Warning");
                    noDataMessage.ShowDialog();
                    cbName.DataSource = null;
                    txtName.AutoCompleteCustomSource = null;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (PopulateCustomerNames): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void cbName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Agar txtName visible hai aur uska text cbName ke selected item se match karta hai, to validation skip karo
                if (txtName.Visible && !string.IsNullOrWhiteSpace(txtName.Text) && cbName.Text == txtName.Text.Trim())
                {
                    return; // No validation message jab txtName se select ho raha hai
                }

                if (cbName.SelectedValue != null && cbName.SelectedValue != DBNull.Value)
                {
                    if (!int.TryParse(cbName.SelectedValue.ToString(), out int customerId))
                    {
                        CustomeMessage warningMessage = new CustomeMessage("Selected value valid integer nahi hai!", "Warning");
                        warningMessage.ShowDialog();
                    }
                }
                else if (cbName.SelectedIndex != -1) // Agar koi item select hua hai lekin value null hai
                {
                    CustomeMessage warningMessage = new CustomeMessage("Koi valid customer select nahi hua!", "Warning");
                    warningMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (cbName_SelectedIndexChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void TriggerRecalculation(object sender, EventArgs e)
        {
            try
            {
                decimal litter = 0, rate = 0, advance = 0, credit = 0, amount = 0;

                bool isLitterValid = decimal.TryParse(txtlitter.Text, out litter);
                bool isRateValid = decimal.TryParse(txtrate.Text, out rate);
                bool isAdvanceValid = decimal.TryParse(txtadvance.Text.Replace(",", ""), out advance);
                bool isCreditValid = decimal.TryParse(txtcredit.Text.Replace(",", ""), out credit);
                bool isAmountValid = decimal.TryParse(txtamount.Text.Replace(",", ""), out amount);

                if (isLitterValid && isRateValid)
                {
                    amount = litter * rate;
                    // Amount - Credit - Advance (advance ab minus)
                    decimal finalBalance = amount - credit - advance;

                    txtamount.Text = amount.ToString("F2");
                    txtbalance.Text = finalBalance.ToString("F2");
                }
                else if (isAmountValid && !isLitterValid && !isRateValid) // Agar litter aur rate khali hain, lekin amount diya gaya hai
                {
                    decimal finalBalance = amount - credit - advance;
                    txtbalance.Text = finalBalance.ToString("F2");
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (TriggerRecalculation): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void LoadData()
        {
            try
            {
                _isLoadingData = true;
                string qry = "SELECT PetrolAdd.Date, PetrolAdd.ReceiptNo, PetrolAdd.vehicle, PetrolAdd.Litter, PetrolAdd.Rate, " +
                       "PetrolAdd.Advance, " +
                       "CASE WHEN (IFNULL(PetrolAdd.Litter, 0) = 0 OR IFNULL(PetrolAdd.Rate, 0) = 0) " +
                       "     THEN 0 " +
                       "     ELSE PetrolAdd.Litter * PetrolAdd.Rate " +
                       "END AS Amount, " +
                       "CASE WHEN (IFNULL(PetrolAdd.Litter, 0) = 0 OR IFNULL(PetrolAdd.Rate, 0) = 0) " +
                       "     THEN PetrolAdd.Balance " +
                       "     ELSE (PetrolAdd.Litter * PetrolAdd.Rate - PetrolAdd.Credit) " +
                       "END AS Balance, " +
                       "PetrolAdd.Credit, PetrolAdd.Note, PetrolAdd.CustomerId, IFNULL(PetrolAdd.OfficeName,'') AS OfficeName " +
                       "FROM PetrolAdd " +
                       "INNER JOIN AddCustomer ON PetrolAdd.CustomerId = AddCustomer.Id " +
                       "WHERE PetrolAdd.Pid = @id";

                Hashtable ht = new Hashtable();
                ht.Add("@id", id);

                DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    txtdate.Text = row["Date"].ToString();
                    txtrecipt.Text = row["ReceiptNo"].ToString();
                    txtVehicle.Text = row["vehicle"].ToString();
                    txtlitter.Text = row["Litter"].ToString();
                    txtrate.Text = row["Rate"] != DBNull.Value ? FormatRate(Convert.ToDecimal(row["Rate"])) : "";
                    txtadvance.Text = row["Advance"].ToString();

                    decimal amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0;
                    decimal credit = row["Credit"] != DBNull.Value ? Convert.ToDecimal(row["Credit"]) : 0;
                    decimal advance = row["Advance"] != DBNull.Value ? Convert.ToDecimal(row["Advance"]) : 0;
                    decimal balance = row["Balance"] != DBNull.Value ? Convert.ToDecimal(row["Balance"]) : 0;

                    txtamount.Text = amount.ToString("F2");
                    txtcredit.Text = credit.ToString("F2");
                    // Amount - Credit - Advance
                    txtbalance.Text = (amount - credit - advance).ToString("F2");

                    txtnote.Text = row["Note"].ToString();
                    OfficeHelper.SetSelectedOffice(this, Convert.ToString(row["OfficeName"]));

                    if (cbName.Items.Count > 0)
                    {
                        cbName.SelectedValue = row["CustomerId"];
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (LoadData): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 🔹 Save se pehle last selected customer yaad rakh lo
                object lastCustomerId = cbName.SelectedValue;

                string customerName = string.Empty;

                if (txtName.Visible && !string.IsNullOrWhiteSpace(txtName.Text))
                {
                    customerName = txtName.Text;
                }
                else if (cbName.SelectedIndex != -1)
                {
                    customerName = cbName.Text;
                }
                else
                {
                    CustomeMessage validationMessage = new CustomeMessage("Customer select karein ya naam daalein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }
                // ... (baaki tumhara code same ka same)


                decimal advance;
                if (!decimal.TryParse(txtadvance.Text.Replace(",", ""), out advance))
                    advance = 0;

                decimal credit;
                if (!decimal.TryParse(txtcredit.Text.Replace(",", ""), out credit))
                    credit = 0;

                decimal litter;
                if (!decimal.TryParse(txtlitter.Text.Replace(",", ""), out litter))
                    litter = 0; // Default to 0 agar khali ho

                decimal rate;
                if (!decimal.TryParse(txtrate.Text.Replace(",", ""), out rate))
                    rate = 0; // Default to 0 agar khali ho

                if (!decimal.TryParse(txtamount.Text.Replace(",", ""), out decimal amount))
                {
                    CustomeMessage validationMessage = new CustomeMessage("Valid amount daalein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!DateTime.TryParse(txtdate.Text, out DateTime dateValue))
                {
                    CustomeMessage validationMessage = new CustomeMessage("Valid date daalein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!OfficeHelper.RequireOfficeSelected(this))
                    return;
                string office = OfficeHelper.GetSelectedOffice(this);

                int customerId = ResolveCustomerIdForSave(customerName);
                if (customerId <= 0)
                {
                    CustomeMessage validationMessage = new CustomeMessage(
                        "Customer select karein ya sahi naam likhein!\n(Milta-julta naam mix nahi hoga — exact naam chahiye.)",
                        "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!OfficeHelper.ConfirmCustomerForOffice(customerId, customerName, office))
                    return;

                // Amount - Credit - Advance (advance minus)
                decimal finalBalance = amount - credit - advance;
                TotalBalance += finalBalance;

                string qry;
                Hashtable htSave = new Hashtable();
                bool isEdit = id > 0;

                if (!isEdit)
                {
                    qry = "INSERT INTO PetrolAdd (CustomerId, Date, ReceiptNo, vehicle, Litter, Rate, Advance, Amount, Credit, Balance, Note, IsInitialEntry, OfficeName) " +
                          "VALUES (@customerId, @date, @recipt, @veh, @litter, @rate, @advance, @amount, @credit, @balance, @note, 1, @office)";
                }
                else
                {
                    // Sirf sale row (IsInitialEntry=1) — credit entry touch nahi
                    qry = "UPDATE PetrolAdd SET CustomerId = @customerId, Date = @date, ReceiptNo = @recipt, vehicle = @veh, " +
                          "Litter = @litter, Rate = @rate, Advance = @advance, Amount = @amount, Credit = @credit, " +
                          "Balance = @balance, Note = @note, OfficeName = @office WHERE pid = @pid AND IFNULL(IsInitialEntry,1)=1";
                    htSave.Add("@pid", id);
                }

                htSave.Add("@customerId", customerId);
                htSave.Add("@date", dateValue.ToString("yyyy-MM-dd"));
                htSave.Add("@recipt", txtrecipt.Text);
                htSave.Add("@veh", txtVehicle.Text);
                htSave.Add("@litter", litter.ToString("F2")); // 0.0 save hoga agar khali ho
                htSave.Add("@rate", FormatRate(rate));
                htSave.Add("@advance", advance.ToString("F2"));
                htSave.Add("@amount", amount.ToString("F2"));
                htSave.Add("@credit", credit.ToString("F2"));
                htSave.Add("@balance", finalBalance.ToString("F2"));
                htSave.Add("@note", txtnote.Text);
                htSave.Add("@office", office);

                int r = MainClass.DataInsertUpdateDelete(qry, htSave);
                string lastTypedName = txtName.Text.Trim();

                if (r > 0)
                {
                    CustomeMessage successMessage = new CustomeMessage(
                        isEdit ? "Entry update ho gayi!" : "Entry save ho gayi!", "Success");
                    successMessage.ShowDialog();

                    if (isEdit)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }

                    ResetFormKeepSelectedDate();
                    id = 0;
                    TotalBalance = 0;
                    SetupVehicleAutoComplete();

                    // ------------------------------
                    // ⭐ LOGIC: kis mode me ho?
                    // ------------------------------

                    if (txtName.Visible)   // MODE = CTRL+1 (manual name)
                    {
                        txtName.Text = lastTypedName;   // same name wapas fill
                        SuggestVehicleByCustomer();
                        txtName.Focus();                // focus textBox par
                    }
                    else                   // MODE = CTRL+2 (comboBox)
                    {
                        cbName.SelectedValue = lastCustomerId;
                        SuggestVehicleByCustomer();
                        cbName.Focus();
                    }

                    // txtamount aur txtbalance ko hamesha clear rakho
                    txtamount.Clear();
                    txtbalance.Clear();
                }
                else if (isEdit)
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage(
                        "Update nahi hua. Ye Credit entry ho sakti hai — Credit Customer se double-click karein.", "Error");
                    errorMessage.ShowDialog();
                }


            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (Save): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        public override void btnDel_Click(object sender, EventArgs e)
        {
            try
            {
                if (id > 0)
                {
                    YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        try
                        {
                            string qry = "DELETE FROM PetrolAdd WHERE pid = @pid";
                            Hashtable ht = new Hashtable();
                            ht.Add("@pid", id);

                            int r = MainClass.DataInsertUpdateDelete(qry, ht);
                            if (r > 0)
                            {
                                CustomeMessage successMessage = new CustomeMessage("Record delete ho gaya!", "Success");
                                successMessage.ShowDialog();
                                ResetFormKeepSelectedDate();
                                id = 0;
                            }
                            else
                            {
                                ErrorFormMessage errorMessage = new ErrorFormMessage("Record delete nahi hua!", "Error");
                                errorMessage.ShowDialog();
                            }
                        }
                        catch (Exception exInner)
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage("Error (Delete Inner): " + exInner.Message, "Error");
                            errorMessage.ShowDialog();
                        }
                    }
                }
                else
                {
                    CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek record select karein!", "Warning");
                    noSelectionMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (Delete): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtbalance_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = txtbalance.Text.Replace(",", "");
                if (decimal.TryParse(currentText, out decimal value))
                {
                    txtbalance.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                    txtbalance.SelectionStart = txtbalance.Text.Length;
                }
            }
            catch (Exception ex)
            {
                // Formatting error pe app crash na ho
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtbalance_TextChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtcredit_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = txtcredit.Text.Replace(",", "");
                if (decimal.TryParse(currentText, out decimal value))
                {
                    txtcredit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                    txtcredit.SelectionStart = txtcredit.Text.Length;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtcredit_TextChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtamount_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = txtamount.Text.Replace(",", "");
                if (decimal.TryParse(currentText, out decimal value))
                {
                    txtamount.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                    txtamount.SelectionStart = txtamount.Text.Length;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtamount_TextChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtadvance_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = txtadvance.Text.Replace(",", "");
                if (decimal.TryParse(currentText, out decimal value))
                {
                    txtadvance.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                    txtadvance.SelectionStart = txtadvance.Text.Length;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtadvance_TextChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtlitter_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (char.IsControl(e.KeyChar))
                    return;

                var tb = sender as Control;
                string text = tb is Guna.UI2.WinForms.Guna2TextBox gtb ? gtb.Text : (tb as TextBox)?.Text ?? "";

                // Starting mein point (.) allow nahi
                if (e.KeyChar == '.' && (text.Length == 0 || GetSelectionStart(tb) == 0))
                {
                    e.Handled = true;
                    return;
                }

                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    e.Handled = true;
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtlitter_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private const int RateDecimalPlaces = 4;

        private static string FormatRate(decimal rate)
        {
            return rate == decimal.Truncate(rate)
                ? rate.ToString("0", CultureInfo.InvariantCulture)
                : rate.ToString("0.0####", CultureInfo.InvariantCulture);
        }

        private void txtrate_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (char.IsControl(e.KeyChar))
                    return;

                var tb = sender as Control;
                string text = tb is Guna.UI2.WinForms.Guna2TextBox gtb ? gtb.Text : (tb as TextBox)?.Text ?? "";
                int selStart = GetSelectionStart(tb);
                int selLen = GetSelectionLength(tb);

                if (e.KeyChar == '.')
                {
                    if (text.Contains(".") || selStart == 0)
                    {
                        e.Handled = true;
                        return;
                    }
                    return;
                }

                if (!char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                    return;
                }

                string before = text.Substring(0, selStart);
                string after = text.Substring(selStart + selLen);
                string newText = before + e.KeyChar + after;

                int dotIdx = newText.IndexOf('.');
                if (dotIdx >= 0 && newText.Length - dotIdx - 1 > RateDecimalPlaces)
                    e.Handled = true;
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtrate_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtlitter_Leave(object sender, EventArgs e)
        {
            try
            {
                string t = txtlitter.Text ?? "";
                if (t.StartsWith("."))
                    txtlitter.Text = t.TrimStart('.');
            }
            catch { }
        }

        private void txtrate_Leave(object sender, EventArgs e)
        {
            try
            {
                SanitizeRateText();
            }
            catch { }
        }

        private void SanitizeRateText()
        {
            string t = (txtrate.Text ?? "").Trim().Replace(",", "");
            if (string.IsNullOrEmpty(t))
                return;

            if (t.StartsWith("."))
                t = "0" + t;

            if (t.EndsWith("."))
            {
                txtrate.Text = t.TrimEnd('.');
                return;
            }

            if (decimal.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal rate))
            {
                string formatted = FormatRate(rate);

                if (formatted != txtrate.Text)
                    txtrate.Text = formatted;
            }
        }

        private static int GetSelectionStart(Control ctrl)
        {
            if (ctrl is Guna.UI2.WinForms.Guna2TextBox g) return g.SelectionStart;
            if (ctrl is TextBox t) return t.SelectionStart;
            return 0;
        }

        private static int GetSelectionLength(Control ctrl)
        {
            if (ctrl is Guna.UI2.WinForms.Guna2TextBox g) return g.SelectionLength;
            if (ctrl is TextBox t) return t.SelectionLength;
            return 0;
        }

        private void txtamount_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                    CustomeMessage customMessageBox = new CustomeMessage("آپ یہاں صرف نمبر درج کر سکتے ہیں۔", "غلطی");
                    customMessageBox.ShowDialog();
                }
                if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtamount_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtcredit_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                    CustomeMessage customMessageBox = new CustomeMessage("آپ یہاں صرف نمبر درج کر سکتے ہیں۔", "غلطی");
                    customMessageBox.ShowDialog();
                }
                if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtcredit_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtbalance_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                    CustomeMessage customMessageBox = new CustomeMessage("آپ یہاں صرف نمبر درج کر سکتے ہیں۔", "غلطی");
                    customMessageBox.ShowDialog();
                }
                if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtbalance_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtadvance_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                    CustomeMessage customMessageBox = new CustomeMessage("آپ یہاں صرف نمبر درج کر سکتے ہیں۔", "غلطی");
                    customMessageBox.ShowDialog();
                }
                if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtadvance_KeyPress): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private int ResolveCustomerIdForSave(string customerName)
        {
            if (cbName.SelectedValue != null && cbName.SelectedValue != DBNull.Value &&
                int.TryParse(cbName.SelectedValue.ToString(), out int selectedId) && selectedId > 0)
                return selectedId;

            if (string.IsNullOrWhiteSpace(customerName))
                return 0;

            var ht = new Hashtable { { "@name", customerName.Trim() } };
            DataTable dt = MainClass.ExecuteSelectQuery(
                "SELECT id FROM AddCustomer WHERE TRIM(Name) = @name COLLATE NOCASE LIMIT 1", ht);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["id"]);
            return 0;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string enteredName = txtName.Text.Trim();
                if (!string.IsNullOrEmpty(enteredName))
                {
                    DataTable dt = cbName.DataSource as DataTable;
                    if (dt != null)
                    {
                        DataRow match = null;
                        foreach (DataRow row in dt.Rows)
                        {
                            if (string.Equals(Convert.ToString(row["Name"]), enteredName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                match = row;
                                break;
                            }
                        }

                        if (match != null)
                        {
                            cbName.SelectedValue = match["Id"];
                            SuggestVehicleByCustomer();
                        }
                        else
                            cbName.SelectedIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtName_TextChanged): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }
    }
}
