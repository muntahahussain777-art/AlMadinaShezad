using Guna.UI2.WinForms;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    internal enum TwoTableReportTempSide
    {
        Receivable,
        Payable
    }

    internal sealed class TwoTableReportTempEntry
    {
        public string Name;
        public decimal Amount;
        public TwoTableReportTempSide Side;
    }

    /// <summary>
    /// Sirf report session ke liye temporary name/amount — form band hone par clear.
    /// </summary>
    internal sealed class TwoTableReportTempEntryController
    {
        private readonly List<TwoTableReportTempEntry> _entries = new List<TwoTableReportTempEntry>();
        private readonly Action _refreshReport;

        private Guna2TextBox _txtName;
        private Guna2TextBox _txtAmount;
        private ComboBox _cmbSide;
        private Guna2Button _btnAdd;
        private Label _lblInfo;

        internal TwoTableReportTempEntryController(Form form, ReportViewer reportViewer, Action refreshReport)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            _refreshReport = refreshReport ?? throw new ArgumentNullException(nameof(refreshReport));

            BuildUi(form, reportViewer);
            form.FormClosed += (s, e) => _entries.Clear();
        }

        internal IReadOnlyList<TwoTableReportTempEntry> Entries => _entries;

        internal int Count => _entries.Count;

        private void BuildUi(Form form, ReportViewer reportViewer)
        {
            var panel = new Panel
            {
                Location = new Point(12, 33),
                Size = new Size(830, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(45, 52, 82)
            };

            var lblName = new Label
            {
                Text = "Temp Name",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(4, 8)
            };

            _txtName = new Guna2TextBox
            {
                Location = new Point(88, 4),
                Size = new Size(150, 28),
                BorderRadius = 12,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderColor = Color.FromArgb(212, 175, 55),
                PlaceholderText = "Name"
            };

            var lblAmount = new Label
            {
                Text = "Amount",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(248, 8)
            };

            _txtAmount = new Guna2TextBox
            {
                Location = new Point(312, 4),
                Size = new Size(90, 28),
                BorderRadius = 12,
                FillColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderColor = Color.FromArgb(212, 175, 55),
                PlaceholderText = "0"
            };

            var lblSide = new Label
            {
                Text = "Side",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(412, 8)
            };

            _cmbSide = new ComboBox
            {
                Location = new Point(452, 5),
                Size = new Size(110, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbSide.Items.AddRange(new object[] { "Receivable", "Payable" });
            _cmbSide.SelectedIndex = 0;

            _btnAdd = new Guna2Button
            {
                Text = "Add Temp",
                Location = new Point(572, 3),
                Size = new Size(95, 28),
                BorderRadius = 14,
                FillColor = Color.FromArgb(26, 39, 68),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            _btnAdd.Click += BtnAdd_Click;

            _lblInfo = new Label
            {
                Text = "Temporary (report only — close form = remove)",
                AutoSize = true,
                ForeColor = Color.FromArgb(212, 175, 55),
                Location = new Point(676, 8)
            };

            panel.Controls.Add(lblName);
            panel.Controls.Add(_txtName);
            panel.Controls.Add(lblAmount);
            panel.Controls.Add(_txtAmount);
            panel.Controls.Add(lblSide);
            panel.Controls.Add(_cmbSide);
            panel.Controls.Add(_btnAdd);
            panel.Controls.Add(_lblInfo);

            form.Controls.Add(panel);
            panel.BringToFront();

            if (reportViewer != null)
            {
                reportViewer.Top += 36;
                if (reportViewer.Height > 36)
                    reportViewer.Height -= 36;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = (_txtName.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Temporary name likhein.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtName.Focus();
                return;
            }

            if (!TryParseAmount(_txtAmount.Text, out decimal amount) || amount == 0m)
            {
                MessageBox.Show("Sahi amount likhein (0 se zyada).", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtAmount.Focus();
                return;
            }

            var side = _cmbSide.SelectedIndex == 1
                ? TwoTableReportTempSide.Payable
                : TwoTableReportTempSide.Receivable;

            _entries.Add(new TwoTableReportTempEntry
            {
                Name = name,
                Amount = amount,
                Side = side
            });

            _txtName.Clear();
            _txtAmount.Clear();
            _lblInfo.Text = "Temp entries: " + _entries.Count + "  (report only)";

            _refreshReport();
        }

        private static bool TryParseAmount(string text, out decimal amount)
        {
            amount = 0m;
            string s = (text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s))
                return false;

            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
                || decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }

    internal static partial class TwoTableReceivablePayableReportHelper
    {
        internal const string TemporaryEntryNote = "Temporary entry (report only)";

        internal static DataTable MergeTempEntriesIntoCombined(
            DataTable raw,
            IReadOnlyList<TwoTableReportTempEntry> tempEntries)
        {
            DataTable merged = raw == null ? CreateRawCombinedSchema() : raw.Copy();
            EnsureReportEntryTagColumn(merged);

            if (tempEntries == null || tempEntries.Count == 0)
                return merged;

            foreach (TwoTableReportTempEntry entry in tempEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Amount == 0m)
                    continue;

                if (entry.Side == TwoTableReportTempSide.Receivable)
                {
                    merged.Rows.Add(
                        entry.Name.Trim(),
                        entry.Amount,
                        null,
                        DBNull.Value,
                        "Temporary");
                }
                else
                {
                    merged.Rows.Add(
                        null,
                        DBNull.Value,
                        entry.Name.Trim(),
                        Math.Abs(entry.Amount),
                        "Temporary");
                }
            }

            return merged;
        }

        internal static void MergeTempEntriesIntoSeparate(
            DataTable dtCustomer,
            DataTable dtDealer,
            IReadOnlyList<TwoTableReportTempEntry> tempEntries)
        {
            if (tempEntries == null || tempEntries.Count == 0)
                return;

            if (dtCustomer != null)
                EnsureReportEntryTagColumn(dtCustomer);
            if (dtDealer != null)
                EnsureReportEntryTagColumn(dtDealer);

            foreach (TwoTableReportTempEntry entry in tempEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Amount == 0m)
                    continue;

                if (entry.Side == TwoTableReportTempSide.Receivable)
                {
                    if (dtCustomer == null) continue;
                    var row = dtCustomer.NewRow();
                    row["CustomerName"] = entry.Name.Trim();
                    row["CustomerRate"] = entry.Amount;
                    row["ReportEntryTag"] = "Temporary";
                    dtCustomer.Rows.Add(row);
                }
                else
                {
                    if (dtDealer == null) continue;
                    var row = dtDealer.NewRow();
                    row["DealerName"] = entry.Name.Trim();
                    row["DealerRate"] = Math.Abs(entry.Amount);
                    row["ReportEntryTag"] = "Temporary";
                    dtDealer.Rows.Add(row);
                }
            }
        }

        private static DataTable CreateRawCombinedSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("CustomerRate", typeof(decimal));
            dt.Columns.Add("DealerName", typeof(string));
            dt.Columns.Add("DealerRate", typeof(decimal));
            dt.Columns.Add("ReportEntryTag", typeof(string));
            return dt;
        }

        private static void EnsureReportEntryTagColumn(DataTable dt)
        {
            if (dt != null && !dt.Columns.Contains("ReportEntryTag"))
                dt.Columns.Add("ReportEntryTag", typeof(string));
        }

        private static bool IsTemporaryRow(DataRow row)
        {
            if (row == null || !row.Table.Columns.Contains("ReportEntryTag"))
                return false;
            return string.Equals(Convert.ToString(row["ReportEntryTag"]), "Temporary", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatReceivableName(string name, bool isTemporary)
        {
            if (!isTemporary)
                return name;
            return name + " [Temporary]";
        }
    }
}
