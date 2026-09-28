using CRM.api.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class LoyaltyAwardService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public LoyaltyAwardService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<LoyaltyOverviewDto> GetLoyaltyOverviewAsync(int companyId, string searchTerm = "")
    {
        await using var db = _contextFactory();

        // Fetch defined tier rules ordered by threshold ascending
        var tiers = await db.LoyaltyAwards
            .AsNoTracking()
            .OrderBy(t => t.MinLifetimeSpend)
            .ToListAsync();

        if (tiers.Count == 0)
        {
            tiers =
            [
                new() { TierName = "Standard", MinLifetimeSpend = 0, MinRentalCount = 0, DiscountPercentage = 0m, RewardDescription = "Standard membership and new arrival notifications" },
                new() { TierName = "Gold", MinLifetimeSpend = 8000, MinRentalCount = 2, DiscountPercentage = 5m, RewardDescription = "5% off leases and complimentary fitting reservations" },
                new() { TierName = "VIP", MinLifetimeSpend = 15000, MinRentalCount = 3, DiscountPercentage = 10m, RewardDescription = "10% off leases, free alterations, and priority access" }
            ];
        }

        // Query customers and non-cancelled bookings
        var customersQuery = db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .Include(c => c.RentalBookings.Where(b => b.BookingStage != "Cancelled"));

        var customers = await customersQuery.ToListAsync();

        var customerLoyaltyList = new List<CustomerLoyaltyDto>();

        foreach (var c in customers)
        {
            var validBookings = c.RentalBookings.ToList();
            decimal totalSpend = validBookings.Sum(b => b.RentalFee);
            int rentalCount = validBookings.Count;

            // Determine matching tier (highest threshold met)
            var currentTier = tiers
                .Where(t => totalSpend >= t.MinLifetimeSpend && rentalCount >= t.MinRentalCount)
                .OrderByDescending(t => t.MinLifetimeSpend)
                .FirstOrDefault() ?? tiers.First();

            // Next tier progression
            var nextTier = tiers
                .Where(t => t.MinLifetimeSpend > currentTier.MinLifetimeSpend)
                .OrderBy(t => t.MinLifetimeSpend)
                .FirstOrDefault();

            decimal remainingSpend = 0;
            int progressPct = 100;

            if (nextTier != null)
            {
                decimal range = nextTier.MinLifetimeSpend - currentTier.MinLifetimeSpend;
                decimal progress = totalSpend - currentTier.MinLifetimeSpend;
                remainingSpend = Math.Max(0, nextTier.MinLifetimeSpend - totalSpend);

                progressPct = range > 0
                    ? Math.Clamp((int)Math.Round((progress / range) * 100m), 0, 99)
                    : 100;
            }

            customerLoyaltyList.Add(new CustomerLoyaltyDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FullName = $"{c.FirstName} {c.LastName}".Trim(),
                ContactNumber = c.ContactNumber,
                CurrentTier = currentTier.TierName,
                DiscountPercentage = currentTier.DiscountPercentage,
                LifetimeSpend = totalSpend,
                CompletedRentalsCount = rentalCount,
                NextTier = nextTier?.TierName,
                NextTierSpendRemaining = remainingSpend,
                ProgressPercentage = progressPct
            });
        }

        // Apply client search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            customerLoyaltyList = [.. customerLoyaltyList
                .Where(c => c.FullName.ToLower().Contains(term) ||
                            c.CustomerCode.ToLower().Contains(term) ||
                            c.CurrentTier.ToLower().Contains(term))];
        }

        customerLoyaltyList = [.. customerLoyaltyList.OrderByDescending(c => c.LifetimeSpend)];

        // Assemble Tier DTOs with active enrolled counts
        var tierDtos = tiers.Select(t => new LoyaltyTierDto
        {
            LoyaltyAwardId = t.LoyaltyAwardId,
            TierName = t.TierName,
            MinLifetimeSpend = t.MinLifetimeSpend,
            MinRentalCount = t.MinRentalCount,
            DiscountPercentage = t.DiscountPercentage,
            RewardDescription = t.RewardDescription,
            EnrolledMembersCount = customerLoyaltyList.Count(c => string.Equals(c.CurrentTier, t.TierName, StringComparison.OrdinalIgnoreCase))
        }).ToList();

        return new LoyaltyOverviewDto
        {
            TotalClients = customers.Count,
            VipClientsCount = customerLoyaltyList.Count(c => c.CurrentTier.Equals("VIP", StringComparison.OrdinalIgnoreCase)),
            GoldClientsCount = customerLoyaltyList.Count(c => c.CurrentTier.Equals("Gold", StringComparison.OrdinalIgnoreCase)),
            StandardClientsCount = customerLoyaltyList.Count(c => c.CurrentTier.Equals("Standard", StringComparison.OrdinalIgnoreCase)),
            AverageSpendPerClient = customers.Count > 0 ? customerLoyaltyList.Average(c => c.LifetimeSpend) : 0,
            Tiers = tierDtos,
            Customers = customerLoyaltyList
        };
    }

    /// <summary>
    /// Computes the automatic booking discount for a client based on their loyalty qualification.
    /// </summary>
    public async Task<LoyaltyDiscountCalculationDto> CalculateCustomerDiscountAsync(int companyId, int customerId, decimal baseRentalFee)
    {
        await using var db = _contextFactory();

        var customer = await db.Customers
            .AsNoTracking()
            .Include(c => c.RentalBookings.Where(b => b.BookingStage != "Cancelled"))
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == companyId);

        if (customer == null)
        {
            return new LoyaltyDiscountCalculationDto
            {
                CustomerId = customerId,
                TierName = "Standard",
                DiscountPercentage = 0,
                OriginalRentalFee = baseRentalFee,
                DiscountAmount = 0,
                AdjustedRentalFee = baseRentalFee
            };
        }

        decimal totalSpend = customer.RentalBookings.Sum(b => b.RentalFee);
        int rentalCount = customer.RentalBookings.Count;

        var tiers = await db.LoyaltyAwards
            .AsNoTracking()
            .OrderByDescending(t => t.MinLifetimeSpend)
            .ToListAsync();

        var matchedTier = tiers.FirstOrDefault(t => totalSpend >= t.MinLifetimeSpend && rentalCount >= t.MinRentalCount);

        decimal discountPct = matchedTier?.DiscountPercentage ?? 0m;
        decimal discountAmount = Math.Round(baseRentalFee * (discountPct / 100m), 2);

        return new LoyaltyDiscountCalculationDto
        {
            CustomerId = customerId,
            TierName = matchedTier?.TierName ?? "Standard",
            DiscountPercentage = discountPct,
            OriginalRentalFee = baseRentalFee,
            DiscountAmount = discountAmount,
            AdjustedRentalFee = Math.Max(0, baseRentalFee - discountAmount)
        };
    }

    /// <summary>
    /// Updates or creates a loyalty tier rule configuration.
    /// </summary>
    public async Task SaveTierRuleAsync(LoyaltyTierDto tierDto)
    {
        await using var db = _contextFactory();

        var existing = await db.LoyaltyAwards.FirstOrDefaultAsync(t => t.LoyaltyAwardId == tierDto.LoyaltyAwardId);
        if (existing != null)
        {
            existing.TierName = tierDto.TierName;
            existing.MinLifetimeSpend = tierDto.MinLifetimeSpend;
            existing.MinRentalCount = tierDto.MinRentalCount;
            existing.DiscountPercentage = tierDto.DiscountPercentage;
            existing.RewardDescription = tierDto.RewardDescription;
        }
        else
        {
            db.LoyaltyAwards.Add(new LoyaltyAward
            {
                TierName = tierDto.TierName,
                MinLifetimeSpend = tierDto.MinLifetimeSpend,
                MinRentalCount = tierDto.MinRentalCount,
                DiscountPercentage = tierDto.DiscountPercentage,
                RewardDescription = tierDto.RewardDescription
            });
        }

        await db.SaveChangesAsync();
    }
}