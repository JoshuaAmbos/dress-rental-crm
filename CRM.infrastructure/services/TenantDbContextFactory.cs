using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CRM.infrastructure.data;

namespace CRM.infrastructure.services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantDatabaseResolver _resolver;
    private readonly IConfiguration _configuration;

    public TenantDbContextFactory(
        ITenantDatabaseResolver resolver,
        IConfiguration configuration)
    {
        _resolver = resolver;
        _configuration = configuration;
    }

    public async Task<TenantCrmDbContext> CreateAsync(int companyId)
    {
        var databaseInfo = await _resolver.GetDatabaseInfoAsync(companyId);

        var userId = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];
        var password = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:Password"];

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Credentials not found for key '{databaseInfo.CredentialKey}'.");
        }

        var connectionString =
            $"Server={databaseInfo.ServerName};" +
            $"Database={databaseInfo.DatabaseName};" +
            $"User Id={userId};" +
            $"Password={password};" +
            $"Encrypt=False;" +
            $"TrustServerCertificate=True;" +
            $"MultipleActiveResultSets=True;";

        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TenantCrmDbContext(options);
    }
}