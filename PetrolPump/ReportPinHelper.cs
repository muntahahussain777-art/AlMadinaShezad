using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public static class ReportPinHelper
    {
        private const int RowHighlightHeight = 22;
        private const int RowHighlightMaxWidth = 340;
        private const int MaxMarkersPerForm = 80;

        private sealed class ScrollPinState
        {
            public int Page = 1;
            public int DocumentY;
            public bool HasPin;
        }

        private sealed class RowMarker
        {
            public int Page;
            public int DocumentX;
            public int DocumentY;
        }

        private sealed class FormContext
        {
            public Form Form;
            public ReportViewer Viewer;
            public ScrollPinState ScrollPin = new ScrollPinState();
            public List<RowMarker> Markers = new List<RowMarker>();
            public List<RowPinHighlight> Highlights = new List<RowPinHighlight>();
            public Timer ScrollTimer;
        }

        private static readonly Dictionary<Form, FormContext> Contexts = new Dictionary<Form, FormContext>();
        private static readonly Dictionary<Form, ToolTip> ToolTips = new Dictionary<Form, ToolTip>();

        public static void Attach(Form form)
        {
            if (form == null || form.IsDisposed) return;

            ReportViewer viewer = FindReportViewer(form);
            if (viewer == null) return;

            if (Contexts.ContainsKey(form))
                return;

            var ctx = new FormContext
            {
                Form = form,
                Viewer = viewer
            };

            form.Resize += (s, e) => UpdateAllHighlights(ctx);
            form.FormClosed += OnFormClosed;
            form.Shown += (s, e) => UpdateAllHighlights(ctx);

            viewer.RenderingComplete += (s, e) => UpdateAllHighlights(ctx);

            ctx.ScrollTimer = new Timer { Interval = 120 };
            ctx.ScrollTimer.Tick += (s, e) => UpdateAllHighlights(ctx);
            ctx.ScrollTimer.Start();

            Contexts[form] = ctx;
        }

        public static bool TryHandleKey(Form form, Keys keyData)
        {
            if (!Contexts.TryGetValue(form, out FormContext ctx))
                return false;

            if (keyData == Keys.F3 || keyData == Keys.F4)
            {
                AddMarkerAtMouse(ctx);
                return true;
            }

            if (keyData == Keys.F6)
            {
                RemoveNearestMarkerAtMouse(ctx);
                return true;
            }

            if (keyData == Keys.F9)
            {
                SaveScrollPin(ctx, useTopOfView: true);
                return true;
            }

            if (keyData == Keys.F10)
            {
                RestoreScrollPin(ctx);
                return true;
            }

            return false;
        }

        private static bool TryGetMouseOnReport(FormContext ctx, out Point onViewer)
        {
            onViewer = Point.Empty;
            if (ctx.Viewer == null || !ctx.Viewer.IsHandleCreated)
                return false;

            onViewer = ctx.Viewer.PointToClient(Control.MousePosition);
            if (!ctx.Viewer.ClientRectangle.Contains(onViewer))
                return false;

            return IsOnReportArea(ctx.Viewer, onViewer);
        }

        private static bool IsOnReportArea(ReportViewer viewer, Point onViewer)
        {
            ScrollableControl panel = FindScrollPanel(viewer);
            if (panel == null)
                return true;

            Point onPanel = panel.PointToClient(viewer.PointToScreen(onViewer));
            return panel.ClientRectangle.Contains(onPanel);
        }

        private static void AddMarkerAtMouse(FormContext ctx)
        {
            if (!TryGetMouseOnReport(ctx, out Point onViewer))
            {
                ShowTip(ctx.Form, "Pehle mouse report ki row par le jayein, phir F3 ya F4 dabayein.");
                return;
            }

            Point doc = GetDocumentPoint(ctx.Viewer, onViewer);
            int page = Math.Max(1, ctx.Viewer.CurrentPage);

            if (ctx.Markers.Count >= MaxMarkersPerForm)
                ctx.Markers.RemoveAt(0);

            ctx.Markers.Add(new RowMarker
            {
                Page = page,
                DocumentX = doc.X,
                DocumentY = SnapToRow(doc.Y)
            });

            ctx.ScrollPin.Page = page;
            ctx.ScrollPin.DocumentY = SnapToRow(doc.Y);
            ctx.ScrollPin.HasPin = true;

            EnsureHighlight(ctx, ctx.Markers.Count - 1);
            UpdateAllHighlights(ctx);

            int onPage = 0;
            foreach (RowMarker m in ctx.Markers)
            {
                if (m.Page == page)
                    onPage++;
            }

            ShowTip(ctx.Form,
                $"Row pin lag gaya ({onPage} is page). F6 = hatao. F10 = wapas.");
        }

        private static void RemoveNearestMarkerAtMouse(FormContext ctx)
        {
            if (ctx.Markers.Count == 0)
            {
                ShowTip(ctx.Form, "Koi row pin nahi hai.");
                return;
            }

            if (!TryGetMouseOnReport(ctx, out Point onViewer))
            {
                ShowTip(ctx.Form, "Mouse us row par le jayein jahan se pin hatana hai, phir F6 dabayein.");
                return;
            }

            int page = Math.Max(1, ctx.Viewer.CurrentPage);
            Point doc = GetDocumentPoint(ctx.Viewer, onViewer);
            int docY = SnapToRow(doc.Y);

            int bestIndex = -1;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < ctx.Markers.Count; i++)
            {
                RowMarker marker = ctx.Markers[i];
                if (marker.Page != page)
                    continue;

                int distance = Math.Abs(marker.DocumentY - docY);
                if (distance <= RowHighlightHeight * 2 && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                ShowTip(ctx.Form, "Is row par koi pin nahi. Mouse pin wali row par rakhein.");
                return;
            }

            ctx.Markers.RemoveAt(bestIndex);
            SyncScrollPinFromMarkers(ctx);
            UpdateAllHighlights(ctx);
            ShowTip(ctx.Form, $"Row pin hata diya. Baqi: {ctx.Markers.Count}.");
        }

        private static void SyncScrollPinFromMarkers(FormContext ctx)
        {
            if (ctx.Markers.Count == 0)
            {
                ctx.ScrollPin.HasPin = false;
                return;
            }

            RowMarker last = ctx.Markers[ctx.Markers.Count - 1];
            ctx.ScrollPin.Page = last.Page;
            ctx.ScrollPin.DocumentY = last.DocumentY;
            ctx.ScrollPin.HasPin = true;
        }

        private static void SaveScrollPin(FormContext ctx, bool useTopOfView)
        {
            ctx.ScrollPin.Page = Math.Max(1, ctx.Viewer.CurrentPage);
            ctx.ScrollPin.DocumentY = useTopOfView
                ? GetScrollY(ctx.Viewer)
                : ctx.ScrollPin.DocumentY;
            ctx.ScrollPin.HasPin = true;
            ShowTip(ctx.Form, $"Scroll pin — Page {ctx.ScrollPin.Page}. F10 se wapas aayein.");
        }

        private static void RestoreScrollPin(FormContext ctx)
        {
            if (!ctx.ScrollPin.HasPin)
            {
                ShowTip(ctx.Form, "Abhi koi pin nahi. Mouse row par + F3/F4 dabayein.");
                return;
            }

            int total = Math.Max(1, ctx.Viewer.GetTotalPages());
            int page = Math.Min(Math.Max(1, ctx.ScrollPin.Page), total);
            ctx.Viewer.CurrentPage = page;

            ctx.Form.BeginInvoke(new Action(() =>
            {
                int scrollTarget = Math.Max(0, ctx.ScrollPin.DocumentY - 30);
                SetScrollY(ctx.Viewer, scrollTarget);
                UpdateAllHighlights(ctx);
                ShowTip(ctx.Form, $"Pin par aa gaye — Page {page}.");
            }));
        }

        private static void EnsureHighlight(FormContext ctx, int index)
        {
            while (ctx.Highlights.Count <= index)
            {
                var highlight = new RowPinHighlight();
                highlight.Visible = false;
                ctx.Form.Controls.Add(highlight);
                highlight.BringToFront();
                ctx.Highlights.Add(highlight);
            }
        }

        private static int GetHighlightWidth(int panelWidth)
        {
            int width = Math.Min(RowHighlightMaxWidth, panelWidth * 55 / 100);
            return Math.Max(140, width);
        }

        private static void UpdateAllHighlights(FormContext ctx)
        {
            if (ctx.Viewer == null || ctx.Form == null || ctx.Form.IsDisposed || !ctx.Viewer.IsHandleCreated)
                return;

            int currentPage = Math.Max(1, ctx.Viewer.CurrentPage);
            ScrollableControl panel = FindScrollPanel(ctx.Viewer);
            int scrollX = GetScrollX(ctx.Viewer);
            int scrollY = GetScrollY(ctx.Viewer);
            int panelTop = panel?.Top ?? 0;
            int panelLeft = panel?.Left ?? 0;
            int panelHeight = panel?.Height ?? ctx.Viewer.ClientSize.Height;
            int panelWidth = panel?.Width ?? ctx.Viewer.ClientSize.Width;
            int highlightWidth = GetHighlightWidth(panelWidth);

            for (int i = 0; i < ctx.Markers.Count; i++)
            {
                EnsureHighlight(ctx, i);
                RowMarker marker = ctx.Markers[i];
                RowPinHighlight highlight = ctx.Highlights[i];

                if (marker.Page != currentPage)
                {
                    highlight.Visible = false;
                    continue;
                }

                int visibleY = marker.DocumentY - scrollY;
                if (visibleY < -RowHighlightHeight || visibleY > panelHeight + RowHighlightHeight)
                {
                    highlight.Visible = false;
                    continue;
                }

                int visibleX = marker.DocumentX - scrollX - highlightWidth / 2;
                visibleX = Math.Max(panelLeft, Math.Min(visibleX, panelLeft + panelWidth - highlightWidth));

                Point screenPt = ctx.Viewer.PointToScreen(new Point(visibleX, panelTop + visibleY));
                Point formPt = ctx.Form.PointToClient(screenPt);
                highlight.SetBounds(
                    formPt.X,
                    formPt.Y,
                    highlightWidth,
                    RowHighlightHeight);
                highlight.Visible = true;
                highlight.BringToFront();
            }

            for (int i = ctx.Markers.Count; i < ctx.Highlights.Count; i++)
                ctx.Highlights[i].Visible = false;
        }

        private static int SnapToRow(int documentY)
        {
            return Math.Max(0, documentY - RowHighlightHeight / 2);
        }

        private static void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            var form = sender as Form;
            if (form == null) return;

            if (Contexts.TryGetValue(form, out FormContext ctx))
            {
                ctx.ScrollTimer?.Stop();
                ctx.ScrollTimer?.Dispose();
                foreach (RowPinHighlight h in ctx.Highlights)
                    h?.Dispose();
                Contexts.Remove(form);
            }

            if (ToolTips.TryGetValue(form, out ToolTip tip))
            {
                tip.Dispose();
                ToolTips.Remove(form);
            }
        }

        private static void ShowTip(Form form, string text)
        {
            if (!ToolTips.TryGetValue(form, out ToolTip tip))
            {
                tip = new ToolTip { IsBalloon = true, ToolTipIcon = ToolTipIcon.Info };
                ToolTips[form] = tip;
            }
            int x = Math.Max(40, form.ClientSize.Width / 2 - 100);
            int y = Math.Max(40, form.ClientSize.Height - 100);
            tip.Show(text, form, x, y, 2800);
        }

        private static Point GetDocumentPoint(ReportViewer viewer, Point onViewer)
        {
            ScrollableControl panel = FindScrollPanel(viewer);
            if (panel == null)
                return new Point(onViewer.X, GetScrollY(viewer) + onViewer.Y);

            Point onPanel = panel.PointToClient(viewer.PointToScreen(onViewer));
            return new Point(
                GetScrollX(viewer) + onPanel.X,
                GetScrollY(viewer) + onPanel.Y);
        }

        private static ReportViewer FindReportViewer(Control parent)
        {
            if (parent is ReportViewer rv)
                return rv;

            foreach (Control child in parent.Controls)
            {
                ReportViewer found = FindReportViewer(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static ScrollableControl FindScrollPanel(Control parent)
        {
            ScrollableControl best = null;
            int bestArea = 0;

            void Walk(Control c)
            {
                if (c is ScrollableControl sc)
                {
                    int area = sc.ClientSize.Width * sc.ClientSize.Height;
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = sc;
                    }
                }
                foreach (Control child in c.Controls)
                    Walk(child);
            }

            Walk(parent);
            return best;
        }

        private static int GetScrollY(ReportViewer viewer)
        {
            ScrollableControl panel = FindScrollPanel(viewer);
            if (panel == null) return 0;
            return -panel.AutoScrollPosition.Y;
        }

        private static int GetScrollX(ReportViewer viewer)
        {
            ScrollableControl panel = FindScrollPanel(viewer);
            if (panel == null) return 0;
            return -panel.AutoScrollPosition.X;
        }

        private static void SetScrollY(ReportViewer viewer, int scrollY)
        {
            ScrollableControl panel = FindScrollPanel(viewer);
            if (panel == null) return;
            panel.AutoScrollPosition = new Point(0, scrollY);
        }

        private sealed class RowPinHighlight : Control
        {
            public RowPinHighlight()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = Color.FromArgb(255, 255, 230, 90);
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using (var fill = new SolidBrush(Color.FromArgb(190, 255, 210, 50)))
                {
                    e.Graphics.FillRectangle(fill, ClientRectangle);
                }

                using (var border = new Pen(Color.FromArgb(255, 220, 80, 0), 2))
                {
                    e.Graphics.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
                }
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_NCHITTEST = 0x84;
                const int HTTRANSPARENT = -1;
                if (m.Msg == WM_NCHITTEST)
                {
                    m.Result = (IntPtr)HTTRANSPARENT;
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}
