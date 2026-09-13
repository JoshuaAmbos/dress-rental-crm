using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Prevent infinite navigation property cycles during JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Add DbContexts
builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MasterCrm")));

builder.Services.AddDbContext<TenantCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TenantCrm")));

// Register Dynamic Multi-Tenant Services
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

var app = builder.Build();

// Master Endpoints
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

// Dynamic Multi-Tenant Endpoints
app.MapGet("/test-tenant/{companyId:int}", async (int companyId, ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    var rentalItemCount = await tenantDb.RentalItems.CountAsync();
    return Results.Ok(new { companyId, rentalItemCount });
});

app.MapPost("/tenant/{companyId:int}/rental-items", async (
    int companyId,
    RentalItem rentalItem,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    tenantDb.RentalItems.Add(rentalItem);
    await tenantDb.SaveChangesAsync();
    return Results.Created($"/tenant/{companyId}/rental-items/{rentalItem.RentalItemId}", rentalItem);
});

app.MapGet("/tenant/{companyId:int}/rental-items", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);
    var items = await tenantDb.RentalItems
        .AsNoTracking()
        .OrderBy(x => x.RentalItemId)
        .ToListAsync();
    return Results.Ok(items);
});

// Customer endpoints
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

// POST: Create a new Rental Booking with details
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

    // Verify Customer exists
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

// GET: Retrieve all active and pending bookings with customer details
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