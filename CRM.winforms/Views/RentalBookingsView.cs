using CRM.infrastructure.data;
using CRM.winforms.Controls.RentalBookingControls;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class    RentalBookingsView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly RentalBookingController _controller;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;

    private string _currentStageFilter = "All";
    private string _currentSearchTerm = string.Empty;
    private List<BookingRowViewModel> _cachedRows = [];
    private RentalPipelineDto? _pipelineData;

    public event EventHandler? RequestNewBooking;

    // Parameterless constructor for WinForms Designer
    public RentalBookingsView() : this(() =>
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }, () => 1, null)
    {
    }

    public RentalBookingsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
        : this(contextFactory, getCompanyId, null)
    {
    }

    // Primary constructor invoked by MainForm (supports multi-showroom branch filtering)
    public RentalBookingsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
        _controller = new RentalBookingController(_contextFactory);

        InitializeComponent();
        ApplyThemeTokens();

        if (!DesignMode)
        {
            SetupGridAppearance();
            WireGridEvents();
        }
    }

    private void RentalBookingsView_Load(object? sender, EventArgs e)
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _ = LoadBookingsAsync();
    }

    private void ApplyThemeTokens()
    {
        DoubleBuffered = true;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);

        if (label1 != null)
        {
            label1.Text = "Rental & Returns Pipeline";
            label1.UseMnemonic = false;
            label1.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            label1.ForeColor = ColorPrimary;
        }

        if (lblSubtitle != null)
        {
            lblSubtitle.Text = "Monitor reservations, ongoing fittings, active leases, and returns.";
            lblSubtitle.Font = new Font("Segoe UI", 9.5f);
            lblSubtitle.ForeColor = ColorSubtext;
        }

        if (primaryButtonNewBooking != null)
        {
            primaryButtonNewBooking.Text = "+ New Booking";
            primaryButtonNewBooking.BackColor = ColorAccent;
            primaryButtonNewBooking.ForeColor = Color.White;
            primaryButtonNewBooking.FlatStyle = FlatStyle.Flat;
            primaryButtonNewBooking.FlatAppearance.BorderSize = 0;
            primaryButtonNewBooking.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            primaryButtonNewBooking.Cursor = Cursors.Hand;
            primaryButtonNewBooking.MouseEnter += (s, e) => primaryButtonNewBooking.BackColor = ColorDustyRoseHover;
            primaryButtonNewBooking.MouseLeave += (s, e) => primaryButtonNewBooking.BackColor = ColorAccent;
        }

        if (txtSearch != null)
        {
            txtSearch.Font = new Font("Segoe UI", 9.5f);
            txtSearch.ForeColor = ColorBrandDark;
            txtSearch.PlaceholderText = "Search by client, garment, or code...";
            txtSearch.TextChanged += async (s, e) =>
            {
                _currentSearchTerm = txtSearch.Text.Trim();
                await LoadBookingsAsync();
            };
        }
    }

    private void SetupGridAppearance()
    {
        if (dgvBookings == null) return;

        dgvBookings.AutoGenerateColumns = false;
        dgvBookings.BackgroundColor = ColorCardBg;
        dgvBookings.GridColor = ColorDivider;
        dgvBookings.BorderStyle = BorderStyle.None;
        dgvBookings.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvBookings.RowHeadersVisible = false;
        dgvBookings.AllowUserToAddRows = false;
        dgvBookings.AllowUserToDeleteRows = false;
        dgvBookings.AllowUserToResizeRows = false;
        dgvBookings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvBookings.MultiSelect = false;
        dgvBookings.RowTemplate.Height = 44;

        // Header Styling
        dgvBookings.EnableHeadersVisualStyles = false;
        dgvBookings.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvBookings.ColumnHeadersHeight = 42;
        dgvBookings.ColumnHeadersDefaultCellStyle.BackColor = ColorCardBg;
        dgvBookings.ColumnHeadersDefaultCellStyle.ForeColor = ColorMutedLabel;
        dgvBookings.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorCardBg;
        dgvBookings.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorMutedLabel;
        dgvBookings.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

        // Row Styling
        dgvBookings.DefaultCellStyle.BackColor = ColorCardBg;
        dgvBookings.DefaultCellStyle.ForeColor = ColorBrandDark;
        dgvBookings.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
        dgvBookings.DefaultCellStyle.SelectionBackColor = ColorSelectedBg;
        dgvBookings.DefaultCellStyle.SelectionForeColor = ColorPrimary;

        dgvBookings.AlternatingRowsDefaultCellStyle.BackColor = ColorBgSoft;
        dgvBookings.AlternatingRowsDefaultCellStyle.ForeColor = ColorBrandDark;
        dgvBookings.AlternatingRowsDefaultCellStyle.SelectionBackColor = ColorSelectedBg;
        dgvBookings.AlternatingRowsDefaultCellStyle.SelectionForeColor = ColorPrimary;
    }

    private void WireGridEvents()
    {
        dgvBookings.CellPainting += DgvBookings_CellPainting;
        dgvBookings.CellClick += DgvBookings_CellClick;
        dgvBookings.CellDoubleClick += DgvBookings_CellDoubleClick;
        dgvBookings.CellMouseMove += DgvBookings_CellMouseMove;
    }

    public async Task LoadBookingsAsync()
    {
        try
        {
            int? branchId = _getBranchId?.Invoke();
            _pipelineData = await _controller.LoadPipelineAsync(_getCompanyId(), branchId, _currentStageFilter, _currentSearchTerm);

            kpiCardControlActiveLeases?.SetData(
                "ACTIVE LEASES",
                _pipelineData.ActiveCount.ToString(),
                "In circulation",
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(239, 246, 255),
                Color.FromArgb(29, 78, 216));

            kpiCardControlOverdue?.SetData(
                "OVERDUE RETURNS",
                _pipelineData.OverdueCount.ToString(),
                "Requires action",
                Color.FromArgb(220, 38, 38),
                Color.FromArgb(254, 242, 242),
                Color.FromArgb(185, 28, 28));

            kpiCardControlUpcomingReturns?.SetData(
                "UPCOMING RETURNS (7 DAYS)",
                _pipelineData.UpcomingCount.ToString(),
                "Due this week",
                ColorAccent,
                ColorActivePill,
                ColorAccent);

            RenderFilterTabs();

            _cachedRows = _pipelineData.Rows;
            dgvBookings.AutoGenerateColumns = false;
            dgvBookings.DataSource = null;
            dgvBookings.DataSource = _cachedRows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load rentals pipeline: {ex.GetBaseException().Message}", "Data Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderFilterTabs()
    {
        pnlFilterTabs.SuspendLayout();
        pnlFilterTabs.Controls.Clear();

        var tabs = new (string Stage, int Count)[]
        {
            ("All", _pipelineData?.TotalCount ?? 0),
            ("Reserved", _pipelineData?.ReservedCount ?? 0),
            ("Fitting", _pipelineData?.FittingCount ?? 0),
            ("Active", _pipelineData?.ActiveCount ?? 0),
            ("Overdue", _pipelineData?.OverdueCount ?? 0),
            ("Returned", _pipelineData?.ReturnedCount ?? 0)
        };

        foreach (var (stage, count) in tabs)
        {
            bool isSelected = string.Equals(_currentStageFilter, stage, StringComparison.OrdinalIgnoreCase);

            var btn = new Button
            {
                Text = stage == "All" ? "All" : $"{stage}  {count}",
                AutoSize = true,
                Height = 32,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                BackColor = isSelected ? ColorAccent : ColorCardBg,
                ForeColor = isSelected ? Color.White : ColorNavInactiveText
            };

            btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            btn.FlatAppearance.BorderColor = isSelected ? ColorAccent : ColorBorder;

            btn.Click += async (s, e) =>
            {
                _currentStageFilter = stage;
                await LoadBookingsAsync();
            };

            pnlFilterTabs.Controls.Add(btn);
        }

        pnlFilterTabs.ResumeLayout();
    }

    private void DgvBookings_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _cachedRows.Count || e.ColumnIndex < 0 || e.Graphics == null)
            return;

        var column = dgvBookings.Columns[e.ColumnIndex];
        if (column != null)
        {
            RentalGridCellPainter.PaintCell(e, _cachedRows[e.RowIndex], column.Name);
        }
    }

    private async void DgvBookings_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _cachedRows.Count || e.ColumnIndex < 0) return;

        var colName = dgvBookings.Columns[e.ColumnIndex].Name;
        if (colName is "actionDataGridViewTextBoxColumn" or "ColAction")
        {
            var item = _cachedRows[e.RowIndex];

            if (item.Stage is "Active" or "Overdue")
            {
                var confirm = MessageBox.Show(
                    $"Process return for booking {item.BookingCode} ({item.ClientName})?\n\nThis will mark garments as returned and transfer them to cleaning.",
                    "Process Garment Return",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    var result = await _controller.ProcessReturnAsync(item.BookingId);

                    if (result.WasOverdue)
                    {
                        MessageBox.Show(
                            $"Garment returned successfully.\n\n⚠️ OVERDUE NOTICE:\nThis lease was {result.DaysLate} day(s) overdue.\n" +
                            $"A penalty fee of ₱{result.LateFeeCharged:N2} was automatically assessed based on the boutique configuration.",
                            "Overdue Return Penalty Assessed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Garment returned successfully and transferred to sanitization queue.", "Return Processed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    await LoadBookingsAsync();
                }
            }
            else
            {
                OpenBookingDetailsDialog(item.BookingId);
            }
        }
    }

    private void DgvBookings_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _cachedRows.Count) return;

        var item = _cachedRows[e.RowIndex];
        OpenBookingDetailsDialog(item.BookingId);
    }

    private async void OpenBookingDetailsDialog(int bookingId)
    {
        using var dialog = new BookingDetailsDialog(bookingId, _contextFactory);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadBookingsAsync();
        }
    }

    private void DgvBookings_CellMouseMove(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var colName = dgvBookings.Columns[e.ColumnIndex].Name;
            if (colName is "actionDataGridViewTextBoxColumn" or "ColAction")
            {
                dgvBookings.Cursor = Cursors.Hand;
                return;
            }
        }
        dgvBookings.Cursor = Cursors.Default;
    }

    private void PrimaryButtonNewBooking_Click(object? sender, EventArgs e)
    {
        if (RequestNewBooking != null)
        {
            RequestNewBooking.Invoke(this, EventArgs.Empty);
            return;
        }

        if (Parent is Control container)
        {
            var wizard = new NewBookingWizardView(_contextFactory, _getCompanyId, async () =>
            {
                container.SuspendLayout();
                container.Controls.Clear();
                this.Dock = DockStyle.Fill;
                container.Controls.Add(this);
                container.ResumeLayout();

                await LoadBookingsAsync();
            })
            {
                Dock = DockStyle.Fill
            };

            container.SuspendLayout();
            container.Controls.Clear();
            container.Controls.Add(wizard);
            container.ResumeLayout();
        }
    }
}