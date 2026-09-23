using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using CRM.winforms.Controls;
using CRM.winforms.Models;
using CRM.winforms.Services;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class AnalyticsAndReportsView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
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

    // Charts
    private AtelierBarChart chartRevenue = null!;
    private AtelierDonutChart chartStages = null!;

    // Grids
    private DataGridView dgvTopGarments = null!;
    private DataGridView dgvTopCustomers = null!;
    private DataGridView dgvAuditLedger = null!;

    // Cached state
    private List<RentalLedgerRowDto> _currentLedger = new();

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
        _controller = new AnalyticsReportController(_contextFactory);
        _csvExportService = new CsvExportService();

        InitializeLayout();
        _ = LoadDashboardDataAsync();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 32);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = BuildHeaderSection();

        var pnlFilterBar = BuildFilterBar();

        var pnlKpiRow = BuildKpiRow();

        var pnlChartsRow = BuildChartsRow();

        var pnlLeaderboardRow = BuildLeaderboardsSection();

        var pnlLedgerSection = BuildAuditLedgerSection();

        Controls.Add(pnlLedgerSection);
        Controls.Add(pnlLeaderboardRow);
        Controls.Add(pnlChartsRow);
        Controls.Add(pnlKpiRow);
        Controls.Add(pnlFilterBar);
        Controls.Add(pnlHeader);
    }

    private Panel BuildHeaderSection()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };

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
            Text = "Executive lease revenue metrics, fleet utilization, and customer performance.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 32),
            AutoSize = true
        };

        btnExportCsv = new Button
        {
            Text = "📥 Export to CSV",
            Size = new Size(140, 36),
            BackColor = Color.White,
            ForeColor = ColorEspresso,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExportCsv.FlatAppearance.BorderColor = ColorBorder;
        btnExportCsv.Click += BtnExportCsv_Click;

        pnl.Resize += (s, e) =>
        {
            btnExportCsv.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportCsv.Width), 10);
        };
        btnExportCsv.Location = new Point(Math.Max(0, pnl.ClientSize.Width - btnExportCsv.Width), 10);

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
            Width = 380,
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
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0),
                BackColor = preset == _activePreset ? ColorDustyRose : Color.White,
                ForeColor = preset == _activePreset ? Color.White : ColorEspresso
            };
            btn.FlatAppearance.BorderColor = ColorBorder;
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
            BackColor = ColorDustyRose,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnApplyFilter.FlatAppearance.BorderSize = 0;
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
                btn.BackColor = isSelected ? ColorDustyRose : Color.White;
                btn.ForeColor = isSelected ? Color.White : ColorEspresso;
                btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            }
        }
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

        var cardGarments = CreateCardContainer("TOP PERFORMING WARDROBE ASSETS", out var bodyGarments);
        cardGarments.Margin = new Padding(0, 0, 8, 0);

        dgvTopGarments = CreateBaseDataGrid();
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemCode", HeaderText = "CODE", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StyleName", HeaderText = "GARMENT STYLE", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Size", HeaderText = "SIZE", Width = 65, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRentals", HeaderText = "LEASES", Width = 75, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvTopGarments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenueGenerated", HeaderText = "REVENUE", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorDustyRose } });
        bodyGarments.Controls.Add(dgvTopGarments);

        var cardCustomers = CreateCardContainer("TOP VALUED CLIENTS (VIP LEADERBOARD)", out var bodyCustomers);
        cardCustomers.Margin = new Padding(8, 0, 0, 0);

        dgvTopCustomers = CreateBaseDataGrid();
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientTier", HeaderText = "TIER", Width = 80, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorDustyRose } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalBookings", HeaderText = "BOOKINGS", Width = 90, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalSpent", HeaderText = "TOTAL SPENT", Width = 120, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorDustyRose } });
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
        dgvAuditLedger.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RentalFee", HeaderText = "FEE", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorDustyRose } });
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
            RowTemplate = { Height = 44 },
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorSubtext;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorSubtext;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
        dgv.ColumnHeadersHeight = 36;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.DefaultCellStyle.ForeColor = ColorEspresso;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 246, 246);
        dgv.DefaultCellStyle.SelectionForeColor = ColorEspresso;
        dgv.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

        return dgv;
    }

    private static Panel CreateCardContainer(string title, out Panel bodyPanel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 14, 18, 14)
        };

        card.Resize += (s, e) =>
        {
            using var clipPath = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, card.Width, card.Height), 8);
            card.Region = new Region(clipPath);
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1.25f);
            using var path = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
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
            DateTime? start = _activePreset == "All Time" ? null : dtpStart.Value.Date;
            DateTime? end = _activePreset == "All Time" ? null : dtpEnd.Value.Date;

            var data = await _controller.LoadDashboardMetricsAsync(_getCompanyId(), start, end);

            // KPI Cards
            kpiRevenue.SetData("GROSS LEASE REVENUE", $"₱{data.TotalLeaseRevenue:N2}", "Filtered Period", ColorDustyRose, Color.FromArgb(254, 242, 243), ColorDustyRose);
            kpiDeposits.SetData("SECURITY DEPOSITS HELD", $"₱{data.ActiveDepositsHeld:N2}", "Active Escrow", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));
            kpiUtilization.SetData("FLEET UTILIZATION", $"{data.FleetUtilizationRate:0.#}%", $"{data.CurrentlyRentedGarments}/{data.TotalActiveGarments} Rented", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105));
            kpiReturnRate.SetData("RETURN COMPLIANCE", $"{data.OnTimeReturnRate:0.#}%", $"{data.TotalBookingsCompleted} Completed", Color.FromArgb(124, 58, 237), Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237));

            // Charts
            chartRevenue.SetData(data.MonthlyRevenueTrend);
            chartStages.SetData(data.StageDistribution);

            // Top Garments Grid
            dgvTopGarments.DataSource = null;
            dgvTopGarments.DataSource = data.TopPerformingGarments;
            dgvTopGarments.ClearSelection();

            // Top Customers Grid
            dgvTopCustomers.DataSource = null;
            dgvTopCustomers.DataSource = data.TopValuedCustomers;
            dgvTopCustomers.ClearSelection();

            // Audit Ledger Grid
            _currentLedger = data.AuditLedger;
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
}