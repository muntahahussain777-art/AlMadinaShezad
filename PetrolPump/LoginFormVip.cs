using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public partial class LoginForm
    {
        private static readonly Color VipDeep = Color.FromArgb(8, 14, 34);
        private static readonly Color VipNavy = Color.FromArgb(16, 27, 60);
        private static readonly Color VipCard = Color.FromArgb(21, 32, 66);
        private static readonly Color VipGold = Color.FromArgb(214, 174, 74);
        private static readonly Color VipGoldSoft = Color.FromArgb(240, 208, 122);

        private Timer _vipEntryTimer;
        private Timer _vipPulseTimer;
        private float _vipEntry;
        private float _vipPulse;
        private Image _vipLogo;
        private MemoryStream _vipLogoStream;
        private Point _vipDragOffset;
        private bool _vipDragging;

        private readonly Control[] _vipStaggered = new Control[5];
        private readonly int[] _vipTargetTop = new int[5];

        private void SetupVipLogin()
        {
            try
            {
                DoubleBuffered = true;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                ClientSize = new Size(460, 680);
                BackColor = VipNavy;
                KeyPreview = true;
                Opacity = 0d;

                ApplyRoundedRegion();
                LayoutVipControls();
                StyleVipControls();
                LoadVipLogo();
                WireVipEvents();

                Paint += LoginForm_VipPaint;

                _vipPulseTimer = new Timer { Interval = 40 };
                _vipPulseTimer.Tick += VipPulseTick;

                _vipEntryTimer = new Timer { Interval = 15 };
                _vipEntryTimer.Tick += VipEntryTick;
                _vipEntryTimer.Start();
            }
            catch
            {
                // Design fail ho to bhi login kaam karta rahe
                Opacity = 1d;
            }
        }

        private void ApplyRoundedRegion()
        {
            using (var path = RoundedPath(new Rectangle(0, 0, Width, Height), 24))
                Region = new Region(path);
        }

        private void LayoutVipControls()
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Size = new Size(214, 214);
            pictureBox1.Location = new Point((ClientSize.Width - 214) / 2, 58);

            label1.BackColor = Color.Transparent;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold);
            label1.ForeColor = VipGoldSoft;

            label2.BackColor = Color.Transparent;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Regular);
            label2.ForeColor = Color.FromArgb(150, 168, 205);

            txtname.Size = new Size(330, 50);
            txtname.Location = new Point((ClientSize.Width - 330) / 2, 388);

            txtpassword.Size = new Size(330, 50);
            txtpassword.Location = new Point((ClientSize.Width - 330) / 2, 452);

            btnSave.Size = new Size(330, 52);
            btnSave.Location = new Point((ClientSize.Width - 330) / 2, 528);

            btnClose.Size = new Size(330, 44);
            btnClose.Location = new Point((ClientSize.Width - 330) / 2, 592);

            CenterVipLabel(label1, 288);
            CenterVipLabel(label2, 330);

            _vipStaggered[0] = label1;
            _vipStaggered[1] = label2;
            _vipStaggered[2] = txtname;
            _vipStaggered[3] = txtpassword;
            _vipStaggered[4] = btnSave;
            for (int i = 0; i < _vipStaggered.Length; i++)
                _vipTargetTop[i] = _vipStaggered[i].Top;
        }

        private void CenterVipLabel(Label lbl, int top)
        {
            Size measured = TextRenderer.MeasureText(lbl.Text, lbl.Font);
            lbl.Location = new Point((ClientSize.Width - measured.Width) / 2, top);
        }

        private void StyleVipControls()
        {
            StyleVipTextBox(txtname);
            StyleVipTextBox(txtpassword);

            btnSave.Animated = true;
            btnSave.AutoRoundedCorners = false;
            btnSave.BorderRadius = 14;
            btnSave.BorderThickness = 0;
            btnSave.FillColor = VipGold;
            btnSave.HoverState.FillColor = VipGoldSoft;
            btnSave.ForeColor = Color.FromArgb(12, 20, 44);
            btnSave.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSave.ShadowDecoration.Enabled = true;
            btnSave.ShadowDecoration.Depth = 12;
            btnSave.ShadowDecoration.Color = Color.FromArgb(120, 0, 0, 0);
            btnSave.Cursor = Cursors.Hand;

            btnClose.Animated = true;
            btnClose.AutoRoundedCorners = false;
            btnClose.BorderRadius = 14;
            btnClose.BorderThickness = 1;
            btnClose.BorderColor = Color.FromArgb(70, 90, 130);
            btnClose.FillColor = Color.FromArgb(18, 28, 56);
            btnClose.HoverState.FillColor = Color.FromArgb(28, 42, 78);
            btnClose.HoverState.BorderColor = VipGold;
            btnClose.ForeColor = Color.FromArgb(168, 186, 220);
            btnClose.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular);
            btnClose.ShadowDecoration.Enabled = false;
            btnClose.Cursor = Cursors.Hand;
        }

        private void StyleVipTextBox(Guna.UI2.WinForms.Guna2TextBox box)
        {
            box.AutoRoundedCorners = false;
            box.BorderRadius = 14;
            box.BorderThickness = 1;
            box.BorderColor = Color.FromArgb(58, 76, 118);
            box.FillColor = VipCard;
            box.ForeColor = Color.White;
            box.PlaceholderForeColor = Color.FromArgb(126, 145, 182);
            box.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Regular);
            box.HoverState.BorderColor = Color.FromArgb(120, 148, 200);
            box.FocusedState.BorderColor = VipGold;
            box.FocusedState.FillColor = Color.FromArgb(26, 39, 78);
            box.TextAlign = HorizontalAlignment.Center;
            box.ShadowDecoration.Enabled = false;
        }

        private void WireVipEvents()
        {
            MouseDown += VipDragDown;
            MouseMove += VipDragMove;
            MouseUp += VipDragUp;
            pictureBox1.MouseDown += VipDragDown;
            pictureBox1.MouseMove += VipDragMove;
            pictureBox1.MouseUp += VipDragUp;
            label1.MouseDown += VipDragDown;
            label1.MouseMove += VipDragMove;
            label1.MouseUp += VipDragUp;

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) btnSave.PerformClick();
                else if (e.KeyCode == Keys.Escape) btnClose.PerformClick();
            };

            FormClosed += (s, e) =>
            {
                if (_vipEntryTimer != null) _vipEntryTimer.Stop();
                if (_vipPulseTimer != null) _vipPulseTimer.Stop();
            };
        }

        private void VipDragDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _vipDragging = true;
            Point screen = (sender as Control).PointToScreen(e.Location);
            _vipDragOffset = new Point(screen.X - Left, screen.Y - Top);
        }

        private void VipDragMove(object sender, MouseEventArgs e)
        {
            if (!_vipDragging) return;
            Point screen = (sender as Control).PointToScreen(e.Location);
            Location = new Point(screen.X - _vipDragOffset.X, screen.Y - _vipDragOffset.Y);
        }

        private void VipDragUp(object sender, MouseEventArgs e)
        {
            _vipDragging = false;
        }

        private void VipEntryTick(object sender, EventArgs e)
        {
            _vipEntry += 0.035f;
            if (_vipEntry >= 1f)
            {
                _vipEntry = 1f;
                _vipEntryTimer.Stop();
                _vipPulseTimer.Start();
                txtname.Focus();
            }

            float eased = 1f - (float)Math.Pow(1f - _vipEntry, 3);
            Opacity = Math.Min(1d, eased);

            for (int i = 0; i < _vipStaggered.Length; i++)
            {
                float delay = i * 0.12f;
                float local = (_vipEntry - delay) / (1f - delay <= 0 ? 1f : 1f - delay);
                if (local < 0f) local = 0f;
                if (local > 1f) local = 1f;
                float localEased = 1f - (float)Math.Pow(1f - local, 3);
                _vipStaggered[i].Top = _vipTargetTop[i] + (int)(36 * (1f - localEased));
            }

            pictureBox1.Top = 58 - (int)(18 * (1f - eased));
            Invalidate();
        }

        private void VipPulseTick(object sender, EventArgs e)
        {
            _vipPulse += 0.03f;
            if (_vipPulse > 1f) _vipPulse -= 1f;
            Invalidate(new Rectangle(0, 0, ClientSize.Width, 300));
        }

        private void LoginForm_VipPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var full = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

            using (var bg = new LinearGradientBrush(full, VipDeep, VipNavy, 60f))
                g.FillRectangle(bg, full);

            DrawVipSheen(g, full);
            DrawVipGlow(g);
            DrawVipDividers(g);
            DrawVipBorder(g, full);
        }

        private void DrawVipSheen(Graphics g, Rectangle full)
        {
            float shift = (float)Math.Sin(_vipPulse * Math.PI * 2) * 40f;
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(new[]
                {
                    new PointF(-120 + shift, full.Height),
                    new PointF(120 + shift, full.Height),
                    new PointF(full.Width + 60 + shift, -40),
                    new PointF(full.Width - 200 + shift, -40)
                });
                using (var brush = new LinearGradientBrush(full,
                    Color.FromArgb(16, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 45f))
                    g.FillPath(brush, path);
            }
        }

        private void DrawVipGlow(Graphics g)
        {
            float breathe = 0.5f + 0.5f * (float)Math.Sin(_vipPulse * Math.PI * 2);
            var center = new PointF(ClientSize.Width / 2f, 165f);

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 120f + ring * 26f + breathe * 14f;
                int alpha = (int)((46 - ring * 13) * (0.55f + 0.45f * breathe));
                if (alpha <= 0) continue;

                var rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var glow = new PathGradientBrush(path))
                    {
                        glow.CenterColor = Color.FromArgb(alpha, VipGold);
                        glow.SurroundColors = new[] { Color.FromArgb(0, VipGold) };
                        g.FillPath(glow, path);
                    }
                }
            }

            float arcRadius = 128f + breathe * 6f;
            using (var pen = new Pen(Color.FromArgb(90 + (int)(70 * breathe), VipGold), 1.6f))
            {
                var arcRect = new RectangleF(center.X - arcRadius, center.Y - arcRadius, arcRadius * 2, arcRadius * 2);
                float sweepStart = _vipPulse * 360f;
                g.DrawArc(pen, arcRect, sweepStart, 70f);
                g.DrawArc(pen, arcRect, sweepStart + 180f, 70f);
            }
        }

        private void DrawVipDividers(Graphics g)
        {
            int cx = ClientSize.Width / 2;
            DrawGoldDivider(g, cx, label2.Top + label2.Height + 16, 150);
            DrawGoldDivider(g, cx, ClientSize.Height - 26, 110);
        }

        private void DrawGoldDivider(Graphics g, int centerX, int y, int halfWidth)
        {
            var rect = new Rectangle(centerX - halfWidth, y, halfWidth * 2, 1);
            using (var brush = new LinearGradientBrush(
                new Rectangle(rect.X, rect.Y, rect.Width, 2),
                Color.FromArgb(0, VipGold), Color.FromArgb(0, VipGold), 0f))
            {
                var blend = new ColorBlend
                {
                    Colors = new[]
                    {
                        Color.FromArgb(0, VipGold),
                        Color.FromArgb(170, VipGold),
                        Color.FromArgb(0, VipGold)
                    },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, rect);
            }

            using (var dot = new SolidBrush(Color.FromArgb(200, VipGoldSoft)))
                g.FillEllipse(dot, centerX - 2, y - 2, 5, 5);
        }

        private void DrawVipBorder(Graphics g, Rectangle full)
        {
            var inner = new Rectangle(full.X, full.Y, full.Width - 1, full.Height - 1);
            using (var path = RoundedPath(inner, 24))
            using (var pen = new Pen(Color.FromArgb(110, VipGold), 1.4f))
                g.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LoadVipLogo()
        {
            string path = ResolveLogoPath();
            if (path == null) return;

            try
            {
                // GIF/JPG ko as-is load karo taake MP4 wali animation (GIF) chale
                // aur black/gold Al Noor logo MakeWhiteTransparent se spoil na ho.
                byte[] bytes = File.ReadAllBytes(path);
                var ms = new MemoryStream(bytes);
                Image img = Image.FromStream(ms);

                if (_vipLogo != null)
                {
                    try { _vipLogo.Dispose(); } catch { }
                }
                if (_vipLogoStream != null)
                {
                    try { _vipLogoStream.Dispose(); } catch { }
                }

                _vipLogoStream = ms;
                _vipLogo = img;
                pictureBox1.Image = _vipLogo;
            }
            catch
            {
                // Purani resource image hi chalti rahegi
            }
        }

        private static string ResolveLogoPath()
        {
            var roots = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Application.StartupPath,
                Directory.GetCurrentDirectory()
            };

            string[] fileNames =
            {
                "AlMadinaShahzadLogo.jpg",
                "AlMadinaShahzad Diesel & Cement Logo.jpg",
                "AlMadinaShahzad Diesel & Cement Logo.ico",
                "AlNoorLogo.gif",
                "ZaibLogo.gif",
                "AlNoorLogo.jpg",
                "ZaibLogo.jpg"
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = new DirectoryInfo(root);
                for (int up = 0; up < 4 && dir != null; up++, dir = dir.Parent)
                {
                    foreach (string name in fileNames)
                    {
                        string candidate = Path.Combine(dir.FullName, "Assets", name);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            return null;
        }

        private static Bitmap MakeWhiteTransparent(Bitmap source)
        {
            var copy = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(copy))
                g.DrawImage(source, new Rectangle(0, 0, copy.Width, copy.Height));

            BitmapData data = copy.LockBits(new Rectangle(0, 0, copy.Width, copy.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int bytes = Math.Abs(data.Stride) * copy.Height;
                var buffer = new byte[bytes];
                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                for (int i = 0; i < bytes; i += 4)
                {
                    byte b = buffer[i], gr = buffer[i + 1], r = buffer[i + 2];
                    int min = Math.Min(r, Math.Min(gr, b));
                    int max = Math.Max(r, Math.Max(gr, b));

                    if (min >= 238)
                    {
                        buffer[i + 3] = 0;
                        continue;
                    }
                    if (min >= 214)
                        buffer[i + 3] = (byte)(255 - (min - 214) * 255 / 24);

                    // Dark navy strokes ko light silver-blue karo warna dark bg mein doob jate hain
                    if (max < 150)
                    {
                        float mix = 1f - max / 150f;
                        buffer[i + 2] = Blend(r, 214, mix);
                        buffer[i + 1] = Blend(gr, 226, mix);
                        buffer[i] = Blend(b, 245, mix);
                    }
                }

                Marshal.Copy(buffer, 0, data.Scan0, bytes);
            }
            finally
            {
                copy.UnlockBits(data);
            }

            return copy;
        }

        private static byte Blend(byte from, byte to, float amount)
        {
            if (amount < 0f) amount = 0f;
            if (amount > 1f) amount = 1f;
            return (byte)(from + (to - from) * amount);
        }
    }
}
