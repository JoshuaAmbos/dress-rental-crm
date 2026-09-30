using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public class TenantProvisioningService(MasterCrmDbContext masterDb, IConfiguration config)
{
    private readonly MasterCrmDbContext _masterDb = masterDb;
    private readonly IConfiguration _config = config;

    public async Task<Company> ProvisionTenantAsync(
        string companyCode,
        string companyName,
        int subscriptionPackageId,
        string initialBranchName = "Flagship Atelier",
        string initialCity = "Main City")
    {
        var sanitizedCode = new string(companyCode.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
        var databaseName = $"DB_Tenant_{sanitizedCode}";
        var server = _config["DatabaseSettings:Server"] ?? "localhost,1433";
        var saPassword = _config["DatabaseSettings:SaPassword"] ?? "YourStrong@Passw0rd!";

        // Create the tenant record in Master CRM
        var company = new Company
        {
            CompanyCode = sanitizedCode,
            CompanyName = companyName.Trim(),
            SubscriptionPackageId = subscriptionPackageId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _masterDb.Companies.Add(company);
        await _masterDb.SaveChangesAsync();

        // Register Database routing entry in Master CRM
        var companyDb = new CompanyDatabase
        {
            CompanyId = company.CompanyId,
            ServerName = server,
            DatabaseName = databaseName,
            CredentialKey = "DefaultKey",
            IsActive = true
        };
        _masterDb.CompanyDatabases.Add(companyDb);
        await _masterDb.SaveChangesAsync();

        // Dynamically provision the physical SQL Server database
        var masterConnStr = $"Server={server};Database=master;User Id=sa;Password={saPassword};TrustServerCertificate=True;";
        await using (var conn = new SqlConnection(masterConnStr))
        {
            await conn.OpenAsync();
            var sql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{databaseName}') CREATE DATABASE [{databaseName}];";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // Initialize schema on the new tenant database via EF Core
        var tenantConnStr = $"Server={server};Database={databaseName};User Id=sa;Password={saPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;";
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(tenantConnStr)
            .Options;

        await using (var tenantContext = new TenantCrmDbContext(options))
        {
            await tenantContext.Database.EnsureCreatedAsync();

            // Seed default primary showroom branch
            var defaultBranch = new Branch
            {
                CompanyId = company.CompanyId,
                BranchCode = $"{sanitizedCode}-MAIN",
                BranchName = initialBranchName,
                City = initialCity,
                Address = "Central Commercial District",
                ContactPhone = "+63 2 8000 0000",
                IsActive = true
            };
            tenantContext.Branches.Add(defaultBranch);
            await tenantContext.SaveChangesAsync();
        }

        return company;
    }
}