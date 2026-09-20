namespace CRM.winforms.Models.RentalBookingModels;

public class BookingRowViewModel
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string GarmentSummary { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentalFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string Stage { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class RentalPipelineDto
{
    public int ActiveCount { get; set; }
    public int OverdueCount { get; set; }
    public int UpcomingCount { get; set; }

    public int TotalCount { get; set; }
    public int ReservedCount { get; set; }
    public int FittingCount { get; set; }
    public int ReturnedCount { get; set; }

    public List<BookingRowViewModel> Rows { get; set; } = new();
}