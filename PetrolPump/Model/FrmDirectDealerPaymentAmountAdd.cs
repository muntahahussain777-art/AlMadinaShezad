using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class FrmDirectDealerPaymentAmountAdd : SampleAdd
    {
        public int LedgerID { get; set; }

        // Yeh do private variables rakhe hain purana dealer/amount store karne ke liye
        private int _oldDid = 0;
        private decimal _oldAmountPaid = 0;
        private bool _savedInSession;

        public FrmDirectDealerPaymentAmountAdd(int ledgerID = 0)
        {
            InitializeComponent();
            LedgerID = ledgerID;
        }

        private void FrmDirectDealerPaymentAmountAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            // Pehle comboBox fill karo
            PopulateDealerNames();

            // Agar editing mode hai (LedgerID > 0), to LoadLedgerDetails se data bharo
            if (LedgerID > 0)
            {
                LoadLedgerDetails(LedgerID);
            }
            else
            {
                dtpPaymentDate.Value = DateTime.Now;
            }

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(FrmDirectDealerPaymentAmountAdd_KeyDown);

            txtDealerName.Visible = false;
            txtDealerName.TextChanged += txtDealerName_TextChanged;

            this.FormClosing -= FrmDirectDealerPaymentAmountAdd_FormClosing;
            this.FormClosing += FrmDirectDealerPaymentAmountAdd_FormClosing;
        }

        private void FrmDirectDealerPaymentAmountAdd_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_savedInSession && DialogResult == DialogResult.None)
                DialogResult = DialogResult.OK;
        }

        private void ResetAfterSaveKeepDate()
        {
            object lastDealerId = cbDealer.SelectedValue;
            MainClass.Enable_reset_keep_date(this, dtpPaymentDate);
            LedgerID = 0;
            _oldDid = 0;
            _oldAmountPaid = 0;

            if (lastDealerId != null && cbDealer.DataSource != null)
            {
                try { cbDealer.SelectedValue = lastDealerId; }
                catch { /* ignore invalid restore */ }
            }
        }

        private void FrmDirectDealerPaymentAmountAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1)
                {
                    txtDealerName.Visible = true;
                    cbDealer.Visible = false;
                    txtDealerName.Focus();
                }
                else if (e.KeyCode == Keys.D2)
                {
                    txtDealerName.Visible = false;
                    cbDealer.Visible = true;
                    cbDealer.Focus();
                }
                else if (e.KeyCode == Keys.S)   // Ctrl + S
                {
                    e.SuppressKeyPress = true;
                    btnSave_Click(null, null);   // ← BEST, AZAADI, NO ERROR
                }
            }
        }


        // Edit mode ke liye purana record load karein
        private void LoadLedgerDetails(int ledgerID)
        {
            string query = "SELECT * FROM DieselLedgerDebit WHERE LedgerID = @LedgerID";
            Hashtable ht = new Hashtable();
            ht.Add("@LedgerID", ledgerID);

            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt != null && dt.Rows.Count > 0)
            {
                cbDealer.SelectedValue = Convert.ToInt32(dt.Rows[0]["Did"]);
                txtAmountPaid.Text = dt.Rows[0]["AmounGiven"].ToString();
                dtpPaymentDate.Value = Convert.ToDateTime(dt.Rows[0]["Date"]);
                txtNote.Text = dt.Rows[0]["Note"].ToString();

                // Yahan hum purana dealer aur purana amount save kar lete hain
                _oldDid = Convert.ToInt32(dt.Rows[0]["Did"]);
                _oldAmountPaid = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);
                OfficeHelper.SetSelectedOffice(this, Convert.ToString(dt.Rows[0]["OfficeName"]));
            }
            else
            {
                CustomeMessage msg = new CustomeMessage("منتخب ریکارڈ کے لیے کوئی تفصیل نہیں ملی۔", "Al Madina Shezad Chhutta Services");
                msg.ShowDialog();
            }
        }

        private void ShowDealerBalance(int dealerId)
        {
            string query = "SELECT DAmount, DDAmount FROM AddDealer WHERE Did = @dealerId";
            Hashtable ht = new Hashtable();
            ht.Add("@dealerId", dealerId);

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

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (cbDealer.SelectedIndex == -1)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("براہ کرم ڈیلر کا انتخاب کریں۔", "Al Madina Shezad Chhutta Services");
                errorMessage.ShowDialog();
                return;
            }

            if (!decimal.TryParse(txtAmountPaid.Text.Replace(",", ""), out decimal newAmountPaid))
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("براہ کرم ایک درست رقم درج کریں۔", "Al Madina Shezad Chhutta Services");
                errorMessage.ShowDialog();
                return;
            }

            DateTime paymentDate = dtpPaymentDate.Value;

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;
            string office = OfficeHelper.GetSelectedOffice(this);
            int dealerId = Convert.ToInt32(cbDealer.SelectedValue);
            if (!OfficeHelper.ConfirmDealerForOffice(dealerId, cbDealer.Text, office))
                return;

            if (SaveLedgerEntry(LedgerID, dealerId, newAmountPaid, txtNote.Text, paymentDate, office))
            {
                ResetAfterSaveKeepDate();
                _savedInSession = true;
            }
        }

        private bool SaveLedgerEntry(int ledgerId, int newDealerId, decimal newAmountPaid, string note, DateTime paymentDate, string office)
        {
            // 1) Agar ledgerId == 0 => naya record
            if (ledgerId == 0)
            {
                // Insert karein
                string insertQuery = @"
                    INSERT INTO DieselLedgerDebit (Did, AmounGiven, Date, Note, OfficeName) 
                    VALUES (@dealerId, @amountPaid, @paymentDate, @note, @office)";

                Hashtable ht = new Hashtable();
                ht.Add("@dealerId", newDealerId);
                ht.Add("@amountPaid", newAmountPaid);
                ht.Add("@paymentDate", paymentDate.ToString("yyyy-MM-dd"));
                ht.Add("@note", note);
                ht.Add("@office", office);

                int insertResult = MainClass.DataInsertUpdateDelete(insertQuery, ht);
                if (insertResult > 0)
                {
                    // Ab naya amount naya dealer mein add kar do
                    AddDDAmount(newDealerId, newAmountPaid);

                    CustomeMessage successMsg = new CustomeMessage("نیا ریکارڈ محفوظ ہوگیا اور ڈیلر کا بیلنس اپڈیٹ ہوگیا۔", "Al Madina Shezad Chhutta Services");
                    successMsg.ShowDialog();
                    return true;
                }
                else
                {
                    ErrorFormMessage err = new ErrorFormMessage("ڈیٹا محفوظ کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                    err.ShowDialog();
                    return false;
                }
            }
            else
            {
                // 2) Edit mode => pehle purana dealer/amount undo karo, phir naya set karo

                // Pehle DieselLedgerDebit ka record update kar dein
                string updateQuery = @"
                    UPDATE DieselLedgerDebit
                    SET Did = @newDealerId, AmounGiven = @newAmountPaid, Date = @paymentDate, Note = @note, OfficeName = @office
                    WHERE LedgerID = @LedgerID";

                Hashtable ht = new Hashtable();
                ht.Add("@newDealerId", newDealerId);
                ht.Add("@newAmountPaid", newAmountPaid);
                ht.Add("@paymentDate", paymentDate.ToString("yyyy-MM-dd"));
                ht.Add("@note", note);
                ht.Add("@office", office);
                ht.Add("@LedgerID", ledgerId);

                // Pehle hum DieselLedgerDebit ko update karein
                int updateResult = MainClass.DataInsertUpdateDelete(updateQuery, ht);
                if (updateResult > 0)
                {
                    // Ab dealer balances ko adjust karein
                    if (_oldDid != newDealerId)
                    {
                        // Agar purana dealer aur naya dealer mukhtalif hain,
                        // purana amount puranay dealer se minus karo,
                        SubtractDDAmount(_oldDid, _oldAmountPaid);

                        // aur naya amount naye dealer mein add karo
                        AddDDAmount(newDealerId, newAmountPaid);
                    }
                    else
                    {
                        // Agar dealer same hai, to bas difference handle karo
                        decimal difference = newAmountPaid - _oldAmountPaid;
                        if (difference > 0)
                        {
                            AddDDAmount(newDealerId, difference);
                        }
                        else if (difference < 0)
                        {
                            SubtractDDAmount(newDealerId, Math.Abs(difference));
                        }
                    }

                    CustomeMessage successMsg = new CustomeMessage("ریکارڈ کامیابی سے اپڈیٹ ہوگیا اور ڈیلر کے بیلنس میں تبدیلی کردی گئی۔", "Al Madina Shezad Chhutta Services");
                    successMsg.ShowDialog();
                    return true;
                }
                else
                {
                    ErrorFormMessage err = new ErrorFormMessage("ڈیٹا اپڈیٹ کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                    err.ShowDialog();
                    return false;
                }
            }
        }

        // Yeh helper method new amount add karne ke liye
        private void AddDDAmount(int dealerId, decimal amount)
        {
            if (amount == 0) return; // koi faida nahi agar 0 ho

            Hashtable ht = new Hashtable();
            ht.Add("@dealerId", dealerId);
            ht.Add("@amount", amount);

            string updateQuery = "UPDATE AddDealer SET DDAmount = DDAmount + @amount WHERE Did = @dealerId";
            MainClass.DataInsertUpdateDelete(updateQuery, ht);
        }

        // Yeh helper method amount minus karne ke liye
        private void SubtractDDAmount(int dealerId, decimal amount)
        {
            if (amount == 0) return; // koi faida nahi agar 0 ho

            Hashtable ht = new Hashtable();
            ht.Add("@dealerId", dealerId);
            ht.Add("@amount", amount);

            string updateQuery = "UPDATE AddDealer SET DDAmount = DDAmount - @amount WHERE Did = @dealerId";
            MainClass.DataInsertUpdateDelete(updateQuery, ht);
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

                AutoCompleteStringCollection autoSource = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    autoSource.Add(row["DealerName"].ToString());
                }
                txtDealerName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtDealerName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtDealerName.AutoCompleteCustomSource = autoSource;
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

        private void cbDealer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDealer.SelectedItem != null)
            {
                int dealerId = Convert.ToInt32((cbDealer.SelectedItem as DataRowView)["Did"]);
                ShowDealerBalance(dealerId);
            }
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

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (LedgerID > 0)
            {
                string selectQuery = "SELECT Did, AmounGiven FROM DieselLedgerDebit WHERE LedgerID = @LedgerID";
                Hashtable selectParams = new Hashtable();
                selectParams.Add("@LedgerID", LedgerID);

                DataTable dt = MainClass.ExecuteSelectQuery(selectQuery, selectParams);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string dealerId = dt.Rows[0]["Did"].ToString();
                    decimal amountToDelete = Convert.ToDecimal(dt.Rows[0]["AmounGiven"]);

                    YesOrNoMessage confirmDelete = new YesOrNoMessage(
                        "کیا آپ واقعی اس ریکارڈ کو حذف کرنا چاہتے ہیں؟", "حذف کی تصدیق کریں");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        string deleteQuery = "DELETE FROM DieselLedgerDebit WHERE LedgerID = @LedgerID";
                        Hashtable deleteParams = new Hashtable();
                        deleteParams.Add("@LedgerID", LedgerID);

                        int deleteResult = MainClass.DataInsertUpdateDelete(deleteQuery, deleteParams);
                        if (deleteResult > 0)
                        {
                            // Ab Dealer se DDAmount kam karo
                            SubtractDDAmount(Convert.ToInt32(dealerId), amountToDelete);

                            CustomeMessage successMessage = new CustomeMessage(
                                "ریکارڈ کامیابی کے ساتھ حذف کر دیا گیا اور ڈیلر کا DDAmount اپ ڈیٹ ہو گیا۔",
                                "Al Madina Shezad Chhutta Services");
                            successMessage.ShowDialog();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage(
                                "ڈیزل لیجر سے ریکارڈ ختم کرنے میں خرابی آئی۔", "Al Madina Shezad Chhutta Services");
                            errorMessage.ShowDialog();
                        }
                    }
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage(
                        "ڈیلر کا ID اور رقم حاصل کرنے میں خرابی آئی حذف کرنے سے پہلے۔", "Al Madina Shezad Chhutta Services");
                    errorMessage.ShowDialog();
                }
            }
            else
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage(
                    "براہ کرم حذف کرنے کے لیے ایک درست ریکارڈ منتخب کریں۔", "Al Madina Shezad Chhutta Services");
                errorMessage.ShowDialog();
            }
        }
    }
}
