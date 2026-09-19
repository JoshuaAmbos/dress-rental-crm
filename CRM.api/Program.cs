using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM.api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// =============================================================
// 1. SERVICES & DEPENDENCY INJECTION CONFIGURATION
// =============================================================

// Serialization: Prevent infinite navigation cycles across relational entities
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// DbContext Registrations
builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MasterCrm")));

builder.Services.AddDbContext<TenantCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TenantCrm")));

// ASP.NET Core Identity (User, Password Security, and Role Management)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<MasterCrmDbContext>()
.AddDefaultTokenProviders();

// Dynamic Multi-Tenant Resolution Services
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

var app = builder.Build();

// =============================================================
// 2. DATABASE STARTUP INITIALIZATION & ROLE SEEDING
// =============================================================

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Seed roles defined in AppRoles.AllRoles
    foreach (var role in AppRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed root Super Admin account if missing
    var superAdminEmail = "superadmin@crm.local";
    var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
    if (superAdmin == null)
    {
        var adminUser = new IdentityUser
        {
            UserName = "superadmin",
            Email = superAdminEmail
        };

        var result = await userManager.CreateAsync(adminUser, "SuperSecret123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Superadmin);
        }
    }
}

// =============================================================
// 3. AUTHENTICATION & IDENTITY ENDPOINTS
// =============================================================

// POST: Authenticate user credentials and return roles
app.MapPost("/api/auth/login", async (
    LoginRequestDto request,
    UserManager<IdentityUser> userManager) =>
{
    // 1. Locate user by username
    var user = await userManager.FindByNameAsync(request.Username);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    // 2. Validate salted hash password
    var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
    if (!isPasswordValid)
    {
        return Results.Unauthorized();
    }

    // 3. Retrieve user roles for client-side authorization
    var roles = await userManager.GetRolesAsync(user);

    return Results.Ok(new LoginResponseDto
    {
        UserId = user.Id,
        Username = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Roles = roles,
        Message = "Login successful."
    });
});

// POST: Administrative account registration (Admin / Super Admin)
app.MapPost("/api/users/register", async (
    CreateUserRequestDto request,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) =>
{
    if (!AppRoles.AllRoles.Contains(request.Role))
    {
        return Results.BadRequest(new { message = $"Role '{request.Role}' is invalid." });
    }

    var existingUser = await userManager.FindByNameAsync(request.Username);
    if (existingUser != null)
    {
        return Results.Conflict(new { message = "Username already taken." });
    }

    var user = new IdentityUser
    {
        UserName = request.Username,
        Email = request.Email
    };

    var createResult = await userManager.CreateAsync(user, request.Password);
    if (!createResult.Succeeded)
    {
        return Results.BadRequest(createResult.Errors);
    }

    await userManager.AddToRoleAsync(user, request.Role);

    return Results.Created($"/api/users/{user.Id}", new
    {
        userId = user.Id,
        username = user.UserName,
        email = user.Email,
        role = request.Role
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

// POST: Create a new Rental Booking with defensive validation
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

// GET: Retrieve all active and pending bookings with related data
app.MapGet("/tenant/{companyId:int}/bookings", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var bookings = await tenantDb.RentalBookings
        .Include(b => b.Customer)
        .Include(b => b.BookingDetails)
            .ThenInclude(d => d.RentalItem)
        .AsNoTracking()
        .OrderByDescending(b => b.RentalStartDate)
        .ToListAsync();

    return Results.Ok(bookings);
});

app.Run();