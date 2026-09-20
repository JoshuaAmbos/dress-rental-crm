using CRM.infrastructure.data;
using CRM.winforms.Controls.RentalReturnControls;
using CRM.winforms.Forms;
using CRM.winforms.Models.RentalBookingModels;
using CRM.winforms.Services.RentalBookingServices;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CRM.winforms.Views;

public partial class RentalBookingsView : UserControl
{
    private readonly RentalBookingService _bookingService;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;

    private string _currentStageFilter = "All";
    private string _currentSearchTerm = string.Empty;
    private List<BookingRowViewModel> _cachedRows = [];

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
        _bookingService = new RentalBookingService(_contextFactory);

        WireGridEvents();
    }

    public RentalBookingsView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _bookingService = new RentalBookingService(_contextFactory);

        WireGridEvents();
    }

    private void WireGridEvents()
    {
        dgvBookings.CellPainting += DgvBookings_CellPainting;
        dgvBookings.CellClick += DgvBookings_CellClick;
        dgvBookings.CellMouseMove += DgvBookings_CellMouseMove;
    }

    private async void RentalBookingsView_Load(object? sender, EventArgs e)
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        await LoadBookingsAsync();
    }

    private void PrimaryButtonNewBooking_Click(object? sender, EventArgs e)
    {
        // If MainForm is handling navigation via event, notify it:
        if (RequestNewBooking != null)
        {
            RequestNewBooking.Invoke(this, EventArgs.Empty);
            return;
        }

        // Direct host swap fallback: replaces RentalBookingsView inside its parent container
        if (Parent is Control container)
        {
            var wizard = new NewBookingWizardView(_contextFactory, _getCompanyId, async () =>
            {
                // Callback executed when wizard is cancelled or completed:
                container.SuspendLayout();
                container.Controls.Clear();
                this.Dock = DockStyle.Fill;
                container.Controls.Add(this);
                container.ResumeLayout();

                // Refresh the pipeline and KPI cards with any newly created booking
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

    public async Task LoadBookingsAsync()
    {
        try
        {
            var pipeline = await _bookingService.GetPipelineAsync(_getCompanyId(), _currentStageFilter, _currentSearchTerm);

            // Populate KPI Cards
            SetKpiCard(kpiCardControlActiveLeases, "ACTIVE LEASES", pipeline.ActiveCount.ToString(), "Currently booked or out");
            SetKpiCard(kpiCardControlOverdue, "OVERDUE RETURNS", pipeline.OverdueCount.ToString(), "Action required immediately");
            SetKpiCard(kpiCardControlUpcomingReturns, "UPCOMING RETURNS", pipeline.UpcomingCount.ToString(), "Due in the next 3 days");

            _cachedRows = pipeline.Rows;
            dgvBookings.DataSource = _cachedRows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load rentals pipeline: {ex.GetBaseException().Message}", "Data Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
                    $"Process return for booking {item.BookingCode} ({item.ClientName})?\n\nThis will mark garments as returned and calculate any deposit releases.",
                    "Process Garment Return",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    await _bookingService.ProcessReturnAsync(item.BookingId);
                    await LoadBookingsAsync();
                }
            }
            else
            {
                MessageBox.Show($"Booking {item.BookingCode} details:\nClient: {item.ClientName}\nItems: {item.GarmentSummary}", "Booking Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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

    private static void SetKpiCard(Control? card, string title, string value, string subtitle)
    {
        if (card == null) return;
        var type = card.GetType();
        type.GetProperty("Title")?.SetValue(card, title);
        type.GetProperty("KpiTitle")?.SetValue(card, title);
        type.GetProperty("Value")?.SetValue(card, value);
        type.GetProperty("KpiValue")?.SetValue(card, value);
        type.GetProperty("Subtitle")?.SetValue(card, subtitle);
        type.GetProperty("KpiSubtitle")?.SetValue(card, subtitle);
        card.Invalidate();
    }

    private void label1_Click(object? sender, EventArgs e) { }
}