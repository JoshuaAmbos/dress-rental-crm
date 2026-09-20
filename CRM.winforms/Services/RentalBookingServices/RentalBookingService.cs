using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Models.RentalBookingModels;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Services.RentalBookingServices;

public class RentalBookingService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public RentalBookingService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<RentalPipelineDto> GetPipelineAsync(int companyId, string stageFilter = "All", string searchTerm = "")
    {
        await using var db = _contextFactory();
        var today = DateTime.Today;

        var query = db.RentalBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .Where(b => b.CompanyId == companyId);

        if (!string.Equals(stageFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(b => b.BookingStage == stageFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(b =>
                (b.Customer != null && (
                    b.Customer.FirstName.ToLower().Contains(term) ||
                    b.Customer.LastName.ToLower().Contains(term) ||
                    b.Customer.ContactNumber.Contains(term))) ||
                b.BookingDetails.Any(d => d.Garment != null && (
                    d.Garment.StyleName.ToLower().Contains(term) ||
                    d.Garment.ItemCode.ToLower().Contains(term))));
        }

        var bookings = await query
            .OrderByDescending(b => b.RentalStartDate)
            .ToListAsync();

        return new RentalPipelineDto
        {
            ActiveCount = bookings.Count(b => b.BookingStage is "Active" or "Reserved"),
            OverdueCount = bookings.Count(b => b.RentalEndDate.Date < today && b.BookingStage != "Returned"),
            UpcomingCount = bookings.Count(b => b.RentalEndDate.Date >= today && b.RentalEndDate.Date <= today.AddDays(3) && b.BookingStage != "Returned"),
            Rows = bookings.Select(b => new BookingRowViewModel
            {
                BookingId = b.RentalBookingId,
                BookingCode = $"BKG-{b.RentalBookingId:D4}",
                ClientName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}" : "Unknown Client",
                GarmentSummary = b.BookingDetails.Count > 0
                    ? string.Join(", ", b.BookingDetails.Select(d => d.Garment != null ? d.Garment.StyleName : $"Garment #{d.GarmentId}"))
                    : "No garments assigned",
                StartDate = b.RentalStartDate,
                EndDate = b.RentalEndDate,
                RentalFee = b.RentalFee,
                SecurityDeposit = b.SecurityDeposit,
                Stage = b.BookingStage,
                IsOverdue = b.RentalEndDate.Date < today && b.BookingStage != "Returned"
            }).ToList()
        };
    }

    public async Task<List<GarmentPickerRowViewModel>> GetAvailableGarmentsAsync(int companyId, DateTime startDate, DateTime endDate, string search = "")
    {
        await using var db = _contextFactory();

        var query = db.Garments
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId && g.IsActive);

        var list = await query.OrderBy(g => g.StyleName).ToListAsync();

        return list.Select(g => new GarmentPickerRowViewModel
        {
            GarmentEntity = g,
            GarmentId = g.GarmentId,
            Code = g.ItemCode,
            Title = g.StyleName,
            SizeLabel = string.IsNullOrWhiteSpace(g.Size) ? "Standard" : g.Size,
            RentalRate = g.RentalRate,
            SecurityDeposit = g.SecurityDeposit
        }).ToList();
    }

    public async Task ProcessReturnAsync(int bookingId)
    {
        await using var db = _contextFactory();
        var booking = await db.RentalBookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .FirstOrDefaultAsync(b => b.RentalBookingId == bookingId);

        if (booking == null) return;

        booking.BookingStage = "Returned";
        foreach (var detail in booking.BookingDetails)
        {
            if (detail.Garment != null)
            {
                detail.Garment.Status = "In Cleaning";
            }
        }

        await db.SaveChangesAsync();
    }
}