namespace CRM.api.Services;

public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
}