using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using AlMadinaShahzad.ProjectConnection;

namespace AlMadinaShahzad.View
{
    /// <summary>
    /// Home ke sab se upar "Aaj ka Stock Board" — purana chart/groupbox layout aur Designer
    /// bilkul same rehta hai, sirf sab controls neeche shift ho jate hain.
    /// </summary>
    public partial class frmDashBoard
    {
        private const int StockBoardHeight = 118;
        private const int StockBoardGap = 10;

        private Panel _stockBoardPanel;
        private Label _lblStockBoardInfo;
        private bool _stockBoardReady;

        private Label _valInToday;
        private Label _valOutToday;
        private Label _valStockLeft;
        private Label _valCashCustomer;
        private Label _valCashDealer;

        private void SetupStockBoard()
        {
            if (_stockBoardReady)
                return;

            _stockBoardReady = true;

            int shift = StockBoardHeight + StockBoardGap;
            foreach (Control existing in Controls)
                existing.Top += shift;

            _stockBoardPanel = new Panel
            {
                Name = "panelStockBoard",
                Location = new Point(12, 8),
                Size = new Size(Math.Max(600, ClientSize.Width - 24), StockBoardHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(32, 36, 61),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_stockBoardPanel);
            _stockBoardPanel.BringToFront();

            var lblTitle = new Label
            {
                Text = "Aaj Ka Stock Board",
                Location = new Point(12, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 204, 0),
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            _stockBoardPanel.Controls.Add(lblTitle);

            _lblStockBoardInfo = new Label
            {
                Location = new Point(210, 12),
                AutoSize = true,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent
            };
            _stockBoardPanel.Controls.Add(_lblStockBoardInfo);

            var btnRefresh = new Button
            {
                Text = "Refresh",
                Size = new Size(90, 28),
                Location = new Point(_stockBoardPanel.ClientSize.Width - 102, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 140);
            btnRefresh.Click += (s, e) => LoadStockBoard();
            _stockBoardPanel.Controls.Add(btnRefresh);

            _valInToday = AddStockCard(0, "Aaj Andar Aaya (L)", Color.FromArgb(39, 174, 96));
            _valOutToday = AddStockCard(1, "Aaj Bahar Gaya (L)", Color.FromArgb(66, 133, 244));
            _valStockLeft = AddStockCard(2, "Stock Reh Gaya (L)", Color.FromArgb(255, 140, 0));
            _valCashCustomer = AddStockCard(3, "Aaj Sirf Paise - Customer", Color.FromArgb(142, 68, 173));
            _valCashDealer = AddStockCard(4, "Aaj Sirf Paise - Dealer", Color.FromArgb(220, 53, 69));

            AutoScroll = true;
            _stockBoardPanel.Resize += (s, e) => LayoutStockCards();
            LayoutStockCards();
            LoadStockBoard();
        }

        private readonly List<Panel> _stockCards = new List<Panel>();

        private Label AddStockCard(int index, string caption, Color accent)
        {
            var card = new Panel
            {
                Size = new Size(200, 66),
                Location = new Point(12 + index * 210, 42),
                BackColor = Color.FromArgb(24, 28, 48),
                BorderStyle = BorderStyle.FixedSingle
            };

            var stripe = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = accent
            };
            card.Controls.Add(stripe);

            var lblCaption = new Label
            {
                Text = caption,
                Location = new Point(12, 6),
                AutoSize = true,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblCaption);

            var lblValue = new Label
            {
                Text = "0",
                Location = new Point(12, 28),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblValue);

            _stockBoardPanel.Controls.Add(card);
            _stockCards.Add(card);
            return lblValue;
        }

        private void LayoutStockCards()
        {
            if (_stockCards.Count == 0)
                return;

            const int left = 12;
            const int gap = 10;
            int available = _stockBoardPanel.ClientSize.Width - left * 2 - gap * (_stockCards.Count - 1);
            int cardWidth = Math.Max(150, available / _stockCards.Count);

            for (int i = 0; i < _stockCards.Count; i++)
            {
                _stockCards[i].Width = cardWidth;
                _stockCards[i].Left = left + i * (cardWidth + gap);
            }
        }

        private void LoadStockBoard()
        {
            try
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();

                    decimal inToday = ScalarDecimal(con,
                        "SELECT IFNULL(SUM(AddDisel), 0) FROM AddStock WHERE date(Date) = date(@d)", today);

                    decimal outToday = ScalarDecimal(con,
                        "SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd WHERE date(Date) = date(@d)", today);

                    decimal inTotal = ScalarDecimal(con,
                        "SELECT IFNULL(SUM(AddDisel), 0) FROM AddStock", null);

                    decimal outTotal = ScalarDecimal(con,
                        "SELECT IFNULL(SUM(Litter), 0) FROM PetrolAdd", null);

                    decimal cashCustomer = ScalarDecimal(con,
                        @"SELECT IFNULL(SUM(IFNULL(Amount, 0) + IFNULL(Advance, 0)), 0)
                          FROM PetrolAdd
                          WHERE date(Date) = date(@d)
                            AND (IFNULL(Litter, 0) = 0 OR IFNULL(Rate, 0) = 0)", today);

                    decimal cashDealer = ScalarDecimal(con,
                        "SELECT IFNULL(SUM(AmounGiven), 0) FROM DieselLedgerDebit WHERE date(Date) = date(@d)", today);

                    _valInToday.Text = inToday.ToString("N2");
                    _valOutToday.Text = outToday.ToString("N2");
                    _valStockLeft.Text = (inTotal - outTotal).ToString("N2");
                    _valCashCustomer.Text = "Rs " + cashCustomer.ToString("N0");
                    _valCashDealer.Text = "Rs " + cashDealer.ToString("N0");

                    _lblStockBoardInfo.Text =
                        $"{DateTime.Today:dd-MMM-yyyy}  |  Stock = DealerAmount ke liter - Daily Sales ke liter  |  Sirf paise stock se alag";
                }
            }
            catch (Exception ex)
            {
                _lblStockBoardInfo.Text = "Stock board load nahi hua: " + ex.Message;
            }
        }

        private static decimal ScalarDecimal(SQLiteConnection con, string query, string dateParam)
        {
            using (var cmd = new SQLiteCommand(query, con))
            {
                if (dateParam != null)
                    cmd.Parameters.AddWithValue("@d", dateParam);

                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;

                decimal.TryParse(result.ToString(), out decimal value);
                return value;
            }
        }
    }
}
