using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace PetrolPump.Model
{
    public partial class CustomerLedgerAdd : SampleAdd
    {
        public int id = 0;
        public string date { get; set; }

        public CustomerLedgerAdd()
        {
            InitializeComponent();
            PopulateCustomerNames();
            LoadData();
        }

        public void PopulateCustomerNames()
        {
            string customerQuery = "SELECT id, Name FROM AddCustomer";
            string dealerQuery = "SELECT Did as id, DealerName as Name FROM AddDealer";

            DataTable combinedDt = new DataTable();
            combinedDt.Columns.Add("id", typeof(int));
            combinedDt.Columns.Add("Name", typeof(string));

            combinedDt.Merge(ExecuteSelectQuery(customerQuery));
            combinedDt.Merge(ExecuteSelectQuery(dealerQuery));

            cbName.DataSource = combinedDt;
            cbName.DisplayMember = "Name";
            cbName.ValueMember = "id";
        }

        public void SetCustomerAndDealerNames(string selectedName)
        {
            cbName.SelectedIndex = -1;
            for (int i = 0; i < cbName.Items.Count; i++)
            {
                if (cbName.Items[i] is DataRowView row && row["Name"].ToString() == selectedName)
                {
                    cbName.SelectedIndex = i;
                    txtName.Text = selectedName;
                    break;
                }
            }
        }

        private void LoadData()
        {
            if (id > 0)
            {
                string query = @"SELECT CL.sid, CL.CustomerId, C.Name AS CustomerName, D.DealerName, CL.Debit, CL.Credit, CL.Notes, CL.Date 
                         FROM CustomerLedger CL 
                         INNER JOIN AddCustomer C ON CL.CustomerId = C.id 
                         LEFT JOIN AddDealer D ON CL.CustomerId = D.Did 
                         WHERE CL.sid = @id";

                Hashtable parameters = new Hashtable { { "@id", id } };

                // ExecuteSelectQuery ko use karte hain taake DataTable return ho sake
                DataTable dt = ExecuteSelectQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    cbName.SelectedValue = row["CustomerId"];
                    txtName.Text = row["CustomerName"].ToString();
                    txtdebit.Text = row["Debit"].ToString();
                    txtcredit.Text = row["Credit"].ToString();
                    txtnote.Text = row["Notes"].ToString();
                    txtdate.Text = DateTime.TryParse(row["Date"].ToString(), out DateTime parsedDate) ? parsedDate.ToString("MM-dd-yyyy") : string.Empty;
                }
            }
        }


        public override void btnSave_Click(object sender, EventArgs e)
        {
            string query = id == 0
                ? "INSERT INTO CustomerLedger (CustomerId, Debit, Credit, Notes, Date) VALUES (@customerId, @debit, @credit, @notes, @date)"
                : "UPDATE CustomerLedger SET CustomerId = @customerId, Debit = @debit, Credit = @credit, Notes = @notes, Date = @date WHERE sid = @id";

            Hashtable parameters = new Hashtable
            {
                { "@customerId", cbName.SelectedValue },
                { "@debit", string.IsNullOrEmpty(txtdebit.Text) ? (object)DBNull.Value : Convert.ToDecimal(txtdebit.Text) },
                { "@credit", string.IsNullOrEmpty(txtcredit.Text) ? (object)DBNull.Value : Convert.ToDecimal(txtcredit.Text) },
                { "@notes", txtnote.Text },
                { "@date", Convert.ToDateTime(txtdate.Text).ToString("yyyy-MM-dd") }
            };

            if (id != 0)
                parameters.Add("@id", id);

            ExecuteNonQuery(query, parameters);
            MessageBox.Show("Saved Successfully");
            this.Close();
        }

        public override void btnDel_Click(object sender, EventArgs e)
        {
            if (id > 0)
            {
                DialogResult dialogResult = MessageBox.Show("Kya aap waqai ye record delete karna chahte hain?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    string query = "DELETE FROM CustomerLedger WHERE sid = @id";
                    Hashtable parameters = new Hashtable { { "@id", id } };
                    ExecuteNonQuery(query, parameters);
                    MessageBox.Show("Record deleted successfully.");
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Please select a valid record for deletion.");
            }
        }

        private DataTable ExecuteSelectQuery(string query)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30"))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        private void ExecuteNonQuery(string query, Hashtable parameters)
        {
            using (SqlConnection con = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30"))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                foreach (DictionaryEntry param in parameters)
                    cmd.Parameters.AddWithValue(param.Key.ToString(), param.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        private DataTable ExecuteSelectQuery(string query, Hashtable parameters)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30"))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                foreach (DictionaryEntry param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key.ToString(), param.Value);
                }

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

    }
}
