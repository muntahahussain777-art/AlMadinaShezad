using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmDieselLedgerDealerAdd : SampleAdd
    {
        // ==============
        // 1) Two private fields to store old (dealer & amount)
        //    for "undo" logic when editing
        // ==============
        private int _oldDealerId = 0;
        private decimal _oldAmountPaid = 0;
        private bool _savedInSession;

        public int LedgerID { get; set; }  // Track if editing or adding a new record

        public frmDieselLedgerDealerAdd(int ledgerID = 0)
        {
            InitializeComponent();
            LedgerID = ledgerID;

            // NOTE: We do NOT call LoadLedgerDetails here.
            // We'll do that in frmDieselLedgerDealerAdd_Load, after PopulateDealerNames().

            cbDealer.SelectedIndexChanged += cbDealer_SelectedIndexChanged;
        }

        private void frmDieselLedgerDealerAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            // 1) Pehle dealers ComboBox fill karo
            PopulateDealerNames();

            // 2) Agar edit mode hai, to record load karo
            if (LedgerID > 0)
            {
                LoadLedgerDetails(LedgerID);
            }
            else
            {
                dtpPaymentDate.Value = DateTime.Now;
            }

            // NAYA CODE (shortcut keys, etc.)
            this.KeyPreview = true;
            this.KeyDown += frmDieselLedgerDealerAdd_KeyDown;

            txtDealerName.TextChanged += txtDealerName_TextChanged;
            txtDealerName.Visible = false; // by default hidden

            this.FormClosing -= frmDieselLedgerDealerAdd_FormClosing;
            this.FormClosing += frmDieselLedgerDealerAdd_FormClosing;
        }

        private void frmDieselLedgerDealerAdd_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_savedInSession && DialogResult == DialogResult.None)
                DialogResult = DialogResult.OK;
        }

        private void ResetAfterSaveKeepDate()
        {
            object lastDealerId = cbDealer.SelectedValue;
            MainClass.Enable_reset_keep_date(this, dtpPaymentDate);
            LedgerID = 0;
            _oldDealerId = 0;
            _oldAmountPaid = 0;

            if (lastDealerId != null && cbDealer.DataSource != null)
            {
                try { cbDealer.SelectedValue = lastDealerId; }
                catch { /* ignore invalid restore */ }
            }
        }
        private void PopulateDealerNames()
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

                // [NAYA CODE for txtDealerName AutoComplete ↓↓]
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
        }

        // =========================
        // HOTKEYS FOR TEXTBOX/COMBO
        // =========================
        private void frmDieselLedgerDealerAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1) // Ctrl + 1
                {
                    txtDealerName.Visible = true;
                    cbDealer.Visible = false;
                    txtDealerName.Focus();
                }
                else if (e.KeyCode == Keys.D2) // Ctrl + 2
                {
                    txtDealerName.Visible = false;
                    cbDealer.Visible = true;
                    cbDealer.Focus();
                }
            }
        }

        // =========================
        // LOADLedgerDetails for Edit
        // =========================
        private void LoadLedgerDetails(int ledgerID)
        {
            string query = "SELECT * FROM DieselLedgerCredit WHERE LedgerID = @LedgerID";
            Hashtable ht = new Hashtable { { "@LedgerID", ledgerID } };

            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt != null && dt.Rows.Count > 0)
            {
                // Combobox set karne ke liye
                cbDealer.SelectedValue = Convert.ToInt32(dt.Rows[0]["Did"]);

                txtAmountPaid.Text = dt.Rows[0]["AmounGiven"].ToString();
                dtpPaymentDate.Value = Convert.ToDateTime(dt.Rows[0]["Date"]);
                txtNote.Text = dt.Rows[0]["Note"].ToString();

                // Store old scenario for "undo" if user changes dealer or amount
                _oldDealerId = Convert.ToInt32(dt.Rows[0]["Did"]);
                _oldAmountPaid = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);
                OfficeHelper.SetSelectedOffice(this, Convert.ToString(dt.Rows[0]["OfficeName"]));
            }
            else
            {
                CustomeMessage customMessageBox = new CustomeMessage("منتخب ریکارڈ کے لیے کوئی تفصیل نہیں ملی۔", "INFORMATION");
                customMessageBox.ShowDialog();
            }
        }

        // =========================
        // SAVE (INSERT OR UPDATE)
        // =========================
        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (cbDealer.SelectedIndex == -1)
            {
                ErrorFormMessage err = new ErrorFormMessage("براہ کرم ڈیلر کا انتخاب کریں۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
                return;
            }

            if (!decimal.TryParse(txtAmountPaid.Text.Replace(",", ""), out decimal newAmountPaid))
            {
                ErrorFormMessage err = new ErrorFormMessage("براہ کرم ایک درست رقم درج کریں۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
                return;
            }

            DateTime paymentDate = dtpPaymentDate.Value;
            int newDealerId = Convert.ToInt32(cbDealer.SelectedValue);

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;
            string office = OfficeHelper.GetSelectedOffice(this);
            if (!OfficeHelper.ConfirmDealerForOffice(newDealerId, cbDealer.Text, office))
                return;

            decimal netDeduction = BalanceConfirmationService.NetDeduction(
                newAmountPaid, _oldAmountPaid, LedgerID > 0 && newDealerId == _oldDealerId);
            if (!BalanceConfirmationService.ConfirmDealerCreditDeduction(newDealerId, netDeduction, cbDealer.Text))
                return;

            try
            {
                if (LedgerID == 0)
                {
                    // === NEW RECORD ===
                    InsertLedgerRecord(newDealerId, newAmountPaid, txtNote.Text, paymentDate, office);

                    // Ab dealer ke DAmount me yeh newAmount add
                    AddToDealerAmount(newDealerId, newAmountPaid);

                    CustomeMessage success = new CustomeMessage("ڈیٹا کامیابی کے ساتھ محفوظ کر لیا گیا۔", "Al Madina Shezad Chhutta Services");
                    success.ShowDialog();
                    ResetAfterSaveKeepDate();
                    _savedInSession = true;
                }
                else
                {
                    // === EDIT MODE ===
                    // 1) Undo old scenario => minus old amount from old dealer
                    if (_oldDealerId != 0 && _oldAmountPaid > 0)
                    {
                        SubtractFromDealerAmount(_oldDealerId, _oldAmountPaid);
                    }

                    // 2) Update DieselLedgerCredit
                    UpdateLedgerRecord(LedgerID, newDealerId, newAmountPaid, txtNote.Text, paymentDate, office);

                    // 3) New scenario => new dealer me newAmount add
                    AddToDealerAmount(newDealerId, newAmountPaid);

                    CustomeMessage success = new CustomeMessage("ڈیٹا کامیابی کے ساتھ اپڈیٹ ہوگیا۔", "Al Madina Shezad Chhutta Services");
                    success.ShowDialog();
                    ResetAfterSaveKeepDate();
                    _savedInSession = true;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage err = new ErrorFormMessage("خرابی: " + ex.Message, "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
            }
        }

        // =========================
        // INSERT & UPDATE Queries
        // =========================
        private void InsertLedgerRecord(int dealerId, decimal amountPaid, string note, DateTime paymentDate, string office)
        {
            string sql = @"
                INSERT INTO DieselLedgerCredit (Did, AmounGiven, Date, Note, OfficeName)
                VALUES (@did, @amt, @dt, @note, @office)";

            Hashtable ht = new Hashtable
            {
                { "@did", dealerId },
                { "@amt", amountPaid },
                { "@dt", paymentDate.ToString("yyyy-MM-dd") },
                { "@note", note },
                { "@office", office }
            };

            int result = MainClass.DataInsertUpdateDelete(sql, ht);
            if (result <= 0)
            {
                ErrorFormMessage err = new ErrorFormMessage("ڈیٹا محفوظ کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
            }
        }

        private void UpdateLedgerRecord(int ledgerId, int dealerId, decimal amountPaid, string note, DateTime paymentDate, string office)
        {
            string sql = @"
                UPDATE DieselLedgerCredit
                SET Did        = @did,
                    AmounGiven = @amt,
                    Date       = @dt,
                    Note       = @note,
                    OfficeName = @office
                WHERE LedgerID = @lid";

            Hashtable ht = new Hashtable
            {
                { "@lid", ledgerId },
                { "@did", dealerId },
                { "@amt", amountPaid },
                { "@dt", paymentDate.ToString("yyyy-MM-dd") },
                { "@note", note },
                { "@office", office }
            };

            int result = MainClass.DataInsertUpdateDelete(sql, ht);
            if (result <= 0)
            {
                ErrorFormMessage err = new ErrorFormMessage("ڈیٹا اپڈیٹ کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
            }
        }

        // =========================
        // ADD & SUBTRACT Dealer's DAmount
        // =========================
        private void AddToDealerAmount(int dealerId, decimal amountPaid)
        {
            if (dealerId == 0 || amountPaid == 0) return;

            string updateQuery = "UPDATE AddDealer SET DAmount = DAmount + @amount WHERE Did = @dealerId";
            Hashtable htUpdate = new Hashtable
            {
                { "@dealerId", dealerId },
                { "@amount", amountPaid }
            };

            int updateResult = MainClass.DataInsertUpdateDelete(updateQuery, htUpdate);
            if (updateResult <= 0)
            {
                ErrorFormMessage err = new ErrorFormMessage("ڈیلر کے DAmount کو اپڈیٹ کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
            }
        }

        private void SubtractFromDealerAmount(int dealerId, decimal oldAmount)
        {
            if (dealerId == 0 || oldAmount == 0) return;

            string updateQuery = "UPDATE AddDealer SET DAmount = DAmount - @amount WHERE Did = @dealerId";
            Hashtable htUpdate = new Hashtable
            {
                { "@dealerId", dealerId },
                { "@amount", oldAmount }
            };

            int updateResult = MainClass.DataInsertUpdateDelete(updateQuery, htUpdate);
            if (updateResult <= 0)
            {
                ErrorFormMessage err = new ErrorFormMessage("پرانے ڈیلر کے DAmount کو واپس کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                err.ShowDialog();
            }
        }

        // =========================
        // DELETE BUTTON
        // =========================
        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (LedgerID > 0)
            {
                // Query to get the dealer's ID and the amount paid from DieselLedgerCredit
                string selectQuery = "SELECT Did, AmounGiven FROM DieselLedgerCredit WHERE LedgerID = @LedgerID";
                Hashtable selectParams = new Hashtable { { "@LedgerID", LedgerID } };

                DataTable dt = MainClass.ExecuteSelectQuery(selectQuery, selectParams);
                if (dt != null && dt.Rows.Count > 0)
                {
                    int dealerId = Convert.ToInt32(dt.Rows[0]["Did"]);
                    decimal amountToDelete = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);

                    YesOrNoMessage confirmDelete = new YesOrNoMessage("کیا آپ واقعی اس ریکارڈ کو حذف کرنا چاہتے ہیں؟", "حذف کی تصدیق کریں");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        string deleteQuery = "DELETE FROM DieselLedgerCredit WHERE LedgerID = @LedgerID";
                        Hashtable deleteParams = new Hashtable { { "@LedgerID", LedgerID } };

                        int deleteResult = MainClass.DataInsertUpdateDelete(deleteQuery, deleteParams);
                        if (deleteResult > 0)
                        {
                            // Undo from the dealer's DAmount
                            SubtractFromDealerAmount(dealerId, amountToDelete);

                            CustomeMessage successMessage = new CustomeMessage("ریکارڈ کامیابی کے ساتھ حذف کر دیا گیا اور ڈیلر کا DAmount اپ ڈیٹ ہو گیا۔", "Al Madina Shezad Chhutta Services");
                            successMessage.ShowDialog();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage("ڈیزل لیجر سے ریکارڈ ختم کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                            errorMessage.ShowDialog();
                        }
                    }
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Record details fetch nahi hui!", "Al Madina Shezad Chhutta Services");
                    errorMessage.ShowDialog();
                }
            }
            else
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("براہ کرم حذف کرنے کے لیے ایک درست ریکارڈ منتخب کریں۔", "Al Madina Shezad Chhutta Services");
                errorMessage.ShowDialog();
            }
        }

        // =========================
        // COMBOBOX, TEXTBOX EVENTS
        // =========================
        private void cbDealer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDealer.SelectedItem != null)
            {
                int dealerId = Convert.ToInt32((cbDealer.SelectedItem as DataRowView)["Did"]);
                ShowDealerBalance(dealerId);
            }
        }

        private void ShowDealerBalance(int dealerId)
        {
            string query = "SELECT DAmount, DDAmount FROM AddDealer WHERE Did = @dealerId";
            Hashtable ht = new Hashtable { { "@dealerId", dealerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);

            if (dt != null && dt.Rows.Count > 0)
            {
                decimal dAmount = Convert.ToDecimal(dt.Rows[0]["DAmount"]);
                decimal ddAmount = Convert.ToDecimal(dt.Rows[0]["DDAmount"]);

                lblDealerBalance.Text = $"Balance: {ddAmount:F2}";
                lblcreditbalance.Text = $"Balance: {dAmount:F2}";
                decimal creditBalance = ddAmount - dAmount;
                lblbalance.Text = $"Credit Balance: {creditBalance:F2}";
            }
            else
            {
                lblDealerBalance.Text = "Balance: 0.00";
                lblcreditbalance.Text = "Balance: 0.00";
                lblbalance.Text = "Balance: 0.00";
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
            }
            else
            {
                cbDealer.SelectedIndex = -1;
            }
        }

        private void txtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            string currentText = txtAmountPaid.Text.Replace(",", "");
            if (decimal.TryParse(currentText, out decimal value))
            {
                txtAmountPaid.Text = value.ToString("N0", CultureInfo.InvariantCulture);
                txtAmountPaid.SelectionStart = txtAmountPaid.Text.Length;
            }
        }
    }
}
