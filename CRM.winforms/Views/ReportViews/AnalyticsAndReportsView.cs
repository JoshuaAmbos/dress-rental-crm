using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Services;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class AnalyticsAndReportsView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly AnalyticsReportService _reportService;

    // KPI Cards
    private KpiCardControl kpiRevenue = null!;
    private KpiCardControl kpiDeposits = null!;
    private KpiCardControl kpiCompleted = null!;
    private KpiCardControl kpiReturnRate = null!;

    // Charts
    private AtelierBarChart chartRevenue = null!;
    private AtelierDonutChart chartStages = null!;

    // Top Garments Table
    private DataGridView dgvTopGarments = null!;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    public AnalyticsAndReportsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _reportService = new AnalyticsReportService(_contextFactory);

        InitializeLayout();
        _ = LoadDashboardDataAsync();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header (UseMnemonic = false prevents the _ underscore)
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "Business Intelligence & Reports",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Track lease performance, inventory utilization, and customer return compliance.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };
        pnlHeader.Controls.AddRange([lblTitle, lblSub]);

        // 2. 4 KPI Cards Row
        var pnlKpiRow = BuildKpiRow();

        // 3. Middle Charts Row (Bar + Donut)
        var pnlChartsRow = BuildChartsRow();

        // 4. Bottom Leaderboard Table
        var pnlLeaderboard = BuildLeaderboardSection();

        // Order matters for DockStyle.Top
        Controls.Add(pnlLeaderboard);
        Controls.Add(pnlChartsRow);
        Controls.Add(pnlKpiRow);
        Controls.Add(pnlHeader);
    }

    private Panel BuildKpiRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 145, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiRevenue = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        kpiDeposits = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiCompleted = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiReturnRate = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };

        table.Controls.Add(kpiRevenue, 0, 0);
        table.Controls.Add(kpiDeposits, 1, 0);
        table.Controls.Add(kpiCompleted, 2, 0);
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

        // Card 1: Revenue Trend (Bar Chart)
        var cardLeft = CreateCardContainer("MONTHLY LEASE REVENUE TREND", out var bodyLeft);
        cardLeft.Margin = new Padding(0, 0, 10, 0);
        chartRevenue = new AtelierBarChart { Dock = DockStyle.Fill };
        bodyLeft.Controls.Add(chartRevenue);

        // Card 2: Lifecycle Breakdown (Donut Chart)
        var cardRight = CreateCardContainer("BOOKING STATUS BREAKDOWN", out var bodyRight);
        cardRight.Margin = new Padding(5, 0, 0, 0);
        chartStages = new AtelierDonutChart { Dock = DockStyle.Fill };
        bodyRight.Controls.Add(chartStages);

        table.Controls.Add(cardLeft, 0, 0);
        table.Controls.Add(cardRight, 1, 0);

        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildLeaderboardSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 280, Padding = new Padding(0, 6, 0, 18), BackColor = Color.Transparent };
        var card = CreateCardContainer("MOST REQUESTED & PROFITABLE GARMENTS", out var body);

        dgvTopGarments = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(244, 237, 237),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowTemplate = { Height = 46 }
        };

        // Header Styling (Fixes the electric blue selection)
        dgvTopGarments.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvTopGarments.EnableHeadersVisualStyles = false;
        dgvTopGarments.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        dgvTopGarments.ColumnHeadersDefaultCellStyle.ForeColor = ColorSubtext;
        dgvTopGarments.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        dgvTopGarments.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorSubtext;
        dgvTopGarments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgvTopGarments.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        dgvTopGarments.ColumnHeadersHeight = 38;
        dgvTopGarments.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // Row Styling (Atelier blush on row select, Espresso text)
        dgvTopGarments.DefaultCellStyle.BackColor = Color.White;
        dgvTopGarments.DefaultCellStyle.ForeColor = ColorEspresso;
        dgvTopGarments.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 246, 246);
        dgvTopGarments.DefaultCellStyle.SelectionForeColor = ColorEspresso;
        dgvTopGarments.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);

        // Columns - all set to NotSortable to avoid blue highlight selection
        var colCode = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ItemCode",
            HeaderText = "CODE",
            Width = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) }
        };

        var colStyle = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "StyleName",
            HeaderText = "GARMENT STYLE",
            SortMode = DataGridViewColumnSortMode.NotSortable,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };

        var colSize = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Size",
            HeaderText = "SIZE",
            Width = 90,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        var colLeases = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TotalRentals",
            HeaderText = "TOTAL LEASES",
            Width = 130,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        };

        var colRev = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TotalRevenueGenerated",
            HeaderText = "TOTAL REVENUE",
            Width = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            DefaultCellStyle =
        {
            Alignment = DataGridViewContentAlignment.MiddleRight,
            Format = "$#,##0",
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = ColorDustyRose,
            SelectionForeColor = ColorDustyRose
        }
        };

        dgvTopGarments.Columns.AddRange(new DataGridViewColumn[] { colCode, colStyle, colSize, colLeases, colRev });

        body.Controls.Add(dgvTopGarments);
        pnl.Controls.Add(card);
        return pnl;
    }

    private static Panel CreateCardContainer(string title, out Panel bodyPanel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 14, 18, 14)
        };

        // Clip all inner children to the rounded bounds so sharp square corners never poke through
        card.Resize += (s, e) =>
        {
            using var clipPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width, card.Height), 8);
            card.Region = new Region(clipPath);
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
            BackColor = Color.White
        };

        card.Controls.Add(bodyPanel);
        card.Controls.Add(lbl);

        return card;
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            var data = await _reportService.GetAnalyticsOverviewAsync(_getCompanyId());

            // 1. KPI Cards
            kpiRevenue.SetData("TOTAL LEASE REVENUE", $"${data.TotalLeaseRevenue:N0}", "Flat Lease Basis", ColorDustyRose, Color.FromArgb(254, 242, 243), ColorDustyRose);
            kpiDeposits.SetData("SECURITY DEPOSITS HELD", $"${data.ActiveDepositsHeld:N0}", "Active Circulation", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));
            kpiCompleted.SetData("COMPLETED LEASES", data.TotalBookingsCompleted.ToString(), "Settled Returns", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105));
            kpiReturnRate.SetData("ON-TIME RETURN RATE", $"{data.OnTimeReturnRate:0.#}%", "Turnaround Health", Color.FromArgb(124, 58, 237), Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237));

            // 2. Charts
            chartRevenue.SetData(data.MonthlyRevenueTrend);
            chartStages.SetData(data.StageDistribution);

            // 3. Grid
            dgvTopGarments.DataSource = data.TopPerformingGarments;
            dgvTopGarments.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to generate analytics: {ex.GetBaseException().Message}", "Analytics Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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