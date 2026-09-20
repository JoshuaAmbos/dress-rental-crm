using System.ComponentModel;
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

    // Atelier Palette Consistency
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    // Parameterless constructor for WinForms Designer
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

                card.CardClicked += (s, e) => OpenGarmentDetails(garment.GarmentId);
                card.ActionClicked += (s, e) => OpenGarmentDetails(garment.GarmentId);

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
        MessageBox.Show(
            $"Opening details for Garment ID #{garmentId}",
            "Garment Details",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void CatalogView_Load_1(object sender, EventArgs e)
    {
    }
}