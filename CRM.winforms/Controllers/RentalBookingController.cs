using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Models.RentalBookingModels;
using CRM.winforms.Services.RentalBookingServices;

namespace CRM.winforms.Controllers;

public class RentalBookingController
{
    private readonly RentalBookingService _bookingService;

    public RentalBookingController(Func<TenantCrmDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _bookingService = new RentalBookingService(contextFactory);
    }

    // 1. Pipeline Queries
    public async Task<RentalPipelineDto> LoadPipelineAsync(int companyId, string stageFilter = "All", string searchTerm = "")
    {
        return await _bookingService.GetPipelineAsync(companyId, stageFilter, searchTerm);
    }

    public async Task<List<GarmentPickerRowViewModel>> LoadAvailableGarmentsAsync(int companyId, DateTime start, DateTime end, string search = "")
    {
        return await _bookingService.GetAvailableGarmentsAsync(companyId, start, end, search);
    }

    // 2. Booking Actions
    public async Task<int> ConfirmBookingAsync(int companyId, BookingDraftModel draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.SelectedCustomer == null)
            throw new InvalidOperationException("A client must be selected before confirming the booking.");

        if (draft.SelectedGarments.Count == 0)
            throw new InvalidOperationException("At least one garment must be selected.");

        return await _bookingService.CreateBookingFromDraftAsync(companyId, draft);
    }

    public async Task<RentalBooking?> GetBookingDetailsAsync(int bookingId)
    {
        return await _bookingService.GetBookingDetailsAsync(bookingId);
    }

    public async Task UpdateBookingStageAsync(int bookingId, string newStage)
    {
        if (string.IsNullOrWhiteSpace(newStage))
            throw new ArgumentException("Stage cannot be empty.", nameof(newStage));

        await _bookingService.UpdateBookingStageAsync(bookingId, newStage);
    }

    public async Task ProcessReturnAsync(int bookingId)
    {
        await _bookingService.ProcessReturnAsync(bookingId);
    }
}