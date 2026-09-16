using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms;

public partial class LoginForm : Form
{
    private readonly AuthService _authService;

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private CheckBox chkShowPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;

    private Panel pnlBrand = null!;
    private Panel pnlBrandContent = null!;
    private Panel pnlFormArea = null!;
    private Panel pnlCard = null!;

    // Colors
    private static readonly Color ColorBrandDark = Color.FromArgb(44, 34, 38);
    private static readonly Color ColorBrandLight = Color.FromArgb(96, 58, 66);
    private static readonly Color ColorPrimary = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorPrimaryHover = Color.FromArgb(171, 99, 108);
    private static readonly Color ColorBorder = Color.FromArgb(220, 210, 210);
    private static readonly Color ColorSubtext = Color.FromArgb(120, 110, 115);
    private static readonly Color ColorFormBg = Color.FromArgb(250, 248, 248);

    public LoginResult? AuthenticatedUser { get; private set; }

    public LoginForm() : this(new AuthService())
    {
    }

    public LoginForm(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        InitializeLoginUI();
    }

    private sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void InitializeLoginUI()
    {
        this.Text = "Vantage CRM - Sign In";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.WindowState = FormWindowState.Maximized;
        this.MinimumSize = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 10f, FontStyle.Regular);

        var pnlRoot = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        pnlRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        pnlRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        pnlRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildBrandPanel();
        BuildFormPanel();

        pnlRoot.Controls.Add(pnlBrand, 0, 0);
        pnlRoot.Controls.Add(pnlFormArea, 1, 0);
        this.Controls.Add(pnlRoot);

