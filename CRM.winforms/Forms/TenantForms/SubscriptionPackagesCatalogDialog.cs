using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class SubscriptionPackagesCatalogDialog : Form
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private FlowLayoutPanel pnlCards = null!;
    private Button btnClose = null!;

    public SubscriptionPackagesCatalogDialog(Func<MasterCrmDbContext> masterDbFactory)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));

        BuildLayout();
        Load += async (s, e) => await LoadPackagesAsync();
    }

    private void BuildLayout()
    {
        Text = "CRM Subscription Packages Catalog";
        Size = new Size(820, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(28, 20, 28, 20);

        var lblTitle = new Label
        {
            Text = "CRM Subscription Packages",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Available subscription tiers, pricing schedules, and system feature entitlements.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(28, 44),
            AutoSize = true
        };
        Controls.AddRange(new Control[] { lblTitle, lblSub });

        pnlCards = new FlowLayoutPanel
        {
            Location = new Point(28, 80),
            Size = new Size(748, 280),
            BackColor = Color.Transparent,
            WrapContents = false,
            AutoScroll = false
        };
        Controls.Add(pnlCards);

        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };
        btnClose = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(100, 36),
            Location = new Point(pnlActions.Width - 100, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        pnlActions.Controls.Add(btnClose);
        Controls.Add(pnlActions);

        AcceptButton = btnClose;
    }

    private async Task LoadPackagesAsync()
    {
        try
        {
            await using var db = _masterDbFactory();
            var packages = await db.SubscriptionPackages
                .Include(p => p.Companies)
                .AsNoTracking()
                .OrderByDescending(p => p.MonthlyFee)
                .ToListAsync();

            pnlCards.SuspendLayout();
            pnlCards.Controls.Clear();

            foreach (var pkg in packages)
            {
                var card = new Panel
                {
                    Size = new Size(236, 260),
                    BackColor = ColorCardBg,
                    Margin = new Padding(0, 0, 16, 0)
                };

                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(ColorBorder, 1.2f);
                    using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                    e.Graphics.DrawPath(pen, path);
                };

                var lblTier = new Label
                {
                    Text = pkg.PackageName.ToUpperInvariant(),
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    ForeColor = ColorAccent,
                    Location = new Point(14, 14),
                    Size = new Size(208, 22)
                };

                var lblFee = new Label
                {
                    Text = $"₱{pkg.MonthlyFee:N0}",
                    Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                    ForeColor = ColorPrimary,
                    Location = new Point(14, 38),
                    AutoSize = true
                };

                var lblFeeSub = new Label
                {
                    Text = "per month",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = ColorSubtext,
                    Location = new Point(16, 68),
                    AutoSize = true
                };

                var lblBranches = new Label
                {
                    Text = pkg.HasMultiBranch ? $"🏢 Up to {pkg.MaxBranches} Showrooms" : "🏢 Single Showroom",
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    ForeColor = ColorBrandDark,
                    Location = new Point(14, 94),
                    AutoSize = true
                };

                var lblLoyalty = new Label
                {
                    Text = pkg.HasLoyalty ? "✓ Client Loyalty Awards" : "✗ No Loyalty Automation",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = pkg.HasLoyalty ? Color.FromArgb(5, 150, 105) : ColorSubtext,
                    Location = new Point(14, 116),
                    AutoSize = true
                };

                var lblAnalytics = new Label
                {
                    Text = pkg.HasAnalytics ? "✓ BI Analytics Dashboards" : "✗ Basic Reporting Only",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = pkg.HasAnalytics ? Color.FromArgb(5, 150, 105) : ColorSubtext,
                    Location = new Point(14, 138),
                    AutoSize = true
                };

                var lblSubsCount = new Label
                {
                    Text = $"{pkg.Companies.Count(c => c.IsActive)} active boutique subscriber(s)",
                    Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                    ForeColor = ColorAccent,
                    Location = new Point(14, 226),
                    AutoSize = true
                };

                card.Controls.AddRange(new Control[] { lblTier, lblFee, lblFeeSub, lblBranches, lblLoyalty, lblAnalytics, lblSubsCount });
                pnlCards.Controls.Add(card);
            }

            pnlCards.ResumeLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load package catalog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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