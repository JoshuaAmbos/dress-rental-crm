using CRM.api.Endpoints;
using CRM.api.Extensions;
using CRM.domain.Constants;
using CRM.api.Services;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Security.Claims;
using CRM.api.DTOs;
using CRM.domain.entities;

var builder = WebApplication.CreateBuilder(args);

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
// 2. DATABASE STARTUP INITIALIZATION & ROLE/TENANT USER SEEDING
// =============================================================
using (var scope = app.Services.CreateScope())
{
    var masterDb = scope.ServiceProvider.GetRequiredService<MasterCrmDbContext>();
    await masterDb.Database.EnsureCreatedAsync();

    var tenantDb = scope.ServiceProvider.GetRequiredService<TenantCrmDbContext>();
    await tenantDb.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in AppRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    async Task EnsureUserAsync(string username, string email, string password, string role, int companyId, string companyName)
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
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            var claims = await userManager.GetClaimsAsync(user);
            foreach (var c in claims.Where(c => c.Type == "CompanyId" || c.Type == "CompanyName"))
            {
                await userManager.RemoveClaimAsync(user, c);
            }
            await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
        }
    }

    // Tenant C Accounts: Atelier Haute Couture
    await EnsureUserAsync("superadmin", "superadmin@crm.local", "SuperSecret123!", AppRoles.Superadmin, 1, "Atelier Haute Couture");
    await EnsureUserAsync("atelier_admin", "admin@atelier.local", "SuperSecret123!", AppRoles.Admin, 1, "Atelier Haute Couture");
    await EnsureUserAsync("atelier_manager", "manager@atelier.local", "SuperSecret123!", AppRoles.Manager, 1, "Atelier Haute Couture");
    await EnsureUserAsync("atelier_staff", "staff@atelier.local", "SuperSecret123!", AppRoles.Staff, 1, "Atelier Haute Couture");

    // Tenant B Accounts: Maison Étoile Bridal
    await EnsureUserAsync("maison_admin", "admin@maisonetoile.local", "SuperSecret123!", AppRoles.Admin, 2, "Maison Étoile Bridal");
    await EnsureUserAsync("maison_manager", "manager@maisonetoile.local", "SuperSecret123!", AppRoles.Manager, 2, "Maison Étoile Bridal");
    await EnsureUserAsync("maison_staff", "staff@maisonetoile.local", "SuperSecret123!", AppRoles.Staff, 2, "Maison Étoile Bridal");

    // Tenant A Accounts: Davao Haute Rentals
    await EnsureUserAsync("davao_admin", "admin@davaohaute.local", "SuperSecret123!", AppRoles.Admin, 3, "Davao Haute Rentals");
    await EnsureUserAsync("davao_manager", "manager@davaohaute.local", "SuperSecret123!", AppRoles.Manager, 3, "Davao Haute Rentals");
    await EnsureUserAsync("davao_staff", "staff@davaohaute.local", "SuperSecret123!", AppRoles.Staff, 3, "Davao Haute Rentals");
}

// =============================================================
// 3. AUTHENTICATION & IDENTITY ENDPOINTS
// =============================================================

app.MapAuthEndpoints();

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