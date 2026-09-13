namespace CRM.domain.entities;

public class RentalBooking
{
    public int RentalBookingId { get; set; }
    public int CustomerId { get; set; }
    public string DressDescription { get; set; } = string.Empty;
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public decimal RentalFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? AlterationNotes { get; set; }
    public string BookingStage { get; set; } = "Fitting"; // Fitting, Reserved, Active, Returned
    public bool AgreedToTerms { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav properties
    public Customer Customer { get; set; } = null!;
    public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
    public ICollection<ServiceIncident> ServiceIncidents { get; set; } = new List<ServiceIncident>();
}