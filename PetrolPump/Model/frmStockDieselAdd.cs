using AlMadinaShahzad.Services;
using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmStockDieselAdd : SampleAdd1
    {
        public frmStockDieselAdd()
        {
            InitializeComponent();
            SetupMinusLitterControls();
        }

        public int id = 0;

        private void frmStockDieselAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            SetupMinusLitterControls();
            BringMinusControlsToFront();
            Shown += (s, ev) => BringMinusControlsToFront();

            PopulateDealerNames();      // Existing code
            if (id == 0)
                txtdate.Value = DateTime.Now; // Existing code

            if (id > 0)
            {
                LoadExistingRecord(id);
            }

            // 🔹 KeyPreview for Ctrl+1 / Ctrl+2
            this.KeyPreview = true;
            this.KeyDown += frmStockDieselAdd_KeyDown;

            // 🔹 TextBox by default hidden
            txtDealerName.Visible = false;

            // 🔹 AutoComplete text changed event
            txtDealerName.TextChanged += txtDealerName_TextChanged;
        }

        private void frmStockDieselAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1)
                {
                    // Ctrl+1 → Show TextBox, Hide ComboBox
                    txtDealerName.Visible = true;
                    cbDealer.Visible = false;
                    txtDealerName.Focus();
                }
                else if (e.KeyCode == Keys.D2)
                {
                    // Ctrl+2 → Show ComboBox, Hide TextBox
                    txtDealerName.Visible = false;
                    cbDealer.Visible = true;
                    cbDealer.Focus();
                }
            }
        }

        private void LoadExistingRecord(int id)
        {
            string query = "SELECT * FROM StockDiesel WHERE SID = @id";
            Hashtable parameters = new Hashtable();
            parameters.Add("@id", id);

            try
            {
                DataTable dt = MainClass.ExecuteSelectQuery(query, parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtdate.Value = Convert.ToDateTime(row["Date"]);
                    txtvehicle.Text = row["Vehicle"].ToString();

                    // 🔹 SAFE: agar SDid ka dealer ab AddDealer me nahi hai to form crash na kare
                    if (row["SDid"] != DBNull.Value)
                    {
                        int dealerId;
                        if (int.TryParse(row["SDid"].ToString(), out dealerId))
                        {
                            cbDealer.SelectedValue = dealerId;

                            if (cbDealer.SelectedIndex == -1)
                            {
                                // Dealer delete ho chuka / nahi mil raha
                                // ComboBox clear + textbox me sirf information dikhado (optional)
                                cbDealer.SelectedIndex = -1;
                                txtDealerName.Visible = true;
                                cbDealer.Visible = false;
                                txtDealerName.Text = "(Missing Dealer ID: " + dealerId + ")";
                            }
                        }
                    }

                    LoadLitterBoxesFromRecord(
                        Convert.ToDecimal(row["Litter"]),
                        row["Note"] != DBNull.Value ? row["Note"].ToString() : "");
                    txtRate.Text = row["Rate"].ToString();
                    txtcredit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", row["Credit"]);
                    txtdebit.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", row["Debit"]);
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
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void PopulateDealerNames()
        {
            string query = "SELECT Did, DealerName FROM AddDealer LIMIT 1000";
            try
            {
                DataTable dt = MainClass.ExecuteSelectQuery(query, null);

                if (dt != null && dt.Rows.Count > 0)
                {
                    cbDealer.DataSource = dt;
                    cbDealer.DisplayMember = "DealerName";
                    cbDealer.ValueMember = "Did";
                    cbDealer.DropDownHeight = 200;
                    cbDealer.IntegralHeight = false;

                    // AutoComplete for txtDealerName
                    AutoCompleteStringCollection autoSource = new AutoCompleteStringCollection();
                    foreach (DataRow row in dt.Rows)
                    {
                        autoSource.Add(row["DealerName"].ToString());
                    }
                    txtDealerName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtDealerName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtDealerName.AutoCompleteCustomSource = autoSource;
                }
                else
                {
                    CustomeMessage noDataMessage = new CustomeMessage("Koi dealer nahi mila!", "Warning");
                    noDataMessage.ShowDialog();
                    cbDealer.DataSource = null;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
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

        protected override void btnSave_Click(object sender, EventArgs e)
        {
            // 🔹 1) Dealer selection strong validation
            if (cbDealer.SelectedValue == null || cbDealer.SelectedIndex == -1)
            {
                CustomeMessage validationMessage = new CustomeMessage("Valid dealer select karein!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            int dealerId;
            if (!int.TryParse(cbDealer.SelectedValue.ToString(), out dealerId) || dealerId <= 0)
            {
                CustomeMessage validationMessage = new CustomeMessage("Invalid dealer selected!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            // 🔹 2) Date validation
            if (!DateTime.TryParse(txtdate.Text, out DateTime specifiedDate))
            {
                CustomeMessage validationMessage = new CustomeMessage("Date valid nahi hai!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            // 🔹 3) Numeric parsing (same as before)
            decimal rate = decimal.TryParse(txtRate.Text.Replace(",", ""), out decimal parsedRate) ? parsedRate : 0;
            if (!TryGetLitterSaveDetails(out decimal litterToSave, out bool isMinusEntry, out string litterError))
            {
                CustomeMessage validationMessage = new CustomeMessage(litterError, "Warning");
                validationMessage.ShowDialog();
                return;
            }
            decimal creditAmount = decimal.TryParse(txtcredit.Text.Replace(",", ""), out decimal parsedCredit) ? parsedCredit : 0;
            decimal debitAmount = decimal.TryParse(txtdebit.Text.Replace(",", ""), out decimal parsedDebit) ? parsedDebit : 0;
            string noteToSave = StockDieselLitterHelper.BuildNoteForSave(isMinusEntry, txtNote.Text);

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;
            string office = OfficeHelper.GetSelectedOffice(this);
            if (!OfficeHelper.ConfirmDealerForOffice(dealerId, cbDealer.Text, office))
                return;

            string qry = id == 0
                ? "INSERT INTO StockDiesel (Vehicle, Rate, Litter, Date, SDid, Credit, Debit, Note, OfficeName) " +
                  "VALUES (@vehicle, @rate, @adddisel, @date, @did, @credit, @debit, @note, @office)"
                : "UPDATE StockDiesel SET Vehicle = @vehicle, Rate = @rate, Litter = @adddisel, Date = @date, " +
                  "SDid = @did, Credit = @credit, Debit = @debit, Note = @note, OfficeName = @office WHERE SID = @id";

            // 🔹 4) @did hamesha valid int dealerId hoga
            Hashtable ht = new Hashtable
            {
                { "@id", id },
                { "@did", dealerId },
                { "@office", office },
                { "@vehicle", string.IsNullOrWhiteSpace(txtvehicle.Text) ? (object)DBNull.Value : txtvehicle.Text },
                { "@rate", rate },
                { "@adddisel", litterToSave },
                { "@date", specifiedDate.ToString("yyyy-MM-dd") },
                { "@credit", creditAmount },
                { "@debit", debitAmount },
                { "@note", string.IsNullOrWhiteSpace(noteToSave) ? (object)DBNull.Value : noteToSave }
            };

            try
            {
                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    CustomeMessage successMessage = new CustomeMessage("Stock entry save ho gayi!", "Success");
                    successMessage.ShowDialog();
                    MainClass.Enable_reset_keep_date(this, txtdate);
                    id = 0;
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Stock data save nahi hua!", "Error");
                    errorMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                try
                {
                    YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        string qry = "DELETE FROM StockDiesel WHERE SID = @id";
                        Hashtable htDelete = new Hashtable();
                        htDelete.Add("@id", id);

                        int deleteResult = MainClass.DataInsertUpdateDelete(qry, htDelete);
                        if (deleteResult > 0)
                        {
                            CustomeMessage successMessage = new CustomeMessage("Record delete ho gaya!", "Success");
                            successMessage.ShowDialog();
                            MainClass.Enable_reset_keep_date(this, txtdate);
                            id = 0;
                        }
                        else
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage("Stock record delete nahi hua!", "Error");
                            errorMessage.ShowDialog();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                    errorMessage.ShowDialog();
                }
            }
            else
            {
                CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek record select karein!", "Warning");
                noSelectionMessage.ShowDialog();
            }
        }

        private void txtlitter_Leave(object sender, EventArgs e)
        {
            FormatPositiveLitterBox(txtlitter);
        }

        private void txtlitter_TextChanged(object sender, EventArgs e)
        {
            KeepLitterCaretAtEndWhileTyping(txtlitter);
            EnforceExclusiveLitterBoxes(txtlitter);
        }

        private void txtlitter_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtRate_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtcredit_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtdebit_KeyPress(object sender, KeyPressEventArgs e)
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
    }
}
