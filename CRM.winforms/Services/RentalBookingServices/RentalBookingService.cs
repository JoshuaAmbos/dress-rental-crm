using CRM.domain.entities;
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
        var sevenDaysAhead = today.AddDays(7);

        var query = db.RentalBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .Where(b => b.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(b =>
                (b.Customer != null && (
                    b.Customer.FirstName.ToLower().Contains(term) ||
                    b.Customer.LastName.ToLower().Contains(term) ||
                    (b.Customer.ContactNumber != null && b.Customer.ContactNumber.Contains(term)))) ||
                b.BookingDetails.Any(d => d.Garment != null && (
                    d.Garment.StyleName.ToLower().Contains(term) ||
                    d.Garment.ItemCode.ToLower().Contains(term))));
        }

        var bookings = await query
            .OrderByDescending(b => b.RentalStartDate)
            .ToListAsync();

        var allRows = bookings.Select(b =>
        {
            bool isOverdue = b.RentalEndDate.Date < today && b.BookingStage != "Returned";
            string effectiveStage = isOverdue ? "Overdue" : b.BookingStage;

            return new BookingRowViewModel
            {
                BookingId = b.RentalBookingId,
                BookingCode = $"BKG-{b.RentalBookingId:D4}",
                ClientName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}".Trim() : "Unknown Client",
                GarmentSummary = b.BookingDetails.Count > 0
                    ? string.Join(", ", b.BookingDetails.Select(d => d.Garment != null ? d.Garment.StyleName : $"Garment #{d.GarmentId}"))
                    : "No garments assigned",
                StartDate = b.RentalStartDate,
                EndDate = b.RentalEndDate,
                RentalFee = b.RentalFee,
                SecurityDeposit = b.SecurityDeposit,
                Stage = effectiveStage,
                IsOverdue = isOverdue
            };
        }).ToList();

        var filteredRows = stageFilter switch
        {
            "Reserved" => allRows.Where(r => r.Stage == "Reserved").ToList(),
            "Fitting" => allRows.Where(r => r.Stage == "Fitting").ToList(),
            "Active" => allRows.Where(r => r.Stage == "Active").ToList(),
            "Overdue" => allRows.Where(r => r.IsOverdue).ToList(),
            "Returned" => allRows.Where(r => r.Stage == "Returned").ToList(),
            _ => allRows
        };

        return new RentalPipelineDto
        {
            TotalCount = allRows.Count,
            ActiveCount = allRows.Count(r => r.Stage == "Active"),
            OverdueCount = allRows.Count(r => r.IsOverdue),
            UpcomingCount = allRows.Count(r => r.EndDate.Date >= today && r.EndDate.Date <= sevenDaysAhead && r.Stage != "Returned"),
            ReservedCount = allRows.Count(r => r.Stage == "Reserved"),
            FittingCount = allRows.Count(r => r.Stage == "Fitting"),
            ReturnedCount = allRows.Count(r => r.Stage == "Returned"),
            Rows = filteredRows
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

    public async Task<int> CreateBookingFromDraftAsync(int companyId, BookingDraftModel draft)
    {
        if (draft.SelectedCustomer == null)
            throw new InvalidOperationException("Cannot create a booking without a selected customer.");

        if (draft.SelectedGarments == null || draft.SelectedGarments.Count == 0)
            throw new InvalidOperationException("Cannot create a booking without at least one selected garment.");

        await using var db = _contextFactory();

        // 1. Instantiate the RentalBooking parent entity
        var booking = new RentalBooking
        {
            CompanyId = companyId,
            CustomerId = draft.SelectedCustomer.CustomerId,
            RentalStartDate = draft.RentalStartDate.Date,
            RentalEndDate = draft.RentalEndDate.Date,
            RentalFee = draft.TotalRentalFee,
            SecurityDeposit = draft.TotalSecurityDeposit,
            TotalAmount = draft.TotalDue,
            PaymentMethod = draft.SelectedPaymentMethod,
            AlterationNotes = draft.AlterationNotes,
            BookingStage = "Fitting",
            AgreedToTerms = draft.AgreedToTerms,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Map selected garments into BookingDetail line items
        foreach (var garment in draft.SelectedGarments)
        {
            booking.BookingDetails.Add(new BookingDetail
            {
                GarmentId = garment.GarmentId,
                UnitPrice = garment.RentalRate,
                AlterationNotes = draft.AlterationNotes
            });
        }

        // 3. Save entity graph atomically
        db.RentalBookings.Add(booking);
        await db.SaveChangesAsync();

        return booking.RentalBookingId;
    }

    public async Task UpdateBookingStageAsync(int bookingId, string newStage)
    {
        await using var db = _contextFactory();

        var booking = await db.RentalBookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .FirstOrDefaultAsync(b => b.RentalBookingId == bookingId);

        if (booking == null)
            throw new InvalidOperationException($"Booking with ID {bookingId} not found.");

        booking.BookingStage = newStage;

        foreach (var detail in booking.BookingDetails)
        {
            if (detail.Garment == null) continue;

            switch (newStage)
            {
                case "Active":
                    detail.Garment.Status = "Rented";
                    break;
                case "Returned":
                    detail.Garment.Status = "In Cleaning";
                    break;
                case "Reserved":
                case "Fitting":
                    detail.Garment.Status = "Reserved";
                    break;
                case "Cancelled":
                    detail.Garment.Status = "Available";
                    break;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<RentalBooking?> GetBookingDetailsAsync(int bookingId)
    {
        await using var db = _contextFactory();

        return await db.RentalBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .FirstOrDefaultAsync(b => b.RentalBookingId == bookingId);
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