using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Services.RentalBookingServices;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Views;

public partial class Step2GarmentDatesView : UserControl, IBookingWizardStep
{
    private readonly Func<TenantCrmDbContext>? _contextFactory;
    private readonly RentalBookingService? _bookingService;
    private readonly Func<int>? _getCompanyId;

    private BookingDraftModel? _draft;
    private List<GarmentPickerRowViewModel> _garments = new();
    private readonly HashSet<int> _selectedGarmentIds = new();
    private bool _isUpdatingDates = false;

    private Label lblSelectedCustomer = null!;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorSelectedBg = Color.FromArgb(254, 250, 250);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    private const string CurrencySymbol = "₱";

    public string StepTitle => "Select Garment & Dates";

    public Step2GarmentDatesView()
    {
        InitializeComponent();
        ConfigureLayout();
        WireEvents();
    }

    public Step2GarmentDatesView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _bookingService = new RentalBookingService(_contextFactory);

        ConfigureLayout();
        WireEvents();
    }

    private void ConfigureLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);

        if (lblTitle != null)
        {
            lblTitle.Text = "Select Garment & Dates";
            lblTitle.Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold);
            lblTitle.ForeColor = ColorEspresso;
            lblTitle.Location = new Point(32, 20);
            lblTitle.AutoSize = true;
        }

        lblSelectedCustomer = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            Size = new Size(650, 30),
            Location = new Point(Math.Max(32, Width - 682), 22),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Text = "Selected Customer: —"
        };
        Controls.Add(lblSelectedCustomer);
        lblSelectedCustomer.BringToFront();

        if (lblStartDate != null)
        {
            lblStartDate.Text = "Rental Start Date";
            lblStartDate.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblStartDate.ForeColor = ColorSubtext;
            lblStartDate.Location = new Point(32, 64);
            lblStartDate.AutoSize = true;
        }

        if (dtpStartDate != null)
        {
            dtpStartDate.Location = new Point(32, 86);
            dtpStartDate.Size = new Size(240, 30);
            dtpStartDate.Font = new Font("Segoe UI", 9.5f);
            dtpStartDate.Format = DateTimePickerFormat.Short;
        }

        if (lblEndDate != null)
        {
            lblEndDate.Text = "Rental End Date";
            lblEndDate.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblEndDate.ForeColor = ColorSubtext;
            lblEndDate.Location = new Point(296, 64);
            lblEndDate.AutoSize = true;
        }

        if (dtpEndDate != null)
        {
            dtpEndDate.Location = new Point(296, 86);
            dtpEndDate.Size = new Size(240, 30);
            dtpEndDate.Font = new Font("Segoe UI", 9.5f);
            dtpEndDate.Format = DateTimePickerFormat.Short;
        }

        if (lblAvailableGarments != null)
        {
            lblAvailableGarments.Text = "AVAILABLE GARMENTS";
            lblAvailableGarments.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            lblAvailableGarments.ForeColor = ColorSubtext;
            lblAvailableGarments.Location = new Point(32, 134);
            lblAvailableGarments.AutoSize = true;
        }

        if (listBoxGarments != null)
        {
            listBoxGarments.Location = new Point(32, 158);
            listBoxGarments.Size = new Size(Width - 64, Height - 180);
            listBoxGarments.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxGarments.BackColor = ColorViewBg;
            listBoxGarments.BorderStyle = BorderStyle.None;
            listBoxGarments.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxGarments.ItemHeight = 68;
            listBoxGarments.IntegralHeight = false;
        }
    }

    private void WireEvents()
    {
        if (listBoxGarments != null)
        {
            listBoxGarments.DrawItem -= ListBoxGarments_DrawItem;
            listBoxGarments.DrawItem += ListBoxGarments_DrawItem;

            listBoxGarments.MouseClick -= ListBoxGarments_MouseClick;
            listBoxGarments.MouseClick += ListBoxGarments_MouseClick;
        }

        if (dtpStartDate != null)
        {
            dtpStartDate.ValueChanged -= DtpStartDate_ValueChanged;
            dtpStartDate.ValueChanged += DtpStartDate_ValueChanged;
        }

        if (dtpEndDate != null)
        {
            dtpEndDate.ValueChanged -= DtpEndDate_ValueChanged;
            dtpEndDate.ValueChanged += DtpEndDate_ValueChanged;
        }
    }

    private async void DtpStartDate_ValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingDates || dtpStartDate == null) return;

        try
        {
            _isUpdatingDates = true;
            if (dtpEndDate != null && dtpEndDate.Value.Date < dtpStartDate.Value.Date)
            {
                dtpEndDate.Value = dtpStartDate.Value.Date.AddDays(3);
            }
        }
        finally
        {
            _isUpdatingDates = false;
        }

        await LoadAvailableGarmentsAsync();
    }

    private async void DtpEndDate_ValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingDates) return;
        await LoadAvailableGarmentsAsync();
    }

    public async Task LoadAvailableGarmentsAsync()
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime || _bookingService == null || _getCompanyId == null)
            return;

        try
        {
            DateTime start = dtpStartDate?.Value.Date ?? DateTime.Today;
            DateTime end = dtpEndDate?.Value.Date ?? DateTime.Today.AddDays(3);

            if (end < start) end = start.AddDays(3);

            // Query only garments free of overlapping bookings in this exact date range
            _garments = await _bookingService.GetAvailableGarmentsAsync(_getCompanyId(), start, end);

            // Prune any previously selected garments that are no longer available in this date range
            var availableIds = _garments.Select(g => g.GarmentId).ToHashSet();
            _selectedGarmentIds.IntersectWith(availableIds);

            listBoxGarments.BeginUpdate();
            listBoxGarments.Items.Clear();

            foreach (var item in _garments)
            {
                listBoxGarments.Items.Add(item);
            }

            listBoxGarments.EndUpdate();
            UpdateSectionHeader();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to query available garments: {ex.GetBaseException().Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ListBoxGarments_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= listBoxGarments.Items.Count || e.Graphics == null)
            return;

        if (listBoxGarments.Items[e.Index] is not GarmentPickerRowViewModel item)
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        bool isSelected = _selectedGarmentIds.Contains(item.GarmentId);
        var bounds = e.Bounds;

        using (var bgBrush = new SolidBrush(ColorViewBg))
        {
            g.FillRectangle(bgBrush, bounds);
        }

        var cardRect = new Rectangle(bounds.Left + 1, bounds.Top + 3, bounds.Width - 4, bounds.Height - 7);

        using (var cardPath = CreateRoundedRectangle(cardRect, 8))
        {
            using (var fillBrush = new SolidBrush(isSelected ? ColorSelectedBg : ColorCardBg))
            {
                g.FillPath(fillBrush, cardPath);
            }

            using (var borderPen = new Pen(isSelected ? ColorDustyRose : ColorBorder, isSelected ? 1.4f : 1f))
            {
                g.DrawPath(borderPen, cardPath);
            }
        }

        int boxSize = 16;
        int boxX = cardRect.Left + 18;
        int boxY = cardRect.Top + (cardRect.Height - boxSize) / 2;
        var boxRect = new Rectangle(boxX, boxY, boxSize, boxSize);

        using (var boxPath = CreateRoundedRectangle(boxRect, 4))
        {
            if (isSelected)
            {
                using var fillBrush = new SolidBrush(ColorDustyRose);
                g.FillPath(fillBrush, boxPath);

                using var checkPen = new Pen(Color.White, 1.75f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(checkPen, new[]
                {
                    new Point(boxRect.Left + 3, boxRect.Top + 8),
                    new Point(boxRect.Left + 7, boxRect.Top + 12),
                    new Point(boxRect.Left + 13, boxRect.Top + 4)
                });
            }
            else
            {
                using var borderPen = new Pen(ColorBorder, 1.5f);
                g.DrawPath(borderPen, boxPath);
            }
        }

        // Details
        int textLeft = boxX + boxSize + 18;
        int topY = cardRect.Top + 13;

        using (var titleFont = new Font("Segoe UI Semibold", 10.25f, FontStyle.Bold))
        using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        {
            TextRenderer.DrawText(g, item.Title, titleFont, new Point(textLeft, topY), ColorEspresso);
            string subtitle = $"{item.Code} · Size {item.SizeLabel}";
            TextRenderer.DrawText(g, subtitle, subFont, new Point(textLeft, topY + 20), ColorSubtext);
        }

        // Price & Deposit
        using (var priceFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold))
        using (var depFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        {
            string priceText = $"{CurrencySymbol}{item.RentalRate:N0}/lease";
            string depText = $"dep. {CurrencySymbol}{item.SecurityDeposit:N0}";

            var priceSize = TextRenderer.MeasureText(g, priceText, priceFont);
            var depSize = TextRenderer.MeasureText(g, depText, depFont);

            int rightMargin = cardRect.Right - 20;
            TextRenderer.DrawText(g, priceText, priceFont, new Point(rightMargin - priceSize.Width, topY), ColorEspresso);
            TextRenderer.DrawText(g, depText, depFont, new Point(rightMargin - depSize.Width, topY + 21), ColorSubtext);
        }
    }

    private void ListBoxGarments_MouseClick(object? sender, MouseEventArgs e)
    {
        int index = listBoxGarments.IndexFromPoint(e.Location);
        if (index >= 0 && index < listBoxGarments.Items.Count)
        {
            if (listBoxGarments.Items[index] is GarmentPickerRowViewModel item)
            {
                if (_selectedGarmentIds.Contains(item.GarmentId))
                    _selectedGarmentIds.Remove(item.GarmentId);
                else
                    _selectedGarmentIds.Add(item.GarmentId);

                listBoxGarments.Invalidate();
                UpdateSectionHeader();
            }
        }
    }

    private void UpdateSectionHeader()
    {
        if (lblAvailableGarments == null) return;

        int selectedCount = _selectedGarmentIds.Count;
        int totalAvailable = _garments.Count;

        if (totalAvailable == 0)
        {
            lblAvailableGarments.Text = "AVAILABLE GARMENTS (0 available for selected dates)";
        }
        else if (selectedCount > 0)
        {
            lblAvailableGarments.Text = $"AVAILABLE GARMENTS ({selectedCount} of {totalAvailable} selected)";
        }
        else
        {
            lblAvailableGarments.Text = $"AVAILABLE GARMENTS ({totalAvailable} available)";
        }
    }

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;
        UpdateSelectedCustomerLabel();

        _isUpdatingDates = true;
        try
        {
            // Ensure valid date ranges
            DateTime initialStart = (_draft.RentalStartDate > DateTime.MinValue) ? _draft.RentalStartDate : DateTime.Today;
            DateTime initialEnd = (_draft.RentalEndDate > DateTime.MinValue) ? _draft.RentalEndDate : initialStart.AddDays(3);

            if (initialEnd < initialStart) initialEnd = initialStart.AddDays(3);

            if (dtpStartDate != null) dtpStartDate.Value = initialStart;
            if (dtpEndDate != null) dtpEndDate.Value = initialEnd;
        }
        finally
        {
            _isUpdatingDates = false;
        }

        _selectedGarmentIds.Clear();
        foreach (var g in _draft.SelectedGarments)
        {
            _selectedGarmentIds.Add(g.GarmentId);
        }

        _ = LoadAvailableGarmentsAsync();
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        if (dtpStartDate != null) draft.RentalStartDate = dtpStartDate.Value.Date;
        if (dtpEndDate != null) draft.RentalEndDate = dtpEndDate.Value.Date;

        draft.SelectedGarments = _garments
            .Where(g => _selectedGarmentIds.Contains(g.GarmentId))
            .Select(g => g.GarmentEntity)
            .ToList();
    }

    public bool ValidateStep(out string errorMessage)
    {
        if (dtpStartDate != null && dtpEndDate != null && dtpEndDate.Value.Date < dtpStartDate.Value.Date)
        {
            errorMessage = "Rental end date cannot be earlier than start date.";
            return false;
        }

        var validSelected = _garments.Where(g => _selectedGarmentIds.Contains(g.GarmentId)).ToList();
        if (validSelected.Count == 0)
        {
            errorMessage = "Please choose at least one available garment to continue.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void UpdateSelectedCustomerLabel()
    {
        if (lblSelectedCustomer == null) return;

        if (_draft?.SelectedCustomer is { } customer)
        {
            string fullName = $"{customer.FirstName} {customer.LastName}".Trim();
            string sizes = FormatCustomerMeasurements(customer);

            lblSelectedCustomer.Text = string.IsNullOrEmpty(sizes)
                ? $"Selected Customer: {fullName}"
                : $"Selected Customer: {fullName}  ({sizes})";
        }
        else
        {
            lblSelectedCustomer.Text = "Selected Customer: —";
        }
    }

    private static string FormatCustomerMeasurements(Customer c)
    {
        string b = c.BustSize > 0 ? $"{c.BustSize:0.#}" : "—";
        string w = c.WaistSize > 0 ? $"{c.WaistSize:0.#}" : "—";
        string h = c.HipSize > 0 ? $"{c.HipSize:0.#}" : "—";

        if (b == "—" && w == "—" && h == "—")
            return string.Empty;

        return $"{b} – {w} – {h} in";
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