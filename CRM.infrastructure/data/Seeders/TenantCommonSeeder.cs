using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data.Seeders;

public static class TenantCommonSeeder
{
    public static async Task SeedConfigurationsAndLoyaltyAsync(TenantCrmDbContext db, DateTime today)
    {
        if (!await db.SystemConfigurations.AnyAsync())
        {
            db.SystemConfigurations.AddRange(new List<SystemConfiguration>
            {
                new() { ConfigKey = "DefaultLateFeePerDay", ConfigValue = "500.00", Description = "Daily penalty for overdue garment returns", LastModified = today },
                new() { ConfigKey = "StandardDepositPct", ConfigValue = "50.0", Description = "Standard security deposit percentage", LastModified = today },
                new() { ConfigKey = "CleaningBufferDays", ConfigValue = "2", Description = "Required turnaround days for dry cleaning", LastModified = today }
            });
        }

        if (!await db.LoyaltyAwards.AnyAsync())
        {
            db.LoyaltyAwards.AddRange(new List<LoyaltyAward>
            {
                new() { TierName = "Standard", MinLifetimeSpend = 0, MinRentalCount = 0, DiscountPercentage = 0.0m, RewardDescription = "Standard boutique membership and priority catalog notifications" },
                new() { TierName = "Gold", MinLifetimeSpend = 8000, MinRentalCount = 2, DiscountPercentage = 5.0m, RewardDescription = "5% off wardrobe leases and complimentary fitting adjustments" },
                new() { TierName = "VIP", MinLifetimeSpend = 15000, MinRentalCount = 3, DiscountPercentage = 10.0m, RewardDescription = "10% off, free minor alterations, and preview access to designer collections" }
            });
        }

        await db.SaveChangesAsync();
    }
}