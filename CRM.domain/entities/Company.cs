namespace CRM.domain.entities;

public class Company
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int SubscriptionPackageId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public SubscriptionPackage? SubscriptionPackage { get; set; }
    public ICollection<CompanyDatabase> CompanyDatabases { get; set; } = [];
    public ICollection<Device> Devices { get; set; } = [];
}