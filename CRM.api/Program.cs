using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();