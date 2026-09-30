using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Forms;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class LoyaltyAwardsView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly bool _isAdmin;
    private readonly LoyaltyAwardService _loyaltyService;

    // Header & Actions
    private Label lblHeader = null!;
    private Label lblSub = null!;
    private Button btnRefresh = null!;
    private Button btnAddTier = null!;

    // Containers & Toolbars
    private FlowLayoutPanel pnlTierCards = null!;
    private TextBox txtSearch = null!;
    private CheckBox chkShowArchived = null!;
    private Label lblRecordsCount = null!;
    private DataGridView dgvClients = null!;

    public LoyaltyAwardsView(
        Func<TenantCrmDbContext> contextFactory,
        Func<int> getCompanyId,
        Func<int?>? getBranchId = null,
        string[]? userRoles = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
        _loyaltyService = new LoyaltyAwardService(_contextFactory);

        _isAdmin = userRoles != null && userRoles.Any(r =>
            r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

        InitializeLayout();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Zone 1: Header & Primary Actions
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };

        lblHeader = new Label
        {
            Text = "Client Loyalty & Tier Rewards",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblSub = new Label
        {
            Text = "Configure spend thresholds, leasing milestones, and automated boutique tier discounts.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnAddTier = new Button
        {
            Text = "+ New Tier",
            Size = new Size(115, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _isAdmin // Gated strictly to Admin and Superadmin
        };
        btnAddTier.FlatAppearance.BorderSize = 0;
        btnAddTier.MouseEnter += (s, e) => btnAddTier.BackColor = ColorAccentHover;
        btnAddTier.MouseLeave += (s, e) => btnAddTier.BackColor = ColorAccent;
        btnAddTier.Click += async (s, e) => await OpenCreateTierDialogAsync();

        btnRefresh = new Button
        {
            Text = "↻ Refresh",
            Size = new Size(95, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 1;
        btnRefresh.FlatAppearance.BorderColor = ColorBorder;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        pnlHeader.Resize += (s, e) => RepositionHeaderButtons(pnlHeader);
        RepositionHeaderButtons(pnlHeader);

        pnlHeader.Controls.AddRange(new Control[] { lblHeader, lblSub, btnAddTier, btnRefresh });

        // 2. Zone 2: Tier Summary Cards Strip
        var pnlTierWrapper = new Panel { Dock = DockStyle.Top, Height = 145, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
        pnlTierCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        pnlTierWrapper.Controls.Add(pnlTierCards);

        // 3. Zone 3: Toolbar & Search Strip
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 8) };

        var pnlSearch = new Panel { Location = new Point(0, 4), Size = new Size(320, 34), BackColor = ColorCardBg };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Width = 300,
            Location = new Point(10, 8),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            PlaceholderText = "Search by client name, code, or tier..."
        };
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();
        pnlSearch.Controls.Add(txtSearch);

        chkShowArchived = new CheckBox
        {
            Text = "Show Inactive Tiers",
            Location = new Point(334, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Cursor = Cursors.Hand,
            Visible = _isAdmin
        };
        chkShowArchived.CheckedChanged += async (s, e) => await LoadDataAsync();

        lblRecordsCount = new Label
        {
            Dock = DockStyle.Right,
            Text = "0 clients enrolled",
            ForeColor = ColorSubtext,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 8, 0, 0)
        };

        pnlToolbar.Controls.AddRange(new Control[] { pnlSearch, chkShowArchived, lblRecordsCount });

        // 4. Zone 4: Data Card Canvas for Clients Grid
        var pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 4), BackColor = Color.Transparent };
        var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = ColorCardBg, Padding = new Padding(1) };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        dgvClients = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ColorCardBg,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = ColorDivider,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowTemplate = { Height = 44 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 42,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        };

        dgvClients.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0)
        };

        dgvClients.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        ConfigureGridColumns();

        pnlCard.Controls.Add(dgvClients);
        pnlGridWrapper.Controls.Add(pnlCard);

        Controls.Add(pnlGridWrapper);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlTierWrapper);
        Controls.Add(pnlHeader);
    }

    private void RepositionHeaderButtons(Panel header)
    {
        if (_isAdmin)
        {
            btnAddTier.Location = new Point(Math.Max(0, header.ClientSize.Width - btnAddTier.Width), 12);
            btnRefresh.Location = new Point(Math.Max(0, header.ClientSize.Width - btnAddTier.Width - btnRefresh.Width - 8), 12);
        }
        else
        {
            btnRefresh.Location = new Point(Math.Max(0, header.ClientSize.Width - btnRefresh.Width), 12);
        }
    }

    private void ConfigureGridColumns()
    {
        dgvClients.Columns.Clear();
        var boldStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold) };

        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerCode", HeaderText = "CODE", Width = 100, DefaultCellStyle = boldStyle });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "CLIENT NAME", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = boldStyle });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentTier", HeaderText = "CURRENT TIER", Width = 130 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Discount", HeaderText = "PERK DISCOUNT", Width = 130 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompletedRentals", HeaderText = "TOTAL LEASES", Width = 120 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LifetimeSpend", HeaderText = "LIFETIME SPEND", Width = 150 });
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Progression", HeaderText = "NEXT TIER STATUS", Width = 260 });

        foreach (DataGridViewColumn col in dgvClients.Columns)
            col.SortMode = DataGridViewColumnSortMode.NotSortable;

        dgvClients.CellPainting += DgvClients_CellPainting;
    }

    private void DgvClients_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;

        if (dgvClients.Columns[e.ColumnIndex].Name == "CurrentTier" && e.Value is string tier)
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color badgeBg = tier switch
            {
                "VIP" or "Platinum" or "Diamond" => Color.FromArgb(255, 243, 205),
                "Gold" or "Silver" => ColorActivePill,
                _ => Color.FromArgb(243, 244, 246)
            };

            Color badgeFg = tier switch
            {
                "VIP" or "Platinum" or "Diamond" => Color.FromArgb(146, 110, 15),
                "Gold" or "Silver" => ColorAccent,
                _ => ColorNavInactiveText
            };

            var rect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + 10, 82, 24);
            using (var brush = new SolidBrush(badgeBg))
            using (var path = CreateRoundedRectangle(rect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                tier.ToUpper(),
                new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                rect,
                badgeFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    public async Task LoadDataAsync()
    {
        try
        {
            int? branchId = _getBranchId?.Invoke();
            bool showInactive = chkShowArchived.Checked;

            var overview = await _loyaltyService.GetLoyaltyOverviewAsync(
                _getCompanyId(),
                branchId,
                txtSearch.Text.Trim(),
                showInactive);

            var rawTiers = await _loyaltyService.GetAllTiersAsync(showInactive);

            RenderTierSummaryCards(overview.Tiers, rawTiers);
            PopulateClientGrid(overview.Customers);

            lblRecordsCount.Text = $"{overview.Customers.Count} clients enrolled";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load loyalty metrics: {ex.GetBaseException().Message}", "Loyalty Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RenderTierSummaryCards(List<LoyaltyTierDto> dtos, List<LoyaltyAward> rawTiers)
    {
        pnlTierCards.SuspendLayout();
        pnlTierCards.Controls.Clear();

        foreach (var tier in dtos)
        {
            var raw = rawTiers.FirstOrDefault(t => t.LoyaltyAwardId == tier.LoyaltyAwardId);
            bool isActive = raw?.IsActive ?? true;

            var card = new Panel
            {
                Size = new Size(260, 126),
                BackColor = isActive ? ColorCardBg : Color.FromArgb(248, 245, 245),
                Margin = new Padding(0, 0, 16, 0)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(isActive ? ColorBorder : Color.LightGray, 1f);
                using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };

            Color tierTagColor = tier.TierName.Equals("VIP", StringComparison.OrdinalIgnoreCase)
                ? ColorGold
                : (tier.TierName.Equals("Gold", StringComparison.OrdinalIgnoreCase) ? ColorAccent : ColorNavInactiveText);

            var lblTier = new Label
            {
                Text = isActive ? tier.TierName.ToUpper() : $"{tier.TierName.ToUpper()} (ARCHIVED)",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = isActive ? tierTagColor : ColorSubtext,
                Location = new Point(14, 12),
                AutoSize = true
            };

            var lblDiscount = new Label
            {
                Text = $"{tier.DiscountPercentage:0.#}% Discount",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = isActive ? ColorPrimary : ColorSubtext,
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
                Text = $"{tier.EnrolledMembersCount} members enrolled",
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = ColorAccent,
                Location = new Point(14, 84),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblTier, lblDiscount, lblCrit, lblCount });

            // Admin Action Buttons: Edit and Archive / Reactivate
            if (_isAdmin && raw != null)
            {
                var btnEdit = new Button
                {
                    Text = "✏️",
                    Size = new Size(28, 26),
                    Location = new Point(card.Width - 66, 8),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorCardBg,
                    ForeColor = ColorPrimary,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand
                };
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Click += async (s, e) => await OpenEditTierDialogAsync(raw);

                var btnToggle = new Button
                {
                    Text = isActive ? "🗑" : "↻",
                    Size = new Size(28, 26),
                    Location = new Point(card.Width - 36, 8),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorCardBg,
                    ForeColor = isActive ? Color.Firebrick : ColorSuccess,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand
                };
                btnToggle.FlatAppearance.BorderSize = 0;
                btnToggle.Click += async (s, e) => await ConfirmToggleTierStatusAsync(raw);

                card.Controls.AddRange(new Control[] { btnEdit, btnToggle });
            }

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

        dgvClients.ClearSelection();
    }

    private async Task OpenCreateTierDialogAsync()
    {
        using var dlg = new LoyaltyTierDialogForm();
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            try
            {
                await _loyaltyService.CreateTierAsync(dlg.TierModel);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot Create Tier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private async Task OpenEditTierDialogAsync(LoyaltyAward tier)
    {
        using var dlg = new LoyaltyTierDialogForm(tier);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            try
            {
                await _loyaltyService.UpdateTierAsync(dlg.TierModel);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot Update Tier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private async Task ConfirmToggleTierStatusAsync(LoyaltyAward tier)
    {
        string action = tier.IsActive ? "archive" : "reactivate";
        var res = MessageBox.Show(
            $"Are you sure you want to {action} the '{tier.TierName}' tier?\n\n" +
            (tier.IsActive
                ? "Archived tiers will no longer be applied to future customer bookings."
                : "Reactivated tiers will become available for customer qualification immediately."),
            $"Confirm Tier {char.ToUpper(action[0])}{action[1..]}",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            try
            {
                await _loyaltyService.ToggleTierStatusAsync(tier.LoyaltyAwardId);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Action Prohibited", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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