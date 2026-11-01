using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmDieselLedgerAdd : SampleAdd1
    {
        // ===== UNDO/REDO Variables =====
        private int _oldCustomerId = 0;             // Purani Customer ID
        private int _oldDealerId = 0;               // Purani Dealer ID
        private decimal _oldAmountPaid = 0;         // Purana Amount
        private DateTime _oldDate = DateTime.MinValue; // Purani Date

        public int LedgerID { get; set; }
        private string _customerName;
        private string _dealerName;

        public frmDieselLedgerAdd(int ledgerID = 0, string customerName = null, string dealerName = null)
        {
            InitializeComponent();
            LedgerID = ledgerID;
            _customerName = customerName;
            _dealerName = dealerName;

            // Combobox events
            cbDealer.SelectedIndexChanged += cbDealer_SelectedIndexChanged;
            cbCustomer.SelectedIndexChanged += cbCustomer_SelectedIndexChanged;
        }

        private void frmDieselLedgerAdd_Load(object sender, EventArgs e)
        {
            try
            {
                OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
                PopulateCustomerNames();
                PopulateDealerNames();

                dtpPaymentDate.Value = DateTime.Now;

                txtAmountPaid.TextChanged += txtAmountPaid_TextChanged;
                txtAmountPaid.KeyPress += txtAmountPaid_KeyPress;
                lblDealerBalance.TextChanged += lblDealerBalance_TextChanged;

                // Shortcut keys (Ctrl+1, Ctrl+2, Ctrl+S)
                this.KeyPreview = true;
                this.KeyDown += frmDieselLedgerAdd_KeyDown;

                // TextBoxes (manual name entry) – hidden by default
                txtCustomer.TextChanged += txtCustomer_TextChanged;
                txtDealer.TextChanged += txtDealer_TextChanged;

                txtCustomer.Visible = false;
                txtDealer.Visible = false;

                // Agar constructor se customerName / dealerName aya ho to combobox set karo
                if (!string.IsNullOrEmpty(_customerName))
                {
                    SetComboBoxValue(cbCustomer, _customerName);
                }
                if (!string.IsNullOrEmpty(_dealerName))
                {
                    SetComboBoxValue(cbDealer, _dealerName);
                }

                // Agar edit mode hai to record load karo
                if (LedgerID > 0)
                {
                    LoadLedgerDetails(LedgerID);
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Form load error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void frmDieselLedgerAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control) return;

            if (e.KeyCode == Keys.D1)
            {
                // Textboxes show, comboboxes hide
                txtCustomer.Visible = true;
                txtDealer.Visible = true;
                cbCustomer.Visible = false;
                cbDealer.Visible = false;
                txtCustomer.Focus();
            }
            else if (e.KeyCode == Keys.D2)
            {
                // Comboboxes show, textboxes hide
                txtCustomer.Visible = false;
                txtDealer.Visible = false;
                cbCustomer.Visible = true;
                cbDealer.Visible = true;
                cbCustomer.Focus();
            }
            else if (e.KeyCode == Keys.S)   // Ctrl + S for Save
            {
                e.SuppressKeyPress = true;
                btnSave.PerformClick();
            }
        }

        // =======================================
        // LOAD RECORD FOR EDIT
        // =======================================
        private void LoadLedgerDetails(int ledgerID)
        {
            try
            {
                string query = "SELECT * FROM DieselLedger WHERE LedgerID = @LedgerID";
                Hashtable ht = new Hashtable { { "@LedgerID", ledgerID } };

                DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    int customerId = Convert.ToInt32(row["id"]);   // id column
                    int dealerId = Convert.ToInt32(row["Did"]);    // Did column

                    cbCustomer.SelectedValue = customerId;
                    cbDealer.SelectedValue = dealerId;

                    txtAmountPaid.Text = row["AmounGiven"].ToString();
                    dtpPaymentDate.Value = Convert.ToDateTime(row["Date"]);
                    txtNote.Text = row["Note"]?.ToString() ?? string.Empty;

                    ShowCustomerBalance(customerId);
                    ShowDealerBalance(dealerId);

                    // ===== STORE OLD SCENARIO =====
                    _oldCustomerId = customerId;
                    _oldDealerId = dealerId;
                    _oldAmountPaid = Convert.ToDecimal(row["AmounGiven"]);
                    _oldDate = Convert.ToDateTime(row["Date"]);
                    OfficeHelper.SetSelectedOffice(this, Convert.ToString(row["OfficeName"]));
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Koi record nahi mila!", "Warning");
                    noDataMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("LoadLedgerDetails error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // =======================================
        // SAVE BUTTON
        // =======================================
        protected override void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbCustomer.SelectedIndex == -1 || cbDealer.SelectedIndex == -1)
                {
                    CustomeMessage validationMessage = new CustomeMessage("Customer aur dealer dono select karein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!decimal.TryParse(txtAmountPaid.Text.Replace(",", ""), out decimal newAmountPaid) || newAmountPaid <= 0)
                {
                    CustomeMessage validationMessage = new CustomeMessage("Valid payment amount daalein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                DateTime newDate = dtpPaymentDate.Value.Date;
                int newCustomerId = Convert.ToInt32(cbCustomer.SelectedValue);
                int newDealerId = Convert.ToInt32(cbDealer.SelectedValue);

                if (!OfficeHelper.RequireOfficeSelected(this))
                    return;
                string office = OfficeHelper.GetSelectedOffice(this);
                if (!OfficeHelper.ConfirmDealerForOffice(newDealerId, cbDealer.Text, office))
                    return;
                if (!OfficeHelper.ConfirmCustomerForOffice(newCustomerId, cbCustomer.Text, office))
                    return;

                decimal netCustomerDeduction = BalanceConfirmationService.NetDeduction(
                    newAmountPaid, _oldAmountPaid, LedgerID > 0 && newCustomerId == _oldCustomerId);
                if (!BalanceConfirmationService.ConfirmCustomerLedgerDeduction(newCustomerId, netCustomerDeduction, cbCustomer.Text))
                    return;

                // Edit Mode
                if (LedgerID > 0)
                {
                    // Agar purana data properly load hua tha tabhi undo karo
                    if (_oldCustomerId > 0 && _oldDealerId > 0 && _oldAmountPaid > 0 && _oldDate != DateTime.MinValue)
                    {
                        UndoOldScenario(_oldCustomerId, _oldDealerId, _oldAmountPaid, _oldDate);
                    }
                }

                // Naya scenario (insert or update)
                InsertOrUpdateLedger(LedgerID, newCustomerId, newDealerId, newAmountPaid, txtNote.Text, newDate, office);

                CustomeMessage successMessage = new CustomeMessage("Entry save ho gayi!", "Success");
                successMessage.ShowDialog();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("Save error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // =======================================
        // UNDO OLD SCENARIO
        // (PetrolAdd se sirf 1 purani entry remove kardo)
        // =======================================
        private void UndoOldScenario(int oldCustId, int oldDealId, decimal oldAmt, DateTime oldDt)
        {
            try
            {
                string dateStr = oldDt.ToString("yyyy-MM-dd");

                // 1) PetrolAdd se SIRF EK row delete karo (ROWID + LIMIT 1)
                string deletePetrolAddQuery = @"
                    DELETE FROM PetrolAdd
                    WHERE ROWID IN (
                        SELECT ROWID
                        FROM PetrolAdd
                        WHERE CustomerId = @custId
                          AND IsInitialEntry = 0
                          AND Credit = @amt
                          AND Date = @payDate
                          AND ReceiptNo = 'DealerPaymentForm'
                        ORDER BY ROWID DESC
                        LIMIT 1
                    );
                ";

                Hashtable delParams = new Hashtable
                {
                    { "@custId",  oldCustId },
                    { "@amt",     oldAmt },
                    { "@payDate", dateStr }
                };
                MainClass.DataInsertUpdateDelete(deletePetrolAddQuery, delParams);

                // 2) Dealer ke DAmount se purana amount minus karo
                string undoDealerQuery = "UPDATE AddDealer SET DAmount = DAmount - @amt WHERE Did = @did";
                Hashtable htDealer = new Hashtable
                {
                    { "@amt", oldAmt },
                    { "@did", oldDealId }
                };
                MainClass.DataInsertUpdateDelete(undoDealerQuery, htDealer);
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("UndoOldScenario error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // =======================================
        // INSERT / UPDATE LEDGER (and PetrolAdd)
        // =======================================
        private void InsertOrUpdateLedger(int ledgerId, int customerId, int dealerId, decimal amountPaid, string note, DateTime payDate, string office)
        {
            try
            {
                Hashtable ht = new Hashtable
                {
                    { "@custId",  customerId },
                    { "@did",     dealerId },
                    { "@amt",     amountPaid },
                    { "@dt",      payDate.ToString("yyyy-MM-dd") },
                    { "@note",    note ?? string.Empty },
                    { "@office",  office ?? string.Empty }
                };

                string sql = (ledgerId == 0)
                    ? @"INSERT INTO DieselLedger (Did, AmounGiven, Date, id, Balance, Note, OfficeName)
                        VALUES (@did, @amt, @dt, @custId, 0, @note, @office)"
                    : @"UPDATE DieselLedger
                        SET Did = @did,
                            AmounGiven = @amt,
                            Date = @dt,
                            id = @custId,
                            Note = @note,
                            Balance = 0,
                            OfficeName = @office
                        WHERE LedgerID = @lid";

                if (ledgerId != 0)
                    ht.Add("@lid", ledgerId);

                int result = MainClass.DataInsertUpdateDelete(sql, ht);
                if (result <= 0)
                {
                    throw new Exception("DieselLedger mein data save/update nahi hua!");
                }

                // Har scenario me PetrolAdd me bhi entry
                InsertPetrolAddEntry(customerId, amountPaid, payDate, note, office);

                // Dealer ke DAmount me add
                AddToDealerAmount(dealerId, amountPaid);
            }
            catch (Exception ex)
            {
                throw new Exception("InsertOrUpdateLedger error: " + ex.Message);
            }
        }

        // =======================================
        // INSERT PetrolAdd Entry (1 hi new row)
        // =======================================
        private void InsertPetrolAddEntry(int customerId, decimal amountPaid, DateTime payDate, string note, string office)
        {
            try
            {
                string insertQuery = @"
                    INSERT INTO PetrolAdd
                        (CustomerId, Date, ReceiptNo, Credit, Balance, Note, IsInitialEntry, OfficeName)
                    VALUES
                        (@custId, @dt, @rcpt, @credit, 0, @note, 0, @office)";
                Hashtable ht = new Hashtable
                {
                    { "@custId", customerId },
                    { "@dt", payDate.ToString("yyyy-MM-dd") },
                    { "@rcpt", "DealerPaymentForm" },
                    { "@credit", amountPaid },
                    { "@note", note ?? string.Empty },
                    { "@office", office ?? string.Empty }
                };
                int r = MainClass.DataInsertUpdateDelete(insertQuery, ht);
                if (r <= 0)
                {
                    throw new Exception("PetrolAdd mein data insert nahi hua!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("InsertPetrolAddEntry error: " + ex.Message);
            }
        }

        private void AddToDealerAmount(int dealerId, decimal amountPaid)
        {
            try
            {
                string updateQuery = "UPDATE AddDealer SET DAmount = DAmount + @amt WHERE Did = @did";
                Hashtable ht = new Hashtable
                {
                    { "@amt", amountPaid },
                    { "@did", dealerId }
                };
                int r = MainClass.DataInsertUpdateDelete(updateQuery, ht);
                if (r <= 0)
                {
                    throw new Exception("Dealer ka DAmount update nahi hua!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("AddToDealerAmount error: " + ex.Message);
            }
        }

        // =======================================
        // DELETE BUTTON
        // =======================================
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (LedgerID <= 0)
            {
                CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek valid record select karein!", "Warning");
                noSelectionMessage.ShowDialog();
                return;
            }

            try
            {
                string selectQuery = "SELECT Did, AmounGiven, id, Date FROM DieselLedger WHERE LedgerID = @LedgerID";
                Hashtable selectParams = new Hashtable { { "@LedgerID", LedgerID } };

                DataTable dt = MainClass.ExecuteSelectQuery(selectQuery, selectParams);
                if (dt == null || dt.Rows.Count == 0)
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Delete karne se pehle data fetch nahi hua!", "Error");
                    errorMessage.ShowDialog();
                    return;
                }

                DataRow row = dt.Rows[0];
                int dealerId = Convert.ToInt32(row["Did"]);
                decimal oldAmt = Convert.ToDecimal(row["AmounGiven"]);
                int custId = Convert.ToInt32(row["id"]);
                DateTime oldDate = Convert.ToDateTime(row["Date"]);

                YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                if (confirmDelete.ShowDialog() != DialogResult.Yes)
                    return;

                // 1) PetrolAdd se SIRF EK related row remove
                string deletePetrolAddQuery = @"
                    DELETE FROM PetrolAdd
                    WHERE ROWID IN (
                        SELECT ROWID
                        FROM PetrolAdd
                        WHERE CustomerId = @custId
                          AND Credit = @amt
                          AND Date = @dt
                          AND IsInitialEntry = 0
                          AND ReceiptNo = 'DealerPaymentForm'
                        ORDER BY ROWID DESC
                        LIMIT 1
                    );
                ";
                Hashtable delPetrolParams = new Hashtable
                {
                    { "@custId", custId },
                    { "@amt",    oldAmt },
                    { "@dt",     oldDate.ToString("yyyy-MM-dd") }
                };
                MainClass.DataInsertUpdateDelete(deletePetrolAddQuery, delPetrolParams);

                // 2) DieselLedger se row delete
                string deleteQuery = "DELETE FROM DieselLedger WHERE LedgerID = @Lid";
                Hashtable deleteParams = new Hashtable { { "@Lid", LedgerID } };
                int deleteResult = MainClass.DataInsertUpdateDelete(deleteQuery, deleteParams);

                if (deleteResult <= 0)
                {
                    ErrorFormMessage err = new ErrorFormMessage("DieselLedger se record delete nahi hua!", "Error");
                    err.ShowDialog();
                    return;
                }

                // 3) Dealer ke DAmount me se oldAmt minus
                string updateDealerQuery = "UPDATE AddDealer SET DAmount = DAmount - @amt WHERE Did = @did";
                Hashtable updateDealerParams = new Hashtable
                {
                    { "@amt", oldAmt },
                    { "@did", dealerId }
                };
                MainClass.DataInsertUpdateDelete(updateDealerQuery, updateDealerParams);

                ErrorFormMessage successMessage = new ErrorFormMessage(
                    "Record delete ho gaya, dealer ka DAmount minus ho gaya aur PetrolAdd se entry remove ho gayi!",
                    "Success");
                successMessage.ShowDialog();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Delete error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        // =======================================
        // COMBOBOX EVENT HANDLERS
        // =======================================
        private void cbCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbCustomer.SelectedItem is DataRowView drv)
                {
                    int customerId = Convert.ToInt32(drv["id"]);
                    ShowCustomerBalance(customerId);
                }
            }
            catch { /* ignore minor UI errors */ }
        }

        private void cbDealer_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbDealer.SelectedItem is DataRowView drv)
                {
                    int dealerId = Convert.ToInt32(drv["Did"]);
                    ShowDealerBalance(dealerId);
                }
            }
            catch { /* ignore minor UI errors */ }
        }

        // =======================================
        // SHOW BALANCES
        // =======================================
        private void ShowDealerBalance(int dealerId)
        {
            try
            {
                // DDAmount (total) aur DAmount (jitna diya gaya) dono uthao
                string query = @"
            SELECT 
                IFNULL(DDAmount, 0) AS DDAmount,
                IFNULL(DAmount, 0) AS DAmount
            FROM AddDealer 
            WHERE Did = @dealerId
        ";

                Hashtable ht = new Hashtable { { "@dealerId", dealerId } };

                DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    decimal ddAmount = Convert.ToDecimal(dt.Rows[0]["DDAmount"]); // total
                    decimal dAmount = Convert.ToDecimal(dt.Rows[0]["DAmount"]);  // paid / used

                    // jo tum chahte ho:
                    // RESULT = DDAmount - DAmount
                    decimal remaining = ddAmount - dAmount;

                    // label pe sirf result show:
                    lblDealerBalance.Text = $"Balance: {remaining:N0}";
                }
                else
                {
                    lblDealerBalance.Text = "Balance: 0";
                }
            }
            catch
            {
                // koi error ho jaye to app crash na ho
                lblDealerBalance.Text = "Balance: 0";
            }
        }

        private void ShowCustomerBalance(int customerId)
        {
            try
            {
                // Initial balance (IsInitialEntry = 1)
                string initialBalanceQuery = @"
                    SELECT SUM(Balance) AS InitialBalance 
                    FROM PetrolAdd 
                    WHERE CustomerId = @customerId AND IsInitialEntry = 1";
                Hashtable htInitial = new Hashtable { { "@customerId", customerId } };

                DataTable dtInitial = MainClass.ExecuteSelectQuery(initialBalanceQuery, htInitial);
                decimal initialBalance = 0;
                if (dtInitial != null && dtInitial.Rows.Count > 0 && dtInitial.Rows[0]["InitialBalance"] != DBNull.Value)
                {
                    initialBalance = Convert.ToDecimal(dtInitial.Rows[0]["InitialBalance"]);
                }

                // Total credit (IsInitialEntry = 0)
                string creditQuery = @"
                    SELECT SUM(Credit) AS TotalCredit 
                    FROM PetrolAdd 
                    WHERE CustomerId = @customerId AND IsInitialEntry = 0";
                DataTable dtCredit = MainClass.ExecuteSelectQuery(creditQuery, htInitial);

                decimal totalCredit = 0;
                if (dtCredit != null && dtCredit.Rows.Count > 0 && dtCredit.Rows[0]["TotalCredit"] != DBNull.Value)
                {
                    totalCredit = Convert.ToDecimal(dtCredit.Rows[0]["TotalCredit"]);
                }

                decimal remainingBalance = initialBalance - totalCredit;
                customerlbl.Text = $"Remaining Balance: {remainingBalance:N0}";
            }
            catch
            {
                customerlbl.Text = "Remaining Balance: 0";
            }
        }

        // =======================================
        // TEXTBOX EVENTS (Name Type Search)
        // =======================================
        private void txtCustomer_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string enteredName = txtCustomer.Text.Trim();
                DataTable dt = cbCustomer.DataSource as DataTable;

                if (string.IsNullOrEmpty(enteredName) || dt == null)
                {
                    cbCustomer.SelectedIndex = -1;
                    return;
                }

                string filter = $"Name = '{enteredName.Replace("'", "''")}'";
                DataRow[] rows = dt.Select(filter);

                if (rows.Length > 0)
                {
                    cbCustomer.SelectedValue = rows[0]["id"];
                }
                else
                {
                    cbCustomer.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void txtDealer_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string enteredName = txtDealer.Text.Trim();
                DataTable dt = cbDealer.DataSource as DataTable;

                if (string.IsNullOrEmpty(enteredName) || dt == null)
                {
                    cbDealer.SelectedIndex = -1;
                    return;
                }

                string filter = $"DealerName = '{enteredName.Replace("'", "''")}'";
                DataRow[] rows = dt.Select(filter);

                if (rows.Length > 0)
                {
                    cbDealer.SelectedValue = rows[0]["Did"];
                }
                else
                {
                    cbDealer.SelectedIndex = -1;
                }
            }
            catch { }
        }

        // =======================================
        // SET COMBOBOX VALUE by Name
        // =======================================
        private void SetComboBoxValue(ComboBox comboBox, string name)
        {
            try
            {
                if (comboBox.DataSource == null || string.IsNullOrEmpty(name))
                    return;

                foreach (DataRowView item in comboBox.Items)
                {
                    if (item[comboBox.DisplayMember]
                        .ToString()
                        .Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedValue = item[comboBox.ValueMember];
                        return;
                    }
                }

                // Agar exact match na mile to text set kar do (user ko pata chal jaye)
                comboBox.Text = name;
            }
            catch { }
        }

        // =======================================
        // POPULATE COMBOBOXES
        // =======================================
        private void PopulateCustomerNames()
        {
            try
            {
                string query = "SELECT id, Name FROM AddCustomer";
                DataTable dt = MainClass.ExecuteSelectQuery(query, null);
                if (dt != null && dt.Rows.Count > 0)
                {
                    cbCustomer.DataSource = dt;
                    cbCustomer.DisplayMember = "Name";
                    cbCustomer.ValueMember = "id";
                    cbCustomer.SelectedIndex = -1;

                    cbCustomer.DropDownHeight = 200;
                    cbCustomer.IntegralHeight = false;

                    AutoCompleteStringCollection autoSourceCustomer = new AutoCompleteStringCollection();
                    foreach (DataRow row in dt.Rows)
                    {
                        autoSourceCustomer.Add(row["Name"].ToString());
                    }
                    txtCustomer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtCustomer.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtCustomer.AutoCompleteCustomSource = autoSourceCustomer;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("PopulateCustomerNames error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        private void PopulateDealerNames()
        {
            try
            {
                string query = "SELECT Did, DealerName FROM AddDealer";
                DataTable dt = MainClass.ExecuteSelectQuery(query, null);
                if (dt != null && dt.Rows.Count > 0)
                {
                    cbDealer.DataSource = dt;
                    cbDealer.DisplayMember = "DealerName";
                    cbDealer.ValueMember = "Did";
                    cbDealer.SelectedIndex = -1;

                    cbDealer.DropDownHeight = 200;
                    cbDealer.IntegralHeight = false;

                    AutoCompleteStringCollection autoSourceDealer = new AutoCompleteStringCollection();
                    foreach (DataRow row in dt.Rows)
                    {
                        autoSourceDealer.Add(row["DealerName"].ToString());
                    }
                    txtDealer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtDealer.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtDealer.AutoCompleteCustomSource = autoSourceDealer;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("PopulateDealerNames error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // =======================================
        // AMOUNT FORMATTING
        // =======================================
        private void txtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = txtAmountPaid.Text.Replace(",", "");
                if (string.IsNullOrWhiteSpace(currentText))
                    return;

                if (decimal.TryParse(currentText, out decimal value))
                {
                    string formatted = value.ToString("N0", CultureInfo.InvariantCulture);
                    if (txtAmountPaid.Text != formatted)
                    {
                        int selStart = txtAmountPaid.SelectionStart;
                        txtAmountPaid.Text = formatted;
                        txtAmountPaid.SelectionStart = txtAmountPaid.Text.Length;
                    }
                }
            }
            catch { }
        }

        private void lblDealerBalance_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = lblDealerBalance.Text
                    .Replace("Balance:", "")
                    .Replace(",", "")
                    .Trim();

                if (decimal.TryParse(currentText, out decimal value))
                {
                    lblDealerBalance.Text = $"Balance: {value:N0}";
                }
            }
            catch { }
        }

        private void txtAmountPaid_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow control chars, digits and one decimal
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
                CustomeMessage msg = new CustomeMessage("صرف نمبر یا دہائی کا نشان درج کریں!", "Error");
                msg.ShowDialog();
                return;
            }

            TextBox textBox = sender as TextBox;
            if (e.KeyChar == '.' && textBox != null && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }
    }
}
