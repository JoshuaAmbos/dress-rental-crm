using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM.winforms.Views;

public partial class CustomerProfilesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<int> _getCompanyId;

    public CustomerProfilesView() : this(() => 1)
    {
    }

    public CustomerProfilesView(Func<int> getCompanyId)
    {
        _getCompanyId = getCompanyId ?? (() => 1);
        InitializeComponent();
    }

    private TenantCrmDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }

    private void CustomerProfilesView_Load(object sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode)
        {
            _ = LoadCustomerDataAsync();
        }
    }

    public async Task LoadCustomerDataAsync(string search = "")
    {
        if (DesignMode) return;

        try
        {
            await using var db = GetDbContext();
            int companyId = _getCompanyId();

            var query = db.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CustomerCode.Contains(search) ||
                    c.CustomerName.Contains(search) ||
                    c.ContactNumber.Contains(search) ||
                    c.EmailAddress.Contains(search) ||
                    c.Address.Contains(search));
            }

            // Return full Customer entity instances so DataPropertyName matches designer columns
            var list = await query
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            if (customerBindingSource != null)
            {
                customerBindingSource.DataSource = list;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customer profiles: {ex.Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void searchBar1_SearchTextChanged(object? sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            _ = LoadCustomerDataAsync(searchBar1.TextValue.Trim());
        }
    }

    private void searchBar1_Load(object sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            searchBar1.SearchTextChanged += searchBar1_SearchTextChanged;
        }
    }

    private void textBox1_TextChanged(object sender, EventArgs e) { }
    private void label1_Click(object sender, EventArgs e) { }
    private void pictureBox1_Click(object sender, EventArgs e) { }
}