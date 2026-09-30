namespace CRM.domain.entities;

public class CompanyDatabase
{
    public int CompanyDatabaseId { get; set; }
    public int CompanyId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CredentialKey { get; set; } = "DefaultKey";
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Company? Company { get; set; }
}