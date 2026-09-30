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
        // 1. Configure Master DB with a 180-second timeout and explicit Encrypt=False
        var masterConnStr = $"Server={server};Database=DB_MasterCRM;User Id=sa;Password={saPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        var masterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(masterConnStr, sql => sql.CommandTimeout(180))
            .Options;

        await using var masterDb = new MasterCrmDbContext(masterOptions);

        // Seed Master CRM Metadata & Subscription Packages
        await MasterDbSeeder.SeedMasterCatalogAsync(masterDb, server);

        var companies = await masterDb.Companies
            .Include(c => c.CompanyDatabases)
            .AsNoTracking()
            .ToListAsync();

        var sysConnStr = $"Server={server};Database=master;User Id=sa;Password={saPassword};Encrypt=False;TrustServerCertificate=True;";

        foreach (var company in companies)
        {
            var dbName = company.CompanyDatabases.FirstOrDefault(d => d.IsActive)?.DatabaseName
                         ?? $"DB_Tenant_{company.CompanyCode}";

            // Ensure physical SQL database exists with an extended 180-second command timeout
            await using (var conn = new SqlConnection(sysConnStr))
            {
                await conn.OpenAsync();
                var sql = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                BEGIN
                    CREATE DATABASE [{dbName}];
                END";

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = 180 // Prevents wait operation timeout during SQL file allocation
                };
                await cmd.ExecuteNonQueryAsync();
            }

            // Apply EF Schema to the dedicated database with an extended 180-second timeout
            var tenantConnStr = $"Server={server};Database={dbName};User Id=sa;Password={saPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(tenantConnStr, sql => sql.CommandTimeout(180))
                .Options;

            await using var tenantDb = new TenantCrmDbContext(tenantOptions);

            // Apply tables only if the schema has not been built yet
            await tenantDb.Database.EnsureCreatedAsync();

            // Seed initial showroom branches, garments, and customers
            await SeedTenantSpecificDataAsync(tenantDb, company.CompanyId, company.CompanyCode);
        }
    }

    private static async Task SeedTenantSpecificDataAsync(TenantCrmDbContext db, int companyId, string companyCode)
    {
        // 1. Seed Showroom Branches and Inventory only if not already present
        if (!await db.Branches.AnyAsync())
        {
            if (companyId == 1 || companyCode.Equals("ATELIER", StringComparison.OrdinalIgnoreCase))
            {
                var b1 = new Branch { CompanyId = companyId, BranchCode = "ATELIER-HQ", BranchName = "Flagship Atelier (Makati)", City = "Makati City", Address = "Paseo de Roxas, Legazpi Village", ContactPhone = "+63 2 8812 3456", IsActive = true };
                var b2 = new Branch { CompanyId = companyId, BranchCode = "ATELIER-BGC", BranchName = "BGC Design Studio", City = "Taguig", Address = "High Street South, BGC", ContactPhone = "+63 2 8890 1234", IsActive = true };
                var b3 = new Branch { CompanyId = companyId, BranchCode = "ATELIER-CEB", BranchName = "Cebu Pop-Up Showroom", City = "Cebu City", Address = "IT Park, Lahug", ContactPhone = "+63 32 411 9876", IsActive = true };
                db.Branches.AddRange(b1, b2, b3);
                await db.SaveChangesAsync();

                db.Garments.AddRange(
                    new() { CompanyId = companyId, BranchId = b1.BranchId, ItemCode = "GOW-001", StyleName = "Blush Silk A-Line Gown", Category = "Evening Gown", Color = "Dusty Rose", Size = "M", RentalRate = 4500m, SecurityDeposit = 2500m, ReplacementValue = 18000m, Status = "Available" },
                    new() { CompanyId = companyId, BranchId = b2.BranchId, ItemCode = "FIL-001", StyleName = "Modern Embroidered Filipiniana", Category = "Filipiniana", Color = "Alabaster White", Size = "L", RentalRate = 6000m, SecurityDeposit = 3500m, ReplacementValue = 25000m, Status = "Available" },
                    new() { CompanyId = companyId, BranchId = b3.BranchId, ItemCode = "BAL-001", StyleName = "Midnight Velvet Corset Gown", Category = "Ball Gown", Color = "Midnight Blue", Size = "S", RentalRate = 6800m, SecurityDeposit = 4000m, ReplacementValue = 30000m, Status = "Available" }
                );

                db.Customers.AddRange(
                    new() { CompanyId = companyId, BranchId = b1.BranchId, CustomerCode = "CUST-001", FirstName = "Elena", LastName = "Reyes", ContactNumber = "+63 917 111 2222", EmailAddress = "elena.reyes@example.com", Address = "Makati City" },
                    new() { CompanyId = companyId, BranchId = b2.BranchId, CustomerCode = "CUST-002", FirstName = "Camille", LastName = "Santos", ContactNumber = "+63 918 333 4444", EmailAddress = "camille.santos@example.com", Address = "Taguig City" }
                );
                await db.SaveChangesAsync();
            }
            else if (companyId == 2 || companyCode.Equals("MAISON", StringComparison.OrdinalIgnoreCase))
            {
                var b1 = new Branch { CompanyId = companyId, BranchCode = "ME-MAIN", BranchName = "Maison Étoile Atelier", City = "Pasig City", Address = "Ortigas Center", ContactPhone = "+63 2 8631 0000", IsActive = true };
                db.Branches.Add(b1);
                await db.SaveChangesAsync();

                db.Garments.AddRange(
                    new() { CompanyId = companyId, BranchId = b1.BranchId, ItemCode = "BRD-101", StyleName = "Celestial Silk Bridal Gown", Category = "Bridal", Color = "Off-White", Size = "S", RentalRate = 12000m, SecurityDeposit = 6000m, ReplacementValue = 75000m, Status = "Available" },
                    new() { CompanyId = companyId, BranchId = b1.BranchId, ItemCode = "BRD-102", StyleName = "Chantilly Lace Veil Ensemble", Category = "Bridal", Color = "Pure White", Size = "M", RentalRate = 15000m, SecurityDeposit = 7500m, ReplacementValue = 90000m, Status = "Available" }
                );

                db.Customers.Add(new() { CompanyId = companyId, BranchId = b1.BranchId, CustomerCode = "CUST-M01", FirstName = "Adrianna", LastName = "Vanderbilt", ContactNumber = "+63 917 555 1234", EmailAddress = "adrianna@vanderbilt.com", Address = "Pasig City" });
                await db.SaveChangesAsync();
            }
            else if (companyId == 3 || companyCode.Equals("DAVAO", StringComparison.OrdinalIgnoreCase))
            {
                var b1 = new Branch { CompanyId = companyId, BranchCode = "DVO-MAIN", BranchName = "Davao Haute Flagship", City = "Davao City", Address = "F. Torres St., Poblacion", ContactPhone = "+63 82 221 4500", IsActive = true };
                db.Branches.Add(b1);
                await db.SaveChangesAsync();

                db.Garments.Add(new() { CompanyId = companyId, BranchId = b1.BranchId, ItemCode = "EVN-301", StyleName = "Emerald Pleated Midi", Category = "Cocktail", Color = "Emerald", Size = "M", RentalRate = 3200m, SecurityDeposit = 2000m, ReplacementValue = 14000m, Status = "Available" });
                db.Customers.Add(new() { CompanyId = companyId, BranchId = b1.BranchId, CustomerCode = "CUST-D01", FirstName = "Patricia", LastName = "Lim", ContactNumber = "+63 920 888 9999", EmailAddress = "patricia.lim@example.com", Address = "Davao City" });
                await db.SaveChangesAsync();
            }
        }

        // 2. Seed System Configurations safely by checking if keys exist
        bool hasChanges = false;

        if (!await db.SystemConfigurations.AnyAsync(s => s.ConfigKey == "DefaultLateFeePerDay"))
        {
            db.SystemConfigurations.Add(new()
            {
                ConfigKey = "DefaultLateFeePerDay",
                ConfigValue = "500.00",
                Description = "Daily penalty for overdue returns"
            });
            hasChanges = true;
        }

        if (!await db.SystemConfigurations.AnyAsync(s => s.ConfigKey == "StandardDepositPercentage"))
        {
            db.SystemConfigurations.Add(new()
            {
                ConfigKey = "StandardDepositPercentage",
                ConfigValue = "50",
                Description = "Security deposit percentage"
            });
            hasChanges = true;
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
        }
    }
}