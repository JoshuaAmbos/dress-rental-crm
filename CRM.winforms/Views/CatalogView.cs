using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Services.RentalBookingServices;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CRM.winforms.Views;

public partial class CatalogView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly RentalBookingService _bookingService;

    private string _currentStatusFilter = "All";
    private string _currentSearchTerm = string.Empty;

    // Filter Bar & Tracking
    private FlowLayoutPanel pnlFilterTabs = null!;
    private readonly List<Button> _filterButtons = new();

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    public CatalogView()
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

        ConfigureView();
    }

    public CatalogView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _bookingService = new RentalBookingService(_contextFactory);

        ConfigureView();
    }

    private void ConfigureView()
    {
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);

        if (label1 != null)
        {
            label1.Text = "Garment Catalog";
            label1.UseMnemonic = false;
            label1.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            label1.ForeColor = ColorEspresso;
        }

        // Hide legacy designer buttons so the unified dynamic bar takes over
        if (secondaryButtonAll != null) secondaryButtonAll.Visible = false;
        if (secondaryButtonRented != null) secondaryButtonRented.Visible = false;

        // Initialize Dynamic Atelier Filter Strip
        SetupFilterTabs();

        if (searchBar1 != null)
        {
            searchBar1.SetCueBanner("Search by garment name, SKU, category, or color...");
            if (searchBar1 is Control searchCtrl)
            {
                searchCtrl.TextChanged += SearchBar_TextChanged;
            }
        }

        this.Load += CatalogView_Load;
    }

    private void SetupFilterTabs()
    {
        // Position the filter strip right above flpGarments
        int stripY = (secondaryButtonAll != null) ? secondaryButtonAll.Top : 75;
        int stripX = (secondaryButtonAll != null) ? secondaryButtonAll.Left : 32;

        pnlFilterTabs = new FlowLayoutPanel
        {
            Location = new Point(stripX, stripY),
            Height = 36,
            Width = Width - stripX - 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        // Add above the garments flow panel
        Controls.Add(pnlFilterTabs);
        pnlFilterTabs.BringToFront();
    }

    private async void CatalogView_Load(object? sender, EventArgs e)
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        try
        {
            int companyId = _getCompanyId();

            // Synchronize garment statuses against active rental bookings
            await _bookingService.SyncGarmentStatusesAsync(companyId);

            // Load and render catalog
            await LoadGarmentsAsync();
        }
        catch (Exception ex)
        {
            string realError = ex.GetBaseException().Message;
            MessageBox.Show($"Database Error: {realError}", "Catalog Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public async Task LoadGarmentsAsync()
    {
        try
        {
            await using var db = _contextFactory();
            int companyId = _getCompanyId();

            // Fetch active wardrobe items
            var allGarments = await db.Garments
                .AsNoTracking()
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .OrderBy(g => g.ItemCode)
                .ToListAsync();

            // 1. Render / Update Filter Pill Buttons with Real-Time Counts
            RenderFilterButtons(allGarments);

            // 2. Apply Status Filter
            var filtered = (_currentStatusFilter == "All")
                ? allGarments
                : allGarments.Where(g => string.Equals(g.Status, _currentStatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            // 3. Apply Search Filter
            if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
            {
                string term = _currentSearchTerm.Trim().ToLower();
                filtered = filtered.Where(g =>
                    g.ItemCode.ToLower().Contains(term) ||
                    g.StyleName.ToLower().Contains(term) ||
                    g.Category.ToLower().Contains(term) ||
                    g.Color.ToLower().Contains(term)).ToList();
            }

            // 4. Populate Cards into flpGarments
            flpGarments.SuspendLayout();
            while (flpGarments.Controls.Count > 0)
            {
                var ctrl = flpGarments.Controls[0];
                flpGarments.Controls.RemoveAt(0);
                ctrl.Dispose();
            }

            var cardList = new List<Control>();
            foreach (var garment in filtered)
            {
                var card = new GarmentCardControl
                {
                    RentalItemId = garment.GarmentId,
                    ItemCode = garment.ItemCode,
                    StyleName = garment.StyleName,
                    Category = garment.Category,
                    SizeLabel = $"Size {garment.Size} ({garment.BustSize:0.#}\" - {garment.WaistSize:0.#}\" - {garment.HipSize:0.#}\")",
                    RentalRate = garment.RentalRate,
                    Status = garment.Status,
                    ImagePath = garment.ImagePath
                };

                card.CardClicked += async (s, e) => await OpenGarmentDetailsAsync(garment.GarmentId);
                card.ActionClicked += async (s, e) => await OpenGarmentDetailsAsync(garment.GarmentId);

                cardList.Add(card);
            }

            flpGarments.Controls.AddRange(cardList.ToArray());
            flpGarments.ResumeLayout();
        }
        catch (Exception ex)
        {
            flpGarments.ResumeLayout();
            MessageBox.Show($"Failed to load garments: {ex.GetBaseException().Message}", "Catalog Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RenderFilterButtons(List<Garment> allGarments)
    {
        pnlFilterTabs.SuspendLayout();
        pnlFilterTabs.Controls.Clear();
        _filterButtons.Clear();

        var tabs = new (string Status, int Count)[]
        {
            ("All", allGarments.Count),
            ("Available", allGarments.Count(g => string.Equals(g.Status, "Available", StringComparison.OrdinalIgnoreCase))),
            ("Rented", allGarments.Count(g => string.Equals(g.Status, "Rented", StringComparison.OrdinalIgnoreCase))),
            ("Reserved", allGarments.Count(g => string.Equals(g.Status, "Reserved", StringComparison.OrdinalIgnoreCase) || string.Equals(g.Status, "Fitting", StringComparison.OrdinalIgnoreCase))),
            ("In Cleaning", allGarments.Count(g => string.Equals(g.Status, "In Cleaning", StringComparison.OrdinalIgnoreCase)))
        };

        foreach (var (status, count) in tabs)
        {
            bool isSelected = string.Equals(_currentStatusFilter, status, StringComparison.OrdinalIgnoreCase);

            var btn = new Button
            {
                Text = status == "All" ? $"All  {count}" : $"{status}  {count}",
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
                _currentStatusFilter = status;
                UpdateActiveTabVisuals(btn);
                await LoadGarmentsAsync();
            };

            _filterButtons.Add(btn);
            pnlFilterTabs.Controls.Add(btn);
        }

        pnlFilterTabs.ResumeLayout();
    }

    private void UpdateActiveTabVisuals(Button activeBtn)
    {
        foreach (var btn in _filterButtons)
        {
            bool isSelected = (btn == activeBtn);
            btn.BackColor = isSelected ? ColorDustyRose : Color.White;
            btn.ForeColor = isSelected ? Color.White : ColorEspresso;
            btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
        }
    }

    private async void SearchBar_TextChanged(object? sender, EventArgs e)
    {
        _currentSearchTerm = searchBar1?.Text ?? string.Empty;
        await LoadGarmentsAsync();
    }

    /// <summary>
    /// Interactive details sheet allowing staff to view and transition garment statuses.
    /// </summary>
    private async Task OpenGarmentDetailsAsync(int garmentId)
    {
        await using var db = _contextFactory();
        var garment = await db.Garments.FirstOrDefaultAsync(g => g.GarmentId == garmentId);
        if (garment == null) return;

        string info = $"Garment: {garment.StyleName} ({garment.ItemCode})\n" +
                      $"Category: {garment.Category} | Size: {garment.Size}\n" +
                      $"Rental Rate: ₱{garment.RentalRate:N2}\n" +
                      $"Security Deposit: ₱{garment.SecurityDeposit:N2}\n" +
                      $"Current Status: {garment.Status}\n\n";

        if (garment.Status == "In Cleaning")
        {
            var res = MessageBox.Show(
                info + "This garment has been returned and is currently IN CLEANING.\n\nDo you want to mark this item as CLEANED and return it to AVAILABLE inventory?",
                "Garment Maintenance",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                await _bookingService.CompleteGarmentCleaningAsync(garmentId);
                await LoadGarmentsAsync();
            }
        }
        else if (garment.Status == "Available")
        {
            var res = MessageBox.Show(
                info + "This garment is currently AVAILABLE.\n\nDo you want to send this item to CLEANING / ALTERATIONS?",
                "Garment Options",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                garment.Status = "In Cleaning";
                await db.SaveChangesAsync();
                await LoadGarmentsAsync();
            }
        }
        else
        {
            MessageBox.Show(
                info + $"This garment is currently {garment.Status.ToUpper()} and cannot be manually modified while locked in an active booking.",
                "Garment Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
    // Designer event handler stubs to satisfy CatalogView.Designer.cs
    private async void secondaryButtonAll_Click(object? sender, EventArgs e)
    {
        _currentStatusFilter = "All";
        await LoadGarmentsAsync();
    }

    private async void secondaryButtonRented_Click(object? sender, EventArgs e)
    {
        _currentStatusFilter = "Rented";
        await LoadGarmentsAsync();
    }
}