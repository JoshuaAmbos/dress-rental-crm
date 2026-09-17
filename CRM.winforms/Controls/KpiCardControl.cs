using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class KpiCardControl : UserControl
{
    private readonly Label _lblTitle;
    private readonly Label _lblValue;
    private readonly Label _lblBadge;
    private readonly PictureBox _picIcon;

    private const int CornerRadius = 14;
    private static readonly Color ColorBorder = Color.FromArgb(232, 226, 226);
    private static readonly Color ColorTitle = Color.FromArgb(120, 110, 115);
    private static readonly Color ColorValue = Color.FromArgb(38, 22, 24);

    public KpiCardControl()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer |
                      ControlStyles.ResizeRedraw, true);

        this.BackColor = Color.Transparent;
        this.Size = new Size(220, 100);
        this.Padding = new Padding(14);

        _picIcon = new PictureBox
        {
            Location = new Point(14, 14),
            Size = new Size(18, 18),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        _lblTitle = new Label
        {
            Location = new Point(36, 14),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorTitle,
            BackColor = Color.Transparent
        };

        _lblValue = new Label
        {
            Location = new Point(12, 44),
            AutoSize = true,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = ColorValue,
            BackColor = Color.Transparent
        };

        _lblBadge = new Label
        {
            Location = new Point(80, 54),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            Padding = new Padding(5, 2, 5, 2)
        };

        this.Controls.AddRange(new Control[] { _picIcon, _lblTitle, _lblValue, _lblBadge });
    }

    public void SetData(string title, string value, string trendText, bool isPositive, Image? icon = null)
    {
        _lblTitle.Text = title;
        _lblValue.Text = value;
        _picIcon.Image = icon;

        _lblBadge.Text = trendText;
        _lblBadge.Location = new Point(_lblValue.Right + 8, 54);

        if (isPositive)
        {
            _lblBadge.BackColor = Color.FromArgb(232, 247, 238);
            _lblBadge.ForeColor = Color.FromArgb(35, 120, 68);
        }
        else
        {
            _lblBadge.BackColor = Color.FromArgb(253, 236, 238);
            _lblBadge.ForeColor = Color.FromArgb(185, 45, 55);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (this.Width > 0 && this.Height > 0)
        {
            // Clip OS region to eliminate rectangular corner artifacts
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
        using var fillBrush = new SolidBrush(Color.White);
        using var borderPen = new Pen(ColorBorder, 1.2f);

        // Draw solid card face and smooth outline
        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);
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