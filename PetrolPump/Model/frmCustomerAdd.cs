using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad.Model
{
    public partial class frmCustomerAdd : SampleAdd
    {
        public frmCustomerAdd()
        {
            InitializeComponent();
            txtmobile.KeyPress += new KeyPressEventHandler(txtmobile_KeyPress);
        }

        public int id = 0;

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtname.Text))
            {
                new CustomeMessage("Name zaroori hai!", "Warning").ShowDialog();
                return;
            }

            if (!OfficeHelper.RequireOfficeSelected(this))
                return;

            string office = OfficeHelper.GetSelectedOffice(this);
            string nameTrim = txtname.Text.Trim();

            if (id == 0)
            {
                DataTable existing = MainClass.ExecuteSelectQuery(
                    "SELECT id, IFNULL(OfficeName,'') AS OfficeName FROM AddCustomer WHERE TRIM(Name)=@n COLLATE NOCASE LIMIT 1",
                    new Hashtable { { "@n", nameTrim } });
                if (existing != null && existing.Rows.Count > 0)
                {
                    string existingOffice = Convert.ToString(existing.Rows[0]["OfficeName"]) ?? "";
                    if (!OfficeHelper.ConfirmCrossOfficeUse("customer", nameTrim, existingOffice, office))
                        return;
                    new CustomeMessage(
                        "Ye customer pehle se maujood hai.\nDaily diesel / entries mein isi customer ko select karein (duplicate nahi banega).",
                        "Info").ShowDialog();
                    return;
                }
            }
            else
            {
                string homeOffice = OfficeHelper.GetCustomerOffice(id);
                if (!OfficeHelper.ConfirmCrossOfficeUse("customer", nameTrim, homeOffice, office))
                    return;
            }

            DateTime dateValue;
            if (!DateTime.TryParse(txtdate.Text, out dateValue))
            {
                new CustomeMessage("Date ka format ghalat hai!", "Warning").ShowDialog();
                return;
            }

            string qry = id == 0
                ? "INSERT INTO AddCustomer (Name, Mobile, Date, OfficeName) VALUES (@name, @mobile, @date, @office)"
                : "UPDATE AddCustomer SET Name=@name, Mobile=@mobile, Date=@date, OfficeName=@office WHERE id=@id";

            Hashtable ht = new Hashtable
            {
                { "@id", id },
                { "@name", nameTrim },
                { "@mobile", txtmobile.Text.Trim() },
                { "@date", dateValue },
                { "@office", office }
            };

            try
            {
                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    new CustomeMessage("Record save ho gaya!", "Success").ShowDialog();
                    MainClass.Enable_reset(this);
                    id = 0;
                }
                else
                {
                    new ErrorFormMessage("Data save nahi hua!", "Error").ShowDialog();
                }
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Error: " + ex.Message, "Error").ShowDialog();
            }
        }

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id <= 0)
            {
                new CustomeMessage("Pehle record select karein!", "Warning").ShowDialog();
                return;
            }

            // ✅ Check dependencies in Diesel App related tables
            string blockingTable = CheckCustomerDependencies(id);
            if (blockingTable != null)
            {
                new ErrorFormMessage(
                    $"Ye customer delete nahi ho sakta kyunki iski entry '{blockingTable}' table me maujood hai!",
                    "Delete Blocked").ShowDialog();
                return;
            }

            // ✅ Confirm before delete
            YesOrNoMessage confirm = new YesOrNoMessage("Kya aap is record ko delete karna chahte hain?", "Confirm Delete");
            if (confirm.ShowDialog() != DialogResult.Yes) return;

            try
            {
                string qry = "DELETE FROM AddCustomer WHERE id=@id";
                Hashtable ht = new Hashtable { { "@id", id } };

                int result = MainClass.DataInsertUpdateDelete(qry, ht);
                if (result > 0)
                {
                    new CustomeMessage("Record delete ho gaya!", "Success").ShowDialog();
                    MainClass.Enable_reset(this);
                    id = 0;
                }
                else
                {
                    new ErrorFormMessage("Record delete nahi hua!", "Error").ShowDialog();
                }
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Error: " + ex.Message, "Error").ShowDialog();
            }
        }

        private string CheckCustomerDependencies(int customerId)
        {
            try
            {
                // ✅ Diesel app ke customer-related tables aur columns
                var tables = new (string Table, string Column)[]
                {
                    ("BankTransactions", "CustomerId"),
                    ("CustomerToCustomer", "id"),
                    ("DieselLedger", "id"),
                    ("PetrolAdd", "CustomerId")
                };

                foreach (var t in tables)
                {
                    string qry = $"SELECT COUNT(1) FROM {t.Table} WHERE {t.Column} = @cid";
                    Hashtable ht = new Hashtable { { "@cid", customerId } };

                    DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
                    if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                    {
                        return t.Table; // blocking table name return
                    }
                }
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Dependency check error: " + ex.Message, "Error").ShowDialog();
            }
            return null;
        }

        private void frmCustomerAdd_Load(object sender, EventArgs e)
        {
            OfficeHelper.AttachOfficeCombo(this, includeAll: false, left: 420, top: 55);
            if (id > 0)
            {
                LoadCustomerData();
            }
            txtdate.Value = DateTime.Now;
        }

        private void LoadCustomerData()
        {
            string qry = "SELECT Name, Mobile, Date, IFNULL(OfficeName,'') AS OfficeName FROM AddCustomer WHERE id=@id";
            Hashtable ht = new Hashtable { { "@id", id } };

            DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
            if (dt.Rows.Count > 0)
            {
                txtname.Text = dt.Rows[0]["Name"].ToString();
                txtmobile.Text = dt.Rows[0]["Mobile"].ToString();
                OfficeHelper.SetSelectedOffice(this, Convert.ToString(dt.Rows[0]["OfficeName"]));

                DateTime parsedDate;
                if (DateTime.TryParse(dt.Rows[0]["Date"].ToString(), out parsedDate))
                    txtdate.Value = parsedDate;
            }
        }

        private void txtmobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back && e.KeyChar != '-')
            {
                e.Handled = true;
            }

            if (txtmobile.Text.Length == 12 && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }

            if (txtmobile.Text.Length == 4 && e.KeyChar != (char)Keys.Back)
            {
                txtmobile.Text += "-";
                txtmobile.SelectionStart = txtmobile.Text.Length;
            }
        }
    }
}
