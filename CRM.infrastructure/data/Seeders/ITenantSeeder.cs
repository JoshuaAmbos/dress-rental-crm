using System.Threading.Tasks;

namespace CRM.infrastructure.data.Seeders;

public interface ITenantSeeder
{
    int CompanyId { get; }
    string CompanyCode { get; }
    Task SeedAsync(TenantCrmDbContext db);
}