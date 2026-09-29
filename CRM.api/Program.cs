using CRM.api.DTOs;
using CRM.api.Services;
using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =============================================================
// 1. SERVICES & DEPENDENCY INJECTION CONFIGURATION
// =============================================================

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MasterCrm")));

builder.Services.AddDbContext<TenantCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TenantCrm")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<MasterCrmDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

var app = builder.Build();

// =============================================================
// 2. DATABASE STARTUP INITIALIZATION & ROLE/TENANT SEEDING
// =============================================================

using (var scope = app.Services.CreateScope())
{
    var masterDb = scope.ServiceProvider.GetRequiredService<MasterCrmDbContext>();
    await masterDb.Database.EnsureCreatedAsync();

    var tenantDb = scope.ServiceProvider.GetRequiredService<TenantCrmDbContext>();
    await tenantDb.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Seed roles
    foreach (var role in AppRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Helper to seed users with Tenant Claims
    async Task SeedTenantUserAsync(string username, string email, string password, string role, int companyId, string companyName)
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
            }
        }
        else
        {
            var existingClaims = await userManager.GetClaimsAsync(user);
            foreach (var claim in existingClaims.Where(c => c.Type == "CompanyId" || c.Type == "CompanyName"))
            {
                await userManager.RemoveClaimAsync(user, claim);
            }

            await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
        }
    }

    // Tenant 1 User: Atelier Haute Couture
    await SeedTenantUserAsync(
        username: "superadmin",
        email: "superadmin@crm.local",
        password: "SuperSecret123!",
        role: AppRoles.Superadmin,
        companyId: 1,
        companyName: "Atelier Haute Couture"
    );

    // Tenant 2 User: Maison Étoile Bridal
    await SeedTenantUserAsync(
        username: "maison_admin",
        email: "admin@maisonetoile.local",
        password: "SuperSecret123!",
        role: AppRoles.Admin,
        companyId: 2,
        companyName: "Maison Étoile Bridal"
    );
}

// =============================================================
// 3. AUTHENTICATION & IDENTITY ENDPOINTS
// =============================================================

app.MapPost("/api/auth/login", async (
    LoginRequestDto request,
    UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByNameAsync(request.Username);
    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        return Results.Unauthorized();
    }

    var roles = await userManager.GetRolesAsync(user);

    int companyId = user.UserName.Equals("maison_admin", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
    string companyName = companyId == 2 ? "Maison Étoile Bridal" : "Atelier Haute Couture";

    return Results.Ok(new LoginResult
    {
        UserId = user.Id,
        Username = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        CompanyId = companyId,
        CompanyName = companyName,
        Roles = roles.ToArray()
    });
});

// =============================================================
// 4. MASTER CRM INFRASTRUCTURE ENDPOINTS
// =============================================================

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
// 5. TENANT CATALOG & DIAGNOSTIC ENDPOINTS
// =============================================================

app.MapGet("/test-tenant/{companyId:int}", async (int companyId, ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    var rentalItemCount = await tenantDb.RentalItems.CountAsync();
    return Results.Ok(new { companyId, rentalItemCount });
});

app.MapPost("/tenant/{companyId:int}/rental-items", async (
    int companyId,
    Garment rentalItem,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    tenantDb.RentalItems.Add(rentalItem);
    await tenantDb.SaveChangesAsync();
    return Results.Created($"/tenant/{companyId}/rental-items/{rentalItem.GarmentId}", rentalItem);
});

app.MapGet("/tenant/{companyId:int}/rental-items", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    var items = await tenantDb.RentalItems
        .AsNoTracking()
        .OrderBy(x => x.GarmentId)
        .ToListAsync();
    return Results.Ok(items);
});

// =============================================================
// 6. TENANT CUSTOMER PROFILE ENDPOINTS (UC-01)
// =============================================================

app.MapPost("/tenant/{companyId:int}/customers", async (
    int companyId,
    Customer customer,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    tenantDb.Customers.Add(customer);
    await tenantDb.SaveChangesAsync();

    return Results.Created(
        $"/tenant/{companyId}/customers/{customer.CustomerId}",
        customer);
});

app.MapGet("/tenant/{companyId:int}/customers", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    var customers = await tenantDb.Customers
        .AsNoTracking()
        .OrderBy(x => x.CustomerId)
        .ToListAsync();

    return Results.Ok(customers);
});

// =============================================================
// 7. TENANT RENTAL BOOKINGS PIPELINE ENDPOINTS (UC-02)
// =============================================================

app.MapPost("/tenant/{companyId:int}/bookings", async (
    int companyId,
    RentalBooking booking,
    ITenantDbContextFactory tenantFactory) =>
{
    if (!booking.AgreedToTerms)
    {
        return Results.BadRequest(new { error = "Terms and conditions must be acknowledged before processing a rental." });
    }

    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var customerExists = await tenantDb.Customers.AnyAsync(c => c.CustomerId == booking.CustomerId);
    if (!customerExists)
    {
        return Results.NotFound(new { error = $"Customer with ID {booking.CustomerId} not found." });
    }

    booking.CreatedAt = DateTime.UtcNow;
    tenantDb.RentalBookings.Add(booking);
    await tenantDb.SaveChangesAsync();

    return Results.Created($"/tenant/{companyId}/bookings/{booking.RentalBookingId}", booking);
});

app.MapGet("/tenant/{companyId:int}/bookings", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var activeStages = new[] { "Fitting", "Reserved", "Active", "Overdue" };

    var bookings = await tenantDb.RentalBookings
        .Include(b => b.Customer)
        .Include(b => b.BookingDetails)
            .ThenInclude(d => d.Garment)
        .AsNoTracking()
        .Where(b => b.CompanyId == companyId && activeStages.Contains(b.BookingStage))
        .OrderByDescending(b => b.RentalStartDate)
        .ToListAsync();

    return Results.Ok(bookings);
});

app.Run();
