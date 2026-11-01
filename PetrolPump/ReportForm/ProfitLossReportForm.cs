using Guna.UI2.WinForms;

using AlMadinaShahzad.Services;

using System;

using System.Drawing;

using System.Windows.Forms;



namespace AlMadinaShahzad.ReportForm

{

    public class ProfitLossReportForm : Sample

    {

        private readonly Guna2DateTimePicker _dtFrom = new Guna2DateTimePicker();

        private readonly Guna2DateTimePicker _dtTo = new Guna2DateTimePicker();

        private readonly Guna2Button _btnLoad = new Guna2Button();

        private readonly Guna2DataGridView _grid = new Guna2DataGridView();



        public ProfitLossReportForm()

        {

            Text = "Profit & Loss Statement";

            Size = new Size(900, 600);

            StartPosition = FormStartPosition.CenterScreen;



            var lblFrom = new Label { Text = "From:", Location = new Point(20, 20), AutoSize = true, ForeColor = Color.White };

            _dtFrom.Location = new Point(70, 15);

            _dtFrom.Size = new Size(180, 30);

            _dtFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);



            var lblTo = new Label { Text = "To:", Location = new Point(270, 20), AutoSize = true, ForeColor = Color.White };

            _dtTo.Location = new Point(305, 15);

            _dtTo.Size = new Size(180, 30);

            _dtTo.Value = DateTime.Today;



            _btnLoad.Text = "Load P&L";

            _btnLoad.Location = new Point(510, 15);

            _btnLoad.Size = new Size(120, 30);

            _btnLoad.FillColor = Color.FromArgb(39, 174, 96);

            _btnLoad.ForeColor = Color.White;

            _btnLoad.Click += (s, e) => LoadReport();



            _grid.Location = new Point(20, 60);

            _grid.Size = new Size(840, 480);

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

            ProfessionalFormHelper.AddCloseButton(this, 780, 15);



            Load += (s, e) => LoadReport();

        }



        private void LoadReport()

        {

            if (_dtFrom.Value.Date > _dtTo.Value.Date)

            {

                MessageBox.Show("From date To date se zyada nahi ho sakti.", "Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;

            }



            var data = FinancialReportService.GetProfitLoss(_dtFrom.Value.Date, _dtTo.Value.Date);

            _grid.DataSource = FinancialReportService.BuildProfitLossTable(data);

        }

    }

}


