using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlMadinaShahzad.ProjectConnection;
using AlMadinaShahzad.Services;
using System.Data.SQLite;
using System.IO;

namespace AlMadinaShahzad
{
    class MainClass
    {
        // Connection string ko ProjectConnection class se le rahe hain
        public static readonly string con_string = projectconnection.ConnectionString;  // Assuming projectconnection.ConnectionString is the SQLite connection string

        // SQLiteConnection object (replace SqlConnection with SQLiteConnection)
        public static SQLiteConnection con = new SQLiteConnection(con_string);
        public static string USER { get; set; }

        // Declare IMG as Image to store the user's image
        public static Image IMG { get; set; }

        public static bool isValiduser(string user, string pass)
        {
            bool isvalid = false;
            string qry = @"select * from users where uUsername ='" + user + "' and uPass = '" + pass + "'";

            // Assuming 'con' is your SQLiteConnection object
            SQLiteCommand cmd = new SQLiteCommand(qry, con);
            DataTable dt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                isvalid = true;

                // Assigning user name to USER (which is a string)
                USER = dt.Rows[0]["uName"].ToString();

                // Assigning image to IMG (which is an Image)
                byte[] ImageArray = (byte[])dt.Rows[0]["uImage"];
                IMG = Image.FromStream(new MemoryStream(ImageArray)); // Store image in the global IMG variable
            }

            return isvalid;
        }
        public static object ExecuteScalar(string query, Hashtable parameters)
        {
            object result = null;

            using (SQLiteConnection connection = new SQLiteConnection(projectconnection.ConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    // Add parameters to the command
                    if (parameters != null)
                    {
                        foreach (DictionaryEntry param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key.ToString(), param.Value);
                        }
                    }

                    // Execute scalar query and get the result
                    result = command.ExecuteScalar();
                }
            }

