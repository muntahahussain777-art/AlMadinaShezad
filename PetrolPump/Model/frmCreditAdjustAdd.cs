using Guna.UI2.WinForms;
using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmCreditAdjustAdd : SampleAdd1
    {
        private decimal totalCredit = 0;
        private decimal totalAmount = 0;
        private decimal _loadedCredit = 0; // edit pe purana credit (confirm + balance sahi)

        public int id = 0;  // This ID is used to determine if we are editing an existing entry or creating a new one.
        private bool _savedInSession;

        public frmCreditAdjustAdd()
        {
            InitializeComponent();
        }

        private void frmCreditAdjustAdd_Load(object sender, EventArgs e)
        {
            txttotalCredit.TextChanged += new EventHandler(txttotalCredit_TextChanged);
            txtcredit.TextChanged += new EventHandler(txtcredit_TextChanged);
            PopulateCustomerNames();
            LoadExistingData();
            if (id == 0)
                txtdate.Value = DateTime.Now;
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(frmCreditAdjustAdd_KeyDown);
            txtName.KeyDown += new KeyEventHandler(txtName_KeyDown);

            this.FormClosing -= frmCreditAdjustAdd_FormClosing;
            this.FormClosing += frmCreditAdjustAdd_FormClosing;

            // YAHAN NAYA LINE ADD KAR DAIN:
            txtName.TextChanged += new EventHandler(txtName_TextChanged);
        }

        private void frmCreditAdjustAdd_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_savedInSession && DialogResult == DialogResult.None)
                DialogResult = DialogResult.OK;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            // Jo bhi user ne type kiya hai, use trim kar lein:
            string enteredName = txtName.Text.Trim();

            // Agar user ne kuch likha hai:
            if (!string.IsNullOrEmpty(enteredName))
            {
                // cbName.DataSource as DataTable:
                DataTable dt = cbName.DataSource as DataTable;
                if (dt != null)
                {
                    // DataRow[] se match dhoondein. 
                    // Note: single quotes handle karne ke liye Replace kar rahe hain:
                    string filter = $"Name = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        // Agar match mil gaya to us customer ka ID select kar dein:
                        cbName.SelectedValue = rows[0]["Id"];
                    }
                    else
                    {
                        // Agar match nahin mila to reset kar dein:
                        cbName.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                // Agar text box khali ho gaya, to combobox bhi reset kar dein:
                cbName.SelectedIndex = -1;
            }
        }

        // Public controls to access them from other forms
        public ComboBox cbDealer;
        public ComboBox cbCustomer;
        private void frmCreditAdjustAdd_KeyDown(object sender, KeyEventArgs e)
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
                    btnSave.PerformClick(); // Save on Ctrl + S
                }
            }
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string customerName = txtName.Text;

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    string query = "SELECT Id FROM AddCustomer WHERE Name = @customerName";
                    Hashtable ht = new Hashtable();
                    ht.Add("@customerName", customerName);

                    DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        int customerId = Convert.ToInt32(dt.Rows[0]["Id"]);
                        UpdateCustomerCreditAndBalance(customerId, id > 0 ? id : 0);
                        cbName.SelectedValue = customerId; // Sync ComboBox with selected customer ID
                    }
                    else
                    {
                        CustomeMessage customMessageBox = new CustomeMessage("Customer not found.", "");
                        customMessageBox.ShowDialog();
                        txttotalCredit.Text = "0.00"; // Reset balance if customer is not found
                    }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void LoadExistingData()
        {
            if (id > 0)
            {
                string query = @"SELECT Date, ReceiptNo, Credit, Note, IsInitialEntry, CustomerId FROM PetrolAdd WHERE pid = @id";
                Hashtable ht = new Hashtable();
                ht.Add("@id", id);

                DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["Date"] != DBNull.Value &&
                        DateTime.TryParse(dt.Rows[0]["Date"].ToString(), out DateTime loadedDate))
                        txtdate.Value = loadedDate;

                    txtrecipt.Text = dt.Rows[0]["ReceiptNo"].ToString();
                    _loadedCredit = 0m;
                    if (dt.Rows[0]["Credit"] != DBNull.Value)
                        decimal.TryParse(dt.Rows[0]["Credit"].ToString().Replace(",", ""), out _loadedCredit);
                    txtcredit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", _loadedCredit);
                    txtnote.Text = dt.Rows[0]["Note"].ToString();

                    int customerId = Convert.ToInt32(dt.Rows[0]["CustomerId"]);
                    cbName.SelectedValue = customerId; // Customer ID set karo

                    // Sale entry (IsInitialEntry=1) yahan edit nahi — Daily Diesel sale form kholta hai
                    int isInitial = 1;
                    if (dt.Rows[0]["IsInitialEntry"] != DBNull.Value)
                        isInitial = Convert.ToInt32(dt.Rows[0]["IsInitialEntry"]);
                    if (isInitial != 0)
                    {
                        btnSave.Enabled = false;
                        btnDel.Enabled = false;
                        CustomeMessage customMessageBox = new CustomeMessage("یہ اندراج پہلی فارم میں بنایا گیا تھا اور اسے یہاں تبدیل نہیں کیا جا سکتا۔", "INFORMATION");
                        customMessageBox.ShowDialog();
                    }

                    // Manually event trigger karo agar zaroorat ho
                    if (cbName.SelectedValue != null)
                    {
                        cbName_SelectedIndexChanged(cbName, EventArgs.Empty);
                    }
                }
            }
        }
        private void PopulateCustomerNames()
        {
            string query = "SELECT Id, Name FROM AddCustomer";
            DataTable dt = MainClass.ExecuteSelectQuery(query, null);
            cbName.DropDownHeight = 200; // Adjust height
            cbName.IntegralHeight = false;

            if (dt != null && dt.Rows.Count > 0)
            {
                cbName.DataSource = null; // Pehle clear karo taake duplicates na hon
                cbName.DataSource = dt;
                cbName.DisplayMember = "Name"; // Customer name
                cbName.ValueMember = "Id"; // Customer ID

                AutoCompleteStringCollection customerNames = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    customerNames.Add(row["Name"].ToString());
                }

                txtName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtName.AutoCompleteCustomSource = customerNames;

                // Event ko ensure karo
                cbName.SelectedIndexChanged -= cbName_SelectedIndexChanged; // Pehle remove karo
                cbName.SelectedIndexChanged += cbName_SelectedIndexChanged; // Phir add karo
            }
            else
            {
                CustomeMessage customMessageBox = new CustomeMessage("Customer not found.", "");
                customMessageBox.ShowDialog();
                cbName.DataSource = null;
                txtName.AutoCompleteCustomSource = null;
            }
        }

        private void cbName_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("cbName_SelectedIndexChanged called"); // Debugging ke liye
            if (cbName.SelectedValue != null)
            {
                Console.WriteLine($"SelectedValue Type: {cbName.SelectedValue.GetType()}, Value: {cbName.SelectedValue}");

                // Agar SelectedValue already integer hai
                if (cbName.SelectedValue is int customerId)
                {
                    UpdateCustomerCreditAndBalance(customerId, id > 0 ? id : 0);
                }
                // Agar SelectedValue string ya koi aur type hai jo integer mein convert ho sakta hai
                else if (int.TryParse(cbName.SelectedValue.ToString(), out customerId))
                {
                    UpdateCustomerCreditAndBalance(customerId, id > 0 ? id : 0);
                }
                else
                {
                    // Yeh message tab hi show karo jab debugging ke liye zaroorat ho
                    Console.WriteLine("SelectedValue integer mein convert nahi ho saka.");
                    // MessageBox.Show("SelectedValue integer mein convert nahi ho saka."); // Isay comment kar do
                }
            }
            else
            {
                Console.WriteLine("cbName.SelectedValue null hai");
            }
        }
        private void UpdateCustomerCreditAndBalance(int customerId)
        {
            UpdateCustomerCreditAndBalance(customerId, 0);
        }

        private void UpdateCustomerCreditAndBalance(int customerId, int excludePid)
        {
            // Amount - Advance, Credit minus — Daily Diesel ke sath same (advance minus)
            string query = excludePid > 0
                ? @"SELECT SUM(IFNULL(Amount, 0) - IFNULL(Advance, 0)) AS TotalAmount,
                            SUM(IFNULL(Credit, 0)) AS TotalCredit
                     FROM PetrolAdd
                     WHERE CustomerId = @customerId AND pid <> @excludePid"
                : @"SELECT SUM(IFNULL(Amount, 0) - IFNULL(Advance, 0)) AS TotalAmount,
                            SUM(IFNULL(Credit, 0)) AS TotalCredit
                     FROM PetrolAdd
                     WHERE CustomerId = @customerId";

            Hashtable ht = new Hashtable();
            ht.Add("@customerId", customerId);
            if (excludePid > 0)
                ht.Add("@excludePid", excludePid);

            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

            if (dt.Rows.Count > 0)
            {
                totalAmount = dt.Rows[0]["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["TotalAmount"]) : 0;
                totalCredit = dt.Rows[0]["TotalCredit"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["TotalCredit"]) : 0;

                decimal result = totalAmount - totalCredit;
                if (excludePid > 0 && decimal.TryParse(txtcredit.Text.Replace(",", ""), out decimal editingCredit))
                    result -= editingCredit;
                txttotalCredit.Text = result.ToString("F2");
            }
            else
            {
                txttotalCredit.Text = "0.00";
            }
        }

        protected override void btnSave_Click(object sender, EventArgs e)
        {
            if (!btnSave.Enabled)
            {
                CustomeMessage customMessageBox = new CustomeMessage("This entry cannot be modified.", "");
                customMessageBox.ShowDialog();
                return;
            }

            if (!decimal.TryParse(txtcredit.Text.Replace(",", ""), out decimal credit))
            {
                CustomeMessage customMessageBox = new CustomeMessage("Invalid credit value.", "");
                customMessageBox.ShowDialog();
                return;
            }

            DateTime specifiedDate;
            if (!DateTime.TryParse(txtdate.Text, out specifiedDate))
            {
                CustomeMessage customMessageBox = new CustomeMessage("Invalid date value.", "");
                customMessageBox.ShowDialog();
                return;
            }

            if (cbName.SelectedValue == null || !int.TryParse(cbName.SelectedValue.ToString(), out int customerId))
            {
                CustomeMessage customMessageBox = new CustomeMessage("Invalid customer ID selected.", "");
                customMessageBox.ShowDialog();
                return;
            }

            bool isEdit = id > 0;

            // Balance: edit pe is row ko SUM se hatao, phir naya credit lagao
            UpdateCustomerCreditAndBalance(customerId, isEdit ? id : 0);
            decimal updatedBalance = totalAmount - (totalCredit + credit);

            // Confirm sirf net naya deduction (edit pe purana credit minus)
            decimal confirmCredit = isEdit
                ? BalanceConfirmationService.NetDeduction(credit, _loadedCredit, true)
                : credit;
            string customerLabel = string.IsNullOrWhiteSpace(txtName.Text) ? cbName.Text : txtName.Text;
            if (!BalanceConfirmationService.ConfirmCustomerPetrolCredit(customerId, confirmCredit, customerLabel))
                return;

            Hashtable ht = new Hashtable
            {
                { "@customerId", customerId },
                { "@date", specifiedDate.ToString("yyyy-MM-dd") },
                { "@receiptNo", txtrecipt.Text },
                { "@credit", credit },
                { "@balance", updatedBalance },
                { "@note", txtnote.Text }
            };

            string qry;
            if (isEdit)
            {
                // Sirf credit entry (IsInitialEntry=0) — sale/doosri row touch nahi
                ht.Add("@pid", id);
                qry = @"UPDATE PetrolAdd SET CustomerId=@customerId, Date=@date, ReceiptNo=@receiptNo,
                        Credit=@credit, Balance=@balance, Note=@note
                        WHERE pid=@pid AND IFNULL(IsInitialEntry,1)=0";
            }
            else
            {
                qry = @"INSERT INTO PetrolAdd (CustomerId, Date, ReceiptNo, Credit, Balance, Note, IsInitialEntry)
            VALUES (@customerId, @date, @receiptNo, @credit, @balance, @note, 0)";
            }

            try
            {
                int resultSave = MainClass.DataInsertUpdateDelete(qry, ht);
                if (resultSave > 0)
                {
                    CustomeMessage customMessageBox = new CustomeMessage(
                        isEdit ? "Record update ho gaya." : "Record saved successfully.", "");
                    customMessageBox.ShowDialog();

                    if (isEdit)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }

                    object lastCustomerId = cbName.SelectedValue;
                    string lastCustomerName = txtName.Text.Trim();
                    if (string.IsNullOrEmpty(lastCustomerName) && cbName.SelectedIndex >= 0)
                        lastCustomerName = cbName.Text;

                    txtName.TextChanged -= txtName_TextChanged;
                    MainClass.Enable_reset_keep_date(this, txtdate);
                    id = 0;
                    _loadedCredit = 0;

                    if (lastCustomerId != null && cbName.DataSource != null)
                    {
                        try
                        {
                            cbName.SelectedValue = Convert.ToInt32(lastCustomerId);
                        }
                        catch
                        {
                            try { cbName.SelectedValue = lastCustomerId; } catch { /* ignore */ }
                        }
                    }

                    if (!string.IsNullOrEmpty(lastCustomerName))
                        txtName.Text = lastCustomerName;

                    txtName.TextChanged += txtName_TextChanged;

                    if (cbName.SelectedValue != null &&
                        int.TryParse(cbName.SelectedValue.ToString(), out int refreshedCustomerId))
                        UpdateCustomerCreditAndBalance(refreshedCustomerId);

                    txtcredit.Focus();

                    _savedInSession = true;
                }
                else
                {
                    MessageBox.Show(isEdit
                        ? "Update nahi hua (ye sale entry ho sakti hai — Daily Diesel se dobara double-click karein)."
                        : "Error saving data.");
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"SQL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (btnDel.Enabled == false)
            {
                CustomeMessage customMessageBox = new CustomeMessage("This entry cannot be deleted.", "Warning");
                customMessageBox.ShowDialog();
                return;
            }

            if (id > 0)
            {
                // Custom Yes/No confirmation dialog
                YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                if (confirmDelete.ShowDialog() == DialogResult.Yes) // Yes/No check
                {
                    try
                    {
                        // Parameterized query for deletion
                        string qry = "DELETE FROM PetrolAdd WHERE pid = @pid";
                        Hashtable ht = new Hashtable();
                        ht.Add("@pid", id);

                        // Delete operation
                        int resultDelete = MainClass.DataInsertUpdateDelete(qry, ht);

                        if (resultDelete > 0)
                        {
                            CustomeMessage successMessage = new CustomeMessage("Record delete ho gaya!", "Success");
                            successMessage.ShowDialog();
                            this.DialogResult = DialogResult.OK; // Signal that the data was deleted
                            this.Close(); // Close the form after deletion
                        }
                        else
                        {
                            CustomeMessage errorMessage = new CustomeMessage("Record delete nahi hua!", "Error");
                            errorMessage.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomeMessage errorMessage = new CustomeMessage("Error: " + ex.Message, "Error");
                        errorMessage.ShowDialog();
                    }
                }
            }
        }

        private void txttotalCredit_TextChanged(object sender, EventArgs e)
        {
            string currentText = txttotalCredit.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                txttotalCredit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                txttotalCredit.SelectionStart = txttotalCredit.Text.Length;
            }
        }

        private void txtcredit_TextChanged(object sender, EventArgs e)
        {
            string currentText = txtcredit.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                txtcredit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);
                txtcredit.SelectionStart = txtcredit.Text.Length;
            }
        }

        private void txttotalCredit_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtcredit_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow control characters (like backspace), digits, and one decimal point
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true; // Block invalid input
                CustomeMessage customMessageBox = new CustomeMessage("یہاں صرف نمبر اور دہائی کا نشان (.) لکھ سکتے ہو!", "غلطی");
                customMessageBox.ShowDialog();
                return;
            }

            // Block multiple decimal points
            TextBox textBox = sender as TextBox;
            if (e.KeyChar == '.' && textBox != null && textBox.Text.Contains("."))
            {
                e.Handled = true; // Block if a decimal point already exists
            }
        }
        public void SetCustomerName(string customerName)
        {
            Console.WriteLine($"SetCustomerName called with: {customerName}");
            cbName.SelectedIndexChanged -= cbName_SelectedIndexChanged;

            if (!string.IsNullOrEmpty(customerName))
            {
                bool found = false;
                foreach (DataRowView item in cbName.Items)
                {
                    if (item["Name"].ToString() == customerName)
                    {
                        cbName.SelectedValue = item["Id"];
                        found = true;
                        Console.WriteLine($"Found matching name: {customerName}, SelectedValue set to: {cbName.SelectedValue}");
                        break;
                    }
                }
                if (!found)
                {
                    cbName.Text = customerName;
                    Console.WriteLine($"Name not found in items, Text set to: {customerName}");
                }
            }

            cbName.SelectedIndexChanged += cbName_SelectedIndexChanged;

            if (cbName.SelectedValue != null)
            {
                Console.WriteLine($"Triggering cbName_SelectedIndexChanged with SelectedValue: {cbName.SelectedValue}");
                cbName_SelectedIndexChanged(cbName, EventArgs.Empty);
            }
        }
    }
}
