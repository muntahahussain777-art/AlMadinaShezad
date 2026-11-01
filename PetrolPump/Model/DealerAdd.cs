using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class DealerAdd : SampleAdd
    {
        public DealerAdd()
        {
            InitializeComponent();
        }

        public int id = 0;
        private decimal DAmount = 0;  // Variable to store DAmount from database
        private bool _savedInSession;

        private void DealerAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            txtmobile.TextChanged += new EventHandler(txtmobile_TextChanged); // Bind event

            if (id > 0) // Agar id zero se zyada hai to existing customer data ko load karo
            {
                LoadCustomerData(); // Naya method call karte hain
            }
            else
            {
                txtdate.Value = DateTime.Now;
            }

            this.FormClosing -= DealerAdd_FormClosing;
            this.FormClosing += DealerAdd_FormClosing;
        }

        private void DealerAdd_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_savedInSession && DialogResult == DialogResult.None)
                DialogResult = DialogResult.OK;
        }

        private void LoadCustomerData()
        {
            // Query to fetch dealer data by id
            string qry = "SELECT DealerName, DDAmount, DAmount, Date, IFNULL(OfficeName,'') AS OfficeName FROM AddDealer WHERE Did = @id";
            Hashtable ht = new Hashtable();
            ht.Add("@id", id);

            DataTable dt = MainClass.ExecuteSelectQuery(qry, ht); // Updated method name
            if (dt.Rows.Count > 0)
            {
                txtname.Text = dt.Rows[0]["DealerName"].ToString();
                txtmobile.Text = dt.Rows[0]["DDAmount"].ToString();
                txtdate.Text = dt.Rows[0]["Date"].ToString();
                OfficeHelper.SetSelectedOffice(this, Convert.ToString(dt.Rows[0]["OfficeName"]));

                // Fetch the DAmount and store it in the variable
                DAmount = Convert.ToDecimal(dt.Rows[0]["DAmount"]);
            }
        }

        private void txtmobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only digits, backspace, and hyphen
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back && e.KeyChar != '-')
            {
                e.Handled = true;
            }

            // Check if input length is 11 characters
            if (txtmobile.Text.Length == 12 && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }

            // Automatically add hyphen after 4th digit
            if (txtmobile.Text.Length == 4 && e.KeyChar != (char)Keys.Back)
            {
                txtmobile.Text += "-";
                txtmobile.SelectionStart = txtmobile.Text.Length; // Set cursor to end
            }
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtname.Text))
            {
                ErrorFormMessage customMessageBox = new ErrorFormMessage("Name is required.", "غلطی");
                customMessageBox.ShowDialog();
                return;
            }

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;

            string office = OfficeHelper.GetSelectedOffice(this);
            string nameTrim = txtname.Text.Trim();

            if (id == 0)
            {
                DataTable existing = MainClass.ExecuteSelectQuery(
                    "SELECT Did, IFNULL(OfficeName,'') AS OfficeName FROM AddDealer WHERE TRIM(DealerName)=@n COLLATE NOCASE LIMIT 1",
                    new Hashtable { { "@n", nameTrim } });
                if (existing != null && existing.Rows.Count > 0)
                {
                    string existingOffice = Convert.ToString(existing.Rows[0]["OfficeName"]) ?? "";
                    if (!OfficeHelper.ConfirmCrossOfficeUse("dealer", nameTrim, existingOffice, office))
                        return;
                    CustomeMessage info = new CustomeMessage(
                        "Ye dealer pehle se maujood hai.\nDealer amount / payout / stock mein isi dealer ko select karein (duplicate nahi banega).",
                        "Info");
                    info.ShowDialog();
                    return;
                }
            }
            else
            {
                string homeOffice = OfficeHelper.GetDealerOffice(id);
                if (!OfficeHelper.ConfirmCrossOfficeUse("dealer", nameTrim, homeOffice, office))
                    return;
            }

            // Validate and parse the date
            DateTime dateValue;
            if (!DateTime.TryParse(txtdate.Text, out dateValue))
            {
                MessageBox.Show("Invalid date format. Please use yyyy-MM-dd format.");
                return;
            }

            // Prepare query for database
            string qry = "";
            if (id == 0)
            {
                qry = "INSERT INTO AddDealer (DealerName, DDAmount, DAmount, Date, OfficeName) VALUES (@name, @Amount, @DAmount, @date, @office)";
            }
            else
            {
                qry = "UPDATE AddDealer SET DealerName = @name, DDAmount = @Amount, DAmount = @DAmount, Date = @date, OfficeName = @office WHERE Did = @id";
            }

            Hashtable ht = new Hashtable
    {
        { "@id", id },
        { "@name", nameTrim },
        // Set DDAmount to 0 if txtmobile is empty
        { "@Amount", string.IsNullOrWhiteSpace(txtmobile.Text) ? 0 : Convert.ToDecimal(txtmobile.Text) },
        { "@DAmount", DAmount },  // DAmount from the variable
        { "@date", dateValue.ToString("yyyy-MM-dd") }, // Convert date to proper format
        { "@office", office }
    };

            int r = MainClass.DataInsertUpdateDelete(qry, ht);
            if (r > 0)
            {
                CustomeMessage customMessageBox = new CustomeMessage("Saved Successfully", "Save");
                customMessageBox.ShowDialog();
                MainClass.Enable_reset_keep_date(this, txtdate);
                id = 0;
                _savedInSession = true;
            }
            else
            {
                MessageBox.Show("Error saving data.");
            }
        }

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                // Pehle check karo ke DieselLedgerCredit mein entry hai ya nahi
                string checkQuery = "SELECT COUNT(*) FROM DieselLedgerCredit WHERE Did = @id";
                Hashtable checkHt = new Hashtable();
                checkHt.Add("@id", id);

                int count = Convert.ToInt32(MainClass.ExecuteScalar(checkQuery, checkHt)); // Count nikalne ke liye

                if (count > 0) // Agar entry mojood hai
                {
                    ErrorFormMessage errorMessage = new ErrorFormMessage("یہ ڈیلر ڈیلر پے آؤٹ میں موجود ہے، اسے ڈیلیٹ نہیں کیا جا سکتا۔", "غلطی");
                    errorMessage.ShowDialog();
                    return; // Delete nahi hoga
                }

                // Agar DieselLedgerCredit mein entry nahi hai to delete karo
                string qry = "DELETE FROM AddDealer WHERE Did = @id";
                Hashtable ht = new Hashtable();
                ht.Add("@id", id);

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    CustomeMessage customMessageBox = new CustomeMessage("ڈیلیٹ کامیابی سے ہو گیا۔", "ڈیلیٹ");
                    customMessageBox.ShowDialog();
                    MainClass.Enable_reset_keep_date(this, txtdate);
                    id = 0;
                }
                else
                {
                    MessageBox.Show("ڈیٹا ڈیلیٹ کرنے میں خرابی ہوئی۔");
                }
            }
        }
        private void txtmobile_TextChanged(object sender, EventArgs e)
        {
            // Remove any existing commas
            string currentText = txtmobile.Text.Replace(",", "");

            // Check if the current text is a valid number
            if (decimal.TryParse(currentText, out decimal value))
            {
                // Format the value with commas and set it back to the TextBox
                txtmobile.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0}", value);

                // Set the caret position to the end of the text
                txtmobile.SelectionStart = txtmobile.Text.Length;
            }
        }

        private void txtmobile_KeyPress_1(object sender, KeyPressEventArgs e)
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
    }
}
