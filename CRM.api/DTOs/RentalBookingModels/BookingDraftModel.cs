using CRM.domain.entities;

namespace CRM.api.DTOs;

public class BookingDraftModel
{
    public Customer? SelectedCustomer { get; set; }
    public List<Garment> SelectedGarments { get; set; } = new();

    public DateTime RentalStartDate { get; set; } = DateTime.Today;
    public DateTime RentalEndDate { get; set; } = DateTime.Today.AddDays(7);

    public decimal FittingBust { get; set; }
    public decimal FittingWaist { get; set; }
    public decimal FittingHip { get; set; }
    public string? AlterationNotes { get; set; }

    public string SelectedPaymentMethod { get; set; } = "Gcash";

    // Loyalty Tier & Discount Fields
    public string LoyaltyTierName { get; set; } = "Standard";
    public decimal LoyaltyDiscountPercentage { get; set; } = 0m;

    // Financial Computations
    public int RentalDurationDays => Math.Max(1, (RentalEndDate.Date - RentalStartDate.Date).Days);

    public decimal SubtotalRentalFee => SelectedGarments.Sum(g => g.RentalRate);

    public decimal LoyaltyDiscountAmount => Math.Round(SubtotalRentalFee * (LoyaltyDiscountPercentage / 100m), 2);

    public decimal TotalRentalFee => Math.Max(0, SubtotalRentalFee - LoyaltyDiscountAmount);

    public decimal TotalSecurityDeposit => SelectedGarments.Sum(g => g.SecurityDeposit);

    public decimal TotalDue => TotalRentalFee + TotalSecurityDeposit;

    public bool AgreedToTerms { get; set; }
}