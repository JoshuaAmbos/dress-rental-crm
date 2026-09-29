using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class InquiriesView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly InquiryController _controller;

    private string _currentStatusFilter = "All";
    private string _currentSearch = string.Empty;
    private InquiryPipelineDto? _pipelineData;

    // KPI Cards
    private KpiCardControl kpiNew = null!;
    private KpiCardControl kpiInReview = null!;
    private KpiCardControl kpiRate = null!;
    private KpiCardControl kpiTotal = null!;

    private FlowLayoutPanel pnlChevronPills = null!;
    private FlowLayoutPanel pnlFilterTabs = null!;
    private TextBox txtSearch = null!;
    private Label lblRecordsCount = null!;
    private DataGridView dgvInquiries = null!;

    // Palette Constants
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    public InquiriesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
        : this(contextFactory, getCompanyId, null)
    {
    }

    public InquiriesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
        _controller = new InquiryController(_contextFactory);

        InitializeLayout();
        _ = LoadInquiriesAsync();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f);

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label { Text = "Client Inquiries", Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = ColorEspresso, AutoSize = true };
        var lblSub = new Label { Text = "Track inbound wardrobe consultations and convert qualified prospects to bookings.", Font = new Font("Segoe UI", 9.75f), ForeColor = ColorSubtext, Location = new Point(0, 34), AutoSize = true };

        var btnLog = new Button
        {
            Text = "+ Log Inquiry",
            Size = new Size(130, 36),
            Location = new Point(Width - 130 - 64, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = ColorDustyRose,
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
        kpiInReview = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiRate = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 10, 0) };
        kpiTotal = new KpiCardControl { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };

        table.Controls.AddRange(new Control[] { kpiNew, kpiInReview, kpiRate, kpiTotal });
        pnl.Controls.Add(table);
        return pnl;
    }

    private Panel BuildChevronStrip()
    {
        var wrapper = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(0, 4, 0, 10), BackColor = Color.Transparent };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 12, 16, 12) };
        ApplyRoundedCard(card, ColorBorder);

        pnlChevronPills = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        card.Controls.Add(pnlChevronPills);
        wrapper.Controls.Add(card);
        return wrapper;
    }

    private Panel BuildFilterStrip()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 8) };

        var pnlSearch = new Panel { Location = new Point(0, 4), Size = new Size(240, 32), BackColor = Color.White };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Width = 220,
            Location = new Point(8, 7),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            PlaceholderText = "Search by name, event, ID..."
        };
        txtSearch.TextChanged += async (s, e) =>
        {
            _currentSearch = txtSearch.Text.Trim();
            await LoadInquiriesAsync();
        };
        pnlSearch.Controls.Add(txtSearch);

        pnlFilterTabs = new FlowLayoutPanel { Location = new Point(255, 4), AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
        InitFilterTabs();

        lblRecordsCount = new Label { Dock = DockStyle.Right, Text = "0 records", ForeColor = ColorSubtext, AutoSize = true, Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 8, 0, 0) };

        pnl.Controls.AddRange(new Control[] { pnlSearch, pnlFilterTabs, lblRecordsCount });
        return pnl;
    }

    private void InitFilterTabs()
    {
        string[] tabs = new[] { "All", "New", "In Review", "Quoted", "Converted", "Closed" };
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
                await LoadInquiriesAsync();
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
                btn.BackColor = isSelected ? ColorDustyRose : Color.White;
                btn.ForeColor = isSelected ? Color.White : ColorEspresso;
                btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            }
        }
    }

    private Panel BuildTableCard()
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 4), BackColor = Color.Transparent };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        ApplyRoundedCard(card, ColorBorder);

        dgvInquiries = new DataGridView
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

        dgvInquiries.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvInquiries.EnableHeadersVisualStyles = false;
        dgvInquiries.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        dgvInquiries.ColumnHeadersDefaultCellStyle.ForeColor = ColorSubtext;
        dgvInquiries.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgvInquiries.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        dgvInquiries.ColumnHeadersHeight = 38;

        var boldStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };

        dgvInquiries.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "InquiryCode", HeaderText = "ID", Width = 100, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT", Width = 160, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "EventType", HeaderText = "EVENT", Width = 130 },
            new DataGridViewTextBoxColumn { DataPropertyName = "EventDateFormatted", HeaderText = "EVENT DATE", Width = 120 },
            new DataGridViewTextBoxColumn { DataPropertyName = "GarmentRequest", HeaderText = "GARMENT REQUEST", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { DataPropertyName = "BudgetRange", HeaderText = "BUDGET", Width = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Priority", HeaderText = "PRIORITY", Width = 100 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "STATUS", Width = 100 },
            new DataGridViewButtonColumn { Name = "ColView", HeaderText = "", Text = "View", UseColumnTextForButtonValue = true, Width = 75, FlatStyle = FlatStyle.Flat },
            new DataGridViewButtonColumn { Name = "ColConvert", HeaderText = "ACTIONS", Text = "Convert", UseColumnTextForButtonValue = true, Width = 85, FlatStyle = FlatStyle.Flat }
        });

        foreach (DataGridViewColumn col in dgvInquiries.Columns)
        {
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        dgvInquiries.CellContentClick += Grid_CellContentClick;
        dgvInquiries.CellPainting += DgvInquiries_CellPainting;

        card.Controls.Add(dgvInquiries);
        wrapper.Controls.Add(card);
        return wrapper;
    }

    public async Task LoadInquiriesAsync()
    {
        try
        {
            int? branchId = _getBranchId?.Invoke();
            _pipelineData = await _controller.LoadPipelineAsync(_getCompanyId(), branchId, _currentStatusFilter, _currentSearch);
            if (_pipelineData == null) return;

            kpiNew.SetData("NEW INQUIRIES", _pipelineData.NewCount.ToString(), "Unactioned", ColorDustyRose, Color.FromArgb(254, 242, 243), ColorDustyRose);
            kpiInReview.SetData("IN REVIEW", _pipelineData.InReviewCount.ToString(), "Being assessed", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9));
            kpiRate.SetData("CONVERSION RATE", $"{_pipelineData.ConversionRate:0.#}%", "Inquiries → Bookings", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105));
            kpiTotal.SetData("TOTAL INQUIRIES", _pipelineData.TotalCount.ToString(), "Scoped showroom", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));

            lblRecordsCount.Text = $"{_pipelineData.Rows.Count} records";
            RenderChevronPills();

            dgvInquiries.DataSource = null;
            dgvInquiries.DataSource = _pipelineData.Rows;
            dgvInquiries.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load inquiries: {ex.GetBaseException().Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderChevronPills()
    {
        if (pnlChevronPills == null || _pipelineData == null) return;

        pnlChevronPills.SuspendLayout();
        pnlChevronPills.Controls.Clear();

        var stages = new (string Stage, int Count, Color BadgeBg, Color BadgeText)[]
        {
            ("New", _pipelineData.NewCount, Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
            ("In Review", _pipelineData.InReviewCount, Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
            ("Quoted", _pipelineData.QuotedCount, Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
            ("Converted", _pipelineData.ConvertedCount, Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105)),
            ("Closed", _pipelineData.ClosedCount, Color.FromArgb(243, 244, 246), Color.FromArgb(107, 114, 128))
        };

        for (int i = 0; i < stages.Length; i++)
        {
            var item = stages[i];
            var container = new Panel { Width = 150, Height = 48, BackColor = Color.Transparent };
            var lblCount = new Label { Text = item.Count.ToString(), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = ColorEspresso, AutoSize = true };
            var badge = new Label { Text = item.Stage, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = item.BadgeText, BackColor = item.BadgeBg, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Location = new Point(0, 24) };

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

    private void DgvInquiries_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _pipelineData?.Rows == null || e.RowIndex >= _pipelineData.Rows.Count || e.Graphics == null)
            return;

        var colName = dgvInquiries.Columns[e.ColumnIndex]?.DataPropertyName;
        if (colName is not ("Priority" or "Status")) return;

        var row = _pipelineData.Rows[e.RowIndex];
        string text = (colName == "Priority" ? row.Priority : row.Status) ?? string.Empty;

        var (bg, fg) = text switch
        {
            "High" => (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38)),
            "Medium" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
            "New" => (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
            "In Review" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
            "Quoted" => (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
            "Converted" => (Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105)),
            _ => (Color.FromArgb(243, 244, 246), Color.FromArgb(107, 114, 128))
        };

        e.PaintBackground(e.ClipBounds, true);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 12, 76, 24);
        using (var brush = new SolidBrush(bg))
        using (var path = GraphicsHelper.CreateRoundedRectangle(badgeRect, 4))
        {
            e.Graphics.FillPath(brush, path);
        }

        using var font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, text, font, badgeRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        e.Handled = true;
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _pipelineData?.Rows == null || e.RowIndex >= _pipelineData.Rows.Count)
            return;

        var row = _pipelineData.Rows[e.RowIndex];
        var colName = dgvInquiries.Columns[e.ColumnIndex]?.Name;

        if (colName == "ColView")
        {
            var entity = await _controller.GetInquiryAsync(row.InquiryId);
            if (entity != null)
            {
                using var dlg = new InquiryDialogForm(_contextFactory, _getCompanyId(), entity, _getBranchId?.Invoke());
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    await _controller.SaveInquiryAsync(dlg.InquiryModel);
                    await LoadInquiriesAsync();
                }
            }
        }
        else if (colName == "ColConvert")
        {
            var confirm = MessageBox.Show(
                $"Convert inquiry {row.InquiryCode} for '{row.ClientName}' into a confirmed booking draft?",
                "Convert to Booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                await _controller.MarkConvertedAsync(row.InquiryId);
                await LoadInquiriesAsync();
                MessageBox.Show($"Inquiry {row.InquiryCode} successfully converted.", "Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private async void BtnLog_Click(object? sender, EventArgs e)
    {
        using var dlg = new InquiryDialogForm(_contextFactory, _getCompanyId(), null, _getBranchId?.Invoke());
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            dlg.InquiryModel.CompanyId = _getCompanyId();
            await _controller.SaveInquiryAsync(dlg.InquiryModel);
            await LoadInquiriesAsync();
        }
    }

    private static void ApplyRoundedCard(Panel pnl, Color borderColor)
    {
        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(borderColor, 1f);
            using var path = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };
    }
}