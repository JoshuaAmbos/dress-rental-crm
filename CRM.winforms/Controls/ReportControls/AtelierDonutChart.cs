using CRM.winforms.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Controls;

public class AtelierDonutChart : Control
{
    private List<StageDistributionMetric> _segments = [];

    private static readonly Color[] Palette =
    [
        Color.FromArgb(190, 110, 120), // Dusty Rose
        Color.FromArgb(37, 99, 235),   // Blue
        Color.FromArgb(220, 38, 38),   // Red
        Color.FromArgb(124, 58, 237),  // Purple
        Color.FromArgb(5, 150, 105),   // Green
        Color.FromArgb(145, 135, 140)  // Gray
    ];

    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);

    public AtelierDonutChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.White;
    }

    public void SetData(List<StageDistributionMetric> segments)
    {
        _segments = segments ?? [];
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (_segments.Count == 0)
        {
            using var fontEmpty = new Font("Segoe UI", 9.5f);
            TextRenderer.DrawText(g, "No stage distribution data", fontEmpty, ClientRectangle, ColorSubtext, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int donutDiameter = Math.Min(160, Math.Min(Width / 2 - 20, Height - 40));
        var donutRect = new Rectangle(24, (Height - donutDiameter) / 2, donutDiameter, donutDiameter);

        float currentAngle = -90f;
        for (int i = 0; i < _segments.Count; i++)
        {
            var item = _segments[i];
            float sweepAngle = (float)(item.Percentage / 100.0 * 360.0);
            if (sweepAngle <= 0) continue;

            using var brush = new SolidBrush(Palette[i % Palette.Length]);
            g.FillPie(brush, donutRect, currentAngle, sweepAngle);
            currentAngle += sweepAngle;
        }

        // Inner Cutout (Donut Hole)
        int holeDiameter = (int)(donutDiameter * 0.62);
        var holeRect = new Rectangle(
            donutRect.Left + (donutDiameter - holeDiameter) / 2,
            donutRect.Top + (donutDiameter - holeDiameter) / 2,
            holeDiameter,
            holeDiameter);

        using (var holeBrush = new SolidBrush(BackColor))
        {
            g.FillEllipse(holeBrush, holeRect);
        }

        // Center Label
        using (var centerFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, "Status", centerFont, holeRect, ColorEspresso, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // Legend List (Right side)
        int legendX = donutRect.Right + 28;
        int legendY = Math.Max(16, (Height - (_segments.Count * 24)) / 2);

        using var fontLabel = new Font("Segoe UI", 8.75f);
        using var fontVal = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold);

        for (int i = 0; i < _segments.Count; i++)
        {
            var item = _segments[i];
            var dotRect = new Rectangle(legendX, legendY + (i * 24) + 4, 10, 10);

            using (var dotBrush = new SolidBrush(Palette[i % Palette.Length]))
            {
                g.FillEllipse(dotBrush, dotRect);
            }

            TextRenderer.DrawText(g, item.StageName, fontLabel, new Point(legendX + 16, legendY + (i * 24)), ColorEspresso);
            TextRenderer.DrawText(g, $"{item.Percentage:0.#}%", fontVal, new Point(legendX + 110, legendY + (i * 24)), ColorSubtext);
        }
    }
}