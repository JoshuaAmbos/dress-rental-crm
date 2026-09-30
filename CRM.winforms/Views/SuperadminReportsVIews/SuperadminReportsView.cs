using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class SuperadminReportsView : UserControl
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly Func<TenantCrmDbContext> _tenantDbFactory;
    private readonly string _currentUsername;

    // Header & Actions
    private Label lblTitle = null!;
    private Label lblSub = null!;
    private Button btnExportReport = null!;
    private Button btnRefresh = null!;

    // 4 Platform KPIs
    private KpiCardControl kpiTenants = null!;
    private KpiCardControl kpiMrr = null!;
    private KpiCardControl kpiDatabases = null!;
    private KpiCardControl kpiUsers = null!;

    // Charts
    private AtelierBarChart chartRevenueByTenant = null!;
    private AtelierDonutChart chartRoleDistribution = null!;

    // Infrastructure Ledger
    private DataGridView dgvTenantLedger = null!;

    public SuperadminReportsView(
        Func<MasterCrmDbContext> masterDbFactory,
        Func<TenantCrmDbContext> tenantDbFactory,
        string currentUsername = "superadmin")
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _tenantDbFactory = tenantDbFactory ?? throw new ArgumentNullException(nameof(tenantDbFactory));
        _currentUsername = currentUsername;

        InitializeLayout();
        Load += async (s, e) => await LoadPlatformMetricsAsync();
    }

    private void InitializeLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 28);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = BuildHeaderSection();
        var pnlKpiRow = BuildKpiRow();
        var pnlChartsRow = BuildChartsRow();
        var pnlLedgerSection = BuildTenantLedgerSection();

        // Top-down docking hierarchy added in reverse order
        Controls.Add(pnlLedgerSection);
        Controls.Add(pnlChartsRow);
        Controls.Add(pnlKpiRow);
        Controls.Add(pnlHeader);
    }

    private Panel BuildHeaderSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };

        lblTitle = new Label
        {
            Text = "Platform Intelligence & Compliance",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblSub = new Label
        {
            Text = "Cross-tenant infrastructure telemetry, subscription MRR, and global identity distribution.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnExportReport = new Button
        {
            Text = "📑 Export Audit Report",
            Size = new Size(175, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExportReport.FlatAppearance.BorderSize = 0;
        btnExportReport.MouseEnter += (s, e) => btnExportReport.BackColor = ColorAccentHover;
        btnExportReport.MouseLeave += (s, e) => btnExportReport.BackColor = ColorAccent;
        btnExportReport.Click += BtnExportReport_Click;

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
        btnRefresh.Click += async (s, e) => await LoadPlatformMetricsAsync();

        pnl.Resize += (s, e) =>
        {
            btnExportReport.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportReport.Width), 12);
            btnRefresh.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportReport.Width - btnRefresh.Width - 8), 12);
        };
        btnExportReport.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportReport.Width), 12);
        btnRefresh.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportReport.Width - btnRefresh.Width - 8), 12);

        pnl.Controls.AddRange(new Control[] { lblTitle, lblSub, btnExportReport, btnRefresh });
        return pnl;
    }

    private Panel BuildKpiRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 145, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiTenants = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        kpiMrr = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiDatabases = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiUsers = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };

        table.Controls.Add(kpiTenants, 0, 0);
        table.Controls.Add(kpiMrr, 1, 0);
        table.Controls.Add(kpiDatabases, 2, 0);
        table.Controls.Add(kpiUsers, 3, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildChartsRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 300, Padding = new Padding(0, 6, 0, 14), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

        // Card 1: Subscription MRR by Boutique (Bar Chart)
        var cardLeft = CreateCardContainer("MONTHLY RECURRING REVENUE BY BOUTIQUE TENANT", out var bodyLeft);
        cardLeft.Margin = new Padding(0, 0, 10, 0);
        chartRevenueByTenant = new AtelierBarChart { Dock = DockStyle.Fill };
        bodyLeft.Controls.Add(chartRevenueByTenant);

        // Card 2: Global Security Role Distribution (Donut Chart)
        var cardRight = CreateCardContainer("GLOBAL IDENTITY & RBAC ROLE ALLOCATION", out var bodyRight);
        cardRight.Margin = new Padding(5, 0, 0, 0);
        chartRoleDistribution = new AtelierDonutChart { Dock = DockStyle.Fill };
        bodyRight.Controls.Add(chartRoleDistribution);

        table.Controls.Add(cardLeft, 0, 0);
        table.Controls.Add(cardRight, 1, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildTenantLedgerSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 320, Padding = new Padding(0, 6, 0, 16), BackColor = Color.Transparent };
        var card = CreateCardContainer("TENANT INFRASTRUCTURE & LICENSING REGISTRY", out var body);

        dgvTenantLedger = new DataGridView
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
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false
        };

        dgvTenantLedger.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        dgvTenantLedger.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        var bold = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold) };

        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CompanyCode", HeaderText = "CODE", Width = 95, DefaultCellStyle = bold });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CompanyName", HeaderText = "BOUTIQUE TENANT", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = bold });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PackageName", HeaderText = "SUBSCRIPTION PLAN", Width = 170 });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MonthlyFee", HeaderText = "FEE / MO", Width = 120, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorAccent } });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DatabaseName", HeaderText = "ISOLATED DATABASE", Width = 180 });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServerHost", HeaderText = "SERVER HOST", Width = 140 });
        dgvTenantLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "STATUS", Width = 100, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold) } });

        body.Controls.Add(dgvTenantLedger);
        pnl.Controls.Add(card);
        return pnl;
    }

    private static Panel CreateCardContainer(string title, out Panel bodyPanel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(18, 14, 18, 14)
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        };

        var lbl = new Label
        {
            Text = title,
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Dock = DockStyle.Top,
            Height = 26
        };

        bodyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        card.Controls.Add(bodyPanel);
        card.Controls.Add(lbl);
        return card;
    }

    public async Task LoadPlatformMetricsAsync()
    {
        try
        {
            await using var masterDb = _masterDbFactory();

            var companies = await masterDb.Companies
                .AsNoTracking()
                .Include(c => c.SubscriptionPackage)
                .Include(c => c.CompanyDatabases)
                .OrderBy(c => c.CompanyId)
                .ToListAsync();

            var databases = await masterDb.CompanyDatabases
                .AsNoTracking()
                .ToListAsync();

            var rawRoles = await (from ur in masterDb.UserRoles
                                  join r in masterDb.Roles on ur.RoleId equals r.Id into roles
                                  from r in roles.DefaultIfEmpty()
                                  select r != null ? r.Name : "Staff").ToListAsync();

            int totalTenants = companies.Count;
            int activeTenants = companies.Count(c => c.IsActive);
            decimal totalMrr = companies.Where(c => c.IsActive).Sum(c => c.SubscriptionPackage?.MonthlyFee ?? 0);
            int onlineDbs = databases.Count(d => d.IsActive);
            int totalIdentities = rawRoles.Count;

            // 1. Update KPI Cards
            kpiTenants.SetData("ACTIVE BOUTIQUES", $"{activeTenants}/{totalTenants}", "100% Operational", ColorPrimary, Color.FromArgb(254, 242, 243), ColorPrimary);
            kpiMrr.SetData("PLATFORM MRR", $"₱{totalMrr:N0}", "Active Subscriptions", ColorAccent, Color.FromArgb(254, 242, 243), ColorAccent);
            kpiDatabases.SetData("ONLINE DATABASES", $"{onlineDbs} Active", "Physical SQL DBs", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105));
            kpiUsers.SetData("SYSTEM IDENTITIES", totalIdentities.ToString(), "Cross-Tenant RBAC", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));

            // 2. Bar Chart: Revenue Generated per Boutique Tenant
            var revenueSeries = companies.Select(c => new MonthlyRevenueMetric
            {
                MonthLabel = c.CompanyCode,
                Revenue = c.SubscriptionPackage?.MonthlyFee ?? 0,
                BookingCount = 1
            }).ToList();
            chartRevenueByTenant.SetData(revenueSeries);

            // 3. Donut Chart: RBAC Role Distribution Across All Tenants
            var roleGroups = rawRoles
                .GroupBy(r => r ?? "Staff")
                .Select(g => new StageDistributionMetric
                {
                    StageName = g.Key,
                    Count = g.Count(),
                    Percentage = totalIdentities > 0 ? (double)g.Count() / totalIdentities * 100 : 0
                }).ToList();
            chartRoleDistribution.SetData(roleGroups);

            // 4. Populate Data Grid
            dgvTenantLedger.DataSource = companies.Select(c => new
            {
                c.CompanyCode,
                c.CompanyName,
                PackageName = c.SubscriptionPackage?.PackageName ?? "None",
                MonthlyFee = c.SubscriptionPackage?.MonthlyFee ?? 0m,
                DatabaseName = c.CompanyDatabases.FirstOrDefault(d => d.IsActive)?.DatabaseName ?? $"DB_Tenant_{c.CompanyCode}",
                ServerHost = c.CompanyDatabases.FirstOrDefault(d => d.IsActive)?.ServerName ?? "localhost,1433",
                Status = c.IsActive ? "Active" : "Suspended"
            }).ToList();

            dgvTenantLedger.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to aggregate platform metrics: {ex.GetBaseException().Message}", "Telemetry Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnExportReport_Click(object? sender, EventArgs e)
    {
        using var dlg = new SuperadminReportExportDialog(
            _masterDbFactory,
            _tenantDbFactory,
            _currentUsername);

        dlg.ShowDialog(this.FindForm());
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