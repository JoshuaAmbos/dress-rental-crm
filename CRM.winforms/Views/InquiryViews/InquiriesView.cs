using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class InquiriesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly InquiryController _controller;

    private string _currentStatusFilter = "All";
    private string _currentSearch = string.Empty;
    private InquiryPipelineDto? _pipelineData;

    // Header & Actions
    private Button btnLog = null!;

    // KPI Cards
    private KpiCardControl kpiNew = null!;
    private KpiCardControl kpiInReview = null!;
    private KpiCardControl kpiRate = null!;
    private KpiCardControl kpiTotal = null!;

    // Chevron Strip & Filter Bar
    private FlowLayoutPanel pnlChevronPills = null!;
    private FlowLayoutPanel pnlFilterTabs = null!;
    private TextBox txtSearch = null!;
    private Label lblRecordsCount = null!;
    private DataGridView dgvInquiries = null!;

    public InquiriesView() : this(() =>
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }, () => 1, null)
    {
    }

    public InquiriesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
        : this(contextFactory, getCompanyId, null)
    {
    }

    public InquiriesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
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
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f);

        // 1. Header & Primary Action
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "Client Inquiries",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Track inbound wardrobe consultations and convert qualified prospects to bookings.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnLog = new Button
        {
            Text = "+ Log Inquiry",
            Size = new Size(130, 38),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLog.FlatAppearance.BorderSize = 0;
        btnLog.MouseEnter += (s, e) => btnLog.BackColor = ColorAccentHover;
        btnLog.MouseLeave += (s, e) => btnLog.BackColor = ColorAccent;
        btnLog.Click += BtnLog_Click;

        pnlHeader.Resize += (s, e) =>
        {
            btnLog.Location = new Point(Math.Max(0, pnlHeader.ClientSize.Width - btnLog.Width), 12);
        };
        btnLog.Location = new Point(Math.Max(0, pnlHeader.ClientSize.Width - btnLog.Width), 12);

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnLog });

        // 2. Sections
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
        var pnl = new Panel { Dock = DockStyle.Top, Height = 136, Padding = new Padding(0, 6, 0, 10), BackColor = Color.Transparent };
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
        var card = new Panel { Dock = DockStyle.Fill, BackColor = ColorCardBg, Padding = new Padding(16, 12, 16, 12) };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

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

        var pnlSearch = new Panel { Location = new Point(0, 4), Size = new Size(250, 34), BackColor = ColorCardBg };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Width = 230,
            Location = new Point(8, 8),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            PlaceholderText = "Search by client, event, code..."
        };
        txtSearch.TextChanged += async (s, e) =>
        {
            _currentSearch = txtSearch.Text.Trim();
            await LoadInquiriesAsync();
        };
        pnlSearch.Controls.Add(txtSearch);

        pnlFilterTabs = new FlowLayoutPanel { Location = new Point(265, 4), AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
        InitFilterTabs();

        lblRecordsCount = new Label
        {
            Dock = DockStyle.Right,
            Text = "0 records",
            ForeColor = ColorSubtext,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 8, 0, 0)
        };

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
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
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
                btn.BackColor = isSelected ? ColorAccent : ColorCardBg;
                btn.ForeColor = isSelected ? Color.White : ColorNavInactiveText;
                btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
                btn.FlatAppearance.BorderColor = isSelected ? ColorAccent : ColorBorder;
            }
        }
    }

    private Panel BuildTableCard()
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 4), BackColor = Color.Transparent };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = ColorCardBg, Padding = new Padding(1) };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        dgvInquiries = new DataGridView
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
            ColumnHeadersHeight = 42
        };

        dgvInquiries.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            SelectionBackColor = ColorCardBg,
            SelectionForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0)
        };

        dgvInquiries.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        dgvInquiries.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorRowAlt,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        var boldStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold) };

        dgvInquiries.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "InquiryCode", HeaderText = "CODE", Width = 95, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "CLIENT", Width = 150, DefaultCellStyle = boldStyle },
            new DataGridViewTextBoxColumn { DataPropertyName = "EventType", HeaderText = "EVENT", Width = 120 },
            new DataGridViewTextBoxColumn { DataPropertyName = "EventDateFormatted", HeaderText = "EVENT DATE", Width = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = "GarmentRequest", HeaderText = "GARMENT REQUEST", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { DataPropertyName = "BudgetRange", HeaderText = "BUDGET", Width = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Priority", HeaderText = "PRIORITY", Width = 100 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "STATUS", Width = 105 },
            new DataGridViewButtonColumn { Name = "ColView", HeaderText = "", Text = "View", UseColumnTextForButtonValue = true, Width = 74 },
            new DataGridViewButtonColumn { Name = "ColConvert", HeaderText = "ACTIONS", Text = "", UseColumnTextForButtonValue = false, Width = 84 }
        });

        foreach (DataGridViewColumn col in dgvInquiries.Columns)
        {
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        dgvInquiries.CellContentClick += Grid_CellContentClick;
        dgvInquiries.CellPainting += DgvInquiries_CellPainting;
        dgvInquiries.CellMouseMove += DgvInquiries_CellMouseMove;

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

            kpiNew.SetData("NEW INQUIRIES", _pipelineData.NewCount.ToString(), "Unactioned", ColorAccent, ColorActivePill, ColorAccent);
            kpiInReview.SetData("IN REVIEW", _pipelineData.InReviewCount.ToString(), "Being assessed", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9));
            kpiRate.SetData("CONVERSION RATE", $"{_pipelineData.ConversionRate:0.#}%", "Inquiries → Bookings", ColorSuccess, ColorBadgeBg, ColorSuccess);
            kpiTotal.SetData("TOTAL INQUIRIES", _pipelineData.TotalCount.ToString(), "Active showroom", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216));

            lblRecordsCount.Text = $"{_pipelineData.Rows.Count} records";

            RenderChevronPills();
            UpdateFilterTabStyles();

            dgvInquiries.AutoGenerateColumns = false;
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
            ("Converted", _pipelineData.ConvertedCount, ColorBadgeBg, ColorSuccess),
            ("Closed", _pipelineData.ClosedCount, Color.FromArgb(243, 244, 246), ColorNavInactiveText)
        };

        for (int i = 0; i < stages.Length; i++)
        {
            var item = stages[i];
            var container = new Panel { Width = 150, Height = 48, BackColor = Color.Transparent };
            var lblCount = new Label { Text = item.Count.ToString(), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            var badge = new Label
            {
                Text = item.Stage,
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = item.BadgeText,
                BackColor = item.BadgeBg,
                AutoSize = true,
                Padding = new Padding(8, 2, 8, 2),
                Location = new Point(0, 24)
            };

            container.Controls.AddRange(new Control[] { lblCount, badge });
            pnlChevronPills.Controls.Add(container);

            if (i < stages.Length - 1)
            {
                var chevron = new Label
                {
                    Text = "›",
                    Font = new Font("Segoe UI", 14f),
                    ForeColor = ColorBorder,
                    Width = 24,
                    Height = 48,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlChevronPills.Controls.Add(chevron);
            }
        }

        pnlChevronPills.ResumeLayout();
    }

    private void DgvInquiries_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _pipelineData?.Rows == null || e.RowIndex >= _pipelineData.Rows.Count || e.Graphics == null)
            return;

        var column = dgvInquiries.Columns[e.ColumnIndex];
        string colName = column.Name;
        string dataProp = column.DataPropertyName;
        var row = _pipelineData.Rows[e.RowIndex];
        bool isRowSelected = (e.State & DataGridViewElementStates.Selected) != 0;
        bool isConvertedOrClosed = string.Equals(row.Status, "Converted", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(row.Status, "Closed", StringComparison.OrdinalIgnoreCase);

        // Custom render flat modern action buttons
        if (colName is "ColView" or "ColConvert")
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Do not paint "Convert" button for inquiries that are already Converted or Closed
            if (colName == "ColConvert" && isConvertedOrClosed)
            {
                e.Handled = true;
                return;
            }

            bool isConvert = (colName == "ColConvert");
            string btnText = isConvert ? "Convert" : "View";

            Color btnBg = isConvert
                ? (isRowSelected ? Color.White : ColorBadgeBg)
                : (isRowSelected ? Color.FromArgb(240, 220, 225) : ColorActivePill);

            Color btnFg = isConvert
                ? ColorSuccess
                : ColorAccent;

            var btnRect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 8, e.CellBounds.Width - 8, e.CellBounds.Height - 16);

            using var brush = new SolidBrush(btnBg);
            using var path = CreateRoundedRectangle(btnRect, 6);
            e.Graphics.FillPath(brush, path);

            if (!isConvert)
            {
                using var borderPen = new Pen(isRowSelected ? Color.Transparent : ColorBorder, 1f);
                e.Graphics.DrawPath(borderPen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                btnText,
                new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                btnRect,
                btnFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
            return;
        }

        // Custom render Status & Priority pill badges
        if (dataProp is "Priority" or "Status")
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            string text = (dataProp == "Priority" ? row.Priority : row.Status) ?? string.Empty;

            var (bg, fg) = text switch
            {
                "High" => (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38)),
                "Medium" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
                "Low" => (Color.FromArgb(243, 244, 246), Color.FromArgb(107, 114, 128)),
                "New" => (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
                "In Review" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
                "Quoted" => (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
                "Converted" => (ColorBadgeBg, ColorSuccess),
                _ => (Color.FromArgb(243, 244, 246), ColorNavInactiveText)
            };

            var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 10, 78, 24);
            using (var brush = new SolidBrush(bg))
            using (var path = CreateRoundedRectangle(badgeRect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            using var font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, text, font, badgeRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    private void DgvInquiries_CellMouseMove(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _pipelineData?.Rows != null && e.RowIndex < _pipelineData.Rows.Count)
        {
            var colName = dgvInquiries.Columns[e.ColumnIndex]?.Name;
            var row = _pipelineData.Rows[e.RowIndex];
            bool isConvertedOrClosed = string.Equals(row.Status, "Converted", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(row.Status, "Closed", StringComparison.OrdinalIgnoreCase);

            if (colName == "ColView" || (colName == "ColConvert" && !isConvertedOrClosed))
            {
                dgvInquiries.Cursor = Cursors.Hand;
                return;
            }
        }
        dgvInquiries.Cursor = Cursors.Default;
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
            // Guard: Do not allow converting an already converted or closed inquiry
            if (string.Equals(row.Status, "Converted", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}