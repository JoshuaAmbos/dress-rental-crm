using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data.Seeders;

public static class MasterDbSeeder
{
    public static async Task SeedMasterCatalogAsync(MasterCrmDbContext masterDb, string server = "localhost,1433")
    {
        await masterDb.Database.EnsureCreatedAsync();

        // 1. Seed Subscription Packages (Let SQL Server generate SubscriptionPackageId)
        SubscriptionPackage pkgEnterprise;
        SubscriptionPackage pkgGrowth;
        SubscriptionPackage pkgStarter;

        if (!await masterDb.SubscriptionPackages.AnyAsync())
        {
            pkgEnterprise = new SubscriptionPackage
            {
                PackageName = "Package A - Enterprise",
                Description = "Full suite with multi-branch management, advanced BI analytics, and loyalty automation.",
                MaxBranches = 10,
                HasMultiBranch = true,
                HasLoyalty = true,
                HasAnalytics = true,
                MonthlyFee = 9999.00m,
                IsActive = true
            };

            pkgGrowth = new SubscriptionPackage
            {
                PackageName = "Package B - Growth",
                Description = "Core rental transactions, customer profiles, and analytics for single boutique showrooms.",
                MaxBranches = 1,
                HasMultiBranch = false,
                HasLoyalty = true,
                HasAnalytics = true,
                MonthlyFee = 4999.00m,
                IsActive = true
            };

            pkgStarter = new SubscriptionPackage
            {
                PackageName = "Package C - Starter",
                Description = "Essential bookings, client intake, and inventory tracking for micro boutiques.",
                MaxBranches = 1,
                HasMultiBranch = false,
                HasLoyalty = false,
                HasAnalytics = false,
                MonthlyFee = 1999.00m,
                IsActive = true
            };

            masterDb.SubscriptionPackages.AddRange(pkgEnterprise, pkgGrowth, pkgStarter);
            await masterDb.SaveChangesAsync();
        }
        else
        {
            pkgEnterprise = await masterDb.SubscriptionPackages.FirstAsync(p => p.PackageName.Contains("Enterprise"));
            pkgGrowth = await masterDb.SubscriptionPackages.FirstAsync(p => p.PackageName.Contains("Growth"));
            pkgStarter = await masterDb.SubscriptionPackages.FirstAsync(p => p.PackageName.Contains("Starter"));
        }

        // 2. Seed Boutique Companies (Let SQL Server generate CompanyId)
        var tenantConfigs = new[]
        {
            new { Code = "ATELIER", Name = "Atelier Haute Couture", PackageId = pkgEnterprise.SubscriptionPackageId, Db = "DB_Tenant_ATELIER" },
            new { Code = "MAISON",  Name = "Maison Étoile Bridal",   PackageId = pkgGrowth.SubscriptionPackageId,      Db = "DB_Tenant_MAISON" },
            new { Code = "DAVAO",   Name = "Davao Haute Rentals",     PackageId = pkgStarter.SubscriptionPackageId,     Db = "DB_Tenant_DAVAO" }
        };

        foreach (var cfg in tenantConfigs)
        {
            var company = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == cfg.Code);
            if (company == null)
            {
                company = new Company
                {
                    CompanyCode = cfg.Code,
                    CompanyName = cfg.Name,
                    SubscriptionPackageId = cfg.PackageId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                masterDb.Companies.Add(company);
                await masterDb.SaveChangesAsync();
            }

            var dbEntry = await masterDb.CompanyDatabases.FirstOrDefaultAsync(d => d.CompanyId == company.CompanyId);
            if (dbEntry == null)
            {
                masterDb.CompanyDatabases.Add(new CompanyDatabase
                {
                    CompanyId = company.CompanyId,
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