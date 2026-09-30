using CRM.infrastructure.services;

namespace CRM.infrastructure.services;

public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
}