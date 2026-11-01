using Guna.UI2.WinForms;
using AlMadinaShahzad;
using AlMadinaShahzad.Services;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad.View
{
    public class AuditTrailView : Sample
    {
        private readonly Guna2DateTimePicker _dtFrom = new Guna2DateTimePicker();
        private readonly Guna2DateTimePicker _dtTo = new Guna2DateTimePicker();
        private readonly Guna2Button _btnLoad = new Guna2Button();
        private readonly Guna2DataGridView _grid = new Guna2DataGridView();

        public AuditTrailView()
        {
            Text = "Audit Trail";
            Size = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;

            var lblFrom = new Label { Text = "From:", Location = new Point(20, 20), AutoSize = true, ForeColor = Color.White };
            _dtFrom.Location = new Point(70, 15);
            _dtFrom.Size = new Size(160, 30);
            _dtFrom.Value = DateTime.Today.AddDays(-30);

            var lblTo = new Label { Text = "To:", Location = new Point(250, 20), AutoSize = true, ForeColor = Color.White };
            _dtTo.Location = new Point(285, 15);
            _dtTo.Size = new Size(160, 30);
            _dtTo.Value = DateTime.Today;

            _btnLoad.Text = "Load Audit Log";
            _btnLoad.Location = new Point(470, 15);
            _btnLoad.Size = new Size(140, 30);
            _btnLoad.FillColor = Color.FromArgb(142, 68, 173);
            _btnLoad.ForeColor = Color.White;
            _btnLoad.Click += (s, e) => LoadData();

            _grid.Location = new Point(20, 60);
            _grid.Size = new Size(1040, 540);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BackgroundColor = Color.White;

            Controls.Add(lblFrom);
            Controls.Add(_dtFrom);
            Controls.Add(lblTo);
            Controls.Add(_dtTo);
            Controls.Add(_btnLoad);
            Controls.Add(_grid);
            ProfessionalFormHelper.AddCloseButton(this, 960, 15);

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            string qry = @"
                SELECT Id, ActionTime, UserName, UserRole, ActionType, TableName, RecordId, QuerySummary, Details
                FROM AuditLog
                WHERE date(ActionTime) >= date(@from) AND date(ActionTime) <= date(@to)
                ORDER BY Id DESC";

            var ht = new System.Collections.Hashtable
            {
                { "@from", _dtFrom.Value.ToString("yyyy-MM-dd") },
                { "@to", _dtTo.Value.ToString("yyyy-MM-dd") }
            };

            DataTable dt = MainClass.ExecuteSelectQuery(qry, ht);
            _grid.DataSource = dt ?? new DataTable();
        }
    }
}
