using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class ComplaintsView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly ComplaintController _controller;

    private string _currentStatusFilter = "All";
    private string _currentSearch = string.Empty;
    private ComplaintPipelineDto? _pipelineData;

    // KPI Cards
    private KpiCardControl kpiNew = null!;
    private KpiCardControl kpiInvestigating = null!;
    private KpiCardControl kpiResolutionRate = null!;
    private KpiCardControl kpiTotal = null!;

    private FlowLayoutPanel pnlChevronPills = null!;
    private FlowLayoutPanel pnlFilterTabs = null!;
    private TextBox txtSearch = null!;
    private Label lblRecordsCount = null!;
    private DataGridView dgvComplaints = null!;

    public ComplaintsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
        _controller = new ComplaintController(_contextFactory);

        InitializeLayout();
        _ = LoadComplaintsAsync();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f);

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label { Text = "Client Complaints & Incidents", Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
        var lblSub = new Label { Text = "Log customer disputes, track quality escalations, and audit corrective resolutions.", Font = new Font("Segoe UI", 9.75f), ForeColor = ColorSubtext, Location = new Point(0, 34), AutoSize = true };

        var btnLog = new Button
        {
            Text = "+ Log Complaint",
            Size = new Size(140, 36),
            Location = new Point(Width - 140 - 64, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLog.FlatAppearance.BorderSize = 0;
        btnLog.Click += BtnLog_Click;

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnLog });

        var pnlKpis = BuildKpiRow();
        var pnlChevronStrip = BuildChevronStrip();
        var pnlFilterStrip = BuildFilterStrip();
        var pnlTableCard = BuildTableCard();

        Controls.Add(pnlTableCard);
        Controls.Add(pnlFilterStrip);
        Controls.Add(pnlChevronStrip);
        Controls.Add(pnlKpis);
        Controls.Add(pnlHeader);
    }

    private Panel BuildKpiRow()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiNew = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        kpiInvestigating = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiResolutionRate = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiTotal = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };

        table.Controls.AddRange(new Control[] { kpiNew, kpiInvestigating, kpiResolutionRate, kpiTotal });
        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildChevronStrip()
    {
        var wrapper = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(0, 4, 0, 10), BackColor = Color.Transparent };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 12, 16, 12) };
        ApplyRoundedCard(card, ColorBorder);

        pnlChevronPills = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, BackColor = Color.Transparent };
        card.Controls.Add(pnlChevronPills);
        wrapper.Controls.Add(card);
        return wrapper;
    }

    private Panel BuildFilterStrip()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 8) };

        var pnlSearch = new Panel { Location = new Point(0, 4), Size = new Size(260, 32), BackColor = Color.White };
        pnlSearch.Paint += (s, e) => { using var pen = new Pen(ColorBorder, 1f); e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1); };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Width = 240,
            Location = new Point(8, 7),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorPrimary,
            PlaceholderText = "Search by client, ID, category..."
        };
        txtSearch.TextChanged += async (s, e) =>
        {
            _currentSearch = txtSearch.Text.Trim();
            await LoadComplaintsAsync();
        };
        pnlSearch.Controls.Add(txtSearch);

        pnlFilterTabs = new FlowLayoutPanel { Location = new Point(275, 4), AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
        InitFilterTabs();

        lblRecordsCount = new Label { Dock = DockStyle.Right, Text = "0 records", ForeColor = ColorSubtext, AutoSize = true, Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 8, 0, 0) };

        pnl.Controls.AddRange(new Control[] { pnlSearch, pnlFilterTabs, lblRecordsCount });
        return pnl;
    }

    private void InitFilterTabs()
    {
        string[] tabs = new[] { "All", "New", "Under Investigation", "In Progress", "Resolved", "Escalated" };
        foreach (var tab in tabs)
        {
            var btn = new Button
            {
                Text = tab,
                Tag = tab,
                AutoSize = true,
                Height = 32,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ColorBorder;
            btn.Click += async (s, e) =>
            {
                _currentStatusFilter = tab;
                UpdateFilterTabStyles();
                await LoadComplaintsAsync();
            };
            pnlFilterTabs.Controls.Add(btn);
        }
        UpdateFilterTabStyles();
    }

    private void UpdateFilterTabStyles()
    {
        foreach (Control c in pnlFilterTabs.Controls)
        {
            if (c is Button btn && btn.Tag is string tabName)
            {
                bool isSelected = string.Equals(_currentStatusFilter, tabName, StringComparison.OrdinalIgnoreCase);
                btn.BackColor = isSelected ? ColorAccent : Color.White;
                btn.ForeColor = isSelected ? Color.White : ColorPrimary;
                btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            }
        }
    }

    private Panel BuildTableCard()
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 4), BackColor = Color.Transparent };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        ApplyRoundedCard(card, ColorBorder);

        dgvComplaints = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(244, 237, 237),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate = { Height = 48 }
        };

        dgvComplaints.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvComplaints.EnableHeadersVisualStyles = false;
        dgvComplaints.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        dgvComplaints.ColumnHeadersDefaultCellStyle.ForeColor = ColorSubtext;
        dgvComplaints.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgvComplaints.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        dgvComplaints.ColumnHeadersHeight = 38;

        var boldStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };

        dgvComplaints.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "ComplaintCode", HeaderText = "CODE", Width = 100, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT", Width = 150, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "Category", HeaderText = "CATEGORY", Width = 150 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Severity", HeaderText = "SEVERITY", Width = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "STATUS", Width = 140 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "INCIDENT DESCRIPTION", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { DataPropertyName = "CompensationAmount", HeaderText = "REFUND/CREDIT", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = ColorAccent } },
            new DataGridViewTextBoxColumn { DataPropertyName = "LoggedDateFormatted", HeaderText = "LOGGED", Width = 110 },
            new DataGridViewButtonColumn { Name = "ColView", HeaderText = "", Text = "View", UseColumnTextForButtonValue = true, Width = 75, FlatStyle = FlatStyle.Flat },
            new DataGridViewButtonColumn { Name = "ColResolve", HeaderText = "ACTION", Text = "Resolve", UseColumnTextForButtonValue = true, Width = 85, FlatStyle = FlatStyle.Flat }
        });

        foreach (DataGridViewColumn col in dgvComplaints.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;

        dgvComplaints.CellContentClick += Grid_CellContentClick;
        dgvComplaints.CellPainting += DgvComplaints_CellPainting;

        card.Controls.Add(dgvComplaints);
        wrapper.Controls.Add(card);
        return wrapper;
    }

    public async Task LoadComplaintsAsync()
    {
        try
        {
            int? branchId = _getBranchId?.Invoke();
            _pipelineData = await _controller.LoadPipelineAsync(_getCompanyId(), branchId, _currentStatusFilter, _currentSearch);
            if (_pipelineData == null) return;

            kpiNew.SetData("NEW COMPLAINTS", _pipelineData.NewCount.ToString(), "Requires triage", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28));
            kpiInvestigating.SetData("UNDER REVIEW", _pipelineData.InvestigatingCount.ToString(), "In assessment", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9));
            kpiResolutionRate.SetData("RESOLUTION RATE", $"{_pipelineData.ResolutionRate:0.#}%", $"{_pipelineData.ResolvedCount} Settled", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105));
            kpiTotal.SetData("TOTAL INCIDENTS", _pipelineData.TotalCount.ToString(), "Scoped showroom", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));

            lblRecordsCount.Text = $"{_pipelineData.Rows.Count} records";
            RenderChevronPills();

            dgvComplaints.DataSource = null;
            dgvComplaints.DataSource = _pipelineData.Rows;
            dgvComplaints.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load complaints: {ex.GetBaseException().Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderChevronPills()
    {
        if (pnlChevronPills == null || _pipelineData == null) return;

        pnlChevronPills.SuspendLayout();
        pnlChevronPills.Controls.Clear();

        var stages = new (string Stage, int Count, Color Bg, Color Fg)[]
        {
            ("New", _pipelineData.NewCount, Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38)),
            ("Under Investigation", _pipelineData.InvestigatingCount, Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
            ("In Progress", _pipelineData.InProgressCount, Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
            ("Resolved", _pipelineData.ResolvedCount, Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105)),
            ("Escalated", _pipelineData.EscalatedCount, Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206))
        };

        for (int i = 0; i < stages.Length; i++)
        {
            var item = stages[i];
            var container = new Panel { Width = 150, Height = 48, BackColor = Color.Transparent };
            var lblCount = new Label { Text = item.Count.ToString(), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            var badge = new Label { Text = item.Stage, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = item.Fg, BackColor = item.Bg, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Location = new Point(0, 24) };

            container.Controls.AddRange(new Control[] { lblCount, badge });
            pnlChevronPills.Controls.Add(container);

            if (i < stages.Length - 1)
            {
                var chevron = new Label { Text = "›", Font = new Font("Segoe UI", 14f), ForeColor = Color.FromArgb(209, 213, 219), Width = 24, Height = 48, TextAlign = ContentAlignment.MiddleCenter };
                pnlChevronPills.Controls.Add(chevron);
            }
        }

        pnlChevronPills.ResumeLayout();
    }

    private void DgvComplaints_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _pipelineData?.Rows == null || e.RowIndex >= _pipelineData.Rows.Count || e.Graphics == null)
            return;

        var colName = dgvComplaints.Columns[e.ColumnIndex]?.DataPropertyName;
        if (colName is not ("Severity" or "Status")) return;

        var row = _pipelineData.Rows[e.RowIndex];
        string text = (colName == "Severity" ? row.Severity : row.Status) ?? string.Empty;

        var (bg, fg) = text switch
        {
            "Critical" or "New" => (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38)),
            "High" or "Under Investigation" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
            "In Progress" => (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
            "Resolved" or "Low" => (Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105)),
            "Escalated" => (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
            _ => (Color.FromArgb(243, 244, 246), Color.FromArgb(107, 114, 128))
        };

        e.PaintBackground(e.ClipBounds, true);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 12, 115, 24);
        using (var brush = new SolidBrush(bg))
        using (var path = CreateRoundedRectangle(badgeRect, 4))
        {
            e.Graphics.FillPath(brush, path);
        }

        using var font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, text, font, badgeRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        e.Handled = true;
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _pipelineData?.Rows == null || e.RowIndex >= _pipelineData.Rows.Count) return;

        var row = _pipelineData.Rows[e.RowIndex];
        var colName = dgvComplaints.Columns[e.ColumnIndex]?.Name;

        if (colName == "ColView")
        {
            var entity = await _controller.GetComplaintAsync(row.ComplaintId);
            if (entity != null)
            {
                using var dlg = new ComplaintDialogForm(_contextFactory, _getCompanyId(), entity, _getBranchId?.Invoke());
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    await _controller.SaveComplaintAsync(dlg.ComplaintModel);
                    await LoadComplaintsAsync();
                }
            }
        }
        else if (colName == "ColResolve")
        {
            var entity = await _controller.GetComplaintAsync(row.ComplaintId);
            if (entity != null)
            {
                entity.Status = "Resolved";
                using var dlg = new ComplaintDialogForm(_contextFactory, _getCompanyId(), entity, _getBranchId?.Invoke());
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    await _controller.SaveComplaintAsync(dlg.ComplaintModel);
                    await LoadComplaintsAsync();
                }
            }
        }
    }

    private async void BtnLog_Click(object? sender, EventArgs e)
    {
        using var dlg = new ComplaintDialogForm(_contextFactory, _getCompanyId(), null, _getBranchId?.Invoke());
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await _controller.SaveComplaintAsync(dlg.ComplaintModel);
            await LoadComplaintsAsync();
        }
    }

    private static void ApplyRoundedCard(Panel pnl, Color borderColor)
    {
        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(borderColor, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };
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