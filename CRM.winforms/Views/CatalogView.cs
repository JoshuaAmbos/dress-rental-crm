using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;

namespace CRM.winforms.Views;

public partial class CatalogView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private string _currentStatusFilter = "All";
    private string _currentSearchTerm = string.Empty;

    // Parameterless constructor for WinForms Designer
    public CatalogView()
    {
        InitializeComponent();

        // Default fallback factory using local connection string
        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(options);
        };
        _getCompanyId = () => 1;

        ConfigureView();
    }

    // Runtime constructor for DI
    public CatalogView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));

        ConfigureView();
    }

    private void ConfigureView()
    {
        searchBar1.SetCueBanner("Search by garment name, SKU, or category...");

        // Wire up live search
        if (searchBar1 is Control searchCtrl)
        {
            searchCtrl.TextChanged += SearchBar_TextChanged;
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

            // 2. Load the garments
            await LoadGarmentsAsync();
        }
        catch (Exception ex)
        {
            // This unwraps the real SQL Server error message:
            string realError = ex.GetBaseException().Message;
            MessageBox.Show($"Database Error: {realError}", "Catalog Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public async Task LoadGarmentsAsync()
    {
        // Suspend layout logic while batch-adding to eliminate UI flicker
        flpGarments.SuspendLayout();

        // Dispose old cards to free up GDI+ resources and memory
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

            // Create and append a card for every garment in the database
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

                // Fixed: Corrected typo and wired both events
                card.CardClicked += (s, e) => OpenGarmentDetails(garment.GarmentId);
                card.ActionClicked += (s, e) => OpenGarmentDetails(garment.GarmentId);

                cardList.Add(card);
            }

            // Add all controls in one batch
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

    private async void secondaryButtonAll_Click(object sender, EventArgs e)
    {
        _currentStatusFilter = "All";
        await LoadGarmentsAsync();
    }

    private async void secondaryButtonRented_Click(object sender, EventArgs e)
    {
        _currentStatusFilter = "Rented";
        await LoadGarmentsAsync();
    }

    private void OpenGarmentDetails(int garmentId)
    {
        // Placeholder for Garment detail/edit dialog
        MessageBox.Show(
            $"Opening details for Garment ID #{garmentId}",
            "Garment Details",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}