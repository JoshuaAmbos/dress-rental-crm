namespace CRM.domain.entities;

public class LoyaltyAward
{
    public int LoyaltyAwardId { get; set; }
    public string TierName { get; set; } = string.Empty; // Standard, Bronze, Silver, VIP
    public decimal MinLifetimeSpend { get; set; }
    public int MinRentalCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string RewardDescription { get; set; } = string.Empty;
}