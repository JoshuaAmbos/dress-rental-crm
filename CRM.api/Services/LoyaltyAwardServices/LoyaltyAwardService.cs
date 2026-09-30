using CRM.api.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class LoyaltyAwardService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly EmailNotificationService _emailService;

    public LoyaltyAwardService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _emailService = new EmailNotificationService(_contextFactory);
    }

    public Task<LoyaltyOverviewDto> GetLoyaltyOverviewAsync(int companyId, string searchTerm = "")
        => GetLoyaltyOverviewAsync(companyId, null, searchTerm, false);

    public async Task<LoyaltyOverviewDto> GetLoyaltyOverviewAsync(
        int companyId,
        int? branchId = null,
        string searchTerm = "",
        bool includeArchivedTiers = false)
    {
        await using var db = _contextFactory();

        // 1. Fetch tier rules (filtered by active status unless admin toggled archive view)
        var tiersQuery = db.LoyaltyAwards.AsNoTracking();
        if (!includeArchivedTiers)
        {
            tiersQuery = tiersQuery.Where(t => t.IsActive);
        }

        var tiers = await tiersQuery
            .OrderBy(t => t.MinLifetimeSpend)
            .ToListAsync();

        // Auto-seed baseline tiers if the tenant's isolated table is completely empty
        if (tiers.Count == 0 && !includeArchivedTiers)
        {
            tiers =
            [
                new() { TierName = "Standard", MinLifetimeSpend = 0, MinRentalCount = 0, DiscountPercentage = 0m, RewardDescription = "Standard membership and new arrival notifications", IsActive = true },
                new() { TierName = "Gold", MinLifetimeSpend = 8000, MinRentalCount = 2, DiscountPercentage = 5m, RewardDescription = "5% off leases and complimentary fitting reservations", IsActive = true },
                new() { TierName = "VIP", MinLifetimeSpend = 15000, MinRentalCount = 3, DiscountPercentage = 10m, RewardDescription = "10% off leases, free alterations, and priority access", IsActive = true }
            ];

            db.LoyaltyAwards.AddRange(tiers);
            await db.SaveChangesAsync();
        }

        // Active tiers used for customer rank calculations
        var activeTiers = tiers.Where(t => t.IsActive).OrderBy(t => t.MinLifetimeSpend).ToList();

        // 2. Query customers scoped to the active boutique and showroom branch
        var customersQuery = db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive);

        if (branchId.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.BranchId == branchId.Value);
        }

        customersQuery = customersQuery.Include(c => c.RentalBookings.Where(b => b.BookingStage != "Cancelled"));

        var customers = await customersQuery.ToListAsync();
        var customerLoyaltyList = new List<CustomerLoyaltyDto>();

        foreach (var c in customers)
        {
            var validBookings = branchId.HasValue
                ? c.RentalBookings.Where(b => b.BranchId == branchId.Value).ToList()
                : [.. c.RentalBookings];

            decimal totalSpend = validBookings.Sum(b => b.RentalFee);
            int rentalCount = validBookings.Count;

            // Match highest active tier threshold met
            var currentTier = activeTiers
                .Where(t => totalSpend >= t.MinLifetimeSpend && rentalCount >= t.MinRentalCount)
                .OrderByDescending(t => t.MinLifetimeSpend)
                .FirstOrDefault() ?? activeTiers.FirstOrDefault() ?? new LoyaltyAward { TierName = "Standard" };

            // Determine next active tier progression
            var nextTier = activeTiers
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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            customerLoyaltyList = [.. customerLoyaltyList
                .Where(c => c.FullName.ToLower().Contains(term) ||
                            c.CustomerCode.ToLower().Contains(term) ||
                            c.CurrentTier.ToLower().Contains(term))];
        }

        customerLoyaltyList = [.. customerLoyaltyList.OrderByDescending(c => c.LifetimeSpend)];

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
            AverageSpendPerClient = customerLoyaltyList.Count > 0 ? customerLoyaltyList.Average(c => c.LifetimeSpend) : 0,
            Tiers = tierDtos,
            Customers = customerLoyaltyList
        };
    }

    public async Task<List<LoyaltyAward>> GetAllTiersAsync(bool includeArchived = false)
    {
        await using var db = _contextFactory();
        var query = db.LoyaltyAwards.AsNoTracking();
        if (!includeArchived) query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.MinLifetimeSpend).ToListAsync();
    }

    public async Task CreateTierAsync(LoyaltyAward tier)
    {
        await using var db = _contextFactory();

        var duplicate = await db.LoyaltyAwards
            .AnyAsync(t => t.IsActive && t.TierName.ToLower() == tier.TierName.Trim().ToLower());

        if (duplicate)
            throw new InvalidOperationException($"An active loyalty tier named '{tier.TierName}' already exists.");

        tier.TierName = tier.TierName.Trim();
        tier.RewardDescription = tier.RewardDescription.Trim();
        tier.IsActive = true;
        tier.CreatedAt = DateTime.UtcNow;

        db.LoyaltyAwards.Add(tier);
        await db.SaveChangesAsync();
    }

    public async Task UpdateTierAsync(LoyaltyAward updated)
    {
        await using var db = _contextFactory();
        var existing = await db.LoyaltyAwards.FindAsync(updated.LoyaltyAwardId);
        if (existing == null) throw new KeyNotFoundException("Loyalty tier not found.");

        var duplicate = await db.LoyaltyAwards
            .AnyAsync(t => t.LoyaltyAwardId != updated.LoyaltyAwardId &&
                           t.IsActive &&
                           t.TierName.ToLower() == updated.TierName.Trim().ToLower());

        if (duplicate)
            throw new InvalidOperationException($"Another active tier is already named '{updated.TierName}'.");

        existing.TierName = updated.TierName.Trim();
        existing.MinLifetimeSpend = updated.MinLifetimeSpend;
        existing.MinRentalCount = updated.MinRentalCount;
        existing.DiscountPercentage = updated.DiscountPercentage;
        existing.RewardDescription = updated.RewardDescription.Trim();
        existing.IsActive = updated.IsActive;

        await db.SaveChangesAsync();
    }

    public async Task ToggleTierStatusAsync(int loyaltyAwardId)
    {
        await using var db = _contextFactory();
        var tier = await db.LoyaltyAwards.FindAsync(loyaltyAwardId);
        if (tier == null) return;

        // Guard the foundational Standard tier
        if (tier.TierName.Equals("Standard", StringComparison.OrdinalIgnoreCase) && tier.IsActive)
        {
            throw new InvalidOperationException("The baseline 'Standard' tier cannot be archived as it serves as the zero-threshold tier.");
        }

        tier.IsActive = !tier.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task<LoyaltyDiscountCalculationDto> CalculateCustomerDiscountAsync(
        int companyId, int customerId, decimal baseRentalFee, int? branchId = null)
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

        var validBookings = branchId.HasValue
            ? customer.RentalBookings.Where(b => b.BranchId == branchId.Value).ToList()
            : [.. customer.RentalBookings];

        decimal totalSpend = validBookings.Sum(b => b.RentalFee);
        int rentalCount = validBookings.Count;

        // Query only active tiers for discounting
        var tiers = await db.LoyaltyAwards
            .AsNoTracking()
            .Where(t => t.IsActive)
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
    /// Saves or updates a loyalty tier from a DTO, routing to Create or Update accordingly.
    /// </summary>
    public async Task SaveTierRuleAsync(LoyaltyTierDto tierDto)
    {
        if (tierDto.LoyaltyAwardId > 0)
        {
            await UpdateTierAsync(new LoyaltyAward
            {
                LoyaltyAwardId = tierDto.LoyaltyAwardId,
                TierName = tierDto.TierName,
                MinLifetimeSpend = tierDto.MinLifetimeSpend,
                MinRentalCount = tierDto.MinRentalCount,
                DiscountPercentage = tierDto.DiscountPercentage,
                RewardDescription = tierDto.RewardDescription,
                IsActive = true
            });
        }
        else
        {
            await CreateTierAsync(new LoyaltyAward
            {
                TierName = tierDto.TierName,
                MinLifetimeSpend = tierDto.MinLifetimeSpend,
                MinRentalCount = tierDto.MinRentalCount,
                DiscountPercentage = tierDto.DiscountPercentage,
                RewardDescription = tierDto.RewardDescription,
                IsActive = true
            });
        }
    }
}