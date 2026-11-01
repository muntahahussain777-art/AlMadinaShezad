using AlMadinaShahzad.Services;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    public partial class frmStockAdd : SampleAdd1
    {
        public frmStockAdd()
        {
            InitializeComponent();
            SetupMinusLitterControls();
            txtvehicle.Leave += txtvehicle_Leave;
            txtvehicle.KeyDown += txtvehicle_KeyDown;
        }

        public int id = 0;

        // ===== NAYA CODE: purani values store karne ke liye =====
        private decimal oldAddDisel = 0m;
        private decimal oldRate = 0m;
        private int oldDealerId = 0;
        // ========================================================

        private void frmStockAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            SetupMinusLitterControls();
            BringMinusControlsToFront();
            Shown += (s, ev) => BringMinusControlsToFront();

            PopulateDealerNames();           // Existing code
            if (id == 0)
                txtdate.Value = DateTime.Now;    // Existing code

            if (id > 0)
            {
                LoadExistingRecord(id);
            }

            // NAYA: rate change pe bhi preview amount update
            txtRate.TextChanged += txtRate_TextChanged;

            this.KeyPreview = true;  // Taki Ctrl+1, Ctrl+2 capture ho sake
            this.KeyDown += frmStockAdd_KeyDown; // KeyDown event attach

            txtDealerName.Visible = false; // By default hidden
            txtDealerName.TextChanged += txtDealerName_TextChanged; // Apna event

            BeginInvoke(new Action(() =>
            {
                txtvehicle.Focus();
                txtvehicle.Select();
            }));
        }

        private void txtvehicle_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SuggestDealerByVehicle();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtvehicle_KeyDown): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtvehicle_Leave(object sender, EventArgs e)
        {
            try
            {
                SuggestDealerByVehicle();
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (txtvehicle_Leave): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void SuggestDealerByVehicle()
        {
            try
            {
                string vehicle = (txtvehicle.Text ?? "").Trim();
                if (string.IsNullOrEmpty(vehicle))
                    return;

                string qry = @"
            SELECT 
                s.DealerId,
                d.DealerName,
                COUNT(*) AS UseCount
            FROM AddStock s
            INNER JOIN AddDealer d ON d.Did = s.DealerId
            WHERE TRIM(IFNULL(s.Vehicle, '')) = @veh
            GROUP BY s.DealerId, d.DealerName
            ORDER BY UseCount DESC
            LIMIT 1";

                Hashtable ht = new Hashtable { { "@veh", vehicle } };
                DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);

                if (dt != null && dt.Rows.Count > 0)
                {
                    int dealerId = Convert.ToInt32(dt.Rows[0]["DealerId"]);
                    string dealerName = dt.Rows[0]["DealerName"].ToString();

                    if (cbDealer.DataSource != null)
                        cbDealer.SelectedValue = dealerId;

                    txtDealerName.Text = dealerName;
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error (SuggestDealerByVehicle): " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void frmStockAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.D1) // Ctrl + 1
                {
                    // TextBox show, ComboBox hide
                    txtDealerName.Visible = true;
                    cbDealer.Visible = false;
                    txtDealerName.Focus();
                }
                else if (e.KeyCode == Keys.D2) // Ctrl + 2
                {
                    // ComboBox show, TextBox hide
                    txtDealerName.Visible = false;
                    cbDealer.Visible = true;
                    cbDealer.Focus();
                }
            }
        }

        private void LoadExistingRecord(int id)
        {
            string query = "SELECT * FROM AddStock WHERE Sid = @id";
            Hashtable parameters = new Hashtable();
            parameters.Add("@id", id);

            try
            {
                DataTable dt = MainClass.ExecuteSelectQuery(query, parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtvehicle.Text = row["Vehicle"].ToString();
                    txtRate.Text = row["Rate"].ToString();
                    LoadLitterBoxesFromStored(Convert.ToDecimal(row["AddDisel"]));
                    txtdate.Value = Convert.ToDateTime(row["Date"]);
                    cbDealer.SelectedValue = row["DealerId"];
                    txtNote.Text = row["Note"].ToString();
                    OfficeHelper.SetSelectedOffice(this, Convert.ToString(row["OfficeName"]));

                    // ===== purani values yahan save kar rahe =====
                    oldDealerId = Convert.ToInt32(row["DealerId"]);
                    oldRate = Convert.ToDecimal(row["Rate"]);
                    oldAddDisel = Convert.ToDecimal(row["AddDisel"]);
                    // ========================================================

                    // NAYA: edit mode me bhi preview amount dikhao
                    UpdateLitterRateAmount();
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
            string query = "SELECT Did, DealerName FROM AddDealer LIMIT 1000"; // Existing
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
                    // Single quote handle
                    string filter = $"DealerName = '{enteredName.Replace("'", "''")}'";
                    DataRow[] rows = dt.Select(filter);

                    if (rows.Length > 0)
                    {
                        // EXACT match mila → set combobox
                        cbDealer.SelectedValue = rows[0]["Did"];
                    }
                    else
                    {
                        // match na mila → reset
                        cbDealer.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                // user ne sab text hata dia
                cbDealer.SelectedIndex = -1;
            }
        }

        // ============= NAYA HELPER: Litter * Rate preview =============
        private void UpdateLitterRateAmount()
        {
            try
            {
                decimal addLitter = ParseLitterOrZero(txtAddDisel.Text);
                decimal minusLitter = ParseLitterOrZero(txtMinusDisel?.Text);
                // Sirf active side: Add → +amount, Minus → -amount (dono mix nahi)
                decimal activeLitter = 0m;
                if (addLitter > 0 && minusLitter <= 0)
                    activeLitter = addLitter;
                else if (minusLitter > 0 && addLitter <= 0)
                    activeLitter = -minusLitter;
                else
                    activeLitter = 0m;

                string rateText = txtRate.Text.Trim();

                if (activeLitter == 0 || string.IsNullOrWhiteSpace(rateText))
                {
                    txtlitterRateAmount.Text = string.Empty;
                    return;
                }

                if (decimal.TryParse(rateText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal rate))
                {
                    decimal amount = activeLitter * rate;
                    txtlitterRateAmount.Text = amount.ToString("N2", CultureInfo.InvariantCulture);
                }
                else
                {
                    txtlitterRateAmount.Text = string.Empty;
                }
            }
            catch
            {
                txtlitterRateAmount.Text = string.Empty;
            }
        }
        // =============================================================

        // ================== SAVE BUTTON ==================
        protected override void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbDealer.Text) || cbDealer.SelectedValue == null)
            {
                CustomeMessage validationMessage = new CustomeMessage("Valid dealer select karein!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            if (!DateTime.TryParse(txtdate.Text, out DateTime specifiedDate))
            {
                CustomeMessage validationMessage = new CustomeMessage("Date valid nahi hai!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            if (!decimal.TryParse(txtRate.Text, out decimal rate))
            {
                CustomeMessage validationMessage = new CustomeMessage("Valid Rate daalein!", "Warning");
                validationMessage.ShowDialog();
                return;
            }

            decimal addDisel = GetNetLitterForSave(out string litterError);
            if (!string.IsNullOrEmpty(litterError))
            {
                CustomeMessage validationMessage = new CustomeMessage(litterError, "Warning");
                validationMessage.ShowDialog();
                return;
            }

            int dealerId = Convert.ToInt32(cbDealer.SelectedValue);
            decimal newAmount = rate * addDisel;   // naya amount (liter * rate)

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;
            string office = OfficeHelper.GetSelectedOffice(this);
            if (!OfficeHelper.ConfirmDealerForOffice(dealerId, cbDealer.Text, office))
                return;

            string qry;
            if (id == 0)
            {
                // INSERT
                qry = "INSERT INTO AddStock (Vehicle, Rate, AddDisel, Date, DealerId, Note, OfficeName) " +
                      "VALUES (@vehicle, @rate, @adddisel, @date, @did, @note, @office)";
            }
            else
            {
                // UPDATE
                qry = "UPDATE AddStock SET Vehicle = @vehicle, Rate = @rate, AddDisel = @adddisel, " +
                      "Date = @date, DealerId = @did, Note = @note, OfficeName = @office WHERE Sid = @id";
            }

            Hashtable ht = new Hashtable
            {
                { "@id", id },
                { "@did", dealerId },
                { "@office", office },
                { "@vehicle", string.IsNullOrWhiteSpace(txtvehicle.Text) ? (object)DBNull.Value : txtvehicle.Text },
                { "@rate", rate },
                { "@adddisel", addDisel },
                { "@date", specifiedDate.ToString("yyyy-MM-dd") },
                { "@note", string.IsNullOrWhiteSpace(txtNote.Text) ? (object)DBNull.Value : txtNote.Text }
            };

            try
            {
                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    if (id == 0)
                    {
                        // INSERT MODE
                        UpdateDealerDAmount(dealerId, newAmount);
                    }
                    else
                    {
                        // UPDATE MODE: purana minus + naya plus
                        if (oldDealerId == 0)
                        {
                            string getOldQry = "SELECT DealerId, Rate, AddDisel FROM AddStock WHERE Sid = @id";
                            Hashtable htOld = new Hashtable();
                            htOld.Add("@id", id);
                            DataTable dtOld = MainClass.ExecuteSelectQuery(getOldQry, htOld);
                            if (dtOld != null && dtOld.Rows.Count > 0)
                            {
                                oldDealerId = Convert.ToInt32(dtOld.Rows[0]["DealerId"]);
                                oldRate = Convert.ToDecimal(dtOld.Rows[0]["Rate"]);
                                oldAddDisel = Convert.ToDecimal(dtOld.Rows[0]["AddDisel"]);
                            }
                        }

                        decimal oldAmount = oldRate * oldAddDisel;

                        if (dealerId == oldDealerId)
                        {
                            string updSameDealer = "UPDATE AddDealer " +
                                                   "SET DDAmount = DDAmount - @oldAmount + @newAmount " +
                                                   "WHERE Did = @Did";

                            Hashtable htSame = new Hashtable();
                            htSame.Add("@oldAmount", oldAmount);
                            htSame.Add("@newAmount", newAmount);
                            htSame.Add("@Did", dealerId);

                            MainClass.DataInsertUpdateDelete(updSameDealer, htSame);
                        }
                        else
                        {
                            string updOldDealer = "UPDATE AddDealer " +
                                                  "SET DDAmount = DDAmount - @oldAmount " +
                                                  "WHERE Did = @OldDid";

                            Hashtable htOldDealer = new Hashtable();
                            htOldDealer.Add("@oldAmount", oldAmount);
                            htOldDealer.Add("@OldDid", oldDealerId);

                            MainClass.DataInsertUpdateDelete(updOldDealer, htOldDealer);

                            string updNewDealer = "UPDATE AddDealer " +
                                                  "SET DDAmount = DDAmount + @newAmount " +
                                                  "WHERE Did = @NewDid";

                            Hashtable htNewDealer = new Hashtable();
                            htNewDealer.Add("@newAmount", newAmount);
                            htNewDealer.Add("@NewDid", dealerId);

                            MainClass.DataInsertUpdateDelete(updNewDealer, htNewDealer);
                        }
                    }

                    CustomeMessage successMessage = new CustomeMessage("Entry save ho gayi!", "Success");
                    successMessage.ShowDialog();
                    MainClass.Enable_reset_keep_date(this, txtdate);
                    id = 0;
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("Data save nahi hua!", "Error");
                    errorMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }
        // ================== btnSave_Click yahan tak ==================


        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                string dealerIdQuery = "SELECT DealerId, Rate, AddDisel FROM AddStock WHERE Sid = @id";
                Hashtable htGetDealerId = new Hashtable();
                htGetDealerId.Add("@id", id);

                int dealerId = 0;
                decimal addDisel = 0;
                decimal rate = 0;

                try
                {
                    DataTable dealerIdTable = MainClass.ExecuteSelectQuery(dealerIdQuery, htGetDealerId);
                    if (dealerIdTable != null && dealerIdTable.Rows.Count > 0)
                    {
                        dealerId = Convert.ToInt32(dealerIdTable.Rows[0]["DealerId"]);
                        rate = Convert.ToDecimal(dealerIdTable.Rows[0]["Rate"]);
                        addDisel = Convert.ToDecimal(dealerIdTable.Rows[0]["AddDisel"]);
                    }
                    else
                    {
                        CustomeMessage noDataMessage = new CustomeMessage("Koi record nahi mila ID ke liye!", "Warning");
                        noDataMessage.ShowDialog();
                        return;
                    }

                    YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                    if (confirmDelete.ShowDialog() == DialogResult.Yes)
                    {
                        string qry = "DELETE FROM AddStock WHERE Sid = @id";
                        Hashtable htDelete = new Hashtable();
                        htDelete.Add("@id", id);

                        int deleteResult = MainClass.DataInsertUpdateDelete(qry, htDelete);
                        if (deleteResult > 0)
                        {
                            if (dealerId > 0)
                            {
                                decimal ddAmountToSubtract = rate * addDisel;
                                UpdateDealerDAmount(dealerId, -ddAmountToSubtract);
                            }
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

        private void UpdateDealerDAmount(int Did, decimal ddAmount)
        {
            string qry = "UPDATE AddDealer SET DDAmount = DDAmount + @ddAmount WHERE Did = @Did";
            Hashtable ht = new Hashtable();
            ht.Add("@Did", Did);
            ht.Add("@ddAmount", ddAmount);

            try
            {
                int result = MainClass.DataInsertUpdateDelete(qry, ht);
                if (result > 0)
                {
                    CustomeMessage successMessage = new CustomeMessage("AddDealer amount update ho gaya!", "Success");
                    successMessage.ShowDialog();
                }
                else
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("AddDealer amount update nahi hua!", "Error");
                    errorMessage.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                errorMessage.ShowDialog();
            }
        }

        private void txtAddDisel_Leave(object sender, EventArgs e)
        {
            FormatPositiveLitterBox(txtAddDisel);
            UpdateLitterRateAmount();
        }

        private void txtAddDisel_TextChanged(object sender, EventArgs e)
        {
            KeepLitterCaretAtEndWhileTyping(txtAddDisel);
            EnforceExclusiveLitterBoxes(txtAddDisel);
            UpdateLitterRateAmount();
        }

        private void txtAddDisel_KeyPress(object sender, KeyPressEventArgs e)
        {
            PositiveLitter_KeyPress(sender, e);
        }

        private void txtRate_KeyPress(object sender, KeyPressEventArgs e)
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

        // NAYA: jab rate change ho, preview update
        private void txtRate_TextChanged(object sender, EventArgs e)
        {
            UpdateLitterRateAmount();
        }
    }
}