using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class TenantsManagementView : UserControl
{
    private readonly Func<MasterCrmDbContext> _masterContextFactory;
    private readonly TenantProvisioningService _provisioningService;

    // Header & Actions
    private Label lblTitle = null!;
    private Label lblSub = null!;
    private Button btnAddTenant = null!;
    private Button btnViewPackages = null!;
    private Button btnRefresh = null!;

    // Toolbar Controls
    private TextBox txtSearch = null!;
    private CheckBox chkShowDeactivated = null!;
    private Label lblStats = null!;

    // Data Card & Grid
    private DataGridView dgvTenants = null!;
    private List<Company> _cachedCompanies = [];

    public TenantsManagementView(
        Func<MasterCrmDbContext> masterContextFactory,
        TenantProvisioningService provisioningService)
    {
        _masterContextFactory = masterContextFactory ?? throw new ArgumentNullException(nameof(masterContextFactory));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));

        InitializeLayout();
        Load += async (s, e) => await LoadTenantsAsync();
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
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.Transparent
        };

        lblTitle = new Label
        {
            Text = "Boutique Tenants & Subscriptions",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblSub = new Label
        {
            Text = "Manage multi-tenant databases, monthly billing renewals, and subscription tier licenses.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnAddTenant = new Button
        {
            Text = "+ Provision Tenant",
            Size = new Size(155, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnAddTenant.FlatAppearance.BorderSize = 0;
        btnAddTenant.MouseEnter += (s, e) => btnAddTenant.BackColor = ColorAccentHover;
        btnAddTenant.MouseLeave += (s, e) => btnAddTenant.BackColor = ColorAccent;
        btnAddTenant.Click += async (s, e) => await OpenCreateTenantDialogAsync();

        btnViewPackages = new Button
        {
            Text = "📦 Packages",
            Size = new Size(110, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorPrimary,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnViewPackages.FlatAppearance.BorderSize = 1;
        btnViewPackages.FlatAppearance.BorderColor = ColorBorder;
        btnViewPackages.Click += (s, e) =>
        {
            using var pkgDlg = new SubscriptionPackagesCatalogDialog(_masterContextFactory);
            pkgDlg.ShowDialog(FindForm());
        };

        btnRefresh = new Button
        {
            Text = "↻ Refresh",
            Size = new Size(95, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 1;
        btnRefresh.FlatAppearance.BorderColor = ColorBorder;
        btnRefresh.Click += async (s, e) => await LoadTenantsAsync();

        pnlHeader.Resize += (s, e) => RepositionHeaderButtons(pnlHeader);
        RepositionHeaderButtons(pnlHeader);

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnAddTenant, btnViewPackages, btnRefresh });

        // 2. Zone 2: Toolbar, Search Box & Deactivated Filter
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };

        var pnlSearch = new Panel
        {
            Location = new Point(0, 4),
            Size = new Size(300, 34),
            BackColor = ColorCardBg
        };
        pnlSearch.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1), 4);
            e.Graphics.DrawPath(pen, path);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Width = 280,
            Location = new Point(10, 8),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            PlaceholderText = "Search by boutique, code, or plan..."
        };
        txtSearch.TextChanged += (s, e) => PopulateGrid();
        pnlSearch.Controls.Add(txtSearch);

        chkShowDeactivated = new CheckBox
        {
            Text = "Show Deactivated Tenants",
            Checked = false, // Deactivated hidden by default
            Location = new Point(314, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Cursor = Cursors.Hand
        };
        chkShowDeactivated.CheckedChanged += (s, e) => PopulateGrid();

        lblStats = new Label
        {
            Dock = DockStyle.Right,
            Text = "Loading tenants...",
            ForeColor = ColorSubtext,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 8, 0, 0)
        };

        pnlToolbar.Controls.AddRange(new Control[] { pnlSearch, chkShowDeactivated, lblStats });

        // 3. Zone 3: Main Data Card Container
        var pnlGridWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 4),
            BackColor = Color.Transparent
        };

        var pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(1)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        dgvTenants = new DataGridView
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowTemplate = { Height = 46 },
            ColumnHeadersHeight = 42,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false
        };

        dgvTenants.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        dgvTenants.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(8, 0, 8, 0)
        };

        ConfigureColumns();
        dgvTenants.CellPainting += DgvTenants_CellPainting;

        dgvTenants.CellContentClick += async (s, e) =>
        {
            if (e.RowIndex < 0) return;
            var cellVal = dgvTenants.Rows[e.RowIndex].Cells["CompanyId"].Value;
            if (cellVal is not int companyId) return;

            var boutiqueName = dgvTenants.Rows[e.RowIndex].Cells["CompanyName"].Value?.ToString() ?? "Tenant";
            var colName = dgvTenants.Columns[e.ColumnIndex].Name;

            if (colName == "ColAction")
            {
                await ToggleTenantStatusAsync(companyId);
            }
            else if (colName == "ColRenew")
            {
                await RecordSubscriptionRenewalAsync(companyId, boutiqueName);
            }
            else if (colName == "ColEditPlan")
            {
                using var editDlg = new EditTenantSubscriptionDialogForm(_masterContextFactory, companyId);
                if (editDlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    await LoadTenantsAsync();
                }
            }
        };

        pnlCard.Controls.Add(dgvTenants);
        pnlGridWrapper.Controls.Add(pnlCard);

        Controls.Add(pnlGridWrapper);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
    }

    private void RepositionHeaderButtons(Panel header)
    {
        int right = header.ClientSize.Width;
        btnAddTenant.Location = new Point(Math.Max(0, right - btnAddTenant.Width), 12);
        btnViewPackages.Location = new Point(Math.Max(0, right - btnAddTenant.Width - btnViewPackages.Width - 8), 12);
        btnRefresh.Location = new Point(Math.Max(0, right - btnAddTenant.Width - btnViewPackages.Width - btnRefresh.Width - 16), 12);
    }

    private void ConfigureColumns()
    {
        dgvTenants.Columns.Clear();
        var boldFont = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold);
        var boldStyle = new DataGridViewCellStyle { Font = boldFont };

        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", HeaderText = "ID", Visible = false });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyCode", HeaderText = "CODE", Width = 90, DefaultCellStyle = boldStyle });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "BOUTIQUE TENANT", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = boldStyle });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Package", HeaderText = "SUBSCRIPTION", Width = 150 });

        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MonthlyFee",
            HeaderText = "FEE / MO",
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                ForeColor = ColorAccent,
                Font = boldFont
            }
        });

        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubscribedDate", HeaderText = "SUBSCRIBED", Width = 100 });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "NextDueDate", HeaderText = "NEXT DUE", Width = 100 });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "BillingStatus", HeaderText = "BILLING STATUS", Width = 120 });
        dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountState", HeaderText = "STATE", Width = 95 });

        // Action Column: Renew
        dgvTenants.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "ColRenew",
            HeaderText = "RENEW",
            Text = "💳 Renew",
            UseColumnTextForButtonValue = true,
            Width = 85,
            FlatStyle = FlatStyle.Flat
        });

        // Action Column: Edit Plan
        dgvTenants.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "ColEditPlan",
            HeaderText = "PLAN",
            Text = "✏️ Plan",
            UseColumnTextForButtonValue = true,
            Width = 75,
            FlatStyle = FlatStyle.Flat
        });

        // Action Column: Deactivate / Reactivate
        dgvTenants.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "ColAction",
            HeaderText = "ACCESS",
            Width = 95,
            FlatStyle = FlatStyle.Flat
        });

        foreach (DataGridViewColumn c in dgvTenants.Columns)
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
    }

    private void DgvTenants_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;

        // Custom Render Billing Status Pill
        if (dgvTenants.Columns[e.ColumnIndex].Name == "BillingStatus" && e.Value is string billing)
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var (bg, fg) = billing switch
            {
                "Active / Paid" => (Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105)),
                "Due Soon" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
                "Past Due" => (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38)),
                _ => (Color.FromArgb(243, 244, 246), ColorNavInactiveText)
            };

            var rect = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + 11, 105, 24);
            using (var brush = new SolidBrush(bg))
            using (var path = CreateRoundedRectangle(rect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                billing,
                new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
        // Custom Render Account State Pill
        else if (dgvTenants.Columns[e.ColumnIndex].Name == "AccountState" && e.Value is string state)
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isActive = state == "Active";
            var bg = isActive ? Color.FromArgb(236, 253, 245) : Color.FromArgb(243, 244, 246);
            var fg = isActive ? Color.FromArgb(5, 150, 105) : ColorNavInactiveText;

            var rect = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + 11, 80, 24);
            using (var brush = new SolidBrush(bg))
            using (var path = CreateRoundedRectangle(rect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                state.ToUpper(),
                new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    public async Task LoadTenantsAsync()
    {
        try
        {
            await using var db = _masterContextFactory();
            _cachedCompanies = await db.Companies
                .Include(c => c.SubscriptionPackage)
                .AsNoTracking()
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.CompanyName)
                .ToListAsync();

            PopulateGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tenants: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateGrid()
    {
        dgvTenants.Rows.Clear();

        var query = txtSearch.Text.Trim();
        bool showDeactivated = chkShowDeactivated.Checked;
        var today = DateTime.UtcNow.Date;

        decimal totalMrr = 0m;
        int displayedCount = 0;

        foreach (var c in _cachedCompanies)
        {
            if (c.IsActive) totalMrr += (c.SubscriptionPackage?.MonthlyFee ?? 0m);

            // 1. Deactivated filter
            if (!showDeactivated && !c.IsActive) continue;

            // 2. Search query filter
            var pkgName = c.SubscriptionPackage?.PackageName ?? "No Plan";
            if (!string.IsNullOrWhiteSpace(query))
            {
                bool matches = c.CompanyCode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               c.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               pkgName.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;
            }

            // Determine dynamic monthly billing status
            string billingStatus = "Suspended";
            if (c.IsActive)
            {
                if (c.NextBillingDueDate.Date < today)
                {
                    billingStatus = "Past Due";
                }
                else if (c.NextBillingDueDate.Date <= today.AddDays(5))
                {
                    billingStatus = "Due Soon";
                }
                else
                {
                    billingStatus = "Active / Paid";
                }
            }

            var rowIndex = dgvTenants.Rows.Add(
                c.CompanyId,
                c.CompanyCode,
                c.CompanyName,
                pkgName,
                $"₱{(c.SubscriptionPackage?.MonthlyFee ?? 0m):N0}",
                c.SubscriptionStartDate.ToString("MMM dd, yyyy"),
                c.NextBillingDueDate.ToString("MMM dd, yyyy"),
                billingStatus,
                c.IsActive ? "Active" : "Deactivated",
                "💳 Renew",
                "✏️ Plan",
                c.IsActive ? "Deactivate" : "Reactivate"
            );

            if (!c.IsActive)
            {
                dgvTenants.Rows[rowIndex].DefaultCellStyle.ForeColor = ColorSubtext;
            }

            displayedCount++;
        }

        dgvTenants.ClearSelection();
        lblStats.Text = $"{displayedCount} boutique tenant(s) displayed • Platform MRR: ₱{totalMrr:N0}";
    }

    private async Task RecordSubscriptionRenewalAsync(int companyId, string boutiqueName)
    {
        var confirm = MessageBox.Show(
            $"Record monthly renewal payment for '{boutiqueName}'?\n\n" +
            "This will roll the next billing due date forward by +1 month and log payment received today.",
            "Record Monthly Renewal",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = _masterContextFactory();
            var tenant = await db.Companies.FindAsync(companyId);
            if (tenant == null) return;

            var baseDate = tenant.NextBillingDueDate > DateTime.UtcNow ? tenant.NextBillingDueDate : DateTime.UtcNow;
            tenant.NextBillingDueDate = baseDate.AddMonths(1);
            tenant.LastPaymentDate = DateTime.UtcNow;
            tenant.IsActive = true;
            tenant.DeactivatedAt = null;

            var dbs = await db.CompanyDatabases.Where(d => d.CompanyId == companyId).ToListAsync();
            foreach (var d in dbs) d.IsActive = true;

            await db.SaveChangesAsync();
            await LoadTenantsAsync();

            MessageBox.Show(
                $"Subscription renewed for {boutiqueName}!\nNext payment due on: {tenant.NextBillingDueDate:MMMM dd, yyyy}",
                "Payment Recorded",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to record renewal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleTenantStatusAsync(int companyId)
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to change this tenant's activation status?\n\n" +
            "Deactivated tenants cannot be accessed by any of that boutique's staff or administrators.",
            "Confirm State Change",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = _masterContextFactory();
            var tenant = await db.Companies.FindAsync(companyId);
            if (tenant == null) return;

            tenant.IsActive = !tenant.IsActive;
            tenant.DeactivatedAt = tenant.IsActive ? null : DateTime.UtcNow;

            var dbs = await db.CompanyDatabases.Where(d => d.CompanyId == companyId).ToListAsync();
            foreach (var d in dbs) d.IsActive = tenant.IsActive;

            await db.SaveChangesAsync();
            await LoadTenantsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenCreateTenantDialogAsync()
    {
        using var dlg = new TenantDialogForm(_masterContextFactory, _provisioningService);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadTenantsAsync();
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