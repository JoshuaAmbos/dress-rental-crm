using CRM.api.Endpoints;
using CRM.domain.Constants;
using CRM.domain.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;

// Load environment variables from .env file if present
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Allow Windows VM or external devices to connect via Ubuntu host gateway
builder.WebHost.UseUrls("http://0.0.0.0:5171");

// =============================================================
// 1. SERVICES & DEPENDENCY INJECTION CONFIGURATION
// =============================================================

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var masterConn = Environment.GetEnvironmentVariable("MASTER_CRM_CONNECTION")
                 ?? builder.Configuration.GetConnectionString("MasterCrm")
                 ?? "Server=localhost,1433;Database=DB_MasterCRM;User Id=sa;Password=YourStrong@Passw0rd!;Encrypt=False;TrustServerCertificate=True;";

var tenantConn = Environment.GetEnvironmentVariable("TENANT_CRM_CONNECTION")
                 ?? builder.Configuration.GetConnectionString("TenantCrm")
                 ?? "Server=localhost,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;Encrypt=False;TrustServerCertificate=True;";

// If running natively on Linux/Ubuntu, force 10.0.2.2 back to localhost
if (OperatingSystem.IsLinux())
{
    masterConn = masterConn.Replace("10.0.2.2", "localhost");
    tenantConn = tenantConn.Replace("10.0.2.2", "localhost");
}

builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(masterConn, sql => sql.CommandTimeout(60)));

builder.Services.AddDbContext<TenantCrmDbContext>(options =>
    options.UseSqlServer(tenantConn, sql => sql.CommandTimeout(60)));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<MasterCrmDbContext>()
.AddDefaultTokenProviders();

// Dynamic Multi-Tenant Infrastructure Services
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
builder.Services.AddScoped<TenantProvisioningService>();

var app = builder.Build();

// =============================================================
// 2. DATABASE STARTUP INITIALIZATION & MULTI-TENANT SEEDING
// =============================================================

using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var server = Environment.GetEnvironmentVariable("DB_SERVER")
                 ?? config["DatabaseSettings:Server"]
                 ?? "localhost,1433";

    if (OperatingSystem.IsLinux())
    {
        server = server.Replace("10.0.2.2", "localhost");
    }

    var saPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                     ?? config["DatabaseSettings:SaPassword"]
                     ?? "YourStrong@Passw0rd!";

    await DatabaseSeeder.ProvisionAndSeedAllTenantsAsync(server, saPassword);

    // 1. Provision Master CRM, Subscription Packages, and the 3 Isolated Tenant Databases
    await DatabaseSeeder.ProvisionAndSeedAllTenantsAsync(server, saPassword);

    // 2. Seed Identity Roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in AppRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // 3. User Provisioning Helper with Showroom Branch Claims
    async Task EnsureUserAsync(
        string username,
        string email,
        string password,
        string role,
        int companyId,
        string companyName,
        string branchName = "All Showrooms")
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            user = new IdentityUser { UserName = username, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
                await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
                await userManager.AddClaimAsync(user, new Claim("BranchName", branchName));
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            var claims = await userManager.GetClaimsAsync(user);
            foreach (var c in claims.Where(c => c.Type is "CompanyId" or "CompanyName" or "BranchName"))
            {
                await userManager.RemoveClaimAsync(user, c);
            }

            await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
            await userManager.AddClaimAsync(user, new Claim("BranchName", branchName));
        }
    }

    // Tenant 1: Atelier Haute Couture (Package A - Multi-Branch)
    await EnsureUserAsync("superadmin", "superadmin@crm.local", "SuperSecret123!", AppRoles.Superadmin, 1, "Atelier Haute Couture", "Global Corporate");
    await EnsureUserAsync("atelier_admin", "admin@atelier.local", "SuperSecret123!", AppRoles.Admin, 1, "Atelier Haute Couture", "All Showrooms");
    await EnsureUserAsync("atelier_manager", "manager@atelier.local", "SuperSecret123!", AppRoles.Manager, 1, "Atelier Haute Couture", "Flagship Atelier (Makati)");
    await EnsureUserAsync("atelier_staff", "staff@atelier.local", "SuperSecret123!", AppRoles.Staff, 1, "Atelier Haute Couture", "Flagship Atelier (Makati)");

    // Tenant 2: Maison Étoile Bridal (Package B - Single-Branch)
    await EnsureUserAsync("maison_admin", "admin@maisonetoile.local", "SuperSecret123!", AppRoles.Admin, 2, "Maison Étoile Bridal", "Maison Étoile Atelier");
    await EnsureUserAsync("maison_manager", "manager@maisonetoile.local", "SuperSecret123!", AppRoles.Manager, 2, "Maison Étoile Bridal", "Maison Étoile Atelier");
    await EnsureUserAsync("maison_staff", "staff@maisonetoile.local", "SuperSecret123!", AppRoles.Staff, 2, "Maison Étoile Bridal", "Maison Étoile Atelier");

    // Tenant 3: Davao Haute Rentals (Package C - Starter)
    await EnsureUserAsync("davao_admin", "admin@davaohaute.local", "SuperSecret123!", AppRoles.Admin, 3, "Davao Haute Rentals", "Davao Haute Flagship");
    await EnsureUserAsync("davao_manager", "manager@davaohaute.local", "SuperSecret123!", AppRoles.Manager, 3, "Davao Haute Rentals", "Davao Haute Flagship");
    await EnsureUserAsync("davao_staff", "staff@davaohaute.local", "SuperSecret123!", AppRoles.Staff, 3, "Davao Haute Rentals", "Davao Haute Flagship");
}

