using System.Drawing.Drawing2D;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Services.RentalBookingServices;

namespace CRM.winforms.Forms;

public partial class BookingDetailsDialog : Form
{
    private readonly int _bookingId;
    private readonly RentalBookingService _bookingService;
    private RentalBooking? _booking;

    // UI Controls
    private Label lblBookingCode = null!;
    private Label lblClient = null!;
    private Label lblDates = null!;
    private Label lblGarments = null!;
    private Label lblTotal = null!;
    private Label lblPayment = null!;
    private Label lblAlterations = null!;
    private ComboBox cmbStage = null!;
    private Button btnSaveStage = null!;
    private Button btnClose = null!;
    private Button btnQuickAction = null!;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorDialogBg = Color.FromArgb(249, 241, 241);

    public BookingDetailsDialog(int bookingId, Func<TenantCrmDbContext> contextFactory)
    {
        _bookingId = bookingId;
        _bookingService = new RentalBookingService(contextFactory);

        InitializeComponentTree();
        _ = LoadDetailsAsync();
    }

    private void InitializeComponentTree()
    {
        Text = "Rental Booking Lifecycle & Status";
        Size = new Size(620, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorDialogBg;
        Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 16, 24, 8), BackColor = Color.White };
        lblBookingCode = new Label
        {
            Text = $"Booking BKG-{_bookingId:D4}",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblBookingCode);

        // Bottom Actions Panel
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(20, 12, 20, 12), BackColor = Color.White };

        btnClose = new Button
        {
            Text = "Close",
            Size = new Size(100, 38),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 120, 13),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderColor = ColorBorder;
        btnClose.Click += (s, e) => DialogResult = DialogResult.OK;

        btnSaveStage = new Button
        {
            Text = "Update Status",
            Size = new Size(130, 38),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 260, 13),
            Cursor = Cursors.Hand
        };
        btnSaveStage.FlatAppearance.BorderSize = 0;
        btnSaveStage.Click += async (s, e) => await SaveStageAsync();

        btnQuickAction = new Button
        {
            Text = "Quick Action",
            Size = new Size(160, 38),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(37, 99, 235),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Location = new Point(20, 13),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnQuickAction.FlatAppearance.BorderSize = 0;
        btnQuickAction.Click += async (s, e) => await HandleQuickActionAsync();

        pnlBottom.Controls.AddRange([btnClose, btnSaveStage, btnQuickAction]);

        // Main Center Content Card
        var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16) };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = ColorCardBg, Padding = new Padding(20) };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1.25f);
            using var path = CreateRoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        };

        // Layout rows
        var lblStatusTag = new Label { Text = "CHANGE LIFECYCLE STAGE", Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(20, 18), AutoSize = true };
        cmbStage = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            Location = new Point(20, 40),
            Size = new Size(300, 32)
        };
        cmbStage.Items.AddRange(["Fitting", "Reserved", "Active", "Returned", "Cancelled"]);

        int startY = 88;
        int rowGap = 34;

        AddDetailPair(card, "Client", out lblClient, ref startY, rowGap);
        AddDetailPair(card, "Rental Period", out lblDates, ref startY, rowGap);
        AddDetailPair(card, "Assigned Garments", out lblGarments, ref startY, rowGap);
        AddDetailPair(card, "Total Amount", out lblTotal, ref startY, rowGap);
        AddDetailPair(card, "Payment Method", out lblPayment, ref startY, rowGap);
        AddDetailPair(card, "Alteration Notes", out lblAlterations, ref startY, rowGap);

        card.Controls.AddRange([lblStatusTag, cmbStage]);
        pnlContent.Controls.Add(card);

        Controls.Add(pnlContent);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);
    }

    private void AddDetailPair(Panel parent, string title, out Label valLabel, ref int y, int gap)
    {
        var lblTag = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Location = new Point(20, y),
            Width = 140
        };

        valLabel = new Label
        {
            Text = "Loading...",
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(160, y),
            AutoSize = true,
            MaximumSize = new Size(360, 60)
        };

        parent.Controls.Add(lblTag);
        parent.Controls.Add(valLabel);
        y += gap;
    }

    private async Task LoadDetailsAsync()
    {
        _booking = await _bookingService.GetBookingDetailsAsync(_bookingId);
        if (_booking == null)
        {
            MessageBox.Show("Unable to find booking records.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        lblBookingCode.Text = $"Booking BKG-{_booking.RentalBookingId:D4}";

        if (_booking.Customer != null)
            lblClient.Text = $"{_booking.Customer.FirstName} {_booking.Customer.LastName} ({_booking.Customer.ContactNumber ?? "No Phone"})";

        lblDates.Text = $"{_booking.RentalStartDate:MMM dd, yyyy} → {_booking.RentalEndDate:MMM dd, yyyy}";

        if (_booking.BookingDetails.Count > 0)
        {
            lblGarments.Text = string.Join("\n", _booking.BookingDetails.Select(d =>
                d.Garment != null ? $"• {d.Garment.StyleName} ({d.Garment.ItemCode} · Size {d.Garment.Size})" : $"• Garment #{d.GarmentId}"));
        }
        else
        {
            lblGarments.Text = "No garments associated.";
        }

        lblTotal.Text = $"${_booking.TotalAmount:N0} (Fee: ${_booking.RentalFee:N0} | Dep: ${_booking.SecurityDeposit:N0})";
        lblPayment.Text = _booking.PaymentMethod ?? "Not specified";
        lblAlterations.Text = string.IsNullOrWhiteSpace(_booking.AlterationNotes) ? "None" : _booking.AlterationNotes;

        cmbStage.SelectedItem = _booking.BookingStage;

        ConfigureQuickAction(_booking.BookingStage);
    }

    private void ConfigureQuickAction(string stage)
    {
        switch (stage)
        {
            case "Fitting":
            case "Reserved":
                btnQuickAction.Text = "Mark Active (Picked Up)";
                btnQuickAction.BackColor = Color.FromArgb(37, 99, 235); // Blue
                btnQuickAction.Visible = true;
                break;
            case "Active":
                btnQuickAction.Text = "Process Return";
                btnQuickAction.BackColor = ColorDustyRose;
                btnQuickAction.Visible = true;
                break;
            default:
                btnQuickAction.Visible = false;
                break;
        }
    }

    private async Task HandleQuickActionAsync()
    {
        if (_booking == null) return;

        string targetStage = _booking.BookingStage switch
        {
            "Fitting" or "Reserved" => "Active",
            "Active" => "Returned",
            _ => _booking.BookingStage
        };

        if (targetStage != _booking.BookingStage)
        {
            cmbStage.SelectedItem = targetStage;
            await SaveStageAsync();
        }
    }

    private async Task SaveStageAsync()
    {
        if (cmbStage.SelectedItem is not string newStage) return;

        try
        {
            btnSaveStage.Enabled = false;
            btnSaveStage.Text = "Saving...";

            await _bookingService.UpdateBookingStageAsync(_bookingId, newStage);

            MessageBox.Show($"Booking status updated to '{newStage}' successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update booking stage: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSaveStage.Enabled = true;
            btnSaveStage.Text = "Update Status";
        }
    }

    private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
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