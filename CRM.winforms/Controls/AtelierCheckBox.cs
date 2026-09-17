using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class AtelierCheckBox : CheckBox
{
    private bool _isHovered;
    private bool _isPressed;

    private static readonly Color ActiveColor = Color.FromArgb(190, 110, 120);
    private static readonly Color HoverBorderColor = Color.FromArgb(171, 99, 108);
    private static readonly Color NormalBorderColor = Color.FromArgb(210, 200, 202);
    private static readonly Color TextColor = Color.FromArgb(45, 35, 40);

    public AtelierCheckBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9.5f);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs mevent) { _isPressed = true; Invalidate(); base.OnMouseDown(mevent); }
    protected override void OnMouseUp(MouseEventArgs mevent) { _isPressed = false; Invalidate(); base.OnMouseUp(mevent); }
    protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Clear background safely without creating solid blocks over custom-drawn parents
        if (Parent != null && BackColor == Color.Transparent)
        {
            InvokePaintBackground(this, pevent);
        }
        else
        {
            using var bg = new SolidBrush(BackColor);
            g.FillRectangle(bg, ClientRectangle);
        }

        const int boxSize = 16;
        int boxY = (Height - boxSize) / 2;
        var boxRect = new Rectangle(1, boxY, boxSize, boxSize);

        using (var path = CreateRoundedRect(boxRect, 4))
        {
            if (Checked)
            {
                var fillColor = _isPressed ? HoverBorderColor : ActiveColor;
                using var fillBrush = new SolidBrush(fillColor);
                g.FillPath(fillBrush, path);

                using var pen = new Pen(fillColor, 1.2f);
                g.DrawPath(pen, path);

                using var checkPen = new Pen(Color.White, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                var p1 = new PointF(boxRect.Left + 3.5f, boxRect.Top + 8f);
                var p2 = new PointF(boxRect.Left + 6.5f, boxRect.Top + 11.5f);
                var p3 = new PointF(boxRect.Left + 12.5f, boxRect.Top + 4.5f);
                g.DrawLines(checkPen, new[] { p1, p2, p3 });
            }
            else
            {
                using var fillBrush = new SolidBrush(Color.White);
                g.FillPath(fillBrush, path);

                var borderColor = _isHovered ? HoverBorderColor : NormalBorderColor;
                using var pen = new Pen(borderColor, 1.4f);
                g.DrawPath(pen, path);
            }
        }

        int textX = boxSize + 8;
        var textRect = new Rectangle(textX, 0, Math.Max(0, Width - textX), Height);
        Color textClr = Enabled ? ForeColor : Color.FromArgb(160, 155, 155);
        TextRenderer.DrawText(g, Text, Font, textRect, textClr,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}