using CRM.infrastructure.data.Seeders;

namespace CRM.infrastructure.data;

public static class DatabaseSeeder
{
    private static readonly List<ITenantSeeder> _tenantSeeders = new()
    {
        new TenantCSeeder(), // Atelier Haute Couture (Multi-Branch Tenant C)
        new TenantASeeder()  // Maison Étoile Bridal (Single-Branch Boutique Tenant A)
    };

    public static async Task ResetAndSeedDatabaseAsync(Func<TenantCrmDbContext> contextFactory, int? targetCompanyId = null)
    {
        await using var db = contextFactory();

        // Clean database recreation
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var today = new DateTime(2026, 9, 21);

        // Seed shared configuration parameters & loyalty rules
        await TenantCommonSeeder.SeedConfigurationsAndLoyaltyAsync(db, today);

        // Run tenant-specific seeders
        foreach (var seeder in _tenantSeeders)
        {
            if (targetCompanyId.HasValue && seeder.CompanyId != targetCompanyId.Value)
                continue;

            await seeder.SeedAsync(db);
        }
    }
}