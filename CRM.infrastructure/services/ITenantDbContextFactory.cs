using CRM.infrastructure.data;

namespace CRM.infrastructure.services;

public interface ITenantDbContextFactory
{
    Task<TenantCrmDbContext> CreateAsync(int companyId);
}