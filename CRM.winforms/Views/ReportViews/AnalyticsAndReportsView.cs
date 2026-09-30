using CRM.infrastructure.data;
using CRM.winforms.Controls;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class AnalyticsAndReportsView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly AnalyticsReportController _controller;
    private readonly CsvExportService _csvExportService;

    // Filter Controls
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private Button btnExportCsv = null!;
    private Button btnApplyFilter = null!;
    private FlowLayoutPanel pnlPresets = null!;
    private string _activePreset = "All Time";

    // KPI Cards
    private KpiCardControl kpiRevenue = null!;
    private KpiCardControl kpiDeposits = null!;
    private KpiCardControl kpiUtilization = null!;
    private KpiCardControl kpiReturnRate = null!;

    // Visual Charts
    private AtelierBarChart chartRevenue = null!;
    private AtelierDonutChart chartStages = null!;

    // Leaderboards & Audit Grids
    private DataGridView dgvTopGarments = null!;
    private DataGridView dgvTopCustomers = null!;
    private DataGridView dgvAuditLedger = null!;

    // Cached state
    private List<RentalLedgerRowDto> _currentLedger = [];

    // Parameterless constructor for WinForms Designer
    public AnalyticsAndReportsView() : this(() =>
    {
        var opt = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(opt);
    }, () => 1, null)
    {
    }

    public AnalyticsAndReportsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
        : this(contextFactory, getCompanyId, null)
    {
    }

    // Primary constructor invoked by MainForm (supports multi-showroom branch filtering)
    public AnalyticsAndReportsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
        _controller = new AnalyticsReportController(_contextFactory);
        _csvExportService = new CsvExportService();

        InitializeLayout();
        _ = LoadDashboardDataAsync();
    }

    private void InitializeLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 32);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header & Primary Export
        var pnlHeader = BuildHeaderSection();

        // 2. Interactive Filter Slicer Bar
        var pnlFilterBar = BuildFilterBar();

        // 3. 4-Column KPI Metric Row
        var pnlKpiRow = BuildKpiRow();

        // 4. Trend & Breakdown Visual Charts
        var pnlChartsRow = BuildChartsRow();

        // 5. Dual Leaderboards Row (Top Garments + VIP Customers)
        var pnlLeaderboardRow = BuildLeaderboardsSection();

        // 6. Detailed Audit Ledger Section
        var pnlLedgerSection = BuildAuditLedgerSection();

        // Adding top-down docking hierarchy in reverse order
        Controls.Add(pnlLedgerSection);
        Controls.Add(pnlLeaderboardRow);
        Controls.Add(pnlChartsRow);
        Controls.Add(pnlKpiRow);
        Controls.Add(pnlFilterBar);
        Controls.Add(pnlHeader);
    }

    private Panel BuildHeaderSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };

        var lblTitle = new Label
        {
            Text = "Business Intelligence & Reports",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Executive lease revenue metrics, fleet utilization, and customer performance.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnExportCsv = new Button
        {
            Text = "📥 Export to CSV",
            Size = new Size(140, 38),
            BackColor = ColorCardBg,
            ForeColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExportCsv.FlatAppearance.BorderColor = ColorBorder;
        btnExportCsv.MouseEnter += (s, e) =>
        {
            btnExportCsv.BackColor = ColorActivePill;
            btnExportCsv.FlatAppearance.BorderColor = ColorAccent;
        };
        btnExportCsv.MouseLeave += (s, e) =>
        {
            btnExportCsv.BackColor = ColorCardBg;
            btnExportCsv.FlatAppearance.BorderColor = ColorBorder;
        };
        btnExportCsv.Click += BtnExportCsv_Click;

        pnl.Resize += (s, e) =>
        {
            btnExportCsv.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportCsv.Width), 12);
        };
        btnExportCsv.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportCsv.Width), 12);

        pnl.Controls.AddRange(new Control[] { lblTitle, lblSub, btnExportCsv });
        return pnl;
    }

    private Panel BuildFilterBar()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };

        pnlPresets = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 400,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        string[] presets = { "This Month", "Last 30 Days", "Year to Date", "All Time" };
        foreach (var preset in presets)
        {
            var btn = new Button
            {
                Text = preset,
                Height = 32,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = preset == _activePreset ? ColorAccent : ColorCardBg,
                ForeColor = preset == _activePreset ? Color.White : ColorNavInactiveText
            };
            btn.FlatAppearance.BorderColor = preset == _activePreset ? ColorAccent : ColorBorder;
            btn.FlatAppearance.BorderSize = preset == _activePreset ? 0 : 1;

            btn.Click += async (s, e) =>
            {
                _activePreset = preset;
                ApplyPresetDates(preset);
                HighlightActivePresetButton();
                await LoadDashboardDataAsync();
            };

            pnlPresets.Controls.Add(btn);
        }

        var pnlDatePickers = new Panel
        {
            Dock = DockStyle.Right,
            Width = 430,
            BackColor = Color.Transparent
        };

        var lblFrom = new Label { Text = "From:", ForeColor = ColorSubtext, Location = new Point(0, 8), AutoSize = true };
        dtpStart = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Location = new Point(44, 4),
            Font = new Font("Segoe UI", 9f),
            Value = DateTime.Today.AddMonths(-1)
        };

        var lblTo = new Label { Text = "To:", ForeColor = ColorSubtext, Location = new Point(164, 8), AutoSize = true };
        dtpEnd = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Location = new Point(190, 4),
            Font = new Font("Segoe UI", 9f),
            Value = DateTime.Today
        };

        btnApplyFilter = new Button
        {
            Text = "Filter",
            Size = new Size(80, 30),
            Location = new Point(312, 4),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnApplyFilter.FlatAppearance.BorderSize = 0;
        btnApplyFilter.MouseEnter += (s, e) => btnApplyFilter.BackColor = ColorAccentHover;
        btnApplyFilter.MouseLeave += (s, e) => btnApplyFilter.BackColor = ColorAccent;
        btnApplyFilter.Click += async (s, e) =>
        {
            _activePreset = "Custom";
            HighlightActivePresetButton();
            await LoadDashboardDataAsync();
        };

        pnlDatePickers.Controls.AddRange(new Control[] { lblFrom, dtpStart, lblTo, dtpEnd, btnApplyFilter });
        pnl.Controls.AddRange(new Control[] { pnlPresets, pnlDatePickers });
        return pnl;
    }

    private void ApplyPresetDates(string preset)
    {
        var today = DateTime.Today;
        switch (preset)
        {
            case "This Month":
                dtpStart.Value = new DateTime(today.Year, today.Month, 1);
                dtpEnd.Value = today;
                break;
            case "Last 30 Days":
                dtpStart.Value = today.AddDays(-30);
                dtpEnd.Value = today;
                break;
            case "Year to Date":
                dtpStart.Value = new DateTime(today.Year, 1, 1);
                dtpEnd.Value = today;
                break;
            case "All Time":
                dtpStart.Value = new DateTime(2020, 1, 1);
                dtpEnd.Value = today.AddYears(1);
                break;
        }
    }

    private void HighlightActivePresetButton()
    {
        foreach (Control c in pnlPresets.Controls)
        {
            if (c is Button btn)
            {
                bool isSelected = btn.Text == _activePreset;
                btn.BackColor = isSelected ? ColorAccent : ColorCardBg;
                btn.ForeColor = isSelected ? Color.White : ColorNavInactiveText;
                btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
                btn.FlatAppearance.BorderColor = isSelected ? ColorAccent : ColorBorder;
            }
        }
    }

    private Panel BuildKpiRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiRevenue = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        kpiDeposits = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiUtilization = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiReturnRate = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };

        table.Controls.Add(kpiRevenue, 0, 0);
        table.Controls.Add(kpiDeposits, 1, 0);
        table.Controls.Add(kpiUtilization, 2, 0);
        table.Controls.Add(kpiReturnRate, 3, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildChartsRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 300, Padding = new Padding(0, 6, 0, 14), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

        var cardLeft = CreateCardContainer("MONTHLY LEASE REVENUE TREND", out var bodyLeft);
        cardLeft.Margin = new Padding(0, 0, 10, 0);
        chartRevenue = new AtelierBarChart { Dock = DockStyle.Fill };
        bodyLeft.Controls.Add(chartRevenue);

        var cardRight = CreateCardContainer("BOOKING STATUS BREAKDOWN", out var bodyRight);
        cardRight.Margin = new Padding(5, 0, 0, 0);
        chartStages = new AtelierDonutChart { Dock = DockStyle.Fill };
        bodyRight.Controls.Add(chartStages);

        table.Controls.Add(cardLeft, 0, 0);
        table.Controls.Add(cardRight, 1, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildLeaderboardsSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 280, Padding = new Padding(0, 6, 0, 14), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        // 1. Top Garments Leaderboard
        var cardGarments = CreateCardContainer("TOP PERFORMING WARDROBE ASSETS", out var bodyGarments);
        cardGarments.Margin = new Padding(0, 0, 8, 0);

        dgvTopGarments = CreateBaseDataGrid();
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemCode", HeaderText = "CODE", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StyleName", HeaderText = "GARMENT STYLE", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Size", HeaderText = "SIZE", Width = 65, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRentals", HeaderText = "LEASES", Width = 75, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenueGenerated", HeaderText = "REVENUE", Width = 115, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorAccent } });
        bodyGarments.Controls.Add(dgvTopGarments);

        // 2. Top VIP Clients Leaderboard
        var cardCustomers = CreateCardContainer("TOP VALUED CLIENTS (VIP LEADERBOARD)", out var bodyCustomers);
        cardCustomers.Margin = new Padding(8, 0, 0, 0);

        dgvTopCustomers = CreateBaseDataGrid();
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientTier", HeaderText = "TIER", Width = 80, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorAccent } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalBookings", HeaderText = "BOOKINGS", Width = 90, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalSpent", HeaderText = "TOTAL SPENT", Width = 120, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorAccent } });
        bodyCustomers.Controls.Add(dgvTopCustomers);

        table.Controls.Add(cardGarments, 0, 0);
        table.Controls.Add(cardCustomers, 1, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildAuditLedgerSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 340, Padding = new Padding(0, 6, 0, 20), BackColor = Color.Transparent };
        var card = CreateCardContainer("ITEMIZED AUDIT LEDGER (TRANSACTION DRILL-DOWN)", out var body);

        dgvAuditLedger = CreateBaseDataGrid();
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BookingCode", HeaderText = "CODE", Width = 105, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) } });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT", Width = 160, SortMode = DataGridViewColumnSortMode.NotSortable });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GarmentSummary", HeaderText = "GARMENTS ALLOCATED", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RentalStartDate", HeaderText = "START", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Format = "MMM dd, yyyy" } });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RentalEndDate", HeaderText = "END", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Format = "MMM dd, yyyy" } });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RentalFee", HeaderText = "FEE", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorAccent } });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SecurityDeposit", HeaderText = "DEPOSIT", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00" } });
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Stage", HeaderText = "STAGE", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable });

        body.Controls.Add(dgvAuditLedger);
        pnl.Controls.Add(card);
        return pnl;
    }

    private static DataGridView CreateBaseDataGrid()
    {
        var dgv = new DataGridView
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
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 40
        };

        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorCardBg;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorMutedLabel;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorCardBg;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorMutedLabel;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

        dgv.DefaultCellStyle.BackColor = ColorCardBg;
        dgv.DefaultCellStyle.ForeColor = ColorBrandDark;
        dgv.DefaultCellStyle.SelectionBackColor = ColorRowSelected;
        dgv.DefaultCellStyle.SelectionForeColor = ColorRowSelectedText;
        dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
        dgv.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

        dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorRowAlt;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = ColorBrandDark;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = ColorRowSelected;
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = ColorRowSelectedText;
        dgv.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
        dgv.AlternatingRowsDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

        return dgv;
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
            using var pen = new Pen(ColorBorder, 1.25f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        };

        var lbl = new Label
        {
            Text = title,
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Top,
            Height = 26
        };

        bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg
        };

        card.Controls.Add(bodyPanel);
        card.Controls.Add(lbl);
        return card;
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            DateTime? start = _activePreset == "All Time" ? null : dtpStart.Value.Date;
            DateTime? end = _activePreset == "All Time" ? null : dtpEnd.Value.Date;
            int? branchId = _getBranchId?.Invoke();

            var data = await _controller.LoadDashboardMetricsAsync(_getCompanyId(), branchId, start, end);

            // 1. KPI Cards
            kpiRevenue.SetData("GROSS LEASE REVENUE", $"₱{data.TotalLeaseRevenue:N2}", "Filtered Period", ColorAccent, ColorActivePill, ColorAccent);
            kpiDeposits.SetData("SECURITY DEPOSITS HELD", $"₱{data.ActiveDepositsHeld:N2}", "Active Escrow", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));
            kpiUtilization.SetData("FLEET UTILIZATION", $"{data.FleetUtilizationRate:0.#}%", $"{data.CurrentlyRentedGarments}/{data.TotalActiveGarments} Rented", ColorSuccess, ColorBadgeBg, ColorSuccess);
            kpiReturnRate.SetData("RETURN COMPLIANCE", $"{data.OnTimeReturnRate:0.#}%", $"{data.TotalBookingsCompleted} Completed", Color.FromArgb(124, 58, 237), Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237));

            // 2. Charts
            chartRevenue.SetData(data.MonthlyRevenueTrend);
            chartStages.SetData(data.StageDistribution);

            // 3. Top Garments Grid
            dgvTopGarments.AutoGenerateColumns = false;
            dgvTopGarments.DataSource = null;
            dgvTopGarments.DataSource = data.TopPerformingGarments;
            dgvTopGarments.ClearSelection();

            // 4. Top Customers Grid
            dgvTopCustomers.AutoGenerateColumns = false;
            dgvTopCustomers.DataSource = null;
            dgvTopCustomers.DataSource = data.TopValuedCustomers;
            dgvTopCustomers.ClearSelection();

            // 5. Audit Ledger Grid
            _currentLedger = data.AuditLedger;
            dgvAuditLedger.AutoGenerateColumns = false;
            dgvAuditLedger.DataSource = null;
            dgvAuditLedger.DataSource = _currentLedger;
            dgvAuditLedger.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to generate analytics: {ex.GetBaseException().Message}", "Analytics Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnExportCsv_Click(object? sender, EventArgs e)
    {
        if (_currentLedger.Count == 0)
        {
            MessageBox.Show(
                "No transaction records available to export for the selected filter.",
                "Export Notice",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv",
            FileName = $"Atelier_Rental_Ledger_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            await _csvExportService.ExportRentalLedgerAsync(sfd.FileName, _currentLedger);

            MessageBox.Show(
                $"Audit ledger successfully exported to:\n{sfd.FileName}",
                "Export Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export CSV: {ex.Message}",
                "Export Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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