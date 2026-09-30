namespace CRM.winforms.Configuration;

public static class DatabaseConfig
{
    public static string Server =>
        Environment.GetEnvironmentVariable("DB_SERVER") ?? "10.0.2.2,1433";

    public static string User =>
        Environment.GetEnvironmentVariable("DB_USER") ?? "sa";

    public static string Password =>
        Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong@Passw0rd!";

    public static string MasterConnectionString =>
        Environment.GetEnvironmentVariable("MASTER_CRM_CONNECTION")
        ?? $"Server={Server};Database=DB_MasterCRM;User Id={User};Password={Password};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    /// <summary>
    /// Builds a dynamic connection string for the active tenant's isolated database.
    /// </summary>
    public static string BuildTenantConnectionString(string? databaseName, string? serverOverride = null)
    {
        var targetDb = !string.IsNullOrWhiteSpace(databaseName) ? databaseName : "DB_TenantCRM";
        var host = !string.IsNullOrWhiteSpace(serverOverride)
            ? serverOverride.Replace("localhost", Server)
            : Server;

        return $"Server={host};Database={targetDb};User Id={User};Password={Password};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }
}