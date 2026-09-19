using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class GarmentCardControl : UserControl
{
    // Atelier Color Palette
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);      // #EADFD9
    private static readonly Color ColorBorderHover = Color.FromArgb(190, 110, 120); // #BE6E78 (Dusty Rose)
    private static readonly Color ColorTextPrimary = Color.FromArgb(38, 22, 24);    // #261618 (Espresso Rose)
    private static readonly Color ColorTextMuted = Color.FromArgb(140, 124, 126);   // #8C7C7E (Mineral Taupe)
    private static readonly Color ColorImagePlaceholder = Color.FromArgb(249, 241, 241); // #F9F1F1 (Alabaster Blush)

    private bool _isHovered;
    private Image? _garmentImage;
    private string? _imagePath;
    private string _itemCode = "SKU-000";
    private string _styleName = "Gown Name";
    private string _category = "Evening Gown";
    private string _sizeLabel = "Size M";
    private decimal _rentalRate = 0.00m;
    private string _status = "Available"; // "Available", "Rented", "In Cleaning", "Alterations"

    // Exposed click event that passes the ID or card reference
    public event EventHandler? CardClicked;
    public event EventHandler? ActionClicked;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int RentalItemId { get; set; }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ItemCode
    {
        get => _itemCode;
        set { _itemCode = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string StyleName
    {
        get => _styleName;
        set { _styleName = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Category
    {
        get => _category;
        set { _category = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SizeLabel
    {
        get => _sizeLabel;
        set { _sizeLabel = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal RentalRate
    {
        get => _rentalRate;
        set { _rentalRate = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Status
    {
        get => _status;
        set { _status = value; Invalidate(); }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            _imagePath = value;
            LoadImageFromPath(_imagePath);
            Invalidate();
        }
    }

    [Category("Garment Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? GarmentImage
    {
        get => _garmentImage;
        set { _garmentImage = value; Invalidate(); }
    }

    public GarmentCardControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        DoubleBuffered = true;
        Size = new Size(240, 340);
        Margin = new Padding(12);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    private void LoadImageFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _garmentImage = null;
            return;
        }

        try
        {
            // Load file into memory stream to prevent file lock
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            _garmentImage = Image.FromStream(stream);
        }
        catch
        {
            _garmentImage = null;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        var actionRect = new Rectangle(14, Height - 46, Width - 28, 32);
        if (actionRect.Contains(e.Location))
        {
            ActionClicked?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            CardClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);

        // 1. Card Container & Border
        using (var cardPath = CreateRoundedRectangle(bounds, 10))
        {
            using (var bgBrush = new SolidBrush(ColorCardBg))
            {
                g.FillPath(bgBrush, cardPath);
            }

            using (var borderPen = new Pen(_isHovered ? ColorBorderHover : ColorBorder, _isHovered ? 1.5f : 1.0f))
            {
                g.DrawPath(borderPen, cardPath);
            }
        }

        // 2. Garment Image Preview or Stylized Fallback
        var imgRect = new Rectangle(12, 12, Width - 24, 150);
        using (var imgPath = CreateRoundedRectangle(imgRect, 8))
        {
            g.SetClip(imgPath);

            if (_garmentImage != null)
            {
                g.DrawImage(_garmentImage, imgRect);
            }
            else
            {
                using (var phBrush = new SolidBrush(ColorImagePlaceholder))
                {
                    g.FillRectangle(phBrush, imgRect);
                }

                using (var iconFont = new Font("Segoe UI", 26f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(
                        g,
                        "👗",
                        iconFont,
                        imgRect,
                        ColorBorderHover,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            g.ResetClip();

            using var imgPen = new Pen(ColorBorder, 1f);
            g.DrawPath(imgPen, imgPath);
        }

        // 3. Status Badge (Top-Right overlay on image)
        DrawStatusBadge(g, imgRect.Right - 8, imgRect.Top + 8);

        // 4. Garment ItemCode & Category (Subtext)
        int textY = 172;
        var subtextRect = new Rectangle(14, textY, Width - 28, 16);
        using (var subFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                g,
                $"{_itemCode.ToUpper()} • {_category.ToUpper()}",
                subFont,
                subtextRect,
                ColorTextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // 5. Garment StyleName
        textY += 18;
        var titleRect = new Rectangle(14, textY, Width - 28, 22);
        using (var titleFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                g,
                _styleName,
                titleFont,
                titleRect,
                ColorTextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // 6. Size Label
        textY += 22;
        var sizeRect = new Rectangle(14, textY, Width - 28, 18);
        using (var sizeFont = new Font("Segoe UI", 9f, FontStyle.Regular))
        {
            TextRenderer.DrawText(
                g,
                _sizeLabel,
                sizeFont,
                sizeRect,
                ColorTextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        // 7. Rental Rate
        textY += 24;
        var priceRect = new Rectangle(14, textY, Width - 28, 24);
        using (var priceFont = new Font("Segoe UI Semibold", 12f, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                g,
                $"₱{_rentalRate:N0} / lease",
                priceFont,
                priceRect,
                ColorBorderHover,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        // 8. Quick Action Button
        var actionRect = new Rectangle(14, Height - 44, Width - 28, 30);
        using (var btnPath = CreateRoundedRectangle(actionRect, 6))
        {
            Color btnBg = _isHovered ? ColorBorderHover : Color.FromArgb(249, 241, 241);
            Color btnFg = _isHovered ? Color.White : ColorTextPrimary;

            using (var fillBrush = new SolidBrush(btnBg))
            {
                g.FillPath(fillBrush, btnPath);
            }

            if (!_isHovered)
            {
                using var borderPen = new Pen(ColorBorder, 1f);
                g.DrawPath(borderPen, btnPath);
            }

            using (var btnFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    "View Details",
                    btnFont,
                    actionRect,
                    btnFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    private void DrawStatusBadge(Graphics g, int right, int top)
    {
        Color badgeBg;
        Color badgeFg;

        switch (_status.ToLower())
        {
            case "available":
                badgeBg = Color.FromArgb(232, 247, 238);
                badgeFg = Color.FromArgb(35, 120, 68);
                break;
            case "rented":
            case "leased":
                badgeBg = Color.FromArgb(253, 242, 242);
                badgeFg = Color.FromArgb(185, 45, 55);
                break;
            case "in cleaning":
            case "alterations":
                badgeBg = Color.FromArgb(255, 247, 230);
                badgeFg = Color.FromArgb(180, 115, 20);
                break;
            default:
                badgeBg = Color.FromArgb(245, 245, 245);
                badgeFg = ColorTextMuted;
                break;
        }

        using var font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
        var size = TextRenderer.MeasureText(_status, font);
        int badgeWidth = size.Width + 12;
        int badgeHeight = 20;
        var badgeRect = new Rectangle(right - badgeWidth, top, badgeWidth, badgeHeight);

        using (var path = CreateRoundedRectangle(badgeRect, 4))
        {
            using (var brush = new SolidBrush(badgeBg))
            {
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                g,
                _status,
                font,
                badgeRect,
                badgeFg,
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