using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using AlMadinaShahzad;
using AlMadinaShahzad.Model; // Agar zaroorat ho namespace ke liye
using AlMadinaShahzad.Services;

namespace DigiKhataApp
{
    public partial class frmBankTransactionAdd : SampleAdd
    {
        // Agar edit karna ho to id set hogi, warna new entry
        public int id = 0;

        public frmBankTransactionAdd()
        {
            InitializeComponent();
            this.Load += new EventHandler(frmBankTransactionAdd_Load);

            // ComboBoxes load
            LoadCustomers();
            LoadDealers();

            // Transaction type
            cbTransactionType.Items.Clear();
            cbTransactionType.Items.Add("In");
            cbTransactionType.Items.Add("Out");
            cbTransactionType.SelectedIndex = 0;
            LoadBanks(); // [NAYA CODE]

            // Form key events
            this.KeyPreview = true;                   // [NAYA CODE]
            this.KeyDown += FrmBankTransactionAdd_KeyDown; // [NAYA CODE]

            // TextBoxes by default hidden
            txtCustomerName.Visible = false; // [NAYA CODE]
            txtDealerName.Visible = false;   // [NAYA CODE]

            // TextChanged events for the textboxes
            txtCustomerName.TextChanged += txtCustomerName_TextChanged; // [NAYA CODE]
            txtDealerName.TextChanged += txtDealerName_TextChanged;   // [NAYA CODE]
        }


        // Form load event – agar id > 0, to record load karein for update
        private void frmBankTransactionAdd_Load(object sender, EventArgs e)
        {
            if (id > 0)
            {
                LoadRecord();
            }
        }

        // Record load karne ka method, jismein database se data fetch karke controls populate ho jayenge
        private void LoadRecord()
        {
            Hashtable ht = new Hashtable();
            ht.Add("@id", id);
            string qry = "SELECT * FROM BankTransactions WHERE Id = @id";
            DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                // Pehle se existing code
                if (DateTime.TryParse(row["TransactionDate"].ToString(), out DateTime transDate))
                    dtTransaction.Value = transDate;
                string transType = row["TransactionType"].ToString();
                cbTransactionType.SelectedItem = transType;

                if (row["CustomerId"] != DBNull.Value)
                    cbCustomer.SelectedValue = row["CustomerId"];
                if (row["DealerId"] != DBNull.Value)
                    cbDealer.SelectedValue = row["DealerId"];

                txtAmount.Text = row["Amount"].ToString();
                txtNote.Text = row["Note"].ToString();

                // [NAYA CODE ↓↓]
                // BankName ko load karke combobox par set kar dein
                if (row["BankName"] != DBNull.Value)
                {
                    string bankNameFromDB = row["BankName"].ToString();
                    // Agar wo aapki list mein maujood hai
                    if (cbBankName.Items.Contains(bankNameFromDB))
                    {
                        cbBankName.SelectedItem = bankNameFromDB;
                    }
                    else
                    {
                        // optional: agar list mein nahin hai, to aap .Text hi set kar sakte hain
                        cbBankName.Text = bankNameFromDB;
                    }
                }
                // [NAYA CODE ↑↑]
            }
        }

