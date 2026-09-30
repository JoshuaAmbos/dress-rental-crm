using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class CatalogView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Func<int?>? _getBranchId;
    private readonly RentalBookingService _bookingService;

    private string _currentStatusFilter = "All";
    private string _currentSearchTerm = string.Empty;
    private decimal _currentDepositPct = 50m;

    private FlowLayoutPanel pnlFilterTabs = null!;
    private readonly List<Button> _filterButtons = new();

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
        _getBranchId = null;
        _bookingService = new RentalBookingService(_contextFactory);

        ConfigureView();
    }

    public CatalogView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Func<int?>? getBranchId = null)
    {
        InitializeComponent();
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _getBranchId = getBranchId;
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
            label1.ForeColor = ColorPrimary;
        }

        if (secondaryButtonAll != null) secondaryButtonAll.Visible = false;
        if (secondaryButtonRented != null) secondaryButtonRented.Visible = false;

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

            // 1. Load active deposit % from SystemConfigurations
            try
            {
                var depCfg = await db.SystemConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ConfigKey == "StandardDepositPct");

                if (depCfg != null && decimal.TryParse(depCfg.ConfigValue, out var pct))
                {
                    _currentDepositPct = pct;
                }
            }
            catch { }

            // 2. Query garments with multi-branch awareness
            var query = db.Garments
                .AsNoTracking()
                .Where(g => g.CompanyId == companyId && g.IsActive);

            int? branchId = _getBranchId?.Invoke();
            if (branchId.HasValue)
            {
                query = query.Where(g => g.BranchId == branchId.Value);
            }

            var allGarments = await query.OrderBy(g => g.ItemCode).ToListAsync();

            RenderFilterButtons(allGarments);

            var filtered = (_currentStatusFilter == "All")
                ? allGarments
                : allGarments.Where(g => string.Equals(g.Status, _currentStatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
            {
                string term = _currentSearchTerm.Trim().ToLower();
                filtered = filtered.Where(g =>
                    g.ItemCode.ToLower().Contains(term) ||
                    g.StyleName.ToLower().Contains(term) ||
                    g.Category.ToLower().Contains(term) ||
                    g.Color.ToLower().Contains(term)).ToList();
            }

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
                // Dynamic deposit calculation matching system configuration
                decimal calculatedDeposit = Math.Round(garment.RentalRate * (_currentDepositPct / 100m), 2);

                var card = new GarmentCardControl
                {
                    RentalItemId = garment.GarmentId,
                    ItemCode = garment.ItemCode,
                    StyleName = garment.StyleName,
                    Category = garment.Category,
                    SizeLabel = $"Size {garment.Size} ({garment.BustSize:0.#}\" - {garment.WaistSize:0.#}\" - {garment.HipSize:0.#}\")",
                    RentalRate = garment.RentalRate,
                    SecurityDeposit = calculatedDeposit,
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
                BackColor = isSelected ? ColorAccent : Color.White,
                ForeColor = isSelected ? Color.White : ColorPrimary
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
            btn.BackColor = isSelected ? ColorAccent : Color.White;
            btn.ForeColor = isSelected ? Color.White : ColorPrimary;
            btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
        }
    }

    private async void SearchBar_TextChanged(object? sender, EventArgs e)
    {
        _currentSearchTerm = searchBar1?.Text ?? string.Empty;
        await LoadGarmentsAsync();
    }

    private async Task OpenGarmentDetailsAsync(int garmentId)
    {
        await using var db = _contextFactory();
        var garment = await db.Garments.FirstOrDefaultAsync(g => g.GarmentId == garmentId);
        if (garment == null) return;

        decimal calculatedDeposit = Math.Round(garment.RentalRate * (_currentDepositPct / 100m), 2);

        string info = $"Garment: {garment.StyleName} ({garment.ItemCode})\n" +
                      $"Category: {garment.Category} | Size: {garment.Size}\n" +
                      $"Rental Rate: ₱{garment.RentalRate:N2}\n" +
                      $"Security Deposit ({_currentDepositPct:0.#}%): ₱{calculatedDeposit:N2}\n" +
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
}