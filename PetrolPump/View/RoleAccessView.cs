using Guna.UI2.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.Services;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public class RoleAccessView : Sample
    {
        private readonly Guna2ComboBox _cbRole = new Guna2ComboBox();
        private readonly Guna2ComboBox _cbUsers = new Guna2ComboBox();
        private readonly Guna2Button _btnSaveUserRole = new Guna2Button();
        private readonly Guna2DataGridView _grid = new Guna2DataGridView();

        public RoleAccessView()
        {
            Text = "Role Access Management";
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;

            if (!RoleAccessService.IsAdmin)
            {
                MessageBox.Show("Sirf Admin role access manage kar sakta hai.", "Access Denied",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            var lblRole = new Label { Text = "Filter Role:", Location = new Point(20, 20), AutoSize = true, ForeColor = Color.White };
            _cbRole.Location = new Point(110, 15);
            _cbRole.Size = new Size(150, 30);
            _cbRole.Items.AddRange(new object[] { "Admin", "Manager", "Cashier" });
            _cbRole.SelectedIndex = 1;
            _cbRole.SelectedIndexChanged += (s, e) => LoadPermissions();

            var lblUser = new Label { Text = "Assign User Role:", Location = new Point(290, 20), AutoSize = true, ForeColor = Color.White };
            _cbUsers.Location = new Point(430, 15);
            _cbUsers.Size = new Size(220, 30);

            _btnSaveUserRole.Text = "Save User Role";
            _btnSaveUserRole.Location = new Point(670, 15);
            _btnSaveUserRole.Size = new Size(140, 30);
            _btnSaveUserRole.FillColor = Color.FromArgb(230, 126, 34);
            _btnSaveUserRole.ForeColor = Color.White;
            _btnSaveUserRole.Click += BtnSaveUserRole_Click;

            _grid.Location = new Point(20, 60);
            _grid.Size = new Size(940, 540);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.AllowUserToAddRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.CellContentClick += Grid_CellContentClick;

            Controls.Add(lblRole);
            Controls.Add(_cbRole);
            Controls.Add(lblUser);
            Controls.Add(_cbUsers);
            Controls.Add(_btnSaveUserRole);
            Controls.Add(_grid);
            ProfessionalFormHelper.AddCloseButton(this, 880, 15);

            Load += (s, e) =>
            {
                LoadUsers();
                LoadPermissions();
            };
        }

        private void LoadUsers()
        {
            DataTable dt = MainClass.GetData("SELECT UserID, uName, IFNULL(uRole,'Admin') AS uRole FROM tblUser ORDER BY uName");
            _cbUsers.DisplayMember = "uName";
            _cbUsers.ValueMember = "UserID";
            _cbUsers.DataSource = dt;
        }

        private void LoadPermissions()
        {
            string role = _cbRole.SelectedItem?.ToString() ?? "Manager";
            string qry = "SELECT Id, RoleName, FormKey, CanAccess FROM RolePermissions WHERE RoleName='" + role.Replace("'", "''") + "' ORDER BY FormKey";
            _grid.DataSource = MainClass.GetData(qry);

            if (_grid.Columns.Contains("CanAccess"))
            {
                var col = new DataGridViewButtonColumn
                {
                    Name = "ToggleAccess",
                    HeaderText = "Toggle",
                    Text = "Change",
                    UseColumnTextForButtonValue = true
                };
                if (!_grid.Columns.Contains("ToggleAccess"))
                    _grid.Columns.Add(col);
            }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "ToggleAccess") return;

            DataGridViewRow row = _grid.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            int current = Convert.ToInt32(row.Cells["CanAccess"].Value);
            RoleAccessService.SavePermission(id, current == 1 ? 0 : 1);
            LoadPermissions();
        }

        private void BtnSaveUserRole_Click(object sender, EventArgs e)
        {
            if (_cbUsers.SelectedValue == null) return;
            string role = _cbRole.SelectedItem?.ToString() ?? "Cashier";
            int userId = Convert.ToInt32(_cbUsers.SelectedValue);
            RoleAccessService.SetUserRole(userId, role);
            MessageBox.Show("User role update ho gaya.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }
    }
}