            return result; // Return the scalar result
        }
        public static void loadData1(string query, DataGridView dgv, ListBox lb, Hashtable parameters)
        {
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(query, MainClass.con);
                foreach (DictionaryEntry param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key.ToString(), param.Value); // Bind parameters
                }
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt;

                // Set headers based on ListBox
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (!lb.Items.Contains(col.Name))
                    {
                        col.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void loadData(string qry, DataGridView gv, ListBox lb)
        {
            try
            {
                // Ensure the connection is open before executing the query
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }

                SQLiteCommand cmd = new SQLiteCommand(qry, con);
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                // Check if ListBox is not null
                if (lb != null && lb.Items.Count > 0)
                {
                    // Ensure the number of columns matches the DataGridView columns
                    for (int i = 0; i < lb.Items.Count; i++)
                    {
                        if (i < dt.Columns.Count)
                        {
                            string colName1 = ((DataGridViewColumn)lb.Items[i]).Name;

                            // Check if the column exists in DataGridView before assigning
                            if (gv.Columns.Contains(colName1))
                            {
                                gv.Columns[colName1].DataPropertyName = dt.Columns[i].ColumnName;
                            }
                        }
                    }
                }

                // Bind the data to the DataGridView
                gv.DataSource = dt;

                // Close the connection after the operation
                con.Close();
            }
            catch (Exception ex)
            {
                // Ensure connection is closed in case of an exception
                con.Close();
                MessageBox.Show(ex.ToString());
            }
        }
        public static void LoadDataIntoComboBox(string query, ComboBox cb, string displayMember, string valueMember)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection con = new SQLiteConnection(con_string))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    da.Fill(dt);
                }
            }
            cb.DisplayMember = displayMember;
            cb.ValueMember = valueMember;
            cb.DataSource = dt;
            cb.SelectedIndex = -1; // To not select any item by default
        }
        public static DataTable ExecuteSelectQuery(string query, Hashtable parameters)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(con_string))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        if (parameters != null)
                        {
                            foreach (DictionaryEntry parameter in parameters)
                            {
                                cmd.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);
                            }
                        }

                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching data: " + ex.Message);
            }
            return dt;
        }

        public static DataTable GetData(string qry)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(qry, con);
                cmd.CommandType = CommandType.Text;
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                con.Close();
                MessageBox.Show(ex.ToString());
            }
            return dt;
        }
        public static int DataInsertUpdateDelete(string qry, Hashtable ht)
        {
            int res = 0;
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(qry, con);
                cmd.CommandType = CommandType.Text;
                foreach (DictionaryEntry item in ht)
                {
                    cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                }
                con.Open();
                res = cmd.ExecuteNonQuery();
                con.Close();
                AuditTrailService.TryLog(qry, ht, res);
                if (res > 0)
                    AutoBackupService.NotifyEntryMadeIfBusinessQuery(qry);
            }
            catch (Exception ex)
            {
                con.Close();
                MessageBox.Show(ex.Message);
            }
            return res;
        }
        public static void Enable_reset(Form p)
        {
            foreach (Control c in p.Controls)
            {
                if (c is Guna.UI2.WinForms.Guna2TextBox)
                {
                    Guna.UI2.WinForms.Guna2TextBox t = (Guna.UI2.WinForms.Guna2TextBox)c;
                    t.Text = "";
                }
                else if (c is Guna.UI2.WinForms.Guna2ComboBox)
                {
                    Guna.UI2.WinForms.Guna2ComboBox cb = (Guna.UI2.WinForms.Guna2ComboBox)c;
                    if (cb.Items.Count > 0)
                    {
                        cb.SelectedIndex = 0; // Set to first item if there are items
                    }
                }
                else if (c is Guna.UI2.WinForms.Guna2RadioButton)
                {
                    Guna.UI2.WinForms.Guna2RadioButton rb = (Guna.UI2.WinForms.Guna2RadioButton)c;
                    rb.Checked = false;
                }
                else if (c is Guna.UI2.WinForms.Guna2CheckBox)
                {
                    Guna.UI2.WinForms.Guna2CheckBox ck = (Guna.UI2.WinForms.Guna2CheckBox)c;
                    ck.Checked = false;
                }
                else if (c is Guna.UI2.WinForms.Guna2DateTimePicker)
                {
                    Guna.UI2.WinForms.Guna2DateTimePicker dp = (Guna.UI2.WinForms.Guna2DateTimePicker)c;
                    dp.Value = DateTime.Today;
                }
                else if (c is ListBox)
                {
                    ListBox list = (ListBox)c;
                }
                else if (c is NumericUpDown)
                {
                    NumericUpDown cb = (NumericUpDown)c;
                    cb.Value = 0;
                }
                else if (c is MaskedTextBox)
                {
                    MaskedTextBox cb = (MaskedTextBox)c;
                    cb.Text = "";
                }
            }
        }

        public static void Enable_reset_keep_date(Form p, params Guna.UI2.WinForms.Guna2DateTimePicker[] keepDates)
        {
            var saved = new System.Collections.Generic.Dictionary<Guna.UI2.WinForms.Guna2DateTimePicker, DateTime>();
            if (keepDates != null)
            {
                foreach (var dp in keepDates)
                {
                    if (dp != null)
                        saved[dp] = dp.Value;
                }
            }

            Enable_reset(p);

            foreach (var kv in saved)
                kv.Key.Value = kv.Value;
        }

        public static void SrNo(Guna.UI2.WinForms.Guna2DataGridView gv)
        {
            try
            {
                int count = 0;
                foreach (DataGridViewRow row in gv.Rows)
                {
                    count++;
                    row.Cells[0].Value = count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                con.Close();
            }
        }
        public static int SQL(string qry, Hashtable ht)
        {
            int res = 0;
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(qry, con);
                cmd.CommandType = CommandType.Text;
                foreach (DictionaryEntry item in ht)
                {
                    cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                }
                con.Open();
                res = cmd.ExecuteNonQuery();
                con.Close();
                AuditTrailService.TryLog(qry, ht, res);
            }
            catch (Exception ex)
            {
                con.Close();
                MessageBox.Show(ex.Message);
            }
            return res;
        }
        public static void LoadData(string qry, DataGridView gv, ListBox lb)
        {
            gv.CellFormatting += new DataGridViewCellFormattingEventHandler(gv_CellFormatting);
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(qry, con);
                cmd.CommandType = CommandType.Text;
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                for (int i = 0; i < lb.Items.Count; i++)
                {
                    string colName1 = ((DataGridViewColumn)lb.Items[i]).Name;
                    gv.Columns[colName1].DataPropertyName = dt.Columns[i].ToString();
                }
                gv.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                con.Close();
            }
        }
        private static void gv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Cast sender to Guna2DataGridView (not GunaDataGridView)
            Guna.UI2.WinForms.Guna2DataGridView gv = (Guna.UI2.WinForms.Guna2DataGridView)sender;

            int count = 0;

            foreach (DataGridViewRow row in gv.Rows)
            {
                count++;
                row.Cells[0].Value = count;
            }
        }

        public static void BlurBackground(Form Model)
        {
            Form Background = new Form();
            using (Model)
            {
                Background.StartPosition = FormStartPosition.CenterScreen;
                Background.FormBorderStyle = FormBorderStyle.None;
                Background.Opacity = 0.5d;
                Background.BackColor = Color.Black;
                //Background.Size = frmMain.instance.Size;
                //Background.Location = frmMain.instance.Location;
                Background.WindowState = FormWindowState.Maximized;
                Background.ShowInTaskbar = false;
                Background.Show();
                Model.Owner = Background;
                Model.ShowDialog(Background);
                Background.Dispose();
            }
        }
        public static void CBFill(string qry, ComboBox cb)
        {
            SQLiteCommand cmd = new SQLiteCommand(qry, con);
            cmd.CommandType = CommandType.Text;
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            cb.DisplayMember = "Name";
            cb.ValueMember = "id";
            cb.DataSource = dt;
            cb.SelectedIndex = -1;

        }
        public static bool Validation(Form F)
        {
            bool isValid = false;
            int count = 0;
            foreach (Control c in F.Controls)
            {
                //using tag of the control to check if we want to validate it or not 
                if (Convert.ToString(c.Tag) != "" && Convert.ToString(c.Tag) != null)
                {
                    if (c is Guna.UI2.WinForms.Guna2TextBox)
                    {
                        Guna.UI2.WinForms.Guna2TextBox t = (Guna.UI2.WinForms.Guna2TextBox)c;
                        if (t.Text.Trim() == "")
                        {
                            t.BorderColor = Color.Red;
                            t.FocusedState.BorderColor = Color.Red;
                            t.HoverState.BorderColor = Color.Red;
                            count++;
                        }
                        else
                        {
                            t.BorderColor = Color.FromArgb(213, 218, 223);
                            t.FocusedState.BorderColor = Color.FromArgb(95, 61, 204);
                            t.HoverState.BorderColor = Color.FromArgb(95, 61, 204);
                        }

                    }
                }
                if (count == 0)
                {
                    isValid = true;
                }
                else
                {
                    isValid = false;
                }
            }
            return isValid;

        }
    }
}
