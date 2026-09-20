using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace CRM.winforms.Controls;

public class WizardStepperControl : Control
{
    private int _currentStep = 1;
    private string[] _steps = new[] { "Client Selection", "Garment & Dates", "Fittings & Notes", "Payment", "Confirmation" };

    // Atelier Palette
    private static readonly Color ColorActiveBorder = Color.FromArgb(190, 110, 120);    // Dusty Rose
    private static readonly Color ColorActiveBg = Color.FromArgb(254, 250, 250);        // Soft Blush
    private static readonly Color ColorActiveText = Color.FromArgb(190, 110, 120);      // Dusty Rose
    private static readonly Color ColorInactiveBorder = Color.FromArgb(234, 223, 217);  // Soft Border
    private static readonly Color ColorInactiveBg = Color.White;
    private static readonly Color ColorInactiveText = Color.FromArgb(150, 140, 145);    // Muted Gray
    private static readonly Color ColorLine = Color.FromArgb(234, 223, 217);          // Connector Line
    private static readonly Color ColorCompletedLine = Color.FromArgb(190, 110, 120);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = Math.Clamp(value, 1, _steps.Length);
            Invalidate(); // Triggers clean repaint
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Steps
    {
        get => _steps;
        set
        {
            if (value != null && value.Length > 0)
            {
                _steps = value;
                Invalidate();
            }
        }
    }

    public WizardStepperControl()
    {
        // 1. Explicitly enable transparent background support for GDI+
        SetStyle(ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Height = 72;

        // 2. Default to Color.White (or container background) so the Designer won't crash
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        int count = _steps.Length;
        if (count == 0) return;

        int circleDiameter = 60;
        int circleRadius = circleDiameter / 2;
        int circleY = 8;
        int centerY = circleY + circleRadius;

        // Calculate horizontal centers for each node
        int stepWidth = Width / count;
        var centers = new Point[count];
        for (int i = 0; i < count; i++)
        {
            centers[i] = new Point((i * stepWidth) + (stepWidth / 2), centerY);
        }

        // 1. Draw Connecting Lines between Circle Nodes
        for (int i = 0; i < count - 1; i++)
        {
            int startX = centers[i].X + circleRadius + 8;
            int endX = centers[i + 1].X - circleRadius - 8;

            if (endX > startX)
            {
                Color lineColor = (i + 1 < _currentStep) ? ColorCompletedLine : ColorLine;
                using var linePen = new Pen(lineColor, 1.5f);
                g.DrawLine(linePen, startX, centerY, endX, centerY);
            }
        }

        // 2. Draw Circles, Numbers, and Labels
        using var numFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var activeLabelFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

        for (int i = 0; i < count; i++)
        {
            int stepNumber = i + 1;
            bool isCurrent = stepNumber == _currentStep;
            bool isPast = stepNumber < _currentStep;

            var center = centers[i];
            var circleRect = new Rectangle(center.X - circleRadius, circleY, circleDiameter, circleDiameter);

            // Determine Colors based on state
            Color borderColor = (isCurrent || isPast) ? ColorActiveBorder : ColorInactiveBorder;
            Color circleBgColor = isCurrent ? ColorActiveBg : (isPast ? ColorActiveBorder : ColorInactiveBg);
            Color numColor = isPast ? Color.White : (isCurrent ? ColorActiveText : ColorInactiveText);
            Color labelColor = isCurrent ? ColorActiveText : ColorInactiveText;

            // Fill Circle
            using (var bgBrush = new SolidBrush(circleBgColor))
            {
                g.FillEllipse(bgBrush, circleRect);
            }

            // Outline Circle
            using (var borderPen = new Pen(borderColor, isCurrent ? 1.75f : 1.25f))
            {
                g.DrawEllipse(borderPen, circleRect);
            }

            // Step Number or Checkmark
            string badgeText = isPast ? "✓" : stepNumber.ToString();
            TextRenderer.DrawText(g, badgeText, numFont, circleRect, numColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Step Text Label Below Circle
            var labelRect = new Rectangle(center.X - (stepWidth / 2) + 4, circleY + circleDiameter + 6, stepWidth - 8, 22);
            TextRenderer.DrawText(g, _steps[i], isCurrent ? activeLabelFont : labelFont, labelRect, labelColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordEllipsis);
        }
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        if (Parent != null)
        {
            BackColor = Parent.BackColor;
            Invalidate();
        }
    }
}