namespace CRM.domain.entities;

public class SystemConfiguration
{
    public int SystemConfigurationId { get; set; }
    public string ConfigKey { get; set; } = string.Empty;   // DefaultLateFeePerDay, StandardDepositPct, CleaningFeeRate
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}