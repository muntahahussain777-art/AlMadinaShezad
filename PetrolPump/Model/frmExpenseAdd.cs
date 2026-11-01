using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmExpenseAdd : SampleAdd
    {
        public frmExpenseAdd()
        {
            InitializeComponent();
        }
        public int id = 0;

        private void frmExpenseAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            LoadData();
            if (id == 0)
                txtdate.Value = DateTime.Today;
        }

        private void LoadData()
        {
            if (id > 0)
            {
                try
                {
                    string qry = "SELECT * FROM Expensetable WHERE sid = @id";
                    Hashtable ht = new Hashtable();
                    ht.Add("@id", id);

                    DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        txtname.Text = row["Name"].ToString();
                        txtcategory.Text = row["Category"].ToString();
                        txtamount.Text = row["Amount"].ToString();
                        if (dt.Columns.Contains("OfficeName"))
                            OfficeHelper.SetSelectedOffice(this, Convert.ToString(row["OfficeName"]));

                        if (row["EDate"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(row["EDate"].ToString(), out DateTime dateValue))
                            {
                                if (dateValue >= DateTimePicker.MinimumDateTime && dateValue <= DateTimePicker.MaximumDateTime)
                                {
                                    txtdate.Value = dateValue;
                                }
                                else
                                {
                                    CustomeMessage rangeMessage = new CustomeMessage("Date DateTimePicker ke range se bahar hai!", "Warning");
                                    rangeMessage.ShowDialog();
                                    txtdate.Value = DateTime.Now;
                                }
                            }
                            else
                            {
                                CustomeMessage formatMessage = new CustomeMessage("EDate ka format ghalat hai!", "Warning");
                                formatMessage.ShowDialog();
                                txtdate.Value = DateTime.Now;
                            }
                        }
                        else
                        {
                            txtdate.Value = DateTime.Now;
                        }

                        txtnote.Text = row["Note"].ToString();
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
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtname.Text) || string.IsNullOrWhiteSpace(txtcategory.Text) || string.IsNullOrWhiteSpace(txtamount.Text))
                {
                    CustomeMessage validationMessage = new CustomeMessage("Name, category, aur amount zaroori hain!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!decimal.TryParse(txtamount.Text, out decimal amount))
                {
                    CustomeMessage validationMessage = new CustomeMessage("Valid amount daalein!", "Warning");
                    validationMessage.ShowDialog();
                    return;
                }

                if (!OfficeHelper.RequireOfficeSelected(this))
                    return;
                string office = OfficeHelper.GetSelectedOffice(this);

                string qry = id == 0 ?
                    "INSERT INTO Expensetable (Name, Category, Amount, EDate, Note, OfficeName) VALUES (@name, @category, @amount, @date, @note, @office)" :
                    "UPDATE Expensetable SET Name = @name, Category = @category, Amount = @amount, EDate = @date, Note = @note, OfficeName = @office WHERE sid = @id";

                Hashtable ht = new Hashtable
                {
                    { "@id", id },
                    { "@name", txtname.Text },
                    { "@category", txtcategory.Text },
                    { "@amount", amount },
                    { "@date", txtdate.Value.ToString("yyyy-MM-dd") },
                    { "@note", txtnote.Text },
                    { "@office", office }
                };

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    CustomeMessage successMessage = new CustomeMessage("Entry save ho gayi!", "Success");
                    successMessage.ShowDialog();
                    MainClass.Enable_reset(this);
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

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                YesOrNoMessage confirmDelete = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
                if (confirmDelete.ShowDialog() == DialogResult.Yes)
                {
                    try
                    {
                        string qry = "DELETE FROM Expensetable WHERE sid = @id";
                        Hashtable ht = new Hashtable();
                        ht.Add("@id", id);

                        int r = MainClass.DataInsertUpdateDelete(qry, ht);
                        if (r > 0)
                        {
                            CustomeMessage successMessage = new CustomeMessage("Record delete ho gaya!", "Success");
                            successMessage.ShowDialog();
                            MainClass.Enable_reset(this);
                            id = 0;
                        }
                        else
                        {
                            ErrorFormMessage errorMessage = new ErrorFormMessage("Record delete nahi hua!", "Error");
                            errorMessage.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorFormMessage errorMessage = new ErrorFormMessage("Error: " + ex.Message, "Error");
                        errorMessage.ShowDialog();
                    }
                }
            }
            else
            {
                CustomeMessage noSelectionMessage = new CustomeMessage("Pehle ek record select karein!", "Warning");
                noSelectionMessage.ShowDialog();
            }
        }

        private void txtamount_KeyPress(object sender, KeyPressEventArgs e)
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
    }
}