        this.AcceptButton = btnLogin;
        this.Shown += (s, e) => { CenterBrandContent(); CenterCard(); };
    }

    private void BuildBrandPanel()
    {
        pnlBrand = new BufferedPanel { Dock = DockStyle.Fill, BackColor = ColorBrandDark };
        pnlBrand.Paint += PaintBrandBackground;
        pnlBrand.Resize += (s, e) => CenterBrandContent();

        pnlBrandContent = new BufferedPanel { Size = new Size(420, 420), BackColor = Color.Transparent };

        pnlBrandContent.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var badgeBrush = new SolidBrush(Color.FromArgb(249, 241, 241));
            using var badgePen = new Pen(ColorPrimary, 2f);
            e.Graphics.FillEllipse(badgeBrush, 1, 1, 70, 70);
            e.Graphics.DrawEllipse(badgePen, 1, 1, 70, 70);

            using var badgeFont = new Font("Segoe UI Semibold", 24f, FontStyle.Bold);
            using var textBrush = new SolidBrush(ColorPrimary);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString("D", badgeFont, textBrush, new RectangleF(0, 0, 72, 72), sf);

            using var bulletBrush = new SolidBrush(ColorPrimary);
            int startY = 200 + 8;
            for (int i = 0; i < 3; i++)
            {
                e.Graphics.FillEllipse(bulletBrush, 2, startY + (i * 38), 9, 9);
            }
        };

        var lblTitle = new Label
        {
            Text = "VANTAGE - DRESS RENTAL CRM",
            Font = new Font("Segoe UI Semibold", 21f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Location = new Point(0, 94),
            Size = new Size(420, 38),
            BackColor = Color.Transparent
        };

        var lblSubtitle = new Label
        {
            Text = "Boutique client, fitting & rental management",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(225, 210, 212),
            AutoSize = false,
            Location = new Point(0, 136),
            Size = new Size(420, 28),
            BackColor = Color.Transparent
        };

        pnlBrandContent.Controls.Add(lblTitle);
        pnlBrandContent.Controls.Add(lblSubtitle);

        string[] features =
        {
            "Centralized client profiles & measurements",
            "End-to-end rental & fitting pipeline",
            "Multi-boutique tenant management"
        };

        int featureY = 200;
        foreach (var feature in features)
        {
            var lblFeature = new Label
            {
                Text = feature,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(235, 225, 226),
                AutoSize = false,
                Location = new Point(24, featureY),
                Size = new Size(396, 26),
                BackColor = Color.Transparent
            };

            pnlBrandContent.Controls.Add(lblFeature);
            featureY += 38;
        }

        var lblFooter = new Label
        {
            Text = "Authorized boutique staff only",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(180, 165, 168),
            AutoSize = false,
            Location = new Point(0, featureY + 28),
            Size = new Size(420, 24),
            BackColor = Color.Transparent
        };

        pnlBrandContent.Controls.Add(lblFooter);
        pnlBrandContent.Height = featureY + 60;

        pnlBrand.Controls.Add(pnlBrandContent);
    }

    private void PaintBrandBackground(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var gradientBrush = new LinearGradientBrush(
            new Rectangle(0, 0, Math.Max(pnlBrand.Width, 1), Math.Max(pnlBrand.Height, 1)),
            ColorBrandDark, ColorBrandLight, 45f);
        e.Graphics.FillRectangle(gradientBrush, pnlBrand.ClientRectangle);

        using var circleBrush1 = new SolidBrush(Color.FromArgb(16, 255, 255, 255));
        e.Graphics.FillEllipse(circleBrush1, pnlBrand.Width - 240, -100, 360, 360);

        using var circleBrush2 = new SolidBrush(Color.FromArgb(10, 255, 255, 255));
        e.Graphics.FillEllipse(circleBrush2, -140, pnlBrand.Height - 240, 340, 340);
    }

    private void CenterBrandContent()
    {
        if (pnlBrandContent == null || pnlBrand == null) return;
        pnlBrandContent.Left = Math.Max(24, (pnlBrand.ClientSize.Width - pnlBrandContent.Width) / 2);
        pnlBrandContent.Top = Math.Max(24, (pnlBrand.ClientSize.Height - pnlBrandContent.Height) / 2);
    }

    private void BuildFormPanel()
    {
        pnlFormArea = new BufferedPanel { Dock = DockStyle.Fill, BackColor = ColorFormBg };
        pnlFormArea.Paint += PaintCardShadow;
        pnlFormArea.Resize += (s, e) => CenterCard();

        const int cardWidth = 480;
        const int cardHeight = 540;
        const int pad = 40;
        int fieldWidth = cardWidth - (pad * 2);

        pnlCard = new BufferedPanel { Size = new Size(cardWidth, cardHeight), BackColor = Color.White };

        using (var cardPath = RoundedRect(new Rectangle(0, 0, cardWidth, cardHeight), 18))
        {
            pnlCard.Region = new Region(cardPath);
        }

        pnlCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            using var path = RoundedRect(rect, 18);
            using var borderPen = new Pen(ColorBorder, 1.2f);
            e.Graphics.DrawPath(borderPen, path);
        };

        var lblWelcome = new Label
        {
            Text = "Welcome Back",
            Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
            ForeColor = ColorBrandDark,
            AutoSize = false,
            Location = new Point(pad, 36),
            Size = new Size(fieldWidth, 34)
        };

        var lblWelcomeSub = new Label
        {
            Text = "Sign in to manage your boutique",
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = ColorSubtext,
            AutoSize = false,
            Location = new Point(pad, 74),
            Size = new Size(fieldWidth, 24)
        };

        var lblUser = new Label
        {
            Text = "Username",
            Location = new Point(pad, 118),
            AutoSize = true,
            ForeColor = ColorBrandDark,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
        };

        txtUsername = new TextBox
        {
            Text = "superadmin",
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 12f),
            Location = new Point(14, 11),
            Width = fieldWidth - 28,
            BackColor = Color.White
        };
        var pnlUserBox = CreateInputContainer(pad, 144, fieldWidth, 46, txtUsername);

        var lblPass = new Label
        {
            Text = "Password",
            Location = new Point(pad, 204),
            AutoSize = true,
            ForeColor = ColorBrandDark,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
        };

        txtPassword = new TextBox
        {
            Text = "SuperSecret123!",
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 12f),
            UseSystemPasswordChar = true,
            Location = new Point(14, 11),
            Width = fieldWidth - 28,
            BackColor = Color.White
        };
        var pnlPassBox = CreateInputContainer(pad, 230, fieldWidth, 46, txtPassword);

        chkShowPassword = new CheckBox
        {
            Text = "Show password",
            Location = new Point(pad, 288),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorSubtext,
            Cursor = Cursors.Hand
        };
        chkShowPassword.CheckedChanged += (s, e) =>
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        };

        lblStatus = new Label
        {
            Location = new Point(pad, 324),
            Size = new Size(fieldWidth, 32),
            ForeColor = Color.Firebrick,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = ""
        };

        btnLogin = new Button
        {
            Text = "Sign In",
            Location = new Point(pad, 364),
            Size = new Size(fieldWidth, 48),
            BackColor = ColorPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;

        using (var btnPath = RoundedRect(new Rectangle(0, 0, fieldWidth, 48), 10))
        {
            btnLogin.Region = new Region(btnPath);
        }

        btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = ColorPrimaryHover;
        btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = ColorPrimary;
        btnLogin.Click += async (s, e) => await AttemptLoginAsync();

        var lblFootnote = new Label
        {
            Text = "Contact your administrator if you need access.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            AutoSize = false,
            Location = new Point(pad, 432),
            Size = new Size(fieldWidth, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlCard.Controls.AddRange(new Control[]
        {
            lblWelcome, lblWelcomeSub,
            lblUser, pnlUserBox,
            lblPass, pnlPassBox,
            chkShowPassword, lblStatus, btnLogin, lblFootnote
        });

        pnlFormArea.Controls.Add(pnlCard);
    }

    private void PaintCardShadow(object? sender, PaintEventArgs e)
    {
        if (pnlCard == null) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var baseRect = pnlCard.Bounds;
        for (int i = 12; i >= 1; i--)
        {
            int inflate = i * 2;
            var shadowRect = Rectangle.Inflate(baseRect, inflate, inflate);
            shadowRect.Offset(0, 5);
            using var path = RoundedRect(shadowRect, 18 + (inflate / 2));
            using var brush = new SolidBrush(Color.FromArgb(4, 0, 0, 0));
            e.Graphics.FillPath(brush, path);
        }
    }

    private void CenterCard()
    {
        if (pnlCard == null || pnlFormArea == null) return;
        pnlCard.Left = Math.Max(24, (pnlFormArea.ClientSize.Width - pnlCard.Width) / 2);
        pnlCard.Top = Math.Max(24, (pnlFormArea.ClientSize.Height - pnlCard.Height) / 2);
        pnlFormArea.Invalidate();
    }

    private static Panel CreateInputContainer(int x, int y, int w, int h, TextBox linkedTextBox)
    {
        var borderColor = ColorBorder;
        var panel = new BufferedPanel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.White
        };

        panel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, w - 1, h - 1);
            using var path = RoundedRect(rect, 10);
            using var pen = new Pen(borderColor, 1.5f);
            e.Graphics.DrawPath(pen, path);
        };

        panel.Click += (s, e) => linkedTextBox.Focus();
        linkedTextBox.Enter += (s, e) => { borderColor = ColorPrimary; panel.Invalidate(); };
        linkedTextBox.Leave += (s, e) => { borderColor = ColorBorder; panel.Invalidate(); };

        panel.Controls.Add(linkedTextBox);
        return panel;
    }

    private async Task AttemptLoginAsync()
    {
        lblStatus.ForeColor = ColorSubtext;
        lblStatus.Text = "Authenticating...";
        SetFormBusy(true);

        var result = await _authService.LoginAsync(txtUsername.Text, txtPassword.Text);

        if (result.Success && result.User != null)
        {
            AuthenticatedUser = result.User;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            lblStatus.ForeColor = Color.Firebrick;
            lblStatus.Text = result.Message;
            if (!this.IsDisposed)
            {
                SetFormBusy(false);
            }
        }
    }

    private void SetFormBusy(bool busy)
    {
        btnLogin.Enabled = !busy;
        btnLogin.Text = busy ? "Signing In..." : "Sign In";
        txtUsername.Enabled = !busy;
        txtPassword.Enabled = !busy;
        chkShowPassword.Enabled = !busy;
    }
}