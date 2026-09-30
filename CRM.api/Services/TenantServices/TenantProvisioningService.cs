using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public class TenantProvisioningService
{
    private readonly MasterCrmDbContext _masterDb;
    private readonly IConfiguration? _configuration;

    public TenantProvisioningService(MasterCrmDbContext masterDb, IConfiguration? configuration = null)
    {
        _masterDb = masterDb ?? throw new ArgumentNullException(nameof(masterDb));
        _configuration = configuration;
    }

    public async Task<Company> ProvisionTenantAsync(
        string companyCode,
        string companyName,
        int subscriptionPackageId,
        string initialBranchName,
        string initialBranchCity)
    {
        // 1. Resolve host IP: Reads .env (10.0.2.2 on Windows VM, localhost on Ubuntu)
        var server = Environment.GetEnvironmentVariable("DB_SERVER")
                     ?? _configuration?["DatabaseSettings:Server"]
                     ?? (OperatingSystem.IsWindows() ? "10.0.2.2,1433" : "localhost,1433");

        var saPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                         ?? _configuration?["DatabaseSettings:SaPassword"]
                         ?? "YourStrong@Passw0rd!";

        var cleanCode = companyCode.Trim().ToUpperInvariant();
        var dbName = $"DB_Tenant_{cleanCode}";

        // 2. Physically create the SQL database on SQL Server
        var sysConnStr = $"Server={server};Database=master;User Id=sa;Password={saPassword};Encrypt=False;TrustServerCertificate=True;";
        await using (var conn = new SqlConnection(sysConnStr))
        {
            await conn.OpenAsync();
            var sql = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                BEGIN
                    CREATE DATABASE [{dbName}];
                END";

            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandTimeout = 180 // Avoid timeout during database creation
            };
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Apply EF Core Schema to the newly created database
        var tenantConnStr = $"Server={server};Database={dbName};User Id=sa;Password={saPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(tenantConnStr, sql => sql.CommandTimeout(180))
            .Options;

        await using (var tenantDb = new TenantCrmDbContext(tenantOptions))
        {
            await tenantDb.Database.EnsureCreatedAsync();

            // 4. Seed Initial Showroom Branch
            var initialBranch = new Branch
            {
                BranchCode = $"{cleanCode}-MAIN",
                BranchName = initialBranchName,
                City = initialBranchCity,
                IsActive = true
            };
            tenantDb.Branches.Add(initialBranch);

            // Seed default baseline configuration
            tenantDb.SystemConfigurations.AddRange(
                new SystemConfiguration { ConfigKey = "DefaultLateFeePerDay", ConfigValue = "500.00", Description = "Daily penalty for overdue returns" },
                new SystemConfiguration { ConfigKey = "StandardDepositPercentage", ConfigValue = "50", Description = "Security deposit percentage" }
            );

            await tenantDb.SaveChangesAsync();
        }

        // 5. Register Boutique in Master CRM
        var company = new Company
        {
            CompanyCode = cleanCode,
            CompanyName = companyName.Trim(),
            SubscriptionPackageId = subscriptionPackageId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _masterDb.Companies.Add(company);
        await _masterDb.SaveChangesAsync();

        // 6. Record Database Routing in Master CRM
        var dbRouting = new CompanyDatabase
        {
            CompanyId = company.CompanyId,
            ServerName = server,
            DatabaseName = dbName,
            CredentialKey = "DefaultKey",
            IsActive = true
        };
        _masterDb.CompanyDatabases.Add(dbRouting);
        await _masterDb.SaveChangesAsync();

        return company;
    }
}