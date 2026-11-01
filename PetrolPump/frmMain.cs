using ApplicationPermissions;
using DigiKhataApp;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using AlMadinaShahzad;
using AlMadinaShahzad.Model;
using AlMadinaShahzad.Services;
using AlMadinaShahzad.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public partial class frmMain : Sample
    {

        private Guna2ControlBox _btnMaximize;
        private Rectangle _restoreBounds = Rectangle.Empty;

        private readonly Dictionary<Guna2Button, MenuButtonStyle> _menuButtonStyles =
            new Dictionary<Guna2Button, MenuButtonStyle>();
        private readonly Dictionary<string, Guna2Button> _formMenuMap =
            new Dictionary<string, Guna2Button>(StringComparer.OrdinalIgnoreCase);
        private Guna2Button _activeMenuButton;

        private Guna2Button[] _orderedSideMenuButtons;

        private static readonly Color MenuActiveFill = Color.FromArgb(220, 53, 69);
        private static readonly Color MenuActiveHoverFill = Color.FromArgb(255, 82, 97);
        private static readonly Color MenuActiveBorder = Color.FromArgb(255, 204, 0);

        private sealed class MenuButtonStyle
        {
            public string Text;
            public Color FillColor;
            public Color HoverFillColor;
            public Color BorderColor;
            public int BorderThickness;
            public Color ForeColor;
            public Font Font;
        }

        public frmMain()
        {
            InitializeComponent();
            TryLoadMainLogo();
            SetupResponsiveSideMenu();
            _obj = this;
            MinimumSize = new Size(1024, 600);
            ShowInTaskbar = true;
            SetupMultiMonitorWindow();
            KeyPreview = true;
            KeyDown += frmMain_KeyDown;
        }

        private void TryLoadMainLogo()
        {
            try
            {
                string[] names =
                {
                    "AlMadinaShahzadLogo.jpg",
                    "AlMadinaShahzad Diesel & Cement Logo.jpg",
                    "AlNoorLogo.jpg",
                    "ZaibLogo.jpg",
                    "AlNoorLogo.gif",
                    "ZaibLogo.gif"
                };
                var roots = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    Application.StartupPath,
                    Directory.GetCurrentDirectory()
                };
                foreach (string root in roots)
                {
                    if (string.IsNullOrEmpty(root)) continue;
                    var dir = new DirectoryInfo(root);
                    for (int up = 0; up < 4 && dir != null; up++, dir = dir.Parent)
                    {
                        foreach (string name in names)
                        {
                            string candidate = Path.Combine(dir.FullName, "Assets", name);
                            if (!File.Exists(candidate)) continue;
                            pictureBox1.Image = Image.FromFile(candidate);
                            return;
                        }
                    }
                }

                var resources = new ComponentResourceManager(typeof(frmMain));
                if (resources.GetObject("pictureBox1.Image") is Image img)
                    pictureBox1.Image = img;
            }
            catch
            {
                // Logo optional if resources missing in stale Release build
            }
        }

        /// <summary>Side menu — sahi sequence, original size, PanelSideMenu scroll.</summary>
        private void SetupResponsiveSideMenu()
        {
            PanelSideMenu.AutoScroll = true;
            panelLogo.Dock = DockStyle.Top;

            _orderedSideMenuButtons = new[]
            {
                guna2Button1,
                guna2Button7,
                guna2Button8,
                guna2Button2,
                guna2Button12,
                guna2Button3,
                guna2Button5,
                guna2Button13,
                guna2Button9,
                guna2Button11,
                guna2Button14,
                btnstock,
                dgvFaida,
                btnClosing2,
                guna2Button15,
                guna2Button16,
                guna2Button4,
                guna2Button10
            };

            guna2Button6.Visible = false;

            foreach (Guna2Button btn in _orderedSideMenuButtons)
            {
                if (btn.Parent != PanelSideMenu)
                {
                    if (btn.Parent != null)
                        btn.Parent.Controls.Remove(btn);
                    PanelSideMenu.Controls.Add(btn);
                }
            }

            if (btnLogout.Parent != PanelSideMenu)
            {
                if (btnLogout.Parent != null)
                    btnLogout.Parent.Controls.Remove(btnLogout);
                PanelSideMenu.Controls.Add(btnLogout);
            }

            PanelSideMenu.Resize += (s, e) => LayoutSideMenuButtons();
            LayoutSideMenuButtons();
        }

        private void LayoutSideMenuButtons()
        {
            if (_orderedSideMenuButtons == null)
                return;

            const int left = 12;
            const int gap = 6;
            const int btnH = 35;
            int width = Math.Max(200, PanelSideMenu.ClientSize.Width - left * 2 - 4);
            int y = panelLogo.Bottom + gap;

            foreach (Guna2Button btn in _orderedSideMenuButtons)
            {
                btn.Size = new Size(width, btnH);
                btn.Location = new Point(left, y);
                btn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                y += btnH + gap;
            }

            btnLogout.Size = new Size(width, btnH);
            btnLogout.Location = new Point(left, y + gap);
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panelLogo.BringToFront();
        }

        private void SetupMultiMonitorWindow()
        {
            panel1.MouseDown += TitleBar_MouseDown;
            panel1.DoubleClick += TitleBar_DoubleClick;
            label1.MouseDown += TitleBar_MouseDown;
            label1.DoubleClick += TitleBar_DoubleClick;
            PanelSideMenu.MouseDown += (s, e) =>
            {
                if (e.Y <= 140)
                    TitleBar_MouseDown(s, e);
            };

            btnGeminiAi.Anchor = AnchorStyles.Top;
            btnPetroleumCalc.Anchor = AnchorStyles.Top;

            _btnMaximize = new Guna2ControlBox
            {
                Name = "btnMaximizeMain",
                ControlBoxType = ControlBoxType.MaximizeBox,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Size = btnExit.Size,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panel1.Controls.Add(_btnMaximize);
            _btnMaximize.BringToFront();
            btnExit.BringToFront();

            Resize += (s, e) => AdjustMainLayout();
            AdjustMainLayout();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.Clicks > 1) return;
            AppWindowHelper.BeginDrag(this, (Control)sender, e);
        }

        private void TitleBar_DoubleClick(object sender, EventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void ToggleMaximizeRestore()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                if (!_restoreBounds.IsEmpty && _restoreBounds.Width >= MinimumSize.Width && _restoreBounds.Height >= MinimumSize.Height)
                    Bounds = _restoreBounds;
                else
                {
                    Size = new Size(1200, 750);
                    CenterToScreen();
                }
            }
            else
            {
                _restoreBounds = Bounds;
                WindowState = FormWindowState.Maximized;
            }
            AdjustMainLayout();
        }

        private void AdjustMainLayout()
        {
            panel1.Left = PanelSideMenu.Width;
            panel1.Width = Math.Max(200, ClientSize.Width - PanelSideMenu.Width);

            const int gap = 8;
            int boxW = btnExit.Width;
            int top = btnExit.Top;
            int right = panel1.Width - 10;

            btnExit.Location = new Point(right - boxW, top);
            if (_btnMaximize != null)
                _btnMaximize.Location = new Point(btnExit.Left - boxW - gap, top);
            guna2ControlBox2.Location = new Point(
                (_btnMaximize != null ? _btnMaximize.Left : btnExit.Left) - boxW - gap, top);

            btnGeminiAi.Location = new Point(guna2ControlBox2.Left - btnGeminiAi.Width - 12, btnGeminiAi.Top);
            btnPetroleumCalc.Location = new Point(btnGeminiAi.Left - btnPetroleumCalc.Width - gap, btnPetroleumCalc.Top);

            int maxLabelW = btnPetroleumCalc.Left - label1.Left - 10;
            if (maxLabelW > 80)
                label1.MaximumSize = new Size(maxLabelW, 0);
        }

        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x84;
            const int htLeft = 10, htRight = 11, htTop = 12, htTopLeft = 13, htTopRight = 14;
            const int htBottom = 15, htBottomLeft = 16, htBottomRight = 17;

            if (m.Msg == wmNcHitTest && WindowState == FormWindowState.Normal)
            {
                int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                var client = PointToClient(new Point(x, y));
                const int grip = 8;

                if (client.X <= grip && client.Y <= grip) { m.Result = (IntPtr)htTopLeft; return; }
                if (client.X >= ClientSize.Width - grip && client.Y <= grip) { m.Result = (IntPtr)htTopRight; return; }
                if (client.X <= grip && client.Y >= ClientSize.Height - grip) { m.Result = (IntPtr)htBottomLeft; return; }
                if (client.X >= ClientSize.Width - grip && client.Y >= ClientSize.Height - grip) { m.Result = (IntPtr)htBottomRight; return; }
                if (client.X <= grip) { m.Result = (IntPtr)htLeft; return; }
                if (client.X >= ClientSize.Width - grip) { m.Result = (IntPtr)htRight; return; }
                if (client.Y <= grip) { m.Result = (IntPtr)htTop; return; }
                if (client.Y >= ClientSize.Height - grip) { m.Result = (IntPtr)htBottom; return; }
            }

            base.WndProc(ref m);
        }

        static frmMain _obj;
        public static frmMain instance
        {
            get
            {
                if (_obj == null || _obj.IsDisposed)
                {
                    _obj = new frmMain();  // Ensure instance is created
                }
                return _obj;
            }
        }

        public void AddControls(Form F)
        {
            if (!RoleAccessService.CanAccess(F.GetType().Name))
            {
                MessageBox.Show("Aap ke role ki ijazat nahi hai is form par.", "Access Denied",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Controlspanel.Controls.Clear();  // Clear existing controls
            F.TopLevel = false;  // Set form as a non-top-level
            Controlspanel.Controls.Add(F);  // Add the form to the panel
            F.Dock = DockStyle.Fill;  // Fill the panel
            F.Show();  // Show the form
            F.BringToFront();

            // Shortcut/menu se aane par turant focus — Ctrl+N bina click ke kaam kare
            BeginInvoke(new Action(() =>
            {
                FocusEmbeddedForm(F);
                HighlightMenuForForm(F);
            }));
        }

        private void InitializeSideMenuHighlight()
        {
            _formMenuMap.Clear();
            _menuButtonStyles.Clear();

            MapMenuButton<frmDashBoard>(guna2Button1);
            MapMenuButton<frmCreditAdjust>(guna2Button2);
            MapMenuButton<frmDieselLedger>(guna2Button3);
            MapMenuButton<frmCustomerToCustomerView>(guna2Button12);
            MapMenuButton<frmCustomerView>(guna2Button7);
            MapMenuButton<frmDiselView>(guna2Button8);
            MapMenuButton<frmDealerNameView>(guna2Button5);
            MapMenuButton<frmStockView>(guna2Button9);
            MapMenuButton<frmDealertoDealerView>(guna2Button11);
            MapMenuButton<frmDieselLedgerView>(guna2Button13);
            MapMenuButton<FrmDirectDealerPaymentAmountView>(guna2Button14);
            MapMenuButton<frmStockDieselView>(btnstock);
            MapMenuButton<frmClosingformEntry>(dgvFaida);
            MapMenuButton<frmClosing2>(btnClosing2);
            MapMenuButton<frmBankAccountView>(guna2Button15);
            MapMenuButton<frmExpense>(guna2Button16);
            MapMenuButton<ReportAndBackup>(guna2Button4);
            MapMenuButton<changepassword>(guna2Button10);

            if (_orderedSideMenuButtons != null)
            {
                foreach (Guna2Button btn in _orderedSideMenuButtons.Concat(new[] { btnLogout }))
                {
                    if (!_menuButtonStyles.ContainsKey(btn))
                        _menuButtonStyles[btn] = CaptureMenuButtonStyle(btn);
                }
            }
        }

        private void MapMenuButton<TForm>(Guna2Button button) where TForm : Form
        {
            _formMenuMap[typeof(TForm).Name] = button;
            if (!_menuButtonStyles.ContainsKey(button))
                _menuButtonStyles[button] = CaptureMenuButtonStyle(button);
        }

        private static MenuButtonStyle CaptureMenuButtonStyle(Guna2Button btn)
        {
            return new MenuButtonStyle
            {
                Text = CleanMenuText(btn.Text),
                FillColor = btn.FillColor,
                HoverFillColor = btn.HoverState.FillColor,
                BorderColor = btn.BorderColor,
                BorderThickness = btn.BorderThickness,
                ForeColor = btn.ForeColor,
                Font = btn.Font
            };
        }

        private void HighlightMenuForForm(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            ResetAllMenuButtons();

            if (!_formMenuMap.TryGetValue(form.GetType().Name, out Guna2Button activeBtn) || activeBtn == null)
                return;

            _activeMenuButton = activeBtn;
            ApplyActiveMenuStyle(activeBtn);
            PanelSideMenu.ScrollControlIntoView(activeBtn);
        }

        private void ResetAllMenuButtons()
        {
            foreach (var pair in _menuButtonStyles)
                ApplyNormalMenuStyle(pair.Key, pair.Value);

            _activeMenuButton = null;
        }

        private void ApplyActiveMenuStyle(Guna2Button btn)
        {
            if (btn == null || !_menuButtonStyles.TryGetValue(btn, out MenuButtonStyle style))
                return;

            btn.FillColor = MenuActiveFill;
            btn.HoverState.FillColor = MenuActiveHoverFill;
            btn.BorderColor = MenuActiveBorder;
            btn.BorderThickness = 2;
            btn.ForeColor = Color.White;
            btn.Font = new Font(style.Font.FontFamily, style.Font.Size + 0.5f, FontStyle.Bold);
            btn.Text = "★ " + style.Text;
        }

        private static void ApplyNormalMenuStyle(Guna2Button btn, MenuButtonStyle style)
        {
            if (btn == null || style == null)
                return;

            btn.FillColor = style.FillColor;
            btn.HoverState.FillColor = style.HoverFillColor;
            btn.BorderColor = style.BorderColor;
            btn.BorderThickness = style.BorderThickness;
            btn.ForeColor = style.ForeColor;
            btn.Font = style.Font;
            btn.Text = style.Text;
        }

        private static string CleanMenuText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string cleaned = text.Trim();
            if (cleaned.StartsWith("★"))
                cleaned = cleaned.Substring(1).Trim();
            if (cleaned.StartsWith("▶"))
                cleaned = cleaned.Substring(1).Trim();
            return cleaned;
        }

        private void FocusEmbeddedForm(Form form)
        {
            if (form == null || form.IsDisposed || !form.Visible)
                return;

            form.Select();
            form.Focus();
            form.Activate();

            if (form is SampleView view && view.txtSearch != null && view.txtSearch.Visible && view.txtSearch.Enabled)
            {
                view.txtSearch.Focus();
                return;
            }

            Control focusTarget = FindFirstFocusableControl(form);
            focusTarget?.Focus();
        }

        private static Control FindFirstFocusableControl(Control parent)
        {
            if (parent == null)
                return null;

            foreach (Control child in parent.Controls)
            {
                if (child.CanSelect && child.CanFocus && child.Visible && child.Enabled)
                    return child;

                Control nested = FindFirstFocusableControl(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        // 2 frmCreditAdjust
        // 3 frmDieselLedger
        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            //// If '1' is pressed
            //if (e.KeyCode == Keys.D1)
            //{
            //    AddControls(new frmDashBoard());  // Open Dashboard
            //}
            //// If '2' is pressed
            //else if (e.KeyCode == Keys.D2)
            //{
            //    AddControls(new frmCustomerView());  // Open Credit Adjust form
            //}
            //// If '3' is pressed
            //else if (e.KeyCode == Keys.D3)
            //{
            //    AddControls(new frmDealerNameView());  // Open Diesel Ledger form
            //}
            //// If '4' is pressed
            //else if (e.KeyCode == Keys.D4)
            //{
            //    AddControls(new frmDiselView());  // Open Payment form
            //}
            //// Add more keys for other forms if needed...
            //else if (e.KeyCode == Keys.D5)
            //{
            //    AddControls(new frmCreditAdjust());  // Open Dealer Name View form
            //}
            //else if (e.KeyCode == Keys.D6)
            //{
            //    AddControls(new frmDieselLedger());  // Open Customer View form
            //}
            //else if (e.KeyCode == Keys.D7)
            //{
            //    AddControls(new changepassword());  // Open Change Password form
            //}
        }


        private void guna2Button2_Click(object sender, EventArgs e)
        {
            AddControls(new frmCreditAdjust());


        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            AddControls(new frmCustomerView());
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            AddControls(new frmDiselView());
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            AddControls(new frmDashBoard());
        }


        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

   
        private void guna2Button10_Click(object sender, EventArgs e)
        {
            AddControls(new changepassword());
        }

        private void guna2Button3_Click_1(object sender, EventArgs e)
        {
            AddControls(new frmDieselLedger());
        }

        private void guna2Button4_Click_1(object sender, EventArgs e)
        {
            AddControls(new ReportAndBackup());
        }

        private void guna2Button5_Click_1(object sender, EventArgs e)
        {
            AddControls(new frmDealerNameView());

        }

        private void guna2Button6_Click_1(object sender, EventArgs e)
        {
            AddControls(new frmExpense());

        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Hide();
            LoginForm df = new LoginForm();
            df.Show();
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            AddControls(new frmStockView());

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            PermissionManager.GrantFullControlToAppFolder();
            InitializeSideMenuHighlight();
            Services.FormNavigationService.Initialize();
            Services.AutoBackupService.Start();
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            AddControls(new frmDealerNameView());

        }

        private void btnstock_Click(object sender, EventArgs e)
        {
            AddControls(new frmDealerNameView());

        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {
            AddControls(new frmCustomerToCustomerView());
        }

        private void btnstock_Click_1(object sender, EventArgs e)
        {
            AddControls(new frmStockDieselView());
        }

        private void guna2Button11_Click_1(object sender, EventArgs e)
        {
            AddControls(new frmDealertoDealerView());

        }

        private void guna2Button13_Click(object sender, EventArgs e)
        {
            AddControls(new frmDieselLedgerView());
        }

        private void guna2Button14_Click(object sender, EventArgs e)
        {
            AddControls(new FrmDirectDealerPaymentAmountView());
        }

        private void guna2Button15_Click(object sender, EventArgs e)
        {
            AddControls(new frmBankAccountView());
        }

        private void dgvFaida_Click(object sender, EventArgs e)
        {
            AddControls(new frmClosingformEntry());
        }

        private void btnClosing2_Click(object sender, EventArgs e)
        {
            AddControls(new frmClosing2());
        }

        private void guna2Button16_Click(object sender, EventArgs e)
        {
            AddControls(new frmExpense());
        }

        private void btnGeminiAi_Click(object sender, EventArgs e)
        {
            MainClass.BlurBackground(new frmGeminiChat());
        }

        private void btnPetroleumCalc_Click(object sender, EventArgs e)
        {
            MainClass.BlurBackground(new frmPetroleumCalculator());
        }
    }
}
