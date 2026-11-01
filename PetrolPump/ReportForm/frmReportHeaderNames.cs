using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    /// <summary>
    /// Report header names: Insert / Update / Delete. Al Madina permanent.
    /// </summary>
    public class frmReportHeaderNames : Form
    {
        private ListBox _list;
        private Guna2TextBox _txtName;
        private Guna2Button _btnAdd;
        private Guna2Button _btnUpdate;
        private Guna2Button _btnDelete;
        private Guna2Button _btnUse;
        private Guna2Button _btnClose;

        public frmReportHeaderNames()
        {
            Text = "Report Header Names";
            ClientSize = new Size(460, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(32, 36, 66);

            var lblInfo = new Label
            {
                Text = "Al Madina Shezad Chhutta Services permanent hai.\nYahan naya header name Add / Update / Delete karein.",
                ForeColor = Color.White,
                Location = new Point(14, 10),
                Size = new Size(430, 40),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };

            _list = new ListBox
            {
                Location = new Point(14, 55),
                Size = new Size(430, 160),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _list.SelectedIndexChanged += (s, e) =>
            {
                if (_list.SelectedItem != null)
                    _txtName.Text = _list.SelectedItem.ToString();
            };

            _txtName = new Guna2TextBox
            {
                Location = new Point(14, 225),
                Size = new Size(430, 36),
                BorderRadius = 10,
                BorderColor = Color.FromArgb(112, 51, 255),
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                PlaceholderText = "Header name likhein..."
            };

            _btnAdd = MakeBtn("Add", 14, 275, Color.FromArgb(44, 215, 207));
            _btnUpdate = MakeBtn("Update", 110, 275, Color.FromArgb(241, 196, 15), Color.Black);
            _btnDelete = MakeBtn("Delete", 206, 275, Color.FromArgb(192, 57, 43));
            _btnUse = MakeBtn("Use", 302, 275, Color.FromArgb(52, 152, 219));
            _btnClose = MakeBtn("Close", 370, 275, Color.FromArgb(100, 88, 255));
            _btnClose.Width = 74;

            _btnAdd.Click += BtnAdd_Click;
            _btnUpdate.Click += BtnUpdate_Click;
            _btnDelete.Click += BtnDelete_Click;
            _btnUse.Click += BtnUse_Click;
            _btnClose.Click += (s, e) => Close();

            Controls.Add(lblInfo);
            Controls.Add(_list);
            Controls.Add(_txtName);
            Controls.Add(_btnAdd);
            Controls.Add(_btnUpdate);
            Controls.Add(_btnDelete);
            Controls.Add(_btnUse);
            Controls.Add(_btnClose);

            Load += (s, e) => ReloadList();
        }

        private static Guna2Button MakeBtn(string text, int x, int y, Color fill, Color? fore = null)
        {
            return new Guna2Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(88, 34),
                BorderRadius = 8,
                FillColor = fill,
                ForeColor = fore ?? Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
        }

        private void ReloadList()
        {
            string sel = _list.SelectedItem as string;
            _list.Items.Clear();
            foreach (string n in ReportHeaderNamesStore.LoadAll())
                _list.Items.Add(n);

            string current = ReportHeaderNamesStore.GetSelected();
            for (int i = 0; i < _list.Items.Count; i++)
            {
                string item = _list.Items[i].ToString();
                if ((!string.IsNullOrEmpty(sel) && string.Equals(item, sel, StringComparison.OrdinalIgnoreCase))
                    || string.Equals(item, current, StringComparison.OrdinalIgnoreCase))
                {
                    _list.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ReportHeaderNamesStore.Add(_txtName.Text, out string err))
            {
                MessageBox.Show(err, "Add", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ReloadList();
            MessageBox.Show("Name add ho gaya.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            string oldName = _list.SelectedItem as string;
            if (string.IsNullOrEmpty(oldName))
            {
                MessageBox.Show("Pehle list se name select karein.", "Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ReportHeaderNamesStore.Update(oldName, _txtName.Text, out string err))
            {
                MessageBox.Show(err, "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ReloadList();
            MessageBox.Show("Name update ho gaya.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            string name = _list.SelectedItem as string;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Pehle list se name select karein.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete '" + name + "'?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (!ReportHeaderNamesStore.Delete(name, out string err))
            {
                MessageBox.Show(err, "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _txtName.Text = "";
            ReloadList();
        }

        private void BtnUse_Click(object sender, EventArgs e)
        {
            string name = (_list.SelectedItem as string) ?? _txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name select/likhein.", "Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ReportHeaderNamesStore.SetSelected(name);
            MessageBox.Show("Ab reports ka header:\n" + name, "Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
