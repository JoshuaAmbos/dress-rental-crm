using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class CustomerProfilesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly CustomerProfileService _customerService;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;

    // Primary constructor invoked by MainForm (supports multi-showroom branch filtering)
    public CustomerProfilesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? (() => 1);
        _getBranchId = getBranchId;
        _customerService = new CustomerProfileService(_contextFactory);

        InitializeComponent();
        ApplyThemeTokens();

        if (!DesignMode)
        {
            SetupGridAppearance();
            AddActionColumns();
            WireEvents();
            _ = LoadCustomerDataAsync();
        }
    }

    public CustomerProfilesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
        : this(contextFactory, getCompanyId, null)
    {
    }

    public CustomerProfilesView(Func<int> getCompanyId) : this(() =>
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }, getCompanyId, null)
    {
    }

    // Parameterless constructor for WinForms Designer
    public CustomerProfilesView() : this(() => 1)
    {
    }

    private void CustomerProfilesView_Load(object? sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode)
        {
            _ = LoadCustomerDataAsync();
        }
    }

    private void ApplyThemeTokens()
    {
        DoubleBuffered = true;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);

        if (label1 != null)
        {
            label1.Text = "Client Directory";
            label1.UseMnemonic = false;
            label1.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            label1.ForeColor = ColorPrimary;
        }

        if (searchBar1 != null)
        {
            searchBar1.SetCueBanner("Search by client name, code, contact or email...");
        }

        if (chkShowArchived != null)
        {
            chkShowArchived.Font = new Font("Segoe UI", 9.25f);
            chkShowArchived.ForeColor = ColorNavInactiveText;
            chkShowArchived.Cursor = Cursors.Hand;
        }

        if (primaryButtonNewCustomer != null)
        {
            primaryButtonNewCustomer.Text = "+ New Client";
            primaryButtonNewCustomer.BackColor = ColorAccent;
            primaryButtonNewCustomer.ForeColor = Color.White;
            primaryButtonNewCustomer.FlatStyle = FlatStyle.Flat;
            primaryButtonNewCustomer.FlatAppearance.BorderSize = 0;
            primaryButtonNewCustomer.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            primaryButtonNewCustomer.Cursor = Cursors.Hand;
            primaryButtonNewCustomer.MouseEnter += (s, e) => primaryButtonNewCustomer.BackColor = ColorDustyRoseHover;
            primaryButtonNewCustomer.MouseLeave += (s, e) => primaryButtonNewCustomer.BackColor = ColorAccent;
        }
    }

    private void SetupGridAppearance()
    {
        if (dgvCustomers == null) return;

        dgvCustomers.BackgroundColor = ColorCardBg;
        dgvCustomers.GridColor = ColorDivider;
        dgvCustomers.BorderStyle = BorderStyle.None;
        dgvCustomers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvCustomers.RowHeadersVisible = false;
        dgvCustomers.AllowUserToAddRows = false;
        dgvCustomers.AllowUserToDeleteRows = false;
        dgvCustomers.AllowUserToResizeRows = false;
        dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCustomers.MultiSelect = false;
        dgvCustomers.RowTemplate.Height = 42;

        // Modern flat column headers
        dgvCustomers.EnableHeadersVisualStyles = false;
        dgvCustomers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvCustomers.ColumnHeadersHeight = 42;
        dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = ColorCardBg;
        dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = ColorMutedLabel;
        dgvCustomers.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorCardBg;
        dgvCustomers.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorMutedLabel;
        dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

        // Standardized row styles
        dgvCustomers.DefaultCellStyle.BackColor = ColorCardBg;
        dgvCustomers.DefaultCellStyle.ForeColor = ColorBrandDark;
        dgvCustomers.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
        dgvCustomers.DefaultCellStyle.SelectionBackColor = ColorSelectedBg;
        dgvCustomers.DefaultCellStyle.SelectionForeColor = ColorPrimary;

        dgvCustomers.AlternatingRowsDefaultCellStyle.BackColor = ColorBgSoft;
        dgvCustomers.AlternatingRowsDefaultCellStyle.ForeColor = ColorBrandDark;
        dgvCustomers.AlternatingRowsDefaultCellStyle.SelectionBackColor = ColorSelectedBg;
        dgvCustomers.AlternatingRowsDefaultCellStyle.SelectionForeColor = ColorPrimary;

        // Custom render flat action pills
        dgvCustomers.CellPainting += DgvCustomers_CellPainting;
    }

    private void WireEvents()
    {
        if (primaryButtonNewCustomer != null)
            primaryButtonNewCustomer.Click += BtnNewCustomer_Click;

        if (dgvCustomers != null)
        {
            dgvCustomers.CellContentClick -= DgvCustomers_CellContentClick;
            dgvCustomers.CellContentClick += DgvCustomers_CellContentClick;
        }

        if (chkShowArchived != null)
            chkShowArchived.CheckedChanged += ChkShowArchived_CheckedChanged;

        if (searchBar1 != null)
            searchBar1.SearchTextChanged += SearchBar1_SearchTextChanged;
    }

    private void AddActionColumns()
    {
        if (dgvCustomers == null) return;

        dgvCustomers.Columns.Clear();
        dgvCustomers.AutoGenerateColumns = false;

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Code),
            HeaderText = "CODE",
            FillWeight = 85,
            MinimumWidth = 80,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Name),
            HeaderText = "CLIENT NAME",
            FillWeight = 135,
            MinimumWidth = 110,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Phone),
            HeaderText = "PHONE",
            FillWeight = 95,
            MinimumWidth = 90,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Email),
            HeaderText = "EMAIL ADDRESS",
            FillWeight = 135,
            MinimumWidth = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Address),
            HeaderText = "CITY / ADDRESS",
            FillWeight = 110,
            MinimumWidth = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.BustSize),
            HeaderText = "BUST",
            FillWeight = 70,
            MinimumWidth = 65,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.WaistSize),
            HeaderText = "WAIST",
            FillWeight = 70,
            MinimumWidth = 65,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.HipSize),
            HeaderText = "HIPS",
            FillWeight = 70,
            MinimumWidth = 65,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        var colEdit = new DataGridViewButtonColumn
        {
            Name = "colEdit",
            HeaderText = "ACTIONS",
            Text = "Edit",
            UseColumnTextForButtonValue = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
            Width = 76,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        var colArchive = new DataGridViewButtonColumn
        {
            Name = "colArchive",
            HeaderText = "",
            Text = "Archive",
            UseColumnTextForButtonValue = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
            Width = 84,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        dgvCustomers.Columns.Add(colEdit);
        dgvCustomers.Columns.Add(colArchive);
    }

    private void DgvCustomers_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        string colName = dgvCustomers.Columns[e.ColumnIndex].Name;
        if (colName != "colEdit" && colName != "colArchive") return;

        e.PaintBackground(e.CellBounds, true);

        bool isEdit = (colName == "colEdit");
        bool showingArchived = chkShowArchived?.Checked ?? false;

        string buttonText = isEdit ? "Edit" : (showingArchived ? "Restore" : "Archive");
        Color btnBg = isEdit
            ? ColorActivePill
            : (showingArchived ? ColorBadgeBg : ColorCloseBtnBg);
        Color btnFg = isEdit
            ? ColorAccent
            : (showingArchived ? ColorSuccess : ColorSubtext);

        var btnRect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 7, e.CellBounds.Width - 8, e.CellBounds.Height - 14);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(btnBg);
        using var pen = new Pen(isEdit ? ColorBorder : Color.Transparent, 1f);
        using var path = CreateRoundedRectangle(btnRect, 6);

        e.Graphics.FillPath(brush, path);
        if (isEdit) e.Graphics.DrawPath(pen, path);

        TextRenderer.DrawText(
            e.Graphics,
            buttonText,
            new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            btnRect,
            btnFg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        e.Handled = true;
    }

    private async void DgvCustomers_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvCustomers.Rows.Count) return;

        var clickedColumn = dgvCustomers.Columns[e.ColumnIndex]?.Name;
        if (clickedColumn == null) return;

        var row = dgvCustomers.Rows[e.RowIndex];
        if (row.DataBoundItem is not CustomerRowViewModel item) return;

        if (clickedColumn == "colEdit")
        {
            OnEditCustomer(item.CustomerEntity);
        }
        else if (clickedColumn == "colArchive")
        {
            if (item.IsActive)
            {
                await OnArchiveCustomerAsync(item);
            }
            else
            {
                await OnRestoreCustomerAsync(item);
            }
        }
    }

    private async void OnEditCustomer(Customer customer)
    {
        using var dialog = new CustomerDialogForm(_contextFactory, _getCompanyId(), customer, customer.BranchId);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
    }

    private async Task OnArchiveCustomerAsync(CustomerRowViewModel item)
    {
        var confirm = MessageBox.Show(
            $"Archive client profile for '{item.Name}'?\n\nTheir records will be preserved, but hidden from active lists.",
            "Archive Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _customerService.ArchiveCustomerAsync(item.CustomerId);
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.GetBaseException().Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnRestoreCustomerAsync(CustomerRowViewModel item)
    {
        var confirm = MessageBox.Show(
            $"Restore and reactivate client '{item.Name}'?",
            "Restore Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _customerService.RestoreCustomerAsync(item.CustomerId);
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.GetBaseException().Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChkShowArchived_CheckedChanged(object? sender, EventArgs e)
    {
        dgvCustomers?.Invalidate();
        _ = LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
    }

    public async Task LoadCustomerDataAsync(string search = "")
    {
        if (DesignMode) return;

        try
        {
            int companyId = _getCompanyId();
            int? branchId = _getBranchId?.Invoke();
            bool showArchived = chkShowArchived?.Checked ?? false;

            var displayList = await _customerService.GetCustomersAsync(companyId, branchId, showArchived, search);

            if (dgvCustomers != null)
            {
                dgvCustomers.DataSource = displayList;
                dgvCustomers.ClearSelection();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customer profiles: {ex.GetBaseException().Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnNewCustomer_Click(object? sender, EventArgs e)
    {
        int? currentBranchId = _getBranchId?.Invoke();
        using var dialog = new CustomerDialogForm(_contextFactory, _getCompanyId(), null, currentBranchId);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
    }

    private void SearchBar1_SearchTextChanged(object? sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            _ = LoadCustomerDataAsync(searchBar1.TextValue.Trim());
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