        // Existing methods for loading customers and dealers, saving record, etc.
        private void LoadCustomers()
        {
            string qry = "SELECT id, Name FROM AddCustomer";
            DataTable dt = MainClass.ExecuteSelectQuery(qry, null);
            if (dt != null && dt.Rows.Count > 0)
            {
                cbCustomer.DisplayMember = "Name";
                cbCustomer.ValueMember = "id";
                cbCustomer.DataSource = dt;
                cbCustomer.SelectedIndex = -1;

                // [NAYA CODE ↓↓ for txtCustomerName auto-complete]
                AutoCompleteStringCollection autoCust = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    autoCust.Add(row["Name"].ToString());
                }
                txtCustomerName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCustomerName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCustomerName.AutoCompleteCustomSource = autoCust;
                // [NAYA CODE ↑↑]
            }
            else
            {
                ErrorFormMessage customMessageBox = new ErrorFormMessage("Koi customer data nahi mila!", "Warning");
                customMessageBox.ShowDialog();
            }
        }

        private void LoadDealers()
        {
            string qry = "SELECT Did, DealerName FROM AddDealer";
            DataTable dt = MainClass.ExecuteSelectQuery(qry, null);
            if (dt != null && dt.Rows.Count > 0)
            {
                cbDealer.DisplayMember = "DealerName";
                cbDealer.ValueMember = "Did";
                cbDealer.DataSource = dt;
                cbDealer.SelectedIndex = -1;

                // [NAYA CODE ↓↓ for txtDealerName auto-complete]
                AutoCompleteStringCollection autoDealer = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    autoDealer.Add(row["DealerName"].ToString());
                }
                txtDealerName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtDealerName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtDealerName.AutoCompleteCustomSource = autoDealer;
                // [NAYA CODE ↑↑]
            }
            else
            {
                ErrorFormMessage customMessageBox = new ErrorFormMessage("Koi dealer data nahi mila!", "Warning");
                customMessageBox.ShowDialog();
            }
        }
        
        private void txtCustomerName_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtCustomerName.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt = cbCustomer.DataSource as DataTable;
                if (dt != null)
                {
                    // Single-quote handle
                    string filter = $"Name = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        cbCustomer.SelectedValue = rows[0]["id"];
                    }
                    else
                    {
                        cbCustomer.SelectedIndex = -1; // no match
                    }
                }
            }
            else
            {
                // user ne sab text hata dia
                cbCustomer.SelectedIndex = -1;
            }
        }
        private void txtDealerName_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtDealerName.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt = cbDealer.DataSource as DataTable;
                if (dt != null)
                {
                    // Single-quote handle
                    string filter = $"DealerName = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        cbDealer.SelectedValue = rows[0]["Did"];
                    }
                    else
                    {
                        cbDealer.SelectedIndex = -1; // no match
                    }
                }
            }
            else
            {
                cbDealer.SelectedIndex = -1;
            }
        }


        // Ctrl+S se save trigger hota hai
        private void FrmBankTransactionAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1)
                {
                    // Ctrl+1 → Dono TextBoxes show, Dono ComboBoxes hide
                    txtCustomerName.Visible = true;
                    txtDealerName.Visible = true;
                    txtBankName.Visible = true;   // [NAYA LINE]

                    cbCustomer.Visible = false;
                    cbDealer.Visible = false;
                    cbBankName.Visible = false;       // [NAYA LINE]

                    // Pehla focus – aapke marzi, yahan CustomerName pe
                    txtCustomerName.Focus();
                }
                else if (e.KeyCode == Keys.D2)
                {
                    // Ctrl+2 → Dono ComboBoxes show, Dono TextBoxes hide
                    txtCustomerName.Visible = false;
                    txtDealerName.Visible = false;
                    txtBankName.Visible = false;  // [NAYA LINE]

                    cbCustomer.Visible = true;
                    cbDealer.Visible = true;
                    cbBankName.Visible = true;        // [NAYA LINE]

                    // Focus wapas cbCustomer
                    cbCustomer.Focus();
                }
            }
        }
        private void txtBankName_TextChanged(object sender, EventArgs e)
        {
            string enteredBank = txtBankName.Text.Trim();
            if (!string.IsNullOrEmpty(enteredBank))
            {
                // cbBankName.DataSource is a string[] in your code
                // We can simply check if the typed name is EXACT in cbBankName.Items
                if (cbBankName.Items.Contains(enteredBank))
                {
                    cbBankName.SelectedItem = enteredBank;
                }
                else
                {
                    // No match
                    cbBankName.SelectedIndex = -1;
                }
            }
            else
            {
                // user ne sab text hata dia
                cbBankName.SelectedIndex = -1;
            }
        }



        // Save button click event – record insert/update logic
        public override void btnSave_Click(object sender, EventArgs e)
        {
            DateTime transactionDate = dtTransaction.Value;
            string transType = cbTransactionType.SelectedItem.ToString();

            decimal amount = 0;
            if (!decimal.TryParse(txtAmount.Text, out amount))
            {
                MessageBox.Show("Barah-e-Karam sahi amount daalein!", "Warning");
                return;
            }

            // Agar "Out" select ho to amount negative, agar "In" to positive store karein
            if (transType == "Out")
                amount = -Math.Abs(amount);
            else
                amount = Math.Abs(amount);

            string bankName = cbBankName.Text;
            if (!BalanceConfirmationService.ConfirmBankOutTransaction(bankName, amount, id))
                return;

            Hashtable ht = new Hashtable();
            string qry = "";
            if (id == 0)
            {
                qry = @"
INSERT INTO BankTransactions 
(
    TransactionDate, TransactionType,
    CustomerId, DealerId,
    Amount, Note, BankName
)
VALUES 
(
    @TransactionDate, @TransactionType,
    @CustomerId, @DealerId,
    @Amount, @Note, @BankName
)";
            }
            else
            {
                qry = @"
UPDATE BankTransactions SET 
    TransactionDate   = @TransactionDate, 
    TransactionType   = @TransactionType,
    CustomerId        = @CustomerId, 
    DealerId          = @DealerId, 
    Amount            = @Amount, 
    Note              = @Note,
    BankName          = @BankName
WHERE Id = @Id
";
                ht.Add("@Id", id);
            }

            // Aik line mein yeh check karein ke agar SelectedIndex == -1 (ya user ne kuch select nahin kiya),
            // to DB mein NULL jaaye, warna jo SelectedValue hai.
            object customerValue = (cbCustomer.SelectedIndex == -1)
                                   ? (object)DBNull.Value
                                   : cbCustomer.SelectedValue;
            object dealerValue = (cbDealer.SelectedIndex == -1)
                                   ? (object)DBNull.Value
                                   : cbDealer.SelectedValue;

            ht.Add("@TransactionDate", transactionDate.ToString("yyyy-MM-dd"));
            ht.Add("@TransactionType", transType);
            ht.Add("@CustomerId", customerValue);
            ht.Add("@DealerId", dealerValue);
            ht.Add("@Amount", amount.ToString("F2"));
            ht.Add("@Note", txtNote.Text);
            ht.Add("@BankName", cbBankName.Text); // or txtBankName.Text if you want

            try
            {
                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    CustomeMessage customMessageBox = new CustomeMessage("Transaction save ho gayi!", "Save");
                    customMessageBox.ShowDialog();

                    ResetForm();
                    id = 0;
                }
                else
                {
                    ErrorFormMessage    customMessageBox = new ErrorFormMessage("Data save nahi hua!", "Save");
                    customMessageBox.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

        private void LoadBanks()
        {
            // Hard-coded list of Pakistani banks + e-wallets
            string[] pakistaniBanks = new string[]
            {
        "Allied Bank",
        "Askari Bank",
        "Bank Al Habib",
        "Bank Alfalah",
        "Faysal Bank",
        "HBL",
        "MCB Bank",
        "NBP",
        "UBL",
        "Meezan Bank",
        "HabibMetropolitanBank",
        "JS Bank",
        "Samba Bank",
        "Soneri Bank",
        "StandardCharteredBank",
        "Bank of Khyber",
        "Bank of Punjab",
        "Summit Bank",
        "Silk Bank",
        "Al Baraka Bank (Pakistan)",
        // Microfinance + eWallets
        "(UPaisa)",
        "FINCA Microfinance Bank",
        "Apna Microfinance Bank",
        "The First MicroFinance Bank",
        "JazzCash",
        "EasyPaisa",
        "SadaPay",
        "NayaPay"
            };

            cbBankName.DataSource = pakistaniBanks;
            // cbBankName.DropDownStyle = ComboBoxStyle.DropDownList;

            // [NAYA CODE for txtBankName autoComplete ↓↓]
            AutoCompleteStringCollection autoBanks = new AutoCompleteStringCollection();
            autoBanks.AddRange(pakistaniBanks);

            txtBankName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtBankName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtBankName.AutoCompleteCustomSource = autoBanks;
            // [NAYA CODE ↑↑]
        }

        private void ResetForm()
        {
            dtTransaction.Value = DateTime.Now;
            txtAmount.Clear();
            txtNote.Clear();
            LoadCustomers();
            cbDealer.SelectedIndex = -1;
            cbCustomer.SelectedIndex = -1;
            LoadDealers();
            cbTransactionType.SelectedIndex = 0;
        }
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                // Custom Yes/No confirmation dialog
                YesOrNoMessage confirmDelete = new YesOrNoMessage(
                    "Kya aap is record ko delete karna chahte hain?",
                    "Confirm Delete"
                );

                if (confirmDelete.ShowDialog() == DialogResult.Yes) // Yes/No check
                {
                    try
                    {
                        // Parameterized query for deletion 
                        // (BankTransactions table ke liye)
                        string qry = "DELETE FROM BankTransactions WHERE Id = @Id";

                        Hashtable ht = new Hashtable();
                        ht.Add("@Id", id);

                        // Delete operation
                        int resultDelete = MainClass.DataInsertUpdateDelete(qry, ht);

                        if (resultDelete > 0)
                        {
                            CustomeMessage successMessage = new CustomeMessage(
                                "Record delete ho gaya!",
                                "Success"
                            );
                            successMessage.ShowDialog();

                            this.DialogResult = DialogResult.OK; // Signal that the data was deleted
                            this.Close(); // Close the form after deletion
                        }
                        else
                        {
                            CustomeMessage errorMessage = new CustomeMessage(
                                "Record delete nahi hua!",
                                "Error"
                            );
                            errorMessage.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomeMessage errorMessage = new CustomeMessage(
                            "Error: " + ex.Message,
                            "Error"
                        );
                        errorMessage.ShowDialog();
                    }
                }
            }
        }

    }
}
