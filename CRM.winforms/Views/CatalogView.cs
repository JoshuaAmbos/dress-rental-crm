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

        searchBar1.SetCueBanner("Search by garment name, SKU, or category...");

        if (searchBar1 is Control searchCtrl)
        {
            searchCtrl.TextChanged += SearchBar_TextChanged;
        }

        // Re-wire status buttons
        if (secondaryButtonAll != null)
        {
            secondaryButtonAll.Click -= secondaryButtonAll_Click;
            secondaryButtonAll.Click += secondaryButtonAll_Click;
        }
        if (secondaryButtonRented != null)
        {
            secondaryButtonRented.Click -= secondaryButtonRented_Click;
            secondaryButtonRented.Click += secondaryButtonRented_Click;
        }

        this.Load += CatalogView_Load;
    }

    private async void CatalogView_Load(object? sender, EventArgs e)
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        try
        {
            await using var db = _contextFactory();
            int companyId = _getCompanyId();

            // 1. Seed wardrobe if empty
            await GarmentSeeder.SeedGarmentsAsync(db, companyId);

            // 2. Synchronize garment statuses against real-time active bookings
            await _bookingService.SyncGarmentStatusesAsync(companyId);

            // 3. Load catalog items
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
        flpGarments.SuspendLayout();

        while (flpGarments.Controls.Count > 0)
        {
            var ctrl = flpGarments.Controls[0];
            flpGarments.Controls.RemoveAt(0);
            ctrl.Dispose();
        }

        try
        {
            await using var db = _contextFactory();
            int companyId = _getCompanyId();

            var query = db.Garments
                .AsNoTracking()
                .Where(g => g.CompanyId == companyId && g.IsActive);

            if (_currentStatusFilter != "All")
            {
                query = query.Where(g => g.Status == _currentStatusFilter);
            }

            if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
            {
                string term = _currentSearchTerm.Trim().ToLower();
                query = query.Where(g =>
                    g.ItemCode.ToLower().Contains(term) ||
                    g.StyleName.ToLower().Contains(term) ||
                    g.Category.ToLower().Contains(term) ||
                    g.Color.ToLower().Contains(term));
            }

            var garments = await query.OrderBy(g => g.ItemCode).ToListAsync();

            var cardList = new List<Control>();
            foreach (var garment in garments)
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
        }
        finally
        {
            flpGarments.ResumeLayout();
        }
    }

    private async void SearchBar_TextChanged(object? sender, EventArgs e)
    {
        _currentSearchTerm = searchBar1.Text;
        await LoadGarmentsAsync();
    }

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
                info + $"This garment is currently {garment.Status.ToUpper()} and cannot be manually modified while locked in a booking.",
                "Garment Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}