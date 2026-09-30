using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data.Seeders;

public static class MasterDbSeeder
{
    public static async Task SeedMasterCatalogAsync(MasterCrmDbContext masterDb, string server = "localhost,1433")
    {
        await masterDb.Database.EnsureCreatedAsync();

        // 1. Seed Subscription Packages
        if (!await masterDb.SubscriptionPackages.AnyAsync())
        {
            var packages = new List<SubscriptionPackage>
            {
                new()
                {
                    SubscriptionPackageId = 1,
                    PackageName = "Package A - Enterprise",
                    Description = "Full suite with multi-branch management, advanced BI analytics, and loyalty automation.",
                    MaxBranches = 10,
                    HasMultiBranch = true,
                    HasLoyalty = true,
                    HasAnalytics = true,
                    MonthlyFee = 9999.00m,
                    IsActive = true
                },
                new()
                {
                    SubscriptionPackageId = 2,
                    PackageName = "Package B - Growth",
                    Description = "Core rental transactions, customer profiles, and analytics for single boutique showrooms.",
                    MaxBranches = 1,
                    HasMultiBranch = false,
                    HasLoyalty = true,
                    HasAnalytics = true,
                    MonthlyFee = 4999.00m,
                    IsActive = true
                },
                new()
                {
                    SubscriptionPackageId = 3,
                    PackageName = "Package C - Starter",
                    Description = "Essential bookings, client intake, and inventory tracking for micro boutiques.",
                    MaxBranches = 1,
                    HasMultiBranch = false,
                    HasLoyalty = false,
                    HasAnalytics = false,
                    MonthlyFee = 1999.00m,
                    IsActive = true
                }
            };

            await masterDb.SubscriptionPackages.AddRangeAsync(packages);
            await masterDb.SaveChangesAsync();
        }

        // 2. Seed Boutique Companies and Database Routing Entries
        var tenantConfigs = new[]
        {
            new { Id = 1, Code = "ATELIER", Name = "Atelier Haute Couture", PackageId = 1, Db = "DB_Tenant_ATELIER" },
            new { Id = 2, Code = "MAISON",  Name = "Maison Étoile Bridal",   PackageId = 2, Db = "DB_Tenant_MAISON" },
            new { Id = 3, Code = "DAVAO",   Name = "Davao Haute Rentals",     PackageId = 3, Db = "DB_Tenant_DAVAO" }
        };

        foreach (var cfg in tenantConfigs)
        {
            var company = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyId == cfg.Id);
            if (company == null)
            {
                company = new Company
                {
                    CompanyId = cfg.Id,
                    CompanyCode = cfg.Code,
                    CompanyName = cfg.Name,
                    SubscriptionPackageId = cfg.PackageId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                masterDb.Companies.Add(company);
                await masterDb.SaveChangesAsync();
            }

            var dbEntry = await masterDb.CompanyDatabases.FirstOrDefaultAsync(d => d.CompanyId == cfg.Id);
            if (dbEntry == null)
            {
                masterDb.CompanyDatabases.Add(new CompanyDatabase
                {
                    CompanyId = cfg.Id,
                    ServerName = server,
                    DatabaseName = cfg.Db,
                    CredentialKey = "DefaultKey",
                    IsActive = true
                });
                await masterDb.SaveChangesAsync();
            }
        }
    }
}