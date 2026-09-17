using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class SearchBar : UserControl
{
    private readonly TextBox _innerTextBox;
    private bool _isFocused = false;

    private const int CornerRadius = 8;

    // Palette Colors matching Atelier system theme
    private static readonly Color ColorNormalBorder = Color.FromArgb(220, 210, 210);
    private static readonly Color ColorActiveBorder = Color.FromArgb(190, 110, 120); // Dusty Rose Accent
    private static readonly Color ColorIcon = Color.FromArgb(150, 140, 145);
    private static readonly Color ColorBackground = Color.White;
    private static readonly Color ColorText = Color.FromArgb(45, 35, 40);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

    public event EventHandler? SearchTextChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue("")]
    public string TextValue
    {
        get => _innerTextBox.Text;
        set => _innerTextBox.Text = value;
    }

    public SearchBar()
    {
        this.DoubleBuffered = true;
        this.SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        this.BackColor = Color.Transparent;
        this.Size = new Size(320, 34);

        _innerTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorText,
            BackColor = ColorBackground,
            Location = new Point(34, 7),
            Width = this.Width - 44,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        _innerTextBox.TextChanged += (s, e) => SearchTextChanged?.Invoke(this, EventArgs.Empty);

        _innerTextBox.GotFocus += (s, e) => { _isFocused = true; this.Invalidate(); };
        _innerTextBox.LostFocus += (s, e) => { _isFocused = false; this.Invalidate(); };

        this.Click += (s, e) => _innerTextBox.Focus();
        this.Controls.Add(_innerTextBox);

        SetCueBanner("Search customer by name, code, phone...");
    }

    public void SetCueBanner(string placeholder)
    {
        if (_innerTextBox.IsHandleCreated)
        {
            SendMessage(_innerTextBox.Handle, 0x1501, 0, placeholder);
        }
        else
        {
            _innerTextBox.HandleCreated += (s, e) => SendMessage(_innerTextBox.Handle, 0x1501, 0, placeholder);
        }
    }

    public void Clear()
    {
        _innerTextBox.Clear();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

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

        // Inset by 1px so the border stroke is not clipped
        var rect = new Rectangle(1, 1, this.Width - 2, this.Height - 2);
        using var path = CreateRoundedRect(rect, CornerRadius);

        using (var fillBrush = new SolidBrush(ColorBackground))
        {
            e.Graphics.FillPath(fillBrush, path);
        }

        var borderColor = _isFocused ? ColorActiveBorder : ColorNormalBorder;
        using (var borderPen = new Pen(borderColor, _isFocused ? 1.4f : 1.1f))
        {
            borderPen.Alignment = PenAlignment.Center;
            e.Graphics.DrawPath(borderPen, path);
        }

        // Crisp vector search glass instead of an emoji
        DrawSearchIcon(e.Graphics, new Point(12, (this.Height - 14) / 2));
    }

    private static void DrawSearchIcon(Graphics g, Point pt)
    {
        using var iconPen = new Pen(ColorIcon, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        // Lens circle
        g.DrawEllipse(iconPen, pt.X, pt.Y, 9, 9);
        // Handle line
        g.DrawLine(iconPen, pt.X + 7.5f, pt.Y + 7.5f, pt.X + 12.5f, pt.Y + 12.5f);
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