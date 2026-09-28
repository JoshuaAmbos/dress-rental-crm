namespace CRM.domain.entities;

public class Branch
{
    public int BranchId { get; set; }
    public int CompanyId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Garment> Garments { get; set; } = new List<Garment>();
    public ICollection<RentalBooking> RentalBookings { get; set; } = new List<RentalBooking>();
}