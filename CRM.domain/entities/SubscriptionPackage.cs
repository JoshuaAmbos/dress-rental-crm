namespace CRM.domain.entities;

public class SubscriptionPackage
{
    public int SubscriptionPackageId { get; set; }
    public string PackageName { get; set; } = string.Empty; // "Package A - Complete", "Package B - Growth", "Package C - Starter"
    public string Description { get; set; } = string.Empty;
    public int MaxBranches { get; set; } = 1;
    public bool HasLoyalty { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasMultiBranch { get; set; }
    public decimal MonthlyFee { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Company> Companies { get; set; } = [];
}