// =============================================================
// 3. AUTHENTICATION & IDENTITY ENDPOINTS
// =============================================================

app.MapAuthEndpoints();

// =============================================================
// 4. SUPERADMIN PROVISIONING & TENANT MANAGEMENT
// =============================================================

app.MapPost("/api/superadmin/tenants", async (
    CreateTenantRequestDto dto,
    TenantProvisioningService provisioningService) =>
{
    var createdTenant = await provisioningService.ProvisionTenantAsync(
        dto.CompanyCode,
        dto.CompanyName,
        dto.SubscriptionPackageId,
        dto.InitialBranchName,
        dto.InitialBranchCity);

    return Results.Created($"/api/superadmin/tenants/{createdTenant.CompanyId}", createdTenant);
});

app.MapGet("/api/superadmin/tenants", async (MasterCrmDbContext db) =>
{
    var tenants = await db.Companies
        .Include(c => c.SubscriptionPackage)
        .Include(c => c.CompanyDatabases)
        .OrderByDescending(c => c.IsActive)
        .ThenBy(c => c.CompanyName)
        .ToListAsync();

    return Results.Ok(tenants);
});

app.MapDelete("/api/superadmin/tenants/{companyId:int}", async (int companyId, MasterCrmDbContext db) =>
{
    var tenant = await db.Companies.FindAsync(companyId);
    if (tenant == null) return Results.NotFound();

    tenant.IsActive = !tenant.IsActive;
    tenant.DeactivatedAt = tenant.IsActive ? null : DateTime.UtcNow;

    var databases = await db.CompanyDatabases.Where(d => d.CompanyId == companyId).ToListAsync();
    foreach (var d in databases)
    {
        d.IsActive = tenant.IsActive;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { message = tenant.IsActive ? "Tenant reactivated" : "Tenant deactivated" });
});

// Master CRM Infrastructure Endpoints
app.MapPost("/companies", async (Company company, MasterCrmDbContext db) =>
{
    db.Companies.Add(company);
    await db.SaveChangesAsync();
    return Results.Created($"/companies/{company.CompanyId}", company);
});

app.MapPost("/devices", async (Device device, MasterCrmDbContext db) =>
{
    db.Devices.Add(device);
    await db.SaveChangesAsync();
    return Results.Created($"/devices/{device.DeviceId}", device);
});

app.MapPost("/company-databases", async (CompanyDatabase companyDatabase, MasterCrmDbContext db) =>
{
    db.CompanyDatabases.Add(companyDatabase);
    await db.SaveChangesAsync();
    return Results.Created($"/company-databases/{companyDatabase.CompanyDatabaseId}", companyDatabase);
});

// =============================================================
// 5. MODULAR TENANT CRM ENDPOINTS
// =============================================================

app.MapTenantEndpoints();

app.Run();