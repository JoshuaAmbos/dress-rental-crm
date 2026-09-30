namespace CRM.domain.DTOs;

public class CreateTenantRequestDto
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int SubscriptionPackageId { get; set; }

    // Initial Tenant Admin Credentials
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    // Initial Flagship Showroom details
    public string InitialBranchName { get; set; } = "Initial Branch Name";
    public string InitialBranchCity { get; set; } = "Initial Branch City";
}

public class UpdateTenantDto
{
    public string CompanyName { get; set; } = string.Empty;
    public int SubscriptionPackageId { get; set; }
    public bool IsActive { get; set; }
}

public class TenantSummaryDto
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int SubscriptionPackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int MaxBranches { get; set; }
    public int ActiveBranchCount { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}

public class SubscriptionPackageDto
{
    public int SubscriptionPackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxBranches { get; set; }
    public bool HasLoyalty { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasMultiBranch { get; set; }
    public decimal MonthlyFee { get; set; }
}