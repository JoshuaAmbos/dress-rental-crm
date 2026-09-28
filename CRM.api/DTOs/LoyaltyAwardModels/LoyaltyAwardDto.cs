namespace CRM.api.DTOs;

public class LoyaltyTierDto
{
    public int LoyaltyAwardId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public decimal MinLifetimeSpend { get; set; }
    public int MinRentalCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string RewardDescription { get; set; } = string.Empty;
    public int EnrolledMembersCount { get; set; }
}

public class CustomerLoyaltyDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string CurrentTier { get; set; } = "Standard";
    public decimal DiscountPercentage { get; set; }
    public decimal LifetimeSpend { get; set; }
    public int CompletedRentalsCount { get; set; }
    public string? NextTier { get; set; }
    public decimal NextTierSpendRemaining { get; set; }
    public int ProgressPercentage { get; set; }
}

public class LoyaltyOverviewDto
{
    public int TotalClients { get; set; }
    public int VipClientsCount { get; set; }
    public int GoldClientsCount { get; set; }
    public int StandardClientsCount { get; set; }
    public decimal AverageSpendPerClient { get; set; }
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
    public List<CustomerLoyaltyDto> Customers { get; set; } = new();
}

public class LoyaltyDiscountCalculationDto
{
    public int CustomerId { get; set; }
    public string TierName { get; set; } = "Standard";
    public decimal DiscountPercentage { get; set; }
    public decimal OriginalRentalFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AdjustedRentalFee { get; set; }
}