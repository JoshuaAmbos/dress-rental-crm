using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRM.api.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.api.Services;

public class RentalBookingService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public RentalBookingService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    // =========================================================================
    // DYNAMIC CONFIGURATION HELPERS (With Resilient Default Fallbacks)
    // =========================================================================
    private static async Task<decimal> GetConfigDecimalAsync(TenantCrmDbContext db, string key, decimal fallback)
    {
        var cfg = await db.SystemConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        return (cfg != null && decimal.TryParse(cfg.ConfigValue, out var val)) ? val : fallback;
    }

    private static async Task<int> GetConfigIntAsync(TenantCrmDbContext db, string key, int fallback)
    {
        var cfg = await db.SystemConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        return (cfg != null && int.TryParse(cfg.ConfigValue, out var val)) ? val : fallback;
    }

    // =========================================================================
    // 1. PIPELINE OVERVIEW QUERY (Multi-Branch & Stage Aware)
    // =========================================================================
    public async Task<RentalPipelineDto> GetPipelineAsync(int companyId, int? branchId = null, string stageFilter = "All", string searchTerm = "")
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

        // Apply branch filter if a specific branch is selected
        if (branchId.HasValue)
        {
            query = query.Where(b => b.BranchId == branchId.Value);
        }

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
            UpcomingCount = allRows.Count(r => r.Stage == "Active" && r.EndDate.Date >= today && r.EndDate.Date <= sevenDaysAhead),
            ReservedCount = allRows.Count(r => r.Stage == "Reserved"),
            FittingCount = allRows.Count(r => r.Stage == "Fitting"),
            ReturnedCount = allRows.Count(r => r.Stage == "Returned"),
            Rows = filteredRows
        };
    }

    // =========================================================================
    // 2. GARMENT AVAILABILITY (Enforces CleaningBufferDays)
    // =========================================================================
    /// <summary>
    /// Returns only garments that are active, not damaged/lost, and free of date overlaps
    /// including the post-return dry-cleaning turnaround buffer.
    /// </summary>
    public async Task<List<GarmentPickerRowViewModel>> GetAvailableGarmentsAsync(int companyId, DateTime startDate, DateTime endDate, string search = "")
    {
        await using var db = _contextFactory();
        var reqStart = startDate.Date;
        var reqEnd = endDate.Date;

        // Dynamically retrieve configured dry-cleaning buffer days (default: 2)
        int bufferDays = await GetConfigIntAsync(db, "CleaningBufferDays", 2);

        // Identify all Garment IDs locked in overlapping bookings (including cleaning buffer)
        var bookedGarmentIds = await db.RentalBookings
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId
                     && b.BookingStage != "Cancelled"
                     && b.BookingStage != "Returned"
                     && b.RentalStartDate.Date <= reqEnd
                     && b.RentalEndDate.Date.AddDays(bufferDays) >= reqStart)
            .SelectMany(b => b.BookingDetails.Select(d => d.GarmentId))
            .Distinct()
            .ToListAsync();

        // Query garments matching search, active, in good repair, and not booked in range
        var query = db.Garments
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId
                     && g.IsActive
                     && g.Status != "Damaged"
                     && g.Status != "Lost"
                     && g.Status != "Retired"
                     && !bookedGarmentIds.Contains(g.GarmentId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(g =>
                g.StyleName.ToLower().Contains(term) ||
                g.ItemCode.ToLower().Contains(term) ||
                g.Category.ToLower().Contains(term) ||
                g.Color.ToLower().Contains(term));
        }

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

    // =========================================================================
    // 3. BOOKING CREATION (Enforces MaxActiveRentalsPerClient & Turnaround Buffer)
    // =========================================================================
    /// <summary>
    /// Creates a new booking, enforces client active lease limits, checks date conflicts with buffers,
    /// and synchronizes Garment.Status to "Reserved".
    /// </summary>
    public async Task<int> CreateBookingFromDraftAsync(int companyId, BookingDraftModel draft)
    {
        if (draft.SelectedCustomer == null)
            throw new InvalidOperationException("Cannot create a booking without a selected customer.");

        if (draft.SelectedGarments == null || draft.SelectedGarments.Count == 0)
            throw new InvalidOperationException("Cannot create a booking without at least one selected garment.");

        await using var db = _contextFactory();

        // 1. Enforce Max Active Leases Rule from System Configurations (Default: 3)
        int maxAllowedRentals = await GetConfigIntAsync(db, "MaxActiveRentalsPerClient", 3);

        int currentActiveCount = await db.RentalBookings
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == draft.SelectedCustomer.CustomerId
                          && b.CompanyId == companyId
                          && b.BookingStage != "Returned"
                          && b.BookingStage != "Cancelled");

        if (currentActiveCount >= maxAllowedRentals)
        {
            throw new InvalidOperationException(
                $"Client '{draft.SelectedCustomer.FirstName} {draft.SelectedCustomer.LastName}' currently holds {currentActiveCount} active lease(s). " +
                $"System configuration limits clients to a maximum of {maxAllowedRentals} concurrent active lease(s).");
        }

        // 2. Defensive validation: ensure no date conflicts including turnaround cleaning buffer
        int bufferDays = await GetConfigIntAsync(db, "CleaningBufferDays", 2);
        var reqStart = draft.RentalStartDate.Date;
        var reqEnd = draft.RentalEndDate.Date;
        var selectedIds = draft.SelectedGarments.Select(g => g.GarmentId).Distinct().ToList();

        var conflictingStyles = await db.RentalBookings
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId
                     && b.BookingStage != "Cancelled"
                     && b.BookingStage != "Returned"
                     && b.RentalStartDate.Date <= reqEnd
                     && b.RentalEndDate.Date.AddDays(bufferDays) >= reqStart
                     && b.BookingDetails.Any(d => selectedIds.Contains(d.GarmentId)))
            .SelectMany(b => b.BookingDetails
                .Where(d => selectedIds.Contains(d.GarmentId) && d.Garment != null)
                .Select(d => d.Garment!.StyleName))
            .Distinct()
            .ToListAsync();

        if (conflictingStyles.Count > 0)
        {
            throw new InvalidOperationException(
                $"The following garment(s) are unavailable for the selected dates (including {bufferDays}-day cleaning turnaround): {string.Join(", ", conflictingStyles)}");
        }

        string notes = draft.AlterationNotes ?? string.Empty;
        if (draft.LoyaltyDiscountAmount > 0)
        {
            notes = $"{notes} [Loyalty perk applied: {draft.LoyaltyTierName} (-₱{draft.LoyaltyDiscountAmount:N2})]".Trim();
        }

        // 3. Instantiate the RentalBooking parent entity
        var booking = new RentalBooking
        {
            CompanyId = companyId,
            CustomerId = draft.SelectedCustomer.CustomerId,
            RentalStartDate = reqStart,
            RentalEndDate = reqEnd,
            RentalFee = draft.TotalRentalFee,
            SecurityDeposit = draft.TotalSecurityDeposit,
            TotalAmount = draft.TotalDue,
            PaymentMethod = draft.SelectedPaymentMethod,
            AlterationNotes = notes,
            BookingStage = "Fitting",
            AgreedToTerms = draft.AgreedToTerms,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Map selected garments into BookingDetail line items
        foreach (var garment in draft.SelectedGarments)
        {
            booking.BookingDetails.Add(new BookingDetail
            {
                GarmentId = garment.GarmentId,
                UnitPrice = garment.RentalRate,
                AlterationNotes = draft.AlterationNotes
            });
        }

        // 5. Load garments and synchronize their physical status to 'Reserved'
        var garmentsToUpdate = await db.Garments
            .Where(g => selectedIds.Contains(g.GarmentId) && g.CompanyId == companyId)
            .ToListAsync();

        foreach (var garment in garmentsToUpdate)
        {
            garment.Status = "Reserved";
        }

        db.RentalBookings.Add(booking);
        await db.SaveChangesAsync();

        return booking.RentalBookingId;
    }

    // =========================================================================
    // 4. RETURN PROCESSING (Calculates DefaultLateFeePerDay)
    // =========================================================================
    /// <summary>
    /// Processes garment return, checks for overdue return status, dynamically computes penalty fees
    /// using DefaultLateFeePerDay, appends audit notes, and transitions garments to 'In Cleaning'.
    /// </summary>
    public async Task<(bool WasOverdue, int DaysLate, decimal LateFeeCharged)> ProcessReturnAsync(int bookingId)
    {
        await using var db = _contextFactory();
        var booking = await db.RentalBookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .FirstOrDefaultAsync(b => b.RentalBookingId == bookingId);

        if (booking == null) return (false, 0, 0m);

        var today = DateTime.Today;
        bool wasOverdue = booking.RentalEndDate.Date < today && booking.BookingStage != "Returned";
        int daysLate = wasOverdue ? (today - booking.RentalEndDate.Date).Days : 0;
        decimal lateFeeCharged = 0m;

        if (wasOverdue && daysLate > 0)
        {
            // Dynamically load configured daily penalty rate (default: 500.00)
            decimal dailyPenaltyRate = await GetConfigDecimalAsync(db, "DefaultLateFeePerDay", 500.00m);
            lateFeeCharged = daysLate * dailyPenaltyRate;

            string auditNote = $"[Return Audit: {daysLate} day(s) overdue. Penalty assessed: ₱{lateFeeCharged:N2} (@ ₱{dailyPenaltyRate:N2}/day)]";
            booking.AlterationNotes = string.IsNullOrWhiteSpace(booking.AlterationNotes)
                ? auditNote
                : $"{booking.AlterationNotes} | {auditNote}";
        }

        booking.BookingStage = "Returned";

        foreach (var detail in booking.BookingDetails)
        {
            if (detail.Garment != null)
            {
                detail.Garment.Status = "In Cleaning";
            }
        }

        await db.SaveChangesAsync();
        return (wasOverdue, daysLate, lateFeeCharged);
    }

    // =========================================================================
    // 5. STAGE UPDATES, CLEANING TRANSITIONS & GARMENT STATUS SYNC
    // =========================================================================
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

            detail.Garment.Status = newStage switch
            {
                "Active" => "Rented",
                "Returned" => "In Cleaning",
                "Reserved" or "Fitting" => "Reserved",
                "Cancelled" => "Available",
                _ => detail.Garment.Status
            };
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

    /// <summary>
    /// Transitions a garment from 'In Cleaning' back to 'Available'.
    /// </summary>
    public async Task CompleteGarmentCleaningAsync(int garmentId)
    {
        await using var db = _contextFactory();
        var garment = await db.Garments.FirstOrDefaultAsync(g => g.GarmentId == garmentId);
        if (garment != null && garment.Status == "In Cleaning")
        {
            garment.Status = "Available";
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Audits and synchronizes all garment statuses based on real-time booking stages and current date.
    /// </summary>
    public async Task SyncGarmentStatusesAsync(int companyId)
    {
        await using var db = _contextFactory();
        var today = DateTime.Today;

        var garments = await db.Garments
            .Where(g => g.CompanyId == companyId && g.IsActive)
            .Include(g => g.BookingDetails)
                .ThenInclude(d => d.RentalBooking)
            .ToListAsync();

        foreach (var garment in garments)
        {
            // Ignore garments manually flagged as damaged, lost, or retired
            if (garment.Status is "Damaged" or "Lost" or "Retired") continue;

            // Find relevant active bookings
            var activeBookings = garment.BookingDetails
                .Select(d => d.RentalBooking)
                .Where(b => b != null && b.BookingStage != "Cancelled")
                .ToList();

            // 1. Is it currently out with a client?
            bool isRented = activeBookings.Any(b =>
                (b.BookingStage == "Active" || (b.BookingStage != "Returned" && b.RentalStartDate.Date <= today && b.RentalEndDate.Date >= today)));

            // 2. Is it in an upcoming reserved or fitting stage?
            bool isReserved = !isRented && activeBookings.Any(b =>
                (b.BookingStage is "Reserved" or "Fitting") ||
                (b.BookingStage != "Returned" && b.RentalStartDate.Date > today));

            // 3. Was it returned and waiting for cleaning?
            bool isCleaning = !isRented && !isReserved && garment.Status == "In Cleaning";

            if (isRented)
                garment.Status = "Rented";
            else if (isReserved)
                garment.Status = "Reserved";
            else if (isCleaning)
                garment.Status = "In Cleaning";
            else
                garment.Status = "Available";
        }

        await db.SaveChangesAsync();
    }
}