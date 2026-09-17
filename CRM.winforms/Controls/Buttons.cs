using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class PrimaryButton : Button
{
    private const int CornerRadius = 8;
    private bool _isHovered;
    private bool _isPressed;

    private static readonly Color DefaultBg = Color.FromArgb(190, 110, 120);       // Dusty Rose
    private static readonly Color HoverBg = Color.FromArgb(171, 99, 108);         // Deep Rosewood
    private static readonly Color PressedBg = Color.FromArgb(150, 85, 93);
    private static readonly Color DisabledBg = Color.FromArgb(215, 205, 205);

    public PrimaryButton()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer |
                      ControlStyles.ResizeRedraw, true);

        this.FlatStyle = FlatStyle.Flat;
        this.FlatAppearance.BorderSize = 0;
        this.BackColor = Color.Transparent;
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        this.Cursor = Cursors.Hand;
        this.Size = new Size(130, 38);
    }

    protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _isPressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (this.Width > CornerRadius * 2 && this.Height > CornerRadius * 2)
        {
            using var path = CreateRoundedRect(new Rectangle(0, 0, this.Width, this.Height), CornerRadius);
            this.Region = new Region(path);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using var path = CreateRoundedRect(rect, CornerRadius);

        Color fill = !this.Enabled ? DisabledBg :
                     _isPressed ? PressedBg :
                     _isHovered ? HoverBg : DefaultBg;

        using (var brush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(brush, path);
        }

        // Crisp border matching fill
        using (var pen = new Pen(fill, 1f))
        {
            e.Graphics.DrawPath(pen, path);
        }

        Color textClr = this.Enabled ? this.ForeColor : Color.FromArgb(140, 130, 130);
        TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, textClr,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
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

public class SecondaryButton : Button
{
    private const int CornerRadius = 8;
    private bool _isHovered;
    private bool _isPressed;

    private static readonly Color BorderColor = Color.FromArgb(215, 205, 205);
    private static readonly Color TextColor = Color.FromArgb(90, 75, 80);
    private static readonly Color HoverBg = Color.FromArgb(246, 240, 240);
    private static readonly Color PressedBg = Color.FromArgb(238, 230, 230);

    public SecondaryButton()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer |
                      ControlStyles.ResizeRedraw, true);

        this.FlatStyle = FlatStyle.Flat;
        this.FlatAppearance.BorderSize = 0;
        this.BackColor = Color.Transparent;
        this.ForeColor = TextColor;
        this.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        this.Cursor = Cursors.Hand;
        this.Size = new Size(110, 38);
    }

    protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _isPressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (this.Width > CornerRadius * 2 && this.Height > CornerRadius * 2)
        {
            using var path = CreateRoundedRect(new Rectangle(0, 0, this.Width, this.Height), CornerRadius);
            this.Region = new Region(path);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(1, 1, this.Width - 2, this.Height - 2);
        using var path = CreateRoundedRect(rect, CornerRadius);

        Color fill = !this.Enabled ? Color.FromArgb(245, 243, 243) :
                     _isPressed ? PressedBg :
                     _isHovered ? HoverBg : Color.White;

        using (var fillBrush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(fillBrush, path);
        }

        using (var borderPen = new Pen(BorderColor, 1.2f))
        {
            borderPen.Alignment = PenAlignment.Center;
            e.Graphics.DrawPath(borderPen, path);
        }

        Color textClr = this.Enabled ? this.ForeColor : Color.FromArgb(160, 155, 155);
        TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, textClr,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
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