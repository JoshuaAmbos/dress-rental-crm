using CRM.domain.entities;
using CRM.infrastructure.data.Seeders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data;

public static class DatabaseSeeder
{
    public static async Task ProvisionAndSeedAllTenantsAsync(
        string server = "localhost,1433",
        string saPassword = "YourStrong@Passw0rd!")
    {
        var masterConnStr = $"Server={server};Database=DB_MasterCRM;User Id=sa;Password={saPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;";
        var masterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(masterConnStr).Options;
        await using var masterDb = new MasterCrmDbContext(masterOptions);

        // 1. Seed Master CRM Metadata & Packages
        await MasterDbSeeder.SeedMasterCatalogAsync(masterDb, server);

        var companies = await masterDb.Companies
            .Include(c => c.CompanyDatabases)
            .AsNoTracking()
            .ToListAsync();

        // 2. Provision and Seed Each Tenant Database Separately
        foreach (var company in companies)
        {
            var dbName = company.CompanyDatabases.FirstOrDefault(d => d.IsActive)?.DatabaseName
                         ?? $"DB_Tenant_{company.CompanyCode}";

            // Ensure physical SQL database exists
            var sysConnStr = $"Server={server};Database=master;User Id=sa;Password={saPassword};TrustServerCertificate=True;";
            await using (var conn = new SqlConnection(sysConnStr))
            {
                await conn.OpenAsync();
                var sql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}];";
                await using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Apply EF Schema to the dedicated database
            var tenantConnStr = $"Server={server};Database={dbName};User Id=sa;Password={saPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(tenantConnStr).Options;

            await using var tenantDb = new TenantCrmDbContext(tenantOptions);
            await tenantDb.Database.EnsureCreatedAsync();

            await SeedTenantSpecificDataAsync(tenantDb, company.CompanyId, company.CompanyCode);
        }
    }

    private static async Task SeedTenantSpecificDataAsync(TenantCrmDbContext db, int companyId, string companyCode)
    {
        if (await db.Branches.AnyAsync(b => b.CompanyId == companyId)) return;

        // Tenant 1 (Atelier Haute Couture - Package A): Multi-Branch
        if (companyId == 1)
        {
            var b1 = new Branch { CompanyId = 1, BranchCode = "ATELIER-HQ", BranchName = "Flagship Atelier (Makati)", City = "Makati City", Address = "Paseo de Roxas, Legazpi Village", ContactPhone = "+63 2 8812 3456", IsActive = true };
            var b2 = new Branch { CompanyId = 1, BranchCode = "ATELIER-BGC", BranchName = "BGC Design Studio", City = "Taguig", Address = "High Street South, BGC", ContactPhone = "+63 2 8890 1234", IsActive = true };
            var b3 = new Branch { CompanyId = 1, BranchCode = "ATELIER-CEB", BranchName = "Cebu Pop-Up Showroom", City = "Cebu City", Address = "IT Park, Lahug", ContactPhone = "+63 32 411 9876", IsActive = true };
            db.Branches.AddRange(b1, b2, b3);
            await db.SaveChangesAsync();

            db.Garments.AddRange(
                new() { CompanyId = 1, BranchId = b1.BranchId, ItemCode = "GOW-001", StyleName = "Blush Silk A-Line Gown", Category = "Evening Gown", Color = "Dusty Rose", Size = "M", RentalRate = 4500m, SecurityDeposit = 2500m, ReplacementValue = 18000m, Status = "Available" },
                new() { CompanyId = 1, BranchId = b2.BranchId, ItemCode = "FIL-001", StyleName = "Modern Embroidered Filipiniana", Category = "Filipiniana", Color = "Alabaster White", Size = "L", RentalRate = 6000m, SecurityDeposit = 3500m, ReplacementValue = 25000m, Status = "Available" },
                new() { CompanyId = 1, BranchId = b3.BranchId, ItemCode = "BAL-001", StyleName = "Midnight Velvet Corset Gown", Category = "Ball Gown", Color = "Midnight Blue", Size = "S", RentalRate = 6800m, SecurityDeposit = 4000m, ReplacementValue = 30000m, Status = "Available" }
            );

            db.Customers.AddRange(
                new() { CompanyId = 1, BranchId = b1.BranchId, CustomerCode = "CUST-001", FirstName = "Elena", LastName = "Reyes", ContactNumber = "+63 917 111 2222", EmailAddress = "elena.reyes@example.com", Address = "Makati City" },
                new() { CompanyId = 1, BranchId = b2.BranchId, CustomerCode = "CUST-002", FirstName = "Camille", LastName = "Santos", ContactNumber = "+63 918 333 4444", EmailAddress = "camille.santos@example.com", Address = "Taguig City" }
            );
        }
        // Tenant 2 (Maison Étoile - Package B): Single Branch
        else if (companyId == 2)
        {
            var b1 = new Branch { CompanyId = 2, BranchCode = "ME-MAIN", BranchName = "Maison Étoile Atelier", City = "Pasig City", Address = "Ortigas Center", ContactPhone = "+63 2 8631 0000", IsActive = true };
            db.Branches.Add(b1);
            await db.SaveChangesAsync();

            db.Garments.AddRange(
                new() { CompanyId = 2, BranchId = b1.BranchId, ItemCode = "BRD-101", StyleName = "Celestial Silk Bridal Gown", Category = "Bridal", Color = "Off-White", Size = "S", RentalRate = 12000m, SecurityDeposit = 6000m, ReplacementValue = 75000m, Status = "Available" },
                new() { CompanyId = 2, BranchId = b1.BranchId, ItemCode = "BRD-102", StyleName = "Chantilly Lace Veil Ensemble", Category = "Bridal", Color = "Pure White", Size = "M", RentalRate = 15000m, SecurityDeposit = 7500m, ReplacementValue = 90000m, Status = "Available" }
            );

            db.Customers.Add(new() { CompanyId = 2, BranchId = b1.BranchId, CustomerCode = "CUST-M01", FirstName = "Adrianna", LastName = "Vanderbilt", ContactNumber = "+63 917 555 1234", EmailAddress = "adrianna@vanderbilt.com", Address = "Pasig City" });
        }
        // Tenant 3 (Davao Haute - Package C): Single Branch
        else if (companyId == 3)
        {
            var b1 = new Branch { CompanyId = 3, BranchCode = "DVO-MAIN", BranchName = "Davao Haute Flagship", City = "Davao City", Address = "F. Torres St., Poblacion", ContactPhone = "+63 82 221 4500", IsActive = true };
            db.Branches.Add(b1);
            await db.SaveChangesAsync();

            db.Garments.Add(new() { CompanyId = 3, BranchId = b1.BranchId, ItemCode = "EVN-301", StyleName = "Emerald Pleated Midi", Category = "Cocktail", Color = "Emerald", Size = "M", RentalRate = 3200m, SecurityDeposit = 2000m, ReplacementValue = 14000m, Status = "Available" });
            db.Customers.Add(new() { CompanyId = 3, BranchId = b1.BranchId, CustomerCode = "CUST-D01", FirstName = "Patricia", LastName = "Lim", ContactNumber = "+63 920 888 9999", EmailAddress = "patricia.lim@example.com", Address = "Davao City" });
        }

        // Shared baseline configs
        db.SystemConfigurations.AddRange(
            new() { ConfigKey = "DefaultLateFeePerDay", ConfigValue = "500.00", Description = "Daily penalty for overdue returns" },
            new() { ConfigKey = "StandardDepositPercentage", ConfigValue = "50", Description = "Security deposit percentage" }
        );

        await db.SaveChangesAsync();
    }
}