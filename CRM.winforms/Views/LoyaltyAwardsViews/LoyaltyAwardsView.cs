using CRM.infrastructure.data;
using CRM.winforms.Controls;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class LoyaltyAwardsView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly LoyaltyAwardController _controller;

    // Atelier Color Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorGold = Color.FromArgb(212, 175, 55);

    // UI Controls
    private FlowLayoutPanel pnlTierCards = null!;
    private DataGridView dgvClients = null!;
    private TextBox txtSearch = null!;
    private Button btnRefresh = null!;
    private Label lblHeader = null!;
    private Label lblSub = null!;

    public LoyaltyAwardsView()
    {
        _contextFactory = () =>
        {
            var opt = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(opt);
        };
        _getCompanyId = () => 1;
        _controller = new LoyaltyAwardController(_contextFactory);

        InitializeLayout();
    }

    public LoyaltyAwardsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _controller = new LoyaltyAwardController(_contextFactory);

        InitializeLayout();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9f, FontStyle.Regular);

        // Header Labels
        lblHeader = new Label
        {
            Text = "Client Loyalty & Tier Rewards",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(32, 20),
            AutoSize = true
        };

        lblSub = new Label
        {
            Text = "Track customer lifetime value, automated leasing discounts, and VIP membership thresholds.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(32, 52),
            AutoSize = true
        };

        // Tier Summary Cards Container
        pnlTierCards = new FlowLayoutPanel
        {
            Location = new Point(32, 88),
            Size = new Size(Width - 64, 115),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        // Search Bar & Actions Container
        var pnlActions = new Panel
        {
            Location = new Point(32, 215),
            Size = new Size(Width - 64, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        txtSearch = new TextBox
        {
            Location = new Point(0, 4),
            Size = new Size(320, 30),
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorEspresso,
            PlaceholderText = "Search by client name, code, or tier..."
        };
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();

        btnRefresh = new Button
        {
            Text = "↻ Refresh Tiers",
            Location = new Point(332, 3),
            Size = new Size(120, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorEspresso,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderColor = ColorBorder;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        pnlActions.Controls.AddRange(new Control[] { txtSearch, btnRefresh });

        // Client Progression DataGridView
        dgvClients = new DataGridView
        {
            Location = new Point(32, 265),
            Size = new Size(Width - 64, Height - 295),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = ColorCardBg,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = ColorBorder,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowTemplate = { Height = 44 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(244, 237, 237),
                ForeColor = ColorEspresso,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            },

            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = ColorEspresso,
                SelectionBackColor = Color.FromArgb(254, 242, 243),
                SelectionForeColor = ColorEspresso,
                Font = new Font("Segoe UI", 9.25f),
                Padding = new Padding(8, 0, 0, 0)
            }
        };

        ConfigureGridColumns();

        Controls.Add(lblHeader);
        Controls.Add(lblSub);
        Controls.Add(pnlTierCards);
        Controls.Add(pnlActions);
        Controls.Add(dgvClients);

        Load += async (s, e) => await LoadDataAsync();
    }

    private void ConfigureGridColumns()
    {
        dgvClients.Columns.Clear();

        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerCode", HeaderText = "CODE", Width = 110 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "CLIENT NAME", Width = 220, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentTier", HeaderText = "CURRENT TIER", Width = 140 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Discount", HeaderText = "PERK DISCOUNT", Width = 130 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompletedRentals", HeaderText = "TOTAL LEASES", Width = 120 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LifetimeSpend", HeaderText = "LIFETIME SPEND", Width = 150 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Progression", HeaderText = "NEXT TIER STATUS", Width = 230 });

        dgvClients.CellPainting += DgvClients_CellPainting;
    }

    private void DgvClients_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;

        // Custom Tier Badge Rendering
        if (dgvClients.Columns[e.ColumnIndex].Name == "CurrentTier" && e.Value is string tier)
        {
            e.PaintBackground(e.ClipBounds, true);

            Color badgeBg = tier switch
            {
                "VIP" => Color.FromArgb(255, 243, 205),
                "Gold" => Color.FromArgb(254, 242, 243),
                _ => Color.FromArgb(240, 240, 240)
            };

            Color badgeFg = tier switch
            {
                "VIP" => Color.FromArgb(133, 100, 4),
                "Gold" => ColorDustyRose,
                _ => ColorSubtext
            };

            var rect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + 8, 85, 26);
            using (var brush = new SolidBrush(badgeBg))
            using (var path = GraphicsHelper.CreateRoundedRectangle(rect, 4))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(e.Graphics, tier.ToUpper(), new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), rect, badgeFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var res = await _controller.GetOverview(_getCompanyId(), txtSearch.Text);
            if (res.Result is not OkObjectResult ok || ok.Value is not LoyaltyOverviewDto overview)
                return;

            RenderTierSummaryCards(overview);
            PopulateClientGrid(overview.Customers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load loyalty metrics: {ex.GetBaseException().Message}", "Loyalty Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RenderTierSummaryCards(LoyaltyOverviewDto overview)
    {
        pnlTierCards.SuspendLayout();
        pnlTierCards.Controls.Clear();

        foreach (var tier in overview.Tiers)
        {
            var card = new Panel
            {
                Size = new Size(240, 105),
                BackColor = ColorCardBg,
                Margin = new Padding(0, 0, 16, 0)
            };

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(ColorBorder, 1f);
                using var path = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            };

            var lblTier = new Label
            {
                Text = tier.TierName.ToUpper(),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = tier.TierName == "VIP" ? ColorGold : (tier.TierName == "Gold" ? ColorDustyRose : ColorEspresso),
                Location = new Point(14, 12),
                AutoSize = true
            };

            var lblDiscount = new Label
            {
                Text = $"{tier.DiscountPercentage:0.#}% Discount",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = ColorEspresso,
                Location = new Point(14, 32),
                AutoSize = true
            };

            var lblCrit = new Label
            {
                Text = $"₱{tier.MinLifetimeSpend:N0}+ spend · {tier.MinRentalCount}+ leases",
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = ColorSubtext,
                Location = new Point(14, 62),
                AutoSize = true
            };

            var lblCount = new Label
            {
                Text = $"{tier.EnrolledMembersCount} members",
                Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
                ForeColor = ColorDustyRose,
                Location = new Point(14, 80),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblTier, lblDiscount, lblCrit, lblCount });
            pnlTierCards.Controls.Add(card);
        }

        pnlTierCards.ResumeLayout();
    }

    private void PopulateClientGrid(List<CustomerLoyaltyDto> clients)
    {
        dgvClients.Rows.Clear();

        foreach (var c in clients)
        {
            string status = c.NextTier != null
                ? $"{c.ProgressPercentage}% (needs ₱{c.NextTierSpendRemaining:N0} for {c.NextTier})"
                : "Highest Tier Achieved ★";

            dgvClients.Rows.Add(
                c.CustomerCode,
                c.FullName,
                c.CurrentTier,
                $"{c.DiscountPercentage:0.#}% Off",
                $"{c.CompletedRentalsCount} leases",
                $"₱{c.LifetimeSpend:N2}",
                status
            );
        }
    }
}