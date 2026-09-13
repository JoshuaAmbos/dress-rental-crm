namespace CRM.domain.entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }

    // Boutique sizing attributes (CRM Contact Management)
    public decimal? BustSize { get; set; }
    public decimal? WaistSize { get; set; }
    public decimal? HipSize { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav properties
    public ICollection<RentalBooking> RentalBookings { get; set; } = new List<RentalBooking>();
}