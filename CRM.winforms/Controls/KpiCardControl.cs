using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Controls;

public class KpiCardControl : Control
{
    private string _title = "METRIC";
    private string _value = "0";
    private string _badgeText = "Status";
    private Color _accentColor = Color.FromArgb(37, 99, 235);
    private Color _badgeBgColor = Color.FromArgb(239, 246, 255);
    private Color _badgeTextColor = Color.FromArgb(29, 78, 216);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BadgeText
    {
        get => _badgeText;
        set { _badgeText = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BadgeBgColor
    {
        get => _badgeBgColor;
        set { _badgeBgColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BadgeTextColor
    {
        get => _badgeTextColor;
        set { _badgeTextColor = value; Invalidate(); }
    }

    // Atelier Palette
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);

    public KpiCardControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(280, 130);
    }

    public void SetData(string title, string value, string badgeText, Color accentColor, Color badgeBg, Color badgeFg)
    {
        _title = title;
        _value = value;
        _badgeText = badgeText;
        _accentColor = accentColor;
        _badgeBgColor = badgeBg;
        _badgeTextColor = badgeFg;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Card background & rounded border
        using (var path = CreateRoundedRectangle(rect, 8))
        {
            using var fillBrush = new SolidBrush(ColorCardBg);
            g.FillPath(fillBrush, path);

            using var borderPen = new Pen(ColorBorder, 1.25f);
            g.DrawPath(borderPen, path);
        }


        // Category title
        using (var titleFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, _title.ToUpperInvariant(), titleFont, new Point(22, 16), ColorSubtext);
        }

        // Values
        using (var valFont = new Font("Segoe UI", 26f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, _value, valFont, new Point(18, 36), ColorEspresso);
        }

        // Pill badge
        using (var badgeFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
        {
            var sz = TextRenderer.MeasureText(g, _badgeText, badgeFont);
            int pillWidth = sz.Width + 16;
            int pillHeight = 22;
            var pillRect = new Rectangle(22, 92, pillWidth, pillHeight);

            using var pillPath = CreateRoundedRectangle(pillRect, pillHeight / 2);
            using (var bgBrush = new SolidBrush(_badgeBgColor))
            {
                g.FillPath(bgBrush, pillPath);
            }

            TextRenderer.DrawText(g, _badgeText, badgeFont,
                new Rectangle(pillRect.Left, pillRect.Top + 1, pillRect.Width, pillRect.Height),
                _badgeTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}