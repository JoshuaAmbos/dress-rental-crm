using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Models;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class Step2GarmentDatesView : UserControl, IBookingWizardStep
{
    // Dependencies & State
    private readonly Func<TenantCrmDbContext>? _contextFactory;
    private readonly RentalBookingService? _bookingService;
    private readonly Func<int>? _getCompanyId;

    private BookingDraftModel? _draft;
    private List<GarmentPickerRowViewModel> _garments = [];
    private readonly HashSet<int> _selectedGarmentIds = [];
    private bool _isUpdatingDates = false;

    // UI Controls
    private Label lblSelectedCustomer = null!;
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
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header Title & Selected Client Banner
        if (lblTitle != null)
        {
            lblTitle.Text = "Select Garment & Dates";
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = ColorPrimary;
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
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Text = "Selected Customer: —"
        };
        Controls.Add(lblSelectedCustomer);
        lblSelectedCustomer.BringToFront();

        // 2. Date Pickers Strip
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

        // 3. Section Tagline
        if (lblAvailableGarments != null)
        {
            lblAvailableGarments.Text = "AVAILABLE GARMENTS";
            lblAvailableGarments.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            lblAvailableGarments.ForeColor = ColorMutedLabel;
            lblAvailableGarments.Location = new Point(32, 134);
            lblAvailableGarments.AutoSize = true;
        }

        // 4. Owner-Drawn Garment Cards ListBox
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
                dtpEndDate.Value = dtpStartDate.Value.Date.AddDays(7);
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
            DateTime end = dtpEndDate?.Value.Date ?? DateTime.Today.AddDays(7);

            if (end < start) end = start.AddDays(7);

            // 1. Query garments free of date overlaps (including cleaning turnaround buffer)
            _garments = await _bookingService.GetAvailableGarmentsAsync(_getCompanyId(), start, end);

            // 2. Synchronize each garment's deposit to match the active configured percentage
            decimal depPct = (_draft != null && _draft.DepositPercentage > 0) ? _draft.DepositPercentage : 50m;
            foreach (var g in _garments)
            {
                g.SecurityDeposit = Math.Round(g.RentalRate * (depPct / 100m), 2);
            }

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

        // 1. Fill card gap background
        using (var bgBrush = new SolidBrush(ColorViewBg))
        {
            g.FillRectangle(bgBrush, bounds);
        }

        // 2. Floating Card Bounds
        var cardRect = new Rectangle(bounds.Left + 1, bounds.Top + 3, bounds.Width - 4, bounds.Height - 7);

        using (var cardPath = CreateRoundedRectangle(cardRect, 8))
        {
            using (var fillBrush = new SolidBrush(isSelected ? ColorActivePill : ColorCardBg))
            {
                g.FillPath(fillBrush, cardPath);
            }

            using (var borderPen = new Pen(isSelected ? ColorAccent : ColorBorder, isSelected ? 1.4f : 1f))
            {
                g.DrawPath(borderPen, cardPath);
            }
        }

        // 3. Selection Checkbox Box (18x18 Rounded Box)
        int boxSize = 18;
        int boxX = cardRect.Left + 18;
        int boxY = cardRect.Top + (cardRect.Height - boxSize) / 2;
        var boxRect = new Rectangle(boxX, boxY, boxSize, boxSize);

        using (var boxPath = CreateRoundedRectangle(boxRect, 4))
        {
            if (isSelected)
            {
                using var fillBrush = new SolidBrush(ColorAccent);
                g.FillPath(fillBrush, boxPath);

                using var checkPen = new Pen(Color.White, 1.85f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(checkPen, new[]
                {
                    new Point(boxRect.Left + 4, boxRect.Top + 9),
                    new Point(boxRect.Left + 8, boxRect.Top + 13),
                    new Point(boxRect.Left + 14, boxRect.Top + 5)
                });
            }
            else
            {
                using var borderPen = new Pen(ColorBorder, 1.5f);
                g.DrawPath(borderPen, boxPath);
            }
        }

        // 4. Garment Title & Details
        int textLeft = boxX + boxSize + 18;
        int topY = cardRect.Top + 13;

        using (var titleFont = new Font("Segoe UI Semibold", 10.25f, FontStyle.Bold))
        using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        {
            TextRenderer.DrawText(g, item.Title, titleFont, new Point(textLeft, topY), ColorPrimary);
            string subtitle = $"{item.Code} · Size {item.SizeLabel}";
            TextRenderer.DrawText(g, subtitle, subFont, new Point(textLeft, topY + 20), ColorSubtext);
        }

        // 5. Price & Dynamic Deposit Display
        using (var priceFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold))
        using (var depFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        {
            decimal depPct = (_draft != null && _draft.DepositPercentage > 0) ? _draft.DepositPercentage : 50m;
            decimal dynamicDep = Math.Round(item.RentalRate * (depPct / 100m), 2);

            string priceText = $"{CurrencySymbol}{item.RentalRate:N0}/lease";
            string depText = $"dep. {CurrencySymbol}{dynamicDep:N0}";

            var priceSize = TextRenderer.MeasureText(g, priceText, priceFont);
            var depSize = TextRenderer.MeasureText(g, depText, depFont);

            int rightMargin = cardRect.Right - 20;
            TextRenderer.DrawText(g, priceText, priceFont, new Point(rightMargin - priceSize.Width, topY), ColorAccent);
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
        const string turnaroundNote = " · includes turnaround buffer";

        if (totalAvailable == 0)
        {
            lblAvailableGarments.Text = $"AVAILABLE GARMENTS (0 available{turnaroundNote})";
        }
        else if (selectedCount > 0)
        {
            lblAvailableGarments.Text = $"AVAILABLE GARMENTS ({selectedCount} of {totalAvailable} selected{turnaroundNote})";
        }
        else
        {
            lblAvailableGarments.Text = $"AVAILABLE GARMENTS ({totalAvailable} available{turnaroundNote})";
        }
    }

    // IBookingWizardStep Implementation

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;
        UpdateSelectedCustomerLabel();

        _isUpdatingDates = true;
        try
        {
            DateTime initialStart = (_draft.RentalStartDate > DateTime.MinValue) ? _draft.RentalStartDate : DateTime.Today;
            DateTime initialEnd = (_draft.RentalEndDate > DateTime.MinValue) ? _draft.RentalEndDate : initialStart.AddDays(7);

            if (initialEnd < initialStart) initialEnd = initialStart.AddDays(7);

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

        decimal depPct = draft.DepositPercentage > 0 ? draft.DepositPercentage : 50m;

        draft.SelectedGarments = [.. _garments
            .Where(g => _selectedGarmentIds.Contains(g.GarmentId))
            .Select(g =>
            {
                var entity = g.GarmentEntity;
                entity.SecurityDeposit = Math.Round(entity.RentalRate * (depPct / 100m), 2);
                return entity;
            })];
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