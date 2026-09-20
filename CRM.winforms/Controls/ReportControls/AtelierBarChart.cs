using CRM.winforms.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Controls;

public class AtelierBarChart : Control
{
    private List<MonthlyRevenueMetric> _data = [];

    private static readonly Color ColorBar = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorGridLine = Color.FromArgb(242, 235, 235);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);

    public AtelierBarChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.White;
    }

    public void SetData(List<MonthlyRevenueMetric> data)
    {
        _data = data ?? [];
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (_data.Count == 0)
        {
            using var fontEmpty = new Font("Segoe UI", 9.5f);
            TextRenderer.DrawText(g, "No revenue history available", fontEmpty, ClientRectangle, ColorSubtext, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        // Expanded left padding to prevent currency clipping
        int padLeft = 74;
        int padRight = 24;
        int padBottom = 34;
        int padTop = 32;

        int chartWidth = Width - padLeft - padRight;
        int chartHeight = Height - padTop - padBottom;

        decimal maxRevenue = Math.Max(1000m, _data.Max(d => d.Revenue) * 1.18m);

        using var gridPen = new Pen(ColorGridLine, 1f);
        using var axisFont = new Font("Segoe UI", 8.25f);

        // 1. Draw horizontal gridlines & right-aligned Y-axis labels
        for (int i = 0; i <= 4; i++)
        {
            int y = padTop + (chartHeight * i / 4);
            g.DrawLine(gridPen, padLeft, y, Width - padRight, y);

            decimal val = maxRevenue - (maxRevenue * i / 4);
            string label = $"${val:N0}";

            var labelRect = new Rectangle(0, y - 8, padLeft - 10, 16);
            TextRenderer.DrawText(g, label, axisFont, labelRect, ColorSubtext, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        // 2. Render Bars
        int count = _data.Count;
        int slotWidth = chartWidth / count;
        int barWidth = Math.Min(42, (int)(slotWidth * 0.52));

        for (int i = 0; i < count; i++)
        {
            var item = _data[i];
            int slotCenter = padLeft + (i * slotWidth) + (slotWidth / 2);
            int barLeft = slotCenter - (barWidth / 2);

            if (item.Revenue > 0)
            {
                int barHeight = Math.Max(6, (int)((item.Revenue / maxRevenue) * chartHeight));
                int barTop = padTop + chartHeight - barHeight;

                var barRect = new Rectangle(barLeft, barTop, barWidth, barHeight);

                using (var path = CreateTopRoundedRect(barRect, 4))
                using (var brush = new SolidBrush(ColorBar))
                {
                    g.FillPath(brush, path);
                }

                // Top Value Label
                using var valFont = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold);
                string revText = $"${item.Revenue:N0}";
                var sz = TextRenderer.MeasureText(g, revText, valFont);
                TextRenderer.DrawText(g, revText, valFont, new Point(slotCenter - (sz.Width / 2), barTop - 18), ColorEspresso);
            }
            else
            {
                // Baseline muted indicator dot for $0 months instead of an awkward floating bar
                using var zeroBrush = new SolidBrush(ColorGridLine);
                g.FillRectangle(zeroBrush, barLeft, padTop + chartHeight - 2, barWidth, 2);
            }

            // Month Label at baseline
            var lblSz = TextRenderer.MeasureText(g, item.MonthLabel, axisFont);
            TextRenderer.DrawText(g, item.MonthLabel, axisFont, new Point(slotCenter - (lblSz.Width / 2), Height - padBottom + 8), ColorSubtext);
        }
    }

    private static GraphicsPath CreateTopRoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
        path.CloseFigure();
        return path;
    }
}