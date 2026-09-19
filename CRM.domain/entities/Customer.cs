namespace CRM.domain.entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty; // Optional
    public string LastName { get; set; } = string.Empty;

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{FirstName} {MiddleName} {LastName}".Trim();


    public string ContactNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public decimal BustSize { get; set; }
    public decimal WaistSize { get; set; }
    public decimal HipSize { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav properties
    public ICollection<RentalBooking> RentalBookings { get; set; } = [];
}