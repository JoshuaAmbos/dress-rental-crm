using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using CRM.winforms.Controls.RentalReturnControls;
using CRM.winforms.Forms;
using CRM.winforms.Models.RentalBookingModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CRM.winforms.Views;

public partial class RentalBookingsView : UserControl
{
    private readonly RentalBookingController _controller;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;

    private string _currentStageFilter = "All";
    private string _currentSearchTerm = string.Empty;
    private List<BookingRowViewModel> _cachedRows = [];
    private RentalPipelineDto? _pipelineData;

    private FlowLayoutPanel pnlFilterTabs = null!;
    private TextBox txtSearch = null!;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    public event EventHandler? RequestNewBooking;

    public RentalBookingsView()
    {
        InitializeComponent();

        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(options);
        };
        _getCompanyId = () => 1;
        _controller = new RentalBookingController(_contextFactory);

        SetupFilterBar();
        WireGridEvents();
    }

    public RentalBookingsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _controller = new RentalBookingController(_contextFactory);

        SetupFilterBar();
        WireGridEvents();
    }

    private void SetupFilterBar()
    {
        int filterY = 286;
        int filterHeight = 38;

        dgvBookings.Location = new Point(33, filterY + filterHeight + 10);
        dgvBookings.Height = ClientSize.Height - dgvBookings.Top - 30;

        var pnlFilterStrip = new Panel
        {
            Location = new Point(33, filterY),
            Size = new Size(dgvBookings.Width, filterHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        var pnlSearch = new Panel
        {
            Dock = DockStyle.Right,
            Width = 260,
            Height = 34,
            BackColor = Color.White
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            Location = new Point(10, 8),
            Width = 240,
            PlaceholderText = "Search bookings..."
        };
        txtSearch.TextChanged += async (s, e) =>
        {
            _currentSearchTerm = txtSearch.Text.Trim();
            await LoadBookingsAsync();
        };
        pnlSearch.Controls.Add(txtSearch);

        pnlFilterTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        pnlFilterStrip.Controls.Add(pnlFilterTabs);
        pnlFilterStrip.Controls.Add(pnlSearch);
        Controls.Add(pnlFilterStrip);
        pnlFilterStrip.BringToFront();
    }

    private void WireGridEvents()
    {
        dgvBookings.CellPainting += DgvBookings_CellPainting;
        dgvBookings.CellClick += DgvBookings_CellClick;
        dgvBookings.CellDoubleClick += DgvBookings_CellDoubleClick;
        dgvBookings.CellMouseMove += DgvBookings_CellMouseMove;
    }

    private async void RentalBookingsView_Load(object? sender, EventArgs e)
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        await LoadBookingsAsync();
    }

    public async Task LoadBookingsAsync()
    {
        try
        {
            // Call through the MVC Controller layer instead of service directly
            _pipelineData = await _controller.LoadPipelineAsync(_getCompanyId(), _currentStageFilter, _currentSearchTerm);

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
                Color.FromArgb(186, 105, 115),
                Color.FromArgb(255, 241, 242),
                Color.FromArgb(186, 105, 115));

            RenderFilterTabs();

            _cachedRows = _pipelineData.Rows;
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
                BackColor = isSelected ? ColorDustyRose : Color.White,
                ForeColor = isSelected ? Color.White : ColorEspresso
            };

            btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            btn.FlatAppearance.BorderColor = ColorBorder;

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
                    // Call controller method instead of service directly
                    await _controller.ProcessReturnAsync(item.BookingId);
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

    private void label1_Click(object? sender, EventArgs e) { }
}