namespace CRM.domain.entities;

public class Garment
{
    public int GarmentId { get; set; }
    public int CompanyId { get; set; }  
    public string ItemCode { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public decimal BustSize { get; set; }
    public decimal WaistSize { get; set; }
    public decimal HipSize { get; set; }
    public decimal RentalRate { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal ReplacementValue { get; set; }
    public string Status { get; set; } = "Available";
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav properties
    public CompanyDatabase? Company { get; set; }
    public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}