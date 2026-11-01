using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using System.Xml.Linq;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmCustomerToCustomerAdd : SampleAdd1
    {
        // Yeh 4 private variables rakhein, 
        // taa-ke hum "undo" ke liye purana data store kar sakein
        private int _oldCustomerId = 0;
        private int _oldDealerId = 0;
        private decimal _oldAmountPaid = 0;
        private DateTime _oldDate = DateTime.MinValue;

        public int LedgerID { get; set; }

        // In case you’re also passing a customerName/dealerName from outside
        private string _customerName;
        private string _dealerName;

        public frmCustomerToCustomerAdd(int ledgerID = 0, string customerName = null, string dealerName = null)
        {
            InitializeComponent();
            LedgerID = ledgerID;
            _customerName = customerName;
            _dealerName = dealerName;

            // NOTE: Pahle hi form ke Load event mein 
            // hum sab kuch karenge (Populate + agar edit ho to LoadLedgerDetails).
            // Yahan direct LoadLedgerDetails(ledgerID) mat chalayein

            cbDealer.SelectedIndexChanged += cbDealer_SelectedIndexChanged;
            cbCustomer.SelectedIndexChanged += cbCustomer_SelectedIndexChanged;
        }

        private void frmCustomerToCustomerAdd_Load(object sender, EventArgs e)
        {
            PopulateCustomerNames();
            PopulateDealerNames();
            dtpPaymentDate.Value = DateTime.Now;
            txtAmountPaid.TextChanged += txtAmountPaid_TextChanged;

            // NAYA CODE
            txtCustomer.TextChanged += txtCustomer_TextChanged;
            txtDealer.TextChanged += txtDealer_TextChanged;

            // form ke liye KeyPreview
            this.KeyPreview = true;
            this.KeyDown += frmCustomerToCustomerAdd_KeyDown;

            // Agar constructor se aap _customerName / _dealerName la rahe ho, unko set kar dein
            if (!string.IsNullOrEmpty(_customerName))
            {
                SetComboBoxValue(cbCustomer, _customerName);
            }
            if (!string.IsNullOrEmpty(_dealerName))
            {
                SetComboBoxValue(cbDealer, _dealerName);
            }

            // Sab ke baad agar editing mode ho, to record load karo
            if (LedgerID > 0)
            {
                LoadLedgerDetails(LedgerID);
            }
        }

        // ==========================
        // HOTKEYS FOR TEXTBOX/COMBO
        // ==========================
        private void frmCustomerToCustomerAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1)
                {
                    txtCustomer.Visible = true;
                    txtDealer.Visible = true;
                    cbCustomer.Visible = false;
                    cbDealer.Visible = false;
                    txtCustomer.Focus();
                }
                else if (e.KeyCode == Keys.D2)
                {
                    txtCustomer.Visible = false;
                    txtDealer.Visible = false;
                    cbCustomer.Visible = true;
                    cbDealer.Visible = true;
                    cbCustomer.Focus();
                }
            }
        }

        // =====================================
        // LOAD RECORD (for EDIT mode)
        // =====================================
        private void LoadLedgerDetails(int ledgerID)
        {
            string query = "SELECT * FROM CustomerToCustomer WHERE LedgerID = @LedgerID";
            Hashtable ht = new Hashtable();
            ht.Add("@LedgerID", ledgerID);

            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt != null && dt.Rows.Count > 0)
            {
                // Combo fill pehle hi ho chuki hogi
                int customerId = Convert.ToInt32(dt.Rows[0]["id"]);
                int dealerId = Convert.ToInt32(dt.Rows[0]["Did"]);

                cbCustomer.SelectedValue = customerId;
                cbDealer.SelectedValue = dealerId;

                txtAmountPaid.Text = dt.Rows[0]["AmounGiven"].ToString();
                dtpPaymentDate.Value = Convert.ToDateTime(dt.Rows[0]["Date"]);
                txtNote.Text = dt.Rows[0]["Note"].ToString();

                // Show balances
                ShowCustomerBalance(customerId);
                ShowDealerBalance(dealerId);

                // NAYA CODE:
                // store old scenario for "undo" if user changes this record
                _oldCustomerId = customerId;
                _oldDealerId = dealerId;
                _oldAmountPaid = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);
                _oldDate = Convert.ToDateTime(dt.Rows[0]["Date"]);
            }
            else
            {
                MessageBox.Show("No details found for the selected record.");
            }
        }

        // ==========================
        // SAVE BUTTON
        // ==========================
        protected override void btnSave_Click(object sender, EventArgs e)
        {
            if (cbCustomer.SelectedIndex == -1 || cbDealer.SelectedIndex == -1)
            {
                MessageBox.Show("Please select both a customer and a dealer.");
                return;
            }
            if (!decimal.TryParse(txtAmountPaid.Text.Replace(",", ""), out decimal newAmountPaid) || newAmountPaid <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.");
                return;
            }

            DateTime newDate = dtpPaymentDate.Value;
            int newCustomerId = Convert.ToInt32(cbCustomer.SelectedValue);
            int newDealerId = Convert.ToInt32(cbDealer.SelectedValue);

            decimal netDealerDeduction = BalanceConfirmationService.NetDeduction(
                newAmountPaid, _oldAmountPaid, LedgerID > 0 && newDealerId == _oldDealerId);
            if (!BalanceConfirmationService.ConfirmCustomerLedgerDeduction(newDealerId, netDealerDeduction, cbDealer.Text))
                return;

            try
            {
                // Agar naya record hai
                if (LedgerID == 0)
                {
                    // Sidha naya scenario insert ho jaye
                    SaveLedgerEntry(0, newCustomerId.ToString(), newDealerId.ToString(), newAmountPaid, txtNote.Text, newDate);
                }
                else
                {
                    // ====================
                    // EDIT MODE
                    // ====================

                    // 1) Undo old scenario in PetrolAdd
                    UndoOldScenario(_oldCustomerId, _oldDealerId, _oldAmountPaid, _oldDate);

                    // 2) Ab naya scenario insert/update
                    SaveLedgerEntry(LedgerID, newCustomerId.ToString(), newDealerId.ToString(), newAmountPaid, txtNote.Text, newDate);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // ==========================
        // UNDO OLD SCENARIO (PetrolAdd se record hatana)
        // ==========================
        private void UndoOldScenario(int oldCustomerId, int oldDealerId, decimal oldAmountPaid, DateTime oldDate)
        {
            // Essentially yeh code 'btnDel_Click' jaisa hai, 
            // lekin hum CustomerToCustomer se row delete nahin karna chahte 
            // (kyunke hum sirf old petrolAdd entries hata rahe hain).
            // Bas unhi 2 rows ko remove kar dein jo oldCustomer ki (IsInitialEntry=1) 
            // aur oldDealer ki (IsInitialEntry=0) humnein bnayi theen.

            // 1) Customer ki balance entry remove
            string dateStr = oldDate.ToString("yyyy-MM-dd");

            string deleteCust = @"
                DELETE FROM PetrolAdd 
                WHERE CustomerId = @CustomerId
                  AND ReceiptNo = 'CustomerToCustomer'
                  AND Date = @PaymentDate
                  AND IsInitialEntry = 1
            ";
            Hashtable htCust = new Hashtable();
            htCust.Add("@CustomerId", oldCustomerId);
            htCust.Add("@PaymentDate", dateStr);
            MainClass.DataInsertUpdateDelete(deleteCust, htCust);

            // 2) Dealer ki credit entry remove
            string deleteDealer = @"
                DELETE FROM PetrolAdd
                WHERE CustomerId = @DealerId
                  AND ReceiptNo = 'CustomerToCustomer'
                  AND Date = @PaymentDate
                  AND IsInitialEntry = 0
            ";
            Hashtable htDealer = new Hashtable();
            htDealer.Add("@DealerId", oldDealerId);
            htDealer.Add("@PaymentDate", dateStr);
            MainClass.DataInsertUpdateDelete(deleteDealer, htDealer);

            // ab hum CustomerToCustomer se row nahin hata rahe 
            // kyunke hum update kar rahe hain, poora row delete nahin karna
        }

        // ==========================
        // INSERT / UPDATE SCENARIO
        // ==========================
        private void SaveLedgerEntry(int ledgerId, string customerId, string dealerId, decimal amountPaid, string note, DateTime paymentDate)
        {
            // Pura method waisa hi jaisa aapke code me tha. 
            // Pehle PetrolAdd me 2 entries (ek customer ke liye IsInitialEntry=1, ek dealer ke liye IsInitialEntry=0),
            // phir CustomerToCustomer table me insert/update.

            // 1) Insert balance entry for customer
            decimal newBalance = amountPaid;
            string insertBalanceQuery = @"
                INSERT INTO PetrolAdd 
                (CustomerId, Date, ReceiptNo, vehicle, Amount, Advance, Credit, Balance, Note, IsInitialEntry) 
                VALUES 
                (@customerId, @paymentDate, @receiptNo, @vehicle, @amountPaid, 0, 0, @newBalance, @note, 1)
            ";

            Hashtable htInsertBalance = new Hashtable
            {
                { "@customerId",  customerId },
                { "@paymentDate", paymentDate.ToString("yyyy-MM-dd") },
                { "@receiptNo",   "CustomerToCustomer" },
                { "@vehicle",     "" },
                { "@amountPaid",  amountPaid },
                { "@newBalance",  newBalance },
                { "@note",        note }
            };

            int balanceInsertResult = MainClass.DataInsertUpdateDelete(insertBalanceQuery, htInsertBalance);
            if (balanceInsertResult <= 0)
            {
                MessageBox.Show("Error saving balance entry to PetrolAdd.");
                return;
            }

            // 2) Insert credit entry for dealer
            string insertDealerCreditQuery = @"
                INSERT INTO PetrolAdd 
                (CustomerId, Date, ReceiptNo, vehicle, Litter, Rate, Amount, Advance, Credit, Balance, Note, IsInitialEntry) 
                VALUES 
                (@dealerId, @paymentDate, @receiptNo, @vehicle, 0, 0, @amount, 0, @amountPaid, 0, @note, 0)
            ";

            Hashtable htInsertDealer = new Hashtable
            {
                { "@dealerId",    dealerId },
                { "@paymentDate", paymentDate.ToString("yyyy-MM-dd") },
                { "@receiptNo",   "CustomerToCustomer" },
                { "@vehicle",     "" },
                { "@amount",      0.0m },
                { "@amountPaid",  amountPaid },
                { "@note",        note }
            };

            int dealerInsertResult = MainClass.DataInsertUpdateDelete(insertDealerCreditQuery, htInsertDealer);
            if (dealerInsertResult <= 0)
            {
                MessageBox.Show("Error inserting dealer credit in PetrolAdd.");
                return;
            }

            // 3) Ab CustomerToCustomer table me insert/update
            SaveCustomerToCustomer(ledgerId, customerId, dealerId, amountPaid, note, paymentDate);
        }

        private void SaveCustomerToCustomer(int ledgerId, string customerId, string dealerId, decimal amountPaid, string note, DateTime paymentDate)
        {
            string customerName = cbCustomer.Text;
            string dealerName = cbDealer.Text;

            Hashtable ht = new Hashtable
            {
                { "@customerId",    customerId },
                { "@dealerId",      dealerId },
                { "@amountPaid",    amountPaid },
                { "@paymentDate",   paymentDate.ToString("yyyy-MM-dd") },
                { "@note",          note },
                { "@customerName",  customerName },
                { "@dealerName",    dealerName }
            };

            string query = (ledgerId == 0)
                ? @"INSERT INTO CustomerToCustomer 
                   (Did, AmounGiven, Date, id, Note, CustomerName, DealerName) 
                   VALUES 
                   (@dealerId, @amountPaid, @paymentDate, @customerId, @note, @customerName, @dealerName)"
                : @"UPDATE CustomerToCustomer 
                   SET Did = @dealerId, 
                       AmounGiven = @amountPaid, 
                       Date = @paymentDate, 
                       id = @customerId, 
                       Note = @note, 
                       CustomerName = @customerName, 
                       DealerName = @dealerName
                   WHERE LedgerID = @LedgerID";

            if (ledgerId != 0)
            {
                ht.Add("@LedgerID", ledgerId);
            }

            int result = MainClass.DataInsertUpdateDelete(query, ht);
            if (result > 0)
            {
                CustomeMessage msg = new CustomeMessage("Entry saved successfully!", "Save");
                msg.ShowDialog();
            }
            else
            {
                MessageBox.Show("Error saving data to CustomerToCustomer.");
            }
        }

        // ==========================
        // DELETE BUTTON
        // ==========================
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (LedgerID <= 0)
            {
                CustomeMessage msg = new CustomeMessage("Koi record select nahi kiya gaya hai!", "Warning");
                msg.ShowDialog();
                return;
            }

            YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
            if (confirmDelete.ShowDialog() != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Step 1: fetch
                string fetchQuery = "SELECT id, Did, Date FROM CustomerToCustomer WHERE LedgerID = @LedgerID";
                Hashtable htFetch = new Hashtable { { "@LedgerID", LedgerID } };
                DataTable dt = MainClass.ExecuteSelectQuery(fetchQuery, htFetch);

                if (dt == null || dt.Rows.Count == 0)
                {
                    CustomeMessage err = new CustomeMessage("Ledger entry nahi mili!", "Error");
                    err.ShowDialog();
                    return;
                }

                int customerId = Convert.ToInt32(dt.Rows[0]["id"]);
                int dealerId = Convert.ToInt32(dt.Rows[0]["Did"]);
                string paymentDate = Convert.ToDateTime(dt.Rows[0]["Date"]).ToString("yyyy-MM-dd");

                // Step 2: remove from PetrolAdd (customer's entry)
                string deleteCustomerBalanceQuery = @"
                    DELETE FROM PetrolAdd
                    WHERE CustomerId = @CustomerId
                      AND ReceiptNo = 'CustomerToCustomer'
                      AND Date = @PaymentDate
                      AND IsInitialEntry = 1
                ";
                Hashtable htCust = new Hashtable
                {
                    { "@CustomerId",  customerId },
                    { "@PaymentDate", paymentDate }
                };
                MainClass.DataInsertUpdateDelete(deleteCustomerBalanceQuery, htCust);

                // Step 3: remove from PetrolAdd (dealer's entry)
                string deleteDealerCreditQuery = @"
                    DELETE FROM PetrolAdd
                    WHERE CustomerId = @DealerId
                      AND ReceiptNo = 'CustomerToCustomer'
                      AND Date = @PaymentDate
                      AND IsInitialEntry = 0
                ";
                Hashtable htDealer = new Hashtable
                {
                    { "@DealerId",  dealerId },
                    { "@PaymentDate", paymentDate }
                };
                MainClass.DataInsertUpdateDelete(deleteDealerCreditQuery, htDealer);

                // Step 4: remove from CustomerToCustomer
                string deleteLedgerQuery = "DELETE FROM CustomerToCustomer WHERE LedgerID = @LedgerID";
                Hashtable htLedger = new Hashtable { { "@LedgerID", LedgerID } };
                int ledgerDeleteResult = MainClass.DataInsertUpdateDelete(deleteLedgerQuery, htLedger);
                if (ledgerDeleteResult <= 0)
                {
                    CustomeMessage err2 = new CustomeMessage("Ledger entry delete nahi hui!", "Error");
                    err2.ShowDialog();
                    return;
                }

                CustomeMessage success = new CustomeMessage("Record aur related entries delete ho gayi!", "Success");
                success.ShowDialog();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                CustomeMessage err = new CustomeMessage("Error: " + ex.Message, "Error");
                err.ShowDialog();
            }
        }

        // =====================================
        // COMBOBOX / TEXTBOX / BALANCE Methods
        // =====================================
        private void cbCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCustomer.SelectedItem != null && cbCustomer.SelectedItem is DataRowView drv)
            {
                int customerId = Convert.ToInt32(drv["id"]);
                ShowCustomerBalance(customerId);
            }
        }

        private void cbDealer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDealer.SelectedItem != null && cbDealer.SelectedItem is DataRowView drv)
            {
                int dealerId = Convert.ToInt32(drv["id"]);
                ShowDealerBalance(dealerId);
            }
        }

        private void txtCustomer_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtCustomer.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt = cbCustomer.DataSource as DataTable;
                if (dt != null)
                {
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
            }
            else
            {
                cbCustomer.SelectedIndex = -1;
            }
        }

        private void txtDealer_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtDealer.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt = cbDealer.DataSource as DataTable;
                if (dt != null)
                {
                    string filter = $"Name = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        cbDealer.SelectedValue = rows[0]["id"];
                    }
                    else
                    {
                        cbDealer.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                cbDealer.SelectedIndex = -1;
            }
        }

        private void SetComboBoxValue(ComboBox comboBox, string name)
        {
            if (comboBox.DataSource != null)
            {
                foreach (DataRowView item in comboBox.Items)
                {
                    if (item["Name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedValue = item["id"];
                        return;
                    }
                }
                // agar name nahin mila
                comboBox.Text = name;
            }
        }

        private void PopulateCustomerNames()
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

                AutoCompleteStringCollection autoSource = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    autoSource.Add(row["Name"].ToString());
                }
                txtCustomer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCustomer.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCustomer.AutoCompleteCustomSource = autoSource;

            }
        }

        private void PopulateDealerNames()
        {
            string query = "SELECT id, Name FROM AddCustomer";
            DataTable dt = MainClass.ExecuteSelectQuery(query, null);
            if (dt != null && dt.Rows.Count > 0)
            {
                cbDealer.DataSource = dt;
                cbDealer.DisplayMember = "Name";
                cbDealer.ValueMember = "id";
                cbDealer.SelectedIndex = -1;


                cbDealer.DropDownHeight = 200;
                cbDealer.IntegralHeight = false;

                AutoCompleteStringCollection autoSource = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    autoSource.Add(row["Name"].ToString());
                }
                txtDealer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtDealer.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtDealer.AutoCompleteCustomSource = autoSource;
            }
        }

        private void ShowCustomerBalance(int customerId)
        {
            string query = "SELECT SUM(Balance) AS InitialBalance FROM PetrolAdd WHERE CustomerId = @customerId AND IsInitialEntry = 1";
            Hashtable ht = new Hashtable { { "@customerId", customerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

            decimal initialBalance = 0;
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["InitialBalance"] != DBNull.Value)
            {
                initialBalance = Convert.ToDecimal(dt.Rows[0]["InitialBalance"]);
            }

            string creditQuery = "SELECT SUM(Credit) AS TotalCredit FROM PetrolAdd WHERE CustomerId = @customerId AND IsInitialEntry = 0";
            DataTable dtCredit = MainClass.ExecuteSelectQuery(creditQuery, ht);

            decimal totalCredit = 0;
            if (dtCredit != null && dtCredit.Rows.Count > 0 && dtCredit.Rows[0]["TotalCredit"] != DBNull.Value)
            {
                totalCredit = Convert.ToDecimal(dtCredit.Rows[0]["TotalCredit"]);
            }

            decimal remainingBalance = initialBalance - totalCredit;
            customerlbl.Text = $"Remaining Balance: {remainingBalance:F2}";
        }

        private void ShowDealerBalance(int dealerId)
        {
            string query = "SELECT SUM(Balance) AS InitialBalance FROM PetrolAdd WHERE CustomerId = @dealerId AND IsInitialEntry = 1";
            Hashtable ht = new Hashtable { { "@dealerId", dealerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

            decimal initialBalance = 0;
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["InitialBalance"] != DBNull.Value)
            {
                initialBalance = Convert.ToDecimal(dt.Rows[0]["InitialBalance"]);
            }

            string creditQuery = "SELECT SUM(Credit) AS TotalCredit FROM PetrolAdd WHERE CustomerId = @dealerId AND IsInitialEntry = 0";
            DataTable dtCredit = MainClass.ExecuteSelectQuery(creditQuery, ht);

            decimal totalCredit = 0;
            if (dtCredit != null && dtCredit.Rows.Count > 0 && dtCredit.Rows[0]["TotalCredit"] != DBNull.Value)
            {
                totalCredit = Convert.ToDecimal(dtCredit.Rows[0]["TotalCredit"]);
            }

            decimal remainingBalance = initialBalance - totalCredit;
            lblDealerBalance.Text = $"Balance: {remainingBalance:F2}";
        }

        private void txtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            string currentText = txtAmountPaid.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                txtAmountPaid.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                txtAmountPaid.SelectionStart = txtAmountPaid.Text.Length;
            }
        }

        private void txtAmountPaid_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
                CustomeMessage customMessageBox = new CustomeMessage("یہاں صرف نمبر اور دہائی کا نشان (.) لکھ سکتے ہو!", "غلطی");
                customMessageBox.ShowDialog();
                return;
            }

            TextBox textBox = sender as TextBox;
            if (e.KeyChar == '.' && textBox != null && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void lblDealerBalance_TextChanged(object sender, EventArgs e)
        {
            // If you'd like to format the label also with commas, you can do so
            string currentText = lblDealerBalance.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                lblDealerBalance.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
            }
        }
    }
}
