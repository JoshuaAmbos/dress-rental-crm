using CRM.api.Services;
using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/tenant/{companyId:int}");

        routes.MapGet("/test-tenant/{companyId:int}", async (int companyId, ITenantDbContextFactory factory) =>
        {
            await using var db = await factory.CreateAsync(companyId);
            var rentalItemCount = await db.RentalItems.CountAsync();
            return Results.Ok(new { companyId, rentalItemCount });
        });

        group.MapGet("/rental-items", async (int companyId, ITenantDbContextFactory factory) =>
        {
            await using var db = await factory.CreateAsync(companyId);
            var items = await db.RentalItems.AsNoTracking().OrderBy(x => x.GarmentId).ToListAsync();
            return Results.Ok(items);
        });

        group.MapGet("/customers", async (int companyId, ITenantDbContextFactory factory) =>
        {
            await using var db = await factory.CreateAsync(companyId);
            var customers = await db.Customers.AsNoTracking().OrderBy(x => x.CustomerId).ToListAsync();
            return Results.Ok(customers);
        });

        group.MapPost("/customers", async (int companyId, Customer customer, ITenantDbContextFactory factory) =>
        {
            await using var db = await factory.CreateAsync(companyId);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return Results.Created($"/tenant/{companyId}/customers/{customer.CustomerId}", customer);
        });

        group.MapGet("/bookings", async (int companyId, ITenantDbContextFactory factory) =>
        {
            await using var db = await factory.CreateAsync(companyId);
            var activeStages = new[] { "Fitting", "Reserved", "Active", "Overdue" };

            var bookings = await db.RentalBookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Garment)
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && activeStages.Contains(b.BookingStage))
                .OrderByDescending(b => b.RentalStartDate)
                .ToListAsync();

            return Results.Ok(bookings);
        });

        group.MapPost("/bookings", async (int companyId, RentalBooking booking, ITenantDbContextFactory factory) =>
        {
            if (!booking.AgreedToTerms)
                return Results.BadRequest(new { error = "Terms and conditions must be acknowledged before processing a rental." });

            await using var db = await factory.CreateAsync(companyId);
            var customerExists = await db.Customers.AnyAsync(c => c.CustomerId == booking.CustomerId);
            if (!customerExists)
                return Results.NotFound(new { error = $"Customer with ID {booking.CustomerId} not found." });

            booking.CreatedAt = DateTime.UtcNow;
            db.RentalBookings.Add(booking);
            await db.SaveChangesAsync();

            return Results.Created($"/tenant/{companyId}/bookings/{booking.RentalBookingId}", booking);
        });
    }
}