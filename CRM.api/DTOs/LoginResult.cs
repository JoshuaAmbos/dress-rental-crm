namespace CRM.api.DTOs;

public class LoginResult
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];

    // Subscription Package Flags for Feature Gating
    public string PackageName { get; set; } = "Package A - Complete";
    public int MaxBranches { get; set; } = 1;
    public bool HasMultiBranch { get; set; } = true;
    public bool HasLoyalty { get; set; } = true;
    public bool HasAnalytics { get; set; } = true;

    // Database connection details resolved from Master DB
    public string DatabaseName { get; set; } = "DB_TenantCRM";
    public string ServerName { get; set; } = "localhost,1433";
}