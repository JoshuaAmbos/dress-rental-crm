using CRM.infrastructure.data;

namespace CRM.api.Services;

public interface ITenantDbContextFactory
{
    Task<TenantCrmDbContext> CreateAsync(int companyId);
}