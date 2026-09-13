using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.infrastructure.data;

public class TenantCrmDbContextFactory : IDesignTimeDbContextFactory<TenantCrmDbContext>
{
    public TenantCrmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantCrmDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;");

        return new TenantCrmDbContext(optionsBuilder.Options);
    }
}