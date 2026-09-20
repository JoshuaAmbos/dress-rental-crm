namespace CRM.domain.entities;

public class RentalBooking
{
    public int RentalBookingId { get; set; }
    public int CustomerId { get; set; }
    public int CompanyId { get; set; }

    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public decimal RentalFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? AlterationNotes { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string BookingStage { get; set; } = "Fitting";
    public bool AgreedToTerms { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;

    public ICollection<BookingDetail> BookingDetails { get; set; } = [];
    public ICollection<ServiceIncident> ServiceIncidents { get; set; } = [];
}