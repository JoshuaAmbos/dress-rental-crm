using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class BranchesView : UserControl
{
    private readonly BranchService _branchService;
    private readonly Func<int> _getCompanyId;
    private readonly Action? _onBranchesChanged;

    private List<Branch> _branches = [];
    private SearchBar searchBar = null!;
    private Button btnNewBranch = null!;
    private DataGridView gridBranches = null!;
    private CheckBox chkShowInactive = null!;

    public BranchesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Action? onBranchesChanged = null)
    {
        _branchService = new BranchService(contextFactory ?? throw new ArgumentNullException(nameof(contextFactory)));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _onBranchesChanged = onBranchesChanged;

        BuildLayout();
        _ = LoadBranchesAsync();
    }

    private void BuildLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "Showroom & Branch Locations",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Manage physical boutique showrooms, design studios, and operational inventory hubs.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnNewBranch = new Button
        {
            Text = "+ Add Showroom",
            Size = new Size(150, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlHeader.Width - 150, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnNewBranch.FlatAppearance.BorderSize = 0;
        btnNewBranch.MouseEnter += (s, e) => btnNewBranch.BackColor = ColorAccentHover;
        btnNewBranch.MouseLeave += (s, e) => btnNewBranch.BackColor = ColorAccent;
        btnNewBranch.Click += BtnNewBranch_Click;

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnNewBranch });
        pnlHeader.Resize += (s, e) => btnNewBranch.Location = new Point(pnlHeader.ClientSize.Width - btnNewBranch.Width, 12);

        // Toolbar Strip
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
        searchBar = new SearchBar { Dock = DockStyle.Left, Width = 360, Height = 36 };
        searchBar.SetCueBanner("Search by code, showroom name, city, address...");
        searchBar.SearchTextChanged += async (s, e) => await LoadBranchesAsync();

        chkShowInactive = new CheckBox
        {
            Text = "Include Inactive Showrooms",
            Checked = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Right,
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        chkShowInactive.CheckedChanged += async (s, e) => await LoadBranchesAsync();

        pnlToolbar.Controls.AddRange(new Control[] { searchBar, chkShowInactive });
        var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };

        // Grid Card Container
        var pnlGridCard = new Panel { Dock = DockStyle.Fill, BackColor = ColorCardBg, Padding = new Padding(1) };
        pnlGridCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.2f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

        gridBranches = new DataGridView
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            RowTemplate = { Height = 48 }
        };

        gridBranches.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        gridBranches.ColumnHeadersHeight = 40;
        gridBranches.EnableHeadersVisualStyles = false;

        gridBranches.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            Font = new Font("Segoe UI", 9.5f),
            SelectionBackColor = ColorActivePill,
            SelectionForeColor = ColorPrimary,
            Padding = new Padding(12, 0, 0, 0)
        };

        ConfigureGridColumns();
        gridBranches.CellContentClick += GridBranches_CellContentClick;
        gridBranches.CellPainting += GridBranches_CellPainting;

        pnlGridCard.Controls.Add(gridBranches);

        Controls.AddRange(new Control[] { pnlGridCard, pnlSpacer, pnlToolbar, pnlHeader });
    }

    private void ConfigureGridColumns()
    {
        gridBranches.Columns.Clear();
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "BranchId", Visible = false });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "BranchCode", HeaderText = "CODE", Width = 110 });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "BranchName", HeaderText = "SHOWROOM NAME", FillWeight = 26, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "City", HeaderText = "CITY", FillWeight = 16, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "STREET ADDRESS", FillWeight = 26, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContactPhone", HeaderText = "PHONE", FillWeight = 16, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        gridBranches.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", Width = 95 });

        var btnEditCol = new DataGridViewButtonColumn
        {
            Name = "Edit",
            HeaderText = "",
            Text = "Edit",
            UseColumnTextForButtonValue = true,
            Width = 65,
            FlatStyle = FlatStyle.Flat
        };
        btnEditCol.DefaultCellStyle.ForeColor = ColorAccent;
        gridBranches.Columns.Add(btnEditCol);

        var btnToggleCol = new DataGridViewButtonColumn
        {
            Name = "Toggle",
            HeaderText = "",
            Width = 85,
            FlatStyle = FlatStyle.Flat
        };
        gridBranches.Columns.Add(btnToggleCol);
    }

    public async Task LoadBranchesAsync()
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

        try
        {
            _branches = await _branchService.GetBranchesAsync(_getCompanyId(), searchBar.TextValue, chkShowInactive.Checked);
            gridBranches.Rows.Clear();

            foreach (var b in _branches)
            {
                int rowIndex = gridBranches.Rows.Add(
                    b.BranchId,
                    b.BranchCode,
                    b.BranchName,
                    string.IsNullOrWhiteSpace(b.City) ? "—" : b.City,
                    string.IsNullOrWhiteSpace(b.Address) ? "—" : b.Address,
                    string.IsNullOrWhiteSpace(b.ContactPhone) ? "—" : b.ContactPhone,
                    b.IsActive ? "ACTIVE" : "INACTIVE",
                    "Edit",
                    b.IsActive ? "Deactivate" : "Activate"
                );

                if (!b.IsActive)
                {
                    gridBranches.Rows[rowIndex].DefaultCellStyle.ForeColor = ColorSubtext;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load showrooms: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GridBranches_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == gridBranches.Columns["Status"].Index)
        {
            e.PaintBackground(e.ClipBounds, true);

            string status = e.Value?.ToString() ?? "INACTIVE";
            bool isActive = status == "ACTIVE";

            Color badgeBg = isActive ? ColorBadgeBg : Color.FromArgb(240, 238, 239);
            Color badgeText = isActive ? ColorSuccess : ColorSubtext;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var badgeRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + (e.CellBounds.Height - 22) / 2, 70, 22);
            using var path = CreateRoundedRectangle(badgeRect, 4);
            using var brush = new SolidBrush(badgeBg);
            g.FillPath(brush, path);

            using var font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            TextRenderer.DrawText(g, status, font, badgeRect, badgeText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    private async void GridBranches_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _branches.Count) return;
        var selectedBranch = _branches[e.RowIndex];

        if (e.ColumnIndex == gridBranches.Columns["Edit"].Index)
        {
            using var dlg = new BranchDialogForm(_getCompanyId(), selectedBranch);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                await _branchService.UpdateBranchAsync(dlg.BranchModel);
                await LoadBranchesAsync();
                _onBranchesChanged?.Invoke();
            }
        }
        else if (e.ColumnIndex == gridBranches.Columns["Toggle"].Index)
        {
            string actionWord = selectedBranch.IsActive ? "deactivate" : "activate";
            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionWord} '{selectedBranch.BranchName}' ({selectedBranch.BranchCode})?",
                "Confirm Status Change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                await _branchService.ToggleBranchStatusAsync(selectedBranch.BranchId);
                await LoadBranchesAsync();
                _onBranchesChanged?.Invoke();
            }
        }
    }

    private async void BtnNewBranch_Click(object? sender, EventArgs e)
    {
        using var dlg = new BranchDialogForm(_getCompanyId());
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await _branchService.CreateBranchAsync(dlg.BranchModel);
            await LoadBranchesAsync();
            _onBranchesChanged?.Invoke();
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