using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmDealertoDealerAdd : SampleAdd1
    {
        // ==============================
        // TEEN PRIVATE VARIABLES (Purana record track karne ke liye)
        // ==============================
        private int _oldDealerId1 = 0;
        private int _oldDealerId2 = 0;
        private decimal _oldAmountPaid = 0;

        public int LedgerID { get; set; }

        public frmDealertoDealerAdd(int ledgerID = 0)
        {
            InitializeComponent();
            LedgerID = ledgerID;

            // Yeh events register kara do
            cbDealer2.SelectedIndexChanged += cbDealer2_SelectedIndexChanged;
            cbDealer1.SelectedIndexChanged += cbDealer1_SelectedIndexChanged;
        }

        private void frmDealertoDealerAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            // 1) Sab se pehle combobox fill karo
            PopulateDealerNames();

            // 2) Agar LedgerID > 0 hai, to record load karo
            if (LedgerID > 0)
            {
                LoadLedgerDetails(LedgerID);
            }

            // Baaki setup:
            dtpPaymentDate.Value = DateTime.Now;
            txtAmountPaid.TextChanged += new EventHandler(txtAmountPaid_TextChanged);

            // KeyPreview taake Ctrl+1, Ctrl+2 capture ho sake
            this.KeyPreview = true;
            this.KeyDown += frmDealertoDealerAdd_KeyDown;

            txtDealerName1.Visible = false;
            txtDealerName2.Visible = false;

            txtDealerName1.TextChanged += txtDealerName1_TextChanged;
            txtDealerName2.TextChanged += txtDealerName2_TextChanged;
        }

        // -----------------------------
        // SHORTCUT KEYS
        // -----------------------------
        private void frmDealertoDealerAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1)
                {
                    txtDealerName1.Visible = true;
                    txtDealerName2.Visible = true;
                    cbDealer1.Visible = false;
                    cbDealer2.Visible = false;
                    txtDealerName1.Focus();
                }
                else if (e.KeyCode == Keys.D2)
                {
                    txtDealerName1.Visible = false;
                    txtDealerName2.Visible = false;
                    cbDealer1.Visible = true;
                    cbDealer2.Visible = true;
                    cbDealer1.Focus();
                }
            }
        }

        // -----------------------------
        // COMBOBOX FILL
        // -----------------------------
        private void PopulateDealerNames()
        {
            string query = "SELECT Did, DealerName FROM AddDealer ORDER BY Did ASC";
            DataTable dt = MainClass.ExecuteSelectQuery(query, null);

            if (dt != null && dt.Rows.Count > 0)
            {
                // ComboBox1
                cbDealer1.DataSource = dt.Copy();
                cbDealer1.DisplayMember = "DealerName";
                cbDealer1.ValueMember = "Did";
                cbDealer1.SelectedIndex = -1;

                // ComboBox2
                DataTable dt2 = dt.Copy();
                cbDealer2.DataSource = dt2;
                cbDealer2.DisplayMember = "DealerName";
                cbDealer2.ValueMember = "Did";
                cbDealer2.SelectedIndex = -1;

                // AutoComplete for TextBoxes
                AutoCompleteStringCollection auto1 = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    auto1.Add(row["DealerName"].ToString());
                }
                txtDealerName1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtDealerName1.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtDealerName1.AutoCompleteCustomSource = auto1;

                AutoCompleteStringCollection auto2 = new AutoCompleteStringCollection();
                foreach (DataRow row in dt2.Rows)
                {
                    auto2.Add(row["DealerName"].ToString());
                }
                txtDealerName2.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtDealerName2.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtDealerName2.AutoCompleteCustomSource = auto2;
            }
            else
            {
                CustomeMessage msg = new CustomeMessage("No dealers found in AddDealer!", "Warning");
                msg.ShowDialog();
            }
        }

        // -----------------------------
        // LOAD FOR EDIT
        // -----------------------------
        private void LoadLedgerDetails(int ledgerID)
        {
            string query = "SELECT * FROM DealertoDealer WHERE LedgerID = @LedgerID";
            Hashtable ht = new Hashtable { { "@LedgerID", ledgerID } };

            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt != null && dt.Rows.Count > 0)
            {
                // Yahan hum 'id' aur 'Did' se combobox set kar rahe hain
                object val1 = dt.Rows[0]["id"];
                object val2 = dt.Rows[0]["Did"];

                if (val1 != DBNull.Value) cbDealer1.SelectedValue = Convert.ToInt32(val1);
                if (val2 != DBNull.Value) cbDealer2.SelectedValue = Convert.ToInt32(val2);

                // Amount
                decimal oldAmt = 0;
                if (dt.Rows[0]["AmounGiven"] != DBNull.Value)
                {
                    oldAmt = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);
                    txtAmountPaid.Text = oldAmt.ToString();
                }
                else
                {
                    txtAmountPaid.Text = string.Empty;
                }

                // Date
                if (DateTime.TryParse(dt.Rows[0]["Date"].ToString(), out DateTime d))
                    dtpPaymentDate.Value = d;
                else
                    dtpPaymentDate.Value = DateTime.Now;

                // Note
                txtNote.Text = dt.Rows[0]["Note"]?.ToString() ?? string.Empty;

                // Purana scenario store kar lo
                _oldDealerId1 = (val1 != DBNull.Value) ? Convert.ToInt32(val1) : 0;
                _oldDealerId2 = (val2 != DBNull.Value) ? Convert.ToInt32(val2) : 0;
                _oldAmountPaid = oldAmt;
                OfficeHelper.SetSelectedOffice(this, Convert.ToString(dt.Rows[0]["OfficeName"]));
            }
            else
            {
                CustomeMessage noDataMessage = new CustomeMessage("کوئی ریکارڈ نہیں ملا!", "وارننگ");
                noDataMessage.ShowDialog();
            }
        }

        // -----------------------------
        // SAVE BUTTON
        // -----------------------------
        protected override void btnSave_Click(object sender, EventArgs e)
        {
            if (cbDealer1.SelectedIndex == -1 || cbDealer2.SelectedIndex == -1)
            {
                CustomeMessage validationMessage = new CustomeMessage("Dono dealers select karein!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            if (!decimal.TryParse(txtAmountPaid.Text.Replace(",", ""), out decimal newAmountPaid))
            {
                CustomeMessage validationMessage = new CustomeMessage("Valid payment amount daalein!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            DateTime paymentDate = dtpPaymentDate.Value;
            int newDealerId1 = Convert.ToInt32(cbDealer1.SelectedValue); // numeric ID
            int newDealerId2 = Convert.ToInt32(cbDealer2.SelectedValue); // numeric ID

            string newDealerName1 = cbDealer1.Text;
            string newDealerName2 = cbDealer2.Text;

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;
            string office = OfficeHelper.GetSelectedOffice(this);
            if (!OfficeHelper.ConfirmDealerForOffice(newDealerId1, newDealerName1, office))
                return;
            if (!OfficeHelper.ConfirmDealerForOffice(newDealerId2, newDealerName2, office))
                return;

            decimal netDealer1Deduction = BalanceConfirmationService.NetDeduction(
                newAmountPaid, _oldAmountPaid, LedgerID > 0 && newDealerId1 == _oldDealerId1);
            if (!BalanceConfirmationService.ConfirmDealerCreditDeduction(newDealerId1, netDealer1Deduction, newDealerName1))
                return;

            try
            {
                if (LedgerID == 0)
                {
                    // =========== NEW RECORD ===========
                    InsertLedgerRecord(newDealerId1, newDealerId2, newDealerName1, newDealerName2, newAmountPaid, txtNote.Text, paymentDate, office);

                    // Ab new dealers mein amount add karo
                    AddToDealer1(newDealerId1, newAmountPaid);
                    AddToDealer2(newDealerId2, newAmountPaid);

                    CustomeMessage successMessage = new CustomeMessage("نیا ریکارڈ سیو ہو گیا!", "Success");
                    successMessage.ShowDialog();
                }
                else
                {
                    // =========== EDIT MODE ===========
                    // 1) Undo the old scenario
                    if (_oldDealerId1 != 0 && _oldAmountPaid != 0) SubtractFromDealer1(_oldDealerId1, _oldAmountPaid);
                    if (_oldDealerId2 != 0 && _oldAmountPaid != 0) SubtractFromDealer2(_oldDealerId2, _oldAmountPaid);

                    // 2) Update DealertoDealer record with the new info
                    UpdateLedgerRecord(LedgerID, newDealerId1, newDealerId2, newDealerName1, newDealerName2, newAmountPaid, txtNote.Text, paymentDate, office);

                    // 3) Add new scenario
                    AddToDealer1(newDealerId1, newAmountPaid);
                    AddToDealer2(newDealerId2, newAmountPaid);

                    CustomeMessage successMessage = new CustomeMessage("ریکارڈ اپڈیٹ ہو گیا!", "Success");
                    successMessage.ShowDialog();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        // -----------------------------
        // INSERT & UPDATE methods
        // -----------------------------
        private void InsertLedgerRecord(
            int newDealerId1, int newDealerId2,
            string newDealerName1, string newDealerName2,
            decimal newAmountPaid, string note, DateTime paymentDate, string office)
        {
            string sql = @"
                INSERT INTO DealertoDealer
                    (Date, FirstDealer, SecondDealer, AmounGiven, Note, id, Did, OfficeName)
                VALUES
                    (@payDate, @name1, @name2, @amt, @note, @d1, @d2, @office)";

            Hashtable ht = new Hashtable
            {
                { "@payDate", paymentDate.ToString("yyyy-MM-dd") },
                { "@name1", newDealerName1 },
                { "@name2", newDealerName2 },
                { "@amt", newAmountPaid },
                { "@note", note },
                { "@d1", newDealerId1 },
                { "@d2", newDealerId2 },
                { "@office", office }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }

        private void UpdateLedgerRecord(
            int ledgerId,
            int newDealerId1, int newDealerId2,
            string newDealerName1, string newDealerName2,
            decimal newAmountPaid, string note, DateTime paymentDate, string office)
        {
            string sql = @"
                UPDATE DealertoDealer
                SET
                    Date         = @payDate,
                    FirstDealer  = @name1,
                    SecondDealer = @name2,
                    AmounGiven   = @amt,
                    Note         = @note,
                    id           = @d1,
                    Did          = @d2,
                    OfficeName   = @office
                WHERE LedgerID = @lid";

            Hashtable ht = new Hashtable
            {
                { "@lid", ledgerId },
                { "@payDate", paymentDate.ToString("yyyy-MM-dd") },
                { "@name1", newDealerName1 },
                { "@name2", newDealerName2 },
                { "@amt", newAmountPaid },
                { "@note", note },
                { "@d1", newDealerId1 },
                { "@d2", newDealerId2 },
                { "@office", office }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }

        // -----------------------------
        // ADD & SUBTRACT HELPER METHODS
        // -----------------------------
        private void AddToDealer1(int dealerId, decimal amount)
        {
            if (dealerId == 0 || amount == 0) return;
            string sql = "UPDATE AddDealer SET DAmount = DAmount + @amt WHERE Did = @did";
            Hashtable ht = new Hashtable
            {
                { "@amt", amount },
                { "@did", dealerId }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }

        private void AddToDealer2(int dealerId, decimal amount)
        {
            if (dealerId == 0 || amount == 0) return;
            string sql = "UPDATE AddDealer SET DDAmount = DDAmount + @amt WHERE Did = @did";
            Hashtable ht = new Hashtable
            {
                { "@amt", amount },
                { "@did", dealerId }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }

        private void SubtractFromDealer1(int dealerId, decimal amount)
        {
            if (dealerId == 0 || amount == 0) return;
            string sql = "UPDATE AddDealer SET DAmount = DAmount - @amt WHERE Did = @did";
            Hashtable ht = new Hashtable
            {
                { "@amt", amount },
                { "@did", dealerId }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }

        private void SubtractFromDealer2(int dealerId, decimal amount)
        {
            if (dealerId == 0 || amount == 0) return;
            string sql = "UPDATE AddDealer SET DDAmount = DDAmount - @amt WHERE Did = @did";
            Hashtable ht = new Hashtable
            {
                { "@amt", amount },
                { "@did", dealerId }
            };
            MainClass.DataInsertUpdateDelete(sql, ht);
        }


        // -----------------------------
        // DELETE BUTTON
        // -----------------------------
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (LedgerID > 0)
            {
                string selectQuery = @"SELECT id AS FirstDealerId, Did AS SecondDealerId, AmounGiven 
                                       FROM DealertoDealer WHERE LedgerID = @LedgerID";
                Hashtable selectParams = new Hashtable { { "@LedgerID", LedgerID } };

                DataTable dt = MainClass.ExecuteSelectQuery(selectQuery, selectParams);
                if (dt != null && dt.Rows.Count > 0)
                {
                    int firstDealerId = dt.Rows[0]["FirstDealerId"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["FirstDealerId"]) : 0;
                    int secondDealerId = dt.Rows[0]["SecondDealerId"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["SecondDealerId"]) : 0;
                    decimal amountToDelete = dt.Rows[0]["AmounGiven"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["AmounGiven"]) : 0;

                    YesOrNoMessage confirmDelete = new YesOrNoMessage(
                        "Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        // Undo from old dealers
                        if (firstDealerId != 0 && amountToDelete != 0) SubtractFromDealer1(firstDealerId, amountToDelete);
                        if (secondDealerId != 0 && amountToDelete != 0) SubtractFromDealer2(secondDealerId, amountToDelete);

                        // Delete from table
                        string deleteQuery = "DELETE FROM DealertoDealer WHERE LedgerID = @LedgerID";
                        Hashtable deleteParams = new Hashtable { { "@LedgerID", LedgerID } };

                        int deleteResult = MainClass.DataInsertUpdateDelete(deleteQuery, deleteParams);
                        if (deleteResult > 0)
                        {
                            CustomeMessage successMessage = new CustomeMessage("Record delete ho gaya aur amounts adjust ho gaye!", "Success");
                            successMessage.ShowDialog();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage("DealertoDealer se record delete nahi hua!", "Error");
                            errorMessage.ShowDialog();
                        }
                    }
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Delete se pehle record load nahi hua!", "Error");
                    errorMessage.ShowDialog();
                }
            }
            else
            {
                CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek valid record select karein!", "Warning");
                noSelectionMessage.ShowDialog();
            }
        }

        // -----------------------------
        // TEXT CHANGED EVENTS
        // -----------------------------
        private void txtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            string currentText = txtAmountPaid.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                txtAmountPaid.Text = value.ToString("N0", CultureInfo.InvariantCulture);
                txtAmountPaid.SelectionStart = txtAmountPaid.Text.Length;
            }
        }

        private void txtDealerName1_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtDealerName1.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt = cbDealer1.DataSource as DataTable;
                if (dt != null)
                {
                    string filter = $"DealerName = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        cbDealer1.SelectedValue = rows[0]["Did"];
                    }
                    else
                    {
                        cbDealer1.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                cbDealer1.SelectedIndex = -1;
            }
        }

        private void txtDealerName2_TextChanged(object sender, EventArgs e)
        {
            string enteredName = txtDealerName2.Text.Trim();
            if (!string.IsNullOrEmpty(enteredName))
            {
                DataTable dt2 = cbDealer2.DataSource as DataTable;
                if (dt2 != null)
                {
                    string filter = $"DealerName = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt2.Select(filter);

                    if (rows.Length > 0)
                    {
                        cbDealer2.SelectedValue = rows[0]["Did"];
                    }
                    else
                    {
                        cbDealer2.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                cbDealer2.SelectedIndex = -1;
            }
        }

        private void cbDealer1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDealer1.SelectedValue != null && int.TryParse(cbDealer1.SelectedValue.ToString(), out int selectedDealerId))
            {
                decimal dealerDDAmount = GetDealerDAmount(selectedDealerId);
                lblDealerBalance.Text = dealerDDAmount.ToString("N0", CultureInfo.InvariantCulture);
            }
        }

        private void cbDealer2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDealer2.SelectedValue != null && int.TryParse(cbDealer2.SelectedValue.ToString(), out int selectedDealerId))
            {
                decimal dealerDDAmount = GetDealerDAmount(selectedDealerId);
                customerlbl.Text = dealerDDAmount.ToString("N0", CultureInfo.InvariantCulture);
            }
        }

        private decimal GetDealerDAmount(int dealerId)
        {
            string query = "SELECT DDAmount FROM AddDealer WHERE Did = @dealerId";
            Hashtable ht = new Hashtable { { "@dealerId", dealerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["DDAmount"] != DBNull.Value)
            {
                return Convert.ToDecimal(dt.Rows[0]["DDAmount"]);
            }
            return 0;
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
            string currentText = lblDealerBalance.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                lblDealerBalance.Text = value.ToString("N0", CultureInfo.InvariantCulture);
            }
        }
    }
}
