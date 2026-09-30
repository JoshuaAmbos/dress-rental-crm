using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public partial class ActiveTermsDialogForm : Form
{
    private const string DefaultConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;

    private Label lblTitle = null!;
    private Label lblBadge = null!;
    private Label lblEffective = null!;
    private TextBox txtContent = null!;
    private Button btnAccept = null!;
    private Button btnDecline = null!;
    private Panel pnlContentCard = null!;

    public RentalTerm? ActiveTerm { get; private set; }

    public ActiveTermsDialogForm(Func<TenantCrmDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory ?? (() =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(DefaultConnectionString)
                .Options;
            return new TenantCrmDbContext(options);
        });

        InitializeDialogLayout();
        _ = LoadActiveTermsAsync();
    }

    private void InitializeDialogLayout()
    {
        Text = "Rental Agreement Terms & Conditions";
        Size = new Size(720, 680);
        MinimumSize = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        Padding = new Padding(28, 20, 28, 20);

        // 1. Header Area
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.Transparent
        };

        lblTitle = new Label
        {
            Text = "Rental Terms & Liability Agreement",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblBadge = new Label
        {
            Text = "ACTIVE POLICY",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSuccess,
            BackColor = ColorBadgeBg,
            Location = new Point(0, 36),
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2)
        };
        lblBadge.Paint += (s, e) =>
        {
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, lblBadge.Width - 1, lblBadge.Height - 1), 4);
            lblBadge.Region = new Region(path);
        };

        lblEffective = new Label
        {
            Text = "Loading active agreement...",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(106, 38),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblBadge, lblEffective });

        // 2. Bottom Action Buttons Bar
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        btnAccept = new Button
        {
            Text = "I Agree and Accept",
            DialogResult = DialogResult.OK,
            Size = new Size(160, 38),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlActions.Width - 160, 10)
        };
        btnAccept.FlatAppearance.BorderSize = 0;
        btnAccept.MouseEnter += (s, e) => btnAccept.BackColor = ColorAccentHover;
        btnAccept.MouseLeave += (s, e) => btnAccept.BackColor = ColorAccent;

        btnDecline = new Button
        {
            Text = "Decline",
            DialogResult = DialogResult.Cancel,
            Size = new Size(110, 38),
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlActions.Width - 280, 10)
        };
        btnDecline.FlatAppearance.BorderSize = 1;
        btnDecline.FlatAppearance.BorderColor = ColorBorder;

        pnlActions.Resize += (s, e) =>
        {
            btnAccept.Location = new Point(pnlActions.ClientSize.Width - btnAccept.Width, 8);
            btnDecline.Location = new Point(pnlActions.ClientSize.Width - btnAccept.Width - btnDecline.Width - 10, 8);
        };

        pnlActions.Controls.AddRange(new Control[] { btnDecline, btnAccept });

        // 3. Central Document Card
        pnlContentCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(18)
        };
        pnlContentCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.25f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlContentCard.Width - 1, pnlContentCard.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

        txtContent = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Text = "Fetching active contractual terms and policies from database..."
        };

        pnlContentCard.Controls.Add(txtContent);

        // Assembly
        Controls.Add(pnlContentCard);
        Controls.Add(pnlActions);
        Controls.Add(pnlHeader);

        AcceptButton = btnAccept;
        CancelButton = btnDecline;
    }

    private async Task LoadActiveTermsAsync()
    {
        try
        {
            await using var db = _contextFactory();

            // Fetch the currently active policy revision
            ActiveTerm = await db.RentalTerms
                .AsNoTracking()
                .OrderByDescending(t => t.EffectiveDate)
                .FirstOrDefaultAsync(t => t.IsActive);

            if (ActiveTerm != null)
            {
                lblTitle.Text = ActiveTerm.PolicyTitle;
                lblEffective.Text = $"Version {ActiveTerm.VersionNumber} • Enforced since {ActiveTerm.EffectiveDate:MMMM dd, yyyy}";
                txtContent.Text = ActiveTerm.PolicyContent;
            }
            else
            {
                lblTitle.Text = "Standard Wardrobe Lease Agreement";
                lblEffective.Text = "Version 1.0 • Default System Terms";
                txtContent.Text =
                    "1. LEASE & RETURN POLICIES:\r\n" +
                    "All wardrobe items must be returned on or before the agreed Return Date in good condition.\r\n\r\n" +
                    "2. SECURITY DEPOSIT ESCROW:\r\n" +
                    "A refundable security deposit is held for the duration of the lease. Minor professional dry cleaning is included.\r\n\r\n" +
                    "3. LOSS & DAMAGE:\r\n" +
                    "Severe fabric tears, permanent stains, burns, or unapproved alterations will result in partial or full forfeiture of the security deposit.\r\n\r\n" +
                    "4. OVERDUE PENALTIES:\r\n" +
                    "Returns past the designated deadline incur late fees assessed per day overdue.";
            }

            txtContent.Select(0, 0); // Scroll to top
        }
        catch (Exception ex)
        {
            txtContent.Text = $"Failed to load terms from database: {ex.GetBaseException().Message}";
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}